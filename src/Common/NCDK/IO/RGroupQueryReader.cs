/*
 * Copyright (C) 2010  Mark Rijnbeek <mark_rynbeek@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may
 * distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Common.Primitives;
using NCDK.IO.Formats;
using NCDK.Isomorphisms.Matchers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace NCDK.IO
{
    /// <summary>
    /// A reader for Symyx' Rgroup files (RGFiles).
    /// </summary>
    /// <remarks>
    /// An RGfile describes a single molecular query with Rgroups.
    /// Each RGfile is a combination of Ctabs defining the root molecule and each
    /// member of each Rgroup in the query.
    ///
    /// <para>The RGFile format is described in the manual
    /// <see href="http://www.symyx.com/downloads/public/ctfile/ctfile.pdf">"CTFile Formats"</see> , Chapter 5.
    /// </para>
    /// </remarks>
    // @cdk.module io
    // @cdk.iooptions
    // @cdk.keyword Rgroup
    // @cdk.keyword R group
    // @cdk.keyword R-group
    // @author Mark Rijnbeek
    public class RGroupQueryReader : DefaultChemObjectReader
    {
        /// <summary>
        /// Private bean style class to capture LOG (logic) lines.
        /// </summary>
        private class RGroupLogic
        {
            public int rgoupNumberRequired;
            public bool restH;
            public string occurence;
        }

        TextReader input = null;

        /// <summary>
        /// Constructs a new RgroupQueryReader that can read RgroupAtomContainerSet
        /// from a given Stream.
        /// <param name="ins">The Stream to read from.</param>
        /// </summary>
        public RGroupQueryReader(Stream ins)
            : this(new StreamReader(ins))
        { }

        /// <summary>
        /// Constructs a new RgroupQueryReader that can read RgroupAtomContainerSet
        /// from a given Reader.
        /// </summary>
        /// <param name="ins">The Reader to read from.</param>
        public RGroupQueryReader(TextReader ins)
        {
            input = ins;
        }

        public override IResourceFormat Format => RGroupQueryFormat.Instance;

        public override bool Accepts(Type type)
        {
            if (typeof(IRGroupQuery).IsAssignableFrom(type))
                return true;
            return false;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    input.Dispose();
                }

                input = null;

                disposedValue = true;
                base.Dispose(disposing);
            }
        }
        #endregion

        /// <summary>
        /// Check input <see cref="IChemObject"/> and proceed to parse.
        /// Accepts/returns <see cref="IChemObject"/> of type <see cref="RGroupQuery"/> only.
        /// </summary>
        /// <returns><see cref="IChemObject"/> read from file</returns>
        /// <param name="obj">class must be of type <see cref="RGroupQuery"/></param>
        public override T Read<T>(T obj)
        {
            return (T)Read((IChemObject)obj);
        }

        private IChemObject Read(IChemObject obj)
        {
            if (obj is RGroupQuery)
            {
                return (IChemObject)ParseRGFile((RGroupQuery)obj);
            }
            else
            {
                throw new CDKException("Reader only supports " + typeof(RGroupQuery).Name + " objects");
            }
        }

        /// <summary>
        /// Parse the RGFile. Uses of <see cref="MDLV2000Reader"/>
        /// to parse individual $CTAB blocks.
        /// </summary>
        /// <param name="rGroupQuery">empty</param>
        /// <returns>populated query</returns>
        private RGroupQuery ParseRGFile(RGroupQuery rGroupQuery)
        {
            var defaultChemObjectBuilder = rGroupQuery.Builder;
            int lineCount = 0;
            string line = "";
            /* Variable to capture the LOG Line(s) */
            var logicDefinitions = new Dictionary<int, RGroupLogic>();

            // Variable to captures attachment order for Rgroups. Contains: - pseudo
            // atom (Rgroup) - map with (integer,bond) meaning "bond" has attachment
            // order "integer" (1,2,3) for the Rgroup The order is based on the atom
            // block, unless there is an AAL line for the pseudo atom.
            var attachmentPoints = new Dictionary<IAtom, IReadOnlyDictionary<int, IBond>>();

            try
            {
                // Process the Header block_________________________________________
                //__________________________________________________________________
                Trace.TraceInformation("Process the Header block");
                CheckLineBeginsWith(input.ReadLine(), "$MDL", ++lineCount);
                CheckLineBeginsWith(input.ReadLine(), "$MOL", ++lineCount);
                CheckLineBeginsWith(input.ReadLine(), "$HDR", ++lineCount);

                for (int i = 1; i <= 3; i++)
                {
                    lineCount++;
                    if (input.ReadLine() == null)
                    {
                        throw new CDKException("RGFile invalid, empty/null header line at #" + lineCount);
                    }
                    //optional: parse header info here (not implemented)
                }
                CheckLineBeginsWith(input.ReadLine(), "$END HDR", ++lineCount);

                string rootStr;
                {
                    //Process the root structure (scaffold)_____________________________
                    //__________________________________________________________________
                    Trace.TraceInformation("Process the root structure (scaffold)");
                    CheckLineBeginsWith(input.ReadLine(), "$CTAB", ++lineCount);
                    //Force header
                    var sb = new StringBuilder(RGroup.RootLabelKey + "\n\n\n");
                    line = input.ReadLine();
                    ++lineCount;
                    while (line != null && !string.Equals(line, "$END CTAB", StringComparison.Ordinal))
                    {
                        sb.Append(line).Append('\n');

                        //LOG lines: Logic, Unsatisfied Sites, Range of Occurrence.
                        if (line.StartsWith("M  LOG", StringComparison.Ordinal))
                        {
                            var tokens = Strings.Tokenize(line);
                            RGroupLogic log = null;

                            log = new RGroupLogic();
                            var rgroupNumber = int.Parse(tokens[3], NumberFormatInfo.InvariantInfo);
                            var tok = tokens[4];
                            log.rgoupNumberRequired = string.Equals(tok, "0", StringComparison.Ordinal) ? 0 : int.Parse(tok, NumberFormatInfo.InvariantInfo);
                            log.restH = string.Equals(tokens[5], "1", StringComparison.Ordinal) ? true : false;
                            tok = "";
                            for (int i = 6; i < tokens.Count; i++)
                            {
                                tok += tokens[i];
                            }
                            log.occurence = tok;
                            logicDefinitions[rgroupNumber] = log;
                        }

                        line = input.ReadLine();
                        ++lineCount;
                    }
                    rootStr = sb.ToString();
                }

                //Let MDL reader process $CTAB block of the root structure.
                var reader = new MDLV2000Reader(new StringReader(rootStr), ChemObjectReaderMode.Strict);
                var root = reader.Read(defaultChemObjectBuilder.NewAtomContainer());
                rGroupQuery.RootStructure = root;

                //Atom attachment order: parse AAL lines first
                using (var rootLinesReader = new StringReader(rootStr))
                { 
                    while ((line = rootLinesReader.ReadLine()) != null)
                    {
                        if (line.StartsWith("M  AAL", StringComparison.Ordinal))
                        {
                            var stAAL = Strings.Tokenize(line);
                            var pos = int.Parse(stAAL[2], NumberFormatInfo.InvariantInfo);
                            var rGroup = root.Atoms[pos - 1];
                            var bondMap = new Dictionary<int, IBond>();
                            for (int i = 4; i < stAAL.Count; i += 2)
                            {
                                pos = int.Parse(stAAL[i], NumberFormatInfo.InvariantInfo);
                                var partner = root.Atoms[pos - 1];
                                var bond = root.GetBond(rGroup, partner);
                                var order = int.Parse(stAAL[i + 1], NumberFormatInfo.InvariantInfo);
                                bondMap[order] = bond;
                                Trace.TraceInformation($"AAL {order} {((IPseudoAtom)rGroup).Label}-{partner.Symbol}");
                            }
                            if (bondMap.Count != 0)
                            {
                                attachmentPoints[rGroup] = bondMap;
                            }
                        }
                    }
                }
                //Deal with remaining attachment points (non AAL)
                foreach (var atom in root.Atoms)
                {
                    if (atom is IPseudoAtom rGroup)
                    {
                        if (rGroup.Label.StartsWithChar('R') 
                         && !rGroup.Label.Equals("R") // only numbered ones
                         && !attachmentPoints.ContainsKey(rGroup))
                        {
                            //Order reflects the order of atoms in the Atom Block
                            int order = 0;
                            var bondMap = new Dictionary<int, IBond>();
                            foreach (var atom2 in root.Atoms)
                            {
                                if (!atom.Equals(atom2))
                                {
                                    foreach (var bond in root.Bonds)
                                    {
                                        if (bond.Contains(atom) && bond.Contains(atom2))
                                        {
                                            bondMap[++order] = bond;
                                            Trace.TraceInformation($"Def {order} {rGroup.Label}-{atom2.Symbol}");
                                            break;
                                        }
                                    }
                                }
                            }
                            if (bondMap.Count != 0)
                            {
                                attachmentPoints[rGroup] = bondMap;
                            }
                        }
                    }
                }
                //Done with attachment points
                rGroupQuery.RootAttachmentPoints = attachmentPoints;
                Trace.TraceInformation($"Attachm.points defined for {attachmentPoints.Count} R# atoms");

                //Process each Rgroup's $CTAB block(s)_____________________________
                //__________________________________________________________________

                //Set up the RgroupLists, one for each unique R# (# = 1..32 max)
                var rGroupDefinitions = new Dictionary<int, RGroupList>();

                foreach (var atom in root.Atoms)
                {
                    if (atom is IPseudoAtom rGroup)
                    {
                        if (RGroupQuery.IsValidRgroupQueryLabel(rGroup.Label))
                        {
                            var rgroupNum = int.Parse(rGroup.Label.Substring(1), NumberFormatInfo.InvariantInfo);
                            var rgroupList = new RGroupList(rgroupNum);
                            if (!rGroupDefinitions.ContainsKey(rgroupNum))
                            {
                                Trace.TraceInformation($"Define Rgroup R{rgroupNum}");
                                RGroupLogic logic = logicDefinitions[rgroupNum];
                                if (logic != null)
                                {
                                    rgroupList.IsRestH = logic.restH;
                                    rgroupList.Occurrence = logic.occurence;
                                    rgroupList.RequiredRGroupNumber = logic.rgoupNumberRequired;
                                }
                                else
                                {
                                    rgroupList.IsRestH = false;
                                    rgroupList.Occurrence = ">0";
                                    rgroupList.RequiredRGroupNumber = 0;
                                }
                                rgroupList.RGroups = new List<RGroup>();
                                rGroupDefinitions[rgroupNum] = rgroupList;
                            }
                        }
                    }
                }

                //Parse all $CTAB blocks per Rgroup (there can be more than one)
                line = input.ReadLine();
                ++lineCount;
                bool hasMoreRGP = true;
                while (hasMoreRGP)
                {
                    CheckLineBeginsWith(line, "$RGP", lineCount);
                    line = input.ReadLine();
                    ++lineCount;
                    Trace.TraceInformation("line for num is " + line);
                    int rgroupNum = int.Parse(line.Trim(), NumberFormatInfo.InvariantInfo);
                    line = input.ReadLine();
                    ++lineCount;

                    bool hasMoreCTAB = true;
                    while (hasMoreCTAB)
                    {
                        CheckLineBeginsWith(line, "$CTAB", lineCount);
                        var sb = new StringBuilder(RGroup.MakeLabel(rgroupNum) + "\n\n\n");
                        line = input.ReadLine();
                        while (line != null && !line.StartsWith("$END CTAB", StringComparison.Ordinal))
                        {
                            sb.Append(line).Append('\n');
                            line = input.ReadLine();
                            ++lineCount;
                        }
                        var groupStr = sb.ToString();
                        reader = new MDLV2000Reader(new StringReader(groupStr), ChemObjectReaderMode.Strict);
                        var group = reader.Read(defaultChemObjectBuilder.NewAtomContainer());
                        var rGroup = new RGroup
                        {
                            Group = group
                        };

                        //Parse the Rgroup's attachment points (APO)
                        using (var groupLinesReader = new StringReader(groupStr))
                        {
                            while ((line = groupLinesReader.ReadLine()) != null)
                            {
                                if (line.StartsWith("M  APO", StringComparison.Ordinal))
                                {
                                    var stAPO = Strings.Tokenize(line);
                                    for (int i = 3; i < stAPO.Count; i += 2)
                                    {
                                        var pos = int.Parse(stAPO[i], NumberFormatInfo.InvariantInfo);
                                        var apo = int.Parse(stAPO[i + 1], NumberFormatInfo.InvariantInfo);
                                        var at = group.Atoms[pos - 1];
                                        switch (apo)
                                        {
                                            case 1:
                                                rGroup.FirstAttachmentPoint = at;
                                                break;
                                            case 2:
                                                rGroup.SecondAttachmentPoint = at;
                                                break;
                                            case 3:
                                                {
                                                    rGroup.FirstAttachmentPoint = at;
                                                    rGroup.SecondAttachmentPoint = at;
                                                }
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                        var rList = rGroupDefinitions[rgroupNum];
                        if (rList == null)
                        {
                            throw new CDKException("R" + rgroupNum + " not defined but referenced in $RGP.");
                        }
                        else
                        {
                            rList.RGroups.Add(rGroup);
                        }
                        line = input.ReadLine();
                        ++lineCount;
                        if (line.StartsWith("$END RGP", StringComparison.Ordinal))
                        {
                            Trace.TraceInformation("end of RGP block");
                            hasMoreCTAB = false;
                        }
                    }

                    line = input.ReadLine();
                    ++lineCount;
                    if (line.StartsWith("$END MOL", StringComparison.Ordinal))
                    {
                        hasMoreRGP = false;
                    }
                }

                rGroupQuery.RGroupDefinitions = rGroupDefinitions;
                Trace.TraceInformation("Number of lines was " + lineCount);
                return rGroupQuery;
            }
            catch (CDKException exception)
            {
                string error = $"CDK Error while parsing line {lineCount}: {line} -> {exception.Message}";
                Trace.TraceError(error);
                Debug.WriteLine(exception);
                throw;
            }
            catch (Exception exception)
            {
                if (!(exception is IOException || exception is ArgumentException))
                    throw;
                Console.Error.WriteLine(exception.StackTrace);
                var error = exception.GetType() + "Error while parsing line " + lineCount + ": " + line + " -> " + exception.Message;
                Trace.TraceError(error);
                Debug.WriteLine(exception);
                throw new CDKException(error, exception);
            }
        }

        /// <summary>
        /// Checks that a given line starts as expected, according to RGFile format.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="expect"></param>
        /// <param name="lineCount"></param>
        private static void CheckLineBeginsWith(string line, string expect, int lineCount)
        {
            if (line == null)
            {
                throw new CDKException("RGFile invalid, empty/null line at #" + lineCount);
            }
            if (!line.StartsWith(expect, StringComparison.Ordinal))
            {
                throw new CDKException("RGFile invalid, line #" + lineCount + " should start with:" + expect + ".");
            }
        }
    }
}

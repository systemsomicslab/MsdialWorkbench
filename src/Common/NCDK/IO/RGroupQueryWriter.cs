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

using NCDK.IO.Formats;
using NCDK.Isomorphisms.Matchers;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace NCDK.IO
{
    /// <summary>
    /// A writer for Symyx' Rgroup files (RGFiles).
    /// </summary>
    /// <remarks>
    /// An RGfile describes a single molecular query with Rgroups.
    /// Each RGfile is a combination of Ctabs defining the root molecule and each
    /// member of each Rgroup in the query.
    /// 
    /// This class relies on the <see cref="MDLV2000Writer"/> to
    /// create CTAB data blocks.
    /// </remarks>
    // @cdk.module io
    // @cdk.iooptions
    // @cdk.keyword Rgroup
    // @cdk.keyword R group
    // @cdk.keyword R-group
    // @author Mark Rijnbeek
    public class RGroupQueryWriter : DefaultChemObjectWriter
    {
        private TextWriter writer;
        private const string LSEP = "\n";

        /// <summary>
        /// Constructs a new writer that can write an <see cref="IRGroupQuery"/>
        /// to the Symx RGFile format.
        /// </summary>
        /// <param name="output">The Writer to write to</param>
        public RGroupQueryWriter(TextWriter output)
        {
            writer = output;
        }

        public RGroupQueryWriter(Stream output)
            : this(new StreamWriter(output))
        { }

        /// <summary>
        /// Returns true for accepted input types.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public override bool Accepts(Type type)
        {
            if (typeof(IRGroupQuery).IsAssignableFrom(type)) return true;
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
                    writer.Dispose();
                }

                writer = null;

                disposedValue = true;
                base.Dispose(disposing);
            }
        }
        #endregion

        /// <summary>
        /// Produces a CTAB block for an atomContainer, without the header lines.
        /// </summary>
        /// <param name="atomContainer"></param>
        /// <returns>CTAB block</returns>
        private static string GetCTAB(IAtomContainer atomContainer)
        {
            StringWriter strWriter = new StringWriter();
            MDLV2000Writer mdlWriter = new MDLV2000Writer(strWriter);
            mdlWriter.Write(atomContainer);
            try
            {
                mdlWriter.Close();
            }
            catch (IOException)
            {
                // FIXME
            }
            string ctab = strWriter.ToString();
            //strip of the individual header, as we have one super header instead.
            for (int line = 1; line <= 3; line++)
            {
                ctab = ctab.Substring(ctab.IndexOf(LSEP, StringComparison.Ordinal) + (LSEP.Length));
            }
            return ctab;
        }

        /// <summary>
        /// Returns output format.
        /// </summary>
        public override IResourceFormat Format => RGroupQueryFormat.Instance;

        /// <summary>
        /// The actual writing of the output.
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="CDKException">could not write RGroup query</exception>
        public override void Write(IChemObject obj)
        {
            if (!(obj is IRGroupQuery))
            {
                throw new CDKException("Only IRGroupQuery input is accepted.");
            }
            try
            {
                IRGroupQuery rGroupQuery = (IRGroupQuery)obj;
                string now = DateTime.UtcNow.ToString("MMddyyHHmm", DateTimeFormatInfo.InvariantInfo);
                IAtomContainer rootAtc = rGroupQuery.RootStructure;

                //Construct header
                var rootBlock = new StringBuilder();
                string header = "$MDL  REV  1   " + now + LSEP + "$MOL" + LSEP + "$HDR" + LSEP
                        + "  Rgroup query file (RGFile)" + LSEP + "  CDK    " + now + "2D" + LSEP + LSEP + "$END HDR"
                        + LSEP + "$CTAB";
                rootBlock.Append(header).Append(LSEP);

                //Construct the root structure, the scaffold
                string rootCTAB = GetCTAB(rootAtc);
                rootCTAB = rootCTAB.Replace(LSEP + "M  END" + LSEP, "");
                rootBlock.Append(rootCTAB).Append(LSEP);

                //Write the root's LOG lines
                foreach (var rgrpNum in rGroupQuery.RGroupDefinitions.Keys)
                {
                    RGroupList rgList = rGroupQuery.RGroupDefinitions[rgrpNum];
                    int restH = rgList.IsRestH ? 1 : 0;
                    string logLine = "M  LOG" + MDLV2000Writer.FormatMDLInt(1, 3) + MDLV2000Writer.FormatMDLInt(rgrpNum, 4)
                            + MDLV2000Writer.FormatMDLInt(rgList.RequiredRGroupNumber, 4)
                            + MDLV2000Writer.FormatMDLInt(restH, 4) + "   " + rgList.Occurrence;
                    rootBlock.Append(logLine).Append(LSEP);
                }

                //AAL lines are optional, they are needed for R-atoms with multiple bonds to the root
                //for which the order of the attachment points can not be implicitly derived
                //from the order in the atom block. See CT spec for more on that.
                foreach (var rgroupAtom in rGroupQuery.RootAttachmentPoints.Keys)
                {
                    var rApo = rGroupQuery.RootAttachmentPoints[rgroupAtom];
                    if (rApo.Count > 1)
                    {
                        int prevPos = -1;
                        int apoIdx = 1;
                        bool implicitlyOrdered = true;
                        while (rApo.ContainsKey(apoIdx) && implicitlyOrdered)
                        {
                            IAtom partner = rApo[apoIdx].GetOther(rgroupAtom);
                            for (int atIdx = 0; atIdx < rootAtc.Atoms.Count; atIdx++)
                            {
                                if (rootAtc.Atoms[atIdx].Equals(partner))
                                {
                                    if (atIdx < prevPos) implicitlyOrdered = false;
                                    prevPos = atIdx;
                                    break;
                                }
                            }
                            apoIdx++;
                        }
                        if (!implicitlyOrdered)
                        {
                            StringBuilder aalLine = new StringBuilder("M  AAL");
                            for (int atIdx = 0; atIdx < rootAtc.Atoms.Count; atIdx++)
                            {
                                if (rootAtc.Atoms[atIdx].Equals(rgroupAtom))
                                {
                                    aalLine.Append(MDLV2000Writer.FormatMDLInt((atIdx + 1), 4));
                                    aalLine.Append(MDLV2000Writer.FormatMDLInt(rApo.Count, 3));

                                    apoIdx = 1;
                                    while (rApo.ContainsKey(apoIdx))
                                    {
                                        IAtom partner = rApo[apoIdx].GetOther(rgroupAtom);

                                        for (int a = 0; a < rootAtc.Atoms.Count; a++)
                                        {
                                            if (rootAtc.Atoms[a].Equals(partner))
                                            {
                                                aalLine.Append(MDLV2000Writer.FormatMDLInt(a + 1, 4));
                                                aalLine.Append(MDLV2000Writer.FormatMDLInt(apoIdx, 4));
                                            }
                                        }
                                        apoIdx++;
                                    }
                                }
                            }
                            rootBlock.Append(aalLine.ToString()).Append(LSEP);
                        }
                    }
                }

                rootBlock.Append("M  END").Append(LSEP).Append("$END CTAB").Append(LSEP);

                //Construct each R-group block
                var rgpBlock = new StringBuilder();
                foreach (var rgrpNum in rGroupQuery.RGroupDefinitions.Keys)
                {
                    var rgrpList = rGroupQuery.RGroupDefinitions[rgrpNum].RGroups;
                    if (rgrpList != null && rgrpList.Count != 0)
                    {
                        rgpBlock.Append("$RGP").Append(LSEP); ;
                        rgpBlock.Append(MDLV2000Writer.FormatMDLInt(rgrpNum, 4)).Append(LSEP);

                        foreach (var rgroup in rgrpList)
                        {
                            //CTAB block
                            rgpBlock.Append("$CTAB").Append(LSEP);
                            string ctab = GetCTAB(rgroup.Group);
                            ctab = ctab.Replace(LSEP + "M  END" + LSEP, "");
                            rgpBlock.Append(ctab).Append(LSEP);

                            //The APO line
                            IAtom firstAttachmentPoint = rgroup.FirstAttachmentPoint;
                            IAtom secondAttachmentPoint = rgroup.SecondAttachmentPoint;
                            int apoCount = 0;
                            if (firstAttachmentPoint != null)
                            {
                                var apoLine = new StringBuilder();
                                for (int atIdx = 0; atIdx < rgroup.Group.Atoms.Count; atIdx++)
                                {
                                    if (rgroup.Group.Atoms[atIdx].Equals(firstAttachmentPoint))
                                    {
                                        apoLine.Append(MDLV2000Writer.FormatMDLInt((atIdx + 1), 4));
                                        apoCount++;
                                        if (secondAttachmentPoint != null
                                                && secondAttachmentPoint.Equals(firstAttachmentPoint))
                                        {
                                            apoLine.Append(MDLV2000Writer.FormatMDLInt(3, 4));
                                        }
                                        else
                                        {
                                            apoLine.Append(MDLV2000Writer.FormatMDLInt(1, 4));
                                        }
                                    }
                                }
                                if (secondAttachmentPoint != null && !secondAttachmentPoint.Equals(firstAttachmentPoint))
                                {
                                    for (int atIdx = 0; atIdx < rgroup.Group.Atoms.Count; atIdx++)
                                    {
                                        if (rgroup.Group.Atoms[atIdx].Equals(secondAttachmentPoint))
                                        {
                                            apoCount++;
                                            apoLine.Append(MDLV2000Writer.FormatMDLInt((atIdx + 1), 4));
                                            apoLine.Append(MDLV2000Writer.FormatMDLInt(2, 4));
                                        }
                                    }
                                }
                                if (apoCount > 0)
                                {
                                    apoLine.Insert(0, "M  APO" + MDLV2000Writer.FormatMDLInt(apoCount, 3));
                                    rgpBlock.Append(apoLine).Append(LSEP);
                                }
                            }

                            rgpBlock.Append("M  END").Append(LSEP);
                            rgpBlock.Append("$END CTAB").Append(LSEP);
                        }
                        rgpBlock.Append("$END RGP").Append(LSEP);
                    }
                }
                rgpBlock.Append("$END MOL").Append(LSEP);

                writer.Write(rootBlock.ToString());
                writer.Write(rgpBlock.ToString());
                writer.Flush();

            }
            catch (IOException e)
            {
                Console.Error.WriteLine(e.StackTrace);
                throw new CDKException("Unexpected exception when writing RGFile" + LSEP + e.Message);
            }
        }
    }
}

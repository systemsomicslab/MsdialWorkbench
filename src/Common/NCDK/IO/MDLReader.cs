/* Copyright (C) 1997-2007  Christoph Steinbeck <steinbeck@users.sourceforge.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All we ask is that proper credit is given for our work, which includes
 *  - but is not limited to - adding the above copyright notice to the beginning
 *  of your source code files, and to any copyright notice that you may distribute
 *  with programs based on this work.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Common.Primitives;
using NCDK.IO.Formats;
using NCDK.IO.Setting;
using NCDK.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace NCDK.IO
{
    /// <summary>
    /// Reads a molecule from the original MDL MOL or SDF file <token>cdk-cite-DAL92</token>. An SD files
    /// is read into a <see cref="IChemSequence"/> of <see cref="IChemModel"/>'s. Each ChemModel will contain one
    /// Molecule. If the MDL molfile contains a property block, the <see cref="MDLV2000Reader"/> should be
    /// used.
    /// <para>
    /// If all z coordinates are 0.0, then the xy coordinates are taken as
    /// 2D, otherwise the coordinates are read as 3D.
    /// </para>
    /// <para>
    /// The title of the MOL file is read and can be retrieved with:
    /// <code>
    ///   molecule.GetProperty&lt;string&gt;(CDKPropertyName.Title);
    /// </code>
    /// </para>
    /// </summary>
    /// <seealso cref="MDLV2000Reader"/>
    // @cdk.module io
    // @cdk.iooptions
    // @author     steinbeck
    // @author     Egon Willighagen
    // @cdk.created    2000-10-02
    // @cdk.keyword    file format, MDL molfile
    // @cdk.keyword    file format, SDF
    [Obsolete("This reader is only for molfiles without a version tag, typically the most " 
        + "common molfile now encountered is V2000 and the " + nameof(MDLV2000Reader) + " should be used "
        + "instead. The " + nameof(MDLV2000Reader) + " reader can actually read files missing the version tag when "
        + "in relaxed mode.")]
    public class MDLReader : DefaultChemObjectReader
    {
        TextReader input = null;

        private BooleanIOSetting forceReadAs3DCoords;
        private static readonly Regex TRAILING_SPACE = new Regex("\\s+$", RegexOptions.Compiled);

        /// <summary>
        ///  Constructs a new MDLReader that can read Molecule from a given Stream.
        /// </summary>
        /// <param name="input">The Stream to read from</param>
        public MDLReader(Stream input)
            : this(input, ChemObjectReaderMode.Relaxed)
        {
        }

        public MDLReader(Stream input, ChemObjectReaderMode mode)
            : this(new StreamReader(input))
        {
            base.ReaderMode = mode;
        }

        /// <summary>
        /// Constructs a new MDLReader that can read Molecule from a given Reader.
        /// </summary>
        /// <param name="input">The Reader to read from</param>
        public MDLReader(TextReader input)
            : this(input, ChemObjectReaderMode.Relaxed)
        {
        }

        public MDLReader(TextReader input, ChemObjectReaderMode mode)
        {
            base.ReaderMode = mode;
            this.input = input;
            InitIOSettings();
        }

        public override IResourceFormat Format => MDLFormat.Instance;

        public override bool Accepts(Type type)
        {
            if (typeof(IChemFile).IsAssignableFrom(type)) return true;
            if (typeof(IChemModel).IsAssignableFrom(type)) return true;
            if (typeof(IAtomContainer).IsAssignableFrom(type)) return true;
            return false;
        }

        /// <summary>
        ///  Takes an object which subclasses <see cref="IChemObject"/>, e.g. Molecule, and will read
        ///  this (from file, database, internet etc). If the specific implementation
        ///  does not support a specific <see cref="IChemObject"/> it will throw an Exception.
        /// </summary>
        /// <param name="obj">The object that subclasses <see cref="IChemObject"/></param>
        /// <returns>The IChemObject read</returns>
        /// <exception cref="CDKException"></exception>
        public override T Read<T>(T obj)
        {
            if (obj is IChemFile)
            {
                return (T)ReadChemFile((IChemFile)obj);
            }
            else if (obj is IChemModel)
            {
                return (T)ReadChemModel((IChemModel)obj);
            }
            else if (obj is IAtomContainer)
            {
                return (T)ReadMolecule((IAtomContainer)obj);
            }
            else
            {
                throw new CDKException("Only supported are ChemFile and Molecule.");
            }
        }

        private IChemModel ReadChemModel(IChemModel chemModel)
        {
            IChemObjectSet<IAtomContainer> setOfMolecules = chemModel.MoleculeSet;
            if (setOfMolecules == null)
            {
                setOfMolecules = chemModel.Builder.NewAtomContainerSet();
            }
            IAtomContainer m = ReadMolecule(chemModel.Builder.NewAtomContainer());
            if (m != null)
            {
                setOfMolecules.Add(m);
            }
            chemModel.MoleculeSet = setOfMolecules;
            return chemModel;
        }

        /// <summary>
        /// Read a ChemFile from a file in MDL SDF format.
        /// </summary>
        /// <returns>The ChemFile that was read from the MDL file.</returns>
        private IChemFile ReadChemFile(IChemFile chemFile)
        {
            IChemSequence chemSequence = chemFile.Builder.NewChemSequence();

            IChemModel chemModel = chemFile.Builder.NewChemModel();
            IChemObjectSet<IAtomContainer> setOfMolecules = chemFile.Builder.NewAtomContainerSet();
            IAtomContainer m = ReadMolecule(chemFile.Builder.NewAtomContainer());
            if (m != null)
            {
                setOfMolecules.Add(m);
            }
            chemModel.MoleculeSet = setOfMolecules;
            chemSequence.Add(chemModel);

            setOfMolecules = chemFile.Builder.NewAtomContainerSet();
            chemModel = chemFile.Builder.NewChemModel();
            string str;
            try
            {
                string line;
                while ((line = input.ReadLine()) != null)
                {
                    Debug.WriteLine($"line: {line}");
                    // apparently, this is a SDF file, continue with
                    // reading mol files
                    str = line;
                    if (string.Equals(line, "M  END", StringComparison.Ordinal))
                        continue;
                    if (string.Equals(str, "$$$$", StringComparison.Ordinal))
                    {
                        m = ReadMolecule(chemFile.Builder.NewAtomContainer());

                        if (m != null)
                        {
                            setOfMolecules.Add(m);

                            chemModel.MoleculeSet = setOfMolecules;
                            chemSequence.Add(chemModel);

                            setOfMolecules = chemFile.Builder.NewAtomContainerSet();
                            chemModel = chemFile.Builder.NewChemModel();

                        }
                    }
                    else
                    {
                        // here the stuff between 'M  END' and '$$$$'
                        if (m != null)
                        {
                            // ok, the first lines should start with '>'
                            string fieldName = null;
                            if (str.StartsWith("> ", StringComparison.Ordinal))
                            {
                                // ok, should extract the field name
                                int index = str.IndexOf('<', 2);
                                if (index != -1)
                                {
                                    int index2 = str.Substring(index).IndexOf('>');
                                    if (index2 != -1)
                                    {
                                        fieldName = str.Substring(index + 1, index2 - 1);
                                    }
                                }
                                // end skip all other lines
                                while ((line = input.ReadLine()) != null && line.StartsWithChar('>'))
                                {
                                    Debug.WriteLine($"data header line: {line}");
                                }
                            }
                            if (line == null)
                            {
                                throw new CDKException("Expecting data line here, but found null!");
                            }
                            string data = line;
                            while ((line = input.ReadLine()) != null && line.Trim().Length > 0)
                            {
                                if (string.Equals(line, "$$$$", StringComparison.Ordinal))
                                {
                                    Trace.TraceError($"Expecting data line here, but found end of molecule: {line}");
                                    break;
                                }
                                Debug.WriteLine($"data line: {line}");
                                data += line;
                                // preserve newlines, unless the line is exactly 80 chars; in that case it
                                // is assumed to continue on the next line. See MDL documentation.
                                if (line.Length < 80) data += "\n";
                            }
                            if (fieldName != null)
                            {
                                Trace.TraceInformation($"fieldName, data: {fieldName}, {data}");
                                m.SetProperty(fieldName, data);
                            }
                        }
                    }
                }
            }
            catch (CDKException)
            {
                throw;
            }
            catch (Exception exception)
            {
                if (!(exception is IOException || exception is ArgumentException))
                    throw;
                string error = "Error while parsing SDF";
                Trace.TraceError(error);
                Debug.WriteLine(exception);
                throw new CDKException(error, exception);
            }
            try
            {
                input.Close();
            }
            catch (Exception exc)
            {
                string error = "Error while closing file: " + exc.Message;
                Trace.TraceError(error);
                throw new CDKException(error, exc);
            }

            chemFile.Add(chemSequence);
            return chemFile;
        }

        /// <summary>
        ///  Read a Molecule from a file in MDL sd format
        /// </summary>
        /// <returns>The Molecule that was read from the MDL file.</returns>
        private IAtomContainer ReadMolecule(IAtomContainer molecule)
        {
            Debug.WriteLine("Reading new molecule");
            int linecount = 0;
            int atoms = 0;
            int bonds = 0;
            int atom1 = 0;
            int atom2 = 0;
            int order = 0;
            BondStereo stereo = BondStereo.None;
            int RGroupCounter = 1;
            int Rnumber = 0;
            double x = 0.0;
            double y = 0.0;
            double z = 0.0;
            double totalX = 0.0;
            double totalY = 0.0;
            double totalZ = 0.0;
            //int[][] conMat = Array.Empty<int>()[0];
            //string help;
            IAtom atom;
            string line = "";

            try
            {
                var isotopeFactory = CDK.IsotopeFactory;
                Trace.TraceInformation("Reading header");
                line = input.ReadLine();
                linecount++;
                if (line == null)
                {
                    return null;
                }
                Debug.WriteLine("Line " + linecount + ": " + line);

                if (line.StartsWith("$$$$", StringComparison.Ordinal))
                {
                    Debug.WriteLine("File is empty, returning empty molecule");
                    return molecule;
                }
                if (line.Length > 0)
                {
                    molecule.Title = line;
                }
                line = input.ReadLine();
                linecount++;
                Debug.WriteLine("Line " + linecount + ": " + line);
                line = input.ReadLine();
                linecount++;
                Debug.WriteLine("Line " + linecount + ": " + line);
                if (line.Length > 0)
                {
                    molecule.SetProperty(CDKPropertyName.Remark, line);
                }

                Trace.TraceInformation("Reading rest of file");
                line = input.ReadLine();
                linecount++;
                Debug.WriteLine("Line " + linecount + ": " + line);
                if (ReaderMode == ChemObjectReaderMode.Strict)
                {
                    if (line.Contains("V2000") || line.Contains("v2000"))
                    {
                        throw new CDKException("This file must be read with the MDLV2000Reader.");
                    }
                    if (line.Contains("V3000") || line.Contains("v3000"))
                    {
                        throw new CDKException("This file must be read with the MDLV3000Reader.");
                    }
                }
                atoms = int.Parse(Strings.Substring(line, 0, 3).Trim(), NumberFormatInfo.InvariantInfo);
                Debug.WriteLine($"Atomcount: {atoms}");
                bonds = int.Parse(Strings.Substring(line, 3, 3).Trim(), NumberFormatInfo.InvariantInfo);
                Debug.WriteLine($"Bondcount: {bonds}");

                // read ATOM block
                Trace.TraceInformation("Reading atom block");
                for (int f = 0; f < atoms; f++)
                {
                    line = input.ReadLine();
                    linecount++;
                    var trailingSpaceMatcher = TRAILING_SPACE.Match(line);
                    if (trailingSpaceMatcher.Success)
                    {
                        HandleError("Trailing space found", linecount, trailingSpaceMatcher.Index, trailingSpaceMatcher.Index + trailingSpaceMatcher.Length);
                        line = Strings.Substring(line, 0, trailingSpaceMatcher.Index);
                    }
                    x = double.Parse(Strings.Substring(line, 0, 10).Trim(), NumberFormatInfo.InvariantInfo);
                    y = double.Parse(Strings.Substring(line, 10, 10).Trim(), NumberFormatInfo.InvariantInfo);
                    z = double.Parse(Strings.Substring(line, 20, 10).Trim(), NumberFormatInfo.InvariantInfo);
                    // *all* values should be zero, not just the sum
                    totalX += Math.Abs(x);
                    totalY += Math.Abs(y);
                    totalZ += Math.Abs(z);
                    Debug.WriteLine("Coordinates: " + x + "; " + y + "; " + z);
                    string element = Strings.Substring(line, 31, 3).Trim();
                    if (line.Length < 34)
                    {
                        HandleError("Element atom type does not follow V2000 format type should of length three"
                                + " and padded with space if required", linecount, 31, 34);
                    }

                    Debug.WriteLine($"Atom type: {element}");
                    if (isotopeFactory.IsElement(element))
                    {
                        atom = isotopeFactory.Configure(molecule.Builder.NewAtom(element));
                    }
                    else if (string.Equals("A", element, StringComparison.Ordinal))
                    {
                        atom = molecule.Builder.NewPseudoAtom(element);
                    }
                    else if (string.Equals("Q", element, StringComparison.Ordinal))
                    {
                        atom = molecule.Builder.NewPseudoAtom(element);
                    }
                    else if (string.Equals("*", element, StringComparison.Ordinal))
                    {
                        atom = molecule.Builder.NewPseudoAtom(element);
                    }
                    else if (string.Equals("LP", element, StringComparison.Ordinal))
                    {
                        atom = molecule.Builder.NewPseudoAtom(element);
                    }
                    else if (string.Equals("L", element, StringComparison.Ordinal))
                    {
                        atom = molecule.Builder.NewPseudoAtom(element);
                    }
                    else if (element.Length > 0 && element[0] == 'R')
                    {
                        Debug.WriteLine("Atom ", element, " is not an regular element. Creating a PseudoAtom.");
                        //check if the element is R
                        string rn = element.Substring(1);
                        if (int.TryParse(rn, out Rnumber))
                        {
                            RGroupCounter = Rnumber;
                        }
                        else
                        {
                            Rnumber = RGroupCounter;
                            RGroupCounter++;
                        }
                        element = "R" + Rnumber;
                        atom = molecule.Builder.NewPseudoAtom(element);
                    }
                    else
                    {
                        if (ReaderMode == ChemObjectReaderMode.Strict)
                        {
                            throw new CDKException(
                                    "Invalid element type. Must be an existing element, or one in: A, Q, L, LP, *.");
                        }
                        atom = molecule.Builder.NewPseudoAtom(element);
                    }

                    // store as 3D for now, convert to 2D (if totalZ == 0.0) later
                    atom.Point3D = new Vector3(x, y, z);

                    // parse further fields
                    if (line.Length >= 36)
                    {
                        string massDiffString = Strings.Substring(line, 34, 2).Trim();
                        Debug.WriteLine($"Mass difference: {massDiffString}");
                        if (!(atom is IPseudoAtom))
                        {
                            try
                            {
                                int massDiff = int.Parse(massDiffString, NumberFormatInfo.InvariantInfo);
                                if (massDiff != 0)
                                {
                                    var major = CDK.IsotopeFactory.GetMajorIsotope(element);
                                    atom.AtomicNumber = major.AtomicNumber + massDiff;
                                }
                            }
                            catch (Exception exception)
                            {
                                if (!(exception is FormatException || exception is IOException))
                                    throw;
                                Trace.TraceError("Could not parse mass difference field");
                            }
                        }
                        else
                        {
                            Trace.TraceError("Cannot set mass difference for a non-element!");
                        }
                    }
                    else
                    {
                        HandleError("Mass difference is missing", linecount, 34, 36);
                    }

                    if (line.Length >= 39)
                    {
                        string chargeCodeString = Strings.Substring(line, 36, 3).Trim();
                        Debug.WriteLine($"Atom charge code: {chargeCodeString}");
                        int chargeCode = int.Parse(chargeCodeString, NumberFormatInfo.InvariantInfo);
                        if (chargeCode == 0)
                        {
                            // uncharged species
                        }
                        else if (chargeCode == 1)
                        {
                            atom.FormalCharge = +3;
                        }
                        else if (chargeCode == 2)
                        {
                            atom.FormalCharge = +2;
                        }
                        else if (chargeCode == 3)
                        {
                            atom.FormalCharge = +1;
                        }
                        else if (chargeCode == 4)
                        {
                        }
                        else if (chargeCode == 5)
                        {
                            atom.FormalCharge = -1;
                        }
                        else if (chargeCode == 6)
                        {
                            atom.FormalCharge = -2;
                        }
                        else if (chargeCode == 7)
                        {
                            atom.FormalCharge = -3;
                        }
                    }
                    else
                    {
                        HandleError("Atom charge count is empty", linecount, 35, 39);
                    }

                    if (line.Length >= 64)
                    {
                        // read the mmm field as position 61-63
                        string reactionAtomIDString = Strings.Substring(line, 60, 3).Trim();
                        Debug.WriteLine($"Parsing mapping id: {reactionAtomIDString}");
                        try
                        {
                            int reactionAtomID = int.Parse(reactionAtomIDString, NumberFormatInfo.InvariantInfo);
                            if (reactionAtomID != 0)
                            {
                                atom.SetProperty(CDKPropertyName.AtomAtomMapping, reactionAtomID);
                            }
                        }
                        catch (Exception exception)
                        {
                            Trace.TraceError("Mapping number ", reactionAtomIDString, " is not an integer.");
                            Debug.WriteLine(exception);
                        }
                    }

                    //shk3: This reads shifts from after the molecule. I don't think this is an official format, but I saw it frequently 80=>78 for alk
                    if (line.Length >= 78)
                    {
                        double shift = double.Parse(Strings.Substring(line, 69, 11).Trim(), NumberFormatInfo.InvariantInfo);
                        atom.SetProperty("first shift", shift);
                    }
                    if (line.Length >= 87)
                    {
                        double shift = double.Parse(Strings.Substring(line, 79, 8).Trim(), NumberFormatInfo.InvariantInfo);
                        atom.SetProperty("second shift", shift);
                    }

                    molecule.Atoms.Add(atom);
                }

                // convert to 2D, if totalZ == 0
                if (totalX == 0.0 && totalY == 0.0 && totalZ == 0.0)
                {
                    Trace.TraceInformation("All coordinates are 0.0");
                    foreach (var atomToUpdate in molecule.Atoms)
                    {
                        atomToUpdate.Point3D = null;
                    }
                }
                else if (totalZ == 0.0 && !forceReadAs3DCoords.IsSet)
                {
                    Trace.TraceInformation("Total 3D Z is 0.0, interpreting it as a 2D structure");
                    IEnumerator<IAtom> atomsToUpdate = molecule.Atoms.GetEnumerator();
                    while (atomsToUpdate.MoveNext())
                    {
                        IAtom atomToUpdate = (IAtom)atomsToUpdate.Current;
                        Vector3? p3d = atomToUpdate.Point3D;
                        atomToUpdate.Point2D = new Vector2(p3d.Value.X, p3d.Value.Y);
                        atomToUpdate.Point3D = null;
                    }
                }

                // read BOND block
                Trace.TraceInformation("Reading bond block");
                for (int f = 0; f < bonds; f++)
                {
                    line = input.ReadLine();
                    linecount++;
                    atom1 = int.Parse(Strings.Substring(line, 0, 3).Trim(), NumberFormatInfo.InvariantInfo);
                    atom2 = int.Parse(Strings.Substring(line, 3, 3).Trim(), NumberFormatInfo.InvariantInfo);
                    order = int.Parse(Strings.Substring(line, 6, 3).Trim(), NumberFormatInfo.InvariantInfo);
                    if (line.Length > 12)
                    {
                        int mdlStereo = int.Parse(Strings.Substring(line, 9, 3).Trim(), NumberFormatInfo.InvariantInfo);
                        if (mdlStereo == 1)
                        {
                            // MDL up bond
                            stereo = BondStereo.Up;
                        }
                        else if (mdlStereo == 6)
                        {
                            // MDL down bond
                            stereo = BondStereo.Down;
                        }
                        else if (mdlStereo == 0)
                        {
                            // bond has no stereochemistry
                            stereo = BondStereo.None;
                        }
                        else if (mdlStereo == 4)
                        {
                            //MDL up or down bond
                            stereo = BondStereo.UpOrDown;
                        }
                        else if (mdlStereo == 3)
                        {
                            //MDL e or z undefined
                            stereo = BondStereo.EOrZ;
                        }
                    }
                    else
                    {
                        Trace.TraceWarning("Missing expected stereo field at line: " + line);
                    }
                    Debug.WriteLine("Bond: " + atom1 + " - " + atom2 + "; order " + order);
                    // interpret CTfile's special bond orders
                    IAtom a1 = molecule.Atoms[atom1 - 1];
                    IAtom a2 = molecule.Atoms[atom2 - 1];
                    IBond newBond = null;
                    if (order >= 1 && order <= 3)
                    {
                        BondOrder cdkOrder = BondOrder.Single;
                        if (order == 2) cdkOrder = BondOrder.Double;
                        if (order == 3) cdkOrder = BondOrder.Triple;
                        if (stereo != BondStereo.None)
                        {
                            newBond = molecule.Builder.NewBond(a1, a2, cdkOrder, stereo);
                        }
                        else
                        {
                            newBond = molecule.Builder.NewBond(a1, a2, cdkOrder);
                        }
                    }
                    else if (order == 4)
                    {
                        // aromatic bond
                        if (stereo != BondStereo.None)
                        {
                            newBond = molecule.Builder.NewBond(a1, a2, BondOrder.Single, stereo);
                        }
                        else
                        {
                            newBond = molecule.Builder.NewBond(a1, a2, BondOrder.Single);
                        }
                        // mark both atoms and the bond as aromatic
                        newBond.IsAromatic = true;
                        a1.IsAromatic = true;
                        a2.IsAromatic = true;
                    }
                    molecule.Bonds.Add(newBond);
                }

            }
            catch (Exception exception)
            {
                if (!(exception is IOException
                    | exception is CDKException
                    | exception is ArgumentException))
                    throw;
                string error = "Error while parsing line " + linecount + ": " + line + " -> " + exception.Message;
                Trace.TraceError(error);
                Debug.WriteLine(exception);
                throw new CDKException(error, exception);
            }
            return molecule;
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

        private void InitIOSettings()
        {
            forceReadAs3DCoords = IOSettings.Add(new BooleanIOSetting("ForceReadAs3DCoordinates", Importance.Low,
                "Should coordinates always be read as 3D?", "false"));
        }

        public void CustomizeJob()
        {
            ProcessIOSettingQuestion(forceReadAs3DCoords);
        }
    }
}

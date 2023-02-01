/* Copyright (C) 1997-2007  The Chemistry Development Kit (CDK) project
 *                    2009  Egon Willighagen <egonw@users.sf.net>
 *                    2010  Mark Rijnbeek <mark_rynbeek@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
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
using NCDK.IO.Setting;
using NCDK.Isomorphisms.Matchers;
using NCDK.Sgroups;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NCDK.IO
{
    /// <summary>
    /// Writes MDL molfiles, which contains a single molecule (see <token>cdk-cite-DAL92</token>).
    /// </summary>
    /// <example>
    /// For writing a MDL molfile you can this code:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.IO.MDLV2000Writer_Example.cs"]/*' />
    /// The writer has two IO settings: one for writing 2D coordinates, even if
    /// 3D coordinates are given for the written data; the second writes aromatic
    /// bonds as bond type 4, which is, strictly speaking, a query bond type, but
    /// my many tools used to reflect aromaticity. The full IO setting API is
    /// explained in CDK News <token>cdk-cite-WILLIGHAGEN2004</token>. One programmatic option
    /// to set the option for writing 2D coordinates looks like:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.IO.MDLV2000Writer_Example.cs+listener"]/*' />
    /// </example>
    // @cdk.module io
    // @cdk.iooptions
    // @cdk.keyword file format, MDL molfile
    public class MDLV2000Writer : DefaultChemObjectWriter
    {
        public const string OptForceWriteAs2DCoordinates = "ForceWriteAs2DCoordinates";
        public const string OptWriteMajorIsotopes = "WriteMajorIsotopes";
        public const string OptWriteAromaticBondTypes = "WriteAromaticBondTypes";
        public const string OptWriteQueryFormatValencies = "WriteQueryFormatValencies";
        public const string OptWriteDefaultProperties = "WriteDefaultProperties";
        public const string OptProgramName = "ProgramName";

        // regular expression to capture R groups with attached numbers
        private static readonly Regex NUMERED_R_GROUP = new Regex("R(\\d+)", RegexOptions.Compiled);

        /// <summary>
        /// Enumeration of all valid radical values.
        /// </summary>
        internal class SpinMultiplicity
        {
            public static readonly SpinMultiplicity None = new SpinMultiplicity(0, 0);
            public static readonly SpinMultiplicity Monovalent = new SpinMultiplicity(2, 1);
            public static readonly SpinMultiplicity DivalentSinglet = new SpinMultiplicity(1, 2);
            public static readonly SpinMultiplicity DivalentTriplet = new SpinMultiplicity(3, 2);

            /// <summary>
            /// Radical value for the spin multiplicity in the properties block.
            /// </summary>
            public int Value { get; private set; }

            /// <summary>
            /// The number of single electrons that correspond to the spin multiplicity.
            /// </summary>
            public int SingleElectrons { get; private set; }

            private SpinMultiplicity(int value, int singleElectrons)
            {
                Value = value;
                SingleElectrons = singleElectrons;
            }

            /// <summary>
            /// Create a SpinMultiplicity instance for the specified value.
            /// </summary>
            /// <param name="value">input value (in the property block)</param>
            /// <returns>instance</returns>
            /// <exception cref="CDKException">unknown spin multiplicity value</exception>
            public static SpinMultiplicity OfValue(int value)
            {
                switch (value)
                {
                    case 0:
                        return None;
                    case 1:
                        return DivalentSinglet;
                    case 2:
                        return Monovalent;
                    case 3:
                        return DivalentTriplet;
                    default:
                        throw new CDKException("unknown spin multiplicity: " + value);
                }
            }
        }

        // number of entries on line; value = 1 to 8
        private const int NN8 = 8;

        // spacing between entries on line
        private const int WIDTH = 3;

        private BooleanIOSetting ForceWriteAs2DCoords;

        private BooleanIOSetting writeMajorIsotopes;

        // The next two options are MDL Query format options, not really
        // belonging to the MDLV2000 format, and will be removed when
        // a MDLV2000QueryWriter is written.

        /// <summary>
        /// Should aromatic bonds be written as bond type 4? If true, this makes the
        /// output a query file.
        /// </summary>
        private BooleanIOSetting WriteAromaticBondTypes;

        /* Should atomic valencies be written in the Query format. */
        [Obsolete]
        private BooleanIOSetting WriteQueryFormatValencies;

        private BooleanIOSetting writeDefaultProps;

        private StringIOSetting programNameOpt;

        private TextWriter writer;

        /// <summary>
        /// Used only for InitIOSettings
        /// </summary>
        internal MDLV2000Writer()
            : this((TextWriter)null)
        { }

        /// <summary>
        /// Constructs a new MDLWriter that can write an <see cref="IAtomContainer"/>
        /// to the MDL molfile format.
        /// </summary>
        /// <param name="writer">The Writer to write to</param>
        public MDLV2000Writer(TextWriter writer)
        {
            this.writer = writer;
            InitIOSettings();
        }

        /// <summary>
        /// Constructs a new MDLWriter that can write an <see cref="IAtomContainer"/>
        /// to a given Stream.
        /// </summary>
        /// <param name="output">The Stream to write to</param>
        public MDLV2000Writer(Stream output)
            : this(new StreamWriter(output, Encoding.UTF8))
        { }

        public override IResourceFormat Format => MDLFormat.Instance;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (writer != null)
                        writer.Dispose();
                }

                writer = null;

                disposedValue = true;
                base.Dispose(disposing);
            }
        }
        #endregion

        public override bool Accepts(Type type)
        {
            if (typeof(IAtomContainer).IsAssignableFrom(type))
                return true;
            if (typeof(IChemFile).IsAssignableFrom(type))
                return true;
            if (typeof(IChemModel).IsAssignableFrom(type))
                return true;
            return false;
        }

        /// <summary>
        /// Writes a <see cref="IChemObject"/> to the MDL molfile formated output.
        /// It can only output ChemObjects of type <see cref="IChemFile"/>,
        /// <see cref="IChemObject"/> and <see cref="IAtomContainer"/>.
        /// </summary>
        /// <param name="obj"><see cref="IChemObject"/> to write</param>
        /// <see cref="Accepts(Type)"/>
        public override void Write(IChemObject obj)
        {
            CustomizeJob();
            try
            {
                switch (obj)
                {
                    case IChemFile cf:
                        WriteChemFile(cf);
                        return;
                    case IChemModel cm:
                        var file = obj.Builder.NewChemFile();
                        var sequence = cm.Builder.NewChemSequence();
                        sequence.Add(cm);
                        file.Add(sequence);
                        WriteChemFile((IChemFile)file);
                        return;
                    case IAtomContainer ac:
                        WriteMolecule(ac);
                        return;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                Debug.WriteLine(ex);
                throw new CDKException($"Exception while writing MDL file: {ex.Message}", ex);
            }
            throw new CDKException("Only supported is writing of IChemFile, IChemModel, and IAtomContainer objects.");
        }

        private void WriteChemFile(IChemFile file)
        {
            var bigPile = file.Builder.NewAtomContainer();
            foreach (var container in ChemFileManipulator.GetAllAtomContainers(file))
            {
                bigPile.Add(container);
                if (container.Title != null)
                {
                    if (bigPile.Title != null)
                        bigPile.Title = bigPile.Title + "; " + container.Title;
                    else
                        bigPile.Title = container.Title;
                }
                if (container.GetProperty<string>(CDKPropertyName.Remark) != null)
                {
                    if (bigPile.GetProperty<string>(CDKPropertyName.Remark) != null)
                        bigPile.SetProperty(CDKPropertyName.Remark, bigPile.GetProperty<string>(CDKPropertyName.Remark) + "; " + container.GetProperty<string>(CDKPropertyName.Remark));
                    else
                        bigPile.SetProperty(CDKPropertyName.Remark, container.GetProperty<string>(CDKPropertyName.Remark));
                }
            }
            WriteMolecule(bigPile);
        }

        private string GetProgName()
        {
            var progname = programNameOpt.Setting;
            if (progname == null)
                return "        ";
            else if (progname.Length > 8)
                return progname.Substring(0, 8);
            else if (progname.Length < 8)
                return new string(' ', 8 - progname.Length) + progname;
            else
                return progname;
        }

        /// <summary>
        /// Writes a Molecule to an Stream in MDL sdf format.
        /// </summary>
        /// <param name="container">Molecule that is written to an Stream</param>
        public void WriteMolecule(IAtomContainer container)
        {
            int dim = GetNumberOfDimensions(container);
            var line = new StringBuilder();
            SortedDictionary<int, int> rgroups = null;
            SortedDictionary<int, string> aliases = null;
            // write header block
            // lines get shortened to 80 chars, that's in the spec
            string title = container.Title;
            if (title == null)
                title = "";
            if (title.Length > 80)
                title = title.Substring(0, 80);
            writer.Write(title);
            writer.Write('\n');

            // From CTX spec This line has the format:
            // IIPPPPPPPPMMDDYYHHmmddSSssssssssssEEEEEEEEEEEERRRRRR (FORTRAN:
            // A2<--A8--><---A10-->A2I2<--F10.5-><---F12.5--><-I6-> ) User's first
            // and last initials (l), program name (P), date/time (M/D/Y,H:m),
            // dimensional codes (d), scaling factors (S, s), energy (E) if modeling
            // program input, internal registry number (R) if input through MDL
            // form. A blank line can be substituted for line 2.
            writer.Write("  ");
            writer.Write(GetProgName());
            writer.Write(DateTime.Now.ToUniversalTime().ToString("MMddyyHHmm", DateTimeFormatInfo.InvariantInfo));
            if (dim != 0)
            {
                writer.Write(dim.ToString(NumberFormatInfo.InvariantInfo));
                writer.Write('D');
            }
            writer.Write('\n');

            string comment = container.GetProperty<string>(CDKPropertyName.Remark);
            if (comment == null)
                comment = "";
            if (comment.Length > 80)
                comment = comment.Substring(0, 80);
            writer.Write(comment);
            writer.Write('\n');

            // index stereo elements for setting atom parity values
            var atomstereo = new Dictionary<IAtom, ITetrahedralChirality>();
            var atomindex = new Dictionary<IAtom, int>();
            foreach (var element in container.StereoElements)
                if (element is ITetrahedralChirality)
                    atomstereo[((ITetrahedralChirality)element).ChiralAtom] = (ITetrahedralChirality)element;
            foreach (var atom in container.Atoms)
                atomindex[atom] = atomindex.Count;

            // write Counts line
            line.Append(FormatMDLInt(container.Atoms.Count, 3));
            line.Append(FormatMDLInt(container.Bonds.Count, 3));
            line.Append("  0  0");
            // we mark all stereochemistry to absolute for now
            line.Append(!atomstereo.Any() ? "  0" : "  1");
            line.Append("  0  0  0  0  0999 V2000");
            writer.Write(line.ToString());
            writer.Write('\n');

            // write Atom block
            for (int f = 0; f < container.Atoms.Count; f++)
            {
                IAtom atom = container.Atoms[f];
                line.Clear();
                switch (dim)
                {
                    case 0:
                        // if no coordinates available, then output a number
                        // of zeros
                        line.Append("    0.0000    0.0000    0.0000 ");
                        break;
                    case 2:
                        if (atom.Point2D != null)
                        {
                            line.Append(FormatMDLFloat(atom.Point2D.Value.X));
                            line.Append(FormatMDLFloat(atom.Point2D.Value.Y));
                            line.Append("    0.0000 ");
                        }
                        else
                        {
                            line.Append("    0.0000    0.0000    0.0000 ");
                        }
                        break;
                    case 3:
                        if (atom.Point3D != null)
                        {
                            line.Append(FormatMDLFloat((float)atom.Point3D.Value.X));
                            line.Append(FormatMDLFloat((float)atom.Point3D.Value.Y));
                            line.Append(FormatMDLFloat((float)atom.Point3D.Value.Z));
                            line.Append(" ");
                        }
                        else
                        {
                            line.Append("    0.0000    0.0000    0.0000 ");
                        }
                        break;
                }
                //according to http://www.google.co.uk/url?sa=t&ct=res&cd=2&url=http%3A%2F%2Fwww.mdl.com%2Fdownloads%2Fpublic%2Fctfile%2Fctfile.pdf&ei=MsJjSMbjAoyq1gbmj7zCDQ&usg=AFQjCNGaJSvH4wYy4FTXIaQ5f7hjoTdBAw&sig2=eSfruNOSsdMFdlrn7nhdAw an R group is written as R#
                if (container.Atoms[f] is IPseudoAtom pseudoAtom)
                {
                    var label = pseudoAtom.Label;
                    if (label == null) // set to empty string if null
                        label = "";

                    // firstly check if it's a numbered R group
                    var matcher = NUMERED_R_GROUP.Match(label);
                    if (pseudoAtom.Symbol.Equals("R", StringComparison.Ordinal) && !string.IsNullOrEmpty(label) && matcher.Success)
                    {
                        line.Append("R# ");
                        if (rgroups == null)
                        {
                            // we use a tree map to ensure the output order is always the same
                            rgroups = new SortedDictionary<int, int>();
                        }
                        rgroups[f + 1] = int.Parse(matcher.Groups[1].Value, NumberFormatInfo.InvariantInfo);
                    }
                    // not a numbered R group - note the symbol may still be R
                    else
                    {
                        // note: no distinction made between alias and pseudo atoms - normally
                        //       aliases maintain their original symbol while pseudo atoms are
                        //       written with a 'A' in the atom block

                        // if the label is longer then 3 characters we need
                        // to use an alias.
                        if (label.Length > 3)
                        {

                            if (aliases == null)
                                aliases = new SortedDictionary<int, string>();

                            aliases[f + 1] = label; // atom index to alias

                            line.Append(FormatMDLString(atom.Symbol, 3));

                        }
                        else
                        { // label is short enough to fit in the atom block

                            // make sure it's not empty
                            if (!string.IsNullOrEmpty(label))
                                line.Append(FormatMDLString(label, 3));
                            else
                                line.Append(FormatMDLString(atom.Symbol, 3));
                        }
                    }
                }
                else
                {
                    line.Append(FormatMDLString(container.Atoms[f].Symbol, 3));
                }

                // atom properties
                var atomprops = new int[12];
                atomprops[0] = DetermineIsotope(atom);
                atomprops[1] = DetermineCharge(container, atom);
                atomprops[2] = DetermineStereoParity(container, atomstereo, atomindex, atom);
                atomprops[5] = DetermineValence(container, atom);
                atomprops[9] = DetermineAtomMap(atom);
                line.Append(FormatMDLInt(atomprops[0], 2)); // dd (mass-number)
                line.Append(FormatMDLInt(atomprops[1], 3)); // ccc (charge)
                int last = atomprops.Length - 1;
                if (!writeDefaultProps.IsSet)
                {
                    while (last >= 0)
                    {
                        if (atomprops[last] != 0)
                            break;
                        last--;
                    }
                    // matches BIOVIA syntax
                    if (last >= 2 && last < 5)
                        last = 5;
                }
                for (int i = 2; i <= last; i++)
                    line.Append(FormatMDLInt(atomprops[i], 3));
                line.Append('\n');
                writer.Write(line.ToString());
            }

            // write Bond block
            foreach (var bond in container.Bonds)
            {
                line.Length = 0;
                if (bond.Atoms.Count != 2)
                {
                    Trace.TraceWarning("Skipping bond with more/less than two atoms: " + bond);
                }
                else
                {
                    if (bond.Stereo == BondStereo.UpInverted 
                     || bond.Stereo == BondStereo.DownInverted
                     || bond.Stereo == BondStereo.UpOrDownInverted)
                    {
                        // turn around atom coding to correct for inversed stereo
                        line.Append(FormatMDLInt(atomindex[bond.End] + 1, 3));
                        line.Append(FormatMDLInt(atomindex[bond.Begin] + 1, 3));
                    }
                    else
                    {
                        line.Append(FormatMDLInt(atomindex[bond.Begin] + 1, 3));
                        line.Append(FormatMDLInt(atomindex[bond.End] + 1, 3));
                    }
                    int bondType = 0;

                    if (bond is QueryBond qbond)
                    {
                        var e = qbond.Expression;
                        switch (e.GetExprType())
                        {
                            case ExprType.AliphaticElement:
                            case ExprType.Order:
                                bondType = e.Value;
                                break;
                            case ExprType.IsAromatic:
                                bondType = 4;
                                break;
                            case ExprType.SingleOrDouble:
                                bondType = 5;
                                break;
                            case ExprType.SingleOrAromatic:
                                bondType = 6;
                                break;
                            case ExprType.DoubleOrAromatic:
                                bondType = 7;
                                break;
                            case ExprType.True:
                                bondType = 8;
                                break;
                            case ExprType.Or:
                                // SINGLE_OR_DOUBLE
                                if (e.Equals(new Expr(ExprType.AliphaticOrder, 1).Or(new Expr(ExprType.AliphaticOrder, 2))) ||
                                    e.Equals(new Expr(ExprType.AliphaticOrder, 2).Or(new Expr(ExprType.AliphaticOrder, 1))))
                                    bondType = 5;
                                // SINGLE_OR_AROMATIC
                                else if (e.Equals(new Expr(ExprType.AliphaticOrder, 1).Or(new Expr(ExprType.IsAromatic))) ||
                                    e.Equals(new Expr(ExprType.IsAromatic).Or(new Expr(ExprType.AliphaticOrder, 1))))
                                    bondType = 6;
                                // DOUBLE_OR_AROMATIC
                                else if (e.Equals(new Expr(ExprType.AliphaticOrder, 2).Or(new Expr(ExprType.IsAromatic))) ||
                                         e.Equals(new Expr(ExprType.IsAromatic).Or(new Expr(ExprType.AliphaticOrder, 2))))
                                    bondType = 6;
                                break;
                            default:
                                throw new ArgumentException("Unsupported bond type!");
                        }
                    }
                    else
                    {
                        switch (bond.Order)
                        {
                            case BondOrder.Single:
                            case BondOrder.Double:
                            case BondOrder.Triple:
                                if (WriteAromaticBondTypes.IsSet && bond.IsAromatic)
                                    bondType = 4;
                                else
                                    bondType = bond.Order.Numeric();
                                break;
                            case BondOrder.Unset:
                                if (bond.IsAromatic)
                                {
                                    if (!WriteAromaticBondTypes.IsSet)
                                        throw new CDKException($"Bond at index {container.Bonds.IndexOf(bond)} was an unspecific aromatic bond which should only be used for queries in Molfiles. These can be written if desired by enabling the option '{nameof(WriteAromaticBondTypes)}'.");
                                    bondType = 4;
                                }
                                break;
                        }
                    }

                    if (bondType == 0)
                        throw new CDKException($"Bond at index={container.Bonds.IndexOf(bond)} is not supported by Molfile, bond={bond.Order}");

                    line.Append(FormatMDLInt(bondType, 3));
                    line.Append("  ");
                    switch (bond.Stereo)
                    {
                        case BondStereo.Up:
                        case BondStereo.UpInverted:
                            line.Append("1");
                            break;
                        case BondStereo.Down:
                        case BondStereo.DownInverted:
                            line.Append("6");
                            break;
                        case BondStereo.UpOrDown:
                        case BondStereo.UpOrDownInverted:
                            line.Append("4");
                            break;
                        case BondStereo.EOrZ:
                            line.Append("3");
                            break;
                        default:
                            line.Append("0");
                            break;
                    }
                    if (writeDefaultProps.IsSet)
                        line.Append("  0  0  0 ");
                    line.Append('\n');
                    writer.Write(line.ToString());
                }
            }

            // Write Atom Value
            for (int i = 0; i < container.Atoms.Count; i++)
            {
                var atom = container.Atoms[i];
                if (atom.GetProperty<object>(CDKPropertyName.Comment) != null
                 && atom.GetProperty<object>(CDKPropertyName.Comment) is string
                 && atom.GetProperty<string>(CDKPropertyName.Comment).Trim().Length != 0)
                {
                    writer.Write("V  ");
                    writer.Write(FormatMDLInt(i + 1, 3));
                    writer.Write(" ");
                    writer.Write(atom.GetProperty<string>(CDKPropertyName.Comment));
                    writer.Write('\n');
                }
            }

            // write formal atomic charges
            for (int i = 0; i < container.Atoms.Count; i++)
            {
                var atom = container.Atoms[i];
                int? charge = atom.FormalCharge;
                if (charge != null && charge != 0)
                {
                    writer.Write("M  CHG  1 ");
                    writer.Write(FormatMDLInt(i + 1, 3));
                    writer.Write(" ");
                    writer.Write(FormatMDLInt(charge.Value, 3));
                    writer.Write('\n');
                }
            }

            // write radical information
            if (container.SingleElectrons.Count > 0)
            {
                var atomIndexSpinMap = new SortedDictionary<int, SpinMultiplicity>();
                for (int i = 0; i < container.Atoms.Count; i++)
                {
                    var eCount = container.GetConnectedSingleElectrons(container.Atoms[i]).Count();
                    switch (eCount)
                    {
                        case 0:
                            continue;
                        case 1:
                            atomIndexSpinMap[i] = SpinMultiplicity.Monovalent;
                            break;
                        case 2:
                            // information loss, divalent but singlet or triplet?
                            atomIndexSpinMap[i] = SpinMultiplicity.DivalentSinglet;
                            break;
                        default:
                            Debug.WriteLine($"Invalid number of radicals found: {eCount}");
                            break;
                    }
                }
                var iterator = atomIndexSpinMap.GetEnumerator();
                for (int i = 0; i < atomIndexSpinMap.Count; i += NN8)
                {
                    if (atomIndexSpinMap.Count - i <= NN8)
                    {
                        writer.Write("M  RAD" + FormatMDLInt(atomIndexSpinMap.Count - i, WIDTH));
                        iterator.MoveNext();
                        WriteRadicalPattern(iterator, 0);
                    }
                    else
                    {
                        writer.Write("M  RAD" + FormatMDLInt(NN8, WIDTH));
                        iterator.MoveNext();
                        WriteRadicalPattern(iterator, 0);
                    }
                    writer.Write('\n');
                }
            }

            // write formal isotope information
            for (int i = 0; i < container.Atoms.Count; i++)
            {
                var atom = container.Atoms[i];
                if (!(atom is IPseudoAtom))
                {
                    var atomicMass = atom.MassNumber;
                    if (!writeMajorIsotopes.IsSet && IsMajorIsotope(atom))
                        atomicMass = null;
                    if (atomicMass != null)
                    {
                        writer.Write("M  ISO  1 ");
                        writer.Write(FormatMDLInt(i + 1, 3));
                        writer.Write(" ");
                        writer.Write(FormatMDLInt(atomicMass.Value, 3));
                        writer.Write('\n');
                    }
                }
            }

            //write RGP line (max occurrence is 16 data points per line)
            if (rgroups != null)
            {
                var rgpLine = new StringBuilder();
                int cnt = 0;

                // the order isn't guarantied but as we index with the atom
                // number this isn't an issue
                foreach (var e in rgroups)
                {
                    rgpLine.Append(FormatMDLInt(e.Key, 4));
                    rgpLine.Append(FormatMDLInt(e.Value, 4));
                    cnt++;
                    if (cnt == 8)
                    {
                        rgpLine.Insert(0, "M  RGP" + FormatMDLInt(cnt, 3));
                        writer.Write(rgpLine.ToString());
                        writer.Write('\n');
                        rgpLine = new StringBuilder();
                        cnt = 0;
                    }
                }
                if (cnt != 0)
                {
                    rgpLine.Insert(0, "M  RGP" + FormatMDLInt(cnt, 3));
                    writer.Write(rgpLine.ToString());
                    writer.Write('\n');
                }
            }

            // write atom aliases
            if (aliases != null)
            {
                foreach (var e in aliases)
                {
                    writer.Write("A" + FormatMDLInt(e.Key, 5));
                    writer.Write('\n');

                    var label = e.Value;

                    // fixed width file - doubtful someone would have a label > 70 but trim if they do
                    if (label.Length > 70)
                        label = label.Substring(0, 70);

                    writer.Write(label);
                    writer.Write('\n');
                }
            }

            WriteSgroups(container, writer, atomindex);

            // close molecule
            writer.Write("M  END");
            writer.Write('\n');
            writer.Flush();
        }

        // 0 = uncharged or value other than these, 1 = +3, 2 = +2, 3 = +1,
        // 4 = doublet radical, 5 = -1, 6 = -2, 7 = -3
        private int DetermineCharge(IAtomContainer mol, IAtom atom)
        {
            var q = atom.FormalCharge ?? 0;
            switch (q)
            {
                case -3: return 7;
                case -2: return 6;
                case -1: return 5;
                case 0:
                    if (mol.GetConnectedSingleElectrons(atom).Count() == 1)
                        return 4;
                    return 0;
                case +1: return 3;
                case +2: return 2;
                case +3: return 1;
            }
            return 0;
        }

        private int DetermineIsotope(IAtom atom)
        {
            var mass = atom.MassNumber;
            IIsotope major = null;
            if (mass == null)
                return 0;
            try
            {
                major = CDK.IsotopeFactory.GetMajorIsotope(atom.Symbol);
            }
            catch (IOException)
            {
                // ignored
            }
            if (!writeMajorIsotopes.IsSet
             && major != null 
             && mass.Equals(major.MassNumber))
                mass = null;
            if (mass != null)
            {
                mass -= major != null ? major.MassNumber : 0;
                return mass >= -3 && mass <= 4 ? (int)mass : 0;
            }
            return 0;
        }

        private int DetermineAtomMap(IAtom atom)
        {
            var amap = atom.GetProperty<object>(CDKPropertyName.AtomAtomMapping);
            if (amap == null)
                return 0;
            if (amap is int?)
                return (int)amap;
            else
            {
                if (amap is string)
                {
                    try
                    {
                        return int.Parse((string)amap);
                    }
                    catch (Exception)
                    {
                        //ignored
                    }
                }
                Trace.TraceWarning($"Skipping non-integer atom map: {amap} type:{amap}");
                return 0;
            }
        }

        private int DetermineValence(IAtomContainer container, IAtom atom)
        {
            var explicitValence = (int)AtomContainerManipulator.GetBondOrderSum(container, atom);
            var charge = atom.FormalCharge ?? 0;
            var element = atom.AtomicNumber;
            int valence = 0;

            var implied = MDLValence.ImplicitValence(element, charge, explicitValence);
            int actual;
            if (atom.ImplicitHydrogenCount != null)
                actual = explicitValence + atom.ImplicitHydrogenCount.Value;
            else if (atom.Valency != null)
                actual = atom.Valency.Value;
            else
                return 0;
            if (implied != actual)
            {
                if (actual == 0)
                    return 15;
                else if (actual > 0 && actual < 15)
                    return actual;
            }

            return valence;
        }

        private int DetermineStereoParity(
            IAtomContainer container,
            Dictionary<IAtom, ITetrahedralChirality> atomstereo,
            Dictionary<IAtom, int> atomindex, 
            IAtom atom)
        {
            if (!atomstereo.TryGetValue(atom, out ITetrahedralChirality tc))
                return 0;
            var parity = tc.Stereo == TetrahedralStereo.Clockwise ? 1 : 2;
            var focus = tc.ChiralAtom;
            var carriers = tc.Ligands;
            int hidx = -1;
            for (int i = 0; i < 4; i++)
            {
                // hydrogen position
                if (carriers[i].Equals(focus) || carriers[i].AtomicNumber == 1)
                {
                    if (hidx >= 0)
                        parity = 0;
                    hidx = i;
                }
            }
            if (parity != 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    for (int j = i + 1; j < 4; j++)
                    {
                        int a = atomindex[carriers[i]];
                        int b = atomindex[carriers[j]];
                        if (i == hidx)
                            a = container.Atoms.Count;
                        if (j == hidx)
                            b = container.Atoms.Count;
                        if (a > b)
                            parity ^= 0x3;
                    }
                }
            }
            return parity;
        }

        private bool IsMajorIsotope(IAtom atom)
        {
            if (atom.MassNumber == null)
                return false;

            var major = CDK.IsotopeFactory.GetMajorIsotope(atom.Symbol);
            return major != null && major.MassNumber.Equals(atom.MassNumber);
        }

        private static void WriteSgroups(IAtomContainer container, TextWriter writer, Dictionary<IAtom, int> atomidxs)
        {
            var sgroups = container.GetCtabSgroups();
            if (sgroups == null)
                return;

            // going to modify
            sgroups = new List<Sgroup>(sgroups);

            // remove non-ctab Sgroups 
            {
                var removes = new List<Sgroup>();
                foreach (var e in sgroups.Where(n => n.Type == SgroupType.ExtMulticenter))
                    removes.Add(e);
                foreach (var e in removes)
                    sgroups.Remove(e);
            }

            foreach (var wrapSgroups in Wrap(sgroups, 8))
            {
                // Declare the Sgroup type
                writer.Write("M  STY");
                writer.Write(FormatMDLInt(wrapSgroups.Count, 3));
                foreach (var sgroup in wrapSgroups)
                {
                    writer.Write(' ');
                    writer.Write(FormatMDLInt(1 + sgroups.IndexOf(sgroup), 3));
                    writer.Write(' ');
                    writer.Write(sgroup.Type.Key());
                }
                writer.Write('\n');
            }

            // Sgroup output is non-compact for now - but valid
            for (int id = 1; id <= sgroups.Count; id++)
            {
                Sgroup sgroup = sgroups[id - 1];

                // Sgroup Atom List
                foreach (var atoms in Wrap(sgroup.Atoms, 15))
                {
                    writer.Write("M  SAL ");
                    writer.Write(FormatMDLInt(id, 3));
                    writer.Write(FormatMDLInt(atoms.Count, 3));
                    foreach (var atom in atoms)
                    {
                        writer.Write(' ');
                        writer.Write(FormatMDLInt(1 + atomidxs[atom], 3));
                    }
                    writer.Write('\n');
                }

                // Sgroup Bond List
                foreach (var bonds in Wrap(sgroup.Bonds, 15))
                {
                    writer.Write("M  SBL ");
                    writer.Write(FormatMDLInt(id, 3));
                    writer.Write(FormatMDLInt(bonds.Count, 3));
                    foreach (var bond in bonds)
                    {
                        writer.Write(' ');
                        writer.Write(FormatMDLInt(1 + container.Bonds.IndexOf(bond), 3));
                    }
                    writer.Write('\n');
                }

                // Sgroup Parent List
                foreach (var parents in Wrap(sgroup.Parents.ToReadOnlyList(), 8))
                {
                    writer.Write("M  SPL");
                    writer.Write(FormatMDLInt(parents.Count, 3));
                    foreach (var parent in parents)
                    {
                        writer.Write(' ');
                        writer.Write(FormatMDLInt(id, 3));
                        writer.Write(' ');
                        writer.Write(FormatMDLInt(1 + sgroups.IndexOf(parent), 3));
                    }
                    writer.Write('\n');
                }

                var attributeKeys = sgroup.AttributeKeys;
                // TODO order and aggregate attribute keys
                foreach (var key in attributeKeys)
                {
                    switch (key)
                    {
                        case SgroupKey.CtabSubScript:
                            writer.Write("M  SMT ");
                            writer.Write(FormatMDLInt(id, 3));
                            writer.Write(' ');
                            writer.Write((string)sgroup.GetValue(key));
                            writer.Write('\n');
                            break;
                        case SgroupKey.CtabExpansion:
                            var expanded = (bool)sgroup.GetValue(key);
                            if (expanded)
                            {
                                writer.Write("M  SDS EXP");
                                writer.Write(FormatMDLInt(1, 3));
                                writer.Write(' ');
                                writer.Write(FormatMDLInt(id, 3));
                                writer.Write('\n');
                            }
                            break;
                        case SgroupKey.CtabBracket:
                            var brackets = (IEnumerable<SgroupBracket>)sgroup.GetValue(key);
                            foreach (var bracket in brackets)
                            {
                                writer.Write("M  SDI ");
                                writer.Write(FormatMDLInt(id, 3));
                                writer.Write(FormatMDLInt(4, 3));
                                writer.Write(FormatMDLFloat(bracket.FirstPoint.X));
                                writer.Write(FormatMDLFloat(bracket.FirstPoint.Y));
                                writer.Write(FormatMDLFloat(bracket.SecondPoint.X));
                                writer.Write(FormatMDLFloat(bracket.SecondPoint.Y));
                                writer.Write('\n');
                            }
                            break;
                        case SgroupKey.CtabBracketStyle:
                            writer.Write("M  SBT");
                            writer.Write(FormatMDLInt(1, 3));
                            writer.Write(' ');
                            writer.Write(FormatMDLInt(id, 3));
                            writer.Write(' ');
                            writer.Write(FormatMDLInt((int)sgroup.GetValue(key), 3));
                            writer.Write('\n');
                            break;
                        case SgroupKey.CtabConnectivity:
                            writer.Write("M  SCN");
                            writer.Write(FormatMDLInt(1, 3));
                            writer.Write(' ');
                            writer.Write(FormatMDLInt(id, 3));
                            writer.Write(' ');
                            writer.Write(((string)sgroup.GetValue(key)).ToUpperInvariant());
                            writer.Write('\n');
                            break;
                        case SgroupKey.CtabSubType:
                            writer.Write("M  SST");
                            writer.Write(FormatMDLInt(1, 3));
                            writer.Write(' ');
                            writer.Write(FormatMDLInt(id, 3));
                            writer.Write(' ');
                            writer.Write((string)sgroup.GetValue(key));
                            writer.Write('\n');
                            break;
                        case SgroupKey.CtabParentAtomList:
                            var parentAtomList = (IEnumerable<IAtom>)sgroup.GetValue(key);
                            foreach (var atoms in Wrap(parentAtomList.ToReadOnlyList(), 15))
                            {
                                writer.Write("M  SPA ");
                                writer.Write(FormatMDLInt(id, 3));
                                writer.Write(FormatMDLInt(atoms.Count, 3));
                                foreach (var atom in atoms)
                                {
                                    writer.Write(' ');
                                    writer.Write(FormatMDLInt(1 + atomidxs[atom], 3));
                                }
                                writer.Write('\n');
                            }
                            break;
                        case SgroupKey.CtabComponentNumber:
                            var compNumber = (int)sgroup.GetValue(key);
                            writer.Write("M  SNC");
                            writer.Write(FormatMDLInt(1, 3));
                            writer.Write(' ');
                            writer.Write(FormatMDLInt(id, 3));
                            writer.Write(' ');
                            writer.Write(FormatMDLInt(compNumber, 3));
                            writer.Write('\n');
                            break;
                    }
                }
            }
        }

        private static List<List<T>> Wrap<T>(IEnumerable<T> set, int lim)
        {
            var wrapped = new List<List<T>>();
            var list = new List<T>(set);
            if (list.Count <= lim)
            {
                if (list.Count != 0)
                    wrapped.Add(list);
            }
            else
            {
                int i = 0;
                for (; (i + lim) < list.Count; i += lim)
                {
                    wrapped.Add(list.GetRange(i, lim));
                }
                wrapped.Add(list.GetRange(i, list.Count - i));
            }
            return wrapped;
        }

        private int GetNumberOfDimensions(IAtomContainer mol)
        {
            foreach (var atom in mol.Atoms)
            {
                if (atom.Point3D != null && !ForceWriteAs2DCoords.IsSet)
                    return 3;
                else if (atom.Point2D != null)
                    return 2;
            }
            return 0;
        }

        private void WriteRadicalPattern(IEnumerator<KeyValuePair<int, SpinMultiplicity>> iterator, int i)
        {
            var entry = iterator.Current;
            writer.Write(" ");
            writer.Write(FormatMDLInt(entry.Key + 1, WIDTH));
            writer.Write(" ");
            writer.Write(FormatMDLInt(entry.Value.Value, WIDTH));

            i = i + 1;
            if (i < NN8 && iterator.MoveNext())
                WriteRadicalPattern(iterator, i);
        }

        /// <summary>
        /// Formats an integer to fit into the connection table and changes it
        /// to a string.
        /// </summary>
        /// <param name="x">The int to be formated</param>
        /// <param name="n">Length of the string</param>
        /// <returns>The string to be written into the connection table</returns>
        protected internal static string FormatMDLInt(int x, int n)
        {
            var buf = new string(' ', n).ToCharArray();
            var val = x.ToString(NumberFormatInfo.InvariantInfo);
            if (val.Length > n)
                val = "0";
            var off = n - val.Length;
            for (int i = 0; i < val.Length; i++)
                buf[off + i] = val[i];
            return new string(buf);
        }

        /// <summary>
        /// Formats a float to fit into the connection table and changes it
        /// to a string.
        /// </summary>
        /// <param name="fl">The float to be formated</param>
        /// <returns>The string to be written into the connection table</returns>
        protected static string FormatMDLFloat(double fl)
        {
            string s;
            if (double.IsNaN(fl) || double.IsInfinity(fl))
                s = "0.0000";
            else
                s = fl.ToString("F4", CultureInfo.InvariantCulture);
            return s.PadLeft(10);
        }

        /// <summary>
        /// Formats a string to fit into the connection table.
        /// </summary>
        /// <param name="s">The string to be formated</param>
        /// <param name="le">The length of the string</param>
        /// <returns>The string to be written in the connection table</returns>
        protected static string FormatMDLString(string s, int le)
        {
            return (s + new string(' ', le)).Substring(0, le);
        }

        /// <summary>
        /// Initializes IO settings.
        /// <para>
        /// Please note with regards to "WriteAromaticBondTypes": bond type values 4 through 8 are for SSS queries only,
        /// so a 'query file' is created if the container has aromatic bonds and this settings is true.
        /// </para>
        /// </summary>
        private void InitIOSettings()
        {
            ForceWriteAs2DCoords = IOSettings.Add(
                new BooleanIOSetting(OptForceWriteAs2DCoordinates, Importance.Low,
                "Should coordinates always be written as 2D?", "false"));
            writeMajorIsotopes = IOSettings.Add(
                new BooleanIOSetting(OptWriteMajorIsotopes, Importance.Low,
                 "Write atomic mass of any non-null atomic mass including major isotopes (e.g. [12]C)", "true"));
            WriteAromaticBondTypes = IOSettings.Add(
                new BooleanIOSetting(OptWriteAromaticBondTypes, Importance.Low,
                "Should aromatic bonds be written as bond type 4?", "false"));
            WriteQueryFormatValencies = IOSettings.Add(
                new BooleanIOSetting(OptWriteQueryFormatValencies, Importance.Low,
                "Should valencies be written in the MDL Query format? (deprecated)", "false"));
            writeDefaultProps = IOSettings.Add(
                new BooleanIOSetting(OptWriteDefaultProperties, Importance.Low,
                "Write trailing zero's on atom/bond property blocks even if they're not used.", "true"));
            programNameOpt = IOSettings.Add(
                new StringIOSetting(OptProgramName, Importance.Low,
                "Program name to write at the top of the molfile header, should be exactly 8 characters long", "CDK"));
        }

        /// <summary>
        /// Convenience method to set the option for writing aromatic bond types.
        /// </summary>
        /// <param name="val">the value.</param>
        public void SetWriteAromaticBondTypes(bool val)
        {
            try
            {
                WriteAromaticBondTypes.Setting = val.ToString(NumberFormatInfo.InvariantInfo);
            }
            catch (CDKException)
            {
                // ignored can't happen since we are statically typed here
            }
        }

        public void CustomizeJob()
        {
            foreach (var setting in IOSettings.Settings)
            {
                ProcessIOSettingQuestion(setting);
            }
        }
    }
}

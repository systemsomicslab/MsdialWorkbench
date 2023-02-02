/*
 * Copyright (c) 2015 John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Common.Primitives;
using NCDK.IO.Formats;
using NCDK.IO.Setting;
using NCDK.Numerics;
using NCDK.Sgroups;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static NCDK.IO.MDLV2000Writer;

namespace NCDK.IO
{
    /// <summary>
    /// Ctab V3000 format output. 
    /// </summary>
    /// <remarks>
    /// This writer provides output to the more modern (but less widely
    /// supported) V3000 format. Unlikely the V2000 format that is limited to 999 atoms or bonds
    /// V3000 can write arbitrarily large molecules. Beyond this the format removes some (but not all)
    /// ambiguities and simplifies output values with tagging (e.g 'CHG=-1' instead of '5').
    /// Supported Features:
    /// <list type="bullet">
    ///     <item>Atom Block, non-query features</item>
    ///     <item>Bond Block, non-query features</item>
    ///     <item>Sgroup Block, partial support for all chemical Sgroups, complete support for: Abbreviations,
    ///     MultipleGroup, SRUs, (Un)ordered Mixtures</item>
    /// </list>
    /// The 3D block and enhanced stereochemistry is not currently supported.
    /// </remarks>
    public sealed class MDLV3000Writer : DefaultChemObjectWriter
    {
        private static readonly Regex R_GRP_NUM = new Regex("R(\\d+)", RegexOptions.Compiled);
        private V30LineWriter writer;
        private StringIOSetting programNameOpt;

        internal MDLV3000Writer()
        {
            InitIOSettings();
        }

        /// <summary>
        /// Create a new V3000 writer, output to the provided JDK writer.
        /// </summary>
        /// <param name="writer">output location</param>
        public MDLV3000Writer(TextWriter writer)
            : this()
        {
            this.writer = new V30LineWriter(writer);
        }

        /// <summary>
        /// Create a new V3000 writer, output to the provided output stream.
        /// </summary>
        /// <param name="output">output location</param>
        public MDLV3000Writer(Stream output)
            : this(new StreamWriter(output))
        { }

        /// <summary>
        /// Safely access nullable int fields by defaulting to zero.
        /// </summary>
        /// <param name="x">value</param>
        /// <returns>value, or zero if null</returns>
        private static int NullAsZero(int? x)
        {
            return x ?? 0;
        }

        /// <summary>
        /// Access the index of Obj->Int map, if the entry isn't found we return -1.
        /// </summary>
        /// <typeparam name="T">the object type</typeparam>
        /// <param name="idxs">index map</param>
        /// <param name="obj">the object</param>
        /// <returns>index or -1 if not found</returns>
        private static int FindIdx<T>(IReadOnlyDictionary<T, int> idxs, T obj)
        {
            if (!idxs.TryGetValue(obj, out int idx))
                return -1;
            return idx;
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
        /// Write the three line header of the MDL format: title, version/timestamp, remark.
        /// </summary>
        /// <param name="mol">molecule being output</param>
        /// <exception cref="IOException">low-level IO error</exception>
        private void WriteHeader(IAtomContainer mol)
        {
            var title = mol.Title;
            if (title != null)
                writer.WriteDirect(title.Substring(0, Math.Min(80, title.Length)));
            writer.WriteDirect('\n');

            //  From CTX spec This line has the format:
            //  IIPPPPPPPPMMDDYYHHmmddSSssssssssssEEEEEEEEEEEERRRRRR (FORTRAN:
            //  A2<--A8--><---A10-->A2I2<--F10.5-><---F12.5--><-I6-> ) User's first
            //  and last initials (l), program name (P), date/time (M/D/Y,H:m),
            //  dimensional codes (d), scaling factors (S, s), energy (E) if modeling
            //  program input, internal registry number (R) if input through MDL
            //  form. A blank line can be substituted for line 2.
            writer.Write("  ");
            writer.Write(GetProgName());
            writer.WriteDirect(DateTime.UtcNow.ToString("MMddyyHHmm", DateTimeFormatInfo.InvariantInfo));
            var dim = GetNumberOfDimensions(mol);
            if (dim != 0)
            {
                writer.WriteDirect(dim.ToString(NumberFormatInfo.InvariantInfo));
                writer.WriteDirect('D');
            }
            writer.WriteDirect('\n');

            var comment = mol.GetProperty<string>(CDKPropertyName.Remark);
            if (comment != null)
                writer.WriteDirect(comment.Substring(0, Math.Min(80, comment.Length - 80)));
            writer.WriteDirect('\n');
            writer.WriteDirect("  0  0  0     0  0            999 V3000\n");
        }

        /// <summary>
        /// Utility function for computing CTfile windings. The return value is adjusted
        /// to the MDL's model (look to lowest rank/highest number) from CDK's model (look from
        /// first).
        /// </summary>
        /// <param name="idxs">atom/bond index lookup</param>
        /// <param name="stereo">the tetrahedral configuration</param>
        /// <returns>winding to write to molfile</returns>
        private static TetrahedralStereo GetLocalParity(Dictionary<IChemObject, int> idxs, ITetrahedralChirality stereo)
        {
            var neighbours = stereo.Ligands;
            var neighbourIdx = new int[neighbours.Count];
            Trace.Assert(neighbours.Count == 4);
            for (int i = 0; i < 4; i++)
            {
                // impl H is last
                if (neighbours[i] == stereo.ChiralAtom)
                {
                    neighbourIdx[i] = int.MaxValue;
                }
                else
                {
                    neighbourIdx[i] = idxs[neighbours[i]];
                }
            }

            // determine winding swaps
            bool inverted = false;
            for (int i = 0; i < 4; i++)
            {
                for (int j = i + 1; j < 4; j++)
                {
                    if (neighbourIdx[i] > neighbourIdx[j])
                        inverted = !inverted;
                }
            }

            // CDK winding is looking from the first atom, MDL is looking
            // towards the last so we invert by default, note inverting twice
            // would be a no op and is omitted
            return inverted ? stereo.Stereo
                            : stereo.Stereo.Invert();
        }

        /// <summary>
        /// Write the atoms of a molecule. We pass in the order of atoms since for compatibility we
        /// have shifted all hydrogens to the back.
        /// </summary>
        /// <param name="mol">molecule</param>
        /// <param name="atoms">the atoms of a molecule in desired output order</param>
        /// <param name="idxs">index lookup</param>
        /// <param name="atomToStereo">tetrahedral stereo lookup</param>
        /// <exception cref="IOException">low-level IO error</exception>
        /// <exception cref="CDKException">inconsistent state etc</exception>
        private void WriteAtomBlock(IAtomContainer mol, IAtom[] atoms, Dictionary<IChemObject, int> idxs, Dictionary<IAtom, ITetrahedralChirality> atomToStereo)
        {
            if (mol.Atoms.Count == 0)
                return;
            var dim = GetNumberOfDimensions(mol);
            writer.Write("BEGIN ATOM\n");
            int atomIdx = 0;
            foreach (var atom in atoms)
            {
                var elem = NullAsZero(atom.AtomicNumber);
                var chg = NullAsZero(atom.FormalCharge);
                var mass = NullAsZero(atom.MassNumber);
                var hcnt = NullAsZero(atom.ImplicitHydrogenCount);
                var elec = mol.GetConnectedSingleElectrons(atom).Count();
                int rad = 0;
                switch (elec)
                {
                    case 1: // 2
                        rad = MDLV2000Writer.SpinMultiplicity.Monovalent.Value;
                        break;
                    case 2: // 1 or 3? Information loss as to which
                        rad = MDLV2000Writer.SpinMultiplicity.DivalentSinglet.Value;
                        break;
                }

                int expVal = 0;
                foreach (var bond in mol.GetConnectedBonds(atom))
                {
                    if (bond.Order == BondOrder.Unset)
                        throw new CDKException($"Unsupported bond order: {bond.Order}");
                    expVal += bond.Order.Numeric();
                }

                string symbol = GetSymbol(atom, elem);

                int rnum = -1;
                if (symbol[0] == 'R')
                {
                    var matcher = R_GRP_NUM.Match(symbol);
                    if (matcher.Success)
                    {
                        symbol = "R#";
                        rnum = int.Parse(matcher.Groups[1].Value, NumberFormatInfo.InvariantInfo);
                    }
                }

                writer.Write(++atomIdx)
                      .Write(' ')
                      .Write(symbol)
                      .Write(' ');
                var p2d = atom.Point2D;
                var p3d = atom.Point3D;
                switch (dim)
                {
                    case 0:
                        writer.Write("0 0 0 ");
                        break;
                    case 2:
                        if (p2d != null)
                        {
                            writer.Write(p2d.Value.X).WriteDirect(' ')
                                  .Write(p2d.Value.Y).WriteDirect(' ')
                                  .Write("0 ");
                        }
                        else
                        {
                            writer.Write("0 0 0 ");
                        }
                        break;
                    case 3:
                        if (p3d != null)
                        {
                            writer.Write(p3d.Value.X).WriteDirect(' ')
                                  .Write(p3d.Value.Y).WriteDirect(' ')
                                  .Write(p3d.Value.Z).WriteDirect(' ');
                        }
                        else
                        {
                            writer.Write("0 0 0 ");
                        }
                        break;
                }
                writer.Write(NullAsZero(atom.GetProperty<int>(CDKPropertyName.AtomAtomMapping)));

                if (chg != 0 && chg >= -15 && chg <= 15)
                    writer.Write(" CHG=").Write(chg);
                if (mass != 0)
                    writer.Write(" MASS=").Write(mass);
                if (rad > 0 && rad < 4)
                    writer.Write(" RAD=").Write(rad);
                if (rnum >= 0)
                    writer.Write(" RGROUPS=(1 ").Write(rnum).Write(")");

                // determine if we need to write the valence
                if (MDLValence.ImplicitValence(elem, chg, expVal) - expVal != hcnt)
                {
                    int val = expVal + hcnt;
                    if (val <= 0 || val > 14)
                        val = -1; // -1 is 0
                    writer.Write(" VAL=").Write(val);
                }

                if (atomToStereo.TryGetValue(atom, out ITetrahedralChirality stereo))
                {
                    switch (GetLocalParity(idxs, stereo))
                    {
                        case TetrahedralStereo.Clockwise:
                            writer.Write(" CFG=1");
                            break;
                        case TetrahedralStereo.AntiClockwise:
                            writer.Write(" CFG=2");
                            break;
                        default:
                            break;
                    }
                }

                writer.Write('\n');
            }
            writer.Write("END ATOM\n");
        }

        /// <summary>
        /// Access the atom symbol to write.
        /// </summary>
        /// <param name="atom">atom</param>
        /// <param name="elem">atomic number</param>
        /// <returns>atom symbol</returns>
        private static string GetSymbol(IAtom atom, int elem)
        {
            if (atom is IPseudoAtom)
                return ((IPseudoAtom)atom).Label;
            string symbol = ChemicalElement.Of(elem).Symbol;
            if (symbol.Length == 0)
                symbol = atom.Symbol;
            if (symbol == null)
                symbol = "*";
            if (symbol.Length > 3)
                symbol = symbol.Substring(0, 3);
            return symbol;
        }

        /// <summary>
        /// Write the bonds of a molecule.
        /// </summary>
        /// <param name="mol">molecule</param>
        /// <param name="idxs">index lookup</param>
        /// <exception cref="IOException">low-level IO error</exception>
        /// <exception cref="CDKException">inconsistent state etc</exception>
        private void WriteBondBlock(IAtomContainer mol, IReadOnlyDictionary<IChemObject, int> idxs)
        {
            if (mol.Bonds.Count == 0)
                return;

            // collect multicenter Sgroups before output
            var sgroups = mol.GetCtabSgroups();
            var multicenterSgroups = new Dictionary<IBond, Sgroup>();
            if (sgroups != null)
            {
                foreach (var sgroup in sgroups)
                {
                    if (sgroup.Type != SgroupType.ExtMulticenter)
                        continue;
                    foreach (var bond in sgroup.Bonds)
                        multicenterSgroups[bond] = sgroup;
                }
            }

            writer.Write("BEGIN BOND\n");
            int bondIdx = 0;
            foreach (var bond in mol.Bonds)
            {
                var beg = bond.Begin;
                var end = bond.End;
                if (beg == null || end == null)
                    throw new InvalidOperationException($"Bond {bondIdx} had one or more atoms.");
                int begIdx = FindIdx(idxs, beg);
                int endIdx = FindIdx(idxs, end);
                if (begIdx < 0 || endIdx < 0)
                    throw new InvalidOperationException($"Bond {bondIdx} had atoms not present in the molecule.");

                var stereo = bond.Stereo;

                // swap beg/end if needed
                if (stereo == BondStereo.UpInverted
                 || stereo == BondStereo.DownInverted
                 || stereo == BondStereo.UpOrDownInverted)
                {
                    int tmp = begIdx;
                    begIdx = endIdx;
                    endIdx = tmp;
                }

                var order = bond.Order.Numeric();

                if (order < 1 || order > 3)
                    throw new CDKException($"Bond order {bond.Order} cannot be written to V3000");

                writer.Write(++bondIdx)
                      .Write(' ')
                      .Write(order)
                      .Write(' ')
                      .Write(begIdx)
                      .Write(' ')
                      .Write(endIdx);

                switch (stereo)
                {
                    case BondStereo.Up:
                    case BondStereo.UpInverted:
                        writer.Write(" CFG=1");
                        break;
                    case BondStereo.UpOrDown:
                    case BondStereo.UpOrDownInverted:
                        writer.Write(" CFG=2");
                        break;
                    case BondStereo.Down:
                    case BondStereo.DownInverted:
                        writer.Write(" CFG=3");
                        break;
                    case BondStereo.None:
                        break;
                    default:
                        // warn?
                        break;
                }

                if (multicenterSgroups.TryGetValue(bond, out Sgroup sgroup))
                {
                    var atoms = new List<IAtom>(sgroup.Atoms);
                    atoms.Remove(bond.Begin);
                    atoms.Remove(bond.End);
                    writer.Write(" ATTACH=Any ENDPTS=(").Write(atoms, idxs).Write(')');
                }

                writer.Write('\n');
            }
            writer.Write("END BOND\n");
        }

        /// <summary>
        /// CTfile specification is ambiguous as to how parity values should be written
        /// for implicit hydrogens. Old applications (Symyx Draw) seem to push any
        /// hydrogen to (implied) the last position but newer applications
        /// (Accelrys/BioVia Draw) only do so for implicit hydrogens (makes more sense).
        /// </summary>
        /// <remarks>
        /// To avoid the ambiguity for those who read 0D stereo (bad anyways) we
        /// actually do push all hydrogens atoms to the back of the atom list giving
        /// them highest value (4) when writing parity values.
        /// </remarks>
        /// <param name="mol">molecule</param>
        /// <param name="atomToIdx">mapping that will be filled with the output index</param>
        /// <returns>the output order of atoms</returns>
        private static IAtom[] PushHydrogensToBack(IAtomContainer mol, Dictionary<IChemObject, int> atomToIdx)
        {
            Trace.Assert(atomToIdx.Count == 0);
            var atoms = new IAtom[mol.Atoms.Count];
            foreach (var atom in mol.Atoms)
            {
                if (atom.AtomicNumber == 1)
                    continue;
                atoms[atomToIdx.Count] = atom;
                atomToIdx[atom] = atomToIdx.Count + 1;
            }
            foreach (var atom in mol.Atoms)
            {
                if (atom.AtomicNumber != 1)
                    continue;
                atoms[atomToIdx.Count] = atom;
                atomToIdx[atom] = atomToIdx.Count + 1;
            }
            return atoms;
        }

        /// <summary>
        /// Safely access the Sgroups of a molecule retuning an empty list
        /// if none are defined..
        /// </summary>
        /// <param name="mol">molecule</param>
        /// <returns>the sgroups</returns>
        private static IList<Sgroup> GetSgroups(IAtomContainer mol)
        {
            var sgroups = mol.GetCtabSgroups();
            if (sgroups == null)
                sgroups = Array.Empty<Sgroup>();
            return sgroups;
        }

        private static readonly SgroupComparator aSgroupComparator = new SgroupComparator();
        private class SgroupComparator : IComparer<Sgroup>
        {
            public int Compare(Sgroup o1, Sgroup o2)
            {
                // empty parents come first
                var cmp = -(o1.Parents.Count == 0).CompareTo(o2.Parents.Count == 0);
                if (cmp != 0 || o1.Parents.Count == 0)
                    return cmp;
                // non-empty parents, if one contains the other we have an ordering
                if (o1.Parents.Contains(o2))
                    return +1;
                else if (o2.Parents.Contains(o1))
                    return -1;
                else
                    return 0;
            }
        }

        private static int GetNumberOfDimensions(IAtomContainer mol)
        {
            foreach (var atom in mol.Atoms)
            {
                if (atom.Point3D != null)
                    return 3;
                else if (atom.Point2D != null)
                    return 2;
            }
            return 0;
        }

        /// <summary>
        /// Write the Sgroup block to the output.
        /// </summary>
        /// <param name="sgroups">the sgroups, non-null</param>
        /// <param name="idxs">index map for looking up atom and bond indexes</param>
        /// <exception cref="IOException">low-level IO error</exception>
        /// <exception cref="CDKException">unsupported format feature or invalid state</exception>
        private void WriteSgroupBlock(IEnumerable<Sgroup> sgroups, IReadOnlyDictionary<IChemObject, int> idxs)
        {
            // Short of building a full dependency graph we write the parents
            // first, this sort is good for three levels of nesting. Not perfect
            // but really tools should be able to handle output of order parents
            // when reading (we do).
            var a_sgroups = new List<Sgroup>(
                sgroups
                .Where(g => g.Type != SgroupType.ExtMulticenter)    // remove non-ctab Sgroups
                .OrderBy(g => g, aSgroupComparator));
            // going to reorder but keep the originals untouched
            // don't use  sgroups.Sort(aSgroupComparator) because Sort method is not stable sort but OrderBy is stable.
            if (a_sgroups.Count == 0)
                return;

            writer.Write("BEGIN SGROUP\n");

            int sgroupIdx = 0;
            foreach (var sgroup in a_sgroups)
            {
                var type = sgroup.Type;
                writer.Write(++sgroupIdx).Write(' ').Write(type.Key()).Write(" 0");

                if (sgroup.Atoms.Any())
                {
                    writer.Write(" ATOMS=(")
                          .Write(sgroup.Atoms, idxs)
                          .Write(")");
                }

                if (sgroup.Bonds.Any())
                {
                    if (type == SgroupType.CtabData)
                    {
                        writer.Write(" CBONDS=("); // containment bonds
                    }
                    else
                    {
                        writer.Write(" XBONDS=("); // crossing bonds
                    }
                    writer.Write(sgroup.Bonds, idxs);
                    writer.Write(")");
                }

                if (sgroup.Parents.Any())
                {
                    var parents = sgroup.Parents;
                    if (parents.Count > 1)
                        throw new CDKException("Cannot write Sgroup with multiple parents");
                    writer.Write(" PARENT=").Write(1 + a_sgroups.IndexOf(parents.First()));
                }

                foreach (var key in sgroup.AttributeKeys)
                {
                    switch (key)
                    {
                        case SgroupKey.CtabSubType:
                            writer.Write(" SUBTYPE=").Write(sgroup.GetValue(key).ToString());
                            break;
                        case SgroupKey.CtabConnectivity:
                            writer.Write(" CONNECT=").Write(sgroup.GetValue(key).ToString().ToUpperInvariant());
                            break;
                        case SgroupKey.CtabSubScript:
                            if (type == SgroupType.CtabMultipleGroup)
                                writer.Write(" MULT=").Write(sgroup.GetValue(key).ToString());
                            else
                                writer.Write(" LABEL=").Write(sgroup.GetValue(key).ToString());
                            break;
                        case SgroupKey.CtabBracketStyle:
                            var btype = (int)sgroup.GetValue(key);
                            if (btype.Equals(1))
                                writer.Write(" BRKTYP=PAREN");
                            break;
                        case SgroupKey.CtabParentAtomList:
                            var parentAtoms = (IEnumerable<IChemObject>)sgroup.GetValue(key);
                            writer.Write(" PATOMS=(")
                                                          .Write(parentAtoms, idxs)
                                                          .Write(')');
                            break;
                        case SgroupKey.CtabComponentNumber:
                            var number = (int)sgroup.GetValue(key);
                            if (number > 0)
                                writer.Write(" COMPNO=").Write(number);
                            break;
                        case SgroupKey.CtabExpansion:
                            var expanded = (bool)sgroup.GetValue(key);
                            if (expanded)
                                writer.Write(" ESTATE=E");
                            break;
                        case SgroupKey.CtabBracket:
                            var brackets = (IEnumerable<SgroupBracket>)sgroup.GetValue(key);
                            foreach (var bracket in brackets)
                            {
                                writer.Write(" BRKXYZ=(");
                                var p1 = bracket.FirstPoint;
                                var p2 = bracket.SecondPoint;
                                writer.Write("9");
                                writer.Write(' ').Write(p1.X).Write(' ').Write(p1.Y).Write(" 0");
                                writer.Write(' ').Write(p2.X).Write(' ').Write(p2.Y).Write(" 0");
                                writer.Write(" 0 0 0");
                                writer.Write(")");
                            }
                            //writer.Write(" BRKTYP=").Write(sgroup.GetValue(key).ToString());
                            break;
                    }
                }
                writer.Write('\n');
            }
            writer.Write("END SGROUP\n");
        }

        /// <summary>
        /// Writes a molecule to the V3000 format.
        /// </summary>
        /// <param name="mol">molecule</param>
        /// <exception cref="IOException">low-level IO error</exception>
        /// <exception cref="CDKException">state exception (e.g undef bonds), unsupported format feature etc</exception>
        private void WriteMol(IAtomContainer mol)
        {
            WriteHeader(mol);

            var sgroups = (IEnumerable<Sgroup>)GetSgroups(mol);

            int numSgroups = 0;
            foreach (var sgroup in sgroups)
                if (sgroup.Type != SgroupType.ExtMulticenter)
                    numSgroups++;

            writer.Write("BEGIN CTAB\n");
            writer.Write("COUNTS ")
                  .Write(mol.Atoms.Count)
                  .Write(' ')
                  .Write(mol.Bonds.Count)
                  .Write(' ')
                  .Write(numSgroups)
                  .Write(" 0 0\n");

            // fast lookup atom indexes, MDL indexing starts at 1
            var idxs = new Dictionary<IChemObject, int>();
            var atomToStereo = new Dictionary<IAtom, ITetrahedralChirality>();

            // work around specification ambiguities but reordering atom output
            // order, we also insert the index into a map for lookup
            var atoms = PushHydrogensToBack(mol, idxs);

            // bonds are in molecule order
            foreach (var bond in mol.Bonds)
                idxs[bond] = 1 + idxs.Count - mol.Atoms.Count;

            // index stereo elements for lookup
            foreach (var se in mol.StereoElements)
            {
                if (se is ITetrahedralChirality)
                    atomToStereo[((ITetrahedralChirality)se).ChiralAtom] = (ITetrahedralChirality)se;
            }

            WriteAtomBlock(mol, atoms, idxs, atomToStereo);
            WriteBondBlock(mol, idxs);
            WriteSgroupBlock(sgroups.ToReadOnlyList(), idxs);

            writer.Write("END CTAB\n");
            writer.WriteDirect("M  END\n");
        }

        /// <summary>
        /// Writes a molecule to the V3000 format. 
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="CDKException">state exception (e.g undef bonds), unsupported format feature, object not supported etc</exception>
        public override void Write(IChemObject obj)
        {
            try
            {
                if (obj is IAtomContainer)
                    WriteMol((IAtomContainer)obj);
                else
                    throw new CDKException($"Unsupported ChemObject {obj.GetType()}");
            }
            catch (IOException ex)
            {
                throw new CDKException("Could not write V3000 format", ex);
            }
        }

        public override IResourceFormat Format => MDLV3000Format.Instance;

        public override bool Accepts(Type c)
        {
            if (typeof(IAtomContainer).IsAssignableFrom(c)) return true;
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
                    if (writer != null)
                        writer.Dispose();
                }

                writer = null;

                disposedValue = true;
                base.Dispose(disposing);
            }
        }
        #endregion

        /// <summary>
        /// A convenience function for writing V3000 lines that auto
        /// wrap when >80 characters. We actually wrap at 78 since
        /// the '-\n' takes the final two. We normally only need to wrap
        /// for Sgroups but all lines are handled.
        /// </summary>
        private sealed class V30LineWriter : IDisposable
        {
            // note: non-static
            public const string  PREFIX = "M  V30 ";
            public const int LIMIT = 78; // -\n takes two chars (80 total)

            // the base writer instance
            private readonly TextWriter writer;

            // tracks the current line length
            private int currLength = 0;

            public V30LineWriter(TextWriter writer)
            {
                this.writer = writer;
            }

            /// <summary>
            /// Write the string to the output directly without any prefix or wrapping.
            /// </summary>
            /// <param name="str">the string</param>
            /// <returns>self-reference for chaining</returns>
            /// <exception cref="IOException">low-level IO error</exception>
            public V30LineWriter WriteDirect(string str)
            {
                this.writer.Write(str);
                return this;
            }

            /// <summary>
            /// Write the char to the output directly without any prefix or wrapping.
            /// </summary>
            /// <param name="c">the character</param>
            /// <returns>self-reference for chaining</returns>
            /// <exception cref="IOException">low-level IO error</exception>
            public V30LineWriter WriteDirect(char c)
            {
                this.writer.Write(c);
                return this;
            }

            private void WriteUnbroken(string str)
            {
                NewLineIfNeeded();
                WritePreFixIfNeeded();
                int len = str.Length;
                if (currLength + len < LIMIT)
                {
                    this.writer.Write(str);
                    currLength += len;
                }
                else
                {
                    // could be more efficient but sufficient
                    for (int i = 0; i < len; i++)
                        Write(str[i]);
                }
            }

            private void NewLineIfNeeded()
            {
                if (currLength == LIMIT)
                {
                    this.writer.Write('-');
                    this.writer.Write('\n');
                    currLength = 0;
                }
            }

            private void WritePreFixIfNeeded()
            {
                if (currLength == 0)
                {
                    this.writer.Write(PREFIX);
                    currLength = PREFIX.Length;
                }
            }

            /// <summary>
            /// Write a floating point number to the output, wrapping
            /// if needed.
            /// </summary>
            /// <param name="num">value</param>
            /// <returns>self-reference for chaining.</returns>
            /// <exception cref="IOException">low-level IO error</exception>
            public V30LineWriter Write(double num)
            {
                return Write(Strings.JavaFormat(num, 4, true));
            }

            /// <summary>
            /// Write a int number to the output, wrapping if needed.
            /// </summary>
            /// <param name="num">value</param>
            /// <returns>self-reference for chaining.</returns>
            /// <exception cref="IOException">low-level IO error</exception>
            public V30LineWriter Write(int num)
            {
                return Write(num.ToString(NumberFormatInfo.InvariantInfo));
            }

            /// <summary>
            /// Write a string to the output, wrapping if needed.
            /// </summary>
            /// <param name="str">value</param>
            /// <returns>self-reference for chaining.</returns>
            /// <exception cref="IOException">low-level IO error</exception>
            public V30LineWriter Write(string str)
            {
                var i = str.IndexOf('\n');
                if (i < 0)
                {
                    WriteUnbroken(str);
                }
                else if (i == str.Length - 1)
                {
                    WriteUnbroken(str);
                    currLength = 0;
                }
                else
                {
                    throw new NotSupportedException();
                }
                return this;
            }

            /// <summary>
            /// Write a char number to the output, wrapping if needed.
            /// </summary>
            /// <param name="c">char</param>
            /// <returns>self-reference for chaining.</returns>
            /// <exception cref="IOException">low-level IO error</exception>
            public V30LineWriter Write(char c)
            {
                if (c == '\n' && currLength == PREFIX.Length)
                    return this;
                if (c != '\n') NewLineIfNeeded();
                WritePreFixIfNeeded();
                this.writer.Write(c);
                currLength++;
                if (c == '\n')
                    currLength = 0;
                return this;
            }

            /// <summary>
            /// Write chemobject index list, mainly useful for Sgroup output.
            /// </summary>
            /// <param name="chemObjects">collection of chemobjects</param>
            /// <param name="idxs">index map</param>
            /// <returns>self-reference for chaining.</returns>
            /// <exception cref="IOException">low-level IO error</exception>
            public V30LineWriter Write(IEnumerable<IChemObject> chemObjects, IReadOnlyDictionary<IChemObject, int> idxs)
            {
                var chemObjectList = chemObjects.ToReadOnlyList();
                this.Write(chemObjectList.Count);
                var integers = new List<int>();
                foreach (var chemObject in chemObjectList)
                    integers.Add(idxs[chemObject]);
                integers.Sort();
                foreach (var integer in integers)
                    this.Write(' ').Write(integer);
                return this;
            }

            public void Close()
            {
                writer.Close();
            }

            public void Dispose()
            {
                Close();
            }
        }

        /// <summary>
        /// Initializes IO settings.
        /// </summary>
        /// <remarks>
        /// Please note with regards to "writeAromaticBondTypes": bond type values 4 through 8 are for SSS queries only,
        /// so a 'query file' is created if the container has aromatic bonds and this settings is true.
        /// </remarks>
        private void InitIOSettings()
        {
            programNameOpt = IOSettings.Add(
                new StringIOSetting(OptProgramName, Importance.Low,
                "Program name to write at the top of the molfile header, should be exactly 8 characters long", "CDK"));
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

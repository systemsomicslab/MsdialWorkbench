/* 
 * Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
 *                    2014  Mark B Vine (orcid:0000-0002-7794-0426)
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

using NCDK.AtomTypes;
using NCDK.Config;
using NCDK.Graphs;
using NCDK.RingSearches;
using NCDK.Sgroups;
using NCDK.Stereo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NCDK.Tools.Manipulator
{
    public enum MolecularWeightTypes
    {
        /// <summary>
        /// For use with <see cref="AtomContainerManipulator.GetMass(IAtomContainer, MolecularWeightTypes)"/>. This option uses the mass
        /// stored on atoms (<see cref="IIsotope.ExactMass"/>) or the average mass of the
        /// element when unspecified. 
        /// </summary>
        MolWeight = 0x1,

        /// <summary>
        /// For use with <see cref="AtomContainerManipulator.GetMass(IAtomContainer, MolecularWeightTypes)"/>. This option ignores the
        /// mass stored on atoms (<see cref="IIsotope.ExactMass"/>) and uses the average
        /// mass of each element. This option is primarily provided for backwards
        /// compatibility.
        /// </summary>
        MolWeightIgnoreSpecified = 0x2,

        /// <summary>
        /// For use with <see cref="AtomContainerManipulator.GetMass(IAtomContainer, MolecularWeightTypes)"/>. This option uses the mass
        /// stored on atoms <see cref="IIsotope.ExactMass"/> or the mass of the major
        /// isotope when this is not specified.
        /// </summary>
        MonoIsotopic = 0x3,

        /// <summary>
        /// For use with <see cref="AtomContainerManipulator.GetMass(IAtomContainer, MolecularWeightTypes)"/>. This option uses the mass
        /// stored on atoms <see cref="IIsotope.ExactMass"/> and then calculates a
        /// distribution for any unspecified atoms and uses the most abundant
        /// distribution. For example C<sub>6</sub>Br<sub>6</sub> would have three
        /// <sup>79</sup>Br and <sup>81</sup>Br because their abundance is 51 and
        /// 49%.
        /// </summary>
        MostAbundant = 0x4,
    }

    /// <summary>
    /// Class with convenience methods that provide methods to manipulate
    /// AtomContainer's. 
    /// </summary>
    /// <example>
    /// For example:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Tools.AtomContainerManipulator_Example.cs+1"]/*' />
    /// will replace the Atom in the AtomContainer, but in all the ElectronContainer's
    /// it participates too.
    /// </example>
    // @cdk.module standard
    // @author  Egon Willighagen
    // @cdk.created 2003-08-07
    public static class AtomContainerManipulator
    {
        /// <summary>
        /// Extract a substructure from an atom container, in the form of a new
        /// cloned atom container with only the atoms with indices in atomIndices and
        /// bonds that connect these atoms.
        /// </summary>
        /// <remarks>
        /// Note that this may result in a disconnected atom container.
        /// </remarks>
        /// <param name="atomContainer">the source container to extract from</param>
        /// <param name="atomIndices">the indices of the substructure</param>
        /// <returns>a cloned atom container with a substructure of the source</returns>
        public static IAtomContainer ExtractSubstructure(IAtomContainer atomContainer, params int[] atomIndices)
        {
            var substructure = (IAtomContainer)atomContainer.Clone();
            int numberOfAtoms = substructure.Atoms.Count;
            var atoms = new IAtom[numberOfAtoms];
            for (int atomIndex = 0; atomIndex < numberOfAtoms; atomIndex++)
            {
                atoms[atomIndex] = substructure.Atoms[atomIndex];
            }
            Array.Sort(atomIndices);
            for (int index = 0; index < numberOfAtoms; index++)
            {
                if (Array.BinarySearch(atomIndices, index) < 0)
                {
                    var atom = atoms[index];
                    substructure.RemoveAtom(atom);
                }
            }

            return substructure;
        }

        /// <summary>
        /// Returns an atom in an atom container identified by id
        /// </summary>
        /// <param name="ac">The atom container to search in</param>
        /// <param name="id">The id to search for</param>
        /// <returns>An atom having <paramref name="id"/></returns>
        /// <exception cref="CDKException">There is no such atom</exception>
        public static IAtom GetAtomById(IAtomContainer ac, string id)
        {
            for (int i = 0; i < ac.Atoms.Count; i++)
            {
                if (ac.Atoms[i].Id != null && string.Equals(ac.Atoms[i].Id, id, StringComparison.Ordinal))
                    return ac.Atoms[i];
            }
            throw new CDKException("no suc atom");
        }

        /// <summary>
        /// Substitute one atom in a container for another adjusting bonds, single electrons, lone pairs, and stereochemistry
        /// as required.
        /// </summary>
        /// <param name="container">the container to replace the atom of</param>
        /// <param name="oldAtom">the atom to replace</param>
        /// <param name="newAtom">the atom to insert</param>
        /// <returns>whether replacement was made</returns>
        public static bool ReplaceAtomByAtom(IAtomContainer container, IAtom oldAtom, IAtom newAtom)
        {
            if (oldAtom == null)
                throw new ArgumentNullException(nameof(oldAtom), "Atom to be replaced was null!");
            var idx = container.Atoms.IndexOf(oldAtom);
            if (idx < 0)
                return false;
            container.Atoms[idx] = newAtom ?? throw new ArgumentNullException(nameof(newAtom), "Replacement atom was null!");
            var sgrougs = container.GetCtabSgroups();
            if (sgrougs != null)
            {
                bool updated = false;
                var replaced = new List<Sgroup>();
                foreach (var org in sgrougs)
                {
                    if (org.Atoms.Contains(oldAtom))
                    {
                        updated = true;
                        var cpy = new Sgroup();
                        foreach (var atom in org.Atoms)
                        {
                            if (!oldAtom.Equals(atom))
                                cpy.Atoms.Add(atom);
                            else
                                cpy.Atoms.Add(newAtom);
                        }
                        foreach (var bond in org.Bonds)
                            cpy.Bonds.Add(bond);
                        foreach (var parent in org.Parents)
                            cpy.AddParent(parent);
                        foreach (var key in org.AttributeKeys)
                            cpy.PutValue(key, org.GetValue(key));
                        replaced.Add(cpy);
                    }
                    else
                    {
                        replaced.Add(org);
                    }
                }
                if (updated)
                {
                    container.SetCtabSgroups(replaced);
                }
            }

            return true;
        }

        /// <summary>
        /// Get the summed charge of all atoms in an AtomContainer
        /// </summary>
        /// <param name="atomContainer">The IAtomContainer to manipulate</param>
        /// <returns>The summed charges of all atoms in this AtomContainer.</returns>
        public static double GetTotalCharge(IAtomContainer atomContainer)
        {
            double charge = 0.0;
            foreach (var atom in atomContainer.Atoms)
            {
                // we assume CDKConstant.Unset is equal to 0
                var thisCharge = atom.Charge;
                if (thisCharge.HasValue)
                    charge += thisCharge.Value;
            }
            return charge;
        }

        private static bool HasIsotopeSpecified(IIsotope atom)
        {
            return (atom.MassNumber ?? 0) != 0;
        }

        private static double GetExactMass(IsotopeFactory isofact, IIsotope atom)
        {
            if (atom.ExactMass != null)
                return atom.ExactMass.Value;
            else if (atom.MassNumber != null)
                return isofact.GetExactMass(atom.AtomicNumber, atom.MassNumber.Value);
            else
                return isofact.GetMajorIsotopeMass(atom.AtomicNumber);
        }

        private static double GetMassOrAvg(IsotopeFactory isofact, IIsotope atom)
        {
            if (!HasIsotopeSpecified(atom))
                return isofact.GetNaturalMass(atom.AtomicNumber);
            return GetExactMass(isofact, atom);
        }

        internal static readonly IComparer<IIsotope> NAT_ABUN_COMP = new NAT_ABUN_COMP_Comparator();

        class NAT_ABUN_COMP_Comparator : IComparer<IIsotope>
        {
            public int Compare(IIsotope o1, IIsotope o2)
            {
                var a1 = o1.Abundance ?? 0;
                var a2 = o2.Abundance ?? 0;
                return -a1.CompareTo(a2);
            }
        }

        private static double GetDistMass(IsotopeFactory isofact,
                                          IIsotope[] isos, int idx, int count)
        {
            if (count == 0)
                return 0;
            double frac = 100d;
            double res = 0;
            for (int i = 0; i < idx; i++)
                frac -= isos[i].Abundance.Value;
            double p = isos[idx].Abundance.Value / frac;
            if (p >= 1.0)
                return count * isos[idx].ExactMass.Value;
            double kMin = (count + 1) * (1 - p) - 1;
            double kMax = (count + 1) * (1 - p);
            if ((int)Math.Ceiling(kMin) == (int)Math.Floor(kMax))
            {
                int k = (int)kMax;
                res = (count - k) * GetExactMass(isofact, isos[idx]);
                res += GetDistMass(isofact, isos, idx + 1, k);
            }
            return res;
        }

        private static int GetImplHCount(IAtom atom)
        {
            var implh = atom.ImplicitHydrogenCount;
            if (implh == null)
                throw new ArgumentException("An atom had 'null' implicit hydrogens!");
            return implh.Value;
        }

        /// <summary>
        /// Calculate the mass of a molecule, this function takes an optional
        /// 'mass flavour' that switches the computation type.The key distinction
        /// is how specified/unspecified isotopes are handled. A specified isotope
        /// is an atom that has either <see cref="IIsotope.MassNumber"/>
        /// or <see cref="IIsotope.ExactMass"/> set to non-null and non-zero.
        /// </summary>
        /// <remarks>
        /// The flavours are:
        /// <ul>
        ///     <li><see cref="MolecularWeightTypes.MolWeight"/> (default) - uses the exact mass of each
        /// atom when an isotope is specified, if not specified the average mass
        /// of the element is used.</li>
        ///     <li><see cref="MolecularWeightTypes.MolWeightIgnoreSpecified"/> - uses the average mass of each
        /// element, ignoring any isotopic/exact mass specification</li>
        ///     <li><see cref="MolecularWeightTypes.MonoIsotopic"/> - uses the exact mass of each
        /// atom when an isotope is specified, if not specified the major isotope
        /// mass for that element is used.</li>
        ///     <li><see cref="MolecularWeightTypes.MostAbundant"/> - uses the exact mass of each atom when
        /// specified, if not specified a distribution is calculated and the
        /// most abundant isotope pattern is used.</li>
        /// </ul>
        /// </remarks>
        /// <param name="mol">molecule to compute mass for</param>
        /// <param name="flav">flavor</param>
        /// <returns>the mass of the molecule</returns>
        /// <seealso cref="GetMass(IAtomContainer, MolecularWeightTypes)"/>
        /// <seealso cref="MolecularWeightTypes.MolWeight"/>
        /// <seealso cref="MolecularWeightTypes.MolWeightIgnoreSpecified"/>
        /// <seealso cref="MolecularWeightTypes.MonoIsotopic"/>
        /// <seealso cref="MolecularWeightTypes.MostAbundant"/>
        public static double GetMass(IAtomContainer mol, MolecularWeightTypes flav)
        {
            var isofact = CDK.IsotopeFactory;

            double mass = 0;
            int hcnt = 0;

            switch (flav)
            {
                case MolecularWeightTypes.MolWeight:
                    foreach (var atom in mol.Atoms)
                    {
                        mass += GetMassOrAvg(isofact, atom);
                        hcnt += GetImplHCount(atom);
                    }
                    mass += hcnt * isofact.GetNaturalMass(1);
                    break;
                case MolecularWeightTypes.MolWeightIgnoreSpecified:
                    foreach (var atom in mol.Atoms)
                    {
                        mass += isofact.GetNaturalMass(atom.AtomicNumber);
                        hcnt += GetImplHCount(atom);
                    }
                    mass += hcnt * isofact.GetNaturalMass(1);
                    break;
                case MolecularWeightTypes.MonoIsotopic:
                    foreach (var atom in mol.Atoms)
                    {
                        mass += GetExactMass(isofact, atom);
                        hcnt += GetImplHCount(atom);
                    }
                    mass += hcnt * isofact.GetMajorIsotopeMass(1);
                    break;
                case MolecularWeightTypes.MostAbundant:
                    var mf = new int[128];
                    foreach (var atom in mol.Atoms)
                    {
                        if (HasIsotopeSpecified(atom))
                            mass += GetExactMass(isofact, atom);
                        else
                            mf[atom.AtomicNumber]++;
                        mf[1] += atom.ImplicitHydrogenCount.Value;
                    }

                    for (int atno = 0; atno < mf.Length; atno++)
                    {
                        if (mf[atno] == 0)
                            continue;
                        var isotopes = isofact.GetIsotopes(atno).ToArray();
                        Array.Sort(isotopes, NAT_ABUN_COMP);
                        mass += GetDistMass(isofact, isotopes, 0, mf[atno]);
                    }
                    break;
            }
            return mass;
        }

        /// <summary>
        /// Calculate the mass of a molecule, this function takes an optional
        /// 'mass flavour' that switches the computation type. The key distinction
        /// is how specified/unspecified isotopes are handled. A specified isotope
        /// is an atom that has either <see cref="IIsotope.MassNumber"/>
        /// or <see cref="IIsotope.ExactMass"/> set to non-null and non-zero.
        /// </summary>
        /// <remarks>
        /// The flavours are:
        /// <ul>
        ///     <li><see cref="MolecularWeightTypes.MolWeight"/> (default) - uses the exact mass of each
        ///     atom when an isotope is specified, if not specified the average mass
        ///     of the element is used.</li>
        ///     <li><see cref="MolecularWeightTypes.MolWeightIgnoreSpecified"/> - uses the average mass of each
        ///     element, ignoring any isotopic/exact mass specification</li>
        ///     <li><see cref="MolecularWeightTypes.MonoIsotopic"/> - uses the exact mass of each
        ///     atom when an isotope is specified, if not specified the major isotope
        ///     mass for that element is used.</li>
        ///     <li><see cref="MolecularWeightTypes.MostAbundant"/> - uses the exact mass of each atom when
        ///     specified, if not specified a distribution is calculated and the
        ///     most abundant isotope pattern is used.</li>
        /// </ul>
        /// </remarks>
        /// <param name="mol">molecule to compute mass for</param>
        /// <returns>the mass of the molecule</returns>
        /// <seealso cref="GetMass(IAtomContainer, MolecularWeightTypes)"/>
        /// <seealso cref="MolecularWeightTypes.MolWeight"/>
        /// <seealso cref="MolecularWeightTypes.MolWeightIgnoreSpecified"/>
        /// <seealso cref="MolecularWeightTypes.MonoIsotopic"/>
        /// <seealso cref="MolecularWeightTypes.MostAbundant"/>
        public static double GetMass(IAtomContainer mol)
        {
            return GetMass(mol, MolecularWeightTypes.MolWeight);
        }

        [Obsolete("Use " + nameof(GetMass) + "(" + nameof(IAtomContainer) + ", int) and " + nameof(MolecularWeightTypes.MonoIsotopic) + ".")]
        public static double GetTotalExactMass(IAtomContainer mol)
        {
            return GetMass(mol, MolecularWeightTypes.MonoIsotopic);
        }

        [Obsolete("Use " + nameof(GetMass) + "(" + nameof(IAtomContainer) + ", int) and " + nameof(MolecularWeightTypes.MolWeightIgnoreSpecified) + ". You probably want " + nameof(MolecularWeightTypes.MolWeight))]
        public static double GetNaturalExactMass(IAtomContainer mol)
        {
            return GetMass(mol, MolecularWeightTypes.MolWeightIgnoreSpecified);
        }

        [Obsolete("Use " + nameof(GetMass) + "(" + nameof(IAtomContainer) + ", int) and " + nameof(MolecularWeightTypes.MolWeight) + ".")]
        public static double GetMolecularWeight(IAtomContainer mol)
        {
            return GetMass(mol, MolecularWeightTypes.MolWeight);
        }

        /// <summary>
        /// Get the summed natural abundance of all atoms in an AtomContainer
        /// </summary>
        /// <param name="atomContainer">The IAtomContainer to manipulate</param>
        /// <returns>The summed natural abundance of all atoms in this AtomContainer.</returns>
        public static double GetTotalNaturalAbundance(IAtomContainer atomContainer)
        {
            try
            {
                var isotopes = CDK.IsotopeFactory;
                double abundance = 1.0;
                var hAbundance = isotopes.GetMajorIsotope(1).Abundance.Value;

                int nImplH = 0;

                foreach (var atom in atomContainer.Atoms)
                {
                    if (!atom.ImplicitHydrogenCount.HasValue)
                        throw new ArgumentException("an atom had with unknown (null) implicit hydrogens");
                    abundance *= atom.Abundance.Value;
                    for (int h = 0; h < atom.ImplicitHydrogenCount.Value; h++)
                        abundance *= hAbundance;
                    nImplH += atom.ImplicitHydrogenCount.Value;
                }
                return abundance / Math.Pow(100, nImplH + atomContainer.Atoms.Count);
            }
            catch (IOException e)
            {
                throw new IOException("Isotopes definitions could not be loaded", e);
            }
        }

        /// <summary>
        /// Get the total formal charge on a molecule.
        /// </summary>
        /// <param name="atomContainer">the atom container to consider</param>
        /// <returns>The summed formal charges of all atoms in this AtomContainer.</returns>
        public static int GetTotalFormalCharge(IAtomContainer atomContainer)
        {
            var chargeP = GetTotalNegativeFormalCharge(atomContainer);
            var chargeN = GetTotalPositiveFormalCharge(atomContainer);

            return chargeP + chargeN;
        }

        /// <summary>
        /// Get the total formal negative charge on a molecule.
        /// </summary>
        /// <param name="atomContainer">the atom container to consider</param>
        /// <returns>The summed negative formal charges of all atoms in this AtomContainer.</returns>
        public static int GetTotalNegativeFormalCharge(IAtomContainer atomContainer)
        {
            int charge = 0;
            for (int i = 0; i < atomContainer.Atoms.Count; i++)
            {
                var chargeI = atomContainer.Atoms[i].FormalCharge.Value;
                if (chargeI < 0)
                    charge += chargeI;
            }
            return charge;
        }

        /// <summary>
        /// Get the total positive formal charge on a molecule.
        /// </summary>
        /// <param name="atomContainer">the atom container to consider</param>
        /// <returns>The summed positive formal charges of all atoms in this AtomContainer.</returns>
        public static int GetTotalPositiveFormalCharge(IAtomContainer atomContainer)
        {
            int charge = 0;
            for (int i = 0; i < atomContainer.Atoms.Count; i++)
            {
                var chargeI = atomContainer.Atoms[i].FormalCharge.Value;
                if (chargeI > 0)
                    charge += chargeI;
            }
            return charge;
        }

        /// <summary>
        /// Counts the number of hydrogens on the provided IAtomContainer. As this
        /// method will sum all implicit hydrogens on each atom it is important to
        /// ensure the atoms have already been perceived (and thus have an implicit
        /// hydrogen count) (see. <see cref="PercieveAtomTypesAndConfigureAtoms(IAtomContainer)"/>).
        /// </summary>
        /// <param name="container">the container to count the hydrogens on</param>
        /// <returns>the total number of hydrogens</returns>
        /// <seealso cref="IAtom.ImplicitHydrogenCount"/>
        /// <seealso cref="PercieveAtomTypesAndConfigureAtoms"/>
        /// <exception cref="ArgumentNullException">if the provided container was null</exception>
        public static int GetTotalHydrogenCount(IAtomContainer container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container), "null container provided");
            int hydrogens = 0;
            foreach (var atom in container.Atoms)
            {
                if (AtomicNumbers.H.Equals(atom.AtomicNumber))
                {
                    hydrogens++;
                }

                // rare but a hydrogen may have an implicit hydrogen so we don't use 'else'
                var implicit_ = atom.ImplicitHydrogenCount;
                if (implicit_.HasValue)
                {
                    hydrogens += implicit_.Value;
                }
            }
            return hydrogens;
        }

        /// <summary>
        /// Counts the number of implicit hydrogens on the provided IAtomContainer.
        /// As this method will sum all implicit hydrogens on each atom it is
        /// important to ensure the atoms have already been perceived (and thus have
        /// an implicit hydrogen count) (see. <see cref="PercieveAtomTypesAndConfigureAtoms(IAtomContainer)"/>.
        /// </summary>
        /// <param name="container">the container to count the implicit hydrogens on</param>
        /// <returns>the total number of implicit hydrogens</returns>
        /// <seealso cref="IAtom.ImplicitHydrogenCount"/>
        /// <seealso cref="PercieveAtomTypesAndConfigureAtoms"/>
        /// <exception cref="ArgumentNullException">if the provided container was null</exception>
        public static int GetImplicitHydrogenCount(IAtomContainer container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container), "null container provided");
            int count = 0;
            foreach (var atom in container.Atoms)
            {
                var implicit_ = atom.ImplicitHydrogenCount;
                if (implicit_.HasValue)
                {
                    count += implicit_.Value;
                }
            }
            return count;
        }

        /// <summary>
        /// Count explicit hydrogens.
        /// </summary>
        /// <param name="atomContainer">the atom container to consider</param>
        /// <returns>The number of explicit hydrogens on the given IAtom.</returns>
        /// <exception cref="ArgumentNullException">if the provided container was null</exception>
        public static int CountExplicitHydrogens(IAtomContainer atomContainer, IAtom atom)
        {
            if (atomContainer == null || atom == null)
                throw new ArgumentException("null container or atom provided");
            int hCount = 0;
            foreach (var connected in atomContainer.GetConnectedAtoms(atom))
            {
                if (AtomicNumbers.H.Equals(connected.AtomicNumber))
                {
                    hCount++;
                }
            }
            return hCount;
        }

        private static void ReplaceAtom(IAtom[] atoms, IAtom org, IAtom rep)
        {
            for (int i = 0; i < atoms.Length; i++)
            {
                if (atoms[i].Equals(org))
                    atoms[i] = rep;
            }
        }

        /// <summary>
        /// Adds explicit hydrogens (without coordinates) to the IAtomContainer,
        /// equaling the number of set implicit hydrogens.
        /// </summary>
        /// <param name="atomContainer">the atom container to consider</param>
        // @cdk.keyword hydrogens, adding
        public static void ConvertImplicitToExplicitHydrogens(IAtomContainer atomContainer)
        {
            var hydrogens = new List<IAtom>();
            var newBonds = new List<IBond>();

            // store a single explicit hydrogen of each original neighbor
            var hNeighbor = new Dictionary<IAtom, IAtom>();

            foreach (var atom in atomContainer.Atoms)
            {
                if (!atom.AtomicNumber.Equals(AtomicNumbers.H))
                {
                    var hCount = atom.ImplicitHydrogenCount;
                    if (hCount != null)
                    {
                        for (int i = 0; i < hCount; i++)
                        {
                            var hydrogen = atom.Builder.NewAtom("H");
                            hydrogen.AtomTypeName = "H";
                            hydrogen.ImplicitHydrogenCount = 0;
                            hydrogens.Add(hydrogen);
                            newBonds.Add(atom.Builder.NewBond(atom, hydrogen, BondOrder.Single));
                            if (!hNeighbor.ContainsKey(atom))
                                hNeighbor.Add(atom, hydrogen);
                        }
                        atom.ImplicitHydrogenCount = 0;
                    }
                }
            }
            foreach (var atom in hydrogens)
                atomContainer.Atoms.Add(atom);
            foreach (var bond in newBonds)
                atomContainer.Bonds.Add(bond);

            // update stereo elements with an implicit part
            var stereos = new List<IStereoElement<IChemObject, IChemObject>>();
            foreach (var stereo in atomContainer.StereoElements)
            {
                switch (stereo)
                {
                    case ITetrahedralChirality tc: {
                        var focus = tc.Focus;
                        var carriers = tc.Carriers.ToArray();
                        // in sulfoxide - the implicit part of the tetrahedral centre
                        // is a lone pair

                        if (hNeighbor == null || hNeighbor.Count == 0) {
                            stereos.Add(stereo);

                        }
                        else if (hNeighbor.TryGetValue(focus, out IAtom hydrogen)) {
                            ReplaceAtom(carriers, focus, hydrogen);
                            stereos.Add(new TetrahedralChirality(focus, carriers, tc.Stereo));
                        }
                        else {
                            stereos.Add(stereo);
                        }
                    }
                    break; 
                    case ExtendedTetrahedral tc: {
                        var focus = tc.Focus;
                        var carriers = tc.Carriers.ToArray();
                        var ends = ExtendedTetrahedral.FindTerminalAtoms(atomContainer, focus);

                        if (!hNeighbor.ContainsKey(ends[0]) || !hNeighbor.ContainsKey(ends[1])) {
                            stereos.Add(stereo);
                        }
                        else {

                            var h1 = hNeighbor[ends[0]];
                            var h2 = hNeighbor[ends[1]];
                            if (h1 != null || h2 != null) {
                                if (h1 != null)
                                    ReplaceAtom(carriers, ends[0], h1);
                                if (h2 != null)
                                    ReplaceAtom(carriers, ends[1], h2);
                                stereos.Add(new ExtendedTetrahedral(focus, carriers, tc.Configure));
                            }
                            else {
                                stereos.Add(stereo);
                            }
                        }

                    }
                    break;
                    default:
                        stereos.Add(stereo);
                        break;
                }
            }
            atomContainer.SetStereoElements(stereos);
        }

        /// <returns>The summed implicit + explicit hydrogens of the given IAtom.</returns>
        public static int CountHydrogens(IAtomContainer atomContainer, IAtom atom)
        {
            var hCount = atom.ImplicitHydrogenCount ?? 0;
            hCount += CountExplicitHydrogens(atomContainer, atom);
            return hCount;
        }

        public static IEnumerable<string> GetAllIDs(IAtomContainer mol)
        {
            if (mol != null)
            {
                if (mol.Id != null)
                    yield return mol.Id;
                foreach (var atom in mol.Atoms)
                    if (atom.Id != null)
                        yield return atom.Id;

                foreach (var bond in mol.Bonds)
                    if (bond.Id != null)
                        yield return bond.Id;
            }
            yield break;
        }

        /// <summary>
        /// Produces an AtomContainer without explicit non stereo-relevant Hs but with H count from one with Hs.
        /// The new molecule is a deep copy.
        /// </summary>
        /// <param name="org">The AtomContainer from which to remove the hydrogens</param>
        /// <returns>The molecule without non stereo-relevant Hs.</returns>
        // @cdk.keyword         hydrogens, removal
        public static IAtomContainer RemoveNonChiralHydrogens(IAtomContainer org)
        {
            var map = new CDKObjectMap(); // maps original atoms to clones.
            var orgAtomsToRemove = new List<IAtom>(); // lists removed Hs.

            // Clone atoms except those to be removed.
            var cpy = (IAtomContainer)org.Clone(map);
            var count = org.Atoms.Count;

            foreach (var atom in org.Atoms)
            {
                // Clone/remove this atom?
                bool addToRemove = false;
                if (SuppressibleHydrogen(org, atom))
                {
                    // test whether connected to a single hetero atom only, otherwise keep
                    if (org.GetConnectedAtoms(atom).Count() == 1)
                    {
                        var neighbour = org.GetConnectedAtoms(atom).ElementAt(0);
                        // keep if the neighbouring hetero atom has stereo information, otherwise continue checking
                        var stereoParity = neighbour.StereoParity;
                        if (stereoParity == StereoAtomParities.Undefined)
                        {
                            addToRemove = true;
                            // keep if any of the bonds of the hetero atom have stereo information
                            foreach (var bond in org.GetConnectedBonds(neighbour))
                            {
                                var bondStereo = bond.Stereo;
                                if (bondStereo != BondStereo.None)
                                    addToRemove = false;
                                var neighboursNeighbour = bond.GetOther(neighbour);
                                // remove in any case if the hetero atom is connected to more than one hydrogen
                                if (neighboursNeighbour.AtomicNumber.Equals(AtomicNumbers.H) && neighboursNeighbour != atom)
                                {
                                    addToRemove = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (addToRemove)
                    orgAtomsToRemove.Add(atom);
            }

            // rescue any false positives, i.e., hydrogens that are stereo-relevant
            // the use of IStereoElement<IChemObject, IChemObject> is not fully integrated yet to describe stereo information
            foreach (var stereoElement in org.StereoElements)
            {
                switch (stereoElement)
                {
                    case ITetrahedralChirality tetChirality:
                        {
                            foreach (var atom in tetChirality.Ligands)
                            {
                                if (atom.AtomicNumber.Equals(AtomicNumbers.H) && orgAtomsToRemove.Contains(atom))
                                {
                                    orgAtomsToRemove.Remove(atom);
                                }
                            }
                        }
                        break;
                    case IDoubleBondStereochemistry dbs:
                        {
                            IBond stereoBond = dbs.StereoBond;
                            foreach (var neighbor in org.GetConnectedAtoms(stereoBond.Begin))
                            {
                                orgAtomsToRemove.Remove(neighbor);
                            }
                            foreach (var neighbor in org.GetConnectedAtoms(stereoBond.End))
                            {
                                orgAtomsToRemove.Remove(neighbor);
                            }
                        }
                        break;
                }
            }

            foreach (var atom in orgAtomsToRemove)
                cpy.RemoveAtom(map.Get(atom)); // this removes connected bond

            // Recompute hydrogen counts of neighbours of removed Hydrogens.
            foreach (var atom in orgAtomsToRemove)
            {
                // Process neighbours.
                foreach (var connectedAtom in org.GetConnectedAtoms(atom))
                {
                    if (!map.TryGetValue(connectedAtom, out IAtom neighb))
                        continue; // since for the case of H2, neight H has a heavy atom neighbor
                    neighb.ImplicitHydrogenCount = (neighb.ImplicitHydrogenCount ?? 0) + 1;
                }
            }
            foreach (var atom in cpy.Atoms)
            {
                if (atom.ImplicitHydrogenCount == null)
                    atom.ImplicitHydrogenCount = 0;
            }

            return cpy;
        }

        /// <summary>
        /// Copy the input container and suppress any explicit hydrogens. Only
        /// hydrogens that can be represented as a hydrogen count value on the atom
        /// are suppressed. If a copy is not needed please use <see cref="SuppressHydrogens"/>.
        /// </summary>
        /// <param name="org">the container from which to remove hydrogens</param>
        /// <returns>a copy of the input with suppressed hydrogens</returns>
        /// <seealso cref="SuppressHydrogens"/>
        public static IAtomContainer CopyAndSuppressedHydrogens(IAtomContainer org)
        {
            return SuppressHydrogens((IAtomContainer)org.Clone());
        }

        /// <summary>
        /// Suppress any explicit hydrogens in the provided container. Only hydrogens
        /// that can be represented as a hydrogen count value on the atom are
        /// suppressed. The container is updated and no elements are copied, please
        /// use either <see cref="CopyAndSuppressedHydrogens(IAtomContainer)"/> if you would to preserve
        /// the old instance.
        /// </summary>
        /// <param name="org">the container from which to remove hydrogens</param>
        /// <returns>the input for convenience</returns>
        /// <seealso cref="CopyAndSuppressedHydrogens"/>
        public static IAtomContainer SuppressHydrogens(IAtomContainer org)
        {
            bool anyHydrogenPresent = false;
            foreach (var atom in org.Atoms)
            {
                if (AtomicNumbers.H.Equals(atom.AtomicNumber))
                {
                    anyHydrogenPresent = true;
                    break;
                }
            }

            if (!anyHydrogenPresent)
                return org;

            // crossing atoms, positional variation atoms etc
            ISet<IAtom> xatoms = new NCDK.Common.Collections.EmptySet<IAtom>();
            var sgroups = org.GetCtabSgroups();
            if (sgroups != null)
            {
                xatoms = new HashSet<IAtom>();
                foreach (var sgroup in sgroups)
                {
                    foreach (var bond in sgroup.Bonds)
                    {
                        xatoms.Add(bond.Begin);
                        xatoms.Add(bond.End);
                    }
                }
            }

            // we need fast adjacency checks (to check for suppression and
            // update hydrogen counts)
            var bondmap = EdgeToBondMap.WithSpaceFor(org);
            var graph = GraphUtil.ToAdjList(org, bondmap);

            int nOrgAtoms = org.Atoms.Count;
            int nOrgBonds = org.Bonds.Count;

            int nCpyAtoms = 0;
            int nCpyBonds = 0;

            var hydrogens = new HashSet<IAtom>();
            var bondsToHydrogens = new HashSet<IBond>();
            var cpyAtoms = new IAtom[nOrgAtoms];

            // filter the original container atoms for those that can/can't
            // be suppressed
            for (int v = 0; v < nOrgAtoms; v++)
            {
                var atom = org.Atoms[v];
                if (SuppressibleHydrogen(org, graph, bondmap, v) &&
                    !xatoms.Contains(atom))
                {
                    hydrogens.Add(atom);
                    IncrementImplHydrogenCount(org.Atoms[graph[v][0]]);
                }
                else
                {
                    cpyAtoms[nCpyAtoms++] = atom;
                }
            }

            // none of the hydrogens could be suppressed - no changes need to be made
            if (hydrogens.Count == 0)
                return org;

            // we now update the bonds - we have auxiliary variable remaining that
            // bypasses the set membership checks if all suppressed bonds are found
            var cpyBonds = new IBond[nOrgBonds - hydrogens.Count()];
            var remaining = hydrogens.Count();

            foreach (var bond in org.Bonds)
            {
                if (remaining > 0 && (hydrogens.Contains(bond.Begin) || hydrogens.Contains(bond.End)))
                {
                    bondsToHydrogens.Add(bond);
                    remaining--;
                    continue;
                }
                cpyBonds[nCpyBonds++] = bond;
            }

            // we know how many hydrogens we removed and we should have removed the
            // same number of bonds otherwise the containers is a strange
            if (nCpyBonds != cpyBonds.Count())
                throw new ArgumentException("number of removed bonds was less than the number of removed hydrogens");

            var elements = new List<IStereoElement<IChemObject, IChemObject>>();

            foreach (var se in org.StereoElements)
            {
                switch (se)
                {
                    case ITetrahedralChirality tc:
                        {
                            var focus = tc.ChiralAtom;
                            var neighbors = tc.Ligands.ToList();
                            bool updated = false;
                            for (int i = 0; i < neighbors.Count; i++)
                            {
                                if (hydrogens.Contains(neighbors[i]))
                                {
                                    neighbors[i] = focus;
                                    updated = true;
                                }
                            }
                            tc.Ligands = neighbors;

                            // no changes
                            if (!updated)
                            {
                                elements.Add(tc);
                            }
                            else
                            {
                                elements.Add(new TetrahedralChirality(focus, neighbors, tc.Stereo));
                            }
                        }
                        break;
                    case ExtendedTetrahedral tc:
                        {
                            var focus = tc.Focus;
                            var carriers = tc.Carriers.ToArray();
                            var ends = ExtendedTetrahedral.FindTerminalAtoms(org, focus);
                            bool updated = false;
                            for (int i = 0; i < carriers.Length; i++)
                            {
                                if (hydrogens.Contains(carriers[i]))
                                {
                                    if (org.GetBond(carriers[i], ends[0]) != null)
                                        carriers[i] = ends[0];
                                    else
                                        carriers[i] = ends[1];
                                    updated = true;
                                }
                            }
                            // no changes
                            if (!updated)
                            {
                                elements.Add(tc);
                            }
                            else
                            {
                                elements.Add(new ExtendedTetrahedral(focus, carriers, tc.Configure));
                            }
                        }
                        break;
                    case IDoubleBondStereochemistry db:
                        {
                            var conformation = db.Stereo;

                            var orgStereo = db.StereoBond;
                            var orgLeft = db.Bonds[0];
                            var orgRight = db.Bonds[1];

                            // we use the following variable names to refer to the
                            // double bond atoms and substituents
                            // x       y
                            //  \     /
                            //   u = v

                            var u = orgStereo.Begin;
                            var v = orgStereo.End;
                            var x = orgLeft.GetOther(u);
                            var y = orgRight.GetOther(v);

                            // if xNew == x and yNew == y we don't need to find the
                            // connecting bonds
                            var xNew = x;
                            var yNew = y;

                            if (hydrogens.Contains(x))
                            {
                                conformation = conformation.Invert();
                                xNew = FindSingleBond(org, u, x);
                            }

                            if (hydrogens.Contains(y))
                            {
                                conformation = conformation.Invert();
                                yNew = FindSingleBond(org, v, y);
                            }

                            // no other atoms connected, invalid double-bond configuration
                            // is removed. example [2H]/C=C/[H]
                            if (x == null || y == null || xNew == null || yNew == null)
                                continue;

                            // no changes
                            if (x.Equals(xNew) && y.Equals(yNew))
                            {
                                elements.Add(db);
                                continue;
                            }

                            // XXX: may perform slow operations but works for now
                            var cpyLeft = !object.Equals(xNew, x) ? org.GetBond(u, xNew) : orgLeft;
                            var cpyRight = !object.Equals(yNew, y) ? org.GetBond(v, yNew) : orgRight;

                            elements.Add(new DoubleBondStereochemistry(orgStereo, new IBond[] { cpyLeft, cpyRight }, conformation));
                        }
                        break;
                    case ExtendedCisTrans db:
                        {
                            var config = db.Configure;
                            var focus = db.Focus;
                            var orgLeft = db.Carriers[0];
                            var orgRight = db.Carriers[1];
                            // we use the following variable names to refer to the
                            // extended double bond atoms and substituents
                            // x       y
                            //  \     /
                            //   u===v
                            var ends = ExtendedCisTrans.FindTerminalAtoms(org, focus);
                            var u = ends[0];
                            var v = ends[1];
                            var x = orgLeft.GetOther(u);
                            var y = orgRight.GetOther(v);
                            // if xNew == x and yNew == y we don't need to find the
                            // connecting bonds
                            var xNew = x;
                            var yNew = y;
                            if (hydrogens.Contains(x))
                            {
                                config = config.Flip();
                                xNew = FindSingleBond(org, u, x);
                            }
                            if (hydrogens.Contains(y))
                            {
                                config = config.Flip();
                                yNew = FindSingleBond(org, v, y);
                            }
                            // no other atoms connected, invalid double-bond configuration
                            // is removed. example [2H]/C=C/[H]
                            if (x == null || y == null || xNew == null || yNew == null)
                                continue;
                            // no changes
                            if (x.Equals(xNew) && y.Equals(yNew))
                            {
                                elements.Add(db);
                                continue;
                            }
                            // XXX: may perform slow operations but works for now
                            var cpyLeft = !object.Equals(xNew, x) ? org.GetBond(u, xNew) : orgLeft;
                            var cpyRight = !object.Equals(yNew, y) ? org.GetBond(v, yNew) : orgRight;
                            elements.Add(new ExtendedCisTrans(focus,
                                                             new IBond[] { cpyLeft, cpyRight },
                                                             config));
                        }
                        break;
                    case Atropisomeric a:
                        // can not have any H's
                        elements.Add(se);
                        break;
                }
            }

            org.SetAtoms(cpyAtoms.Take(nCpyAtoms));
            org.SetBonds(cpyBonds.Take(nCpyBonds));
            org.SetStereoElements(elements);

            // single electron and lone pairs are not really used but we update
            // them just in-case but we just use the inefficient AtomContainer
            // methods

            if (org.SingleElectrons.Count > 0)
            {
                var remove = new HashSet<ISingleElectron>();
                foreach (var se in org.SingleElectrons)
                {
                    if (hydrogens.Contains(se.Atom))
                        remove.Add(se);
                }
                foreach (var se in remove)
                {
                    org.SingleElectrons.Remove(se);
                }
            }

            if (org.LonePairs.Count > 0)
            {
                var remove = new HashSet<ILonePair>();
                foreach (var lp in org.LonePairs)
                {
                    if (hydrogens.Contains(lp.Atom))
                        remove.Add(lp);
                }
                foreach (var lp in remove)
                {
                    org.LonePairs.Remove(lp);
                }
            }

            if (sgroups != null)
            {
                foreach (var sgroup in sgroups)
                {
                    if (sgroup.GetValue(SgroupKey.CtabParentAtomList) != null)
                    {
                        var pal = (ICollection<IAtom>)sgroup.GetValue(SgroupKey.CtabParentAtomList);
                        foreach (var hydrogen in hydrogens)
                            pal.Remove(hydrogen);
                    }
                    foreach (var hydrogen in hydrogens)
                        sgroup.Remove(hydrogen);
                    foreach (var bondToHydrogen in bondsToHydrogens)
                        sgroup.Remove(bondToHydrogen);
                }
            }

            return org;
        }

        /// <summary>
        /// Create an copy of the <paramref name="org"/> structure with explicit hydrogens
        /// removed. Stereochemistry is updated but up and down bonds in a depiction
        /// may need to be recalculated (see. StructureDiagramGenerator).
        /// </summary>
        /// <param name="org">The AtomContainer from which to remove the hydrogens</param>
        /// <returns>The molecule without hydrogens.</returns>
        /// <seealso cref="CopyAndSuppressedHydrogens"/>
        // @cdk.keyword hydrogens, removal, suppress
        public static IAtomContainer RemoveHydrogens(IAtomContainer org)
        {
            return CopyAndSuppressedHydrogens(org);
        }

        /// <summary>
        /// Is the <paramref name="atom"/> a suppressible hydrogen and can be represented as
        /// implicit. A hydrogen is suppressible if it is not an ion, not the major
        /// isotope (i.e. it is a deuterium or tritium atom) and is not molecular
        /// hydrogen.
        /// </summary>
        /// <param name="container">the structure</param>
        /// <param name="atom">an atom in the structure</param>
        /// <returns>the atom is a hydrogen and it can be suppressed (implicit)</returns>
        private static bool SuppressibleHydrogen(IAtomContainer container, IAtom atom)
        {
            // is the atom a hydrogen
            if (!atom.AtomicNumber.Equals(AtomicNumbers.H))
                return false;
            // is the hydrogen an ion?
            if (atom.FormalCharge != null && atom.FormalCharge != 0)
                return false;
            // is the hydrogen deuterium / tritium?
            if (atom.MassNumber != null)
                return false;
            // molecule hydrogen with implicit H?
            if (atom.ImplicitHydrogenCount != null && atom.ImplicitHydrogenCount != 0)
                return false;
            // molecule hydrogen
            var neighbors = container.GetConnectedAtoms(atom).ToReadOnlyList();
            if (neighbors.Count == 1 && (neighbors[0].AtomicNumber.Equals(AtomicNumbers.H) || neighbors[0] is IPseudoAtom))
                return false;
            // what about bridging hydrogens?
            // hydrogens with atom-atom mapping?
            return true;
        }

        /// <summary>
        /// Increment the implicit hydrogen count of the provided atom. If the atom
        /// was a non-pseudo atom and had an unset hydrogen count an exception is
        /// thrown.
        /// </summary>
        /// <param name="atom">an atom to increment the hydrogen count of</param>
        private static void IncrementImplHydrogenCount(IAtom atom)
        {
            var hCount = atom.ImplicitHydrogenCount;

            if (hCount == null)
            {
                if (!(atom is IPseudoAtom))
                    throw new ArgumentException("a non-pseudo atom had an unset hydrogen count");
                hCount = 0;
            }

            atom.ImplicitHydrogenCount = hCount + 1;
        }

        /// <summary>
        /// Is the 'atom' a suppressible hydrogen and can be represented as
        /// implicit. A hydrogen is suppressible if it is not an ion, not the major
        /// isotope (i.e. it is a deuterium or tritium atom) and is not molecular
        /// hydrogen.
        /// </summary>
        /// <param name="container">the structure</param>
        /// <param name="graph">adjacent list representation</param>
        /// <param name="v">vertex (atom index)</param>
        /// <returns>the atom is a hydrogen and it can be suppressed (implicit)</returns>
        private static bool SuppressibleHydrogen(IAtomContainer container, int[][] graph, EdgeToBondMap bondmap, int v)
        {
            var atom = container.Atoms[v];

            // is the atom a hydrogen
            if (!atom.AtomicNumber.Equals(AtomicNumbers.H))
                return false;
            // is the hydrogen an ion?
            if (atom.FormalCharge != null && atom.FormalCharge != 0)
                return false;
            // is the hydrogen deuterium / tritium?
            if (atom.MassNumber != null)
                return false;
            // hydrogen is either not attached to 0 or 2 neighbors
            if (graph[v].Length != 1)
                return false;
            // non-single bond
            if (bondmap[v, graph[v][0]].Order != BondOrder.Single)
                return false;

            // okay the hydrogen has one neighbor, if that neighbor is a
            // hydrogen (i.e. molecular hydrogen) then we can not suppress it
            if (string.Equals("H", container.Atoms[graph[v][0]].Symbol, StringComparison.Ordinal))
                return false;
            // can not nicely suppress hydrogens on pseudo atoms
            if (container.Atoms[graph[v][0]] is IPseudoAtom)
                return false;

            return true;
        }

        /// <summary>
        /// Finds an neighbor connected to <paramref name="atom"/> which is connected by a
        /// single bond and is not 'exclude'.
        /// </summary>
        /// <param name="container">structure</param>
        /// <param name="atom">atom to find a neighbor of</param>
        /// <param name="exclude">the neighbor should not be this atom</param>
        /// <returns>a neighbor of <paramref name="atom"/>, <see langword="null"/> if not found</returns>
        private static IAtom FindSingleBond(IAtomContainer container, IAtom atom, IAtom exclude)
        {
            foreach (var bond in container.GetConnectedBonds(atom))
            {
                if (bond.Order != BondOrder.Single)
                    continue;
                var neighbor = bond.GetOther(atom);
                if (!neighbor.Equals(exclude))
                    return neighbor;
            }
            return null;
        }

        /// <summary>
        /// Produces an AtomContainer without explicit Hs but with H count from one with Hs.
        /// Hs bonded to more than one heavy atom are preserved.  The new molecule is a deep copy.
        /// </summary>
        /// <returns>The mol without Hs.</returns>
        // @cdk.keyword    hydrogens, removal
        [Obsolete(nameof(SuppressHydrogens) + " will now not removed bridging hydrogens by default")]
        public static IAtomContainer RemoveHydrogensPreserveMultiplyBonded(IAtomContainer ac)
        {
            return CopyAndSuppressedHydrogens(ac);
        }

        /// <summary>
        /// Produces an AtomContainer without explicit Hs (except those listed) but with H count from one with Hs.
        /// The new molecule is a deep copy.
        /// </summary>
        /// <param name="preserve">a list of H atoms to preserve.</param>
        /// <returns>The mol without Hs.</returns>
        // @cdk.keyword      hydrogens, removal
        [Obsolete("not used by the internal API " + nameof(SuppressHydrogens) + " will now only suppress hydrogens that can be represent as a h count")]
        private static IAtomContainer RemoveHydrogens(IAtomContainer ac, List<IAtom> preserve)
        {
            var map = new Dictionary<IAtom, IAtom>();
            // maps original atoms to clones.
            var remove = new List<IAtom>();
            // lists removed Hs.

            // Clone atoms except those to be removed.
            var mol = ac.Builder.NewAtomContainer();
            int count = ac.Atoms.Count;
            for (int i = 0; i < count; i++)
            {
                // Clone/remove this atom?
                var atom = ac.Atoms[i];
                if (!SuppressibleHydrogen(ac, atom) || preserve.Contains(atom))
                {
                    IAtom a = null;
                    a = (IAtom)atom.Clone();
                    a.ImplicitHydrogenCount = 0;
                    mol.Atoms.Add(a);
                    map.Add(atom, a);
                }
                else
                {
                    remove.Add(atom);
                    // maintain list of removed H.
                }
            }

            // Clone bonds except those involving removed atoms.
            count = ac.Bonds.Count;
            for (int i = 0; i < count; i++)
            {
                // Check bond.
                var bond = ac.Bonds[i];
                var atom0 = bond.Begin;
                var atom1 = bond.End;
                bool remove_bond = false;
                foreach (var atom in bond.Atoms)
                {
                    if (remove.Contains(atom))
                    {
                        remove_bond = true;
                        break;
                    }
                }

                // Clone/remove this bond?
                if (!remove_bond)
                {
                    var clone = (IBond)ac.Bonds[i].Clone();
                    clone.SetAtoms(new[] { map[atom0], map[atom1] });
                    mol.Bonds.Add(clone);
                }
            }

            // Recompute hydrogen counts of neighbours of removed Hydrogens.
            foreach (var Remove in remove)
            {
                // Process neighbours.
                foreach (var neighbor in ac.GetConnectedAtoms(Remove))
                {
                    var neighb = map[neighbor];
                    neighb.ImplicitHydrogenCount = neighb.ImplicitHydrogenCount + 1;
                }
            }

            return (mol);
        }

        /// <summary>
        /// Sets a property on all <see cref="IAtom"/>s in the given container.
        /// </summary>
        public static void SetAtomProperties(IAtomContainer container, string propKey, object propVal)
        {
            if (container != null)
            {
                foreach (var atom in container.Atoms)
                {
                    atom.SetProperty(propKey, propVal);
                }
            }
        }

        /// <summary>
        /// A method to remove ElectronContainerListeners.
        /// ElectronContainerListeners are used to detect changes
        /// in ElectronContainers (like bonds) and to notify
        /// registered Listeners in the event of a change.
        /// If an object looses interest in such changes, it should
        /// unregister with this AtomContainer in order to improve
        /// performance of this class.
        /// </summary>
        public static void UnregIsterElectronContainerListeners(IAtomContainer container)
        {
            foreach (var electronContainer in container.GetElectronContainers())
            {
                electronContainer.Listeners.Remove(container);
            }
        }
        /// <summary>
        /// A method to remove AtomListeners.
        /// AtomListeners are used to detect changes
        /// in Atom objects within this AtomContainer and to notify
        /// registered Listeners in the event of a change.
        /// If an object looses interest in such changes, it should
        /// unregister with this AtomContainer in order to improve
        /// performance of this class.
        /// </summary>
        public static void UnregIsterAtomListeners(IAtomContainer container)
        {
            foreach (var atom in container.Atoms)
                atom.Listeners.Remove(container);
        }

        /// <summary>
        /// Compares this AtomContainer with another given AtomContainer and returns
        /// the Intersection between them.
        /// </summary>
        /// <remarks>
        /// <b>Important Note</b> : This is not the maximum common substructure.
        /// </remarks>
        /// <param name="container1">an AtomContainer object</param>
        /// <param name="container2">an AtomContainer object</param>
        /// <returns>An AtomContainer containing the intersection between <paramref name="container1"/> and <paramref name="container2"/></returns>
        public static IAtomContainer GetIntersection(IAtomContainer container1, IAtomContainer container2)
        {
            var intersection = container1.Builder.NewAtomContainer();

            foreach (var atom1 in container1.Atoms)
                if (container2.Contains(atom1))
                    intersection.Atoms.Add(atom1);
            foreach (var electronContainer1 in container1.GetElectronContainers())
                if (container2.Contains(electronContainer1))
                    intersection.Add(electronContainer1);

            return intersection;
        }

        /// <summary>
        /// Convenience method to perceive atom types for all <see cref="IAtom"/>s in the
        /// <see cref="IAtomContainer"/>, using the <see cref="CDKAtomTypeMatcher"/>.If the
        /// matcher finds a matching atom type, the <see cref="IAtom"/> will be configured
        /// to have the same properties as the <see cref="IAtomType"/>. If no matching atom
        /// type is found, no configuration is performed.
        /// <b>This method overwrites existing values.</b>
        /// </summary>
        /// <param name="container"></param>
        public static void PercieveAtomTypesAndConfigureAtoms(IAtomContainer container)
        {
            var matcher = CDK.AtomTypeMatcher;
            foreach (var atom in container.Atoms)
            {
                var matched = matcher.FindMatchingAtomType(container, atom);
                if (matched != null)
                    AtomTypeManipulator.Configure(atom, matched);
            }
        }

        /// <summary>
        /// Convenience method to perceive atom types for all <see cref="IAtom"/>s in the
        /// <see cref="IAtomContainer"/>, using the <see cref="CDKAtomTypeMatcher"/>. If the
        /// matcher finds a matching atom type, the <see cref="IAtom"/> will be configured
        /// to have the same properties as the <see cref="IAtomType"/>. If no matching atom
        /// type is found, no configuration is performed.
        /// <b>This method overwrites existing values.</b>
        /// </summary>
        public static void PercieveAtomTypesAndConfigureUnsetProperties(IAtomContainer container)
        {
            var matcher = CDK.AtomTypeMatcher;
            foreach (var atom in container.Atoms)
            {
                var matched = matcher.FindMatchingAtomType(container, atom);
                if (matched != null) AtomTypeManipulator.ConfigureUnsetProperties(atom, matched);
            }
        }

        /// <summary>
        /// This method will reset all atom configuration to unset.
        /// </summary>
        /// <remarks>
        /// This method is the reverse of <see cref="PercieveAtomTypesAndConfigureAtoms(IAtomContainer)"/> 
        /// and after a call to this method all atoms will be "unconfigured".
        /// <note type="note">
        /// Note that it is not a complete reversal of <see cref="PercieveAtomTypesAndConfigureAtoms(IAtomContainer)"/> 
        /// since the atomic symbol of the atoms remains unchanged. Also, all the flags that were set
        /// by the configuration method (such as <see cref="IAtomType.IsHydrogenBondAcceptor"/> or <see cref="IMolecularEntity.IsAromatic"/>) will be set to False.
        /// </note>
        /// </remarks>
        /// <param name="container">The molecule, whose atoms are to be unconfigured</param>
        /// <seealso cref="PercieveAtomTypesAndConfigureAtoms(IAtomContainer)"/>
        public static void ClearAtomConfigurations(IAtomContainer container)
        {
            foreach (var atom in container.Atoms)
            {
                atom.AtomTypeName = (string)null;
                atom.MaxBondOrder = BondOrder.Unset;
                atom.BondOrderSum = null;
                atom.CovalentRadius = null;
                atom.Valency = null;
                atom.FormalCharge = null;
                atom.Hybridization = Hybridization.Unset;
                atom.FormalNeighbourCount = null;
                atom.IsHydrogenBondAcceptor = false;
                atom.IsHydrogenBondDonor = false;
                atom.SetProperty(CDKPropertyName.ChemicalGroupConstant, null);
                atom.IsAromatic = false;
                atom.SetProperty(CDKPropertyName.Color, null);
                atom.ExactMass = null;
            }
        }

        /// <summary>
        /// Returns the sum of bond orders, where a single bond counts as one
        /// <i>single bond equivalent</i>, a double as two, etc.
        /// </summary>
        public static int GetSingleBondEquivalentSum(IAtomContainer container)
        {
            int sum = 0;
            foreach (var bond in container.Bonds)
            {
                var order = bond.Order;
                if (!order.IsUnset())
                {
                    sum += order.Numeric();
                }
            }
            return sum;
        }

        public static BondOrder GetMaximumBondOrder(IAtomContainer container)
        {
            return BondManipulator.GetMaximumBondOrder(container.Bonds);
        }

        /// <summary>
        /// Returns a set of nodes excluding all the hydrogens.
        /// </summary>
        /// <returns>The heavyAtoms value</returns>
        // @cdk.keyword    hydrogens, removal
        public static IList<IAtom> GetHeavyAtoms(IAtomContainer container)
        {
            var newAc = new List<IAtom>();
            for (int f = 0; f < container.Atoms.Count; f++)
            {
                if (!container.Atoms[f].AtomicNumber.Equals(AtomicNumbers.H))
                {
                    newAc.Add(container.Atoms[f]);
                }
            }
            return newAc;
        }

        /// <summary>
        /// Generates a cloned atomcontainer with all atoms being carbon, all bonds
        /// being single non-aromatic
        /// </summary>
        /// <param name="atomContainer">The input atomcontainer</param>
        /// <returns>The new atomcontainer</returns>
        [Obsolete("not all attributes are removed producing unexpected results, use " + nameof(Anonymise))]
        public static IAtomContainer CreateAllCarbonAllSingleNonAromaticBondAtomContainer(IAtomContainer atomContainer)
        {
            var query = (IAtomContainer)atomContainer.Clone();
            for (int i = 0; i < query.Bonds.Count; i++)
            {
                query.Bonds[i].Order = BondOrder.Single;
                query.Bonds[i].IsAromatic = false;
                query.Bonds[i].IsSingleOrDouble = false;
                query.Bonds[i].Begin.Symbol = "C";
                query.Bonds[i].Begin.Hybridization = Hybridization.Unset;
                query.Bonds[i].End.Symbol = "C";
                query.Bonds[i].End.Hybridization = Hybridization.Unset;
                query.Bonds[i].Begin.IsAromatic = false;
                query.Bonds[i].End.IsAromatic = false;
            }
            return query;
        }

        /// <summary>
        /// Anonymise the provided container to single-bonded carbon atoms. No
        /// information other then the connectivity from the original container is
        /// retrained.
        /// </summary>
        /// <param name="src">an atom container</param>
        /// <returns>anonymised container</returns>
        public static IAtomContainer Anonymise(IAtomContainer src)
        {
            var builder = src.Builder;

            var atoms = new IAtom[src.Atoms.Count];
            var bonds = new IBond[src.Bonds.Count];

            for (int i = 0; i < atoms.Length; i++)
            {
                atoms[i] = builder.NewAtom();
                atoms[i].AtomicNumber = 6;
                atoms[i].Symbol = "C";
                atoms[i].ImplicitHydrogenCount = 0;
                var srcAtom = src.Atoms[i];
                if (srcAtom.Point2D != null)
                    atoms[i].Point2D = srcAtom.Point2D;
                if (srcAtom.Point3D != null)
                    atoms[i].Point3D = srcAtom.Point3D;
            }
            for (int i = 0; i < bonds.Length; i++)
            {
                var bond = src.Bonds[i];
                int u = src.Atoms.IndexOf(bond.Begin);
                int v = src.Atoms.IndexOf(bond.End);
                bonds[i] = builder.NewBond(atoms[u], atoms[v]);
            }

            var dest = builder.NewAtomContainer(atoms, bonds);
            return dest;
        }

        /// <summary>
        /// Create a skeleton copy of the provided structure. The skeleton copy is
        /// similar to an anonymous copy (<see cref="Anonymise(IAtomContainer)"/>) except that atom
        /// elements are preserved. All bonds are converted to single bonds and a
        /// 'clean' atom is created for the input elements. The 'clean' atom has
        /// unset charge, mass, and hydrogen count.
        /// </summary>
        /// <param name="src">input structure</param>
        /// <returns>the skeleton copy</returns>
        public static IAtomContainer Skeleton(IAtomContainer src)
        {
            var builder = src.Builder;

            var atoms = new IAtom[src.Atoms.Count];
            var bonds = new IBond[src.Bonds.Count];

            for (int i = 0; i < atoms.Length; i++)
            {
                atoms[i] = builder.NewAtom(src.Atoms[i].AtomicNumber);
            }
            for (int i = 0; i < bonds.Length; i++)
            {
                var bond = src.Bonds[i];
                var u = src.Atoms.IndexOf(bond.Begin);
                var v = src.Atoms.IndexOf(bond.End);
                bonds[i] = builder.NewBond(atoms[u], atoms[v]);
            }

            var dest = builder.NewAtomContainer(atoms, bonds);
            return dest;
        }

        /// <summary>
        /// Returns the sum of the bond order equivalents for a given IAtom. It
        /// considers single bonds as 1.0, double bonds as 2.0, triple bonds as 3.0,
        /// and quadruple bonds as 4.0.
        /// </summary>
        /// <param name="atom">The atom for which to calculate the bond order sum</param>
        /// <returns>The number of bond order equivalents for this atom</returns>
        public static double GetBondOrderSum(IAtomContainer container, IAtom atom)
        {
            double count = 0;
            foreach (var bond in container.GetConnectedBonds(atom))
            {
                var order = bond.Order;
                if (!order.IsUnset())
                {
                    count += order.Numeric();
                }
            }
            return count;
        }

        /// <summary>
        /// Assigns <see cref="IBond.IsSingleOrDouble"/> to the bonds of
        /// a container. The single or double flag indicates uncertainty of bond
        /// order and in this case is assigned to all aromatic bonds (and atoms)
        /// which occur in rings. If any such bonds are found the flag is also set
        /// on the container.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Tools.AtomContainerManipulator_Example.cs+SetSingleOrDoubleFlags"]/*' />
        /// </example>
        /// <param name="ac">container to which the flags are assigned</param>
        /// <returns>the input for convenience</returns>
        public static IAtomContainer SetSingleOrDoubleFlags(IAtomContainer ac)
        {
            // note - we could check for any aromatic bonds to avoid RingSearch but
            // RingSearch is fast enough it probably wouldn't do much to check
            // before hand
            var rs = new RingSearch(ac);
            bool singleOrDouble = false;
            foreach (var bond in rs.RingFragments().Bonds)
            {
                if (bond.IsAromatic)
                {
                    bond.IsSingleOrDouble = true;
                    bond.Begin.IsSingleOrDouble = true;
                    bond.End.IsSingleOrDouble = true;
                    singleOrDouble = singleOrDouble | true;
                }
            }
            if (singleOrDouble)
            {
                ac.IsSingleOrDouble = true;
            }

            return ac;
        }

        public static void PerceiveRadicals(IAtomContainer mol)
        {
            foreach (var atom in mol.Atoms)
            {
                int v;
                var q = atom.FormalCharge ?? 0;
                switch (atom.AtomicNumber)
                {
                    case AtomicNumbers.C:
                        if (q == 0)
                        {
                            v = CalcValence(atom);
                            if (v == 2)
                                mol.AddSingleElectronTo(atom);
                            if (v < 4)
                                mol.AddSingleElectronTo(atom);
                        }
                        break;
                    case AtomicNumbers.N:
                        if (q == 0)
                        {
                            v = CalcValence(atom);
                            if (v < 3)
                                mol.AddSingleElectronTo(atom);
                        }
                        break;
                    case AtomicNumbers.O:
                        if (q == 0)
                        {
                            v = CalcValence(atom);
                            if (v < 2)
                                mol.AddSingleElectronTo(atom);
                            if (v < 1)
                                mol.AddSingleElectronTo(atom);
                        }
                        break;
                }
            }
        }

        public static void PerceiveDativeBonds(IAtomContainer mol)
        {
            foreach (var bond in mol.Bonds)
            {
                var beg = bond.Begin;
                var end = bond.End;
                if (IsDativeDonor(end) && IsDativeAcceptor(beg))
                {
                    bond.Display = BondDisplay.ArrowBegin;
                }
                else if (IsDativeDonor(beg) && IsDativeAcceptor(end))
                {
                    bond.Display = BondDisplay.ArrowEnd;
                }
                else if (IsChargedDativeDonor(end) && IsChargedDativeAcceptor(beg))
                {
                    bond.Display = BondDisplay.ArrowBegin;
                }
                else if (IsChargedDativeDonor(beg) && IsChargedDativeAcceptor(end))
                {
                    bond.Display = BondDisplay.ArrowEnd;
                }
            }
            foreach (var bond in mol.Bonds)
            {
                var beg = bond.Begin;
                var end = bond.End;
                if (bond.Display == BondDisplay.ArrowBegin
                 || bond.Display == BondDisplay.ArrowEnd)
                {
                    beg.FormalCharge = 0;
                    end.FormalCharge = 0;
                }
            }
        }

        private static int CalcValence(IAtom atom)
        {
            int v = atom.ImplicitHydrogenCount ?? 0;
            foreach (var bond in atom.Bonds)
            {
                var order = bond.Order;
                if (!order.IsUnset())
                    v += order.Numeric();
            }
            return v;
        }

        private static bool IsDativeDonor(IAtom a)
        {
            switch (a.AtomicNumber)
            {
                case AtomicNumbers.N:
                case AtomicNumbers.P:
                    return a.FormalCharge == 0 && CalcValence(a) == 4;
                case AtomicNumbers.O:
                    return a.FormalCharge == 0 && CalcValence(a) == 3;
                default:
                    return false;
            }
        }

        private static bool IsDativeAcceptor(IAtom a)
        {
            if (PeriodicTable.IsMetal(a.AtomicNumber))
                return true;
            switch (a.AtomicNumber)
            {
                case AtomicNumbers.B:
                    return a.FormalCharge == 0 && CalcValence(a) == 4;
                case AtomicNumbers.O:
                    return a.FormalCharge == 0 && CalcValence(a) == 1;
                default:
                    return false;
            }
        }

        private static bool IsChargedDativeDonor(IAtom a)
        {
            switch (a.AtomicNumber)
            {
                case AtomicNumbers.N:
                case AtomicNumbers.P:
                    return a.FormalCharge == +1 && CalcValence(a) == 4;
                case AtomicNumbers.O:
                    return a.FormalCharge == +1 && CalcValence(a) == 3;
                default:
                    return false;
            }
        }

        private static bool IsChargedDativeAcceptor(IAtom a)
        {
            if (a.FormalCharge != -1)
                return false;
            if (PeriodicTable.IsMetal(a.AtomicNumber))
                return true;
            switch (a.AtomicNumber)
            {
                case AtomicNumbers.B:
                    return CalcValence(a) == 4;
                case AtomicNumbers.O:
                    return CalcValence(a) == 1;
                default:
                    return false;
            }
        }
    }
}

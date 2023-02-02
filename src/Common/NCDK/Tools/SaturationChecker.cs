/* Copyright (C) 2001-2007  The Chemistry Development Kit (CDK) project
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

using NCDK.Config;
using NCDK.Graphs;
using NCDK.RingSearches;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NCDK.Tools
{
    public interface ISaturationChecker : IValencyChecker, IDeduceBondOrderTool
    {
        double GetCurrentMaxBondOrder(IAtom atom, IAtomContainer ac);
        int CalculateNumberOfImplicitHydrogens(IAtom atom, IAtomContainer container);
    }

    /// <summary>
    /// Provides methods for checking whether an atoms valences are saturated with
    /// respect to a particular atom type.
    /// </summary>
    /// <remarks>
    /// Important: this class does not deal with hybridization states, which makes
    /// it fail, for example, for situations where bonds are marked as aromatic (either
    /// 1.5 or single an Aromatic).</remarks>
    // @author     steinbeck
    // @author  Egon Willighagen
    // @cdk.created    2001-09-04
    // @cdk.keyword    saturation
    // @cdk.keyword    atom, valency
    // @cdk.module     valencycheck
    public class SaturationChecker : ISaturationChecker
    {
        private static readonly AtomTypeFactory structgenATF = CDK.StructgenAtomTypeFactory;

        public SaturationChecker()
        {
        }

        public bool HasPerfectConfiguration(IAtom atom, IAtomContainer ac)
        {
            var bondOrderSum = ac.GetBondOrderSum(atom);
            var maxBondOrder = ac.GetMaximumBondOrder(atom);
            var atomTypes = structgenATF.GetAtomTypes(atom.Symbol);
            if (!atomTypes.Any())
                return true;
            Debug.WriteLine("*** Checking for perfect configuration ***");
            try
            {
                Debug.WriteLine($"Checking configuration of atom {ac.Atoms.IndexOf(atom)}");
                Debug.WriteLine($"Atom has bondOrderSum = {bondOrderSum}");
                Debug.WriteLine($"Atom has max = {bondOrderSum}");
            }
            catch (Exception)
            {
            }
            foreach (var atomType in atomTypes)
            {
                if (bondOrderSum == atomType.BondOrderSum && maxBondOrder == atomType.MaxBondOrder)
                {
                    try
                    {
                        Debug.WriteLine($"Atom {ac.Atoms.IndexOf(atom)} has perfect configuration");
                    }
                    catch (Exception)
                    {
                    }
                    return true;
                }
            }
            try
            {
                Debug.WriteLine($"*** Atom {ac.Atoms.IndexOf(atom)} has imperfect configuration ***");
            }
            catch (Exception)
            {
            }
            return false;
        }

        /// <summary>
        /// Determines of all atoms on the AtomContainer are saturated.
        /// </summary>
        public bool IsSaturated(IAtomContainer ac)
        {
            Debug.WriteLine("Are all atoms saturated?");
            for (int f = 0; f < ac.Atoms.Count; f++)
            {
                if (!IsSaturated(ac.Atoms[f], ac))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns whether a bond is unsaturated. A bond is unsaturated if
        /// <b>both</b> Atoms in the bond are unsaturated.
        /// </summary>
        public bool IsUnsaturated(IBond bond, IAtomContainer atomContainer)
        {
            var atoms = BondManipulator.GetAtomArray(bond);
            bool isUnsaturated = true;
            for (int i = 0; i < atoms.Length; i++)
            {
                isUnsaturated = isUnsaturated && !IsSaturated(atoms[i], atomContainer);
            }
            return isUnsaturated;
        }

        /// <summary>
        /// Returns whether a bond is saturated. A bond is saturated if
        /// <b>both</b> Atoms in the bond are saturated.
        /// </summary>
        public bool IsSaturated(IBond bond, IAtomContainer atomContainer)
        {
            var atoms = BondManipulator.GetAtomArray(bond);
            bool isSaturated = true;
            for (int i = 0; i < atoms.Length; i++)
            {
                isSaturated = isSaturated && IsSaturated(atoms[i], atomContainer);
            }
            return isSaturated;
        }

        /// <summary>
        /// Checks whether an atom is saturated by comparing it with known atom types.
        /// </summary>
        public bool IsSaturated(IAtom atom, IAtomContainer ac)
        {
            var atomTypes = structgenATF.GetAtomTypes(atom.Symbol);
            if (!atomTypes.Any())
                return true;
            double bondOrderSum = 0;
            var maxBondOrder = BondOrder.Unset;
            int hcount = 0;
            int charge = 0;
            bool isInited = false;
            foreach (var atomType in atomTypes)
            {
                if (!isInited)
                {
                    isInited = true;
                    bondOrderSum = ac.GetBondOrderSum(atom);
                    maxBondOrder = ac.GetMaximumBondOrder(atom);
                    hcount = atom.ImplicitHydrogenCount ?? 0;
                    charge = atom.FormalCharge ?? 0;
                    try
                    {
                        Debug.WriteLine($"*** Checking saturation of atom {atom.Symbol} {ac.Atoms.IndexOf(atom)} ***");
                        Debug.WriteLine($"bondOrderSum: {bondOrderSum}");
                        Debug.WriteLine($"maxBondOrder: {maxBondOrder}");
                        Debug.WriteLine($"hcount: {hcount}");
                    }
                    catch (Exception exc)
                    {
                        Debug.WriteLine(exc);
                    }
                }
                if (bondOrderSum - charge + hcount == atomType.BondOrderSum
                 && !BondManipulator.IsHigherOrder(maxBondOrder, atomType.MaxBondOrder))
                {
                    Debug.WriteLine("*** Good ! ***");
                    return true;
                }
            }
            Debug.WriteLine("*** Bad ! ***");
            return false;
        }

        /// <summary>
        /// Checks if the current atom has exceeded its bond order sum value.
        /// </summary>
        /// <param name="atom">The Atom to check</param>
        /// <param name="ac">The atom container context</param>
        /// <returns>oversaturated or not</returns>
        public bool IsOverSaturated(IAtom atom, IAtomContainer ac)
        {
            var atomTypes = structgenATF.GetAtomTypes(atom.Symbol);
            if (!atomTypes.Any())
                return false;
            var bondOrderSum = ac.GetBondOrderSum(atom);
            var maxBondOrder = ac.GetMaximumBondOrder(atom);
            var hcount = atom.ImplicitHydrogenCount ?? 0;
            var charge = atom.FormalCharge ?? 0;
            try
            {
                Debug.WriteLine($"*** Checking saturation of atom {ac.Atoms.IndexOf(atom)} ***");
                Debug.WriteLine($"bondOrderSum: {bondOrderSum}");
                Debug.WriteLine($"maxBondOrder: {maxBondOrder}");
                Debug.WriteLine($"hcount: {hcount}");
            }
            catch (Exception)
            {
            }
            foreach (var atomType in atomTypes)
            {
                if (bondOrderSum - charge + hcount > atomType.BondOrderSum)
                {
                    Debug.WriteLine("*** Good ! ***");
                    return true;
                }
            }
            Debug.WriteLine("*** Bad ! ***");
            return false;
        }

        /// <summary>
        /// Returns the currently maximum bond order for this atom.
        /// </summary>
        /// <param name="atom">The atom to be checked</param>
        /// <param name="ac">The AtomContainer that provides the context</param>
        /// <returns>the currently maximum bond order for this atom</returns>
        public double GetCurrentMaxBondOrder(IAtom atom, IAtomContainer ac)
        {
            var atomTypes = structgenATF.GetAtomTypes(atom.Symbol);
            if (!atomTypes.Any())
                return 0;
            var bondOrderSum = ac.GetBondOrderSum(atom);
            var hcount = atom.ImplicitHydrogenCount ?? 0;
            double max = 0;
            double current = 0;
            foreach (var atomType in atomTypes)
            {
                current = hcount + bondOrderSum;
                if (atomType.BondOrderSum - current > max)
                {
                    max = atomType.BondOrderSum.Value - current;
                }
            }
            return max;
        }

        /// <summary>
        /// Resets the bond orders of all atoms to 1.0.
        /// </summary>
        private static void Unsaturate(IAtomContainer atomContainer)
        {
            foreach (var bond in atomContainer.Bonds)
                bond.Order = BondOrder.Single;
        }

        /// <summary>
        /// Resets the bond order of the Bond to 1.0.
        /// </summary>
        private static void UnsaturateBonds(IAtomContainer container)
        {
            foreach (var bond in container.Bonds)
                bond.Order = BondOrder.Single;
        }

        /// <summary>
        /// Saturates a molecule by setting appropriate bond orders.
        /// This method is known to fail, especially on pyrrole-like compounds.
        /// Consider using <see cref="Smiles.DeduceBondSystemTool"/>, which should work better
        /// </summary>
        // @cdk.keyword bond order, calculation
        // @cdk.created 2003-10-03
        public void NewSaturate(IAtomContainer atomContainer)
        {
            Trace.TraceInformation("Saturating atomContainer by adjusting bond orders...");
            bool allSaturated = IsSaturated(atomContainer);
            if (!allSaturated)
            {
                var bonds = new IBond[atomContainer.Bonds.Count];
                for (int i = 0; i < bonds.Length; i++)
                    bonds[i] = atomContainer.Bonds[i];
                var succeeded = NewSaturate(bonds, atomContainer);
                for (int i = 0; i < bonds.Length; i++)
                {
                    if (bonds[i].Order == BondOrder.Double 
                     && bonds[i].IsAromatic
                     && bonds[i].Begin.AtomicNumber.Equals(AtomicNumbers.N) 
                     && bonds[i].End.AtomicNumber.Equals(AtomicNumbers.N))
                    {
                        int atomtohandle = 0;
                        if (string.Equals(bonds[i].Begin.Symbol, "N", StringComparison.Ordinal))
                            atomtohandle = 1;
                        var bondstohandle = atomContainer.GetConnectedBonds(bonds[i].Atoms[atomtohandle]);
                        foreach (var bond in bondstohandle)
                        {
                            if (bond.Order == BondOrder.Single && bond.IsAromatic)
                            {
                                bond.Order = BondOrder.Double;
                                bonds[i].Order = BondOrder.Single;
                                break;
                            }
                        }
                    }
                }
                if (!succeeded)
                {
                    throw new CDKException("Could not saturate this atomContainer!");
                }
            }
        }

        /// <summary>
        /// Saturates a set of Bonds in an AtomContainer.
        /// This method is known to fail, especially on pyrrole-like compounds.
        /// Consider using <see cref="Smiles.DeduceBondSystemTool"/>, which should work better
        /// </summary>
        public bool NewSaturate(IBond[] bonds, IAtomContainer atomContainer)
        {
            Debug.WriteLine($"Saturating bond set of size: {bonds.Length}");
            bool bondsAreFullySaturated = true;
            if (bonds.Length > 0)
            {
                var bond = bonds[0];

                // determine bonds left
                var leftBondCount = bonds.Length - 1;
                var leftBonds = new IBond[leftBondCount];
                Array.Copy(bonds, 1, leftBonds, 0, leftBondCount);

                // examine this bond
                if (IsUnsaturated(bond, atomContainer))
                {
                    // either this bonds should be saturated or not

                    // try to leave this bond unsaturated and saturate the left bonds saturate this bond
                    if (leftBondCount > 0)
                    {
                        Debug.WriteLine($"Recursing with unsaturated bond with #bonds: {leftBondCount}");
                        bondsAreFullySaturated = NewSaturate(leftBonds, atomContainer) && !IsUnsaturated(bond, atomContainer);
                    }
                    else
                    {
                        bondsAreFullySaturated = false;
                    }

                    // ok, did it work? if not, saturate this bond, and recurse
                    if (!bondsAreFullySaturated)
                    {
                        Debug.WriteLine("First try did not work...");
                        // ok, revert saturating this bond, and recurse again
                        bool couldSaturate = NewSaturate(bond, atomContainer);
                        if (couldSaturate)
                        {
                            if (leftBondCount > 0)
                            {
                                Debug.WriteLine($"Recursing with saturated bond with #bonds: {leftBondCount}");
                                bondsAreFullySaturated = NewSaturate(leftBonds, atomContainer);
                            }
                            else
                            {
                                bondsAreFullySaturated = true;
                            }
                        }
                        else
                        {
                            bondsAreFullySaturated = false;
                            // no need to recurse, because we already know that this bond
                            // unsaturated does not work
                        }
                    }
                }
                else if (IsSaturated(bond, atomContainer))
                {
                    Debug.WriteLine("This bond is already saturated.");
                    if (leftBondCount > 0)
                    {
                        Debug.WriteLine($"Recursing with #bonds: {leftBondCount}");
                        bondsAreFullySaturated = NewSaturate(leftBonds, atomContainer);
                    }
                    else
                    {
                        bondsAreFullySaturated = true;
                    }
                }
                else
                {
                    Debug.WriteLine("Cannot saturate this bond");
                    // but, still recurse (if possible)
                    if (leftBondCount > 0)
                    {
                        Debug.WriteLine($"Recursing with saturated bond with #bonds: {leftBondCount}");
                        bondsAreFullySaturated = NewSaturate(leftBonds, atomContainer) && !IsUnsaturated(bond, atomContainer);
                    }
                    else
                    {
                        bondsAreFullySaturated = !IsUnsaturated(bond, atomContainer);
                    }
                }
            }
            Debug.WriteLine($"Is bond set fully saturated?: {bondsAreFullySaturated}");
            Debug.WriteLine($"Returning to level: {(bonds.Length + 1)}");
            return bondsAreFullySaturated;
        }

        /// <summary>
        /// Saturate atom by adjusting its bond orders.
        /// This method is known to fail, especially on pyrrole-like compounds.
        /// Consider using <see cref="Smiles.DeduceBondSystemTool"/>, which should work better
        /// </summary>
        public bool NewSaturate(IBond bond, IAtomContainer atomContainer)
        {
            var atoms = BondManipulator.GetAtomArray(bond);
            var atom = atoms[0];
            var partner = atoms[1];
            Debug.WriteLine($"  saturating bond: {atom.Symbol}-{partner.Symbol}");
            var atomTypes1 = structgenATF.GetAtomTypes(atom.Symbol);
            var atomTypes2 = structgenATF.GetAtomTypes(partner.Symbol);
            bool bondOrderIncreased = true;
            while (bondOrderIncreased && !IsSaturated(bond, atomContainer))
            {
                Debug.WriteLine("Can increase bond order");
                bondOrderIncreased = false;
                foreach (var aType1 in atomTypes1)
                {
                    if (bondOrderIncreased)
                        break;
                    Debug.WriteLine($"  considering atom type: {aType1}");
                    if (CouldMatchAtomType(atomContainer, atom, aType1))
                    {
                        Debug.WriteLine($"  trying atom type: {aType1}");
                        foreach (var aType2 in atomTypes2)
                        {
                            if (bondOrderIncreased)
                                break;
                            Debug.WriteLine($"  considering partner type: {aType1}");
                            if (CouldMatchAtomType(atomContainer, partner, aType2))
                            {
                                Debug.WriteLine($"    with atom type: {aType2}");
                                if (!BondManipulator.IsLowerOrder(bond.Order, aType2.MaxBondOrder)
                                 || !BondManipulator.IsLowerOrder(bond.Order, aType1.MaxBondOrder))
                                {
                                    Debug.WriteLine("Bond order not increased: atoms has reached (or exceeded) maximum bond order for this atom type");
                                }
                                else if (BondManipulator.IsLowerOrder(bond.Order, aType2.MaxBondOrder)
                                      && BondManipulator.IsLowerOrder(bond.Order, aType1.MaxBondOrder))
                                {
                                    BondManipulator.IncreaseBondOrder(bond);
                                    Debug.WriteLine($"Bond order now {bond.Order}");
                                    bondOrderIncreased = true;
                                }
                            }
                        }
                    }
                }
            }
            return IsSaturated(bond, atomContainer);
        }

        /// <summary>
        /// Determines if the atom can be of atom type.
        /// </summary>
        private static bool CouldMatchAtomType(IAtomContainer atomContainer, IAtom atom, IAtomType atomType)
        {
            Debug.WriteLine($"   ... matching atom {atom.Symbol} vs {atomType}");
            if (atomContainer.GetBondOrderSum(atom) + atom.ImplicitHydrogenCount < atomType.BondOrderSum)
            {
                Debug.WriteLine("    Match!");
                return true;
            }
            Debug.WriteLine("    No Match");
            return false;
        }

        /// <summary>
        /// The method is known to fail for certain compounds. For more information, see
        /// cdk.test.limitations package.
        /// This method is known to fail, especially on pyrrole-like compounds.
        /// Consider using <see cref="Smiles.DeduceBondSystemTool"/>, which should work better
        /// </summary>
        public void Saturate(IAtomContainer atomContainer)
        {
            for (int i = 1; i < 4; i++)
            {
                // handle atoms with degree 1 first and then proceed to higher order
                for (int f = 0; f < atomContainer.Atoms.Count; f++)
                {
                    var atom = atomContainer.Atoms[f];
                    Debug.WriteLine($"symbol: {atom.Symbol}");
                    var atomTypes1 = structgenATF.GetAtomTypes(atom.Symbol);
                    var atomType1 = atomTypes1.FirstOrDefault();
                    if (atomType1 != null)
                    {
                        Debug.WriteLine($"first atom type: {atomType1}");
                        if (atomContainer.GetConnectedBonds(atom).Count() == i)
                        {
                            var hcount = atom.ImplicitHydrogenCount ?? 0;
                            if (atom.IsAromatic 
                             && atomContainer.GetBondOrderSum(atom) < atomType1.BondOrderSum - hcount)
                            {
                                var partners = atomContainer.GetConnectedAtoms(atom);
                                foreach (var partner in partners)
                                {
                                    Debug.WriteLine($"Atom has {partners.Count()} partners");
                                    var atomType2 = structgenATF.GetAtomTypes(partner.Symbol).FirstOrDefault();
                                    if (atomType2 == null)
                                        return;

                                    hcount = partner.ImplicitHydrogenCount ?? 0;
                                    if (atomContainer.GetBond(partner, atom).IsAromatic
                                     && atomContainer.GetBondOrderSum(partner) < atomType2.BondOrderSum - hcount)
                                    {
                                        Debug.WriteLine($"Partner has {atomContainer.GetBondOrderSum(partner)}, may have: {atomType2.BondOrderSum}");
                                        var bond = atomContainer.GetBond(atom, partner);
                                        Debug.WriteLine($"Bond order was {bond.Order}");
                                        BondManipulator.IncreaseBondOrder(bond);
                                        Debug.WriteLine($"Bond order now {bond.Order}");
                                        break;
                                    }
                                }
                            }

                            var bondOrderSum = atomType1.BondOrderSum ?? 0;
                            var hydrogenCount = atom.ImplicitHydrogenCount ?? 0;
                            var atomContainerBondOrderSum = atomContainer.GetBondOrderSum(atom);

                            if (atomContainerBondOrderSum < bondOrderSum - hydrogenCount)
                            {
                                Debug.WriteLine("Atom has " + atomContainerBondOrderSum + ", may have: " + bondOrderSum);
                                var partners = atomContainer.GetConnectedAtoms(atom);
                                foreach (var partner in partners)
                                {
                                    Debug.WriteLine("Atom has " + partners.Count() + " partners");
                                    var atomType2 = structgenATF.GetAtomTypes(partner.Symbol).FirstOrDefault();
                                    if (atomType2 == null)
                                        return;

                                    var bos2 = atomType2.BondOrderSum ?? 0;
                                    var hc2 = partner.ImplicitHydrogenCount ?? 0;
                                    var acbos2 = atomContainer.GetBondOrderSum(partner);

                                    if (acbos2 < bos2 - hc2)
                                    {
                                        Debug.WriteLine($"Partner has {acbos2}, may have: {bos2}");
                                        var bond = atomContainer.GetBond(atom, partner);
                                        Debug.WriteLine($"Bond order was {bond.Order}");
                                        BondManipulator.IncreaseBondOrder(bond);
                                        Debug.WriteLine($"Bond order now {bond.Order}");
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void SaturateRingSystems(IAtomContainer atomContainer)
        {
            var rs0 = Cycles.FindSSSR(atomContainer.Builder.NewAtomContainer(atomContainer)).ToRingSet();
            var ringSets = RingPartitioner.PartitionRings(rs0);
            IAtom atom = null;
            foreach (var rs in ringSets)
            {
                var containers = RingSetManipulator.GetAllAtomContainers(rs);
                foreach (var ac in containers)
                {
                    var temp = new int[ac.Atoms.Count];
                    for (int g = 0; g < ac.Atoms.Count; g++)
                    {
                        atom = ac.Atoms[g];
                        temp[g] = atom.ImplicitHydrogenCount.Value;
                        atom.ImplicitHydrogenCount = (atomContainer.GetConnectedBonds(atom).Count() - ac.GetConnectedBonds(atom).Count() - temp[g]);
                    }
                    Saturate(ac);
                    for (int g = 0; g < ac.Atoms.Count; g++)
                    {
                        atom = ac.Atoms[g];
                        atom.ImplicitHydrogenCount = temp[g];
                    }
                }
            }
        }

        /// <summary>
        /// Calculate the number of missing hydrogens by subtracting the number of
        /// bonds for the atom from the expected number of bonds. Charges are included
        /// in the calculation. The number of expected bonds is defined by the atom type
        /// generated with the atom type factory.
        /// </summary>
        public int CalculateNumberOfImplicitHydrogens(IAtom atom, IAtomContainer container)
        {
            return this.CalculateNumberOfImplicitHydrogens(atom, container, false);
        }

        public int CalculateNumberOfImplicitHydrogens(IAtom atom)
        {
            var bonds = new List<IBond>();
            return this.CalculateNumberOfImplicitHydrogens(atom, 0, 0, bonds, false);
        }

        public int CalculateNumberOfImplicitHydrogens(IAtom atom, IAtomContainer container, bool throwExceptionForUnknowAtom)
        {
            return this.CalculateNumberOfImplicitHydrogens(atom, container.GetBondOrderSum(atom),
                    container.GetConnectedSingleElectrons(atom).Count(), container.GetConnectedBonds(atom),
                    throwExceptionForUnknowAtom);
        }

        /// <summary>
        /// Calculate the number of missing hydrogens by subtracting the number of
        /// bonds for the atom from the expected number of bonds. Charges are included
        /// in the calculation. The number of expected bonds is defined by the AtomType
        /// generated with the AtomTypeFactory.
        /// </summary>
        /// <param name="throwExceptionForUnknowAtom">Should an exception be thrown if an unknown atomtype is found or 0 returned ?</param>
        /// <seealso cref="AtomTypeFactory"/>
        public int CalculateNumberOfImplicitHydrogens(IAtom atom, double bondOrderSum, double singleElectronSum, IEnumerable<IBond> connectedBonds, bool throwExceptionForUnknowAtom)
        {
            int missingHydrogen = 0;
            if (atom is IPseudoAtom)
            {
                // don't figure it out... it simply does not lack H's
            }
            else if (atom.AtomicNumber == 1)
            {
                missingHydrogen = (int)(1 - bondOrderSum - singleElectronSum - atom.FormalCharge);
            }
            else
            {
                Trace.TraceInformation("Calculating number of missing hydrogen atoms");
                // get default atom
                var defaultAtom = structgenATF.GetAtomTypes(atom.Symbol).FirstOrDefault();
                if (defaultAtom == null && throwExceptionForUnknowAtom)
                    return 0;
                if (defaultAtom != null)
                {
                    Debug.WriteLine($"DefAtom: {defaultAtom}");
                    var formalCharge = atom.FormalCharge ?? 0;
                    var tmpBondOrderSum = defaultAtom.BondOrderSum ?? 0;
                    missingHydrogen = (int)(tmpBondOrderSum - bondOrderSum - singleElectronSum + formalCharge);

                    if (atom.IsAromatic)
                    {
                        bool subtractOne = true;
                        foreach (var conBond in connectedBonds)
                        {
                            if (conBond.Order == BondOrder.Double || conBond.IsAromatic)
                                subtractOne = false;
                        }
                        if (subtractOne)
                            missingHydrogen--;
                    }
                    Debug.WriteLine($"Atom: {atom.Symbol}");
                    Debug.WriteLine($"  max bond order: {tmpBondOrderSum}");
                    Debug.WriteLine($"  bond order sum: {bondOrderSum}");
                    Debug.WriteLine($"  charge        : {formalCharge}");
                }
                else
                {
                    Trace.TraceWarning($"Could not find atom type for {atom.Symbol}");
                }
            }
            return missingHydrogen;
        }
    }
}

/* Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
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
using NCDK.Tools.Manipulator;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NCDK.Tools
{
    /// <summary>
    /// Small customization of <see cref="SmilesValencyChecker"/> suggested by Todd Martin
    /// specially tuned for SMILES parsing.
    /// </summary>
    // @author       Egon Willighagen
    // @cdk.created  2004-06-12
    // @cdk.keyword  atom, valency
    // @cdk.module   valencycheck
    public class SmilesValencyChecker
        : IValencyChecker, IDeduceBondOrderTool
    {
        private readonly AtomTypeFactory structgenATF;

        public SmilesValencyChecker()
        {
            structgenATF = CDK.CdkAtomTypeFactory;
        }

        public SmilesValencyChecker(string atomTypeList)
        {
            structgenATF = AtomTypeFactory.GetInstance(atomTypeList);
            Trace.TraceInformation($"Using configuration file: {atomTypeList}");
        }

        /// <summary>
        /// Saturates a molecule by setting appropriate bond orders.
        /// </summary>
        // @cdk.keyword            bond order, calculation
        // @cdk.created 2003-10-03
        public void Saturate(IAtomContainer atomContainer)
        {
            Trace.TraceInformation("Saturating atomContainer by adjusting bond orders...");
            var allSaturated = IsSaturated(atomContainer);
            if (!allSaturated)
            {
                Trace.TraceInformation("Saturating bond orders is needed...");
                var bonds = new IBond[atomContainer.Bonds.Count];
                for (int i = 0; i < bonds.Length; i++)
                    bonds[i] = atomContainer.Bonds[i];
                var succeeded = Saturate(bonds, atomContainer);
                if (!succeeded)
                {
                    throw new CDKException("Could not saturate this atomContainer!");
                }
            }
        }

        /// <summary>
        /// Saturates a set of Bonds in an AtomContainer.
        /// </summary>
        public bool Saturate(IEnumerable<IBond> bonds, IAtomContainer atomContainer)
        {
            var _bonds = bonds.ToReadOnlyList();
            Debug.WriteLine($"Saturating bond set of size: {_bonds.Count}");
            var bondsAreFullySaturated = false;
            if (_bonds.Count > 0)
            {
                var bond = _bonds[0];

                // determine bonds left
                var leftBondCount = _bonds.Count - 1;
                var leftBonds = _bonds;

                // examine this bond
                Debug.WriteLine($"Examining this bond: {bond}");
                if (IsSaturated(bond, atomContainer))
                {
                    Debug.WriteLine("OK, bond is saturated, now try to saturate remaining bonds (if needed)");
                    bondsAreFullySaturated = Saturate(leftBonds, atomContainer);
                }
                else if (IsUnsaturated(bond, atomContainer))
                {
                    Debug.WriteLine("Ok, this bond is unsaturated, and can be saturated");
                    // two options now:
                    // 1. saturate this one directly
                    // 2. saturate this one by saturating the rest
                    Debug.WriteLine("Option 1: Saturating this bond directly, then trying to saturate rest");
                    // considering organic bonds, the max order is 3, so increase twice
                    var bondOrderIncreased = SaturateByIncreasingBondOrder(bond, atomContainer);
                    bondsAreFullySaturated = bondOrderIncreased && Saturate(_bonds, atomContainer);
                    if (bondsAreFullySaturated)
                    {
                        Debug.WriteLine("Option 1: worked");
                    }
                    else
                    {
                        Debug.WriteLine("Option 1: failed. Trying option 2.");
                        Debug.WriteLine("Option 2: Saturating this bond by saturating the rest");
                        // revert the increase (if succeeded), then saturate the rest
                        if (bondOrderIncreased)
                            UnsaturateByDecreasingBondOrder(bond);
                        bondsAreFullySaturated = Saturate(leftBonds, atomContainer) && IsSaturated(bond, atomContainer);
                        if (!bondsAreFullySaturated)
                            Debug.WriteLine("Option 2: failed");
                    }
                }
                else
                {
                    Debug.WriteLine("Ok, this bond is unsaturated, but cannot be saturated");
                    // try recursing and see if that fixes things
                    bondsAreFullySaturated = Saturate(leftBonds, atomContainer) && IsSaturated(bond, atomContainer);
                }
            }
            else
            {
                bondsAreFullySaturated = true; // empty is saturated by default
            }
            return bondsAreFullySaturated;
        }

        private static bool UnsaturateByDecreasingBondOrder(IBond bond)
        {
            if (bond.Order != BondOrder.Single)
            {
                bond.Order = BondManipulator.DecreaseBondOrder(bond.Order);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns whether a bond is unsaturated. A bond is unsaturated if
        /// <b>all</b> Atoms in the bond are unsaturated.
        /// </summary>
        public bool IsUnsaturated(IBond bond, IAtomContainer atomContainer)
        {
            Debug.WriteLine($"isBondUnsaturated?: {bond}");
            var atoms = BondManipulator.GetAtomArray(bond);
            var isUnsaturated = true;
            for (int i = 0; i < atoms.Length && isUnsaturated; i++)
            {
                isUnsaturated = isUnsaturated && !IsSaturated(atoms[i], atomContainer);
            }
            Debug.WriteLine($"Bond is unsaturated?: {isUnsaturated}");
            return isUnsaturated;
        }

        /// <summary>
        /// Tries to saturate a bond by increasing its bond orders by 1.0.
        /// </summary>
        /// <returns>true if the bond could be increased</returns>
        public bool SaturateByIncreasingBondOrder(IBond bond, IAtomContainer atomContainer)
        {
            var atoms = BondManipulator.GetAtomArray(bond);
            var atom = atoms[0];
            var partner = atoms[1];
            Debug.WriteLine("  saturating bond: ", atom.Symbol, "-", partner.Symbol);
            var atomTypes1 = structgenATF.GetAtomTypes(atom.Symbol);
            var atomTypes2 = structgenATF.GetAtomTypes(partner.Symbol);
            foreach (var aType1 in atomTypes1)
            {
                Debug.WriteLine($"  considering atom type: {aType1}");
                if (CouldMatchAtomType(atomContainer, atom, aType1))
                {
                    Debug.WriteLine($"  trying atom type: {aType1}");
                    foreach (var aType2 in atomTypes2)
                    {
                        Debug.WriteLine($"  considering partner type: {aType1}");
                        if (CouldMatchAtomType(atomContainer, partner, aType2))
                        {
                            Debug.WriteLine($"    with atom type: {aType2}");
                            if (BondManipulator.IsLowerOrder(bond.Order, aType2.MaxBondOrder)
                                    && BondManipulator.IsLowerOrder(bond.Order, aType1.MaxBondOrder))
                            {
                                bond.Order = BondManipulator.IncreaseBondOrder(bond.Order);
                                Debug.WriteLine($"Bond order now {bond.Order}");
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns whether a bond is saturated. A bond is saturated if
        /// <b>both</b> Atoms in the bond are saturated.
        /// </summary>
        public bool IsSaturated(IBond bond, IAtomContainer atomContainer)
        {
            Debug.WriteLine($"isBondSaturated?: {bond}");
            var atoms = BondManipulator.GetAtomArray(bond);
            bool isSaturated = true;
            for (int i = 0; i < atoms.Length; i++)
            {
                Debug.WriteLine($"IsSaturated(Bond, AC): atom I={i}");
                isSaturated = isSaturated && IsSaturated(atoms[i], atomContainer);
            }
            Debug.WriteLine($"IsSaturated(Bond, AC): result={isSaturated}");
            return isSaturated;
        }

        /// <summary>
        /// Check all atoms are saturated.
        /// </summary>
        /// <param name="ac"><see cref="IAtomContainer"/> to check.</param>
        /// <returns><see langword="true"/> if all atoms are saturated.</returns>
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
        /// Determines if the atom can be of atom type. That is, it sees if this
        /// atom type only differs in bond orders, or implicit hydrogen count.
        /// </summary>
        private static bool CouldMatchAtomType(IAtom atom, double bondOrderSum, BondOrder maxBondOrder, IAtomType type)
        {
            Debug.WriteLine($"{nameof(CouldMatchAtomType)}:   ... matching atom {atom} vs {type}");
            var hcount = atom.ImplicitHydrogenCount.Value;
            var charge = atom.FormalCharge.Value;
            if (charge == type.FormalCharge)
            {
                Debug.WriteLine($"{nameof(CouldMatchAtomType)}e:     formal charge matches...");
                if (bondOrderSum + hcount <= type.BondOrderSum)
                {
                    Debug.WriteLine($"{nameof(CouldMatchAtomType)}:     bond order sum is OK...");
                    if (!BondManipulator.IsHigherOrder(maxBondOrder, type.MaxBondOrder))
                    {
                        Debug.WriteLine($"{nameof(CouldMatchAtomType)}:     max bond order is OK... We have a match!");
                        return true;
                    }
                }
                else
                {
                    Debug.WriteLine($"{nameof(CouldMatchAtomType)}:      no match {(bondOrderSum + hcount)} > {type.BondOrderSum}");
                }
            }
            else
            {
                Debug.WriteLine($"{nameof(CouldMatchAtomType)}:     formal charge does NOT match...");
            }
            Debug.WriteLine($"{nameof(CouldMatchAtomType)}e:    No Match");
            return false;
        }

        /// <summary>
        /// Calculates the number of hydrogens that can be added to the given atom to fulfil
        /// the atom's valency. It will return 0 for PseudoAtoms, and for atoms for which it
        /// does not have an entry in the configuration file.
        /// </summary>
        public int CalculateNumberOfImplicitHydrogens(IAtom atom, double bondOrderSum, BondOrder maxBondOrder, int neighbourCount)
        {
            int missingHydrogens = 0;
            if (atom is IPseudoAtom)
            {
                Debug.WriteLine("don't figure it out... it simply does not lack H's");
                return 0;
            }

            Debug.WriteLine("Calculating number of missing hydrogen atoms");
            // get default atom
            var atomTypes = structgenATF.GetAtomTypes(atom.Symbol);
            foreach (var type in atomTypes)
            {
                if (CouldMatchAtomType(atom, bondOrderSum, maxBondOrder, type))
                {
                    Debug.WriteLine($"This type matches: {type}");
                    int formalNeighbourCount = type.FormalNeighbourCount.Value;
                    switch (type.Hybridization)
                    {
                        case Hybridization.Unset:
                            missingHydrogens = (int)(type.BondOrderSum - bondOrderSum);
                            break;
                        case Hybridization.SP3:
                            missingHydrogens = formalNeighbourCount - neighbourCount;
                            break;
                        case Hybridization.SP2:
                            missingHydrogens = formalNeighbourCount - neighbourCount;
                            break;
                        case Hybridization.SP1:
                            missingHydrogens = formalNeighbourCount - neighbourCount;
                            break;
                        default:
                            missingHydrogens = (int)(type.BondOrderSum - bondOrderSum);
                            break;
                    }
                    break;
                }
            }

            Debug.WriteLine($"missing hydrogens: {missingHydrogens}");
            return missingHydrogens;
        }

        /// <summary>
        /// Checks whether an atom is saturated by comparing it with known atom types.
        /// </summary>
        /// <returns><see langword="true"/> if the atom is an pseudo atom and when the element is not in the list.</returns>
        public bool IsSaturated(IAtom atom, IAtomContainer container)
        {
            if (atom is IPseudoAtom)
            {
                Debug.WriteLine("don't figure it out... it simply does not lack H's");
                return true;
            }

            var atomTypes = structgenATF.GetAtomTypes(atom.Symbol);
            if (atomTypes.Any())
            {
                Trace.TraceWarning($"Missing entry in atom type list for {atom.Symbol}");
                return true;
            }
            var bondOrderSum = container.GetBondOrderSum(atom);
            var maxBondOrder = container.GetMaximumBondOrder(atom);
            var hcount = atom.ImplicitHydrogenCount.Value;
            var charge = atom.FormalCharge.Value;

            Debug.WriteLine($"Checking saturation of atom {atom.Symbol}");
            Debug.WriteLine($"bondOrderSum: {bondOrderSum}");
            Debug.WriteLine($"maxBondOrder: {maxBondOrder}");
            Debug.WriteLine($"hcount: {hcount}");
            Debug.WriteLine($"charge: {charge}");

            bool elementPlusChargeMatches = false;
            foreach (var type in atomTypes)
            {
                if (CouldMatchAtomType(atom, bondOrderSum, maxBondOrder, type))
                {
                    if (bondOrderSum + hcount == type.BondOrderSum
                            && !BondManipulator.IsHigherOrder(maxBondOrder, type.MaxBondOrder))
                    {
                        Debug.WriteLine($"We have a match: {type}");
                        Debug.WriteLine($"Atom is saturated: {atom.Symbol}");
                        return true;
                    }
                    else
                    {
                        // ok, the element and charge matche, but unfulfilled
                        elementPlusChargeMatches = true;
                    }
                } // else: formal charges don't match
            }

            if (elementPlusChargeMatches)
            {
                Debug.WriteLine("No, atom is not saturated.");
                return false;
            }

            // ok, the found atom was not in the list
            Trace.TraceError("Could not find atom type!");
            throw new CDKException($"The atom with element {atom.Symbol} and charge {charge} is not found.");
        }

        public int CalculateNumberOfImplicitHydrogens(IAtom atom, IAtomContainer container)
        {
            return this.CalculateNumberOfImplicitHydrogens(atom, container.GetBondOrderSum(atom), container.GetMaximumBondOrder(atom), container.GetConnectedBonds(atom).Count());
        }

        /// <summary>
        /// Determines if the atom can be of type <see cref="IAtomType"/>. That is, it sees if this
        /// <see cref="IAtomType"/> only differs in bond orders, or implicit hydrogen count.
        /// </summary>
        private static bool CouldMatchAtomType(IAtomContainer container, IAtom atom, IAtomType type)
        {
            var bondOrderSum = container.GetBondOrderSum(atom);
            var maxBondOrder = container.GetMaximumBondOrder(atom);
            return CouldMatchAtomType(atom, bondOrderSum, maxBondOrder, type);
        }
    }
}

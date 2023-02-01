/* Copyright (C) 2008  Miguel Rojas <miguelrojasch@yahoo.es>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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

using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;

namespace NCDK.Reactions.Mechanisms
{
    /// <summary>
    /// <para>This mechanism adduct together two fragments due to a double bond.
    /// The second fragment will be deficient in charge.
    /// It returns the reaction mechanism which has been cloned the <see cref="IAtomContainer"/>.</para>
    /// <para>This reaction could be represented as A=B + [C+] ==&gt; [A+]-B-C</para>
    /// </summary>
    // @author         miguelrojasch
    // @cdk.created    2008-02-10
    // @cdk.module     reaction
    public class AdductionPBMechanism : IReactionMechanism
    {
        /// <summary>
        /// Initiates the process for the given mechanism. The atoms and bonds to apply are mapped between
        /// reactants and products.
        /// </summary>
        /// <param name="atomContainerSet"></param>
        /// <param name="atomList">The list of atoms taking part in the mechanism. Only allowed three atoms</param>
        /// <param name="bondList">The list of bonds taking part in the mechanism. Only allowed one bond</param>
        /// <returns>The Reaction mechanism</returns>
        public IReaction Initiate(IChemObjectSet<IAtomContainer> atomContainerSet, IList<IAtom> atomList, IList<IBond> bondList)
        {
            var atMatcher = CDK.AtomTypeMatcher;
            if (atomContainerSet.Count != 2)
            {
                throw new CDKException("AdductionPBMechanism expects two IAtomContainer's");
            }
            if (atomList.Count != 3)
            {
                throw new CDKException("AdductionPBMechanism expects two atoms in the List");
            }
            if (bondList.Count != 1)
            {
                throw new CDKException("AdductionPBMechanism don't expect bonds in the List");
            }
            IAtomContainer molecule1 = atomContainerSet[0];
            IAtomContainer molecule2 = atomContainerSet[1];

            IAtomContainer reactantCloned;
            reactantCloned = (IAtomContainer)atomContainerSet[0].Clone();
            reactantCloned.Add((IAtomContainer)atomContainerSet[1].Clone());
            IAtom atom1 = atomList[0];// Atom 1: to be deficient in charge
            IAtom atom1C = reactantCloned.Atoms[molecule1.Atoms.IndexOf(atom1)];
            IAtom atom2 = atomList[1];// Atom 2: receive the adduct
            IAtom atom2C = reactantCloned.Atoms[molecule1.Atoms.IndexOf(atom2)];
            IAtom atom3 = atomList[2];// Atom 2: deficient in charge
            IAtom atom3C = reactantCloned.Atoms[molecule1.Atoms.Count + molecule2.Atoms.IndexOf(atom3)];
            IBond bond1 = bondList[0];
            int posBond1 = atomContainerSet[0].Bonds.IndexOf(bond1);

            BondManipulator.DecreaseBondOrder(reactantCloned.Bonds[posBond1]);
            IBond newBond = molecule1.Builder.NewBond(atom2C, atom3C, BondOrder.Single);
            reactantCloned.Bonds.Add(newBond);

            int charge = atom1C.FormalCharge.Value;
            atom1C.FormalCharge = charge + 1;
            atom1C.Hybridization = Hybridization.Unset;
            AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(reactantCloned);
            IAtomType type = atMatcher.FindMatchingAtomType(reactantCloned, atom1C);
            if (type == null || type.AtomTypeName.Equals("X", StringComparison.Ordinal))
                return null;

            atom2C.Hybridization = Hybridization.Unset;
            AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(reactantCloned);
            type = atMatcher.FindMatchingAtomType(reactantCloned, atom2C);
            if (type == null || type.AtomTypeName.Equals("X", StringComparison.Ordinal))
                return null;

            charge = atom3C.FormalCharge.Value;
            atom3C.FormalCharge = charge - 1;
            atom3C.Hybridization = Hybridization.Unset;
            AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(reactantCloned);
            type = atMatcher.FindMatchingAtomType(reactantCloned, atom3C);
            if (type == null || type.AtomTypeName.Equals("X", StringComparison.Ordinal))
                return null;

            IReaction reaction = atom1C.Builder.NewReaction();
            reaction.Reactants.Add(molecule1);

            /* mapping */
            foreach (var atom in molecule1.Atoms)
            {
                IMapping mapping = atom1C.Builder.NewMapping(atom,
                        reactantCloned.Atoms[molecule1.Atoms.IndexOf(atom)]);
                reaction.Mappings.Add(mapping);
            }
            foreach (var atom in molecule2.Atoms)
            {
                IMapping mapping = atom1C.Builder.NewMapping(atom,
                        reactantCloned.Atoms[molecule2.Atoms.IndexOf(atom)]);
                reaction.Mappings.Add(mapping);
            }

            reaction.Products.Add(reactantCloned);

            return reaction;
        }
    }
}

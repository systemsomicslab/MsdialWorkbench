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
    /// <para>This mechanism produces the tautomerization chemical reaction between two tautomers.
    /// It returns the reaction mechanism which has been cloned the <see cref="IAtomContainer"/>.</para>
    /// <para>This reaction could be represented as X=Y-Z-H =&gt; X(H)-Y=Z</para>
    /// </summary>
    // @author         miguelrojasch
    // @cdk.created    2008-02-10
    // @cdk.module     reaction
    public class TautomerizationMechanism : IReactionMechanism
    {
        /// <summary>
        /// Initiates the process for the given mechanism. The atoms and bonds to apply are mapped between
        /// reactants and products.
        /// </summary>
        /// <param name="atomContainerSet"></param>
        /// <param name="atomList">The list of atoms taking part in the mechanism. Only allowed fourth atoms.</param>
        /// <param name="bondList">The list of bonds taking part in the mechanism. Only allowed two bond.
        ///     The first bond is the bond to decrease the order and the second is the bond to increase the order.
        ///     It is the bond which is moved</param>
        /// <returns>The Reaction mechanism</returns>
        public IReaction Initiate(IChemObjectSet<IAtomContainer> atomContainerSet, IList<IAtom> atomList, IList<IBond> bondList)
        {
            var atMatcher = CDK.AtomTypeMatcher;
            if (atomContainerSet.Count != 1)
            {
                throw new CDKException("TautomerizationMechanism only expects one IAtomContainer");
            }
            if (atomList.Count != 4)
            {
                throw new CDKException("TautomerizationMechanism expects four atoms in the List");
            }
            if (bondList.Count != 3)
            {
                throw new CDKException("TautomerizationMechanism expects three bonds in the List");
            }
            var molecule = atomContainerSet[0];
            IAtomContainer reactantCloned;
            reactantCloned = (IAtomContainer)molecule.Clone();
            IAtom atom1 = atomList[0];// Atom to be added the hydrogen
            IAtom atom1C = reactantCloned.Atoms[molecule.Atoms.IndexOf(atom1)];
            IAtom atom2 = atomList[1];// Atom 2
            IAtom atom2C = reactantCloned.Atoms[molecule.Atoms.IndexOf(atom2)];
            IAtom atom3 = atomList[2];// Atom 3
            IAtom atom3C = reactantCloned.Atoms[molecule.Atoms.IndexOf(atom3)];
            IAtom atom4 = atomList[3];// hydrogen Atom
            IAtom atom4C = reactantCloned.Atoms[molecule.Atoms.IndexOf(atom4)];
            IBond bond1 = bondList[0];// Bond with double bond
            int posBond1 = molecule.Bonds.IndexOf(bond1);
            IBond bond2 = bondList[1];// Bond with single bond
            int posBond2 = molecule.Bonds.IndexOf(bond2);
            IBond bond3 = bondList[2];// Bond to be removed
            int posBond3 = molecule.Bonds.IndexOf(bond3);

            BondManipulator.DecreaseBondOrder(reactantCloned.Bonds[posBond1]);
            BondManipulator.IncreaseBondOrder(reactantCloned.Bonds[posBond2]);
            reactantCloned.Bonds.Remove(reactantCloned.Bonds[posBond3]);
            IBond newBond = molecule.Builder.NewBond(atom1C, atom4C, BondOrder.Single);
            reactantCloned.Bonds.Add(newBond);

            atom1C.Hybridization = Hybridization.Unset;
            AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(reactantCloned);
            IAtomType type = atMatcher.FindMatchingAtomType(reactantCloned, atom1C);
            if (type == null || type.AtomTypeName.Equals("X", StringComparison.Ordinal))
                return null;

            atom3C.Hybridization = Hybridization.Unset;
            AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(reactantCloned);
            type = atMatcher.FindMatchingAtomType(reactantCloned, atom3C);
            if (type == null || type.AtomTypeName.Equals("X", StringComparison.Ordinal))
                return null;

            IReaction reaction = atom2C.Builder.NewReaction();
            reaction.Reactants.Add(molecule);

            /* mapping */
            foreach (var atom in molecule.Atoms)
            {
                IMapping mapping = atom2C.Builder.NewMapping(atom,
                        reactantCloned.Atoms[molecule.Atoms.IndexOf(atom)]);
                reaction.Mappings.Add(mapping);
            }

            reaction.Products.Add(reactantCloned);

            return reaction;
        }
    }
}

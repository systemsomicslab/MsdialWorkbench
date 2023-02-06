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

using NCDK.Graphs;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;

namespace NCDK.Reactions.Mechanisms
{
    /// <summary>
    /// This mechanism breaks the chemical bond between atoms. Generating two atoms with
    /// attached radicals.
    /// It returns the reaction mechanism which has been cloned the <see cref="IAtomContainer"/>.
    /// </summary>
    // @author         miguelrojasch
    // @cdk.created    2008-02-10
    // @cdk.module     reaction
    public class HomolyticCleavageMechanism : IReactionMechanism
    {
        /// <summary>
        /// Initiates the process for the given mechanism. The atoms to apply are mapped between
        /// reactants and products.
        /// </summary>
        /// <param name="atomContainerSet"></param>
        /// <param name="atomList">The list of atoms taking part in the mechanism. Only allowed two atoms. Both atoms acquire a ISingleElectron</param>
        /// <param name="bondList">The list of bonds taking part in the mechanism. Only allowed one bond</param>
        /// <returns>The Reaction mechanism</returns>
        public IReaction Initiate(IChemObjectSet<IAtomContainer> atomContainerSet, IList<IAtom> atomList, IList<IBond> bondList)
        {
            var atMatcher = CDK.AtomTypeMatcher;
            if (atomContainerSet.Count != 1)
            {
                throw new CDKException("TautomerizationMechanism only expects one IAtomContainer");
            }
            if (atomList.Count != 2)
            {
                throw new CDKException("HomolyticCleavageMechanism expects two atoms in the List");
            }
            if (bondList.Count != 1)
            {
                throw new CDKException("HomolyticCleavageMechanism only expect one bond in the List");
            }
            IAtomContainer molecule = atomContainerSet[0];
            IAtomContainer reactantCloned;
            reactantCloned = (IAtomContainer)molecule.Clone();
            IAtom atom1 = atomList[0];
            IAtom atom1C = reactantCloned.Atoms[molecule.Atoms.IndexOf(atom1)];
            IAtom atom2 = atomList[1];
            IAtom atom2C = reactantCloned.Atoms[molecule.Atoms.IndexOf(atom2)];
            IBond bond1 = bondList[0];
            int posBond1 = molecule.Bonds.IndexOf(bond1);

            if (bond1.Order == BondOrder.Single)
                reactantCloned.Bonds.Remove(reactantCloned.Bonds[posBond1]);
            else
                BondManipulator.DecreaseBondOrder(reactantCloned.Bonds[posBond1]);

            reactantCloned.SingleElectrons.Add(bond1.Builder.NewSingleElectron(atom1C));
            reactantCloned.SingleElectrons.Add(bond1.Builder.NewSingleElectron(atom2C));
            AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(reactantCloned);

            // check if resulting atom type is reasonable
            atom1C.Hybridization = Hybridization.Unset;
            IAtomType type = atMatcher.FindMatchingAtomType(reactantCloned, atom1C);
            if (type == null || type.AtomTypeName.Equals("X", StringComparison.Ordinal))
                return null;

            // check if resulting atom type is reasonable: an acceptor atom cannot be charged positive*/
            atom2C.Hybridization = Hybridization.Unset;
            type = atMatcher.FindMatchingAtomType(reactantCloned, atom2C);
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
            if (bond1.Order != BondOrder.Single)
            {
                reaction.Products.Add(reactantCloned);
            }
            else
            {
                var moleculeSetP = ConnectivityChecker.PartitionIntoMolecules(reactantCloned);
                foreach (var moleculeP in moleculeSetP)
                {
                    reaction.Products.Add(moleculeP);
                }
            }

            return reaction;
        }
    }
}

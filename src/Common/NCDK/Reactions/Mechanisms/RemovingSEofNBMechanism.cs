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

using NCDK.AtomTypes;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Reactions.Mechanisms
{
    /// <summary>
    /// This mechanism extracts a single electron from a non-bonding orbital which located in
    /// a ILonePair container. It returns the reaction mechanism which has been cloned the
    /// <see cref="IAtomContainer"/> with an ILonPair electron less and an ISingleElectron more.
    /// </summary>
    // @author         miguelrojasch
    // @cdk.created    2008-02-10
    // @cdk.module     reaction
    public class RemovingSEofNBMechanism : IReactionMechanism
    {      
         /// <summary>
         /// Initiates the process for the given mechanism. The atoms to apply are mapped between reactants and products.
         /// </summary>
         /// <param name="atomContainerSet"></param>
         /// <param name="atomList">The list of atoms taking part in the mechanism. Only allowed one atom</param>
         /// <param name="bondList">The list of bonds taking part in the mechanism. Only allowed one Bond</param>
         /// <returns>The Reaction mechanism</returns>
        public IReaction Initiate(IChemObjectSet<IAtomContainer> atomContainerSet, IList<IAtom> atomList, IList<IBond> bondList)
        {
            var atMatcher = CDK.AtomTypeMatcher;
            if (atomContainerSet.Count != 1)
            {
                throw new CDKException("RemovingSEofNBMechanism only expects one IAtomContainer");
            }
            if (atomList.Count != 1)
            {
                throw new CDKException("RemovingSEofNBMechanism only expects one atom in the List");
            }
            if (bondList != null)
            {
                throw new CDKException("RemovingSEofNBMechanism don't expect any bond in the List");
            }
            var molecule = atomContainerSet[0];
            var reactantCloned = (IAtomContainer)molecule.Clone();

            // remove one lone pair electron and substitute with one single electron and charge 1.
            int posAtom = molecule.Atoms.IndexOf(atomList[0]);
            var lps = reactantCloned.GetConnectedLonePairs(reactantCloned.Atoms[posAtom]);
            reactantCloned.LonePairs.Remove(lps.Last());

            reactantCloned.SingleElectrons.Add(molecule.Builder.NewSingleElectron(reactantCloned.Atoms[posAtom]));
            int charge = reactantCloned.Atoms[posAtom].FormalCharge.Value;
            reactantCloned.Atoms[posAtom].FormalCharge = charge + 1;

            // check if resulting atom type is reasonable
            reactantCloned.Atoms[posAtom].Hybridization = Hybridization.Unset;
            AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(reactantCloned);
            var type = atMatcher.FindMatchingAtomType(reactantCloned, reactantCloned.Atoms[posAtom]);
            if (type == null || type.AtomTypeName.Equals("X", StringComparison.Ordinal))
                return null;

            var reaction = molecule.Builder.NewReaction();
            reaction.Reactants.Add(molecule);

            /* mapping */
            foreach (var atom in molecule.Atoms)
            {
                var mapping = molecule.Builder.NewMapping(atom, reactantCloned.Atoms[molecule.Atoms.IndexOf(atom)]);
                reaction.Mappings.Add(mapping);
            }
            reaction.Products.Add(reactantCloned);

            return reaction;
        }
    }
}

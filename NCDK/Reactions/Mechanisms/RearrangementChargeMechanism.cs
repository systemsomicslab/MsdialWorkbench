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
using System.Linq;

namespace NCDK.Reactions.Mechanisms
{
    /// <summary>
    /// <para>This mechanism displaces the Charge(radical, charge + or charge -) because of
    /// a double bond which is associated.
    /// It returns the reaction mechanism which has been cloned the <see cref="IAtomContainer"/>.</para>
    /// <para>This reaction could be represented as [A*]-Y=Z =&gt; A=Z-[Y*]</para>
    /// </summary>
    // @author         miguelrojasch
    // @cdk.created    2008-02-10
    // @cdk.module     reaction
    public class RearrangementChargeMechanism : IReactionMechanism
    {

        /// <summary>
        /// Initiates the process for the given mechanism. The atoms to apply are mapped between
        /// reactants and products.
        /// </summary>
        /// <param name="atomContainerSet"></param>
        /// <param name="atomList">The list of atoms taking part in the mechanism. Only allowed two three.
        ///                    The first atom is the atom which must contain the charge to be moved, the second
        ///                    is the atom which is in the middle and the third is the atom which acquires the new charge
        /// <param name="bondList">The list of bonds taking part in the mechanism. Only allowed two bond.</param>
        ///                       The first bond is the bond to increase the order and the second is the bond
        ///                       to decrease the order
        ///                       It is the bond which is moved</param>
        /// <returns>The Reaction mechanism</returns>
        public IReaction Initiate(IChemObjectSet<IAtomContainer> atomContainerSet, IList<IAtom> atomList, IList<IBond> bondList)
        {
            var atMatcher = CDK.AtomTypeMatcher;
            if (atomContainerSet.Count != 1)
            {
                throw new CDKException("RearrangementChargeMechanism only expects one IAtomContainer");
            }
            if (atomList.Count != 3)
            {
                throw new CDKException("RearrangementChargeMechanism expects three atoms in the List");
            }
            if (bondList.Count != 2)
            {
                throw new CDKException("RearrangementChargeMechanism only expect one bond in the List");
            }
            IAtomContainer molecule = atomContainerSet[0];
            IAtomContainer reactantCloned;
            reactantCloned = (IAtomContainer)molecule.Clone();
            IAtom atom1 = atomList[0];// Atom with the charge
            IAtom atom1C = reactantCloned.Atoms[molecule.Atoms.IndexOf(atom1)];
            IAtom atom3 = atomList[2];// Atom which acquires the charge
            IAtom atom3C = reactantCloned.Atoms[molecule.Atoms.IndexOf(atom3)];
            IBond bond1 = bondList[0];// Bond with single bond
            int posBond1 = molecule.Bonds.IndexOf(bond1);
            IBond bond2 = bondList[1];// Bond with double bond
            int posBond2 = molecule.Bonds.IndexOf(bond2);

            BondManipulator.IncreaseBondOrder(reactantCloned.Bonds[posBond1]);
            if (bond2.Order == BondOrder.Single)
                reactantCloned.Bonds.Remove(reactantCloned.Bonds[posBond2]);
            else
                BondManipulator.DecreaseBondOrder(reactantCloned.Bonds[posBond2]);

            //Depending of the charge moving (radical, + or -) there is a different situation
            if (reactantCloned.GetConnectedSingleElectrons(atom1C).Any())
            {
                var selectron = reactantCloned.GetConnectedSingleElectrons(atom1C);
                reactantCloned.SingleElectrons.Remove(selectron.Last());

                reactantCloned.SingleElectrons.Add(bond2.Builder.NewSingleElectron(atom3C));

            }
            else if (atom1C.FormalCharge > 0)
            {
                int charge = atom1C.FormalCharge.Value;
                atom1C.FormalCharge = charge - 1;

                charge = atom3C.FormalCharge.Value;
                atom3C.FormalCharge = charge + 1;

            }
            else if (atom1C.FormalCharge < 1)
            {
                int charge = atom1C.FormalCharge.Value;
                atom1C.FormalCharge = charge + 1;
                var ln = reactantCloned.GetConnectedLonePairs(atom1C);
                reactantCloned.LonePairs.Remove(ln.Last());
                atom1C.IsAromatic = false;

                charge = atom3C.FormalCharge.Value;
                atom3C.FormalCharge = charge - 1;
                reactantCloned.LonePairs.Add(bond2.Builder.NewLonePair(atom3C));
                atom3C.IsAromatic = false;
            }
            else
                return null;

            atom1C.Hybridization = Hybridization.Unset;
            atom3C.Hybridization = Hybridization.Unset;
            AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(reactantCloned);

            IAtomType type = atMatcher.FindMatchingAtomType(reactantCloned, atom1C);
            if (type == null || type.AtomTypeName.Equals("X", StringComparison.Ordinal))
                return null;

            type = atMatcher.FindMatchingAtomType(reactantCloned, atom3C);
            if (type == null || type.AtomTypeName.Equals("X", StringComparison.Ordinal))
                return null;

            IReaction reaction = bond2.Builder.NewReaction();
            reaction.Reactants.Add(molecule);

            /* mapping */
            foreach (var atom in molecule.Atoms)
            {
                IMapping mapping = bond2.Builder.NewMapping(atom,
                        reactantCloned.Atoms[molecule.Atoms.IndexOf(atom)]);
                reaction.Mappings.Add(mapping);
            }
            if (bond2.Order != BondOrder.Single)
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

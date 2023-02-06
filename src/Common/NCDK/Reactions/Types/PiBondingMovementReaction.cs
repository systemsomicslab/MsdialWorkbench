/* Copyright (C) 2008 Miguel Rojas <miguelrojasch@users.sf.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
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

using NCDK.Reactions.Types.Parameters;
using NCDK.RingSearches;
using NCDK.Tools.Manipulator;

namespace NCDK.Reactions.Types
{
    /// <summary>
    /// IReactionProcess which tries to reproduce the delocalization of electrons
    /// which are unsaturated bonds from conjugated rings. Only is allowed those
    /// movements which produces from neutral to neutral structures and not take account the possible
    /// movements influenced from lone pairs, or empty orbitals. This movements are
    /// typically from rings without any access or deficiency of charge and have a
    /// even number of atoms. 
    /// <para>The reaction don't care if the product are the same in symmetry.</para>
    /// </summary>
    // @author         Miguel Rojas
    // @cdk.created    2007-02-02
    // @cdk.module     reaction
    public partial class PiBondingMovementReaction : ReactionEngine, IReactionProcess
    {
        /// <summary>
        /// Constructor of the PiBondingMovementReaction object
        /// </summary>
        public PiBondingMovementReaction() { }

        /// <summary>
        ///  The specification attribute of the PiBondingMovementReaction object
        /// </summary>
        public ReactionSpecification Specification =>
            new ReactionSpecification(
                "http://almost.cubic.uni-koeln.de/jrg/Members/mrc/reactionDict/reactionDict#PiBondingMovement", 
                this.GetType().Name, "$Id$", "The Chemistry Development Kit");

        /// <summary>
        /// Initiate process.
        /// It is needed to call the addExplicitHydrogensToSatisfyValency
        /// from the class tools.HydrogenAdder.
        /// </summary>
        /// <exception cref="CDKException"> Description of the Exception</exception>
        /// <param name="reactants">reactants of the reaction.</param>
        /// <param name="agents">agents of the reaction (Must be in this case null).</param>
        public IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents)
        {
            CheckInitiateParams(reactants, agents);

            var setOfReactions = reactants.Builder.NewReactionSet();
            var reactant = reactants[0];

            AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(reactant);

            // if the parameter hasActiveCenter is not fixed yet, set the active centers
            var ipr = base.GetParameterClass(typeof(SetReactionCenter));
            if (ipr != null && !ipr.IsSetParameter) SetActiveCenters(reactant);

            var arf = new AllRingsFinder();
            var ringSet = arf.FindAllRings(reactant);
            for (int ir = 0; ir < ringSet.Count; ir++)
            {
                var ring = ringSet[ir];

                //only rings with even number of atoms
                int nrAtoms = ring.Atoms.Count;
                if (nrAtoms % 2 == 0)
                {
                    int nrSingleBonds = 0;
                    foreach (var bond in ring.Bonds)
                    {
                        if (bond.Order == BondOrder.Single)
                            nrSingleBonds++;
                    }
                    //if exactly half (nrAtoms/2==nrSingleBonds)
                    if (nrSingleBonds != 0 && nrAtoms / 2 == nrSingleBonds)
                    {
                        bool ringCompletActive = false;
                        foreach (var bond in ring.Bonds)
                        {
                            if (bond.IsReactiveCenter)
                                ringCompletActive = true;
                            else
                            {
                                ringCompletActive = false;
                                break;
                            }
                        }
                        if (!ringCompletActive)
                            continue;

                        var reaction = reactants.Builder.NewReaction();
                        reaction.Reactants.Add(reactant);

                        var reactantCloned = (IAtomContainer)reactant.Clone();

                        foreach (var bondi in ring.Bonds)
                        {
                            int bondiP = reactant.Bonds.IndexOf(bondi);
                            if (bondi.Order == BondOrder.Single)
                                BondManipulator.IncreaseBondOrder(reactantCloned.Bonds[bondiP]);
                            else
                                BondManipulator.DecreaseBondOrder(reactantCloned.Bonds[bondiP]);
                        }

                        reaction.Products.Add(reactantCloned);
                        setOfReactions.Add(reaction);
                    }
                }
            }

            return setOfReactions;
        }

        /// <summary>
        /// Set the active center for this molecule.
        /// The active center will be those which correspond to a ring
        /// with pi electrons with resonance.
        /// </summary>
        /// FIXME REACT: It could be possible that a ring is a super ring of others small rings
        /// <param name="reactant">The molecule to set the activity</param>
        private static void SetActiveCenters(IAtomContainer reactant)
        {
            var arf = new AllRingsFinder();
            var ringSet = arf.FindAllRings(reactant);
            for (int ir = 0; ir < ringSet.Count; ir++)
            {
                var ring = ringSet[ir];
                //only rings with even number of atoms
                int nrAtoms = ring.Atoms.Count;
                if (nrAtoms % 2 == 0)
                {
                    int nrSingleBonds = 0;
                    foreach (var bond in ring.Bonds)
                    {
                        if (bond.Order == BondOrder.Single)
                            nrSingleBonds++;
                    }
                    //if exactly half (nrAtoms/2==nrSingleBonds)
                    if (nrSingleBonds != 0 && nrAtoms / 2 == nrSingleBonds)
                    {
                        foreach (var bond in ring.Bonds)
                        {
                            bond.IsReactiveCenter = true;
                        }
                    }
                }
            }
        }
    }
}

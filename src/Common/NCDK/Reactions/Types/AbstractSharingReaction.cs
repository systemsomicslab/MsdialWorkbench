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
using System.Linq;

namespace NCDK.Reactions.Types
{
    // @author         Miguel Rojas
    // @cdk.created    2006-05-05
    // @cdk.module     reaction
    public abstract class AbstractSharingReaction : ReactionEngine, IReactionProcess
    {
        /// <inheritdoc/>
        public abstract ReactionSpecification Specification { get; }

        /// <inheritdoc/>
        public abstract IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents);

        internal delegate bool CheckReactantAtom(IAtomContainer mol, IAtom atom);
        internal delegate bool CheckAtom(IAtom atom);
        internal delegate bool CheckBond(IBond bond);

        internal IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents, bool isReverse, CheckReactantAtom checkReactantAtom, CheckAtom checkAtom, CheckBond checkBond)
        {
            CheckInitiateParams(reactants, agents);

            IReactionSet setOfReactions = reactants.Builder.NewReactionSet();
            IAtomContainer reactant = reactants[0];

            // if the parameter hasActiveCenter is not fixed yet, set the active centers
            var ipr = base.GetParameterClass(typeof(SetReactionCenter));
            if (ipr != null && !ipr.IsSetParameter) SetActiveCenters(reactant, checkReactantAtom, checkAtom, checkBond);

            foreach (var atomi in reactant.Atoms)
            {
                if (atomi.IsReactiveCenter && checkReactantAtom(reactant, atomi))
                {
                    foreach (var bondi in reactant.GetConnectedBonds(atomi))
                    {
                        if (bondi.IsReactiveCenter && checkBond(bondi))
                        {
                            IAtom atomj = bondi.GetOther(atomi);
                            if (atomj.IsReactiveCenter && checkAtom(atomj) && !reactant.GetConnectedSingleElectrons(atomj).Any())
                            {
                                IAtom[] atomList;
                                if (isReverse)
                                    atomList = new[] { atomj, atomi };
                                else
                                    atomList = new[] { atomi, atomj };
                                var bondList = new[] { bondi };

                                var moleculeSet = reactant.Builder.NewChemObjectSet<IAtomContainer>();

                                moleculeSet.Add(reactant);
                                var reaction = Mechanism.Initiate(moleculeSet, atomList, bondList);
                                if (reaction == null)
                                    continue;
                                else
                                    setOfReactions.Add(reaction);
                            }
                        }
                    }
                }
            }
            return setOfReactions;
        }
        
        private static void SetActiveCenters(IAtomContainer reactant, CheckReactantAtom checkReactantAtom, CheckAtom checkAtom, CheckBond checkBond)
        {
            foreach (var atomi in reactant.Atoms)
            {
                if (checkReactantAtom(reactant, atomi))
                {
                    foreach (var bondi in reactant.GetConnectedBonds(atomi))
                    {
                        if (checkBond(bondi))
                        {
                            IAtom atomj = bondi.GetOther(atomi);
                            if (checkAtom(atomj) && !reactant.GetConnectedSingleElectrons(atomj).Any())
                            {
                                atomi.IsReactiveCenter = true;
                                atomj.IsReactiveCenter = true;
                                bondi.IsReactiveCenter = true;
                            }
                        }
                    }
                }
            }
        }
    }
}

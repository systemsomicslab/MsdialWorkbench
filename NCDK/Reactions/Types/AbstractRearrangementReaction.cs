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
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Reactions.Types
{
    // @author         Miguel Rojas
    // @cdk.created    2006-05-05
    // @cdk.module     reaction
    public abstract class AbstractRearrangementReaction : ReactionEngine, IReactionProcess
    {
        /// <inheritdoc/>
        public abstract ReactionSpecification Specification { get; }

        /// <inheritdoc/>
        public abstract IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents);

        internal delegate bool CheckReactant(IAtomContainer reactant);
        internal delegate bool CheckReactantAtom(IAtomContainer reactant, IAtom atom);
        internal delegate bool CheckAtom(IAtom atom);
        
        internal IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents, CheckReactant checkReactant, CheckReactantAtom checkReatantAtom, CheckAtom checkAtom)
        {
            CheckInitiateParams(reactants, agents);

            IReactionSet setOfReactions = reactants.Builder.NewReactionSet();
            IAtomContainer reactant = reactants[0];

            // if the parameter hasActiveCenter is not fixed yet, set the active centers
            var ipr = base.GetParameterClass(typeof(SetReactionCenter));
            if (ipr != null && !ipr.IsSetParameter) SetActiveCenters(reactant, checkReactant, checkReatantAtom, checkAtom);

            foreach (var atomi in reactant.Atoms)
            {
                if (atomi.IsReactiveCenter && checkReatantAtom(reactant, atomi))
                {
                    foreach (var bondi in reactant.GetConnectedBonds(atomi))
                    {
                        if (bondi.IsReactiveCenter && bondi.Order == BondOrder.Single)
                        {
                            IAtom atomj = bondi.GetOther(atomi);
                            if (atomj.IsReactiveCenter
                                    && (atomj.FormalCharge ?? 0) == 0
                                    && !reactant.GetConnectedSingleElectrons(atomj).Any())
                            {
                                foreach (var bondj in reactant.GetConnectedBonds(atomj))
                                {
                                    if (bondj.Equals(bondi)) continue;

                                    if (bondj.IsReactiveCenter
                                            && bondj.Order == BondOrder.Double)
                                    {
                                        IAtom atomk = bondj.GetOther(atomj);
                                        if (atomk.IsReactiveCenter
                                                && checkAtom(atomk)
                                                && !reactant.GetConnectedSingleElectrons(atomk).Any())
                                        {
                                            var atomList = new List<IAtom>
                                            {
                                                atomi,
                                                atomj,
                                                atomk
                                            };
                                            var bondList = new List<IBond>
                                            {
                                                bondi,
                                                bondj
                                            };

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
                    }
                }
            }
            return setOfReactions;
        }
        
        private static void SetActiveCenters(IAtomContainer reactant, CheckReactant checkReatant, CheckReactantAtom checkReatantAtom, CheckAtom checkAtom)
        {
            if (checkReatant != null && !checkReatant(reactant)) return;

            foreach (var atomi in reactant.Atoms)
            {
                if (checkReatantAtom(reactant, atomi))
                {
                    foreach (var bondi in reactant.GetConnectedBonds(atomi))
                    {
                        if (bondi.Order == BondOrder.Single)
                        {
                            IAtom atomj = bondi.GetOther(atomi);
                            if ((atomj.FormalCharge ?? 0) == 0
                                    && !reactant.GetConnectedSingleElectrons(atomj).Any())
                            {
                                foreach (var bondj in reactant.GetConnectedBonds(atomj))
                                {
                                    if (bondj.Equals(bondi)) continue;

                                    if (bondj.Order == BondOrder.Double)
                                    {
                                        IAtom atomk = bondj.GetOther(atomj);
                                        if (checkAtom(atomk)
                                            && !reactant.GetConnectedSingleElectrons(atomk).Any())
                                        {
                                            atomi.IsReactiveCenter = true;
                                            atomj.IsReactiveCenter = true;
                                            atomk.IsReactiveCenter = true;
                                            bondi.IsReactiveCenter = true;
                                            bondj.IsReactiveCenter = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

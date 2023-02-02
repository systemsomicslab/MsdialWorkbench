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
    // @cdk.created    2006-04-01
    // @cdk.module     reaction
    public abstract class AbstractElectronImpactDBReaction : ReactionEngine, IReactionProcess
    {
        /// <inheritdoc/>
        public abstract ReactionSpecification Specification { get; }

        /// <inheritdoc/>
        public abstract IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents);

        internal delegate bool BondCheck(IBond bond);

        internal IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents, BondCheck bondChecker)
        {
            CheckInitiateParams(reactants, agents);

            var setOfReactions = reactants.Builder.NewReactionSet();
            var reactant = reactants[0];

            // if the parameter hasActiveCenter is not fixed yet, set the active centers
            var ipr = base.GetParameterClass(typeof(SetReactionCenter));
            if (ipr != null && !ipr.IsSetParameter) SetActiveCenters(reactant, bondChecker);
            foreach (var bondi in reactant.Bonds)
            {
                var atom1 = bondi.Begin;
                var atom2 = bondi.End;
                if (bondi.IsReactiveCenter
                 && bondChecker(bondi)
                 && atom1.IsReactiveCenter && atom2.IsReactiveCenter
                 && (atom1.FormalCharge ?? 0) == 0
                 && (atom2.FormalCharge ?? 0) == 0
                 && !reactant.GetConnectedSingleElectrons(atom1).Any()
                 && !reactant.GetConnectedSingleElectrons(atom2).Any())
                {
                    for (int j = 0; j < 2; j++)
                    {
                        var atomList = new List<IAtom>();
                        if (j == 0)
                        {
                            atomList.Add(atom1);
                            atomList.Add(atom2);
                        }
                        else
                        {
                            atomList.Add(atom2);
                            atomList.Add(atom1);
                        }
                        var bondList = new List<IBond>
                        {
                            bondi
                        };

                        var moleculeSet = reactant.Builder.NewAtomContainerSet();
                        moleculeSet.Add(reactant);
                        var reaction = Mechanism.Initiate(moleculeSet, atomList, bondList);
                        if (reaction == null)
                            continue;
                        else
                            setOfReactions.Add(reaction);
                    }
                }
            }
            return setOfReactions;
        }

        private static void SetActiveCenters(IAtomContainer reactant, BondCheck bondChecker)
        {
            foreach (var bondi in reactant.Bonds)
            {
                var atom1 = bondi.Begin;
                var atom2 = bondi.End;
                if (bondChecker(bondi)
                 && (atom1.FormalCharge ?? 0) == 0
                 && (atom2.FormalCharge ?? 0) == 0
                 && !reactant.GetConnectedSingleElectrons(atom1).Any()
                 && !reactant.GetConnectedSingleElectrons(atom2).Any())
                {
                    bondi.IsReactiveCenter = true;
                    atom1.IsReactiveCenter = true;
                    atom2.IsReactiveCenter = true;
                }
            }
        }
    }
}

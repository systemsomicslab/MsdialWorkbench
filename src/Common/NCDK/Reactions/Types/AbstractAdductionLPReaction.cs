/* Copyright (C) 2008  Miguel Rojas <miguelrojasch@users.sf.net>
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
using NCDK.Tools.Manipulator;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Reactions.Types
{
    // @author         Miguel Rojas
    // @cdk.created    2008-02-11
    // @cdk.module     reaction
    public abstract class AbstractAdductionLPReaction : ReactionEngine, IReactionProcess
    {
        /// <inheritdoc/>
        public abstract ReactionSpecification Specification { get; }

        /// <inheritdoc/>
        public abstract IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents);
        
        internal IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents, string atomSymbol)
        {
            CheckInitiateParams(reactants, agents);

            IReactionSet setOfReactions = reactants.Builder.NewReactionSet();
            IAtomContainer reactant = reactants[0];

            var ipr = base.GetParameterClass(typeof(SetReactionCenter));
            if (ipr != null && !ipr.IsSetParameter)
                SetActiveCenters(reactant);

            if (AtomContainerManipulator.GetTotalCharge(reactant) > 0)
                return setOfReactions;

            foreach (var atomi in reactant.Atoms)
            {
                if (atomi.IsReactiveCenter
                   && (atomi.FormalCharge ?? 0) <= 0
                   && reactant.GetConnectedLonePairs(atomi).Any()
                   && !reactant.GetConnectedSingleElectrons(atomi).Any())
                {
                    var atomList = new List<IAtom> { atomi };
                    IAtom atomH = reactant.Builder.NewAtom(atomSymbol);
                    atomH.FormalCharge = 1;
                    atomList.Add(atomH);

                    var moleculeSet = reactant.Builder.NewAtomContainerSet();
                    moleculeSet.Add(reactant);
                    IAtomContainer adduct = reactant.Builder.NewAtomContainer();
                    adduct.Atoms.Add(atomH);
                    moleculeSet.Add(adduct);

                    var reaction = Mechanism.Initiate(moleculeSet, atomList, null);
                    if (reaction == null)
                        continue;
                    else
                        setOfReactions.Add(reaction);
                }
            }

            return setOfReactions;
        }

        private static void SetActiveCenters(IAtomContainer reactant)
        {
            if (AtomContainerManipulator.GetTotalCharge(reactant) > 0)
                return;

            foreach (var atomi in reactant.Atoms)
            {
                if ((atomi.FormalCharge ?? 0) <= 0
                        && reactant.GetConnectedLonePairs(atomi).Any()
                        && !reactant.GetConnectedSingleElectrons(atomi).Any())
                {
                    atomi.IsReactiveCenter = true;
                }
            }
        }
    }
}

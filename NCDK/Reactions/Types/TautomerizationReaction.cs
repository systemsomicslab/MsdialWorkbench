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
    /// <summary>
    /// <para>IReactionProcess which produces a tautomerization chemical reaction.
    /// As most commonly encountered, this reaction results in the formal migration
    /// of a hydrogen atom or proton, accompanied by a switch of a single bond and adjacent double bond</para>
    /// <pre>X=Y-Z-H =&gt; X(H)-Y=Z</pre>
    /// </summary>
    /// <seealso cref="Mechanisms.TautomerizationMechanism"/>
    // @author         Miguel Rojas
    // @cdk.created    2008-02-11
    // @cdk.module     reaction
    public partial class TautomerizationReaction : ReactionEngine, IReactionProcess
    {
        public TautomerizationReaction() { }

        /// <inheritdoc/>
        public ReactionSpecification Specification =>
            new ReactionSpecification(
                    "http://almost.cubic.uni-koeln.de/jrg/Members/mrc/reactionDict/reactionDict#Tautomerization", this
                            .GetType().Name, "$Id$", "The Chemistry Development Kit");

        /// <inheritdoc/>
        public IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents)
        {
            CheckInitiateParams(reactants, agents);

            var setOfReactions = reactants.Builder.NewReactionSet();
            var reactant = reactants[0];

            // if the parameter hasActiveCenter is not fixed yet, set the active centers
            var ipr = base.GetParameterClass(typeof(SetReactionCenter));
            if (ipr != null && !ipr.IsSetParameter)
                SetActiveCenters(reactant);

            foreach (var atomi in reactant.Atoms)
            {
                if (atomi.IsReactiveCenter
                        && (atomi.FormalCharge ?? 0) == 0
                        && !reactant.GetConnectedSingleElectrons(atomi).Any())
                {
                    foreach (var bondi in reactant.GetConnectedBonds(atomi))
                    {
                        if (bondi.IsReactiveCenter && bondi.Order == BondOrder.Double)
                        {
                            var atomj = bondi.GetOther(atomi);
                            if (atomj.IsReactiveCenter
                             && (atomj.FormalCharge ?? 0) == 0
                             && !reactant.GetConnectedSingleElectrons(atomj).Any())
                            {
                                foreach (var bondj in reactant.GetConnectedBonds(atomj))
                                {
                                    if (bondj.Equals(bondi))
                                        continue;

                                    if (bondj.IsReactiveCenter
                                     && bondj.Order == BondOrder.Single)
                                    {
                                        var atomk = bondj.GetOther(atomj);
                                        if (atomk.IsReactiveCenter
                                         && (atomk.FormalCharge ?? 0) == 0
                                         && !reactant.GetConnectedSingleElectrons(atomk).Any())
                                        {
                                            foreach (var bondk in reactant.GetConnectedBonds(atomk))
                                            {
                                                if (bondk.Equals(bondj))
                                                    continue;
                                                if (bondk.IsReactiveCenter && bondk.Order == BondOrder.Single)
                                                {
                                                    IAtom atoml = bondk.GetOther(atomk); // Atom pos 4
                                                    if (atoml.IsReactiveCenter && atoml.AtomicNumber.Equals(AtomicNumbers.H))
                                                    {
                                                        var atomList = new List<IAtom>
                                                        {
                                                            atomi,
                                                            atomj,
                                                            atomk,
                                                            atoml
                                                        };
                                                        var bondList = new List<IBond>
                                                        {
                                                            bondi,
                                                            bondj,
                                                            bondk
                                                        };

                                                        var moleculeSet = reactant.Builder.NewChemObjectSet<IAtomContainer>();
                                                        moleculeSet.Add(reactant);
                                                        var reaction = Mechanism.Initiate(moleculeSet, atomList, bondList);
                                                        if (reaction == null)
                                                            continue;
                                                        else
                                                            setOfReactions.Add(reaction);

                                                        break; // because of the others atoms are hydrogen too.
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
            return setOfReactions;
        }

        /// <summary>
        /// set the active center for this molecule.
        /// The active center will be those which correspond with X=Y-Z-H.
        /// <pre>
        /// X: Atom
        /// =: bond
        /// Y: Atom
        /// -: bond
        /// Z: Atom
        /// -: bond
        /// H: Atom
        ///  </pre>
        /// </summary>
        /// <param name="reactant">The molecule to set the activity</param>
        private static void SetActiveCenters(IAtomContainer reactant)
        {
            foreach (var atomi in reactant.Atoms)
            {
                if ((atomi.FormalCharge ?? 0) == 0
                 && !reactant.GetConnectedSingleElectrons(atomi).Any())
                {
                    foreach (var bondi in reactant.GetConnectedBonds(atomi))
                    {
                        if (bondi.Order == BondOrder.Double)
                        {
                            var atomj = bondi.GetOther(atomi);
                            if ((atomj.FormalCharge ?? 0) == 0
                             && !reactant.GetConnectedSingleElectrons(atomj).Any())
                            {
                                foreach (var bondj in reactant.GetConnectedBonds(atomj))
                                {
                                    if (bondj.Equals(bondi))
                                        continue;
                                    if (bondj.Order == BondOrder.Single)
                                    {
                                        var atomk = bondj.GetOther(atomj);
                                        if ((atomk.FormalCharge ?? 0) == 0
                                                && !reactant.GetConnectedSingleElectrons(atomk).Any()
                                        )
                                        {
                                            foreach (var bondk in reactant.GetConnectedBonds(atomk))
                                            {
                                                if (bondk.Equals(bondj))
                                                    continue;
                                                if (bondk.Order == BondOrder.Single)
                                                {
                                                    IAtom atoml = bondk.GetOther(atomk); // Atom pos 4
                                                    if (atoml.AtomicNumber.Equals(AtomicNumbers.H))
                                                    {
                                                        atomi.IsReactiveCenter = true;
                                                        atomj.IsReactiveCenter = true;
                                                        atomk.IsReactiveCenter = true;
                                                        atoml.IsReactiveCenter = true;
                                                        bondi.IsReactiveCenter = true;
                                                        bondj.IsReactiveCenter = true;
                                                        bondk.IsReactiveCenter = true;
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
        }
    }
}

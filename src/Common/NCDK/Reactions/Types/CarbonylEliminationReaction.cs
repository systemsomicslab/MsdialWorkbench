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

namespace NCDK.Reactions.Types
{
    /// <summary>
    /// <para>IReactionProcess which participate mass spectrum process.
    /// This reaction could be represented as RC-C#[O+] =&gt; R[C] + |C#[O+]</para>
    /// Make sure that the molecule has the correspond lone pair electrons
    /// for each atom. You can use <see cref="Tools.LonePairElectronChecker"/>.
    /// </summary>
    /// <seealso cref="Mechanisms.HeterolyticCleavageMechanism"/>
    // @author         Miguel Rojas
    // @cdk.created    2006-10-16
    // @cdk.module     reaction
    public partial class CarbonylEliminationReaction : ReactionEngine, IReactionProcess
    {
        public CarbonylEliminationReaction() { }

        /// <summary>
        ///  Gets the specification attribute of the CarbonylEliminationReaction object.
        /// </summary>
        /// <returns>The specification value</returns>
        public ReactionSpecification Specification =>
            new ReactionSpecification(
                "http://almost.cubic.uni-koeln.de/jrg/Members/mrc/reactionDict/reactionDict#CarbonylElimination", 
                this.GetType().Name, "$Id$", "The Chemistry Development Kit");

        /// <summary>
        ///  Initiate process.
        /// </summary>
        /// <exception cref="CDKException"> Description of the Exception</exception>
        /// <param name="reactants">reactants of the reaction</param>
        /// <param name="agents">agents of the reaction (Must be in this case null)</param>
        public IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents)
        {
            CheckInitiateParams(reactants, agents);

            IReactionSet setOfReactions = reactants.Builder.NewReactionSet();
            IAtomContainer reactant = reactants[0];

            // if the parameter hasActiveCenter is not fixed yet, set the active
            // centers
            var ipr = base.GetParameterClass(typeof(SetReactionCenter));
            if (ipr != null && !ipr.IsSetParameter) SetActiveCenters(reactant);
            foreach (var atomi in reactant.Atoms)
            {
                if (atomi.IsReactiveCenter && atomi.AtomicNumber.Equals(AtomicNumbers.O)
                    && atomi.FormalCharge == 1)
                {
                    foreach (var bondi in reactant.GetConnectedBonds(atomi))
                    {
                        if (bondi.IsReactiveCenter && bondi.Order == BondOrder.Triple)
                        {
                            IAtom atomj = bondi.GetOther(atomi);
                            if (atomj.IsReactiveCenter)
                            {
                                foreach (var bondj in reactant.GetConnectedBonds(atomj))
                                {
                                    if (bondj.Equals(bondi)) continue;

                                    if (bondj.IsReactiveCenter
                                            && bondj.Order == BondOrder.Single)
                                    {
                                        IAtom atomk = bondj.GetOther(atomj);
                                        if (atomk.IsReactiveCenter && atomk.FormalCharge == 0)
                                        {
                                            var atomList = new List<IAtom>
                                            {
                                                atomk,
                                                atomj
                                            };
                                            var bondList = new List<IBond>
                                            {
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

        /// <summary>
        /// set the active center for this molecule.
        /// The active center will be those which correspond with RC-C#[O+].
        /// <pre>
        /// C: Atom
        /// -: single bond
        /// C: Atom
        /// #: triple bond
        /// O: Atom with formal charge = 1
        ///  </pre>
        /// </summary>
        /// <param name="reactant">The molecule to set the activity</param>
        private static void SetActiveCenters(IAtomContainer reactant)
        {
            foreach (var atomi in reactant.Atoms)
            {
                if (atomi.AtomicNumber.Equals(AtomicNumbers.O) && atomi.FormalCharge == 1)
                {
                    foreach (var bondi in reactant.GetConnectedBonds(atomi))
                    {
                        if (bondi.Order == BondOrder.Triple)
                        {
                            IAtom atomj = bondi.GetOther(atomi);
                            foreach (var bondj in reactant.GetConnectedBonds(atomj))
                            {
                                if (bondj.Equals(bondi))
                                    continue;

                                if (bondj.Order == BondOrder.Single)
                                {
                                    IAtom atomk = bondj.GetOther(atomj);
                                    if (atomk.FormalCharge == 0)
                                    {
                                        atomi.IsReactiveCenter = true;
                                        bondi.IsReactiveCenter = true;
                                        atomj.IsReactiveCenter = true;
                                        bondj.IsReactiveCenter = true;
                                        atomk.IsReactiveCenter = true;
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

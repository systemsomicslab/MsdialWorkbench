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
using System.Diagnostics;
using System.Linq;

namespace NCDK.Reactions.Types
{
    /// <summary>
    /// <para>IReactionProcess which make an electron impact for for Non-Bonding Electron Lost.</para>
    /// <para>This reaction type is a representation of the processes which occurs in the mass spectrometer.</para>
    /// </summary>
    /// <seealso cref="Mechanisms.RemovingSEofNBMechanism"/>
    // @author         Miguel Rojas
    // @cdk.created    2006-04-01
    // @cdk.module     reaction
    // @cdk.dictref    reaction-types:electronImpact
    public partial class ElectronImpactNBEReaction : ReactionEngine, IReactionProcess
    {
        public ElectronImpactNBEReaction()
            : base()
        { }

        /// <summary>
        /// Gets the specification attribute of the ElectronImpactNBEReaction object.
        /// </summary>
        /// <returns>The specification value</returns>
        public ReactionSpecification Specification =>
            new ReactionSpecification(
                    "http://almost.cubic.uni-koeln.de/jrg/Members/mrc/reactionDict/reactionDict#ElectronImpactNBE", this
                            .GetType().Name, "$Id$", "The Chemistry Development Kit");

        /// <summary>
        ///  Initiate process.
        ///  It is needed to call the addExplicitHydrogensToSatisfyValency
        ///  from the class tools.HydrogenAdder.
        /// </summary>
        /// <param name="reactants">Reactants of the reaction</param>
        /// <param name="agents">Agents of the reaction (Must be in this case null)</param>
        /// <exception cref="CDKException"> Description of the Exception</exception>
        public IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents)
        {
            Debug.WriteLine("initiate reaction: ElectronImpactNBEReaction");

            if (reactants.Count != 1)
            {
                throw new CDKException("ElectronImpactNBEReaction only expects one reactant");
            }
            if (agents != null)
            {
                throw new CDKException("ElectronImpactNBEReaction don't expects agents");
            }

            var setOfReactions = reactants.Builder.NewReactionSet();
            var reactant = reactants[0];

            // if the parameter hasActiveCenter is not fixed yet, set the active centers
            var ipr = base.GetParameterClass(typeof(SetReactionCenter));
            if (ipr != null && !ipr.IsSetParameter)
                SetActiveCenters(reactant);
            foreach (var atom in reactant.Atoms)
            {
                if (atom.IsReactiveCenter && reactant.GetConnectedLonePairs(atom).Any()
                        && !reactant.GetConnectedSingleElectrons(atom).Any())
                {
                    var atomList = new List<IAtom> { atom };
                    var moleculeSet = reactant.Builder.NewAtomContainerSet();
                    moleculeSet.Add(reactant);
                    var reaction = Mechanism.Initiate(moleculeSet, atomList, null);
                    if (reaction == null)
                        continue;
                    else
                        setOfReactions.Add(reaction);
                }
            }
            return setOfReactions;

        }

        /// <summary>
        /// set the active center for this molecule. The active center
        /// will be heteroatoms which contain at least one group of
        /// lone pair electrons.
        /// </summary>
        /// <param name="reactant">The molecule to set the activity</param>
        private static void SetActiveCenters(IAtomContainer reactant)
        {
            foreach (var atom in reactant.Atoms)
            {
                if (reactant.GetConnectedLonePairs(atom).Any() && !reactant.GetConnectedSingleElectrons(atom).Any())
                    atom.IsReactiveCenter = true;
            }
        }
    }
}

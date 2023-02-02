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

namespace NCDK.Reactions.Types
{
    /// <summary>
    /// <see cref="IReactionProcess"/> which produces a protonation.
    /// </summary>
    /// <para>
    /// As most commonly encountered, this reaction results in the formal migration
    /// of a hydrogen atom or proton, accompanied by a switch of a single bond and adjacent double bond
    /// </para>
    /// <para>
    /// [X-] + [H+] =&gt; X -H<br/>
    /// |X + [H+]   =&gt; [X+]-H<br/>
    /// </para>
    /// <seealso cref="Mechanisms.AdductionLPMechanism"/>
    // @author         Miguel Rojas
    // @cdk.created    2008-02-11
    // @cdk.module     reaction
    public partial class AdductionProtonLPReaction : AbstractAdductionLPReaction
    {
        public AdductionProtonLPReaction() { }

        /// <inheritdoc/>
        public override ReactionSpecification Specification =>
            new ReactionSpecification(
                    "http://almost.cubic.uni-koeln.de/jrg/Members/mrc/reactionDict/reactionDict#AdductionProtonLP",
                    this.GetType().Name, "$Id$", "The Chemistry Development Kit");

        /// <summary>
        ///  Initiate process.
        ///  It is needed to call the addExplicitHydrogensToSatisfyValency
        ///  from the class tools.HydrogenAdder.
        /// </summary>
        /// <exception cref="CDKException"></exception>
        /// <param name="reactants">reactants of the reaction</param>
        /// <param name="agents">agents of the reaction (Must be in this case null)</param>
        public override IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents)
        {
            return base.Initiate(reactants, agents, "H");
        }
    }
}

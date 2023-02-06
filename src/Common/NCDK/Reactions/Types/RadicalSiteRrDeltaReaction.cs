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

namespace NCDK.Reactions.Types
{
    /// <summary>
    /// <para>This reaction could be represented as [A*]-(C)_4-C5[R] =&gt; A([R])-(C_4)-[C5*]. Due to
    /// the single electron of atom A the R is moved.</para>
    /// </summary>
    /// <seealso cref="Mechanisms.RadicalSiteRearrangementMechanism"/>
    // @author         Miguel Rojas
    // @cdk.created    2006-10-20
    // @cdk.module     reaction
    public partial class RadicalSiteRrDeltaReaction : AbstractRadicalSiteReaction
    {
        public RadicalSiteRrDeltaReaction() { }

        /// <inheritdoc/>
        public override ReactionSpecification Specification => new ReactionSpecification(
                    "http://almost.cubic.uni-koeln.de/jrg/Members/mrc/reactionDict/reactionDict#RadicalSiteRrDelta", this
                            .GetType().Name, "$Id$", "The Chemistry Development Kit");

        /// <inheritdoc/>
        public override IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents)
        {
            return base.Initiate(reactants, agents, 5, true, atom => (atom.FormalCharge ?? 0) == 0);
        }
    }
}

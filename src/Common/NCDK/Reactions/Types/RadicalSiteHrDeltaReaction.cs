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
    /// <para>
    /// This reaction could be represented as [A*]-(C)_4-C5[H] =&gt; A([H])-(C_4)-[C5*]. Due to
    /// the single electron of atom A the proton is moved.</para>
    /// </summary>
    /// <seealso cref="Mechanisms.RadicalSiteRearrangementMechanism"/>
    // @author         Miguel Rojas
    // @cdk.created    2006-10-20
    // @cdk.module     reaction
    public partial class RadicalSiteHrDeltaReaction : AbstractRadicalSiteReaction
    {
        /// <summary>
        /// Constructor of the RadicalSiteHrDeltaReaction object
        /// </summary>
        public RadicalSiteHrDeltaReaction() { }

        /// <summary>
        ///  Gets the specification attribute of the RadicalSiteHrDeltaReaction object
        /// </summary>
        /// <returns>The specification value</returns>
        public override ReactionSpecification Specification =>
            new ReactionSpecification(
                    "http://almost.cubic.uni-koeln.de/jrg/Members/mrc/reactionDict/reactionDict#RadicalSiteHrDelta", this
                            .GetType().Name, "$Id$", "The Chemistry Development Kit");

        /// <summary>
        ///  Initiate process.
        ///  It is needed to call the addExplicitHydrogensToSatisfyValency
        ///  from the class tools.HydrogenAdder.
        /// </summary>
        /// <exception cref="CDKException"> Description of the Exception</exception>
        /// <param name="reactants">reactants of the reaction.</param>
        /// <param name="agents">agents of the reaction (Must be in this case null).</param>
        public override IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents)
        {
            return base.Initiate(reactants, agents, 5, false, atom => atom.AtomicNumber.Equals(AtomicNumbers.H));
        }
    }
}

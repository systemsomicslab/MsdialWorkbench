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
    /// <para>IReactionProcess which participate mass spectrum process. Homolitic dissocitation.
    /// This reaction could be represented as H-B-[C*] =&gt; [H*] + B=C. H is hydrogen atom</para>
    /// <para>
    /// Make sure that the molecule has the correspond lone pair electrons
    /// for each atom. You can use the method: <see cref="Tools.LonePairElectronChecker"/></para>
    /// </summary>
    /// <seealso cref="Mechanisms.RadicalSiteIonizationMechanism"/>
    // @author         Miguel Rojas
    // @cdk.created    2006-10-18
    // @cdk.module     reaction
    public partial class RadicalSiteInitiationHReaction : AbstractRadicalSiteInitiationReaction
    {
        public RadicalSiteInitiationHReaction() { }

        /// <inheritdoc/>
        public override ReactionSpecification Specification => new ReactionSpecification(
                    "http://almost.cubic.uni-koeln.de/jrg/Members/mrc/reactionDict/reactionDict#RadicalSiteInitiationH",
                    this.GetType().Name, "$Id$", "The Chemistry Development Kit");

        /// <summary>
        ///  Initiate process.
        /// </summary>
        /// <exception cref="CDKException"></exception>
        /// <param name="reactants">reactants of the reaction.</param>
        /// <param name="agents">agents of the reaction (Must be in this case null).</param>
        public override IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents)
        {
            return base.Initiate(reactants, agents, "H", 0);
        }
    }
}

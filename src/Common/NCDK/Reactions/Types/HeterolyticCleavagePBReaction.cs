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
    /// IReactionProcess which a bond is broken displacing the electron to one of the
    /// atoms. The mechanism will produce one atom with excess of charge and the other one deficiency.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Depending of the bond order, the bond will be removed or simply the order decreased.
    /// </para>
    /// <para>
    /// As there are two directions for displacing a bond in a polar manner,
    /// each case is investigated twice:</para>
    ///
    /// <pre>A=B =&gt; [A+]-|[B-]</pre>
    /// <pre>A=B =&gt; |[A-]-[B+]</pre>
    ///
    /// <para>It will not be created structures no possible, e.g; C=O =&gt; [C-][O+].</para>
    /// </remarks>
    /// <seealso cref="Mechanisms.HeterolyticCleavageMechanism"/>
    // @author         Miguel Rojas
    // @cdk.created    2006-06-09
    // @cdk.module     reaction
    public partial class HeterolyticCleavagePBReaction : AbstractHeterolyticCleavageReaction
    {
        public HeterolyticCleavagePBReaction() { }

        /// <summary>
        ///  Gets the specification attribute of the HeterolyticCleavagePBReaction object.
        /// </summary>
        /// <returns>The specification value</returns>
        public override ReactionSpecification Specification =>
            new ReactionSpecification(
                    "http://almost.cubic.uni-koeln.de/jrg/Members/mrc/reactionDict/reactionDict#HeterolyticCleavagePB",
                    this.GetType().Name, "$Id$", "The Chemistry Development Kit");

        /// <summary>
        ///  Initiate process.
        ///  It is needed to call the addExplicitHydrogensToSatisfyValency
        ///  from the class tools.HydrogenAdder.
        /// </summary>
        /// <exception cref="CDKException"> Description of the Exception</exception>
        /// <param name="reactants">reactants of the reaction</param>
        /// <param name="agents">agents of the reaction (Must be in this case null)</param>
        public override IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents)
        {
            return base.Initiate(reactants, agents, 2, bond => bond.Order != BondOrder.Single);
        }
    }
}

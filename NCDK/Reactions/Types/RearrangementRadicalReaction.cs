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

using NCDK.Tools.Manipulator;
using System.Linq;

namespace NCDK.Reactions.Types
{
    /// <summary>
    /// <para>IReactionProcess which participate in movement resonance.
    /// This reaction could be represented as [A*]-B=C =&gt; A=B-[c*]. Due to
    /// excess of charge of the atom B, the single electron of the atom A is
    /// displaced through the double bond.</para>
    /// <para>Make sure that the molecule has the correspond lone pair electrons
    /// for each atom. You can use the method: <see cref="Tools.LonePairElectronChecker"/></para>
    /// </summary>
    /// <seealso cref="Mechanisms.RearrangementChargeMechanism"/>
    // @author         Miguel Rojas
    // @cdk.created    2006-05-05
    // @cdk.module     reaction
    public partial class RearrangementRadicalReaction : AbstractRearrangementReaction
    {
        public RearrangementRadicalReaction() { }

        /// <inheritdoc/>
        public override ReactionSpecification Specification =>
            new ReactionSpecification(
                    "http://almost.cubic.uni-koeln.de/jrg/Members/mrc/reactionDict/reactionDict#RearrangementRadical", this
                            .GetType().Name, "$Id$", "The Chemistry Development Kit");

        /// <inheritdoc/>
        public override IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents)
        {
            return base.Initiate(reactants, agents,
                reactant => AtomContainerManipulator.GetTotalNegativeFormalCharge(reactant) == 0,
                (mol, atom) => mol.GetConnectedSingleElectrons(atom).Count() == 1,
                atom => (atom.FormalCharge ?? 0) == 0);
        }
    }
}

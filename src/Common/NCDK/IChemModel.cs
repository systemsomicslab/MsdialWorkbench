/* Copyright (C) 2006-2007,2011  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

namespace NCDK
{
    /// <summary>
    /// An object containing multiple MoleculeSet and
    /// the other lower level concepts like rings, sequences,
    /// fragments, etc.
    /// </summary>
    public interface IChemModel
        : IChemObject
    {
        /// <summary>
        /// The <see cref="IChemObjectSet{T}"/> of this <see cref="IChemModel"/>.
        /// </summary>
        IChemObjectSet<IAtomContainer> MoleculeSet { get; set; }

        /// <summary>
        /// The <see cref="IRingSet"/> of this <see cref="IChemModel"/>.
        /// </summary>
        IRingSet RingSet { get; set; }

        /// <summary>
        /// The <see cref="ICrystal"/> of this <see cref="IChemModel"/>.
        /// </summary>
        ICrystal Crystal { get; set; }

        /// <summary>
        /// The <see cref="IReactionSet"/> of this <see cref="IChemModel"/>.
        /// </summary>
        IReactionSet ReactionSet { get; set; }

        /// <summary>
        /// Returns true if this <see cref="IChemModel"/> is empty.
        /// </summary>
        /// <returns>a boolean indicating if this model has no content</returns>
        bool IsEmpty();
    }
}

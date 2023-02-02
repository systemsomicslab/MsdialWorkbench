/* Copyright (C) 2009  Gilleain Torrance <gilleain@users.sf.net>
 *               2009  Arvid Berg <goglepox@users.sf.net>
 *
 * Contact: cdk-devel@list.sourceforge.net
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

using System.Collections.Generic;

namespace NCDK.Renderers.Selection
{
    /// <summary>
    /// A selection of some atoms and bonds from an <see cref="IAtomContainer"/> or
    /// <see cref="IChemModel"/>.
    /// </summary>
    // @author maclean
    // @cdk.module render
    public interface IChemObjectSelection
    {
        /// <summary>
        /// Perform a selection by some method. This is used for selecting outside
        /// the hub.
        /// </summary>
        /// <example>
        /// <code>
        ///   IChemModel model = CreateModelBySomeMethod();
        ///   selection.Select(model);
        ///   renderModel.SetSelection(selection);
        /// </code>
        /// </example>
        /// <param name="chemModel">an IChemModel to select from.</param>
        void Select(IChemModel chemModel);

        /// <summary>
        /// Make an IAtomContainer where all the bonds
        /// only have atoms that are in the selection.
        /// </summary>
        /// <returns>a well defined atom container.</returns>
        IAtomContainer GetConnectedAtomContainer();

        /// <summary>
        /// The opposite of a method like "isEmpty".
        /// </summary>
        /// <returns>true if there is anything in the selection</returns>
        bool IsFilled();

        /// <summary>
        /// Determines if the <see cref="IChemObject"/> is part of the current selection.
        /// </summary>
        /// <param name="obj"><see cref="IChemObject"/> which might be part of the selection</param>
        /// <returns>true, if the given <paramref name="obj"/> is part of the selection</returns>
        bool Contains(IChemObject obj);

        /// <summary>
        /// Returns a <see cref="ICollection{T}"/> of all selected <see cref="IChemObject"/>s of the given type.
        /// </summary>
        /// <typeparam name="T">type of <see cref="IChemObject"/>s that should be returned.</typeparam>
        /// <returns>a <see cref="ICollection{T}"/>  of <see cref="IChemObject"/> of the given type</returns>
        ICollection<T> Elements<T>() where T: IChemObject;
    }
}

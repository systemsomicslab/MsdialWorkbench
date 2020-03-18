/* Copyright (C) 2008  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Numerics;
using System.Collections.Generic;

namespace NCDK.Tools.Diff.Tree
{
    /// <summary>
    /// <see cref="IDifference"/> between two <see cref="object"/>s which contains one or more child
    /// <see cref="IDifference"/> objects.
    /// </summary>
    // @author     egonw
    // @cdk.module diff
    public interface IDifferenceList : IDifference
    {
        /// <summary>
        /// Adds a new <see cref="IDifference"/> as child to this tree. For example, an <see cref="IAtom"/> difference
        /// would have a child difference for <see cref="Vector2"/>.
        /// </summary>
        /// <param name="childDiff">child <see cref="IDifference"/> to add to this <see cref="IDifference"/></param>
        void AddChild(IDifference childDiff);

        /// <summary>
        /// Adds multiple <see cref="IDifference"/>s as child to this tree.
        ///
        /// <param name="children">a <see cref="List{T}"/> of <see cref="IDifference"/>s to add to this <see cref="IDifference"/></param>
        /// </summary>
        void AddChildren(IEnumerable<IDifference> children);

        /// <summary>
        /// Returns an <see cref="IEnumerable{T}"/> of <see cref="IDifference"/> for all childs of this <see cref="IDifference"/>.
        /// </summary>
        /// <returns>an <see cref="IEnumerable{T}"/> implementation with all children</returns>
        IEnumerable<IDifference> GetChildren();

        /// <summary>
        /// Returns the number of children of this <see cref="IDifference"/>.
        /// </summary>
        /// <returns>an int reflecting the number of children</returns>
        int ChildCount();
    }
}

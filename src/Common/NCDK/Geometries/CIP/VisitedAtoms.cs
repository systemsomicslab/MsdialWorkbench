/* Copyright (C) 2010  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
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

namespace NCDK.Geometries.CIP
{
    /// <summary>
    /// Helper class for the <see cref="CIPTool"/> to keep track of which atoms have
    /// already been visited.
    /// </summary>
    // @cdk.module cip
    public class VisitedAtoms
    {
        /// <summary>
        /// <see cref="List{T}"/> to hold the visited <see cref="IAtom"/>s.
        /// </summary>
        private List<IAtom> visitedItems;

        /// <summary>
        /// Creates a new empty list of visited <see cref="IAtom"/>s.
        /// </summary>
        public VisitedAtoms()
        {
            visitedItems = new List<IAtom>();
        }

        /// <summary>
        /// Returns true if the given atom already has been visited.
        /// </summary>
        /// <param name="atom"><see cref="IAtom"/> which may have been visited</param>
        /// <returns>true if the <see cref="IAtom"/> was visited</returns>
        public bool IsVisited(IAtom atom)
        {
            return visitedItems.Contains(atom);
        }

        /// <summary>
        /// Marks the given atom as visited.
        /// </summary>
        /// <param name="atom"><see cref="IAtom"/> that is now marked as visited</param>
        public void Visited(IAtom atom)
        {
            visitedItems.Add(atom);
        }

        /// <summary>
        /// Adds all atoms from the <paramref name="visitedAtoms"/> list to the current list.
        /// </summary>
        /// <param name="visitedAtoms">the <see cref="VisitedAtoms"/> from which all atoms are added</param>
        public void Visited(VisitedAtoms visitedAtoms)
        {
            visitedItems.AddRange(visitedAtoms.visitedItems);
        }
    }
}

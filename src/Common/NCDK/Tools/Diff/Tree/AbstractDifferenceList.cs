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

using System.Collections.Generic;

namespace NCDK.Tools.Diff.Tree
{
    /// <summary>
    /// Diff between two IChemObjects.
    /// </summary>
    // @author     egonw
    // @cdk.module diff
    public abstract class AbstractDifferenceList : IDifferenceList
    {
        private List<IDifference> differences;

        protected AbstractDifferenceList()
        {
            differences = new List<IDifference>();
        }

        /// <inheritdoc/>
        public void AddChild(IDifference childDiff)
        {
            if (childDiff != null)
            {
                differences.Add(childDiff);
            }
        }

        /// <inheritdoc/>
        public void AddChildren(IEnumerable<IDifference> children)
        {
            if (children != null)
            {
                differences.AddRange(children);
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IDifference> GetChildren()
        {
            return differences;
        }

        /// <inheritdoc/>
        public int ChildCount()
        {
            return differences.Count;
        }
    }
}

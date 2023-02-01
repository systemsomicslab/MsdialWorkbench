/* Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
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
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System.Collections.Generic;

namespace NCDK.Tools.Manipulator
{
    public enum SortMode
    {
        /// <summary>Flag to denote that the set is order with the largest ring first</summary>
        LargeFirst = 1,
        
        /// <summary>Flag to denote that the set is order with the smallest ring first</summary>
        SmallFirst = 2,
    }

    // @cdk.module standard
    public class RingSizeComparator : IComparer<IRing>
    {
        private readonly SortMode sortOrder = SortMode.SmallFirst;

        /// <summary>
        /// Constructs a new comparator to sort rings by size.
        /// </summary>
        /// <param name="order">Sort order: either <see cref="SortMode.SmallFirst"/> or <see cref="SortMode.LargeFirst"/>.</param>
        public RingSizeComparator(SortMode order)
        {
            sortOrder = order;
        }

        public int Compare(IRing object1, IRing object2)
        {
            int size1 = object1.Atoms.Count;
            int size2 = object2.Atoms.Count;
            if (size1 == size2) return 0;
            if (size1 > size2 && sortOrder == SortMode.SmallFirst)
            {
                return 1;
            }
            if (size1 > size2 && sortOrder == SortMode.LargeFirst)
            {
                return -1;
            }
            if (size1 < size2 && sortOrder == SortMode.SmallFirst)
            {
                return -1;
            }
            if (size1 < size2 && sortOrder == SortMode.LargeFirst)
            {
                return 1;
            }
            return 0;
        }
    }
}

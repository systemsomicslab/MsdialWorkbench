/* Copyright (C) 2012  Gilleain Torrance <gilleain.torrance@gmail.com>
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

using NCDK.Common.Collections;

namespace NCDK.Groups
{
    /// <summary>
    /// Implementation of a union-find data structure, largely copied from
    /// code due to Derrick Stolee.
    /// </summary>
    // @author maclean
    // @cdk.module standard
    // @cdk.keyword union-find
    public class DisjointSetForest
    {
        /// <summary>
        /// The sets stored as pointers to their parents. The root of each
        /// set is stored as the negated size of the set - ie a set of size
        /// 5 with a root element 2 will mean forest[2] = -5.
        /// </summary>
        private int[] forest;

        /// <summary>
        /// Initialize a disjoint set forest with a number of elements.
        /// </summary>
        /// <param name="numberOfElements">the number of elements in the forest</param>
        public DisjointSetForest(int numberOfElements)
        {
            forest = new int[numberOfElements];
            for (int i = 0; i < numberOfElements; i++)
            {
                forest[i] = -1;
            }
        }

        /// <summary>
        /// Get the value of the forest at this index - note that this will <i>not</i>
        /// necessarily give the set for that element : use <see cref="GetSets"/>  after
        /// union-ing elements.
        /// </summary>
        /// <param name="i">the index in the forest</param>
        /// <returns>the value at this index</returns>
        public int this[int i] => forest[i];

        /// <summary>
        /// Travel up the tree that this element is in, until the root of the set
        /// is found, and return that root.
        /// </summary>
        /// <param name="element">the starting point</param>
        /// <returns>the root of the set containing element</returns>
        public int GetRoot(int element)
        {
            if (forest[element] < 0)
            {
                return element;
            }
            else
            {
                return GetRoot(forest[element]);
            }
        }

        /// <summary>
        /// Union these two elements - in other words, put them in the same set.
        /// </summary>
        /// <param name="elementX">an element</param>
        /// <param name="elementY">an element</param>
        public void MakeUnion(int elementX, int elementY)
        {
            int xRoot = GetRoot(elementX);
            int yRoot = GetRoot(elementY);

            if (xRoot == yRoot)
            {
                return;
            }

            if (forest[xRoot] < forest[yRoot])
            {
                forest[yRoot] = forest[yRoot] + forest[xRoot];
                forest[xRoot] = yRoot;
            }
            else
            {
                forest[xRoot] = forest[xRoot] + forest[yRoot];
                forest[yRoot] = xRoot;
            }
        }

        /// <summary>
        /// Retrieve the sets as 2D-array of ints.
        /// </summary>
        /// <returns>the sets</returns>
        public int[][] GetSets()
        {
            int n = 0;
            for (int i = 0; i < forest.Length; i++)
            {
                if (forest[i] < 0)
                {
                    n++;
                }
            }
            int[][] sets = new int[n][];
            int currentSet = 0;
            for (int i = 0; i < forest.Length; i++)
            {
                if (forest[i] < 0)
                {
                    int setSize = 1 - forest[i] - 1;
                    sets[currentSet] = new int[setSize];
                    int currentIndex = 0;
                    for (int element = 0; element < forest.Length; element++)
                    {
                        if (GetRoot(element) == i)
                        {
                            sets[currentSet][currentIndex] = element;
                            currentIndex++;
                        }
                    }
                    currentSet++;
                }
            }
            return sets;
        }

        public override string ToString()
        {
            return Arrays.ToJavaString(forest);
        }
    }
}

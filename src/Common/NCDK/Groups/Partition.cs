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
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Common.Base;
using NCDK.Common.Collections;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace NCDK.Groups
{
    /// <summary>
    /// A partition of a set of integers, such as the discrete partition {{1}, {2},
    /// {3}, {4}} or the unit partition {{1, 2, 3, 4}} or an intermediate like {{1,
    /// 2}, {3, 4}}.
    /// </summary>
    // @author maclean
    // @cdk.module group
    public class Partition
    {
        /// <summary>
        /// The subsets of the partition, known as cells.
        /// </summary>
        private List<SortedSet<int>> cells;

        /// <summary>
        /// Creates a new, empty partition with no cells.
        /// </summary>
        public Partition()
        {
            this.cells = new List<SortedSet<int>>();
        }

        /// <summary>
        /// Copy constructor to make one partition from another.
        /// </summary>
        /// <param name="other">the partition to copy</param>
        public Partition(Partition other)
            : this()
        {
            foreach (var block in other.cells)
            {
                this.cells.Add(new SortedSet<int>(block));
            }
        }

        /// <summary>
        /// Constructor to make a partition from an array of int arrays.
        /// </summary>
        /// <param name="cellData">the partition to copy</param>
        public Partition(int[][] cellData)
                : this()
        {
            foreach (var aCellData in cellData)
            {
                AddCell(aCellData);
            }
        }

        /// <summary>
        /// Create a unit partition - in other words, the coarsest possible partition
        /// where all the elements are in one cell.
        /// </summary>
        /// <param name="size">the number of elements</param>
        /// <returns>a new Partition with one cell containing all the elements</returns>
        public static Partition Unit(int size)
        {
            Partition unit = new Partition();
            unit.cells.Add(new SortedSet<int>());
            for (int i = 0; i < size; i++)
            {
                unit.cells[0].Add(i);
            }
            return unit;
        }

        public override bool Equals(object o)
        {

            if (this == o) return true;
            if (o == null || GetType() != o.GetType()) return false;

            Partition partition = (Partition)o;

            return cells != null ? Compares.AreDeepEqual(cells, partition.cells) : partition.cells == null;

        }

        public override int GetHashCode()
        {
            return cells != null ? Lists.GetDeepHashCode(cells) : 0;
        }

        /// <summary>
        /// Gets the size of the partition, in terms of the number of cells.
        /// </summary>
        /// <returns>the number of cells in the partition</returns>
        public int Count => this.cells.Count;

        /// <summary>
        /// Calculate the size of the partition as the sum of the sizes of the cells.
        /// </summary>
        /// <returns>the number of elements in the partition</returns>
        public int NumberOfElements()
        {
            int n = 0;
            foreach (var cell in cells)
            {
                n += cell.Count;
            }
            return n;
        }

        /// <summary>
        /// Checks that all the cells are singletons - that is, they only have one
        /// element. A discrete partition is equivalent to a permutation.
        /// </summary>
        /// <returns>true if all the cells are discrete</returns>
        public bool IsDiscrete()
        {
            foreach (var cell in cells)
            {
                if (cell.Count != 1)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Converts the whole partition into a permutation.
        /// </summary>
        /// <returns>the partition as a permutation</returns>
        public Permutation ToPermutation()
        {
            Permutation p = new Permutation(this.Count);
            for (int i = 0; i < this.Count; i++)
            {
                p[i] = this.cells[i].First();
            }
            return p;
        }

        /// <summary>
        /// Check whether the cells are ordered such that for cells i and j,
        /// First(j)  &gt; First(i) and Last(j)  &gt; Last(i).
        /// </summary>
        /// <returns>true if all cells in the partition are ordered</returns>
        public bool InOrder()
        {
            SortedSet<int> prev = null;
            foreach (var cell in cells)
            {
                if (prev == null)
                {
                    prev = cell;
                }
                else
                {
                    int first = cell.First();
                    int last = cell.Last();
                    if (first > prev.First() && last > prev.Last())
                    {
                        prev = cell;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the first element in the specified cell.
        /// </summary>
        /// <param name="cellIndex">the cell to use</param>
        /// <returns>the first element in this cell</returns>
        public int GetFirstInCell(int cellIndex)
        {
            return this.cells[cellIndex].First();
        }

        /// <summary>
        /// Gets the cell at this index.
        /// </summary>
        /// <param name="cellIndex">the index of the cell to return</param>
        /// <returns>the cell at this index</returns>
        public SortedSet<int> GetCell(int cellIndex)
        {
            return this.cells[cellIndex];
        }

        /// <summary>
        /// Splits this partition by taking the cell at cellIndex and making two
        /// new cells - the first with the singleton splitElement and the second
        /// with the rest of the elements from that cell.
        /// </summary>
        /// <param name="cellIndex">the index of the cell to split on</param>
        /// <param name="splitElement">the element to put in its own cell</param>
        /// <returns>a new (finer) Partition</returns>
        public Partition SplitBefore(int cellIndex, int splitElement)
        {
            Partition r = new Partition();
            // copy the cells up to cellIndex
            for (int j = 0; j < cellIndex; j++)
            {
                r.AddCell(this.CopyBlock(j));
            }

            // split the block at block index
            r.AddSingletonCell(splitElement);
            SortedSet<int> splitBlock = this.CopyBlock(cellIndex);
            splitBlock.Remove(splitElement);
            r.AddCell(splitBlock);

            // copy the blocks after blockIndex, shuffled up by one
            for (int j = cellIndex + 1; j < this.Count; j++)
            {
                r.AddCell(this.CopyBlock(j));
            }
            return r;
        }

        /// <summary>
        /// Splits this partition by taking the cell at cellIndex and making two
        /// new cells - the first with the rest of the elements from that cell
        /// and the second with the singleton splitElement.
        /// </summary>
        /// <param name="cellIndex">the index of the cell to split on</param>
        /// <param name="splitElement">the element to put in its own cell</param>
        /// <returns>a new (finer) Partition</returns>
        public Partition SplitAfter(int cellIndex, int splitElement)
        {
            Partition r = new Partition();
            // copy the blocks up to blockIndex
            for (int j = 0; j < cellIndex; j++)
            {
                r.AddCell(this.CopyBlock(j));
            }

            // split the block at block index
            SortedSet<int> splitBlock = this.CopyBlock(cellIndex);
            splitBlock.Remove(splitElement);
            r.AddCell(splitBlock);
            r.AddSingletonCell(splitElement);

            // copy the blocks after blockIndex, shuffled up by one
            for (int j = cellIndex + 1; j < this.Count; j++)
            {
                r.AddCell(this.CopyBlock(j));
            }
            return r;
        }

        /// <summary>
        /// Fill the elements of a permutation from the first element of each
        /// cell, up to the point <paramref name="upTo"/>.
        /// </summary>
        /// <param name="upTo">take values from cells up to this one</param>
        /// <returns>the permutation representing the first element of each cell</returns>
        public Permutation SetAsPermutation(int upTo)
        {
            int[] p = new int[upTo];
            for (int i = 0; i < upTo; i++)
            {
                p[i] = this.cells[i].First();
            }
            return new Permutation(p);
        }

        /// <summary>
        /// Check to see if the cell at <paramref name="cellIndex"/> is discrete - that is,
        /// it only has one element.
        /// </summary>
        /// <param name="cellIndex">the index of the cell to check</param>
        /// <returns>true of the cell at this index is discrete</returns>
        public bool IsDiscreteCell(int cellIndex)
        {
            return this.cells[cellIndex].Count == 1;
        }

        /// <summary>
        /// Gets the index of the first cell in the partition that is discrete.
        /// </summary>
        /// <returns>the index of the first discrete cell</returns>
        public int GetIndexOfFirstNonDiscreteCell()
        {
            for (int i = 0; i < this.cells.Count; i++)
            {
                if (!IsDiscreteCell(i)) return i;
            }
            return -1; // XXX
        }

        /// <summary>
        /// Add a new singleton cell to the end of the partition containing only
        /// this element.
        /// </summary>
        /// <param name="element">the element to add in its own cell</param>
        public void AddSingletonCell(int element)
        {
            SortedSet<int> cell = new SortedSet<int>
            {
                element
            };
            this.cells.Add(cell);
        }

        /// <summary>
        /// Removes the cell at the specified index.
        /// </summary>
        /// <param name="index">the index of the cell to remove</param>
        public void RemoveCell(int index)
        {
            this.cells.RemoveAt(index);
        }

        /// <summary>
        /// Adds a new cell to the end of the partition containing these elements.
        /// </summary>
        /// <param name="elements">the elements to add in a new cell</param>
        public void AddCell(params int[] elements)
        {
            SortedSet<int> cell = new SortedSet<int>();
            foreach (var element in elements)
            {
                cell.Add(element);
            }
            this.cells.Add(cell);
        }

        /// <summary>
        /// Adds a new cell to the end of the partition.
        /// </summary>
        /// <param name="elements">the collection of elements to put in the cell</param>
        public void AddCell(ICollection<int> elements)
        {
            cells.Add(new SortedSet<int>(elements));
        }

        /// <summary>
        /// Add an element to a particular cell.
        /// </summary>
        /// <param name="index">the index of the cell to add to</param>
        /// <param name="element">the element to add</param>
        public void AddToCell(int index, int element)
        {
            if (cells.Count < index + 1)
            {
                AddSingletonCell(element);
            }
            else
            {
                cells[index].Add(element);
            }
        }

        /// <summary>
        /// Insert a cell into the partition at the specified index.
        /// </summary>
        /// <param name="index">the index of the cell to add</param>
        /// <param name="cell">the cell to add</param>
        public void InsertCell(int index, SortedSet<int> cell)
        {
            this.cells.Insert(index, cell);
        }

        /// <summary>
        /// Creates and returns a copy of the cell at cell index.
        /// </summary>
        /// <param name="cellIndex">the cell to copy</param>
        /// <returns>the copy of the cell</returns>
        public SortedSet<int> CopyBlock(int cellIndex)
        {
            return new SortedSet<int>(this.cells[cellIndex]);
        }

        /// <summary>
        /// Sort the cells in increasing order.
        /// </summary>
        public void Order()
        {
            cells.Sort(delegate (SortedSet<int> cellA, SortedSet<int> cellB) { return cellA.First().CompareTo(cellB.First()); });
        }

        /// <summary>
        /// Check that two elements are in the same cell of the partition.
        /// </summary>
        /// <param name="elementI">an element in the partition</param>
        /// <param name="elementJ">an element in the partition</param>
        /// <returns>true if both elements are in the same cell</returns>
        public bool InSameCell(int elementI, int elementJ)
        {
            for (int cellIndex = 0; cellIndex < Count; cellIndex++)
            {
                SortedSet<int> cell = GetCell(cellIndex);
                if (cell.Contains(elementI) && cell.Contains(elementJ))
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('[');
            for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
            {
                SortedSet<int> cell = cells[cellIndex];
                int elementIndex = 0;
                foreach (var element in cell)
                {
                    sb.Append(element);
                    if (cell.Count > 1 && elementIndex < cell.Count - 1)
                    {
                        sb.Append(',');
                    }
                    elementIndex++;
                }
                if (cells.Count > 1 && cellIndex < cells.Count - 1)
                {
                    sb.Append('|');
                }
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// Parse a string like "[0,2|1,3]" to form the partition; cells are
        /// separated by '|' characters and elements within the cell by commas.
        /// </summary>
        /// <param name="strForm">the partition in string form</param>
        /// <returns>the partition corresponding to the string</returns>
        /// <exception cref="ArgumentException">thrown if the provided strFrom is null or empty</exception>
        public static Partition FromString(string strForm)
        {
            if (strForm == null || strForm.Length == 0) throw new ArgumentException("null or empty string provided");

            Partition p = new Partition();
            int index = 0;
            if (strForm[0] == '[')
            {
                index++;
            }
            int endIndex;
            if (strForm[strForm.Length - 1] == ']')
            {
                endIndex = strForm.Length - 2;
            }
            else
            {
                endIndex = strForm.Length - 1;
            }
            int currentCell = -1;
            int numStart = -1;
            while (index <= endIndex)
            {
                char c = strForm[index];
                if (char.IsDigit(c))
                {
                    if (numStart == -1)
                    {
                        numStart = index;
                    }
                }
                else if (c == ',')
                {
                    int element = int.Parse(strForm.Substring(numStart, index - numStart), NumberFormatInfo.InvariantInfo);
                    if (currentCell == -1)
                    {
                        p.AddCell(element);
                        currentCell = 0;
                    }
                    else
                    {
                        p.AddToCell(currentCell, element);
                    }
                    numStart = -1;
                }
                else if (c == '|')
                {
                    int element = int.Parse(strForm.Substring(numStart, index - numStart), NumberFormatInfo.InvariantInfo);
                    if (currentCell == -1)
                    {
                        p.AddCell(element);
                        currentCell = 0;
                    }
                    else
                    {
                        p.AddToCell(currentCell, element);
                    }
                    currentCell++;
                    p.AddCell();
                    numStart = -1;
                }
                index++;
            }
            int lastElement = int.Parse(strForm.Substring(numStart, endIndex + 1 - numStart), NumberFormatInfo.InvariantInfo);
            p.AddToCell(currentCell, lastElement);
            return p;
        }
    }
}

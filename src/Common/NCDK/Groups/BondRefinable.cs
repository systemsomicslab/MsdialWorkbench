/* Copyright (C) 2017  Gilleain Torrance <gilleain.torrance@gmail.com>
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
using System.Globalization;

namespace NCDK.Groups
{
    /// <summary>
    /// Wraps an atom container to provide information on the bond connectivity.
    /// </summary>
    // @author maclean
    // @cdk.module group  
    class BondRefinable : IRefinable
    {
        private readonly IAtomContainer atomContainer;

        /// <summary>
        /// The connectivity between bonds; two bonds are connected
        /// if they share an atom.
        /// </summary>
        private int[][] connectionTable;

        /// <summary>
        /// Specialised option to allow generating automorphisms that ignore the bond order.
        /// </summary>
        private readonly bool ignoreBondOrders;

        public BondRefinable(IAtomContainer atomContainer) : this(atomContainer, false)
        {
        }

        public BondRefinable(IAtomContainer atomContainer, bool ignoreBondOrders)
        {
            this.atomContainer = atomContainer;
            this.ignoreBondOrders = ignoreBondOrders;
            SetupConnectionTable(atomContainer);
        }

        public virtual int GetVertexCount()
        {
            return atomContainer.Bonds.Count;
        }

        public virtual int GetConnectivity(int vertexI, int vertexJ)
        {
            var maxRowIndex = connectionTable[vertexI].Length;
            for (int indexInRow = 0; indexInRow < maxRowIndex; indexInRow++)
            {
                if (connectionTable[vertexI][indexInRow] == vertexJ)
                {
                    return 1;
                }
            }
            return 0;
        }

        public virtual IInvariant NeighboursInBlock(ISet<int> block, int vertexIndex)
        {
            int neighbours = 0;
            foreach (int connected in GetConnectedIndices(vertexIndex))
            {
                if (block.Contains(connected))
                {
                    neighbours++;
                }
            }
            return new IntegerInvariant(neighbours);
        }

        private int[] GetConnectedIndices(int vertexIndex)
        {
            return connectionTable[vertexIndex];
        }

        /// <summary>
        /// Get the bond partition, based on the element types of the atoms at either end
        /// of the bond, and the bond order.
        /// </summary>
        /// <returns>a partition of the bonds based on the element types and bond order</returns>
        public Partition GetInitialPartition()
        {
            var bondCount = atomContainer.Bonds.Count;
            var cellMap = new Dictionary<string, SortedSet<int>>();

            // make mini-'descriptors' for bonds like "C=O" or "C#N" etc
            for (int bondIndex = 0; bondIndex < bondCount; bondIndex++)
            {
                var bond = atomContainer.Bonds[bondIndex];
                var el0 = bond.Atoms[0].Symbol;
                var el1 = bond.Atoms[1].Symbol;
                string boS;
                if (ignoreBondOrders)
                {
                    // doesn't matter what it is, so long as it's constant
                    boS = "1";
                }
                else
                {
                    var isArom = bond.IsAromatic;
                    var orderNumber = isArom ? 5 : bond.Order.Numeric();
                    boS = orderNumber.ToString(NumberFormatInfo.InvariantInfo);
                }
                string bondString;
                if (string.CompareOrdinal(el0, el1) < 0)
                {
                    bondString = el0 + boS + el1;
                }
                else
                {
                    bondString = el1 + boS + el0;
                }
                SortedSet<int> cell;
                if (cellMap.ContainsKey(bondString))
                {
                    cell = cellMap[bondString];
                }
                else
                {
                    cell = new SortedSet<int>();
                    cellMap[bondString] = cell;
                }
                cell.Add(bondIndex);
            }

            // sorting is necessary to get cells in order
            var bondStrings = new List<string>(cellMap.Keys);
            bondStrings.Sort();

            // the partition of the bonds by these 'descriptors'
            var bondPartition = new Partition();
            foreach (string key in bondStrings)
            {
                var cell = cellMap[key];
                bondPartition.AddCell(cell);
            }
            bondPartition.Order();
            return bondPartition;
        }

        private void SetupConnectionTable(IAtomContainer atomContainer)
        {
            var bondCount = atomContainer.Bonds.Count;
            // unfortunately, we have to sort the bonds
            var bonds = new List<IBond>();
            var bondMap = new Dictionary<string, IBond>();
            for (int bondIndexI = 0; bondIndexI < bondCount; bondIndexI++)
            {
                var bond = atomContainer.Bonds[bondIndexI];
                bonds.Add(bond);
                var a0 = atomContainer.Atoms.IndexOf(bond.Atoms[0]);
                var a1 = atomContainer.Atoms.IndexOf(bond.Atoms[1]);
                string boS;
                if (ignoreBondOrders)
                {
                    // doesn't matter what it is, so long as it's constant
                    boS = "1";
                }
                else
                {
                    var isArom = bond.IsAromatic;
                    var orderNumber = isArom ? 5 : bond.Order.Numeric();
                    boS = orderNumber.ToString(NumberFormatInfo.InvariantInfo);
                }
                string bondString;
                if (a0 < a1)
                {
                    bondString = a0 + "," + boS + "," + a1;
                }
                else
                {
                    bondString = a1 + "," + boS + "," + a0;
                }
                bondMap[bondString] = bond;
            }

            var keys = new List<string>(bondMap.Keys);
            keys.Sort();
            foreach (string key in keys)
            {
                bonds.Add(bondMap[key]);
            }

            connectionTable = new int[bondCount][];
            for (int bondIndexI = 0; bondIndexI < bondCount; bondIndexI++)
            {
                var bondI = bonds[bondIndexI];
                var connectedBondIndices = new List<int>();
                for (int bondIndexJ = 0; bondIndexJ < bondCount; bondIndexJ++)
                {
                    if (bondIndexI == bondIndexJ)
                        continue;
                    var bondJ = bonds[bondIndexJ];
                    if (bondI.IsConnectedTo(bondJ))
                    {
                        connectedBondIndices.Add(bondIndexJ);
                    }
                }
                var connBondCount = connectedBondIndices.Count;
                connectionTable[bondIndexI] = new int[connBondCount];
                for (int index = 0; index < connBondCount; index++)
                {
                    connectionTable[bondIndexI][index] = connectedBondIndices[index];
                }
            }
        }
    }
}

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

namespace NCDK.Groups
{
    /// <summary>
    /// Wraps an atom container to provide information on the atom connectivity.
    /// </summary>
    // @author maclean
    // @cdk.module group  
    class AtomRefinable : IRefinable
    {
        private readonly IAtomContainer atomContainer;

        /// <summary>
        /// A convenience lookup table for atom-atom connections.
        /// </summary>
        private int[][] connectionTable;

        /// <summary>
        /// A convenience lookup table for bond orders.
        /// </summary>
        private int[][] bondOrders;

        /// <summary>
        /// Ignore the elements when creating the initial partition.
        /// </summary>
        private readonly bool ignoreElements;

        /// <summary>
        /// Specialised option to allow generating automorphisms
        /// that ignore the bond order.
        /// </summary>
        private readonly bool ignoreBondOrders;

        private int maxBondOrder;

        /// <summary>
        /// Create a refinable from an atom container with flags set to false.
        /// </summary>
        /// <param name="atomContainer">the atom and bond data</param>
        public AtomRefinable(IAtomContainer atomContainer) 
            : this(atomContainer, false, false)
        {
        }

        /// <summary>
        /// Create a refinable from an atom container with supplied flags.
        /// </summary>
        /// <param name="atomContainer">the atom and bond data</param>
        /// <param name="ignoreElements"></param>
        /// <param name="ignoreBondOrders"></param>
        public AtomRefinable(IAtomContainer atomContainer, bool ignoreElements, bool ignoreBondOrders)
        {
            this.atomContainer = atomContainer;
            this.ignoreElements = ignoreElements;
            this.ignoreBondOrders = ignoreBondOrders;
            SetupConnectionTable(atomContainer);
        }

        public virtual IInvariant NeighboursInBlock(ISet<int> block, int vertexIndex)
        {
            // choose the invariant to use 
            if (ignoreBondOrders || maxBondOrder == 1)
            {
                return GetSimpleInvariant(block, vertexIndex);
            }
            else
            {
                return GetMultipleInvariant(block, vertexIndex);
            }
        }

        /// <returns>a simple count of the neighbours of vertexIndex that are in block</returns>
        private IInvariant GetSimpleInvariant(ISet<int> block, int vertexIndex)
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

        /// <returns>a list of bond orders of connections to neighbours of vertexIndex that are in block</returns>
        private IInvariant GetMultipleInvariant(ISet<int> block, int vertexIndex)
        {
            int[] bondOrderCounts = new int[maxBondOrder];
            foreach (int connected in GetConnectedIndices(vertexIndex))
            {
                if (block.Contains(connected))
                {
                    int bondOrder = GetConnectivity(vertexIndex, connected);
                    bondOrderCounts[bondOrder - 1]++;
                }
            }
            return new IntegerListInvariant(bondOrderCounts);
        }

        public virtual int GetVertexCount()
        {
            return atomContainer.Atoms.Count;
        }

        public virtual int GetConnectivity(int vertexI, int vertexJ)
        {
            int indexInRow;
            int maxRowIndex = connectionTable[vertexI].Length;
            for (indexInRow = 0; indexInRow < maxRowIndex; indexInRow++)
            {
                if (connectionTable[vertexI][indexInRow] == vertexJ)
                {
                    break;
                }
            }
            if (ignoreBondOrders)
            {
                if (indexInRow < maxRowIndex)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                if (indexInRow < maxRowIndex)
                {
                    return bondOrders[vertexI][indexInRow];
                }
                else
                {
                    return 0;
                }
            }
        }

        private int[] GetConnectedIndices(int vertexIndex)
        {
            return connectionTable[vertexIndex];
        }

        /// <summary>
        /// Get the element partition from an atom container, which is simply a list
        /// of sets of atom indices where all atoms in one set have the same element
        /// symbol.
        /// 
        /// So for atoms [C0, N1, C2, P3, C4, N5] the partition would be
        /// [{0, 2, 4}, {1, 5}, {3}] with cells for elements C, N, and P.
        /// </summary>
        /// <returns>a partition of the atom indices based on the element symbols</returns>
        public virtual Partition GetInitialPartition()
        {
            if (ignoreElements)
            {
                int n = atomContainer.Atoms.Count;
                return Partition.Unit(n);
            }

            var cellMap = new Dictionary<string, SortedSet<int>>();
            int numberOfAtoms = atomContainer.Atoms.Count;
            for (int atomIndex = 0; atomIndex < numberOfAtoms; atomIndex++)
            {
                string symbol = atomContainer.Atoms[atomIndex].Symbol;
                SortedSet<int> cell;
                if (cellMap.ContainsKey(symbol))
                {
                    cell = cellMap[symbol];
                }
                else
                {
                    cell = new SortedSet<int>();
                    cellMap[symbol] = cell;
                }
                cell.Add(atomIndex);
            }

            var atomSymbols = new List<string>(cellMap.Keys);
            atomSymbols.Sort();

            var elementPartition = new Partition();
            foreach (string key in atomSymbols)
            {
                var cell = cellMap[key];
                elementPartition.AddCell(cell);
            }

            return elementPartition;
        }

        /// <summary>
        /// Makes a lookup table for the connection between atoms, to avoid looking
        /// through the bonds each time.
        /// </summary>
        /// <param name="atomContainer">the atom</param>
        private void SetupConnectionTable(IAtomContainer atomContainer)
        {
            var atomCount = atomContainer.Atoms.Count;
            connectionTable = new int[atomCount][];
            if (!ignoreBondOrders)
            {
                bondOrders = new int[atomCount][];
            }
            for (int atomIndex = 0; atomIndex < atomCount; atomIndex++)
            {
                var atom = atomContainer.Atoms[atomIndex];
                var connectedAtoms = atomContainer.GetConnectedAtoms(atom).ToReadOnlyList();
                var numConnAtoms = connectedAtoms.Count;
                connectionTable[atomIndex] = new int[numConnAtoms];
                if (!ignoreBondOrders)
                {
                    bondOrders[atomIndex] = new int[numConnAtoms];
                }
                int i = 0;
                foreach (IAtom connected in connectedAtoms)
                {
                    var index = atomContainer.Atoms.IndexOf(connected);
                    connectionTable[atomIndex][i] = index;
                    if (!ignoreBondOrders)
                    {
                        var bond = atomContainer.GetBond(atom, connected);
                        var isArom = bond.IsAromatic;
                        var orderNumber = isArom ? 5 : bond.Order.Numeric();
                        bondOrders[atomIndex][i] = orderNumber;

                        // TODO
                        if (orderNumber > maxBondOrder)
                        {
                            maxBondOrder = orderNumber;
                        }
                    }
                    i++;
                }
            }
        }
    }
}

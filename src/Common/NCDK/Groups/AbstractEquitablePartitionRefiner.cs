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

using System.Collections.Generic;

namespace NCDK.Groups
{
    /// <summary>
    /// Refines a 'coarse' partition (with more blocks) to a 'finer' partition that
    /// is equitable.
    /// </summary>
    /// <remarks>
    /// Closely follows algorithm 7.5 in CAGES <token>cdk-cite-Kreher98</token>. The basic idea is that the refiner
    /// maintains a queue of blocks to refine, starting with all the initial blocks
    /// in the partition to refine. These blocks are popped off the queue, and
    /// </remarks>
    // @author maclean
    // @cdk.module group
    public abstract class AbstractEquitablePartitionRefiner
    {
        /// <summary>
        /// A forward split order tends to favor partitions where the cells are
        /// refined from lowest to highest. A reverse split order is, of course, the
        /// opposite.
        /// </summary>
        public enum SplitOrder
        {
            Forward, Reverse
        }

        /// <summary>
        /// The bias in splitting cells when refining
        /// </summary>
        private SplitOrder splitOrder = SplitOrder.Forward;

        /// <summary>
        /// The block of the partition that is being refined
        /// </summary>
        private int currentBlockIndex;

        /// <summary>
        /// The blocks to be refined, or at least considered for refinement
        /// </summary>
        private Queue<SortedSet<int>> blocksToRefine;

        /// <summary>
        /// Gets from the graph the number of vertices. Abstract to allow different
        /// graph classes to be used (eg: Graph or IAtomContainer, etc).
        /// </summary>
        /// <returns>the number of vertices</returns>
        public abstract int GetVertexCount();

        /// <summary>
        /// Find |a Åø b| - that is, the size of the intersection between a and b.
        /// </summary>
        /// <param name="block">a set of numbers</param>
        /// <param name="vertexIndex">the element to compare</param>
        /// <returns>the size of the intersection</returns>
        public abstract int NeighboursInBlock(ISet<int> block, int vertexIndex);

        /// <summary>
        /// Set the preference for splitting cells.
        /// </summary>
        /// <param name="splitOrder">either <see cref="SplitOrder.Forward"/>  or <see cref="SplitOrder.Reverse"/></param>
        public void SetSplitOrder(SplitOrder splitOrder)
        {
            this.splitOrder = splitOrder;
        }

        /// <summary>
        /// Refines the coarse partition "a" into a finer one.
        /// </summary>
        /// <param name="coarser">the partition to refine</param>
        /// <returns>a finer partition</returns>
        public Partition Refine(Partition coarser)
        {
            var finer = new Partition(coarser);

            // start the queue with the blocks of a in reverse order
            blocksToRefine = new Queue<SortedSet<int>>();
            for (int i = 0; i < finer.Count; i++)
            {
                blocksToRefine.Enqueue(finer.CopyBlock(i));
            }

            var numberOfVertices = GetVertexCount();
            while (blocksToRefine.Count != 0)
            {
                var t = blocksToRefine.Dequeue();
                currentBlockIndex = 0;
                while (currentBlockIndex < finer.Count && finer.Count < numberOfVertices)
                {
                    if (!finer.IsDiscreteCell(currentBlockIndex))
                    {

                        // get the neighbor invariants for this block
                        var invariants = GetInvariants(finer, t);

                        // split the block on the basis of these invariants
                        Split(invariants, finer);
                    }
                    currentBlockIndex++;
                }

                // the partition is discrete
                if (finer.Count == numberOfVertices)
                {
                    return finer;
                }
            }
            return finer;
        }

        /// <summary>
        /// Gets the neighbor invariants for the block j as a map of
        /// |N<sub>g</sub>(v) Åø T| to elements of the block j. That is, the
        /// size of the intersection between the set of neighbors of element v in
        /// the graph and the target block T.
        /// </summary>
        /// <param name="partition">the current partition</param>
        /// <param name="targetBlock">the current target block of the partition</param>
        /// <returns>a map of set intersection sizes to elements</returns>
        private Dictionary<int, SortedSet<int>> GetInvariants(Partition partition, SortedSet<int> targetBlock)
        {
            var setList = new Dictionary<int, SortedSet<int>>();
            foreach (var u in partition.GetCell(currentBlockIndex))
            {
                var h = NeighboursInBlock(targetBlock, u);
                if (setList.ContainsKey(h))
                {
                    setList[h].Add(u);
                }
                else
                {
                    var set = new SortedSet<int>
                    {
                        u
                    };
                    setList[h] = set;
                }
            }
            return setList;
        }

        /// <summary>
        /// Split the current block using the invariants calculated in getInvariants.
        /// </summary>
        /// <param name="invariants">a map of neighbor counts to elements</param>
        /// <param name="partition">the partition that is being refined</param>
        private void Split(Dictionary<int, SortedSet<int>> invariants, Partition partition)
        {
            var nonEmptyInvariants = invariants.Keys.Count;
            if (nonEmptyInvariants > 1)
            {
                var invariantKeys = new List<int>();
                invariantKeys.AddRange(invariants.Keys);
                partition.RemoveCell(currentBlockIndex);
                int k = currentBlockIndex;
                if (splitOrder == SplitOrder.Reverse)
                {
                    invariantKeys.Sort();
                }
                else
                {
                    invariantKeys.Sort();
                    invariantKeys.Reverse();
                }
                foreach (var h in invariantKeys)
                {
                    var setH = invariants[h];
                    partition.InsertCell(k, setH);
                    blocksToRefine.Enqueue(setH);
                    k++;

                }
                // skip over the newly added blocks
                currentBlockIndex += nonEmptyInvariants - 1;
            }
        }
    }
}

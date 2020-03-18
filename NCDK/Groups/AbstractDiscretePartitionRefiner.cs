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

using System.Text;

namespace NCDK.Groups
{
    /// <summary>
    /// Refines vertex partitions until they are discrete, and therefore equivalent
    /// to permutations. These permutations are automorphisms of the graph that was
    /// used during the refinement to guide the splitting of partition blocks.
    /// </summary>
    // @author maclean
    // @cdk.module group
    public abstract class AbstractDiscretePartitionRefiner 
        : IDiscretePartitionRefiner
    {
        /// <summary>
        /// The result of a comparison between the current partition
        /// and the best permutation found so far.
        /// </summary>
        internal enum Result
        {
            Worse, Equal, Better
        }

        /// <summary>
        /// If true, then at least one partition has been refined
        /// to a permutation (IE extends to a discrete partition).
        /// </summary>
        private bool bestExist;

        /// <summary>
        /// The best permutation is the one that gives the maximal
        /// half-matrix string (so far) when applied to the graph.
        /// </summary>
        private Permutation best;

        /// <summary>
        /// The first permutation seen when refining.
        /// </summary>
        private Permutation first;

        /// <summary>
        /// An equitable refiner.
        /// </summary>
        private EquitablePartitionRefiner equitableRefiner;

        /// <summary>
        /// The automorphism group that is used to prune the search.
        /// </summary>
        private PermutationGroup group;

        /// <summary>
        /// A refiner - it is necessary to call <see cref="Setup(PermutationGroup, EquitablePartitionRefiner)"/> before use.
        /// </summary>
        protected AbstractDiscretePartitionRefiner()
        {
            this.bestExist = false;
            this.best = null;
            this.equitableRefiner = null;
        }

        /// <summary>
        /// Get the number of vertices in the graph to be refined.
        /// </summary>
        /// <returns>a count of the vertices in the underlying graph</returns>
        protected internal abstract int GetVertexCount();

        /// <summary>
        /// Get the connectivity between two vertices as an integer, to allow
        /// for multigraphs : so a single edge is 1, a double edge 2, etc. If
        /// there is no edge, then 0 should be returned.
        /// </summary>
        /// <param name="vertexI">a vertex of the graph</param>
        /// <param name="vertexJ">a vertex of the graph</param>
        /// <returns>the multiplicity of the edge (0, 1, 2, 3, ...)</returns>
        protected internal abstract int GetConnectivity(int vertexI, int vertexJ);

        /// <summary>
        /// Setup the group and refiner; it is important to call this method before
        /// calling <see cref="Refine(Partition)"/>  otherwise the refinement process will fail.
        /// </summary>
        /// <param name="group">a group (possibly empty) of automorphisms</param>
        /// <param name="refiner">the equitable refiner</param>
        public void Setup(PermutationGroup group, EquitablePartitionRefiner refiner)
        {
            this.bestExist = false;
            this.best = null;
            this.group = group;
            this.equitableRefiner = refiner;
        }

        /// <summary>
        /// Check that the first refined partition is the identity.
        ///
        /// <returns>true if the first is the identity permutation</returns>
        /// </summary>
        public bool FirstIsIdentity()
        {
            return this.first.IsIdentity();
        }

        /// <summary>
        /// The automorphism partition is a partition of the elements of the group.
        ///
        /// <returns>a partition of the elements of group</returns>
        /// </summary>
        public Partition GetAutomorphismPartition()
        {
            int n = group.Count;
            DisjointSetForest forest = new DisjointSetForest(n);
            group.Apply(new AutomorphismPartitionBacktracker(n, forest));

            // convert to a partition
            Partition partition = new Partition();
            foreach (var set in forest.GetSets())
            {
                partition.AddCell(set);
            }

            // necessary for comparison by string
            partition.Order();
            return partition;
        }

        class AutomorphismPartitionBacktracker
            : IBacktracker
        {
            private readonly int n;
            DisjointSetForest forest;
            private readonly bool[] inOrbit;
            private int inOrbitCount = 0;
            private bool isFinished;

            public AutomorphismPartitionBacktracker(int n, DisjointSetForest forest)
            {
                this.n = n;
                this.forest = forest;
                inOrbit = new bool[n];
            }

            public bool IsFinished()
            {
                return isFinished;
            }

            public void ApplyTo(Permutation p)
            {
                for (int elementX = 0; elementX < n; elementX++)
                {
                    if (inOrbit[elementX])
                    {
                        continue;
                    }
                    else
                    {
                        int elementY = p[elementX];
                        while (elementY != elementX)
                        {
                            if (!inOrbit[elementY])
                            {
                                inOrbitCount++;
                                inOrbit[elementY] = true;
                                forest.MakeUnion(elementX, elementY);
                            }
                            elementY = p[elementY];
                        }
                    }
                }
                if (inOrbitCount == n)
                {
                    isFinished = true;
                }
            }
        }

        /// <summary>
        /// Get the upper-half of the adjacency matrix under the permutation.
        /// </summary>
        /// <param name="permutation">a permutation of the adjacency matrix</param>
        /// <returns>a string containing the permuted values of half the matrix</returns>
        public string GetHalfMatrixString(Permutation permutation)
        {
            StringBuilder builder = new StringBuilder(permutation.Count);
            int size = permutation.Count;
            for (int indexI = 0; indexI < size - 1; indexI++)
            {
                for (int indexJ = indexI + 1; indexJ < size; indexJ++)
                {
                    builder.Append(GetConnectivity(permutation[indexI], permutation[indexJ]));
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// Get the half-matrix string under the first permutation.
        /// </summary>
        /// <returns>the upper-half adjacency matrix string permuted by the first</returns>
        public string GetFirstHalfMatrixString()
        {
            return GetHalfMatrixString(first);
        }

        /// <summary>
        /// Get the initial (unpermuted) half-matrix string.
        /// </summary>
        /// <returns>the upper-half adjacency matrix string</returns>
        public string GetHalfMatrixString()
        {
            return GetHalfMatrixString(new Permutation(GetVertexCount()));
        }

        /// <summary>
        /// Get the automorphism group used to prune the search.
        /// </summary>
        /// <returns>the automorphism group</returns>
        public PermutationGroup GetAutomorphismGroup()
        {
            return this.group;
        }

        /// <summary>
        /// Get the best permutation found.
        /// </summary>
        /// <returns>the permutation that gives the maximal half-matrix string</returns>
        public Permutation GetBest()
        {
            return this.best;
        }

        /// <summary>
        /// Get the first permutation reached by the search.
        /// </summary>
        /// <returns>the first permutation reached</returns>
        public Permutation GetFirst()
        {
            return this.first;
        }

        /// <summary>
        /// Check for a canonical graph, without generating the whole
        /// automorphism group.
        /// </summary>
        /// <returns>true if the graph is canonical</returns>
        public bool IsCanonical()
        {
            return best.IsIdentity();
        }

        /// <summary>
        /// Refine the partition. The main entry point for subclasses.
        /// </summary>
        /// <param name="partition">the initial partition of the vertices</param>
        public void Refine(Partition partition)
        {
            Refine(this.group, partition);
        }

        /// <summary>
        /// Does the work of the class, that refines a coarse partition into a finer
        /// one using the supplied automorphism group to prune the search.
        /// </summary>
        /// <param name="group">the automorphism group of the graph</param>
        /// <param name="coarser">the partition to refine</param>
        private void Refine(PermutationGroup group, Partition coarser)
        {
            int vertexCount = GetVertexCount();

            Partition finer = equitableRefiner.Refine(coarser);

            int firstNonDiscreteCell = finer.GetIndexOfFirstNonDiscreteCell();
            if (firstNonDiscreteCell == -1)
            {
                firstNonDiscreteCell = vertexCount;
            }

            Permutation pi1 = new Permutation(firstNonDiscreteCell);

            Result result = Result.Better;
            if (bestExist)
            {
                pi1 = finer.SetAsPermutation(firstNonDiscreteCell);
                result = CompareRowwise(pi1);
            }

            // partition is discrete
            if (finer.Count == vertexCount)
            {
                if (!bestExist)
                {
                    best = finer.ToPermutation();
                    first = finer.ToPermutation();
                    bestExist = true;
                }
                else
                {
                    if (result == Result.Better)
                    {
                        best = new Permutation(pi1);
                    }
                    else if (result == Result.Equal)
                    {
                        group.Enter(pi1.Multiply(best.Invert()));
                    }
                }
            }
            else
            {
                if (result != Result.Worse)
                {
                    var blockCopy = finer.CopyBlock(firstNonDiscreteCell);
                    for (int vertexInBlock = 0; vertexInBlock < vertexCount; vertexInBlock++)
                    {
                        if (blockCopy.Contains(vertexInBlock))
                        {
                            Partition nextPartition = finer.SplitBefore(firstNonDiscreteCell, vertexInBlock);

                            this.Refine(group, nextPartition);

                            int[] permF = new int[vertexCount];
                            int[] invF = new int[vertexCount];
                            for (int i = 0; i < vertexCount; i++)
                            {
                                permF[i] = i;
                                invF[i] = i;
                            }

                            for (int j = 0; j <= firstNonDiscreteCell; j++)
                            {
                                int x = nextPartition.GetFirstInCell(j);
                                int i = invF[x];
                                int h = permF[j];
                                permF[j] = x;
                                permF[i] = h;
                                invF[h] = i;
                                invF[x] = j;
                            }
                            Permutation pPermF = new Permutation(permF);
                            group.ChangeBase(pPermF);
                            for (int j = 0; j < vertexCount; j++)
                            {
                                Permutation g = group[firstNonDiscreteCell, j];
                                if (g != null)
                                {
                                    blockCopy.Remove(g[vertexInBlock]);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check a permutation to see if it is better, equal, or worse than the
        /// current best.
        /// </summary>
        /// <param name="perm">the permutation to check</param>
        /// <returns><see cref="Result.Better"/>, <see cref="Result.Equal"/>, or <see cref="Result.Worse"/></returns>
        private Result CompareRowwise(Permutation perm)
        {
            int m = perm.Count;
            for (int i = 0; i < m - 1; i++)
            {
                for (int j = i + 1; j < m; j++)
                {
                    int x = GetConnectivity(best[i], best[j]);
                    int y = GetConnectivity(perm[i], perm[j]);
                    if (x > y) return Result.Worse;
                    if (x < y) return Result.Better;
                }
            }
            return Result.Equal;
        }
    }
}

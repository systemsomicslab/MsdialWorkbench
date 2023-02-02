/*
 * Copyright (c) 2014 European Bioinformatics Institute (EMBL-EBI)
 *                    John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Common.Collections;
using NCDK.Groups;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Graphs
{
    /// <summary>
    /// Maximum matching in general graphs using Edmond's Blossom Algorithm
    /// <token>cdk-cite-Edmonds65</token>. <p/>
    /// </summary>
    /// <remarks>
    /// This implementation was adapted from D Eppstein's python implementation 
    /// (<see href="http://www.ics.uci.edu/~eppstein/PADS/CardinalityMatching.py">src</see>)
    /// providing efficient tree traversal and handling of blossoms.
    /// <see href="http://en.wikipedia.org/wiki/Blossom_algorithm">Blossom algorithm, Wikipedia</see>
    /// <see href="http://research.microsoft.com/apps/video/dl.aspx?id=171055">Presentation from Vazirani on his and Micali O(|E| * Sqrt(|V|)) algorithm</see>
    /// </remarks>
    // @author John May
    // @cdk.module standard
    internal sealed class EdmondsMaximumMatching
    {
        /// <summary>The graph we are matching on.</summary>
        private readonly int[][] graph;

        /// <summary>The current matching.</summary>
        private readonly Matching matching;

        /// <summary>Subset of vertices to be matched.</summary>
        private readonly BitArray subset;

        /* Algorithm data structures below. */

        /// <summary>Storage of the forest, even and odd levels</summary>
        private readonly int[] even, odd;

        /// <summary>Special 'nil' vertex.</summary>
        private const int NIL = -1;

        /// <summary>Queue of 'even' (free) vertices to start paths from.</summary>
        private readonly List<int> queue;

        /// <summary>Union-Find to store blossoms.</summary>
        private DisjointSetForest dsf;

        /// <summary>
        /// Dictionary stores the bridges of the blossom - indexed by with support vertices.
        /// </summary>
        private readonly Dictionary<int, Tuple> bridges = new Dictionary<int, Tuple>();

        /// <summary>Temporary array to fill with path information.</summary>
        private readonly int[] path;

        /// <summary>
        /// Temporary bit sets when walking down 'trees' to check for paths/blossoms.
        /// </summary>
        private readonly BitArray vAncestors, wAncestors;

        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <param name="graph">adjacency list graph representation</param>
        /// <param name="matching">the matching of the graph</param>
        /// <param name="subset">subset a subset of vertices</param>
        private EdmondsMaximumMatching(int[][] graph, Matching matching, BitArray subset)
        {
            this.graph = graph;
            this.matching = matching;
            this.subset = subset;

            this.even = new int[graph.Length];
            this.odd = new int[graph.Length];

            this.queue = new List<int>();   // LinkedList
            this.dsf = new DisjointSetForest(graph.Length);

            // tmp storage of paths in the algorithm
            path = new int[graph.Length];
            vAncestors = new BitArray(graph.Length);
            wAncestors = new BitArray(graph.Length);

            // continuously augment while we find new paths
            while (ExistAugmentingPath())
                ;
        }

        /// <summary>
        /// Find an augmenting path an alternate it's matching. If an augmenting path
        /// was found then the search must be restarted. If a blossom was detected
        /// the blossom is contracted and the search continues.
        /// </summary>
        /// <returns>an augmenting path was found</returns>
        private bool ExistAugmentingPath()
        {
            // reset data structures
            Arrays.Fill(even, NIL);
            Arrays.Fill(odd, NIL);
            dsf = new DisjointSetForest(graph.Length);
            bridges.Clear();
            queue.Clear();

            // enqueue every unmatched vertex and place in the
            // even level (level = 0)
            for (int v = 0; v < graph.Length; v++)
            {
                if (subset[v] && matching.Unmatched(v))
                {
                    even[v] = v;
                    queue.Add(v);
                }
            }

            // for each 'free' vertex, start a bfs search
            while (queue.Count != 0)
            {
                var v = queue[0];
                queue.RemoveAt(0);

                foreach (var w in graph[v])
                {
                    if (!subset[w]) continue;

                    // the endpoints of the edge are both at even levels in the
                    // forest - this means it is either an augmenting path or
                    // a blossom
                    if (even[dsf.GetRoot(w)] != NIL)
                    {
                        if (Check(v, w)) return true;
                    }

                    // add the edge to the forest if is not already and extend
                    // the tree with this matched edge
                    else if (odd[w] == NIL)
                    {
                        odd[w] = v;
                        var u = matching.Other(w);
                        // add the matched edge (potential though a blossom) if it
                        // isn't in the forest already
                        if (even[dsf.GetRoot(u)] == NIL)
                        {
                            even[u] = w;
                            queue.Add(u);
                        }
                    }
                }
            }

            // no augmenting paths, matching is maximum
            return false;
        }

        /// <summary>
        /// An edge was found which connects two 'even' vertices in the forest. If
        /// the vertices have the same root we have a blossom otherwise we have
        /// identified an augmenting path. This method checks for these cases and
        /// responds accordingly. 
        /// </summary>
        /// <remarks>
        /// If an augmenting path was found - then it's edges are alternated and the
        /// method returns true. Otherwise if a blossom was found - it is contracted
        /// and the search continues.
        /// </remarks>
        /// <param name="v">endpoint of an edge</param>
        /// <param name="w">another endpoint of an edge</param>
        /// <returns>a path was augmented</returns>
        private bool Check(int v, int w)
        {
            // self-loop (within blossom) ignored
            if (dsf.GetRoot(v) == dsf.GetRoot(w)) return false;

            vAncestors.SetAll(false);
            wAncestors.SetAll(false);
            var vCurr = v;
            var wCurr = w;

            // walk back along the trees filling up 'vAncestors' and 'wAncestors'
            // with the vertices in the tree -  vCurr and wCurr are the 'even' parents
            // from v/w along the tree
            while (true)
            {
                vCurr = GetNExtEvenVertex(vAncestors, vCurr);
                wCurr = GetNExtEvenVertex(wAncestors, wCurr);

                // v and w lead to the same root - we have found a blossom. We
                // traveled all the way down the tree thus vCurr (and wCurr) are
                // the base of the blossom
                if (vCurr == wCurr)
                {
                    CreatebBlossom(v, w, vCurr);
                    return false;
                }

                // we are at the root of each tree and the roots are different, we
                // have found and augmenting path
                if (dsf.GetRoot(even[vCurr]) == vCurr && dsf.GetRoot(even[wCurr]) == wCurr)
                {
                    Augment(v);
                    Augment(w);
                    matching.Match(v, w);
                    return true;
                }

                // the current vertex in 'v' can be found in w's ancestors they must
                // share a root - we have found a blossom whose base is 'vCurr'
                if (wAncestors[vCurr])
                {
                    CreatebBlossom(v, w, vCurr);
                    return false;
                }

                // the current vertex in 'w' can be found in v's ancestors they must
                // share a root, we have found a blossom whose base is 'wCurr'
                if (vAncestors[wCurr])
                {
                    CreatebBlossom(v, w, wCurr);
                    return false;
                }
            }
        }

        /// <summary>
        /// Access the next ancestor in a tree of the forest. Note we go back two
        /// places at once as we only need check 'even' vertices.
        /// </summary>
        /// <param name="ancestors">temporary set which fills up the path we traversed</param>
        /// <param name="curr">the current even vertex in the tree</param>
        /// <returns>the next 'even' vertex</returns>
        private int GetNExtEvenVertex(BitArray ancestors, int curr)
        {
            curr = dsf.GetRoot(curr);
            ancestors.Set(curr, true);
            int parent = dsf.GetRoot(even[curr]);
            if (parent == curr)
                return curr; // root of tree
            ancestors.Set(parent, true);
            return dsf.GetRoot(odd[parent]);
        }

        /// <summary>
        /// Create a new blossom for the specified 'bridge' edge.
        /// </summary>
        /// <param name="v">adjacent to w</param>
        /// <param name="w">adjacent to v</param>
        /// <param name="base_">connected to the stem (common ancestor of <paramref name="v"/> and <paramref name="w"/>)</param>
        private void CreatebBlossom(int v, int w, int base_)
        {
            base_ = dsf.GetRoot(base_);
            var supports1 = BlossomSupports(v, w, base_);
            var supports2 = BlossomSupports(w, v, base_);

            for (int i = 0; i < supports1.Length; i++)
                dsf.MakeUnion(supports1[i], supports1[0]);
            for (int i = 0; i < supports2.Length; i++)
                dsf.MakeUnion(supports2[i], supports2[0]);

            even[dsf.GetRoot(base_)] = even[base_];
        }

        /// <summary>
        /// Creates the blossom 'supports' for the specified blossom 'bridge' edge
        /// (<paramref name="v"/>, <paramref name="w"/>). We travel down each side to the base of the blossom ('<paramref name="base_"/>')
        /// collapsing vertices and point any 'odd' vertices to the correct 'bridge'
        /// edge. We do this by indexing the birdie to each vertex in the 'bridges'
        /// map.
        /// </summary>
        /// <param name="v">an endpoint of the blossom bridge</param>
        /// <param name="w">another endpoint of the blossom bridge</param>
        /// <param name="base_">the base of the blossom</param>
        private int[] BlossomSupports(int v, int w, int base_)
        {
            int n = 0;
            path[n++] = dsf.GetRoot(v);
            var b = new Tuple(v, w);
            while (path[n - 1] != base_)
            {
                var u = even[path[n - 1]];
                path[n++] = u;
                this.bridges.Add(u, b);
                // contracting the blossom allows us to continue searching from odd
                // vertices (any odd vertices are now even - part of the blossom set)
                queue.Add(u);
                path[n++] = dsf.GetRoot(odd[u]);
            }

            return Arrays.CopyOf(path, n);
        }

        /// <summary>
        /// Augment all ancestors in the tree of vertex 'v'.
        /// </summary>
        /// <param name="v">the leaf to augment from</param>
        private void Augment(int v)
        {
            var n = BuildPath(path, 0, v, NIL);
            for (int i = 2; i < n; i += 2)
            {
                matching.Match(path[i], path[i - 1]);
            }
        }

        /// <summary>
        /// Builds the path backwards from the specified 'start' vertex until the
        /// 'goal'. If the path reaches a blossom then the path through the blossom
        /// is lifted to the original graph.
        /// </summary>
        /// <param name="path">path storage</param>
        /// <param name="i">offset (in path)</param>
        /// <param name="start">start vertex</param>
        /// <param name="goal">end vertex</param>
        /// <returns>the number of items set to the path[].</returns>
        private int BuildPath(int[] path, int i, int start, int goal)
        {
            while (true)
            {
                // lift the path through the contracted blossom
                while (odd[start] != NIL)
                {
                    var bridge = bridges[start];

                    // add to the path from the bridge down to where 'start'
                    // is - we need to reverse it as we travel 'up' the blossom
                    // and then...
                    var j = BuildPath(path, i, bridge.First, start);
                    Reverse(path, i, j - 1);
                    i = j;

                    // ... we travel down the other side of the bridge
                    start = bridge.Second;
                }
                path[i++] = start;

                // root of the tree
                if (matching.Unmatched(start))
                    return i;

                path[i++] = matching.Other(start);

                // end of recursive
                if (path[i - 1] == goal)
                    return i;

                start = odd[path[i - 1]];
            }
        }

        /// <summary>
        /// Reverse a section of a fixed size array.
        /// </summary>
        /// <param name="path">a path</param>
        /// <param name="i">start index</param>
        /// <param name="j">end index</param>
        private static void Reverse(int[] path, int i, int j)
        {
            while (i < j)
            {
                int tmp = path[i];
                path[i] = path[j];
                path[j] = tmp;
                i++;
                j--;
            }
        }

        /// <summary>
        /// Attempt to maximise the provided matching over a subset of vertices in a
        /// graph.
        /// </summary>
        /// <param name="matching">the independent edge set to maximise</param>
        /// <param name="graph">adjacency list graph representation</param>
        /// <param name="subset">subset of vertices</param>
        /// <returns>the matching</returns>
        public static Matching Maxamise(Matching matching, int[][] graph, BitArray subset)
        {
            new EdmondsMaximumMatching(graph, matching, subset);
            return matching;
        }

        /// <summary>
        /// Storage and indexing of a two int values.
        /// </summary>
        private sealed class Tuple
        {
            /// <summary>Values.</summary>
            public int First { get; private set; }
            public int Second { get; private set; }

            /// <summary>
            /// Create a new tuple.
            /// </summary>
            /// <param name="first">a value</param>
            /// <param name="second">another value</param>
            public Tuple(int first, int second)
            {
                First = first;
                Second = second;
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return 31 * First + Second;
            }

            /// <inheritdoc/>
            public override bool Equals(object o)
            {
                if (this == o)
                    return true;
                if (o == null || GetType() != o.GetType())
                    return false;
                var that = (Tuple)o;
                return this.First == that.First && this.Second == that.Second;
            }
        }
    }
}

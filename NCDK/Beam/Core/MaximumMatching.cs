using NCDK.Common.Collections;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Beam
{
    /// <summary>
    /// Maximum matching in general graphs using Edmond's Blossom Algorithm. 
    /// </summary>
    /// <para>
    /// This implementation was adapted D Eppstein's python code 
    /// (<see href="http://www.ics.uci.edu/~eppstein/PADS/CardinalityMatching.py">src</see>)
    /// which provides efficient tree traversal and handling of blossoms. The
    /// implementation may be quite daunting as a general introduction to the ideas.
    /// Personally I found <see href="http://www.keithschwarz.com/interesting/">Keith Schwarz</see> 
    /// version very informative when starting to understand the workings. 
    /// </para>
    /// <para>
    /// An asymptotically better algorithm is described by Micali and Vazirani (1980)
    /// and is similar to bipartite matching (<see href="http://en.wikipedia.org/wiki/Hopcroft%E2%80%93Karp_algorithm">Hopkroft-Karp</see>)
    /// where by multiple augmenting paths are discovered at once. In general though
    /// this version is very fast - particularly if given an existing matching to
    /// start from. Even the very simple <see cref="ArbitraryMatching"/> eliminates many
    /// loop iterations particularly at the start when all length 1 augmenting paths
    /// are discovered.
    /// </para>
    /// <seealso href="http://en.wikipedia.org/wiki/Blossom_algorithm">Blossom algorithm, Wikipedia</seealso>
    /// <seealso href="http://en.wikipedia.org/wiki/Hopcroft%E2%80%93Karp_algorithm">Hopkroft-Karp, Wikipedia</seealso>
    /// <seealso href="http://research.microsoft.com/apps/video/dl.aspx?id=171055">Presentation from Vazirani on his and Micali O(|E| * Sqrt(|V|)) algorithm</seealso>
    // @author John May
    internal sealed class MaximumMatching
    {
        /// <summary>The graph we are matching on.</summary>
        private readonly Graph graph;

        /// <summary>The current matching.</summary>
        private readonly Matching matching;

        /// <summary>Subset of vertices to be matched.</summary>
        private readonly IntSet subset;

        /* Algorithm data structures below. */

        /// <summary>Storage of the forest, even and odd levels</summary>
        private readonly int[] even, odd;

        /// <summary>Special 'nil' vertex.</summary>
        private const int nil = -1;

        /// <summary>Queue of 'even' (free) vertices to start paths from.</summary>
        private readonly FixedSizeQueue queue;

        /// <summary>Union-Find to store blossoms.</summary>
        private readonly UnionFind uf;

        /// <summary>
        /// Dictionary stores the bridges of the blossom - indexed by with support
        /// vertices.
        /// </summary>
        private readonly Dictionary<int, Tuple> bridges = new Dictionary<int, Tuple>();

        /// <summary>Temporary array to fill with path information.</summary>
        private readonly int[] path;

        /// <summary>
        /// Temporary bit sets when walking down 'trees' to check for
        /// paths/blossoms.
        /// </summary>
        private readonly BitArray vAncestors, wAncestors;

        /// <summary>Number of matched vertices. </summary>
        private readonly int nMatched;

        private MaximumMatching(Graph graph, Matching matching, int nMatched, IntSet subset)
        {
            this.graph = graph;
            this.matching = matching;
            this.subset = subset;

            this.even = new int[graph.Order];
            this.odd = new int[graph.Order];

            this.queue = new FixedSizeQueue(graph.Order);
            this.uf = new UnionFind(graph.Order);

            // tmp storage of paths in the algorithm
            path = new int[graph.Order];
            vAncestors = new BitArray(graph.Order);
            wAncestors = new BitArray(graph.Order);

            // continuously augment while we find new paths, each
            // path increases the matching cardinality by 2
            while (Augment())
            {
                nMatched += 2;
            }

            this.nMatched = nMatched;
        }

        /// <summary>
        /// Find an augmenting path an alternate it's matching. If an augmenting path
        /// was found then the search must be restarted. If a blossom was detected
        /// the blossom is contracted and the search continues.
        /// </summary>
        /// <returns>an augmenting path was found</returns>
        private bool Augment()
        {
            // reset data structures
            Arrays.Fill(even, nil);
            Arrays.Fill(odd, nil);
            uf.Clear();
            bridges.Clear();
            queue.Clear();

            // queue every unmatched vertex and place in the
            // even level (level = 0)        
            for (int v = 0; v < graph.Order; v++)
            {
                if (subset.Contains(v) && matching.Unmatched(v))
                {
                    even[v] = v;
                    queue.Enqueue(v);
                }
            }

            // for each 'free' vertex, start a bfs search
            while (!queue.IsEmpty())
            {
                int v = queue.Poll();

                int d = graph.Degree(v);
                for (int j = 0; j < d; ++j)
                {
                     Edge e = graph.EdgeAt(v, j);
                    if (e.Bond == Bond.Single)
                        continue;
                    int w = e.Other(v);

                    if (!subset.Contains(w))
                        continue;

                    // the endpoints of the edge are both at even levels in the                
                    // forest - this means it is either an augmenting path or
                    // a blossom
                    if (even[uf.Find(w)] != nil)
                    {
                        if (Check(v, w))
                            return true;
                    }

                    // add the edge to the forest if is not already and extend
                    // the tree with this matched edge
                    else if (odd[w] == nil)
                    {
                        odd[w] = v;
                        int u = matching.Other(w);
                        // add the matched edge (potential though a blossom) if it
                        // isn't in the forest already
                        if (even[uf.Find(u)] == nil)
                        {
                            even[u] = w;
                            queue.Enqueue(u);
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
        /// <para>
        /// If an augmenting path was found - then it's edges are alternated and the
        /// method returns true. Otherwise if a blossom was found - it is contracted
        /// and the search continues.
        /// </para>
        /// </summary>
        /// <param name="v">endpoint of an edge</param>
        /// <param name="w">another endpoint of an edge</param>
        /// <returns>a path was augmented</returns>
        private bool Check(int v, int w)
        {
            // self-loop (within blossom) ignored
            if (uf.Connected(v, w))
                return false;

            vAncestors.SetAll(false);
            wAncestors.SetAll(false);
            int vCurr = v;
            int wCurr = w;

            // walk back along the trees filling up 'vAncestors' and 'wAncestors'
            // with the vertices in the tree -  vCurr and wCurr are the 'even' parents
            // from v/w along the tree
            while (true)
            {
                vCurr = Parent(vAncestors, vCurr);
                wCurr = Parent(wAncestors, wCurr);

                // v and w lead to the same root - we have found a blossom. We
                // travelled all the way down the tree thus vCurr (and wCurr) are
                // the base of the blossom
                if (vCurr == wCurr)
                {
                    Blossom(v, w, vCurr);
                    return false;
                }

                // we are at the root of each tree and the roots are different, we
                // have found and augmenting path
                if (uf.Find(even[vCurr]) == vCurr && uf.Find(even[wCurr]) == wCurr)
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
                    Blossom(v, w, vCurr);
                    return false;
                }

                // the current vertex in 'w' can be found in v's ancestors they must
                // share a root, we have found a blossom whose base is 'wCurr'
                if (vAncestors[wCurr])
                {
                    Blossom(v, w, wCurr);
                    return false;
                }
            }
        }

        /// <summary>
        /// Access the next ancestor in a tree of the forest. Note we go back two
        /// places at once as we only need check 'even' vertices.
        /// </summary>
        /// <param name="ancestors">temporary set which fills up the path we traversed</param>
        /// <param name="curr">     the current even vertex in the tree</param>
        /// <returns>the next 'even' vertex</returns>
        private int Parent(BitArray ancestors, int curr)
        {
            curr = uf.Find(curr);
            ancestors.Set(curr, true);
            int parent = uf.Find(even[curr]);
            if (parent == curr)
                return curr; // root of tree       
            ancestors.Set(parent, true);
            return uf.Find(odd[parent]);
        }

        /// <summary>
        /// Create a new blossom for the specified 'bridge' edge.
        /// </summary>
        /// <param name="v">adjacent to w</param>
        /// <param name="w">adjacent to v</param>
        /// <param name="base_">connected to the stem (common ancestor of v and w)</param>
        private void Blossom(int v, int w, int base_)
        {
            base_ = uf.Find(base_);
            int[] supports1 = BlossomSupports(v, w, base_);
            int[] supports2 = BlossomSupports(w, v, base_);

            for (int i = 0; i < supports1.Length; i++)
                uf.Union(supports1[i], supports1[0]);
            for (int i = 0; i < supports2.Length; i++)
                uf.Union(supports2[i], supports2[0]);

            even[uf.Find(base_)] = even[base_];
        }

        /// <summary>
        /// Creates the blossom 'supports' for the specified blossom 'bridge' edge
        /// (v, w). We travel down each side to the base of the blossom ('base')
        /// collapsing vertices and point any 'odd' vertices to the correct 'bridge'
        /// edge. We do this by indexing the birdie to each vertex in the 'bridges'
        /// map.
        /// </summary>
        /// <param name="v">an endpoint of the blossom bridge</param>
        /// <param name="w">another endpoint of the blossom bridge</param>
        /// <param name="base">the base of the blossom</param>
        private int[] BlossomSupports(int v, int w, int @base)
        {
            int n = 0;
            path[n++] = uf.Find(v);
            Tuple b = Tuple.Of(v, w);
            while (path[n - 1] != @base)
            {
                int u = even[path[n - 1]];
                path[n++] = u;
                this.bridges.Add(u, b);
                // contracting the blossom allows us to continue searching from odd
                // vertices (any odd vertices are now even - part of the blossom set)
                queue.Enqueue(u);
                path[n++] = uf.Find(odd[u]);
            }

            return Arrays.CopyOf(path, n);
        }

        /// <summary>
        /// Augment all ancestors in the tree of vertex 'v'.
        /// </summary>
        /// <param name="v">the leaf to augment from</param>
        private void Augment(int v)
        {
            int n = BuildPath(path, 0, v, nil);
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
        /// <returns>the number of items set to the <paramref name="path"/>[].</returns>
        private int BuildPath(int[] path, int i, int start, int goal)
        {
            while (true)
            {
                // lift the path through the contracted blossom
                while (odd[start] != nil)
                {
                    Tuple bridge = bridges[start];

                    // add to the path from the bridge down to where 'start'
                    // is - we need to reverse it as we travel 'up' the blossom
                    // and then...
                    int j = BuildPath(path, i, bridge.First(), start);
                    Reverse(path, i, j - 1);
                    i = j;

                    // ... we travel down the other side of the bridge 
                    start = bridge.Second();
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
        /// Utility to maximise an existing matching of the provided graph.
        /// </summary>
        /// <param name="g">a graph</param>
        /// <param name="m">matching on the graph, will me modified</param>
        /// <param name="n">current matching cardinality</param>
        /// <param name="s">subset of vertices to match</param>
        /// <returns>the maximal matching on the graph</returns>
        public static int Maximise(Graph g, Matching m, int n, IntSet s)
        {
            MaximumMatching mm = new MaximumMatching(g, m, n, s);
            return mm.nMatched;
        }

        /// <summary>
        /// Utility to maximise an existing matching of the provided graph.
        /// </summary>
        /// <param name="g">a graph</param>
        /// <param name="m">matching on the graph</param>
        /// <param name="n"></param>
        /// <returns>the maximal matching on the graph</returns>
        public static int Maximise(Graph g, Matching m, int n)
            {
            return Maximise(g, m, n, IntSet.Universe);
        }

        /// <summary>
        /// Utility to get the maximal matching of the specified graph.
        /// </summary>
        /// <param name="g">a graph</param>
        /// <returns>the maximal matching on the graph</returns>
        public static Matching Maximal(Graph g)
        {
            Matching m = Matching.CreateEmpty(g);
            Maximise(g, m, 0);
            return m;
        }

        /// <summary>
        /// Utility class provides a fixed size queue. Enough space is allocated for
        /// every vertex in the graph. Any new vertices are added at the 'end' index
        /// and 'polling' a vertex advances the 'start'.
        /// </summary>
        private sealed class FixedSizeQueue
        {
            private readonly int[] vs;
            private int i = 0;
            private int n = 0;

            /// <summary>
            /// Create a queue of size 'n'.
            /// </summary>
            /// <param name="n">size of the queue</param>
            public FixedSizeQueue(int n)
            {
                vs = new int[n];
            }

            /// <summary>
            /// Add an element to the queue.
            /// </summary>
            /// <param name="e"></param>
           public void Enqueue(int e)
            {
                vs[n++] = e;
            }

            /// <summary>
            /// Poll the first element from the queue.
            /// </summary>
            /// <returns>the first element</returns>.
            public int Poll()
            {
                return vs[i++];
            }

            /// <summary>
            /// The queue is empty.
            /// </summary>
            public bool IsEmpty() => i == n;

            /// <summary>Reset the queue.</summary>
            public void Clear()
            {
                i = 0;
                n = 0;
            }
        }

        /// <summary>Utility to reverse a section of a fixed size array</summary>
        static void Reverse(int[] path, int i, int j)
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
    }
}

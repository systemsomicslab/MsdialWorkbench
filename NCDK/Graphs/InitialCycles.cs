/*
 * Copyright (c) 2013 European Bioinformatics Institute (EMBL-EBI)
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
using NCDK.Common.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Graphs
{
    /// <summary>
    /// Compute the set of initial cycles (<i>C'<sub>I</sub></i>) in a graph. The
    /// super-set contains the minimum cycle basis (<i>C<sub>B</sub></i>) and the
    /// relevant cycles (<i>C<sub>R</sub></i>) of the provided graph <token>cdk-cite-Vismara97</token>.
    /// This class is intend for internal use by other cycle processing
    /// algorithms.
    /// </summary>
    /// <seealso cref="RelevantCycles"/>
    // @author John May
    // @cdk.module core
    internal class InitialCycles
    {
        /// <summary>Adjacency list representation of a chemical graph.</summary>
        private readonly int[][] graph;

        /// <summary>Vertex ordering.</summary>
        private readonly int[] ordering;

        /// <summary>Cycle prototypes indexed by their length.</summary>
        private readonly IMultiDictionary<int, Cycle> cycles = new SortedMultiDictionary<int, Cycle>();

        /// <summary>Index of edges in the graph</summary>
        private readonly BiDiDictionary<Cycle.Edge, int> edges;

        /// <summary>
        /// Initial array size for <see cref="GetOrdering(int[][])"/>. This method sorts vertices by degree
        /// by counting how many of each degree there is then putting values in place
        /// directly. This is known as key-value counting and is used in radix
        /// sorts.
        /// </summary>
        /// <seealso href="https://en.wikipedia.org/wiki/Radix_sort#Least_significant_digit_radix_sorts">Radix Sort</seealso>
        private const int DEFAULT_DEGREE = 4;

        /// <summary>Number of vertices which have degree 2.</summary>
        private int nDeg2Vertices;

        /// <summary>Limit the size of cycles discovered.</summary>
        private readonly int limit;

        /// <summary>
        /// Is the graph known to be a biconnected component. This allows a small
        /// optimisation.
        /// </summary>
        private readonly bool biconnected;

        /// <summary>
        /// Create a set of initial cycles for the provided graph.
        /// </summary>
        /// <param name="graph">input graph</param>
        /// <exception cref="ArgumentNullException">the <paramref name="graph"/> was null</exception>
        public InitialCycles(int[][] graph)
            : this(graph, graph == null ? 0 : graph.Length, false)
        {
        }

        /// <summary>
        /// Create a set of initial cycles for the provided graph.
        /// </summary>
        /// <param name="graph">input graph</param>
        /// <param name="limit">the maximum size of cycle found</param>
        /// <exception cref="ArgumentNullException">the <paramref name="graph"/> was null</exception>
        public InitialCycles(int[][] graph, int limit)
            : this(graph, limit, false)
        {
        }

        /// <summary>
        /// Internal constructor - takes a graph and a flag that the graph is a
        /// biconnected component. This allows a minor optimisation to trigger.
        /// </summary>
        /// <param name="graph">input graph</param>
        /// <param name="limit"></param>
        /// <param name="biconnected">the graph is known to be biconnected</param>
        /// <exception cref="ArgumentNullException">the <paramref name="graph"/> was null</exception>
        private InitialCycles(int[][] graph, int limit, bool biconnected)
        {
            this.graph = graph ?? throw new ArgumentNullException(nameof(graph), "no graph provided");

            // ordering ensures the number of initial cycles is polynomial
            this.biconnected = biconnected;
            this.limit = limit;
            this.ordering = GetOrdering(graph);

            // index the edges to allow us to jump between edge and path representation
            // - edge representation: binary vector indicates whether an edge
            //                        is present or
            // - path representation: sequential list vertices forming the cycle
            edges = new BiDiDictionary<Cycle.Edge, int>();
            int n = graph.Length;
            for (int v = 0; v < n; v++)
            {
                foreach (int w in graph[v])
                {
                    if (w > v)
                    {
                        Cycle.Edge edge = new Cycle.Edge(v, w);
                        edges.Add(edge, edges.Count);
                    }
                }
            }

            // compute the initial set of cycles
            Compute();
        }

        /// <summary>
        /// Access to graph used to calculate the initial cycle set.
        /// </summary>
        /// <returns>the graph</returns>
        public int[][] Graph => graph;

        /// <summary>
        /// Unique lengths of all cycles found in natural order.
        /// </summary>
        /// <returns>lengths of the discovered cycles</returns>
        public IEnumerable<int> Lengths => cycles.Keys;

        /// <summary>
        /// Access all the prototype cycles of the given length. If no cycles were
        /// found of given length an empty list is returned.
        /// </summary>
        /// <param name="length">desired length of cycles</param>
        /// <returns>cycles of the given length</returns>
        /// <seealso cref="Lengths"/>
        public IEnumerable<Cycle> GetCyclesOfLength(int length)
        {
            if (!cycles.ContainsKey(length))
                return Enumerable.Empty<Cycle>();
            return cycles[length];
        }

        /// <summary>
        /// Construct a list of all cycles.
        /// </summary>
        /// <returns>list of cycles</returns>
        public IEnumerable<Cycle> GetCycles()
        {
            return cycles.Values;
        }

        /// <summary>
        /// Number of cycles in the initial set.
        /// </summary>
        /// <returns>number of cycles</returns>
        public int GetNumberOfCycles()
        {
            return cycles.Count;
        }

        /// <summary>
        /// The number of edges (<i>m</i>) in the graph.
        /// </summary>
        /// <returns>number of edges</returns>
        public int GetNumberOfEdges()
        {
            return edges.Count;
        }

        /// <summary>
        /// Access the <see cref="edges"/> at the given index.
        /// </summary>
        /// <param name="i">index of edge</param>
        /// <returns>the edge at the given index</returns>
        public Cycle.Edge GetEdge(int i)
        {
            return edges.InverseGet(i);
        }

        /// <summary>
        /// Lookup the index of the edge formed by the vertices <paramref name="u"/> and
        /// <paramref name="v"/>.
        /// </summary>
        /// <param name="u">a vertex adjacent to <paramref name="v"/></param>
        /// <param name="v">a vertex adjacent to <paramref name="u"/></param>
        /// <returns>the index of the edge</returns>
        public int IndexOfEdge(int u, int v)
        {
            return edges[new Cycle.Edge(u, v)];
        }

        /// <summary>
        /// Convert a path of vertices to a binary vector of edges. It is possible to
        /// convert the vector back to the path using <see cref="edges"/>.
        /// </summary>
        /// <param name="path">the vertices which define the cycle</param>
        /// <returns>vector edges which make up the path</returns>
        /// <seealso cref="IndexOfEdge(int, int)"/>
        public BitArray ToEdgeVector(int[] path)
        {
            BitArray incidence = new BitArray(edges.Count);
            int len = path.Length - 1;
            for (int i = 0; i < len; i++)
            {
                incidence.Set(IndexOfEdge(path[i], path[i + 1]), true);
            }
            return incidence;
        }

        /// <summary>
        /// Compute the initial cycles. The code corresponds to algorithm 1 from
        /// <token>cdk-cite-Vismara97</token>, where possible the variable names have been kept
        /// the same.
        /// </summary>
        private void Compute()
        {
            int n = graph.Length;

            // the set 'S' contains the pairs of vertices adjacent to 'y'
            int[] s = new int[n];
            int sizeOfS;

            // order the vertices by degree
            int[] vertices = new int[n];
            for (int v = 0; v < n; v++)
            {
                vertices[ordering[v]] = v;
            }

            // if the graph is known to be a biconnected component (prepossessing)
            // and there is at least one vertex with a degree > 2 we can skip all
            // vertices of degree 2.
            //
            // otherwise the smallest possible cycle is {0,1,2} (no parallel edges
            // or loops) we can therefore don't need to do the first two shortest
            // paths calculations
            int first = biconnected && nDeg2Vertices < n ? nDeg2Vertices : 2;

            for (int i = first; i < n; i++)
            {
                int r = vertices[i];

                ShortestPaths pathsFromR = new ShortestPaths(graph, null, r, limit / 2, ordering);

                // we only check the vertices which belong to the set Vr. this
                // set is vertices reachable from 'r' by only travelling though
                // vertices smaller then r. In the ShortestPaths API this is
                // name 'IsPrecedingPathTo'.
                //
                // using Vr allows us to prune the number of vertices to check and
                // discover each cycle exactly once. This is possible as for each
                // simple cycle there is only one vertex with a maximum ordering.
                for (int j = 0; j < i; j++)
                {
                    int y = vertices[j];
                    if (!pathsFromR.IsPrecedingPathTo(y)) continue;

                    // start refilling set 's' by resetting it's size
                    sizeOfS = 0;

                    // z is adjacent to y and belong to Vr
                    foreach (var z in graph[y])
                    {
                        if (!pathsFromR.IsPrecedingPathTo(z)) continue;

                        int distToZ = pathsFromR.GetDistanceTo(z);
                        int distToY = pathsFromR.GetDistanceTo(y);

                        // the distance of the path to z is one less then the
                        // path to y. the vertices are adjacent, therefore z must
                        // also belong to the shortest path from r to y.
                        //
                        // we queue up (in 's') all the vertices adjacent to y for
                        // which this holds and then check these once we've processed
                        // all adjacent vertices
                        //
                        //  / ¯ ¯ z1 \          z1 and z2 are added to 's' and
                        // r          y - z3    checked later as p and q (see below)
                        //  \ _ _ z2 /
                        //
                        if (distToZ + 1 == distToY)
                        {
                            s[sizeOfS++] = z;
                        }

                        // if the distances are equal we could have an odd cycle
                        // but we need to check the paths only intersect at 'r'.
                        //
                        // we check the intersect for cases like this, shortest
                        // cycle here is {p .. y, z .. p} not {r .. y, z .. r}
                        //
                        //           / ¯ ¯ y         / ¯ ¯ \ / ¯ ¯ y
                        //  r - - - p      |    or  r       p      |
                        //           \ _ _ z         \ _ _ / \ _ _ z
                        //
                        // if it's the shortest cycle then the intersect is just {r}
                        //
                        //  / ¯ ¯ y
                        // r      |
                        //  \ _ _ z
                        //
                        else if (distToZ == distToY && ordering[z] < ordering[y])
                        {
                            int[] pathToY = pathsFromR.GetPathTo(y);
                            int[] pathToZ = pathsFromR.GetPathTo(z);
                            if (GetSingletonIntersect(pathToZ, pathToY))
                            {
                                Cycle cycle = new Cycle.OddCycle(this, pathsFromR, pathToY, pathToZ);
                                Add(cycle);
                            }
                        }
                    }

                    // check each pair vertices adjacent to 'y' for an
                    // even cycle, as with the odd cycle we ensure the intersect
                    // of the paths {r .. p} and {r .. q} is {r}.
                    //
                    //  / ¯ ¯ p \
                    // r         y
                    //  \ _ _ q /
                    //
                    for (int k = 0; k < sizeOfS; k++)
                    {
                        for (int l = k + 1; l < sizeOfS; l++)
                        {
                            int[] pathToP = pathsFromR.GetPathTo(s[k]);
                            int[] pathToQ = pathsFromR.GetPathTo(s[l]);
                            if (GetSingletonIntersect(pathToP, pathToQ))
                            {
                                Cycle cycle = new Cycle.EvenCycle(this, pathsFromR, pathToP, y, pathToQ);
                                Add(cycle);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add a newly discovered initial cycle.
        /// </summary>
        /// <param name="cycle">the cycle to add</param>
        private void Add(Cycle cycle)
        {
            if (cycle.Length <= limit)
                cycles.Add(cycle.Length, cycle);
        }

        /// <summary>
        /// Compute the vertex ordering (π). The ordering is based on the vertex
        /// degree and <![CDATA[π(x) < π(y) => Deg(x) ≤ Deg(y)]]>. The ordering
        /// guarantees the number of elements in <i>C<sub>I</sub></i> is 
        /// <i>2m<sup>2</sup> + vn</i>. See Lemma 3 of <token>cdk-cite-Vismara97</token>.
        /// </summary>
        /// <returns>the order of each vertex</returns>
        private int[] GetOrdering(int[][] graph)
        {
            var n = graph.Length;
            var order = new int[n];
            var count = new int[(n == 0 ? 0 : graph.Select(a => a.Length).Max()) + 3];

            // count the occurrences of each key (degree)
            for (int v = 0; v < n; v++)
            {
                int key = graph[v].Length + 1;
                count[key]++;
            }
            // cumulated degree counts
            for (int i = 1; i < count.Length; i++)
            {
                count[i] += count[i - 1];
            }
            // store the location each vertex would occur
            for (int v = 0; v < n; v++)
            {
                order[v] = count[graph[v].Length]++;
            }
            nDeg2Vertices = count[2];
            return order;
        }

        /// <summary>
        /// Given two paths from a common start vertex "r" check whether there
        /// are any intersects. If the paths are different length the shorter of the
        /// two should be given as <paramref name="p"/>.
        /// </summary>
        /// <param name="p">a path from "r"</param>
        /// <param name="q">a path from "r"</param>
        /// <returns>whether the only intersect is <c>r</c></returns>
        public static bool GetSingletonIntersect(int[] p, int[] q)
        {
            int n = p.Length;
            for (int i = 1; i < n; i++)
                if (p[i] == q[i])
                    return false;
            return true;
        }

        /// <summary>
        /// Join the two paths end on end and ignore the first vertex of the second
        /// path. {0, 1, 2} and {0, 3, 4} becomes {0, 1, 2, 4, 3}.
        /// </summary>
        /// <param name="pathToY">first path</param>
        /// <param name="pathToZ">second path</param>
        /// <returns>the paths joined end on end and the last vertex truncated</returns>
        public static int[] Join(int[] pathToY, int[] pathToZ)
        {
            List<int> aa = new List<int>(pathToY);
            aa.AddRange(pathToZ.Reverse());
            return aa.ToArray();
        }

        /// <summary>
        /// Join the two paths end on end using 'y'. The first vertex of the second
        /// path is truncated. {0, 1, 2}, {5} and {0, 3, 4} becomes {0, 1, 2, 5, 4,
        /// 3}.
        /// </summary>
        /// <param name="pathToP">first path</param>
        /// <param name="y">how to join the two paths</param>
        /// <param name="pathToQ">second path</param>
        /// <returns>the paths joined end on end and the last vertex truncated</returns>
        public static int[] Join(int[] pathToP, int y, int[] pathToQ)
        {
            List<int> aa = new List<int>(pathToP)
            {
                y
            };
            aa.AddRange(pathToQ.Reverse());
            return aa.ToArray();
        }

        /// <summary>
        /// Compute the initial cycles of a biconnected graph.
        /// </summary>
        /// <param name="graph">the biconnected graph</param>
        /// <returns>computed initial cycles</returns>
        /// <exception cref="ArgumentNullException">the <paramref name="graph"/> was null</exception>
        public static InitialCycles OfBiconnectedComponent(int[][] graph)
        {
            return OfBiconnectedComponent(graph, graph.Length);
        }

        /// <summary>
        /// Compute the initial cycles of a biconnected graph.
        /// </summary>
        /// <param name="graph">the biconnected graph</param>
        /// <param name="limit">maximum size of the cycle to find</param>
        /// <returns>computed initial cycles</returns>
        /// <exception cref="ArgumentNullException">the <paramref name="graph"/> was null</exception>
        public static InitialCycles OfBiconnectedComponent(int[][] graph, int limit)
        {
            return new InitialCycles(graph, limit, true);
        }

        /// <summary>
        /// Abstract description of a cycle. Stores the path and computes the edge
        /// vector representation.
        /// </summary>
        public abstract class Cycle
            : IComparable<Cycle>
        {
            protected InitialCycles parent;

            internal readonly int[] path;
            internal readonly ShortestPaths paths;

            private BitArray edgeVector = null;

            /// <summary>
            /// The edge vector for this cycle.
            /// </summary>
            public virtual BitArray EdgeVector
            {
                get
                {
                    if (edgeVector == null)
                        edgeVector = GetEdges(path); // XXX allows static Cycle
                    return edgeVector;
                }
            }

            public Cycle(InitialCycles parent, ShortestPaths paths, int[] path)
            {
                this.parent = parent;

                this.path = path;
                this.paths = paths;
            }

            /// <summary>
            /// Provides the edges of <i>path</i>, this method only exists so we can
            /// refer to the class in a static context.
            /// </summary>
            /// <param name="path">path of vertices</param>
            /// <returns>set of edges</returns>
            public abstract BitArray GetEdges(int[] path);

            /// <summary>
            /// Access the path of this cycle.
            /// </summary>
            /// <returns>the path of the cycle</returns>
            public virtual int[] Path => path;

            /// <summary>
            /// Reconstruct the entire cycle family (may be exponential).
            /// </summary>
            /// <returns>all cycles in this family.</returns>
            public abstract int[][] GetFamily();

            /// <summary>
            /// The number of cycles in this prototypes family. This method be used
            /// to avoid the potentially exponential reconstruction of all the cycles
            /// using <see cref="GetFamily"/>.
            /// </summary>
            /// <returns>number of cycles</returns>
            public abstract int SizeOfFamily();

            /// <summary>
            /// The length of the cycles (number of vertices in the path).
            /// </summary>
            /// <returns>cycle length</returns>
            public virtual int Length => path.Length - 1; // first/last vertex repeats

            public virtual int CompareTo(Cycle that)
            {
                return Primitive.GetLexicographicalComparator<int>().Compare(this.path, that.path);
            }

            /// <summary>
            /// An even cycle is formed from two shortest paths of the same length
            /// and 'two' edges to a common vertex. The cycle formed by these is
            /// even, 2n + 2 = even.
            /// </summary>
            /// <seealso cref="Compute"/>
            public class EvenCycle
                : Cycle
            {
                private readonly int p;
                private readonly int q;

                int Y { get; set; }

                public EvenCycle(InitialCycles parent, ShortestPaths paths, int[] pathToP, int y, int[] pathToQ)
                    : base(parent, paths, Join(pathToP, y, pathToQ))
                {
                    this.p = pathToP[pathToP.Length - 1];
                    this.q = pathToQ[pathToQ.Length - 1];
                    this.Y = y;
                }

                /// <inheritdoc/>
                public override BitArray GetEdges(int[] path)
                {
                    return parent.ToEdgeVector(path);
                }

                /// <inheritdoc/>
                public override int[][] GetFamily()
                {
                    int[][] pathsToP = paths.GetPathsTo(p);
                    int[][] pathsToQ = paths.GetPathsTo(q);

                    int[][] pathss = new int[SizeOfFamily()][];
                    int i = 0;
                    foreach (var pathToP in pathsToP)
                    {
                        foreach (var pathToQ in pathsToQ)
                        {
                            pathss[i++] = Join(pathToP, Y, pathToQ);
                        }
                    }
                    return pathss;
                }

                /// <inheritdoc/>
                public override int SizeOfFamily()
                {
                    return paths.GetNPathsTo(p) * paths.GetNPathsTo(q);
                }
            }

            /// <summary>
            /// An odd cycle is formed from two shortest paths of the same length
            /// and 'one' edge to a common vertex. The cycle formed by these is odd,
            /// 2n + 1 = odd.
            /// </summary>
            /// <seealso cref="Compute"/>
            public class OddCycle
                : Cycle
            {
                private readonly int y;
                private readonly int z;

                public OddCycle(InitialCycles parent, ShortestPaths paths, int[] pathToY, int[] pathToZ)
                    : base(parent, paths, Join(pathToY, pathToZ))
                {
                    this.parent = parent;

                    y = pathToY[pathToY.Length - 1];
                    z = pathToZ[pathToY.Length - 1];
                }

                /// <inheritdoc/>
                public override BitArray GetEdges(int[] path)
                {
                    return parent.ToEdgeVector(path);
                }

                /// <inheritdoc/>
                public override int[][] GetFamily()
                {
                    int[][] pathsToY = paths.GetPathsTo(y);
                    int[][] pathsToZ = paths.GetPathsTo(z);

                    int[][] pathss = new int[SizeOfFamily()][];
                    int i = 0;
                    foreach (var pathToY in pathsToY)
                    {
                        foreach (var pathToZ in pathsToZ)
                        {
                            pathss[i++] = Join(pathToY, pathToZ);
                        }
                    }
                    return pathss;
                }

                /// <inheritdoc/>
                public override int SizeOfFamily()
                {
                    return paths.GetNPathsTo(y) * paths.GetNPathsTo(z);
                }
            }

            /// <summary>
            /// A simple value which acts as an immutable unordered tuple for two
            /// primitive integers. This allows to index edges of a graph.
            /// </summary>
            public sealed class Edge
            {
                private readonly int v, w;

                public Edge(int v, int w)
                {
                    this.v = v;
                    this.w = w;
                }

                public override bool Equals(object o)
                {
                    Edge that = (Edge)o;
                    return (this.v == that.v && this.w == that.w) || (this.v == that.w && this.w == that.v);
                }

                public override int GetHashCode()
                {
                    return v ^ w;
                }

                public override string ToString()
                {
                    return "{" + v + ", " + w + "}";
                }
            }
        }
    }
}

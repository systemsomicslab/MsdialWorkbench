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

using System;
using System.Collections.Generic;
using NCDK.Common.Primitives;

namespace NCDK.Graphs
{
    /// <summary>
    /// A path graph (<b>P-Graph</b>) for graphs with less than 64 vertices - the
    /// P-Graph provides efficient generation of all simple cycles in a graph
    /// <token>cdk-cite-HAN96</token>. Vertices are sequentially removed from the graph by
    /// reducing incident edges and forming new 'path edges'. The order in which the
    /// vertices are to be removed should be pre-defined in the constructor as the
    /// <see cref="rank"/> parameter.
    /// </summary>
    /// <seealso href="http://en.wikipedia.org/wiki/Biconnected_component">Wikipedia: Biconnected Component</seealso>
    /// <seealso cref="RingSearches.RingSearch"/>
    /// <seealso cref="GraphUtil"/>
    // @author John May
    // @author Till Schäfer (predefined vertex ordering)
    // @cdk.module core
    sealed class RegularPathGraph
        : PathGraph
    {
        private static readonly List<PathEdge> EmptyPathEdgeList = new List<PathEdge>();

        /// <summary>Path edges, indexed by their end points (incidence list).</summary>
        private List<PathEdge>[] graph;

        /// <summary>Limit on the maximum Length of cycle to be found.</summary>
        private readonly int limit;

        /// <summary>Indicates when each vertex will be removed, '0' = first, '|V|' = last.</summary>
        private readonly int[] rank;

        /// <summary>
        /// Create a regular path graph (<b>P-Graph</b>) for the given molecule graph (<b>M-Graph</b>).
        /// </summary>
        /// <param name="mGraph">The molecule graph (M-Graph) in adjacency list representation.</param>
        /// <param name="rank">Unique rank of each vertex - indicates when it will be removed.</param>
        /// <param name="limit">Limit for size of cycles found, to find all cycles specify the limit as the number of vertices in the graph.</param>
        /// <exception cref="ArgumentException">limit was invalid or the graph was too large</exception>
        /// <exception cref="ArgumentNullException">the molecule graph was not provided</exception>
        public RegularPathGraph(int[][] mGraph, int[] rank, int limit)
        {
            if (mGraph == null)
                throw new ArgumentNullException(nameof(mGraph));
            this.graph = new List<PathEdge>[mGraph.Length];
            this.rank = rank ?? throw new ArgumentNullException(nameof(rank));
            this.limit = limit + 1; // first/last vertex repeats
            int ord = graph.Length;

            // check configuration
            if (!(ord > 2)) 
                throw new ArgumentOutOfRangeException(nameof(mGraph), "graph was acyclic");
            if (!(limit >= 3 && limit <= ord))
                throw new ArgumentOutOfRangeException(nameof(limit), "limit should be from 3 to |V|");
            if (!(ord < 64))
                throw new ArgumentOutOfRangeException(nameof(mGraph), "graph has 64 or more atoms, use JumboPathGraph");

            for (int v = 0; v < ord; v++)
                graph[v] = new List<PathEdge>();

            // construct the path-graph
            for (int v = 0; v < ord; v++)
            {
                foreach (int w in mGraph[v])
                    if (w > v)
                        Add(new SimpleEdge(v, w));
            }
        }

        /// <summary>
        /// Add a path-edge to the path-graph. Edges are only added to the vertex of
        /// lowest rank (see. constructor).
        /// </summary>
        /// <param name="edge">path edge</param>
        private void Add(PathEdge edge)
        {
            int u = edge.Either();
            int v = edge.Other(u);
            if (rank[u] < rank[v])
                graph[u].Add(edge);
            else
                graph[v].Add(edge);
        }

        /// <inheritdoc/>
        public override int Degree(int x)
        {
            return graph[x].Count;
        }

        /// <summary>
        /// Access edges which are incident to <paramref name="x"/> and remove them from the graph.
        /// </summary>
        /// <param name="x">a vertex</param>
        /// <returns>vertices incident to <paramref name="x"/></returns>
        private IReadOnlyList<PathEdge> Remove(int x)
        {
            var edges = graph[x];
            graph[x] = EmptyPathEdgeList; 
            return edges;
        }

        /// <summary>
        /// Pairwise combination of all disjoint <i>edges</i> incident to a vertex <paramref name="x"/>.
        /// </summary>
        /// <param name="edges">edges which are currently incident to <paramref name="x"/></param>
        /// <param name="x">a vertex in the graph</param>
        /// <returns>reduced edges</returns>
        private static List<PathEdge> Combine(IReadOnlyList<PathEdge> edges, int x)
        {
            int n = edges.Count;
            var reduced = new List<PathEdge>();

            for (int i = 0; i < n; i++)
            {
                PathEdge e = edges[i];
                for (int j = i + 1; j < n; j++)
                {
                    PathEdge f = edges[j];
                    if (e.Disjoint(f))
                        reduced.Add(new ReducedEdge(e, f, x));
                }
            }
            return reduced;
        }

        /// <inheritdoc/>
        public override void Remove(int x, IList<int[]> cycles)
        {
            var edges = Remove(x);
            var reduced = Combine(edges, x);

            foreach (PathEdge e in reduced)
            {
                if (e.Length <= limit) {
                    if (e.IsLoop)
                        cycles.Add(e.Path());
                    else
                        Add(e);
                }
            }
        }

        /// <summary>Empty bit set.</summary>
        private const long EMPTY_SET = 0;

        /// <summary>
        /// An abstract path edge. A path edge has two end points and 0 or more
        /// reduced vertices which represent a path between those endpoints.
        /// </summary>
        internal abstract class PathEdge
        {
            /// <summary>Endpoints of the edge.</summary>
            public readonly int u, v;

            /// <summary>Bits indicate reduced vertices between endpoints (exclusive).</summary>
            public readonly long xs;

            /// <summary>
            /// A new edge specified by two endpoints and a bit set indicating which
            /// vertices have been reduced.
            /// </summary>
            /// <param name="u">an endpoint</param>
            /// <param name="v">the other endpoint</param>
            /// <param name="xs">reduced vertices between endpoints</param>
            public PathEdge(int u, int v, long xs)
            {
                this.u = u;
                this.v = v;
                this.xs = xs;
            }

            /// <summary>
            /// Check if the edges are disjoint with respect to their reduced
            /// vertices. That is, excluding the endpoints, no reduced vertices are
            /// shared.
            /// </summary>
            /// <param name="other">another edge</param>
            /// <returns>the edges reduced vertices are disjoint.</returns>
            public bool Disjoint(PathEdge other)
            {
                return (this.xs & other.xs) == EMPTY_SET;
            }

            /// <summary>
            /// Is the edge a loop and connects a vertex to its self.
            /// </summary>
            /// <returns>whether the edge is a loop</returns>
            public bool IsLoop => u == v;

            /// <summary>
            /// Access either endpoint of the edge.
            /// </summary>
            /// <returns>either endpoint.</returns>
            public int Either()
            {
                return u;
            }

            /// <summary>
            /// Given one endpoint, retrieve the other endpoint.
            /// </summary>
            /// <param name="x">an endpoint</param>
            /// <returns>the other endpoint.</returns>
            public int Other(int x)
            {
                return u == x ? v : u;
            }

            /// <summary>
            /// Total Length of the path formed by this edge. The value includes
            /// endpoints and reduced vertices.
            /// </summary>
            /// <returns>Length of path</returns>
            public abstract int Length { get; }

            /// <summary>
            /// Reconstruct the path through the edge by appending vertices to a
            /// mutable <see cref="ArrayBuilder"/>.
            /// </summary>
            /// <param name="ab">array builder to append vertices to</param>
            /// <returns>the array builder parameter for convenience</returns>
            public abstract ArrayBuilder Reconstruct(ArrayBuilder ab);

            /// <summary>
            /// The path stored by the edge as a fixed size array of vertices.
            /// </summary>
            /// <returns>fixed size array of vertices which are in the path.</returns>
            public int[] Path()
            {
                return Reconstruct(new ArrayBuilder(Length).Append(Either())).xs;
            }
        }

        /// <summary>A simple non-reduced edge, just the two end points.</summary>
        internal sealed class SimpleEdge
            : PathEdge
        {
            /// <summary>
            /// A new simple edge, with two endpoints.
            /// </summary>
            /// <param name="u">an endpoint</param>
            /// <param name="v">another endpoint</param>
            public SimpleEdge(int u, int v) 
                : base(u, v, EMPTY_SET)
            { 
            }

            /// <inheritdoc/>
            public override ArrayBuilder Reconstruct(ArrayBuilder ab)
            {
                return ab.Append(Other(ab.Prev()));
            }

            /// <inheritdoc/>
            public override int Length => 2;
        }

        /// <summary>
        /// A reduced edge, made from two existing path edges and an endpoint they
        /// have in common.
        /// </summary>
        internal sealed class ReducedEdge
            : PathEdge
        {
            /// <summary>Reduced edges.</summary>
            private readonly PathEdge e, f;

            /// <summary>
            /// Create a new reduced edge from two existing edges and vertex they
            /// have in common.
            /// </summary>
            /// <param name="e">an edge</param>
            /// <param name="f">another edge</param>
            /// <param name="x">a common vertex</param>
            public ReducedEdge(PathEdge e, PathEdge f, int x)
                : base(e.Other(x), f.Other(x), e.xs | f.xs | 1L << x)
            {
                this.e = e;
                this.f = f;
            }

            /// <inheritdoc/>
            public override ArrayBuilder Reconstruct(ArrayBuilder ab)
            {
                return u == ab.Prev() ? f.Reconstruct(e.Reconstruct(ab)) : e.Reconstruct(f.Reconstruct(ab));
            }

            /// <inheritdoc/>
            public override int Length => Longs.BitCount(xs) + 2;
        }

        /// <summary>
        /// A simple helper class for constructing a fixed size int[] array and
        /// sequentially appending vertices.
        /// </summary>
        internal sealed class ArrayBuilder
        {
            private int i = 0;
            public readonly int[] xs;

            /// <summary>
            /// A new array builder of fixed size.
            /// </summary>
            /// <param name="n">size of the array</param>
            public ArrayBuilder(int n)
            {
                xs = new int[n];
            }

            /// <summary>
            /// Append a value to the end of the sequence.
            /// </summary>
            /// <param name="x">a new value</param>
            /// <returns>self-reference for chaining</returns>
            public ArrayBuilder Append(int x)
            {
                xs[i++] = x;
                return this;
            }

            /// <summary>
            /// Previously value in the sequence.
            /// </summary>
            /// <returns>previous value</returns>
            public int Prev()
            {
                return xs[i - 1];
            }
        }
    }
}

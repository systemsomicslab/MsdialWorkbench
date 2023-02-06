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

namespace NCDK.Graphs
{
    /// <summary>
    /// Collection of static utilities for manipulating adjacency list
    /// representations stored as a <see cref="int"/>[][]. May well be replaced in
    /// future with a <i>Graph</i> data type.
    /// </summary>
    /// <seealso cref="ShortestPaths"/>
    /// <seealso cref="RingSearches.RingSearch"/>
    // @author John May
    // @cdk.module core
    public static class GraphUtil
    {
        /// <summary>
        /// Create an adjacent list representation of the <paramref name="container"/>.
        /// </summary>
        /// <param name="container">the molecule</param>
        /// <returns>adjacency list representation stored as an <see cref="int"/>[][].</returns>
        /// <exception cref="ArgumentNullException">the container was null</exception>
        /// <exception cref="ArgumentException">a bond was found which contained atoms not in the molecule</exception>
        public static int[][] ToAdjList(IAtomContainer container)
        {
            return ToAdjList(container, null);
        }

        /// <summary>
        /// Create an adjacent list representation of the <paramref name="container"/> and
        /// fill in the <paramref name="bondMap"/> for quick lookup.
        /// </summary>
        /// <param name="container">the molecule</param>
        /// <param name="bondMap">a map to index the bonds into</param>
        /// <returns>adjacency list representation stored as an <see cref="int"/>[][].</returns>
        /// <exception cref="ArgumentNullException">the container was null</exception>
        /// <exception cref="ArgumentException">a bond was found which contained atoms not in the molecule</exception>
        public static int[][] ToAdjList(IAtomContainer container, EdgeToBondMap bondMap)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            int n = container.Atoms.Count;

            var graph = new List<int>[n];
            for (var i = 0; i < n; i++)
                graph[i] = new List<int>();

            foreach (var bond in container.Bonds)
            {
                int v = container.Atoms.IndexOf(bond.Begin);
                int w = container.Atoms.IndexOf(bond.End);

                if (v < 0 || w < 0)
                    throw new ArgumentException($"bond at index {container.Bonds.IndexOf(bond)} contained an atom not present in molecule");

                graph[v].Add(w);
                graph[w].Add(v);

                bondMap?.Add(v, w, bond);
            }

            var agraph = new int[n][];
            for (int v = 0; v < n; v++)
            {
                agraph[v] = graph[v].ToArray();
            }

            return agraph;
        }

        /// <summary>
        /// Create an adjacent list representation of the <paramref name="container"/> that only
        /// includes bonds that are in the set provided as an argument.
        /// </summary>
        /// <param name="container">the molecule</param>
        /// <returns>adjacency list representation.</returns>
        /// <exception cref="ArgumentNullException">the container was null</exception>
        /// <exception cref="ArgumentException">a bond was found which contained atoms not in the molecule</exception>
        public static int[][] ToAdjListSubgraph(IAtomContainer container, ISet<IBond> include)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container), "atom container was null");

            int n = container.Atoms.Count;

            List<int>[] graph = new List<int>[n];
            for (var i = 0; i < n; i++)
                graph[i] = new List<int>();

            foreach (IBond bond in container.Bonds)
            {
                if (!include.Contains(bond))
                    continue;

                int v = container.Atoms.IndexOf(bond.Begin);
                int w = container.Atoms.IndexOf(bond.End);

                if (v < 0 || w < 0)
                    throw new ArgumentException($"bond at index {container.Bonds.IndexOf(bond)} contained an atom not present in molecule");

                graph[v].Add(w);
                graph[w].Add(v);
            }

            int[][] agraph = new int[n][];
            for (int v = 0; v < n; v++)
            {
                agraph[v] = graph[v].ToArray();
            }

            return agraph;
        }

        /// <summary>
        /// Create a subgraph by specifying the vertices from the original <paramref name="graph"/>
        /// to <paramref name="include"/> in the subgraph. The provided vertices also
        /// provide the mapping between vertices in the subgraph and the original.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.GraphUtil_Example.cs+Subgraph"]/*' />
        /// </example>
        /// <param name="graph">adjacency list graph</param>
        /// <param name="include">the vertices of he graph to include in the subgraph</param>
        /// <returns>the subgraph</returns>
        public static int[][] Subgraph(int[][] graph, int[] include)
        {
            // number of vertices in the graph and the subgraph
            int n = graph.Length;
            int m = include.Length;

            // mapping from vertex in 'graph' to 'subgraph'
            var mapping = new int[n];
            for (int i = 0; i < m; i++)
                mapping[include[i]] = i + 1;

            // initialise the subgraph
            var subgraph = new List<int>[m];
            for (var i = 0; i < m; i++)
                subgraph[i] = new List<int>();

            // build the subgraph, in the subgraph we denote to adjacent
            // vertices p and q. If p or q is less then 0 then it is not
            // in the subgraph
            for (int v = 0; v < n; v++)
            {
                int p = mapping[v] - 1;
                if (p < 0)
                    continue;

                foreach (int w in graph[v])
                {
                    int q = mapping[w] - 1;
                    if (q < 0)
                        continue;
                    subgraph[p].Add(q);
                }
            }

            int[][] asubgraph = new int[m][];
            // truncate excess storage
            for (int p = 0; p < m; p++)
                asubgraph[p] = subgraph[p].ToArray();

            return asubgraph;
        }
        
        /// <summary>
        /// Arrange the <paramref name="vertices"/> in a simple cyclic path. 
        /// </summary>
        /// <param name="graph">a graph</param>
        /// <param name="vertices">set of vertices</param>
        /// <returns>vertices in a walk which makes a cycle (first and last are the same)</returns>
        /// <exception cref="ArgumentException">If the vertices do not form a cycle</exception>
        /// <seealso cref="RingSearches.RingSearch.Isolated"/>
        public static int[] Cycle(int[][] graph, int[] vertices)
        {
            int n = graph.Length;
            int m = vertices.Length;

            // mark vertices
            bool[] marked = new bool[n];
            foreach (int v in vertices)
                marked[v] = true;

            int[] path = new int[m + 1];

            path[0] = path[m] = vertices[0];
            marked[vertices[0]] = false;

            for (int i = 1; i < m; i++)
            {
                int w = FirstMarked(graph[path[i - 1]], marked);
                if (w < 0)
                    throw new ArgumentException("broken path");
                path[i] = w;
                marked[w] = false;
            }

            // the path is a cycle if the start and end are adjacent, if this is
            // the case return the path
            foreach (int w in graph[path[m - 1]])
                if (w == path[0])
                    return path;

            throw new ArgumentException("path does not make a cycle");
        }

        /// <summary>
        /// Find the first value in <paramref name="xs"/> which is <paramref name="marked"/>.
        /// </summary>
        /// <param name="xs">array of values</param>
        /// <param name="marked">marked values</param>
        /// <returns>first marked value, -1 if none found</returns>
        internal static int FirstMarked(int[] xs, bool[] marked)
        {
            foreach (int x in xs)
                if (marked[x])
                    return x;
            return -1;
        }
    }

    /// <summary>
    /// Utility for storing <see cref="IBond"/>s indexed by vertex end points.
    /// </summary>
    public class EdgeToBondMap
    {
        Dictionary<Tuple, IBond> lookup = new Dictionary<Tuple, IBond>();

        public EdgeToBondMap()
        {
        }

        /// <summary>
        /// Index a bond by the endpoints.
        /// </summary>
        /// <param name="v">an endpoint</param>
        /// <param name="w">another endpoint</param>
        /// <param name="bond">the bond value</param>
        public void Add(int v, int w, IBond bond)
        {
            lookup[new Tuple(v, w)] = bond;
        }

        /// <summary>
        /// Access the bond store at the end points v and w. If no bond is
        /// store, null is returned.
        /// </summary>
        /// <param name="v">an endpoint</param>
        /// <param name="w">another endpoint</param>
        /// <returns>the bond stored for the endpoints</returns>
        public IBond this[int v, int w]
        {
            get
            {
                if (lookup.TryGetValue(new Tuple(v, w), out IBond bond))
                    return bond;
                return null;
            }
        }

        /// <summary>
        /// Create a map with enough space for all the bonds in the molecule,
        /// <paramref name="container"/>. Note - the map is not filled by this method.
        /// </summary>
        /// <param name="container">the container</param>
        /// <returns>a map with enough space for the container</returns>
        public static EdgeToBondMap WithSpaceFor(IAtomContainer container)
        {
            return new EdgeToBondMap();
        }

        /// <summary>
        /// Unordered storage of two int values. Mainly useful to index bonds by
        /// it's vertex end points.
        /// </summary>
        struct Tuple : IEquatable<Tuple>
        {
            private readonly int u, v;

            /// <summary>
            /// Create a new tuple with the specified values.
            /// </summary>
            /// <param name="u">a value</param>
            /// <param name="v">another value</param>
            public Tuple(int u, int v)
            {
                this.u = u;
                this.v = v;
            }

            public override bool Equals(object o)
            {
                if (o is Tuple other)
                {
                    return this.Equals(other);
                }
                return false;
            }

            public bool Equals(Tuple other)
            {
                return this.u == other.u && this.v == other.v || this.u == other.v && this.v == other.u;
            }

            public override int GetHashCode()
            {
                return u ^ v;
            }
        }
    }
}

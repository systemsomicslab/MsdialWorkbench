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
using System;
using System.Collections;
using System.Diagnostics;
using System.Text;

namespace NCDK.Graphs
{
    /// <summary>
    /// A matching is an independent edge set of a graph. This is a set of edges that
    /// share no common vertices. A matching is perfect if every vertex in the graph
    /// is matched. Each vertex can be matched with exactly one other vertex.
    /// </summary>
    /// <remarks>
    /// This class provides storage and manipulation of a matching. A new match is
    /// added with <see cref="Match(int, int)"/>, any existing match for the newly matched
    /// vertices is no-longer available. The status of a vertex can be queried with
    /// <see cref="Matched(int)"/>  and the matched vertex obtained with <see cref="Other(int)"/>.
    /// </remarks>
    /// <seealso href="http://en.wikipedia.org/wiki/Matching_(graph_theory)">Matching (graph theory), Wikipedia</seealso>
    // @author John May
    // @cdk.module standard
    public sealed class Matching
    {
        /// <summary>Indicate an unmatched vertex.</summary>
        private const int NIL = -1;

        /// <summary>Match storage.</summary>
        private readonly int[] match;

        /// <summary>
        /// Create a matching of the given size.
        /// </summary>
        /// <param name="n">number of items</param>
        private Matching(int n)
        {
            this.match = new int[n];
            Arrays.Fill(match, NIL);
        }

        /// <summary>
        /// Add the edge '{<paramref name="u"/>, <paramref name="v"/>}' to the matched edge set. Any existing matches for
        /// <paramref name="u"/> or <paramref name="v"/> are removed from the matched set.
        /// </summary>
        /// <param name="u">a vertex</param>
        /// <param name="v">another vertex</param>
        public void Match(int u, int v)
        {
            // set the new match, don't need to update existing - we only provide
            // access to bidirectional mappings
            match[u] = v;
            match[v] = u;
        }

        /// <summary>
        /// Access the vertex matched with 'v'.
        /// </summary>
        /// <param name="v">vertex</param>
        /// <returns>matched vertex</returns>
        /// <exception cref="ArgumentException">the vertex is currently unmatched</exception>
        public int Other(int v)
        {
            if (Unmatched(v)) throw new ArgumentException(v + " is not matched");
            return match[v];
        }

        /// <summary>
        /// Remove a matching for the specified vertex.
        /// </summary>
        /// <param name="v">vertex</param>
        public void Unmatch(int v)
        {
            match[v] = NIL;
        }

        /// <summary>
        /// Determine if a vertex has a match.
        /// </summary>
        /// <param name="v">vertex</param>
        /// <returns>the vertex is matched</returns>
        public bool Matched(int v)
        {
            return !Unmatched(v);
        }

        /// <summary>
        /// Determine if a vertex is not matched.
        /// </summary>
        /// <param name="v">a vertex</param>
        /// <returns>the vertex has no matching</returns>
        public bool Unmatched(int v)
        {
            return match[v] == NIL || match[match[v]] != v;
        }

        /// <summary>
        /// Attempt to augment the matching such that it is perfect over the subset
        /// of vertices in the provided graph.
        /// </summary>
        /// <param name="graph">adjacency list representation of graph</param>
        /// <param name="subset">subset of vertices</param>
        /// <returns>the matching was perfect</returns>
        /// <exception cref="ArgumentException">the graph was a different size to the matching capacity</exception>
        public bool Perfect(int[][] graph, BitArray subset)
        {
            if (graph.Length != match.Length || BitArrays.Cardinality(subset) > graph.Length)
                throw new ArgumentException("graph and matching had different capacity");

            // and odd set can never provide a perfect matching
            if ((BitArrays.Cardinality(subset) & 0x1) == 0x1) return false;

            // arbitrary matching was perfect
            if (ArbitaryMatching(graph, subset)) return true;

            EdmondsMaximumMatching.Maxamise(this, graph, subset);

            // the matching is imperfect if any vertex was
            for (int v = BitArrays.NextSetBit(subset, 0); v >= 0; v = BitArrays.NextSetBit(subset, v + 1))
                if (Unmatched(v)) return false;

            return true;
        }

        /// <summary>
        /// Assign an arbitrary matching that covers the subset of vertices.
        /// </summary>
        /// <param name="graph">adjacency list representation of graph</param>
        /// <param name="subset">subset of vertices in the graph</param>
        /// <returns>the matching was perfect</returns>
        internal bool ArbitaryMatching(int[][] graph, BitArray subset)
        {
            BitArray unmatched = new BitArray(subset.Length);

            // indicates the deg of each vertex in unmatched subset
            int[] deg = new int[graph.Length];

            // queue/stack of vertices with deg1 vertices
            int[] deg1 = new int[graph.Length];
            int nd1 = 0, nMatched = 0;

            for (int v = BitArrays.NextSetBit(subset, 0); v >= 0; v = BitArrays.NextSetBit(subset, v + 1))
            {
                if (Matched(v))
                {
                    Trace.Assert(subset[Other(v)]);
                    nMatched++;
                    continue;
                }
                unmatched.Set(v, true);
                foreach (var w in graph[v])
                    if (subset[w] && Unmatched(w)) deg[v]++;
                if (deg[v] == 1) deg1[nd1++] = v;
            }

            while (!BitArrays.IsEmpty(unmatched))
            {
                int v = -1;

                // attempt to select a vertex with degree = 1 (in matched set)
                while (nd1 > 0)
                {
                    v = deg1[--nd1];
                    if (unmatched[v]) break;
                }

                // no unmatched degree 1 vertex, select the first unmatched
                if (v < 0 || unmatched[v]) v = BitArrays.NextSetBit(unmatched, 0);

                unmatched.Set(v, false);

                // find a unmatched edge and match it, adjacent degrees are updated
                foreach (var w in graph[v])
                {
                    if (unmatched[w])
                    {
                        Match(v, w);
                        nMatched += 2;
                        unmatched.Set(w, false);
                        // update neighbors of w and v (if needed)
                        foreach (var u in graph[w])
                            if (--deg[u] == 1 && unmatched[u]) deg1[nd1++] = u;

                        // if deg == 1, w is the only neighbor
                        if (deg[v] > 1)
                        {
                            foreach (var u in graph[v])
                                if (--deg[u] == 1 && unmatched[u]) deg1[nd1++] = u;
                        }
                        break;
                    }
                }
            }

            return nMatched == BitArrays.Cardinality(subset);
        }

        /// <summary>
        /// Create an empty matching with the specified capacity.
        /// </summary>
        /// <param name="capacity">maximum number of vertices</param>
        /// <returns>empty matching</returns>
        public static Matching WithCapacity(int capacity)
        {
            return new Matching(capacity);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(4 * match.Length);
            sb.Append('[');
            for (int u = 0; u < match.Length; u++)
            {
                int v = match[u];
                if (v > u && match[v] == u)
                {
                    if (sb.Length > 1) sb.Append(", ");
                    sb.Append(u).Append('=').Append(v);
                }
            }
            sb.Append(']');
            return sb.ToString();
        }
    }
}

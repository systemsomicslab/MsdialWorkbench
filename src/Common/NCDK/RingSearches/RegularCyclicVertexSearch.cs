/*
 * Copyright (C) 2012 John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by the
 * Free Software Foundation; either version 2.1 of the License, or (at your
 * option) any later version. All we ask is that proper credit is given for our
 * work, which includes - but is not limited to - adding the above copyright
 * notice to the beginning of your source code files, and to any copyright
 * notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation, Inc.,
 * 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System.Linq;
using System.Collections.Generic;
using NCDK.Common.Primitives;
using NCDK.Common.Collections;

namespace NCDK.RingSearches
{
    /// <summary>
    /// CyclicVertexSearch for graphs with 64 vertices or less. This search is
    /// optimised using primitive <see cref="long"/> values to represent vertex sets.
    /// </summary>
    // @author John May
    // @cdk.module core
    internal class RegularCyclicVertexSearch
        : ICyclicVertexSearch
    {
        /// <summary>graph representation</summary>
        private readonly IReadOnlyList<IReadOnlyList<int>> g;

        /// <summary>set of known cyclic vertices</summary>
        private long cyclic;

        /// <summary>cycle systems as they are discovered</summary>
        private List<long> cycles = new List<long>(1);

        /// <summary>indicates if the 'cycle' at 'i' in 'cycles' is fused</summary>
        private List<bool> fused = new List<bool>(1);

        /// <summary>set of visited vertices</summary>
        private long visited;

        /// <summary>the vertices in our path at a given vertex index</summary>
        private readonly long[] state;

        /// <summary>Vertex colors - which component does each vertex belong.</summary>
        private volatile int[] colors;

        /// <summary>
        /// Create a new cyclic vertex search for the provided graph.
        /// </summary>
        /// <param name="graph">adjacency list representation of a graph</param>
        internal RegularCyclicVertexSearch(IReadOnlyList<IReadOnlyList<int>> graph)
        {
            this.g = graph;
            int n = graph.Count;

            // skip search if empty graph
            if (n == 0) return;

            state = new long[n];

            // start from vertex 0
            Search(0, 0L, 0L);

            // if disconnected we have not visited all vertices
            int v = 1;
            while (Longs.BitCount(visited) != n)
            {

                // haven't visited v, start a new search from there
                if (!Visited(v))
                {
                    Search(v, 0L, 0L);
                }
                v++;
            }

            // no longer needed for the lifetime of the object
            state = null;
        }

        /// <summary>
        /// Perform a depth first search from the vertex <paramref name="v"/>.
        /// </summary>
        /// <param name="v">vertex to search from</param>
        /// <param name="prev">the state before we vistaed our parent (previous state)</param>
        /// <param name="curr">the current state (including our parent)</param>
        private void Search(int v, long prev, long curr)
        {
            state[v] = curr; // store the state before we visited v
            curr = SetBit(curr, v); // include v in our current state (state[v] is unmodified)
            visited |= curr; // mark v as visited (or being visited)

            // neighbors of v
            foreach (var w in g[v])
            {
                // w has been visited or is partially visited further up stack
                if (Visited(w))
                {
                    // if w is in our prev state we have a cycle of size >2.
                    // we don't check out current state as this will always
                    // include w - they are adjacent
                    if (IsBitSet(prev, w))
                    {
                        NumCycles++;

                        // xor the state when we last visited 'w' with our current
                        // state. this set is all the vertices we visited since then
                        // and are all in a cycle
                        Add(state[w] ^ curr);
                    }
                }
                else
                {
                    // recursively call for the unvisited neighbor w
                    Search(w, state[v], curr);
                }
            }
        }

        public int NumCycles { get; private set; } = 0;

        /// <summary>
        /// Returns whether the vertex 'v' has been visited.
        /// </summary>
        /// <param name="v">a vertex</param>
        /// <returns>whether the vertex has been visited</returns>
        private bool Visited(int v)
        {
            return IsBitSet(visited, v);
        }

        /// <summary>
        /// Add the cycle vertices to our discovered cycles. The cycle is first
        /// checked to see if it is isolated (shares at most one vertex) or
        /// <i>potentially</i> fused.
        /// </summary>
        /// <param name="cycle">newly discovered cyclic vertex set</param>
        private void Add(long cycle)
        {

            long intersect = cyclic & cycle;

            // intersect by more then 1 vertex, we 'may' have a fused cycle
            if (intersect != 0 && Longs.BitCount(intersect) > 1)
            {
                AddFused(cycle);
            }
            else
            {
                AddIsolated(cycle);
            }

            cyclic |= cycle;

        }

        /// <summary>
        /// Add an a new isolated cycle which is currently edge disjoint with all
        /// other cycles.
        /// </summary>
        /// <param name="cycle">newly discovered cyclic vertices</param>
        private void AddIsolated(long cycle)
        {
            cycles.Add(cycle);
            fused.Add(false);
        }

        /// <summary>
        /// Adds a <i>potentially</i> fused cycle. If the cycle is discovered not be
        /// fused it will still be added as isolated.
        /// </summary>
        /// <param name="cycle">vertex set of a potentially fused cycle, indicated by the set bits</param>
        private void AddFused(long cycle)
        {
            // find index of first fused cycle
            int i = IndexOfFused(0, cycle);

            if (i != -1)
            {
                // include the new cycle vertices and mark as fused
                cycles[i] = cycle | cycles[i];
                fused[i] = true;

                // merge other cycles we are share an edge with
                int j = i;
                while ((j = IndexOfFused(j + 1, cycles[i])) != -1)
                {
                    var newval = cycles[j] | cycles[i];
                    cycles[i] = newval;
                    cycles.RemoveAt(j);
                    fused.RemoveAt(j);
                    j--; // removed a vertex, need to move back one
                }
            }
            else
            {
                // edge disjoint
                AddIsolated(cycle);
            }
        }

        /// <summary>
        /// Find the next index that the <i>cycle</i> intersects with by at least two
        /// vertices. If the intersect of a vertex set with another contains more
        /// then two vertices it cannot be edge disjoint.
        /// </summary>
        /// <param name="start">start searching from here</param>
        /// <param name="cycle">test whether any current cycles are fused with this one</param>
        /// <returns>the index of the first fused after 'start', -1 if none</returns>
        private int IndexOfFused(int start, long cycle)
        {
            for (int i = start; i < cycles.Count(); i++)
            {
                long intersect = cycles[i] & cycle;
                if (intersect != 0 && Longs.BitCount(intersect) > 1)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Synchronisation lock.</summary>
        private readonly object syncLock = new object();

        /// <summary>
        /// Lazily build an indexed lookup of vertex color. The vertex color
        /// indicates which cycle a given vertex belongs. If a vertex belongs to more
        /// then one cycle it is colored '0'. If a vertex belongs to no cycle it is
        /// colored '-1'.
        /// </summary>
        /// <returns>vertex colors</returns>
        public int[] VertexColor()
        {
            int[] result = colors;
            if (result == null)
            {
                lock (syncLock)
                {
                    result = colors;
                    if (result == null)
                    {
                        colors = result = BuildVertexColor();
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Build an indexed lookup of vertex color. The vertex color indicates which
        /// cycle a given vertex belongs. If a vertex belongs to more then one cycle
        /// it is colored '0'. If a vertex belongs to no cycle it is colored '-1'.
        /// </summary>
        /// <returns>vertex colors</returns>
        private int[] BuildVertexColor()
        {
            int[] color = new int[g.Count];
            int n = 1;
            Arrays.Fill(color, -1);
            foreach (var l_cycle in cycles)
            {
                var cycle = l_cycle;
                for (int i = 0; i < g.Count; i++)
                {
                    if ((cycle & 0x1) == 0x1)
                        color[i] = color[i] < 0 ? n : 0;
                    cycle >>= 1;
                }
                n++;
            }
            return color;
        }

        /// <inheritdoc/>
        public bool Cyclic(int v)
        {
            return IsBitSet(cyclic, v);
        }

        /// <inheritdoc/>
        public bool Cyclic(int u, int v)
        {
            var colors = VertexColor();

            // if either vertex has no color then the edge can not
            // be cyclic
            if (colors[u] < 0 || colors[v] < 0) return false;

            // if the vertex color is 0 it is shared between
            // two components (i.e. spiro-rings) we need to
            // check each component
            if (colors[u] == 0 || colors[v] == 0)
            {
                // either vertices are shared - need to do the expensive check
                foreach (var cycle in cycles)
                {
                    if (IsBitSet(cycle, u) && IsBitSet(cycle, v))
                    {
                        return true;
                    }
                }
                return false;
            }

            // vertex is not shared between components check the colors match (i.e.
            // in same component)
            return colors[u] == colors[v];
        }

        /// <inheritdoc/>
        public int[] Cyclic()
        {
            return ToArray(cyclic);
        }

        /// <inheritdoc/>
        public int[][] Isolated()
        {
            var isolated = new List<int[]>(cycles.Count());
            for (int i = 0; i < cycles.Count(); i++)
            {
                if (!fused[i]) isolated.Add(ToArray(cycles[i]));
            }
            return isolated.ToArray();
        }

        /// <inheritdoc/>
        public int[][] Fused()
        {
            List<int[]> fused = new List<int[]>(cycles.Count());
            for (int i = 0; i < cycles.Count(); i++)
            {
                if (this.fused[i]) fused.Add(ToArray(cycles[i]));
            }
            return fused.ToArray();
        }

        /// <summary>
        /// Convert the bits of a <see cref="long"/> to an array of integers. The size of
        /// the output array is the number of bits set in the value.
        /// </summary>
        /// <param name="set">value to convert</param>
        /// <returns>array of the set bits in the long value</returns>
        internal static int[] ToArray(long set)
        {
            int[] vertices = new int[Longs.BitCount(set)];
            int i = 0;

            // fill the cyclic vertices with the bits that have been set
            for (int v = 0; i < vertices.Length; v++)
            {
                if (IsBitSet(set, v))
                    vertices[i++] = v;
            }

            return vertices;
        }

        /// <summary>
        /// Determine if the specified bit on the value is set.
        /// </summary>
        /// <param name="value">bits indicate that vertex is in the set</param>
        /// <param name="bit">bit to test</param>
        /// <returns>whether the specified bit is set</returns>
        internal static bool IsBitSet(long value, int bit)
        {
            return (value & 1L << bit) != 0;
        }

        /// <summary>
        /// Set the specified bit on the value and return the modified value.
        /// </summary>
        /// <param name="value">the value to set the bit on</param>
        /// <param name="bit">the bit to set</param>
        /// <returns>modified value</returns>
        internal static long SetBit(long value, int bit)
        {
            return value | 1L << bit;
        }
    }
}

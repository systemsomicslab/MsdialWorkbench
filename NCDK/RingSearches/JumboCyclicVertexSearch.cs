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

using NCDK.Common.Collections;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.RingSearches
{
    /// <summary>
    /// CyclicVertexSearch for graphs with more then 64 vertices.
    /// </summary>
    // @author John May
    // @cdk.module core
    internal class JumboCyclicVertexSearch
        : ICyclicVertexSearch
    {
        /// <summary>graph representation</summary>
        private readonly IReadOnlyList<IReadOnlyList<int>> g;

        /// <summary>set of known cyclic vertices</summary>
        private readonly BitArray cyclic;

        /// <summary>cycle systems as they are discovered</summary>
        private List<BitArray> cycles = new List<BitArray>(1);

        /// <summary>indicates if the 'cycle' at 'i' in 'cycles' is fused</summary>
        private List<bool> fused = new List<bool>(1);

        /// <summary>set of visited vertices</summary>
        private BitArray visited;

        /// <summary>the vertices in our path at a given vertex index</summary>
        private readonly BitArray[] state;

        /// <summary>vertex colored by each component.</summary>
        private int[] colors;

        /// <summary>
        /// Create a new cyclic vertex search for the provided graph.
        /// </summary>
        /// <param name="graph">adjacency list representation of a graph</param>
        public JumboCyclicVertexSearch(IReadOnlyList<IReadOnlyList<int>> graph)
        {
            this.g = graph;
            int n = graph.Count;

            cyclic = new BitArray(n);

            if (n == 0) return;

            state = new BitArray[n];
            visited = new BitArray(n);

            BitArray empty = new BitArray(n);

            // start from vertex 0
            Search(0, Copy(empty), Copy(empty));

            // if g is a fragment we will not have visited everything
            int v = 0;
            while (BitArrays.Cardinality(visited) != n)
            {
                v++;
                // each search expands to the whole fragment, as we
                // may have fragments we need to visit 0 and then
                // check every other vertex
                if (!visited[v])
                {
                    Search(v, Copy(empty), Copy(empty));
                }
            }

            // allow the states to be collected
            state = null;
            visited = null;
        }

        /// <summary>
        /// Perform a depth first search from the vertex <paramref name="v"/>.
        /// </summary>
        /// <param name="v">vertex to search from</param>
        /// <param name="prev">the state before we vistaed our parent (previous state)</param>
        /// <param name="curr">the current state (including our parent)</param>
        private void Search(int v, BitArray prev, BitArray curr)
        {
            state[v] = curr; // set the state before we visit v
            curr = Copy(curr); // include v in our current state (state[v] is unmodified)
            curr.Set(v, true);
            visited.Or(curr); // mark v as visited (or being visited)

            // for each neighbor w of v
            foreach (var w in g[v])
            {
                // if w is in our prev state we have a cycle of size >3.
                // we don't check out current state as this will always
                // include w - they are adjacent
                if (prev[w])
                {
                    NumCycles++;
                    // we have a cycle, xor the state when we last visited 'w'
                    // with our current state. this set is all the vertices
                    // we visited since then
                    Add(Xor(state[w], curr));
                }

                // check w hasn't been visited or isn't being visited further up the stack.
                // this mainly stops us re-visiting the vertex we came from
                else if (!visited[w])
                {
                    // recursively call for the neighbor 'w'
                    Search(w, state[v], curr);
                }
            }
        }

        /// <summary>Synchronisation lock.</summary>
        private readonly object syncLock = new object();

        public int NumCycles { get; private set; } = 0;

        /// <summary>
        /// Lazily build an indexed lookup of vertex color. The vertex color
        /// indicates which cycle a given vertex belongs. If a vertex belongs to more
        /// then one cycle it is colored '0'. If a vertex belongs to no cycle it is
        /// colored '-1'.
        /// </summary>
        /// <returns>vertex colors</returns>
        public int[] VertexColor()
        {
            var result = colors;
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
            foreach (var cycle in cycles)
            {
                for (int i = BitArrays.NextSetBit(cycle, 0); i >= 0; i = BitArrays.NextSetBit(cycle, i + 1))
                {
                    color[i] = color[i] < 0 ? n : 0;
                }
                n++;
            }

            return color;
        }

        /// <inheritdoc/>
        public bool Cyclic(int v)
        {
            return cyclic[v];
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
                    if (cycle[u] && cycle[v]) return true;
                }
                return false;
            }

            // vertex is not shared between components
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
            var fused = new List<int[]>(cycles.Count());
            for (int i = 0; i < cycles.Count(); i++)
            {
                if (this.fused[i]) fused.Add(ToArray(cycles[i]));
            }
            return fused.ToArray();
        }

        /// <summary>
        /// Add the cycle vertices to our discovered cycles. The cycle is first
        /// checked to see if it is isolated (shares at most one vertex) or
        /// <i>potentially</i> fused.
        /// </summary>
        /// <param name="cycle">newly discovered cyclic vertex set</param>
        private void Add(BitArray cycle)
        {
            BitArray intersect = And(cycle, cyclic);

            if (BitArrays.Cardinality(intersect) > 1)
            {
                AddFused(cycle);
            }
            else
            {
                AddIsolated(cycle);
            }

            cyclic.Or(cycle);
        }

        /// <summary>
        /// Add an a new isolated cycle which is currently edge disjoint with all
        /// other cycles.
        /// </summary>
        /// <param name="cycle">newly discovered cyclic vertices</param>
        private void AddIsolated(BitArray cycle)
        {
            cycles.Add(cycle);
            fused.Add(false);
        }

        /// <summary>
        /// Adds a <i>potentially</i> fused cycle. If the cycle is discovered not be
        /// fused it will still be added as isolated.
        /// </summary>
        /// <param name="cycle">vertex set of a potentially fused cycle, indicated by the set bits</param>
        private void AddFused(BitArray cycle)
        {
            int i = IndexOfFused(0, cycle);

            if (i != -1)
            {
                // add new cycle and mark as fused
                cycles[i].Or(cycle);
                fused[i] = true;
                int j = i;

                // merge other cycles we could be fused with into 'i'
                while ((j = IndexOfFused(j + 1, cycle)) != -1)
                {
                    cycles[i].Or(cycles[j]);
                    cycles.RemoveAt(j);
                    fused.RemoveAt(j);
                    j--;
                }
            }
            else
            {
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
        private int IndexOfFused(int start, BitArray cycle)
        {
            for (int i = start; i < cycles.Count(); i++)
            {
                if (BitArrays.Cardinality(And(cycles[i], cycle)) > 1)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Convert the set bits of a BitArray to an int[].
        /// </summary>
        /// <param name="set">input with 0 or more set bits</param>
        /// <returns>the bits which are set in the input</returns>
        public static int[] ToArray(BitArray set)
        {
            int[] vertices = new int[BitArrays.Cardinality(set)];
            int i = 0;

            // fill the cyclic vertices with the bits that have been set
            for (int v = 0; i < vertices.Length; v++)
            {
                if (set[v])
                {
                    vertices[i++] = v;
                }
            }

            return vertices;
        }

        /// <summary>
        /// XOR the to bit sets together and return the result. Neither input is
        /// modified.
        /// </summary>
        /// <param name="x">first bit set</param>
        /// <param name="y">second bit set</param>
        /// <returns>the XOR of the two bit sets</returns>
        public static BitArray Xor(BitArray x, BitArray y)
        {
            BitArray z = Copy(x);
            z.Xor(y);
            return z;
        }

        /// <summary>
        /// AND the to bit sets together and return the result. Neither input is
        /// modified.
        /// </summary>
        /// <param name="x">first bit set</param>
        /// <param name="y">second bit set</param>
        /// <returns>the AND of the two bit sets</returns>
        public static BitArray And(BitArray x, BitArray y)
        {
            BitArray z = Copy(x);
            z.And(y);
            return z;
        }

        /// <summary>
        /// Copy the original bit set.
        /// </summary>
        /// <param name="org">input bit set</param>
        /// <returns>copy of the input</returns>
        public static BitArray Copy(BitArray org)
        {
            BitArray cpy = (BitArray)org.Clone();
            return cpy;
        }
    }
}

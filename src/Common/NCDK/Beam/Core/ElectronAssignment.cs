/*
 * Copyright (c) 2013, European Bioinformatics Institute (EMBL-EBI)
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met: 
 *
 * 1. Redistributions of source code must retain the above copyright notice, this
 *    list of conditions and the following disclaimer. 
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution. 
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * Any EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
 * Any DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON Any THEORY OF LIABILITY, WHETHER IN CONTRACT, Strict LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN Any WAY OUT OF THE USE OF THIS
 * SOFTWARE, Even IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * The views and conclusions contained in the software and documentation are those
 * of the authors and should not be interpreted as representing official policies, 
 * either expressed or implied, of the FreeBSD Project.
 */

using NCDK.Common.Collections;
using System.Collections;

namespace NCDK.Beam
{
    /// <summary>
    /// Verifies delocalised electrons can be assigned to a structure without
    /// changing bond Orders. To check and assign the electrons please use <see cref="Localise"/>
    /// or <see cref="Graph.Kekule"/>. Although faster than assigning a Kekulé
    /// structure the method is the same and returning a structure with specified
    /// bond Orders is usually preferred.
    /// </summary>
    /// <seealso cref="Localise"/>
    /// <seealso cref="Graph.Kekule"/>
    // @author John May
    internal sealed class ElectronAssignment
    {
        private ElectronAssignment()
        {
        }

        /// <summary>
        /// Check if it is possible to assign electrons to the subgraph (specified by
        /// the set bits in of <paramref name="bs"/>). Each connected subset is counted up and
        /// checked for odd cardinality.
        /// </summary>
        /// <param name="g"> graph</param>
        /// <param name="bs">binary set indicated vertices for the subgraph</param>
        /// <returns>there is an odd cardinality subgraph</returns>
        private static bool ContainsOddCardinalitySubgraph(Graph g, BitArray bs)
        {
            // mark visited those which are not in any subgraph 
            bool[] visited = new bool[g.Order];
            for (int i = BitArrays.NextClearBit(bs, 0); i < g.Order; i = BitArrays.NextClearBit(bs, i + 1))
                visited[i] = true;

            // from each unvisited vertices visit the connected vertices and count
            // how many there are in this component. if there is an odd number there
            // is no assignment of double bonds possible
            for (int i = BitArrays.NextSetBit(bs, 0); i >= 0; i = BitArrays.NextSetBit(bs, i + 1))
            {
                if (!visited[i] && IsOdd(Visit(g, i, 0, visited)))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determine the size the connected component using a depth-first-search.
        /// </summary>
        /// <param name="g">graph</param>
        /// <param name="v">vertex</param>
        /// <param name="c">count</param>
        /// <param name="visited">which vertices have been visited</param>
        /// <returns>size of the component from </returns><paramref name="v"/>
        private static int Visit(Graph g, int v, int c, bool[] visited)
        {
            visited[v] = true;
            foreach (var e in g.GetEdges(v))
            {
                int w = e.Other(v);
                if (!visited[w] && e.Bond.Order == 1)
                    c = Visit(g, w, c, visited);
            }
            return 1 + c;
        }

        /// <summary>
        /// Test if an a number, <paramref name="x"/> is odd.
        /// </summary>
        /// <param name="x">a number</param>
        /// <returns>the number is odd</returns>
        private static bool IsOdd(int x)
        {
            return (x & 0x1) == 1;
        }

        /// <summary>
        /// Utility method to verify electrons can be assigned.
        /// </summary>
        /// <param name="g">graph to check</param>
        /// <returns>electrons could be assigned to delocalised structure</returns>
        public static bool Verify(Graph g)
        {
            return g.GetFlags(Graph.HAS_AROM) == 0 || !ContainsOddCardinalitySubgraph(g, Localise.BuildSet(g, new BitArray(g.Order)));
        }
    }
}
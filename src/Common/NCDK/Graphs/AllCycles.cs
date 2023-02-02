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

using System.Collections.Generic;

namespace NCDK.Graphs
{
    /// <summary>
    /// Compute all simple cycles (rings) in a graph. Generally speaking one does not
    /// need all the cycles and tractable subsets offer good alternatives.
    /// <list type="bullet">
    /// <item><see cref="EdgeShortCycles"/> - the smallest cycle through each edge</item>
    /// <item><see cref="RelevantCycles"/> - union of all minimum cycle bases - unique but may be exponential</item> 
    /// <item><see cref="EssentialCycles"/> - intersection of all minimum cycle bases </item> 
    /// <item><see cref="MinimumCycleBasis"/> - a minimum cycles basis, may not be unique. Often used interchangeable with the term SSSR.</item>
    /// </list>
    /// </summary>
    /// <example>
    /// For maximum performance the algorithm should be run only on ring systems (a
    /// biconnected component with at least one tri-connected vertex). An example of
    /// this is shown below:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.AllCycles_Example.cs"]/*' />
    /// </example>
    /// <seealso href="http://efficientbits.blogspot.co.uk/2013/06/allringsfinder-sport-edition.html">Performance Analysis (Blog Post)</seealso>
    /// <seealso cref="RegularPathGraph"/>
    /// <seealso cref="JumboPathGraph"/>
    /// <seealso cref="GraphUtil"/>
    /// <seealso cref="RingSearches.RingSearch"/>
    // @author John May
    // @cdk.module core
    public sealed class AllCycles
    {
        /// <summary>All simple cycles.</summary>
        private readonly List<int[]> cycles = new List<int[]>();

        /// <summary>Indicates whether the perception completed.</summary>
        private readonly bool completed;

        /// <summary>
        /// Compute all simple cycles up to given <paramref name="maxCycleSize"/> in the provided
        /// <paramref name="graph"/>. In some graphs the topology makes it impracticable to
        /// compute all the simple. To avoid running forever on these molecules the
        /// <paramref name="maxDegree"/> provides an escape clause. The value doesn't quantify
        /// how many cycles we get. 
        /// </summary>
        /// <remarks>
        /// The percentage of molecules in PubChem Compound
        /// (Dec '12) which would successfully complete for a given Degree are listed
        /// below.
        /// <list type="table">
        /// <listheader>Table 1. Number of structures processable in PubChem Compound (Dec 2012) as a result of
        /// setting the max degree</listheader>
        /// <item><term>Percent</term><term>Max Degree</term></item>
        /// <item><term>99%</term><term>9</term></item> <item><term>99.95%</term><term>72</term></item>
        /// <item><term>99.96%</term><term>84</term></item> <item><term>99.97%</term><term>126</term></item>
        /// <item><term>99.98%</term><term>216</term></item> <item><term>99.99%</term><term>684</term></item>
        /// </list>
        /// </remarks>
        /// <param name="graph">adjacency list representation of a graph</param>
        /// <param name="maxCycleSize">the maximum cycle size to perceive</param>
        /// <param name="maxDegree">escape clause to stop the algorithm running forever</param>
        public AllCycles(int[][] graph, int maxCycleSize, int maxDegree)
        {
            // get the order in which we remove vertices, the rank tells us
            // the index in the ordered array of each vertex
            int[] rank = GetRank(graph);
            int[] vertices = GetVerticesInOrder(rank);

            PathGraph pGraph;
            if (graph.Length < 64)
                pGraph = new RegularPathGraph(graph, rank, maxCycleSize);
            else
                pGraph = new JumboPathGraph(graph, rank, maxCycleSize);

            // perceive the cycles by removing the vertices in order
            int removed = 0;
            foreach (int v in vertices)
            {
                if (pGraph.Degree(v) > maxDegree) break; // or could throw exception...

                pGraph.Remove(v, cycles);
                removed++;
            }
            completed = removed == graph.Length;
        }

        /// <summary>
        /// Using the pre-computed rank, get the vertices in order.
        /// </summary>
        /// <param name="rank">see <see cref="GetRank(int[][])"/></param>
        /// <returns>vertices in order</returns>
        internal static int[] GetVerticesInOrder(int[] rank)
        {
            int[] vs = new int[rank.Length];
            for (int v = 0; v < rank.Length; v++)
                vs[rank[v]] = v;
            return vs;
        }

        /// <summary>
        /// Compute a rank for each vertex. This rank is based on the Degree and
        /// indicates the position each vertex would be in a sorted array.
        /// </summary>
        /// <param name="g">a graph in adjacent list representation</param>
        /// <returns>array indicating the rank of each vertex.</returns>
        internal static int[] GetRank(int[][] g)
        {
            int ord = g.Length;

            int[] count = new int[ord + 1];
            int[] rank = new int[ord];

            // frequency of each Degree
            for (int v = 0; v < ord; v++)
                count[g[v].Length + 1]++;
            // cumulated counts
            for (int i = 0; count[i] < ord; i++)
                count[i + 1] += count[i];
            // store sorted position of each vertex
            for (int v = 0; v < ord; v++)
                rank[v] = count[g[v].Length]++;

            return rank;
        }

        /// <summary>
        /// The paths describing all simple cycles in the given graph. The path stats
        /// and ends vertex.
        /// </summary>
        /// <returns>2d array of paths</returns>
        public int[][] GetPaths()
        {
            int[][] paths = new int[cycles.Count][];
            for (int i = 0; i < cycles.Count; i++)
                paths[i] = (int[])cycles[i].Clone();
            return paths;
        }

        /// <summary>
        /// Cardinality of the set.
        /// </summary>
        /// <returns>number of cycles</returns>
        public int Count => cycles.Count;

        /// <summary>
        /// Did the cycle perception complete - if not the molecule was considered
        /// impractical and computation was aborted.
        /// </summary>
        /// <returns>algorithm completed</returns>
        public bool Completed => completed;
    }
}

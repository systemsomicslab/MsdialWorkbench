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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static NCDK.Common.Base.Preconditions;
using static NCDK.Graphs.InitialCycles;

namespace NCDK.Graphs
{
    /// <summary>
    /// Determine the uniquely defined essential cycles of a graph. A cycle is
    /// essential if it a member of all minimum cycle bases. If a graph has a single
    /// minimum cycle basis (MCB) then all of its cycles are essential. Unlikely the
    /// <see cref="RelevantCycles"/> the number of essential cycles is always polynomial
    /// however may not be able generate the cycle space of a graph.
    /// </summary>
    /// <seealso cref="RelevantCycles"/>
    /// <seealso cref="MinimumCycleBasis"/>
    /// <seealso cref="GraphUtil"/>
    // @author John May
    // @cdk.module core
    // @cdk.keyword essential rings
    // @cdk.keyword essential cycles
    // @cdk.keyword graph
    // @cdk.keyword cycles
    // @cdk.keyword rings
    public sealed class EssentialCycles
    {
        /// <summary>Cycles which are essential.</summary>
        private readonly List<Cycle> essential;

        /// <summary>Initial cycles.</summary>
        private readonly InitialCycles initial;

        /// <summary>An MCB extracted from the relevant cycles.</summary>
        private readonly GreedyBasis basis;

        /// <summary>
        /// Determine the essential cycles given a graph. Adjacency list
        /// representation. For maximum performance the graph should be preprocessed
        /// and run on separate biconnected components or fused cycles (see.
        /// <see cref="RingSearches.RingSearch"/>.
        /// </summary>
        /// <param name="graph">a molecule graph</param>
        /// <seealso cref="GraphUtil.ToAdjList(IAtomContainer)"/>
        /// <seealso cref="RingSearches.RingSearch"/>
        public EssentialCycles(int[][] graph)
            : this(new InitialCycles(graph))
        { }

        /// <summary>
        /// Determine the essential cycles from a precomputed set of initial cycles.
        /// </summary>
        /// <param name="initial">a molecule graph</param>
        internal EssentialCycles(InitialCycles initial)
                : this(new RelevantCycles(initial), initial)
        { }

        /// <summary>
        /// Determine the essential cycles from a precomputed set of initial cycles
        /// and relevant cycles.
        /// </summary>
        /// <param name="relevant"></param>
        /// <param name="initial">a molecule graph</param>
        internal EssentialCycles(RelevantCycles relevant, InitialCycles initial)
        {
            CheckNotNull(relevant, nameof(relevant), "No RelevantCycles provided");
            this.initial = CheckNotNull(initial, nameof(initial), "No InitialCycles provided");
            this.basis = new GreedyBasis(initial.GetNumberOfCycles(), initial.GetNumberOfEdges());
            this.essential = new List<Cycle>();

            // for each cycle added to the basis, if it can be
            // replaced with one of equal size it is non-essential
            foreach (var cycles in GroupByLength(relevant))
            {
                foreach (var c in GetMembersOfBasis(cycles))
                {
                    if (IsEssential(c, cycles)) essential.Add(c);
                }
            }
        }

        /// <summary>
        /// The paths for each essential cycle.
        /// </summary>
        /// <returns>array of vertex paths</returns>
        public int[][] GetPaths()
        {
            int[][] paths = new int[Count][];
            for (int i = 0; i < paths.Length; i++)
                paths[i] = essential[i].Path;
            return paths;
        }

        /// <summary>
        /// Number of essential cycles.
        /// </summary>
        /// <returns>number of cycles</returns>
        public int Count => essential.Count;

        /// <summary>
        /// Reconstruct all relevant cycles and group then by length.
        /// </summary>
        /// <param name="relevant">precomputed relevant cycles</param>
        /// <returns>all relevant cycles groped by weight</returns>
        private IEnumerable<IList<Cycle>> GroupByLength(RelevantCycles relevant)
        {
            List<Cycle> cycle_list = new List<Cycle>();
            foreach (var path in relevant.GetPaths())
            {
                var last = cycle_list.LastOrDefault();
                if (last != null && path.Length > last.Path.Length)
                {
                    yield return cycle_list;
                    cycle_list = new List<Cycle>();
                }
                cycle_list.Add(new MyCycle(this, path));
            }
            if (cycle_list.Count > 0)
                yield return cycle_list;
            yield break;
        }

        /// <summary>
        /// For a list of equal length cycles return those which are members of the
        /// minimum cycle basis.
        /// </summary>
        /// <param name="cycles">cycles to add to the basis</param>
        /// <returns>cycles which were added to the basis</returns>
        private IList<Cycle> GetMembersOfBasis(IEnumerable<Cycle> cycles)
        {
            var ret = new List<Cycle>();
            foreach (var c in cycles)
            {
                if (basis.IsIndependent(c))
                {
                    basis.Add(c);
                    ret.Add(c);
                }
            }
            return ret;
        }

        /// <summary>
        /// Determines whether the <i>cycle</i> is essential.
        /// </summary>
        /// <param name="candidate">a cycle which is a member of the MCB</param>
        /// <param name="relevant">relevant cycles of the same length as <i>cycle</i></param>
        /// <returns>whether the candidate is essential</returns>
        private bool IsEssential(Cycle candidate, ICollection<Cycle> relevant)
        {
            // construct an alternative basis with all equal weight relevant cycles
            IList<Cycle> alternate = new List<Cycle>(relevant.Count + basis.Count);

            int weight = candidate.Length;
            foreach (var cycle in basis.Members)
            {
                if (cycle.Length < weight) alternate.Add(cycle);
            }
            foreach (var cycle in relevant)
            {
                if (!cycle.Equals(candidate)) alternate.Add(cycle);
            }

            // if the alternate basis is smaller, the candidate is essential
            return BitMatrix.From(alternate).Eliminate() < basis.Count;
        }

        /// <summary>
        /// Simple class for helping find the essential cycles from the relevant
        /// cycles.
        /// </summary>
        private class MyCycle
                : Cycle
        {
            public MyCycle(EssentialCycles parent, int[] path)
                : base(parent.initial, null, path)
            {
            }

            /// <inheritdoc/>
            public override BitArray GetEdges(int[] path)
            {
                return parent.ToEdgeVector(path);
            }

            /// <inheritdoc/>
            public override int[][] GetFamily() => new int[][] { Path };

            /// <inheritdoc/>
            public override int SizeOfFamily()
            {
                return 1;
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return Arrays.ToJavaString(base.Path);
            }
        }
    }
}

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
using static NCDK.Graphs.InitialCycles;

namespace NCDK.Graphs
{
    /// <summary>
    /// Greedily compute a cycle basis from a provided set of initial cycles using
    /// Gaussian elimination.
    /// </summary>
    /// <seealso cref="RelevantCycles"/>
    // @author John May
    // @cdk.module core
    internal class GreedyBasis
    {
        /// <summary>Cycles which are members of the basis</summary>
        private readonly List<Cycle> basis;

        /// <summary>Edges of the current basis.</summary>
        private readonly BitArray edgesOfBasis;

        /// <summary>Number of edges</summary>
        private readonly int m;

        /// <summary>
        /// Create a new basis for the <i>potential</i> number of cycles and the
        /// <i>exact</i> number of edges. These values can be obtained from an
        /// <see cref="InitialCycles"/> instance.
        /// </summary>
        /// <param name="n">potential number of cycles in the basis</param>
        /// <param name="m">number of edges in the graph</param>
        /// <seealso cref="InitialCycles.GetNumberOfCycles"/>
        /// <seealso cref="InitialCycles.GetNumberOfEdges"/>
        public GreedyBasis(int n, int m)
        {
            this.basis = new List<Cycle>(n);
            this.edgesOfBasis = new BitArray(m);
            this.m = m;
        }

        /// <summary>
        /// Access the members of the basis.
        /// </summary>
        /// <returns>cycles ordered by length</returns>
        public IReadOnlyList<Cycle> Members => basis;

        /// <summary>
        /// The size of the basis.
        /// </summary>
        /// <returns>number of cycles in the basis</returns>
        public int Count => Members.Count;

        /// <summary>
        /// Add a cycle to the basis.
        /// </summary>
        /// <param name="cycle">new basis member</param>
        public void Add(Cycle cycle)
        {
            basis.Add(cycle);
            edgesOfBasis.Or(cycle.EdgeVector);
        }

        /// <summary>
        /// Add all cycles to the basis.
        /// </summary>
        /// <param name="cycles">new members of the basis</param>
        public void AddAll(IEnumerable<Cycle> cycles)
        {
            foreach (var cycle in cycles)
                Add(cycle);
        }

        /// <summary>
        /// Check if all the edges of the <paramref name="cycle"/> are present in the current
        /// <see cref="basis"/>.
        /// </summary>
        /// <param name="cycle">an initial cycle</param>
        /// <returns>any edges of the basis are present</returns>
        public bool IsSubsetOfBasis(Cycle cycle)
        {
            BitArray edgeVector = cycle.EdgeVector;
            int intersect = BitArrays.Cardinality(And(edgesOfBasis, edgeVector));
            return intersect == cycle.Length;
        }

        /// <summary>
        /// Determine whether the <paramref name="candidate"/> cycle is linearly
        /// <i>independent</i> from the current basis.
        /// </summary>
        /// <param name="candidate">a cycle not in currently in the basis</param>
        /// <returns>the candidate is independent</returns>
        public bool IsIndependent(Cycle candidate)
        {
            // simple checks for independence
            if (basis.Count == 0 || !IsSubsetOfBasis(candidate)) return true;

            BitMatrix matrix = BitMatrix.From(basis, candidate);

            // perform gaussian elimination
            matrix.Eliminate();

            // if the last row (candidate) was eliminated it is not independent
            return !matrix.Eliminated(basis.Count);
        }

        /// <summary>and <paramref name="s"/> and <paramref name="t"/> without modifying <paramref name="s"/></summary>
        private static BitArray And(BitArray s, BitArray t)
        {
            BitArray u = (BitArray)s.Clone();
            u.And(t);
            return u;
        }
    }
}


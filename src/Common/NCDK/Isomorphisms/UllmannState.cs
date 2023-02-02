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
using NCDK.Graphs;

namespace NCDK.Isomorphisms
{
    /// <summary>
    /// A mutable state for matching graphs using the Ullmann algorithm <token>cdk-cite-Ullmann76</token>.
    /// There are a couple of modifications in this implementation.
    /// Firstly the mappings are stored in two vectors m1 and m2 and simply allows us
    /// to return <see cref="Mapping"/>  without searching the compatibility matrix.
    /// Secondly the compatibility matrix is non-binary and instead of removing
    /// entries they are <i>marked</i>. The backtracking then resets these entries
    /// rather and avoids storing/copying the matrix between states.
    /// </summary>
    // @author John May
    // @cdk.module isomorphism
    internal sealed class UllmannState : State
    {
        /// <summary>Adjacency list representations.</summary>
        internal readonly int[][] g1, g2;

        /// <summary>Query and target bond maps.</summary>
        private readonly EdgeToBondMap bond1, bonds2;

        /// <summary>The compatibility matrix.</summary>
        internal readonly CompatibilityMatrix matrix;

        /// <summary>Current mapped values.</summary>
        internal readonly int[] m1, m2;

        /// <summary>Size of the current mapping.</summary>
        internal int size = 0;

        /// <summary>How bond semantics are matched.</summary>
        private readonly BondMatcher bondMatcher;

        /// <summary>Indicates a vertex is unmapped.</summary>
        private static int UNMAPPED = -1;

        /// <summary>
        /// Create a state for matching subgraphs using the Ullmann refinement
        /// procedure.
        /// </summary>
        /// <param name="container1">query container</param>
        /// <param name="container2">target container</param>
        /// <param name="g1">query container adjacency list</param>
        /// <param name="g2">target container adjacency list</param>
        /// <param name="bonds1">query container bond map</param>
        /// <param name="bonds2">target container bond map</param>
        /// <param name="atomMatcher">method of matching atom semantics</param>
        /// <param name="bondMatcher">method of matching bond semantics</param>
        public UllmannState(IAtomContainer container1, IAtomContainer container2, int[][] g1, int[][] g2,
                EdgeToBondMap bonds1, EdgeToBondMap bonds2, AtomMatcher atomMatcher, BondMatcher bondMatcher)
        {
            this.bondMatcher = bondMatcher;
            this.g1 = g1;
            this.g2 = g2;
            this.bond1 = bonds1;
            this.bonds2 = bonds2;
            this.m1 = new int[g1.Length];
            this.m2 = new int[g2.Length];
            Arrays.Fill(m1, UNMAPPED);
            Arrays.Fill(m2, UNMAPPED);

            // build up compatibility matrix
            matrix = new CompatibilityMatrix(g1.Length, g2.Length);
            for (int i = 0; i < g1.Length; i++)
            {
                for (int j = 0; j < g2.Length; j++)
                {
                    if (g1[i].Length <= g2[j].Length && atomMatcher.Matches(container1.Atoms[i], container2.Atoms[j]))
                    {
                        matrix.Set1(i, j);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override int NextN(int n)
        {
            return size; // we progress down the rows of the matrix
        }

        /// <inheritdoc/>
        public override int NextM(int n, int m)
        {
            for (int i = m + 1; i < g2.Length; i++)
                if (m2[i] == UNMAPPED) return i;
            return g2.Length;
        }

        /// <inheritdoc/>
        public override int NMax()
        {
            return g1.Length;
        }

        /// <inheritdoc/>
        public override int MMax()
        {
            return g2.Length;
        }

        /// <inheritdoc/>
        public override bool Add(int n, int m)
        {
            if (!matrix.Get1(n, m)) return false;

            // fix the mapping
            matrix.MarkRow(n, -(n + 1));
            matrix.Set1(n, m);

            // attempt to refine the mapping
            if (Refine(n))
            {
                size = size + 1;
                m1[n] = m;
                m2[m] = n;
                return true;
            }
            else
            {
                // mapping became invalid - unfix mapping
                matrix.ResetRows(n, -(n + 1));
                return false;
            }
        }

        /// <inheritdoc/>
        public override void Remove(int n, int m)
        {
            size--;
            m1[n] = m2[m] = UNMAPPED;
            matrix.ResetRows(n, -(n + 1));
        }

        /// <summary>
        /// Refines the compatibility removing any mappings which have now become
        /// invalid (since the last mapping). The matrix is refined from the row
        /// after the current <paramref name="row"/> - all previous rows are fixed. If when
        /// refined we find a query vertex has no more candidates left in the target
        /// we can never reach a feasible matching and refinement is aborted (false
        /// is returned).
        /// </summary>
        /// <param name="row">refine from here</param>
        /// <returns>match is still feasible</returns>
        private bool Refine(int row)
        {
            int marking = -(row + 1);
            bool changed;
            do
            {
                changed = false;
                // for every feasible mapping verify if it is still valid
                for (int n = row + 1; n < matrix.nRows; n++)
                {
                    for (int m = 0; m < matrix.mCols; m++)
                    {

                        if (matrix.Get1(n, m) && !Verify(n, m))
                        {

                            // remove the now invalid mapping
                            matrix.Mark(n, m, marking);
                            changed = true;

                            // no more mappings for n in the feasibility matrix
                            if (!HasCandidate(n)) return false;
                        }
                    }
                }
            } while (changed);
            return true;
        }

        /// <summary>
        /// Verify that for every vertex adjacent to n, there should be at least one
        /// feasible candidate adjacent which can be mapped. If no such candidate
        /// exists the mapping of n -> m is not longer valid.
        /// </summary>
        /// <param name="n">query vertex</param>
        /// <param name="m">target vertex</param>
        /// <returns>mapping is still valid</returns>
        private bool Verify(int n, int m)
        {
            foreach (var n_prime in g1[n])
            {
                bool found = false;
                foreach (var m_prime in g2[m])
                {
                    if (matrix.Get1(n_prime, m_prime) && bondMatcher.Matches(bond1[n, n_prime], bonds2[m, m_prime]))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }
            return true;
        }

        /// <summary>
        /// Check if there are any feasible mappings left for the query vertex n. We
        /// scan the compatibility matrix to see if any value is > 0.
        /// </summary>
        /// <param name="n">query vertex</param>
        /// <returns>a candidate is present</returns>
        private bool HasCandidate(int n)
        {
            //for (var i = 0; i < matrix.mCols; n++)
            //    if (matrix.Get1(n, i)) return true;
            //return false;
            for (int j = (n * matrix.mCols), end = (j + matrix.mCols); j < end; j++)
                if (matrix.Get1(j)) return true;
            return false;
        }

        /// <inheritdoc/>
        public override int[] Mapping()
        {
            return Arrays.CopyOf(m1, m1.Length);
        }

        /// <inheritdoc/>
        public override int Count => size;
    }
}

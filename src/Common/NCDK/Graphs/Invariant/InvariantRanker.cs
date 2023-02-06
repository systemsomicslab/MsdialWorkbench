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

namespace NCDK.Graphs.Invariant
{
    /// <summary>
    /// A utility for ranking indices by invariants. The ranking is built around
    /// a merge/insertion sort with the primary interaction through <see cref="InvariantRanker.Rank(int[], int[], int, long[], long[])"/> .
    /// </summary>
    /// <seealso href="http://algs4.cs.princeton.edu/22mergesort/">Mergesort</seealso >
    /// <seealso cref="Canon"/>
    // @author John May
    // @cdk.module standard
    internal sealed class InvariantRanker
    {
        /// <summary>Auxiliary array for merge sort.</summary>
        private readonly int[] aux;

        /// <summary>
        /// Length at which the sub-array should be sorted using insertion sort. As
        /// insertion sort is adaptive and in-place it's advantageous to use a high
        /// threshold for this use-case. Once we do the first sort, the invariants
        /// will always be 'almost' sorted which is the best case for the insertion
        /// sort.
        /// </summary>
        private const int InsertionSortThreshold = 42;

        /// <summary>
        /// Create an invariant ranker for <paramref name="n"/> invariants.
        /// </summary>
        /// <param name="n">number of values</param>
        public InvariantRanker(int n)
        {
            this.aux = new int[n];
        }

        /// <summary>
        /// Given an array of equivalent indices (currEq) and their values (curr)
        /// assign a rank to the values. The values are sorted using 'prev' and
        /// 'curr' invariants and once ranked the new ranks placed in 'prev'. The
        /// values which are still equivalent are placed in 'nextEq' and terminated
        /// by a '-1'.
        /// </summary>
        /// <param name="currEq">currently equivalent vertices (initially identity)</param>
        /// <param name="nextEq">equivalent vertices (to refine) will be set by this method</param>
        /// <param name="n">the number of currently equivalent vertices</param>
        /// <param name="curr">the current invariants</param>
        /// <param name="prev">the prev invariants (initially = curr) used to sort and then store ranks (set by this method)</param>
        /// <returns>the number of ranks</returns>
        public int Rank(int[] currEq, int[] nextEq, int n, long[] curr, long[] prev)
        {
            SortBy(currEq, 0, n, curr, prev);

            // with the values sorted we now partition the values in to those
            // which are unique and aren't unique.

            // we use the aux array memory but to make it easier to read we alias
            // nu: number unique, nnu: number non-unique
            int nEquivalent = 0;

            // values are partitioned we now need to assign the new ranks. unique
            // values are assigned first then the non-unique ranks. we know which
            // rank to start at by seeing how many have already been assigned. this
            // is given by (|V| - |current non unique|).
            int nRanks = 1 + curr.Length - n;

            int[] tmp = aux;

            int u = currEq[0];
            int labelTick = tmp[u] = (int)prev[u];
            long label = labelTick;

            for (int i = 1; i < n; i++)
            {
                int v = currEq[i];

                if (prev[v] != tmp[u])
                    labelTick = (int)prev[v];
                else
                    labelTick++;

                if (curr[v] != curr[u] || prev[v] != tmp[u])
                {
                    tmp[v] = (int)prev[v];
                    prev[v] = labelTick;
                    label = labelTick;
                    nRanks++;
                }
                else
                {
                    if (nEquivalent == 0 || nextEq[nEquivalent - 1] != u) nextEq[nEquivalent++] = u;
                    nextEq[nEquivalent++] = v;
                    tmp[v] = (int)prev[v];
                    prev[v] = label;
                }

                u = v;
            }

            if (nEquivalent < nextEq.Length) nextEq[nEquivalent] = -1;

            return nRanks;
        }

        /// <summary>
        /// Sort the values (using merge sort) in <paramref name="vs"/> from <paramref name="lo"/> (until
        /// <paramref name="len"/>) by the <paramref name="prev"/>[] and then <paramref name="curr"/>[] invariants to
        /// determine rank. The values in <paramref name="vs"/> are indices into the invariant
        /// arrays.
        /// </summary>
        /// <param name="vs">values (indices)</param>
        /// <param name="lo">the first value to start sorting from</param>
        /// <param name="len">the len of values to consider</param>
        /// <param name="curr">the current invariants</param>
        /// <param name="prev">the previous invariants</param>
        public void SortBy(int[] vs, int lo, int len, long[] curr, long[] prev)
        {
            if (len < InsertionSortThreshold)
            {
                InsertionSortBy(vs, lo, len, curr, prev);
                return;
            }

            int split = len / 2;

            SortBy(vs, lo, split, curr, prev);
            SortBy(vs, lo + split, len - split, curr, prev);

            // sub arrays already sorted, no need to merge
            if (!Less(vs[lo + split], vs[lo + split - 1], curr, prev)) return;

            Merge(vs, lo, split, len, curr, prev);
        }

        /// <summary>
        /// Merge the values which are sorted between <paramref name="lo"/> - <paramref name="split"/> and
        /// <paramref name="split"/> - <paramref name="len"/>.
        /// </summary>
        /// <param name="vs">vertices</param>
        /// <param name="lo">start index</param>
        /// <param name="split">the middle index (partition)</param>
        /// <param name="len">the range to merge</param>
        /// <param name="curr">the current invariants</param>
        /// <param name="prev">the previous invariants</param>
        private void Merge(int[] vs, int lo, int split, int len, long[] curr, long[] prev)
        {
            Array.Copy(vs, lo, aux, lo, len);
            int i = lo, j = lo + split;
            int iMax = lo + split, jMax = lo + len;
            for (int k = lo, end = lo + len; k < end; k++)
            {
                if (i == iMax)
                    vs[k] = aux[j++];
                else if (j == jMax)
                    vs[k] = aux[i++];
                else if (Less(aux[i], aux[j], curr, prev))
                    vs[k] = aux[i++];
                else
                    vs[k] = aux[j++];
            }
        }

        /// <summary>
        /// Sort the values (using insertion sort) in <paramref name="vs"/> from <paramref name="lo"/>
        /// (until <paramref name="len"/>) by the <paramref name="prev"/>[] and then <paramref name="curr"/>[]
        /// invariants to determine rank. The values in <paramref name="vs"/> are indices into
        /// the invariant arrays.
        /// </summary>
        /// <param name="vs">values (indices)</param>
        /// <param name="lo">the first value to start sorting from</param>
        /// <param name="len">the len of values to consider</param>
        /// <param name="curr">the current invariants</param>
        /// <param name="prev">the previous invariants</param>
        public static void InsertionSortBy(int[] vs, int lo, int len, long[] curr, long[] prev)
        {
            for (int j = lo + 1, hi = lo + len; j < hi; j++)
            {
                int v = vs[j];
                int i = j - 1;
                while ((i >= lo) && Less(v, vs[i], curr, prev))
                    vs[i + 1] = vs[i--];
                vs[i + 1] = v;
            }
        }

        /// <summary>
        /// Using the <paramref name="prev"/> and <paramref name="curr"/> invariants is value in index i
        /// less than <paramref name="j"/>. Value i is less than j if it was previously less than <paramref name="j"/>
        /// (<paramref name="prev"/>[]) or it was equal and it is now (<paramref name="curr"/>[]) less than <paramref name="j"/>.
        /// </summary>
        /// <param name="i">an index</param>
        /// <param name="j">an index</param>
        /// <param name="curr">current invariants</param>
        /// <param name="prev">previous invariants</param>
        /// <returns>is the value in index i less than j</returns>
        public static bool Less(int i, int j, long[] curr, long[] prev)
        {
            return prev[i] < prev[j] || prev[i] == prev[j] && curr[i] < curr[j];
        }
    }
}

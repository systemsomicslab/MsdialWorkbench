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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using static NCDK.Graphs.InitialCycles;

namespace NCDK.Graphs
{
    /// <summary>
    /// Mutable bit matrix which can eliminate linearly dependent rows and check
    /// which rows were eliminated. These operations are useful when constructing a
    /// cycle basis. From a graph we can represent the cycles as a binary vector of
    /// incidence (edges). When processing cycles as these vectors we determine
    /// whether a cycle can be made of other cycles in our basis. In the example
    /// below each row can be made by XORing the other two rows.
    /// </summary>
    /// <example>
    /// <pre>
    /// 1:   111000111   (can be made by 2 XOR 3)
    /// 2:   111000000   (can be made by 1 XOR 3)
    /// 3:   000000111   (can be made by 1 XOR 2)
    /// </pre>
    /// <code>
    /// BitMatrix m = new BitMatrix(9, 3);
    /// m.Add(ToBitArray("111000111"));
    /// m.Add(ToBitArray("111000000"));
    /// m.Add(ToBitArray("111000000"));
    /// if (m.Eliminate() &lt; 3) {
    ///   // rows are not independent
    /// }
    /// </code></example>
    // @author John May
    // @cdk.module core
    internal sealed class BitMatrix
    {
        /// <summary>rows of the matrix.</summary>
        private readonly BitArray[] rows;

        /// <summary>keep track of row swaps.</summary>
        private readonly int[] indices;

        /// <summary>maximum number of rows.</summary>
        private readonly int max;

        /// <summary>number of columns.</summary>
        private readonly int n;

        /// <summary>current number of rows.</summary>
        private int m;

        /// <summary>
        /// Create a new bit matrix with the given number of columns and rows. Note
        /// the rows is the <i>maximum</i> number of rows we which to store. The
        /// actual row count only increases with <see cref="Add(BitArray)"/>.
        /// </summary>
        /// <param name="columns">number of columns</param>
        /// <param name="rows">number of rows</param>
        public BitMatrix(int columns, int rows)
        {
            this.n = columns;
            this.max = rows;
            this.rows = new BitArray[rows];
            this.indices = new int[rows];
        }

        /// <summary>
        /// Swap the rows <paramref name="i"/> and <paramref name="j"/>, the swap is kept track of
        /// internally allowing <see cref="Row(int)"/> and <see cref="Eliminated(int)"/> to
        /// access the index of the original row.
        /// </summary>
        /// <param name="i">row index</param>
        /// <param name="j">row index</param>
        public void Swap(int i, int j)
        {
            BitArray row = rows[i];
            int k = indices[i];
            rows[i] = rows[j];
            indices[i] = indices[j];
            rows[j] = row;
            indices[j] = k;
        }

        /// <summary>
        /// Find the current index of row <paramref name="j"/>.
        /// </summary>
        /// <param name="j">original row index to find</param>
        /// <returns>the index now or &lt; 0 if not found</returns>
        private int RowIndex(int j)
        {
            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] == j) return i;
            }
            return -1;
        }

        /// <summary>
        /// Access the row which was added at index <paramref name="j"/>.
        /// </summary>
        /// <param name="j">index of row</param>
        /// <returns>the row which was added at index <paramref name="j"/></returns>
        public BitArray Row(int j)
        {
            return rows[RowIndex(j)];
        }

        /// <summary>
        /// Check whether the row which was added at index <paramref name="j"/> has been
        /// eliminated. <see cref="Eliminate()"/> should be invoked first.
        /// </summary>
        /// <param name="j">row index</param>
        /// <returns>whether the row was eliminated</returns>
        /// <seealso cref="Eliminate()"/>
        public bool Eliminated(int j)
        {
            return BitArrays.IsEmpty(Row(j));
        }

        /// <summary>Clear the matrix, setting the number of rows to 0.</summary>
        public void Clear()
        {
            m = 0;
        }

        /// <summary>
        /// Add a row to the matrix.
        ///
        /// <param name="row">the row</param>
        /// </summary>
        public void Add(BitArray row)
        {
            if (m >= max)
                throw new InvalidOperationException("initialise matrix with more rows");
            rows[m] = row;
            indices[m] = m;
            m++;
        }

        /// <summary>
        /// Eliminate rows from the matrix which can be made by linearly combinations
        /// of other rows.
        /// </summary>
        /// <returns>rank of the matrix</returns>
        /// <seealso cref="Eliminated(int)"/>
        public int Eliminate()
        {
            return Eliminate(0, 0);
        }

        /// <summary>
        /// Gaussian elimination.
        /// </summary>
        /// <param name="x">current column index</param>
        /// <param name="y">current row index</param>
        /// <returns>the rank of the matrix</returns>
        private int Eliminate(int x, int y)
        {
            while (x < n && y < m)
            {

                int i = IndexOf(x, y);

                if (i < 0) return Eliminate(x + 1, y);

                // reorder rows
                if (i != y) Swap(i, y);

                // xor row with all vectors that have x set
                // note: rows above y are not touched, this isn't an issue in
                //       it's current use (cycle basis) as we only care about
                //       new additions being independent. However starting from
                //       j = 0 allows you to change this but of course is slower.
                for (int j = y + 1; j < m; j++)
                    if (rows[j][x]) rows[j] = Xor(rows[j], rows[y]);

                y++;
            }
            return y;
        }

        /// <summary>
        /// Index of the the first row after <paramref name="y"/> where <paramref name="x"/> is set.
        /// </summary>
        /// <param name="x">column index</param>
        /// <param name="y">row index</param>
        /// <returns>the first index where <paramref name="x"/> is set, index is &lt; 0 if none</returns>
        internal int IndexOf(int x, int y)
        {
            for (int j = y; j < m; j++)
            {
                if (rows[j][x]) return j;
            }
            return -1;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int j = 0; j < m; j++)
            {
                sb.Append(indices[j]).Append(": ");
                for (int i = 0; i < n; i++)
                {
                    sb.Append(rows[j][i] ? '1' : '-');
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>
        /// Utility method xors the vectors <paramref name="u"/> and <paramref name="v"/>. Neither
        /// input is modified.
        /// </summary>
        /// <param name="u">a bit set</param>
        /// <param name="v">a bit set</param>
        /// <returns>the 'xor' of <paramref name="u"/> and <paramref name="v"/></returns>
        internal static BitArray Xor(BitArray u, BitArray v)
        {
            BitArray w = (BitArray)u.Clone();
            w.Xor(v);
            return w;
        }

        /// <summary>
        /// Simple creation of a BitMatrix from a collection of cycles.
        ///
        /// <param name="cycles">cycles to create the matrix from</param>
        /// <returns>instance of a BitMatrix for the cycles</returns>
        /// </summary>
        public static BitMatrix From(IEnumerable<Cycle> cycles)
        {

            int rows = 0, cols = 0;
            foreach (var c in cycles)
            {
                if (c.EdgeVector.Count > cols) cols = c.EdgeVector.Count;
                rows++;
            }

            BitMatrix matrix = new BitMatrix(cols, rows);
            foreach (var c in cycles)
                matrix.Add(c.EdgeVector);
            return matrix;
        }

        /// <summary>
        /// Simple creation of a BitMatrix from a collection of cycles. The final
        /// cycle will be added as the last row of the matrix. The <i>cycle</i>
        /// should no be found in <i>cycles</i>.
        ///
        /// <param name="cycles">cycles to create</param>
        /// <param name="cycle">final cycle to add</param>
        /// <returns>instance of a BitMatrix for the cycles</returns>
        /// </summary>
        public static BitMatrix From(IEnumerable<Cycle> cycles, Cycle cycle)
        {

            int rows = 1, cols = cycle.EdgeVector.Count;
            foreach (var c in cycles)
            {
                if (c.EdgeVector.Count > cols) cols = c.EdgeVector.Count;
                rows++;
            }

            BitMatrix matrix = new BitMatrix(cols, rows);
            foreach (var c in cycles)
                matrix.Add(c.EdgeVector);
            matrix.Add(cycle.EdgeVector);
            return matrix;
        }
    }
}

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

namespace NCDK.Isomorphisms
{
    /// <summary>
    /// A compatibility matrix defines which query vertices (rows) could possible be
    /// mapped to a target vertex (columns). The matrix is used in the Ullmann and
    /// Ullmann-like algorithms to provide top-down pruning.
    ///
    /// Instead of using a binary matrix this implementation uses int values. This
    /// allows us to remove a mapping but put it back in later (backtrack).
    /// </summary>
    /// <seealso cref="UllmannState"/>
    // @author John May
    // @cdk.module isomorphism
    internal sealed class CompatibilityMatrix
    {
        /// <summary>Value storage.</summary>
        readonly int[] data;

        /// <summary>Size of the matrix.</summary>
        internal readonly int nRows, mCols;

        /// <summary>
        /// Create a matrix of the given size.
        /// </summary>
        /// <param name="nRows">number of rows</param>
        /// <param name="mCols">number of columns</param>
        public CompatibilityMatrix(int nRows, int mCols)
        {
            this.data = new int[nRows * mCols];
            this.nRows = nRows;
            this.mCols = mCols;
        }

        /// <summary>
        /// Set the value in row, <paramref name="i"/> and column <paramref name="j"/>.
        /// </summary>
        /// <param name="i">row index</param>
        /// <param name="j">column index</param>
        public void Set1(int i, int j)
        {
            data[(i * mCols) + j] = 1;
        }

        /// <summary>
        /// Access the value at index <paramref name="i"/>, values wrap around to the next row.
        /// </summary>
        /// <param name="i">index</param>
        /// <returns>the value is set</returns>
        public bool Get1(int i)
        {
            return data[i] > 0;
        }

        /// <summary>
        /// Access the value at row <paramref name="i"/> and column <paramref name="j"/>. The values wrap around to the
        /// next row.
        /// <param name="i">row index</param>
        /// <param name="j">column index</param>
        /// <returns>the value is set</returns>
        /// </summary>
        public bool Get1(int i, int j)
        {
            return Get1((i * mCols) + j);
        }

        /// <summary>
        /// Mark the value in row <paramref name="i"/> and column <paramref name="j"/> allowing it to be reset later.
        /// </summary>
        /// <param name="i">row index</param>
        /// <param name="j">column index</param>
        /// <param name="marking">the marking to store (should be negative)</param>
        public void Mark(int i, int j, int marking)
        {
            data[(i * mCols) + j] = marking;
        }

        /// <summary>
        /// Mark all values in row <paramref name="i"/> allowing it to be reset later.
        /// </summary>
        /// <param name="i">row index</param>
        /// <param name="marking">the marking to store (should be negative)</param>
        public void MarkRow(int i, int marking)
        {
            for (int j = (i * mCols), end = j + mCols; j < end; j++)
                if (data[j] > 0) data[j] = marking;
        }

        /// <summary>
        /// Reset all values marked with (marking) from row <paramref name="i"/> onwards.
        /// </summary>
        /// <param name="i">row index</param>
        /// <param name="marking">the marking to reset (should be negative)</param>
        public void ResetRows(int i, int marking)
        {
            for (int j = (i * mCols); j < data.Length; j++)
                if (data[j] == marking) data[j] = 1;
        }

        /// <summary>
        /// Create a fixed-size 2D array of the matrix (useful for debug).
        /// </summary>
        /// <returns>a fixed version of the matrix</returns>
        public int[][] Fix()
        {
            int[][] m = Arrays.CreateJagged<int>(nRows, mCols);
            for (int i = 0; i < nRows; i++)
                Array.Copy(data, (i * mCols), m[i], 0, mCols);
            return m;
        }
    }
}

/* Copyright (C) 2015  The Chemistry Development Kit (CDK) project
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Renderers.Elements;
using System;
using System.Collections.Generic;

namespace NCDK.Depict
{
    /// <summary>
    /// Internal: Immutable value class to help with store diagram dimensions
    /// (a tuple width and height). Given some dimensions we can add/subtract,
    /// grow/shrink as needed. Utility methods are provided for laying out rendering
    /// elements in grids and rows.
    /// </summary>
    internal sealed class Dimensions
    {
        /// <summary>
        /// Magic value for automated sizing.
        /// </summary>
        public readonly static Dimensions Automatic = new Dimensions(DepictionGenerator.Automatic, DepictionGenerator.Automatic);

        public readonly double width;
        public readonly double height;

        public Dimensions(double width, double height)
        {
            this.width = width;
            this.height = height;
        }

        public double Width => width;
        public double Height => height;

        internal Dimensions Add(double width, double height)
        {
            return new Dimensions(this.width + width, this.height + height);
        }

        internal Dimensions Scale(double coef)
        {
            return new Dimensions(coef * width, coef * height);
        }

        internal static Dimensions OfRow(IList<Bounds> elems)
        {
            return OfGrid(elems, 1, elems.Count);
        }

        internal static Dimensions OfCol(IList<Bounds> elems)
        {
            return OfGrid(elems, elems.Count, 1);
        }

        internal static Dimensions OfGrid(IList<Bounds> bounds, int nRow, int nCol)
        {
            return OfGrid(bounds, new double[nRow + 1], new double[nCol + 1]);
        }

        /// <summary>
        /// Determine how much space is needed to depiction the bound <see cref="IRenderingElement"/> if
        /// they were aligned in a grid without padding or margins. The method takes arrays
        /// for the offset which are one item bigger than the size of the gird
        /// (e.g. 3x2 would need arrays of length 4 and 2). The arrays are filled with the
        /// cumulative width/heights for each grid point allowing easy alignment.
        /// </summary>
        /// <param name="bounds">bound rendering elements</param>
        /// <param name="yOffset">array for col offsets</param>
        /// <param name="xOffset">array for row offset</param>
        /// <returns>the dimensions required</returns>
        internal static Dimensions OfGrid(IList<Bounds> bounds, double[] yOffset, double[] xOffset)
        {
            int nRow = yOffset.Length - 1;
            int nCol = xOffset.Length - 1;

            int nBounds = bounds.Count;
            for (int i = 0; i < nBounds; i++)
            {
                // +1 because first offset is always 0
                int col = 1 + i % nCol;
                int row = 1 + i / nCol;
                var bound = bounds[i];
                if (bound.IsEmpty())
                    continue;
                var width = bound.Width;
                var height = bound.Height;
                if (width > xOffset[col])
                    xOffset[col] = width;
                if (height > yOffset[row])
                    yOffset[row] = height;
            }

            for (int i = 1; i < yOffset.Length; i++)
                yOffset[i] += yOffset[i - 1];
            for (int i = 1; i < xOffset.Length; i++)
                xOffset[i] += xOffset[i - 1];

            return new Dimensions(xOffset[nCol], yOffset[nRow]);
        }

        /// <summary>
        /// Determine grid size (nrow, ncol) that could be used
        /// for displaying a given number of elements.
        /// </summary>
        /// <param name="nElem">number of elements</param>
        /// <returns>grid dimensions (integers)</returns>
        internal static Dimension DetermineGrid(int nElem)
        {
            switch (nElem)
            {
                case 0:
                    return new Dimension(0, 0);
                case 1:
                    return new Dimension(1, 1);
                case 2:
                    return new Dimension(2, 1);
                case 3:
                    return new Dimension(3, 1);
                case 4:
                    return new Dimension(2, 2);
                case 5:
                    return new Dimension(3, 2);
                case 6:
                    return new Dimension(3, 2);
                case 7:
                    return new Dimension(4, 2);
                case 8:
                    return new Dimension(4, 2);
                case 9:
                    return new Dimension(3, 3);
                default:
                    // not great but okay
                    int nrow = (int)Math.Floor(Math.Sqrt(nElem));
                    int ncol = (int)Math.Ceiling(nElem / (double)nrow);
                    return new Dimension(ncol, nrow);
            }
        }

        public override string ToString()
        {
            return Math.Ceiling(width) + "x" + Math.Ceiling(height);
        }
    }
}

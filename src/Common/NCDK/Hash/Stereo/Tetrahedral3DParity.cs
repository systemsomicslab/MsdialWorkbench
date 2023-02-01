/*
 * Copyright (c) 2013 John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using System;
using NCDK.Numerics;

namespace NCDK.Hash.Stereo
{
    /// <summary>
    /// Geometric parity for 3D tetrahedral geometry. This class requires four 3D
    /// coordinates. The 3D coordinates correspond to the four ligands of a
    /// tetrahedral atom. If a tetrahedral atom has an implicit hydrogen (only 3
    /// ligands) the forth coordinate should be that of the atom at the centre
    /// <see href="http://www.mdpi.org/molecules/papers/61100915/61100915.htm">Cieplak, T and Wisniewski, J.L. 2001</see>.
    /// </summary>
    // @author John May
    // @cdk.module hash
    internal sealed class Tetrahedral3DParity : GeometricParity
    {
        /* array of four 3D coordinates */
        private readonly Vector3[] coordinates;

        /// <summary>
        /// Create a new geometric parity for 3D tetrahedral geometry by specifying
        /// the coordinates.
        /// </summary>
        /// <param name="coordinates">non-null, 4 3D coordinates</param>
        /// <exception cref="ArgumentException">if the number of coordinates was not 4</exception>
        public Tetrahedral3DParity(Vector3[] coordinates)
        {
            if (coordinates.Length != 4) throw new ArgumentException("4 coordinates expected");

            this.coordinates = coordinates;
        }

        public override int Parity
        {
            get
            {
                double x1 = coordinates[0].X;
                double x2 = coordinates[1].X;
                double x3 = coordinates[2].X;
                double x4 = coordinates[3].X;

                double y1 = coordinates[0].Y;
                double y2 = coordinates[1].Y;
                double y3 = coordinates[2].Y;
                double y4 = coordinates[3].Y;

                double z1 = coordinates[0].Z;
                double z2 = coordinates[1].Z;
                double z3 = coordinates[2].Z;
                double z4 = coordinates[3].Z;

                double det = (z1 * Det(x2, y2, x3, y3, x4, y4)) - (z2 * Det(x1, y1, x3, y3, x4, y4))
                        + (z3 * Det(x1, y1, x2, y2, x4, y4)) - (z4 * Det(x1, y1, x2, y2, x3, y3));

                return (int)Math.Sign(det);
            }
        }

        // 3x3 determinant helper for a constant third column
        static double Det(double xa, double ya, double xb, double yb, double xc, double yc)
        {
            return (xa - xc) * (yb - yc) - (ya - yc) * (xb - xc);
        }
    }
}

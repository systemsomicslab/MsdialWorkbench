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
    /// Calculate the geometric configuration of a double bond. The configuration is
    /// provided as a parity (+1,-1) where +1 indicates the substituents are on
    /// <i>opposite</i> sides (E or trans) and -1 indicates they are <i>together</i>
    /// on the same side (Z or cis).
    /// </summary>
    // @author John May
    // @cdk.module hash
    internal sealed class DoubleBond3DParity : GeometricParity
    {
        // coordinates of the double bond atoms:
        // x       w
        //  \     /
        //   u = v
        private Vector3 u, v, x, w;

        /// <summary>
        /// Create a new double bond parity for the 2D coordinates of the atoms.
        /// </summary>
        /// <param name="left">one atom of the double bond</param>
        /// <param name="right">the other atom of a double bond</param>
        /// <param name="leftSubstituent">the substituent atom connected to the left atom</param>
        /// <param name="rightSubstituent">the substituent atom connected to the right atom</param>
        public DoubleBond3DParity(Vector3 left, Vector3 right, Vector3 leftSubstituent, Vector3 rightSubstituent)
        {
            this.u = left;
            this.v = right;
            this.x = leftSubstituent;
            this.w = rightSubstituent;
        }

        /// <summary>
        /// Calculate the configuration of the double bond as a parity.
        /// </summary>
        /// <returns>opposite (+1), together (-1)</returns>
        public override int Parity
        {
            get
            {
                // create three vectors, v->u, v->w and u->x
                double[] vu = ToVector(v, u);
                double[] vw = ToVector(v, w);
                double[] ux = ToVector(u, x);

                // normal vector (to compare against), the normal vector (n) looks like:
                // x     n w
                //  \    |/
                //   u = v
                double[] normal = CrossProduct(vu, CrossProduct(vu, vw));

                // compare the dot products of v->w and u->x, if the signs are the same
                // they are both pointing the same direction. if a value is close to 0
                // then it is at pi/2 radians (i.e. unspecified) however 3D coordinates
                // are generally discrete and do not normally represent on unspecified
                // stereo configurations so we don't check this
                int parity = (int)Math.Sign(Dot(normal, vw)) * (int)Math.Sign(Dot(normal, ux));

                // invert sign, this then matches with Sp2 double bond parity
                return parity * -1;
            }
        }

        /// <summary>
        /// Create a vector by specifying the source and destination coordinates.
        /// </summary>
        /// <param name="src">start point of the vector</param>
        /// <param name="dest">end point of the vector</param>
        /// <returns>a new vector</returns>
        private static double[] ToVector(Vector3 src, Vector3 dest)
        {
            return new double[] { dest.X - src.X, dest.Y - src.Y, dest.Z - src.Z };
        }

        /// <summary>
        /// Dot product of two 3D coordinates
        /// </summary>
        /// <param name="u">either 3D coordinates</param>
        /// <param name="v">other 3D coordinates</param>
        /// <returns>the dot-product</returns>
        private static double Dot(double[] u, double[] v)
        {
            return (u[0] * v[0]) + (u[1] * v[1]) + (u[2] * v[2]);
        }

        /// <summary>
        /// Cross product of two 3D coordinates
        /// </summary>
        /// <param name="u">either 3D coordinates</param>
        /// <param name="v">other 3D coordinates</param>
        /// <returns>the cross-product</returns>
        private static double[] CrossProduct(double[] u, double[] v)
        {
            return new double[] { (u[1] * v[2]) - (v[1] * u[2]), (u[2] * v[0]) - (v[2] * u[0]), (u[0] * v[1]) - (v[0] * u[1]) };
        }
    }
}

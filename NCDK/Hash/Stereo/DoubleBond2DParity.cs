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
    /// provided as a parity (+1,0,-1) where +1 indicates the substituents are on
    /// <i>opposite</i> sides (E or trans) and -1 indicates they are <i>together</i>
    /// on the same side (Z or cis). If one of the substituents is parallel to the
    /// double bond then the configuration is unspecified and 0 is returned.
    /// </summary>
    // @author John May
    // @cdk.module hash
    internal sealed class DoubleBond2DParity : GeometricParity
    {
        // coordinates of the double bond atoms
        // l1      r1
        //  \     /
        //   l = r
        //  /     \
        // l2      r2
        private Vector2 l, r, l1, r1, l2, r2;

        /// <summary>
        /// the area below which we return unspecified parity
        /// </summary>
        private const double Threshold = 0.1;

        /// <summary>
        /// Create a new double bond parity for the 2D coordinates of the atoms.
        /// </summary>
        /// <param name="left">one atom of the double bond</param>
        /// <param name="right">the other atom of a double bond</param>
        /// <param name="leftSubstituent">the substituent atom connected to the left atom</param>
        /// <param name="rightSubstituent">the substituent atom connected to the right atom</param>
        public DoubleBond2DParity(Vector2 left, Vector2 right, Vector2 leftSubstituent, Vector2 rightSubstituent)
        {
            this.l = left;
            this.r = right;
            this.l1 = leftSubstituent;
            this.r1 = rightSubstituent;
            this.l2 = l;
            this.r2 = r;
        }

        /// <summary>
        /// Create a new double bond parity for the 2D coordinates of the atoms. This
        /// method is required for cases where both substituents may lie on the same
        /// side of a bond. If one of the sides has two substituents and the other
        /// side has two then you can pass left/right atom of the double bond as
        /// the second substituent.
        /// </summary>
        /// <example>
        ///  l1      r1
        ///   \     /
        ///    l = r
        ///   /
        ///  l2
        ///
        ///  should be passed as:
        /// <code>
        ///      new DoubleBond2DParity(l, r, l1, l2, r1, r);
        /// </code>
        /// </example>
        /// <param name="left">one atom of the double bond</param>
        /// <param name="right">the other atom of a double bond</param>
        /// <param name="leftSubstituent1">first substituent atom connected to the left atom</param>
        /// <param name="leftSubstituent2">second substituent atom connected to the left atom</param>
        /// <param name="rightSubstituent1">first substituent atom connected to the right atom</param>
        /// <param name="rightSubstituent2">second substituent atom connected to the right atom</param>
        public DoubleBond2DParity(Vector2 left, Vector2 right, Vector2 leftSubstituent1, Vector2 leftSubstituent2, Vector2 rightSubstituent1, Vector2 rightSubstituent2)
        {
            this.l = left;
            this.r = right;
            this.l1 = leftSubstituent1;
            this.r1 = rightSubstituent1;
            this.l2 = leftSubstituent2;
            this.r2 = rightSubstituent2;
        }

        /// <summary>
        /// Calculate the configuration of the double bond as a parity.
        /// </summary>
        /// <returns>opposite (+1), together (-1) or unspecified (0)</returns>
        public override int Parity
        {
            get
            {
                return GetParity(l1, l2, r) * GetParity(r1, r2, l);
            }
        }

        /// <summary>
        /// Determine the rotation parity of one side of the double bond. This parity
        /// is the sign of the area of a triangle.
        /// <pre>
        /// a
        ///  \
        ///   b = c
        /// </pre>
        /// </summary>
        /// <param name="a">coordinates of the substituent atom</param>
        /// <param name="b">coordinates of the atom next to the substituent</param>
        /// <param name="c">coordinates of the atom double bonds to <i>b</i></param>
        /// <returns>clockwise (+1), anti-clockwise (-1) or unspecified (0)</returns>
        private static int GetParity(Vector2 a, Vector2 b, Vector2 c)
        {
            double det = (a.X - c.X) * (b.Y - c.Y) - (a.Y - c.Y) * (b.X - c.X);

            return Math.Abs(det) < Threshold ? 0 : (int)Math.Sign(det);
        }
    }
}

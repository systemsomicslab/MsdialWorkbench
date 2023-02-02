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

namespace NCDK.Hash.Stereo
{
    /// <summary>
    /// Combine two permutation parities into one.
    /// </summary>
    // @author John May
    // @cdk.module hash
    internal sealed class CombinedPermutationParity : PermutationParity
    {
        private readonly PermutationParity left;
        private readonly PermutationParity right;

        /// <summary>
        /// Combines the left and right parity into a single parity. This parity is
        /// the product of the two separate parities.
        /// </summary>
        /// <param name="left">either parity</param>
        /// <param name="right">other parity</param>
        public CombinedPermutationParity(PermutationParity left, PermutationParity right)
        {
            this.left = left;
            this.right = right;
        }

        public override int Parity(long[] current)
        {
            return left.Parity(current) * right.Parity(current);
        }
    }
}

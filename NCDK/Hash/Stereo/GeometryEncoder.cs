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

namespace NCDK.Hash.Stereo
{
    /// <summary>
    /// Given a geometric parity and a permutation parity encode the parity of the
    /// combination at the specified stereo centre indices.
    /// </summary>
    // @author John May
    // @cdk.module hash
    internal sealed class GeometryEncoder : IStereoEncoder
    {
        /* value for a clockwise configuration */
        private const long Clockwise = 15543053;

        /* value for a anticlockwise configuration */
        private const long AntiClockwise = 15521419;

        /* for calculation the permutation parity */
        private readonly PermutationParity permutation;

        /* for calculating the geometric parity */
        private readonly GeometricParity geometric;

        /* index to encode */
        private readonly int[] centres;

        /// <summary>
        /// Create a new encoder for multiple stereo centres (specified as an array).
        /// </summary>
        /// <param name="centres">the stereo centres which will be configured</param>
        /// <param name="permutation">calculator for permutation parity</param>
        /// <param name="geometric">geometric calculator</param>
        /// <exception cref="ArgumentException">if the centres[] were empty</exception>
        public GeometryEncoder(int[] centres, PermutationParity permutation, GeometricParity geometric)
        {
            if (centres.Length == 0) throw new ArgumentException("no centres[] provided");
            this.permutation = permutation;
            this.geometric = geometric;
            this.centres = new int[centres.Length];
            Array.Copy(centres, this.centres, centres.Length);
        }

        /// <summary>
        /// Convenience method to create a new encoder for a single stereo centre.
        /// </summary>
        /// <param name="centre">a stereo centre which will be configured</param>
        /// <param name="permutation">calculator for permutation parity</param>
        /// <param name="geometric">geometric calculator</param>
        /// <exception cref="ArgumentException">if the centres[] were empty</exception>
        public GeometryEncoder(int centre, PermutationParity permutation, GeometricParity geometric)
            : this(new int[] { centre }, permutation, geometric)
        { }

        /// <summary>
        /// Encodes the <see cref="centres"/> specified in the constructor as either
        /// clockwise/anticlockwise or none. If there is a permutation parity but no
        /// geometric parity then we can not encode the configuration and 'true' is
        /// returned to indicate the perception is done. If there is no permutation
        /// parity this may changed with the next <paramref name="current"/>[] values and so
        /// <see langword="false"/> is returned.
        /// </summary>
        /// <inheritdoc/>
        public bool Encode(long[] current, long[] next)
        {
            int p = permutation.Parity(current);

            // if is a permutation parity (all neighbors are different)
            if (p != 0)
            {
                // multiple with the geometric parity
                int q = geometric.Parity * p;

                // configure anticlockwise/clockwise
                if (q > 0)
                {
                    foreach (var i in centres)
                    {
                        next[i] = current[i] * AntiClockwise;
                    }
                }
                else if (q < 0)
                {
                    foreach (var i in centres)
                    {
                        next[i] = current[i] * Clockwise;
                    }
                }

                // 0 parity ignored

                return true;
            }
            return false;
        }

        public void Reset()
        {
            // never inactive
        }
    }
}

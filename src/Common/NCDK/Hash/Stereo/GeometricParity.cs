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
    /// Describes the geometric parity of a stereo configuration.
    /// </summary>
    // @author John May
    // @cdk.module hash
    internal abstract class GeometricParity
    {
        /// <summary>
        /// Calculate the geometric parity.
        /// </summary>
        /// <returns>-1 odd, +1 even and 0 none</returns>
        public abstract int Parity { get; }

        /// <summary>
        /// Simple implementation allows us to wrap a predefined parity up for access
        /// later. See <see cref="TetrahedralElementEncoderFactory"/> for usage example.
        /// </summary>
        private sealed class Predefined : GeometricParity
        {
            int parity;

            public override int Parity => parity;

            /// <summary>
            /// Create a new predefined geometric parity.
            /// </summary>
            /// <param name="parity">value of the parity</param>
            public Predefined(int parity)
            {
                this.parity = parity;
            }
        }

        /// <summary>
        /// Create a geometric parity from a pre-stored value (-1, 0, +1).
        /// </summary>
        /// <param name="parity">existing parity</param>
        /// <returns>instance which when invoked will return the value</returns>
        public static GeometricParity ValueOf(int parity)
        {
            return new Predefined(parity);
        }
    }
}

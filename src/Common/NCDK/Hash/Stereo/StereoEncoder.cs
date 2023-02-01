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
    /// An encoder for stereo chemistry. The stereo configuration is encoded by
    /// checking the <c>current[]</c> invariant values. If there is a configuration
    /// then the appropriate value is the <c>next[]</c> is modified.
    /// </summary>
    // @author John May
    // @cdk.module hash
    public interface IStereoEncoder
    {
        /// <summary>
        /// Encode one or more stereo elements based on the current invariants. If
        /// any stereo element are uncovered then the corresponding value in the
        /// <paramref name="nextInvariants"/> array is modified.
        /// </summary>
        /// <param name="current">current invariants</param>
        /// <param name="nextInvariants">next invariants</param>
        /// <returns>whether any stereo configurations were encoded</returns>
        bool Encode(long[] current, long[] nextInvariants);

        /// <summary>
        /// Reset the stereo-encoders, any currently perceived configurations will be
        /// re-activated.
        /// </summary>
        void Reset();
    }

    public static class StereoEncoder
    {
        /// <summary>
        /// empty stereo encoder when no stereo can be perceived
        /// </summary>
        public static readonly IStereoEncoder Empty = new EmptyStereoEncoder();

        class EmptyStereoEncoder : IStereoEncoder
        {
            public bool Encode(long[] current, long[] next)
            {
                return false;
            }

            public void Reset()
            {
            }
        }
    }
}

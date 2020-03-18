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

using NCDK.Common.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Hash.Stereo
{
    /// <summary>
    /// A multiple stereo encoder. Given a list of other encoders this class wraps
    /// them up into a single method call. Once each encoder has been configured it
    /// is marked and will not be visited again unless the encoder is <see cref="Reset"/>. 
    /// </summary>
    // @author John May
    // @cdk.module hash
    internal sealed class MultiStereoEncoder : IStereoEncoder
    {
        /* indices of unconfigured encoders */
        private readonly BitArray unconfigured;

        /* list of encoders */
        private readonly IReadOnlyList<IStereoEncoder> encoders;

        /// <summary>
        /// Create a new multiple stereo encoder from a single list of encoders
        /// </summary>
        public MultiStereoEncoder(IList<IStereoEncoder> encoders)
        {
            if (encoders.Count == 0)
                throw new ArgumentException("no stereo encoders provided");
            this.encoders = new List<IStereoEncoder>(encoders);
            this.unconfigured = new BitArray(encoders.Count);
            BitArrays.Flip(unconfigured, encoders.Count);
        }

        public bool Encode(long[] current, long[] next)
        {
            bool configured = false;

            for (int i = BitArrays.NextSetBit(unconfigured, 0); i >= 0; i = BitArrays.NextSetBit(unconfigured, i + 1))
            {
                if (encoders[i].Encode(current, next))
                {
                    unconfigured.Set(i, false); // don't configure again (unless reset)
                    configured = true;
                }
            }
            return configured;
        }

        public void Reset()
        {
            // mark all as unperceived and reset
            for (int i = 0; i < encoders.Count; i++)
            {
                unconfigured.Set(i, true);
                encoders[i].Reset();
            }
        }
    }
}

/*
 * Copyright (C) 2012   Syed Asad Rahman <asad@ebi.ac.uk>
 *
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
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using NCDK.Maths;

namespace NCDK.Fingerprints
{
    /// <summary>
    /// Generates pseudorandom numbers using the MersenneTwister method from commons-math.
    /// </summary>
    // @author Syed Asad Rahman (2012)
    // @cdk.keyword fingerprint
    // @cdk.keyword similarity
    // @cdk.module fingerprint
    public class RandomNumber
    {
        [NonSerialized]
        private MersenneTwister rg = new MersenneTwister();

        /// <summary>
        /// Mersenne Twister Random Number for a hashcode within a range between 0 to n.
        /// </summary>
        /// <param name="n">the maximum value the</param>
        /// <param name="seed">the seed for the next pseudorandom number</param>
        /// <returns>next pseudorandom number</returns>
        public int GenerateMersenneTwisterRandomNumber(int n, long seed)
        {
            rg.SetSeed(seed);
            return rg.NextInt(n);
        }
    }
}

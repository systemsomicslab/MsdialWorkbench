/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace NCDK.Maths
{
    /// <summary>
    /// This class implements a powerful pseudo-random number generator
    /// developed by Makoto Matsumoto and Takuji Nishimura during
    /// 1996-1997.
    /// </summary>
    /// <remarks>
    /// <para>This generator features an extremely long period
    /// (2<sup>19937</sup>-1) and 623-dimensional equidistribution up to 32
    /// bits accuracy. The home page for this generator is located at 
    /// <see href="http://www.math.sci.hiroshima-u.ac.jp/~m-mat/MT/emt.html">http://www.math.sci.hiroshima-u.ac.jp/~m-mat/MT/emt.html</see>.
    /// </para>
    /// <para>
    /// This generator is described in a paper by Makoto Matsumoto and
    /// Takuji Nishimura in 1998: <see href="http://www.math.sci.hiroshima-u.ac.jp/~m-mat/MT/ARTICLES/mt.pdf">Mersenne Twister: A 623-Dimensionally Equidistributed Uniform Pseudo-Random Number Generator</see>, 
    /// ACM Transactions on Modeling and Computer
    /// Simulation, Vol. 8, No. 1, January 1998, pp 3--30
    /// </para>
    /// <para>
    /// This class is mainly a Java port of the 2002-01-26 version of
    /// the generator written in C by Makoto Matsumoto and Takuji
    /// Nishimura. Here is their original copyright:
    /// </para>
    /// <list type="table">
    /// <item>
    /// <term>Copyright (C) 1997 - 2002, Makoto Matsumoto and Takuji Nishimura, All rights reserved.</term>
    /// </item>
    /// <item>
    /// <term>Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
    /// <list type="bullet">
    ///   <item>Redistributions of source code must retain the above copyright
    ///       notice, this list of conditions and the following disclaimer.</item>
    ///   <item>Redistributions in binary form must reproduce the above copyright
    ///       notice, this list of conditions and the following disclaimer in the
    ///       documentation and/or other materials provided with the distribution.</item>
    ///   <item>The names of its contributors may not be used to endorse or promote
    ///       products derived from this software without specific prior written
    ///       permission.</item>
    /// </list>
    /// </term>
    /// <item><strong>THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
    /// CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
    /// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
    /// MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
    /// DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS
    /// BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY,
    /// OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
    /// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
    /// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY
    /// OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
    /// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
    /// USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH
    /// DAMAGE.</strong></item></item>
    /// </list> 
    /// </remarks>
    // @version $Id: MersenneTwister.java 1244107 2012-02-14 16:17:55Z erans $
    // @since 2.0
    internal class MersenneTwister : BitsStreamGenerator
    {
        /// <summary>Size of the bytes pool. </summary>
        private const int N = 624;

        /// <summary>Period second parameter. </summary>
        private const int M = 397;

        /// <summary>X * MATRIX_A for X = {0, 1}. </summary>
        private static readonly uint[] MAG01 = { 0x0, 0x9908b0df };

        /// <summary>Bytes pool. </summary>
        private uint[] mt;

        /// <summary>Current index in the bytes pool. </summary>
        private uint mti;

        /// <summary>
        /// Creates a new random number generator.
        /// <para>The instance is initialized using the current time plus the system identity hash code of this instance as the seed.</para>
        /// </summary>
        public MersenneTwister()
        {
            mt = new uint[N];
            SetSeed(System.DateTime.Now.Ticks);
        }

        /// <summary>
        /// Creates a new random number generator using a single int seed.
        /// </summary>
        /// <param name="seed">the initial seed (32 bits integer)</param>
        public MersenneTwister(int seed)
        {
            mt = new uint[N];
            SetSeed(seed);
        }

        /// <summary>
        /// Creates a new random number generator using an int array seed.
        /// </summary>
        /// <param name="seed">the initial seed (32 bits integers array), if null the seed of the generator will be related to the current time</param>
        public MersenneTwister(int[] seed)
        {
            mt = new uint[N];
            SetSeed(seed);
        }

        /// <summary>
        /// Creates a new random number generator using a single long seed.
        /// </summary>
        /// <param name="seed">the initial seed (64 bits integer)</param>
        public MersenneTwister(long seed)
        {
            mt = new uint[N];
            SetSeed(seed);
        }

        /// <summary>
        /// Reinitialize the generator as if just built with the given int seed
        /// <para>The state of the generator is exactly the same as a new generator built with the same seed.</para>
        /// </summary>
        /// <param name="seed">the initial seed (32 bits integer)</param>
        public override void SetSeed(int seed)
        {
            // we use a long masked by 0xffffffffL as a poor man unsigned int
            long longMT = seed;
            // NB: unlike original C code, we are working with java longs, the cast below makes masking unnecessary
            mt[0] = (uint)longMT;
            for (mti = 1; mti < N; ++mti)
            {
                // See Knuth TAOCP Vol2. 3rd Ed. P.106 for multiplier.
                // initializer from the 2002-01-09 C version by Makoto Matsumoto
                longMT = (1812433253L * (longMT ^ (longMT >> 30)) + mti) & 0xffffffffL;
                mt[mti] = (uint)longMT;
            }

            Clear(); // Clear normal deviate cache
        }

        /// <summary>
        /// Reinitialize the generator as if just built with the given int array seed.
        /// <para>The state of the generator is exactly the same as a new
        /// generator built with the same seed.</para>
        /// </summary>
        /// <param name="seed"> the initial seed (32 bits integers array), if null the seed of the generator will be the current system time plus the system identity hash code of this instance</param>
        public override void SetSeed(int[] seed)
        {
            if (seed == null)
            {
                SetSeed(System.DateTime.Now.Ticks);
                return;
            }

            SetSeed(19650218);
            int i = 1;
            int j = 0;

            for (int k = Math.Max(N, seed.Length); k != 0; k--)
            {
                long l0 = (mt[i] & 0x7fffffffL) | (((int)mt[i] < 0) ? 0x80000000L : 0x0L);
                long l1 = (mt[i - 1] & 0x7fffffffL) | (((int)mt[i - 1] < 0) ? 0x80000000L : 0x0L);
                long l = (l0 ^ ((l1 ^ (l1 >> 30)) * 1664525L)) + seed[j] + j; // non linear
                mt[i] = (uint)(l & 0xffffffffL);
                i++; j++;
                if (i >= N)
                {
                    mt[0] = mt[N - 1];
                    i = 1;
                }
                if (j >= seed.Length)
                {
                    j = 0;
                }
            }

            for (int k = N - 1; k != 0; k--)
            {
                long l0 = (mt[i] & 0x7fffffffL) | (((int)mt[i] < 0) ? 0x80000000L : 0x0L);
                long l1 = (mt[i - 1] & 0x7fffffffL) | (((int)mt[i - 1] < 0) ? 0x80000000L : 0x0L);
                long l = (l0 ^ ((l1 ^ (l1 >> 30)) * 1566083941L)) - i; // non linear
                mt[i] = (uint)(l & 0xffffffffL);
                i++;
                if (i >= N)
                {
                    mt[0] = mt[N - 1];
                    i = 1;
                }
            }

            mt[0] = 0x80000000; // MSB is 1; assuring non-zero initial array

            Clear(); // Clear normal deviate cache
        }

        /// <summary>
        /// Reinitialize the generator as if just built with the given long seed.
        /// <para>The state of the generator is exactly the same as a new generator built with the same seed.</para>
        /// </summary>
        /// <param name="seed">seed the initial seed (64 bits integer)</param>
        public override void SetSeed(long seed)
        {
            SetSeed(new int[] { (int)(((ulong)seed) >> 32), (int)(seed & 0xffffffffL) });
        }

        /// <summary>
        /// Generate next pseudorandom number.
        /// </summary>
        /// <param name="bits">number of random bits to produce</param>
        /// <returns>random bits generated</returns>
        protected override uint GenerateNext(int bits)
        {
            uint y;

            if (mti >= N)
            { // generate N words at one time
                uint mtNext = mt[0];
                for (int k = 0; k < N - M; ++k)
                {
                    uint mtCurr = mtNext;
                    mtNext = mt[k + 1];
                    y = (mtCurr & 0x80000000) | (mtNext & 0x7fffffff);
                    mt[k] = mt[k + M] ^ (y >> 1) ^ MAG01[y & 0x1];
                }
                for (int k = N - M; k < N - 1; ++k)
                {
                    uint mtCurr = mtNext;
                    mtNext = mt[k + 1];
                    y = (mtCurr & 0x80000000) | (mtNext & 0x7fffffff);
                    mt[k] = mt[k + (M - N)] ^ (y >> 1) ^ MAG01[y & 0x1];
                }
                y = (mtNext & 0x80000000) | (mt[0] & 0x7fffffff);
                mt[N - 1] = mt[M - 1] ^ (y >> 1) ^ MAG01[y & 0x1];

                mti = 0;
            }

            y = mt[mti++];

            // tempering
            y ^= y >> 11;
            y ^= (y << 7) & 0x9d2c5680;
            y ^= (y << 15) & 0xefc60000;
            y ^= y >> 18;

            return y >> (32 - bits);
        }
    }
}


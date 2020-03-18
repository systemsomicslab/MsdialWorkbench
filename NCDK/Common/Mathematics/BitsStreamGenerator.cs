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

namespace NCDK.Common.Mathematics
{
    /// <summary> 
    /// Base class for random number generators that generates bits streams.
    /// </summary>
    // @version $Id: BitsStreamGenerator.java 1244107 2012-02-14 16:17:55Z erans $
    // @since 2.0
    public abstract class BitsStreamGenerator
    {
       /// <summary>Next gaussian.</summary>
        private double nextGaussian;

        /// <summary> Creates a new random number generator.</summary>
        protected BitsStreamGenerator()
        {
            nextGaussian = double.NaN;
        }

        /// <inheritdoc/>
        public abstract void SetSeed(int seed);

        /// <inheritdoc/>
        public abstract void SetSeed(int[] seed);

        /// <inheritdoc/>
        public abstract void SetSeed(long seed);

        /// <summary>Generate next pseudorandom number.
        /// </summary>
        /// <remarks>
        /// <para>This method is the core generation algorithm. It is used by all the
        /// public generation methods for the various primitive types <see cref="NextBool()"/>,
        /// <see cref="NextBytes(byte[])"/>, <see cref="NextDouble()"/>,
        /// <see cref="NextFloat()"/>, <see cref="NextGaussian()"/>, <see cref="Next()"/>,
        /// <see cref="GetNext(int)"/> and <see cref="NextLong()"/>.</para>
        /// </remarks>
        /// <param name="bits">number of random bits to produce</param>
        /// <returns>random bits generated</returns>
        protected abstract uint GetNext(int bits);

        /// <inheritdoc/>
        public bool NextBool()
        {
            return GetNext(1) != 0;
        }

        /// <inheritdoc/>
        public void NextBytes(byte[] bytes)
        {
            int i = 0;
            int iEnd = bytes.Length - 3;
            while (i < iEnd)
            {
                uint random = GetNext(32);
                bytes[i] = (byte)(random & 0xff);
                bytes[i + 1] = (byte)((random >> 8) & 0xff);
                bytes[i + 2] = (byte)((random >> 16) & 0xff);
                bytes[i + 3] = (byte)((random >> 24) & 0xff);
                i += 4;
            }
            {
                uint random = GetNext(32);
                while (i < bytes.Length)
                {
                    bytes[i++] = (byte)(random & 0xff);
                    random = random >> 8;
                }
            }
        }

        private const double Double_1_40000000000000 = 1.0d / 0x40000000000000;

        /// <inheritdoc/>
        public double NextDouble()
        {
            ulong high = ((ulong)GetNext(26)) << 26;
            ulong low = (ulong)GetNext(26);
            ulong s = high | low;
            return Double_1_40000000000000 * s;
        }

        private const double Double_1_2000000 = 1.0d / 0x2000000;

        /// <inheritdoc/>
        public float NextFloat()
        {
            return (float)(GetNext(23) * Double_1_2000000);
        }

        /// <inheritdoc/>
        public double NextGaussian()
        {
            double random;
            if (double.IsNaN(nextGaussian))
            {
                // generate a new pair of gaussian numbers
                double x = NextDouble();
                double y = NextDouble();
                double alpha = 2 * Math.PI * x;
                double r = Math.Sqrt(-2 * Math.Log(y));
                random = r * Math.Cos(alpha);
                nextGaussian = r * Math.Sin(alpha);
            }
            else
            {
                // use the second element of the pair already generated
                random = nextGaussian;
                nextGaussian = double.NaN;
            }

            return random;
        }

        /// <inheritdoc/>
        public int Next()
        {
            return (int)GetNext(32);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <remarks>
        /// <para>This default implementation is copied from Apache Harmony java.util.Random (r929253).</para>
        /// <note type="note">
        /// <para>If n is a power of 2, this method returns <c>(int) ((n * (long) Next(31)) >> 31)}</c>.</para>
        /// <item>If n is not a power of 2, what is returned is <c>Next(31) % n</c>
        /// with <c>Next(31)</c> values rejected (i.e. regenerated) until a
        /// value that is larger than the remainder of <c>int.MaxValue / n</c>
        /// is generated. Rejection of this initial segment is necessary to ensure
        /// a uniform distribution.</item></note>
        /// </remarks>
        public int NextInt(int n)
        {
            if (n > 0)
            {
                if ((n & -n) == n)
                {
                    var nn = (ulong)GetNext(31);
                    return (int)(((ulong)n * nn) >> 31);
                }
                int bits;
                int val;
                do
                {
                    bits = (int)GetNext(31);
                    val = bits % n;
                } while (bits - val + (n - 1) < 0);
                return val;
            }
            throw new ArgumentOutOfRangeException(nameof(n));
        }

        /// <inheritdoc/>
        public long NextLong()
        {
            long high = ((long)GetNext(32)) << 32;
            long low = ((long)GetNext(32)) & 0xffffffffL;
            return high | low;
        }

        /// <summary>
        /// Clears the cache used by the default implementation of <see cref="nextGaussian"/>.
        /// </summary>
        public void Clear()
        {
            nextGaussian = double.NaN;
        }
    }
}

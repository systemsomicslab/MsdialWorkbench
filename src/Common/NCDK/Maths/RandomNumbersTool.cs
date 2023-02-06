/* Copyright (C) 1997-2007  The Chemistry Development Kit (CDK) project
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

namespace NCDK.Maths
{
    internal class Random
    {
        public Random()
            : this(DateTime.Now.Ticks)
        { }

        public Random(long seed)
            : this((UInt64)seed)
        { }

        public Random(UInt64 seed)
        {
            this.seed = (seed ^ 0x5DEECE66DUL) & ((1UL << 48) - 1);
        }

        public int NextInt(int n)
        {
            if (n <= 0) throw new ArgumentException("n must be positive");

            if ((n & -n) == n)  // i.e., n is a power of 2
                return (int)((n * (long)Next(31)) >> 31);

            long bits, val;
            do
            {
                bits = Next(31);
                val = bits % (UInt32)n;
            }
            while (bits - val + (n - 1) < 0);

            return (int)val;
        }

        protected UInt32 Next(int bits)
        {
            seed = (seed * 0x5DEECE66DL + 0xBL) & ((1L << 48) - 1);

            return (UInt32)(seed >> (48 - bits));
        }

        private UInt64 seed;
    }

    /// <summary>
    /// Class supplying useful methods to generate random numbers.
    /// This class isn't supposed to be instantiated. You should use it by calling
    /// its static methods.
    /// </summary>
    // @cdk.module standard
    public static class RandomNumbersTool
    {
        private static long randomSeed = DateTime.Now.Ticks;

        /// <summary>
        /// The instance of Random used by this class.
        /// </summary>
        public static BitsStreamGenerator Random { get; set; } = new MersenneTwister(randomSeed);

        /// <summary>
        /// The seed being used by this random number generator.
        /// </summary>
        public static long RandomSeed
        {
            get
            {
                return randomSeed;
            }
            set
            {
                randomSeed = value;
                Random.SetSeed(randomSeed);
            }
        }

        /// <summary>
        ///  Generates a random integer between '0' and '1'.
        /// </summary>
        /// <returns> a random integer between '0' and '1'.</returns>
        public static int RandomInt() => RandomInt(0, 1);

        /// <summary>
        /// Generates a random integer between the specified values.
        /// </summary>
        /// <param name="lo">the lower bound for the generated integer.</param>
        /// <param name="hi">the upper bound for the generated integer.</param>
        /// <returns>a random integer between <paramref name="lo"/> and <paramref name="hi"/>.</returns>
        public static int RandomInt(int lo, int hi)
        {
            return (Math.Abs(Random.Next()) % (hi - lo + 1)) + lo;
        }

        /// <summary>
        /// Generates a random long between '0' and '1'.
        /// </summary>
        /// <returns>a random long between '0' and '1'.</returns>
        public static long RandomLong()
        {
            return RandomLong(0, 1);
        }

        /// <summary>
        /// Generates a random long between the specified values.
        /// </summary>
        /// <param name="lo">the lower bound for the generated long.</param>
        /// <param name="hi">the upper bound for the generated long.</param>
        /// <returns>a random long between <paramref name="lo"/> and <paramref name="hi"/>.</returns>
        public static long RandomLong(long lo, long hi)
        {
            return NextLong(Random, hi - lo + 1L) + lo;
        }

        /// <summary>
        /// Access the next long random number between 0 and n.
        /// </summary>
        /// <param name="rng">random number generator</param>
        /// <param name="n">max value</param>
        /// <returns>a long random number between 0 and n</returns>
        /// <seealso href="http://stackoverflow.com/questions/2546078/java-random-long-number-in-0-x-n-range">Random Long Number in range, Stack Overflow</seealso >
        private static long NextLong(BitsStreamGenerator rng, long n)
        {
            if (n <= 0) throw new ArgumentException("n must be greater than 0");
            long bits, val;
            do
            {
                bits = (long)((ulong)(rng.NextLong() << 1) >> 1);
                val = bits % n;
            } while (bits - val + (n - 1) < 0L);
            return val;
        }

        /// <summary>
        /// Generates a random float between '0' and '1'.
        /// </summary>
        /// <returns>a random float between '0' and '1'.</returns>
        public static float RandomFloat()
        {
            return Random.NextFloat();
        }

        /// <summary>
        /// Generates a random float between the specified values.
        /// </summary>
        /// <param name="lo">the lower bound for the generated float.</param>
        /// <param name="hi">the upper bound for the generated float.</param>
        /// <returns>a random float between <paramref name="lo"/> and <paramref name="hi"/>.</returns>
        public static float RandomFloat(float lo, float hi)
        {
            return (hi - lo) * Random.NextFloat() + lo;
        }

        /// <summary>
        /// Generates a random double between '0' and '1'.
        /// </summary>
        /// <returns>a random double between '0' and '1'.</returns>
        public static double RandomDouble()
        {
            return Random.NextDouble();
        }

        /// <summary>
        /// Generates a random double between the specified values.
        /// </summary>
        /// <param name="lo">the lower bound for the generated double.</param>
        /// <param name="hi">the upper bound for the generated double.</param>
        /// <returns>a random double between <paramref name="lo"/> and <paramref name="hi"/>.</returns>
        public static double RandomDouble(double lo, double hi)
        {
            return (hi - lo) * Random.NextDouble() + lo;
        }

        /// <summary>
        /// Generates a random <see cref="Boolean"/>.
        /// </summary>
        /// <returns>a random <see cref="Boolean"/>.</returns>
        public static bool RandomBoolean()
        {
            return (RandomInt() == 1);
        }
        
        /// <summary>
        /// Generates a random bit: either '0' or '1'.
        /// </summary>
        /// <returns>a random bit.</returns>
        public static int RandomBit()
        {
            return RandomInt();
        }

        /// <summary>
        /// Returns a boolean value based on a biased coin toss.
        /// </summary>
        /// <param name="p">the probability of success.</param>
        /// <returns><see langword="true"/> if a success was found; <see langword="false"/> otherwise.</returns>
        public static bool FlipCoin(double p)
        {
            return (RandomDouble() < p ? true : false);
        }

        /// <summary>
        /// Generates a random float from a Gaussian distribution with the specified deviation.
        /// </summary>
        /// <param name="dev">the desired deviation.</param>
        /// <returns>a random float from a Gaussian distribution with deviation <paramref name="dev"/>.</returns>
        public static float GaussianFloat(float dev)
        {
            return (float)Random.NextGaussian() * dev;
        }

        /// <summary>
        /// Generates a random double from a Gaussian distribution with the specified deviation.
        /// </summary>
        /// <param name="dev">the desired deviation.</param>
        /// <returns>a random float from a Gaussian distribution with deviation <paramref name="dev"/>.</returns>
        public static double GaussianDouble(double dev)
        {
            return Random.NextGaussian() * dev;
        }

        /// <summary>
        /// Generates a random double from an Exponential distribution with the specified
        /// mean value.
        /// </summary>
        /// <param name="mean">the desired mean value.</param>
        /// <returns>a random double from an Exponential distribution with mean value <paramref name="mean"/>.</returns>
        public static double ExponentialDouble(double mean)
        {
            return -mean * Math.Log(RandomDouble());
        }
    }

    /// <summary>
    /// Base class for random number generators that generates bits streams.
    /// </summary>
    // @since 2.0
    public abstract class BitsStreamGenerator
    {
        /// <summary>Next gaussian.</summary>
        private double nextGaussian;

        /// <summary>Creates a new random number generator.</summary>
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

        /// <summary> 
        /// Generate next pseudorandom number.
        /// <para>
        /// This method is the core generation algorithm. It is used by all the
        /// public generation methods for the various primitive types 
        /// <see cref="NextBool()"/>,
        /// <see cref="NextBytes(byte[])"/>, 
        /// <see cref="NextDouble()"/>,
        /// <see cref="NextFloat()"/>,
        /// <see cref="NextGaussian()"/>,
        /// <see cref="Next()"/>,
        /// <see cref="NextInt(int)"/>,
        /// <see cref="NextLong()"/>
        /// </para>
        /// </summary>
        /// <param name="bits">number of random bits to produce</param>
        /// <returns>random bits generated</returns>
        protected abstract uint GenerateNext(int bits);

        /// <inheritdoc/>
        public bool NextBool()
        {
            return GenerateNext(1) != 0;
        }

        /// <inheritdoc/>
        public void NextBytes(byte[] bytes)
        {
            int i = 0;
            int iEnd = bytes.Length - 3;
            while (i < iEnd)
            {
                uint random = GenerateNext(32);
                bytes[i] = (byte)(random & 0xff);
                bytes[i + 1] = (byte)((random >> 8) & 0xff);
                bytes[i + 2] = (byte)((random >> 16) & 0xff);
                bytes[i + 3] = (byte)((random >> 24) & 0xff);
                i += 4;
            }
            {
                uint random = GenerateNext(32);
                while (i < bytes.Length)
                {
                    bytes[i++] = (byte)(random & 0xff);
                    random = random >> 8;
                }
            }
        }

        private const double Double_1_10000000000000 = 1.0d / 0x10000000000000;

        /// <inheritdoc/>
        public double NextDouble()
        {
            ulong high = ((ulong)GenerateNext(26)) << 26;
            ulong low = (ulong)GenerateNext(26);
            ulong s = high | low;
            var ret = Double_1_10000000000000 * s;
            return ret;
        }

        private const double Double_1_800000 = 1.0d / 0x800000;

        /// <inheritdoc/>
        public float NextFloat()
        {
            var ret = Double_1_800000 * GenerateNext(23);
            return (float)ret;
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
            return (int)GenerateNext(32);
        }

        /// <summary>
        /// This default implementation is copied from Apache Harmony java.util.Random (r929253).
        /// </summary>
        /// <remarks>
        /// Implementation notes: 
        /// <list type="bullet">
        /// <item>If n is a power of 2, this method returns <c>(int) ((n * (long) Next(31)) >> 31)</c>.</item>
        /// <item>If n is not a power of 2, what is returned is <c>Next(31) % n</c>
        /// with <c>Next(31)</c> values rejected (i.e. regenerated) until a
        /// value that is larger than the remainder of <c>int.MaxValue / n</c>
        /// is generated. Rejection of this initial segment is necessary to ensure
        /// a uniform distribution.</item>
        /// </list>
        /// </remarks>
        public int NextInt(int n)
        {
            if (n > 0)
            {
                if ((n & -n) == n)
                {
                    var nn = (ulong)GenerateNext(31);
                    return (int)(((ulong)n * nn) >> 31);
                }
                int bits;
                int val;
                do
                {
                    bits = (int)GenerateNext(31);
                    val = bits % n;
                } while (bits - val + (n - 1) < 0);
                return val;
            }
            throw new ArgumentOutOfRangeException(nameof(n));
        }

        /// <inheritdoc/>
        public long NextLong()
        {
            long high = ((long)GenerateNext(32)) << 32;
            long low = ((long)GenerateNext(32)) & 0xffffffffL;
            return high | low;
        }

        /// <summary>
        /// Clears the cache used by the default implementation of <see cref="NextGaussian()"/> 
        /// </summary>
        public void Clear()
        {
            nextGaussian = double.NaN;
        }
    }
 }

/*
 * Java compatible Random clsss.
 * This code is from http://stackoverflow.com/questions/2147524/c-java-number-randomization .
 */

using System;

namespace NCDK.Common.Mathematics
{
    public class JavaRandom
    {
        public JavaRandom(int seed)
            : this((long)seed)
        {
        }

        public JavaRandom(long seed)
        {
            this.seed = ((ulong)seed ^ 0x5DEECE66DUL) & ((1UL << 48) - 1);
        }

        public int Next(int n)
        {
            if (n <= 0) throw new ArgumentException("n must be positive");

            if ((n & -n) == n)  // i.e., n is a power of 2
                return (int)((n * (long)BitNext(31)) >> 31);

            long bits, val;
            do
            {
                bits = BitNext(31);
                val = bits % (UInt32)n;
            }
            while (bits - val + (n - 1) < 0);

            return (int)val;
        }

        protected UInt32 BitNext(int bits)
        {
            seed = (seed * 0x5DEECE66DL + 0xBL) & ((1L << 48) - 1);

            return (UInt32)(seed >> (48 - bits));
        }

        private UInt64 seed;
    }
}

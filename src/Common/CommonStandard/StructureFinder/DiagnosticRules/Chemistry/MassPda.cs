using System.Numerics;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Chemistry
{
    /// <summary>
    /// Integer pico-dalton arithmetic (design 4.1). Code written once against this interface runs on
    /// <see cref="LongPda"/> (checked 64-bit, the normal case) and, when that overflows, again on
    /// <see cref="BigPda"/>, so that every result equals the Python reference's unbounded integers.
    /// </summary>
    internal interface IPdaMath<T>
    {
        T Zero { get; }
        T FromLong(long value);
        /// <summary>Throws <see cref="System.OverflowException"/> when the type cannot hold the value.</summary>
        T FromBig(BigInteger value);
        T Add(T a, T b);
        T Sub(T a, T b);
        T Mul(T a, T b);
        int Compare(T a, T b);
        BigInteger ToBig(T value);
    }

    /// <summary>Checked <see cref="long"/> arithmetic: any overflow throws.</summary>
    internal struct LongPda : IPdaMath<long>
    {
        public long Zero => 0;
        public long FromLong(long value) => value;
        public long FromBig(BigInteger value) => (long)value;
        public long Add(long a, long b) => checked(a + b);
        public long Sub(long a, long b) => checked(a - b);
        public long Mul(long a, long b) => checked(a * b);
        public int Compare(long a, long b) => a.CompareTo(b);
        public BigInteger ToBig(long value) => value;
    }

    internal struct BigPda : IPdaMath<BigInteger>
    {
        public BigInteger Zero => BigInteger.Zero;
        public BigInteger FromLong(long value) => value;
        public BigInteger FromBig(BigInteger value) => value;
        public BigInteger Add(BigInteger a, BigInteger b) => a + b;
        public BigInteger Sub(BigInteger a, BigInteger b) => a - b;
        public BigInteger Mul(BigInteger a, BigInteger b) => a * b;
        public int Compare(BigInteger a, BigInteger b) => a.CompareTo(b);
        public BigInteger ToBig(BigInteger value) => value;
    }

    /// <summary>Constants of the mass arithmetic.</summary>
    public static class MassPda
    {
        /// <summary>Every mass and target must lie in (0, 2^53) pDa.</summary>
        public const long Limit = 9007199254740992L;

        /// <summary>A member within 1.0 Da of the hypothesis precursor is never evidence (design 3.6 item 6, 4.4);
        /// the comparison carries the engine's 1e-10 Da slack, which is 100 pDa.</summary>
        public const long PrecursorRegion = 1000000000000L + 100L;
    }
}

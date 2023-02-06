using NCDK.Common.Base;
using NCDK.Common.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace NCDK.Common.Collections
{
    internal static class BitArrays
    {
        public static readonly IEqualityComparer<BitArray> EqualityComparer = new BitArrayComparer();

        internal sealed class BitArrayComparer : IEqualityComparer<BitArray>
        {
            public bool Equals(BitArray x, BitArray y)
            {
                return Compares.AreEqual(x, y);
            }

            public int GetHashCode(BitArray obj)
            {
                return BitArrays.GetHashCode(obj);
            }
        }

        public static bool GetValue(BitArray a, int index)
        {
            if (a.Length > index)
                return a[index];
            return false;
        }

        public static void SetValue(BitArray a, int index, bool value)
        {
            if (a.Length <= index)
                a.Length = index + 1;
            a.Set(index, value);
        }

        public static string AsBitString(BitArray a)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < a.Length; i++)
                sb.Append(a[i] ? '0' : '1');
            return sb.ToString();
        }

        public static BitArray FromString(string str)
        {
            str = str.Trim();
            if (str.StartsWithChar('{') && str.EndsWithChar('}'))
            {
                var ret = new BitArray(0);
                str = str.Substring(1, str.Length - 2);
                foreach (var index in str.Split(',').Select(n => n.Trim()).Select(n => int.Parse(n, NumberFormatInfo.InvariantInfo)))
                {
                    BitArrays.SetValue(ret, index, true);
                }
                return ret;
            }
            else
            {
                var ret = new BitArray(str.Length);
                for (var i = 0; i < str.Length; i++)
                {
                    var c = str[i];
                    switch (c)
                    {
                        case '0':
                            ret[i] = false;
                            break;
                        case '1':
                            ret[i] = true;
                            break;
                        default:
                            throw new ArgumentException(nameof(FromString), nameof(str));
                    }
                }
                return ret;
            }
        }

        public static void EnsureCapacity(BitArray a, int length)
        {
            if (a.Length < length)
                a.Length = length;
        }

        public static void Flip(this BitArray a, int bitIndex)
        {
            Flip(a, 0, bitIndex);
        }

        public static void Flip(this BitArray a, int fromIndex, int toIndex)
        {
            if (fromIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(fromIndex));
            if (toIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(toIndex));
            if (fromIndex > toIndex)
                throw new ArgumentException($"{nameof(fromIndex)} is greater than {nameof(toIndex)}.");
            EnsureCapacity(a, toIndex);
            for (int i = fromIndex; i < a.Length; i++)
                a[i] = !a[i];
        }

        public static bool Intersects(BitArray a, BitArray b)
        {
            var n = Math.Min(a.Length, b.Length);
            for (int i = 0; i < n; i++)
                if (a[i] && b[i])
                    return true;
            return false;
        }

        public static bool IsEmpty(this BitArray a)
        {
            for (int i = 0; i < a.Length; i++)
                if (a[i])
                    return false;
            return true;
        }

        public static int Cardinality(this BitArray a)
        {
            int count = 0;
            for (int i = 0; i < a.Length; i++)
                if (a[i])
                    count++;
            return count;
        }

        public static int NextClearBit(BitArray a, int fromIndex)
        {
            var n = NextBit(a, fromIndex, false);
            if (n == -1)
                n = a.Count;
            return n;
        }

        public static int NextSetBit(BitArray a, int fromIndex)
        {
            return NextBit(a, fromIndex, true);
        }

        private static int NextBit(BitArray a, int fromIndex, bool theBit)
        {
            if (fromIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(fromIndex));
            var n = a.Length;
            for (int i = fromIndex; i < n; i++)
            {
                if (a[i] == theBit)
                    return i;
            }
            return -1;
        }

        public static void And(BitArray a, BitArray set)
        {
            BitArray b;
            if (a.Count == set.Count)
                b = set;
            else
            {
                b = (BitArray)set.Clone();
                b.Length = a.Length;
            }
            a.And(b);
        }

        public static void AndNot(BitArray a, BitArray set)
        {
            var b = (BitArray)set.Clone();
            b.Length = a.Length;
            a.And(b.Not());
        }

        public static int GetLength(BitArray a)
        {
            for (var i = a.Length - 1; i >= 0; i--)
            {
                if (a[i])
                    return i + 1;
            }
            return 0;
        }

        public static string ToString(this BitArray a)
        {
            var sb = new StringBuilder();
            var isFirst = true;
            sb.Append("{");
            for (var i = 0; i < a.Length; i++)
            {
                if (a[i])
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }
                    sb.Append(i);
                }
            }
            sb.Append("}");
            return sb.ToString();
        }

        public static bool Equals(this BitArray a, BitArray b)
        {
            var min = Math.Min(a.Length, b.Length);
            int i;
            for (i = 0; i < min; i++)
                if (a[i] != b[i])
                    return false;
            if (a.Length > b.Length)
                b = a;
            for (i = min; i < b.Length; i++)
                if (b[i])
                    return false;
            return true;
        }

        public static int GetHashCode(this BitArray a)
        {
            uint c = 0;
            for (int i = 0; i < Math.Min(a.Length, 32); i++)
            {
                var b = a[i];
                c <<= 1;
                if (b)
                    c += 1;
            }
            return (int)c;
        }
    }
}
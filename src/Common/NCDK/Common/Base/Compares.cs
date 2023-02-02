using System;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Common.Base
{
    internal static class Compares
    {
        public static bool DeepEquals(IEnumerable expected, IEnumerable sets)
        {
            return AreDeepEqual(expected, sets);
        }

        public static bool AreEqual<T>(ICollection<T> a, ICollection<T> b)
        {
            if (a.Count != b.Count)
                return false;
            return AreEqual((IEnumerable<T>)a, (IEnumerable<T>)b);
        }

        public static bool AreEqual<T>(IEnumerable<T> a, IEnumerable<T> b)
        {
            var ae = a.GetEnumerator();
            var be = b.GetEnumerator();
            while (ae.MoveNext())
            {
                if (!be.MoveNext())
                    return false;
                if (!Equals(ae.Current, be.Current))
                    return false;
            }
            if (be.MoveNext())
                return false;
            return true;
        }
        
        public static bool AreOrderLessDeepEqual<T>(IEnumerable<T> a, IEnumerable<T> b)
        {
            var la = new List<T>(a);
            var lb = new List<T>(b);
            foreach (var aa in la)
            {
                var f = lb.Find(o => AreDeepEqual(o, aa));
                if (f == null)
                    return false;
                lb.Remove(f);
            }
            return true;
        }

        public static bool AreDeepEqual(object expected, object actual)
        {
            if (expected == null && actual == null)
                return true;
            if (expected == null || actual == null)
                return false;
            if (expected is IEnumerable)
            {
                return AreDeepEqual((IEnumerable)expected, (IEnumerable)actual);
            }
            else if (expected is BitArray)
            {
                return AreEqual((BitArray)expected, (BitArray)actual);
            }
            else
            {
                return expected.Equals(actual);
            }
        }

        public static bool AreEqual(BitArray expected, BitArray actual)
        {
            var length = Math.Max(expected.Length, actual.Length);
            var e = (BitArray)expected.Clone();
            var aa = (BitArray)actual.Clone();
            e.Length = length;
            aa.Length = length;
            for (int i = 0; i < e.Length; i++)
                if (e[i] != aa[i])
                    return false;
            return true;
        }

        public static bool AreDeepEqual(IEnumerable expected, IEnumerable actual)
        {
            var ae = expected.GetEnumerator();
            var be = actual.GetEnumerator();
            while (ae.MoveNext())
            {
                if (!be.MoveNext())
                    return false;
                if (!AreDeepEqual(ae.Current, be.Current))
                    return false;
            }
            if (be.MoveNext())
                return false;
            return true;
        }

        public static bool DeepContains(IEnumerable expected, object actual)
        {
            foreach (var e in expected)
            {
                if (AreDeepEqual(e, actual))
                    return true;
            }
            return false;
        }

        public static bool DeepContains<T,U>(IEnumerable<KeyValuePair<T, U>> elements, KeyValuePair<T, U> element)
        {
            foreach (var e in elements)
            {
                var aa = AreDeepEqual(e.Key, element.Key);
                var b = AreDeepEqual(e.Value, element.Value);
                if (aa && b)
                    return true;
            }
            return false;
        }
    }
}

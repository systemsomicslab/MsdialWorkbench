using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace NCDK.Common.Collections
{
    internal static class Arrays
    {
        public static object Comparers { get; private set; }

        public static T[][] CreateJagged<T>(int n1, int n2)
        {
            T[][] array = new T[n1][];
            for (int i = 0; i < n1; i++)
            {
                array[i] = new T[n2];
            }
            return array;
        }

        public static T[][][] CreateJagged<T>(int n1, int n2, int n3)
        {
            T[][][] array = new T[n1][][];
            for (int i = 0; i < n1; i++)
            {
                array[i] = CreateJagged<T>(n2, n3);
            }
            return array;
        }

        public static void Fill<T>(IList<T> array, T value) 
        {
            Fill(array, 0, array.Count, value);
        }

        public static void Fill<T>(IList<T> array, int fromIndex, int toIndex, T value)
        {
            for (var i = fromIndex; i < toIndex; i++)
                array[i] = value;
        }

        public static T[] Clone<T>(T[] array)
        {
            var cloned = new T[array.Length];
            Array.Copy(array, cloned, array.Length);
            return cloned;
        }

        public static string ToJavaString(Array array)
        {
            var sb = new StringBuilder();
            sb.Append("{ ");
            foreach (var e in array)
            {
                sb.Append(e.ToString()).Append(", ");
            }
            sb.Append("}");
            return sb.ToString();
        }

        public static T[] CopyOf<T>(T[] array, int newLength)
        {
            var n = Math.Min(array.Length, newLength);
            var newArray = new T[newLength];
            Array.Copy(array, newArray, n);
            return newArray;
        }

        public static bool AreEqual<T>(T[] a1, T[] a2)
        {
            int n = a1.Length;
            if (n != a2.Length)
                return false;
            for (int i = 0; i < n; i++)
                if (!object.Equals(a1[i], a2[i]))
                    return false;
            return true;
        }

        public static int GetHashCode<T>(T[] a)
        {
            int ret = 0;
            foreach (var e in a)
            {
                ret *= 31;
                ret += e.GetHashCode();
            }
            return ret;
        }

        public static string DeepToString(IEnumerable array)
        {
            var sb = new StringBuilder();
            sb.Append("[");
            bool isFirst = true;
            foreach (var e in array)
            {
                if (isFirst)
                    isFirst = false;
                else
                    sb.Append(", ");
                if (e is IEnumerable)
                    sb.Append(DeepToString((IEnumerable)e));
                else
                    sb.Append(e.ToString());
            }
            sb.Append("]");
            return sb.ToString();
        }
    }
}

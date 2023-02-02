using System;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Common.Collections
{
    internal static class Lists
    {
        public static int GetDeepHashCode(IEnumerable list)
        {
            int ret = 1;
            foreach (var e in list)
            {
                ret *= 31;
                if (e != null)
                {
                    ret += e is IEnumerable ? GetDeepHashCode((IEnumerable)e) : e.GetHashCode();
                }
            }
            return ret;
        }

        public static int GetHashCode<T>(IEnumerable<T> list)
        {
            int ret = 1;
            foreach (var e in list)
            {
                ret *= 31;
                ret += e == null ? 0 : e.GetHashCode();
            }
            return ret;
        }

        public static void StableSort<T>(IList<T> list, Comparison<T> comparison)
        {
            var wrapped = new List<KeyValuePair<int, T>>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                wrapped.Add(new KeyValuePair<int, T>(i, list[i]));
            }

            wrapped.Sort((x, y) =>
            {
                var result = comparison(x.Value, y.Value);
                if (result == 0)
                {
                    result = x.Key.CompareTo(y.Key);
                }
                return result;
            });

            for (int i = 0; i < list.Count; i++)
            {
                list[i] = wrapped[i].Value;
            }
        }
    }
}

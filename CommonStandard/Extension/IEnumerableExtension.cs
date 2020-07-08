using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Extension {
    public static class IEnumerableExtension {
        public static IEnumerable<(T, int)> WithIndex<T>(this IEnumerable<T> ts) {
            return ts.Select((t, i) => (t, i));
        }
        public static IEnumerable<T> OrEmptyIfNull<T>(this IEnumerable<T> collection) {
            return collection ?? Enumerable.Empty<T>();
        }

        public static bool IsEmptyOrNull<T>(this IEnumerable<T> collection) {
            return collection == null || !collection.Any();
        }

        public static IEnumerable<System.Tuple<T1, T2>> Zip<T1, T2>(this IEnumerable<T1> xs, IEnumerable<T2> ys) {
            return xs.Zip(ys, Tuple.Create);
        }

        public static IEnumerable<T4> Zip<T1, T2, T3, T4>(this IEnumerable<T1> xs, IEnumerable<T2> ys, IEnumerable<T3> zs, Func<T1, T2, T3, T4> func) {
            var xys = xs.Zip(ys, Tuple.Create);
            return xys.Zip(zs, (xy, z) => func(xy.Item1, xy.Item2, z));
        }

        public static IEnumerable<System.Tuple<T1, T2, T3>> Zip<T1, T2, T3>(this IEnumerable<T1> xs, IEnumerable<T2> ys, IEnumerable<T3> zs) {
            return xs.Zip(ys, zs, Tuple.Create);
        }
    }
}

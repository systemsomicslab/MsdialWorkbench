using System;
using System.Collections.Generic;
using System.Linq;

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

        public static IEnumerable<T> Return<T>(T value) {
            yield return value;
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

        public static int Argmax<T>(this IEnumerable<T> xs) where T: IComparable {
            return Argmax(xs, Comparer<T>.Default.Compare);
        }

        public static int Argmax<T>(this IEnumerable<T> xs, Comparison<T> comp) {
            return xs.Select((x, idx) => (x, idx)).Aggregate((acc, x) => comp(acc.x, x.x) < 0 ? x : acc).idx;
        }

        public static T Argmax<T, U>(this IEnumerable<T> xs, Func<T, U> func) where U: IComparable {
            return Argmax(xs, func, Comparer<U>.Default);
        }

        public static T Argmax<T, U>(this IEnumerable<T> xs, Func<T, U> func, IComparer<U> comparer) {
            return Argmax(xs, func, comparer.Compare);
        }

        public static T Argmax<T, U>(this IEnumerable<T> xs, Func<T, U> func, Comparison<U> comp) {
            return xs.Select(x => (x, y: func(x))).Aggregate((acc, p) => comp(acc.y, p.y) < 0 ? p : acc).x;
        }

        public static int Argmin<T>(this IEnumerable<T> xs) where T: IComparable {
            return Argmin(xs, Comparer<T>.Default.Compare);
        }

        public static int Argmin<T>(this IEnumerable<T> xs, Comparison<T> comp) {
            return xs.Select((x, idx) => (x, idx)).Aggregate((acc, x) => comp(acc.x, x.x) > 0 ? x : acc).idx;
        }

        public static T Argmin<T, U>(this IEnumerable<T> xs, Func<T, U> func) where U: IComparable {
            return Argmin(xs, func, Comparer<U>.Default);
        }

        public static T Argmin<T, U>(this IEnumerable<T> xs, Func<T, U> func, IComparer<U> comparer) {
            return Argmin(xs, func, comparer.Compare);
        }

        public static T Argmin<T, U>(this IEnumerable<T> xs, Func<T, U> func, Comparison<U> comp) {
            return xs.Select(x => (x, y: func(x))).Aggregate((acc, p) => comp(acc.y, p.y) > 0 ? p : acc).x;
        }

        public static IEnumerable<List<T>> Chunk<T>(this IEnumerable<T> xs, int size)
        {
            var results = new List<T>(size);
            foreach (var x in xs) {
                results.Add(x);
                if (results.Count == size) {
                    yield return results;
                    results = new List<T>(size);
                }
            }
            if (results.Count >= 1) {
                yield return results;
            }
        }

        public static IEnumerable<List<T>> Sequence<T>(this IEnumerable<IEnumerable<T>> xss) {
            var enumerators = xss.Select(xs => xs.GetEnumerator()).ToList();
            var remain = true;
            while (remain) {
                var result = new List<T>();
                foreach(var enumerator in enumerators) {
                    if (enumerator.MoveNext()) {
                        result.Add(enumerator.Current);
                    }
                    else {
                        remain = false;
                        break;
                    }
                }
                if (remain) yield return result;
            }
        }

        public static IEnumerable<List<T>> Sequence<T>(this IReadOnlyCollection<IEnumerable<T>> xss) {
            var n = xss.Count;
            var enumerators = xss.Select(xs => xs.GetEnumerator()).ToList();
            var remain = true;
            while (remain) {
                var result = new List<T>(n);
                foreach(var enumerator in enumerators) {
                    if (enumerator.MoveNext()) {
                        result.Add(enumerator.Current);
                    }
                    else {
                        remain = false;
                        break;
                    }
                }
                if (remain) yield return result;
            }
        }
    }
}

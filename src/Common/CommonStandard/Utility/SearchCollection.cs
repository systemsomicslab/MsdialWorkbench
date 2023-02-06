using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Utility
{
    public static class SearchCollection
    {
        public static int LowerBound<T, U>(this IReadOnlyList<T> collection, U value, int start, int end, Func<T, U, int> compare)
        {
            int lo = start;
            int hi = end;
            int mid;
            while (lo < hi)
            {
                mid = (lo + hi) >> 1;
                if (compare(collection[mid], value) < 0)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            return lo;
        }

        public static int LowerBound<T, U>(this IReadOnlyList<T> collection, U value, Func<T, U, int> compare)
        {
            return LowerBound(collection, value, 0, collection.Count, compare);
        }

        public static int LowerBound<T>(this IReadOnlyList<T> collection, T value, int start, int end, IComparer<T> comparer)
        {
            return LowerBound(collection, value, start, end, comparer.Compare);
        }

        public static int LowerBound<T>(this IReadOnlyList<T> collection, T value) where T : IComparable
        {
            return LowerBound(collection, value, 0, collection.Count, Comparer<T>.Default.Compare);
        }

        public static int LowerBound<T>(this IReadOnlyList<T> collection, T value, int start, int end) where T : IComparable
        {
            return LowerBound(collection, value, start, end, Comparer<T>.Default.Compare);
        }

        public static int LowerBound<T>(this IReadOnlyList<T> collection, T value, IComparer<T> comparer)
        {
            return LowerBound(collection, value, 0, collection.Count, comparer.Compare);
        }

        public static int UpperBound<T, U>(this IReadOnlyList<T> collection, U value, int start, int end, Func<T, U, int> compare)
        {
            int lo = start;
            int hi = end;
            int mid;

            while(lo < hi)
            {
                mid = (lo + hi) >> 1;
                if (compare(collection[mid], value) <= 0)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            return lo;
        }

        public static int UpperBound<T, U>(this IReadOnlyList<T> collection, U value, Func<T, U, int> compare)
        {
            return UpperBound(collection, value, 0, collection.Count, compare);
        }

        public static int UpperBound<T>(this IReadOnlyList<T> collection, T value, int start, int end, IComparer<T> comparer)
        {
            return UpperBound(collection, value, start, end, comparer.Compare);
        }

        public static int UpperBound<T>(this IReadOnlyList<T> collection, T value) where T : IComparable
        {
            return UpperBound(collection, value, 0, collection.Count, Comparer<T>.Default.Compare);
        }

        public static int UpperBound<T>(this IReadOnlyList<T> collection, T value, int start, int end) where T : IComparable
        {
            return UpperBound(collection, value, start, end, Comparer<T>.Default.Compare);
        }

        public static int UpperBound<T>(this IReadOnlyList<T> collection, T value, IComparer<T> comparer)
        {
            return UpperBound(collection, value, 0, collection.Count, comparer.Compare);
        }

        public static int BinarySearch<T, U>(this IReadOnlyList<T> collection, U value, int start, int end, Func<T, U, int> compare)
        {
            var idx = LowerBound(collection, value, start, end, compare);
            if (idx >= end || compare(collection[idx], value) != 0) {
                return -1;
            }
            return idx;
        }

        public static int BinarySearch<T, U>(this IReadOnlyList<T> collection, U value, Func<T, U, int> compare)
        {
            return BinarySearch(collection, value, 0, collection.Count, compare);
        }

        public static int BinarySearch<T>(this IReadOnlyList<T> collection, T value, int start, int end, IComparer<T> comparer)
        {
            return BinarySearch(collection, value, start, end, comparer.Compare);
        }

        public static int BinarySearch<T>(this IReadOnlyList<T> collection, T value) where T : IComparable
        {
            return BinarySearch(collection, value, 0, collection.Count, Comparer<T>.Default.Compare);
        }

        public static int BinarySearch<T>(this IReadOnlyList<T> collection, T value, int start, int end) where T : IComparable
        {
            return BinarySearch(collection, value, start, end, Comparer<T>.Default.Compare);
        }

        public static int BinarySearch<T>(this IReadOnlyList<T> collection, T value, IComparer<T> comparer)
        {
            return BinarySearch(collection, value, 0, collection.Count, comparer.Compare);
        }

        public static IEnumerable<T[]> Permutations<T>(IReadOnlyList<T> collection) {
            var n = collection.Count;
            var used = new bool[n];
            var result = new T[n];
            var memo = new Dictionary<T, int>();
            
            IEnumerable<T[]> recurse(int m) {
                if (m == n) {
                    yield return result.ToArray();
                }
                else {
                    if (!memo.TryGetValue(collection[m], out var j)) {
                        j = 0;
                    }
                    for (int i = j; i < n; i++) {
                        if (used[i]) {
                            continue;
                        }
                        used[i] = true;
                        result[i] = collection[m];
                        memo[collection[m]] = i + 1;
                        foreach (var res in recurse(m + 1)) {
                            yield return res;
                        }
                        used[i] = false;
                        memo[collection[m]] = j;
                    }
                }
            }

            return recurse(0);
        }

        public static IEnumerable<T[]> Combination<T>(IEnumerable<T> collection, int size = 2) {
            if (size < 1) {
                throw new ArgumentException($"{nameof(size)} should be more than 1.");
            }
            var xs = new List<T>();
            foreach (var item in collection) {
                foreach (var items in CombinationCore(xs, size - 1)) {
                    yield return items.Append(item).ToArray();
                }
                xs.Add(item);
            }
        }

        private static IEnumerable<IEnumerable<T>> CombinationCore<T>(List<T> collection, int size) {
            if (size == 0) {
                yield break;
            }
            if (size == 1) {
                foreach (var item in collection) {
                    yield return new[] { item };
                }
                yield break;
            }
            var xs = new List<T>();
            foreach (var item in collection) {
                foreach (var ys in CombinationCore(xs, size - 1)) {
                    yield return ys.Append(item);
                }
                xs.Add(item);
            }
        }

        public static IEnumerable<T[]> CartesianProduct<T>(IReadOnlyList<IReadOnlyList<T>> collections) {
            var set = new T[collections.Count];

            IEnumerable<T[]> rec(int i) {
                if (i == collections.Count) {
                    yield return set.ToArray();
                }
                else {
                    var collection = collections[i];

                    foreach (var item in collection) {
                        set[i] = item;
                        foreach (var res in rec(i + 1)) {
                            yield return res;
                        }
                    }
                }
            }

            return rec(0);
        }
    }
}

using System;
using System.Collections.Generic;

namespace CompMs.Common.Extension
{
    public static class ReadOnlyListExtension
    {
        public static int IndexOf<T>(this IReadOnlyList<T> xs, T item) {
            return IndexOfCore(xs, item, 0, xs.Count);
        }

        public static int IndexOf<T>(this IReadOnlyList<T> xs, T item, int index) {
            return IndexOfCore(xs, item, index, xs.Count);
        }

        public static int IndexOf<T>(this IReadOnlyList<T> xs, T item, int index, int count) {
            return IndexOfCore(xs, item, index, Math.Min(xs.Count, index + count));
        }

        private static int IndexOfCore<T>(IReadOnlyList<T> xs, T item, int start, int end) {
            for (int i = start; i < end; ++i) {
                if (Equals(xs[i], item)) {
                    return i;
                }
            }
            return -1;
        }
    }
}

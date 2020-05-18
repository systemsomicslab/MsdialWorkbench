using System;
using System.Collections.Generic;

namespace CompMs.Common.Utility
{
    public class SearchCollection
    {
        public static int LowerBound<T>(IReadOnlyList<T> collection, T value, int start, int end, Comparison<T> compare)
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

        public static int LowerBound<T>(IReadOnlyList<T> collection, T value, Comparison<T> compare)
        {
            return LowerBound(collection, value, 0, collection.Count, compare);
        }

        public static int LowerBound<T>(IReadOnlyList<T> collection, T value, int start, int end, IComparer<T> comparer)
        {
            return LowerBound(collection, value, start, end, comparer.Compare);
        }

        public static int LowerBound<T>(IReadOnlyList<T> collection, T value) where T : IComparable
        {
            return LowerBound(collection, value, 0, collection.Count, Comparer<T>.Default.Compare);
        }

        public static int LowerBound<T>(IReadOnlyList<T> collection, T value, int start, int end) where T : IComparable
        {
            return LowerBound(collection, value, start, end, Comparer<T>.Default.Compare);
        }

        public static int LowerBound<T>(IReadOnlyList<T> collection, T value, IComparer<T> comparer)
        {
            return LowerBound(collection, value, 0, collection.Count, comparer.Compare);
        }

        public static int UpperBound<T>(IReadOnlyList<T> collection, T value, int start, int end, Comparison<T> compare)
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

        public static int UpperBound<T>(IReadOnlyList<T> collection, T value, Comparison<T> compare)
        {
            return UpperBound(collection, value, 0, collection.Count, compare);
        }

        public static int UpperBound<T>(IReadOnlyList<T> collection, T value, int start, int end, IComparer<T> comparer)
        {
            return UpperBound(collection, value, start, end, comparer.Compare);
        }

        public static int UpperBound<T>(IReadOnlyList<T> collection, T value) where T : IComparable
        {
            return UpperBound(collection, value, 0, collection.Count, Comparer<T>.Default.Compare);
        }

        public static int UpperBound<T>(IReadOnlyList<T> collection, T value, int start, int end) where T : IComparable
        {
            return UpperBound(collection, value, start, end, Comparer<T>.Default.Compare);
        }

        public static int UpperBound<T>(IReadOnlyList<T> collection, T value, IComparer<T> comparer)
        {
            return UpperBound(collection, value, 0, collection.Count, comparer.Compare);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;

namespace CompMs.Common.Utility
{
    public class CompositeComparer {
        public static IComparer<T> Build<T>(params IComparer<T>[] comparers) {
            return new CompositeComparerImpl<T>(comparers);
        }

        class CompositeComparerImpl<T> : IComparer<T>
        {
            private IComparer<T>[] Comparers { get; }

            public CompositeComparerImpl(params IComparer<T>[] comparers){
                Comparers = comparers;
            }

            public int Compare(T x, T y) {
                foreach (var comparer in Comparers) {
                    var result = comparer.Compare(x, y);
                    if (result != 0) return result;
                }
                return 0;
            }
        }

    }
}

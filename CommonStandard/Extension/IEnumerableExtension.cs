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

        public static bool IsNotEmptyOrNull<T>(this IEnumerable<T> collection) {
            return collection != null && collection.Count() > 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv {
    public static class IEnumerableExtension {
        public static IEnumerable<T> OrEmptyIfNull<T>(this IEnumerable<T> collection) {
            return collection ?? Enumerable.Empty<T>();
        }
    }
}

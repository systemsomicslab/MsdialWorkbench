using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public sealed class MassReferenceSearcher<T> : IReferenceSearcher<T, IMSSearchQuery> where T: IMSProperty
    {
        static MassReferenceSearcher() {
            comparer = MassComparer.Comparer;
        }

        public MassReferenceSearcher(IEnumerable<T> db) {
            this.db = db.OrderBy(item => item.PrecursorMz).ToList();
        }

        private readonly List<T> db;
        private static readonly IComparer<IMSProperty> comparer;

        public IReadOnlyList<T> Search(IMSSearchQuery query) {
            var lo = SearchCollection.LowerBound(db as IReadOnlyList<IMSProperty>, query.LowerLimit(), comparer);
            var hi = SearchCollection.UpperBound(db as IReadOnlyList<IMSProperty>, query.UpperLimit(), lo, db.Count, comparer);
            return db.GetRange(lo, hi - lo);
        }
    }
}

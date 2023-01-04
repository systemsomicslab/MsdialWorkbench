using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public sealed class MassRtReferenceSearcher<T> : IReferenceSearcher<T, IMSSearchQuery> where T: IMSProperty
    {
        private List<T> _db;

        public MassRtReferenceSearcher(IEnumerable<T> db) {
            _db = db.OrderBy(x => x.PrecursorMz).ToList();
        }

        public IReadOnlyList<T> Search(IMSSearchQuery query) {
            var lower = query.LowerLimit();
            var upper = query.UpperLimit();
            var lo = ((IReadOnlyList<IMSProperty>)_db).LowerBound(lower, MassComparer.Comparer);
            var hi = ((IReadOnlyList<IMSProperty>)_db).UpperBound(upper, MassComparer.Comparer);
            return _db.GetRange(lo, hi - lo).Where(x => lower.ChromXs.RT.Value <= x.ChromXs.RT.Value && x.ChromXs.RT.Value <= upper.ChromXs.RT.Value).ToArray();
        }
    }
}

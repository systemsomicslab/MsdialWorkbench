using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public sealed class MassRtCcsReferenceSearcher<T> : IReferenceSearcher<T, IMSIonSearchQuery> where T: IMSIonProperty
    {
        private readonly List<T> _db;

        public MassRtCcsReferenceSearcher(IEnumerable<T> db) {
            _db = db.OrderBy(x => x.PrecursorMz).ToList();
        }

        public IReadOnlyList<T> Search(IMSIonSearchQuery query) {
            var lower = query.LowerLimit();
            var upper = query.UpperLimit();
            var lo = ((IReadOnlyList<IMSIonProperty>)_db).LowerBound(lower, MassComparer.Comparer);
            var hi = ((IReadOnlyList<IMSIonProperty>)_db).UpperBound(upper, MassComparer.Comparer);
            return _db.GetRange(lo, hi - lo)
                .Where(x => lower.ChromXs.RT.Value <= x.ChromXs.RT.Value && x.ChromXs.RT.Value <= upper.ChromXs.RT.Value)
                .Where(x => lower.CollisionCrossSection <= x.CollisionCrossSection && x.CollisionCrossSection <= upper.CollisionCrossSection)
                .ToArray();
        }
    }
}

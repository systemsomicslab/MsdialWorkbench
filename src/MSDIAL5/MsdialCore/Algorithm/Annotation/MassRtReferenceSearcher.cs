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
            // A reference that carries no retention time is exempt from the window rather
            // than excluded by it. Before this, the behaviour flipped on the tolerance: a
            // narrow window silently dropped every such entry, while the 100-minute default
            // admitted them and let the unguarded Gaussian score them. Neither is a filter
            // decision that the data supports, and the mass range has already bounded this
            // set, so exempting them costs nothing.
            return _db.GetRange(lo, hi - lo)
                .Where(x => x.ChromXs.RT.Value <= 0d
                    || (lower.ChromXs.RT.Value <= x.ChromXs.RT.Value && x.ChromXs.RT.Value <= upper.ChromXs.RT.Value))
                .ToArray();
        }
    }
}

using CompMs.Common.DataStructure;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public sealed class MassRtCcsReferenceSearcher<T> : IReferenceSearcher<T, IMSIonSearchQuery> where T: IMSIonProperty
    {
        public MassRtCcsReferenceSearcher(IEnumerable<T> db) {
            tree = KdTree.Build(db, x => x.PrecursorMz, x => x.ChromXs.RT.Value,  x => x.CollisionCrossSection);
        }

        private readonly KdTree<T> tree;

        public IReadOnlyList<T> Search(IMSIonSearchQuery query) {
            var lower = query.LowerLimit();
            var upper = query.UpperLimit();
            return tree.RangeSearch(
                new[] { lower.PrecursorMz, lower.ChromXs.RT.Value, lower.CollisionCrossSection, },
                new[] { upper.PrecursorMz, upper.ChromXs.RT.Value, upper.CollisionCrossSection, });
        }
    }
}

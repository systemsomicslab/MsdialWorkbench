using CompMs.Common.DataStructure;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public sealed class MassRtReferenceSearcher<T> : IReferenceSearcher<T, IMSSearchQuery> where T: IMSProperty
    {
        public MassRtReferenceSearcher(IEnumerable<T> db) {
            tree = KdTree.Build(db, x => x.PrecursorMz, x => x.ChromXs.RT.Value);   
        }

        private readonly KdTree<T> tree;

        public IReadOnlyList<T> Search(IMSSearchQuery query) {
            var lower = query.LowerLimit();
            var upper = query.UpperLimit();
            return tree.RangeSearch(
                new[] { lower.PrecursorMz, lower.ChromXs.RT.Value, },
                new[] { upper.PrecursorMz, upper.ChromXs.RT.Value, });
        }
    }
}

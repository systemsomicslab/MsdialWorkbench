using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public class MassReferenceSearcher<T> : IReferenceSearcher<T, IMassSearchQuery> where T: IMSProperty
    {
        static MassReferenceSearcher() {
            comparer = MassComparer.Comparer;
        }

        public MassReferenceSearcher(IEnumerable<T> db) {
            this.db = db.OrderBy(item => item.PrecursorMz).ToList();
        }

        private readonly List<T> db;
        private static readonly IComparer<IMSProperty> comparer;

        public IReadOnlyList<T> Search(IMassSearchQuery query) {
            var lo = SearchCollection.LowerBound(db as IReadOnlyList<IMSProperty>, query.LowerLimit(), comparer);
            var hi = SearchCollection.UpperBound(db as IReadOnlyList<IMSProperty>, query.UpperLimit(), lo, db.Count, comparer);
            return db.GetRange(lo, hi - lo);
        }
    }

    public interface IMassSearchQuery
    {
        IMSProperty LowerLimit();
        IMSProperty UpperLimit();
    }

    public class MassSearchQuery : IMassSearchQuery
    {
        private readonly double mass;
        private readonly double tolerance;

        public MassSearchQuery(double mass, double tolerance) {
            this.mass = mass;
            this.tolerance = tolerance;
        }

        IMSProperty IMassSearchQuery.LowerLimit() {
            return new MSProperty(mass - tolerance, new ChromXs(double.NegativeInfinity), Common.Enum.IonMode.Positive);
        }

        IMSProperty IMassSearchQuery.UpperLimit() {
            return new MSProperty(mass + tolerance, new ChromXs(double.PositiveInfinity), Common.Enum.IonMode.Positive);
        }
    }
}

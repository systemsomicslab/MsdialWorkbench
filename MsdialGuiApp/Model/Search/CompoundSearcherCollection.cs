using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Search
{
    internal sealed class CompoundSearcherCollection
    {
        private readonly IReadOnlyList<CompoundSearcher> _items;

        public CompoundSearcherCollection(IEnumerable<CompoundSearcher> items) {
            if (items is null) {
                throw new ArgumentNullException(nameof(items));
            }

            _items = items as IReadOnlyList<CompoundSearcher> ?? items.ToList();
        }

        public IReadOnlyList<CompoundSearcher> Items => _items;

        public static CompoundSearcherCollection BuildSearchers(DataBaseStorage databases, DataBaseMapper mapper) {
            var metabolomicsSearchers = databases
                .MetabolomicsDataBases
                .SelectMany(db => db.Pairs)
                .Select(pair => new CompoundSearcher(pair.AnnotationQueryFactory, mapper));
            var lipidomicsSearchers = databases
                .EadLipidomicsDatabases
                .SelectMany(db => db.Pairs)
                .Select(pair => new CompoundSearcher(pair.AnnotationQueryFactory, mapper));
            return new CompoundSearcherCollection(metabolomicsSearchers.Concat(lipidomicsSearchers));
        }
    }
}

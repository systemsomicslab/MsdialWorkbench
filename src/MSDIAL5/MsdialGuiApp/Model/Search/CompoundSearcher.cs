using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CompMs.App.Msdial.Model.Search
{
    public sealed class CompoundSearcher
    {
        private readonly IAnnotationQueryFactory<MsScanMatchResult> _queryFactory;
        private readonly IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> _refer;
        private readonly LcmsMspAnnotator? _lcAnnotator;

        public CompoundSearcher(IAnnotationQueryFactory<MsScanMatchResult> queryFactory, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer) {
            _queryFactory = queryFactory ?? throw new ArgumentNullException(nameof(queryFactory));
            _refer = refer ?? throw new ArgumentNullException(nameof(refer));
            MsRefSearchParameter = queryFactory.PrepareParameter();
            _lcAnnotator = queryFactory is AnnotationQueryFactory factory && factory.Annotator is LcmsMspAnnotator annotator
                && annotator.GetType() == typeof(LcmsMspAnnotator) ? annotator : null;
        }

        public string Id => _queryFactory.AnnotatorId;

        public IAnnotationQueryFactory<MsScanMatchResult> QueryFactory => _queryFactory;

        public MsRefSearchParameterBase MsRefSearchParameter { get; }

        public IEnumerable<ICompoundResult> Search(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature) => Search(property, scan, spectrum, ionFeature, default);

        public IEnumerable<ICompoundResult> Search(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            var query = _queryFactory.Create(
                property,
                scan,
                spectrum,
                ionFeature,
                // The legacy copy constructor omits AndromedaScoreCutOff; preserve every setting in this snapshot.
                _lcAnnotator is null ? MsRefSearchParameter : new MsRefSearchParameterBase(MsRefSearchParameter) {
                    AndromedaScoreCutOff = MsRefSearchParameter.AndromedaScoreCutOff,
                }
            );
            if (query is ICancellableAnnotationQuery cancellable) { cancellable.CancellationToken = token; }
            // The main window orders once; ordinary annotator callers still receive their sorted results.
            var candidates = _lcAnnotator is not null
                ? _lcAnnotator.FindCandidatesWithoutOrdering(query).ToList() : query.FindCandidates(forceFind: true).ToList();
            token.ThrowIfCancellationRequested();
            foreach (var candidate in candidates) {
                token.ThrowIfCancellationRequested();
                candidate.Source |= SourceType.Manual;
            }
            return candidates
                .OrderByDescending(result => result.TotalScore)
                .Select(result => new CompoundResult(_refer.Refer(result)!, result));
        }

        public override string ToString() {
            return Id;
        }
    }
}

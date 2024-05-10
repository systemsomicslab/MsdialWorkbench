using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Search
{
    public sealed class CompoundSearcher
    {
        private readonly IAnnotationQueryFactory<MsScanMatchResult> _queryFactory;
        private readonly IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> _refer;

        public CompoundSearcher(IAnnotationQueryFactory<MsScanMatchResult> queryFactory, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer) {
            _queryFactory = queryFactory ?? throw new ArgumentNullException(nameof(queryFactory));
            _refer = refer ?? throw new ArgumentNullException(nameof(refer));
            MsRefSearchParameter = queryFactory.PrepareParameter();
        }

        public string Id => _queryFactory.AnnotatorId;

        public IAnnotationQueryFactory<MsScanMatchResult> QueryFactory => _queryFactory;

        public MsRefSearchParameterBase MsRefSearchParameter { get; }

        public IEnumerable<ICompoundResult> Search(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature) {
            var candidates = _queryFactory.Create(
                property,
                scan,
                spectrum,
                ionFeature,
                MsRefSearchParameter
            ).FindCandidates(forceFind: true).ToList();
            foreach (var candidate in candidates) {
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

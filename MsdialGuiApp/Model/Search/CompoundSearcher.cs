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
    public class CompoundSearcher
    {
        private readonly IAnnotationQueryFactory<ICallableAnnotationQuery<MsScanMatchResult>> queryFactory;
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer;

        public CompoundSearcher(
            IAnnotationQueryFactory<ICallableAnnotationQuery<MsScanMatchResult>> queryFactory,
            MsRefSearchParameterBase msRefSearchParameter,
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            this.queryFactory = queryFactory ?? throw new ArgumentNullException(nameof(queryFactory));
            MsRefSearchParameter = msRefSearchParameter is null
                ? new MsRefSearchParameterBase()
                : new MsRefSearchParameterBase(msRefSearchParameter);
            this.refer = refer ?? throw new ArgumentNullException(nameof(refer));

            Id = queryFactory.AnnotatorId;
        }

        public string Id { get; }

        public MsRefSearchParameterBase MsRefSearchParameter { get; }

        public IEnumerable<ICompoundResult> Search(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature) {
            var candidates = queryFactory.Create(
                property,
                scan,
                spectrum,
                ionFeature,
                MsRefSearchParameter
            ).FindCandidates().ToList();
            foreach (var candidate in candidates) {
                candidate.Source |= SourceType.Manual;
            }
            return candidates
                .OrderByDescending(result => result.TotalScore)
                .Select(result => new CompoundResult(refer.Refer(result), result));
        }
    }
}

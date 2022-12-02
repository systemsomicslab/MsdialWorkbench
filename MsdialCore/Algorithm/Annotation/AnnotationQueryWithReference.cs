using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public class AnnotationQueryWithReference : IAnnotationQuery<MsScanMatchResult>
    {
        private readonly MoleculeMsReference reference;
        private readonly IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MsScanMatchResult> annotator;

        public AnnotationQueryWithReference(
            IMSIonProperty property,
            IMSScanProperty scan,
            MoleculeMsReference reference,
            IReadOnlyList<IsotopicPeak> isotopes,
            IonFeatureCharacter ionFeature,
            MsRefSearchParameterBase parameter,
            IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MsScanMatchResult> annotator) {

            Property = property ?? throw new ArgumentNullException(nameof(property));
            Scan = scan ?? throw new ArgumentNullException(nameof(scan));
            this.reference = reference;
            Isotopes = isotopes;
            Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            this.annotator = annotator ?? throw new ArgumentNullException(nameof(annotator));
            IonFeature = ionFeature;
            BaseQuery = new AnnotationQuery(Property, Scan, Isotopes, IonFeature, Parameter, null);
        }

        public IMSIonProperty Property { get; }

        public IMSScanProperty Scan { get; }

        public IMSScanProperty NormalizedScan => BaseQuery.NormalizedScan;

        public IReadOnlyList<IsotopicPeak> Isotopes { get; }

        public IonFeatureCharacter IonFeature { get; }

        public MsRefSearchParameterBase Parameter { get; }

        private readonly AnnotationQuery BaseQuery;

        public IEnumerable<MsScanMatchResult> FindCandidates() {
            if (annotator is null || reference is null) {
                return Enumerable.Empty<MsScanMatchResult>();
            }
            else {
                return annotator.FindCandidates((BaseQuery, reference));
            }
        }
    }
}

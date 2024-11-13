using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public class AnnotationQueryWithReference : IAnnotationQuery<MsScanMatchResult>
    {
        private readonly MoleculeMsReference? _reference;
        private readonly IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MsScanMatchResult> _finder;
        private readonly AnnotationQuery _baseQuery;
        private readonly bool _ignoreIsotopicPeak;

        public AnnotationQueryWithReference(
            IMSIonProperty property,
            IMSScanProperty scan,
            MoleculeMsReference? reference,
            IReadOnlyList<IsotopicPeak> isotopes,
            IonFeatureCharacter ionFeature,
            MsRefSearchParameterBase parameter,
            IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MsScanMatchResult> annotator,
            bool ignoreIsotopicPeak = true) {

            Property = property ?? throw new ArgumentNullException(nameof(property));
            Scan = scan ?? throw new ArgumentNullException(nameof(scan));
            _reference = reference;
            Isotopes = isotopes;
            Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _finder = annotator ?? throw new ArgumentNullException(nameof(annotator));
            _ignoreIsotopicPeak = ignoreIsotopicPeak;
            IonFeature = ionFeature;
            _baseQuery = new AnnotationQuery(Property, Scan, Isotopes, IonFeature, Parameter, null, ignoreIsotopicPeak);
        }

        public IMSIonProperty Property { get; }

        public IMSScanProperty Scan { get; }

        public IMSScanProperty NormalizedScan => _baseQuery.NormalizedScan;

        public IReadOnlyList<IsotopicPeak> Isotopes { get; }

        public IonFeatureCharacter IonFeature { get; }

        public MsRefSearchParameterBase Parameter { get; }

        public IEnumerable<MsScanMatchResult> FindCandidates(bool forceFind = false) {
            if (_finder is null || _reference is null || (!forceFind && _ignoreIsotopicPeak && !IonFeature.IsMonoIsotopicIon)) {
                return [];
            }
            else {
                return _finder.FindCandidates((_baseQuery, _reference));
            }
        }
    }
}

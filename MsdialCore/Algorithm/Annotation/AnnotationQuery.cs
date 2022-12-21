using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public class AnnotationQuery : IAnnotationQuery<MsScanMatchResult>
    {
        private readonly IMatchResultFinder<AnnotationQuery, MsScanMatchResult> _finder;
        private readonly bool _ignoreIsotopicPeak;

        public AnnotationQuery(
            IMSIonProperty property,
            IMSScanProperty scan,
            IReadOnlyList<IsotopicPeak> isotopes,
            IonFeatureCharacter ionFeature,
            MsRefSearchParameterBase parameter,
            IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder,
            bool ignoreIsotopicPeak = true) {
            if (property is null) {
                throw new ArgumentNullException(nameof(property));
            }
            if (scan is null) {
                throw new ArgumentNullException(nameof(scan));
            }
            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }
            Property = property;
            Scan = scan;
            Isotopes = isotopes;
            Parameter = parameter;
            _finder = finder;
            _ignoreIsotopicPeak = ignoreIsotopicPeak;
            IonFeature = ionFeature;
        }

        public IMSIonProperty Property { get; }
        public IMSScanProperty Scan { get; }
        public IMSScanProperty NormalizedScan {
            get {
                if (_normalizedScan is null) {
                    _normalizedScan = DataAccess.GetNormalizedMSScanProperty(Scan, Parameter);
                }
                return _normalizedScan;
            }
        }
        private MSScanProperty _normalizedScan;
        public IReadOnlyList<IsotopicPeak> Isotopes { get; }
        public IonFeatureCharacter IonFeature { get; }
        public MsRefSearchParameterBase Parameter { get; }

        public IEnumerable<MsScanMatchResult> FindCandidates(bool forceFind = false) {
            if (_finder == null || !forceFind && _ignoreIsotopicPeak && !IonFeature.IsMonoIsotopicIon) {
                return Enumerable.Empty<MsScanMatchResult>();
            }
            return _finder.FindCandidates(this);
        }
    }
}

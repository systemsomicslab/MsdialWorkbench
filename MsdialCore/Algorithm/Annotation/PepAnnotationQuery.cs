using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public class PepAnnotationQuery : IPepAnnotationQuery, IAnnotationQuery<MsScanMatchResult> {
        private readonly IMatchResultFinder<PepAnnotationQuery, MsScanMatchResult> _finder;
        private readonly bool _ignoreIsotopicPeak;

        public PepAnnotationQuery(
            IMSIonProperty property,
            IMSScanProperty scan,
            IReadOnlyList<IsotopicPeak> isotopes,
            IonFeatureCharacter ionFeature,
            MsRefSearchParameterBase msrefSearchParam,
            ProteomicsParameter proteomicsParam,
            IMatchResultFinder<PepAnnotationQuery, MsScanMatchResult> finder,
            bool ignoreIsotopicPeak = false) {
            if (property is null) {
                throw new ArgumentNullException(nameof(property));
            }
            if (scan is null) {
                throw new ArgumentNullException(nameof(scan));
            }
            if (msrefSearchParam is null) {
                throw new ArgumentNullException(nameof(msrefSearchParam));
            }

            Property = property;
            Scan = scan;
            Isotopes = isotopes;
            IonFeature = ionFeature;
            MsRefSearchParameter = msrefSearchParam;
            ProteomicsParameter = proteomicsParam;
            _finder = finder ?? throw new ArgumentNullException(nameof(finder));
            _ignoreIsotopicPeak = ignoreIsotopicPeak;
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
        public MsRefSearchParameterBase Parameter => MsRefSearchParameter;
        public MsRefSearchParameterBase MsRefSearchParameter { get; }
        public ProteomicsParameter ProteomicsParameter { get; }

        public IEnumerable<MsScanMatchResult> FindCandidates(bool forceFind = false) {
            if (_finder is null || (!forceFind && _ignoreIsotopicPeak && !IonFeature.IsMonoIsotopicIon)) {
                return Enumerable.Empty<MsScanMatchResult>();
            }
            return _finder.FindCandidates(this);
        }
    }
}

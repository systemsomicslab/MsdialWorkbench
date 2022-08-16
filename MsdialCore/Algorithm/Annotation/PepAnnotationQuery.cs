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

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public class PepAnnotationQuery : IPepAnnotationQuery, ICallableAnnotationQuery<MsScanMatchResult> {
        public IMSIonProperty Property { get; }
        public IMSScanProperty Scan { get; }
        public IMSScanProperty NormalizedScan {
            get {
                if (normalizedScan is null) {
                    normalizedScan = DataAccess.GetNormalizedMSScanProperty(Scan, Parameter);
                }
                return normalizedScan;
            }
        }
        private MSScanProperty normalizedScan;

        public IReadOnlyList<IsotopicPeak> Isotopes { get; }
        public IonFeatureCharacter IonFeature { get; }
        public MsRefSearchParameterBase Parameter => MsRefSearchParameter;
        public MsRefSearchParameterBase MsRefSearchParameter { get; }
        public ProteomicsParameter ProteomicsParameter { get; }

        private readonly IMatchResultFinder<PepAnnotationQuery, MsScanMatchResult> annotator;

        public PepAnnotationQuery(
            IMSIonProperty property,
            IMSScanProperty scan,
            IReadOnlyList<IsotopicPeak> isotopes,
            IonFeatureCharacter ionFeature,
            MsRefSearchParameterBase msrefSearchParam,
            ProteomicsParameter proteomicsParam,
            IMatchResultFinder<PepAnnotationQuery, MsScanMatchResult> annotator) {
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
            this.annotator = annotator ?? throw new ArgumentNullException(nameof(annotator));
        }

        public IEnumerable<MsScanMatchResult> FindCandidates() {
            return annotator.FindCandidates(this);
        }
    }
}

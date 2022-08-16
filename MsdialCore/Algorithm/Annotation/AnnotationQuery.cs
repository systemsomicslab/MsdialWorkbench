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
    public class AnnotationQuery : IAnnotationQuery, ICallableAnnotationQuery<MsScanMatchResult>
    {
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
        public MsRefSearchParameterBase Parameter { get; }

        private readonly IMatchResultFinder<AnnotationQuery, MsScanMatchResult> annotator;

        public AnnotationQuery(
            IMSIonProperty property,
            IMSScanProperty scan,
            IReadOnlyList<IsotopicPeak> isotopes,
            IonFeatureCharacter ionFeature,
            MsRefSearchParameterBase parameter,
            IMatchResultFinder<AnnotationQuery, MsScanMatchResult> annotator) {
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
            this.annotator = annotator;
            IonFeature = ionFeature;
        }

        public IEnumerable<MsScanMatchResult> FindCandidates() {
            if (annotator != null) {
                return annotator.FindCandidates(this);
            }
            return Enumerable.Empty<MsScanMatchResult>();
        }
    }
}

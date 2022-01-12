using CompMs.Common.DataObj;
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
    public interface IAnnotationQueryFactory<out T>
    {
        T Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter);
    }

    public class AnnotationQueryFactory : IAnnotationQueryFactory<AnnotationQuery>
    {
        private readonly PeakPickBaseParameter peakPickParameter;
        private readonly IMatchResultFinder<AnnotationQuery, MsScanMatchResult> annotator;

        public AnnotationQueryFactory(IMatchResultFinder<AnnotationQuery, MsScanMatchResult> annotator, PeakPickBaseParameter peakPickParameter) {
            this.peakPickParameter = peakPickParameter ?? throw new ArgumentNullException(nameof(peakPickParameter));
            this.annotator = annotator ?? throw new ArgumentNullException(nameof(annotator));
        }

        public AnnotationQuery Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter) {
            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            var isotopes = DataAccess.GetIsotopicPeaks(spectrum, (float)property.PrecursorMz, peakPickParameter.CentroidMs1Tolerance);
            return new AnnotationQuery(property, scan, isotopes, ionFeature, parameter, annotator);
        }
    }

    public class PepAnnotationQueryFactory : IAnnotationQueryFactory<PepAnnotationQuery> {
        private readonly PeakPickBaseParameter peakPickParameter;
        private readonly IMatchResultFinder<PepAnnotationQuery, MsScanMatchResult> annotator;

        public PepAnnotationQueryFactory(IMatchResultFinder<PepAnnotationQuery, MsScanMatchResult> annotator, PeakPickBaseParameter peakPickParameter, ProteomicsParameter proteomicsParameter) {
            this.peakPickParameter = peakPickParameter ?? throw new ArgumentNullException(nameof(peakPickParameter));
            ProteomicsParameter = proteomicsParameter;
            this.annotator = annotator ?? throw new ArgumentNullException(nameof(annotator));
        }

        private readonly ProteomicsParameter ProteomicsParameter;

        public PepAnnotationQuery Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter) {
            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            var isotopes = DataAccess.GetIsotopicPeaks(spectrum, (float)property.PrecursorMz, peakPickParameter.CentroidMs1Tolerance);
            return new PepAnnotationQuery(property, scan, isotopes, ionFeature, parameter, ProteomicsParameter, annotator);
        }
    }

    public class AnnotationQueryWithoutIsotopeFactory : IAnnotationQueryFactory<AnnotationQuery>
    {
        private readonly IMatchResultFinder<AnnotationQuery, MsScanMatchResult> annotator;

        public AnnotationQueryWithoutIsotopeFactory(IMatchResultFinder<AnnotationQuery, MsScanMatchResult> annotator) {
            this.annotator = annotator ?? throw new ArgumentNullException(nameof(annotator));
        }

        public AnnotationQuery Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter) {
            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            return new AnnotationQuery(property, scan, null, ionFeature, parameter, annotator);
        }
    }
}

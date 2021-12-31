using CompMs.Common.DataObj;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialCore.DataObj;
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

        public AnnotationQueryFactory(PeakPickBaseParameter peakPickParameter, MsRefSearchParameterBase searchParameter = null) {
            this.peakPickParameter = peakPickParameter ?? throw new ArgumentNullException(nameof(peakPickParameter));
            SearchParameter = searchParameter;
        }

        public MsRefSearchParameterBase SearchParameter { get; }

        public AnnotationQuery Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter) {
            var isotopes = DataAccess.GetIsotopicPeaks(spectrum, (float)property.PrecursorMz, peakPickParameter.CentroidMs1Tolerance);
            return new AnnotationQuery(property, scan, isotopes, ionFeature, parameter ?? SearchParameter);
        }
    }

    //public interface IPepAnnotationQueryFactory<out T> { //?
    //    T Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum);
    //}

    public class PepAnnotationQueryFactory : IAnnotationQueryFactory<PepAnnotationQuery> {
        private readonly PeakPickBaseParameter peakPickParameter;

        public PepAnnotationQueryFactory(PeakPickBaseParameter peakPickParameter, ProteomicsParameter proteomicsParameter, MsRefSearchParameterBase searchParameter = null) {
            this.peakPickParameter = peakPickParameter ?? throw new ArgumentNullException(nameof(peakPickParameter));
            SearchParameter = searchParameter;
            ProteomicsParameter = proteomicsParameter;
        }

        public MsRefSearchParameterBase SearchParameter { get; set; }
        public ProteomicsParameter ProteomicsParameter { get; set; }

        public PepAnnotationQuery Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter) {
            var isotopes = DataAccess.GetIsotopicPeaks(spectrum, (float)property.PrecursorMz, peakPickParameter.CentroidMs1Tolerance);
            return new PepAnnotationQuery(property, scan, isotopes, ionFeature, parameter ?? SearchParameter, ProteomicsParameter);
        }
    }

    public class AnnotationQueryWithoutIsotopeFactory : IAnnotationQueryFactory<AnnotationQuery>
    {
        public AnnotationQueryWithoutIsotopeFactory(MsRefSearchParameterBase searchParameter = null) {
            SearchParameter = searchParameter;
        }

        public MsRefSearchParameterBase SearchParameter { get; set; }

        public AnnotationQuery Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter) {
            return new AnnotationQuery(property, scan, null, ionFeature, parameter ?? SearchParameter);
        }
    }
}

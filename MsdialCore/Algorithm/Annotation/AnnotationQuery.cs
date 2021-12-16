using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IPepAnnotationQuery : IAnnotationQuery {
        MsRefSearchParameterBase MsRefSearchParameter { get; }
        ProteomicsParameter ProteomicsParameter { get; }
    }

    public class PepAnnotationQuery : IPepAnnotationQuery {
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

        public PepAnnotationQuery(
            IMSIonProperty property,
            IMSScanProperty scan,
            IReadOnlyList<IsotopicPeak> isotopes,
            IonFeatureCharacter ionFeature,
            MsRefSearchParameterBase msrefSearchParam, ProteomicsParameter proteomicsParam) {
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
        }
    }



    public interface IAnnotationQuery
    {
        IMSIonProperty Property { get; }
        IMSScanProperty Scan { get; }
        IMSScanProperty NormalizedScan { get; }
        IReadOnlyList<IsotopicPeak> Isotopes { get; }
        IonFeatureCharacter IonFeature { get; }
        MsRefSearchParameterBase Parameter { get; }
    }

    public class AnnotationQuery : IAnnotationQuery
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

        public AnnotationQuery(
            IMSIonProperty property,
            IMSScanProperty scan,
            IReadOnlyList<IsotopicPeak> isotopes,
            IonFeatureCharacter ionFeature,
            MsRefSearchParameterBase parameter) {
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
            IonFeature = ionFeature;
        }
    }
}

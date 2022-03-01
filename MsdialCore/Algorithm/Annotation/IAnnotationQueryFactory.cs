using CompMs.Common.Components;
using CompMs.Common.DataObj;
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
    public interface IAnnotationQueryFactory<out T>
    {
        string AnnotatorId { get; }
        T Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter);
    }

    public class AnnotationQueryFactory : IAnnotationQueryFactory<AnnotationQuery>
    {
        private readonly IMatchResultFinder<AnnotationQuery, MsScanMatchResult> annotator;
        private readonly Func<IMSIonProperty, IReadOnlyList<RawPeakElement>, IReadOnlyList<IsotopicPeak>> isotopeGetter;

        public AnnotationQueryFactory(IMatchResultFinder<AnnotationQuery, MsScanMatchResult> annotator, Func<IMSIonProperty, IReadOnlyList<RawPeakElement>, IReadOnlyList<IsotopicPeak>> isotopeGetter) {
            this.annotator = annotator;
            this.isotopeGetter = isotopeGetter;
            AnnotatorId = annotator.Id;
        }

        public AnnotationQueryFactory(IMatchResultFinder<AnnotationQuery, MsScanMatchResult> annotator, PeakPickBaseParameter peakPickParameter) : this(annotator, GetIsotopeGetter(peakPickParameter)) {

        }

        public string AnnotatorId { get; }

        public AnnotationQuery Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter) {
            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            return new AnnotationQuery(property, scan, isotopeGetter(property, spectrum), ionFeature, parameter, annotator);
        }

        private static Func<IMSIonProperty, IReadOnlyList<RawPeakElement>, IReadOnlyList<IsotopicPeak>> GetIsotopeGetter(PeakPickBaseParameter peakPickParameter) {
            if (peakPickParameter is null) {
                throw new ArgumentNullException(nameof(peakPickParameter));
            }

            IReadOnlyList<IsotopicPeak> GetIsotope(IMSIonProperty property, IReadOnlyList<RawPeakElement> spectrums) {
                return DataAccess.GetIsotopicPeaks(spectrums, (float)property.PrecursorMz, peakPickParameter.CentroidMs1Tolerance);
            }
            return GetIsotope;
        }
    }

    public class PepAnnotationQueryFactory : IAnnotationQueryFactory<PepAnnotationQuery> {
        private readonly PeakPickBaseParameter peakPickParameter;
        private readonly IMatchResultFinder<PepAnnotationQuery, MsScanMatchResult> annotator;

        public PepAnnotationQueryFactory(IMatchResultFinder<PepAnnotationQuery, MsScanMatchResult> annotator, PeakPickBaseParameter peakPickParameter, ProteomicsParameter proteomicsParameter) {
            this.peakPickParameter = peakPickParameter ?? throw new ArgumentNullException(nameof(peakPickParameter));
            ProteomicsParameter = proteomicsParameter;
            this.annotator = annotator ?? throw new ArgumentNullException(nameof(annotator));
            AnnotatorId = annotator.Id;
        }

        public string AnnotatorId { get; }

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
            AnnotatorId = annotator.Id;
        }

        public string AnnotatorId { get; }

        public AnnotationQuery Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter) {
            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            return new AnnotationQuery(property, scan, null, ionFeature, parameter, annotator);
        }
    }

    public class AnnotationQueryWithReferenceFactory : IAnnotationQueryFactory<AnnotationQueryWithReference>
    {
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer;
        private readonly IMatchResultFinder<(IAnnotationQuery, MoleculeMsReference), MsScanMatchResult> annotator;
        private readonly Func<IMSIonProperty, IReadOnlyList<RawPeakElement>, IReadOnlyList<IsotopicPeak>> isotopeGetter;

        public AnnotationQueryWithReferenceFactory(
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer,
            IMatchResultFinder<(IAnnotationQuery, MoleculeMsReference), MsScanMatchResult> annotator,
            Func<IMSIonProperty, IReadOnlyList<RawPeakElement>, IReadOnlyList<IsotopicPeak>> isotopeGetter) {
            this.refer = refer ?? throw new ArgumentNullException(nameof(refer));
            this.annotator = annotator;
            this.isotopeGetter = isotopeGetter;
            AnnotatorId = annotator.Id;
        }

        public AnnotationQueryWithReferenceFactory(
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer,
            IMatchResultFinder<(IAnnotationQuery, MoleculeMsReference), MsScanMatchResult> annotator,
            PeakPickBaseParameter peakPickParameter)
            : this(refer, annotator, GetIsotopeGetter(peakPickParameter)) {

        }

        public string AnnotatorId { get; }

        public AnnotationQueryWithReference Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter) {
            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }
            var annotatedObject = property as IAnnotatedObject;
            var result = annotatedObject?.MatchResults.TopResults
                .Where(r => r != null)
                .FirstOrDefault(r => !r.Source.HasFlag(SourceType.GeneratedLipid) && !r.Source.HasFlag(SourceType.Unknown));
            if (result is null) {
                return new AnnotationQueryWithReference(property, scan, null, isotopeGetter(property, spectrum), ionFeature, parameter, annotator);
            }
            else {
                var reference = refer.Refer(result);
                return new AnnotationQueryWithReference(property, scan, reference, isotopeGetter(property, spectrum), ionFeature, parameter, annotator);
            }
        }

        private static Func<IMSIonProperty, IReadOnlyList<RawPeakElement>, IReadOnlyList<IsotopicPeak>> GetIsotopeGetter(PeakPickBaseParameter peakPickParameter) {
            if (peakPickParameter is null) {
                throw new ArgumentNullException(nameof(peakPickParameter));
            }

            IReadOnlyList<IsotopicPeak> GetIsotope(IMSIonProperty property, IReadOnlyList<RawPeakElement> spectrums) {
                return DataAccess.GetIsotopicPeaks(spectrums, (float)property.PrecursorMz, peakPickParameter.CentroidMs1Tolerance);
            }
            return GetIsotope;
        }
    }
}

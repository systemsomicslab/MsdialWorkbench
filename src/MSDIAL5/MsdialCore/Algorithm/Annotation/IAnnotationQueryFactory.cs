using Accord.Collections;
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
    public interface IAnnotationQueryFactory<out T> {
        string AnnotatorId { get; }
        int Priority { get; }
        IAnnotationQuery<T> Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter);
        MsRefSearchParameterBase PrepareParameter();
        IMatchResultEvaluator<MsScanMatchResult> CreateEvaluator();
    }

    public sealed class AnnotationQueryFactory : IAnnotationQueryFactory<MsScanMatchResult>
    {
        private readonly IMatchResultFinder<AnnotationQuery, MsScanMatchResult> _annotator;
        private readonly MsRefSearchParameterBase _searchParameter;
        private readonly Func<IMSIonProperty, IReadOnlyList<RawPeakElement>, IReadOnlyList<IsotopicPeak>> _isotopeGetter;
        private readonly bool _ignoreIsotopicPeak;

        private AnnotationQueryFactory(IMatchResultFinder<AnnotationQuery, MsScanMatchResult> annotator, MsRefSearchParameterBase searchParameter, Func<IMSIonProperty, IReadOnlyList<RawPeakElement>, IReadOnlyList<IsotopicPeak>> isotopeGetter, bool ignoreIsotopicPeak) {
            _annotator = annotator;
            _searchParameter = searchParameter ?? throw new ArgumentNullException(nameof(searchParameter));
            _isotopeGetter = isotopeGetter;
            _ignoreIsotopicPeak = ignoreIsotopicPeak;
            AnnotatorId = annotator.Id;
        }

        public AnnotationQueryFactory(IMatchResultFinder<AnnotationQuery, MsScanMatchResult> annotator, PeakPickBaseParameter peakPickParameter, MsRefSearchParameterBase searchParameter, bool ignoreIsotopicPeak = true) : this(annotator, searchParameter, GetIsotopeGetter(peakPickParameter), ignoreIsotopicPeak) {

        }

        public string AnnotatorId { get; }

        int IAnnotationQueryFactory<MsScanMatchResult>.Priority => _annotator.Priority;

        public AnnotationQuery Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter) {
            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }
            return new AnnotationQuery(property, scan, _isotopeGetter(property, spectrum), ionFeature, parameter, _annotator, _ignoreIsotopicPeak);
        }

        IAnnotationQuery<MsScanMatchResult> IAnnotationQueryFactory<MsScanMatchResult>.Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter)
            => Create(property, scan, spectrum, ionFeature, parameter);

        MsRefSearchParameterBase IAnnotationQueryFactory<MsScanMatchResult>.PrepareParameter() {
            return new MsRefSearchParameterBase(_searchParameter);
        }

        private static Func<IMSIonProperty, IReadOnlyList<RawPeakElement>, IReadOnlyList<IsotopicPeak>> GetIsotopeGetter(PeakPickBaseParameter peakPickParameter) {
            if (peakPickParameter is null) {
                throw new ArgumentNullException(nameof(peakPickParameter));
            }

            IReadOnlyList<IsotopicPeak> GetIsotope(IMSIonProperty property, IReadOnlyList<RawPeakElement> spectrums) {
                return DataAccess.GetIsotopicPeaks(spectrums, (float)property.PrecursorMz, peakPickParameter.CentroidMs1Tolerance, peakPickParameter.MaxIsotopesDetectedInMs1Spectrum);
            }
            return GetIsotope;
        }

        IMatchResultEvaluator<MsScanMatchResult> IAnnotationQueryFactory<MsScanMatchResult>.CreateEvaluator() {
            return new MsScanMatchResultEvaluator(_searchParameter);
        }
    }

    public sealed class PepAnnotationQueryFactory : IAnnotationQueryFactory<MsScanMatchResult> {
        private readonly PeakPickBaseParameter _peakPickParameter;
        private readonly MsRefSearchParameterBase _searchParameter;
        private readonly IMatchResultFinder<PepAnnotationQuery, MsScanMatchResult> _annotator;
        private readonly ProteomicsParameter _proteomicsParameter;

        public PepAnnotationQueryFactory(IMatchResultFinder<PepAnnotationQuery, MsScanMatchResult> annotator, PeakPickBaseParameter peakPickParameter, MsRefSearchParameterBase searchParameter, ProteomicsParameter proteomicsParameter) {
            _peakPickParameter = peakPickParameter ?? throw new ArgumentNullException(nameof(peakPickParameter));
            _searchParameter = searchParameter ?? throw new ArgumentNullException(nameof(searchParameter));
            _proteomicsParameter = proteomicsParameter;
            _annotator = annotator ?? throw new ArgumentNullException(nameof(annotator));
            AnnotatorId = annotator.Id;
        }

        public string AnnotatorId { get; }

        int IAnnotationQueryFactory<MsScanMatchResult>.Priority => _annotator.Priority;

        public PepAnnotationQuery Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter) {
            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            var isotopes = DataAccess.GetIsotopicPeaks(spectrum, (float)property.PrecursorMz, _peakPickParameter.CentroidMs1Tolerance, _peakPickParameter.MaxIsotopesDetectedInMs1Spectrum);
            return new PepAnnotationQuery(property, scan, isotopes, ionFeature, parameter, _proteomicsParameter, _annotator, ignoreIsotopicPeak: false);
        }

        IAnnotationQuery<MsScanMatchResult> IAnnotationQueryFactory<MsScanMatchResult>.Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter)
            => Create(property, scan, spectrum, ionFeature, parameter);

        MsRefSearchParameterBase IAnnotationQueryFactory<MsScanMatchResult>.PrepareParameter() {
            return new MsRefSearchParameterBase(_searchParameter);
        }

        IMatchResultEvaluator<MsScanMatchResult> IAnnotationQueryFactory<MsScanMatchResult>.CreateEvaluator() {
            return new MsScanMatchResultEvaluator(_searchParameter);
        }
    }

    public sealed class AnnotationQueryWithoutIsotopeFactory : IAnnotationQueryFactory<MsScanMatchResult>
    {
        private readonly IMatchResultFinder<AnnotationQuery, MsScanMatchResult> _annotator;
        private readonly MsRefSearchParameterBase _searchParameter;

        public AnnotationQueryWithoutIsotopeFactory(IMatchResultFinder<AnnotationQuery, MsScanMatchResult> annotator, MsRefSearchParameterBase searchParameter) {
            _annotator = annotator ?? throw new ArgumentNullException(nameof(annotator));
            _searchParameter = searchParameter ?? throw new ArgumentNullException(nameof(searchParameter));
            AnnotatorId = annotator.Id;
        }

        public string AnnotatorId { get; }

        int IAnnotationQueryFactory<MsScanMatchResult>.Priority => _annotator.Priority;

        public AnnotationQuery Create(IMSIonProperty property, IMSScanProperty scan, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter) {
            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            return new AnnotationQuery(property, scan, null, ionFeature, parameter, _annotator, ignoreIsotopicPeak: false);
        }

        IAnnotationQuery<MsScanMatchResult> IAnnotationQueryFactory<MsScanMatchResult>.Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter)
            => Create(property, scan, ionFeature, parameter);

        MsRefSearchParameterBase IAnnotationQueryFactory<MsScanMatchResult>.PrepareParameter() {
            return new MsRefSearchParameterBase(_searchParameter);
        }

        IMatchResultEvaluator<MsScanMatchResult> IAnnotationQueryFactory<MsScanMatchResult>.CreateEvaluator() {
            return new MsScanMatchResultEvaluator(_searchParameter);
        }
    }

    public sealed class AnnotationQueryWithReferenceFactory : IAnnotationQueryFactory<MsScanMatchResult>
    {
        private readonly IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> _refer;
        private readonly IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MsScanMatchResult> _annotator;
        private readonly MsRefSearchParameterBase _searchParameter;
        private readonly Func<IMSIonProperty, IReadOnlyList<RawPeakElement>, IReadOnlyList<IsotopicPeak>> _isotopeGetter;
        private readonly bool _ignoreIsotopicPeak;

        private AnnotationQueryWithReferenceFactory(
            IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer,
            IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MsScanMatchResult> annotator,
            MsRefSearchParameterBase searchParameter,
            Func<IMSIonProperty, IReadOnlyList<RawPeakElement>, IReadOnlyList<IsotopicPeak>> isotopeGetter,
            bool ignoreIsotopicPeak) {
            _refer = refer ?? throw new ArgumentNullException(nameof(refer));
            _annotator = annotator;
            _searchParameter = searchParameter ?? throw new ArgumentNullException(nameof(searchParameter));
            _isotopeGetter = isotopeGetter;
            _ignoreIsotopicPeak = ignoreIsotopicPeak;
            AnnotatorId = annotator.Id;
        }

        public AnnotationQueryWithReferenceFactory(
            IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer,
            IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MsScanMatchResult> annotator,
            PeakPickBaseParameter peakPickParameter,
            MsRefSearchParameterBase searchParameter,
            bool ignoreIsotopicPeak = true)
            : this(refer, annotator, searchParameter, GetIsotopeGetter(peakPickParameter), ignoreIsotopicPeak) {

        }

        public string AnnotatorId { get; }

        int IAnnotationQueryFactory<MsScanMatchResult>.Priority => _annotator.Priority;

        public AnnotationQueryWithReference Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter) {
            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }
            var annotatedObject = property as IAnnotatedObject;
            var result = annotatedObject?.MatchResults.TopResults
                .Where(r => r != null)
                .FirstOrDefault(r => !r.Source.HasFlag(SourceType.GeneratedLipid) && !r.Source.HasFlag(SourceType.Unknown));
            if (result is null) {
                return new AnnotationQueryWithReference(property, scan, null, _isotopeGetter(property, spectrum), ionFeature, parameter, _annotator, _ignoreIsotopicPeak);
            }
            else {
                var reference = _refer.Refer(result);
                return new AnnotationQueryWithReference(property, scan, reference, _isotopeGetter(property, spectrum), ionFeature, parameter, _annotator, _ignoreIsotopicPeak);
            }
        }

        IAnnotationQuery<MsScanMatchResult> IAnnotationQueryFactory<MsScanMatchResult>.Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter)
            => Create(property, scan, spectrum, ionFeature, parameter);

        MsRefSearchParameterBase IAnnotationQueryFactory<MsScanMatchResult>.PrepareParameter() {
            return new MsRefSearchParameterBase(_searchParameter);
        }

        IMatchResultEvaluator<MsScanMatchResult> IAnnotationQueryFactory<MsScanMatchResult>.CreateEvaluator() {
            return new MsScanMatchResultEvaluator(_searchParameter);
        }

        private static Func<IMSIonProperty, IReadOnlyList<RawPeakElement>, IReadOnlyList<IsotopicPeak>> GetIsotopeGetter(PeakPickBaseParameter peakPickParameter) {
            if (peakPickParameter is null) {
                throw new ArgumentNullException(nameof(peakPickParameter));
            }

            IReadOnlyList<IsotopicPeak> GetIsotope(IMSIonProperty property, IReadOnlyList<RawPeakElement> spectrums) {
                return DataAccess.GetIsotopicPeaks(spectrums, (float)property.PrecursorMz, peakPickParameter.CentroidMs1Tolerance, peakPickParameter.MaxIsotopesDetectedInMs1Spectrum);
            }
            return GetIsotope;
        }
    }
}

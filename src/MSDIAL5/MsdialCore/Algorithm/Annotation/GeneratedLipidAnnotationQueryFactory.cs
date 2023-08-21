using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Lipidomics;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public sealed class GeneratedLipidAnnotationQueryFactory : IAnnotationQueryFactory<MsScanMatchResult>
    {
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> _refer;
        private readonly IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, ILipid), MsScanMatchResult> _finder;
        private readonly MsRefSearchParameterBase _searchParameter;
        private readonly Func<IMSIonProperty, IReadOnlyList<RawPeakElement>, IReadOnlyList<IsotopicPeak>> _isotopeGetter;
        private readonly Func<MsScanMatchResult, ILipid> _lipidGetter;
        private readonly bool _ignoreIsotopicPeak;

        private GeneratedLipidAnnotationQueryFactory(
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer,
            IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, ILipid), MsScanMatchResult> finder,
            MsRefSearchParameterBase searchParameter,
            Func<IMSIonProperty, IReadOnlyList<RawPeakElement>, IReadOnlyList<IsotopicPeak>> isotopeGetter,
            Func<MsScanMatchResult, ILipid> lipidGetter,
            bool ignoreIsotopicPeak) {
            _refer = refer ?? throw new ArgumentNullException(nameof(refer));
            _finder = finder;
            _searchParameter = searchParameter ?? throw new ArgumentNullException(nameof(searchParameter));
            _isotopeGetter = isotopeGetter;
            _lipidGetter = lipidGetter;
            _ignoreIsotopicPeak = ignoreIsotopicPeak;
            AnnotatorId = finder.Id;
        }

        public GeneratedLipidAnnotationQueryFactory(
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer,
            IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, ILipid), MsScanMatchResult> annotator,
            PeakPickBaseParameter peakPickParameter,
            MsRefSearchParameterBase searchParameter,
            Func<MsScanMatchResult, ILipid> lipidGetter,
            bool ignoreIsotopicPeak = true)
            : this(refer, annotator, searchParameter, GetIsotopeGetter(peakPickParameter), lipidGetter, ignoreIsotopicPeak) {

        }

        public string AnnotatorId { get; }

        int IAnnotationQueryFactory<MsScanMatchResult>.Priority => _finder.Priority;

        IAnnotationQuery<MsScanMatchResult> IAnnotationQueryFactory<MsScanMatchResult>.Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter) {
            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }
            var annotatedObject = property as IAnnotatedObject;
            var result = annotatedObject?.MatchResults.TopResults
                .Where(r => r != null)
                .FirstOrDefault(r => !r.Source.HasFlag(SourceType.GeneratedLipid) && !r.Source.HasFlag(SourceType.Unknown));
            if (result is null) {
                return new GeneratedLipidAnnotationQuery(property, scan, null, null, _isotopeGetter(property, spectrum), ionFeature, parameter, _finder, _ignoreIsotopicPeak);
            }
            else {
                var reference = _refer.Refer(result);
                var lipid = _lipidGetter(result);
                return new GeneratedLipidAnnotationQuery(property, scan, reference, lipid, _isotopeGetter(property, spectrum), ionFeature, parameter, _finder, _ignoreIsotopicPeak);
            }
        }

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
                return DataAccess.GetIsotopicPeaks(spectrums, (float)property.PrecursorMz, peakPickParameter.CentroidMs1Tolerance);
            }
            return GetIsotope;
        }

        class GeneratedLipidAnnotationQuery : IAnnotationQuery<MsScanMatchResult>
        {
            private readonly MoleculeMsReference _reference;
            private readonly ILipid _lipid;
            private readonly IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, ILipid), MsScanMatchResult> _finder;
            private readonly AnnotationQuery _baseQuery;
            private readonly bool _ignoreIsotopicPeak;

            public GeneratedLipidAnnotationQuery(
                IMSIonProperty property,
                IMSScanProperty scan,
                MoleculeMsReference reference,
                ILipid lipid,
                IReadOnlyList<IsotopicPeak> isotopes,
                IonFeatureCharacter ionFeature,
                MsRefSearchParameterBase parameter,
                IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, ILipid), MsScanMatchResult> finder,
                bool ignoreIsotopicPeak = true) {

                Property = property ?? throw new ArgumentNullException(nameof(property));
                Scan = scan ?? throw new ArgumentNullException(nameof(scan));
                _reference = reference;
                _lipid = lipid;
                Isotopes = isotopes;
                Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
                _finder = finder ?? throw new ArgumentNullException(nameof(finder));
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
                if (_finder is null || _reference is null || _lipid is null || (!forceFind && _ignoreIsotopicPeak && !IonFeature.IsMonoIsotopicIon)) {
                    return Enumerable.Empty<MsScanMatchResult>();
                }
                else {
                    return _finder.FindCandidates((_baseQuery, _reference, _lipid));
                }
            }
        }
    }
}

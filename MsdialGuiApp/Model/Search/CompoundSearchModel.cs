using CompMs.App.Msdial.Model.Chart;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Search
{
    public abstract class CompoundSearchModel : DisposableModelBase, IDisposable
    {
        public CompoundSearchModel(
            IFileBean file,
            IMSIonProperty msIonProperty,
            IMoleculeProperty moleculeProperty,
            IReadOnlyList<CompoundSearcher> compoundSearchers) {
            if (file is null) {
                throw new ArgumentNullException(nameof(file));
            }

            if (msIonProperty is null) {
                throw new ArgumentNullException(nameof(msIonProperty));
            }

            if (moleculeProperty is null) {
                throw new ArgumentNullException(nameof(moleculeProperty));
            }

            File = file;
            MSIonProperty = msIonProperty;
            MoleculeProperty = moleculeProperty;

            CompoundSearchers = compoundSearchers;
            CompoundSearcher = CompoundSearchers.FirstOrDefault();
        }

        public IReadOnlyList<CompoundSearcher> CompoundSearchers { get; }

        public CompoundSearcher CompoundSearcher {
            get => compoundSearcher;
            set => SetProperty(ref compoundSearcher, value);
        }
        private CompoundSearcher compoundSearcher;
        
        public IFileBean File { get; }

        public IMSIonProperty MSIonProperty { get; }

        public IMoleculeProperty MoleculeProperty { get; }

        public MsRefSearchParameterBase Parameter { get; }

        public MsSpectrumModel MsSpectrumModel { get; protected set; }

        public MoleculeMsReference SelectedReference { 
            get => selectedReference;
            set => SetProperty(ref selectedReference, value);
        }
        private MoleculeMsReference selectedReference;

        public MsScanMatchResult SelectedMatchResult {
            get => selectedMatchResult;
            set => SetProperty(ref selectedMatchResult, value);
        }
        private MsScanMatchResult selectedMatchResult;

        public abstract CompoundResultCollection Search();

        public abstract void SetConfidence();

        public abstract void SetUnsettled();

        public abstract void SetUnknown();
    }

    public class CompoundSearcher
    {
        private readonly IAnnotationQueryFactory<IAnnotationQueryZZZ<MsScanMatchResult>> queryFactory;
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer;

        public CompoundSearcher(
            IAnnotationQueryFactory<IAnnotationQueryZZZ<MsScanMatchResult>> queryFactory,
            MsRefSearchParameterBase msRefSearchParameter,
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            this.queryFactory = queryFactory ?? throw new ArgumentNullException(nameof(queryFactory));
            MsRefSearchParameter = msRefSearchParameter is null
                ? new MsRefSearchParameterBase()
                : new MsRefSearchParameterBase(msRefSearchParameter);
            this.refer = refer ?? throw new ArgumentNullException(nameof(refer));

            Id = queryFactory.AnnotatorId;
        }

        public string Id { get; }

        public MsRefSearchParameterBase MsRefSearchParameter { get; }

        public IEnumerable<ICompoundResult> Search(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature) {
            var candidates = queryFactory.Create(
                property,
                scan,
                spectrum,
                ionFeature,
                MsRefSearchParameter
            ).FindCandidates().ToList();
            foreach (var candidate in candidates) {
                candidate.Source |= SourceType.Manual;
            }
            return candidates
                .OrderByDescending(result => result.TotalScore)
                .Select(result => new CompoundResult(refer.Refer(result), result));
        }
    }

    public class CompoundSearchModel<T> : CompoundSearchModel where T: IMSIonProperty, IMoleculeProperty
    {
        public CompoundSearchModel(
            IFileBean fileBean,
            T property,
            MSDecResult msdecResult,
            IReadOnlyList<CompoundSearcher> compoundSearchers)
            : base(fileBean, property, property, compoundSearchers) {
            if (property == null) {
                throw new ArgumentException(nameof(property));
            }

            Property = property;
            this.msdecResult = msdecResult ?? throw new ArgumentNullException(nameof(msdecResult));

            var referenceSpectrum = this.ObserveProperty(m => m.SelectedReference)
                .Where(c => c != null)
                .Select(c => c.Spectrum)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            MsSpectrumModel = new MsSpectrumModel(
                Observable.Return(this.msdecResult.Spectrum),
                referenceSpectrum,
                new PropertySelector<SpectrumPeak, double>(peak => peak.Mass),
                new PropertySelector<SpectrumPeak, double>(peak => peak.Intensity),
                new GraphLabels(string.Empty, "m/z", "Abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity)),
                nameof(SpectrumPeak.SpectrumComment),
                Observable.Return(MsSpectrumModel.GetBrush(Brushes.Blue)),
                Observable.Return(MsSpectrumModel.GetBrush(Brushes.Red))).AddTo(Disposables);
        }

        [Obsolete]
        public CompoundSearchModel(
            IFileBean fileBean,
            T property, MSDecResult msdecResult,
            IReadOnlyList<IsotopicPeak> isotopes,
            IReadOnlyList<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> annotators)
            : this(fileBean, property, msdecResult, ConvertToSearcherAndConcat(isotopes, annotators, new List<CompoundSearcher>())) {

        }

        private readonly MSDecResult msdecResult;

        public T Property { get; }

        public override CompoundResultCollection Search() {
            return new CompoundResultCollection
            {
                Results = SearchCore().ToList(),
            };
        }

        protected IEnumerable<ICompoundResult> SearchCore() {
            return CompoundSearcher.Search(
                Property,
                msdecResult,
                new List<RawPeakElement>(),
                new IonFeatureCharacter { IsotopeWeightNumber = 0, } // Assume this is not isotope.
            );
        }

        public override void SetConfidence() {
            var reference = SelectedReference;
            var result = SelectedMatchResult;
            DataAccess.SetMoleculeMsPropertyAsConfidence(Property, reference, result);
            if (Property is IAnnotatedObject obj) {
                obj.MatchResults.RemoveManuallyResults();
                obj.MatchResults.AddResult(result);
            }
        }

        public override void SetUnsettled() {
            var reference = SelectedReference;
            var result = SelectedMatchResult;
            DataAccess.SetMoleculeMsPropertyAsUnsettled(Property, reference, result);
            if (Property is IAnnotatedObject obj) {
                obj.MatchResults.RemoveManuallyResults();
                obj.MatchResults.AddResult(result);
            }
        }

        public override void SetUnknown() {
            DataAccess.ClearMoleculePropertyInfomation(Property);
            if (Property is IAnnotatedObject obj) {
                obj.MatchResults.RemoveManuallyResults();
                obj.MatchResults.AddResult(new MsScanMatchResult { Source = SourceType.Manual | SourceType.Unknown });
            }
        }

        private static List<CompoundSearcher> ConvertToSearcherAndConcat(
            IReadOnlyList<IsotopicPeak> isotopes,
            IReadOnlyList<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> annotators,
            IEnumerable<CompoundSearcher> compoundSearchers) {
            return annotators.Select(container => new CompoundSearcher(
                new AnnotationQueryFactory(container.Annotator, (_1, _2) => isotopes),
                container.Parameter,
                container.Annotator))
                .Concat(compoundSearchers).ToList();
        }
    }
}

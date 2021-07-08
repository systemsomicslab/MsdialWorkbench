using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    class AnalysisImmsVM : AnalysisFileViewModel {
        public AnalysisImmsVM(
            ImmsAnalysisModel model,
            AnalysisFileBean analysisFile,
            IAnnotator<IMSIonProperty, IMSScanProperty> mspAnnotator,
            IAnnotator<IMSIonProperty, IMSScanProperty> textDBAnnotator,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService)
            : base(model) {
            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            this.model = model;
            this.compoundSearchService = compoundSearchService;
            this.peakSpotTableService = peakSpotTableService;

            Target = this.model.Target.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Target.Subscribe(t => OnTargetChanged(t)).AddTo(Disposables);

            this.analysisFile = analysisFile;
            MspAnnotator = mspAnnotator;
            TextDBAnnotator = textDBAnnotator;

            var hAxis = model.PlotModel
                .ObserveProperty(m => m.HorizontalRange)
                .ToReactiveAxisManager<double>(new ChartMargin(0.05))
                .AddTo(Disposables);
            var vAxis = model.PlotModel
                .ObserveProperty(m => m.VerticalRange)
                .ToReactiveAxisManager<double>(new ChartMargin(0.05))
                .AddTo(Disposables);

            MassMin = this.model.MassMin;
            MassMax = this.model.MassMax;
            MassLower = new ReactiveProperty<double>(MassMin).AddTo(Disposables);
            MassUpper = new ReactiveProperty<double>(MassMax).AddTo(Disposables);
            MassLower.SetValidateNotifyError(v => v < MassMin ? "Too small" : null)
                .SetValidateNotifyError(v => v > MassUpper.Value ? "Too large" : null);
            MassUpper.SetValidateNotifyError(v => v < MassLower.Value ? "Too small" : null)
                .SetValidateNotifyError(v => v > MassMax ? "Too large" : null);

            DriftMin = this.model.ChromMin;
            DriftMax = this.model.ChromMax;
            DriftLower = new ReactiveProperty<double>(DriftMin).AddTo(Disposables);
            DriftUpper = new ReactiveProperty<double>(DriftMax).AddTo(Disposables);
            DriftLower.SetValidateNotifyError(v => v < DriftMin ? "Too small" : null)
                .SetValidateNotifyError(v => v > DriftUpper.Value ? "Too large" : null);
            DriftUpper.SetValidateNotifyError(v => v < DriftLower.Value ? "Too small" : null)
                .SetValidateNotifyError(v => v > DriftMax ? "Too large" : null);

            MetaboliteFilterKeyword = new ReactivePropertySlim<string>(string.Empty);
            MetaboliteFilterKeywords = MetaboliteFilterKeyword.Select(c => c.Split()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            CommentFilterKeyword = new ReactivePropertySlim<string>(string.Empty);
            CommentFilterKeywords = CommentFilterKeyword.Select(c => c.Split()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            var DisplayFilters = this.ObserveProperty(m => m.DisplayFilters)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var AmplitudeLowerValue = this.ObserveProperty(m => m.AmplitudeLowerValue)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var AmplitudeUpperValue = this.ObserveProperty(m => m.AmplitudeUpperValue)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            AmplitudeOrderMax = this.model.Ms1Peaks.DefaultIfEmpty().Max(peak => peak?.AmplitudeOrderValue) ?? 0d;
            AmplitudeOrderMin = this.model.Ms1Peaks.DefaultIfEmpty().Min(peak => peak?.AmplitudeOrderValue) ?? 0d;

            new[]
            {
                MassLower.ToUnit(),
                MassUpper.ToUnit(),
                DriftLower.ToUnit(),
                DriftUpper.ToUnit(),
                CommentFilterKeyword.ToUnit(),
                MetaboliteFilterKeyword.ToUnit(),
                DisplayFilters.ToUnit(),
                AmplitudeLowerValue.ToUnit(),
                AmplitudeUpperValue.ToUnit(),
            }.Merge()
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOnDispatcher()
            .Subscribe(_ => Ms1Peaks.Refresh())
            .AddTo(Disposables);

            Ms1Peaks = CollectionViewSource.GetDefaultView(this.model.Ms1Peaks);

            PlotViewModel = new AnalysisPeakPlotViewModel(model.PlotModel, brushSource: Observable.Return(model.Brush), horizontalAxis: hAxis, verticalAxis: vAxis).AddTo(Disposables);
            EicViewModel = new EicViewModel(model.EicModel, horizontalAxis: hAxis).AddTo(Disposables);
            RawDecSpectrumsViewModel = new RawDecSpectrumsViewModel(model.Ms2SpectrumModel).AddTo(Disposables);
            SurveyScanViewModel = new SurveyScanViewModel(model.SurveyScanModel, horizontalAxis: vAxis).AddTo(Disposables);
            PeakTableViewModel = new ImmsAnalysisPeakTableViewModel(
                this.model.PeakTableModel,
                Observable.Return(model.EicLoader),
                MassLower,
                MassUpper,
                DriftLower,
                DriftUpper,
                MetaboliteFilterKeyword,
                CommentFilterKeyword)
                .AddTo(Disposables);

            SearchCompoundCommand = new[] {
                Target.Select(t => t != null && t.InnerModel != null),
                model.MsdecResult.Select(r => r != null),
            }.CombineLatestValuesAreAllTrue()
            .ToReactiveCommand()
            .WithSubscribe(SearchCompound)
            .AddTo(Disposables);
        }

        private readonly ImmsAnalysisModel model;
        private readonly AnalysisFileBean analysisFile;
        public IAnnotator<IMSIonProperty, IMSScanProperty> MspAnnotator { get; }
        public IAnnotator<IMSIonProperty, IMSScanProperty> TextDBAnnotator { get; }
        private readonly IWindowService<CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;

        public ICollectionView Ms1Peaks {
            get => ms1Peaks;
            set {
                var old = ms1Peaks;
                if (SetProperty(ref ms1Peaks, value)) {
                    if (old != null) old.Filter -= PeakFilter;
                    if (ms1Peaks != null) ms1Peaks.Filter += PeakFilter;
                }
            }
        }
        private ICollectionView ms1Peaks;

        public override ICollectionView PeakSpots => ms1Peaks;

        public AnalysisPeakPlotViewModel PlotViewModel {
            get => plotViewModel;
            set => SetProperty(ref plotViewModel, value);
        }
        private AnalysisPeakPlotViewModel plotViewModel;

        public EicViewModel EicViewModel {
            get => eicViewModel;
            set => SetProperty(ref eicViewModel, value);
        }
        private EicViewModel eicViewModel;

        public RawDecSpectrumsViewModel RawDecSpectrumsViewModel {
            get => ms2ViewModel;
            set => SetProperty(ref ms2ViewModel, value);
        }
        private RawDecSpectrumsViewModel ms2ViewModel;

        public SurveyScanViewModel SurveyScanViewModel {
            get => surveyScanViewModel;
            set => SetProperty(ref surveyScanViewModel, value);
        }
        private SurveyScanViewModel surveyScanViewModel;

        public ImmsAnalysisPeakTableViewModel PeakTableViewModel {
            get => peakTableViewModel;
            set => SetProperty(ref peakTableViewModel, value);
        }
        private ImmsAnalysisPeakTableViewModel peakTableViewModel;

        public ReadOnlyReactivePropertySlim<ChromatogramPeakFeatureModel> Target { get; }

        public double MassMin { get; }
        public double MassMax { get; }
        public ReactiveProperty<double> MassLower { get; }
        public ReactiveProperty<double> MassUpper { get; }

        public double DriftMin { get; }
        public double DriftMax { get; }
        public ReactiveProperty<double> DriftLower { get; }
        public ReactiveProperty<double> DriftUpper { get; }

        public ReactivePropertySlim<string> MetaboliteFilterKeyword { get; }
        public ReadOnlyReactivePropertySlim<string[]> MetaboliteFilterKeywords { get; }
        public ReactivePropertySlim<string> CommentFilterKeyword { get; }
        public ReadOnlyReactivePropertySlim<string[]> CommentFilterKeywords { get; }

        public bool RefMatchedChecked {
            get => ReadDisplayFilters(DisplayFilter.RefMatched);
            set {
                if (ReadDisplayFilters(DisplayFilter.RefMatched) != value) {
                    displayFilters.Write(DisplayFilter.RefMatched, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public bool SuggestedChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.Suggested);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.Suggested) != value) {
                    displayFilters.Write(DisplayFilter.Suggested, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public bool UnknownChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.Unknown);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.Unknown) != value) {
                    displayFilters.Write(DisplayFilter.Unknown, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public bool Ms2AcquiredChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.Ms2Acquired);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.Ms2Acquired) != value) {
                    displayFilters.Write(DisplayFilter.Ms2Acquired, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public bool MolecularIonChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.MolecularIon);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.MolecularIon) != value) {
                    displayFilters.Write(DisplayFilter.MolecularIon, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public bool UniqueIonsChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.UniqueIons);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.UniqueIons) != value) {
                    displayFilters.Write(DisplayFilter.UniqueIons, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public bool CcsChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.CcsMatched);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.CcsMatched) != value) {
                    displayFilters.Write(DisplayFilter.CcsMatched, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public bool ManuallyModifiedChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.ManuallyModified);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.ManuallyModified) != value) {
                    displayFilters.Write(DisplayFilter.ManuallyModified, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public DisplayFilter DisplayFilters {
            get => displayFilters;
            internal set => SetProperty(ref displayFilters, value);
        }
        private DisplayFilter displayFilters = DisplayFilter.Unset;

        internal bool ReadDisplayFilters(DisplayFilter flag) {
            return displayFilters.Read(flag);
        }

        public double AmplitudeLowerValue {
            get => amplitudeLowerValue;
            set => SetProperty(ref amplitudeLowerValue, value);
        }

        public double AmplitudeUpperValue {
            get => amplitudeUpperValue;
            set => SetProperty(ref amplitudeUpperValue, value);
        }
        private double amplitudeLowerValue = 0d, amplitudeUpperValue = 1d;

        public double AmplitudeOrderMin { get; }
        public double AmplitudeOrderMax { get; }

        public int FocusID {
            get => focusID;
            set => SetProperty(ref focusID, value);
        }
        private int focusID;

        public double FocusDt {
            get => focusDt;
            set => SetProperty(ref focusDt, value);
        }
        private double focusDt;

        public double FocusMz {
            get => focusMz;
            set => SetProperty(ref focusMz, value);
        }
        private double focusMz;

        bool PeakFilter(object obj) {
            if (obj is ChromatogramPeakFeatureModel peak) {
                return AnnotationFilter(peak)
                    && MzFilter(peak)
                    && DriftFilter(peak)
                    && AmplitudeFilter(peak)
                    && (!Ms2AcquiredChecked || peak.IsMsmsContained)
                    && (!MolecularIonChecked || peak.IsotopeWeightNumber == 0)
                    && (!ManuallyModifiedChecked || peak.InnerModel.IsManuallyModifiedForAnnotation)
                    && MetaboliteFilter(peak, MetaboliteFilterKeywords.Value)
                    && CommentFilter(peak, CommentFilterKeywords.Value);
            }
            return false;
        }

        bool AnnotationFilter(ChromatogramPeakFeatureModel peak) {
            if (!ReadDisplayFilters(DisplayFilter.Annotates)) return true;
            return RefMatchedChecked && peak.IsRefMatched
                || SuggestedChecked && peak.IsSuggested
                || UnknownChecked && peak.IsUnknown;
        }

        bool AmplitudeFilter(ChromatogramPeakFeatureModel peak) {
            return AmplitudeLowerValue * (AmplitudeOrderMax - AmplitudeOrderMin) <= peak.AmplitudeOrderValue - AmplitudeOrderMin
                && peak.AmplitudeScore - AmplitudeOrderMin <= AmplitudeUpperValue * (AmplitudeOrderMax - AmplitudeOrderMin);
        }

        bool MzFilter(ChromatogramPeakFeatureModel peak) {
            return MassLower.Value <= peak.Mass && peak.Mass <= MassUpper.Value;
        }

        bool DriftFilter(ChromatogramPeakFeatureModel peak) {
            return DriftLower.Value <= peak.ChromXValue && peak.ChromXValue <= DriftUpper.Value;
        }

        bool CommentFilter(ChromatogramPeakFeatureModel peak, IEnumerable<string> keywords) {
            return keywords.All(keyword => string.IsNullOrEmpty(keyword) || (peak.Comment?.Contains(keyword) ?? false));
        }

        bool MetaboliteFilter(ChromatogramPeakFeatureModel peak, IEnumerable<string> keywords) {
            return keywords.All(keyword => peak.Name.Contains(keyword));
        }

        void OnTargetChanged(ChromatogramPeakFeatureModel target) {
            if (target != null) {
                FocusID = target.InnerModel.MasterPeakID;
                FocusDt = target.ChromXValue ?? 0;
                FocusMz = target.Mass;
            }
        }

        public DelegateCommand<IAxisManager> FocusByIDCommand => focusByIDCommand ?? (focusByIDCommand = new DelegateCommand<IAxisManager>(FocusByID));
        private DelegateCommand<IAxisManager> focusByIDCommand;

        private void FocusByID(IAxisManager axis) {
            var focus = model.Ms1Peaks.FirstOrDefault(peak => peak.InnerModel.MasterPeakID == FocusID);
            Ms1Peaks.MoveCurrentTo(focus);
            axis?.Focus(focus.Mass - MzTol, focus.Mass + MzTol);
        }

        public DelegateCommand<IAxisManager> FocusByMzCommand => focusByMzCommand ?? (focusByMzCommand = new DelegateCommand<IAxisManager>(FocusByMz));
        private DelegateCommand<IAxisManager> focusByMzCommand;

        private static readonly double MzTol = 20;
        private void FocusByMz(IAxisManager axis) {
            axis?.Focus(FocusMz - MzTol, FocusMz + MzTol);
        }

        public ReactiveCommand SearchCompoundCommand { get; }

        private void SearchCompound() {
            using (var model = new ImmsCompoundSearchModel<ChromatogramPeakFeature>(
                analysisFile,
                Target.Value.InnerModel,
                this.model.MsdecResult.Value,
                null,
                MspAnnotator))
            using (var vm = new ImmsCompoundSearchVM(model)) {
                compoundSearchService.ShowDialog(vm);
            }
        }

        public DelegateCommand ShowIonTableCommand => showIonTableCommand ?? (showIonTableCommand = new DelegateCommand(ShowIonTable));

        private DelegateCommand showIonTableCommand;

        private void ShowIonTable() {
            peakSpotTableService.Show(PeakTableViewModel);
        }
    }
}

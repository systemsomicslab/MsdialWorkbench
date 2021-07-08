using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using CompMs.Graphics.Core.Base;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.CommonMVVM.WindowService;
using CompMs.App.Msdial.ViewModel.Table;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using CompMs.App.Msdial.ViewModel.Chart;
using System.Reactive.Linq;
using CompMs.App.Msdial.Model.Search;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    class AnalysisLcmsVM : AnalysisFileViewModel
    {
        public AnalysisLcmsVM(
            LcmsAnalysisModel model,
            AnalysisFileBean analysisFile,
            IWindowService<ViewModel.CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService)
            : base(model) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            if (analysisFile is null) {
                throw new ArgumentNullException(nameof(analysisFile));
            }

            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }

            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            this.model = model;
            this.analysisFile = analysisFile;
            this.compoundSearchService = compoundSearchService;
            this.peakSpotTableService = peakSpotTableService;

            Target = this.model.Target.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Target.Subscribe(OnTargetChanged).AddTo(Disposables);

            var hAxis = this.model.PlotModel
                .ObserveProperty(m => m.HorizontalRange)
                .ToReactiveAxisManager<double>(new ChartMargin(0.05))
                .AddTo(Disposables);
            var vAxis = this.model.PlotModel
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

            RtMin = this.model.ChromMin;
            RtMax = this.model.ChromMax;
            RtLower = new ReactiveProperty<double>(RtMin).AddTo(Disposables);
            RtUpper = new ReactiveProperty<double>(RtMax).AddTo(Disposables);
            RtLower.SetValidateNotifyError(v => v < RtMin ? "Too small" : null)
                .SetValidateNotifyError(v => v > RtUpper.Value ? "Too large" : null);
            RtUpper.SetValidateNotifyError(v => v < RtLower.Value ? "Too small" : null)
                .SetValidateNotifyError(v => v > RtMax ? "Too large" : null);

            MetaboliteFilterKeyword = new ReactivePropertySlim<string>(string.Empty);
            MetaboliteFilterKeywords = MetaboliteFilterKeyword.Select(w => w.Split())
                .ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            CommentFilterKeyword = new ReactivePropertySlim<string>(string.Empty);
            CommentFilterKeywords = CommentFilterKeyword.Select(w => w.Split())
                .ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            var DisplayFilters = this.ObserveProperty(m => m.DisplayFilters)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            var AmplitudeLowerValue = this.ObserveProperty(m => m.AmplitudeLowerValue)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var AmplitudeUpperValue = this.ObserveProperty(m => m.AmplitudeUpperValue)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            AmplitudeOrderMin = this.model.Ms1Peaks.DefaultIfEmpty().Min(peak => peak?.AmplitudeOrderValue) ?? 0d;
            AmplitudeOrderMax = this.model.Ms1Peaks.DefaultIfEmpty().Max(peak => peak?.AmplitudeOrderValue) ?? 0d;

            new[]
            {
                MassLower.ToUnit(),
                MassUpper.ToUnit(),
                RtLower.ToUnit(),
                RtUpper.ToUnit(),
                CommentFilterKeyword.ToUnit(),
                MetaboliteFilterKeyword.ToUnit(),
                DisplayFilters.ToUnit(),
                AmplitudeLowerValue.ToUnit(),
                AmplitudeUpperValue.ToUnit(),
            }.Merge()
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOnDispatcher()
            .Subscribe(_ => Ms1Peaks?.Refresh())
            .AddTo(Disposables);

            Ms1Peaks = CollectionViewSource.GetDefaultView(this.model.Ms1Peaks);

            PlotViewModel = new AnalysisPeakPlotViewModel(
                this.model.PlotModel,
                brushSource: Observable.Return(this.model.Brush),
                horizontalAxis: hAxis,
                verticalAxis: vAxis).AddTo(Disposables);
            EicViewModel = new EicViewModel(
                this.model.EicModel,
                horizontalAxis: hAxis).AddTo(Disposables);
            RawDecSpectrumsViewModel = new RawDecSpectrumsViewModel(this.model.Ms2SpectrumModel).AddTo(Disposables);
            SurveyScanViewModel = new SurveyScanViewModel(this.model.SurveyScanModel, horizontalAxis: hAxis).AddTo(Disposables);
            PeakTableViewModel = new LcmsAnalysisPeakTableViewModel(
                this.model.PeakTableModel,
                Observable.Return(this.model.EicLoader),
                MassLower,
                MassUpper,
                RtLower,
                RtUpper,
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

        private readonly LcmsAnalysisModel model;
        private readonly AnalysisFileBean analysisFile;
        private readonly IWindowService<ViewModel.CompoundSearchVM> compoundSearchService;
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

        public AnalysisPeakPlotViewModel PlotViewModel { get; }
        public EicViewModel EicViewModel { get; }
        public RawDecSpectrumsViewModel RawDecSpectrumsViewModel { get; }
        public SurveyScanViewModel SurveyScanViewModel { get; }
        public LcmsAnalysisPeakTableViewModel PeakTableViewModel { get; }
        public List<ChromatogramPeakFeature> Peaks { get; }

        public ReadOnlyReactivePropertySlim<ChromatogramPeakFeatureModel> Target { get; }

        /*
        public string RawSplashKey {
            get => rawSplashKey;
            set => SetProperty(ref rawSplashKey, value);
        }

        public string DeconvolutionSplashKey {
            get => deconvolutionSplashKey;
            set => SetProperty(ref deconvolutionSplashKey, value);
        }
        */

        internal bool ReadDisplayFilters(DisplayFilter flag) {
            return displayFilters.Read(flag);
        }

        internal void WriteDisplayFilters(DisplayFilter flag, bool value) {
            displayFilters.Write(flag, value);
        }

        internal bool SetDisplayFilters(DisplayFilter flag, bool value) {
            if (ReadDisplayFilters(flag) != value) {
                WriteDisplayFilters(flag, value);
                OnPropertyChanged(nameof(DisplayFilters));
                return true;
            }
            return false;
        }

        public DisplayFilter DisplayFilters {
            get => displayFilters;
            internal set => SetProperty(ref displayFilters, value);
        }
        private DisplayFilter displayFilters = DisplayFilter.Unset;

        public bool RefMatchedChecked {
            get => ReadDisplayFilters(DisplayFilter.RefMatched);
            set => SetDisplayFilters(DisplayFilter.RefMatched, value);
        }

        public bool SuggestedChecked {
            get => ReadDisplayFilters(DisplayFilter.Suggested);
            set => SetDisplayFilters(DisplayFilter.Suggested, value);
        }

        public bool UnknownChecked {
            get => ReadDisplayFilters(DisplayFilter.Unknown);
            set => SetDisplayFilters(DisplayFilter.Unknown, value);
        }

        public bool CcsChecked {
            get => ReadDisplayFilters(DisplayFilter.CcsMatched);
            set => SetDisplayFilters(DisplayFilter.CcsMatched, value);
        }

        public bool Ms2AcquiredChecked {
            get => ReadDisplayFilters(DisplayFilter.Ms2Acquired);
            set => SetDisplayFilters(DisplayFilter.Ms2Acquired, value);
        }

        public bool MolecularIonChecked {
            get => ReadDisplayFilters(DisplayFilter.MolecularIon);
            set => SetDisplayFilters(DisplayFilter.MolecularIon, value);
        }

        public bool BlankFilterChecked {
            get => ReadDisplayFilters(DisplayFilter.Blank);
            set => SetDisplayFilters(DisplayFilter.Blank, value);
        }

        public bool UniqueIonsChecked {
            get => ReadDisplayFilters(DisplayFilter.UniqueIons);
            set => SetDisplayFilters(DisplayFilter.UniqueIons, value);
        }

        public bool ManuallyModifiedChecked {
            get => ReadDisplayFilters(DisplayFilter.ManuallyModified);
            set => SetDisplayFilters(DisplayFilter.ManuallyModified, value);
        }

        public double AmplitudeOrderMin { get; }
        public double AmplitudeOrderMax { get; }

        public double AmplitudeLowerValue {
            get => amplitudeLowerValue;
            set => SetProperty(ref amplitudeLowerValue, value);
        }

        public double AmplitudeUpperValue {
            get => amplitudeUpperValue;
            set => SetProperty(ref amplitudeUpperValue, value);
        }
        private double amplitudeLowerValue = 0d, amplitudeUpperValue = 1d;

        public double RtMin { get; }
        public double RtMax { get; }
        public ReactiveProperty<double> RtLower { get; }
        public ReactiveProperty<double> RtUpper { get; }
        public double MassMin { get; }
        public double MassMax { get; }
        public ReactiveProperty<double> MassLower { get; }
        public ReactiveProperty<double> MassUpper { get; }
        public ReactivePropertySlim<string> MetaboliteFilterKeyword { get; }
        public ReadOnlyReactivePropertySlim<string[]> MetaboliteFilterKeywords { get; }
        public ReactivePropertySlim<string> CommentFilterKeyword { get; }
        public ReadOnlyReactivePropertySlim<string[]> CommentFilterKeywords { get; }

        bool PeakFilter(object obj) {
            if (obj is ChromatogramPeakFeatureModel peak) {
                return AnnotationFilter(peak)
                    && MzFilter(peak)
                    && RtFilter(peak)
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
            if (!(RefMatchedChecked || SuggestedChecked || UnknownChecked || CcsChecked)) return true;
            return RefMatchedChecked && peak.IsRefMatched
                || SuggestedChecked && peak.IsSuggested
                || UnknownChecked && peak.IsUnknown
                || CcsChecked && peak.IsCcsMatch;
        }

        bool MzFilter(ChromatogramPeakFeatureModel peak) {
            return MassLower.Value <= peak.Mass && peak.Mass <= MassUpper.Value;
        }

        bool RtFilter(ChromatogramPeakFeatureModel peak) {
            return RtLower.Value <= peak.ChromXValue && peak.ChromXValue <= RtUpper.Value;
        }

        bool AmplitudeFilter(ChromatogramPeakFeatureModel peak) {
            return AmplitudeLowerValue * (AmplitudeOrderMax - AmplitudeOrderMin) <= peak.AmplitudeOrderValue - AmplitudeOrderMin
                && peak.AmplitudeScore - AmplitudeOrderMin <= AmplitudeUpperValue * (AmplitudeOrderMax - AmplitudeOrderMin);
        }

        bool CommentFilter(ChromatogramPeakFeatureModel peak, IEnumerable<string> keywords) {
            return keywords.All(keyword => string.IsNullOrEmpty(keyword) || (peak.Comment?.Contains(keyword) ?? false));
        }

        bool MetaboliteFilter(ChromatogramPeakFeatureModel peak, IEnumerable<string> keywords) {
            return keywords.All(keyword => peak.Name.Contains(keyword));
        }

        void OnTargetChanged(ChromatogramPeakFeatureModel target) {
            if (!(target is null)) {
                FocusID = target.InnerModel.MasterPeakID;
                FocusRt = target.ChromXValue ?? 0;
                FocusMz = target.Mass;
            }
        }

        public int FocusID {
            get => focusID;
            set => SetProperty(ref focusID, value);
        }
        private int focusID;

        public double FocusRt {
            get => focusRt;
            set => SetProperty(ref focusRt, value);
        }
        private double focusRt;

        public double FocusMz {
            get => focusMz;
            set => SetProperty(ref focusMz, value);
        }
        private double focusMz;

        public DelegateCommand<object[]> FocusByIDCommand => focusByIDCommand ?? (focusByIDCommand = new DelegateCommand<object[]>(FocusByID));
        private DelegateCommand<object[]> focusByIDCommand;

        private void FocusByID(object[] axes) {
            var focus = model.Ms1Peaks.FirstOrDefault(peak => peak.InnerModel.MasterPeakID == FocusID);
            Ms1Peaks.MoveCurrentTo(focus);
            if (axes?.Length == 2) {
                (axes[0] as IAxisManager)?.Focus(focus.ChromXValue - RtTol, focus.ChromXValue + RtTol);
                (axes[1] as IAxisManager)?.Focus(focus.Mass - MzTol, focus.Mass + MzTol);
            }
        }

        public DelegateCommand<IAxisManager> FocusByRtCommand => focusByRtCommand ?? (focusByRtCommand = new DelegateCommand<IAxisManager>(FocusByRt));
        private DelegateCommand<IAxisManager> focusByRtCommand;

        private static readonly double RtTol = 0.5;
        private void FocusByRt(IAxisManager axis) {
            axis?.Focus(FocusRt - RtTol, FocusRt + RtTol);
        }

        public DelegateCommand<IAxisManager> FocusByMzCommand => focusByMzCommand ?? (focusByMzCommand = new DelegateCommand<IAxisManager>(FocusByMz));
        private DelegateCommand<IAxisManager> focusByMzCommand;

        private static readonly double MzTol = 20;
        private void FocusByMz(IAxisManager axis) {
            axis?.Focus(FocusMz - MzTol, FocusMz + MzTol);
        }

        public ReactiveCommand SearchCompoundCommand { get; }

        private void SearchCompound() {
            using (var model = new CompoundSearchModel<ChromatogramPeakFeature>(
                analysisFile,
                Target.Value.InnerModel,
                this.model.MsdecResult.Value,
                null,
                null))
            using (var vm = new ViewModel.CompoundSearchVM(model)) {
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

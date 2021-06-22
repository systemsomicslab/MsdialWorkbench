using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    class AnalysisImmsVM : AnalysisFileVM {
        public AnalysisImmsVM(
            ImmsAnalysisModel model,
            AnalysisFileBean analysisFile,
            IAnnotator<ChromatogramPeakFeature, MSDecResult> mspAnnotator,
            IAnnotator<ChromatogramPeakFeature, MSDecResult> textDBAnnotator,
            IWindowService<CompoundSearchVM<ChromatogramPeakFeature>> compoundSearchService) {

            this.model = model;
            this.compoundSearchService = compoundSearchService;

            var hAxis = model.PlotModel
                .ObserveProperty(m => m.HorizontalRange)
                .ToReactiveAxisManager<double>(new ChartMargin(0.05))
                .AddTo(Disposables);
            var vAxis = model.PlotModel
                .ObserveProperty(m => m.VerticalRange)
                .ToReactiveAxisManager<double>(new ChartMargin(0.05))
                .AddTo(Disposables);

            PlotViewModel = new AnalysisPeakPlotViewModel(model.PlotModel, brushSource: Observable.Return(model.Brush), horizontalAxis: hAxis, verticalAxis: vAxis).AddTo(Disposables);
            EicViewModel = new EicViewModel(model.EicModel, horizontalAxis: hAxis).AddTo(Disposables);
            RawDecSpectrumsViewModel = new RawDecSpectrumsViewModel(model.Ms2SpectrumModel).AddTo(Disposables);
            SurveyScanViewModel = new SurveyScanViewModel(model.SurveyScanModel, horizontalAxis: vAxis).AddTo(Disposables);

            Target = this.model.Target.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Target.Subscribe(t => OnTargetChanged(t)).AddTo(Disposables);

            this.analysisFile = analysisFile;
            this.mspAnnotator = mspAnnotator;
            this.textDBAnnotator = textDBAnnotator;
            FileName = analysisFile.AnalysisFileName;

            AmplitudeOrderMax = model.PlotModel.Spots.DefaultIfEmpty().Max(peak => peak?.AmplitudeOrderValue) ?? 0d;
            AmplitudeOrderMin = model.PlotModel.Spots.DefaultIfEmpty().Min(peak => peak?.AmplitudeOrderValue) ?? 0d;
            Ms1Peaks = CollectionViewSource.GetDefaultView(PlotViewModel.Spots);

            PropertyChanged += OnFilterChanged;

            SearchCompoundCommand = new[] {
                Target.Select(t => t != null && t.InnerModel != null),
                model.MsdecResult.Select(r => r != null),
            }.CombineLatestValuesAreAllTrue()
            .ToReactiveCommand()
            .AddTo(Disposables);

            SearchCompoundCommand.Subscribe(SearchCompound).AddTo(Disposables);
        }

        private readonly ImmsAnalysisModel model;
        private readonly AnalysisFileBean analysisFile;
        private readonly IAnnotator<ChromatogramPeakFeature, MSDecResult> mspAnnotator, textDBAnnotator;
        private readonly IWindowService<CompoundSearchVM<ChromatogramPeakFeature>> compoundSearchService;

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

        public ReadOnlyReactivePropertySlim<ChromatogramPeakFeatureModel> Target { get; }

        public string FileName {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }
        private string fileName;

        public string DisplayLabel {
            get => displayLabel;
            set => SetProperty(ref displayLabel, value);
        }
        private string displayLabel;

        public bool RefMatchedChecked => ReadDisplayFilters(DisplayFilter.RefMatched);
        public bool SuggestedChecked => ReadDisplayFilters(DisplayFilter.Suggested);
        public bool UnknownChecked => ReadDisplayFilters(DisplayFilter.Unknown);
        public bool Ms2AcquiredChecked => ReadDisplayFilters(DisplayFilter.Ms2Acquired);
        public bool MolecularIonChecked => ReadDisplayFilters(DisplayFilter.MolecularIon);
        public bool CcsChecked => ReadDisplayFilters(DisplayFilter.CcsMatched);
        public bool BlankFilterChecked => ReadDisplayFilters(DisplayFilter.Blank);
        public bool UniqueIonsChecked => ReadDisplayFilters(DisplayFilter.UniqueIons);
        public bool ManuallyModifiedChecked => ReadDisplayFilters(DisplayFilter.ManuallyModified);

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
                    && AmplitudeFilter(peak)
                    && (!Ms2AcquiredChecked || peak.IsMsmsContained)
                    && (!MolecularIonChecked || peak.IsotopeWeightNumber == 0)
                    && (!ManuallyModifiedChecked || peak.InnerModel.IsManuallyModifiedForAnnotation);
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

        void OnFilterChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(DisplayFilters)
                || e.PropertyName == nameof(AmplitudeLowerValue)
                || e.PropertyName == nameof(AmplitudeUpperValue))
                Ms1Peaks?.Refresh();
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
            using (var vm = new ImmsCompoundSearchVM<ChromatogramPeakFeature>(analysisFile, Target.Value.InnerModel, model.MsdecResult.Value, null, mspAnnotator)) {
                compoundSearchService.ShowDialog(vm);
            }
        }
    }
}

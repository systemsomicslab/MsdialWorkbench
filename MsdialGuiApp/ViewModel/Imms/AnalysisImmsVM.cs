using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using Microsoft.Win32;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    class AnalysisImmsVM : AnalysisFileViewModel {
        public AnalysisImmsVM(
            ImmsAnalysisModel model,
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

            Target.Subscribe(t => OnTargetChanged(t)).AddTo(Disposables);

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

            var DisplayFilters = this.ObserveProperty(m => m.DisplayFilters)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

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
            .Subscribe(_ => Ms1PeaksView.Refresh())
            .AddTo(Disposables);

            PlotViewModel = new AnalysisPeakPlotViewModel(this.model.PlotModel, brushSource: Observable.Return(this.model.Brush)).AddTo(Disposables);
            EicViewModel = new EicViewModel(this.model.EicModel, horizontalAxis: PlotViewModel.HorizontalAxis).AddTo(Disposables);


            var upperSpecBrush = new KeyBrushMapper<SpectrumComment, string>(
                this.model.parameter.ProjectParam.SpectrumCommentToColorBytes
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
                ),
                item => item.ToString(),
                Colors.Blue);

            var lowerSpecBrush = new KeyBrushMapper<SpectrumComment, string>(
               this.model.parameter.ProjectParam.SpectrumCommentToColorBytes
               .ToDictionary(
                   kvp => kvp.Key,
                   kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
               ),
               item => item.ToString(),
               Colors.Red);

            RawDecSpectrumsViewModel = new RawDecSpectrumsViewModel(this.model.Ms2SpectrumModel,
                upperSpectrumBrushSource: Observable.Return(upperSpecBrush),
                lowerSpectrumBrushSource: Observable.Return(lowerSpecBrush)).AddTo(Disposables);
            SurveyScanViewModel = new SurveyScanViewModel(this.model.SurveyScanModel, horizontalAxis: PlotViewModel.VerticalAxis).AddTo(Disposables);
            PeakTableViewModel = new ImmsAnalysisPeakTableViewModel(
                this.model.PeakTableModel,
                Observable.Return(this.model.EicLoader),
                MassLower,
                MassUpper,
                DriftLower,
                DriftUpper,
                MetaboliteFilterKeyword,
                CommentFilterKeyword)
                .AddTo(Disposables);

            SearchCompoundCommand = this.model.CanSearchCompound
                .ToReactiveCommand()
                .WithSubscribe(SearchCompound)
                .AddTo(Disposables);

            Ms1PeaksView.Filter += PeakFilter;
        }

        private readonly ImmsAnalysisModel model;
        private readonly IWindowService<CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;

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

        public double MassMin { get; }
        public double MassMax { get; }
        public ReactiveProperty<double> MassLower { get; }
        public ReactiveProperty<double> MassUpper { get; }

        public double DriftMin { get; }
        public double DriftMax { get; }
        public ReactiveProperty<double> DriftLower { get; }
        public ReactiveProperty<double> DriftUpper { get; }

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

        public bool Ms2AcquiredChecked {
            get => ReadDisplayFilters(DisplayFilter.Ms2Acquired);
            set => SetDisplayFilters(DisplayFilter.Ms2Acquired, value);
        }

        public bool MolecularIonChecked {
            get => ReadDisplayFilters(DisplayFilter.MolecularIon);
            set => SetDisplayFilters(DisplayFilter.MolecularIon, value);
        }

        public bool UniqueIonsChecked {
            get => ReadDisplayFilters(DisplayFilter.UniqueIons);
            set => SetDisplayFilters(DisplayFilter.UniqueIons, value);
        }

        public bool CcsChecked {
            get => ReadDisplayFilters(DisplayFilter.CcsMatched);
            set => SetDisplayFilters(DisplayFilter.CcsMatched, value);
        }

        public bool ManuallyModifiedChecked {
            get => ReadDisplayFilters(DisplayFilter.ManuallyModified);
            set => SetDisplayFilters(DisplayFilter.ManuallyModified, value);
        }

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

        protected bool PeakFilter(object obj) {
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
            return RefMatchedChecked && peak.IsRefMatched(model.MatchResultEvaluator)
                || SuggestedChecked && peak.IsSuggested(model.MatchResultEvaluator)
                || UnknownChecked && peak.IsUnknown;
        }

        bool MzFilter(ChromatogramPeakFeatureModel peak) {
            return MassLower.Value <= peak.Mass && peak.Mass <= MassUpper.Value;
        }

        bool DriftFilter(ChromatogramPeakFeatureModel peak) {
            return DriftLower.Value <= peak.ChromXValue && peak.ChromXValue <= DriftUpper.Value;
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
            Ms1PeaksView.MoveCurrentTo(focus);
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
            using (var csm = model.CreateCompoundSearchModel()) {
                if (csm is null) {
                    return;
                }
                using (var vm = new ImmsCompoundSearchVM(csm)) {
                    compoundSearchService.ShowDialog(vm);
                }
            }
        }

        public DelegateCommand ShowIonTableCommand => showIonTableCommand ?? (showIonTableCommand = new DelegateCommand(ShowIonTable));

        private DelegateCommand showIonTableCommand;

        private void ShowIonTable() {
            peakSpotTableService.Show(PeakTableViewModel);
        }

        public DelegateCommand<Window> SaveMs2SpectrumCommand => saveMs2SpectrumCommand ?? (saveMs2SpectrumCommand = new DelegateCommand<Window>(SaveSpectra, CanSaveSpectra));
        private DelegateCommand<Window> saveMs2SpectrumCommand;

        private void SaveSpectra(Window owner) {
            var sfd = new SaveFileDialog {
                Title = "Save spectra",
                Filter = "NIST format(*.msp)|*.msp", // MassBank format(*.txt)|*.txt;|MASCOT format(*.mgf)|*.mgf;
                RestoreDirectory = true,
                AddExtension = true,
            };

            if (sfd.ShowDialog(owner) == true) {
                var filename = sfd.FileName;
                this.model.SaveSpectra(filename);
            }
        }

        private bool CanSaveSpectra(Window owner) {
            return this.model.CanSaveSpectra();
        }
    }
}

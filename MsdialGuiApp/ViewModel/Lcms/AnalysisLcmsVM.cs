using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.DataObj;
using Microsoft.Win32;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    class AnalysisLcmsVM : AnalysisFileViewModel
    {
        public AnalysisLcmsVM(
            LcmsAnalysisModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService, 
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService)
            : base(model) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }

            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            if (proteomicsTableService is null) {
                throw new ArgumentNullException(nameof(proteomicsTableService));
            }

            this.model = model;
            this.compoundSearchService = compoundSearchService;
            this.peakSpotTableService = peakSpotTableService;
            this.proteomicsTableService = proteomicsTableService;

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

            var DisplayFilters = this.ObserveProperty(m => m.DisplayFilters)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);


            PlotViewModel = new AnalysisPeakPlotViewModel(this.model.PlotModel, brushSource: Observable.Return(this.model.Brush)).AddTo(Disposables);
            EicViewModel = new EicViewModel(
                this.model.EicModel,
                horizontalAxis: PlotViewModel.HorizontalAxis).AddTo(Disposables);

            var upperSpecBrush = new KeyBrushMapper<SpectrumComment, string>(
                this.model.Parameter.ProjectParam.SpectrumCommentToColorBytes
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
                ),
                item => item.ToString(),
                Colors.Blue);

            var lowerSpecBrush = new DelegateBrushMapper<SpectrumComment>(
                comment =>
                {
                    var commentString = comment.ToString();
                    var parameter = this.model.Parameter.ProjectParam;
                    if (parameter.SpectrumCommentToColorBytes.TryGetValue(commentString, out var color)) {
                        return Color.FromRgb(color[0], color[1], color[2]);
                    }
                    else if ((comment & SpectrumComment.doublebond) == SpectrumComment.doublebond
                        && parameter.SpectrumCommentToColorBytes.TryGetValue(SpectrumComment.doublebond.ToString(), out color)) {
                        return Color.FromRgb(color[0], color[1], color[2]);
                    }
                    else {
                        return Colors.Red;
                    }
                },
                true);

            RawDecSpectrumsViewModel = new RawDecSpectrumsViewModel(this.model.Ms2SpectrumModel,
                upperSpectrumBrushSource: Observable.Return(upperSpecBrush),
                lowerSpectrumBrushSource: Observable.Return(lowerSpecBrush)).AddTo(Disposables);

            RawPurifiedSpectrumsViewModel = new RawPurifiedSpectrumsViewModel(this.model.RawPurifiedSpectrumsModel,
                upperSpectrumBrushSource: Observable.Return(upperSpecBrush),
                lowerSpectrumBrushSource: Observable.Return(lowerSpecBrush)).AddTo(Disposables);


            SurveyScanViewModel = new SurveyScanViewModel(
                this.model.SurveyScanModel,
                horizontalAxis: PlotViewModel.VerticalAxis).AddTo(Disposables);

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

            ProteomicsPeakTableViewModel = new LcmsProteomicsPeakTableViewModel(
                this.model.PeakTableModel,
                Observable.Return(this.model.EicLoader),
                MassLower,
                MassUpper,
                RtLower,
                RtUpper,
                ProteinFilterKeyword,
                MetaboliteFilterKeyword,
                CommentFilterKeyword)
            .AddTo(Disposables);

            SearchCompoundCommand = this.model.CanSearchCompound
                .ToReactiveCommand()
                .WithSubscribe(SearchCompound)
                .AddTo(Disposables);


            ExperimentSpectrumViewModel = model.ExperimentSpectrumModel
                .Where(model_ => model_ != null)
                .Select(model_ => new ExperimentSpectrumViewModel(model_))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            FocusNavigatorViewModel = new FocusNavigatorViewModel(model.FocusNavigatorModel).AddTo(Disposables);

            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);
            PeakFilterViewModel = PeakSpotNavigatorViewModel.PeakFilterViewModel;
            Ms1PeaksView.Filter += PeakFilter;
            new[]
            {
                MassLower.ToUnit(),
                MassUpper.ToUnit(),
                RtLower.ToUnit(),
                RtUpper.ToUnit(),
                CommentFilterKeyword.ToUnit(),
                MetaboliteFilterKeyword.ToUnit(),
                ProteinFilterKeyword.ToUnit(),
                DisplayFilters.ToUnit(),
                AmplitudeLowerValue.ToUnit(),
                AmplitudeUpperValue.ToUnit(),
                PeakFilterViewModel.CheckedFilter.ToUnit(),
            }.Merge()
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOnDispatcher()
            .Subscribe(_ => Ms1PeaksView?.Refresh())
            .AddTo(Disposables);
        }

        private readonly LcmsAnalysisModel model;
        private readonly IWindowService<CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;
        private readonly IWindowService<PeakSpotTableViewModelBase> proteomicsTableService;

        public PeakFilterViewModel PeakFilterViewModel { get; }

        public AnalysisPeakPlotViewModel PlotViewModel { get; }
        public EicViewModel EicViewModel { get; }
        public RawDecSpectrumsViewModel RawDecSpectrumsViewModel { get; }
        public RawPurifiedSpectrumsViewModel RawPurifiedSpectrumsViewModel { get; }
        public SurveyScanViewModel SurveyScanViewModel { get; }
        public LcmsAnalysisPeakTableViewModel PeakTableViewModel { get; }
        public LcmsProteomicsPeakTableViewModel ProteomicsPeakTableViewModel { get; }
        public List<ChromatogramPeakFeature> Peaks { get; }

        public FocusNavigatorViewModel FocusNavigatorViewModel { get; }

        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }

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

        public bool RefMatchedChecked {
            get => PeakFilterViewModel.RefMatched;
            set => PeakFilterViewModel.RefMatched = value;
        }

        public bool SuggestedChecked {
            get => PeakFilterViewModel.Suggested;
            set => PeakFilterViewModel.Suggested = value;
        }

        public bool UnknownChecked {
            get => PeakFilterViewModel.Unknown;
            set => PeakFilterViewModel.Unknown = value;
        }

        public bool CcsChecked {
            get => PeakFilterViewModel.CcsMatched;
            set => PeakFilterViewModel.CcsMatched = value;
        }

        public bool Ms2AcquiredChecked {
            get => PeakFilterViewModel.Ms2Acquired;
            set => PeakFilterViewModel.Ms2Acquired = value;
        }

        public bool MolecularIonChecked {
            get => PeakFilterViewModel.MolecularIon;
            set => PeakFilterViewModel.MolecularIon = value;
        }

        public bool BlankFilterChecked {
            get => PeakFilterViewModel.Blank;
            set => PeakFilterViewModel.Blank = value;
        }

        public bool UniqueIonsChecked {
            get => PeakFilterViewModel.UniqueIons;
            set => PeakFilterViewModel.UniqueIons = value;
        }

        public bool ManuallyModifiedChecked {
            get => PeakFilterViewModel.ManuallyModified;
            set => PeakFilterViewModel.ManuallyModified = value;
        }

        public double RtMin { get; }
        public double RtMax { get; }
        public ReactiveProperty<double> RtLower { get; }
        public ReactiveProperty<double> RtUpper { get; }
        public double MassMin { get; }
        public double MassMax { get; }
        public ReactiveProperty<double> MassLower { get; }
        public ReactiveProperty<double> MassUpper { get; }

        protected bool PeakFilter(object obj) {
            if (obj is ChromatogramPeakFeatureModel peak) {
                return AnnotationFilter(peak)
                    && MzFilter(peak)
                    && RtFilter(peak)
                    && AmplitudeFilter(peak)
                    && (!Ms2AcquiredChecked || peak.IsMsmsContained)
                    && (!MolecularIonChecked || peak.IsotopeWeightNumber == 0)
                    && (!UniqueIonsChecked || peak.IsFragmentQueryExisted)
                    && (!ManuallyModifiedChecked || peak.InnerModel.IsManuallyModifiedForAnnotation)
                    && ProteinFilter(peak, ProteinFilterKeywords.Value)
                    && MetaboliteFilter(peak, MetaboliteFilterKeywords.Value)
                    && CommentFilter(peak, CommentFilterKeywords.Value);
            }
            return false;
        }

        bool AnnotationFilter(ChromatogramPeakFeatureModel peak) {
            if (!(PeakFilterViewModel.RefMatched || PeakFilterViewModel.Suggested || PeakFilterViewModel.Unknown || PeakFilterViewModel.CcsMatched)) return true;
            return RefMatchedChecked && peak.IsRefMatched(model.MatchResultEvaluator)
                || SuggestedChecked && peak.IsSuggested(model.MatchResultEvaluator)
                || UnknownChecked && peak.IsUnknown
                || CcsChecked && peak.IsCcsMatch;
        }

        bool MzFilter(ChromatogramPeakFeatureModel peak) {
            return MassLower.Value <= peak.Mass && peak.Mass <= MassUpper.Value;
        }

        bool RtFilter(ChromatogramPeakFeatureModel peak) {
            return RtLower.Value <= peak.ChromXValue && peak.ChromXValue <= RtUpper.Value;
        }

        public ReactiveCommand SearchCompoundCommand { get; }

        private void SearchCompound() {
            using (var csm = model.CreateCompoundSearchModel()) {
                if (csm is null) {
                    return;
                }
                using (var vm = new LcmsCompoundSearchViewModel(csm)) {
                    compoundSearchService.ShowDialog(vm);
                }
            }
        }

        public DelegateCommand ShowIonTableCommand => showIonTableCommand ?? (showIonTableCommand = new DelegateCommand(ShowIonTable));
        private DelegateCommand showIonTableCommand;

        private void ShowIonTable() {
            if (model.Parameter.TargetOmics == CompMs.Common.Enum.TargetOmics.Proteomics) {
                proteomicsTableService.Show(ProteomicsPeakTableViewModel);
            }
            else {
                peakSpotTableService.Show(PeakTableViewModel);
            }
        }

        public DelegateCommand<Window> SaveMs2SpectrumCommand => saveMs2SpectrumCommand ?? (saveMs2SpectrumCommand = new DelegateCommand<Window>(SaveSpectra, CanSaveSpectra));
        private DelegateCommand<Window> saveMs2SpectrumCommand;

        public DelegateCommand<Window> SaveMs2RawSpectrumCommand => saveMs2RawSpectrumCommand ?? (saveMs2RawSpectrumCommand = new DelegateCommand<Window>(SaveRawSpectra, CanSaveRawSpectra));
        private DelegateCommand<Window> saveMs2RawSpectrumCommand;

        public ReadOnlyReactivePropertySlim<ExperimentSpectrumViewModel> ExperimentSpectrumViewModel { get; }

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

        private void SaveRawSpectra(Window owner) {
            var sfd = new SaveFileDialog {
                Title = "Save raw spectra",
                Filter = "NIST format(*.msp)|*.msp", // MassBank format(*.txt)|*.txt;|MASCOT format(*.mgf)|*.mgf;
                RestoreDirectory = true,
                AddExtension = true,
            };

            if (sfd.ShowDialog(owner) == true) {
                var filename = sfd.FileName;
                this.model.SaveRawSpectra(filename);
            }
        }

        private bool CanSaveRawSpectra(Window owner) {
            return this.model.CanSaveRawSpectra();
        }
    }

}

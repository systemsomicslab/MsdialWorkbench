using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    internal sealed class LcmsAnalysisViewModel : ViewModelBase, IAnalysisResultViewModel
    {
        public LcmsAnalysisViewModel(
            LcmsAnalysisModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService,
            FocusControlManager focusControlManager) {
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

            if (focusControlManager is null) {
                throw new ArgumentNullException(nameof(focusControlManager));
            }

            this.model = model;
            this.compoundSearchService = compoundSearchService;
            this.peakSpotTableService = peakSpotTableService;
            this.proteomicsTableService = proteomicsTableService;

            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);

            var (peakPlotAction, peakPlotFocused) = focusControlManager.Request();
            PlotViewModel = new AnalysisPeakPlotViewModel(this.model.PlotModel, peakPlotAction, peakPlotFocused).AddTo(Disposables);
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

            var projectParameter = this.model.Parameter.ProjectParam;
            var lowerSpecBrush = new DelegateBrushMapper<SpectrumComment>(
                comment =>
                {
                    var commentString = comment.ToString();
                    if (projectParameter.SpectrumCommentToColorBytes.TryGetValue(commentString, out var color)) {
                        return Color.FromRgb(color[0], color[1], color[2]);
                    }
                    else if ((comment & SpectrumComment.doublebond) == SpectrumComment.doublebond
                        && projectParameter.SpectrumCommentToColorBytes.TryGetValue(SpectrumComment.doublebond.ToString(), out color)) {
                        return Color.FromRgb(color[0], color[1], color[2]);
                    }
                    else {
                        return Colors.Red;
                    }
                },
                true);

            var (rawDecSpectraViewFocusAction, rawDecSpectraViewFocused) = focusControlManager.Request();
            RawDecSpectrumsViewModel = new RawDecSpectrumsViewModel(this.model.Ms2SpectrumModel, rawDecSpectraViewFocusAction, rawDecSpectraViewFocused).AddTo(Disposables);

            RawPurifiedSpectrumsViewModel = new RawPurifiedSpectrumsViewModel(this.model.RawPurifiedSpectrumsModel,
                upperSpectrumBrushSource: Observable.Return(upperSpecBrush),
                lowerSpectrumBrushSource: Observable.Return(lowerSpecBrush)).AddTo(Disposables);

            var (ms2ChromatogramViewFocusAction, ms2ChromatogramViewFocused) = focusControlManager.Request();
            Ms2ChromatogramsViewModel = new Ms2ChromatogramsViewModel(model.Ms2ChromatogramsModel, ms2ChromatogramViewFocusAction, ms2ChromatogramViewFocused).AddTo(Disposables);

            SurveyScanViewModel = new SurveyScanViewModel(
                this.model.SurveyScanModel,
                horizontalAxis: PlotViewModel.VerticalAxis).AddTo(Disposables);

            PeakTableViewModel = new LcmsAnalysisPeakTableViewModel(
                this.model.PeakTableModel,
                Observable.Return(this.model.EicLoader),
                PeakSpotNavigatorViewModel.MzLowerValue,
                PeakSpotNavigatorViewModel.MzUpperValue,
                PeakSpotNavigatorViewModel.RtLowerValue,
                PeakSpotNavigatorViewModel.RtUpperValue,
                PeakSpotNavigatorViewModel.MetaboliteFilterKeyword,
                PeakSpotNavigatorViewModel.CommentFilterKeyword,
                PeakSpotNavigatorViewModel.OntologyFilterKeyword,
                PeakSpotNavigatorViewModel.AdductFilterKeyword,
                PeakSpotNavigatorViewModel.IsEditting)
            .AddTo(Disposables);

            ProteomicsPeakTableViewModel = new LcmsProteomicsPeakTableViewModel(
                this.model.PeakTableModel,
                Observable.Return(this.model.EicLoader),
                PeakSpotNavigatorViewModel.MzLowerValue,
                PeakSpotNavigatorViewModel.MzUpperValue,
                PeakSpotNavigatorViewModel.RtLowerValue,
                PeakSpotNavigatorViewModel.RtUpperValue,
                PeakSpotNavigatorViewModel.ProteinFilterKeyword,
                PeakSpotNavigatorViewModel.MetaboliteFilterKeyword,
                PeakSpotNavigatorViewModel.CommentFilterKeyword,
                PeakSpotNavigatorViewModel.OntologyFilterKeyword,
                PeakSpotNavigatorViewModel.AdductFilterKeyword,
                PeakSpotNavigatorViewModel.IsEditting)
            .AddTo(Disposables);

            SetUnknownCommand = model.Target.Select(t => !(t is null))
                .ToReactiveCommand()
                .WithSubscribe(() => model.Target.Value.SetUnknown())
                .AddTo(Disposables);
            SearchCompoundCommand = this.model.CanSearchCompound
                .ToReactiveCommand()
                .WithSubscribe(SearchCompound)
                .AddTo(Disposables);

            ExperimentSpectrumViewModel = model.ExperimentSpectrumModel
                .Where(model_ => model_ != null)
                .Select(model_ => new ExperimentSpectrumViewModel(model_))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            FocusNavigatorViewModel = new FocusNavigatorViewModel(model.FocusNavigatorModel).AddTo(Disposables);

            SaveMs2RawSpectrumCommand = model.CanSaveRawSpectra
                .ToAsyncReactiveCommand<Window>()
                .WithSubscribe(SaveRawSpectraAsync)
                .AddTo(Disposables);

            PeakInformationViewModel = new PeakInformationViewModel(model.PeakInformationModel).AddTo(Disposables);
            CompoundDetailViewModel = new CompoundDetailViewModel(model.CompoundDetailModel).AddTo(Disposables);
            var _peakDetailViewModels = new ReactiveCollection<ViewModelBase>().AddTo(Disposables);
            PeakDetailViewModels = new ViewModelBase[] { PeakInformationViewModel, CompoundDetailViewModel, };

            ProteinResultContainerAsObservable = Observable.Return(model.ProteinResultContainerModel);
        }

        private readonly LcmsAnalysisModel model;
        private readonly IWindowService<CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;
        private readonly IWindowService<PeakSpotTableViewModelBase> proteomicsTableService;

        public AnalysisPeakPlotViewModel PlotViewModel { get; }
        public EicViewModel EicViewModel { get; }
        public RawDecSpectrumsViewModel RawDecSpectrumsViewModel { get; }
        public RawPurifiedSpectrumsViewModel RawPurifiedSpectrumsViewModel { get; }
        public Ms2ChromatogramsViewModel Ms2ChromatogramsViewModel { get; }
        public SurveyScanViewModel SurveyScanViewModel { get; }
        public LcmsAnalysisPeakTableViewModel PeakTableViewModel { get; }
        public LcmsProteomicsPeakTableViewModel ProteomicsPeakTableViewModel { get; }
        public List<ChromatogramPeakFeature> Peaks { get; }

        public FocusNavigatorViewModel FocusNavigatorViewModel { get; }

        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }

        public ICommand SetUnknownCommand { get; }
        public ReactiveCommand SearchCompoundCommand { get; }

        private void SearchCompound() {
            using (var csm = model.CreateCompoundSearchModel()) {
                if (csm is null) {
                    return;
                }
                using (var vm = new LcmsCompoundSearchViewModel(csm, SetUnknownCommand)) {
                    compoundSearchService.ShowDialog(vm);
                }
            }
        }

        public ICommand ShowIonTableCommand => _showIonTableCommand ?? (_showIonTableCommand = new DelegateCommand(ShowIonTable));
        private DelegateCommand _showIonTableCommand;

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

        public AsyncReactiveCommand<Window> SaveMs2RawSpectrumCommand { get; }
        public PeakInformationViewModel PeakInformationViewModel { get; }
        public CompoundDetailViewModel CompoundDetailViewModel { get; }
        public ReadOnlyReactivePropertySlim<ExperimentSpectrumViewModel> ExperimentSpectrumViewModel { get; }
        public ViewModelBase[] PeakDetailViewModels { get; }
        public IObservable<ProteinResultContainerModel> ProteinResultContainerAsObservable { get; }

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

        private async Task SaveRawSpectraAsync(Window owner) {
            var sfd = new SaveFileDialog {
                Title = "Save raw spectra",
                Filter = "NIST format(*.msp)|*.msp", // MassBank format(*.txt)|*.txt;|MASCOT format(*.mgf)|*.mgf;
                RestoreDirectory = true,
                AddExtension = true,
            };

            if (sfd.ShowDialog(owner) == true) {
                var filename = sfd.FileName;
                await model.SaveRawSpectra(filename).ConfigureAwait(false);
            }
        }

        private bool CanSaveRawSpectra(Window owner) {
            return this.model.CanSaveRawSpectra.Value;
        }
    }

}

using CompMs.App.Msdial.Model.Core;
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
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    internal sealed class LcmsAnalysisViewModel : ViewModelBase, IAnalysisResultViewModel
    {
        private readonly LcmsAnalysisModel _model;
        private readonly IWindowService<CompoundSearchVM> _compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> _peakSpotTableService;
        private readonly IWindowService<PeakSpotTableViewModelBase> _proteomicsTableService;
        private readonly IMessageBroker _broker;

        public LcmsAnalysisViewModel(
            LcmsAnalysisModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService,
            IMessageBroker broker,
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

            this._model = model;
            this._compoundSearchService = compoundSearchService;
            this._peakSpotTableService = peakSpotTableService;
            this._proteomicsTableService = proteomicsTableService;
            _broker = broker;
            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);

            var (peakPlotAction, peakPlotFocused) = focusControlManager.Request();
            PlotViewModel = new AnalysisPeakPlotViewModel(this._model.PlotModel, peakPlotAction, peakPlotFocused).AddTo(Disposables);
            EicViewModel = new EicViewModel(
                this._model.EicModel,
                horizontalAxis: PlotViewModel.HorizontalAxis).AddTo(Disposables);

            var upperSpecBrush = new KeyBrushMapper<SpectrumComment, string>(
                this._model.Parameter.ProjectParam.SpectrumCommentToColorBytes
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
                ),
                item => item.ToString(),
                Colors.Blue);

            var projectParameter = this._model.Parameter.ProjectParam;
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
            RawDecSpectrumsViewModel = new RawDecSpectrumsViewModel(this._model.Ms2SpectrumModel, rawDecSpectraViewFocusAction, rawDecSpectraViewFocused).AddTo(Disposables);

            RawPurifiedSpectrumsViewModel = new RawPurifiedSpectrumsViewModel(this._model.RawPurifiedSpectrumsModel,
                upperSpectrumBrushSource: Observable.Return(upperSpecBrush),
                lowerSpectrumBrushSource: Observable.Return(lowerSpecBrush)).AddTo(Disposables);

            var (ms2ChromatogramViewFocusAction, ms2ChromatogramViewFocused) = focusControlManager.Request();
            Ms2ChromatogramsViewModel = new Ms2ChromatogramsViewModel(model.Ms2ChromatogramsModel, ms2ChromatogramViewFocusAction, ms2ChromatogramViewFocused).AddTo(Disposables);

            SurveyScanViewModel = new SurveyScanViewModel(
                this._model.SurveyScanModel,
                horizontalAxis: PlotViewModel.VerticalAxis).AddTo(Disposables);

            SetUnknownCommand = model.CanSetUnknown.ToReactiveCommand().WithSubscribe(model.SetUnknown).AddTo(Disposables);

            PeakTableViewModel = new LcmsAnalysisPeakTableViewModel(
                this._model.PeakTableModel,
                Observable.Return(this._model.EicLoader),
                PeakSpotNavigatorViewModel.MzLowerValue,
                PeakSpotNavigatorViewModel.MzUpperValue,
                PeakSpotNavigatorViewModel.RtLowerValue,
                PeakSpotNavigatorViewModel.RtUpperValue,
                PeakSpotNavigatorViewModel.MetaboliteFilterKeyword,
                PeakSpotNavigatorViewModel.CommentFilterKeyword,
                PeakSpotNavigatorViewModel.OntologyFilterKeyword,
                PeakSpotNavigatorViewModel.AdductFilterKeyword,
                SetUnknownCommand,
                PeakSpotNavigatorViewModel.IsEditting)
            .AddTo(Disposables);

            ProteomicsPeakTableViewModel = new LcmsProteomicsPeakTableViewModel(
                this._model.PeakTableModel,
                Observable.Return(this._model.EicLoader),
                PeakSpotNavigatorViewModel.MzLowerValue,
                PeakSpotNavigatorViewModel.MzUpperValue,
                PeakSpotNavigatorViewModel.RtLowerValue,
                PeakSpotNavigatorViewModel.RtUpperValue,
                PeakSpotNavigatorViewModel.ProteinFilterKeyword,
                PeakSpotNavigatorViewModel.MetaboliteFilterKeyword,
                PeakSpotNavigatorViewModel.CommentFilterKeyword,
                PeakSpotNavigatorViewModel.OntologyFilterKeyword,
                PeakSpotNavigatorViewModel.AdductFilterKeyword,
                SetUnknownCommand,
                PeakSpotNavigatorViewModel.IsEditting)
            .AddTo(Disposables);

            SearchCompoundCommand = this._model.CanSearchCompound
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
                .ToAsyncReactiveCommand()
                .WithSubscribe(SaveRawSpectraAsync)
                .AddTo(Disposables);

            PeakInformationViewModel = new PeakInformationViewModel(model.PeakInformationModel).AddTo(Disposables);
            CompoundDetailViewModel = new CompoundDetailViewModel(model.CompoundDetailModel).AddTo(Disposables);
            var matchResultCandidatesViewModel = new MatchResultCandidatesViewModel(model.MatchResultCandidatesModel).AddTo(Disposables);
            if (model.MoleculeStructureModel is null) {
                PeakDetailViewModels = new ViewModelBase[] { PeakInformationViewModel, CompoundDetailViewModel, matchResultCandidatesViewModel, };
            }
            else {
                MoleculeStructureViewModel = new MoleculeStructureViewModel(model.MoleculeStructureModel).AddTo(Disposables);
                PeakDetailViewModels = new ViewModelBase[] { PeakInformationViewModel, CompoundDetailViewModel, MoleculeStructureViewModel, matchResultCandidatesViewModel, };
            }

            ProteinResultContainerAsObservable = Observable.Return(model.ProteinResultContainerModel);
        }

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
            using (var csm = _model.CreateCompoundSearchModel()) {
                if (csm is null) {
                    return;
                }
                using (var vm = new LcmsCompoundSearchViewModel(csm, SetUnknownCommand)) {
                    _compoundSearchService.ShowDialog(vm);
                }
            }
        }

        public ICommand ShowIonTableCommand => _showIonTableCommand ?? (_showIonTableCommand = new DelegateCommand(ShowIonTable));
        private DelegateCommand _showIonTableCommand;

        private void ShowIonTable() {
            if (_model.Parameter.TargetOmics == CompMs.Common.Enum.TargetOmics.Proteomics) {
                _proteomicsTableService.Show(ProteomicsPeakTableViewModel);
            }
            else {
                _peakSpotTableService.Show(PeakTableViewModel);
            }
        }

        public DelegateCommand SaveMs2SpectrumCommand => _saveMs2SpectrumCommand ?? (_saveMs2SpectrumCommand = new DelegateCommand(SaveSpectra, _model.CanSaveSpectra));
        private DelegateCommand _saveMs2SpectrumCommand;

        public AsyncReactiveCommand SaveMs2RawSpectrumCommand { get; }
        public PeakInformationViewModel PeakInformationViewModel { get; }
        public CompoundDetailViewModel CompoundDetailViewModel { get; }
        public MoleculeStructureViewModel MoleculeStructureViewModel { get; }
        public ReadOnlyReactivePropertySlim<ExperimentSpectrumViewModel> ExperimentSpectrumViewModel { get; }
        public ViewModelBase[] PeakDetailViewModels { get; }
        public IObservable<ProteinResultContainerModel> ProteinResultContainerAsObservable { get; }

        private void SaveSpectra() {
            var filename = string.Empty;
            var request = new SaveFileNameRequest(file => filename = file)
            {
                Title = "Save spectra",
                Filter = "NIST format(*.msp)|*.msp", // MassBank format(*.txt)|*.txt;|MASCOT format(*.mgf)|*.mgf;
                RestoreDirectory = true,
                AddExtension = true,
            };
            _broker.Publish(request);

            if (request.Result == true) {
                _model.SaveSpectra(filename);
            }
        }

        private async Task SaveRawSpectraAsync() {
            var filename = string.Empty;
            var request = new SaveFileNameRequest(file => filename = file)
            {
                Title = "Save raw spectra",
                Filter = "NIST format(*.msp)|*.msp", // MassBank format(*.txt)|*.txt;|MASCOT format(*.mgf)|*.mgf;
                RestoreDirectory = true,
                AddExtension = true,
            };
            _broker.Publish(request);

            if (request.Result == true) {
                await _model.SaveRawSpectra(filename).ConfigureAwait(false);
            }
        }

        // IResultViewModel
        IResultModel IResultViewModel.Model => _model;
    }

}

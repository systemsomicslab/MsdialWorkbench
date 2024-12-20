using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    internal sealed class LcmsAnalysisViewModel : ViewModelBase, IAnalysisResultViewModel
    {
        private readonly LcmsAnalysisModel _model;
        private readonly IMessageBroker _broker;

        public LcmsAnalysisViewModel(LcmsAnalysisModel model, IMessageBroker broker, FocusControlManager focusControlManager) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }
            if (focusControlManager is null) {
                throw new ArgumentNullException(nameof(focusControlManager));
            }

            _model = model;
            _broker = broker;
            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);
            UndoManagerViewModel = new UndoManagerViewModel(model.UndoManager).AddTo(Disposables);

            var (peakPlotAction, peakPlotFocused) = focusControlManager.Request();
            PlotViewModel = new AnalysisPeakPlotViewModel(model.PlotModel, peakPlotAction, peakPlotFocused, broker).AddTo(Disposables);
            EicViewModel = new EicViewModel(
                model.EicModel,
                horizontalAxis: PlotViewModel.HorizontalAxis).AddTo(Disposables);


            var (rawDecSpectraViewFocusAction, rawDecSpectraViewFocused) = focusControlManager.Request();
            RawDecSpectrumsViewModel = new RawDecSpectrumsViewModel(model.Ms2SpectrumModel, rawDecSpectraViewFocusAction, rawDecSpectraViewFocused).AddTo(Disposables);

            RawPurifiedSpectrumsViewModel = new RawPurifiedSpectrumsViewModel(model.RawPurifiedSpectrumsModel, broker).AddTo(Disposables);

            var (ms2ChromatogramViewFocusAction, ms2ChromatogramViewFocused) = focusControlManager.Request();
            Ms2ChromatogramsViewModel = new Ms2ChromatogramsViewModel(model.Ms2ChromatogramsModel, ms2ChromatogramViewFocusAction, ms2ChromatogramViewFocused).AddTo(Disposables);

            SurveyScanViewModel = new SurveyScanViewModel(model.SurveyScanModel, horizontalAxis: PlotViewModel.VerticalAxis).AddTo(Disposables);

            SetUnknownCommand = model.CanSetUnknown.ToReactiveCommand().WithSubscribe(model.SetUnknown).AddTo(Disposables);

            PeakTableViewModel = LcmsTableViewModelHelper.CreateViewModel(
                model.PeakTableModel,
                Observable.Return(_model.EicLoader),
                PeakSpotNavigatorViewModel,
                SetUnknownCommand,
                UndoManagerViewModel)
            .AddTo(Disposables);

            SearchCompoundCommand = model.CanSearchCompound
                .ToReactiveCommand()
                .WithSubscribe(SearchCompound)
                .AddTo(Disposables);

            GoToMsfinderCommand = model.CanSearchCompound
                .ToReactiveCommand().WithSubscribe(() => {
                var msfinder = model.CreateSingleSearchMsfinderModel();
                if (msfinder is not null) {
                    broker.Publish(new InternalMsFinderSingleSpotViewModel(msfinder, broker));
                }
            }).AddTo(Disposables);

            ShowMsfinderSettingCommand = model.CanSearchCompound.ToReactiveCommand().WithSubscribe(() => {
                var msfinderSetting = model.MsfinderParameterSetting;
                if (msfinderSetting is not null) {
                    broker.Publish(new InternalMsfinderSettingViewModel(msfinderSetting, broker));
                }
            }).AddTo(Disposables);

            ExperimentSpectrumViewModel = model.ExperimentSpectrumModel
                .Where(model_ => model_ is not null)
                .Select(model_ => new ExperimentSpectrumViewModel(model_!))
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
        public AnalysisPeakTableViewModelBase PeakTableViewModel { get; }

        public FocusNavigatorViewModel FocusNavigatorViewModel { get; }

        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }
        public UndoManagerViewModel UndoManagerViewModel { get; }
        public ICommand SetUnknownCommand { get; }
        public ReactiveCommand SearchCompoundCommand { get; }

        private void SearchCompound() {
            using var csm = _model.CreateCompoundSearchModel();
            if (csm is null) {
                return;
            }
            using var vm = new LcmsCompoundSearchViewModel(csm);
            _broker.Publish<ICompoundSearchViewModel>(vm);
        }

        public ICommand ShowIonTableCommand => _showIonTableCommand ??= new DelegateCommand(ShowIonTable);
        private DelegateCommand? _showIonTableCommand;

        private void ShowIonTable() {
            _broker.Publish(PeakTableViewModel);
        }

        public DelegateCommand SearchAnalysisSpectrumByMoleculerNetworkingCommand => _searchAnalysisSpectrumByMoleculerNetworkingCommand ??= new DelegateCommand(SearchAnalysisSpectrumByMoleculerNetworkingMethod);
        private DelegateCommand? _searchAnalysisSpectrumByMoleculerNetworkingCommand;

        private void SearchAnalysisSpectrumByMoleculerNetworkingMethod() {
            _model.InvokeMoleculerNetworkingForTargetSpot();
        }
                
        public ReactiveCommand GoToMsfinderCommand { get; }
        public ReactiveCommand ShowMsfinderSettingCommand { get; }

        public DelegateCommand GoToExternalMsfinderCommand => _goToExternalMsfinderCommand ??= new DelegateCommand(_model.InvokeMsfinder);
        private DelegateCommand? _goToExternalMsfinderCommand;

        public DelegateCommand SaveMs2SpectrumCommand => _saveMs2SpectrumCommand ??= new DelegateCommand(SaveSpectra, _model.CanSaveSpectra);
        private DelegateCommand? _saveMs2SpectrumCommand;

        public AsyncReactiveCommand SaveMs2RawSpectrumCommand { get; }
        public PeakInformationViewModel PeakInformationViewModel { get; }
        public CompoundDetailViewModel CompoundDetailViewModel { get; }
        public MoleculeStructureViewModel? MoleculeStructureViewModel { get; }
        public ReadOnlyReactivePropertySlim<ExperimentSpectrumViewModel?> ExperimentSpectrumViewModel { get; }
        public ViewModelBase[] PeakDetailViewModels { get; }
        public IObservable<ProteinResultContainerModel?> ProteinResultContainerAsObservable { get; }

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

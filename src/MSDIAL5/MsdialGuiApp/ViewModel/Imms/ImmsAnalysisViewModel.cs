using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Reactive.Linq;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    internal sealed class ImmsAnalysisViewModel : ViewModelBase, IAnalysisResultViewModel
    {
        private readonly ImmsAnalysisModel _model;
        private readonly IWindowService<PeakSpotTableViewModelBase> _peakSpotTableService;
        private readonly IMessageBroker _broker;

        public ImmsAnalysisViewModel(
            ImmsAnalysisModel model,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IMessageBroker broker,
            FocusControlManager focusControlManager) {
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            if (focusControlManager is null) {
                throw new ArgumentNullException(nameof(focusControlManager));
            }

            _model = model;
            _peakSpotTableService = peakSpotTableService;
            _broker = broker;
            UndoManagerViewModel = new UndoManagerViewModel(model.UndoManager).AddTo(Disposables);
            SetUnknownCommand = model.CanSetUnknown.ToReactiveCommand().WithSubscribe(model.SetUnknown).AddTo(Disposables);

            var (focusAction, focused) = focusControlManager.Request();
            PlotViewModel = new AnalysisPeakPlotViewModel(_model.PlotModel, focusAction, focused, broker).AddTo(Disposables);
            EicViewModel = new EicViewModel(_model.EicModel, horizontalAxis: PlotViewModel.HorizontalAxis).AddTo(Disposables);

            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);

            var (rawDecSpectraViewFocusAction, rawDecSpectraViewFocused) = focusControlManager.Request();
            RawDecSpectrumsViewModel = new RawDecSpectrumsViewModel(_model.Ms2SpectrumModel, rawDecSpectraViewFocusAction, rawDecSpectraViewFocused).AddTo(Disposables);

            var (ms2ChromatogramViewFocusAction, ms2ChromatogramViewFocused) = focusControlManager.Request();
            Ms2ChromatogramsViewModel = new Ms2ChromatogramsViewModel(model.Ms2ChromatogramsModel, ms2ChromatogramViewFocusAction, ms2ChromatogramViewFocused).AddTo(Disposables);

            SurveyScanViewModel = new SurveyScanViewModel(_model.SurveyScanModel, horizontalAxis: PlotViewModel.VerticalAxis).AddTo(Disposables);
            PeakTableViewModel = new ImmsAnalysisPeakTableViewModel(_model.PeakTableModel, Observable.Return(_model.EicLoader), PeakSpotNavigatorViewModel, SetUnknownCommand, UndoManagerViewModel).AddTo(Disposables);

            SearchCompoundCommand = model.CanSearchCompound
                .ToReactiveCommand()
                .WithSubscribe(SearchCompound)
                .AddTo(Disposables);

            PeakInformationViewModel = new PeakInformationViewModel(model.PeakInformationModel).AddTo(Disposables);
            CompoundDetailViewModel = new CompoundDetailViewModel(model.CompoundDetailModel).AddTo(Disposables);
            MoleculeStructureViewModel = new MoleculeStructureViewModel(model.MoleculeStructureModel).AddTo(Disposables);
            var matchResultCandidateViewModel = new MatchResultCandidatesViewModel(model.MatchResultCandidatesModel).AddTo(Disposables);
            PeakDetailViewModels = new ViewModelBase[] { PeakInformationViewModel, CompoundDetailViewModel, MoleculeStructureViewModel, matchResultCandidateViewModel, };

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
        }

        public UndoManagerViewModel UndoManagerViewModel { get; }

        public AnalysisPeakPlotViewModel PlotViewModel { get; }
        public EicViewModel EicViewModel { get; }
        public RawDecSpectrumsViewModel RawDecSpectrumsViewModel { get; }
        public Ms2ChromatogramsViewModel Ms2ChromatogramsViewModel { get; }
        public SurveyScanViewModel SurveyScanViewModel { get; }
        public ImmsAnalysisPeakTableViewModel PeakTableViewModel { get; }
        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }
        public CompoundDetailViewModel CompoundDetailViewModel { get; }
        public MoleculeStructureViewModel MoleculeStructureViewModel { get; }
        public ViewModelBase[] PeakDetailViewModels { get; }

        public ReactiveCommand SearchCompoundCommand { get; }
        public PeakInformationViewModel PeakInformationViewModel { get; }

        public ReactiveCommand GoToMsfinderCommand { get; }
        public ReactiveCommand ShowMsfinderSettingCommand { get; }

        public DelegateCommand GoToExternalMsfinderCommand => _goToExternalMsfinderCommand ??= new DelegateCommand(_model.InvokeMsfinder);
        private DelegateCommand? _goToExternalMsfinderCommand;

        public ICommand SetUnknownCommand { get; }

        private void SearchCompound() {
            using var csm = _model.CreateCompoundSearchModel();
            if (csm is null) {
                return;
            }
            using var vm = new ImmsCompoundSearchVM(csm);
            _broker.Publish<ICompoundSearchViewModel>(vm);
        }

        public ICommand ShowIonTableCommand => _showIonTableCommand ??= new DelegateCommand(ShowIonTable);
        private DelegateCommand? _showIonTableCommand;

        private void ShowIonTable() {
            _peakSpotTableService.Show(PeakTableViewModel);
        }

        public DelegateCommand SearchAnalysisSpectrumByMoleculerNetworkingCommand => _searchAnalysisSpectrumByMoleculerNetworkingCommand ??= new DelegateCommand(SearchAnalysisSpectrumByMoleculerNetworkingMethod);
        private DelegateCommand? _searchAnalysisSpectrumByMoleculerNetworkingCommand;

        private void SearchAnalysisSpectrumByMoleculerNetworkingMethod() {
            _model.InvokeMoleculerNetworkingForTargetSpot();
        }
               
        public DelegateCommand SaveMs2SpectrumCommand => _saveMs2SpectrumCommand ??= new DelegateCommand(SaveSpectra, CanSaveSpectra);
        private DelegateCommand? _saveMs2SpectrumCommand;

        private void SaveSpectra() {
            var request = new SaveFileNameRequest(_model.SaveSpectra)
            {
                Title = "Save spectra",
                Filter = "NIST format(*.msp)|*.msp", // MassBank format(*.txt)|*.txt;|MASCOT format(*.mgf)|*.mgf;
                RestoreDirectory = true,
                AddExtension = true,
            };
            _broker.Publish(request);
        }

        private bool CanSaveSpectra() {
            return _model.CanSaveSpectra();
        }

        // IResultViewModel
        IResultModel IResultViewModel.Model => _model;
    }
}

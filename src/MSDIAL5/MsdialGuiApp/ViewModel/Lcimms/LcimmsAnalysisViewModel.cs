using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Lcimms;
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

namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    internal sealed class LcimmsAnalysisViewModel : ViewModelBase, IAnalysisResultViewModel
    {
        private readonly LcimmsAnalysisModel _model;
        private readonly IWindowService<PeakSpotTableViewModelBase> _peakSpotTableService;

        public LcimmsAnalysisViewModel(
            LcimmsAnalysisModel model,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            FocusControlManager focusControlManager,
            IMessageBroker broker) {
            if (focusControlManager is null) {
                throw new ArgumentNullException(nameof(focusControlManager));
            }

            _model = model ?? throw new ArgumentNullException(nameof(model));
            _peakSpotTableService = peakSpotTableService ?? throw new ArgumentNullException(nameof(peakSpotTableService));

            UndoManagerViewModel = new UndoManagerViewModel(model.UndoManager).AddTo(Disposables);
            SetUnknownCommand = model.CanSetUnknown.ToReactiveCommand().WithSubscribe(model.SetUnknown).AddTo(Disposables);

            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);

            var (rtmzPeakFocusAction, rtmzPeakFocused) = focusControlManager.Request();
            RtMzPlotViewModel = new AnalysisPeakPlotViewModel(model.RtMzPlotModel, rtmzPeakFocusAction, rtmzPeakFocused, broker).AddTo(Disposables);
            RtEicViewModel = new EicViewModel(
                model.RtEicModel,
                horizontalAxis: RtMzPlotViewModel.HorizontalAxis).AddTo(Disposables);

            var (dtmzPeakFocusAction, dtmzPeakFocused) = focusControlManager.Request();
            DtMzPlotViewModel = new AnalysisPeakPlotViewModel(model.DtMzPlotModel, dtmzPeakFocusAction, dtmzPeakFocused, broker).AddTo(Disposables);
            DtEicViewModel = new EicViewModel(
                model.DtEicModel,
                horizontalAxis: DtMzPlotViewModel.HorizontalAxis).AddTo(Disposables);

            var (rawDecSpectraViewFocusAction, rawDecSpectraViewFocused) = focusControlManager.Request();
            RawDecSpectrumsViewModel = new RawDecSpectrumsViewModel(model.Ms2SpectrumModel, rawDecSpectraViewFocusAction, rawDecSpectraViewFocused).AddTo(Disposables);

            var (ms2ChromatogramViewFocusAction, ms2ChromatogramViewFocused) = focusControlManager.Request();
            Ms2ChromatogramsViewModel = new Ms2ChromatogramsViewModel(model.Ms2ChromatogramsModel, ms2ChromatogramViewFocusAction, ms2ChromatogramViewFocused).AddTo(Disposables);

            RawPurifiedSpectrumsViewModel = new RawPurifiedSpectrumsViewModel(model.RawPurifiedSpectrumsModel, broker).AddTo(Disposables);

            SurveyScanViewModel = new SurveyScanViewModel(
                model.SurveyScanModel,
                horizontalAxis: RtMzPlotViewModel.VerticalAxis).AddTo(Disposables);
            PeakTableViewModel = new LcimmsAnalysisPeakTableViewModel(
                model.PeakTableModel,
                Observable.Return(model.DtEicLoader),
                PeakSpotNavigatorViewModel,
                SetUnknownCommand,
                UndoManagerViewModel)
                .AddTo(Disposables);

            SearchCompoundCommand = model.CanSearchCompound
                .ToReactiveCommand()
                .WithSubscribe(() =>
                {
                    if (model.CompoundSearchModel.Value is null) {
                        return;
                    }
                    using var vm = new LcimmsCompoundSearchViewModel(model.CompoundSearchModel.Value);
                    broker.Publish<ICompoundSearchViewModel>(vm);
                }).AddTo(Disposables);

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

            PeakInformationViewModel = new PeakInformationViewModel(model.PeakInformationModel).AddTo(Disposables);
            CompoundDetailViewModel = new CompoundDetailViewModel(model.CompoundDetailModel).AddTo(Disposables);
            MoleculeStructureViewModel = new MoleculeStructureViewModel(model.MoleculeStructureModel).AddTo(Disposables);
            var matchResultCandidateViewModel = new MatchResultCandidatesViewModel(model.MatchResultCandidatesModel).AddTo(Disposables);
            PeakDetailViewModels = new ViewModelBase[] { PeakInformationViewModel, CompoundDetailViewModel, MoleculeStructureViewModel, matchResultCandidateViewModel, };
        }

        public UndoManagerViewModel UndoManagerViewModel { get; }
        public AnalysisPeakPlotViewModel RtMzPlotViewModel { get; private set; }
        public EicViewModel RtEicViewModel { get; private set; }
        public AnalysisPeakPlotViewModel DtMzPlotViewModel { get; private set; }
        public EicViewModel DtEicViewModel { get; private set; }
        public RawDecSpectrumsViewModel RawDecSpectrumsViewModel { get; private set; }
        public Ms2ChromatogramsViewModel Ms2ChromatogramsViewModel { get; }
        public RawPurifiedSpectrumsViewModel RawPurifiedSpectrumsViewModel { get; }
        public SurveyScanViewModel SurveyScanViewModel { get; private set; }
        public LcimmsAnalysisPeakTableViewModel PeakTableViewModel { get; }
        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }
        public PeakInformationViewModel PeakInformationViewModel { get; }
        public CompoundDetailViewModel CompoundDetailViewModel { get; }
        public MoleculeStructureViewModel MoleculeStructureViewModel { get; }
        public ViewModelBase[] PeakDetailViewModels { get; }
        public ReactiveCommand GoToMsfinderCommand { get; }
        public ReactiveCommand ShowMsfinderSettingCommand { get; }
        public ICommand SetUnknownCommand { get; }
        public ReactiveCommand SearchCompoundCommand { get; }

        public ICommand ShowIonTableCommand => _showIonTableCommand ?? (_showIonTableCommand = new DelegateCommand(ShowIonTable));
        private DelegateCommand? _showIonTableCommand;

        private void ShowIonTable() {
            _peakSpotTableService.Show(PeakTableViewModel);
        }

        public DelegateCommand SearchAnalysisSpectrumByMoleculerNetworkingCommand => _searchAnalysisSpectrumByMoleculerNetworkingCommand ?? (_searchAnalysisSpectrumByMoleculerNetworkingCommand = new DelegateCommand(SearchAnalysisSpectrumByMoleculerNetworkingMethod));
        private DelegateCommand? _searchAnalysisSpectrumByMoleculerNetworkingCommand;

        private void SearchAnalysisSpectrumByMoleculerNetworkingMethod() {
            _model.InvokeMoleculerNetworkingForTargetSpot();
        }

        public DelegateCommand GoToExternalMsfinderCommand => _goToExternalMsfinderCommand ??= new DelegateCommand(_model.InvokeMsfinder);
        private DelegateCommand? _goToExternalMsfinderCommand;

        // IResultViewModel
        IResultModel IResultViewModel.Model => _model;
    }
}
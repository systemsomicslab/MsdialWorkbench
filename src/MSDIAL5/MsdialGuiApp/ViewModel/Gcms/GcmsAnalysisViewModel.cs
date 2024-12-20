using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Gcms;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Reactive.Linq;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Gcms
{
    internal sealed class GcmsAnalysisViewModel : ViewModelBase, IAnalysisResultViewModel
    {
        private readonly GcmsAnalysisModel _model;
        private readonly IWindowService<PeakSpotTableViewModelBase> _peakSpotTableService;
        private readonly IMessageBroker _broker;

        public GcmsAnalysisViewModel(GcmsAnalysisModel model, IWindowService<PeakSpotTableViewModelBase> peakSpotTableService, FocusControlManager focusControlManager, IMessageBroker broker) {
            _model = model;
            _peakSpotTableService = peakSpotTableService;
            _broker = broker;
            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);
            PeakPlotViewModel = new SpectrumFeaturePlotViewModel(model.PeakPlotModel).AddTo(Disposables);
            GcgcSpectrumPeakPlotViewModel = new GcgcSpectrumPeakPlotViewModel(model.GcgcPeaks, () => { }, Observable.Return(false), broker).AddTo(Disposables);
            EicViewModel = new EicViewModel(_model.EicModel, horizontalAxis: PeakPlotViewModel.HorizontalAxis as IAxisManager<double>).AddTo(Disposables);
            var (rawDecSpectraViewFocusAction, rawDecSpectraViewFocused) = focusControlManager.Request();
            RawDecSpectrumsViewModel = new RawDecSpectrumsViewModel(model.RawDecSpectrumModel, rawDecSpectraViewFocusAction, rawDecSpectraViewFocused).AddTo(Disposables);
            RawPurifiedSpectrumsViewModel = new RawPurifiedSpectrumsViewModel(model.RawPurifiedSpectrumsModel, broker).AddTo(Disposables);
            var (eiChromatogramsViewFocusAction, eiChromatogramsViewFocused) = focusControlManager.Request();
            EiChromatogramsViewModel = new EiChromatogramsViewModel(model.EiChromatogramsModel, model.NumberOfEIChromatograms, null, eiChromatogramsViewFocusAction, eiChromatogramsViewFocused).AddTo(Disposables);
            SurveyScanViewModel = new SurveyScanViewModel(model.SurveyScanModel, horizontalAxis: PeakPlotViewModel.VerticalAxis as IAxisManager<double>).AddTo(Disposables);

            FocusNavigatorViewModel = new FocusNavigatorViewModel(model.FocusNavigatorModel).AddTo(Disposables);
            
            var peakInformationViewModel = new PeakInformationViewModel(model.PeakInformationModel).AddTo(Disposables);
            var compoundDetailViewModel = new CompoundDetailViewModel(model.CompoundDetailModel).AddTo(Disposables);
            var matchResultCandidatesViewModel = new MatchResultCandidatesViewModel(model.MatchResultCandidatesModel).AddTo(Disposables);
            var moleculeStructureViewModel = new MoleculeStructureViewModel(model.MoleculeStructureModel).AddTo(Disposables);
            PeakDetailViewModels = [peakInformationViewModel, compoundDetailViewModel, moleculeStructureViewModel, matchResultCandidatesViewModel,];

            SetUnknownCommand = model.CanSetUnknown.ToReactiveCommand().WithSubscribe(model.SetUnknown).AddTo(Disposables);
            UndoManagerViewModel = new UndoManagerViewModel(model.UndoManager).AddTo(Disposables);
            PeakTableViewModel = new GcmsAnalysisPeakTableViewModel(model.PeakTableModel, Observable.Return(model.EicLoader), PeakSpotNavigatorViewModel, SetUnknownCommand, UndoManagerViewModel).AddTo(Disposables);

            GoToMsfinderCommand = new ReactiveCommand().WithSubscribe(() => {
                var msfinder = model.CreateSingleSearchMsfinderModel();
                if (msfinder is not null) {
                    broker.Publish(new InternalMsFinderSingleSpotViewModel(msfinder, broker));
                }
            }).AddTo(Disposables);

            ShowMsfinderSettingCommand = new ReactiveCommand().WithSubscribe(() => {
                var msfinderSetting = model.MsfinderParameterSetting;
                if (msfinderSetting is not null) {
                    broker.Publish(new InternalMsfinderSettingViewModel(msfinderSetting, broker));
                }
            }).AddTo(Disposables);
        }

        public IResultModel Model => _model;

        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }
        public GcgcSpectrumPeakPlotViewModel GcgcSpectrumPeakPlotViewModel { get; }

        public SpectrumFeaturePlotViewModel PeakPlotViewModel { get; }
        public EicViewModel EicViewModel { get; }
        public RawDecSpectrumsViewModel RawDecSpectrumsViewModel { get; }
        public RawPurifiedSpectrumsViewModel RawPurifiedSpectrumsViewModel { get; }

        public Ms2ChromatogramsViewModel Ms2ChromatogramsViewModel => throw new NotImplementedException();

        public EiChromatogramsViewModel EiChromatogramsViewModel { get; }
        public SurveyScanViewModel SurveyScanViewModel { get; }
        public FocusNavigatorViewModel FocusNavigatorViewModel { get; }
        public ViewModelBase[] PeakDetailViewModels { get; }

        public ReactiveCommand GoToMsfinderCommand { get; }
        public ReactiveCommand ShowMsfinderSettingCommand { get; }

        public ICommand ShowIonTableCommand => _showIonTableCommand ??= new DelegateCommand(ShowIonTable);
        private DelegateCommand? _showIonTableCommand;

        private void ShowIonTable() {
            _peakSpotTableService.Show(PeakTableViewModel);
        }

        public ICommand SearchCompoundCommand => _searchCompoundCommand ??= new DelegateCommand(ShowSearchCompound);
        private DelegateCommand? _searchCompoundCommand;

        private void ShowSearchCompound() {
            using var csm = _model.CreateCompoundSearchModel();
            if (csm is null) {
                return;
            }
            using var vm = new GcmsAnalysisCompoundSearchViewModel(csm);
            _broker.Publish((ICompoundSearchViewModel)vm);
        }


        public ICommand SetUnknownCommand { get; }

        public UndoManagerViewModel UndoManagerViewModel { get; }
        public GcmsAnalysisPeakTableViewModel PeakTableViewModel { get; }
        public DelegateCommand GoToExternalMsfinderCommand => _goToExternalMsfinderCommand ??= new DelegateCommand(((IResultModel)_model).InvokeMsfinder);
        private DelegateCommand? _goToExternalMsfinderCommand;
    }
}

using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Statistics;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    internal sealed class DimsAlignmentViewModel : ViewModelBase, IAlignmentResultViewModel
    {
        private readonly DimsAlignmentModel _model;
        private readonly IWindowService<PeakSpotTableViewModelBase> _peakSpotTableService;
        private readonly IMessageBroker _broker;
        private readonly InternalStandardSetViewModel _internalStandardSetViewModel;

        public DimsAlignmentViewModel(
            DimsAlignmentModel model,
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

            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);
            SetUnknownCommand = model.CanSetUnknown.ToReactiveCommand().WithSubscribe(model.SetUnknown).AddTo(Disposables);

            var (peakPlotViewFocusAction, peakPlotViewFocused) = focusControlManager.Request();
            PlotViewModel = new AlignmentPeakPlotViewModel(_model.PlotModel, focus: peakPlotViewFocusAction, isFocused: peakPlotViewFocused, broker).AddTo(Disposables);

            var (msSpectrumViewFocusAction, msSpectrumViewFocused) = focusControlManager.Request();
            Ms2SpectrumViewModel = new AlignmentMs2SpectrumViewModel(_model.Ms2SpectrumModel, broker, focusAction: msSpectrumViewFocusAction, isFocused: msSpectrumViewFocused).AddTo(Disposables);
            AlignmentEicViewModel = new AlignmentEicViewModel(_model.AlignmentEicModel).AddTo(Disposables);

            var (barChartViewFocusAction, barChartViewFocused) = focusControlManager.Request();
            BarChartViewModel = new BarChartViewModel(_model.BarChartModel, focusAction: barChartViewFocusAction, isFocused: barChartViewFocused).AddTo(Disposables);
            AlignmentSpotTableViewModel = new DimsAlignmentSpotTableViewModel(_model.AlignmentSpotTableModel, PeakSpotNavigatorViewModel, SetUnknownCommand, UndoManagerViewModel, broker).AddTo(Disposables);

            SearchCompoundCommand = _model.CanSearchCompound
                .ToReactiveCommand()
                .WithSubscribe(SearchCompound)
                .AddTo(Disposables);

            FocusNavigatorViewModel = new FocusNavigatorViewModel(model.FocusNavigatorModel).AddTo(Disposables);

            PeakInformationViewModel = new PeakInformationViewModel(model.PeakInformationModel).AddTo(Disposables);
            CompoundDetailViewModel = new CompoundDetailViewModel(model.CompoundDetailModel).AddTo(Disposables);
            MoleculeStructureViewModel = new MoleculeStructureViewModel(model.MoleculeStructureModel).AddTo(Disposables);
            var matchResultCandidatesViewModel = new MatchResultCandidatesViewModel(model.MatchResultCandidatesModel).AddTo(Disposables);
            PeakDetailViewModels = new ViewModelBase[] { PeakInformationViewModel, CompoundDetailViewModel, MoleculeStructureViewModel, matchResultCandidatesViewModel, };

            _internalStandardSetViewModel = new InternalStandardSetViewModel(model.InternalStandardSetModel).AddTo(Disposables);
            InternalStandardSetCommand = new ReactiveCommand().WithSubscribe(_ => broker.Publish(_internalStandardSetViewModel)).AddTo(Disposables);

            var notification = TaskNotification.Start("Loading alignment results...");
            broker.Publish(notification);
            model.Container.LoadAlginedPeakPropertiesTask.ContinueWith(_ => broker.Publish(TaskNotification.End(notification)));

            GoToMsfinderCommand = model.CanSearchCompound
                .ToReactiveCommand().WithSubscribe(() => {
                    var msfinder = model.CreateSingleSearchMsfinderModel();
                    if (msfinder is not null)
                    {
                        broker.Publish(new InternalMsFinderSingleSpotViewModel(msfinder, broker));
                    }
                }).AddTo(Disposables);

            ShowMsfinderSettingCommand = model.CanSearchCompound.ToReactiveCommand().WithSubscribe(() => {
                var msfinderSetting = model.MsfinderParameterSetting;
                if (msfinderSetting is not null)
                {
                    broker.Publish(new InternalMsfinderSettingViewModel(msfinderSetting, broker));
                }
            }).AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<AnalysisFileBeanModel?> CurrentRepresentativeFile => _model.CurrentRepresentativeFile;
        public UndoManagerViewModel UndoManagerViewModel { get; }

        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }
        public AlignmentPeakPlotViewModel PlotViewModel { get; }
        public AlignmentMs2SpectrumViewModel Ms2SpectrumViewModel { get; }
        public AlignmentEicViewModel AlignmentEicViewModel { get; }
        public BarChartViewModel BarChartViewModel { get; }
        public DimsAlignmentSpotTableViewModel AlignmentSpotTableViewModel { get; }
        public FocusNavigatorViewModel FocusNavigatorViewModel { get; }
        public PeakInformationViewModel PeakInformationViewModel { get; }
        public CompoundDetailViewModel CompoundDetailViewModel { get; }
        public MoleculeStructureViewModel MoleculeStructureViewModel { get; }
        public ViewModelBase[] PeakDetailViewModels { get; }
        public ReactiveCommand GoToMsfinderCommand { get; }
        public ReactiveCommand ShowMsfinderSettingCommand { get; }

        public ICommand SetUnknownCommand { get; }

        public DelegateCommand GoToExternalMsfinderCommand => _goToExternalMsfinderCommand ??= new DelegateCommand(_model.InvokeMsfinder);
        private DelegateCommand? _goToExternalMsfinderCommand;

        public ReactiveCommand SearchCompoundCommand { get; }
        private void SearchCompound() {
            using var model = _model.BuildCompoundSearchModel();
            if (model is null) {
                return;
            }
            using var vm = new DimsCompoundSearchViewModel(model);
            _broker.Publish<ICompoundSearchViewModel>(vm);
        }

        public DelegateCommand SaveMs2SpectrumCommand => _saveMs2SpectrumCommand ??= new DelegateCommand(SaveSpectra, _model.CanSaveSpectra);
        private DelegateCommand? _saveMs2SpectrumCommand;

        private void SaveSpectra()
        {
            var request = new SaveFileNameRequest(_model.SaveSpectra)
            {
                Title = "Save spectra",
                Filter = "NIST format(*.msp)|*.msp|MassBank format(*.txt)|*.txt;|MASCOT format(*.mgf)|*.mgf|MSFINDER format(*.mat)|*.mat;|SIRIUS format(*.ms)|*.ms",
                RestoreDirectory = true,
                AddExtension = true,
            };
            _broker.Publish(request);
        }

        public DelegateCommand CopyMs2SpectrumCommand => _copyMs2SpectrumCommand ??= new DelegateCommand(_model.CopySpectrum, _model.CanSaveSpectra);
        private DelegateCommand? _copyMs2SpectrumCommand;

        public ICommand ShowIonTableCommand => _showIonTableCommand ??= new DelegateCommand(ShowIonTable);
        private DelegateCommand? _showIonTableCommand;

        private void ShowIonTable() {
            _peakSpotTableService.Show(AlignmentSpotTableViewModel);
        }

        public ICommand InternalStandardSetCommand { get; }

        public DelegateCommand NormalizeCommand => _normalizeCommand ??= new DelegateCommand(Normalize);
        private DelegateCommand? _normalizeCommand;

        private void Normalize() {
            var model = _model.NormalizationSetModel;
            using var vm = new NormalizationSetViewModel(model, _internalStandardSetViewModel);
            _broker.Publish(vm);
        }

        public DelegateCommand SearchAlignmentSpectrumByMoleculerNetworkingCommand => _searchAlignmentSpectrumByMoleculerNetworkingCommand ??= new DelegateCommand(SearchAlignmentSpectrumByMoleculerNetworkingMethod);
        private DelegateCommand? _searchAlignmentSpectrumByMoleculerNetworkingCommand;

        private void SearchAlignmentSpectrumByMoleculerNetworkingMethod() {
            _model.InvokeMoleculerNetworkingForTargetSpot();
        }

        // IResultViewModel
        IResultModel IResultViewModel.Model => _model;
    }
}

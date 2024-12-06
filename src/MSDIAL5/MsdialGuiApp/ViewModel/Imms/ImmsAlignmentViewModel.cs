using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imms;
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
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    internal sealed class ImmsAlignmentViewModel : ViewModelBase, IAlignmentResultViewModel
    {
        private readonly ImmsAlignmentModel _model;
        private readonly IWindowService<PeakSpotTableViewModelBase> _peakSpotTableService;
        private readonly IMessageBroker _broker;

        public ImmsAlignmentViewModel(
            ImmsAlignmentModel model,
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
            Target = model.Target.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            SetUnknownCommand = model.CanSetUnknown.ToReactiveCommand().WithSubscribe(model.SetUnknown).AddTo(Disposables);

            Brushes = model.Brushes.AsReadOnly();
            SelectedBrush = model.ToReactivePropertySlimAsSynchronized(m => m.SelectedBrush).AddTo(Disposables);

            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);

            var (focusAction, focused) = focusControlManager.Request();
            PlotViewModel = new AlignmentPeakPlotViewModel(model.PlotModel, focusAction, focused, broker).AddTo(Disposables);

            var (msSpectrumViewFocusAction, msSpectrumViewFocused) = focusControlManager.Request();
            Ms2SpectrumViewModel = new AlignmentMs2SpectrumViewModel(model.Ms2SpectrumModel, broker, focusAction: msSpectrumViewFocusAction, isFocused: msSpectrumViewFocused).AddTo(Disposables);

            var (barChartViewFocusAction, barChartViewFocused) = focusControlManager.Request();
            BarChartViewModel = new BarChartViewModel(model.BarChartModel, barChartViewFocusAction, barChartViewFocused).AddTo(Disposables);
            AlignmentEicViewModel = new AlignmentEicViewModel(model.AlignmentEicModel).AddTo(Disposables);
            AlignmentSpotTableViewModel = new ImmsAlignmentSpotTableViewModel(model.AlignmentSpotTableModel, PeakSpotNavigatorViewModel, SetUnknownCommand, UndoManagerViewModel, broker).AddTo(Disposables);

            SearchCompoundCommand = model.CanSearchCompound
                .ToReactiveCommand()
                .WithSubscribe(SearchCompound)
                .AddTo(Disposables);

            PeakInformationViewModel = new PeakInformationViewModel(model.PeakInformationModel).AddTo(Disposables);
            CompoundDetailViewModel = new CompoundDetailViewModel(model.CompoundDetailModel).AddTo(Disposables);
            MoleculeStructureViewModel = new MoleculeStructureViewModel(model.MoleculeStructureModel).AddTo(Disposables);
            var matchResultCandidatesViewModel = new MatchResultCandidatesViewModel(model.MatchResultCandidatesModel).AddTo(Disposables);
            PeakDetailViewModels = new ViewModelBase[] { PeakInformationViewModel, CompoundDetailViewModel, MoleculeStructureViewModel, matchResultCandidatesViewModel, };

            var internalStandardSetViewModel = new InternalStandardSetViewModel(model.InternalStandardSetModel).AddTo(Disposables);
            InternalStandardSetCommand = new ReactiveCommand().WithSubscribe(_ => broker.Publish(internalStandardSetViewModel)).AddTo(Disposables);

            var notification = TaskNotification.Start("Loading alignment results...");
            broker.Publish(notification);
            model.Container.LoadAlginedPeakPropertiesTask.ContinueWith(_ => broker.Publish(TaskNotification.End(notification)));

            NormalizationSetViewModel = new NormalizationSetViewModel(model.NormalizationSetModel, internalStandardSetViewModel).AddTo(Disposables);
            ShowNormalizationSettingCommand = new ReactiveCommand()
                .WithSubscribe(() => broker.Publish(NormalizationSetViewModel))
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
        }

        public ReadOnlyReactivePropertySlim<AnalysisFileBeanModel?> CurrentRepresentativeFile => _model.CurrentRepresentativeFile;

        public AlignmentPeakPlotViewModel PlotViewModel { get; }
        public AlignmentMs2SpectrumViewModel Ms2SpectrumViewModel { get; }
        public BarChartViewModel BarChartViewModel { get; }
        public AlignmentEicViewModel AlignmentEicViewModel { get; }
        public ImmsAlignmentSpotTableViewModel AlignmentSpotTableViewModel { get; }

        public UndoManagerViewModel UndoManagerViewModel { get; }

        public ReadOnlyReactivePropertySlim<AlignmentSpotPropertyModel?> Target { get; }

        public ReactivePropertySlim<BrushMapData<AlignmentSpotPropertyModel>?> SelectedBrush { get; }

        public ReadOnlyCollection<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }

        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }
        public PeakInformationViewModel PeakInformationViewModel { get; }
        public CompoundDetailViewModel CompoundDetailViewModel { get; }
        public MoleculeStructureViewModel MoleculeStructureViewModel { get; }
        public ViewModelBase[] PeakDetailViewModels { get; }

        public ICommand SetUnknownCommand { get; }
        public ReactiveCommand GoToMsfinderCommand { get; }
        public ReactiveCommand ShowMsfinderSettingCommand { get; }

        public DelegateCommand GoToExternalMsfinderCommand => _goToExternalMsfinderCommand ??= new DelegateCommand(_model.InvokeMsfinder);
        private DelegateCommand? _goToExternalMsfinderCommand;

        public ReactiveCommand SearchCompoundCommand { get; }
        private void SearchCompound() {
            using var csm = _model.CreateCompoundSearchModel();
            if (csm is null) {
                return;
            }
            using var vm = new ImmsCompoundSearchVM(csm);
            _broker.Publish<ICompoundSearchViewModel>(vm);
        }

        public ICommand InternalStandardSetCommand { get; }

        public NormalizationSetViewModel NormalizationSetViewModel { get; }
        public ReactiveCommand ShowNormalizationSettingCommand { get; }

        public ICommand ShowIonTableCommand => _showIonTableCommand ??= new DelegateCommand(ShowIonTable);
        private DelegateCommand? _showIonTableCommand;

        private void ShowIonTable() {
            _peakSpotTableService.Show(AlignmentSpotTableViewModel);
        }

        public DelegateCommand SearchAlignmentSpectrumByMoleculerNetworkingCommand => _searchAlignmentSpectrumByMoleculerNetworkingCommand ??= new DelegateCommand(SearchAlignmentSpectrumByMoleculerNetworkingMethod);
        private DelegateCommand? _searchAlignmentSpectrumByMoleculerNetworkingCommand;

        private void SearchAlignmentSpectrumByMoleculerNetworkingMethod() {
            _model.InvokeMoleculerNetworkingForTargetSpot();
        }

        public ICommand SaveSpectraCommand => _saveSpectraCommand ??= new DelegateCommand(SaveSpectra, CanSaveSpectra);

        private DelegateCommand? _saveSpectraCommand;

        private void SaveSpectra() {
            var request = new SaveFileNameRequest(_model.SaveSpectra)
            {
                Title = "Save spectra",
                Filter = "NIST format(*.msp)|*.msp|MassBank format(*.txt)|*.txt;|MASCOT format(*.mgf)|*.mgf|MSFINDER format(*.mat)|*.mat;|SIRIUS format(*.ms)|*.ms",
                RestoreDirectory = true,
                AddExtension = true,
            };
            _broker.Publish(request);
        }

        private bool CanSaveSpectra() {
            return _model.CanSaveSpectra();
        }

        public void SaveProject() {
            _model.SaveProject();
        }

        // IResultViewModel
        IResultModel IResultViewModel.Model => _model;
    }
}

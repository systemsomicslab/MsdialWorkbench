using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Statistics;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    internal sealed class LcmsAlignmentViewModel : ViewModelBase, IAlignmentResultViewModel
    {
        private readonly LcmsAlignmentModel _model;
        private readonly IMessageBroker _broker;

        public LcmsAlignmentViewModel(
            LcmsAlignmentModel model,
            IMessageBroker broker,
            FocusControlManager focusControlManager) {
            if (focusControlManager is null) {
                throw new ArgumentNullException(nameof(focusControlManager));
            }

            _model = model ?? throw new ArgumentNullException(nameof(model));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));

            Target = _model.Target.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);
            UndoManagerViewModel = new UndoManagerViewModel(model.UndoManager).AddTo(Disposables);

            Ms1Spots = CollectionViewSource.GetDefaultView(_model.Ms1Spots);

            var (peakPlotAction, peakPlotFocused) = focusControlManager.Request();
            PlotViewModel = new AlignmentPeakPlotViewModel(_model.PlotModel, peakPlotAction, peakPlotFocused, broker).AddTo(Disposables);

            var (msSpectrumViewFocusAction, msSpectrumViewFocused) = focusControlManager.Request();
            Ms2SpectrumViewModel = new AlignmentMs2SpectrumViewModel(model.Ms2SpectrumModel, broker, focusAction: msSpectrumViewFocusAction, isFocused: msSpectrumViewFocused).AddTo(Disposables);

            var (barChartViewFocusAction, barChartViewFocused) = focusControlManager.Request();
            BarChartViewModel = new BarChartViewModel(_model.BarChartModel, barChartViewFocusAction, barChartViewFocused).AddTo(Disposables);
            AlignmentEicViewModel = new AlignmentEicViewModel(_model.AlignmentEicModel).AddTo(Disposables);

            SetUnknownCommand = model.CanSetUnknown.ToReactiveCommand().WithSubscribe(model.SetUnknown).AddTo(Disposables);
            
            AlignmentSpotTableViewModel = LcmsTableViewModelHelper.CreateViewModel(_model.AlignmentSpotTableModel, PeakSpotNavigatorViewModel, SetUnknownCommand, UndoManagerViewModel, broker).AddTo(Disposables);

            SearchCompoundCommand = _model.CanSearchCompound
                .ToReactiveCommand()
                .WithSubscribe(SearchCompound)
                .AddTo(Disposables);

            FocusNavigatorViewModel = new FocusNavigatorViewModel(model.FocusNavigatorModel).AddTo(Disposables);
            
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

            var internalStandardSetViewModel = new InternalStandardSetViewModel(model.InternalStandardSetModel).AddTo(Disposables);
            InternalStandardSetCommand = new ReactiveCommand().WithSubscribe(_ => broker.Publish(internalStandardSetViewModel)).AddTo(Disposables);

            NormalizationSetViewModel = new NormalizationSetViewModel(model.NormalizationSetModel, internalStandardSetViewModel).AddTo(Disposables);
            ShowNormalizationSettingCommand = new ReactiveCommand()
                .WithSubscribe(() => broker.Publish(NormalizationSetViewModel))
                .AddTo(Disposables);

            MultivariateAnalysisSettingViewModel = new MultivariateAnalysisSettingViewModel(model.MultivariateAnalysisSettingModel, broker).AddTo(Disposables);
            ShowMultivariateAnalysisSettingCommand = model.NormalizationSetModel.IsNormalized
                .ToReactiveCommand<MultivariateAnalysisOption>()
                .WithSubscribe(option =>
                {
                    MultivariateAnalysisSettingViewModel.MultivariateAnalysisOption.Value = option;
                    broker.Publish(MultivariateAnalysisSettingViewModel);
                })
                .AddTo(Disposables);

            var notification = TaskNotification.Start("Loading alignment results...");
            broker.Publish(notification);
            model.Container.LoadAlginedPeakPropertiesTask.ContinueWith(_ => broker.Publish(TaskNotification.End(notification)));

            var findTargetCompoundsSpotViewModel = new FindTargetCompoundsSpotViewModel(model.FindTargetCompoundSpotModel, broker).AddTo(Disposables);
            ShowFindCompoundSpotViewCommand = new ReactiveCommand().WithSubscribe(() => _broker.Publish(findTargetCompoundsSpotViewModel)).AddTo(Disposables);

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

        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }
        public UndoManagerViewModel UndoManagerViewModel { get; }
        public ICollectionView Ms1Spots { get; }
        public ICollectionView PeakSpotsView => Ms1Spots;

        public ReadOnlyReactivePropertySlim<AlignmentSpotPropertyModel?> Target { get; }
        public ReadOnlyReactivePropertySlim<AnalysisFileBeanModel?> CurrentRepresentativeFile => _model.CurrentRepresentativeFile;

        public AlignmentPeakPlotViewModel PlotViewModel { get; }
        public AlignmentMs2SpectrumViewModel Ms2SpectrumViewModel { get; }
        public BarChartViewModel BarChartViewModel { get; }
        public AlignmentEicViewModel AlignmentEicViewModel { get; }
        public AlignmentSpotTableViewModelBase AlignmentSpotTableViewModel { get; }
        public FocusNavigatorViewModel FocusNavigatorViewModel { get; }
        public PeakInformationViewModel PeakInformationViewModel { get; }
        public CompoundDetailViewModel CompoundDetailViewModel { get; }
        public MoleculeStructureViewModel? MoleculeStructureViewModel { get; }
        public ViewModelBase[] PeakDetailViewModels { get; }
        public IObservable<ProteinResultContainerModel?> ProteinResultContainerAsObservable { get; }
        public ICommand InternalStandardSetCommand { get; }
        public NormalizationSetViewModel NormalizationSetViewModel { get; }
        public ReactiveCommand ShowNormalizationSettingCommand { get; }

        public MultivariateAnalysisSettingViewModel MultivariateAnalysisSettingViewModel { get; }
        public ReactiveCommand<MultivariateAnalysisOption> ShowMultivariateAnalysisSettingCommand { get; }

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

        public DelegateCommand SearchAlignmentSpectrumByMoleculerNetworkingCommand => _searchAlignmentSpectrumByMoleculerNetworkingCommand ??= new DelegateCommand(SearchAlignmentSpectrumByMoleculerNetworkingMethod);
        private DelegateCommand? _searchAlignmentSpectrumByMoleculerNetworkingCommand;

        private void SearchAlignmentSpectrumByMoleculerNetworkingMethod() {
            _model.InvokeMoleculerNetworkingForTargetSpot();
        }

        public ReactiveCommand GoToMsfinderCommand {  get; }
        public ReactiveCommand ShowMsfinderSettingCommand { get; }

        public DelegateCommand GoToExternalMsfinderCommand => _goToExternalMsfinderCommand ??= new DelegateCommand(_model.InvokeMsfinder);
        private DelegateCommand? _goToExternalMsfinderCommand;

        public ICommand ShowIonTableCommand => _showIonTableCommand ??= new DelegateCommand(ShowIonTable);
        private DelegateCommand? _showIonTableCommand;

        private void ShowIonTable() {
            _broker.Publish(AlignmentSpotTableViewModel);
        }

        public ReactiveCommand ShowFindCompoundSpotViewCommand { get; }

        public DelegateCommand SaveSpectraCommand => _saveSpectraCommand ??= new DelegateCommand(SaveSpectra, _model.CanSaveSpectra);
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

        // IResultViewModel
        IResultModel IResultViewModel.Model => _model;
    }
}

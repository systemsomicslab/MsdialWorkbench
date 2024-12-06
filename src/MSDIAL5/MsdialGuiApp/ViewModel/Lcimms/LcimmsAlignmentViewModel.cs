using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Lcimms;
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
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    internal sealed class LcimmsAlignmentViewModel : ViewModelBase, IAlignmentResultViewModel
    {
        private readonly LcimmsAlignmentModel _model;
        private readonly IWindowService<PeakSpotTableViewModelBase> _peakSpotTableService;

        public LcimmsAlignmentViewModel(
            LcimmsAlignmentModel model,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            FocusControlManager focusControlManager,
            IMessageBroker broker) {
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            if (focusControlManager is null) {
                throw new ArgumentNullException(nameof(focusControlManager));
            }

            _model = model;
            _peakSpotTableService = peakSpotTableService;

            UndoManagerViewModel = new UndoManagerViewModel(model.UndoManager).AddTo(Disposables);
            SetUnknownCommand = model.CanSetUnknown.ToReactiveCommand().WithSubscribe(model.SetUnknown).AddTo(Disposables);

            Target = _model.Target.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            Brushes = _model.Brushes.AsReadOnly();
            SelectedBrush = _model.ToReactivePropertySlimAsSynchronized(m => m.SelectedBrush).AddTo(Disposables);

            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);

            Ms1Spots = CollectionViewSource.GetDefaultView(_model.Ms1Spots);

            var (peakPlotFocusAction, peakPlotFocused) = focusControlManager.Request();
            RtMzPlotViewModel = new AlignmentPeakPlotViewModel(model.RtMzPlotModel, peakPlotFocusAction, peakPlotFocused, broker).AddTo(Disposables);
            DtMzPlotViewModel = new AlignmentPeakPlotViewModel(model.DtMzPlotModel, peakPlotFocusAction, peakPlotFocused, broker).AddTo(Disposables);

            var (msSpectrumViewFocusAction, msSpectrumViewFocused) = focusControlManager.Request();
            Ms2SpectrumViewModel = new AlignmentMs2SpectrumViewModel(model.Ms2SpectrumModel, broker, focusAction: msSpectrumViewFocusAction, isFocused: msSpectrumViewFocused).AddTo(Disposables);

            var (barChartViewFocusAction, barChartViewFocused) = focusControlManager.Request();
            RtBarChartViewModel = new BarChartViewModel(model.RtBarChartModel, barChartViewFocusAction, barChartViewFocused).AddTo(Disposables);
            DtBarChartViewModel = new BarChartViewModel(model.DtBarChartModel, barChartViewFocusAction, barChartViewFocused).AddTo(Disposables);
            BarChartViewModels = new MultiBarChartViewModel(RtBarChartViewModel, DtBarChartViewModel).AddTo(Disposables);

            RtAlignmentEicViewModel = new AlignmentEicViewModel(model.RtAlignmentEicModel).AddTo(Disposables);
            DtAlignmentEicViewModel = new AlignmentEicViewModel(model.DtAlignmentEicModel).AddTo(Disposables);
            AlignmentEicViewModels = new MultiAlignmentEicViewModel(RtAlignmentEicViewModel, DtAlignmentEicViewModel).AddTo(Disposables);
            
            AlignmentSpotTableViewModel = new LcimmsAlignmentSpotTableViewModel(
                model.AlignmentSpotTableModel,
                PeakSpotNavigatorViewModel,
                SetUnknownCommand,
                UndoManagerViewModel, broker)
                .AddTo(Disposables);

            SearchCompoundCommand = new[]{
                model.Target.Select(t => t?.innerModel is null),
                model.MsdecResult.Select(r => r is null),
                model.CompoundSearchModel.Select(m => m is null),
            }.CombineLatestValuesAreAllFalse()
            .ToReactiveCommand()
            .WithSubscribe(() => {
                using var vm = new LcimmsCompoundSearchViewModel(model.CompoundSearchModel.Value);
                broker.Publish<ICompoundSearchViewModel>(vm);
            })
            .AddTo(Disposables);

            FocusNavigatorViewModel = new FocusNavigatorViewModel(model.FocusNavigatorModel).AddTo(Disposables);

            PeakInformationViewModel = new PeakInformationViewModel(model.PeakInformationModel).AddTo(Disposables);
            CompoundDetailViewModel = new CompoundDetailViewModel(model.CompoundDetailModel).AddTo(Disposables);
            MoleculeStructureViewModel = new MoleculeStructureViewModel(model.MoleculeStructureModel).AddTo(Disposables);
            var matchResultCandidatesViewModel = new MatchResultCandidatesViewModel(model.MatchResultCandidatesModel).AddTo(Disposables);
            PeakDetailViewModels = new ViewModelBase[] { PeakInformationViewModel, CompoundDetailViewModel, MoleculeStructureViewModel, matchResultCandidatesViewModel, };

            var internalStandardSetViewModel = new InternalStandardSetViewModel(model.InternalStandardSetModel).AddTo(Disposables);
            InternalStandardSetCommand = new ReactiveCommand().WithSubscribe(() => broker.Publish(internalStandardSetViewModel)).AddTo(Disposables);

            NormalizationSetViewModel = new NormalizationSetViewModel(model.NormalizationSetModel, internalStandardSetViewModel).AddTo(Disposables);
            ShowNormalizationSettingCommand = new ReactiveCommand()
                .WithSubscribe(() => broker.Publish(NormalizationSetViewModel))
                .AddTo(Disposables);

            var notification = TaskNotification.Start("Loading alignment results...");
            broker.Publish(notification);
            model.Container.LoadAlginedPeakPropertiesTask.ContinueWith(_ => broker.Publish(TaskNotification.End(notification)));

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

        public ReadOnlyReactivePropertySlim<AnalysisFileBeanModel?> CurrentRepresentativeFile => _model.CurrentRepresentativeFile;
        public UndoManagerViewModel UndoManagerViewModel { get; }
        public AlignmentPeakPlotViewModel RtMzPlotViewModel { get; }
        public AlignmentPeakPlotViewModel DtMzPlotViewModel { get; }
        public AlignmentMs2SpectrumViewModel Ms2SpectrumViewModel { get; }
        public BarChartViewModel RtBarChartViewModel { get; }
        public BarChartViewModel DtBarChartViewModel { get; }
        public MultiBarChartViewModel BarChartViewModels { get; }

        BarChartViewModel IAlignmentResultViewModel.BarChartViewModel => DtBarChartViewModel;
        public AlignmentEicViewModel RtAlignmentEicViewModel { get; }
        public AlignmentEicViewModel DtAlignmentEicViewModel { get; }
        public MultiAlignmentEicViewModel AlignmentEicViewModels { get; }
        public LcimmsAlignmentSpotTableViewModel AlignmentSpotTableViewModel { get; }

        public ICollectionView Ms1Spots { get; }

        public ReadOnlyReactivePropertySlim<AlignmentSpotPropertyModel?> Target { get; }

        public ReactivePropertySlim<BrushMapData<AlignmentSpotPropertyModel>?> SelectedBrush { get; }
        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }
        public ReadOnlyCollection<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }

        public FocusNavigatorViewModel FocusNavigatorViewModel { get; }
        public PeakInformationViewModel PeakInformationViewModel { get; }
        public CompoundDetailViewModel CompoundDetailViewModel { get; }
        public MoleculeStructureViewModel MoleculeStructureViewModel { get; }
        public ViewModelBase[] PeakDetailViewModels { get; }

        public ICommand InternalStandardSetCommand { get; }
        public NormalizationSetViewModel NormalizationSetViewModel { get; }
        public ReactiveCommand ShowNormalizationSettingCommand { get; }
        public ICommand SetUnknownCommand { get; }
        public ReactiveCommand SearchCompoundCommand { get; }

        public ICommand ShowIonTableCommand => showIonTableCommand ??= new DelegateCommand(ShowIonTable);

        private DelegateCommand? showIonTableCommand;

        public DelegateCommand SearchAlignmentSpectrumByMoleculerNetworkingCommand => _searchAlignmentSpectrumByMoleculerNetworkingCommand ??= new DelegateCommand(SearchAlignmentSpectrumByMoleculerNetworkingMethod);
        private DelegateCommand? _searchAlignmentSpectrumByMoleculerNetworkingCommand;

        private void SearchAlignmentSpectrumByMoleculerNetworkingMethod() {
            _model.InvokeMoleculerNetworkingForTargetSpot();
        }

        public ReactiveCommand GoToMsfinderCommand { get; }
        public ReactiveCommand ShowMsfinderSettingCommand { get; }

        public DelegateCommand GoToExternalMsfinderCommand => _goToExternalMsfinderCommand ??= new DelegateCommand(_model.InvokeMsfinder);
        private DelegateCommand? _goToExternalMsfinderCommand;

        private void ShowIonTable() {
            _peakSpotTableService.Show(AlignmentSpotTableViewModel);
        }

        public void SaveProject() {
            _model.SaveProject();
        }

        // IResultViewModel
        IResultModel IResultViewModel.Model => _model;
    }
}
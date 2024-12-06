using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Gcms;
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
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Gcms
{
    internal sealed class GcmsAlignmentViewModel : ViewModelBase, IAlignmentResultViewModel
    {
        private readonly GcmsAlignmentModel _model;
        private readonly IMessageBroker _broker;

        public GcmsAlignmentViewModel(GcmsAlignmentModel model, FocusControlManager focusControlManager, IMessageBroker broker) {
            if (focusControlManager is null) {
                throw new ArgumentNullException(nameof(focusControlManager));
            }

            _model = model;
            _broker = broker;
            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);
            UndoManagerViewModel = new UndoManagerViewModel(model.UndoManager).AddTo(Disposables);

            var (peakPlotAction, peakPlotFocused) = focusControlManager.Request();
            PlotViewModel = new AlignmentPeakPlotViewModel(model.PlotModel, peakPlotAction, peakPlotFocused, broker).AddTo(Disposables);
            GcgcPlotViewModel = new GcgcAlignmentPeakPlotViewModel(model.GcgcPlotModel, peakPlotAction, peakPlotFocused, broker).AddTo(Disposables);

            var (msSpectrumViewFocusAction, msSpectrumViewFocused) = focusControlManager.Request();
            Ms2SpectrumViewModel = new AlignmentMs2SpectrumViewModel(model.MsSpectrumModel, broker, focusAction: msSpectrumViewFocusAction, isFocused: msSpectrumViewFocused).AddTo(Disposables);

            var (barChartAction, barChartFocused) = focusControlManager.Request();
            BarChartViewModel = new BarChartViewModel(model.BarChartModel, barChartAction, barChartFocused).AddTo(Disposables);

            AlignmentEicViewModel = new AlignmentEicViewModel(model.AlignmentEicModel).AddTo(Disposables);

            SetUnknownCommand = model.CanSetUnknown.ToReactiveCommand().WithSubscribe(model.SetUnknown).AddTo(Disposables);

            AlignmentSpotTableViewModel = new GcmsAlignmentSpotTableViewModel(model.AlignmentSpotTableModel, PeakSpotNavigatorViewModel, SetUnknownCommand, UndoManagerViewModel, broker).AddTo(Disposables);

            ShowIonTableCommand = new ReactiveCommand().WithSubscribe(() => broker.Publish<AlignmentSpotTableViewModelBase>(AlignmentSpotTableViewModel)).AddTo(Disposables);

            var peakInformationViewModel = new PeakInformationViewModel(model.PeakInformationModel).AddTo(Disposables);
            var compoundDetailViewModel = new CompoundDetailViewModel(model.CompoundDetailModel).AddTo(Disposables);
            var moleculeStructureViewModel = new MoleculeStructureViewModel(model.MoleculeStructureModel).AddTo(Disposables);
            var matchResultCandidatesViewModel = new MatchResultCandidatesViewModel(model.MatchResultCandidatesModel).AddTo(Disposables);
            PeakDetailViewModels = new ViewModelBase[] { peakInformationViewModel, compoundDetailViewModel, moleculeStructureViewModel, matchResultCandidatesViewModel, };

            FocusNavigatorViewModel = new FocusNavigatorViewModel(model.FocusNavigatorModel).AddTo(Disposables);

            var internalStandardSetViewModel = new InternalStandardSetViewModel(model.InternalStandardSetModel).AddTo(Disposables);
            InternalStandardSetCommand = new ReactiveCommand().WithSubscribe(() => broker.Publish(internalStandardSetViewModel)).AddTo(Disposables);

            NormalizationSetViewModel = new NormalizationSetViewModel(model.NormalizationSetModel, internalStandardSetViewModel).AddTo(Disposables);
            ShowNormalizationSettingCommand = new ReactiveCommand().WithSubscribe(() => broker.Publish(NormalizationSetViewModel)).AddTo(Disposables);

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

            GoToMsfinderCommand = new ReactiveCommand().WithSubscribe(() => {
                var msfinder = model.CreateSingleSearchMsfinderModel();
                if (msfinder is not null)
                {
                    broker.Publish(new InternalMsFinderSingleSpotViewModel(msfinder, broker));
                }
            }).AddTo(Disposables);

            ShowMsfinderSettingCommand = new ReactiveCommand().WithSubscribe(() => {
                var msfinderSetting = model.MsfinderParameterSetting;
                if (msfinderSetting is not null)
                {
                    broker.Publish(new InternalMsfinderSettingViewModel(msfinderSetting, broker));
                }
            }).AddTo(Disposables);
        }

        public BarChartViewModel BarChartViewModel { get; }

        public AlignmentEicViewModel AlignmentEicViewModel { get; }
        public GcmsAlignmentSpotTableViewModel AlignmentSpotTableViewModel { get; }

        public ICommand InternalStandardSetCommand { get; }
        public NormalizationSetViewModel NormalizationSetViewModel { get; }
        public ReactiveCommand ShowNormalizationSettingCommand { get; }

        public IResultModel Model => _model;
        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }
        public ViewModelBase[] PeakDetailViewModels { get; }
        public FocusNavigatorViewModel FocusNavigatorViewModel { get; }
        public ICommand ShowIonTableCommand { get; }

        public ICommand SetUnknownCommand { get; }

        public UndoManagerViewModel UndoManagerViewModel { get; }

        public AlignmentPeakPlotViewModel PlotViewModel { get; }
        public GcgcAlignmentPeakPlotViewModel GcgcPlotViewModel { get; }
        public AlignmentMs2SpectrumViewModel Ms2SpectrumViewModel { get; }

        public MultivariateAnalysisSettingViewModel MultivariateAnalysisSettingViewModel { get; }
        public ReactiveCommand<MultivariateAnalysisOption> ShowMultivariateAnalysisSettingCommand { get; }

        public ICommand SearchCompoundCommand => _searchCompoundCommand ??= new DelegateCommand(SearchCompound);
        private DelegateCommand? _searchCompoundCommand;

        private void SearchCompound() {
            using var csm = _model.CreateCompoundSearchModel();
            if (csm is null) {
                return;
            }
            using var vm = new GcmsAlignmentCompoundSearchViewModel(csm);
            _broker?.Publish((ICompoundSearchViewModel)vm);
        }

        public ReactiveCommand GoToMsfinderCommand { get; }
        public ReactiveCommand ShowMsfinderSettingCommand { get; }

        public DelegateCommand GoToExternalMsfinderCommand => _goToExternalMsfinderCommand ??= new DelegateCommand(Model.InvokeMsfinder);
        private DelegateCommand? _goToExternalMsfinderCommand = null;
    }
}

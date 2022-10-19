using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Statistics;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    internal sealed class ImmsAlignmentViewModel : ViewModelBase, IAlignmentResultViewModel
    {
        private readonly ImmsAlignmentModel _model;
        private readonly IWindowService<CompoundSearchVM> _compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> _peakSpotTableService;
        private readonly IMessageBroker _messageBroker;

        public ImmsAlignmentViewModel(
            ImmsAlignmentModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IMessageBroker messageBroker,
            FocusControlManager focusControlManager) {
            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            if (focusControlManager is null) {
                throw new ArgumentNullException(nameof(focusControlManager));
            }

            _model = model;
            _compoundSearchService = compoundSearchService;
            _peakSpotTableService = peakSpotTableService;
            _messageBroker = messageBroker;
            Target = model.Target.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            Brushes = model.Brushes.AsReadOnly();
            SelectedBrush = model.ToReactivePropertySlimAsSynchronized(m => m.SelectedBrush).AddTo(Disposables);

            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);

            var (focusAction, focused) = focusControlManager.Request();
            PlotViewModel = new Chart.AlignmentPeakPlotViewModel(model.PlotModel, focusAction, focused).AddTo(Disposables);

            Ms2SpectrumViewModel = new Chart.MsSpectrumViewModel(model.Ms2SpectrumModel).AddTo(Disposables);

            var (barChartViewFocusAction, barChartViewFocused) = focusControlManager.Request();
            BarChartViewModel = new Chart.BarChartViewModel(model.BarChartModel, barChartViewFocusAction, barChartViewFocused).AddTo(Disposables);
            AlignmentEicViewModel = new Chart.AlignmentEicViewModel(model.AlignmentEicModel).AddTo(Disposables);
            AlignmentSpotTableViewModel = new ImmsAlignmentSpotTableViewModel(
                model.AlignmentSpotTableModel,
                PeakSpotNavigatorViewModel.MzLowerValue, PeakSpotNavigatorViewModel.MzUpperValue,
                PeakSpotNavigatorViewModel.DtLowerValue, PeakSpotNavigatorViewModel.DtUpperValue,
                PeakSpotNavigatorViewModel.MetaboliteFilterKeyword, PeakSpotNavigatorViewModel.CommentFilterKeyword,
                PeakSpotNavigatorViewModel.OntologyFilterKeyword, PeakSpotNavigatorViewModel.AdductFilterKeyword,
                PeakSpotNavigatorViewModel.IsEditting)
                .AddTo(Disposables);

            SearchCompoundCommand = model.CanSearchCompound
                .ToReactiveCommand()
                .WithSubscribe(SearchCompound)
                .AddTo(Disposables);

            PeakInformationViewModel = new PeakInformationViewModel(model.PeakInformationModel).AddTo(Disposables);
            CompoundDetailViewModel = new CompoundDetailViewModel(model.CompoundDetailModel).AddTo(Disposables);
            PeakDetailViewModels = new ViewModelBase[] { PeakInformationViewModel, CompoundDetailViewModel, };
            SetUnknownCommand = Target.Select(t => !(t is null)).ToReactiveCommand()
                .WithSubscribe(() => Target.Value.SetUnknown())
                .AddTo(Disposables);

            var internalStandardSetViewModel = new InternalStandardSetViewModel(model.InternalStandardSetModel).AddTo(Disposables);
            InternalStandardSetCommand = new ReactiveCommand().WithSubscribe(_ => messageBroker.Publish(internalStandardSetViewModel)).AddTo(Disposables);

            var notification = TaskNotification.Start("Loading alignment results...");
            messageBroker.Publish(notification);
            model.Container.LoadAlginedPeakPropertiesTask.ContinueWith(_ => messageBroker.Publish(TaskNotification.End(notification)));
        }

        public Chart.AlignmentPeakPlotViewModel PlotViewModel {
            get => _plotViewModel;
            set => SetProperty(ref _plotViewModel, value);
        }
        private Chart.AlignmentPeakPlotViewModel _plotViewModel;

        public Chart.MsSpectrumViewModel Ms2SpectrumViewModel {
            get => _ms2SpectrumViewModel;
            set => SetProperty(ref _ms2SpectrumViewModel, value);
        }
        private Chart.MsSpectrumViewModel _ms2SpectrumViewModel;

        public Chart.BarChartViewModel BarChartViewModel {
            get => _barChartViewModel;
            set => SetProperty(ref _barChartViewModel, value);
        }
        private Chart.BarChartViewModel _barChartViewModel;

        public Chart.AlignmentEicViewModel AlignmentEicViewModel {
            get => _alignmentEicViewModel;
            set => SetProperty(ref _alignmentEicViewModel, value);
        }
        private Chart.AlignmentEicViewModel _alignmentEicViewModel;

        public ImmsAlignmentSpotTableViewModel AlignmentSpotTableViewModel {
            get => _alignmentSpotTableViewModel;
            set => SetProperty(ref _alignmentSpotTableViewModel, value);
        }
        private ImmsAlignmentSpotTableViewModel _alignmentSpotTableViewModel;

        public ReadOnlyReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }

        public ReactivePropertySlim<BrushMapData<AlignmentSpotPropertyModel>> SelectedBrush { get; }

        public ReadOnlyCollection<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }

        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }
        public PeakInformationViewModel PeakInformationViewModel { get; }
        public CompoundDetailViewModel CompoundDetailViewModel { get; }
        public ViewModelBase[] PeakDetailViewModels { get; }

        public ICommand SetUnknownCommand { get; }
        public ReactiveCommand SearchCompoundCommand { get; }
        private void SearchCompound() {
            using (var csm = _model.CreateCompoundSearchModel()) {
                if (csm is null) {
                    return;
                }
                using (var vm = new ImmsCompoundSearchVM(csm, SetUnknownCommand)) {
                    _compoundSearchService.ShowDialog(vm);
                }
            }
        }

        public ICommand InternalStandardSetCommand { get; }

        public ICommand ShowIonTableCommand => _showIonTableCommand ?? (_showIonTableCommand = new DelegateCommand(ShowIonTable));
        private DelegateCommand _showIonTableCommand;

        private void ShowIonTable() {
            _peakSpotTableService.Show(AlignmentSpotTableViewModel);
        }

        public ICommand SaveSpectraCommand => _saveSpectraCommand ?? (_saveSpectraCommand = new DelegateCommand(SaveSpectra, CanSaveSpectra));

        private DelegateCommand _saveSpectraCommand;

        private void SaveSpectra() {
            var request = new SaveFileNameRequest(_model.SaveSpectra)
            {
                Title = "Save spectra",
                Filter = "NIST format(*.msp)|*.msp|MassBank format(*.txt)|*.txt;|MASCOT format(*.mgf)|*.mgf|MSFINDER format(*.mat)|*.mat;|SIRIUS format(*.ms)|*.ms",
                RestoreDirectory = true,
                AddExtension = true,
            };
            _messageBroker.Publish(request);
        }

        private bool CanSaveSpectra() {
            return _model.CanSaveSpectra();
        }

        public void SaveProject() {
            _model.SaveProject();
        }
    }
}

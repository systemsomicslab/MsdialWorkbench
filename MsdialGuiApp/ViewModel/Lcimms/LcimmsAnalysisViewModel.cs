using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    internal sealed class LcimmsAnalysisViewModel : AnalysisFileViewModel, IAnalysisResultViewModel
    {
        public LcimmsAnalysisViewModel(
            LcimmsAnalysisModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            FocusControlManager focusControlManager)
            : base(model) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }

            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            if (focusControlManager is null) {
                throw new ArgumentNullException(nameof(focusControlManager));
            }

            this.model = model;
            this.peakSpotTableService = peakSpotTableService;

            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);
            PeakFilterViewModel = PeakSpotNavigatorViewModel.PeakFilterViewModel;

            var (rtmzPeakFocusAction, rtmzPeakFocused) = focusControlManager.Request();
            var brush = Observable.Return(this.model.Brush);
            RtMzPlotViewModel = new AnalysisPeakPlotViewModel(this.model.RtMzPlotModel, rtmzPeakFocusAction, rtmzPeakFocused).AddTo(Disposables);
            RtEicViewModel = new EicViewModel(
                this.model.RtEicModel,
                horizontalAxis: RtMzPlotViewModel.HorizontalAxis).AddTo(Disposables);

            var (dtmzPeakFocusAction, dtmzPeakFocused) = focusControlManager.Request();
            DtMzPlotViewModel = new AnalysisPeakPlotViewModel(this.model.DtMzPlotModel, dtmzPeakFocusAction, dtmzPeakFocused).AddTo(Disposables);
            DtEicViewModel = new EicViewModel(
                this.model.DtEicModel,
                horizontalAxis: DtMzPlotViewModel.HorizontalAxis).AddTo(Disposables);

            var (rawDecSpectraViewFocusAction, rawDecSpectraViewFocused) = focusControlManager.Request();
            RawDecSpectrumsViewModel = new RawDecSpectrumsViewModel(this.model.Ms2SpectrumModel, rawDecSpectraViewFocusAction, rawDecSpectraViewFocused).AddTo(Disposables);
            SurveyScanViewModel = new SurveyScanViewModel(
                this.model.SurveyScanModel,
                horizontalAxis: RtMzPlotViewModel.VerticalAxis).AddTo(Disposables);
            // PeakSpotTableViewModelBase = new LcimmsAnalysisViewModel

            SearchCompoundCommand = model.CanSearchCompound
                .ToReactiveCommand()
                .AddTo(Disposables);
            SearchCompoundCommand.WithLatestFrom(model.CompoundSearchModel)
                .Subscribe(p =>
                {
                    using (var vm = new CompoundSearchVM(p.Second)) {
                        compoundSearchService.ShowDialog(vm);
                    }
                }).AddTo(Disposables);

            PeakInformationViewModel = model.PeakInformationModel
                .Where(m => !(m is null))
                .Select(m => new PeakInformationViewModel(m))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            CompoundDetailViewModel = model.CompoundDetailModel
                .Where(m => !(m is null))
                .Select(m => new CompoundDetailViewModel(m))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var _peakDetailViewModels = new ReactiveCollection<ViewModelBase>().AddTo(Disposables);
            new IReadOnlyReactiveProperty<ViewModelBase>[]
            {
                PeakInformationViewModel,
                CompoundDetailViewModel,
            }.CombineLatest()
            .Subscribe(vms =>
            {
                _peakDetailViewModels.ClearOnScheduler();
                _peakDetailViewModels.AddRangeOnScheduler(vms);
            }).AddTo(Disposables);
            PeakDetailViewModels = _peakDetailViewModels.ToReadOnlyReactiveCollection().AddTo(Disposables);
        }

        private readonly LcimmsAnalysisModel model;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;

        public AnalysisPeakPlotViewModel RtMzPlotViewModel { get; private set; }
        public EicViewModel RtEicViewModel { get; private set; }
        public AnalysisPeakPlotViewModel DtMzPlotViewModel { get; private set; }
        public EicViewModel DtEicViewModel { get; private set; }
        public RawDecSpectrumsViewModel RawDecSpectrumsViewModel { get; private set; }
        public SurveyScanViewModel SurveyScanViewModel { get; private set; }
        public PeakFilterViewModel PeakFilterViewModel { get; }
        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }
        public ReadOnlyReactivePropertySlim<PeakInformationViewModel> PeakInformationViewModel { get; }
        public ReadOnlyReactivePropertySlim<CompoundDetailViewModel> CompoundDetailViewModel { get; }
        public ReadOnlyReactiveCollection<ViewModelBase> PeakDetailViewModels { get; }

        public ReactiveCommand SearchCompoundCommand { get; }

        public DelegateCommand ShowIonTableCommand => showIonTableCommand ?? (showIonTableCommand = new DelegateCommand(ShowIonTable));
        private DelegateCommand showIonTableCommand;

        private void ShowIonTable() {
            // peakSpotTableService.Show(PeakTableViewModel);
        }
    }
}
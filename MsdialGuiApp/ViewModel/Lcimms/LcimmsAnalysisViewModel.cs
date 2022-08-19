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
using System.Reactive.Linq;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    internal sealed class LcimmsAnalysisViewModel : ViewModelBase, IAnalysisResultViewModel
    {
        public LcimmsAnalysisViewModel(
            LcimmsAnalysisModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            FocusControlManager focusControlManager) {
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

            PeakInformationViewModel = new PeakInformationViewModel(model.PeakInformationModel).AddTo(Disposables);
            CompoundDetailViewModel = new CompoundDetailViewModel(model.CompoundDetailModel).AddTo(Disposables);
            var _peakDetailViewModels = new ReactiveCollection<ViewModelBase>().AddTo(Disposables);
            PeakDetailViewModels = new ViewModelBase[] { PeakInformationViewModel, CompoundDetailViewModel, };
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
        public PeakInformationViewModel PeakInformationViewModel { get; }
        public CompoundDetailViewModel CompoundDetailViewModel { get; }
        public ViewModelBase[] PeakDetailViewModels { get; }

        public ReactiveCommand SearchCompoundCommand { get; }

        public ICommand ShowIonTableCommand => _showIonTableCommand ?? (_showIonTableCommand = new DelegateCommand(ShowIonTable));
        private DelegateCommand _showIonTableCommand;

        private void ShowIonTable() {
            // peakSpotTableService.Show(PeakTableViewModel);
        }
    }
}
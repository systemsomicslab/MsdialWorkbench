using CompMs.App.Msdial.Model.Core;
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
        private readonly LcimmsAnalysisModel _model;
        private readonly IWindowService<PeakSpotTableViewModelBase> _peakSpotTableService;

        public LcimmsAnalysisViewModel(
            LcimmsAnalysisModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            FocusControlManager focusControlManager) {
            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }

            if (focusControlManager is null) {
                throw new ArgumentNullException(nameof(focusControlManager));
            }

            _model = model ?? throw new ArgumentNullException(nameof(model));
            _peakSpotTableService = peakSpotTableService ?? throw new ArgumentNullException(nameof(peakSpotTableService));

            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);

            var (rtmzPeakFocusAction, rtmzPeakFocused) = focusControlManager.Request();
            var brush = Observable.Return(model.Brush);
            RtMzPlotViewModel = new AnalysisPeakPlotViewModel(model.RtMzPlotModel, rtmzPeakFocusAction, rtmzPeakFocused).AddTo(Disposables);
            RtEicViewModel = new EicViewModel(
                model.RtEicModel,
                horizontalAxis: RtMzPlotViewModel.HorizontalAxis).AddTo(Disposables);

            var (dtmzPeakFocusAction, dtmzPeakFocused) = focusControlManager.Request();
            DtMzPlotViewModel = new AnalysisPeakPlotViewModel(model.DtMzPlotModel, dtmzPeakFocusAction, dtmzPeakFocused).AddTo(Disposables);
            DtEicViewModel = new EicViewModel(
                model.DtEicModel,
                horizontalAxis: DtMzPlotViewModel.HorizontalAxis).AddTo(Disposables);

            var (rawDecSpectraViewFocusAction, rawDecSpectraViewFocused) = focusControlManager.Request();
            RawDecSpectrumsViewModel = new RawDecSpectrumsViewModel(model.Ms2SpectrumModel, rawDecSpectraViewFocusAction, rawDecSpectraViewFocused).AddTo(Disposables);

            var (ms2ChromatogramViewFocusAction, ms2ChromatogramViewFocused) = focusControlManager.Request();
            Ms2ChromatogramsViewModel = new Ms2ChromatogramsViewModel(model.Ms2ChromatogramsModel, ms2ChromatogramViewFocusAction, ms2ChromatogramViewFocused).AddTo(Disposables);

            RawPurifiedSpectrumsViewModel = new RawPurifiedSpectrumsViewModel(model.RawPurifiedSpectrumsModel).AddTo(Disposables);

            SurveyScanViewModel = new SurveyScanViewModel(
                model.SurveyScanModel,
                horizontalAxis: RtMzPlotViewModel.VerticalAxis).AddTo(Disposables);
            PeakTableViewModel = new LcimmsAnalysisPeakTableViewModel(
                model.PeakTableModel,
                Observable.Return(model.EicLoader),
                PeakSpotNavigatorViewModel.MzLowerValue,
                PeakSpotNavigatorViewModel.MzUpperValue,
                PeakSpotNavigatorViewModel.RtLowerValue,
                PeakSpotNavigatorViewModel.RtUpperValue,
                PeakSpotNavigatorViewModel.DtLowerValue,
                PeakSpotNavigatorViewModel.DtUpperValue,
                PeakSpotNavigatorViewModel.MetaboliteFilterKeyword,
                PeakSpotNavigatorViewModel.CommentFilterKeyword,
                PeakSpotNavigatorViewModel.OntologyFilterKeyword,
                PeakSpotNavigatorViewModel.AdductFilterKeyword,
                PeakSpotNavigatorViewModel.IsEditting)
                .AddTo(Disposables);

            SetUnknownCommand = model.Target.Select(t => !(t is null))
                .ToReactiveCommand()
                .WithSubscribe(() => model.Target.Value.SetUnknown())
                .AddTo(Disposables);
            SearchCompoundCommand = model.CanSearchCompound
                .ToReactiveCommand()
                .WithSubscribe(() =>
                {
                    using (var vm = new LcimmsCompoundSearchViewModel(model.CompoundSearchModel.Value, null)) {
                        compoundSearchService.ShowDialog(vm);
                    }
                }).AddTo(Disposables);

            PeakInformationViewModel = new PeakInformationViewModel(model.PeakInformationModel).AddTo(Disposables);
            CompoundDetailViewModel = new CompoundDetailViewModel(model.CompoundDetailModel).AddTo(Disposables);
            var _peakDetailViewModels = new ReactiveCollection<ViewModelBase>().AddTo(Disposables);
            PeakDetailViewModels = new ViewModelBase[] { PeakInformationViewModel, CompoundDetailViewModel, };
        }

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
        public ViewModelBase[] PeakDetailViewModels { get; }

        public ICommand SetUnknownCommand { get; }
        public ReactiveCommand SearchCompoundCommand { get; }

        public ICommand ShowIonTableCommand => _showIonTableCommand ?? (_showIonTableCommand = new DelegateCommand(ShowIonTable));
        private DelegateCommand _showIonTableCommand;

        private void ShowIonTable() {
            _peakSpotTableService.Show(PeakTableViewModel);
        }

        // IResultViewModel
        IResultModel IResultViewModel.Model => _model;
    }
}
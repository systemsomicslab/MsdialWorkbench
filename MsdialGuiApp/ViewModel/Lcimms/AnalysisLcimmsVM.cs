using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    class AnalysisLcimmsVM : AnalysisFileViewModel
    {
        public AnalysisLcimmsVM(
            LcimmsAnalysisModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService)
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

            this.model = model;
            this.compoundSearchService = compoundSearchService;
            this.peakSpotTableService = peakSpotTableService;

            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);
            PeakFilterViewModel = PeakSpotNavigatorViewModel.PeakFilterViewModel;

            var brush = Observable.Return(this.model.Brush);
            RtMzPlotViewModel = new AnalysisPeakPlotViewModel(this.model.RtMzPlotModel, brushSource: brush).AddTo(Disposables);
            RtEicViewModel = new EicViewModel(
                this.model.RtEicModel,
                horizontalAxis: RtMzPlotViewModel.HorizontalAxis).AddTo(Disposables);

            DtMzPlotViewModel = new AnalysisPeakPlotViewModel(this.model.DtMzPlotModel, brushSource: brush).AddTo(Disposables);
            DtEicViewModel = new EicViewModel(
                this.model.DtEicModel,
                horizontalAxis: DtMzPlotViewModel.HorizontalAxis).AddTo(Disposables);

            var upperSpecBrush = new KeyBrushMapper<SpectrumComment, string>(
                this.model.Parameter.ProjectParam.SpectrumCommentToColorBytes
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
                ),
                item => item.ToString(),
                Colors.Blue);

            var lowerSpecBrush = new KeyBrushMapper<SpectrumComment, string>(
               this.model.Parameter.ProjectParam.SpectrumCommentToColorBytes
               .ToDictionary(
                   kvp => kvp.Key,
                   kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
               ),
               item => item.ToString(),
               Colors.Red);

            RawDecSpectrumsViewModel = new RawDecSpectrumsViewModel(this.model.Ms2SpectrumModel,
                upperSpectrumBrushSource: Observable.Return(upperSpecBrush),
                lowerSpectrumBrushSource: Observable.Return(lowerSpecBrush)).AddTo(Disposables);
            SurveyScanViewModel = new SurveyScanViewModel(
                this.model.SurveyScanModel,
                horizontalAxis: RtMzPlotViewModel.VerticalAxis).AddTo(Disposables);
            // PeakSpotTableViewModelBase = new LcimmsAnalysisViewModel

            SearchCompoundCommand = new[]
            {
                Target.Select(t => t?.InnerModel != null),
                model.MsdecResult.Select(r => r != null),
            }.CombineLatestValuesAreAllTrue()
            .ToReactiveCommand()
            .WithSubscribe(SearchCompound)
            .AddTo(Disposables);
        }

        private readonly LcimmsAnalysisModel model;
        private readonly IWindowService<CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;

        public AnalysisPeakPlotViewModel RtMzPlotViewModel { get; private set; }
        public EicViewModel RtEicViewModel { get; private set; }
        public AnalysisPeakPlotViewModel DtMzPlotViewModel { get; private set; }
        public EicViewModel DtEicViewModel { get; private set; }
        public RawDecSpectrumsViewModel RawDecSpectrumsViewModel { get; private set; }
        public SurveyScanViewModel SurveyScanViewModel { get; private set; }
        public PeakFilterViewModel PeakFilterViewModel { get; }
        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }

        public ReactiveCommand SearchCompoundCommand { get; }

        public void SearchCompound() {
            using (var model = new CompoundSearchModel<ChromatogramPeakFeature>(
                this.model.AnalysisFile,
                Target.Value.InnerModel,
                this.model.MsdecResult.Value,
                null,
                null))
            using (var vm = new CompoundSearchVM(model)) {
                compoundSearchService.ShowDialog(vm);
            }
        }

        public DelegateCommand ShowIonTableCommand => showIonTableCommand ?? (showIonTableCommand = new DelegateCommand(ShowIonTable));
        private DelegateCommand showIonTableCommand;

        private void ShowIonTable() {
            // peakSpotTableService.Show(PeakTableViewModel);
        }
    }
}
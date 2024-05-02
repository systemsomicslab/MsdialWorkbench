using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Statistics;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Statistics
{
    internal sealed class ComponentLoadingViewModel : ViewModelBase
    {
        private readonly ComponentLoadingModel _model;

        public ComponentLoadingViewModel(ComponentLoadingModel model, int xIndex, int yIndex, bool isOPLS) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            ComponentX = _model.Loading[xIndex];
            if (isOPLS) {
                ComponentY = _model.OLoading[yIndex];
            }
            else {
                ComponentY = _model.Loading[yIndex];
            }
        }

        public string Label => _model.Label;

        public double ComponentX { get; }
        public double ComponentY { get; }
        public ComponentLoadingModel Model => _model;
    }

    internal sealed class ComponentScoreViewModel : ViewModelBase
    {
        private readonly ComponentScoreModel _model;

        public ComponentScoreViewModel(ComponentScoreModel model, int xIndex, int yIndex, bool isOPLS)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            ComponentX = _model.Score[xIndex];
            if (isOPLS) {
                ComponentY = _model.OScore[yIndex];
            }
            else {
                ComponentY = _model.Score[yIndex];
            }
        }

        public string Label => _model.Label;

        public double ComponentX { get; }
        public double ComponentY { get; }
        public ComponentScoreModel Model => _model;
    }

    internal sealed class LabelTypeViewModel : ViewModelBase {
        public LabelTypeViewModel(string displayName, string propertyName) {
            DisplayName = displayName;
            PropertyName = propertyName;
        }

        public string DisplayName { get; }
        public string PropertyName { get; }

        public override string ToString() {
            return DisplayName;
        }
    }

    internal sealed class PCAPLSResultViewModel : ViewModelBase {
        private readonly PCAPLSResultModel _model;
        private readonly IMessageBroker _broker;

        public PCAPLSResultViewModel(PCAPLSResultModel model, IMessageBroker broker) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            var option = model.MultivariateAnalysisOption;
            var isOpls = option == CompMs.Common.Enum.MultivariateAnalysisOption.Oplsr || option == CompMs.Common.Enum.MultivariateAnalysisOption.Oplsda ? true : false;
            
            if (isOpls) {
                Components = Enumerable.Range(1, model.NumberOfOComponents).ToList().AsReadOnly();
                ComponentX = new ReactiveProperty<int>(1).AddTo(Disposables);
                ComponentY = new ReactiveProperty<int>(1).AddTo(Disposables);
            }
            else {
                Components = Enumerable.Range(1, model.NumberOfComponents).ToList().AsReadOnly();
                ComponentX = new ReactiveProperty<int>(1).AddTo(Disposables);
                ComponentY = new ReactiveProperty<int>(2).AddTo(Disposables);
            }

            Brush = model.SelectedBrush;
            ClassBrush = model.PointBrush;
            PosnegBrush = model.PosnegBrush;

            LabelTypesSample = new List<IReadOnlyReactiveProperty<LabelTypeViewModel>>
            {
                new ReactivePropertySlim<LabelTypeViewModel>(new LabelTypeViewModel("Sample", nameof(ComponentScoreViewModel.Label))).AddTo(Disposables),
                new ReactivePropertySlim<LabelTypeViewModel>(new LabelTypeViewModel("X value", nameof(ComponentScoreViewModel.ComponentX))).AddTo(Disposables),
                new ReactivePropertySlim<LabelTypeViewModel>(new LabelTypeViewModel("Y value", nameof(ComponentScoreViewModel.ComponentY))).AddTo(Disposables),
                new ReactivePropertySlim<LabelTypeViewModel>(new LabelTypeViewModel("None", string.Empty)).AddTo(Disposables),
            }.AsReadOnly();
            LabelTypeSample = new ReactiveProperty<LabelTypeViewModel>(LabelTypesSample.First().Value).AddTo(Disposables);

            LabelTypesMetabolite = new List<IReadOnlyReactiveProperty<LabelTypeViewModel>>
            {
                new ReactivePropertySlim<LabelTypeViewModel>(new LabelTypeViewModel("Metabolite", nameof(ComponentLoadingViewModel.Label))).AddTo(Disposables),
                new ReactivePropertySlim<LabelTypeViewModel>(new LabelTypeViewModel("X value", nameof(ComponentLoadingViewModel.ComponentX))).AddTo(Disposables),
                new ReactivePropertySlim<LabelTypeViewModel>(new LabelTypeViewModel("Y value", nameof(ComponentLoadingViewModel.ComponentY))).AddTo(Disposables),
                new ReactivePropertySlim<LabelTypeViewModel>(new LabelTypeViewModel("None", string.Empty)).AddTo(Disposables),
            }.AsReadOnly();
            LabelTypeMetabolite = new ReactiveProperty<LabelTypeViewModel>(LabelTypesMetabolite.First().Value).AddTo(Disposables);

            SpotSizeSample = new ReactiveProperty<double>(6).AddTo(Disposables);
            SpotSizeMetabolite = new ReactiveProperty<double>(6).AddTo(Disposables);

            Loadings = new ReactiveCollection<ComponentLoadingViewModel>(UIDispatcherScheduler.Default).AddTo(Disposables);
            if (isOpls) {
                Observable.CombineLatest(ComponentX, ComponentY)
                .Subscribe(xy => {
                    Loadings.ClearOnScheduler();
                    Loadings.AddRangeOnScheduler(_model.Loadings.Select(loading => new ComponentLoadingViewModel(loading, xy[0] - 1, xy[1] - 1, true)));
                }).AddTo(Disposables);
                LoadingHorizontalAxis = ComponentX.Select(i => model.LoadingAxises[i - 1].Value).ToReadOnlyReactivePropertySlim<IAxisManager<double>>().AddTo(Disposables);
                LoadingVerticalAxis = ComponentY.Select(i => model.OLoadingAxises![i - 1].Value).ToReadOnlyReactivePropertySlim<IAxisManager<double>>().AddTo(Disposables);

                PC1LoadingAbsoluteVerticalAxis = ComponentX.Select(i => model.LoadingAbsoluteAxises[i - 1].Value).ToReadOnlyReactivePropertySlim<IAxisManager<double>>().AddTo(Disposables);
                PC2LoadingAbsoluteVerticalAxis = ComponentY.Select(i => model.OLoadingAbsoluteAxises![i - 1].Value).ToReadOnlyReactivePropertySlim<IAxisManager<double>>().AddTo(Disposables);
            }
            else {
                Observable.CombineLatest(ComponentX, ComponentY)
                .Subscribe(xy => {
                    Loadings.ClearOnScheduler();
                    Loadings.AddRangeOnScheduler(_model.Loadings.Select(loading => new ComponentLoadingViewModel(loading, xy[0] - 1, xy[1] - 1, false)));
                }).AddTo(Disposables);
                LoadingHorizontalAxis = ComponentX.Select(i => model.LoadingAxises[i - 1].Value).ToReadOnlyReactivePropertySlim<IAxisManager<double>>().AddTo(Disposables);
                LoadingVerticalAxis = ComponentY.Select(i => model.LoadingAxises[i - 1].Value).ToReadOnlyReactivePropertySlim<IAxisManager<double>>().AddTo(Disposables);

                PC1LoadingAbsoluteVerticalAxis = ComponentX.Select(i => model.LoadingAbsoluteAxises[i - 1].Value).ToReadOnlyReactivePropertySlim<IAxisManager<double>>().AddTo(Disposables);
                PC2LoadingAbsoluteVerticalAxis = ComponentY.Select(i => model.LoadingAbsoluteAxises[i - 1].Value).ToReadOnlyReactivePropertySlim<IAxisManager<double>>().AddTo(Disposables);
            }
            
            Scores = new ReactiveCollection<ComponentScoreViewModel>(UIDispatcherScheduler.Default).AddTo(Disposables);
            if (isOpls) {
                Observable.CombineLatest(ComponentX, ComponentY)
                .Subscribe(xy => {
                    Scores.ClearOnScheduler();
                    Scores.AddRangeOnScheduler(_model.Scores.Select(score => new ComponentScoreViewModel(score, xy[0] - 1, xy[1] - 1, true)));
                }).AddTo(Disposables);

                ScoreHorizontalAxis = ComponentX.Select(i => model.ScoreAxises[i - 1].Value).ToReadOnlyReactivePropertySlim<IAxisManager<double>>().AddTo(Disposables);
                ScoreVerticalAxis = ComponentY.Select(i => model.OScoreAxises![i - 1].Value).ToReadOnlyReactivePropertySlim<IAxisManager<double>>().AddTo(Disposables);

                PCXLabelAxis = ComponentX.Select(x => model.PCAxises[x - 1]).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
                PCYLabelAxis = ComponentY.Select(y => model.PCOAxises![y - 1]).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            }
            else {
                Observable.CombineLatest(ComponentX, ComponentY)
                .Subscribe(xy => {
                    Scores.ClearOnScheduler();
                    Scores.AddRangeOnScheduler(_model.Scores.Select(score => new ComponentScoreViewModel(score, xy[0] - 1, xy[1] - 1, false)));
                }).AddTo(Disposables);

                ScoreHorizontalAxis = ComponentX.Select(i => model.ScoreAxises[i - 1].Value).ToReadOnlyReactivePropertySlim<IAxisManager<double>>().AddTo(Disposables);
                ScoreVerticalAxis = ComponentY.Select(i => model.ScoreAxises[i - 1].Value).ToReadOnlyReactivePropertySlim<IAxisManager<double>>().AddTo(Disposables);

                PCXLabelAxis = ComponentX.Select(x => model.PCAxises[x - 1]).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
                PCYLabelAxis = ComponentY.Select(y => model.PCAxises[y - 1]).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            }

            SaveResultCommand = new ReactiveCommand().WithSubscribe(SaveResult).AddTo(Disposables);
        }


        public ReadOnlyCollection<int> Components { get; }
        public ReactiveProperty<int> ComponentX { get; }
        public ReactiveProperty<int> ComponentY { get; }

        public ReadOnlyCollection<IReadOnlyReactiveProperty<LabelTypeViewModel>> LabelTypesSample { get; }
        public ReactiveProperty<LabelTypeViewModel> LabelTypeSample { get; }

        public ReadOnlyCollection<IReadOnlyReactiveProperty<LabelTypeViewModel>> LabelTypesMetabolite { get; }
        public ReactiveProperty<LabelTypeViewModel> LabelTypeMetabolite { get; }

        public ReadOnlyCollection<double> SpotSizes { get; } = new List<double>
        {
            1, 2, 3, 4, 5, 6, 8, 10, 12, 14, 16, 20, 24,
        }.AsReadOnly();
        public ReactiveProperty<double> SpotSizeSample { get; }
        public ReactiveProperty<double> SpotSizeMetabolite { get; }

        public ReactiveCollection<ComponentLoadingViewModel> Loadings { get; }
        public ReactiveCollection<ComponentScoreViewModel> Scores { get; }
        public BrushMapData<ComponentLoadingViewModel> Brush { get; }
        public IObservable<IBrushMapper<ComponentScoreViewModel>?> ClassBrush { get; }
        public IBrushMapper<ComponentLoadingViewModel> PosnegBrush { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager<double>> LoadingHorizontalAxis { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager<double>> LoadingVerticalAxis { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager<double>> PC1LoadingAbsoluteVerticalAxis { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager<double>> PC2LoadingAbsoluteVerticalAxis { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager<double>> ScoreHorizontalAxis { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager<double>> ScoreVerticalAxis { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager<string>> PCXLabelAxis { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager<string>> PCYLabelAxis { get; }

        public DelegateCommand<Window> ShowContributionsCommand => showContributionsCommand ??= new DelegateCommand<Window>(_model.ShowContributionPlot);
        private DelegateCommand<Window>? showContributionsCommand;

        public DelegateCommand<Window> ShowPredVsExpCommand => showPredVsExpCommand ??= new DelegateCommand<Window>(_model.ShowPredVsExp);
        private DelegateCommand<Window>? showPredVsExpCommand;

        public DelegateCommand<Window> ShowVIPsCommand => showVIPsCommand ??= new DelegateCommand<Window>(_model.ShowVIPs);
        private DelegateCommand<Window>? showVIPsCommand;

        public DelegateCommand<Window> ShowCoefficientsCommand => showCoefficientsCommand ??= new DelegateCommand<Window>(_model.ShowCoefficients);
        private DelegateCommand<Window>? showCoefficientsCommand;

        public DelegateCommand<Window> ShowSPlotCommand => showSPlotCommand ??= new DelegateCommand<Window>(_model.ShowSPlot);
        private DelegateCommand<Window>? showSPlotCommand;

        public ReactiveCommand SaveResultCommand { get; }

        private void SaveResult() {
            var request = new SaveFileNameRequest(_model.SaveResult)
            {
                Title = "Export multivariate analysis result",
            };
            _broker.Publish(request);
        }
    }
}

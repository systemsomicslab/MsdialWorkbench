using CompMs.App.Msdial.Model.Statistics;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Statistics
{
    internal sealed class ComponentLoadingViewModel : ViewModelBase
    {
        private readonly ComponentLoadingModel _model;


        public ComponentLoadingViewModel(ComponentLoadingModel model, int xIndex, int yIndex) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            ComponentX = _model.Loading[xIndex];
            ComponentY = _model.Loading[yIndex];
        }

        public string Label => _model.Label;

        public double ComponentX { get; }
        public double ComponentY { get; }
    }

    internal sealed class ComponentScoreViewModel : ViewModelBase {

    }

    internal sealed class ComponentContributionViewModel : ViewModelBase {

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

    internal sealed class PcaResultViewModel : ViewModelBase
    {
        private readonly PcaResultModel _model;

        public PcaResultViewModel(PcaResultModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));

            Components = Enumerable.Range(1, model.NumberOfComponents).ToList().AsReadOnly();
            ComponentX = new ReactiveProperty<int>(1).AddTo(Disposables);
            ComponentY = new ReactiveProperty<int>(2).AddTo(Disposables);

            LabelTypesSample = new List<IReadOnlyReactiveProperty<LabelTypeViewModel>>
            {
                new ReactivePropertySlim<LabelTypeViewModel>(new LabelTypeViewModel("Sample", null /*nameof(ComponentScoreViewModel.Label)*/)),
                ComponentX.Select(i => new LabelTypeViewModel($"Component {i}", null /*nameof(ComponentScoreViewModel.ComponentX)*/)).ToReadOnlyReactivePropertySlim().AddTo(Disposables),
                ComponentY.Select(i => new LabelTypeViewModel($"Component {i}", null /*nameof(ComponentScoreViewModel.ComponentY)*/)).ToReadOnlyReactivePropertySlim().AddTo(Disposables),
                new ReactivePropertySlim<LabelTypeViewModel>(new LabelTypeViewModel("None", null)),
            }.AsReadOnly();
            LabelTypeSample = new ReactiveProperty<LabelTypeViewModel>(LabelTypesSample.First().Value).AddTo(Disposables);

            LabelTypesMetabolite = new List<IReadOnlyReactiveProperty<LabelTypeViewModel>>
            {
                new ReactivePropertySlim<LabelTypeViewModel>(new LabelTypeViewModel("Metabolite", nameof(ComponentLoadingViewModel.Label))),
                ComponentX.Select(i => new LabelTypeViewModel($"Component {i}", nameof(ComponentLoadingViewModel.ComponentX))).ToReadOnlyReactivePropertySlim().AddTo(Disposables),
                ComponentY.Select(i => new LabelTypeViewModel($"Component {i}", nameof(ComponentLoadingViewModel.ComponentY))).ToReadOnlyReactivePropertySlim().AddTo(Disposables),
                new ReactivePropertySlim<LabelTypeViewModel>(new LabelTypeViewModel("None", null)),
            }.AsReadOnly();
            LabelTypeMetabolite = new ReactiveProperty<LabelTypeViewModel>(LabelTypesMetabolite.First().Value).AddTo(Disposables);

            SpotSizeSample = new ReactiveProperty<double>(6).AddTo(Disposables);
            SpotSizeMetabolite = new ReactiveProperty<double>(6).AddTo(Disposables);

            Loadings = new ReactiveCollection<ComponentLoadingViewModel>(UIDispatcherScheduler.Default).AddTo(Disposables);
            Observable.CombineLatest(ComponentX, ComponentY)
                .Throttle(TimeSpan.FromSeconds(.05d))
                .Subscribe(xy =>
                {
                    Loadings.ClearOnScheduler();
                    Loadings.AddRangeOnScheduler(_model.Loadings.Select(loading => new ComponentLoadingViewModel(loading, xy[0] - 1, xy[1] - 1)));

                }).AddTo(Disposables);

            LoadingHorizontalAxis = ComponentX.Select(i => model.LoadingAxises[i - 1].Value).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            LoadingVerticalAxis = ComponentY.Select(i => model.LoadingAxises[i - 1].Value).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
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

        public ReadOnlyReactiveCollection<ComponentScoreViewModel> Scores { get; }
        public IAxisManager<double> ScoreHorizontalAxis { get; }
        public IAxisManager<double> ScoreVerticalAxis { get; }

        public ReactiveCollection<ComponentLoadingViewModel> Loadings { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager<double>> LoadingHorizontalAxis { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager<double>> LoadingVerticalAxis { get; }
        public IAxisManager<double> ComponentXLoadingHorizontalAxis { get; }
        public IAxisManager<double> ComponentXLoadingVerticalAxis { get; }
        public IAxisManager<double> ComponentYLoadingHorizontalAxis { get; }
        public IAxisManager<double> ComponentYLoadingVerticalAxis { get; }

        public ReadOnlyReactiveCollection<ComponentContributionViewModel> Contributions { get; }
        public IAxisManager<double> ContributionHorizontalAxis { get; }
        public IAxisManager<double> ContributionVerticalAxis { get; }

        public ReactiveCommand ShowContributionsCommand { get; }
        public ReactiveCommand SaveResultCommand { get; }
    }
}

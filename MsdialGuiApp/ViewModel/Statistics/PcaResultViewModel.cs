using CompMs.App.Msdial.Model.Statistics;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace CompMs.App.Msdial.ViewModel.Statistics
{
    internal sealed class ComponentLoadingViewModel : DynamicObject, INotifyPropertyChanged
    {
        private readonly ComponentLoadingModel _model;


        public ComponentLoadingViewModel(ComponentLoadingModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public string Label => _model.Label;

        // DynamicObject
        private static readonly Regex PATTERN = new Regex(@"Component (?<id>\d+)");
        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            var match = PATTERN.Match(binder.Name);
            if (match.Success && match.Groups["id"].Success) {
                var index = int.Parse(match.Groups["id"].Value);
                if (index <= _model.Loading.Length) {
                    result = _model.Loading[index - 1];
                    return true;
                }
            }
            result = default;
            return false;
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
    }

    internal sealed class ComponentScoreViewModel : ViewModelBase {

    }

    internal sealed class ComponentContributionViewModel : ViewModelBase {

    }

    internal sealed class PcaResultViewModel : ViewModelBase
    {
        private readonly PcaResultModel _model;

        public PcaResultViewModel(PcaResultModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));

            Components = Enumerable.Range(1, model.NumberOfComponents).ToList().AsReadOnly();
            ComponentX = new ReactiveProperty<int>(1).AddTo(Disposables);
            ComponentY = new ReactiveProperty<int>(2).AddTo(Disposables);

            LabelTypes = new List<IReadOnlyReactiveProperty<string>>
            {
                new ReactivePropertySlim<string>("Label"),
                ComponentX.Select(i => $"Component {i}").ToReadOnlyReactivePropertySlim().AddTo(Disposables),
                ComponentY.Select(i => $"Component {i}").ToReadOnlyReactivePropertySlim().AddTo(Disposables),
                new ReactivePropertySlim<string>("None"),
            }.AsReadOnly();

            Loadings = model.Loadings.ToReadOnlyReactiveCollection(m => new ComponentLoadingViewModel(m)).AddTo(Disposables);
            LoadingHorizontalAxis = ComponentX.Select(i => model.LoadingAxises[i].Value).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            LoadingVerticalAxis = ComponentY.Select(i => model.LoadingAxises[i].Value).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public ReadOnlyCollection<int> Components { get; }
        public ReactiveProperty<int> ComponentX { get; }
        public ReactiveProperty<int> ComponentY { get; }

        public ReadOnlyCollection<IReadOnlyReactiveProperty<string>> LabelTypes { get; }
        public ReactiveProperty<string> LabelTypeSample { get; }
        public ReactiveProperty<string> LabelTypeMetabolite { get; }

        public ReadOnlyCollection<string> SpotSizes { get; } = new List<string>
        {
            "1", "2", "3", "4", "5", "6", "8", "10", "12", "14", "16", "20", "24",
        }.AsReadOnly();
        public ReactiveProperty<string> SpotSizeSample { get; }
        public ReactiveProperty<string> SpotSizeMetabolite { get; }

        public ReadOnlyReactiveCollection<ComponentScoreViewModel> Scores { get; }
        public IAxisManager<double> ScoreHorizontalAxis { get; }
        public IAxisManager<double> ScoreVerticalAxis { get; }

        public ReadOnlyReactiveCollection<ComponentLoadingViewModel> Loadings { get; }
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

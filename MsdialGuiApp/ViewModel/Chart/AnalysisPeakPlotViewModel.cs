using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    class AnalysisPeakPlotViewModel : ViewModelBase
    {
        public AnalysisPeakPlotViewModel(
            AnalysisPeakPlotModel model,
            IAxisManager<double> horizontalAxis,
            IAxisManager<double> verticalAxis) {
            Spots = model.Spots;

            HorizontalAxis = horizontalAxis;
            VerticalAxis = verticalAxis;

            Target = model.ToReactivePropertyAsSynchronized(m => m.Target)
                .AddTo(Disposables);

            GraphTitle = model.ObserveProperty(m => m.GraphTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            HorizontalTitle = model.ObserveProperty(m => m.HorizontalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            VerticalTitle = model.ObserveProperty(m => m.VerticalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            HorizontalProperty = model.ObserveProperty(m => m.HorizontalProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            VerticalProperty = model.ObserveProperty(m => m.VerticalProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
        }

        public AnalysisPeakPlotViewModel(AnalysisPeakPlotModel model) {
            Spots = model.Spots;

            Target = model.ToReactivePropertyAsSynchronized(m => m.Target)
                .AddTo(Disposables);

            GraphTitle = model.ObserveProperty(m => m.GraphTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            HorizontalTitle = model.ObserveProperty(m => m.HorizontalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            VerticalTitle = model.ObserveProperty(m => m.VerticalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            HorizontalProperty = model.ObserveProperty(m => m.HorizontalProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            VerticalProperty = model.ObserveProperty(m => m.VerticalProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            LabelProperty = model.ObserveProperty(m => m.LabelProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            var hRange = model.ObserveProperty(m => m.HorizontalRange)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            HorizontalAxis = new ReactiveAxisManager<double>(hRange);
            var vRange = model.ObserveProperty(m => m.VerticalRange)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            VerticalAxis = new ReactiveAxisManager<double>(vRange);
        }

        public ObservableCollection<ChromatogramPeakFeatureModel> Spots { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; } 

        public ReactiveProperty<ChromatogramPeakFeatureModel> Target { get; }

        public ReadOnlyReactivePropertySlim<string> GraphTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalProperty { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalProperty { get; }

        public ReadOnlyReactivePropertySlim<string> LabelProperty { get; }
    }
}

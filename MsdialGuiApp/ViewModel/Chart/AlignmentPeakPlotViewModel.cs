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
    class AlignmentPeakPlotViewModel : ViewModelBase
    {
        public AlignmentPeakPlotViewModel(
            AlignmentPeakPlotModel model,
            IAxisManager<double> horizontalAxis = null,
            IAxisManager<double> verticalAxis = null) {

            Spots = model.Spots;

            if (horizontalAxis == null) {
                horizontalAxis = model.ObserveProperty(m => m.HorizontalRange)
                    .ToReactiveAxisManager<double>(new ChartMargin(0.05));
            }
            HorizontalAxis = horizontalAxis;

            if (verticalAxis == null) {
                verticalAxis = model.ObserveProperty(m => m.VerticalRange)
                    .ToReactiveAxisManager<double>(new ChartMargin(0.05));
            }
            VerticalAxis = verticalAxis;

            Target = model.ToReactivePropertyAsSynchronized(m => m.Target)
                .AddTo(Disposables);

            GraphTitle = model.ObserveProperty(m => m.GraphTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            HorizontalTitle = model.ObserveProperty(m => m.HorizontalProperty)
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

        public ObservableCollection<AlignmentSpotPropertyModel> Spots { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; }

        public ReactiveProperty<AlignmentSpotPropertyModel> Target { get; }

        public ReadOnlyReactivePropertySlim<string> GraphTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalProperty { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalProperty { get; }
    }
}

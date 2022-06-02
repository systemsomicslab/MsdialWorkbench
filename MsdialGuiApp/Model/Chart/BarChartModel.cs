using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart
{
    sealed class BarChartModel : DisposableModelBase {
        public BarChartModel(IObservable<List<BarItem>> barItems, IObservable<IBrushMapper<BarItem>> classBrsh) {

            BarItemsSource = barItems ?? throw new ArgumentNullException(nameof(barItems));
            if (classBrsh is null) {
                classBrsh = BarItemsSource.Select(
                    items => new KeyBrushMapper<BarItem>(
                        items.Zip(ChartBrushes.SolidColorBrushList, (item, brush) => (item, brush))
                            .ToDictionary(p => p.item, p => (Brush)p.brush),
                        Brushes.Blue
                    ));
            }
            ClassBrush = classBrsh;
            VerticalRangeAsObservable = BarItemsSource.Select(items =>
                {
                    if (items.Any()) {
                        var minimum = items.Min(item => item.Height - (double.IsNaN(item.Error) ? 0 : item.Error));
                        var maximum = items.Max(item => item.Height + (double.IsNaN(item.Error) ? 0 : item.Error));
                        return new Range(minimum, maximum);
                    }
                    return new Range(0, 1);
                })
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
        }


        public IObservable<List<BarItem>> BarItemsSource { get; }
        public IObservable<Range> VerticalRangeAsObservable { get; }
        public IObservable<IBrushMapper<BarItem>> ClassBrush { get; }
        public GraphElements Elements { get; } = new GraphElements();

        public static BarChartModel Create(IObservable<AlignmentSpotPropertyModel> source, IObservable<IBarItemsLoader> barItemsLoader, IObservable<IBrushMapper<BarItem>> classBrush) {

            return new BarChartModel(
                source.CombineLatest(barItemsLoader,
                    (src, loader) => src is null || loader is null
                        ? Observable.Return(new List<BarItem>())
                        : loader.LoadBarItemsAsObservable(src))
                .Switch(),
                classBrush);
        }
    }
}

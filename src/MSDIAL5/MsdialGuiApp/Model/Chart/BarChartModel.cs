using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class BarChartModel : DisposableModelBase {
        public BarChartModel(IObservable<AlignmentSpotPropertyModel?> source, IReactiveProperty<BarItemsLoaderData> barItemsLoaderData, IList<BarItemsLoaderData> barItemsLoaderDatas, IObservable<IBrushMapper<BarItem>> classBrush, FilePropertiesModel projectBaseParameter, AnalysisFileBeanModelCollection fileModelCollection, FileClassPropertiesModel fileClassProperties) {
            var barItemsLoader = barItemsLoaderData.Where(data => data != null).Select(data => data.Loader).ToReactiveProperty().AddTo(Disposables);
            var barItemCollectionSource = source.CombineLatest(barItemsLoader,
                    (src, loader) => src is null || loader is null
                        ? new BarItemCollection()
                        : loader.LoadBarItemsAsObservable(src))
                .ToReactiveProperty(new BarItemCollection())
                .AddTo(Disposables);
            BarItemsSource = barItemCollectionSource
                .SelectSwitch(collection => collection.ObservableItems)
                .ToReactiveProperty()
                .AddTo(Disposables);
            IsLoading = barItemCollectionSource
                .SelectSwitch(collection => collection.ObservableLoading);

            ClassBrush = classBrush;
            BarItemsLoaderData = barItemsLoaderData;
            BarItemsLoaderDatas = barItemsLoaderDatas;
            VerticalRangeAsObservable = BarItemsSource.Select(items =>
                {
                    if (items?.Any() ?? false) {
                        var minimum = items.Min(item => item.Height - (double.IsNaN(item.Error) ? 0 : item.Error));
                        var maximum = items.Max(item => item.Height + (double.IsNaN(item.Error) ? 0 : item.Error));
                        return new AxisRange(minimum, maximum);
                    }
                    return new AxisRange(0, 1);
                })
                .ToReadOnlyReactivePropertySlim(new AxisRange(0d, 1d))
                .AddTo(Disposables);

            Elements.HorizontalTitle = "Class";
            Elements.VerticalTitle = "Height";
            Elements.HorizontalProperty = nameof(BarItem.Class);
            Elements.VerticalProperty = nameof(BarItem.Height);
            barItemsLoaderData
                .Where(data => !(data is null))
                .SelectSwitch(data => data.AxisLabel)
                .Subscribe(label => Elements.VerticalTitle = label)
                .AddTo(Disposables);

            var orderedClasses = fileClassProperties.GetOrderedUsedClasses(fileModelCollection).ToReadOnlyReactivePropertySlim(Array.Empty<string>()).AddTo(Disposables);
            OrderedClasses = orderedClasses;
        }

        public IObservable<List<BarItem>> BarItemsSource { get; }
        public IObservable<AxisRange> VerticalRangeAsObservable { get; }
        public IObservable<IBrushMapper<BarItem>> ClassBrush { get; }
        public IReactiveProperty<BarItemsLoaderData> BarItemsLoaderData { get; }
        public IList<BarItemsLoaderData> BarItemsLoaderDatas { get; }
        public GraphElements Elements { get; } = new GraphElements();

        public IObservable<bool> IsLoading { get; }
        public IObservable<IReadOnlyList<string>> OrderedClasses { get; }
    }
}

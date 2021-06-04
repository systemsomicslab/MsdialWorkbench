using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    class BarChartModel : BindableBase {
        public BarChartModel(
            IObservable<List<BarItem>> barItems,
            Func<BarItem, string> horizontalSelector,
            Func<BarItem, double> verticalSelector) {

            BarItemsSource = barItems;
            BarItemsSource.Subscribe(items => BarItems = items);

            HorizontalSelector = horizontalSelector;
            VerticalSelector = verticalSelector;
        }

        public List<BarItem> BarItems {
            get => barItems;
            set {
                if (SetProperty(ref barItems, value)) {
                    OnPropertyChanged(nameof(VerticalRange));
                }
            }
        }
        private List<BarItem> barItems = new List<BarItem>();

        public GraphElements Elements { get; } = new GraphElements();

        public Func<BarItem, string> HorizontalSelector { get; }

        public Func<BarItem, double> VerticalSelector { get; }

        public IObservable<List<BarItem>> BarItemsSource { get; }

        public Range VerticalRange {
            get {
                if (BarItems.Any() && VerticalSelector != null) {
                    var minimum = BarItems.Min(VerticalSelector);
                    var maximum = BarItems.Max(VerticalSelector);
                    return new Range(minimum, maximum);
                }
                return new Range(0, 1);
            }
        }

        public static BarChartModel Create(
            IObservable<AlignmentSpotPropertyModel> source,
            IBarItemsLoader loader,
            Func<BarItem, string> horizontalSelector,
            Func<BarItem, double> verticalSelector) {

            return new BarChartModel(
                source.SelectMany(src =>
                    Observable.DeferAsync(async token => {
                        var result = await loader.LoadBarItemsAsync(src, token);
                        return Observable.Return(result);
                    })),
                horizontalSelector, verticalSelector);
        }
    }
}

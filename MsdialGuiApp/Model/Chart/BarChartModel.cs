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
            IObservable<List<BarItem>> barItems) {

            BarItemsSource = barItems;
            BarItemsSource.Subscribe(items => BarItems = items);
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

        public IObservable<List<BarItem>> BarItemsSource { get; }

        public Range VerticalRange {
            get {
                if (BarItems.Any()) {
                    var minimum = BarItems.Min(item => item.Height - (double.IsNaN(item.Error) ? 0 : item.Error));
                    var maximum = BarItems.Max(item => item.Height + (double.IsNaN(item.Error) ? 0 : item.Error));
                    return new Range(minimum, maximum);
                }
                return new Range(0, 1);
            }
        }

        public static BarChartModel Create(
            IObservable<AlignmentSpotPropertyModel> source,
            IBarItemsLoader loader) {

            return new BarChartModel(
                source.SelectMany(src => 
                    Observable.DeferAsync(async token => {
                        var result = await loader.LoadBarItemsAsync(src, token);
                        return Observable.Return(result);
                    })));
        }

        public static BarChartModel Create(
            IObservable<AlignmentSpotPropertyModel> source,
            IObservable<IBarItemsLoader> loader) {

            return new BarChartModel(
                source.SelectMany(src => loader.SelectMany(n => 
                    Observable.DeferAsync(async token => {
                        var result = await n.LoadBarItemsAsync(src, token);
                        return Observable.Return(result);
                    }))));
        }
    }
}

using CompMs.App.Msdial.Model.Statistics;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Statistics {
    internal class SimpleBarChartViewModel : ViewModelBase {
        private readonly SimpleBarChartModel _model;
        public SimpleBarChartViewModel(SimpleBarChartModel model) {
            _model = model;
            XAxisTitle = _model.ToReactivePropertySlimAsSynchronized(m => m.XAxisTitle).AddTo(Disposables);
            YAxisTitle = _model.ToReactivePropertySlimAsSynchronized(m => m.YAxisTitle).AddTo(Disposables);
            GraphTitle = _model.ToReactivePropertySlimAsSynchronized(m => m.GraphTitle).AddTo(Disposables);
            BarItems = _model.BarItems.ToReadOnlyReactiveCollection(m => new BarItemViewModel(m)).AddTo(Disposables);
        }
        public ReactivePropertySlim<string> XAxisTitle { get; }
        public ReactivePropertySlim<string> YAxisTitle { get; }
        public ReactivePropertySlim<string> GraphTitle { get; }
        public ReadOnlyReactiveCollection<BarItemViewModel> BarItems { get; }
    }

    internal class BarItemViewModel : ViewModelBase {
        private readonly SimpleBarItem _barItem;
        public BarItemViewModel(SimpleBarItem barItem) {
            _barItem = barItem;
            Brush = _barItem.ToReactivePropertySlimAsSynchronized(m => m.Brush).AddTo(Disposables);
            ID = _barItem.ToReactivePropertySlimAsSynchronized(m => m.ID).AddTo(Disposables);
            Legend = _barItem.ToReactivePropertySlimAsSynchronized(m => m.Legend).AddTo(Disposables);
            Value = _barItem.ToReactivePropertySlimAsSynchronized(m => m.Value).AddTo(Disposables);
            Error = _barItem.ToReactivePropertySlimAsSynchronized(m => m.Error).AddTo(Disposables);
        }
        public ReactivePropertySlim<SolidColorBrush> Brush { get; }
        public ReactivePropertySlim<int> ID { get; }
        public ReactivePropertySlim<string> Legend { get; }
        public ReactivePropertySlim<double> Value { get; }
        public ReactivePropertySlim<double> Error { get; }
    }
}

using CompMs.App.Msdial.Model.Statistics;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel.Statistics
{
    internal class SimpleBarChartViewModel : ViewModelBase {
        private readonly SimpleBarChartModel _model;
        public SimpleBarChartViewModel(SimpleBarChartModel model) {
            _model = model;
            XAxisTitle = _model.ToReactivePropertySlimAsSynchronized(m => m.XAxisTitle).AddTo(Disposables);
            YAxisTitle = _model.ToReactivePropertySlimAsSynchronized(m => m.YAxisTitle).AddTo(Disposables);
            GraphTitle = _model.ObserveProperty(m => m.GraphTitle).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            //BarItems = _model.BarItems;
            BarItems = _model.ToReactivePropertyAsSynchronized(m => m.BarItems).AddTo(Disposables);
            XAxis = _model.ToReactivePropertySlimAsSynchronized(m => m.XAxis).AddTo(Disposables);
            YAxis = _model.ToReactivePropertySlimAsSynchronized(m => m.YAxis).AddTo(Disposables);
        }
        public ReactivePropertySlim<string?> XAxisTitle { get; }
        public ReactivePropertySlim<string?> YAxisTitle { get; }
        public ReadOnlyReactivePropertySlim<string?> GraphTitle { get; }
        //public ObservableCollection<SimpleBarItem> BarItems { get; }
        public ReactiveProperty<ObservableCollection<SimpleBarItem>> BarItems { get; }
        public ReactivePropertySlim<IAxisManager<string>?> XAxis { get; }
        public ReactivePropertySlim<IAxisManager<double>?> YAxis { get; }
    }
}

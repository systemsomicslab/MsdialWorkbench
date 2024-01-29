using CompMs.App.Msdial.Model.Statistics;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel.Statistics
{
    internal class SimpleScatterPlotViewModel : ViewModelBase {
        private readonly SimpleScatterPlotModel _model;
        public SimpleScatterPlotViewModel(SimpleScatterPlotModel model) {
            _model = model;
            XAxisTitle = _model.ToReactivePropertySlimAsSynchronized(m => m.XAxisTitle).AddTo(Disposables);
            YAxisTitle = _model.ToReactivePropertySlimAsSynchronized(m => m.YAxisTitle).AddTo(Disposables);
            GraphTitle = _model.ToReactivePropertySlimAsSynchronized(m => m.GraphTitle).AddTo(Disposables);
            PlotItems = _model.ToReactivePropertyAsSynchronized(m => m.PlotItems).AddTo(Disposables);
            XAxis = _model.ToReactivePropertySlimAsSynchronized(m => m.XAxis).AddTo(Disposables);
            YAxis = _model.ToReactivePropertySlimAsSynchronized(m => m.YAxis).AddTo(Disposables);
        }
        public ReactivePropertySlim<string?> XAxisTitle { get; }
        public ReactivePropertySlim<string?> YAxisTitle { get; }
        public ReactivePropertySlim<string?> GraphTitle { get; }
        public ReactiveProperty<ObservableCollection<SimplePlotItem>?> PlotItems { get; }
        public ReactivePropertySlim<IAxisManager<double>?> XAxis { get; }
        public ReactivePropertySlim<IAxisManager<double>?> YAxis { get; }
        public IBrushMapper<SimplePlotItem>? PointBrush => _model.PointBrush;
    }
}

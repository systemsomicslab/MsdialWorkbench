using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal class SingleSpectrumViewModel : ViewModelBase
    {
        private readonly SingleSpectrumModel _model;

        public SingleSpectrumViewModel(SingleSpectrumModel model) {
            _model = model;
            MsSpectrum = model.MsSpectrum.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            HorizontalAxis = model.HorizontalAxis.Cast<IAxisManager>().ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            VerticalAxis = model.VerticalAxis.Cast<IAxisManager>().ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Brush = Observable.Return(model.Brush).ToReadOnlyReactivePropertySlim<IBrushMapper>().AddTo(Disposables);
            LineThickness = model.LineThickness;
            IsVisible = model.IsVisible;
            SelectedVerticalAxisItem = model.VerticalAxisItemSelector.GetAxisItemAsObservable().SkipNull().ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<MsSpectrum?> MsSpectrum { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager?> HorizontalAxis { get; }
        public GraphLabels Labels => _model.Labels;
        public string HorizontalProperty => _model.HorizontalProperty;
        public ReadOnlyReactivePropertySlim<IAxisManager?> VerticalAxis { get; }
        public ReadOnlyReactivePropertySlim<AxisItemModel<double>?> SelectedVerticalAxisItem { get; }
        public string VerticalProperty => _model.VerticalProperty;
        public ReadOnlyReactivePropertySlim<IBrushMapper> Brush { get; }
        public string HueProperty => _model.HueProperty;
        public ReadOnlyReactivePropertySlim<bool> SpectrumLoaded => _model.SpectrumLoaded;
        public ReactivePropertySlim<double> LineThickness { get; }
        public ReactivePropertySlim<bool> IsVisible { get; }
    }
}

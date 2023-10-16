using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class SingleSpectrumModel : DisposableModelBase
    {
        private readonly ChartHueItem _hueItem;
        private readonly ObservableMsSpectrum _observableMsSpectrum;

        public SingleSpectrumModel(ObservableMsSpectrum observableMsSpectrum, AxisPropertySelectors<double> horizontalPropertySelectors, AxisPropertySelectors<double> verticalPropertySelectors, ChartHueItem hueItem, GraphLabels graphLabels) {
            _observableMsSpectrum = observableMsSpectrum ?? throw new ArgumentNullException(nameof(observableMsSpectrum));
            HorizontalPropertySelectors = horizontalPropertySelectors ?? throw new ArgumentNullException(nameof(horizontalPropertySelectors));
            VerticalPropertySelectors = verticalPropertySelectors ?? throw new ArgumentNullException(nameof(verticalPropertySelectors));
            _hueItem = hueItem;
            Labels = graphLabels;

            IsVisible = new ReactivePropertySlim<bool>(true).AddTo(Disposables);
            LineThickness = new ReactivePropertySlim<double>(2d).AddTo(Disposables);
        }

        public IObservable<MsSpectrum> MsSpectrum => _observableMsSpectrum.MsSpectrum;
        public AxisPropertySelectors<double> HorizontalPropertySelectors { get; }
        public IObservable<IAxisManager<double>> HorizontalAxis => HorizontalPropertySelectors.AxisItemSelector.GetAxisItemAsObservable().SkipNull().Select(item => item.AxisManager);
        public string HorizontalProperty => HorizontalPropertySelectors.GetSelector(typeof(SpectrumPeak)).Property;
        public AxisPropertySelectors<double> VerticalPropertySelectors { get; }
        public IObservable<IAxisManager<double>> VerticalAxis => VerticalPropertySelectors.AxisItemSelector.GetAxisItemAsObservable().SkipNull().Select(item => item.AxisManager);
        public AxisItemSelector<double> VerticalAxisItemSelector => VerticalPropertySelectors.AxisItemSelector;
        public string VerticalProperty => VerticalPropertySelectors.GetSelector(typeof(SpectrumPeak)).Property;
        public IBrushMapper Brush => _hueItem.Brush;
        public string HueProperty => _hueItem.Property;
        public GraphLabels Labels { get; }
        public ReadOnlyReactivePropertySlim<bool> SpectrumLoaded => _observableMsSpectrum.Loaded;
        public ReactivePropertySlim<bool> IsVisible { get; }
        public ReactivePropertySlim<double> LineThickness { get; }

        public IObservable<bool> CanSave => _observableMsSpectrum.CanSave;

        public void Save(Stream stream) {
            _observableMsSpectrum.Save(stream);
        }

        public IObservable<(double, double)> GetHorizontalRange() {
            return _observableMsSpectrum.GetRange(((PropertySelector<SpectrumPeak, double>)HorizontalPropertySelectors.GetSelector(typeof(SpectrumPeak))).Selector);
        }

        public SingleSpectrumModel Product(SingleSpectrumModel other) {
            return CreateFromMsSpectrum(_observableMsSpectrum.Product(other._observableMsSpectrum).AddTo(Disposables));
        }

        public SingleSpectrumModel Difference(SingleSpectrumModel other) {
            return CreateFromMsSpectrum(_observableMsSpectrum.Difference(other._observableMsSpectrum).AddTo(Disposables));
        }

        private SingleSpectrumModel CreateFromMsSpectrum(ObservableMsSpectrum msSpectrum) {
            var spectrumModel = new SingleSpectrumModel(
                msSpectrum,
                msSpectrum.CreateAxisPropertySelectors((PropertySelector<SpectrumPeak, double>)HorizontalPropertySelectors.GetSelector(typeof(SpectrumPeak)), "m/z", "m/z"),
                msSpectrum.CreateAxisPropertySelectors2((PropertySelector<SpectrumPeak, double>)VerticalPropertySelectors.GetSelector(typeof(SpectrumPeak)), "abundance"),
                _hueItem, Labels);
            spectrumModel.Disposables.Add(msSpectrum);
            return spectrumModel;
        }
    }
}

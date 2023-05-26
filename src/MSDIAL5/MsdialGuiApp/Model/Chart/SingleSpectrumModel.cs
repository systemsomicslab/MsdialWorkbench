using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.AxisManager.Generic;
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
        private readonly PropertySelector<SpectrumPeak, double> _horizontalPropertySelector;
        private readonly PropertySelector<SpectrumPeak, double> _verticalPropertySelector;
        private readonly ChartHueItem _hueItem;
        private readonly ObservableMsSpectrum _observableMsSpectrum;

        public SingleSpectrumModel(
            ObservableMsSpectrum observableMsSpectrum,
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector,
            PropertySelector<SpectrumPeak, double> verticalPropertySelector,
            ChartHueItem hueItem,
            GraphLabels graphLabels) {
            _observableMsSpectrum = observableMsSpectrum;

            _horizontalPropertySelector = horizontalPropertySelector;
            HorizontalProperty = horizontalPropertySelector.Property;
            var horizontalAxis = observableMsSpectrum.MsSpectrum
                .Select(s => s?.GetSpectrumRange(horizontalPropertySelector.Selector) ?? (0d, 1d))
                .ToReactiveContinuousAxisManager(new ConstantMargin(40))
                .AddTo(Disposables);
            HorizontalAxis = Observable.Return(horizontalAxis);

            _verticalPropertySelector = verticalPropertySelector;
            var verticalRangeProperty = observableMsSpectrum.MsSpectrum
                .Select(s => s?.GetSpectrumRange(verticalPropertySelector.Selector) ?? (0d, 1d))
                .Publish();
            var continuousVerticalAxis = verticalRangeProperty
                .ToReactiveContinuousAxisManager(new ConstantMargin(0, 30), 0d, 0d, LabelType.Percent)
                .AddTo(Disposables);
            var logVerticalAxis = verticalRangeProperty
                .ToReactiveLogScaleAxisManager(new ConstantMargin(0, 30), 1d, 1d, labelType: LabelType.Percent)
                .AddTo(Disposables);
            var sqrtVerticalAxis = verticalRangeProperty
                .ToReactiveSqrtAxisManager(new ConstantMargin(0, 30), 0, 0, labelType: LabelType.Percent)
                .AddTo(Disposables);
            Disposables.Add(verticalRangeProperty.Connect());
            var verticalAxisSelector = new AxisItemSelector<double>(
                    new AxisItemModel("Relative", continuousVerticalAxis, verticalPropertySelector.Property),
                    new AxisItemModel("Log10", logVerticalAxis, verticalPropertySelector.Property),
                    new AxisItemModel("Sqrt", sqrtVerticalAxis, verticalPropertySelector.Property)).AddTo(Disposables);
            VerticalAxisItemSelector = verticalAxisSelector;
            VerticalAxis = verticalAxisSelector.GetAxisItemAsObservable().SkipNull().Select(item => item.AxisManager).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            _hueItem = hueItem;
            Labels = graphLabels;

            IsVisible = new ReactivePropertySlim<bool>(true).AddTo(Disposables);
            LineThickness = new ReactivePropertySlim<double>(2d).AddTo(Disposables);
        }

        public IObservable<MsSpectrum> MsSpectrum => _observableMsSpectrum.MsSpectrum;
        public IObservable<IAxisManager<double>> HorizontalAxis { get; }
        public string HorizontalProperty { get; }
        public IObservable<IAxisManager<double>> VerticalAxis { get; }
        public AxisItemSelector<double> VerticalAxisItemSelector { get; }
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
            return _observableMsSpectrum.GetRange(_horizontalPropertySelector.Selector);
        }

        public SingleSpectrumModel Product(SingleSpectrumModel other) {
            var productMsSpectrum = _observableMsSpectrum.Product(other._observableMsSpectrum);
            var upperProductSpectrumModel = new SingleSpectrumModel(productMsSpectrum, _horizontalPropertySelector, _verticalPropertySelector, _hueItem, Labels);
            upperProductSpectrumModel.Disposables.Add(productMsSpectrum);
            return upperProductSpectrumModel;
        }

        public SingleSpectrumModel Difference(SingleSpectrumModel other) {
            var differenceMsSpectrum = _observableMsSpectrum.Difference(other._observableMsSpectrum);
            var upperDifferenceSpectrumModel = new SingleSpectrumModel(differenceMsSpectrum, _horizontalPropertySelector, _verticalPropertySelector, _hueItem, Labels);
            upperDifferenceSpectrumModel.Disposables.Add(differenceMsSpectrum);
            return upperDifferenceSpectrumModel;
        }
    }
}

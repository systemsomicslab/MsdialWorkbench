using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Chart
{
    public class SingleSpectrumModel : BindableBase
    {
        public SingleSpectrumModel(
            IObservable<List<SpectrumPeak>> spectrum,
            IObservable<IAxisManager<double>> horizontalAxis,
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector,
            IObservable<IAxisManager<double>> verticalAxis,
            PropertySelector<SpectrumPeak, double> verticalPropertySelector,
            IObservable<IBrushMapper> brush,
            string hueProperty,
            GraphLabels labels) {

            Spectrum = spectrum;
            HorizontalAxis = horizontalAxis;
            Labels = labels;
            HorizontalPropertySelector = horizontalPropertySelector;
            VerticalAxis = verticalAxis;
            VerticalPropertySelector = verticalPropertySelector;
            Brush = brush;
            HueProperty = hueProperty;
        }

        public IObservable<List<SpectrumPeak>> Spectrum { get; }
        public IObservable<IAxisManager<double>> HorizontalAxis { get; }
        public GraphLabels Labels { get; }
        public PropertySelector<SpectrumPeak, double> HorizontalPropertySelector { get; }
        public IObservable<IAxisManager<double>> VerticalAxis { get; }
        public PropertySelector<SpectrumPeak, double> VerticalPropertySelector { get; }
        public IObservable<IBrushMapper> Brush { get; }
        public string HueProperty { get; }
    }
}

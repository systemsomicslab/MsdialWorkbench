using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Export;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.Model.Chart
{
    public sealed class SingleSpectrumModel : DisposableModelBase
    {
        public SingleSpectrumModel(
            IObservable<List<SpectrumPeak>> spectrum,
            IObservable<IAxisManager<double>> horizontalAxis,
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector,
            IObservable<IAxisManager<double>> verticalAxis,
            PropertySelector<SpectrumPeak, double> verticalPropertySelector,
            IObservable<IBrushMapper> brush,
            string hueProperty,
            GraphLabels labels,
            IObservable<ISpectraExporter> spectraExporter) {

            Spectrum = spectrum;
            HorizontalAxis = horizontalAxis;
            Labels = labels;
            HorizontalPropertySelector = horizontalPropertySelector;
            VerticalAxis = verticalAxis;
            VerticalPropertySelector = verticalPropertySelector;
            Brush = brush;
            HueProperty = hueProperty;

            var save = new Subject<Stream>().AddTo(Disposables);
            save.Where(s => s != null && s.CanWrite)
                .WithLatestFrom(spectraExporter.CombineLatest(spectrum), (stream, pair) => (stream, exporter: pair.First, spectrum: pair.Second))
                .Where(trio => trio.exporter != null && trio.spectrum != null)
                .Subscribe(trio => trio.exporter.Save(trio.stream, trio.spectrum))
                .AddTo(Disposables);
            SaveAsObservable = save;
            CanSave = spectraExporter.CombineLatest(spectrum)
                .Select(p => p.First != null && p.Second != null)
                .ToReadOnlyReactivePropertySlim(false)
                .AddTo(Disposables);

            IsVisible = new ReactivePropertySlim<bool>(true).AddTo(Disposables);
            LineThickness = new ReactivePropertySlim<double>(2d).AddTo(Disposables);
        }

        public IObservable<List<SpectrumPeak>> Spectrum { get; }
        public IObservable<IAxisManager<double>> HorizontalAxis { get; }
        public GraphLabels Labels { get; }
        public PropertySelector<SpectrumPeak, double> HorizontalPropertySelector { get; }
        public IObservable<IAxisManager<double>> VerticalAxis { get; }
        public PropertySelector<SpectrumPeak, double> VerticalPropertySelector { get; }
        public IObservable<IBrushMapper> Brush { get; }
        public string HueProperty { get; }
        public ReactivePropertySlim<bool> IsVisible { get; }
        public ReactivePropertySlim<double> LineThickness { get; }

        private readonly Subject<Stream> SaveAsObservable;

        public void Save(Stream stream) {
            SaveAsObservable.OnNext(stream);
        }

        public IObservable<bool> CanSave { get; }
    }
}

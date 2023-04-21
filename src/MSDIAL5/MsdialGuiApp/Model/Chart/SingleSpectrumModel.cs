using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.MsdialCore.Export;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Disposables;
using CompMs.Graphics.AxisManager;
using CompMs.App.Msdial.Utility;
using System.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    public sealed class SingleSpectrumModel : DisposableModelBase
    {
        private readonly Subject<Stream> _saveAsObservable;

        private SingleSpectrumModel(
            IObservable<MsSpectrum> msSpectrum,
            IObservable<IAxisManager<double>> horizontalAxis,
            string horizontalProperty,
            AxisItemSelector<double> verticalAxisItemSelector,
            IObservable<IBrushMapper> brush,
            string hueProperty,
            GraphLabels labels,
            IObservable<ISpectraExporter> spectraExporter,
            IDisposable disposable) {
            Disposables.Add(disposable);
            var spectrum = msSpectrum.Select(s => s.Spectrum);

            Spectrum = spectrum;
            HorizontalAxis = horizontalAxis;
            Labels = labels;
            HorizontalProperty = horizontalProperty;
            VerticalAxis = verticalAxisItemSelector.GetAxisItemAsObservable().SkipNull().Select(item => item.AxisManager).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            VerticalAxisItemSelector = verticalAxisItemSelector;
            Brush = brush;
            HueProperty = hueProperty;

            var save = new Subject<Stream>().AddTo(Disposables);
            save.Where(s => s != null && s.CanWrite)
                .WithLatestFrom(spectraExporter.CombineLatest(spectrum), (stream, pair) => (stream, exporter: pair.First, spectrum: pair.Second))
                .Where(trio => trio.exporter != null && trio.spectrum != null)
                .SelectMany(trio => Observable.FromAsync(token => trio.exporter.SaveAsync(trio.stream, trio.spectrum, token)))
                .Subscribe()
                .AddTo(Disposables);
            _saveAsObservable = save;
            CanSave = spectraExporter.CombineLatest(spectrum)
                .Select(p => p.First != null && p.Second != null)
                .ToReadOnlyReactivePropertySlim(false)
                .AddTo(Disposables);

            IsVisible = new ReactivePropertySlim<bool>(true).AddTo(Disposables);
            LineThickness = new ReactivePropertySlim<double>(2d).AddTo(Disposables);
        }

        public IObservable<List<SpectrumPeak>> Spectrum { get; }
        public IObservable<IAxisManager<double>> HorizontalAxis { get; }
        public string HorizontalProperty { get; }
        public IObservable<IAxisManager<double>> VerticalAxis { get; }
        public AxisItemSelector<double> VerticalAxisItemSelector { get; }
        public IObservable<IBrushMapper> Brush { get; }
        public string HueProperty { get; }
        public GraphLabels Labels { get; }
        public ReactivePropertySlim<bool> IsVisible { get; }
        public ReactivePropertySlim<double> LineThickness { get; }

        public void Save(Stream stream) {
            _saveAsObservable.OnNext(stream);
        }

        public IObservable<bool> CanSave { get; }

        public static SingleSpectrumModel Create(
            IObservable<MsSpectrum> msSpectrum,
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector,
            PropertySelector<SpectrumPeak, double> verticalPropertySelector,
            IObservable<IBrushMapper> spectrumBrush,
            string hueProperty,
            GraphLabels graphLabels,
            IObservable<ISpectraExporter> spectraExporter) {
            var disposables = new CompositeDisposable();

            var msSpectrum_ = msSpectrum.Publish();

            var horizontalAxis = msSpectrum_.Select(s => s.GetSpectrumRange(horizontalPropertySelector.Selector))
                .ToReactiveContinuousAxisManager(new ConstantMargin(40))
                .AddTo(disposables);

            var verticalRangeProperty = msSpectrum_
                .Select(s => s.GetSpectrumRange(verticalPropertySelector.Selector))
                .Publish();
            var continuousVerticalAxis = verticalRangeProperty
                .ToReactiveContinuousAxisManager(new ConstantMargin(0, 30), 0d, 0d, LabelType.Percent)
                .AddTo(disposables);
            var logVerticalAxis = verticalRangeProperty
                .ToReactiveLogScaleAxisManager(new ConstantMargin(0, 30), 1d, 1d, labelType: LabelType.Percent)
                .AddTo(disposables);
            var sqrtVerticalAxis = verticalRangeProperty
                .ToReactiveSqrtAxisManager(new ConstantMargin(0, 30), 0, 0, labelType: LabelType.Percent)
                .AddTo(disposables);
            disposables.Add(verticalRangeProperty.Connect());
            var verticalAxisSelector = new AxisItemSelector<double>(
                    new AxisItemModel("Relative", continuousVerticalAxis, verticalPropertySelector.Property),
                    new AxisItemModel("Log10", logVerticalAxis, verticalPropertySelector.Property),
                    new AxisItemModel("Sqrt", sqrtVerticalAxis, verticalPropertySelector.Property)).AddTo(disposables);

            disposables.Add(msSpectrum_.Connect());

            return new SingleSpectrumModel(
                msSpectrum_,
                Observable.Return(horizontalAxis), horizontalPropertySelector.Property,
                verticalAxisSelector,
                spectrumBrush, hueProperty,
                graphLabels,
                spectraExporter,
                disposables);
        }
    }
}

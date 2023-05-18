using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Export;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.Model.Chart
{
    public sealed class SingleSpectrumModel : DisposableModelBase
    {
        private readonly Subject<Stream> _saveAsObservable;
        private readonly PropertySelector<SpectrumPeak, double> _horizontalPropertySelector;
        private readonly PropertySelector<SpectrumPeak, double> _verticalPropertySelector;
        private readonly IObservable<ISpectraExporter> _spectraExporter;

        public SingleSpectrumModel(
            IObservable<MsSpectrum> msSpectrum,
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector,
            PropertySelector<SpectrumPeak, double> verticalPropertySelector,
            IObservable<IBrushMapper> spectrumBrush,
            string hueProperty,
            GraphLabels graphLabels,
            IObservable<ISpectraExporter> spectraExporter,
            ReadOnlyReactivePropertySlim<bool> spectrumLoaded) {

            var horizontalAxis = msSpectrum
                .Select(s => s?.GetSpectrumRange(horizontalPropertySelector.Selector) ?? (0d, 1d))
                .ToReactiveContinuousAxisManager(new ConstantMargin(40))
                .AddTo(Disposables);

            var verticalRangeProperty = msSpectrum
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

            var spectrum = msSpectrum.Select(s => s?.Spectrum ?? new List<SpectrumPeak>(0)).Publish();
            MsSpectrum = msSpectrum;
            _horizontalPropertySelector = horizontalPropertySelector;
            _verticalPropertySelector = verticalPropertySelector;
            HorizontalAxis = Observable.Return(horizontalAxis);
            Labels = graphLabels;
            _spectraExporter = spectraExporter;
            SpectrumLoaded = spectrumLoaded ?? Observable.Return(true).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            HorizontalProperty = horizontalPropertySelector.Property;
            VerticalAxis = verticalAxisSelector.GetAxisItemAsObservable().SkipNull().Select(item => item.AxisManager).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            VerticalAxisItemSelector = verticalAxisSelector;
            Brush = spectrumBrush;
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
            Disposables.Add(spectrum.Connect());

            IsVisible = new ReactivePropertySlim<bool>(true).AddTo(Disposables);
            LineThickness = new ReactivePropertySlim<double>(2d).AddTo(Disposables);
        }

        public IObservable<MsSpectrum> MsSpectrum { get; }
        public IObservable<IAxisManager<double>> HorizontalAxis { get; }
        public string HorizontalProperty { get; }
        public IObservable<IAxisManager<double>> VerticalAxis { get; }
        public AxisItemSelector<double> VerticalAxisItemSelector { get; }
        public IObservable<IBrushMapper> Brush { get; }
        public string HueProperty { get; }
        public GraphLabels Labels { get; }
        public ReadOnlyReactivePropertySlim<bool> SpectrumLoaded { get; }
        public ReactivePropertySlim<bool> IsVisible { get; }
        public ReactivePropertySlim<double> LineThickness { get; }

        public IObservable<(double, double)> GetHorizontalRange() {
            return MsSpectrum.Select(msSpectrum => msSpectrum?.GetSpectrumRange(spec => _horizontalPropertySelector.Selector(spec)) ?? (0d, 1d));
        }

        public void Save(Stream stream) {
            _saveAsObservable.OnNext(stream);
        }

        public IObservable<bool> CanSave { get; }

        public SingleSpectrumModel Product(SingleSpectrumModel other) {
            var productMsSpectrum = MsSpectrum.CombineLatest(other.MsSpectrum, (upper, lower) => upper?.Product(lower, 0.05d)).Publish();
            var upperProductSpectrumModel = new SingleSpectrumModel(productMsSpectrum, _horizontalPropertySelector, _verticalPropertySelector, Brush, HueProperty, Labels, _spectraExporter, SpectrumLoaded);
            upperProductSpectrumModel.Disposables.Add(productMsSpectrum.Connect());
            return upperProductSpectrumModel;
        }

        public SingleSpectrumModel Difference(SingleSpectrumModel other) {
            var differenceMsSpectrum = MsSpectrum.CombineLatest(other.MsSpectrum, (upper, lower) => upper?.Difference(lower, 0.05d)).Publish();
            var upperDifferenceSpectrumModel = new SingleSpectrumModel(differenceMsSpectrum, _horizontalPropertySelector, _verticalPropertySelector, Brush, HueProperty, Labels, _spectraExporter, SpectrumLoaded);
            upperDifferenceSpectrumModel.Disposables.Add(differenceMsSpectrum.Connect());
            return upperDifferenceSpectrumModel;
        }

        public static SingleSpectrumModel Create<T>(
            IObservable<T> targetSource,
            IMsSpectrumLoader<T> loader,
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector,
            PropertySelector<SpectrumPeak, double> verticalPropertySelector,
            IObservable<IBrushMapper> brush,
            string hueProperty,
            GraphLabels graphLabels,
            IObservable<ISpectraExporter> spectraExporter) {

            var disposables = new CompositeDisposable();
            var pairs = targetSource.SelectSwitch(t => loader.LoadMsSpectrumAsObservable(t).Select(s => (spectrum: s, loaded: true)).StartWith((null, false))).Publish();
            var msSpectrum = pairs.Select(p => p.spectrum).ToReadOnlyReactivePropertySlim().AddTo(disposables);
            var loaded = pairs.Select(p => p.loaded).ToReadOnlyReactivePropertySlim().AddTo(disposables);
            SingleSpectrumModel spectrumModel = new SingleSpectrumModel(msSpectrum, horizontalPropertySelector, verticalPropertySelector, brush, hueProperty, graphLabels, spectraExporter, loaded);
            disposables.Add(pairs.Connect());
            spectrumModel.Disposables.Add(disposables);
            return spectrumModel;
        }
    }
}

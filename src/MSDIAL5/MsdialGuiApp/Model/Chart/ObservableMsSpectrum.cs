using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.AxisManager.Generic;
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
    internal sealed class ObservableMsSpectrum : DisposableModelBase {
        private readonly Subject<Stream> _saveAsObservable;
        private readonly IObservable<ISpectraExporter?> _exporter;

        public ObservableMsSpectrum(IObservable<MsSpectrum?> msSpectrum, ReadOnlyReactivePropertySlim<bool>? loaded, IObservable<ISpectraExporter?> exporter) {
            MsSpectrum = msSpectrum ?? throw new ArgumentNullException(nameof(msSpectrum));
            Loaded = loaded ?? Observable.Return(true).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            _exporter = exporter;
            var spectrum = msSpectrum.Select(s => s?.Spectrum ?? new List<SpectrumPeak>(0)).Publish();
            var save = new Subject<Stream>().AddTo(Disposables);
            if (exporter != null) {
                save.Where(s => s != null && s.CanWrite)
                    .WithLatestFrom(exporter.CombineLatest(spectrum), (stream, pair) => (stream, exporter: pair.First, spectrum: pair.Second))
                    .Where(trio => trio.exporter != null && trio.spectrum != null)
                    .Subscribe(trio => trio.exporter!.Save(trio.stream, trio.spectrum))
                    .AddTo(Disposables);
            }
            _saveAsObservable = save;
            CanSave = exporter?.CombineLatest(spectrum)
                .Select(p => p.First != null && p.Second != null)
                .ToReadOnlyReactivePropertySlim(false)
                .AddTo(Disposables)
                ?? Observable.Return(false);
            Disposables.Add(spectrum.Connect());
        }

        public IObservable<MsSpectrum?> MsSpectrum { get; }
        public ReadOnlyReactivePropertySlim<bool> Loaded { get; }
        public IObservable<bool> CanSave { get; }

        public void Save(Stream stream) {
            _saveAsObservable.OnNext(stream);
        }

        public IObservable<(double, double)> GetRange(Func<SpectrumPeak, double> propertySelector) {
            return MsSpectrum.Select(msSpectrum => msSpectrum?.GetSpectrumRange(propertySelector) ?? (0d, 1d));
        }

        public ObservableMsSpectrum Product(ObservableMsSpectrum other) {
            var productMsSpectrum = MsSpectrum.CombineLatest(other.MsSpectrum, (upper, lower) => upper?.Product(lower, 0.05d)).Publish();
            ObservableMsSpectrum observableMsSpectrum = new ObservableMsSpectrum(productMsSpectrum, Loaded, _exporter);
            Disposables.Add(productMsSpectrum.Connect());
            return observableMsSpectrum;
        }

        public ObservableMsSpectrum Difference(ObservableMsSpectrum other) {
            var differenceMsSpectrum = MsSpectrum.CombineLatest(other.MsSpectrum, (upper, lower) => upper?.Difference(lower, 0.05d)).Publish();
            ObservableMsSpectrum observableMsSpectrum = new ObservableMsSpectrum(differenceMsSpectrum, Loaded, _exporter);
            Disposables.Add(differenceMsSpectrum.Connect());
            Disposables.Add(observableMsSpectrum);
            return observableMsSpectrum;
        }

        public AxisPropertySelectors<double> CreateAxisPropertySelectors(PropertySelector<SpectrumPeak, double> propertySelector, string displayLabel, string graphLabel) {
            var axis = GetRange(propertySelector.Selector).ToReactiveContinuousAxisManager(new ConstantMargin(40)).AddTo(Disposables);
            var itemSelector = new AxisItemSelector<double>(new AxisItemModel<double>(displayLabel, axis, graphLabel)).AddTo(Disposables);
            var propertySelectors = new AxisPropertySelectors<double>(itemSelector);
            propertySelectors.Register(propertySelector);
            return propertySelectors;
        }

        public AxisPropertySelectors<double> CreateAxisPropertySelectors2(PropertySelector<SpectrumPeak, double> propertySelector, string graphLabel) {
            var rangeProperty = GetRange(propertySelector.Selector).Publish();
            var continuousAxis = rangeProperty.ToReactiveContinuousAxisManager(new ConstantMargin(0, 30), 0d, 0d, LabelType.Percent).AddTo(Disposables);
            var logAxis = rangeProperty.ToReactiveLogScaleAxisManager(new ConstantMargin(0, 30), 1d, 1d, labelType: LabelType.Percent).AddTo(Disposables);
            var sqrtAxis = rangeProperty.ToReactiveSqrtAxisManager(new ConstantMargin(0, 30), 0, 0, labelType: LabelType.Percent).AddTo(Disposables);
            var axisSelector = new AxisItemSelector<double>(
                    new AxisItemModel<double>("Relative", continuousAxis, $"Relative {graphLabel}"),
                    new AxisItemModel<double>("Log10", logAxis, $"Relative {graphLabel} (log10)"),
                    new AxisItemModel<double>("Sqrt", sqrtAxis, $"Relative {graphLabel} (^1/2)")).AddTo(Disposables);
            Disposables.Add(rangeProperty.Connect());
            var propertySelectors = new AxisPropertySelectors<double>(axisSelector);
            propertySelectors.Register(propertySelector);
            return propertySelectors;
        }

        public static ObservableMsSpectrum Create<T>(IObservable<T?> targetSource, IMsSpectrumLoader<T> loader, ISpectraExporter? spectraExporter) where T: class {
            var disposables = new CompositeDisposable();
            IConnectableObservable<(MsSpectrum? spectrum, bool loaded)> pairs = targetSource.DefaultIfNull(t => loader.LoadScanAsObservable(t).Select(s => (spectrum: (MsSpectrum?)new MsSpectrum(s?.Spectrum ?? new List<SpectrumPeak>(0)), loaded: true)).StartWith((null, false)), Observable.Return<(MsSpectrum? spectrum, bool loaded)>((null, true))).Switch().Publish();
            var msSpectrum = pairs.Select(p => p.spectrum).ToReadOnlyReactivePropertySlim().AddTo(disposables);
            var loaded = pairs.Select(p => p.loaded).ToReadOnlyReactivePropertySlim().AddTo(disposables);
            ObservableMsSpectrum observableMsSpectrum = new ObservableMsSpectrum(msSpectrum, loaded, Observable.Return(spectraExporter));
            disposables.Add(pairs.Connect());
            observableMsSpectrum.Disposables.Add(disposables);
            return observableMsSpectrum;
        }
    }
}

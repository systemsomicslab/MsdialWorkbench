using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
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

        public ObservableMsSpectrum(IObservable<MsSpectrum> msSpectrum, ReadOnlyReactivePropertySlim<bool> loaded, IObservable<ISpectraExporter> exporter) {
            MsSpectrum = msSpectrum ?? throw new ArgumentNullException(nameof(msSpectrum));
            Loaded = loaded ?? Observable.Return(true).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            var spectrum = msSpectrum.Select(s => s?.Spectrum ?? new List<SpectrumPeak>(0)).Publish();
            var save = new Subject<Stream>().AddTo(Disposables);
            save.Where(s => s != null && s.CanWrite)
                .WithLatestFrom(exporter.CombineLatest(spectrum), (stream, pair) => (stream, exporter: pair.First, spectrum: pair.Second))
                .Where(trio => trio.exporter != null && trio.spectrum != null)
                .Subscribe(trio => trio.exporter.Save(trio.stream, trio.spectrum))
                .AddTo(Disposables);
            _saveAsObservable = save;
            CanSave = exporter.CombineLatest(spectrum)
                .Select(p => p.First != null && p.Second != null)
                .ToReadOnlyReactivePropertySlim(false)
                .AddTo(Disposables);
            Disposables.Add(spectrum.Connect());
        }

        public IObservable<MsSpectrum> MsSpectrum { get; }
        public ReadOnlyReactivePropertySlim<bool> Loaded { get; }
        public IObservable<ISpectraExporter> Exporter { get; }
        public IObservable<bool> CanSave { get; }

        public void Save(Stream stream) {
            _saveAsObservable.OnNext(stream);
        }

        public IObservable<(double, double)> GetRange(Func<SpectrumPeak, double> propertySelector) {
            return MsSpectrum.Select(msSpectrum => msSpectrum?.GetSpectrumRange(propertySelector) ?? (0d, 1d));
        }

        public ObservableMsSpectrum Product(ObservableMsSpectrum other) {
            var productMsSpectrum = MsSpectrum.CombineLatest(other.MsSpectrum, (upper, lower) => upper?.Product(lower, 0.05d)).Publish();
            ObservableMsSpectrum observableMsSpectrum = new ObservableMsSpectrum(productMsSpectrum, Loaded, Exporter);
            Disposables.Add(productMsSpectrum.Connect());
            return observableMsSpectrum;
        }

        public ObservableMsSpectrum Difference(ObservableMsSpectrum other) {
            var differenceMsSpectrum = MsSpectrum.CombineLatest(other.MsSpectrum, (upper, lower) => upper?.Difference(lower, 0.05d)).Publish();
            ObservableMsSpectrum observableMsSpectrum = new ObservableMsSpectrum(differenceMsSpectrum, Loaded, Exporter);
            Disposables.Add(differenceMsSpectrum.Connect());
            return observableMsSpectrum;
        }

        public static ObservableMsSpectrum Create<T>(IObservable<T> targetSource, IMsSpectrumLoader<T> loader, IObservable<ISpectraExporter> spectraExporter) {
            var disposables = new CompositeDisposable();
            var pairs = targetSource.SelectSwitch(t => loader.LoadMsSpectrumAsObservable(t).Select(s => (spectrum: s, loaded: true)).StartWith((null, false))).Publish();
            var msSpectrum = pairs.Select(p => p.spectrum).ToReadOnlyReactivePropertySlim().AddTo(disposables);
            var loaded = pairs.Select(p => p.loaded).ToReadOnlyReactivePropertySlim().AddTo(disposables);
            ObservableMsSpectrum observableMsSpectrum = new ObservableMsSpectrum(msSpectrum, loaded, spectraExporter);
            disposables.Add(pairs.Connect());
            observableMsSpectrum.Disposables.Add(disposables);
            return observableMsSpectrum;
        }
    }
}

using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Common;
using CompMs.Graphics.Base;
using CompMs.MsdialCore.Export;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class RawDecSpectrumsModel : DisposableModelBase
    {
        public RawDecSpectrumsModel(MsSpectrumModel rawRefSpectrumModels, MsSpectrumModel decRefSpectrumModels, MultiMsRawSpectrumLoader loader) {
            RawRefSpectrumModels = rawRefSpectrumModels ?? throw new ArgumentNullException(nameof(rawRefSpectrumModels));
            DecRefSpectrumModels = decRefSpectrumModels ?? throw new ArgumentNullException(nameof(decRefSpectrumModels));
            RawLoader = loader;
        }

        public MsSpectrumModel RawRefSpectrumModels { get; }
        public MsSpectrumModel DecRefSpectrumModels { get; }
        public MultiMsRawSpectrumLoader RawLoader { get; }

        public static RawDecSpectrumsModel Create<T>(
            IObservable<T> targetSource,
            IMsSpectrumLoader<T> rawLoader,
            IMsSpectrumLoader<T> decLoader,
            IObservable<MsSpectrum> refMsSpectrum,
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector,
            PropertySelector<SpectrumPeak, double> verticalPropertySelector,
            GraphLabels graphLabels,
            string hueProperty,
            IObservable<IBrushMapper> upperSpectrumBrush,
            IObservable<IBrushMapper> lowerSpectrumBrush,
            IObservable<ISpectraExporter> rawSpectraExporeter,
            IObservable<ISpectraExporter> deconvolutedSpectraExporter,
            IObservable<ISpectraExporter> referenceSpectraExporter,
            IObservable<Ms2ScanMatching> ms2ScanMatching) {

            var disposables = new DisposableCollection();
            (var rawMsSpectrum, var rawSpectrumLoaded, var rawDisposable) = Load(targetSource, rawLoader, disposables);
            SingleSpectrumModel rawSpectrumModel = new SingleSpectrumModel(rawMsSpectrum, horizontalPropertySelector, verticalPropertySelector, upperSpectrumBrush, hueProperty, graphLabels, rawSpectraExporeter, rawSpectrumLoaded).AddTo(disposables);
            disposables.Add(rawDisposable);

            (var decMsSpectrum, var decSpectrumLoaded, var decDisposable) = Load(targetSource, decLoader, disposables);
            SingleSpectrumModel decSpectrumModel = new SingleSpectrumModel(decMsSpectrum, horizontalPropertySelector, verticalPropertySelector, upperSpectrumBrush, hueProperty, graphLabels, deconvolutedSpectraExporter, decSpectrumLoaded).AddTo(disposables);
            disposables.Add(decDisposable);

            var refMsSpectrum_ = refMsSpectrum.Publish();
            SingleSpectrumModel referenceSpectrumModel = new SingleSpectrumModel(refMsSpectrum_, horizontalPropertySelector, verticalPropertySelector, lowerSpectrumBrush, hueProperty, graphLabels, referenceSpectraExporter, new ReadOnlyReactivePropertySlim<bool>(Observable.Return(true)).AddTo(disposables)).AddTo(disposables);
            disposables.Add(refMsSpectrum_.Connect());

            var rawRefSpectrumModels = new MsSpectrumModel(rawSpectrumModel, referenceSpectrumModel, graphLabels, ms2ScanMatching).AddTo(disposables);
            var decRefSpectrumModels = new MsSpectrumModel(decSpectrumModel, referenceSpectrumModel, graphLabels, ms2ScanMatching).AddTo(disposables);
            var result = new RawDecSpectrumsModel(rawRefSpectrumModels, decRefSpectrumModels, rawLoader as MultiMsRawSpectrumLoader);
            result.Disposables.Add(disposables);
            return result;
        }

        private static (ReadOnlyReactivePropertySlim<MsSpectrum>, ReadOnlyReactivePropertySlim<bool>, IDisposable) Load<T>(IObservable<T> targetSource, IMsSpectrumLoader<T> rawLoader, DisposableCollection disposables) {
            var pairs = targetSource.SelectSwitch(t => rawLoader.LoadMsSpectrumAsObservable(t).Select(s => (s, true)).StartWith((null, false))).Publish();
            var msSpectrum = pairs.Select(p => p.Item1).ToReadOnlyReactivePropertySlim().AddTo(disposables);
            var loaded = pairs.Select(p => p.Item2).ToReadOnlyReactivePropertySlim().AddTo(disposables);
            disposables.Add(pairs.Connect());
            return (msSpectrum, loaded, pairs.Connect());
        }
    }
}

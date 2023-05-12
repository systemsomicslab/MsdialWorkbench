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
        public RawDecSpectrumsModel(
            IObservable<ChromatogramPeakFeatureModel> targetSource,
            MultiMsRawSpectrumLoader rawLoader,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> decLoader,
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

            RawLoader = rawLoader;

            (var rawMsSpectrum, var rawSpectrumLoaded, var rawDisposable) = Load(targetSource, rawLoader, Disposables);
            SingleSpectrumModel rawSpectrumModel = new SingleSpectrumModel(rawMsSpectrum, horizontalPropertySelector, verticalPropertySelector, upperSpectrumBrush, hueProperty, graphLabels, rawSpectraExporeter, rawSpectrumLoaded).AddTo(Disposables);
            Disposables.Add(rawDisposable);

            (var decMsSpectrum, var decSpectrumLoaded, var decDisposable) = Load(targetSource, decLoader, Disposables);
            SingleSpectrumModel decSpectrumModel = new SingleSpectrumModel(decMsSpectrum, horizontalPropertySelector, verticalPropertySelector, upperSpectrumBrush, hueProperty, graphLabels, deconvolutedSpectraExporter, decSpectrumLoaded).AddTo(Disposables);
            Disposables.Add(decDisposable);

            var refMsSpectrum_ = refMsSpectrum.Publish();
            SingleSpectrumModel referenceSpectrumModel = new SingleSpectrumModel(refMsSpectrum_, horizontalPropertySelector, verticalPropertySelector, lowerSpectrumBrush, hueProperty, graphLabels, referenceSpectraExporter, new ReadOnlyReactivePropertySlim<bool>(Observable.Return(true)).AddTo(Disposables)).AddTo(Disposables);
            Disposables.Add(refMsSpectrum_.Connect());

            RawRefSpectrumModels = new MsSpectrumModel(rawSpectrumModel, referenceSpectrumModel, graphLabels, ms2ScanMatching).AddTo(Disposables);
            DecRefSpectrumModels = new MsSpectrumModel(decSpectrumModel, referenceSpectrumModel, graphLabels, ms2ScanMatching).AddTo(Disposables);
        }

        public MsSpectrumModel RawRefSpectrumModels { get; }
        public MsSpectrumModel DecRefSpectrumModels { get; }
        public MultiMsRawSpectrumLoader RawLoader { get; }

        private static (ReadOnlyReactivePropertySlim<MsSpectrum>, ReadOnlyReactivePropertySlim<bool>, IDisposable) Load(IObservable<ChromatogramPeakFeatureModel> targetSource, IMsSpectrumLoader<ChromatogramPeakFeatureModel> rawLoader, DisposableCollection disposables) {
            var pairs = targetSource.SelectSwitch(t => rawLoader.LoadMsSpectrumAsObservable(t).Select(s => (s, true)).StartWith((null, false))).Publish();
            var msSpectrum = pairs.Select(p => p.Item1).ToReadOnlyReactivePropertySlim().AddTo(disposables);
            var loaded = pairs.Select(p => p.Item2).ToReadOnlyReactivePropertySlim().AddTo(disposables);
            disposables.Add(pairs.Connect());
            return (msSpectrum, loaded, pairs.Connect());
        }
    }
}

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
            Disposables.Add(rawDisposable);

            (var decMsSpectrum, var decSpectrumLoaded, var decDisposable) = Load(targetSource, decLoader, Disposables);
            Disposables.Add(decDisposable);

            var refMsSpectrum_ = refMsSpectrum.Publish();
            RawRefSpectrumModels = new MsSpectrumModel(
                rawMsSpectrum, refMsSpectrum_,
                horizontalPropertySelector,
                verticalPropertySelector,
                graphLabels,
                hueProperty,
                upperSpectrumBrush,
                lowerSpectrumBrush,
                rawSpectraExporeter,
                referenceSpectraExporter,
                rawSpectrumLoaded,
                ms2ScanMatching).AddTo(Disposables);
            DecRefSpectrumModels = new MsSpectrumModel(
                decMsSpectrum, refMsSpectrum_,
                horizontalPropertySelector,
                verticalPropertySelector,
                graphLabels,
                hueProperty,
                upperSpectrumBrush,
                lowerSpectrumBrush,
                deconvolutedSpectraExporter,
                referenceSpectraExporter,
                decSpectrumLoaded,
                ms2ScanMatching).AddTo(Disposables);

            Disposables.Add(refMsSpectrum_.Connect());
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

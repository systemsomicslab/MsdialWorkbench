using CompMs.App.Msdial.Model.Loader;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Export;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class RawDecSpectrumsModel : DisposableModelBase
    {
        public RawDecSpectrumsModel(MsSpectrumModel rawRefSpectrumModels, MsSpectrumModel decRefSpectrumModels, MultiMsmsRawSpectrumLoader loader) {
            RawRefSpectrumModels = rawRefSpectrumModels ?? throw new ArgumentNullException(nameof(rawRefSpectrumModels));
            DecRefSpectrumModels = decRefSpectrumModels ?? throw new ArgumentNullException(nameof(decRefSpectrumModels));
            RawLoader = loader;
        }

        public MsSpectrumModel RawRefSpectrumModels { get; }
        public MsSpectrumModel DecRefSpectrumModels { get; }
        public MultiMsmsRawSpectrumLoader RawLoader { get; }

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

            var disposables = new CompositeDisposable();
            SingleSpectrumModel rawSpectrumModel = SingleSpectrumModel.Create(targetSource, rawLoader, horizontalPropertySelector, verticalPropertySelector, upperSpectrumBrush, hueProperty, graphLabels, rawSpectraExporeter).AddTo(disposables);
            SingleSpectrumModel decSpectrumModel = SingleSpectrumModel.Create(targetSource, decLoader, horizontalPropertySelector, verticalPropertySelector, upperSpectrumBrush, hueProperty, graphLabels, deconvolutedSpectraExporter).AddTo(disposables);

            var refMsSpectrum_ = refMsSpectrum.Publish();
            ReadOnlyReactivePropertySlim<bool> spectrumLoaded = new ReadOnlyReactivePropertySlim<bool>(Observable.Return(true)).AddTo(disposables);
            SingleSpectrumModel referenceSpectrumModel = new SingleSpectrumModel(refMsSpectrum_, horizontalPropertySelector, verticalPropertySelector, lowerSpectrumBrush, hueProperty, graphLabels, referenceSpectraExporter, spectrumLoaded).AddTo(disposables);
            disposables.Add(refMsSpectrum_.Connect());

            var rawRefSpectrumModels = new MsSpectrumModel(rawSpectrumModel, referenceSpectrumModel, graphLabels, ms2ScanMatching).AddTo(disposables);
            var decRefSpectrumModels = new MsSpectrumModel(decSpectrumModel, referenceSpectrumModel, graphLabels, ms2ScanMatching).AddTo(disposables);
            var result = new RawDecSpectrumsModel(rawRefSpectrumModels, decRefSpectrumModels, rawLoader as MultiMsmsRawSpectrumLoader);
            result.Disposables.Add(disposables);
            return result;
        }
    }
}

using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.MsdialCore.Export;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    class RawDecSpectrumsModel : DisposableModelBase
    {
        public RawDecSpectrumsModel(
            IObservable<ChromatogramPeakFeatureModel> targetSource,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> rawLoader,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> decLoader,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> refLoader,
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector,
            PropertySelector<SpectrumPeak, double> verticalPropertySelector,
            GraphLabels graphLabels,
            string hueProperty,
            IObservable<IBrushMapper> upperSpectrumBrush,
            IObservable<IBrushMapper> lowerSpectrumBrush,
            IObservable<ISpectraExporter> rawSpectraExporeter,
            IObservable<ISpectraExporter> deconvolutedSpectraExporter,
            IObservable<ISpectraExporter> referenceSpectraExporter) {

            var rawSource = targetSource.Select(target => Observable.FromAsync(token => rawLoader.LoadSpectrumAsync(target, token))).Switch();
            var decSource = targetSource.Select(target => Observable.FromAsync(token => decLoader.LoadSpectrumAsync(target, token))).Switch();
            var refSource = targetSource.Select(target => Observable.FromAsync(token => refLoader.LoadSpectrumAsync(target, token))).Switch();

            RawRefSpectrumModels = new MsSpectrumModel(
                rawSource, refSource,
                horizontalPropertySelector,
                verticalPropertySelector,
                graphLabels,
                hueProperty,
                upperSpectrumBrush,
                lowerSpectrumBrush,
                rawSpectraExporeter,
                referenceSpectraExporter).AddTo(Disposables);
            DecRefSpectrumModels = new MsSpectrumModel(
                decSource, refSource,
                horizontalPropertySelector,
                verticalPropertySelector,
                graphLabels,
                hueProperty,
                upperSpectrumBrush,
                lowerSpectrumBrush,
                deconvolutedSpectraExporter,
                referenceSpectraExporter).AddTo(Disposables);
        }

        public MsSpectrumModel RawRefSpectrumModels { get; }
        public MsSpectrumModel DecRefSpectrumModels { get; }
    }
}

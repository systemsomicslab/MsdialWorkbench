using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.MsdialCore.Export;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class RawDecSpectrumsModel : DisposableModelBase
    {
        public RawDecSpectrumsModel(
            IObservable<ChromatogramPeakFeatureModel> targetSource,
            MultiMsRawSpectrumLoader rawLoader,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> decLoader,
            IObservable<List<SpectrumPeak>> refSpectrum,
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector,
            PropertySelector<SpectrumPeak, double> verticalPropertySelector,
            GraphLabels graphLabels,
            string hueProperty,
            IObservable<IBrushMapper> upperSpectrumBrush,
            IObservable<IBrushMapper> lowerSpectrumBrush,
            IObservable<ISpectraExporter> rawSpectraExporeter,
            IObservable<ISpectraExporter> deconvolutedSpectraExporter,
            IObservable<ISpectraExporter> referenceSpectraExporter)
            : this(targetSource,
                  (IMsSpectrumLoader<ChromatogramPeakFeatureModel>)rawLoader,
                  decLoader,
                  refSpectrum,
                  horizontalPropertySelector, verticalPropertySelector,
                  graphLabels,
                  hueProperty, upperSpectrumBrush, lowerSpectrumBrush,
                  rawSpectraExporeter, deconvolutedSpectraExporter, referenceSpectraExporter) {
            RawLoader = rawLoader;
        }

        public RawDecSpectrumsModel(
            IObservable<ChromatogramPeakFeatureModel> targetSource,
            MultiMsRawSpectrumLoader rawLoader,
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
            IObservable<ISpectraExporter> referenceSpectraExporter)
            : this(targetSource,
                  (IMsSpectrumLoader<ChromatogramPeakFeatureModel>)rawLoader,
                  decLoader,
                  targetSource.WithLatestFrom(Observable.Return(refLoader),
                    (target, loader) => loader.LoadSpectrumAsObservable(target))
                    .Switch(),
                  horizontalPropertySelector, verticalPropertySelector,
                  graphLabels,
                  hueProperty, upperSpectrumBrush, lowerSpectrumBrush,
                  rawSpectraExporeter, deconvolutedSpectraExporter, referenceSpectraExporter) {
            RawLoader = rawLoader;
        }

        public RawDecSpectrumsModel(
            IObservable<ChromatogramPeakFeatureModel> targetSource,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> rawLoader,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> decLoader,
            IObservable<List<SpectrumPeak>> refSpectrum,
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector,
            PropertySelector<SpectrumPeak, double> verticalPropertySelector,
            GraphLabels graphLabels,
            string hueProperty,
            IObservable<IBrushMapper> upperSpectrumBrush,
            IObservable<IBrushMapper> lowerSpectrumBrush,
            IObservable<ISpectraExporter> rawSpectraExporeter,
            IObservable<ISpectraExporter> deconvolutedSpectraExporter,
            IObservable<ISpectraExporter> referenceSpectraExporter) {

            var rawSource = targetSource.WithLatestFrom(Observable.Return(rawLoader),
                (target, loader) => loader.LoadSpectrumAsObservable(target))
                .Switch()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var rawSpectrumLoaded = new[]
            {
                targetSource.ToConstant(false),
                rawSource.Delay(TimeSpan.FromSeconds(.05d)).ToConstant(true),
            }.Merge()
            .Throttle(TimeSpan.FromSeconds(.1d))
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
            var decSource = targetSource.WithLatestFrom(Observable.Return(decLoader),
                (target, loader) => loader.LoadSpectrumAsObservable(target))
                .Switch()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var decSpectrumLoaded = new[]
            {
                targetSource.ToConstant(false),
                decSource.Delay(TimeSpan.FromSeconds(.05d)).ToConstant(true),
            }.Merge()
            .Throttle(TimeSpan.FromSeconds(.1d))
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            RawRefSpectrumModels = new MsSpectrumModel(
                rawSource, refSpectrum,
                horizontalPropertySelector,
                verticalPropertySelector,
                graphLabels,
                hueProperty,
                upperSpectrumBrush,
                lowerSpectrumBrush,
                rawSpectraExporeter,
                referenceSpectraExporter,
                rawSpectrumLoaded).AddTo(Disposables);
            DecRefSpectrumModels = new MsSpectrumModel(
                decSource, refSpectrum,
                horizontalPropertySelector,
                verticalPropertySelector,
                graphLabels,
                hueProperty,
                upperSpectrumBrush,
                lowerSpectrumBrush,
                deconvolutedSpectraExporter,
                referenceSpectraExporter,
                decSpectrumLoaded).AddTo(Disposables);
        }


        public MsSpectrumModel RawRefSpectrumModels { get; }
        public MsSpectrumModel DecRefSpectrumModels { get; }
        public MultiMsRawSpectrumLoader RawLoader { get; }
    }
}

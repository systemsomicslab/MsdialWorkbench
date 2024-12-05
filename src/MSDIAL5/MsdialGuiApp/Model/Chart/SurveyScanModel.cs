using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class SurveyScanModel : DisposableModelBase
    {
        public SurveyScanModel(
            SurveyScanSpectrum surveyScanSpectrum,
            Func<SpectrumPeakWrapper, double> horizontalSelector,
            Func<SpectrumPeakWrapper, double> verticalSelector) {

            SurveyScanLoaded = surveyScanSpectrum.Loaded;
            SpectrumSource = surveyScanSpectrum.Spectrum;

            HorizontalSelector = horizontalSelector;
            VerticalSelector = verticalSelector;

            var nospec = SpectrumSource.Where(spec => !spec.Any()).Publish();
            var anyspec = SpectrumSource.Where(spec => spec.Any()).Publish();
            var hrox = anyspec
                .Select(spec => new AxisRange(spec.Min(HorizontalSelector), spec.Max(HorizontalSelector)));
            var vrox = anyspec
                .Select(spec => new AxisRange(spec.Min(VerticalSelector), spec.Max(VerticalSelector)));

            HorizontalRangeSource = hrox.Merge(nospec.ToConstant(new AxisRange(0, 1))).ToReactiveProperty(new AxisRange(0d, 1d)).AddTo(Disposables);
            VerticalRangeSource = vrox.Merge(nospec.ToConstant(new AxisRange(0, 1))).ToReactiveProperty(new AxisRange(0d, 1d)).AddTo(Disposables);

            var maxIntensitySource = anyspec
                .Select(spectrum => spectrum.Max(spec => spec?.Intensity) ?? 0d)
                .Merge(nospec.ToConstant(0d));

            var splashKey = anyspec
                .Select(spectrum => spectrum.CalculateSplashKey())
                .Merge(nospec.ToConstant("N/A"));

            maxIntensitySource
                .Zip(splashKey, (intensity, key) => string.Format("MS1 spectra max intensity: {0}\n{1}", intensity, key))
                .Subscribe(title => Elements.GraphTitle = title)
                .AddTo(Disposables);

            Disposables.Add(nospec.Connect());
            Disposables.Add(anyspec.Connect());
        }

        public ReadOnlyReactivePropertySlim<bool> SurveyScanLoaded { get; }

        public IObservable<List<SpectrumPeakWrapper>> SpectrumSource { get; }

        public IObservable<AxisRange> HorizontalRangeSource { get; }

        public IObservable<AxisRange> VerticalRangeSource { get; }

        public GraphElements Elements { get; } = new GraphElements();

        public Func<SpectrumPeakWrapper, double> HorizontalSelector { get; } 

        public Func<SpectrumPeakWrapper, double> VerticalSelector { get; }
    }
}

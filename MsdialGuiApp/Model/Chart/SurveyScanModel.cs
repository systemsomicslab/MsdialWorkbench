using CompMs.App.Msdial.Model.DataObj;
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
    class SurveyScanModel : DisposableModelBase
    {
        public SurveyScanModel(
            IObservable<List<SpectrumPeakWrapper>> spectrumSource,
            Func<SpectrumPeakWrapper, double> horizontalSelector,
            Func<SpectrumPeakWrapper, double> verticalSelector) {

            SpectrumSource = spectrumSource.ToReadOnlyReactivePropertySlim(initialValue: new List<SpectrumPeakWrapper>(0)).AddTo(Disposables);

            HorizontalSelector = horizontalSelector;
            VerticalSelector = verticalSelector;

            var nospec = SpectrumSource.Where(spec => !spec.Any()).Select(_ => new Range(0, 1));

            var anyspec = SpectrumSource.Where(spec => spec.Any());
            var hrox = anyspec
                .Select(spec => new Range(spec.Min(HorizontalSelector), spec.Max(HorizontalSelector)));
            var vrox = anyspec
                .Select(spec => new Range(spec.Min(VerticalSelector), spec.Max(VerticalSelector)));

            HorizontalRangeSource = hrox.Merge(nospec).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            VerticalRangeSource = vrox.Merge(nospec).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            var maxIntensitySource = anyspec
                .Select(spectrum => spectrum.Max(spec => spec?.Intensity) ?? 0d)
                .Merge(nospec.Select(_ => 0d));

            var splashKey = anyspec
                .Select(spectrum => spectrum.CalculateSplashKey())
                .Merge(nospec.Select(_ => "N/A"));

            maxIntensitySource
                .Zip(splashKey, (intensity, key) => string.Format("MS1 spectra max intensity: {0}\n{1}", intensity, key))
                .Subscribe(title => Elements.GraphTitle = title)
                .AddTo(Disposables);
        }

        public IObservable<List<SpectrumPeakWrapper>> SpectrumSource { get; }

        public IObservable<Range> HorizontalRangeSource { get; }

        public IObservable<Range> VerticalRangeSource { get; }

        public GraphElements Elements { get; } = new GraphElements();

        public Func<SpectrumPeakWrapper, double> HorizontalSelector { get; } 

        public Func<SpectrumPeakWrapper, double> VerticalSelector { get; }
    }
}

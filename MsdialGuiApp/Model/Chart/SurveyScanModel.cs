using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    class SurveyScanModel : BindableBase, IDisposable
    {
        public SurveyScanModel(
            IObservable<List<SpectrumPeakWrapper>> spectrumSource,
            Func<SpectrumPeakWrapper, double> horizontalSelector,
            Func<SpectrumPeakWrapper, double> verticalSelector) {

            SpectrumSource = spectrumSource;
            SpectrumSource.Subscribe(spectrum => Spectrum = spectrum);

            HorizontalSelector = horizontalSelector;
            VerticalSelector = verticalSelector;

            var nospec = SpectrumSource.Where(spec => !spec.Any()).Select(_ => new Range(0, 1));

            var anyspec = SpectrumSource.Where(spec => spec.Any());
            var hrox = anyspec
                .Select(spec => new Range(spec.Min(HorizontalSelector), spec.Max(HorizontalSelector)));
            var vrox = anyspec
                .Select(spec => new Range(spec.Min(VerticalSelector), spec.Max(VerticalSelector)));

            HorizontalRangeSource = hrox.Merge(nospec);
            VerticalRangeSource = vrox.Merge(nospec);

            var maxIntensitySource = anyspec
                .Select(spectrum => spectrum.Max(spec => spec?.Intensity) ?? 0d)
                .Merge(nospec.Select(_ => 0d));

            var splashKey = anyspec
                .Select(spectrum => spectrum.CalculateSplashKey())
                .Merge(nospec.Select(_ => "N/A"));

            titleUnsubscriber = maxIntensitySource
                .Zip(splashKey, (intensity, key) => string.Format("MS1 spectra max intensity: {0}\n{1}", intensity, key))
                .Subscribe(title => Elements.GraphTitle = title);
        }

        private IDisposable titleUnsubscriber;

        public List<SpectrumPeakWrapper> Spectrum {
            get => spectrum;
            set => SetProperty(ref spectrum, value);
        }
        private List<SpectrumPeakWrapper> spectrum;

        public IObservable<List<SpectrumPeakWrapper>> SpectrumSource { get; }

        public IObservable<Range> HorizontalRangeSource { get; }

        public IObservable<Range> VerticalRangeSource { get; }

        public GraphElements Elements { get; } = new GraphElements();

        public Func<SpectrumPeakWrapper, double> HorizontalSelector { get; } 

        public Func<SpectrumPeakWrapper, double> VerticalSelector { get; }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    titleUnsubscriber.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

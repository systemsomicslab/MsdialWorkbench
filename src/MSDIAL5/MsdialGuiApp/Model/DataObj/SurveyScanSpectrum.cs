using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal class SurveyScanSpectrum : DisposableModelBase
    {
        public SurveyScanSpectrum(IObservable<ChromatogramPeakFeatureModel?> selectedPeak, Func<ChromatogramPeakFeatureModel, IObservable<List<SpectrumPeakWrapper>>> loadSpectrum) {
            Spectrum = selectedPeak
                .DefaultIfNull(loadSpectrum, Observable.Return(new List<SpectrumPeakWrapper>(0)))
                .Switch()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            Loaded = new[]
            {
                selectedPeak.ToConstant(false),
                Spectrum.Delay(TimeSpan.FromSeconds(.1d)).ToConstant(true),
            }.Merge()
            .Throttle(TimeSpan.FromSeconds(.3d))
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        private SurveyScanSpectrum(ReadOnlyReactivePropertySlim<List<SpectrumPeakWrapper>> spectrum, ReadOnlyReactivePropertySlim<bool> loaded) {
            Spectrum = spectrum;
            Loaded = loaded;
        }

        public ReadOnlyReactivePropertySlim<List<SpectrumPeakWrapper>> Spectrum { get; }
        public ReadOnlyReactivePropertySlim<bool> Loaded { get; }

        public static SurveyScanSpectrum Create<T>(IObservable<T> selectedFeature, Func<T, IObservable<List<SpectrumPeakWrapper>>> loadSpectrum) {
            var Spectrum = selectedFeature
                .SelectSwitch(loadSpectrum)
                .ToReadOnlyReactivePropertySlim();

            var Loaded = new[]
            {
                selectedFeature.ToConstant(false),
                Spectrum.Delay(TimeSpan.FromSeconds(.1d)).ToConstant(true),
            }.Merge()
            .Throttle(TimeSpan.FromSeconds(.3d))
            .ToReadOnlyReactivePropertySlim();

            var result = new SurveyScanSpectrum(Spectrum, Loaded);
            result.Disposables.Add(Spectrum);
            result.Disposables.Add(Loaded);
            return result;
        }
    }
}

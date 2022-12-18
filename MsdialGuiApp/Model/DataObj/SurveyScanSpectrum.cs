using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal class SurveyScanSpectrum : DisposableModelBase
    {
        public SurveyScanSpectrum(IObservable<ChromatogramPeakFeatureModel> selectedPeak, Func<ChromatogramPeakFeatureModel, IObservable<List<SpectrumPeakWrapper>>> loadSpectrum) {
            Spectrum = selectedPeak
                .SelectSwitch(loadSpectrum)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            Loaded = new[]
            {
                selectedPeak.Select(_ => false),
                Spectrum.Delay(TimeSpan.FromSeconds(.1d)).Select(_ => true),
            }.Merge()
            .Throttle(TimeSpan.FromSeconds(.3d))
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<List<SpectrumPeakWrapper>> Spectrum { get; }
        public ReadOnlyReactivePropertySlim<bool> Loaded { get; }
    }
}

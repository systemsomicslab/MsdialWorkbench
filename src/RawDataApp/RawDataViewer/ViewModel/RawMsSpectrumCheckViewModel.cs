using CompMs.App.RawDataViewer.Model;
using CompMs.Common.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.RawDataViewer.ViewModel
{
    public class RawMsSpectrumCheckViewModel : ViewModelBase
    {
        public RawMsSpectrumCheckViewModel(RawMsSpectrumCheckModel model) {
            Model = model ?? throw new ArgumentNullException(nameof(model));

            Spectra = Model.Spectra;
            SelectedSpectrum = Model.ToReactivePropertySlimAsSynchronized(m => m.SelectedSpectrum)
                .AddTo(Disposables);

            Spectrum = SelectedSpectrum
                .Select(spectrum => spectrum?.Spectrum.Select(s => new DataPoint { X = s.Mz, Y = s.Intensity, }).ToList().AsReadOnly())
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            var spectrumOx = SelectedSpectrum
                .Select(spectrum => spectrum?.Spectrum)
                .Where(spectrum => !(spectrum is null || spectrum.Length == 0));

            HorizontalAxis = spectrumOx
                .Select(spectrum => (spectrum.Min(v => v.Mz), spectrum.Max(v => v.Mz)))
                .ToReactiveContinuousAxisManager(new ConstantMargin(10))
                .AddTo(Disposables);
            VerticalAxis = spectrumOx
                .Select(spectrum => (spectrum.Min(v => v.Intensity), spectrum.Max(v => v.Intensity)))
                .ToReactiveContinuousAxisManager(new ConstantMargin(0, 20), new Graphics.Core.Base.AxisRange(0d, 0d))
                .AddTo(Disposables);
        }

        public RawMsSpectrumCheckModel Model { get; }

        public ReadOnlyCollection<RawSpectrum> Spectra { get; }
        public ReactivePropertySlim<RawSpectrum> SelectedSpectrum { get; }

        public ReadOnlyReactivePropertySlim<ReadOnlyCollection<DataPoint>> Spectrum { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; }
    }
}

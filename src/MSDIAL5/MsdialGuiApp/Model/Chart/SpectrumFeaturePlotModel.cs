using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class SpectrumFeaturePlotModel : DisposableModelBase
    {
        public SpectrumFeaturePlotModel(Ms1BasedSpectrumFeatureCollection spectra, ObservableCollection<ChromatogramPeakFeatureModel> peaks, BrushMapDataSelector<ChromatogramPeakFeatureModel> brushMapDataSelector, ReadOnlyReactivePropertySlim<string> label) {
            Spectra = spectra.Items;
            SelectedSpectrum = spectra.SelectedSpectrum;

            BrushMapDataSelector = brushMapDataSelector;
            Label = label;

            ChromatogramPeaks = new ReadOnlyObservableCollection<ChromatogramPeakFeatureModel>(peaks);
            SelectedChromatogramPeak = new ReactivePropertySlim<ChromatogramPeakFeatureModel>().AddTo(Disposables);


            HorizontalLabel = new ReactivePropertySlim<string>("Retention time [min]").AddTo(Disposables);
            VerticalLabel = new ReactivePropertySlim<string>("m/z").AddTo(Disposables);

            Title = new[]
            {
                spectra.SelectedSpectrum.Where(s => s is not null)
                    .Select(s => $"Scan: {s!.Scan.ScanID} RT: {s.Scan.ChromXs.RT.Value} min Quant mass: m/z {s.QuantifiedChromatogramPeak.PeakFeature.Mass}"),
                SelectedChromatogramPeak.Where(p => p is not null)
                    .Select(p => $"ID: {p.MasterPeakID} Scan: {p.MS1RawSpectrumIdTop} RT: {p.RT.Value} min Mass: m/z {p.Mass}"),
            }.Merge()
            .ToReadOnlyReactivePropertySlim(string.Empty)
            .AddTo(Disposables);

            HorizontalAxis = peaks.CollectionChangedAsObservable().ToUnit().StartWith(Unit.Default).Throttle(TimeSpan.FromSeconds(.01d))
                .Select(_ => peaks.Any() ? new AxisRange(peaks.Min(p => p.RT.Value), peaks.Max(p => p.RT.Value)) : new AxisRange(0, 1))
                .ToReactiveContinuousAxisManager<double>(new RelativeMargin(0.05))
                .AddTo(Disposables);
            VerticalAxis = peaks.CollectionChangedAsObservable().ToUnit().StartWith(Unit.Default).Throttle(TimeSpan.FromSeconds(.01d))
                .Select(_ => peaks.Any() ? new AxisRange(peaks.Min(p => p.Mass), peaks.Max(p => p.Mass)) : new AxisRange(0, 1))
                .ToReactiveContinuousAxisManager<double>(new RelativeMargin(0.05))
                .AddTo(Disposables);
        }

        public ReactivePropertySlim<Ms1BasedSpectrumFeature?> SelectedSpectrum { get; }
        public ReadOnlyObservableCollection<Ms1BasedSpectrumFeature> Spectra { get; }
        public ReactivePropertySlim<ChromatogramPeakFeatureModel> SelectedChromatogramPeak { get; }
        public ReadOnlyObservableCollection<ChromatogramPeakFeatureModel> ChromatogramPeaks { get; }

        public IAxisManager<double> HorizontalAxis { get; }
        public IAxisManager<double> VerticalAxis { get; }
        public ReactivePropertySlim<string> HorizontalLabel { get; }
        public ReactivePropertySlim<string> VerticalLabel { get; }
        public ReadOnlyReactivePropertySlim<string> Title { get; }
        public BrushMapDataSelector<ChromatogramPeakFeatureModel> BrushMapDataSelector { get; }
        public ReadOnlyReactivePropertySlim<string> Label { get; }
    }
}

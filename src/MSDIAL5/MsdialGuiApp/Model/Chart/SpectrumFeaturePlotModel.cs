using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class SpectrumFeaturePlotModel : DisposableModelBase
    {
        public SpectrumFeaturePlotModel(Ms1BasedSpectrumFeatureCollection spectra, ObservableCollection<ChromatogramPeakFeatureModel> peaks, BrushMapDataSelector<ChromatogramPeakFeatureModel> brushMapDataSelector) {
            Spectra = new ReadOnlyObservableCollection<Ms1BasedSpectrumFeature>(spectra.Items);
            SelectedSpectrum = new ReactiveProperty<Ms1BasedSpectrumFeature>().AddTo(Disposables);

            ChromatogramPeaks = new ReadOnlyObservableCollection<ChromatogramPeakFeatureModel>(peaks);
            SelectedChromatogramPeak = new ReactivePropertySlim<ChromatogramPeakFeatureModel>().AddTo(Disposables);


            HorizontalLabel = new ReactivePropertySlim<string>("Retention time [min]").AddTo(Disposables);
            VerticalLabel = new ReactivePropertySlim<string>("m/z").AddTo(Disposables);

            Title = new[]
            {
                SelectedSpectrum.Where(s => s != null)
                    .Select(s => $"Scan: {s.Scan.ScanID} RT: {s.Scan.ChromXs.RT.Value} min Quant mass: m/z {s.QuantifiedChromatogramPeak.PeakFeature.Mass}"),
                SelectedChromatogramPeak.Where(p => p != null)
                    .Select(p => $"ID: {p.MasterPeakID} Scan: {p.MS1RawSpectrumIdTop} RT: {p.RT.Value} min Mass: m/z {p.Mass}"),
            }.Merge()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            VerticalAxis = ContinuousAxisManager<double>.Build(peaks, p => p.Mass).AddTo(Disposables);
            HorizontalAxis = ContinuousAxisManager<double>.Build(peaks, p => p.RT.Value).AddTo(Disposables);
            BrushMapDataSelector = brushMapDataSelector;
        }

        public ReactivePropertySlim<object> SelectedSpectrumWrapper { get; }
        public ReadOnlyObservableCollection<object> SpectraWrapper { get; }
        public ReactiveProperty<Ms1BasedSpectrumFeature> SelectedSpectrum { get; }
        public ReadOnlyObservableCollection<Ms1BasedSpectrumFeature> Spectra { get; }
        public ReactivePropertySlim<ChromatogramPeakFeatureModel> SelectedChromatogramPeak { get; }
        public ReadOnlyObservableCollection<ChromatogramPeakFeatureModel> ChromatogramPeaks { get; }

        public IAxisManager<double> HorizontalAxis { get; }
        public IAxisManager<double> VerticalAxis { get; }
        public ReactivePropertySlim<string> HorizontalLabel { get; }
        public ReactivePropertySlim<string> VerticalLabel { get; }
        public ReadOnlyReactivePropertySlim<string> Title { get; }
        public BrushMapDataSelector<ChromatogramPeakFeatureModel> BrushMapDataSelector { get; }

        class SpectrumWrapper : BindableBase {
            public SpectrumWrapper(Ms1BasedSpectrumFeature feature) {
                Feature = feature;
                ChromXValue = feature.Scan.ChromXs.RT.Value;
            }

            public double ChromXValue { get; }
            public Ms1BasedSpectrumFeature Feature { get; }
        }
    }
}

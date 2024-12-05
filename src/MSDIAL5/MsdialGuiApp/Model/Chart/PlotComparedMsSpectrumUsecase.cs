using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Export;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class PlotComparedMsSpectrumUsecase : IDisposable
    {
        private CompositeDisposable? _disposables = new CompositeDisposable();
        private Subject<IMSScanProperty?>? _reference;
        private Subject<Ms2ScanMatching>? _matchingScorer;

        public PlotComparedMsSpectrumUsecase(IMSScanProperty scan)
        {
            _reference = new Subject<IMSScanProperty?>().AddTo(_disposables);
            _matchingScorer = new Subject<Ms2ScanMatching>().AddTo(_disposables);

            var referenceSpectrum = _reference
                .DefaultIfNull(c => new MsSpectrum(c.Spectrum))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(_disposables);
            GraphLabels msGraphLabels = new GraphLabels(string.Empty, "m/z", "Abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ObservableMsSpectrum upperObservableMsSpectrum = new ObservableMsSpectrum(Observable.Return(new MsSpectrum(scan.Spectrum)), null, Observable.Return((ISpectraExporter?)null)).AddTo(_disposables);
            ObservableMsSpectrum lowerObservableMsSpectrum = new ObservableMsSpectrum(referenceSpectrum, new ReadOnlyReactivePropertySlim<bool>(Observable.Return(true)).AddTo(_disposables), Observable.Return((ISpectraExporter?)null)).AddTo(_disposables);
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector = new PropertySelector<SpectrumPeak, double>(peak => peak.Mass);
            PropertySelector<SpectrumPeak, double> verticalPropertySelector = new PropertySelector<SpectrumPeak, double>(peak => peak.Intensity);
            ChartHueItem upperSpectrumHueItem = new ChartHueItem(nameof(SpectrumPeak.SpectrumComment), ChartBrushes.GetBrush(Brushes.Blue));
            SingleSpectrumModel upperSpectrumModel = new SingleSpectrumModel(
                upperObservableMsSpectrum,
                upperObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"),
                upperObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"),
                upperSpectrumHueItem,
                msGraphLabels).AddTo(_disposables);
            ChartHueItem lowerSpectrumHueItem = new ChartHueItem(nameof(SpectrumPeak.SpectrumComment), ChartBrushes.GetBrush(Brushes.Red));
            SingleSpectrumModel lowerSpectrumModel = new SingleSpectrumModel(
                lowerObservableMsSpectrum,
                lowerObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"),
                lowerObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"),
                lowerSpectrumHueItem,
                msGraphLabels).AddTo(_disposables);
            MsSpectrumModel = new MsSpectrumModel(upperSpectrumModel, lowerSpectrumModel, _matchingScorer)
            {
                GraphTitle = string.Empty,
                HorizontalTitle = "m/z",
                VerticalTitle = "Abundance",
            }.AddTo(_disposables);
        }

        public MsSpectrumModel MsSpectrumModel { get; }

        public void UpdateReference(IMSScanProperty? reference) {
            _reference?.OnNext(reference);
        }

        public void UpdateMatchingScorer(Ms2ScanMatching scorer) {
            if (scorer is not null) {
                _matchingScorer?.OnNext(scorer);
            }
        }

        public void Dispose() {
            if (_disposables is not null) {
                _reference!.OnCompleted();
                _reference = null;
                _matchingScorer!.OnCompleted();
                _matchingScorer = null;
                _disposables.Dispose();
                _disposables = null;
            }
        }
    }
}

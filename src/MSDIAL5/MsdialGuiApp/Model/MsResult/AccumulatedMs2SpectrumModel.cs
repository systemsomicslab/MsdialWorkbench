using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Export;
using Reactive.Bindings.Extensions;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.MsResult
{
    internal sealed class AccumulatedMs2SpectrumModel : DisposableModelBase
    {
        private readonly AccumulateSpectraUsecase _accumulateSpectra;
        private readonly BehaviorSubject<MsSpectrum?> _subject;

        public AccumulatedMs2SpectrumModel(DisplayExtractedIonChromatogram chromatogram, AccumulateSpectraUsecase accumulateSpectra) {
            Chromatogram = chromatogram;
            _accumulateSpectra = accumulateSpectra;
            var subject = new BehaviorSubject<MsSpectrum?>(null).AddTo(Disposables);
            _subject = subject;

            PropertySelector<SpectrumPeak, double> horizontalPropertySelector = new PropertySelector<SpectrumPeak, double>(peak => peak.Mass);
            PropertySelector<SpectrumPeak, double> verticalPropertySelector = new PropertySelector<SpectrumPeak, double>(peak => peak.Intensity);
            var graphLabels = new GraphLabels($"Accumulated MS/MS m/z: {chromatogram.Mz}±{chromatogram.Tolerance}", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ChartHueItem hueItem = new ChartHueItem(nameof(SpectrumPeak.SpectrumComment), new ConstantBrushMapper(Brushes.Black));
            var spectrumox = new ObservableMsSpectrum(subject, null, Observable.Return<ISpectraExporter?>(null)).AddTo(Disposables);
            ChartSpectrumModel = new SingleSpectrumModel(spectrumox, spectrumox.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"), spectrumox.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"), hueItem, graphLabels).AddTo(Disposables);
        }

        public DisplayExtractedIonChromatogram Chromatogram { get; }

        public SingleSpectrumModel ChartSpectrumModel { get; }

        public MSScanProperty? Scan {
            get => _scan;
            private set {
                if (SetProperty(ref _scan, value)) {
                    if (_scan is not null) {
                        _subject.OnNext(new MsSpectrum(_scan.Spectrum));
                    }
                    else {
                        _subject.OnNext(null);

                    }
                }
            }
        }
        private MSScanProperty? _scan;

        public async Task CalculateMs2Async((double start, double end) baseRange, CancellationToken token = default) {
            Scan = await _accumulateSpectra.AccumulateMs2Async(Chromatogram.Mz, Chromatogram.Tolerance, baseRange.start, baseRange.end, token).ConfigureAwait(false);
        }

        public async Task CalculateMs2Async((double start, double end) baseRange, IEnumerable<(double start, double end)> subtracts, CancellationToken token = default) {
            Scan = await _accumulateSpectra.AccumulateMs2Async(Chromatogram.Mz, Chromatogram.Tolerance, baseRange, subtracts, token).ConfigureAwait(false);
        }
    }
}

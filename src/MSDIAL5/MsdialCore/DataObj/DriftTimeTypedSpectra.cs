using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    internal class DriftTimeTypedSpectra : IChromatogramTypedSpectra
    {
        private readonly ChromXUnit _unit;
        private readonly AcquisitionType _acquisitionType;
        private readonly ScanPolarity _polarity;
        private readonly ConcurrentDictionary<int, Lazy<DriftTime>> _idToDriftTime;
        private List<RawSpectrum> _spectra;

        public DriftTimeTypedSpectra(IReadOnlyList<RawSpectrum> spectra, ChromXUnit unit, IonMode ionMode, AcquisitionType acquisitionType) {
            _unit = unit;
            _acquisitionType = acquisitionType;
            switch (ionMode) {
                case IonMode.Positive:
                    _polarity = ScanPolarity.Positive;
                    break;
                case IonMode.Negative:
                    _polarity = ScanPolarity.Negative;
                    break;
                default:
                    throw new ArgumentException($"IonMode {ionMode} is not supported.");
            }

            _spectra = spectra?.OrderBy(spectrum => spectrum.DriftTime).ToList() ?? throw new ArgumentNullException(nameof(spectra));
            _idToDriftTime = new ConcurrentDictionary<int, Lazy<DriftTime>>();
        }

        public Chromatogram GetMs1BasePeakChromatogram(double start, double end) {
            var startIndex = _spectra.LowerBound(start, (spectrum, target) => spectrum.DriftTime.CompareTo(target));
            var endIndex = _spectra.UpperBound(end, startIndex, _spectra.Count, (spectrum, target) => spectrum.DriftTime.CompareTo(target));
            var results = new List<ChromatogramPeak>();
            for (int i = startIndex; i < endIndex; i++) {
                if (_spectra[i].MsLevel != 1 ||
                    _spectra[i].ScanPolarity != _polarity) {
                    continue;
                }
                var (basePeakMz, basePeakIntensity, _) = new Spectrum(_spectra[i].Spectrum).RetrieveTotalIntensity();
                var time = _idToDriftTime.GetOrAdd(i, j => new Lazy<DriftTime>(() => new DriftTime(_spectra[j].DriftTime)));
                results.Add(ChromatogramPeak.Create(_spectra[i].Index, basePeakMz, basePeakIntensity, time.Value));
            }
            return new Chromatogram(results, ChromXType.Drift, _unit);
        }

        public Chromatogram GetMs1ExtractedChromatogram(double mz, double tolerance, double start, double end) {
            var startIndex = _spectra.LowerBound(start, (spectrum, target) => spectrum.DriftTime.CompareTo(target));
            var endIndex = _spectra.UpperBound(end, startIndex, _spectra.Count, (spectrum, target) => spectrum.DriftTime.CompareTo(target));
            var results = new List<ChromatogramPeak>();
            for (int i = startIndex; i < endIndex; i++) {
                if (_spectra[i].MsLevel != 1 ||
                    _spectra[i].ScanPolarity != _polarity) {
                    continue;
                }
                var (basePeakMz, _, summedIntensity) = new Spectrum(_spectra[i].Spectrum).RetrieveBin(mz, tolerance);
                var time = _idToDriftTime.GetOrAdd(i, j => new Lazy<DriftTime>(() => new DriftTime(_spectra[j].DriftTime)));
                results.Add(ChromatogramPeak.Create(_spectra[i].Index, basePeakMz, summedIntensity, time.Value));
            }
            return new Chromatogram(results, ChromXType.Drift, _unit);
        }

        public ExtractedIonChromatogram GetMs1ExtractedChromatogram_temp2(double mz, double tolerance, double start, double end) {
            var startIndex = _spectra.LowerBound(start, (spectrum, target) => spectrum.DriftTime.CompareTo(target));
            var endIndex = _spectra.UpperBound(end, startIndex, _spectra.Count, (spectrum, target) => spectrum.DriftTime.CompareTo(target));
            var results = new List<ValuePeak>();
            for (int i = startIndex; i < endIndex; i++) {
                if (_spectra[i].MsLevel != 1 ||
                    _spectra[i].ScanPolarity != _polarity) {
                    continue;
                }
                var (basePeakMz, _, summedIntensity) = new Spectrum(_spectra[i].Spectrum).RetrieveBin(mz, tolerance);
                var time = _idToDriftTime.GetOrAdd(i, j => new Lazy<DriftTime>(() => new DriftTime(_spectra[j].DriftTime)));
                results.Add(new ValuePeak(_spectra[i].Index, time.Value.Value, basePeakMz, summedIntensity));
            }
            return new ExtractedIonChromatogram(results, ChromXType.Drift, _unit, mz);
        }

        public IEnumerable<ExtractedIonChromatogram> GetMs1ExtractedChromatograms_temp2(IEnumerable<double> mzs, double tolerance, double start, double end) {
            var startIndex = _spectra.LowerBound(start, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
            var endIndex = _spectra.UpperBound(end, startIndex, _spectra.Count, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
            var enumerables = new List<IEnumerable<Spectrum.SummarizedSpectrum>>();
            var indexs = new List<int>();
            var times = new List<double>();
            var mzs_ = mzs.ToList();
            for (int i = startIndex; i < endIndex; i++) {
                if (_spectra[i].MsLevel != 1 ||
                    _spectra[i].ScanPolarity != _polarity) {
                    continue;
                }
                enumerables.Add(new Spectrum(_spectra[i].Spectrum).RetrieveBins(mzs_, tolerance));
                indexs.Add(_spectra[i].Index);
                var time = _idToDriftTime.GetOrAdd(i, j => new Lazy<DriftTime>(() => new DriftTime(_spectra[j].DriftTime)));
                times.Add(time.Value.Value);
            }
            return enumerables.Sequence()
                .Select(peaks => peaks.Zip(indexs, times, (peak, index, time) => new ValuePeak(index, time, peak.BasePeakMz, peak.SummedIntensity)).ToArray())
                .Zip(mzs_, (peaks, mz) => new ExtractedIonChromatogram(peaks, ChromXType.Drift, _unit, mz));
        }

        public Chromatogram GetMs1TotalIonChromatogram(double start, double end) {
            var startIndex = _spectra.LowerBound(start, (spectrum, target) => spectrum.DriftTime.CompareTo(target));
            var endIndex = _spectra.UpperBound(end, startIndex, _spectra.Count, (spectrum, target) => spectrum.DriftTime.CompareTo(target));
            var results = new List<ChromatogramPeak>();
            for (int i = startIndex; i < endIndex; i++) {
                if (_spectra[i].MsLevel != 1 ||
                    _spectra[i].ScanPolarity != _polarity) {
                    continue;
                }
                var (basePeakMz, _, summedIntensity) = new Spectrum(_spectra[i].Spectrum).RetrieveTotalIntensity();
                var time = _idToDriftTime.GetOrAdd(i, j => new Lazy<DriftTime>(() => new DriftTime(_spectra[j].DriftTime)));
                results.Add(ChromatogramPeak.Create(_spectra[i].Index, basePeakMz, summedIntensity, time.Value));
            }
            return new Chromatogram(results, ChromXType.Drift, _unit);
        }

        /// <summary>
        /// Generates a chromatogram for product ions within specified precursor and product m/z ranges and a drift time range.
        /// </summary>
        /// <param name="precursor">The m/z range of the precursor ions.</param>
        /// <param name="product">The m/z range of the product ions.</param>
        /// <param name="chromatogramRange">The range of drift times to include in the chromatogram.</param>
        /// <returns>A <see cref="Chromatogram"/> object representing the intensity of product ions across the specified drift time range.</returns>
        /// <remarks>
        /// This method filters spectra based on their MS level, the specified precursor and product m/z ranges, scan polarity, and the specified drift time range.
        /// For each spectrum that meets these criteria, it calculates the summed intensity of product ions that fall within the specified product m/z range,
        /// associating each intensity with its corresponding drift time. The resulting chromatogram provides insights into how the intensity of specified product ions changes
        /// over the selected drift time range, which is useful for analyzing the behavior of ions with different mobility characteristics.
        /// </remarks>
        public Chromatogram GetProductIonChromatogram(MzRange precursor, MzRange product, ChromatogramRange chromatogramRange) {
            var startIndex = _spectra.LowerBound(chromatogramRange.Begin, (spectrum, target) => spectrum.DriftTime.CompareTo(target));
            var endIndex = _spectra.UpperBound(chromatogramRange.End, startIndex, _spectra.Count, (spectrum, target) => spectrum.DriftTime.CompareTo(target));
            var results = new List<ChromatogramPeak>();
            for (int i = startIndex; i < endIndex; i++) {
                if (_spectra[i].MsLevel != 2 ||
                    !_spectra[i].Precursor.ContainsMz(precursor.Mz, precursor.Tolerance, _acquisitionType) ||
                    _spectra[i].ScanPolarity != _polarity) {
                    continue;
                }
                var (basePeakMz, _, summedIntensity) = new Spectrum(_spectra[i].Spectrum).RetrieveBin(product.Mz, product.Tolerance);
                var time = _idToDriftTime.GetOrAdd(i, j => new Lazy<DriftTime>(() => new DriftTime(_spectra[j].DriftTime)));
                results.Add(ChromatogramPeak.Create(_spectra[i].Index, basePeakMz, summedIntensity, time.Value));
            }
            return new Chromatogram(results, ChromXType.Drift, _unit);
        }
    }
}

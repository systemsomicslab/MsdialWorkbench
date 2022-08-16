using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
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
        private readonly ScanPolarity _polarity;
        private readonly ConcurrentDictionary<int, Lazy<DriftTime>> _idToDriftTime;
        private List<RawSpectrum> _spectra;

        public DriftTimeTypedSpectra(IReadOnlyList<RawSpectrum> spectra, ChromXUnit unit, IonMode ionMode) {
            _unit = unit;

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
                results.Add(ChromatogramPeak.Create(i, basePeakMz, basePeakIntensity, time.Value));
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
                results.Add(ChromatogramPeak.Create(i, basePeakMz, summedIntensity, time.Value));
            }
            return new Chromatogram(results, ChromXType.Drift, _unit);
        }

        public Chromatogram_temp GetMs1ExtractedChromatogram_temp(double mz, double tolerance, double start, double end) {
            var startIndex = _spectra.LowerBound(start, (spectrum, target) => spectrum.DriftTime.CompareTo(target));
            var endIndex = _spectra.UpperBound(end, startIndex, _spectra.Count, (spectrum, target) => spectrum.DriftTime.CompareTo(target));
            var results = new List<double[]>();
            for (int i = startIndex; i < endIndex; i++) {
                if (_spectra[i].MsLevel != 1 ||
                    _spectra[i].ScanPolarity != _polarity) {
                    continue;
                }
                var (basePeakMz, _, summedIntensity) = new Spectrum(_spectra[i].Spectrum).RetrieveBin(mz, tolerance);
                var time = _idToDriftTime.GetOrAdd(i, j => new Lazy<DriftTime>(() => new DriftTime(_spectra[j].DriftTime)));
                results.Add(new double[] { i, time.Value.Value, basePeakMz, summedIntensity });
            }
            return new Chromatogram_temp(results, ChromXType.Drift, _unit);
        }

        public Chromatogram_temp2 GetMs1ExtractedChromatogram_temp2(double mz, double tolerance, double start, double end) {
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
            return new Chromatogram_temp2(results, ChromXType.Drift, _unit);
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
                results.Add(ChromatogramPeak.Create(i, basePeakMz, summedIntensity, time.Value));
            }
            return new Chromatogram(results, ChromXType.Drift, _unit);
        }
    }
}

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
    internal class RetentionTimeTypedSpectra : IChromatogramTypedSpectra
    {
        private readonly ChromXUnit _unit;
        private readonly ScanPolarity _polarity;
        private readonly List<RawSpectrum> _spectra;
        private readonly RetentionTime[] _idToRetentionTime;
        private readonly ConcurrentDictionary<int, Lazy<Spectrum>> _lazySpectra;
        private readonly int[] _ms1Counts;

        public RetentionTimeTypedSpectra(IReadOnlyList<RawSpectrum> spectra, ChromXUnit unit, IonMode ionMode) {
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

            _spectra = spectra?.OrderBy(spectrum => spectrum.ScanStartTime).ToList() ?? throw new ArgumentNullException(nameof(spectra));
            _idToRetentionTime = new RetentionTime[_spectra.Count];
            _ms1Counts = new int[_spectra.Count + 1];
            _lazySpectra = new ConcurrentDictionary<int, Lazy<Spectrum>>();
            for (int i = 0; i < _spectra.Count; i++) {
                _idToRetentionTime[i] = new RetentionTime(_spectra[i].ScanStartTime, unit);
                if (_spectra[i].MsLevel == 1) {
                    _ms1Counts[i + 1]++;
                }
            }
            for (int i = 1; i < _ms1Counts.Length; i++) {
                _ms1Counts[i] += _ms1Counts[i - 1];
            }
        }

        public Chromatogram GetMs1BasePeakChromatogram(double start, double end) {
            var startIndex = _spectra.LowerBound(start, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
            var endIndex = _spectra.UpperBound(end, startIndex, _spectra.Count, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
            var results = new List<ChromatogramPeak>();
            for (int i = startIndex; i < endIndex; i++) {
                if (_spectra[i].MsLevel != 1 ||
                    _spectra[i].ScanPolarity != _polarity) {
                    continue;
                }
                var (basePeakMz, basePeakIntensity, _) = new Spectrum(_spectra[i].Spectrum).RetrieveTotalIntensity();
                results.Add(ChromatogramPeak.Create(_spectra[i].Index, results.Count, basePeakMz, basePeakIntensity, _idToRetentionTime[i]));
            }
            return new Chromatogram(results, ChromXType.RT, _unit);
        }

        public Chromatogram GetMs1ExtractedChromatogram(double mz, double tolerance, double start, double end) {
            var startIndex = _spectra.LowerBound(start, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
            var endIndex = _spectra.UpperBound(end, startIndex, _spectra.Count, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
            var results = new List<ChromatogramPeak>();
            for (int i = startIndex; i < endIndex; i++) {
                if (_spectra[i].MsLevel != 1 ||
                    _spectra[i].ScanPolarity != _polarity) {
                    continue;
                }
                var (basePeakMz, _, summedIntensity) = new Spectrum(_spectra[i].Spectrum).RetrieveBin(mz, tolerance);
                results.Add(ChromatogramPeak.Create(_spectra[i].Index, results.Count, basePeakMz, summedIntensity, _idToRetentionTime[i]));
            }
            return new Chromatogram(results, ChromXType.RT, _unit);
        }

        public Chromatogram_temp2 GetMs1ExtractedChromatogram_temp2(double mz, double tolerance, double start, double end) {
            var startIndex = _spectra.LowerBound(start, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
            var endIndex = _spectra.UpperBound(end, startIndex, _spectra.Count, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
            // var results = new List<ValuePeak>(_ms1Counts[endIndex] - _ms1Counts[startIndex]);
            var results = new ValuePeak[_ms1Counts[endIndex] - _ms1Counts[startIndex]];
            var idc = 0;
            for (int i = startIndex; i < endIndex; i++) {
                if (_spectra[i].MsLevel != 1 ||
                    _spectra[i].ScanPolarity != _polarity) {
                    continue;
                }
                var spectrum = _lazySpectra.GetOrAdd(i, index => new Lazy<Spectrum>(() => new Spectrum(_spectra[index].Spectrum))).Value;
                var (basePeakMz, _, summedIntensity) = spectrum.RetrieveBin(mz, tolerance);
                results[idc++] = new ValuePeak(_spectra[i].Index, _idToRetentionTime[i].Value, basePeakMz, summedIntensity);
                // results.Add(new ValuePeak (i, _idToRetentionTime[i].Value, basePeakMz, summedIntensity));
            }
            return new Chromatogram_temp2(results, ChromXType.RT, _unit);
        }

        public Chromatogram GetMs1TotalIonChromatogram(double start, double end) {
            var startIndex = _spectra.LowerBound(start, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
            var endIndex = _spectra.UpperBound(end, startIndex, _spectra.Count, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
            var results = new List<ChromatogramPeak>();
            for (int i = startIndex; i < endIndex; i++) {
                if (_spectra[i].MsLevel != 1 ||
                    _spectra[i].ScanPolarity != _polarity) {
                    continue;
                }
                var (basePeakMz, _, summedIntensity) = new Spectrum(_spectra[i].Spectrum).RetrieveTotalIntensity();
                results.Add(ChromatogramPeak.Create(_spectra[i].Index, results.Count, basePeakMz, summedIntensity, _idToRetentionTime[i]));
            }
            return new Chromatogram(results, ChromXType.RT, _unit);
        }
    }
}

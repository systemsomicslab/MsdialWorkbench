using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    internal class RetentionTimeTypedSpectra : IChromatogramTypedSpectra
    {
        private readonly ChromXUnit _unit;
        private readonly ScanPolarity _polarity;
        private readonly List<RawSpectrum> _spectra;
        private readonly Dictionary<int, RetentionTime> _idToRetentionTime;

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

            _idToRetentionTime = new Dictionary<int, RetentionTime>();
            _spectra = spectra?.OrderBy(spectrum => spectrum.ScanStartTime).ToList() ?? throw new ArgumentNullException(nameof(spectra));
            for (int i = 0; i < _spectra.Count; i++) {
                _idToRetentionTime[i] = new RetentionTime(_spectra[i].ScanStartTime, unit);
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
                results.Add(new ChromatogramPeak(i, basePeakMz, basePeakIntensity, _idToRetentionTime[i]));
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
                results.Add(new ChromatogramPeak(i, basePeakMz, summedIntensity, _idToRetentionTime[i]));
            }
            return new Chromatogram(results, ChromXType.RT, _unit);
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
                results.Add(new ChromatogramPeak(i, basePeakMz, summedIntensity, _idToRetentionTime[i]));
            }
            return new Chromatogram(results, ChromXType.RT, _unit);
        }
    }
}

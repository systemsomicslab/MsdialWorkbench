using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    public class RawSpectra
    {
        private readonly IReadOnlyList<RawSpectrum> _spectra;
        private readonly ChromXType _type;
        private readonly ChromXUnit _unit;
        private readonly ScanPolarity _polarity;
        private readonly Dictionary<int, ChromX> _idToChromX;

        public RawSpectra(IReadOnlyList<RawSpectrum> spectra, ChromXType type, ChromXUnit unit, IonMode ionMode) {
            _type = type;
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

            _idToChromX = new Dictionary<int, ChromX>();
            switch (type) {
                case ChromXType.RT:
                    _spectra = spectra?.OrderBy(spectrum => spectrum.ScanStartTime).ToList() ?? throw new ArgumentNullException(nameof(spectra));
                    for (int i = 0; i < _spectra.Count; i++) {
                        _idToChromX[i] = new RetentionTime(_spectra[i].ScanStartTime, unit);
                    }
                    break;
                case ChromXType.Drift:
                    _spectra = spectra?.OrderBy(spectrum => spectrum.DriftTime).ToList() ?? throw new ArgumentNullException(nameof(spectra));
                    for (int i = 0; i < _spectra.Count; i++) {
                        _idToChromX[i] = new DriftTime(_spectra[i].DriftTime, unit);
                    }
                    break;
                default:
                    throw new ArgumentException($"ChromXType {type} is not supported.");
            }
        }

        public Chromatogram GetMs1Chromatogram(double mz, double tolerance, double start, double end) {
            int startIndex, endIndex;
            var results = new List<ChromatogramPeak>();
            switch (_type) {
                case ChromXType.RT:
                    startIndex = _spectra.LowerBound(start, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
                    endIndex = _spectra.UpperBound(end, startIndex, _spectra.Count, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
                    for (int i = startIndex; i < endIndex; i++) {
                        if (_spectra[i].ScanPolarity != _polarity) {
                            continue;
                        }
                        var (basePeakMz, _, summedIntensity) = new Spectrum(_spectra[i].Spectrum).RetrieveBin(mz, tolerance);
                        results.Add(new ChromatogramPeak(i, basePeakMz, summedIntensity, _idToChromX[i]));
                    }
                    return new Chromatogram(results);
                case ChromXType.Drift:
                    startIndex = _spectra.LowerBound(start, (spectrum, target) => spectrum.DriftTime.CompareTo(target));
                    endIndex = _spectra.UpperBound(end, startIndex, _spectra.Count, (spectrum, target) => spectrum.DriftTime.CompareTo(target));
                    for (int i = startIndex; i < endIndex; i++) {
                        if (_spectra[i].ScanPolarity != _polarity) {
                            continue;
                        }
                        var (basePeakMz, _, summedIntensity) = new Spectrum(_spectra[i].Spectrum).RetrieveBin(mz, tolerance);
                        results.Add(new ChromatogramPeak(i, basePeakMz, summedIntensity, _idToChromX[i]));
                    }
                    return new Chromatogram(results);
                default:
                    throw new NotSupportedException($"{nameof(_type)} {_type} is not supported");
            }
        }
    }
}

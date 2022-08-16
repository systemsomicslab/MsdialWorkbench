using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    public class RawSpectra
    {
        private readonly ConcurrentDictionary<(ChromXType, ChromXUnit), Lazy<IChromatogramTypedSpectra>> _spectraImpls;
        private readonly IReadOnlyList<RawSpectrum> _spectra;
        private readonly IonMode _ionMode;

        public RawSpectra(IDataProvider provider, IonMode ionMode, AcquisitionType acquisitionType) {
            _spectra = provider.LoadMsSpectrums();
            _ionMode = ionMode;
            _spectraImpls = new ConcurrentDictionary<(ChromXType, ChromXUnit), Lazy<IChromatogramTypedSpectra>>();
        }

        public RawSpectra(IReadOnlyList<RawSpectrum> spectra, IonMode ionMode, AcquisitionType acquisitionType) {
            _spectra = spectra;
            _ionMode = ionMode;
            _spectraImpls = new ConcurrentDictionary<(ChromXType, ChromXUnit), Lazy<IChromatogramTypedSpectra>>();
        }

        public Chromatogram GetMs1ExtractedChromatogram(double mz, double tolerance, ChromatogramRange chromatogramRange) {
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return impl.GetMs1ExtractedChromatogram(mz, tolerance, chromatogramRange.Begin, chromatogramRange.End);
        }

        public Chromatogram_temp2 GetMs1ExtractedChromatogram_temp2(double mz, double tolerance, ChromatogramRange chromatogramRange) {
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return impl.GetMs1ExtractedChromatogram_temp2(mz, tolerance, chromatogramRange.Begin, chromatogramRange.End);
        }

        public Chromatogram GetMs1TotalIonChromatogram(ChromatogramRange chromatogramRange) {
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return impl.GetMs1TotalIonChromatogram(chromatogramRange.Begin, chromatogramRange.End);
        }

        public Chromatogram GetMs1BasePeakChromatogram(ChromatogramRange chromatogramRange) {
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return impl.GetMs1BasePeakChromatogram(chromatogramRange.Begin, chromatogramRange.End);
        }

        public Chromatogram GetMs1ExtractedChromatogramByHighestBasePeakMz(IEnumerable<ISpectrumPeak> peaks, double tolerance, ChromatogramRange chromatogramRange) {
            var mz = peaks.Argmax(feature => feature.Intensity).Mass;
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return impl.GetMs1ExtractedChromatogram(mz, tolerance, chromatogramRange.Begin, chromatogramRange.End);
        }

        public Chromatogram GetDriftChromatogramByScanRtMz(int scanID, float rt, float rtWidth, float mz, float mztol) {
            var driftBinToChromPeak = new Dictionary<int, ChromatogramPeak>();
            var driftBinToBasePeakIntensity = new Dictionary<int, double>();

            //accumulating peaks from peak top to peak left
            for (int i = scanID + 1; i >= 0; i--) {
                var spectrum = _spectra[i];
                if (spectrum.MsLevel != 1) continue;
                if (spectrum.ScanStartTime < rt - rtWidth * 0.5) break;
                SetChromatogramPeak(spectrum, mz, mztol, driftBinToChromPeak, driftBinToBasePeakIntensity);
            }

            //accumulating peaks from peak top to peak right
            for (int i = scanID + 2; i < _spectra.Count; i++) {
                var spectrum = _spectra[i];
                if (spectrum.MsLevel != 1) continue;
                if (spectrum.ScanStartTime > rt + rtWidth * 0.5) break;
                SetChromatogramPeak(spectrum, mz, mztol, driftBinToChromPeak, driftBinToBasePeakIntensity);
            }

            return new Chromatogram(driftBinToChromPeak.Values.OrderBy(n => n.ChromXs.Value).ToList(), ChromXType.Drift, ChromXUnit.Msec);
        }

        private static void SetChromatogramPeak(RawSpectrum spectrum, float mz, float mztol, Dictionary<int, ChromatogramPeak> driftBinToChromPeak, Dictionary<int, double> driftBinToBasePeakIntensity) {
            var driftTime = spectrum.DriftTime;
            var driftBin = (int)(driftTime * 1000);

            var intensity = Utility.DataAccess.GetIonAbundanceOfMzInSpectrum(spectrum.Spectrum, mz, mztol, out double basepeakMz, out double basepeakIntensity);
            if (driftBinToChromPeak.TryGetValue(driftBin, out var chromPeak)) {
                chromPeak.Intensity += intensity;
                if (driftBinToBasePeakIntensity[driftBin] < basepeakIntensity) {
                    driftBinToBasePeakIntensity[driftBin] = basepeakIntensity;
                    chromPeak.Mass = basepeakMz;
                }
            }
            else {
                driftBinToChromPeak[driftBin] = new ChromatogramPeak(spectrum.OriginalIndex, basepeakMz, intensity, new ChromXs(new DriftTime(driftTime, ChromXUnit.Msec)));
                driftBinToBasePeakIntensity[driftBin] = basepeakIntensity;
            }
        }

        public PeakMs2Spectra GetPeakMs2Spectra(ChromatogramPeakFeature rtPeakFeature, double ms2Tolerance, AcquisitionType acquisitionType, DriftTime driftTime) {
            var scanPolarity = rtPeakFeature.IonMode.ToPolarity();
            var spectra = Enumerable.Range(rtPeakFeature.MS1RawSpectrumIdLeft, rtPeakFeature.MS1RawSpectrumIdRight - rtPeakFeature.MS1RawSpectrumIdLeft + 1)
                .Select(i => _spectra[i])
                .Where(spectrum => spectrum.MsLevel == 2)
                .Where(spectrum => spectrum.ScanPolarity == scanPolarity)
                .Where(spectrum => spectrum.Precursor.ContainsMz(rtPeakFeature.Mass, ms2Tolerance, acquisitionType)) // mz is in range
                .Where(spectrum => spectrum.Precursor.ContainsDriftTime(driftTime) || spectrum.Precursor.IsNotDiapasefData) // in drift time range for diapasef or normal dia
                .ToArray();
            return new PeakMs2Spectra(spectra, scanPolarity, acquisitionType);
        }

        private IChromatogramTypedSpectra BuildIfNotExists(ChromXType type, ChromXUnit unit) {
            return _spectraImpls.GetOrAdd((type, unit), pair => new Lazy<IChromatogramTypedSpectra>(() => BuildTypedSpectra(_spectra, pair.Item1, pair.Item2, _ionMode))).Value;
        }

        private static IChromatogramTypedSpectra BuildTypedSpectra(IReadOnlyList<RawSpectrum> spectra, ChromXType type, ChromXUnit unit, IonMode ionMode) {
            switch (type) {
                case ChromXType.RT:
                    return new RetentionTimeTypedSpectra(spectra, unit, ionMode);
                case ChromXType.Drift:
                    return new DriftTimeTypedSpectra(spectra, unit, ionMode);
                default:
                    throw new ArgumentException($"ChromXType {type} is not supported.");
            }
        }
    }
}

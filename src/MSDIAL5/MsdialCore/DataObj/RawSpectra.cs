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
    public class RawSpectra : IRawSpectra
    {
        private readonly ConcurrentDictionary<(ChromXType, ChromXUnit), Lazy<IChromatogramTypedSpectra>> _spectraImpls;
        private readonly IReadOnlyList<RawSpectrum> _spectra;
        private readonly IonMode _ionMode;
        private readonly AcquisitionType _acquisitionType;
        public AcquisitionType AcquisitionType { get => _acquisitionType; }

        public RawSpectra(IDataProvider provider, IonMode ionMode, AcquisitionType acquisitionType) {
            _spectra = provider.LoadMsSpectrums();
            _ionMode = ionMode;
            _spectraImpls = new ConcurrentDictionary<(ChromXType, ChromXUnit), Lazy<IChromatogramTypedSpectra>>();
            _acquisitionType = acquisitionType;
        }

        public RawSpectra(IReadOnlyList<RawSpectrum> spectra, IonMode ionMode, AcquisitionType acquisitionType) {
            _spectra = spectra;
            _ionMode = ionMode;
            _spectraImpls = new ConcurrentDictionary<(ChromXType, ChromXUnit), Lazy<IChromatogramTypedSpectra>>();
            _acquisitionType= acquisitionType;
        }

        public double StartRt { 
            get {
                if (_spectra.IsEmptyOrNull()) return 0;
                else return _spectra[0].ScanStartTime;
            } 
        }

        public double EndRt {
            get {
                if (_spectra.IsEmptyOrNull()) return 0;
                else return _spectra[_spectra.Count - 1].ScanStartTime;
            }
        }

        public (int MsLevel, int ExperimentID)[] ExperimentIDs {
            get {
                var result = new HashSet<(int MsLevel, int ExperimentID)>();
                foreach (var spec in _spectra) {
                    result.Add((spec.MsLevel, spec.ExperimentID));
                }
                return [.. result];
            }
        }

        public ExtractedIonChromatogram GetMS1ExtractedChromatogram(MzRange mzRange, ChromatogramRange chromatogramRange) {
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return impl.GetMs1ExtractedChromatogram_temp2(mzRange.Mz, mzRange.Tolerance, chromatogramRange.Begin, chromatogramRange.End);
        }

        public IEnumerable<ExtractedIonChromatogram> GetMS1ExtractedChromatograms(IEnumerable<double> mzs, double tolerance, ChromatogramRange chromatogramRange) {
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return impl.GetMs1ExtractedChromatograms_temp2(mzs, tolerance, chromatogramRange.Begin, chromatogramRange.End);
        }

        public Chromatogram GetMS1TotalIonChromatogram(ChromatogramRange chromatogramRange) {
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return impl.GetMs1TotalIonChromatogram(chromatogramRange.Begin, chromatogramRange.End);
        }

        public Chromatogram GetMS1BasePeakChromatogram(ChromatogramRange chromatogramRange) {
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return impl.GetMs1BasePeakChromatogram(chromatogramRange.Begin, chromatogramRange.End);
        }

        public Chromatogram GetMS1ExtractedChromatogramByHighestBasePeakMz(IEnumerable<ISpectrumPeak> peaks, double tolerance, ChromatogramRange chromatogramRange) {
            var mz = peaks.Argmax(feature => feature.Intensity).Mass;
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return impl.GetMs1ExtractedChromatogram(mz, tolerance, chromatogramRange.Begin, chromatogramRange.End);
        }

        public Chromatogram GetDriftChromatogramByScanRtMz(int scanID, float rt, float rtWidth, float mz, float mztol, float minDt, float maxDt) {
            var driftBinToChromPeak = new Dictionary<int, ChromatogramPeak>();
            var driftBinToBasePeakIntensity = new Dictionary<int, double>();

            //accumulating peaks from peak top to peak left
            for (int i = scanID + 1; i >= 0; i--) {
                var spectrum = _spectra[i];
                if (spectrum.MsLevel != 1) continue;
                if (spectrum.DriftTime < minDt || spectrum.DriftTime > maxDt) continue;
                if (spectrum.ScanStartTime < rt - rtWidth * 0.5) break;
                SetChromatogramPeak(spectrum, mz, mztol, driftBinToChromPeak, driftBinToBasePeakIntensity);
            }

            //accumulating peaks from peak top to peak right
            for (int i = scanID + 2; i < _spectra.Count; i++) {
                var spectrum = _spectra[i];
                if (spectrum.MsLevel != 1) continue;
                if (spectrum.DriftTime < minDt || spectrum.DriftTime > maxDt) continue;
                if (spectrum.ScanStartTime > rt + rtWidth * 0.5) break;
                SetChromatogramPeak(spectrum, mz, mztol, driftBinToChromPeak, driftBinToBasePeakIntensity);
            }

            return new Chromatogram(driftBinToChromPeak.Values.OrderBy(n => n.ChromXs.Value).ToList(), ChromXType.Drift, ChromXUnit.Msec);
        }

        public Chromatogram GetDriftChromatogramByScanRtMz(int scanID, float rt, float rtWidth, float mz, float mztol) {
            return GetDriftChromatogramByScanRtMz(scanID, rt, rtWidth, mz, mztol, float.MinValue, float.MaxValue);
        }

        /// <summary>
        /// Generates a total ion chromatogram for MS2 spectra within a specified chromatogram range.
        /// </summary>
        /// <param name="chromatogramRange">The range of the chromatogram, defined by start and end points along with the type and unit of the chromatographic measurement (e.g., retention time or ion mobility).</param>
        /// <returns>A <see cref="Chromatogram"/> object representing the total ion chromatogram of MS2 spectra within the specified range. The chromatogram includes peaks with details such as index, chromatographic measurement (e.g., retention time or drift time), base peak m/z value, and summed intensity.</returns>
        /// <remarks>
        /// This method dynamically builds or retrieves an appropriate chromatogram generator based on the specified range type and unit, and then generates the total ion chromatogram for MS2 spectra within the given range.
        /// </remarks>
        public Chromatogram GetMS2TotalIonChromatogram(ChromatogramRange chromatogramRange) {
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return impl.GetMs2TotalIonChromatogram(chromatogramRange);
        }

        /// <summary>
        /// Generates a total ion chromatogram for MS2 spectra from a specific experiment within a specified chromatogram range.
        /// </summary>
        /// <param name="experimentID">The ID of the experiment from which to retrieve MS2 spectra. Only spectra matching this experiment ID are included in the chromatogram.</param>
        /// <param name="chromatogramRange">The range of the chromatogram, defined by start and end points along with the type and unit of the chromatographic measurement (e.g., retention time or ion mobility).</param>
        /// <returns>A <see cref="Chromatogram"/> object representing the total ion chromatogram of MS2 spectra from the specified experiment within the given range. Each peak in the chromatogram corresponds to an MS2 spectrum, including details such as index, chromatographic measurement (e.g., retention time or drift time), base peak m/z value, and summed intensity.</returns>
        /// <remarks>
        /// Similar to the overload without the experiment ID, this method also dynamically builds or retrieves a suitable chromatogram generator based on the range type and unit. It then filters the MS2 spectra by the specified experiment ID before generating the chromatogram.
        /// </remarks>
        public SpecificExperimentChromatogram GetMS2TotalIonChromatogram(int experimentID, ChromatogramRange chromatogramRange) {
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return impl.GetMS2TotalIonChromatogram(chromatogramRange, experimentID);
        }

        /// <summary>
        /// Retrieves a chromatogram for product ions based on specified precursor and product m/z ranges, 
        /// using the provided chromatogram range settings.
        /// </summary>
        /// <param name="precursor">The m/z range for the precursor ions.</param>
        /// <param name="product">The m/z range for the product ions.</param>
        /// <param name="chromatogramRange">Specifies the range and type of the chromatogram to be generated, 
        /// including the chromatogram type (e.g., retention time, drift time) and unit.</param>
        /// <returns>A <see cref="ExtractedIonChromatogram"/> object representing the chromatogram of product ions 
        /// within the specified precursor and product m/z ranges and chromatogram range.</returns>
        /// <remarks>
        /// This method dynamically selects or constructs an appropriate implementation for generating the chromatogram
        /// based on the type and unit of the chromatogram specified in <paramref name="chromatogramRange"/>.
        /// It delegates the actual generation of the chromatogram to the selected implementation, ensuring that
        /// the chromatogram is generated in accordance with the specified parameters.
        /// </remarks>
        public ExtractedIonChromatogram GetMS2ExtractedIonChromatogram(MzRange precursor, MzRange product, ChromatogramRange chromatogramRange) {
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return impl.GetProductIonChromatogram(precursor, product, chromatogramRange);
        }

        /// <summary>
        /// Generates an extracted ion chromatogram from MS2 spectra for a specified m/z range within a given chromatogram range, filtering by a specific experiment ID.
        /// </summary>
        /// <param name="experimentID">The ID of the experiment from which to retrieve MS2 spectra. Only spectra matching this experiment ID and falling within the specified chromatogram range are included in the chromatogram.</param>
        /// <param name="product">The target m/z range for the extracted ion, including the center m/z value and the tolerance for the extraction.</param>
        /// <param name="chromatogramRange">Specifies the range and type of the chromatogram to be generated, including the chromatogram type (e.g., retention time, drift time) and unit.</param>
        /// <returns>An <see cref="ExtractedIonChromatogram"/> object representing the chromatogram of extracted ions within the specified product m/z range and chromatogram range, filtered by the specified experiment ID. Each <see cref="ValuePeak"/> in the chromatogram corresponds to an extracted ion, detailing the spectrum index, chromatogram time (e.g., drift time), base peak m/z value, and summed intensity.</returns>
        /// <remarks>
        /// This method filters spectra based on the MS level (MS2), scan polarity, and the specified experiment ID, within the specified chromatogram range. It calculates the extracted ion chromatogram by summing the intensities of ions within the specified m/z range for each selected spectrum. This targeted approach enables detailed analysis of specific ions, facilitating the exploration of their behavior within the experimental setup.
        /// </remarks>
        public ExtractedIonChromatogram GetMS2ExtractedIonChromatogram(int experimentID, MzRange product, ChromatogramRange chromatogramRange) {
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return impl.GetMS2ExtractedIonChromatogram(product, chromatogramRange, experimentID);
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
                driftBinToChromPeak[driftBin] = new ChromatogramPeak(spectrum.Index, basepeakMz, intensity, new ChromXs(new DriftTime(driftTime, ChromXUnit.Msec)));
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
            return _spectraImpls.GetOrAdd((type, unit), pair => new Lazy<IChromatogramTypedSpectra>(() => BuildTypedSpectra(_spectra, pair.Item1, pair.Item2, _ionMode, _acquisitionType))).Value;
        }

        private static IChromatogramTypedSpectra BuildTypedSpectra(IReadOnlyList<RawSpectrum> spectra, ChromXType type, ChromXUnit unit, IonMode ionMode, AcquisitionType acquisitionType) {
            switch (type) {
                case ChromXType.RT:
                    return new RetentionTimeTypedSpectra(spectra, unit, ionMode, acquisitionType);
                case ChromXType.Drift:
                    return new DriftTimeTypedSpectra(spectra, unit, ionMode, acquisitionType);
                default:
                    throw new ArgumentException($"ChromXType {type} is not supported.");
            }
        }
    }
}

using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class Ms1Spectra
    {
        private readonly ConcurrentDictionary<(ChromXType, ChromXUnit), IChromatogramTypedSpectra> _spectraImpls;
        private readonly IReadOnlyList<RawSpectrum> _spectra;
        private readonly IonMode _ionMode;
        private readonly AcquisitionType _acquisitionType;

        public Ms1Spectra(IReadOnlyList<RawSpectrum> spectra, IonMode ionMode, AcquisitionType acquisitionType) {
            _spectra = spectra;
            _ionMode = ionMode;
            _acquisitionType = acquisitionType;
            _spectraImpls = new ConcurrentDictionary<(ChromXType, ChromXUnit), IChromatogramTypedSpectra>();
        }

        public Chromatogram GetMs1ExtractedChromatogram(double mz, double tolerance, ChromatogramRange chromatogramRange) {
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return impl.GetMs1ExtractedChromatogram_temp2(mz, tolerance, chromatogramRange.Begin, chromatogramRange.End);
        }

        public Chromatogram GetMs1ExtractedChromatogram(MzRange mzRange, ChromatogramRange chromatogramRange) {
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return impl.GetMs1ExtractedChromatogram_temp2(mzRange.Mz, mzRange.Tolerance, chromatogramRange.Begin, chromatogramRange.End);
        }

        /// <summary>
        /// Retrieves a representative spectrum from the collection of spectra.
        /// </summary>
        /// <returns>The representative <see cref="RawSpectrum"/> object.</returns>
        /// <remarks>
        /// This method selects a spectrum with MS level 1 and the largest number of peaks, 
        /// excluding empty or null spectra. It is designed with the assumption that, in the 
        /// typical use case, the collection contains only one spectrum. The selected spectrum 
        /// does not hold significant meaning and is chosen arbitrarily.
        /// </remarks>
        public RawSpectrum GetRepresentativeSpectrum() {
            return _spectra
                .Where(spec => spec.MsLevel == 1 && !spec.Spectrum.IsEmptyOrNull())
                .Argmax(spec => spec.Spectrum.Length);
        }

        private IChromatogramTypedSpectra BuildIfNotExists(ChromXType type, ChromXUnit unit) {
            return _spectraImpls.GetOrAdd((type, unit), pair => BuildTypedSpectra(_spectra, pair.Item1, pair.Item2, _ionMode, _acquisitionType));
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

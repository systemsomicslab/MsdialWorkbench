using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj
{
    public class Ms1Spectra
    {
        private readonly Dictionary<ChromXType, IChromatogramTypedSpectra> _spectraImpls;
        private readonly IReadOnlyList<RawSpectrum> _spectra;
        private readonly IonMode _ionMode;

        public Ms1Spectra(IReadOnlyList<RawSpectrum> spectra, IonMode ionMode) {
            _spectra = spectra;
            _ionMode = ionMode;
            _spectraImpls = new Dictionary<ChromXType, IChromatogramTypedSpectra>();
        }

        public Chromatogram GetMs1ExtractedChromatogram(double mz, double tolerance, ChromatogramRange chromatogramRange) {
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return impl.GetMs1ExtractedChromatogram(mz, tolerance, chromatogramRange.Begin, chromatogramRange.End);
        }

        private IChromatogramTypedSpectra BuildIfNotExists(ChromXType type, ChromXUnit unit) {
            if (!_spectraImpls.TryGetValue(type, out var impl)) {
                impl = _spectraImpls[type] = BuildTypedSpectra(_spectra, type, unit, _ionMode);
            }
            return impl;
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

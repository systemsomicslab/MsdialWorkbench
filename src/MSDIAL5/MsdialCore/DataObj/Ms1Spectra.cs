using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class Ms1Spectra
    {
        private readonly ConcurrentDictionary<(ChromXType, ChromXUnit), IChromatogramTypedSpectra> _spectraImpls;
        private readonly IReadOnlyList<RawSpectrum> _spectra;
        private readonly IonMode _ionMode;

        public Ms1Spectra(IReadOnlyList<RawSpectrum> spectra, IonMode ionMode) {
            _spectra = spectra;
            _ionMode = ionMode;
            _spectraImpls = new ConcurrentDictionary<(ChromXType, ChromXUnit), IChromatogramTypedSpectra>();
        }

        public Chromatogram GetMs1ExtractedChromatogram(double mz, double tolerance, ChromatogramRange chromatogramRange) {
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return impl.GetMs1ExtractedChromatogram(mz, tolerance, chromatogramRange.Begin, chromatogramRange.End);
        }

        private IChromatogramTypedSpectra BuildIfNotExists(ChromXType type, ChromXUnit unit) {
            return _spectraImpls.GetOrAdd((type, unit), pair => BuildTypedSpectra(_spectra, pair.Item1, pair.Item2, _ionMode));
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

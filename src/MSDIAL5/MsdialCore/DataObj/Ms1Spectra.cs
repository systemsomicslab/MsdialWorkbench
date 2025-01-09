using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Raw.Abstractions;
using System;
using System.Collections.Concurrent;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class Ms1Spectra
    {
        private readonly ConcurrentDictionary<(ChromXType, ChromXUnit), IChromatogramTypedSpectra> _spectraImpls;
        private readonly IonMode _ionMode;
        private readonly AcquisitionType _acquisitionType;
        private readonly IDataProvider _spectraProvider;

        public Ms1Spectra(IonMode ionMode, AcquisitionType acquisitionType, IDataProvider spectraProvider) {
            _ionMode = ionMode;
            _acquisitionType = acquisitionType;
            _spectraProvider = spectraProvider;
            _spectraImpls = new ConcurrentDictionary<(ChromXType, ChromXUnit), IChromatogramTypedSpectra>();
        }

        public Chromatogram GetMs1ExtractedChromatogram(double mz, double tolerance, ChromatogramRange chromatogramRange) {
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return impl.GetMS1ExtractedChromatogramAsync(mz, tolerance, chromatogramRange.Begin, chromatogramRange.End, default).Result;
        }

        private IChromatogramTypedSpectra BuildIfNotExists(ChromXType type, ChromXUnit unit) {
            return _spectraImpls.GetOrAdd((type, unit), pair => BuildTypedSpectra(pair.Item1, pair.Item2, _ionMode, _acquisitionType, _spectraProvider));
        }

        private static IChromatogramTypedSpectra BuildTypedSpectra(ChromXType type, ChromXUnit unit, IonMode ionMode, AcquisitionType acquisitionType, IDataProvider spectraProvider) {
            switch (type) {
                case ChromXType.RT:
                    return new RetentionTimeTypedSpectra(spectraProvider, unit, ionMode, acquisitionType);
                case ChromXType.Drift:
                    return new DriftTimeTypedSpectra(spectraProvider, unit, ionMode, acquisitionType);
                default:
                    throw new ArgumentException($"ChromXType {type} is not supported.");
            }
        }
    }
}

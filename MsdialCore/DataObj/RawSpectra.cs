using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj
{
    public class RawSpectra
    {
        private readonly IChromatogramTypedSpectra _spectraImpl;

        public RawSpectra(IReadOnlyList<RawSpectrum> spectra, ChromXType type, ChromXUnit unit, IonMode ionMode) {
            switch (type) {
                case ChromXType.RT:
                    _spectraImpl = new RetentionTimeTypedSpectra(spectra, unit, ionMode);
                    break;
                case ChromXType.Drift:
                    _spectraImpl = new DriftTimeTypedSpectra(spectra, unit, ionMode);
                    break;
                default:
                    throw new ArgumentException($"ChromXType {type} is not supported.");
            }
        }

        public Chromatogram GetMs1ExtractedChromatogram(double mz, double tolerance, double start, double end) {
            return _spectraImpl.GetMs1ExtractedChromatogram(mz, tolerance, start, end);
        }

        public Chromatogram GetMs1TotalIonChromatogram(double start, double end) {
            return _spectraImpl.GetMs1TotalIonChromatogram(start, end);
        }

        public Chromatogram GetMs1BasePeakChromatogram(double start, double end) {
            return _spectraImpl.GetMs1BasePeakChromatogram(start, end);
        }
    }
}

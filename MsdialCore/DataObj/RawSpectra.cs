using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
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

        public Chromatogram GetMs1ExtractedChromatogramByHighestBasePeakMz(IEnumerable<ISpectrumPeak> peaks, double tolerance, double start, double end) {
            var mz = peaks.Argmax(feature => feature.Intensity).Mass;
            return _spectraImpl.GetMs1ExtractedChromatogram(mz, tolerance, start, end);
            // if (spectrumList.IsEmptyOrNull()) return null;
            // if (features.IsEmptyOrNull()) return null;

            // var maxSpotID = 0;
            // var maxIntensity = double.MinValue;
            // for (int i = 0; i < features.Count; i++) {
            //     if (features[i].PeakHeightTop > maxIntensity) {
            //         maxIntensity = features[i].PeakHeightTop;
            //         maxSpotID = i;
            //     }
            // }
            // var hSpot = features[maxSpotID];
            // var rawSpectrum = new RawSpectra(spectrumList, type, unit, ionmode);
            // return rawSpectrum.GetMs1ExtractedChromatogram(hSpot.PrecursorMz, mzTol, chromBegin, chromEnd);
        }

    }
}

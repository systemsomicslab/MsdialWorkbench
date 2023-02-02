using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    public class PeakMs2Spectra
    {
        private readonly IReadOnlyList<RawSpectrum> _spectra;
        private readonly ScanPolarity _scanPolarity;
        private readonly AcquisitionType _acquisitionType;

        public PeakMs2Spectra(IReadOnlyList<RawSpectrum> spectra, ScanPolarity scanPolarity, AcquisitionType acquisitionType) {
            _spectra = spectra ?? throw new ArgumentNullException(nameof(spectra));
            _scanPolarity = scanPolarity;
            _acquisitionType = acquisitionType;
        }

        public bool IsEmpty => _spectra.Count == 0;

        public int FindOriginalIndexPeakTop(int peakTopOriginalIndex) {
            return _spectra.Argmin(spec => Math.Abs(spec.OriginalIndex - peakTopOriginalIndex)).OriginalIndex;
        }

        public Dictionary<int, double> FindOriginalIndexToCollisionEnergyAtPeakTop(int peakTopOriginalIndex) {
            if (_acquisitionType == AcquisitionType.AIF) {
                var result = new Dictionary<int, double>();
                foreach (var group in _spectra.GroupBy(spec => Math.Round(spec.CollisionEnergy, 2))) { // must be rounded by 2 decimal points
                    var nearTop = group.Argmin(spec => Math.Abs(spec.OriginalIndex - peakTopOriginalIndex));
                    result[nearTop.OriginalIndex] = nearTop.CollisionEnergy;
                }
                return result;
            }

            var spectrum = _spectra.Argmin(spec => Math.Abs(spec.OriginalIndex - peakTopOriginalIndex));
            return new Dictionary<int, double> { { spectrum.OriginalIndex, spectrum.CollisionEnergy }, };
        }

        public IReadOnlyList<RawSpectrum> Spectra => _spectra;
    }
}

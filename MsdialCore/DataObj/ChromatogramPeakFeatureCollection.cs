using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Parameter;
using System.Collections;
using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class ChromatogramPeakFeatureCollection : IReadOnlyList<ChromatogramPeakFeature>
    {
        private readonly List<ChromatogramPeakFeature> _chromatogramPeakFeatures;

        public ChromatogramPeakFeatureCollection(List<ChromatogramPeakFeature> chromatogramPeakFeatures) {
            _chromatogramPeakFeatures = chromatogramPeakFeatures ?? new List<ChromatogramPeakFeature>(0);
        }

        public bool IsEmpty => _chromatogramPeakFeatures.Count == 0;

        /// <summary>
        /// peak list should contain the original raw spectrum ID
        /// </summary>
        /// <param name="chromPeakFeatures"></param>
        /// <param name="spectrumList"></param>
        /// <param name="peaklist"></param>
        /// <param name="param"></param>
        public void SetRawDataAccessID2ChromatogramPeakFeatures(
            IReadOnlyList<RawSpectrum> ms1Spectra,
            IReadOnlyList<RawSpectrum> ms2Spectra,
            IReadOnlyList<ChromatogramPeak> peaklist,
            ParameterBase param) {

            var peakSpotting = new PeakSpottingCore();
            foreach (var feature in _chromatogramPeakFeatures) {
                peakSpotting.SetMS2RawSpectrumIDs2ChromatogramPeakFeature(feature, ms1Spectra, ms2Spectra, param);
            }
        }

        // implement IReadOnlyList<ChromatogramPeakFeature>
        public ChromatogramPeakFeature this[int index] => ((IReadOnlyList<ChromatogramPeakFeature>)_chromatogramPeakFeatures)[index];

        public int Count => ((IReadOnlyCollection<ChromatogramPeakFeature>)_chromatogramPeakFeatures).Count;

        public IEnumerator<ChromatogramPeakFeature> GetEnumerator() {
            return ((IEnumerable<ChromatogramPeakFeature>)_chromatogramPeakFeatures).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable)_chromatogramPeakFeatures).GetEnumerator();
        }
    }
}

using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class PeakDetectionResultCollection
    {
        public PeakDetectionResultCollection(List<PeakDetectionResult> peaks, ChromatogramPeakCollection rawChromatogram) {
            _peaks = peaks;
            Peaks = _peaks.AsReadOnly();

            RawChromatogram = rawChromatogram;
        }

        public ReadOnlyCollection<PeakDetectionResult> Peaks { get; }
        private readonly List<PeakDetectionResult> _peaks;

        public ChromatogramPeakCollection RawChromatogram { get; }

        public ChromatogramPeakFeatureCollection FilteringPeaks(ParameterBase parameter, ChromXType type, ChromXUnit unit) {
            var filteredPeaks = Peaks.Where(result => result.IntensityAtPeakTop > 0);
            var filteredPairs = filteredPeaks.Select(result => (result, RawChromatogram[result.ScanNumAtPeakTop].Mass));
            //option
            //this method is currently used in LC/MS project.
            //Users can prepare their-own 'exclusion mass' list to exclude unwanted peak features
            foreach (var query in parameter.ExcludedMassList.OrEmptyIfNull()) {
                filteredPairs = filteredPairs.Where(pair => Math.Abs(query.Mass - pair.Mass) >= query.MassTolerance);
            }
            var chromPeakFeatures = filteredPairs.Select(pair => ChromatogramPeakFeature.FromPeakDetectionResult(pair.result, type, unit, pair.Mass, parameter.IonMode)).ToList();
            foreach (var feature in chromPeakFeatures) {
                feature.MS1RawSpectrumIdLeft = RawChromatogram[feature.ChromScanIdLeft].ID; // ??
                feature.MS1RawSpectrumIdTop = RawChromatogram[feature.ChromScanIdTop].ID;
                feature.MS1RawSpectrumIdRight = RawChromatogram[feature.ChromScanIdRight].ID;
            }

            return new ChromatogramPeakFeatureCollection(chromPeakFeatures);
        }
    }
}

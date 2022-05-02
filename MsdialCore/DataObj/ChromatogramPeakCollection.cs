using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Utility;
using System.Collections;
using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class ChromatogramPeakCollection : IReadOnlyList<ChromatogramPeak>
    {
        private readonly List<ChromatogramPeak> _peaks;

        public ChromatogramPeakCollection(List<ChromatogramPeak> peaks) {
            _peaks = peaks ?? new List<ChromatogramPeak>(0);
        }

        public bool IsEmpty => _peaks.Count == 0;

        public ChromatogramPeakCollection Smoothing(SmoothingMethod method, int level) {
            return new ChromatogramPeakCollection(DataAccess.GetSmoothedPeaklist(_peaks, method, level));
        }

        public PeakDetectionResultCollection DetectPeaks(ChromatogramPeakCollection smoothedChromatogram, double minimumDataPoint, double minimumAmplitude) {
            var peaks = PeakDetection.PeakDetectionVS1(smoothedChromatogram._peaks, minimumDataPoint, minimumAmplitude) ?? new List<PeakDetectionResult>(0);
            return new PeakDetectionResultCollection(peaks, this);
        }

        // implement IReadOnlyList<ChromatogramPeak>
        public int Count => ((IReadOnlyCollection<ChromatogramPeak>)_peaks).Count;

        public ChromatogramPeak this[int index] => ((IReadOnlyList<ChromatogramPeak>)_peaks)[index];

        public IEnumerator<ChromatogramPeak> GetEnumerator() {
            return ((IEnumerable<ChromatogramPeak>)_peaks).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable)_peaks).GetEnumerator();
        }
    }
}

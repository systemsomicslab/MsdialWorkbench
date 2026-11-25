using CompMs.Common.Components;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Algorithm.Function
{
    /// <summary>
    /// A spectrum binned by frame (int)(mass / bin): the summed intensity of each frame and the m/z of
    /// its most intense peak (the first one on a tie), in ascending frame order. This is the one place
    /// that binning rule is written; SpectrumHandler.GetBinnedSpectrum and the spectral entropy
    /// similarity both read it from here.
    /// </summary>
    internal readonly struct FrameBinnedSpectrum
    {
        public readonly int[] Frames;
        public readonly double[] Intensities;
        public readonly double[] Masses;
        public readonly int Count;

        private FrameBinnedSpectrum(int[] frames, double[] intensities, double[] masses, int count) {
            Frames = frames;
            Intensities = intensities;
            Masses = masses;
            Count = count;
        }

        public static FrameBinnedSpectrum Create(List<SpectrumPeak> spectrum, double bin) => Create(new SpectrumPeakData(spectrum), bin);

        public static FrameBinnedSpectrum Create(SpectrumPeakData spectrum, double bin) {
            // Spectra are almost always sorted by m/z already; sorting is only the fallback. Frames are
            // then non-decreasing, so the peaks of one frame are adjacent and one pass bins them.
            var order = IsSortedByMass(spectrum) ? null : Enumerable.Range(0, spectrum.Count).OrderBy(index => spectrum[index].Mass).ToArray();
            var frames = new int[spectrum.Count];
            var intensities = new double[spectrum.Count];
            var masses = new double[spectrum.Count];
            var count = 0;
            var maxIntensity = 0d; // of the frame being filled, the last one
            for (var index = 0; index < spectrum.Count; index++) {
                var peak = spectrum[order is null ? index : order[index]];
                var frame = (int)(peak.Mass / bin);
                if (count > 0 && frames[count - 1] == frame) {
                    intensities[count - 1] += peak.Intensity;
                    if (peak.Intensity > maxIntensity) {
                        maxIntensity = peak.Intensity;
                        masses[count - 1] = peak.Mass;
                    }
                }
                else {
                    frames[count] = frame;
                    intensities[count] = peak.Intensity;
                    masses[count] = peak.Mass;
                    maxIntensity = peak.Intensity;
                    count++;
                }
            }
            return new FrameBinnedSpectrum(frames, intensities, masses, count);
        }

        private static bool IsSortedByMass(SpectrumPeakData peaks) {
            for (int i = 1; i < peaks.Count; i++) {
                if (peaks[i - 1].Mass > peaks[i].Mass) {
                    return false;
                }
            }
            return true;
        }
    }
}

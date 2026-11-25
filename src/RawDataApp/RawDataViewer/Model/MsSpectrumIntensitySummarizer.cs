using CompMs.Common.DataObj;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Algorithm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.App.RawDataViewer.Model
{
    public class MsSpectrumIntensitySummarizer
    {
        public async Task<MsSpectrumSummary[]> SummarizeAsync(Task<IDataProvider> providerTask) {
            var provider = await providerTask.ConfigureAwait(false);
            var spectra = await provider.LoadMsSpectrumsAsync(default).ConfigureAwait(false);
            (var ms1, var ms2) = CountIntensityBins(spectra);
            return new[]
            {
                new MsSpectrumSummary(ConvertFreqToDataPoint(ms1), "Histogram of ms1 spectrum intensity"),
                new MsSpectrumSummary(ConvertFreqToDataPoint(ms2), "Histogram of ms2 spectrum intensity"),
            };
        }

        /// <summary>
        /// Counts the peaks of MS1 spectra (MS level below 2) and of all other spectra separately, in bins
        /// 2^n &lt;= intensity &lt; 2^(n+1) keyed by n. Only finite intensities above 0 are counted.
        /// </summary>
        /// <remarks>
        /// The viewer used to take these counts from RawDataAccess.ParseRawdataForStatistics in RawDataHandler, and it is
        /// that method's only caller. The method pre-fills the keys 0 to 40 and increments them by index, so a single
        /// intensity between 0 and 1 (a negative n) or an infinite one throws KeyNotFoundException, and since one call
        /// builds both levels, both histograms were lost. Vendor centroiding does write intensities below 1 into mzML,
        /// so the viewer counts here and gives every power of two that occurs a bin of its own.
        /// The bins are RawDataHandler's, the same Math.Log(intensity, 2) being floored, wherever it does not fold:
        /// it counts every n above 40 in the 2^40 bin, which the chart labels "2^40 &lt;= I &lt; 2^41". That folding
        /// only served the fixed keys and is not kept, so the two differ from 2^41 up, and for the 31 doubles within
        /// 0.008 below 2^41 that Math.Log already rounds up to 41.
        /// </remarks>
        public static (SortedDictionary<int, int> Ms1, SortedDictionary<int, int> Ms2) CountIntensityBins(IEnumerable<RawSpectrum> spectra) {
            // A file has tens of millions of peaks and only a few dozen bins, so each peak is counted in an array indexed
            // by n, and the bins are made from the counts once at the end.
            var ms1 = new int[LargestBin - SmallestBin + 1];
            var ms2 = new int[LargestBin - SmallestBin + 1];
            foreach (var spectrum in spectra ?? Enumerable.Empty<RawSpectrum>()) {
                if (spectrum?.Spectrum is null) {
                    continue;
                }
                var counts = spectrum.MsLevel < 2 ? ms1 : ms2;
                foreach (var peak in spectrum.Spectrum) {
                    var intensity = peak.Intensity;
                    // Zero, negative values and NaN have no logarithm, and an infinite intensity is not a measurement.
                    if (!(intensity > 0d) || double.IsPositiveInfinity(intensity)) {
                        continue;
                    }
                    var n = (int)Math.Floor(Math.Log(intensity, 2));
                    // Math.Log rounds the largest doubles up to 1024, whose 2^n is infinite. No finite double reaches 2^1024.
                    if (n > LargestBin) {
                        n = LargestBin;
                    }
                    // Not reached on today's runtimes (see SmallestBin), but a logarithm one rounding step lower would
                    // index outside the counts, which is exactly the kind of failure this method exists to avoid.
                    else if (n < SmallestBin) {
                        n = SmallestBin;
                    }
                    counts[n - SmallestBin]++;
                }
            }
            return (ToBins(ms1), ToBins(ms2));
        }

        // The smallest positive double, double.Epsilon, is 2^-1074, and Math.Log(double.Epsilon, 2) is exactly -1074, so no
        // n should fall below the first bin; the counting loop clamps it there anyway.
        private const int SmallestBin = -1074;
        private const int LargestBin = 1023;

        private static SortedDictionary<int, int> ToBins(int[] counts) {
            var bins = new SortedDictionary<int, int>();
            for (int i = 0; i < counts.Length; i++) {
                if (counts[i] > 0) {
                    bins.Add(i + SmallestBin, counts[i]);
                }
            }
            return bins;
        }

        private static DataPoint[] ConvertFreqToDataPoint(IDictionary<int, int> binToFreq) {
            return binToFreq.OrderBy(kvp => kvp.Key)
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => new DataPoint { X = Math.Pow(2, kvp.Key), Y = kvp.Value, })
                .ToArray();
        }
    }
}

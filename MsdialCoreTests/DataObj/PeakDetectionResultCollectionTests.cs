using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Query;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass()]
    public class PeakDetectionResultCollectionTests
    {
        [TestMethod()]
        public void FilteringPeaksTest() {
            var rnd = new Random(4869);

            var peaks = new ChromatogramPeakCollection(Enumerable.Range(0, 100).Select(i => new ChromatogramPeak { ID = i, Intensity = Math.Pow(rnd.NextDouble(), 5) * 1000_000d, ChromXs = new ChromXs(i / 100d), Mass = rnd.NextDouble() * 1000d }).ToList());
            var smoothedPeaks = peaks.Smoothing(SmoothingMethod.LinearWeightedMovingAverage, 5);
            var detectedPeaks = peaks.DetectPeaks(smoothedPeaks, 1, 0);

            var parameter = new ParameterBase();
            parameter.ExcludedMassList.Add(new MzSearchQuery { Mass = peaks[detectedPeaks.Peaks[0].ScanNumAtPeakTop].Mass, MassTolerance = 0.001, });
            parameter.ExcludedMassList.Add(new MzSearchQuery { Mass = peaks[detectedPeaks.Peaks[1].ScanNumAtPeakTop].Mass, MassTolerance = 0.001, });
            parameter.ExcludedMassList.Add(new MzSearchQuery { Mass = peaks[detectedPeaks.Peaks[4].ScanNumAtPeakTop].Mass, MassTolerance = 0.001, });

            var actual = detectedPeaks.FilteringPeaks(parameter, ChromXType.RT, ChromXUnit.Min);

            var filteredPeaks = detectedPeaks.Peaks.Where(result => result.IntensityAtPeakTop > 0);
            var filteredPairs = filteredPeaks.Select(result => (result, peaks[result.ScanNumAtPeakTop].Mass));
            var bad = filteredPairs.Select(pair => ChromatogramPeakFeature.FromPeakDetectionResult(pair.result, ChromXType.RT, ChromXUnit.Min, pair.Mass, parameter.IonMode)).ToList();
            foreach (var query in parameter.ExcludedMassList) {
                filteredPairs = filteredPairs.Where(pair => Math.Abs(query.Mass - pair.Mass) >= query.MassTolerance);
            }
            var expected = filteredPairs.Select(pair => ChromatogramPeakFeature.FromPeakDetectionResult(pair.result, ChromXType.RT, ChromXUnit.Min, pair.Mass, parameter.IonMode)).ToList();

            Console.WriteLine($"Number of peaks (expected): {expected.Count}");
            Console.WriteLine($"Number of peaks (bad): {bad.Count}");
            Assert.AreEqual(expected.Count, actual.Count);
            foreach (var (e, a) in expected.Zip(actual)) {
                Assert.AreEqual(e.ChromXsTop.RT.Value, a.ChromXsTop.RT.Value);
                Assert.AreEqual(e.PeakHeightTop, a.PeakHeightTop);
            }
        }
    }
}
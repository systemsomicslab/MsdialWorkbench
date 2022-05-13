using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass()]
    public class ChromatogramPeakCollectionTests
    {
        [TestMethod()]
        public void SmoothingTest() {
            var rnd = new Random(4869);
            var peaks = new ChromatogramPeakCollection(Enumerable.Range(0, 100).Select(i => new ChromatogramPeak { ID = i, Intensity = Math.Pow(rnd.NextDouble(), 5) * 1000_000d, ChromXs = new ChromXs(i / 100d) }).ToList());

            var expected = DataAccess.GetSmoothedPeaklist(peaks, SmoothingMethod.LinearWeightedMovingAverage, 5);
            var actual = peaks.Smoothing(SmoothingMethod.LinearWeightedMovingAverage, 5);

            Assert.AreEqual(expected.Count, actual.Count);
            foreach (var (e, a) in expected.Zip(actual)) {
                Assert.AreEqual(e.ID, a.ID);
                Assert.AreEqual(e.Intensity, a.Intensity);
            }
        }

        [TestMethod()]
        public void DetectPeaksTest() {
            var rnd = new Random(4869);
            var rawPeaks = Enumerable.Range(0, 100).Select(i => new ChromatogramPeak { ID = i, Intensity = Math.Pow(rnd.NextDouble(), 5) * 1000_000d, ChromXs = new ChromXs(i / 100d) }).ToList();
            var smoothedPeaks = DataAccess.GetSmoothedPeaklist(rawPeaks, SmoothingMethod.LinearWeightedMovingAverage, 5);
            var expected = PeakDetection.PeakDetectionVS1(smoothedPeaks, 1, 0);

            var rawPeakCollection = new ChromatogramPeakCollection(rawPeaks);
            var smoothedPeakCollection = new ChromatogramPeakCollection(smoothedPeaks);
            var actual = rawPeakCollection.DetectPeaks(smoothedPeakCollection, 1, 0);

            Console.WriteLine($"Number of peaks: {expected.Count}");
            Assert.AreEqual(expected.Count, actual.Peaks.Count);
            foreach (var (e, a) in expected.Zip(actual.Peaks)) {
                Assert.AreEqual(e.IntensityAtPeakTop, a.IntensityAtPeakTop);
                Assert.AreEqual(e.IntensityAtLeftPeakEdge, a.IntensityAtLeftPeakEdge);
                Assert.AreEqual(e.IntensityAtRightPeakEdge, a.IntensityAtRightPeakEdge);
            }
        }
    }
}
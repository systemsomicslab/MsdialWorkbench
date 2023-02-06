using CompMs.Common.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Algorithm.PeakPick.Tests
{
    [TestClass()]
    public class PeakDetectionTests
    {
        [TestMethod()]
        public void PeakDetectionVS1Test1()
        {
            var actual = PeakDetection.PeakDetectionVS1(new List<ChromatogramPeak>(), 5, 1000);
            Assert.AreEqual(0, actual.Count);
        }

        [TestMethod()]
        public void PeakDetectionVS1Test2()
        {
            var peak1 = BuildPeak(0, 10, 700d, 10000d, 1000000d, 0.01);
            var peak2 = BuildPeak(21, 10, 800d, 10000d, 2000000d, peak1.Last().ChromXs.Value + 0.002);
            var actual = PeakDetection.PeakDetectionVS1(peak1.Concat(peak2).ToList(), 5, 1000);

            Assert.AreEqual(2, actual.Count);
            Assert.AreEqual(0, actual[0].PeakID);
            Assert.AreEqual(1, actual[1].PeakID);
            Assert.AreEqual(1000000d, actual[0].IntensityAtPeakTop);
            Assert.AreEqual(2000000d, actual[1].IntensityAtPeakTop);
            Assert.AreEqual(1000000d / 2000000d, actual[0].AmplitudeScoreValue);
            Assert.AreEqual(2000000d / 2000000d, actual[1].AmplitudeScoreValue);
            Assert.AreEqual(2, actual[0].AmplitudeOrderValue);
            Assert.AreEqual(1, actual[1].AmplitudeOrderValue);
            Assert.AreEqual(0, actual[0].ScanNumAtLeftPeakEdge);
            Assert.AreEqual(10, actual[0].ScanNumAtPeakTop);
            Assert.AreEqual(19, actual[0].ScanNumAtRightPeakEdge);
            Assert.AreEqual(19, actual[1].ScanNumAtLeftPeakEdge);
            Assert.AreEqual(31, actual[1].ScanNumAtPeakTop);
            Assert.AreEqual(41, actual[1].ScanNumAtRightPeakEdge);
        }

        private ChromatogramPeak[] BuildPeak(int start, int radian, double mass, double baseIntensity, double topIntensity, double baseTime)
        {
            var results = Enumerable.Range(start, radian * 2 + 1).Select(i => ChromatogramPeak.Create(i, mass, baseIntensity, new RetentionTime(baseTime + i * 0.002))).ToArray();
            for (int i = 0; i <= radian; i++) {
                results[radian - i].Intensity = results[radian + i].Intensity = baseIntensity + (topIntensity - baseIntensity) / (i + 1) / (i + 1);
            }
            return results;
        }
    }
}
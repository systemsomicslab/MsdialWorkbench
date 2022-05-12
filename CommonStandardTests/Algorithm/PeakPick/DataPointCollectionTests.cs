using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CompMs.Common.Algorithm.PeakPick.Tests
{
    [TestClass()]
    public class DataPointCollectionTests
    {
        [TestMethod()]
        public void GetPeakDetectionResultIdealSlopeValueTest() {
            Chromatogram stubChromatogram = ChromatogramTest.BuildSample();
            ChromatogramBaseline stubBaseline = new ChromatogramBaseline(0, false, 0, 1d);
            var collection = new DataPointCollection(stubChromatogram, stubBaseline);
            collection.AddPoint(0);
            collection.AddPoint(1);
            collection.AddPoint(2);
            collection.AddPoint(3);
            collection.AddPoint(4);
            var result = collection.GetPeakDetectionResult(2, 0, 10000d);
            Console.WriteLine(result.IdealSlopeValue);
            Assert.AreEqual(.95, result.IdealSlopeValue, 1e-6);
        }

        [TestMethod()]
        public void AddPointsTest() {
            var chromatogram = ChromatogramTest.BuildSample();
            var baseline = new ChromatogramBaseline(0d, false, 0d, 1d);

            var collection = new DataPointCollection(chromatogram, baseline);
            collection.AddPoints(new Peak(1, 3));
            Assert.AreEqual(3, collection.Count);
            for (int i = 0; i < collection.Count; i++) {
                Assert.AreEqual(chromatogram.CreateDataPoint(1 + i).ChromValue, collection[i].ChromValue);
                Assert.AreEqual(chromatogram.CreateDataPoint(1 + i).Intensity, collection[i].Intensity);
            }
        }
    }
}

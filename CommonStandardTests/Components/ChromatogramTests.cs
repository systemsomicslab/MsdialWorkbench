using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Components.Tests
{
    [TestClass()]
    public class ChromatogramTests
    {
        [TestMethod()]
        public void SmoothingSimpleTest() {
            var rawdata = new[]
            {
                new ChromatogramPeak(0, 0, 100d, 1000d, new RetentionTime(0d)),
                new ChromatogramPeak(1, 1, 101d, 1001d, new RetentionTime(1d)),
                new ChromatogramPeak(2, 2, 102d, 1002d, new RetentionTime(2d)),
                new ChromatogramPeak(3, 3, 103d, 1003d, new RetentionTime(3d)),
                new ChromatogramPeak(4, 4, 104d, 1004d, new RetentionTime(4d)),
                new ChromatogramPeak(5, 5, 105d, 1005d, new RetentionTime(5d)),
                new ChromatogramPeak(6, 6, 106d, 1006d, new RetentionTime(6d)),
                new ChromatogramPeak(7, 7, 107d, 1007d, new RetentionTime(7d)),
                new ChromatogramPeak(8, 8, 108d, 1008d, new RetentionTime(8d)),
                new ChromatogramPeak(9, 9, 109d, 1009d, new RetentionTime(9d)),
            };
            var chromatogram = new Chromatogram(rawdata, ChromXType.RT, ChromXUnit.Min);
            var actual = chromatogram.Smoothing(SmoothingMethod.SimpleMovingAverage, 2);
            Assert.AreEqual(10, actual.Count);
            Assert.AreEqual(5003d / 5, actual[0].Intensity);
            Assert.AreEqual(5007d / 5, actual[1].Intensity);
            Assert.AreEqual(5010d / 5, actual[2].Intensity);
            Assert.AreEqual(5015d / 5, actual[3].Intensity);
            Assert.AreEqual(5020d / 5, actual[4].Intensity);
            Assert.AreEqual(5025d / 5, actual[5].Intensity);
            Assert.AreEqual(5030d / 5, actual[6].Intensity);
            Assert.AreEqual(5035d / 5, actual[7].Intensity);
            Assert.AreEqual(5038d / 5, actual[8].Intensity);
            Assert.AreEqual(5042d / 5, actual[9].Intensity);
            for (int i = 0; i < 10; i++) {
                // Assert.AreEqual(rawdata[i].ID, actual[i].ID);
                Assert.AreEqual(rawdata[i].Mass, actual[i].Mass);
                Assert.AreEqual(rawdata[i].ChromXs.Value, actual[i].ChromXs.Value);
            }
        }
    }
}
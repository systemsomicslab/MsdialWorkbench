using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Components.Tests
{
    public static class BaseChromatogramPeakFeatureTestHelper {
        public static void AreEqual(this Assert assert, BaseChromatogramPeakFeature expected, BaseChromatogramPeakFeature actual) {
            Assert.AreEqual(expected.ChromScanIdLeft, actual.ChromScanIdLeft);
            Assert.AreEqual(expected.ChromScanIdTop, actual.ChromScanIdTop);
            Assert.AreEqual(expected.ChromScanIdRight, actual.ChromScanIdRight);
            assert.AreEqual(expected.ChromXsLeft, actual.ChromXsLeft);
            assert.AreEqual(expected.ChromXsTop, actual.ChromXsTop);
            assert.AreEqual(expected.ChromXsRight, actual.ChromXsRight);
            Assert.AreEqual(expected.PeakHeightLeft, actual.PeakHeightLeft);
            Assert.AreEqual(expected.PeakHeightTop, actual.PeakHeightTop);
            Assert.AreEqual(expected.PeakHeightRight, actual.PeakHeightRight);
            Assert.AreEqual(expected.PeakAreaAboveZero, actual.PeakAreaAboveZero);
            Assert.AreEqual(expected.PeakAreaAboveBaseline, actual.PeakAreaAboveBaseline);
            Assert.AreEqual(expected.Mass, actual.Mass);
        }
    }
}
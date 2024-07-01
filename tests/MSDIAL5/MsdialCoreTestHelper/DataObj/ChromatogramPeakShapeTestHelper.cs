using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.MsdialCore.DataObj.Tests
{
    public static class ChromatogramPeakShapeTestHelper
    {
        public static void AreEqual(this Assert assert, ChromatogramPeakShape expected, ChromatogramPeakShape actual) {
            Assert.AreEqual(expected.EstimatedNoise, actual.EstimatedNoise);
            Assert.AreEqual(expected.SignalToNoise, actual.SignalToNoise);
            Assert.AreEqual(expected.PeakPureValue, actual.PeakPureValue);
            Assert.AreEqual(expected.ShapenessValue, actual.ShapenessValue);
            Assert.AreEqual(expected.GaussianSimilarityValue, actual.GaussianSimilarityValue);
            Assert.AreEqual(expected.IdealSlopeValue, actual.IdealSlopeValue);
            Assert.AreEqual(expected.BasePeakValue, actual.BasePeakValue);
            Assert.AreEqual(expected.SymmetryValue, actual.SymmetryValue);
            Assert.AreEqual(expected.AmplitudeOrderValue, actual.AmplitudeOrderValue);
            Assert.AreEqual(expected.AmplitudeScoreValue, actual.AmplitudeScoreValue);
        }
    }
}

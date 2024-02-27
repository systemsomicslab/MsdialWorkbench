using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Components.Tests
{
    public static class SpectrumPeakTestHelper
    {
        public static void AreEqual(this Assert assert, SpectrumPeak expected, SpectrumPeak actual) {
            Assert.AreEqual(expected.Mass, actual.Mass);
            Assert.AreEqual(expected.Intensity, actual.Intensity);
            Assert.AreEqual(expected.Comment, actual.Comment);
            Assert.AreEqual(expected.PeakQuality, actual.PeakQuality);
            Assert.AreEqual(expected.PeakID, actual.PeakID);
            Assert.AreEqual(expected.SpectrumComment, actual.SpectrumComment);
            Assert.AreEqual(expected.IsAbsolutelyRequiredFragmentForAnnotation, actual.IsAbsolutelyRequiredFragmentForAnnotation);
        }
    }
}

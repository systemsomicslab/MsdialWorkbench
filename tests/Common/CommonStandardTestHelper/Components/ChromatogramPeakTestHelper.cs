using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Components.Tests
{
    public static class ChromatogramPeakTestHelper
    {
        public static void AreEqual(this Assert assert, ChromatogramPeak expected, ChromatogramPeak actual) {
            Assert.AreEqual(expected.ID, actual.ID);
            Assert.AreEqual(expected.Mass, actual.Mass);
            Assert.AreEqual(expected.Intensity, actual.Intensity);
            assert.AreEqual(expected.ChromXs, actual.ChromXs);
        }
    }
}

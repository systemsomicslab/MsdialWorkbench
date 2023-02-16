using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Components.Tests
{
    public static class ChromXsTestHelper {
        public static void AreEqual(this Assert assert, ChromXs expected, ChromXs actual) {
            assert.AreEqual(expected.RT, actual.RT);
            assert.AreEqual(expected.RI, actual.RI);
            assert.AreEqual(expected.Drift, actual.Drift);
            assert.AreEqual(expected.Mz, actual.Mz);
            Assert.AreEqual(expected.MainType, actual.MainType);
        }
    }
}

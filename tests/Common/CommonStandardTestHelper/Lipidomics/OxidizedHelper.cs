using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Lipidomics.Tests
{
    public static class OxidizedHelper
    {
        public static void AreEqual(this Assert assert, IOxidized expected, IOxidized actual) {
            Assert.AreEqual(expected.Count, actual.Count);
            Assert.AreEqual(expected.DecidedCount, actual.DecidedCount);
            Assert.AreEqual(expected.UnDecidedCount, actual.UnDecidedCount);
            for (int i = 0; i < expected.DecidedCount; i++) {
                Assert.AreEqual(expected.Oxidises[i], actual.Oxidises[i]);
            }
        }
    }
}

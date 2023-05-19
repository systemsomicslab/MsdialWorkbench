using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Lipidomics.Tests
{
    public static class DoubleBondHelper
    {
        public static void AreEqual(this Assert assert, IDoubleBondInfo expected, IDoubleBondInfo actual) {
            Assert.AreEqual(expected.State, actual.State);
            Assert.AreEqual(expected.Position, actual.Position);
        }

        public static void AreEqual(this Assert assert, IDoubleBond expected, IDoubleBond actual) {
            Assert.AreEqual(expected.Count, actual.Count);
            Assert.AreEqual(expected.DecidedCount, actual.DecidedCount);
            Assert.AreEqual(expected.UnDecidedCount, actual.UnDecidedCount);
            for (int i = 0; i < expected.DecidedCount; i++) {
                assert.AreEqual(expected.Bonds[i], actual.Bonds[i]);
            }
        }
    }
}

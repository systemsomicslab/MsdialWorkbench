using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Lipidomics.Tests
{
    public static class ChainHelper
    {
        public static void AreEqual(this Assert assert, IChain expected, IChain actual) {
            Assert.IsInstanceOfType(actual, expected.GetType());
            assert.AreEqual(expected.DoubleBond, actual.DoubleBond);
            assert.AreEqual(expected.Oxidized, actual.Oxidized);
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Components.Tests
{
    public static class ChromXHelper
    {
        public static void AreEqual(this Assert assert, IChromX expected, IChromX actual) {
            Assert.AreEqual(expected.Value, actual.Value);
            Assert.AreEqual(expected.Unit, actual.Unit);
            Assert.AreEqual(expected.Type, actual.Type);
        }
    }
}

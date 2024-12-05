using CompMs.Common.Tests.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.DataObj.Property.Tests
{
    public static class FormulaTestHelper
    {
        public static void AreEqual(this Assert assert, Formula expected, Formula actual) {
            Assert.AreEqual(expected.FormulaString, actual.FormulaString);
            Assert.AreEqual(expected.Mass, actual.Mass);
            Assert.AreEqual(expected.M1IsotopicAbundance, actual.M1IsotopicAbundance);
            Assert.AreEqual(expected.M2IsotopicAbundance, actual.M2IsotopicAbundance);
            Assert.AreEqual(expected.TmsCount, actual.TmsCount);
            Assert.AreEqual(expected.MeoxCount, actual.MeoxCount);
            CollectionAssert.That.AreEquivalent(expected.Element2Count, actual.Element2Count);
        }
    }
}

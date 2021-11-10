using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class AlkylChainParserTests
    {
        [TestMethod()]
        public void ParseTest() {
            var parser = new AlkylChainParser();

            var alkyl = parser.Parse("O-16:0");
            Assert.IsInstanceOfType(alkyl, typeof(AlkylChain));
            Assert.AreEqual(16, alkyl.CarbonCount);
            Assert.AreEqual(0, alkyl.DoubleBondCount);
            Assert.AreEqual(0, alkyl.OxidizedCount);

            alkyl = parser.Parse("O-18:3");
            Assert.IsInstanceOfType(alkyl, typeof(AlkylChain));
            Assert.AreEqual(18, alkyl.CarbonCount);
            Assert.AreEqual(3, alkyl.DoubleBondCount);
            Assert.AreEqual(0, alkyl.OxidizedCount);

            alkyl = parser.Parse("O-18:0;O");
            Assert.IsInstanceOfType(alkyl, typeof(AlkylChain));
            Assert.AreEqual(18, alkyl.CarbonCount);
            Assert.AreEqual(0, alkyl.DoubleBondCount);
            Assert.AreEqual(1, alkyl.OxidizedCount);

            alkyl = parser.Parse("O-20:4;4O");
            Assert.IsInstanceOfType(alkyl, typeof(AlkylChain));
            Assert.AreEqual(20, alkyl.CarbonCount);
            Assert.AreEqual(4, alkyl.DoubleBondCount);
            Assert.AreEqual(4, alkyl.OxidizedCount);

            alkyl = parser.Parse("O-18:2(6,12)");
            Assert.IsInstanceOfType(alkyl, typeof(SpecificAlkylChain));
            Assert.AreEqual(18, alkyl.CarbonCount);
            Assert.AreEqual(2, alkyl.DoubleBondCount);
            Assert.AreEqual(0, alkyl.OxidizedCount);
            CollectionAssert.AreEqual(new[] { 6, 12 }, ((SpecificAlkylChain)alkyl).DoubleBondPosition);

            alkyl = parser.Parse("P-16:0");
            Assert.IsInstanceOfType(alkyl, typeof(PlasmalogenAlkylChain));
            Assert.AreEqual(16, alkyl.CarbonCount);
            Assert.AreEqual(1, alkyl.DoubleBondCount);
            Assert.AreEqual(0, alkyl.OxidizedCount);

            alkyl = parser.Parse("P-18:2");
            Assert.IsInstanceOfType(alkyl, typeof(PlasmalogenAlkylChain));
            Assert.AreEqual(18, alkyl.CarbonCount);
            Assert.AreEqual(3, alkyl.DoubleBondCount);
            Assert.AreEqual(0, alkyl.OxidizedCount);

            alkyl = parser.Parse("P-18:0;O");
            Assert.IsInstanceOfType(alkyl, typeof(PlasmalogenAlkylChain));
            Assert.AreEqual(18, alkyl.CarbonCount);
            Assert.AreEqual(1, alkyl.DoubleBondCount);
            Assert.AreEqual(1, alkyl.OxidizedCount);

            alkyl = parser.Parse("P-20:4;4O");
            Assert.IsInstanceOfType(alkyl, typeof(PlasmalogenAlkylChain));
            Assert.AreEqual(20, alkyl.CarbonCount);
            Assert.AreEqual(5, alkyl.DoubleBondCount);
            Assert.AreEqual(4, alkyl.OxidizedCount);

            alkyl = parser.Parse("P-18:2(6,12)");
            Assert.IsInstanceOfType(alkyl, typeof(SpecificAlkylChain));
            Assert.AreEqual(18, alkyl.CarbonCount);
            Assert.AreEqual(3, alkyl.DoubleBondCount);
            Assert.AreEqual(0, alkyl.OxidizedCount);
            CollectionAssert.AreEqual(new[] { 1, 6, 12 }, ((SpecificAlkylChain)alkyl).DoubleBondPosition);
        }
    }
}
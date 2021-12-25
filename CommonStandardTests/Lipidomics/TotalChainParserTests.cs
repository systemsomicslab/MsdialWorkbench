using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class TotalChainParserTests
    {
        [TestMethod()]
        public void ParseTest() {
            var parser = new TotalChainParser(2);

            var actual = parser.Parse("34:0");
            Assert.IsInstanceOfType(actual, typeof(TotalChains));
            Assert.AreEqual(34, actual.CarbonCount);
            Assert.AreEqual(0, actual.DoubleBondCount);
            Assert.AreEqual(0, actual.OxidizedCount);
            Assert.AreEqual(2, actual.ChainCount);

            actual = parser.Parse("O-34:0;O");
            Assert.IsInstanceOfType(actual, typeof(TotalChains));
            Assert.AreEqual(34, actual.CarbonCount);
            Assert.AreEqual(0, actual.DoubleBondCount);
            Assert.AreEqual(1, actual.OxidizedCount);
            Assert.AreEqual(2, actual.ChainCount);
            Assert.AreEqual(1, ((TotalChains)actual).AlkylChainCount);

            actual = parser.Parse("dO-34:0;O");
            Assert.IsInstanceOfType(actual, typeof(TotalChains));
            Assert.AreEqual(34, actual.CarbonCount);
            Assert.AreEqual(0, actual.DoubleBondCount);
            Assert.AreEqual(1, actual.OxidizedCount);
            Assert.AreEqual(2, actual.ChainCount);
            Assert.AreEqual(2, ((TotalChains)actual).AlkylChainCount);

            actual = parser.Parse("P-34:0");
            Assert.IsInstanceOfType(actual, typeof(TotalChains));
            Assert.AreEqual(34, actual.CarbonCount);
            Assert.AreEqual(1, actual.DoubleBondCount);
            Assert.AreEqual(0, actual.OxidizedCount);
            Assert.AreEqual(2, actual.ChainCount);
            Assert.AreEqual(1, ((TotalChains)actual).AlkylChainCount);

            actual = parser.Parse("16:0_18:2");
            Assert.IsInstanceOfType(actual, typeof(MolecularSpeciesLevelChains));
            Assert.AreEqual(34, actual.CarbonCount);
            Assert.AreEqual(2, actual.DoubleBondCount);
            Assert.AreEqual(0, actual.OxidizedCount);
            Assert.IsInstanceOfType(((MolecularSpeciesLevelChains)actual).Chains[0], typeof(AcylChain));
            Assert.IsInstanceOfType(((MolecularSpeciesLevelChains)actual).Chains[1], typeof(AcylChain));

            actual = parser.Parse("16:0/18:2");
            Assert.IsInstanceOfType(actual, typeof(PositionLevelChains));
            Assert.AreEqual(34, actual.CarbonCount);
            Assert.AreEqual(2, actual.DoubleBondCount);
            Assert.AreEqual(0, actual.OxidizedCount);
            Assert.IsInstanceOfType(((PositionLevelChains)actual).Chains[0], typeof(AcylChain));
            Assert.AreEqual(16, ((PositionLevelChains)actual).Chains[0].CarbonCount);
            Assert.AreEqual(0, ((PositionLevelChains)actual).Chains[0].DoubleBondCount);
            Assert.AreEqual(0, ((PositionLevelChains)actual).Chains[0].OxidizedCount);
            Assert.IsInstanceOfType(((PositionLevelChains)actual).Chains[1], typeof(AcylChain));
            Assert.AreEqual(18, ((PositionLevelChains)actual).Chains[1].CarbonCount);
            Assert.AreEqual(2, ((PositionLevelChains)actual).Chains[1].DoubleBondCount);
            Assert.AreEqual(0, ((PositionLevelChains)actual).Chains[1].OxidizedCount);

            actual = parser.Parse("O-16:0_18:2");
            Assert.IsInstanceOfType(actual, typeof(MolecularSpeciesLevelChains));
            Assert.AreEqual(34, actual.CarbonCount);
            Assert.AreEqual(2, actual.DoubleBondCount);
            Assert.AreEqual(0, actual.OxidizedCount);
            Assert.IsInstanceOfType(((MolecularSpeciesLevelChains)actual).Chains[0], typeof(AlkylChain));
            Assert.IsInstanceOfType(((MolecularSpeciesLevelChains)actual).Chains[1], typeof(AcylChain));

            actual = parser.Parse("O-16:0/O-18:2");
            Assert.IsInstanceOfType(actual, typeof(PositionLevelChains));
            Assert.AreEqual(34, actual.CarbonCount);
            Assert.AreEqual(2, actual.DoubleBondCount);
            Assert.AreEqual(0, actual.OxidizedCount);
            Assert.IsInstanceOfType(((PositionLevelChains)actual).Chains[0], typeof(AlkylChain));
            Assert.AreEqual(16, ((PositionLevelChains)actual).Chains[0].CarbonCount);
            Assert.AreEqual(0, ((PositionLevelChains)actual).Chains[0].DoubleBondCount);
            Assert.AreEqual(0, ((PositionLevelChains)actual).Chains[0].OxidizedCount);
            Assert.IsInstanceOfType(((PositionLevelChains)actual).Chains[1], typeof(AlkylChain));
            Assert.AreEqual(18, ((PositionLevelChains)actual).Chains[1].CarbonCount);
            Assert.AreEqual(2, ((PositionLevelChains)actual).Chains[1].DoubleBondCount);
            Assert.AreEqual(0, ((PositionLevelChains)actual).Chains[1].OxidizedCount);
        }
    }
}
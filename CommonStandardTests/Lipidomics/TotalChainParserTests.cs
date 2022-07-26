using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class TotalChainParserTests
    {
        [TestMethod()]
        public void ParseTest() {
            var parser = TotalChainParser.BuildParser(2);

            var actual = parser.Parse("34:0");
            Assert.IsInstanceOfType(actual, typeof(TotalChain));
            Assert.AreEqual(34, actual.CarbonCount);
            Assert.AreEqual(0, actual.DoubleBondCount);
            Assert.AreEqual(0, actual.OxidizedCount);
            Assert.AreEqual(2, actual.ChainCount);

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

            parser = TotalChainParser.BuildParser(3);
            actual = parser.Parse("16:0_18:1_18:2");
            Assert.IsInstanceOfType(actual, typeof(MolecularSpeciesLevelChains));
            Assert.AreEqual(52, actual.CarbonCount);
            Assert.AreEqual(3, actual.DoubleBondCount);
            Assert.AreEqual(0, actual.OxidizedCount);
            Assert.IsInstanceOfType(((MolecularSpeciesLevelChains)actual).Chains[0], typeof(AcylChain));
            Assert.AreEqual(16, ((MolecularSpeciesLevelChains)actual).Chains[0].CarbonCount);
            Assert.AreEqual(0, ((MolecularSpeciesLevelChains)actual).Chains[0].DoubleBondCount);
            Assert.AreEqual(0, ((MolecularSpeciesLevelChains)actual).Chains[0].OxidizedCount);
            Assert.IsInstanceOfType(((MolecularSpeciesLevelChains)actual).Chains[1], typeof(AcylChain));
            Assert.AreEqual(18, ((MolecularSpeciesLevelChains)actual).Chains[1].CarbonCount);
            Assert.AreEqual(1, ((MolecularSpeciesLevelChains)actual).Chains[1].DoubleBondCount);
            Assert.AreEqual(0, ((MolecularSpeciesLevelChains)actual).Chains[1].OxidizedCount);
            Assert.IsInstanceOfType(((MolecularSpeciesLevelChains)actual).Chains[2], typeof(AcylChain));
            Assert.AreEqual(18, ((MolecularSpeciesLevelChains)actual).Chains[2].CarbonCount);
            Assert.AreEqual(2, ((MolecularSpeciesLevelChains)actual).Chains[2].DoubleBondCount);
            Assert.AreEqual(0, ((MolecularSpeciesLevelChains)actual).Chains[2].OxidizedCount);
        }

        [TestMethod]
        public void EtherParseTest() {
            var parser = TotalChainParser.BuildEtherParser(2);
            var actual = parser.Parse("O-34:0;O");
            Assert.IsInstanceOfType(actual, typeof(TotalChain));
            Assert.AreEqual(34, actual.CarbonCount);
            Assert.AreEqual(0, actual.DoubleBondCount);
            Assert.AreEqual(1, actual.OxidizedCount);
            Assert.AreEqual(2, actual.ChainCount);
            Assert.AreEqual(1, ((TotalChain)actual).AlkylChainCount);

            actual = parser.Parse("dO-34:0;O");
            Assert.IsInstanceOfType(actual, typeof(TotalChain));
            Assert.AreEqual(34, actual.CarbonCount);
            Assert.AreEqual(0, actual.DoubleBondCount);
            Assert.AreEqual(1, actual.OxidizedCount);
            Assert.AreEqual(2, actual.ChainCount);
            Assert.AreEqual(2, ((TotalChain)actual).AlkylChainCount);

            actual = parser.Parse("P-34:0");
            Assert.IsInstanceOfType(actual, typeof(TotalChain));
            Assert.AreEqual(34, actual.CarbonCount);
            Assert.AreEqual(1, actual.DoubleBondCount);
            Assert.AreEqual(0, actual.OxidizedCount);
            Assert.AreEqual(2, actual.ChainCount);
            Assert.AreEqual(1, ((TotalChain)actual).AlkylChainCount);

            actual = parser.Parse("O-16:0_18:2");
            Assert.IsInstanceOfType(actual, typeof(MolecularSpeciesLevelChains));
            Assert.AreEqual(34, actual.CarbonCount);
            Assert.AreEqual(2, actual.DoubleBondCount);
            Assert.AreEqual(0, actual.OxidizedCount);
            Assert.IsInstanceOfType(((MolecularSpeciesLevelChains)actual).Chains[0], typeof(AlkylChain));
            Assert.IsInstanceOfType(((MolecularSpeciesLevelChains)actual).Chains[1], typeof(AcylChain));

            actual = parser.Parse("O-16:0_18:2;2O");
            Assert.IsInstanceOfType(actual, typeof(MolecularSpeciesLevelChains));
            Assert.AreEqual(34, actual.CarbonCount);
            Assert.AreEqual(2, actual.DoubleBondCount);
            Assert.AreEqual(2, actual.OxidizedCount);
            Assert.IsInstanceOfType(((MolecularSpeciesLevelChains)actual).Chains[0], typeof(AlkylChain));
            Assert.IsInstanceOfType(((MolecularSpeciesLevelChains)actual).Chains[1], typeof(AcylChain));

            actual = parser.Parse("19:0/O-17:0");
            Assert.IsInstanceOfType(actual, typeof(PositionLevelChains));
            Assert.AreEqual(36, actual.CarbonCount);
            Assert.AreEqual(0, actual.DoubleBondCount);
            Assert.AreEqual(0, actual.OxidizedCount);
            Assert.IsInstanceOfType(((PositionLevelChains)actual).Chains[0], typeof(AcylChain));
            Assert.AreEqual(19, ((PositionLevelChains)actual).Chains[0].CarbonCount);
            Assert.AreEqual(0, ((PositionLevelChains)actual).Chains[0].DoubleBondCount);
            Assert.AreEqual(0, ((PositionLevelChains)actual).Chains[0].OxidizedCount);
            Assert.IsInstanceOfType(((PositionLevelChains)actual).Chains[1], typeof(AlkylChain));
            Assert.AreEqual(17, ((PositionLevelChains)actual).Chains[1].CarbonCount);
            Assert.AreEqual(0, ((PositionLevelChains)actual).Chains[1].DoubleBondCount);
            Assert.AreEqual(0, ((PositionLevelChains)actual).Chains[1].OxidizedCount);

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

        [TestMethod]
        public void CeramideParseTest() {
            var parser = TotalChainParser.BuildCeramideParser(2);
            //var actual = parser.Parse("18:1;O2/44:2;O2");
            var actual = parser.Parse("18:1;2O/44:2;2O");
            Assert.IsInstanceOfType(actual, typeof(PositionLevelChains));
            Assert.AreEqual(62, actual.CarbonCount);
            Assert.AreEqual(3, actual.DoubleBondCount);
            Assert.AreEqual(4, actual.OxidizedCount);
            Assert.IsInstanceOfType(((PositionLevelChains)actual).Chains[0], typeof(SphingoChain));
            Assert.AreEqual(18, ((PositionLevelChains)actual).Chains[0].CarbonCount);
            Assert.AreEqual(1, ((PositionLevelChains)actual).Chains[0].DoubleBondCount);
            Assert.AreEqual(2, ((PositionLevelChains)actual).Chains[0].OxidizedCount);
            Assert.IsInstanceOfType(((PositionLevelChains)actual).Chains[1], typeof(AcylChain));
            Assert.AreEqual(44, ((PositionLevelChains)actual).Chains[1].CarbonCount);
            Assert.AreEqual(2, ((PositionLevelChains)actual).Chains[1].DoubleBondCount);
            Assert.AreEqual(2, ((PositionLevelChains)actual).Chains[1].OxidizedCount);
        }
    }
}
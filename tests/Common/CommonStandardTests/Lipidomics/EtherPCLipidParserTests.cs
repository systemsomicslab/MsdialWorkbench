using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class EtherPCLipidParserTests
    {
        private EtherPCLipidParser _parser;

        [TestInitialize]
        public void Init()
        {
            _parser = new EtherPCLipidParser();
        }

        [DataTestMethod]
        [DataRow("PC O-36:2", 771.6141911)]
        [DataRow("PC O-18:0_18:2", 771.6141911)]
        [DataRow("PC O-18:0/18:2", 771.6141911)]
        [DataRow("PC P-36:1", 771.6141911)]
        [DataRow("PC P-18:0_18:1", 771.6141911)]
        [DataRow("PC P-18:0/18:1", 771.6141911)]
        public void ParseTest(string name, double mz)
        {
            var lipid = _parser.Parse(name);
            Assert.AreEqual(mz, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.EtherPC, lipid.LipidClass);
            Assert.AreEqual(1, lipid.Chains.AcylChainCount);
            Assert.AreEqual(1, lipid.Chains.AlkylChainCount);
        }
    }
}
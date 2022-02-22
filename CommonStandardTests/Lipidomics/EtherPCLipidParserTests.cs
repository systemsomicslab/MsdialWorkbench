using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class EtherPCLipidParserTests
    {
        [TestMethod()]
        public void ParseTest() {
            var parser = new EtherPCLipidParser();

            var lipid = parser.Parse("PC O-36:2");
            Assert.AreEqual(771.6141911, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.EtherPC, lipid.LipidClass);

            lipid = parser.Parse("PC O-18:0_18:2");
            Assert.AreEqual(771.6141911, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.EtherPC, lipid.LipidClass);

            lipid = parser.Parse("PC O-18:0/18:2");
            Assert.AreEqual(771.6141911, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.EtherPC, lipid.LipidClass);

            lipid = parser.Parse("PC P-36:1");
            Assert.AreEqual(771.6141911, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.EtherPC, lipid.LipidClass);

            lipid = parser.Parse("PC P-18:0_18:1");
            Assert.AreEqual(771.6141911, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.EtherPC, lipid.LipidClass);

            lipid = parser.Parse("PC P-18:0/18:1");
            Assert.AreEqual(771.6141911, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.EtherPC, lipid.LipidClass);
        }
    }
}
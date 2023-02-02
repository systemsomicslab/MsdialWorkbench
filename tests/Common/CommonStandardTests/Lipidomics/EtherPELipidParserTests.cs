using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class EtherPELipidParserTests
    {
        [TestMethod()]
        public void ParseTest() {
            var parser = new EtherPELipidParser();

            var lipid = parser.Parse("PE O-38:7");
            Assert.AreEqual(747.5199, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.EtherPE, lipid.LipidClass);

            lipid = parser.Parse("PE O-18:2_20:5");
            Assert.AreEqual(747.5199, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.EtherPE, lipid.LipidClass);

            lipid = parser.Parse("PE O-18:2/20:5");
            Assert.AreEqual(747.5199, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.EtherPE, lipid.LipidClass);

            lipid = parser.Parse("PE P-38:6");
            Assert.AreEqual(747.5199, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.EtherPE, lipid.LipidClass);

            lipid = parser.Parse("PE P-18:1_20:5");
            Assert.AreEqual(747.5199, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.EtherPE, lipid.LipidClass);

            lipid = parser.Parse("PE P-18:1/20:5");
            Assert.AreEqual(747.5199, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.EtherPE, lipid.LipidClass);
        }
    }
}
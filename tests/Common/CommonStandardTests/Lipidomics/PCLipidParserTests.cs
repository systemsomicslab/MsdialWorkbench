using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class PCLipidParserTests
    {
        [TestMethod()]
        public void ParseTest() {
            var parser = new PCLipidParser();

            var lipid = parser.Parse("PC 34:2");
            Assert.AreEqual(757.5621552, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PC, lipid.LipidClass);

            lipid = parser.Parse("PC 16:0_18:2");
            Assert.AreEqual(757.5621552, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PC, lipid.LipidClass);

            lipid = parser.Parse("PC 16:0/18:2");
            Assert.AreEqual(757.5621552, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PC, lipid.LipidClass);

            lipid = parser.Parse("PC O-16:0/18:2");
            Assert.IsNull(lipid);

            lipid = parser.Parse("PC 16:0/O-18:2");
            Assert.IsNull(lipid);

            lipid = parser.Parse("PC O-16:0_18:2");
            Assert.IsNull(lipid);

            lipid = parser.Parse("PC 16:0_O-18:2");
            Assert.IsNull(lipid);
        }
    }
}
using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class FacadeLipidParserTests
    {
        [TestMethod()]
        public void ParseTest() {
            var parser = new FacadeLipidParser();
            parser.Add(new PCLipidParser());
            parser.Add(new EtherPELipidParser());

            var lipid = parser.Parse("PC 34:2");
            Assert.AreEqual(757.5621552, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PC, lipid.LipidClass);

            lipid = parser.Parse("PC 16:0_18:2");
            Assert.AreEqual(757.5621552, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PC, lipid.LipidClass);

            lipid = parser.Parse("PC 16:0/18:2");
            Assert.AreEqual(757.5621552, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PC, lipid.LipidClass);


            lipid = parser.Parse("PE O-38:7");
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

        [DataTestMethod]
        [DataRow("PC 34:1|PC 16:0_18:1")]
        [DataRow("PC 16:0_18:1")]
        public void Parse_ValidLipid_ReturnsILipidObject(string lipidStr) {
            var parser = new FacadeLipidParser();
            parser.Add(new PCLipidParser());

            // Act
            ILipid result = parser.Parse(lipidStr);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(LbmClass.PC, result.LipidClass);
            Assert.AreEqual("16:0_18:1", result.Chains.ToString());
        }

        [TestMethod]
        public void Parse_InvalidLipid_ReturnsNull() {
            var parser = new FacadeLipidParser();

            // Arrange
            string lipidStr = "InvalidLipid";

            // Act
            ILipid result = parser.Parse(lipidStr);

            // Assert
            Assert.IsNull(result);
        }
    }
}
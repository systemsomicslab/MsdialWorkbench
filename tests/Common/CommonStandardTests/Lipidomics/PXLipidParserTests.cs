using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class PELipidParserTests
    {
        [TestMethod()]
        public void ParseTest() {
            var parser = new PELipidParser();

            var lipid = parser.Parse("PE 38:5");
            Assert.AreEqual(765.5335, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PE, lipid.LipidClass);

            lipid = parser.Parse("PE 18:0_20:5");
            Assert.AreEqual(765.5335, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PE, lipid.LipidClass);

            lipid = parser.Parse("PE 18:0/20:5");
            Assert.AreEqual(765.5335, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PE, lipid.LipidClass);
        }
    }
    [TestClass()]
    public class PSLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new PSLipidParser();

            var lipid = parser.Parse("PS 38:5");
            Assert.AreEqual(809.528, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PS, lipid.LipidClass);

            lipid = parser.Parse("PS 18:0_20:5");
            Assert.AreEqual(809.528, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PS, lipid.LipidClass);

            lipid = parser.Parse("PS 18:0/20:5");
            Assert.AreEqual(809.528, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PS, lipid.LipidClass);
        }
    }

    [TestClass()]
    public class PILipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new PILipidParser();

            var lipid = parser.Parse("PI 38:5");
            Assert.AreEqual(884.5342, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PI, lipid.LipidClass);

            lipid = parser.Parse("PI 18:0_20:5");
            Assert.AreEqual(884.5342, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PI, lipid.LipidClass);

            lipid = parser.Parse("PI 18:0/20:5");
            Assert.AreEqual(884.5342, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PI, lipid.LipidClass);
        }
    }
    [TestClass()]
    public class PGLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new PGLipidParser();

            var lipid = parser.Parse("PG 38:5");
            Assert.AreEqual(796.5182, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PG, lipid.LipidClass);

            lipid = parser.Parse("PG 18:0_20:5");
            Assert.AreEqual(796.5182, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PG, lipid.LipidClass);

            lipid = parser.Parse("PG 18:0/20:5");
            Assert.AreEqual(796.5182, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PG, lipid.LipidClass);
        }
    }

    [TestClass()]
    public class PALipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new PALipidParser();

            var lipid = parser.Parse("PA 38:5");
            Assert.AreEqual(722.4886, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PA, lipid.LipidClass);

            lipid = parser.Parse("PA 18:0_20:5");
            Assert.AreEqual(722.4886, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PA, lipid.LipidClass);

            lipid = parser.Parse("PA 18:0/20:5");
            Assert.AreEqual(722.4886, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.PA, lipid.LipidClass);
        }
    }


}
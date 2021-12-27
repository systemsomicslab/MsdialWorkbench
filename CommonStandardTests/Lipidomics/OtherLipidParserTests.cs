using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class BMPLipidParserTests
    {
        [TestMethod()]
        public void ParseTest() {
            var parser = new BMPLipidParser();

            var lipid = parser.Parse("BMP 38:5");
            Assert.AreEqual(796.5182, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.BMP, lipid.LipidClass);

            lipid = parser.Parse("BMP 18:0_20:5");
            Assert.AreEqual(796.5182, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.BMP, lipid.LipidClass);

            lipid = parser.Parse("BMP 18:0/20:5");
            Assert.AreEqual(796.5182, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.BMP, lipid.LipidClass);
        }
    }
    [TestClass()]
    public class HBMPLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new HBMPLipidParser();

            var lipid = parser.Parse("HBMP 54:3");
            Assert.AreEqual(1038.7864, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.HBMP, lipid.LipidClass);

            lipid = parser.Parse("HBMP 18:1_18:1_18:1");
            Assert.AreEqual(1038.7864, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.HBMP, lipid.LipidClass);

            lipid = parser.Parse("HBMP 18:1/18:1/18:1");
            Assert.AreEqual(1038.7864, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.HBMP, lipid.LipidClass);

            //errored
            //lipid = parser.Parse("HBMP 18:1/18:1_18:1");
            //Assert.AreEqual(1038.7864, lipid.Mass, 0.01);
            //Assert.AreEqual(LbmClass.HBMP, lipid.LipidClass);
        }
    }

    [TestClass()]
    public class CLLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new CLLipidParser();

            var lipid = parser.Parse("CL 68:2");
            Assert.AreEqual(1405.0035, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.CL, lipid.LipidClass);

            //errored
            //lipid = parser.Parse("CL 34:1_34:1");
            //Assert.AreEqual(1405.0035, lipid.Mass, 0.01);
            //Assert.AreEqual(LbmClass.CL, lipid.LipidClass);

            lipid = parser.Parse("CL 16:0_18:1_16:0_18:1");
            Assert.AreEqual(1405.0035, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.CL, lipid.LipidClass);

            //errored
            //lipid = parser.Parse("CL 16:0_18:1/16:0_18:1");
            //Assert.AreEqual(1405.0035, lipid.Mass, 0.01);
            //Assert.AreEqual(LbmClass.CL, lipid.LipidClass);
        }
    }
    [TestClass()]
    public class MGLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new MGLipidParser();

            var lipid = parser.Parse("MG 16:1");
            Assert.AreEqual(328.2614, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.MG, lipid.LipidClass);
        }
    }

    [TestClass()]
    public class DGLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new DGLipidParser();

            var lipid = parser.Parse("DG 36:2");
            Assert.AreEqual(620.538, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.DG, lipid.LipidClass);

            lipid = parser.Parse("DG 18:1_18:1");
            Assert.AreEqual(620.538, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.DG, lipid.LipidClass);

            lipid = parser.Parse("DG 18:1/18:1");
            Assert.AreEqual(620.538, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.DG, lipid.LipidClass);
        }
    }

    [TestClass()]
    public class TGLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new TGLipidParser();

            var lipid = parser.Parse("TG 54:3");
            Assert.AreEqual(884.7833, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.TG, lipid.LipidClass);

            lipid = parser.Parse("TG 18:1_18:1_18:1");
            Assert.AreEqual(884.7833, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.TG, lipid.LipidClass);

            lipid = parser.Parse("TG 18:1/18:1/18:1");
            Assert.AreEqual(884.7833, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.TG, lipid.LipidClass);
        }
    }

}
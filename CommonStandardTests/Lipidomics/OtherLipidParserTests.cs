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

            //need to consider?
            lipid = parser.Parse("HBMP 18:1/18:1_18:1");
            Assert.AreEqual(1038.7864, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.HBMP, lipid.LipidClass);
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

            lipid = parser.Parse("CL 16:0_18:1_16:0_18:1");
            Assert.AreEqual(1405.0035, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.CL, lipid.LipidClass);

            ////need to consider?
            //lipid = parser.Parse("CL 16:0_18:1/16:0_18:1");
            //Assert.AreEqual(1405.0035, lipid.Mass, 0.01);
            //Assert.AreEqual(LbmClass.CL, lipid.LipidClass);

            ////need to consider?
            //lipid = parser.Parse("CL 34:1_34:1");
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

    [TestClass()]
    public class DGTSLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new DGTSLipidParser(); //CCCCCCCCCCCCCCCC(=O)OCC(COCCC(C([O-])=O)[N+](C)(C)C)OC(=O)CCCCCCC\C=C\CCCCCCCC

            var lipid = parser.Parse("DGTS 34:1");
            Assert.AreEqual(737.61695, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.DGTS, lipid.LipidClass);

            lipid = parser.Parse("DGTS 16:0_18:1");
            Assert.AreEqual(737.61695, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.DGTS, lipid.LipidClass);

            lipid = parser.Parse("DGTS 16:0/18:1");
            Assert.AreEqual(737.61695, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.DGTS, lipid.LipidClass);
        }
    }
    [TestClass()]
    public class DGTALipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new DGTALipidParser(); //DGTA(18:1/22:4(10Z,13Z,16Z,19Z)) CCCCCCCCC=CCCCCCCCC(=O)OCC(COCC(C[N+](C)(C)C)C(=O)[O-])OC(=O)CCCCCCCCC=CCC=CCC=CCC=CCC

            var lipid = parser.Parse("DGTA 40:5");
            Assert.AreEqual(813.64825, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.DGTA, lipid.LipidClass);

            lipid = parser.Parse("DGTA 18:1_22:4");
            Assert.AreEqual(813.64825, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.DGTA, lipid.LipidClass);

            lipid = parser.Parse("DGTA 18:1/22:4");
            Assert.AreEqual(813.64825, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.DGTA, lipid.LipidClass);
        }
    }

    [TestClass()]
    public class LDGTSLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new LDGTSLipidParser(); 

            var lipid = parser.Parse("LDGTS 18:1");//O=C(OCC(O)COCCC(C(=O)[O-])[N+](C)(C)C)CCCCCCCC=CCCCCCCCC
            Assert.AreEqual(499.387288, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.LDGTS, lipid.LipidClass);
        }
    }
    [TestClass()]
    public class LDGTALipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new LDGTALipidParser(); 

            var lipid = parser.Parse("LDGTA 18:1");
            Assert.AreEqual(499.387288, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.LDGTA, lipid.LipidClass);
        }
    }

    [TestClass()]
    public class CARLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new CARLipidParser();

            var lipid = parser.Parse("CAR 18:1");
            Assert.AreEqual(425.350509, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.CAR, lipid.LipidClass);
        }
    }

    [TestClass()]
    public class DMEDFAHFALipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new DMEDFAHFALipidParser();

            var lipid = parser.Parse("DMEDFAHFA 38:5;O");
            Assert.AreEqual(654.56994, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.DMEDFAHFA, lipid.LipidClass);

            lipid = parser.Parse("DMEDFAHFA 20:4/18:1;O");
            Assert.AreEqual(654.56994, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.DMEDFAHFA, lipid.LipidClass);
        }
    }


}
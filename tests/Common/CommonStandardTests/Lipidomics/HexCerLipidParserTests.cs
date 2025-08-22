using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class HexCerParserTests
    {
        [TestMethod()]
        public void HexCerParseTest()
        {
            var parser = new HexCerLipidParser();
            //HexCer_AP O=C(NC(COC1OC(CO)C(O)C(O)C1O)C(O)C(O)CCCC=CCCC=CCCCCC)C(O)CCCCCCCCCCCCCCCCCCCCCCCC
            var lipid = parser.Parse("HexCer 18:2;O3/26:0(2OH)");
            Assert.AreEqual(869.6955983, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.HexCer_AP, lipid.LipidClass);

            //HexCer_HS
            lipid = parser.Parse("HexCer 18:2;O2/26:0;O2");
            Assert.AreEqual(869.6955983, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.HexCer_HS, lipid.LipidClass);
            //HexCer_HDS
            lipid = parser.Parse("HexCer 18:0;O2/26:2;O2");
            Assert.AreEqual(869.6955983, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.HexCer_HDS, lipid.LipidClass);
            //HexCer_NS
            lipid = parser.Parse("HexCer 18:2;O2/26:0");
            Assert.AreEqual(837.705769023, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.HexCer_NS, lipid.LipidClass);
            //HexCer_NDS
            lipid = parser.Parse("HexCer 18:0;O2/26:2");
            Assert.AreEqual(837.705769023, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.HexCer_NDS, lipid.LipidClass);
        }
    }
    [TestClass()]
    public class Hex2CerParserTests
    {
        [TestMethod()]
        public void Hex2CerParseTest()
        {
            var parser = new Hex2CerLipidParser();
            //Hex2Cer 18:1;O2/18:0  O=C(NC(COC1OC(CO)C(OC2OC(CO)C(O)C(O)C2O)C(O)C1O)C(O)C=CCCCCCCCCCCCCC)CCCCCCCCCCCCCCCCC
            var lipid = parser.Parse("Hex2Cer 18:1;O2/18:0");
            Console.WriteLine(lipid.Name + " " + lipid.Mass);
            Assert.AreEqual(889.6563, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Hex2Cer, lipid.LipidClass);

        }
    }
    [TestClass()]
    public class HexCerEOSParserTests
    {
        [TestMethod()]
        public void HexCerEOSParseTest()
        {
            var parser = new HexCerEosLipidParser();

            var lipid = parser.Parse("HexCer 52:2;O4"); //O=C(OCCCCCCCCC=CCCCCCCCCCCCCCC(=O)NC(COC1OC(CO)C(O)C(O)C1O)C(O)C=CCCCCCCC)CCCCCCCCCCCCCCC
            Assert.AreEqual(979.8051, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.HexCer_EOS, lipid.LipidClass);

            lipid = parser.Parse("HexCer 36:2;O3(FA 16:0)");
            Assert.AreEqual(979.8051, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.HexCer_EOS, lipid.LipidClass);

            lipid = parser.Parse("HexCer 18:1;O2/34:2;O2");
            Assert.AreEqual(979.8051, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.HexCer_EOS, lipid.LipidClass);

            lipid = parser.Parse("HexCer 18:1;O2/18:1;O(FA 16:0)");
            Assert.AreEqual(979.8051, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.HexCer_EOS, lipid.LipidClass);
        }
    }
    [TestClass()]
    public class SHexCerParserTests
    {
        [TestMethod()]
        public void SHexCerParseTest()
        {
            var parser = new SHexCerLipidParser();

            var lipid = parser.Parse("SHexCer 36:2;O2");
            Assert.AreEqual(805.53738, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.SHexCer, lipid.LipidClass);

            lipid = parser.Parse("SHexCer 18:1;O2/18:1"); // O=C(NC(COC1OC(CO)C(O)C(OS(=O)(=O)O)C1O)C(O)C=CCCCCCCC)CCCCCCCCCCCC=CCCCCCCCCCC
            Assert.AreEqual(805.53738, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.SHexCer, lipid.LipidClass);

            lipid = parser.Parse("SHexCer 18:1(1OH,3OH)/18:1");
            Assert.AreEqual(805.53738, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.SHexCer, lipid.LipidClass);
        }
    }
    [TestClass()]
    public class AHexCerParserTests
    {
        [TestMethod()]
        public void AHexCerParseTest()
        {
            var parser = new AHexCerLipidParser();

            var lipid = parser.Parse("AHexCer 52:2;O2");
            Assert.AreEqual(963.8102, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.AHexCer, lipid.LipidClass);

            lipid = parser.Parse("AHexCer (O-16:0)36:2;O2");
            Assert.AreEqual(963.8102, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.AHexCer, lipid.LipidClass);

            lipid = parser.Parse("AHexCer (O-16:0)18:1;O2/18:1");
            Assert.AreEqual(963.8102, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.AHexCer, lipid.LipidClass);

            lipid = parser.Parse("AHexCer (O-16:0)18:1(1OH,3OH)/18:1");
            Assert.AreEqual(963.8102, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.AHexCer, lipid.LipidClass);
        }
    }
    [TestClass()]
    public class ASHexCerParserTests
    {
        [TestMethod()]
        public void ASHexCerParseTest()
        {
            var parser = new ASHexCerLipidParser();

            var lipid = parser.Parse("ASHexCer 48:2;O3");
            Assert.AreEqual(1003.6994, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.ASHexCer, lipid.LipidClass);

            lipid = parser.Parse("ASHexCer (O-12:0)36:2;O3");
            Assert.AreEqual(1003.6994, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.ASHexCer, lipid.LipidClass);

            lipid = parser.Parse("ASHexCer (O-12:0)18:1;O2/18:1;O");
            Assert.AreEqual(1003.6994, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.ASHexCer, lipid.LipidClass);

            lipid = parser.Parse("ASHexCer (O-12:0)18:1(1OH,3OH)/18:1(2OH)");
            Assert.AreEqual(1003.6994, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.ASHexCer, lipid.LipidClass);
        }

    }
}
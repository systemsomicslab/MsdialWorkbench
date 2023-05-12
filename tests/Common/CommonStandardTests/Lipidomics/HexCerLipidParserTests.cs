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
            Console.WriteLine(lipid.Name+" "+lipid.Mass);
            Assert.AreEqual(889.6563, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Hex2Cer, lipid.LipidClass);

        }
    }

}
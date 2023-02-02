using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class CeramidesLipidParserTests
    {
        [TestMethod()]
        public void SMParseTest()
        {
            var parser = new SMLipidParser();

            var lipid = parser.Parse("SM 36:2;O2"); 
            Assert.AreEqual(728.58323, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.SM, lipid.LipidClass);

            lipid = parser.Parse("SM 18:1;O2/18:1"); // CCCCCCCCCCCCC\C=C\C(O)C(COP([O-])(=O)OCC[N+](C)(C)C)NC(=O)CCCCCCC\C=C/CCCCCCCC
            Assert.AreEqual(728.58323, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.SM, lipid.LipidClass);

            lipid = parser.Parse("SM 18:1;O2/18:1;O");
            Assert.AreEqual(744.57814, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.SM, lipid.LipidClass);

            lipid = parser.Parse("SM 18:1(1OH,3OH)/18:1;O");
            Assert.AreEqual(744.57814, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.SM, lipid.LipidClass);

            lipid = parser.Parse("SM 36:1;O5"); 
            Assert.AreEqual(778.58362, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.SM, lipid.LipidClass);

            lipid = parser.Parse("SM 18:1;O3/18:0;O2"); 
            Assert.AreEqual(778.58362, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.SM, lipid.LipidClass);
        }
        [TestMethod()]
        public void CerNSParseTest()
        {
            var parser = new CeramideLipidParser();

            var lipid = parser.Parse("Cer 42:2;O2"); 
            Assert.AreEqual(647.6216455, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Undefined, lipid.LipidClass);

            lipid = parser.Parse("Cer 18:2;O2/24:0"); // O=C(NC(CO)C(O)C=CCCC=CCCCCCCCCC)CCCCCCCCCCCCCCCCCCCCCCC
            Assert.AreEqual(647.6216455, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_NS, lipid.LipidClass);

            lipid = parser.Parse("Cer 18:2(4,8);O2/24:0"); 
            Assert.AreEqual(647.6216455, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_NS, lipid.LipidClass);


            lipid = parser.Parse("Cer 18:1(4);O2/24:1(9)"); 
            Assert.AreEqual(647.6216455, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_NS, lipid.LipidClass);
        }
        [TestMethod()]
        public void CerNDSParseTest()
        {
            var parser = new CeramideLipidParser();
            var lipid = parser.Parse("Cer 18:0;O2/24:0"); // O=C(NC(CO)C(O)CCCCCCCCCCCCCCC)CCCCCCCCCCCCCCCCCCCCCCC
            Assert.AreEqual(651.6529456, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_NDS, lipid.LipidClass);
        }
        [TestMethod()]
        public void CerNPParseTest()
        {
            var parser = new CeramideLipidParser();
            var lipid = parser.Parse("Cer 18:1;O3/24:0"); // O=C(NC(CO)C(O)C(O)CCCC=CCCCCCCCCC)CCCCCCCCCCCCCCCCCCCCCCC
            Assert.AreEqual(665.6322102, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_NP, lipid.LipidClass);

            lipid = parser.Parse("Cer 18:0;O3/24:0"); // O=C(NC(CO)C(O)C(O)CCCCCCCCCCCCCC)CCCCCCCCCCCCCCCCCCCCCCC
            Assert.AreEqual(667.6478602, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_NP, lipid.LipidClass);

            lipid = parser.Parse("Cer 18:0(1OH,3OH,4OH)/24:0"); // O=C(NC(CO)C(O)C(O)CCCCCCCCCCCCCC)CCCCCCCCCCCCCCCCCCCCCCC
            Assert.AreEqual(667.6478602, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_NP, lipid.LipidClass);
        }
        [TestMethod()]
        public void CerASParseTest()
        {
            var parser = new CeramideLipidParser();
            var lipid = parser.Parse("Cer 18:1;O2/24:0(2OH)"); // O=C(NC(CO)C(O)C=CCCCCCCCCCCCCC)C(O)CCCCCCCCCCCCCCCCCCCCCC
            Assert.AreEqual(665.6322102, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_AS, lipid.LipidClass);
        }
        [TestMethod()]
        public void CerADSParseTest()
        {
            var parser = new CeramideLipidParser();
            var lipid = parser.Parse("Cer 18:0;O2/24:0(2OH)"); //  O=C(NC(CO)C(O)CCCCCCCCCCCCCCC)C(O)CCCCCCCCCCCCCCCCCCCCCC
            Assert.AreEqual(667.6478602, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_ADS, lipid.LipidClass);
        }
        [TestMethod()]
        public void CerAPParseTest()
        {
            var parser = new CeramideLipidParser();
            var lipid = parser.Parse("Cer 18:2;O3/24:0(2OH)"); // O=C(NC(CO)C(O)C(O)CCCC=CCCC=CCCCCC)C(O)CCCCCCCCCCCCCCCCCCCCCC
            Assert.AreEqual(679.611474712, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_AP, lipid.LipidClass);

            lipid = parser.Parse("Cer 18:0;O3/24:0(2OH)"); // O=C(NC(CO)C(O)C(O)CCCCCCCCCCCCCC)C(O)CCCCCCCCCCCCCCCCCCCCCC
            Assert.AreEqual(683.6427748, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_AP, lipid.LipidClass);
        }
        [TestMethod()]
        public void CerBSParseTest()
        {
            var parser = new CeramideLipidParser();
            var lipid = parser.Parse("Cer 18:1;O2/24:0(3OH)"); // 
            Assert.AreEqual(665.6322102, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_BS, lipid.LipidClass);
        }
        [TestMethod()]
        public void CerBDSParseTest()
        {
            var parser = new CeramideLipidParser();
            var lipid = parser.Parse("Cer 18:0;O2/24:0(3OH)"); //  
            Assert.AreEqual(667.6478602, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_BDS, lipid.LipidClass);
        }
        [TestMethod()]
        public void CerHSParseTest()
        {
            var parser = new CeramideLipidParser();
            var lipid = parser.Parse("Cer 18:1;O2/24:0;O"); // 
            Assert.AreEqual(665.6322102, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_HS, lipid.LipidClass);

            lipid = parser.Parse("Cer 18:1(4);O2/24:0(24OH)"); // 
            Assert.AreEqual(665.6322102, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_HS, lipid.LipidClass);

        }
        [TestMethod()]
        public void CerHDSParseTest()
        {
            var parser = new CeramideLipidParser();
            var lipid = parser.Parse("Cer 18:0;O2/24:0;O"); //  
            Assert.AreEqual(667.6478602, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_HDS, lipid.LipidClass);
        }
        [TestMethod()]
        public void GM3ParseTest()
        {
            var parser = new GM3LipidParser();

            var lipid = parser.Parse("GM3 36:2;O2");
            Assert.AreEqual(1178.728808, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.GM3, lipid.LipidClass);

            lipid = parser.Parse("GM3 18:1;O2/18:1"); // CCCCCCCCCCCC=CC(O)C(COC1OC(CO)C(OC2OC(CO)C(O)C(OC3(CC(O)C(N=C(C)O)C(O3)C(O)C(O)CO)C(O)=O)C2O)C(O)C1O)NC(=O)CCCCCCCCCC=CCCCCCCCC
            Assert.AreEqual(1178.728808, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.GM3, lipid.LipidClass);

            lipid = parser.Parse("GM3 18:1(1OH,3OH)/18:1");
            Assert.AreEqual(1178.728808, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.GM3, lipid.LipidClass);
        }

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
}
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Accord.Math;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class BMPLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
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

            lipid = parser.Parse("CL 16:0_18:1/16:0_18:1");
            Assert.AreEqual(1405.0035, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.CL, lipid.LipidClass);

            lipid = parser.Parse("CL 34:1_34:1");
            Assert.AreEqual(1405.0035, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.CL, lipid.LipidClass);
        }
    }

    [TestClass()]
    public class MLCLLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new MLCLLipidParser();

            var lipid = parser.Parse("MLCL 54:5");
            Assert.AreEqual(1188.7582, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.MLCL, lipid.LipidClass);

            lipid = parser.Parse("MLCL 16:0_18:1_20:4");
            Assert.AreEqual(1188.7582, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.MLCL, lipid.LipidClass);

            ////need to consider?
            lipid = parser.Parse("MLCL 18:1/16:0_20:4");
            Assert.AreEqual(1188.7582, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.MLCL, lipid.LipidClass);

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
    public class TG_ESTLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new TG_ESTLipidParser();

            var lipid = parser.Parse("TG 60:3;O2"); //O=C(OCC(OC(=O)CCCCCCCCCCCCC)COC(=O)CCCCCCC(OC(=O)CCCCCCCC=CCCCCCCCC)C=CCCCCCC)CCCCCCCCCCC
            Assert.AreEqual(1000.8670, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.TG_EST, lipid.LipidClass);

            lipid = parser.Parse("TG 12:0_14:0_16:1;O(FA 18:1)");
            Assert.AreEqual(1000.8670, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.TG_EST, lipid.LipidClass);

            lipid = parser.Parse("TG 12:0/16:1;O(FA 18:1)/14:0");
            Assert.AreEqual(1000.8670, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.TG_EST, lipid.LipidClass);
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

    [TestClass()]
    public class ADGGALipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new ADGGALipidParser();

            var lipid = parser.Parse("ADGGA 14:0_16:0_18:1");
            Assert.AreEqual(980.7528, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.ADGGA, lipid.LipidClass);

            lipid = parser.Parse("ADGGA (O-14:0)16:0_18:1");
            Assert.AreEqual(980.7528, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.ADGGA, lipid.LipidClass);

            lipid = parser.Parse("ADGGA 48:1");
            Assert.AreEqual(980.7528, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.ADGGA, lipid.LipidClass);
        }
    }
    [TestClass()] public class NAGlyLipidParserTests { [TestMethod()] public void ParseTest() { var parser = new NAGlyLipidParser(); var lipid = parser.Parse("NAGly 18:1"); Assert.AreEqual(339.277344044, lipid.Mass, 0.01); Assert.AreEqual(LbmClass.NAGly, lipid.LipidClass); } }
    [TestClass()] public class NAGlySerLipidParserTests { [TestMethod()] public void ParseTest() { var parser = new NAGlySerLipidParser(); var lipid = parser.Parse("NAGlySer 18:1;O"); Assert.AreEqual(442.30428706399994, lipid.Mass, 0.01); Assert.AreEqual(LbmClass.NAGlySer, lipid.LipidClass); } }
    [TestClass()] public class NAOrnLipidParserTests { [TestMethod()] public void ParseTest() { var parser = new NAOrnLipidParser(); var lipid = parser.Parse("NAOrn 18:1"); Assert.AreEqual(396.33519326799995, lipid.Mass, 0.01); Assert.AreEqual(LbmClass.NAOrn, lipid.LipidClass); } }
    [TestClass()] public class NATauLipidParserTests { [TestMethod()] public void ParseTest() { var parser = new NATauLipidParser(); var lipid = parser.Parse("NATau 18:1"); Assert.AreEqual(389.259979728, lipid.Mass, 0.01); Assert.AreEqual(LbmClass.NATau, lipid.LipidClass); } }
    [TestClass()] public class NAPheLipidParserTests { [TestMethod()] public void ParseTest() { var parser = new NAPheLipidParser(); var lipid = parser.Parse("NAPhe 18:1"); Assert.AreEqual(429.324294236, lipid.Mass, 0.01); Assert.AreEqual(LbmClass.NAPhe, lipid.LipidClass); } }
    [TestClass()]
    public class NATryALipidParserTests { [TestMethod()] public void ParseTest() { var parser = new NATryALipidParser(); var lipid = parser.Parse("NATryA 18:1"); Assert.AreEqual(424.34536402799995, lipid.Mass, 0.01); Assert.AreEqual(LbmClass.NATryA, lipid.LipidClass); } }
    [TestClass()]
    public class NA5HTLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new NA5HTLipidParser(); var lipid = parser.Parse("NA5HT 18:1");//O=C(NCCC1=CNC=2C=CC(O)=CC21)CCCCCCCC=CCCCCCCCC
            Assert.AreEqual(440.340278648, lipid.Mass, 0.01); Assert.AreEqual(LbmClass.NA5HT, lipid.LipidClass);
        }
    }
    [TestClass()] public class NASerLipidParserTests { [TestMethod()] public void ParseTest() { var parser = new NASerLipidParser(); var lipid = parser.Parse("NASer 18:1"); Assert.AreEqual(369.28790872800005, lipid.Mass, 0.01); Assert.AreEqual(LbmClass.NASer, lipid.LipidClass); } }
    [TestClass()] public class NAAlaLipidParserTests { [TestMethod()] public void ParseTest() { var parser = new NAAlaLipidParser(); var lipid = parser.Parse("NAAla 18:1"); Assert.AreEqual(353.292994108, lipid.Mass, 0.01); Assert.AreEqual(LbmClass.NAAla, lipid.LipidClass); } }
    [TestClass()] public class NAGlnLipidParserTests { [TestMethod()] public void ParseTest() { var parser = new NAGlnLipidParser(); var lipid = parser.Parse("NAGln 18:1"); Assert.AreEqual(410.314457824, lipid.Mass, 0.01); Assert.AreEqual(LbmClass.NAGln, lipid.LipidClass); } }
    [TestClass()] public class NALeuLipidParserTests { [TestMethod()] public void ParseTest() { var parser = new NALeuLipidParser(); var lipid = parser.Parse("NALeu 18:1"); Assert.AreEqual(395.3399443, lipid.Mass, 0.01); Assert.AreEqual(LbmClass.NALeu, lipid.LipidClass); } }
    [TestClass()] public class NAValLipidParserTests { [TestMethod()] public void ParseTest() { var parser = new NAValLipidParser(); var lipid = parser.Parse("NAVal 18:1"); Assert.AreEqual(381.324294236, lipid.Mass, 0.01); Assert.AreEqual(LbmClass.NAVal, lipid.LipidClass); } }
    [TestClass()]
    public class NAOrn_FAHFALipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new NAOrn_FAHFALipidParser();
            var lipid = parser.Parse("NAOrn 18:1;O(FA 16:0)");
            Assert.AreEqual(650.5598, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.NAOrn, lipid.LipidClass);
        }
    }
    [TestClass()]
    public class NAGly_FAHFALipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new NAGly_FAHFALipidParser();
            var lipid = parser.Parse("NAGly 18:1;O(FA 16:0)");
            Assert.AreEqual(593.5019, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.NAGly, lipid.LipidClass);
        }
    }
    [TestClass()]
    public class NAGlySer_FAHFALipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new NAGlySer_FAHFALipidParser();
            var lipid = parser.Parse("NAGlySer 18:1;O(FA 16:0)");
            Assert.AreEqual(680.5340, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.NAGlySer, lipid.LipidClass);
        }
    }
    [TestClass()]
    public class NATryA_FAHFALipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new NATryA_FAHFALipidParser();
            var lipid = parser.Parse("NATryA 18:1;O(FA 16:0)");
            Assert.AreEqual(678.5699, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.NATryA, lipid.LipidClass);
        }
    }
    [TestClass()]
    public class NAGABALipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new NAGABALipidParser();
            var lipid = parser.Parse("NAGABA 18:1");
            Assert.AreEqual(367.3086, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.NAGABA, lipid.LipidClass);
        }
    }

    [TestClass()]
    public class NAAntLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new NAAntLipidParser();
            var lipid = parser.Parse("NAAnt 18:1");
            Assert.AreEqual(401.2930, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.NAAnt, lipid.LipidClass);
        }
    }
    [TestClass()]
    public class VAELipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new VAELipidParser();
            var lipid = parser.Parse("VAE 18:1");
            Assert.AreEqual(550.47498, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.VAE, lipid.LipidClass);
        }
    }


    [TestClass()]
    public class NAELipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new NAELipidParser();
            var lipid = parser.Parse("NAE 18:1");
            Assert.AreEqual(325.29808, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.NAE, lipid.LipidClass);
        }
    }
    [TestClass()]
    public class BRSELipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new BRSELipidParser();
            var lipid = parser.Parse("SE 28:2/18:1"); //C46H78O2
            var lipidMass = 28 * MassDiffDictionary.CarbonMass +
                            46 * MassDiffDictionary.HydrogenMass +
                            1 * MassDiffDictionary.OxygenMass +
                            18 * MassDiffDictionary.CarbonMass +
                            ((18-1) * 2+ (1*-2)) * MassDiffDictionary.HydrogenMass +
                            1 * MassDiffDictionary.OxygenMass;
            Assert.AreEqual(lipidMass, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.BRSE, lipid.LipidClass);
        }
    }
    [TestClass()]
    public class CholestanLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new STLipidParser();
            var lipid = parser.Parse("ST 27:0;O"); 
            var lipidMass = 27 * MassDiffDictionary.CarbonMass +
                            48 * MassDiffDictionary.HydrogenMass +
                            1 * MassDiffDictionary.OxygenMass;
            Assert.AreEqual(lipidMass, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.ST, lipid.LipidClass);
        }
    }
    [TestClass()]
    public class CSPHexLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new CSPHexLipidParser();
            var lipid = parser.Parse("SG 27:1;O;Hex;PA 16:0_20:4"); //C72H123O13P
            var lipidMass = 72 * MassDiffDictionary.CarbonMass +
                            123 * MassDiffDictionary.HydrogenMass +
                            1 * MassDiffDictionary.PhosphorusMass +
                            13 * MassDiffDictionary.OxygenMass;
            Assert.AreEqual(lipidMass, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.CSPHex, lipid.LipidClass);
        }
    }
    [TestClass()]

    public class BAHex_ST_LipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new BAHex_ST_LipidParser();
            var lipid = parser.Parse("ST 24:1;O3;Hex");
            var lipidMass = new[]
                                    {
                        MassDiffDictionary.CarbonMass * 30,
                        MassDiffDictionary.HydrogenMass * 50,
                        MassDiffDictionary.OxygenMass * 8,
                        MassDiffDictionary.NitrogenMass * 0,
                        MassDiffDictionary.PhosphorusMass * 0,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
            Assert.AreEqual(lipidMass, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.BileAcid, lipid.LipidClass);
        }
    }
    [TestClass()]
    public class LipidALipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new LipidALipidParser();
            var lipid = parser.Parse("LipidA 14:0-O-14:0_N-18:0-O-16:0_18:0_N-16:0");//C108H206N2O25P2
            var lipidMass = new[]
                                    {
                        MassDiffDictionary.CarbonMass * 108,
                        MassDiffDictionary.HydrogenMass * 206,
                        MassDiffDictionary.OxygenMass * 25,
                        MassDiffDictionary.NitrogenMass * 2,
                        MassDiffDictionary.PhosphorusMass * 2,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
            Assert.AreEqual(lipidMass, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.LipidA, lipid.LipidClass);
        }
    }
    [TestClass()]
    public class CoqLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new CoqLipidParser();
            var lipid = parser.Parse("CoQ1");
            var lipidMass = new[]
                                    {
                        MassDiffDictionary.CarbonMass * 14,
                        MassDiffDictionary.HydrogenMass * 18,
                        MassDiffDictionary.OxygenMass * 4,
                    }.Sum();
            Assert.AreEqual(lipidMass, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.CoQ, lipid.LipidClass);

            var lipid2 = parser.Parse("CoQ8");
            var lipid2Mass = new[]
                                    {
                        MassDiffDictionary.CarbonMass * 49,
                        MassDiffDictionary.HydrogenMass * 74,
                        MassDiffDictionary.OxygenMass * 4,
                    }.Sum();
            Assert.AreEqual(lipid2Mass, lipid2.Mass, 0.01);
            Assert.AreEqual(LbmClass.CoQ, lipid2.LipidClass);
        }
    }
    [TestClass()]
    public class PimsLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new PimsLipidParser();
            var lipid = parser.Parse("Ac2PIM1 20:0_15:0");//C50H95O18P
            var lipidMass = new[]
                                    {
                        MassDiffDictionary.CarbonMass * 50,
                        MassDiffDictionary.HydrogenMass * 95,
                        MassDiffDictionary.OxygenMass * 18,
                        MassDiffDictionary.PhosphorusMass * 1,
                    }.Sum();
            Assert.AreEqual(lipidMass, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Ac2PIM1, lipid.LipidClass);
        }

        [TestMethod()]
        public void ParseTest2()
        {
            var parser = new PimsLipidParser();
            var lipid = parser.Parse("Ac2PIM2 18:0_18:1");//C57H105O23P
            var lipidMass = new[]
                                    {
                        MassDiffDictionary.CarbonMass * 57,
                        MassDiffDictionary.HydrogenMass * 105,
                        MassDiffDictionary.OxygenMass * 23,
                        MassDiffDictionary.PhosphorusMass * 1,
                    }.Sum();
            Assert.AreEqual(lipidMass, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Ac2PIM2, lipid.LipidClass);
        }
        [TestMethod()]
        public void ParseTest3()
        {
            var parser = new PimsLipidParser();
            var lipid = parser.Parse("Ac3PIM2 16:0_19:0_18:0");//C74H139O24P
            var lipidMass = new[]
                                    {
                        MassDiffDictionary.CarbonMass * 74,
                        MassDiffDictionary.HydrogenMass * 139,
                        MassDiffDictionary.OxygenMass * 24,
                        MassDiffDictionary.PhosphorusMass * 1,
                    }.Sum();
            Assert.AreEqual(lipidMass, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Ac3PIM2, lipid.LipidClass);
        }
        [TestMethod()]
        public void ParseTest4()
        {
            var parser = new PimsLipidParser();
            var lipid = parser.Parse("Ac4PIM2 18:2_16:0_16:1_18:1");//C89H159O25P
            var lipidMass = new[]
                                    {
                        MassDiffDictionary.CarbonMass * 89,
                        MassDiffDictionary.HydrogenMass * 159,
                        MassDiffDictionary.OxygenMass * 25,
                        MassDiffDictionary.PhosphorusMass * 1,
                    }.Sum();
            Assert.AreEqual(lipidMass, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Ac4PIM2, lipid.LipidClass);
        }
    }
}
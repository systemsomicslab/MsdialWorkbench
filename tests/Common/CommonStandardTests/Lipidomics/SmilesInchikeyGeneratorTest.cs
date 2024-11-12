using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    [DeploymentItem("x64/libinchi.dll", "x64")]
    public class SmilesInchikeyGeneratorTest
    {
        [TestMethod()]
        public void GenerateTest()
        {
            //// PC P-18:1(12)/20:5(5,8,11,14,17)
            var alkyl = new AlkylChain(18, DoubleBond.CreateFromPosition(1, 12), new Oxidized(0));
            var acyl = new AcylChain(20, DoubleBond.CreateFromPosition(5, 8, 11, 14, 17), new Oxidized(0));
            var lipid = new Lipid(LbmClass.EtherPC, 789.5672409, new PositionLevelChains(alkyl, acyl));

            var result1 = SmilesInchikeyGenerator.Generate(lipid);
            Assert.AreEqual("C(OC=CCCCCCCCCCC=CCCCCC)C(OC(=O)CCCC=CCC=CCC=CCC=CCC=CCC)COP([O-])(=O)OCC[N+](C)(C)C", result1.Smiles);
            Assert.AreEqual("VZRVIPQLHQGIMS-UHFFFAOYSA-N", result1.InchiKey);

            var parser2 = new EtherPCLipidParser();
            var lipid2 = parser2.Parse("PC P-18:1(12)/20:5(5,8,11,14,17)");
            var result2 = SmilesInchikeyGenerator.Generate((Lipid)lipid2);

            Assert.AreEqual("C(OC=CCCCCCCCCCC=CCCCCC)C(OC(=O)CCCC=CCC=CCC=CCC=CCC=CCC)COP([O-])(=O)OCC[N+](C)(C)C", result2.Smiles);
            Assert.AreEqual("VZRVIPQLHQGIMS-UHFFFAOYSA-N", result2.InchiKey);


            // Cer 18:1;2O/20:5(5,8,11,14,17) Cer_NS
            var sphingo3 = new SphingoChain(18, DoubleBond.CreateFromPosition(4), Oxidized.CreateFromPosition(1, 3));
            var lipid3 = new Lipid(LbmClass.Cer_NS, 585.512095, new PositionLevelChains(sphingo3, acyl));

            var result3 = SmilesInchikeyGenerator.Generate(lipid3);
            Assert.AreEqual("OCC(NC(=O)CCCC=CCC=CCC=CCC=CCC=CCC)C(O)C=CCCCCCCCCCCCCC", result3.Smiles);
            Assert.AreEqual("VWXMLEVTINKCKQ-UHFFFAOYSA-N", result3.InchiKey);

            var parser4 = new CeramideLipidParser();
            var lipid4 = parser4.Parse("Cer 18:1;O2/20:5(5,8,11,14,17)");
            var result4 = SmilesInchikeyGenerator.Generate((Lipid)lipid4);

            Assert.AreEqual("OCC(NC(=O)CCCC=CCC=CCC=CCC=CCC=CCC)C(O)C=CCCCCCCCCCCCCC", result4.Smiles);
            Assert.AreEqual("VWXMLEVTINKCKQ-UHFFFAOYSA-N", result4.InchiKey);

            // HexCer 18:1;O2/22:0 HexCer_NS
            var sphingo5 = new SphingoChain(18, DoubleBond.CreateFromPosition(4), Oxidized.CreateFromPosition(1, 3));
            var acyl5 = new AcylChain(22, DoubleBond.CreateFromPosition(0), new Oxidized(0));
            var lipid5 = new Lipid(LbmClass.HexCer_NS, 803.648649, new PositionLevelChains(sphingo5, acyl5));

            var result5 = SmilesInchikeyGenerator.Generate(lipid5);
            Assert.AreEqual("OCC1OC(OCC(NC(=O)CCCCCCCCCCCCCCCCCCCCC)C(O)C=CCCCCCCCCCCCCC)C(O)C(O)C1O", result5.Smiles);
            Assert.AreEqual("YIGARKIIFOHVPF-UHFFFAOYSA-N", result5.InchiKey);

            var parser6 = new HexCerLipidParser();
            var lipid6 = parser6.Parse("HexCer 18:1;O2/22:0");
            var result6 = SmilesInchikeyGenerator.Generate((Lipid)lipid6);

            Assert.AreEqual("OCC1OC(OCC(NC(=O)CCCCCCCCCCCCCCCCCCCCC)C(O)C=CCCCCCCCCCCCCC)C(O)C(O)C1O", result6.Smiles);
            Assert.AreEqual("YIGARKIIFOHVPF-UHFFFAOYSA-N", result6.InchiKey);
        }
    }
}

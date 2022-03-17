using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class SmilesInchikeyGeneratorTest
    {
        [TestMethod()]
        public void GenerateTest()
        {
            // PC P-18:1(12)/20:5(5,8,11,14,17)
            var alkyl = new AlkylChain(18, DoubleBond.CreateFromPosition(1, 12), new Oxidized(0));
            var acyl = new AcylChain(20, DoubleBond.CreateFromPosition(5, 8, 11, 14, 17), new Oxidized(0));
            var lipid = new Lipid(LbmClass.EtherPC, 789.5672409, new PositionLevelChains(alkyl, acyl));

            var result = SmilesInchikeyGenerator.Generate(lipid);
            Assert.AreEqual("C(OC=CCCCCCCCCCC=CCCCCC)C(OC(CCCC=CCC=CCC=CCC=CCC=CCC)=O)COP([O-])(=O)OCC[N+](C)(C)C", result.Smiles);
            Assert.AreEqual("VZRVIPQLHQGIMS-UHFFFAOYSA-N", result.InchiKey);

            var parser = new EtherPCLipidParser();
            var lipid2 =parser.Parse("PC P-18:1(12)/20:5(5,8,11,14,17)");
            result = SmilesInchikeyGenerator.Generate((Lipid)lipid2);

            Assert.AreEqual("C(OC=CCCCCCCCCCC=CCCCCC)C(OC(CCCC=CCC=CCC=CCC=CCC=CCC)=O)COP([O-])(=O)OCC[N+](C)(C)C", result.Smiles);
            Assert.AreEqual("VZRVIPQLHQGIMS-UHFFFAOYSA-N", result.InchiKey);


            // Cer 18:1;2O/20:5(5,8,11,14,17) Cer_NS
            var sphingo = new SphingoChain(18, DoubleBond.CreateFromPosition(4), Oxidized.CreateFromPosition(1, 3));
            lipid = new Lipid(LbmClass.Cer_NS, 585.512095, new PositionLevelChains(sphingo, acyl));

            result = SmilesInchikeyGenerator.Generate(lipid);
            Assert.AreEqual("C(OC=CCCCCCCCCCC=CCCCCC)C(OC(CCCC=CCC=CCC=CCC=CCC=CCC)=O)COP([O-])(=O)OCC[N+](C)(C)C", result.Smiles);
            Assert.AreEqual("VZRVIPQLHQGIMS-UHFFFAOYSA-N", result.InchiKey);

            var parser3 = new CeramideLipidParser();
            var lipid3 = parser.Parse("Cer 18:1;2O/20:5(5,8,11,14,17)");
            result = SmilesInchikeyGenerator.Generate((Lipid)lipid2);

            Assert.AreEqual("C(OC=CCCCCCCCCCC=CCCCCC)C(OC(CCCC=CCC=CCC=CCC=CCC=CCC)=O)COP([O-])(=O)OCC[N+](C)(C)C", result.Smiles);
            Assert.AreEqual("VZRVIPQLHQGIMS-UHFFFAOYSA-N", result.InchiKey);

        }
    }
}

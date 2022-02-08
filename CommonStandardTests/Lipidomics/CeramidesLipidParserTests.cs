using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class CeramidesLipidParserTests
    {
        [TestMethod()]
        public void SMParseTest() {
            var parser = new SMLipidParser();

            var lipid = parser.Parse("SM 36:2;2O"); // CCCCCCCCCCCCC\C=C\C(O)C(COP([O-])(=O)OCC[N+](C)(C)C)NC(=O)CCCCCCC\C=C/CCCCCCCC
            Assert.AreEqual(728.58323, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.SM, lipid.LipidClass);

            lipid = parser.Parse("SM 18:1;2O/18:1"); // CCCCCCCCCCCCC\C=C\C(O)C(COP([O-])(=O)OCC[N+](C)(C)C)NC(=O)CCCCCCC\C=C/CCCCCCCC
            Assert.AreEqual(728.58323, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.SM, lipid.LipidClass);

            lipid = parser.Parse("SM 18:1;2O/18:1;O");// CCCCCCCCCCCCC\C=C\C(O)C(COP([O-])(=O)OCC[N+](C)(C)C)NC(=O)CCCCCCC\C=C/C(O)CCCCCCC
            Assert.AreEqual(744.57814, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.SM, lipid.LipidClass);

            lipid = parser.Parse("SM 36:1;5O"); // CCCCCCCCCCCC\C=C\C(O)C(O)C(COP([O-])(=O)OCC[N+](C)(C)C)NC(=O)CC(O)CCCCCCCC(O)CCCCCCC
            Assert.AreEqual(778.58362, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.SM, lipid.LipidClass);

            lipid = parser.Parse("SM 18:1;3O/18:0;2O"); // CCCCCCCCCCCC\C=C\C(O)C(O)C(COP([O-])(=O)OCC[N+](C)(C)C)NC(=O)CC(O)CCCCCCCC(O)CCCCCCC
            Assert.AreEqual(778.58362, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.SM, lipid.LipidClass);

        }
    }
}
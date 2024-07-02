using CompMs.Common.FormulaGenerator.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.DataObj.Property.Tests
{
    [TestClass()]
    public class FormulaTests
    {
        [TestMethod()]
        public void FormulaTest() {
            var elementMap = ElementDictionary.MassDict;
            var element2Count = new Dictionary<string, int>()
            {
                ["C"] = 6,
                ["H"] = 12,
                ["O"] = 6,
            };
            var formula = new Formula(element2Count);

            Assert.AreEqual("C6H12O6", formula.FormulaString);
            Assert.AreEqual(elementMap["C"] * 6 + elementMap["H"] * 12 + elementMap["O"] * 6, formula.Mass, 1e-6);
            Assert.AreEqual(6, formula.Cnum);
            Assert.AreEqual(12, formula.Hnum);
            Assert.AreEqual(6, formula.Onum);
        }

        [TestMethod()]
        public void ElementNumTest() {
            var elementMap = ElementDictionary.MassDict;
            var element2Count = new Dictionary<string, int>()
            {
                ["C"] = 1,
                ["[13C]"] = 2,
                ["H"] = 3,
                ["[2H]"] = 4,
                ["Br"] = 5,
                ["[81Br]"] = 6,
                ["[37Cl]"] = 7,
                ["F"] = 8,
                ["I"] = 9,
                ["N"] = 10,
                ["[15N]"] = 11,
                ["O"] = 12,
                ["[18O]"] = 13,
                ["P"] = 14,
                ["S"] = 15,
                ["[34S]"] = 16,
                ["Se"] = 17,
                ["Si"] = 18,
            };
            var formula = new Formula(element2Count);

            Assert.AreEqual("C[13C]2H3[2H]4Br5[81Br]6[37Cl]7F8I9N10[15N]11O12[18O]13P14S15[34S]16Se17Si18", formula.FormulaString);
            Assert.AreEqual(
                elementMap["C"] * 1 +
                elementMap["[13C]"] * 2 +
                elementMap["H"] * 3 +
                elementMap["[2H]"] * 4 +
                elementMap["Br"] * 5 +
                elementMap["[81Br]"] * 6 +
                elementMap["[37Cl]"] * 7 +
                elementMap["F"] * 8 +
                elementMap["I"] * 9 +
                elementMap["N"] * 10 +
                elementMap["[15N]"] * 11 +
                elementMap["O"] * 12 +
                elementMap["[18O]"] * 13 +
                elementMap["P"] * 14 +
                elementMap["S"] * 15 +
                elementMap["[34S]"] * 16 +
                elementMap["Se"] * 17 +
                elementMap["Si"] * 18,
                formula.Mass, 1e-6);

            Assert.AreEqual(1, formula.Cnum);
            Assert.AreEqual(2, formula.C13num);
            Assert.AreEqual(3, formula.Hnum);
            Assert.AreEqual(4, formula.H2num);
            Assert.AreEqual(5, formula.Brnum);
            Assert.AreEqual(6, formula.Br81num);
            Assert.AreEqual(7, formula.Cl37num);
            Assert.AreEqual(8, formula.Fnum);
            Assert.AreEqual(9, formula.Inum);
            Assert.AreEqual(10, formula.Nnum);
            Assert.AreEqual(11, formula.N15num);
            Assert.AreEqual(12, formula.Onum);
            Assert.AreEqual(13, formula.O18num);
            Assert.AreEqual(14, formula.Pnum);
            Assert.AreEqual(15, formula.Snum);
            Assert.AreEqual(16, formula.S34num);
            Assert.AreEqual(17, formula.Senum);
            Assert.AreEqual(18, formula.Sinum);
        }

        [DataTestMethod]
        [DynamicData(nameof(FormulaIsCorrectlyImportedDatas), DynamicDataSourceType.Property)]
        public void FormulaIsCorrectlyImported(Dictionary<string, int> compounds, bool expected) {
            var formula = new Formula(compounds);
            Assert.AreEqual(expected, formula.IsCorrectlyImported);
        }

        public static IEnumerable<object[]> FormulaIsCorrectlyImportedDatas {
            get {
                yield return [new Dictionary<string, int>() { ["C"] = 6, ["H"] = 12, ["O"] = 6, }, true];
                yield return [new Dictionary<string, int>(), false];
            }
        }
    }
}
using CompMs.Common.DataObj.Property;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace CompMs.Common.FormulaGenerator.Parser.Tests
{
    [TestClass()]
    public class FormulaStringParcerTests
    {
        [TestMethod()]
        [DynamicData(nameof(Convert2FormulaObjV2TestDatas), DynamicDataSourceType.Property)]
        public void Convert2FormulaObjV2Test(string formulaString, Formula expected) {
            var actual = FormulaStringParcer.Convert2FormulaObjV2(formulaString);
            AssertFormulaIsEqual(expected, actual);
        }

        [TestMethod()]
        [DynamicData(nameof(Convert2FormulaObjV2TestDatas), DynamicDataSourceType.Property)]
        public void Convert2FormulaObjV3Test(string formulaString, Formula expected) {
            var actual = FormulaStringParcer.Convert2FormulaObjV3(formulaString);
            AssertFormulaIsEqual(expected, actual);
        }

        public static IEnumerable<object[]> Convert2FormulaObjV2TestDatas {
            get {
                yield return new object[]
                {
                    "C",
                    new Formula(new Dictionary<string, int> { { "C", 1 }, })
                };
                yield return new object[]
                {
                    "[13C]",
                    new Formula(new Dictionary<string, int> { { "[13C]", 1 }, })
                };
                yield return new object[]
                {
                    "C ",
                    new Formula(new Dictionary<string, int> { { "C", 1 }, })
                };
                yield return new object[]
                {
                    "[13C] ",
                    new Formula(new Dictionary<string, int> { { "[13C]", 1 }, })
                };
                yield return new object[]
                {
                    "C12H24O12",
                    new Formula(new Dictionary<string, int> { { "C", 12 }, { "H", 24 }, { "O", 12 }, })
                };
                yield return new object[]
                {
                    "C12 H24O12",
                    new Formula(new Dictionary<string, int> { { "C", 12 }, { "H", 24 }, { "O", 12 }, })
                };
                yield return new object[]
                {
                    "C10H24O12C2",
                    new Formula(new Dictionary<string, int> { { "C", 12 }, { "H", 24 }, { "O", 12 }, })
                };
                yield return new object[]
                {
                    "C10H24O12C2 ",
                    new Formula(new Dictionary<string, int> { { "C", 12 }, { "H", 24 }, { "O", 12 }, })
                };
                yield return new object[]
                {
                    "[13C]12H24O12",
                    new Formula(new Dictionary<string, int> { { "[13C]", 12 }, { "H", 24 }, { "O", 12 }, })
                };
                yield return new object[]
                {
                    "C12H24O12 ",
                    new Formula(new Dictionary<string, int> { { "C", 12 }, { "H", 24 }, { "O", 12 }, })
                };
                yield return new object[]
                {
                    "C12H24O ",
                    new Formula(new Dictionary<string, int> { { "C", 12 }, { "H", 24 }, { "O", 1 }, })
                };
                yield return new object[]
                {
                    "H24O12[13C]12 ",
                    new Formula(new Dictionary<string, int> { { "[13C]", 12 }, { "H", 24 }, { "O", 12 }, })
                };
                yield return new object[]
                {
                    "[13C]10H24O12[13C]2 ",
                    new Formula(new Dictionary<string, int> { { "[13C]", 12 }, { "H", 24 }, { "O", 12 }, })
                };
                // add 20230414
                yield return new object[]
                 {
                    "C41H77[2H]5NO8P ",
                    new Formula(new Dictionary<string, int> { { "C", 41 }, { "H", 77 }, { "[2H]", 5 }, { "N", 1 }, { "O", 8 }, { "P", 1 }, })
                 };
            }
        }

        [TestMethod()]
        [DynamicData(nameof(TokenizeFormulaTestDatas), DynamicDataSourceType.Property)]
        public void TokenizeFormulaTest(string formula, List<(string, int)> expected) {
            var actual = FormulaStringParcer.TokenizeFormula(formula);
            CollectionAssert.AreEquivalent(expected, actual);
        }

        public static IEnumerable<object[]> TokenizeFormulaTestDatas {
            get {
                yield return new object[]
                {
                    "C",
                    new List<(string, int)> { ("C", 1), },
                };
                yield return new object[]
                {
                    "C12H24O12",
                    new List<(string, int)> { ("C", 12), ("H", 24), ("O", 12), },
                };
                yield return new object[]
                {
                    "CH24O12",
                    new List<(string, int)> { ("C", 1), ("H", 24), ("O", 12), },
                };
                yield return new object[]
                {
                    "C12H24O12 ",
                    new List<(string, int)> { ("C", 12), ("H", 24), ("O", 12), },
                };
                yield return new object[]
                {
                    "[13C]12H24O12",
                    new List<(string, int)> { ("[13C]", 12), ("H", 24), ("O", 12), },
                };
            }
        }

        private void AssertFormulaIsEqual(Formula expected, Formula actual) {
            Console.WriteLine(actual);
            Assert.AreEqual(expected.FormulaString, actual.FormulaString);
            Assert.AreEqual(expected.Mass, actual.Mass);
        }
    }
}
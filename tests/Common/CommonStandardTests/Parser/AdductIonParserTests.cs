using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.FormulaGenerator.Function;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace CompMs.Common.Parser.Tests
{
    [TestClass()]
    public class AdductIonParserTests
    {
        [DataTestMethod()]
        [DynamicData(nameof(IonTypeFormatCheckerTestData))]
        public void IonTypeFormatCheckerTest(bool expected, string adduct) {
            var actual = AdductIonParser.IonTypeFormatChecker(adduct);
            Assert.AreEqual(expected, actual);
        }

        public static IEnumerable<object[]> IonTypeFormatCheckerTestData {
            get {
                yield return new object[] { true, "[M+H]+" };
                yield return new object[] { true, "[M]+" };
                yield return new object[] { true, "[M].+" };
                yield return new object[] { true, "[2M+H]+" };
                yield return new object[] { true, "[M+2H]2+" };
                yield return new object[] { true, "[M-H]-" };
                yield return new object[] { false, "M+H+" };
                yield return new object[] { false, "[+" };
                yield return new object[] { false, "[[]]+" };
                yield return new object[] { false, "[M+H]" };
                yield return new object[] { false, "[M+H]+]" };
                yield return new object[] { false, "[(M+H)]+" };
                yield return new object[] { false, "[(M+H]+" };
                yield return new object[] { false, "[M+H)]+" };
                yield return new object[] { false, "[M+H])+" };
                yield return new object[] { false, "[M+H](+" };
                yield return new object[] { false, "[[]+" };
                yield return new object[] { false, "[][+" };
                yield return new object[] { false, "[]+" };
                yield return new object[] { false, "[100]+" };
            }
        }

        [TestMethod()]
        public void CountCharTest() {
            Assert.AreEqual(2, AdductIonParser.CountChar("[M+H]+", '+'));
            Assert.AreEqual(1, AdductIonParser.CountChar("[M+HCOO]-", '-'));
        }

        [TestMethod()]
        public void GetAdductIonXmerTest() {
            Assert.AreEqual(1, AdductIonParser.GetAdductIonXmer("[M+H]+"));
            Assert.AreEqual(2, AdductIonParser.GetAdductIonXmer("[2M+H]+"));
            Assert.AreEqual(3, AdductIonParser.GetAdductIonXmer("[3M+H]+"));
            Assert.AreEqual(20, AdductIonParser.GetAdductIonXmer("[20M+H]+"));
        }

        [TestMethod()]
        public void GetIonTypeTest() {
            Assert.AreEqual(IonMode.Positive, AdductIonParser.GetIonType("[M+H]+"));
            Assert.AreEqual(IonMode.Negative, AdductIonParser.GetIonType("[M-H]-"));
            Assert.AreEqual(IonMode.Positive, AdductIonParser.GetIonType("[2M+H]+"));
        }

        [TestMethod()]
        public void GetRadicalInfoTest() {
            Assert.IsTrue(AdductIonParser.GetRadicalInfo("[M].+"));
            Assert.IsFalse(AdductIonParser.GetRadicalInfo("[M+H]+"));
        }

        [TestMethod()]
        public void GetChargeNumberTest() {
            Assert.AreEqual(1, AdductIonParser.GetChargeNumber("[M+H]+"));
            Assert.AreEqual(1, AdductIonParser.GetChargeNumber("[M-H]-"));
            Assert.AreEqual(2, AdductIonParser.GetChargeNumber("[M+2H]2+"));
            Assert.AreEqual(2, AdductIonParser.GetChargeNumber("[M-2H]2-"));
            Assert.AreEqual(3, AdductIonParser.GetChargeNumber("[M+3H]3+"));
            Assert.AreEqual(3, AdductIonParser.GetChargeNumber("[M-3H]3-"));
            Assert.AreEqual(1, AdductIonParser.GetChargeNumber("[M+HCOO]-"));
            Assert.AreEqual(-1, AdductIonParser.GetChargeNumber("[M+H]abc")); // invalid format
            Assert.AreEqual(-1, AdductIonParser.GetChargeNumber("[M+H]123")); // invalid format
        }

        [TestMethod]
        public void CalculateAccurateMassAndIsotopeRatioTest() {
            double c13_c12 = 0.010815728;
            double h2_h1 = 0.000115013;
            double o17_o16 = 0.000380926;
            double o18_o16 = 0.002054994;

            var adduct = AdductIon.GetAdductIon("[M-H]-");
            (var accurateMass, var m1Intensity, var m2Intensity) = AdductIonParser.CalculateAccurateMassAndIsotopeRatio(adduct.AdductIonName);
            Assert.AreEqual(-MassDiffDictionary.HydrogenMass, accurateMass, 1e-10);
            Assert.AreEqual(-h2_h1, m1Intensity, 1e-10);
            Assert.AreEqual(-0, m2Intensity, 1e-10);

            adduct = AdductIon.GetAdductIon("[M+HCOO]-");
            (accurateMass, m1Intensity, m2Intensity) = AdductIonParser.CalculateAccurateMassAndIsotopeRatio(adduct.AdductIonName);
            Assert.AreEqual(MassDiffDictionary.HydrogenMass + MassDiffDictionary.CarbonMass + MassDiffDictionary.OxygenMass * 2, accurateMass, 1e-6);
            Assert.AreEqual(c13_c12 + h2_h1 + 2 * o17_o16, m1Intensity, 1e-6);
            Assert.AreEqual(c13_c12 * h2_h1 + c13_c12 * 2 * o17_o16 + h2_h1 * 2 * o17_o16 + 2 * o18_o16, m2Intensity, 1e-6);

            adduct = AdductIon.GetAdductIon("[M+H]+");
            (accurateMass, m1Intensity, m2Intensity) = AdductIonParser.CalculateAccurateMassAndIsotopeRatio(adduct.AdductIonName);
            Assert.AreEqual(MassDiffDictionary.HydrogenMass, accurateMass, 1e-10);
            Assert.AreEqual(h2_h1, m1Intensity, 1e-10);
            Assert.AreEqual(0, m2Intensity, 1e-10);

            adduct = AdductIon.GetAdductIon("[M+H+H]+");
            (accurateMass, m1Intensity, m2Intensity) = AdductIonParser.CalculateAccurateMassAndIsotopeRatio(adduct.AdductIonName);
            Assert.AreEqual(MassDiffDictionary.HydrogenMass * 2, accurateMass, 1e-10);
            Assert.AreEqual(h2_h1 * 2, m1Intensity, 1e-10);
            Assert.AreEqual(0 * 2, m2Intensity, 1e-10);

            adduct = AdductIon.GetAdductIon("[M+2H]2+");
            (accurateMass, m1Intensity, m2Intensity) = AdductIonParser.CalculateAccurateMassAndIsotopeRatio(adduct.AdductIonName);
            Assert.AreEqual(MassDiffDictionary.HydrogenMass * 2, accurateMass, 1e-10);
            Assert.AreEqual(h2_h1 * 2, m1Intensity, 1e-10);
            Assert.AreEqual(Math.Pow(0.000115013, 2), m2Intensity, 1e-10);

            adduct = AdductIon.GetAdductIon("[M+10H]10+");
            (accurateMass, m1Intensity, m2Intensity) = AdductIonParser.CalculateAccurateMassAndIsotopeRatio(adduct.AdductIonName);
            Assert.AreEqual(MassDiffDictionary.HydrogenMass * 10, accurateMass, 1e-10);
            Assert.AreEqual(h2_h1 * 10, m1Intensity, 1e-10);
            Assert.AreEqual(10 * 9 / 2 * Math.Pow(0.000115013, 2), m2Intensity, 1e-10);

            adduct = AdductIon.GetAdductIon("[M+Mg]2+");
            (accurateMass, m1Intensity, m2Intensity) = AdductIonParser.CalculateAccurateMassAndIsotopeRatio(adduct.AdductIonName);
            Assert.AreEqual(23.98504170000, accurateMass, 1e-10);
            Assert.AreEqual(0, m1Intensity, 1e-10);
            Assert.AreEqual(0, m2Intensity, 1e-10);

            adduct = AdductIon.GetAdductIon("[M+2Mg]4+");
            (accurateMass, m1Intensity, m2Intensity) = AdductIonParser.CalculateAccurateMassAndIsotopeRatio(adduct.AdductIonName);
            Assert.AreEqual(23.98504170000 * 2, accurateMass, 1e-10);
            Assert.AreEqual(0, m1Intensity, 1e-10);
            Assert.AreEqual(0, m2Intensity, 1e-10);

            adduct = AdductIon.GetAdductIon("[M+C6H12O6]+");
            (accurateMass, m1Intensity, m2Intensity) = AdductIonParser.CalculateAccurateMassAndIsotopeRatio(adduct.AdductIonName);
            var formula = new Formula(6, 12, 0, 6, 0, 0, 0, 0, 0, 0, 0);
            Assert.AreEqual(formula.Mass, accurateMass, 1e-6);
            Assert.AreEqual(SevenGoldenRulesCheck.GetM1IsotopicAbundance(formula), m1Intensity, 1e-6);
            Assert.AreEqual(SevenGoldenRulesCheck.GetM2IsotopicAbundance(formula), m2Intensity, 1e-6);

            adduct = AdductIon.GetAdductIon("[M+He]");
            (accurateMass, m1Intensity, m2Intensity) = AdductIonParser.CalculateAccurateMassAndIsotopeRatio(adduct.AdductIonName);
            Assert.AreEqual(0, accurateMass, 1e-10);
            Assert.AreEqual(0, m1Intensity, 1e-10);
            Assert.AreEqual(0, m2Intensity, 1e-10);
        }

        [TestMethod()]
        public void CalculateAccurateMassAndIsotopeRatioOfMolecularFormulaTest() {
            (var accurateMass, var m1Intensity, var m2Intensity) = AdductIonParser.CalculateAccurateMassAndIsotopeRatioOfMolecularFormula("10");
            Assert.AreEqual(10, accurateMass, 1e-10);
            Assert.AreEqual(0, m1Intensity, 1e-10);
            Assert.AreEqual(0, m2Intensity, 1e-10);
            
            (accurateMass, m1Intensity, m2Intensity) = AdductIonParser.CalculateAccurateMassAndIsotopeRatioOfMolecularFormula("H");
            Assert.AreEqual(MassDiffDictionary.HydrogenMass, accurateMass, 1e-10);
            Assert.AreEqual(0.000115013, m1Intensity, 1e-10);
            Assert.AreEqual(0, m2Intensity, 1e-10);
            
            (accurateMass, m1Intensity, m2Intensity) = AdductIonParser.CalculateAccurateMassAndIsotopeRatioOfMolecularFormula("2H");
            Assert.AreEqual(MassDiffDictionary.HydrogenMass * 2, accurateMass, 1e-10);
            Assert.AreEqual(0.000115013 * 2, m1Intensity, 1e-10);
            Assert.AreEqual(Math.Pow(0.000115013, 2), m2Intensity, 1e-10);

            (accurateMass, m1Intensity, m2Intensity) = AdductIonParser.CalculateAccurateMassAndIsotopeRatioOfMolecularFormula("10H");
            Assert.AreEqual(MassDiffDictionary.HydrogenMass * 10, accurateMass, 1e-10);
            Assert.AreEqual(0.000115013 * 10, m1Intensity, 1e-10);
            Assert.AreEqual(10 * 9 / 2 * Math.Pow(0.000115013, 2), m2Intensity, 1e-10);

            (accurateMass, m1Intensity, m2Intensity) = AdductIonParser.CalculateAccurateMassAndIsotopeRatioOfMolecularFormula("Mg");
            Assert.AreEqual(23.98504170000, accurateMass, 1e-10);
            Assert.AreEqual(0, m1Intensity, 1e-10);
            Assert.AreEqual(0, m2Intensity, 1e-10);

            (accurateMass, m1Intensity, m2Intensity) = AdductIonParser.CalculateAccurateMassAndIsotopeRatioOfMolecularFormula("2Mg");
            Assert.AreEqual(23.98504170000 * 2, accurateMass, 1e-10);
            Assert.AreEqual(0, m1Intensity, 1e-10);
            Assert.AreEqual(0, m2Intensity, 1e-10);

            (accurateMass, m1Intensity, m2Intensity) = AdductIonParser.CalculateAccurateMassAndIsotopeRatioOfMolecularFormula("C6H12O6");
            var formula = new Formula(6, 12, 0, 6, 0, 0, 0, 0, 0, 0, 0);
            Assert.AreEqual(formula.Mass, accurateMass, 1e-6);
            Assert.AreEqual(SevenGoldenRulesCheck.GetM1IsotopicAbundance(formula), m1Intensity, 1e-6);
            Assert.AreEqual(SevenGoldenRulesCheck.GetM2IsotopicAbundance(formula), m2Intensity, 1e-6);

            (accurateMass, m1Intensity, m2Intensity) = AdductIonParser.CalculateAccurateMassAndIsotopeRatioOfMolecularFormula("He");
            Assert.AreEqual(0, accurateMass, 1e-10);
            Assert.AreEqual(0, m1Intensity, 1e-10);
            Assert.AreEqual(0, m2Intensity, 1e-10);
        }

        [TestMethod]
        public void GetFormulaAndNumberTest() {
            (var formula, var number) = AdductIonParser.GetFormulaAndNumber("H");
            Assert.AreEqual("H", formula);
            Assert.AreEqual(1, number);

            (formula, number) = AdductIonParser.GetFormulaAndNumber("2H");
            Assert.AreEqual("H", formula);
            Assert.AreEqual(2, number);

            (formula, number) = AdductIonParser.GetFormulaAndNumber("10H");
            Assert.AreEqual("H", formula);
            Assert.AreEqual(10, number);

            (formula, number) = AdductIonParser.GetFormulaAndNumber("HCOO");
            Assert.AreEqual("HCOO", formula);
            Assert.AreEqual(1, number);

            (formula, number) = AdductIonParser.GetFormulaAndNumber("Na");
            Assert.AreEqual("Na", formula);
            Assert.AreEqual(1, number);

            (formula, number) = AdductIonParser.GetFormulaAndNumber("2Na");
            Assert.AreEqual("Na", formula);
            Assert.AreEqual(2, number);
        }

        [TestMethod]
        public void IsCommonAdductTest() {
            var isCommon = AdductIonParser.IsCommonAdduct("H", 1, out var acurateMass, out var m1Intensity, out var m2Intensity);
            Assert.IsTrue(isCommon);
            Assert.AreEqual(MassDiffDictionary.HydrogenMass, acurateMass, 1e-10);
            Assert.AreEqual(0.000115013, m1Intensity, 1e-10);
            Assert.AreEqual(0, m2Intensity, 1e-10);
            
            isCommon = AdductIonParser.IsCommonAdduct("H", 2, out acurateMass, out m1Intensity, out m2Intensity);
            Assert.IsTrue(isCommon);
            Assert.AreEqual(MassDiffDictionary.HydrogenMass * 2, acurateMass, 1e-10);
            Assert.AreEqual(0.000115013 * 2, m1Intensity, 1e-10);
            Assert.AreEqual(Math.Pow(0.000115013, 2), m2Intensity, 1e-10);

            isCommon = AdductIonParser.IsCommonAdduct("H", 10, out acurateMass, out m1Intensity, out m2Intensity);
            Assert.IsTrue(isCommon);
            Assert.AreEqual(MassDiffDictionary.HydrogenMass * 10, acurateMass, 1e-10);
            Assert.AreEqual(0.000115013 * 10, m1Intensity, 1e-10);
            Assert.AreEqual(10 * 9 / 2 * Math.Pow(0.000115013, 2), m2Intensity, 1e-10);

            isCommon = AdductIonParser.IsCommonAdduct("Mg", 1, out acurateMass, out m1Intensity, out m2Intensity);
            Assert.IsFalse(isCommon);
            Assert.AreEqual(0, acurateMass, 1e-10);
            Assert.AreEqual(0, m1Intensity, 1e-10);
            Assert.AreEqual(0, m2Intensity, 1e-10);
        }

        [TestMethod]
        public void GetOrganicAddutFormulaAndMass() {
            (var formula, var mass) = AdductIonParser.GetOrganicAdductFormulaAndMass("C6H12O6", 1);
            Assert.AreEqual(6, formula.Cnum);
            Assert.AreEqual(12, formula.Hnum);
            Assert.AreEqual(6, formula.Onum);
            Assert.AreEqual(MassDiffDictionary.CarbonMass * 6 + MassDiffDictionary.HydrogenMass * 12 + MassDiffDictionary.OxygenMass * 6, mass, 1e-6);

            (formula, mass) = AdductIonParser.GetOrganicAdductFormulaAndMass("C6H12O6", 2);
            Assert.AreEqual(12, formula.Cnum);
            Assert.AreEqual(24, formula.Hnum);
            Assert.AreEqual(12, formula.Onum);
            Assert.AreEqual(MassDiffDictionary.CarbonMass * 12 + MassDiffDictionary.HydrogenMass * 24 + MassDiffDictionary.OxygenMass * 12, mass, 1e-6);
        }
    }
}
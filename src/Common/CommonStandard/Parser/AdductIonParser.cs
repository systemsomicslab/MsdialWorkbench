using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Function;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Parser {
    public static class AdductIonParser {
        private static readonly double c13_c12 = 0.010815728;
        private static readonly double h2_h1 = 0.000115013;
        private static readonly double n15_n14 = 0.003653298;
        private static readonly double o17_o16 = 0.000380926;
        private static readonly double o18_o16 = 0.002054994;
        private static readonly double s33_s32 = 0.007895568;
        private static readonly double s34_s32 = 0.044741552;
        private static readonly double k40_k39 = 0.000125458;
        private static readonly double k41_k39 = 0.072167458;
        private static readonly double ni60_ni58 = 0.385196175;

        public static ReadOnlyDictionary<string, double> IronToMass = new ReadOnlyDictionary<string, double>(
            new Dictionary<string, double>() {
                { "Be", 9.01218220000 }, { "Mg", 23.98504170000 }, { "Al", 26.98153863000 }, 
                { "Ca", 39.96259098000 }, { "Sc", 44.95591190000 }, { "Ti", 47.94794630000 }, { "V", 50.94395950000 }, { "Cr", 51.94050750000 }, { "Mn", 54.93804510000 },
                { "Fe", 55.93493750000 }, { "Cu", 62.92959750000 }, { "Zn", 63.92914220000 }, { "Ga", 68.92557360000 },
                { "Ge", 73.92117780000 }, { "Se", 79.91652130000 }, { "Kr", 83.91150700000 }, { "Rb", 84.91178973800 }, { "Sr", 87.90561210000 }, { "Zr", 89.90470440000 },
                { "Nb", 92.90637810000 }, { "Mo", 97.90540820000 }, { "Ru", 101.90434930000 }, { "Pd", 105.90348600000 }, { "Ag", 106.90509700000 }, { "Cd", 113.90335850000 },
                { "In", 114.90387800000 }, { "Sn", 119.90219470000 }, { "Sb", 120.90381570000 }, { "Cs", 132.90545193300 }, { "La", 138.90635330000 }
        });

        /// <summary>
        /// This method returns the AdductIon class variable from the adduct string.
        /// </summary>
        /// <param name="adductName">Add the formula string such as "C6H12O6"</param>
        /// <returns></returns>
        [Obsolete("Use AdductIon.GetAddutIon instead of this method.")]
        public static AdductIon GetAdductIonBean(string adductName)
        {
            return AdductIon.GetAdductIon(adductName);
        }

        public static AdductIon ConvertDifferentChargedAdduct(AdductIon adduct, int chargeNumber) {
            if (chargeNumber == 0)
                return adduct;
            if (adduct.FormatCheck == false) return adduct;
            if (Math.Abs(adduct.ChargeNumber) == chargeNumber)
                return adduct;

            var adductContent = GetAdductContent(adduct.AdductIonName);

            var xMerString = adduct.AdductIonXmer == 1 ? string.Empty : adduct.AdductIonXmer.ToString();
            var chargeString = chargeNumber == 1 ? string.Empty : chargeNumber.ToString();
            var radicalString = adduct.IsRadical == false ? string.Empty : ".";
            var ionString = adduct.IonMode == IonMode.Positive ? "+" : "-";

            var newAdductString = "[" + xMerString + "M" + adductContent + "]" + chargeString + ionString + radicalString;

            var newAdduct = AdductIon.GetAdductIon(newAdductString);
            return newAdduct;
        }

        public static string GetAdductContent(string adduct) {
            var trimedAdductName = adduct.Split('[')[1].Split(']')[0].Trim();
            if (!trimedAdductName.Contains('+') && !trimedAdductName.Contains('-'))
                return string.Empty;

            var isCharacterMappeared = false;
            var isNextEquationAppreared = false;
            var contentString = string.Empty;

            for (int i = 0; i < trimedAdductName.Length; i++) {
                if (trimedAdductName[i] == 'M') {
                    isCharacterMappeared = true;
                    continue;
                }

                //if (isCharacterMappeared && (trimedAdductName[i] == '-' || trimedAdductName[i] == '+')) {
                //    isNextEquationAppreared = true;
                //    continue;
                //}

                if (isCharacterMappeared) {
                    contentString += trimedAdductName[i];
                }
            }
            return contentString.Trim();
        }

        public static (double, double, double) CalculateAccurateMassAndIsotopeRatio(string adductName) {
            adductName = adductName.Split('[')[1].Split(']')[0].Trim();

            if (!adductName.Contains('+') && !adductName.Contains('-')) {
                return (0d, 0d, 0d);
            }

            int equationNum = CountChar(adductName, '+') + CountChar(adductName, '-');
            string formula = string.Empty;
            double accAccurateMass = 0, accM1Intensity = 0, accM2Intensity = 0;
            for (int i = adductName.Length - 1; i >= 0; i--)
            {
                if (adductName[i].Equals('+'))
                {
                    (var accurateMass, var m1Intensity, var m2Intensity) = CalculateAccurateMassAndIsotopeRatioOfMolecularFormula(formula);
                    accAccurateMass += accurateMass;
                    accM1Intensity += m1Intensity;
                    accM2Intensity += m2Intensity;

                    formula = string.Empty;
                    equationNum--;
                }
                else if (adductName[i].Equals('-'))
                {
                    (var accurateMass, var m1Intensity, var m2Intensity) = CalculateAccurateMassAndIsotopeRatioOfMolecularFormula(formula);
                    accAccurateMass -= accurateMass;
                    accM1Intensity -= m1Intensity;
                    accM2Intensity -= m2Intensity;

                    formula = string.Empty;
                    equationNum--;
                }
                else
                {
                    formula = adductName[i] + formula;
                }
                if (equationNum <= 0) break;
            }
            return (accAccurateMass, accM1Intensity, accM2Intensity);
        }

        public static int CountChar(string s, char c)
        {
            return s.Length - s.Replace(c.ToString(), "").Length;
        }

        private static readonly Regex XmerCheckTemplate = new Regex(@"\[(?<x>\d+)?");
        public static int GetAdductIonXmer(string adductName)
        {
            var match = XmerCheckTemplate.Match(adductName);
            if (match.Success) {
                if (match.Groups["x"].Success) {
                    return int.Parse(match.Groups["x"].Value);
                }
            }
            return 1;
        }

        private static readonly Regex FormatCheckTemplate = new Regex(@"^\[\d*M\s?[ A-Za-z0-9+-]*\]\.?\d*[+-]\.?$");
        public static bool IonTypeFormatChecker(string adductName)
        {
            return adductName != null && FormatCheckTemplate.IsMatch(adductName);
        }

        public static IonMode GetIonType(string adductName)
        {
            string chargeString = adductName.Split(']')[1];
            if (chargeString.Contains('+'))
            {
                return IonMode.Positive;
            }
            else
            {
                return IonMode.Negative;
            }
        }

        public static bool GetRadicalInfo(string adductName) {
            var chargeString = adductName.Split(']')[1];
            return chargeString.Contains('.');
        }


        private static readonly Regex ChargeNumberTemplate = new Regex(@"](?<charge>\d+)?[+-]");
        public static int GetChargeNumber(string adductName)
        {
            var match = ChargeNumberTemplate.Match(adductName);
            if (match.Success) {
                if (match.Groups["charge"].Success) {
                    return int.Parse(match.Groups["charge"].Value);
                }
                else {
                    return 1;
                }
            }
            else {
                return -1;
            }
        }

        /// <summary>
        /// 2H -> H, 2
        /// 3Na -> Na, 3
        /// </summary>
        /// <param name="formula"></param>
        /// <returns></returns>
        public static (string, double) GetFormulaAndNumber(string formula) {
            string numString = string.Empty;
            for (int i = 0; i < formula.Length; i++)
            {
                if (char.IsNumber(formula[i]))
                {
                    numString += formula[i];
                }
                else
                {
                    break;
                }
            }
            return (
                formula.Substring(numString.Length),
                string.IsNullOrEmpty(numString) ? 1 : double.Parse(numString)
            );
        }

        public static (double, double, double) CalculateAccurateMassAndIsotopeRatioOfMolecularFormula(string rawFormula) {

            (var formula, var multipliedNum) = GetFormulaAndNumber(rawFormula);

            if (string.IsNullOrWhiteSpace(formula))
            {
                return (multipliedNum, 0, 0);
            }

            //Common adduct check
            if (IsCommonAdduct(formula, multipliedNum, out var commonAcurateMass, out var m1Intensity, out var m2Intensity)) {
                return (commonAcurateMass, m1Intensity, m2Intensity);
            }

            // check irons
            if (IronToMass.TryGetValue(formula, out var iron)) {
                return (multipliedNum * iron, 0, 0);
            }

            //Organic compound adduct check
            (var formulaBean, var organicAcurateMass) = GetOrganicAdductFormulaAndMass(formula, multipliedNum);
            return (
                organicAcurateMass,
                SevenGoldenRulesCheck.GetM1IsotopicAbundance(formulaBean),
                SevenGoldenRulesCheck.GetM2IsotopicAbundance(formulaBean)
            );
        }

        public static bool IsCommonAdduct(string formula, double multipliedNumber, out double acurateMass, out double m1Intensity, out double m2Intensity) {
            if (formula.Equals("Na"))
            {
                acurateMass = multipliedNumber * 22.9897692809;
                m1Intensity = 0;
                m2Intensity = 0;
                return true;
            }
            else if (formula.Equals("H"))
            {
                acurateMass = multipliedNumber * 1.00782503207;
                m1Intensity = multipliedNumber * h2_h1;
                m2Intensity = multipliedNumber * (multipliedNumber - 1) * 0.5 * Math.Pow(h2_h1, 2);
                return true;
            }
            else if (formula.Equals("K"))
            {
                acurateMass = multipliedNumber * 38.96370668;
                m1Intensity = multipliedNumber * k40_k39;
                m2Intensity = multipliedNumber * (multipliedNumber - 1) * 0.5 * Math.Pow(k40_k39, 2) + multipliedNumber * k41_k39;
                return true;
            }
            else if (formula.Equals("Li"))
            {
                acurateMass = multipliedNumber * 7.01600455;
                m1Intensity = 0;
                m2Intensity = 0;
                return true;
            }
            else if (formula.Equals("ACN") || formula.Equals("CH3CN"))
            {
                acurateMass = multipliedNumber * 41.0265491;
                m1Intensity = multipliedNumber * (2 * c13_c12 + 3 * h2_h1 + n15_n14);
                m2Intensity = (multipliedNumber * (2 * c13_c12 * 3 * h2_h1 + 2 * c13_c12 * n15_n14 + 3 * h2_h1 * n15_n14)
                    + 2 * multipliedNumber * (2 * multipliedNumber - 1) * 0.5 * Math.Pow(c13_c12, 2)
                    + 3 * multipliedNumber * (3 * multipliedNumber - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + multipliedNumber * (multipliedNumber - 1) * 0.5 * Math.Pow(n15_n14, 2));
                return true;
            }
            else if (formula.Equals("NH4"))
            {
                acurateMass = multipliedNumber * 18.03437413;
                m1Intensity = multipliedNumber * (4 * h2_h1 + n15_n14);
                m2Intensity = (multipliedNumber * (4 * h2_h1 * n15_n14)
                    + 4 * multipliedNumber * (4 * multipliedNumber - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + multipliedNumber * (multipliedNumber - 1) * 0.5 * Math.Pow(n15_n14, 2));
                return true;
            }
            else if (formula.Equals("CH3OH"))
            {
                acurateMass = multipliedNumber * 32.02621475;
                m1Intensity = multipliedNumber * (c13_c12 + 4 * h2_h1 + o17_o16);
                m2Intensity = (multipliedNumber * (c13_c12 * 4 * h2_h1 + c13_c12 * o17_o16 + 4 * h2_h1 * o17_o16)
                    + multipliedNumber * (multipliedNumber - 1) * 0.5 * Math.Pow(c13_c12, 2)
                    + 4 * multipliedNumber * (4 * multipliedNumber - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + multipliedNumber * (multipliedNumber - 1) * 0.5 * Math.Pow(o17_o16, 2)
                    + multipliedNumber * o18_o16);
                return true;
            }
            else if (formula.Equals("H2O"))
            {
                acurateMass = multipliedNumber * 18.01056468;
                m1Intensity = multipliedNumber * (2 * h2_h1 + o17_o16);
                m2Intensity = (multipliedNumber * (2 * h2_h1 * o17_o16)
                    + 2 * multipliedNumber * (2 * multipliedNumber - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + multipliedNumber * (multipliedNumber - 1) * 0.5 * Math.Pow(o17_o16, 2)
                    + multipliedNumber * o18_o16);
                return true;
            }
            else if (formula.Equals("IsoProp") || formula.Equals("C3H7OH"))
            {
                acurateMass = multipliedNumber * 60.05751488;
                m1Intensity = multipliedNumber * (3 * c13_c12 + 8 * h2_h1 + o17_o16);
                m2Intensity = (multipliedNumber * (3 * c13_c12 * 8 * h2_h1 + 3 * c13_c12 * o17_o16 + 8 * h2_h1 * o17_o16)
                    + 3 * multipliedNumber * (3 * multipliedNumber - 1) * 0.5 * Math.Pow(c13_c12, 2)
                    + 8 * multipliedNumber * (8 * multipliedNumber - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + multipliedNumber * (multipliedNumber - 1) * 0.5 * Math.Pow(o17_o16, 2)
                    + multipliedNumber * o18_o16);
                return true;
            }
            else if (formula.Equals("DMSO") || formula.Equals("C2H6OS"))
            {
                acurateMass = multipliedNumber * 78.013936;
                m1Intensity = multipliedNumber * (2 * c13_c12 + 6 * h2_h1 + o17_o16 + s33_s32);
                m2Intensity = (multipliedNumber * (2 * c13_c12 * 6 * h2_h1 + 2 * c13_c12 * o17_o16 + 2 * c13_c12 * s33_s32 + 6 * h2_h1 * o17_o16 + 6 * h2_h1 * s33_s32 + o17_o16 * s33_s32)
                    + 2 * multipliedNumber * (2 * multipliedNumber - 1) * 0.5 * Math.Pow(c13_c12, 2)
                    + 6 * multipliedNumber * (6 * multipliedNumber - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + multipliedNumber * (multipliedNumber - 1) * 0.5 * Math.Pow(o17_o16, 2)
                    + multipliedNumber * (multipliedNumber - 1) * 0.5 * Math.Pow(s33_s32, 2)
                    + multipliedNumber * o18_o16
                    + multipliedNumber * s34_s32);
                return true;
            }
            else if (formula.Equals("FA") || formula.Equals("HCOOH"))
            {
                acurateMass = multipliedNumber * 46.005479;
                m1Intensity = multipliedNumber * (c13_c12 + 2 * h2_h1 + 2 * o17_o16);
                m2Intensity = (multipliedNumber * (c13_c12 * 2 * h2_h1 + c13_c12 * 2 * o17_o16 + 2 * h2_h1 * 2 * o17_o16)
                    + multipliedNumber * (multipliedNumber - 1) * 0.5 * Math.Pow(c13_c12, 2)
                    + 2 * multipliedNumber * (2 * multipliedNumber - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + 2 * multipliedNumber * (2 * multipliedNumber - 1) * 0.5 * Math.Pow(o17_o16, 2)
                    + 2 * multipliedNumber * o18_o16);
                return true;
            }
            else if (formula.Equals("HCOO")) {
                acurateMass = multipliedNumber * 44.997654;
                m1Intensity = multipliedNumber * (c13_c12 + 1 * h2_h1 + 2 * o17_o16);
                m2Intensity = (multipliedNumber * (c13_c12 * 1 * h2_h1 + c13_c12 * 2 * o17_o16 + 1 * h2_h1 * 2 * o17_o16)
                    + multipliedNumber * (multipliedNumber - 1) * 0.5 * Math.Pow(c13_c12, 2)
                    + 1 * multipliedNumber * (1 * multipliedNumber - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + 2 * multipliedNumber * (2 * multipliedNumber - 1) * 0.5 * Math.Pow(o17_o16, 2)
                    + 2 * multipliedNumber * o18_o16);
                return true;
            }
            else if (formula.Equals("Hac") || formula.Equals("CH3COOH") || formula.Equals("CH3CO2H") || formula.Equals("C2H4O2"))
            {
                acurateMass = multipliedNumber * 60.021129;
                m1Intensity = multipliedNumber * (2 * c13_c12 + 4 * h2_h1 + 2 * o17_o16);
                m2Intensity = (multipliedNumber * (2 * c13_c12 * 4 * h2_h1 + 2 * c13_c12 * 2 * o17_o16 + 4 * h2_h1 * 2 * o17_o16)
                    + 2 * multipliedNumber * (2 * multipliedNumber - 1) * 0.5 * Math.Pow(c13_c12, 2)
                    + 4 * multipliedNumber * (4 * multipliedNumber - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + 2 * multipliedNumber * (2 * multipliedNumber - 1) * 0.5 * Math.Pow(o17_o16, 2)
                    + 2 * multipliedNumber * o18_o16);
                return true;
            }
            else if (formula.Equals("CH3COO")) {
                acurateMass = multipliedNumber * 59.013305;
                m1Intensity = multipliedNumber * (2 * c13_c12 + 3 * h2_h1 + 2 * o17_o16);
                m2Intensity = (multipliedNumber * (2 * c13_c12 * 3 * h2_h1 + 2 * c13_c12 * 2 * o17_o16 + 3 * h2_h1 * 2 * o17_o16)
                    + 2 * multipliedNumber * (2 * multipliedNumber - 1) * 0.5 * Math.Pow(c13_c12, 2)
                    + 3 * multipliedNumber * (3 * multipliedNumber - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + 2 * multipliedNumber * (2 * multipliedNumber - 1) * 0.5 * Math.Pow(o17_o16, 2)
                    + 2 * multipliedNumber * o18_o16);
                return true;
            }
            else if (formula.Equals("CH3COONa")) {
                acurateMass = multipliedNumber * 82.003075;
                m1Intensity = multipliedNumber * (2 * c13_c12 + 3 * h2_h1 + 2 * o17_o16);
                m2Intensity = (multipliedNumber * (2 * c13_c12 * 3 * h2_h1 + 2 * c13_c12 * 2 * o17_o16 + 3 * h2_h1 * 2 * o17_o16)
                    + 2 * multipliedNumber * (2 * multipliedNumber - 1) * 0.5 * Math.Pow(c13_c12, 2)
                    + 3 * multipliedNumber * (3 * multipliedNumber - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + 2 * multipliedNumber * (2 * multipliedNumber - 1) * 0.5 * Math.Pow(o17_o16, 2)
                    + 2 * multipliedNumber * o18_o16);
                return true;
            }
            else if (formula.Equals("TFA") || formula.Equals("CF3COOH"))
            {
                acurateMass = multipliedNumber * 113.992864;
                m1Intensity = multipliedNumber * (2 * c13_c12 + h2_h1 + 2 * o17_o16);
                m2Intensity = (multipliedNumber * (2 * c13_c12 * h2_h1 + 2 * c13_c12 * 2 * o17_o16 + h2_h1 * 2 * o17_o16)
                    + 2 * multipliedNumber * (2 * multipliedNumber - 1) * 0.5 * Math.Pow(c13_c12, 2)
                    + multipliedNumber * (multipliedNumber - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + 2 * multipliedNumber * (2 * multipliedNumber - 1) * 0.5 * Math.Pow(o17_o16, 2)
                    + 2 * multipliedNumber * o18_o16);
                return true;
            }
            else if (formula.Equals("Co"))
            {
                acurateMass = multipliedNumber * 58.933195;
                m1Intensity = 0;
                m2Intensity = 0;
                return true;
            }
            else if (formula.Equals("Ni"))
            {
                acurateMass = multipliedNumber * 57.9353429;
                m1Intensity = 0;
                m2Intensity = multipliedNumber * ni60_ni58;
                return true;
            }
            else if (formula.Equals("Ba"))
            {
                acurateMass = multipliedNumber * 137.9052472;
                m1Intensity = 0;
                m2Intensity = 0;
                return true;
            }

            acurateMass = 0d;
            m1Intensity = 0d;
            m2Intensity = 0d;
            return false;
        }

        public static (Formula, double) GetOrganicAdductFormulaAndMass(string formula, double multipliedNumber) {
            var formulaBean = new Formula();
            var acurateMass = 0d;
            MatchCollection mc = Regex.Matches(formula, "C(?!a|d|e|l|o|r|s|u)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) { acurateMass += 12.0 * multipliedNumber; formulaBean.Cnum = (int)multipliedNumber; }
                else
                {
                    double num;
                    if (double.TryParse(mc[0].Groups[1].Value, out num)) { acurateMass += num * 12.0 * multipliedNumber; formulaBean.Cnum = (int)(num * multipliedNumber); }
                }
            }

            mc = Regex.Matches(formula, "H(?!e|f|g|o)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) { acurateMass += 1.00782503207 * multipliedNumber; formulaBean.Hnum = (int)multipliedNumber; }
                else
                {
                    double num;
                    if (double.TryParse(mc[0].Groups[1].Value, out num)) { acurateMass += num * 1.00782503207 * multipliedNumber; formulaBean.Hnum = (int)(num * multipliedNumber); }
                }
            }

            mc = Regex.Matches(formula, "N(?!a|b|d|e|i)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) { acurateMass += 14.00307400480 * multipliedNumber; formulaBean.Nnum = (int)multipliedNumber; }
                else
                {
                    double num;
                    if (double.TryParse(mc[0].Groups[1].Value, out num)) { acurateMass += num * 14.00307400480 * multipliedNumber; formulaBean.Nnum = (int)(num * multipliedNumber); }
                }
            }

            mc = Regex.Matches(formula, "O(?!s)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) { acurateMass += 15.99491461956 * multipliedNumber; formulaBean.Onum = (int)multipliedNumber; }
                else
                {
                    double num;
                    if (double.TryParse(mc[0].Groups[1].Value, out num)) { acurateMass += num * 15.99491461956 * multipliedNumber; formulaBean.Onum = (int)(num * multipliedNumber); }
                }
            }

            mc = Regex.Matches(formula, "S(?!b|c|e|i|m|n|r)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) { acurateMass += 31.972071 * multipliedNumber; formulaBean.Snum = (int)multipliedNumber; }
                else
                {
                    double num;
                    if (double.TryParse(mc[0].Groups[1].Value, out num)) { acurateMass += num * 31.972071 * multipliedNumber; formulaBean.Snum = (int)(num * multipliedNumber); }
                }
            }

            mc = Regex.Matches(formula, "Br([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) { acurateMass += 78.9183371 * multipliedNumber; formulaBean.Brnum = (int)multipliedNumber; }
                else
                {
                    double num;
                    if (double.TryParse(mc[0].Groups[1].Value, out num)) { acurateMass += num * 78.9183371 * multipliedNumber; formulaBean.Brnum = (int)(num * multipliedNumber); }
                }
            }

            mc = Regex.Matches(formula, "Cl([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) { acurateMass += 34.96885268 * multipliedNumber; formulaBean.Clnum = (int)multipliedNumber; }
                else
                {
                    double num;
                    if (double.TryParse(mc[0].Groups[1].Value, out num)) { acurateMass += num * 34.96885268 * multipliedNumber; formulaBean.Clnum = (int)(num * multipliedNumber); }
                }
            }

            mc = Regex.Matches(formula, "F(?!e)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) { acurateMass += 18.99840322 * multipliedNumber; formulaBean.Fnum = (int)multipliedNumber; }
                else
                {
                    double num;
                    if (double.TryParse(mc[0].Groups[1].Value, out num)) { acurateMass += num * 18.99840322 * multipliedNumber; formulaBean.Fnum = (int)(num * multipliedNumber); }
                }
            }

            mc = Regex.Matches(formula, "I(?!n|r)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) { acurateMass += 126.904473 * multipliedNumber; formulaBean.Inum = (int)multipliedNumber; }
                else
                {
                    double num;
                    if (double.TryParse(mc[0].Groups[1].Value, out num)) { acurateMass += num * 126.904473 * multipliedNumber; formulaBean.Inum = (int)(num * multipliedNumber); }
                }
            }

            mc = Regex.Matches(formula, "P(?!d|t|b|r)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) { acurateMass += 30.97376163 * multipliedNumber; formulaBean.Pnum = (int)multipliedNumber; }
                else
                {
                    double num;
                    if (double.TryParse(mc[0].Groups[1].Value, out num)) { acurateMass += num * 30.97376163 * multipliedNumber; formulaBean.Pnum = (int)(num + multipliedNumber); }
                }
            }
            formulaBean.IsCorrectlyImported = true;
            return (formulaBean, acurateMass);
        }
    }
}

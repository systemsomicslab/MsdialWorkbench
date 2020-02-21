using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class AdductIonParcer {
        private static double c13_c12 = 0.010815728;
        private static double h2_h1 = 0.000115013;
        private static double n15_n14 = 0.003653298;
        private static double o17_o16 = 0.000380926;
        private static double o18_o16 = 0.002054994;
        private static double s33_s32 = 0.007895568;
        private static double s34_s32 = 0.044741552;
        private static double k40_k39 = 0.000125458;
        private static double k41_k39 = 0.072167458;
        private static double ni60_ni58 = 0.385196175;

        public static Dictionary<string, double> IronToMass = new Dictionary<string, double>() {
            { "Be", 9.01218220000 }, { "Mg", 23.98504170000 }, { "Al", 26.98153863000 }, 
            { "Ca", 39.96259098000 }, { "Sc", 44.95591190000 }, { "Ti", 47.94794630000 }, { "V", 50.94395950000 }, { "Cr", 51.94050750000 }, { "Mn", 54.93804510000 },
            { "Fe", 55.93493750000 }, { "Cu", 62.92959750000 }, { "Zn", 63.92914220000 }, { "Ga", 68.92557360000 },
            { "Ge", 73.92117780000 }, { "Se", 79.91652130000 }, { "Kr", 83.91150700000 }, { "Rb", 84.91178973800 }, { "Sr", 87.90561210000 }, { "Zr", 89.90470440000 },
            { "Nb", 92.90637810000 }, { "Mo", 97.90540820000 }, { "Ru", 101.90434930000 }, { "Pd", 105.90348600000 }, { "Ag", 106.90509700000 }, { "Cd", 113.90335850000 },
            { "In", 114.90387800000 }, { "Sn", 119.90219470000 }, { "Sb", 120.90381570000 }, { "Cs", 132.90545193300 }, { "La", 138.90635330000 }
        };

        private AdductIonParcer() { }

        /// <summary>
        /// This method returns the AdductIon class variable from the adduct string.
        /// </summary>
        /// <param name="adductName">Add the formula string such as "C6H12O6"</param>
        /// <returns></returns>
        public static AdductIon GetAdductIonBean(string adductName)
        {
            AdductIon adduct = new AdductIon() { AdductIonName = adductName };

            if (IonTypeFormatChecker(adductName) == false) { adduct.FormatCheck = false; return adduct; }

            var chargeNum = GetChargeNumber(adductName);
            if (chargeNum == -1) { adduct.FormatCheck = false; return adduct; }

            var adductIonXmer = GetAdductIonXmer(adductName);
            var ionType = GetIonType(adductName);
            var isRadical = GetRadicalInfo(adductName);

            SetAccurateMassAndIsotopeRatio(adduct);

            adduct.AdductIonXmer = adductIonXmer;
            adduct.ChargeNumber = chargeNum;
            adduct.FormatCheck = true;
            adduct.IonMode = ionType;
            adduct.IsRadical = isRadical;

            return adduct;
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

            var newAdduct = GetAdductIonBean(newAdductString);
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

        public static void SetAccurateMassAndIsotopeRatio(AdductIon adductIonBean)
        {
            string adductName = adductIonBean.AdductIonName;
            string trimedAdductName = adductName.Split('[')[1].Split(']')[0].Trim();

            adductIonBean.AdductIonAccurateMass = 0;
            if (!trimedAdductName.Contains('+') && !trimedAdductName.Contains('-')) return;

            int equationNum = CountChar(trimedAdductName, '+') + CountChar(trimedAdductName, '-');

            string formula = string.Empty;
            int counter = 0;
            for (int i = trimedAdductName.Length - 1; i >= 0; i--)
            {
                if (trimedAdductName[i].Equals('+'))
                {
                    SetAccurateMassAndIsotopeRatioOfMolecularFormula(adductIonBean, formula, 1.0);

                    formula = string.Empty;
                    counter++;
                }
                else if (trimedAdductName[i].Equals('-'))
                {
                    SetAccurateMassAndIsotopeRatioOfMolecularFormula(adductIonBean, formula, -1.0);

                    formula = string.Empty;
                    counter++;
                }
                else
                {
                    formula = trimedAdductName[i] + formula;
                }
                if (counter >= equationNum) break;
            }
        }

        public static int CountChar(string s, char c)
        {
            return s.Length - s.Replace(c.ToString(), "").Length;
        }

        public static int GetAdductIonXmer(string adductName)
        {
            string xMerString = adductName.Split('[')[1];

            string numChar = string.Empty;
            for (int i = 0; i < xMerString.Length; i++)
            {
                if (char.IsNumber(xMerString[i]))
                {
                    numChar += xMerString[i];
                }
                else
                {
                    if (numChar == string.Empty) return 1;
                    else
                    {
                        return int.Parse(numChar);
                    }
                }
            }

            return int.Parse(numChar);
        }

        public static bool IonTypeFormatChecker(string adductName)
        {
            if (adductName == null) return false;
            if (!adductName.Contains('[') || !adductName.Contains(']')) return false;

            if (adductName.Split(']').Length != 2) return false;

            string chargeInfo = adductName.Split(']')[1];
            if (!chargeInfo.Contains('+') && !chargeInfo.Contains('-')) return false;

            if (adductName.Contains('(') || adductName.Contains(')')) return false;

            return true;
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


        private static bool GetRadicalInfo(string adductName) {
            var chargeString = adductName.Split(']')[1];
            if (chargeString.Contains('.')) return true;
            else return false;
        }


        public static int GetChargeNumber(string adductName)
        {
            string chargeString = adductName.Split(']')[1];
            if (chargeString.Contains('+'))
            {
                string chargeNum = chargeString.Split('+')[0];
                if (chargeNum == string.Empty) return 1;
                else
                {
                    int num;
                    if (!int.TryParse(chargeNum, out num))
                    {
                        return -1;
                    }
                    else
                    {
                        return num;
                    }
                }
            }
            else
            {
                string chargeNum = chargeString.Split('-')[0];
                if (chargeNum == string.Empty) return 1;
                else
                {
                    int num;
                    if (!int.TryParse(chargeNum, out num))
                    {
                        return -1;
                    }
                    else
                    {
                        return num;
                    }
                }
            }
        }

        public static void SetAccurateMassAndIsotopeRatioOfMolecularFormula(AdductIon adductIonBean, string formula, double mode)
        {
            double acurateMass = 0;
            double multipliedNum = 1.0;

            MatchCollection mc;

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

            if (numString != string.Empty)
            {
                double.TryParse(numString, out multipliedNum);
                if (numString.Length == formula.Length)
                {
                    adductIonBean.AdductIonAccurateMass += mode * multipliedNum;
                    adductIonBean.M1Intensity += 0;
                    adductIonBean.M2Intensity += 0;
                    return;
                }
            }

            formula = formula.Substring(numString.Length);

            //Common adduct check
            #region
            if (formula.Equals("Na"))
            {
                adductIonBean.AdductIonAccurateMass += mode * multipliedNum * 22.9897692809;
                adductIonBean.M1Intensity += mode * 0;
                adductIonBean.M2Intensity += mode * 0;
                return;
            }
            else if (formula.Equals("H"))
            {
                adductIonBean.AdductIonAccurateMass += mode * multipliedNum * 1.00782503207;
                adductIonBean.M1Intensity += mode * multipliedNum * h2_h1;
                adductIonBean.M2Intensity += mode * multipliedNum * (multipliedNum - 1) * 0.5 * Math.Pow(h2_h1, 2);
                return;
            }
            else if (formula.Equals("K"))
            {
                adductIonBean.AdductIonAccurateMass += mode * multipliedNum * 38.96370668;
                adductIonBean.M1Intensity += mode * multipliedNum * k40_k39;
                adductIonBean.M2Intensity += mode * multipliedNum * (multipliedNum - 1) * 0.5 * Math.Pow(k40_k39, 2) + multipliedNum * k41_k39;
                return;
            }
            else if (formula.Equals("Li"))
            {
                adductIonBean.AdductIonAccurateMass += mode * multipliedNum * 7.01600455;
                adductIonBean.M1Intensity += mode * 0;
                adductIonBean.M2Intensity += mode * 0;
                return;
            }
            else if (formula.Equals("ACN") || formula.Equals("CH3CN"))
            {
                adductIonBean.AdductIonAccurateMass += mode * multipliedNum * 41.0265491;
                adductIonBean.M1Intensity += mode * multipliedNum * (2 * c13_c12 + 3 * h2_h1 + n15_n14);
                adductIonBean.M2Intensity += mode * (multipliedNum * (2 * c13_c12 * 3 * h2_h1 + 2 * c13_c12 * n15_n14 + 3 * h2_h1 * n15_n14)
                    + 2 * multipliedNum * (2 * multipliedNum - 1) * 0.5 * Math.Pow(c13_c12, 2)
                    + 3 * multipliedNum * (3 * multipliedNum - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + multipliedNum * (multipliedNum - 1) * 0.5 * Math.Pow(n15_n14, 2));
                return;
            }
            else if (formula.Equals("NH4"))
            {
                adductIonBean.AdductIonAccurateMass += mode * multipliedNum * 18.03437413;
                adductIonBean.M1Intensity += mode * multipliedNum * (4 * h2_h1 + n15_n14);
                adductIonBean.M2Intensity += mode * (multipliedNum * (4 * h2_h1 * n15_n14)
                    + 4 * multipliedNum * (4 * multipliedNum - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + multipliedNum * (multipliedNum - 1) * 0.5 * Math.Pow(n15_n14, 2));
                return;
            }
            else if (formula.Equals("CH3OH"))
            {
                adductIonBean.AdductIonAccurateMass += mode * multipliedNum * 32.02621475;
                adductIonBean.M1Intensity += mode * multipliedNum * (c13_c12 + 4 * h2_h1 + o17_o16);
                adductIonBean.M2Intensity += mode * (multipliedNum * (c13_c12 * 4 * h2_h1 + c13_c12 * o17_o16 + 4 * h2_h1 * o17_o16)
                    + multipliedNum * (multipliedNum - 1) * 0.5 * Math.Pow(c13_c12, 2)
                    + 4 * multipliedNum * (4 * multipliedNum - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + multipliedNum * (multipliedNum - 1) * 0.5 * Math.Pow(o17_o16, 2)
                    + multipliedNum * o18_o16);
                return;
            }
            else if (formula.Equals("H2O"))
            {
                adductIonBean.AdductIonAccurateMass += mode * multipliedNum * 18.01056468;
                adductIonBean.M1Intensity += mode * multipliedNum * (2 * h2_h1 + o17_o16);
                adductIonBean.M2Intensity += mode * (multipliedNum * (2 * h2_h1 * o17_o16)
                    + 2 * multipliedNum * (2 * multipliedNum - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + multipliedNum * (multipliedNum - 1) * 0.5 * Math.Pow(o17_o16, 2)
                    + multipliedNum * o18_o16);
                return;
            }
            else if (formula.Equals("IsoProp") || formula.Equals("C3H7OH"))
            {
                adductIonBean.AdductIonAccurateMass += mode * multipliedNum * 60.05751488;
                adductIonBean.M1Intensity += mode * multipliedNum * (3 * c13_c12 + 8 * h2_h1 + o17_o16);
                adductIonBean.M2Intensity += mode * (multipliedNum * (3 * c13_c12 * 8 * h2_h1 + 3 * c13_c12 * o17_o16 + 8 * h2_h1 * o17_o16)
                    + 3 * multipliedNum * (3 * multipliedNum - 1) * 0.5 * Math.Pow(c13_c12, 2)
                    + 8 * multipliedNum * (8 * multipliedNum - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + multipliedNum * (multipliedNum - 1) * 0.5 * Math.Pow(o17_o16, 2)
                    + multipliedNum * o18_o16);
                return;
            }
            else if (formula.Equals("DMSO") || formula.Equals("C2H6OS"))
            {
                adductIonBean.AdductIonAccurateMass += mode * multipliedNum * 78.013936;
                adductIonBean.M1Intensity += mode * multipliedNum * (2 * c13_c12 + 6 * h2_h1 + o17_o16 + s33_s32);
                adductIonBean.M2Intensity += mode * (multipliedNum * (2 * c13_c12 * 6 * h2_h1 + 2 * c13_c12 * o17_o16 + 2 * c13_c12 * s33_s32 + 6 * h2_h1 * o17_o16 + 6 * h2_h1 * s33_s32 + o17_o16 * s33_s32)
                    + 2 * multipliedNum * (2 * multipliedNum - 1) * 0.5 * Math.Pow(c13_c12, 2)
                    + 6 * multipliedNum * (6 * multipliedNum - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + multipliedNum * (multipliedNum - 1) * 0.5 * Math.Pow(o17_o16, 2)
                    + multipliedNum * (multipliedNum - 1) * 0.5 * Math.Pow(s33_s32, 2)
                    + multipliedNum * o18_o16
                    + multipliedNum * s34_s32);
                return;
            }
            else if (formula.Equals("FA") || formula.Equals("HCOOH"))
            {
                adductIonBean.AdductIonAccurateMass += mode * multipliedNum * 46.005479;
                adductIonBean.M1Intensity += mode * multipliedNum * (c13_c12 + 2 * h2_h1 + 2 * o17_o16);
                adductIonBean.M2Intensity += mode * (multipliedNum * (c13_c12 * 2 * h2_h1 + c13_c12 * 2 * o17_o16 + 2 * h2_h1 * 2 * o17_o16)
                    + multipliedNum * (multipliedNum - 1) * 0.5 * Math.Pow(c13_c12, 2)
                    + 2 * multipliedNum * (2 * multipliedNum - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + 2 * multipliedNum * (2 * multipliedNum - 1) * 0.5 * Math.Pow(o17_o16, 2)
                    + 2 * multipliedNum * o18_o16);
                return;
            }
            else if (formula.Equals("HCOO")) {
                adductIonBean.AdductIonAccurateMass += mode * multipliedNum * 44.997654;
                adductIonBean.M1Intensity += mode * multipliedNum * (c13_c12 + 1 * h2_h1 + 2 * o17_o16);
                adductIonBean.M2Intensity += mode * (multipliedNum * (c13_c12 * 1 * h2_h1 + c13_c12 * 2 * o17_o16 + 1 * h2_h1 * 2 * o17_o16)
                    + multipliedNum * (multipliedNum - 1) * 0.5 * Math.Pow(c13_c12, 2)
                    + 1 * multipliedNum * (1 * multipliedNum - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + 2 * multipliedNum * (2 * multipliedNum - 1) * 0.5 * Math.Pow(o17_o16, 2)
                    + 2 * multipliedNum * o18_o16);
                return;
            }
            else if (formula.Equals("Hac") || formula.Equals("CH3COOH") || formula.Equals("CH3CO2H") || formula.Equals("C2H4O2"))
            {
                adductIonBean.AdductIonAccurateMass += mode * multipliedNum * 60.021129;
                adductIonBean.M1Intensity += mode * multipliedNum * (2 * c13_c12 + 4 * h2_h1 + 2 * o17_o16);
                adductIonBean.M2Intensity += mode * (multipliedNum * (2 * c13_c12 * 4 * h2_h1 + 2 * c13_c12 * 2 * o17_o16 + 4 * h2_h1 * 2 * o17_o16)
                    + 2 * multipliedNum * (2 * multipliedNum - 1) * 0.5 * Math.Pow(c13_c12, 2)
                    + 4 * multipliedNum * (4 * multipliedNum - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + 2 * multipliedNum * (2 * multipliedNum - 1) * 0.5 * Math.Pow(o17_o16, 2)
                    + 2 * multipliedNum * o18_o16);
                return;
            }
            else if (formula.Equals("CH3COO")) {
                adductIonBean.AdductIonAccurateMass += mode * multipliedNum * 59.013305;
                adductIonBean.M1Intensity += mode * multipliedNum * (2 * c13_c12 + 3 * h2_h1 + 2 * o17_o16);
                adductIonBean.M2Intensity += mode * (multipliedNum * (2 * c13_c12 * 3 * h2_h1 + 2 * c13_c12 * 2 * o17_o16 + 3 * h2_h1 * 2 * o17_o16)
                    + 2 * multipliedNum * (2 * multipliedNum - 1) * 0.5 * Math.Pow(c13_c12, 2)
                    + 3 * multipliedNum * (3 * multipliedNum - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + 2 * multipliedNum * (2 * multipliedNum - 1) * 0.5 * Math.Pow(o17_o16, 2)
                    + 2 * multipliedNum * o18_o16);
                return;
            }
            else if (formula.Equals("CH3COONa")) {
                adductIonBean.AdductIonAccurateMass += mode * multipliedNum * 82.003075;
                adductIonBean.M1Intensity += mode * multipliedNum * (2 * c13_c12 + 3 * h2_h1 + 2 * o17_o16);
                adductIonBean.M2Intensity += mode * (multipliedNum * (2 * c13_c12 * 3 * h2_h1 + 2 * c13_c12 * 2 * o17_o16 + 3 * h2_h1 * 2 * o17_o16)
                    + 2 * multipliedNum * (2 * multipliedNum - 1) * 0.5 * Math.Pow(c13_c12, 2)
                    + 3 * multipliedNum * (3 * multipliedNum - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + 2 * multipliedNum * (2 * multipliedNum - 1) * 0.5 * Math.Pow(o17_o16, 2)
                    + 2 * multipliedNum * o18_o16);
                return;
            }
            else if (formula.Equals("TFA") || formula.Equals("CF3COOH"))
            {
                adductIonBean.AdductIonAccurateMass += mode * multipliedNum * 113.992864;
                adductIonBean.M1Intensity += mode * multipliedNum * (2 * c13_c12 + h2_h1 + 2 * o17_o16);
                adductIonBean.M2Intensity += mode * (multipliedNum * (2 * c13_c12 * h2_h1 + 2 * c13_c12 * 2 * o17_o16 + h2_h1 * 2 * o17_o16)
                    + 2 * multipliedNum * (2 * multipliedNum - 1) * 0.5 * Math.Pow(c13_c12, 2)
                    + multipliedNum * (multipliedNum - 1) * 0.5 * Math.Pow(h2_h1, 2)
                    + 2 * multipliedNum * (2 * multipliedNum - 1) * 0.5 * Math.Pow(o17_o16, 2)
                    + 2 * multipliedNum * o18_o16);
                return;
            }
            else if (formula.Equals("Co"))
            {
                adductIonBean.AdductIonAccurateMass += mode * multipliedNum * 58.933195;
                adductIonBean.M1Intensity += mode * 0;
                adductIonBean.M2Intensity += mode * 0;
                return;
            }
            else if (formula.Equals("Ni"))
            {
                adductIonBean.AdductIonAccurateMass += mode * multipliedNum * 57.9353429;
                adductIonBean.M1Intensity += mode * 0;
                adductIonBean.M2Intensity += mode * multipliedNum * ni60_ni58;
                return;
            }
            else if (formula.Equals("Ba"))
            {
                adductIonBean.AdductIonAccurateMass += mode * multipliedNum * 137.9052472;
                adductIonBean.M1Intensity += mode * 0;
                adductIonBean.M2Intensity += mode * 0;
                return;
            }

            // check irons
            foreach (var iron in IronToMass) {
                if (formula.Equals(iron.Key)) {
                    adductIonBean.AdductIonAccurateMass += mode * multipliedNum * iron.Value;
                    adductIonBean.M1Intensity += mode * 0;
                    adductIonBean.M2Intensity += mode * 0;
                    return;
                }
            }
            #endregion

            //Organic compound adduct check
            #region
            var formulaBean = new Formula();
            mc = Regex.Matches(formula, "C(?!a|d|e|l|o|r|s|u)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) { acurateMass += 12.0 * multipliedNum; formulaBean.Cnum = (int)multipliedNum; }
                else
                {
                    double num;
                    if (double.TryParse(mc[0].Groups[1].Value, out num)) { acurateMass += num * 12.0 * multipliedNum; formulaBean.Cnum = (int)(num * multipliedNum); }
                }
            }

            mc = Regex.Matches(formula, "H(?!e|f|g|o)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) { acurateMass += 1.00782503207 * multipliedNum; formulaBean.Hnum = (int)multipliedNum; }
                else
                {
                    double num;
                    if (double.TryParse(mc[0].Groups[1].Value, out num)) { acurateMass += num * 1.00782503207 * multipliedNum; formulaBean.Hnum = (int)(num * multipliedNum); }
                }
            }

            mc = Regex.Matches(formula, "N(?!a|b|d|e|i)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) { acurateMass += 14.00307400480 * multipliedNum; formulaBean.Nnum = (int)multipliedNum; }
                else
                {
                    double num;
                    if (double.TryParse(mc[0].Groups[1].Value, out num)) { acurateMass += num * 14.00307400480 * multipliedNum; formulaBean.Nnum = (int)(num * multipliedNum); }
                }
            }

            mc = Regex.Matches(formula, "O(?!s)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) { acurateMass += 15.99491461956 * multipliedNum; formulaBean.Onum = (int)multipliedNum; }
                else
                {
                    double num;
                    if (double.TryParse(mc[0].Groups[1].Value, out num)) { acurateMass += num * 15.99491461956 * multipliedNum; formulaBean.Onum = (int)(num * multipliedNum); }
                }
            }

            mc = Regex.Matches(formula, "S(?!b|c|e|i|m|n|r)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) { acurateMass += 31.972071 * multipliedNum; formulaBean.Snum = (int)multipliedNum; }
                else
                {
                    double num;
                    if (double.TryParse(mc[0].Groups[1].Value, out num)) { acurateMass += num * 31.972071 * multipliedNum; formulaBean.Snum = (int)(num * multipliedNum); }
                }
            }

            mc = Regex.Matches(formula, "Br([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) { acurateMass += 78.9183371 * multipliedNum; formulaBean.Brnum = (int)multipliedNum; }
                else
                {
                    double num;
                    if (double.TryParse(mc[0].Groups[1].Value, out num)) { acurateMass += num * 78.9183371 * multipliedNum; formulaBean.Brnum = (int)(num * multipliedNum); }
                }
            }

            mc = Regex.Matches(formula, "Cl([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) { acurateMass += 34.96885268 * multipliedNum; formulaBean.Clnum = (int)multipliedNum; }
                else
                {
                    double num;
                    if (double.TryParse(mc[0].Groups[1].Value, out num)) { acurateMass += num * 34.96885268 * multipliedNum; formulaBean.Clnum = (int)(num * multipliedNum); }
                }
            }

            mc = Regex.Matches(formula, "F(?!e)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) { acurateMass += 18.99840322 * multipliedNum; formulaBean.Fnum = (int)multipliedNum; }
                else
                {
                    double num;
                    if (double.TryParse(mc[0].Groups[1].Value, out num)) { acurateMass += num * 18.99840322 * multipliedNum; formulaBean.Fnum = (int)(num * multipliedNum); }
                }
            }

            mc = Regex.Matches(formula, "I(?!n|r)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) { acurateMass += 126.904473 * multipliedNum; formulaBean.Inum = (int)multipliedNum; }
                else
                {
                    double num;
                    if (double.TryParse(mc[0].Groups[1].Value, out num)) { acurateMass += num * 126.904473 * multipliedNum; formulaBean.Inum = (int)(num * multipliedNum); }
                }
            }

            mc = Regex.Matches(formula, "P(?!d|t|b|r)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) { acurateMass += 30.97376163 * multipliedNum; formulaBean.Pnum = (int)multipliedNum; }
                else
                {
                    double num;
                    if (double.TryParse(mc[0].Groups[1].Value, out num)) { acurateMass += num * 30.97376163 * multipliedNum; formulaBean.Pnum = (int)(num + multipliedNum); }
                }
            }

            adductIonBean.AdductIonAccurateMass += mode * acurateMass;
            adductIonBean.M1Intensity += mode * SevenGoldenRulesCheck.GetM1IsotopicAbundance(formulaBean);
            adductIonBean.M2Intensity += mode * SevenGoldenRulesCheck.GetM2IsotopicAbundance(formulaBean);

            return;
            #endregion
        }
    }
}

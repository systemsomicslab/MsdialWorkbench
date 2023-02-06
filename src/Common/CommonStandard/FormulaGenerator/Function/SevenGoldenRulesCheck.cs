using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.FormulaGenerator.Function {
    public sealed class SevenGoldenRulesCheck
    {
        private SevenGoldenRulesCheck() { }

        private static double c13_c12 = 0.010815728;
        private static double h2_h1 = 0.000115013;
        private static double n15_n14 = 0.003653298;
        private static double o17_o16 = 0.000380926;
        private static double o18_o16 = 0.002054994;
        private static double si29_si28 = 0.050800776;
        private static double si30_si28 = 0.033527428;
        private static double s33_s32 = 0.007895568;
        private static double s34_s32 = 0.044741552;
        private static double cl37_cl35 = 0.319957761;
        private static double br81_br79 = 0.972775695;

        //private static double c13_c12_MassDiff = 1.003354838;
        //private static double h2_h1_MassDiff = 1.006276746;
        //private static double n15_n14_MassDiff = 0.997034893;
        //private static double o17_o16_MassDiff = 1.00421708;
        //private static double o18_o16_MassDiff = 2.00424638;
        //private static double si29_si28_MassDiff = 0.999568168;
        //private static double si30_si28_MassDiff = 1.996843638;
        //private static double s33_s32_MassDiff = 0.99938776;
        //private static double s34_s32_MassDiff = 1.9957959;
        //private static double cl37_cl35_MassDiff = 1.99704991;
        //private static double br81_br79_MassDiff = 1.9979535;

        public static bool Check(Formula formula, bool isValenceCheck, CoverRange coverRange, bool isElementProbabilityCheck, AdductIon adduct)
        {
            if (adduct.AdductIonName == "[M]+" || adduct.AdductIonName == "[M]-" || adduct.AdductIonName == "[M-2H]-") {
                if (isValenceCheck && !ValenceCheckByHydrogenShift(formula)) return false;
            }
            else {
                if (isValenceCheck && !ValenceCheck(formula)) return false;
            }


            if (!HeteroAtomCheck(formula, coverRange)) return false;
            if (isElementProbabilityCheck && !ProbabilityCheck(formula)) return false;
            
            return true;
        }

        public static bool ValenceCheck(Formula formula)
        {
            int atomTotal = formula.Brnum + formula.Clnum + formula.Cnum + formula.Fnum + formula.Hnum + formula.Inum + formula.Nnum + formula.Onum + formula.Pnum + formula.Sinum + formula.Snum;
            int oddValenceAtomTotal = formula.Brnum + formula.Clnum + formula.Fnum + formula.Hnum + formula.Inum + formula.Nnum + formula.Pnum;
            int valenceTotal = ValenceDictionary.ValenceDict["Br"] * formula.Brnum
                + ValenceDictionary.ValenceDict["Cl"] * formula.Clnum
                + ValenceDictionary.ValenceDict["C"] * formula.Cnum
                + ValenceDictionary.ValenceDict["F"] * formula.Fnum
                + ValenceDictionary.ValenceDict["H"] * formula.Hnum
                + ValenceDictionary.ValenceDict["I"] * formula.Inum
                + ValenceDictionary.ValenceDict["N"] * formula.Nnum
                + ValenceDictionary.ValenceDict["O"] * formula.Onum
                + ValenceDictionary.ValenceDict["P"] * formula.Pnum
                + ValenceDictionary.ValenceDict["Si"] * formula.Sinum
                + ValenceDictionary.ValenceDict["S"] * formula.Snum;

            if (oddValenceAtomTotal % 2 == 1 && valenceTotal % 2 == 1) return false;
            if (valenceTotal < 2 * (atomTotal - 1)) return false;

            return true;
        }

        public static bool ValenceCheckByHydrogenShift(Formula formula)
        {
            int atomTotal = formula.Brnum + formula.Clnum + formula.Cnum + formula.Fnum + formula.Hnum + formula.Inum + formula.Nnum + formula.Onum + formula.Pnum + formula.Sinum + formula.Snum;
            int oddValenceAtomTotal = formula.Brnum + formula.Clnum + formula.Fnum + formula.Hnum + formula.Inum + formula.Nnum + formula.Pnum;
            int valenceTotal = ValenceDictionary.ValenceDict["Br"] * formula.Brnum
                + ValenceDictionary.ValenceDict["Cl"] * formula.Clnum
                + ValenceDictionary.ValenceDict["C"] * formula.Cnum
                + ValenceDictionary.ValenceDict["F"] * formula.Fnum
                + ValenceDictionary.ValenceDict["H"] * formula.Hnum
                + ValenceDictionary.ValenceDict["I"] * formula.Inum
                + ValenceDictionary.ValenceDict["N"] * formula.Nnum
                + ValenceDictionary.ValenceDict["O"] * formula.Onum
                + ValenceDictionary.ValenceDict["P"] * formula.Pnum
                + ValenceDictionary.ValenceDict["Si"] * formula.Sinum
                + ValenceDictionary.ValenceDict["S"] * formula.Snum;

            int shiftMinus = -1, shiftPlus = 1; 
            
            int atomTotalMinusShifted = atomTotal + shiftMinus;
            int oddValenceAtomTotalMinusShifted = oddValenceAtomTotal + shiftMinus;
            int valenceTotalMinusShifted = valenceTotal + shiftMinus;

            int atomTotalPlusShifted = atomTotal + shiftPlus;
            int oddValenceAtomTotalPlusShifted = oddValenceAtomTotal + shiftPlus;
            int valenceTotalPlusShifted = valenceTotal + shiftPlus;

            if (oddValenceAtomTotalMinusShifted % 2 == 1 && valenceTotalMinusShifted % 2 == 1 && oddValenceAtomTotalPlusShifted % 2 == 1 && valenceTotalPlusShifted % 2 == 1) return false;
            if (valenceTotalMinusShifted < 2 * (atomTotalMinusShifted - 1) && valenceTotalPlusShifted < 2 * (atomTotalPlusShifted - 1)) return false;

            return true;
        }

        public static List<IsotopicPeak> GetIsotopicPeaks(Formula formula)
        {
            var isotopicPeaks = new List<IsotopicPeak>();

            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = 1, Mass = formula.Mass, Comment = formula.FormulaString });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Cnum * c13_c12, Mass = formula.Mass + MassDiffDictionary.C13_C12, Comment = "13C" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Hnum * h2_h1, Mass = formula.Mass + MassDiffDictionary.H2_H1, Comment = "2H" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Nnum * n15_n14, Mass = formula.Mass + MassDiffDictionary.N15_N14, Comment = "15N" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Onum * o17_o16, Mass = formula.Mass + MassDiffDictionary.O17_O16, Comment = "17O" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Snum * s33_s32, Mass = formula.Mass + MassDiffDictionary.S33_S32, Comment = "33S" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Sinum * si29_si28, Mass = formula.Mass + MassDiffDictionary.Si29_Si28, Comment = "29Si" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Cnum * c13_c12 * formula.Hnum * h2_h1, Mass = formula.Mass + MassDiffDictionary.C13_C12 + MassDiffDictionary.H2_H1, Comment = "2H,13C" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Cnum * c13_c12 * formula.Nnum * n15_n14, Mass = formula.Mass + MassDiffDictionary.C13_C12 + MassDiffDictionary.N15_N14, Comment = "13C,15N" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Cnum * c13_c12 * formula.Onum * o17_o16, Mass = formula.Mass + MassDiffDictionary.C13_C12 + MassDiffDictionary.O17_O16, Comment = "13C,17O" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Cnum * c13_c12 * formula.Snum * s33_s32, Mass = formula.Mass + MassDiffDictionary.C13_C12 + MassDiffDictionary.S33_S32, Comment = "13C,33S" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Cnum * c13_c12 * formula.Sinum * si29_si28, Mass = formula.Mass + MassDiffDictionary.C13_C12 + MassDiffDictionary.Si29_Si28, Comment = "13C,29Si" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Hnum * h2_h1 * formula.Nnum * n15_n14, Mass = formula.Mass + MassDiffDictionary.H2_H1 + MassDiffDictionary.N15_N14, Comment = "2H,15N" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Hnum * h2_h1 * formula.Onum * o17_o16, Mass = formula.Mass + MassDiffDictionary.H2_H1 + MassDiffDictionary.O17_O16, Comment = "2H,17O" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Hnum * h2_h1 * formula.Snum * s33_s32, Mass = formula.Mass + MassDiffDictionary.H2_H1 + MassDiffDictionary.S33_S32, Comment = "2H,33S" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Hnum * h2_h1 * formula.Sinum * si29_si28, Mass = formula.Mass + MassDiffDictionary.H2_H1 + MassDiffDictionary.Si29_Si28, Comment = "2H,29Si" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Nnum * n15_n14 * formula.Onum * o17_o16, Mass = formula.Mass + MassDiffDictionary.N15_N14 + MassDiffDictionary.O17_O16, Comment = "15N,17O" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Nnum * n15_n14 * formula.Snum * s33_s32, Mass = formula.Mass + MassDiffDictionary.N15_N14 + MassDiffDictionary.S33_S32, Comment = "15N,33S" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Nnum * n15_n14 * formula.Sinum * si29_si28, Mass = formula.Mass + MassDiffDictionary.N15_N14 + MassDiffDictionary.Si29_Si28, Comment = "15N,29Si" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Onum * o17_o16 * formula.Snum * s33_s32, Mass = formula.Mass + MassDiffDictionary.O17_O16 + MassDiffDictionary.S33_S32, Comment = "17O,33S" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Onum * o17_o16 * formula.Sinum * si29_si28, Mass = formula.Mass + MassDiffDictionary.O17_O16 + MassDiffDictionary.Si29_Si28, Comment = "17O,29Si" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Snum * s33_s32 * formula.Sinum * si29_si28, Mass = formula.Mass + MassDiffDictionary.S33_S32 + MassDiffDictionary.Si29_Si28, Comment = "29Si,33S" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Cnum * (formula.Cnum - 1) * 0.5 * Math.Pow(c13_c12, 2), Mass = formula.Mass + MassDiffDictionary.C13_C12 * 2.0, Comment = "13C,13C" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Hnum * (formula.Hnum - 1) * 0.5 * Math.Pow(h2_h1, 2), Mass = formula.Mass + MassDiffDictionary.H2_H1 * 2.0, Comment = "2H,2H" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Nnum * (formula.Nnum - 1) * 0.5 * Math.Pow(n15_n14, 2), Mass = formula.Mass + MassDiffDictionary.N15_N14 * 2.0, Comment = "15N,15N" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Onum * (formula.Onum - 1) * 0.5 * Math.Pow(o17_o16, 2), Mass = formula.Mass + MassDiffDictionary.O17_O16 * 2.0, Comment = "17O,17O" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Snum * (formula.Snum - 1) * 0.5 * Math.Pow(s33_s32, 2), Mass = formula.Mass + MassDiffDictionary.S33_S32 * 2.0, Comment = "33S,33S" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Sinum * (formula.Sinum - 1) * 0.5 * Math.Pow(si29_si28, 2), Mass = formula.Mass + MassDiffDictionary.Si29_Si28 * 2.0, Comment = "29Si,29Si" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Onum * o18_o16, Mass = formula.Mass + MassDiffDictionary.O18_O16, Comment = "18O" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Snum * s34_s32, Mass = formula.Mass + MassDiffDictionary.S34_S32, Comment = "34S" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Sinum * si30_si28, Mass = formula.Mass + MassDiffDictionary.Si30_Si28, Comment = "30Si" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Brnum * br81_br79, Mass = formula.Mass + MassDiffDictionary.Br81_Br79, Comment = "81Br" });
            isotopicPeaks.Add(new IsotopicPeak() { RelativeAbundance = formula.Clnum * cl37_cl35, Mass = formula.Mass + MassDiffDictionary.Cl37_Cl35, Comment = "37Cl" });

            return isotopicPeaks;
        }

        public static double GetM1IsotopicAbundance(Formula formula)
        {
            double abundance = formula.Cnum * c13_c12 + formula.Hnum * h2_h1 + formula.Nnum * n15_n14 + formula.Onum * o17_o16 + formula.Snum * s33_s32 + formula.Sinum * si29_si28;
            return abundance;
        }

        public static double GetM2IsotopicAbundance(Formula formula)
        {
            double abundance
                = formula.Cnum * c13_c12 * formula.Hnum * h2_h1 + formula.Cnum * c13_c12 * formula.Nnum * n15_n14 + formula.Cnum * c13_c12 * formula.Onum * o17_o16 + formula.Cnum * c13_c12 * formula.Snum * s33_s32 + formula.Cnum * c13_c12 * formula.Sinum * si29_si28
                + formula.Hnum * h2_h1 * formula.Nnum * n15_n14 + formula.Hnum * h2_h1 * formula.Onum * o17_o16 + formula.Hnum * h2_h1 * formula.Snum * s33_s32 + formula.Hnum * h2_h1 * formula.Sinum * si29_si28
                + formula.Nnum * n15_n14 * formula.Onum * o17_o16 + formula.Nnum * n15_n14 * formula.Snum * s33_s32 + formula.Nnum * n15_n14 * formula.Sinum * si29_si28
                + formula.Onum * o17_o16 * formula.Snum * s33_s32 + formula.Onum * o17_o16 * formula.Sinum * si29_si28
                + formula.Snum * s33_s32 * formula.Sinum * si29_si28
                + formula.Cnum * (formula.Cnum - 1) * 0.5 * Math.Pow(c13_c12, 2)
                + formula.Hnum * (formula.Hnum - 1) * 0.5 * Math.Pow(h2_h1, 2)
                + formula.Nnum * (formula.Nnum - 1) * 0.5 * Math.Pow(n15_n14, 2)
                + formula.Onum * (formula.Onum - 1) * 0.5 * Math.Pow(o17_o16, 2)
                + formula.Snum * (formula.Snum - 1) * 0.5 * Math.Pow(s33_s32, 2)
                + formula.Sinum * (formula.Sinum - 1) * 0.5 * Math.Pow(si29_si28, 2)
                + formula.Onum * o18_o16 + formula.Snum * s34_s32 + formula.Sinum * si30_si28 + formula.Brnum * br81_br79 + formula.Clnum * cl37_cl35;
            return abundance;
        }

        public static double GetIsotopicDifference(double tAbundance, double m1Intensity)
        {
            double diff = tAbundance - m1Intensity;
            return diff;
        }

        public static bool HeteroAtomCheck(Formula formula, CoverRange coverRange)
        {
            double cnum = formula.Cnum, nnum = formula.Nnum, onum = formula.Onum, pnum = formula.Pnum, snum = formula.Snum, hnum = formula.Hnum, fnum = formula.Fnum, clnum = formula.Clnum, brnum = formula.Brnum, inum = formula.Inum, sinum = formula.Sinum;
            double n_c = nnum / cnum, o_c = onum / cnum, p_c = pnum / cnum, s_c = snum / cnum, h_c = hnum / cnum, f_c = fnum / cnum, cl_c = clnum / cnum, br_c = brnum / cnum, i_c = inum / cnum, si_c = sinum / cnum;
            double o_p;
            if (pnum > 0) o_p = onum / pnum; else o_p = 4;
            
            switch (coverRange)
            {
                case CoverRange.CommonRange:
                    if (h_c <= 4.0 && f_c <= 1.5 && cl_c <= 1.0 && br_c <= 1.0 && si_c <= 0.5 && n_c <= 2.0 && o_c <= 2.5 && p_c <= 0.5 && s_c <= 1.0 && i_c <= 0.5 && o_p >= 2.0) return true;
                    else return false;
                case CoverRange.ExtendedRange:
                    if (h_c <= 6.0 && f_c <= 3.0 && cl_c <= 3.0 && br_c <= 2.0 && si_c <= 1.0 && n_c <= 4.0 && o_c <= 6.0 && p_c <= 1.9 && s_c <= 3.0 && i_c <= 1.9) return true;
                    else return false;
                default:
                    if (h_c <= 8.0 && f_c <= 4.0 && cl_c <= 4.0 && br_c <= 4.0 && si_c <= 3.0 && n_c <= 4.0 && o_c <= 10.0 && p_c <= 3.0 && s_c <= 6.0 && i_c <= 3.0) return true;
                    else return false;
            }
        }

        public static bool ProbabilityCheck(Formula formula)
        {
            if (formula.Nnum > 1 && formula.Onum > 1 && formula.Pnum > 1 && formula.Snum > 1)
            {
                if (formula.Nnum >= 10 || formula.Onum >= 20 || formula.Pnum >= 4 || formula.Snum >= 3) return false;
            }

            if (formula.Nnum > 3 && formula.Onum > 3 && formula.Pnum > 3)
            {
                if (formula.Nnum >= 11 || formula.Onum >= 22 || formula.Pnum >= 6) return false;
            }

            if (formula.Onum > 1 && formula.Pnum > 1 && formula.Snum > 1)
            {
                if (formula.Onum >= 14 || formula.Pnum >= 3 || formula.Snum >= 3) return false;
            }

            if (formula.Nnum > 1 && formula.Pnum > 1 && formula.Snum > 1)
            {
                if (formula.Nnum >= 4 || formula.Pnum >= 3 || formula.Snum >= 3) return false;
            }

            if (formula.Nnum > 6 && formula.Onum > 6 && formula.Snum > 6)
            {
                if (formula.Nnum >= 19 || formula.Onum >= 14 || formula.Snum >= 8) return false;
            }

            return true;
        }

    }
}

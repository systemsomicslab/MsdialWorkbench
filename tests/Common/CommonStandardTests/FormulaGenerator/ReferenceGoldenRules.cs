// Generated from the CommonStandard SevenGoldenRulesCheck before the formula prediction speed-up
// (MsdialWorkbench 4ff7e299b). Kept verbatim as the reference the optimised checks are compared with.
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Parameter;

namespace CompMs.Common.FormulaGenerator.Tests
{
    internal static class ReferenceGoldenRules
    {
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

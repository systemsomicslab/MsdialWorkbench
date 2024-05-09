using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.FormulaGenerator.Function {
    public static class MolecularFormulaUtility
    {
        public static double ConvertSinglyChargedPrecursorMzAsProtonAdduct(double precursorMz, double chargeNum) {
            var hydrogen = 1.00782504;
            return precursorMz * chargeNum - (chargeNum - 1) * hydrogen;
        }

        public static double ConvertPrecursorMzToExactMass(AdductIon adductIon, double precursorMz)
        {
            return adductIon.ConvertToMz(precursorMz);
        }

        public static double ConvertExactMassToPrecursorMz(AdductIon adductIon, double exactMass)
        {
            return adductIon.ConvertToMz(exactMass);
        }

        public static double PpmCalculator(double exactMass, double actualMass)
        {
            double ppm = Math.Round((actualMass - exactMass) / exactMass * 1000000, 4);
            return ppm;
        }

        public static double ConvertPpmToMassAccuracy(double exactMass, double ppm)
        {
            return ppm * exactMass / 1000000.0;
        }

        public static double FixMassTolerance(double tolerance, double mass) {
            if (mass <= 500)
                return tolerance;
            var ppm = Math.Abs(PpmCalculator(500.00, 500.00 + tolerance));
            return ConvertPpmToMassAccuracy(mass, ppm);
        }

        public static Formula ConvertTmsMeoxSubtractedFormula(Formula formula) {
            if (formula.TmsCount == 0 && formula.MeoxCount == 0) return formula;
            var tmsCount = formula.TmsCount;
            var meoxCount = formula.MeoxCount;

            var cNum = formula.Cnum - tmsCount * 3 - meoxCount;
            var hNum = formula.Hnum - tmsCount * 8 - meoxCount * 3;
            var nNum = formula.Nnum - meoxCount;
            var oNum = formula.Onum;
            var pNum = formula.Pnum;
            var sNum = formula.Snum;
            var fNum = formula.Fnum;
            var clNum = formula.Clnum;
            var brNum = formula.Brnum;
            var iNum = formula.Inum;
            var siNum = formula.Sinum - tmsCount;

            return new Formula(cNum, hNum, nNum, oNum, pNum, sNum, fNum, clNum, brNum, iNum, siNum, 0, 0);
        }

        public static bool IsFormulaCHNOSP(Formula formula) {
            if (formula.Fnum == 0 && formula.Clnum == 0 && formula.Brnum == 0 &&
               formula.Inum == 0 && formula.Sinum == 0) {
                return true;
            }
            else {
                return false;
            }
        }

        public static bool isFormulaMatch(Formula formula1, Formula formula2, bool isIgnoreHydrogen = false)
        {
            if (!isIgnoreHydrogen && formula1.Hnum != formula2.Hnum) return false;

            if (formula1.Cnum == formula2.Cnum && 
                formula1.Nnum == formula2.Nnum &&
                formula1.Onum == formula2.Onum && 
                formula1.Snum == formula2.Snum && 
                formula1.Pnum == formula2.Pnum &&
                formula1.Fnum == formula2.Fnum && 
                formula1.Clnum == formula2.Clnum && 
                formula1.Brnum == formula2.Brnum &&
                formula1.Inum == formula2.Inum && 
                formula1.Sinum == formula2.Sinum &&
                formula1.TmsCount == formula2.TmsCount && 
                formula1.MeoxCount == formula2.MeoxCount) {
                return true;
            }
            else {
                return false;
            }
        }

        public static Formula SumFormulas(Formula formula1, Formula formula2) {
            if (formula1 == null || formula2 == null) return null;
            if (formula1.Element2Count.IsEmptyOrNull() || formula2.Element2Count.IsEmptyOrNull()) return null;
            var dict = new Dictionary<string, int>();
            foreach (var pair in formula1.Element2Count) {
                dict[pair.Key] = pair.Value;
            }
            foreach (var pair in formula2.Element2Count) {
                if (dict.ContainsKey(pair.Key))
                    dict[pair.Key] += pair.Value;
                else
                    dict[pair.Key] = pair.Value;
            }
            return new Formula(dict);
        }

        public static Formula SumFormulas(List<Formula> formulas) {
            if (formulas.IsEmptyOrNull()) return null;
            if (formulas.Count() == 1) return formulas[0];
            var dict = new Dictionary<string, int>();
            foreach (var pair in formulas[0].Element2Count) {
                dict[pair.Key] = pair.Value;
            }

            for (int i = 1; i < formulas.Count; i++) {
                var formula = formulas[i];
                foreach (var pair in formula.Element2Count) {
                    if (dict.ContainsKey(pair.Key))
                        dict[pair.Key] += pair.Value;
                    else
                        dict[pair.Key] = pair.Value;
                }
            }
            return new Formula(dict);
        }

        public static Formula ConvertFormulaAdductPairToPrecursorAdduct(Formula formula, AdductIon adduct) {

            if (adduct.IonMode == IonMode.Positive) {
                switch (adduct.AdductIonName) {
                    case "[M+H]+":
                        return new Formula() {
                            Cnum = formula.Cnum,
                            Hnum = formula.Hnum + 1,
                            Onum = formula.Onum,
                            Nnum = formula.Nnum,
                            Snum = formula.Snum,
                            Pnum = formula.Pnum,
                            Fnum = formula.Fnum,
                            Clnum = formula.Clnum,
                            Brnum = formula.Brnum,
                            Inum = formula.Inum,
                            Sinum = formula.Sinum,
                            TmsCount = formula.TmsCount,
                            MeoxCount = formula.MeoxCount,
                            IsCorrectlyImported = true,
                        };
                    case "[M]+":
                        return formula;
                    case "[M+NH4]+":
                        return new Formula() {
                            Cnum = formula.Cnum,
                            Hnum = formula.Hnum + 4,
                            Onum = formula.Onum,
                            Nnum = formula.Nnum + 1,
                            Snum = formula.Snum,
                            Pnum = formula.Pnum,
                            Fnum = formula.Fnum,
                            Clnum = formula.Clnum,
                            Brnum = formula.Brnum,
                            Inum = formula.Inum,
                            Sinum = formula.Sinum,
                            TmsCount = formula.TmsCount,
                            MeoxCount = formula.MeoxCount,
                            IsCorrectlyImported = true,
                        };
                    case "[M+Na]+":
                        return formula;
                    case "[M+H-H2O]+":
                        return new Formula() {
                            Cnum = formula.Cnum,
                            Hnum = formula.Hnum - 1,
                            Onum = formula.Onum - 1,
                            Nnum = formula.Nnum,
                            Snum = formula.Snum,
                            Pnum = formula.Pnum,
                            Fnum = formula.Fnum,
                            Clnum = formula.Clnum,
                            Brnum = formula.Brnum,
                            Inum = formula.Inum,
                            Sinum = formula.Sinum,
                            TmsCount = formula.TmsCount,
                            MeoxCount = formula.MeoxCount,
                            IsCorrectlyImported = true,
                        };
                    default:
                        return formula;
                }
            } else {
                switch (adduct.AdductIonName) {
                    case "[M-H]-":
                        return new Formula() {
                            Cnum = formula.Cnum,
                            Hnum = formula.Hnum - 1,
                            Onum = formula.Onum,
                            Nnum = formula.Nnum,
                            Snum = formula.Snum,
                            Pnum = formula.Pnum,
                            Fnum = formula.Fnum,
                            Clnum = formula.Clnum,
                            Brnum = formula.Brnum,
                            Inum = formula.Inum,
                            Sinum = formula.Sinum,
                            TmsCount = formula.TmsCount,
                            MeoxCount = formula.MeoxCount,
                            IsCorrectlyImported = true,
                        };
                    case "[M-H2O-H]-":
                        return new Formula() {
                            Cnum = formula.Cnum,
                            Hnum = formula.Hnum - 3,
                            Onum = formula.Onum - 1,
                            Nnum = formula.Nnum,
                            Snum = formula.Snum,
                            Pnum = formula.Pnum,
                            Fnum = formula.Fnum,
                            Clnum = formula.Clnum,
                            Brnum = formula.Brnum,
                            Inum = formula.Inum,
                            Sinum = formula.Sinum,
                            TmsCount = formula.TmsCount,
                            MeoxCount = formula.MeoxCount,
                            IsCorrectlyImported = true,
                        };
                    case "[M+FA-H]-":
                        return new Formula() {
                            Cnum = formula.Cnum + 1,
                            Hnum = formula.Hnum + 1,
                            Onum = formula.Onum + 2,
                            Nnum = formula.Nnum,
                            Snum = formula.Snum,
                            Pnum = formula.Pnum,
                            Fnum = formula.Fnum,
                            Clnum = formula.Clnum,
                            Brnum = formula.Brnum,
                            Inum = formula.Inum,
                            Sinum = formula.Sinum,
                            TmsCount = formula.TmsCount,
                            MeoxCount = formula.MeoxCount,
                            IsCorrectlyImported = true,
                        };
                    case "[M+Hac-H]-":
                        return new Formula() {
                            Cnum = formula.Cnum + 2,
                            Hnum = formula.Hnum + 3,
                            Onum = formula.Onum + 2,
                            Nnum = formula.Nnum,
                            Snum = formula.Snum,
                            Pnum = formula.Pnum,
                            Fnum = formula.Fnum,
                            Clnum = formula.Clnum,
                            Brnum = formula.Brnum,
                            Inum = formula.Inum,
                            Sinum = formula.Sinum,
                            TmsCount = formula.TmsCount,
                            MeoxCount = formula.MeoxCount,
                            IsCorrectlyImported = true,
                        };
                    default:
                        return formula;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class MolecularFormulaUtility
    {
        private MolecularFormulaUtility() { }

        public static double ConvertPrecursorMzToExactMass(AdductIon adductIon, double precursorMz)
        {
            double monoIsotopicMass = (precursorMz * (double)adductIon.ChargeNumber - adductIon.AdductIonAccurateMass) / (double)adductIon.AdductIonXmer;
            if (adductIon.IonMode == IonMode.Positive) monoIsotopicMass += 0.0005485799 * adductIon.ChargeNumber; else monoIsotopicMass -= 0.0005485799 * adductIon.ChargeNumber;
            return monoIsotopicMass;
        }

        public static double ConvertExactMassToPrecursorMz(AdductIon adductIon, double exactMass)
        {
            double precursorMz = (exactMass * adductIon.AdductIonXmer + adductIon.AdductIonAccurateMass) / adductIon.ChargeNumber;
            if (adductIon.IonMode == IonMode.Positive) precursorMz -= 0.0005485799 * adductIon.ChargeNumber; else precursorMz += 0.0005485799 * adductIon.ChargeNumber;
            return precursorMz;
        }

        public static double ConvertPrecursorMzToExactMass(double precursorMz, double adductMass, int chargeNum, int xMer, IonMode ionMode)
        {
            double monoIsotopicMass = (precursorMz * (double)chargeNum - adductMass) / (double)xMer;
            if (ionMode == IonMode.Positive) monoIsotopicMass += 0.0005485799 * chargeNum; else monoIsotopicMass -= 0.0005485799 * chargeNum;
            return monoIsotopicMass;
        }

        public static double ConvertExactMassToPrecursorMz(double exactMass, double adductMass, int chargeNum, int xMer, IonMode ionMode)
        {
            double precursorMz = (exactMass * xMer + adductMass) / chargeNum;
            if (ionMode == IonMode.Positive) precursorMz -= 0.0005485799 * chargeNum; else precursorMz += 0.0005485799 * chargeNum;
            return precursorMz;
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
                            MeoxCount = formula.MeoxCount
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
                            MeoxCount = formula.MeoxCount
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
                            MeoxCount = formula.MeoxCount
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
                            MeoxCount = formula.MeoxCount
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
                            MeoxCount = formula.MeoxCount
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
                            MeoxCount = formula.MeoxCount
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
                            MeoxCount = formula.MeoxCount
                        };
                    default:
                        return formula;
                }
            }
        }
    }
}

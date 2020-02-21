using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Rfx.Riken.OsakaUniv {
    public sealed class FormulaKyusyuUnivDbParcer
    {
        private FormulaKyusyuUnivDbParcer() { }

        public static List<Formula> GetFormulaBeanList(string dbFilePath, AnalysisParamOfMsfinder analysisParam, double maxMass) {
            string line;
            string[] lineArray;

            var formulaBeanList = new List<Formula>();
            Formula formula;

            using (StreamReader sr = new StreamReader(dbFilePath, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    line = sr.ReadLine();
                    if (line == string.Empty) break;
                    lineArray = line.Split(',');

                    formula = new Formula(int.Parse(lineArray[1]), 0, int.Parse(lineArray[3]), int.Parse(lineArray[2]), int.Parse(lineArray[5]), int.Parse(lineArray[4]), 0, 0, 0, 0, 0);
                    if (formula.Mass > maxMass) break;

                    if (dbFormulaCheck(formula, analysisParam)) {
                        formulaBeanList.Add(formula);
                    }
                }
            }

            return formulaBeanList;
        }

        public static List<Formula> GetFormulaBeanList(string dbFilePath, AnalysisParamOfMsfinder analysisParam, double maxMass, double cLabelMass, double hLabelMass, double nLabelMass,
            double oLabelMass, double pLabelMass, double sLabelMass, double fLabelMass, double clLabelMass, double brLabelMass,
            double iLabelMass, double siLabelMass)
        {
            string line;
            string[] lineArray;

            var formulaBeanList = new List<Formula>();
            Formula formula;

            int cnum, onum, nnum, snum, pnum;

            using (StreamReader sr = new StreamReader(dbFilePath, Encoding.ASCII))
            {
                sr.ReadLine();
                while (sr.Peek() > -1)
                {
                    line = sr.ReadLine();
                    if (line == string.Empty) break;
                    lineArray = line.Split(',');

                    cnum = int.Parse(lineArray[1]);
                    onum = int.Parse(lineArray[2]);
                    nnum = int.Parse(lineArray[3]);
                    snum = int.Parse(lineArray[4]);
                    pnum = int.Parse(lineArray[5]);

                    formula = new Formula(cnum, 0, nnum, onum, pnum, snum, 0, 0, 0, 0, 0, cLabelMass, hLabelMass, nLabelMass, oLabelMass, pLabelMass, sLabelMass, fLabelMass, clLabelMass, brLabelMass, iLabelMass, siLabelMass);

                    if (formula.Mass > maxMass) break;

                    if (dbFormulaCheck(formula, analysisParam))
                    {
                        formulaBeanList.Add(formula);
                    }
                }
            }

            return formulaBeanList;
        }

        private static bool dbFormulaCheck(Formula formula, AnalysisParamOfMsfinder analysisParam)
        {
            if (analysisParam.IsLewisAndSeniorCheck && !dbFormulaValenceCheck(formula, analysisParam.IsSiCheck)) return false;

            if (!heteroAtomCheck(formula, analysisParam.CoverRange)) return false;

            if (analysisParam.IsElementProbabilityCheck && !dbFormulaProbabilityCheck(formula)) return false;

            return true;
        }

        private static bool dbFormulaProbabilityCheck(Formula formula)
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

        private static bool heteroAtomCheck(Formula formula, CoverRange coverRange)
        {
            double cnum = formula.Cnum, nnum = formula.Nnum, onum = formula.Onum, pnum = formula.Pnum, snum = formula.Snum;
            double n_c = nnum / cnum, o_c = onum / cnum, p_c = pnum / cnum, s_c = snum / cnum;
            double o_p;
            if (pnum > 0) o_p = onum / pnum; else o_p = 4;

            switch (coverRange)
            {
                case CoverRange.CommonRange:
                    if (n_c <= 2.0 && o_c <= 2.5 && p_c <= 0.5 && s_c <= 1.0 && o_p >= 2) return true;
                    else return false;
                case CoverRange.ExtendedRange:
                    if (n_c <= 4.0 && o_c <= 6.0 && p_c <= 1.9 && s_c <= 3.0) return true;
                    else return false;
                default:
                    return true;
            }
        }

        private static bool dbFormulaValenceCheck(Formula formula, bool tmsCheck)
        {
            if (tmsCheck) return true;

            int atomTotal = formula.Cnum + formula.Nnum + formula.Onum + formula.Pnum + formula.Snum;
            int valenceNum = ValenceDictionary.ValenceDict["C"] * formula.Cnum + ValenceDictionary.ValenceDict["N"] * formula.Nnum + ValenceDictionary.ValenceDict["O"] * formula.Onum + ValenceDictionary.ValenceDict["P"] * formula.Pnum + ValenceDictionary.ValenceDict["S"] * formula.Snum;

            if (valenceNum - 2 * atomTotal + 2 < 0) return false;

            return true;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This class is the main program to find the molecular formula candidates from the mass spectra and to rank them.
    /// </summary>
    public class FormulaGenerator
    {
        private const double cMass = 12.00000000000;
        private const double hMass = 1.00782503207;
        private const double nMass = 14.00307400480;
        private const double oMass = 15.99491461956;
        private const double sMass = 31.97207100000;
        private const double pMass = 30.97376163000;
        private const double fMass = 18.99840322000;
        private const double siMass = 27.97692653250;
        private const double clMass = 34.96885268000;
        private const double brMass = 78.91833710000;
        private const double iMass = 126.90447300000;

        private int isCheckedNum;

        private double hMinFold;
        private double hMaxFold;
        private double fMaxFold;
        private double clMaxFold;
        private double brMaxFold;
        private double nMaxFold;
        private double oMaxFold;
        private double pMaxFold;
        private double sMaxFold;
        private double iMaxFold;
        private double siMaxFold;

        private double maxMassFoldChange;

        private bool oCheck;
        private bool nCheck;
        private bool pCheck;
        private bool sCheck;
        private bool fCheck;
        private bool brCheck;
        private bool clCheck;
        private bool iCheck;
        private bool siCheck;
        private bool valenceCheck;
        private bool probabilityCheck;
        private CoverRange coverRange;

        /// <summary>
        /// This is the constructor of this program.
        /// The parameters will be set by the user-defined paramerters.
        /// </summary>
        /// <param name="param"></param>
        public FormulaGenerator(AnalysisParamOfMsfinder param) 
        {
            this.oCheck = param.IsOcheck;
            this.nCheck = param.IsNcheck;
            this.pCheck = param.IsPcheck;
            this.sCheck = param.IsScheck;
            this.fCheck = param.IsFcheck;
            this.clCheck = param.IsClCheck;
            this.brCheck = param.IsBrCheck;
            this.iCheck = param.IsIcheck;

            this.siCheck = param.IsSiCheck;
            this.valenceCheck = param.IsLewisAndSeniorCheck;
            this.coverRange = param.CoverRange;
            this.probabilityCheck = param.IsElementProbabilityCheck;
            this.isCheckedNum = param.TryTopNmolecularFormulaSearch;

            maxFoldInitialize(this.coverRange, this.fCheck, this.clCheck, this.brCheck, this.iCheck, this.siCheck);
        }

        private void maxFoldInitialize(CoverRange coverRange, bool fCheck, bool clCheck, bool brCheck, bool iCheck, bool siCheck)
        {
            switch (coverRange)
            {
                case CoverRange.CommonRange:
                    this.hMinFold = 0; this.hMaxFold = 4.0; this.fMaxFold = 1.5; this.clMaxFold = 1.0; this.brMaxFold = 1.0; this.nMaxFold = 2.0; this.oMaxFold = 2.5; this.pMaxFold = 0.5; this.sMaxFold = 1.0; this.iMaxFold = 0.5; this.siMaxFold = 0.5;
                    break;
                case CoverRange.ExtendedRange:
                    this.hMinFold = 0; this.hMaxFold = 6.0; this.fMaxFold = 3.0; this.clMaxFold = 3.0; this.brMaxFold = 2.0; this.nMaxFold = 4.0; this.oMaxFold = 6.0; this.pMaxFold = 1.9; this.sMaxFold = 3.0; this.iMaxFold = 1.9; this.siMaxFold = 1.0;
                    break;
                default:
                    this.hMinFold = 0; this.hMaxFold = 8.0; this.fMaxFold = 4.0; this.clMaxFold = 4.0; this.brMaxFold = 4.0; this.nMaxFold = 4.0; this.oMaxFold = 10.0; this.pMaxFold = 3.0; this.sMaxFold = 6.0; this.iMaxFold = 3.0; this.siMaxFold = 3.0;
                    break;
            }

            this.maxMassFoldChange = this.hMaxFold * hMass;
            if (fCheck) this.maxMassFoldChange += this.fMaxFold * fMass;
            if (clCheck) this.maxMassFoldChange += this.clMaxFold * clMass;
            if (brCheck) this.maxMassFoldChange += this.brMaxFold * brMass;
            if (iCheck) this.maxMassFoldChange += this.iMaxFold * iMass;
            if (siCheck) this.maxMassFoldChange += this.siMaxFold * siMass;
        }


        /// <summary>
        /// This is the main method to find the formula candidates.
        /// MS-FINDER program now utilizes three internal databases including formulaDB, neutralLossDB, and existFormulaDB.
        /// </summary>
        /// <param name="formulaDB"></param>
        /// <param name="neutralLossDB"></param>
        /// <param name="existFormulaDB"></param>
        /// <param name="mass"></param>
        /// <param name="ms2Tol"></param>
        /// <param name="m1Intensity"></param>
        /// <param name="m2Intensity"></param>
        /// <param name="rawData"></param>
        /// <param name="adductIon"></param>
        /// <returns></returns>
        //public List<FormulaResult> GetFormulaCandidateList(List<Formula> formulaDB, List<NeutralLoss> neutralLossDB, List<ExistFormulaQuery> existFormulaDB, double mass, AnalysisParamOfMsfinder param, double ms1Tol, double ms2Tol, MassToleranceType massTolType, double m1Intensity, double m2Intensity, double isotopicAbundanceTolPercentage, double relativeAbundanceCutOff, int maxReportNumber, RawData rawData, AdductIon adductIon, bool isotopeCheck) {
        //public List<FormulaResult> GetFormulaCandidateList(List<Formula> formulaDB, List<ProductIon> productIonDB, List<NeutralLoss> neutralLossDB, List<ExistFormulaQuery> existFormulaDB, 
        public List<FormulaResult> GetFormulaCandidateList(List<ProductIon> productIonDB, List<NeutralLoss> neutralLossDB, List<ExistFormulaQuery> existFormulaDB, 
            AnalysisParamOfMsfinder param, double mass, double m1Intensity, double m2Intensity, RawData rawData, AdductIon adductIon, bool isotopeCheck) {
           
            //param set
            var ms1Tol = param.Mass1Tolerance;
            var ms2Tol = param.Mass2Tolerance;
            var massTolType = param.MassTolType;
            var relativeAbundanceCutOff = param.RelativeAbundanceCutOff;
            var maxReportNumber = param.FormulaMaximumReportNumber;

            if (massTolType == MassToleranceType.Ppm)
                ms1Tol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ms1Tol);

            var formulaResults = new List<FormulaResult>();
            var ms2Peaklist = FragmentAssigner.GetCentroidMsMsSpectrum(rawData);
            var refinedPeaklist = FragmentAssigner.GetRefinedPeaklist(ms2Peaklist, relativeAbundanceCutOff, (mass * (double)adductIon.AdductIonXmer + adductIon.AdductIonAccurateMass) / (double)adductIon.ChargeNumber, ms2Tol, massTolType, false, !param.CanExcuteMS2AdductSearch);
            var neutralLosslist = FragmentAssigner.GetNeutralLossList(refinedPeaklist, rawData.PrecursorMz, ms1Tol);

            var syncObj = new object();
            //var endID = getFormulaDbLastIndex(formulaDB, mass + ms1Tol);

            formulaResults = getFormulaResults(rawData, param, mass, ms1Tol, m1Intensity, m2Intensity, adductIon, isotopeCheck, maxReportNumber,
                existFormulaDB, refinedPeaklist, neutralLosslist, productIonDB, neutralLossDB);

            #region old
            //Parallel.For(0, endID, i => {

            //    var tempResults = getFormulaSearchResults(formulaDB[i], rawData,
            //        param, mass, ms1Tol,
            //        m1Intensity, m2Intensity, adductIon, isotopeCheck, maxReportNumber,
            //        existFormulaDB, refinedPeaklist, neutralLosslist, productIonDB, neutralLossDB);

            //    if (tempResults != null && tempResults.Count != 0) {
            //        lock (syncObj) {
            //            foreach (var result in tempResults) {
            //                formulaResults = getFormulaResultCandidates(formulaResults, result, 100000000);
            //            }
            //        }
            //    }
            //});
            ////for (int i = 0; i < formulaDB.Count; i++) {
            ////    if (formulaDB[i].Mass + formulaDB[i].Cnum * this.maxMassFoldChange < mass - ms1Tol) continue;
            ////    if (formulaDB[i].Onum > 0 && !this.oCheck) continue;
            ////    if (formulaDB[i].Nnum > 0 && !this.nCheck) continue;
            ////    if (formulaDB[i].Pnum > 0 && !this.pCheck) continue;
            ////    if (formulaDB[i].Snum > 0 && !this.sCheck) continue;

            ////    if (rawData.CarbonNumberFromLabeledExperiment >= 0 && formulaDB[i].Cnum != rawData.CarbonNumberFromLabeledExperiment)
            ////        continue;
            ////    if (rawData.NitrogenNumberFromLabeledExperiment >= 0 && formulaDB[i].Nnum != rawData.NitrogenNumberFromLabeledExperiment)
            ////        continue;
            ////    if (rawData.OxygenNumberFromLabeledExperiment >= 0 && formulaDB[i].Onum != rawData.OxygenNumberFromLabeledExperiment)
            ////        continue;
            ////    if (rawData.SulfurNumberFromLabeledExperiment >= 0 && formulaDB[i].Snum != rawData.SulfurNumberFromLabeledExperiment)
            ////        continue;
            ////    if (rawData.CarbonNitrogenNumberFromLabeledExperiment >= 0 && formulaDB[i].Cnum + formulaDB[i].Nnum != rawData.CarbonNitrogenNumberFromLabeledExperiment)
            ////        continue;
            ////    if (rawData.CarbonSulfurNumberFromLabeledExperiment >= 0 && formulaDB[i].Cnum + formulaDB[i].Snum != rawData.CarbonSulfurNumberFromLabeledExperiment)
            ////        continue;
            ////    if (rawData.NitrogenSulfurNumberFromLabeledExperiment >= 0 && formulaDB[i].Nnum + formulaDB[i].Snum != rawData.NitrogenSulfurNumberFromLabeledExperiment)
            ////        continue;
            ////    if (rawData.CarbonNitrogenSulfurNumberFromLabeledExperiment >= 0 && formulaDB[i].Cnum + formulaDB[i].Nnum + formulaDB[i].Snum != rawData.CarbonNumberFromLabeledExperiment)
            ////        continue;

            ////    if (formulaDB[i].Mass > mass + ms1Tol) break;

            ////    formulaResults = getFormulaSearchResults(formulaResults, formulaDB[i], param, mass, ms1Tol,
            ////        m1Intensity, m2Intensity, adductIon, isotopeCheck, maxReportNumber, 
            ////        existFormulaDB, refinedPeaklist, neutralLosslist, productIonDB, neutralLossDB);
            ////}
#endregion
            if (formulaResults.Count > 0) {
                formulaResults = formulaResults.OrderByDescending(n => Math.Abs(n.TotalScore)).ToList();

                for (int i = 0; i < isCheckedNum; i++) { if (i > formulaResults.Count - 1) break; formulaResults[i].IsSelected = true; }

                while (formulaResults.Count > maxReportNumber) { formulaResults.RemoveAt(formulaResults.Count - 1); }
            }

            return formulaResults;
        }

        private List<FormulaResult> getFormulaResults(RawData rawData, AnalysisParamOfMsfinder param,
            double mass, double ms1Tol, double m1Intensity, double m2Intensity, 
            AdductIon adduct, bool isotopeCheck, int maxReportNumber,
            List<ExistFormulaQuery> existFormulaDB, List<Peak> refinedPeaklist, 
            List<NeutralLoss> neutralLosses, List<ProductIon> productIonDB, List<NeutralLoss> neutralLossDB) {

            var maxCnum = (int)(mass / 12.0);
            var formulaResultsMaster = new List<FormulaResult>();

            var syncObj = new object();
            var sw = new Stopwatch();
            sw.Start();

            Parallel.For(1, maxCnum, (c, state) => {
                var formulaResults = getFormulaResults(c, rawData, param, mass, ms1Tol, m1Intensity, m2Intensity, adduct, isotopeCheck, 
                    maxReportNumber, existFormulaDB, refinedPeaklist, neutralLosses, productIonDB, neutralLossDB);

                if (formulaResults != null && formulaResults.Count != 0) {
                    lock (syncObj) {
                        var lap = sw.ElapsedMilliseconds * 0.001 / 60.0; // min
                        if (param.FormulaPredictionTimeOut > 0 && param.FormulaPredictionTimeOut < lap) {
                            Debug.WriteLine("Calculation stopped.");
                            state.Stop();
                        }

                        foreach (var result in formulaResults) {
                            formulaResultsMaster = getFormulaResultCandidates(formulaResultsMaster, result, 100000000);
                        }
                    }
                }
            });
            sw.Stop();
            return formulaResultsMaster;
        }

        private List<FormulaResult> getFormulaResults(int c, RawData rawData, AnalysisParamOfMsfinder param, double mass, double ms1Tol, double m1Intensity, double m2Intensity, 
            AdductIon adduct, bool isotopeCheck, int maxReportNumber, 
            List<ExistFormulaQuery> existFormulaDB, List<Peak> refinedPeaklist, List<NeutralLoss> neutralLosses, 
            List<ProductIon> productIonDB, List<NeutralLoss> neutralLossDB) {

            var formulaResults = new List<FormulaResult>();
            var xMer = adduct.AdductIonXmer;
            if (rawData.CarbonNumberFromLabeledExperiment >= 0 && c != rawData.CarbonNumberFromLabeledExperiment / xMer)
                return formulaResults;

            var maxHnum = (int)Math.Ceiling(c * this.hMaxFold); // 1.00782503207
            var maxNnum = this.nCheck ? (int)Math.Ceiling(c * this.nMaxFold) : 0; // 14.00307400480
            var maxOnum = this.oCheck ? (int)Math.Ceiling(c * this.oMaxFold) : 0; // 15.99491461956
            var maxPnum = this.pCheck ? (int)Math.Ceiling(c * this.pMaxFold) : 0; // 30.97376163000
            var maxSnum = this.sCheck ? (int)Math.Ceiling(c * this.sMaxFold) : 0; // 31.97207100000

            var maxFnum = this.fCheck ? (int)Math.Ceiling(c * this.fMaxFold) : 0; // 18.99840322000;
            var maxSinum = this.siCheck ? (int)Math.Ceiling(c * this.siMaxFold) : 0; // 27.97692653250;
            var maxClnum = this.clCheck ? (int)Math.Ceiling(c * this.clMaxFold) : 0; // 34.96885268000;
            var maxBrnum = this.brCheck ? (int)Math.Ceiling(c * this.brMaxFold) : 0; // 78.91833710000;
            var maxInum = this.iCheck ? (int)Math.Ceiling(c * this.iMaxFold) : 0; // 126.90447300000;


            //for lool is used by mass weight order: I > Br > Cl > S > P > Si > F > O > N > H
            var maxHmass = maxHnum * hMass;
            var maxNHmass = maxHmass + maxNnum * nMass;
            var maxONHmass = maxNHmass + maxOnum * oMass;
            var maxFONHmass = maxONHmass + maxFnum * fMass;
            var maxSiFONHmass = maxFONHmass + maxSinum * siMass;
            var maxPSiFONHmass = maxSiFONHmass + maxPnum * pMass;
            var maxSPSiFONHmass = maxPSiFONHmass + maxSnum * sMass;
            var maxClSPSiFONHmass = maxSPSiFONHmass + maxClnum * clMass;
            var maxBrClSPSiFONHmass = maxClSPSiFONHmass + maxBrnum * brMass;

            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i <= maxInum; i++) {
                var ciMass = (double)c * cMass + (double)i * iMass;
                if (ciMass + maxBrClSPSiFONHmass < mass - ms1Tol) continue;
                if (ciMass > mass + ms1Tol) break;

                for (int br = 0; br <= maxBrnum; br++) {
                    var cibrMass = ciMass + (double)br * brMass;
                    if (cibrMass + maxClSPSiFONHmass < mass - ms1Tol) continue;
                    if (cibrMass > mass + ms1Tol) break;

                    for (int cl = 0; cl <= maxClnum; cl++) {
                        var cibrclMass = cibrMass + (double)cl * clMass;
                        if (cibrclMass + maxSPSiFONHmass < mass - ms1Tol) continue;
                        if (cibrclMass > mass + ms1Tol) break;

                        for (int s = 0; s <= maxSnum; s++) {

                            if (rawData.SulfurNumberFromLabeledExperiment >= 0 && s != rawData.SulfurNumberFromLabeledExperiment / xMer) continue;

                            var cibrclsMass = cibrclMass + (double)s * sMass;
                            if (cibrclsMass + maxPSiFONHmass < mass - ms1Tol) continue;
                            if (cibrclsMass > mass + ms1Tol) break;

                            for (int p = 0; p <= maxPnum; p++) {
                                var cibrclspMass = cibrclsMass + (double)p * pMass;
                                if (cibrclspMass + maxSiFONHmass < mass - ms1Tol) continue;
                                if (cibrclspMass > mass + ms1Tol) break;

                                for (int si = 0; si <= maxSinum; si++) {
                                    if (param.IsTmsMeoxDerivative && si < param.MinimumTmsCount) continue;

                                    var cibrclspsiMass = cibrclspMass + (double)si * siMass;
                                    if (cibrclspsiMass + maxFONHmass < mass - ms1Tol) continue;
                                    if (cibrclspsiMass > mass + ms1Tol) break;

                                    for (int f = 0; f <= maxFnum; f++) {
                                        var cibrclspsifMass = cibrclspsiMass + (double)f * fMass;
                                        if (cibrclspsifMass + maxONHmass < mass - ms1Tol) continue;
                                        if (cibrclspsifMass > mass + ms1Tol) break;

                                        for (int o = 0; o <= maxOnum; o++) {

                                            if (rawData.OxygenNumberFromLabeledExperiment >= 0 && o != rawData.OxygenNumberFromLabeledExperiment / xMer) continue;

                                            var cibrclspsifoMass = cibrclspsifMass + (double)o * oMass;
                                            if (cibrclspsifoMass + maxNHmass < mass - ms1Tol) continue;
                                            if (cibrclspsifoMass > mass + ms1Tol) break;

                                            for (int n = 0; n <= maxNnum; n++) {

                                                if (rawData.NitrogenNumberFromLabeledExperiment >= 0 && n != rawData.NitrogenNumberFromLabeledExperiment / xMer) continue;

                                                var cibrclspsifonMass = cibrclspsifoMass + (double)n * nMass;
                                                if (cibrclspsifonMass + maxHmass < mass - ms1Tol) continue;
                                                if (cibrclspsifonMass > mass + ms1Tol) break;

                                                for (int h = 0; h <= maxHnum; h++) {
                                                    var cibrclspsifonhMass = cibrclspsifonMass + (double)h * hMass;
                                                    if (cibrclspsifonhMass < mass - ms1Tol) continue;
                                                    if (cibrclspsifonhMass > mass + ms1Tol) break;

                                                    if (param.IsTmsMeoxDerivative) {
                                                        for (int meox = param.MinimumMeoxCount; meox <= n; meox++) {
                                                            var formula = new Formula(c, h, n, o, p, s, f, cl, br, i, si, si, meox);
                                                            if (formula.Cnum - si * 3 - meox <= 0) continue;

                                                            var convertedFormula = MolecularFormulaUtility.ConvertTmsMeoxSubtractedFormula(formula);
                                                            if (!SevenGoldenRulesCheck.Check(convertedFormula, this.valenceCheck, this.coverRange, this.probabilityCheck, adduct)) continue;
                                                            var formulaResult = tryGetFormulaResultCandidate(formula, param,
                                                                                mass, ms1Tol, m1Intensity, m2Intensity, isotopeCheck, adduct,
                                                                                refinedPeaklist, neutralLosses, existFormulaDB, productIonDB, neutralLossDB);
                                                            if (formulaResult != null) formulaResults.Add(formulaResult);
                                                        }
                                                    }
                                                    else {
                                                        var formula = new Formula(c, h, n, o, p, s, f, cl, br, i, si);
                                                        var formulaResult = tryGetFormulaResultCandidate(formula, param,
                                                                               mass, ms1Tol, m1Intensity, m2Intensity, isotopeCheck, adduct,
                                                                               refinedPeaklist, neutralLosses, existFormulaDB, productIonDB, neutralLossDB);
                                                        if (formulaResult != null) formulaResults.Add(formulaResult);
                                                    }

                                                    var lap = sw.ElapsedMilliseconds * 0.001 / 60.0; // min
                                                    if (param.FormulaPredictionTimeOut > 0 && param.FormulaPredictionTimeOut < lap) {
                                                        Debug.WriteLine("Calculation stopped.");
                                                        sw.Stop();
                                                        return formulaResults;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            sw.Stop();
            return formulaResults;
        }

        private List<FormulaResult> getFormulaSearchResults(Formula formula, RawData rawData, AnalysisParamOfMsfinder param, 
            double mass, double ms1Tol, double m1Intensity, double m2Intensity, AdductIon adductIon, 
            bool isotopeCheck, int maxReportNumber, List<ExistFormulaQuery> existFormulaDB,
            List<Peak> refinedPeaklist, List<NeutralLoss> neutralLosslist, List<ProductIon> productIonDB,
            List<NeutralLoss> neutralLossDB) {

            if (formula.Mass + formula.Cnum * this.maxMassFoldChange < mass - ms1Tol) return null;
            if (formula.Onum > 0 && !this.oCheck) return null;
            if (formula.Nnum > 0 && !this.nCheck) return null;
            if (formula.Pnum > 0 && !this.pCheck) return null;
            if (formula.Snum > 0 && !this.sCheck) return null;

            var xMer = adductIon.AdductIonXmer;
            if (rawData.CarbonNumberFromLabeledExperiment >= 0 && formula.Cnum != rawData.CarbonNumberFromLabeledExperiment / xMer)
                return null;
            if (rawData.NitrogenNumberFromLabeledExperiment >= 0 && formula.Nnum != rawData.NitrogenNumberFromLabeledExperiment / xMer)
                return null;
            if (rawData.OxygenNumberFromLabeledExperiment >= 0 && formula.Onum != rawData.OxygenNumberFromLabeledExperiment / xMer)
                return null;
            if (rawData.SulfurNumberFromLabeledExperiment >= 0 && formula.Snum != rawData.SulfurNumberFromLabeledExperiment / xMer)
                return null;
            if (rawData.CarbonNitrogenNumberFromLabeledExperiment >= 0 && formula.Cnum + formula.Nnum != rawData.CarbonNitrogenNumberFromLabeledExperiment / xMer)
                return null;
            if (rawData.CarbonSulfurNumberFromLabeledExperiment >= 0 && formula.Cnum + formula.Snum != rawData.CarbonSulfurNumberFromLabeledExperiment / xMer)
                return null;
            if (rawData.NitrogenSulfurNumberFromLabeledExperiment >= 0 && formula.Nnum + formula.Snum != rawData.NitrogenSulfurNumberFromLabeledExperiment / xMer)
                return null;
            if (rawData.CarbonNitrogenSulfurNumberFromLabeledExperiment >= 0 && formula.Cnum + formula.Nnum + formula.Snum != rawData.CarbonNumberFromLabeledExperiment / xMer)
                return null;

            ////test for ms-dial and ms-finder integration project
            //if (FormulaStringParcer.OrganicElementsReader(formula.FormulaString).Cnum != formula.Cnum) return null;


            var tempResults = getFormulaSearchResults(formula, param, mass, ms1Tol,
                m1Intensity, m2Intensity, adductIon, isotopeCheck, maxReportNumber,
                existFormulaDB, refinedPeaklist, neutralLosslist, productIonDB, neutralLossDB);
            return tempResults;
        }

        //private int getFormulaDbLastIndex(List<Formula> formulaDB, double targetMass) {
        //    int startIndex = 0, endIndex = formulaDB.Count - 1;

        //    int counter = 0;
        //    while (counter < 10) {
        //        if (formulaDB[startIndex].Mass <= targetMass && 
        //            targetMass < formulaDB[(startIndex + endIndex) / 2].Mass) {
        //            endIndex = (startIndex + endIndex) / 2;
        //        }
        //        else if (formulaDB[(startIndex + endIndex) / 2].Mass <= targetMass && 
        //            targetMass < formulaDB[endIndex].Mass) {
        //            startIndex = (startIndex + endIndex) / 2;
        //        }
        //        counter++;
        //    }

        //    return endIndex;
        //}

        /// <summary>
        /// This is the main method to find the formula candidates.
        /// MS-FINDER program now utilizes three internal databases including formulaDB, neutralLossDB, and existFormulaDB.
        /// </summary>
        /// <param name="formulaDB"></param>
        /// <param name="existFormulaDB"></param>
        /// <param name="mass"></param>
        /// <param name="m1Intensity"></param>
        /// <param name="m2Intensity"></param>
        /// <param name="adductIon"></param>
        /// <param name="isotopeCheck"></param>
        /// <returns></returns>
        //public List<FormulaResult> GetFormulaCandidateList(List<Formula> formulaDB, List<ExistFormulaQuery> existFormulaDB, AnalysisParamOfMsfinder param, double mass, double ms1Tol, double ms2Tol, MassToleranceType massTolType, double m1Intensity, double m2Intensity, double isotopeRatioTolAsPercentage, int maxReportNumber, AdductIon adductIon, bool isotopeCheck) {
        //public List<FormulaResult> GetFormulaCandidateList(RawData rawData, List<Formula> formulaDB, List<ExistFormulaQuery> existFormulaDB,
        public List<FormulaResult> GetFormulaCandidateList(RawData rawData, List<ExistFormulaQuery> existFormulaDB,
            AnalysisParamOfMsfinder param, double mass, double m1Intensity, double m2Intensity, AdductIon adductIon, bool isotopeCheck) {

            //param set
            var ms1Tol = param.Mass1Tolerance;
            var massTolType = param.MassTolType;
            var maxReportNumber = param.FormulaMaximumReportNumber;
            
            var formulaResults = new List<FormulaResult>();
            if (massTolType == MassToleranceType.Ppm) ms1Tol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ms1Tol);

            formulaResults = getFormulaResults(rawData, param, mass, ms1Tol, m1Intensity, m2Intensity, adductIon, isotopeCheck, maxReportNumber,
                existFormulaDB, null, null, null, null);


            //for (int i = 0; i < formulaDB.Count; i++) {
            //    if (formulaDB[i].Mass + formulaDB[i].Cnum * this.maxMassFoldChange < mass - ms1Tol) continue;
            //    if (formulaDB[i].Onum > 0 && !this.oCheck) continue;
            //    if (formulaDB[i].Nnum > 0 && !this.nCheck) continue;
            //    if (formulaDB[i].Pnum > 0 && !this.pCheck) continue;
            //    if (formulaDB[i].Snum > 0 && !this.sCheck) continue;

            //    if (formulaDB[i].Mass > mass + ms1Tol) break;

            //    //formulaResults = getFormulaSearchResults(formulaResults, formulaDB[i], param, mass, ms1Tol, m1Intensity, m2Intensity, adductIon, isotopeCheck, maxReportNumber, existFormulaDB, null, null, null, null);
            //    var tempResults = getFormulaSearchResults(formulaDB[i], param, mass, ms1Tol, m1Intensity, m2Intensity, adductIon, isotopeCheck, maxReportNumber, existFormulaDB, null, null, null, null);
            //    if (tempResults == null || tempResults.Count == 0) continue;
            //    foreach (var result in tempResults) {
            //        formulaResults.Add(result);
            //    }
            //}

            if (formulaResults.Count > 0) {
                formulaResults = formulaResults.OrderByDescending(n => Math.Abs(n.TotalScore)).ToList();

                for (int i = 0; i < isCheckedNum; i++) { if (i > formulaResults.Count - 1) break; formulaResults[i].IsSelected = true; }

                while (formulaResults.Count > maxReportNumber) { formulaResults.RemoveAt(formulaResults.Count - 1); }
            }

            return formulaResults;
        }

        //public FormulaResult GetFormulaScore(string formulaString, List<NeutralLoss> neutralLossDB, List<ExistFormulaQuery> existFormulaDB, double mass, AnalysisParamOfMsfinder param, double ms1Tol, double ms2Tol, MassToleranceType massTolType, double relativeAbundanceCutOff, double subtractedM1Intensity, double subtractedM2Intensity, double isotopeRatioTolAsPercentage, RawData rawData, AdductIon adductIon, bool isotopeCheck) {
        public FormulaResult GetFormulaScore(string formulaString, List<ProductIon> productIonDB, List<NeutralLoss> neutralLossDB, List<ExistFormulaQuery> existFormulaDB, 
            AnalysisParamOfMsfinder param, double mass, double subtractedM1Intensity, double subtractedM2Intensity, RawData rawData, AdductIon adductIon, bool isotopeCheck) {

            //param set
            var ms1Tol = param.Mass1Tolerance;
            var ms2Tol = param.Mass2Tolerance;
            var massTolType = param.MassTolType;
            var isotopicAbundanceTolerance = param.IsotopicAbundanceTolerance;
            var relativeAbundanceCutOff = param.RelativeAbundanceCutOff;
            
            var formula = FormulaStringParcer.OrganicElementsReader(formulaString);
            if (massTolType == MassToleranceType.Ppm) ms1Tol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ms1Tol);
            var formulaResult = getFormulaResult(formula, param, mass, ms1Tol, subtractedM1Intensity, subtractedM2Intensity, isotopeCheck, existFormulaDB);

            var ms2Peaklist = FragmentAssigner.GetCentroidMsMsSpectrum(rawData);

            if (ms2Peaklist != null && ms2Peaklist.Count != 0) {
                var refinedPeaklist = FragmentAssigner.GetRefinedPeaklist(ms2Peaklist, relativeAbundanceCutOff, (mass * (double)adductIon.AdductIonXmer + adductIon.AdductIonAccurateMass) / (double)adductIon.ChargeNumber, ms2Tol, massTolType, false, !param.CanExcuteMS2AdductSearch);
                var neutralLosslist = FragmentAssigner.GetNeutralLossList(refinedPeaklist, rawData.PrecursorMz, ms1Tol);

                if (param.CanExcuteMS2AdductSearch) {
                    if (adductIon.IonMode == IonMode.Positive) {
                        formulaResult.AnnotatedIonResult = FragmentAssigner.GetAnnotatedIon(refinedPeaklist, adductIon, param.MS2PositiveAdductIonList, rawData.PrecursorMz, param.Mass2Tolerance, param.MassTolType);
                    }
                    else {
                        formulaResult.AnnotatedIonResult = FragmentAssigner.GetAnnotatedIon(refinedPeaklist, adductIon, param.MS2NegativeAdductIonList, rawData.PrecursorMz, param.Mass2Tolerance, param.MassTolType);
                    }
                    refinedPeaklist = FragmentAssigner.GetRefinedPeaklist(refinedPeaklist, rawData.PrecursorMz);
                }
                setFragmentProperties(formulaResult, refinedPeaklist, neutralLosslist, productIonDB, neutralLossDB, ms2Tol, massTolType, adductIon);
            }

            formulaResult.TotalScore = Math.Round(Scoring.TotalScore(formulaResult), 3);
            formulaResult.IsSelected = true;

            return formulaResult;
        }

        private void setExistFormulaDbInfo(FormulaResult formulaResult, List<ExistFormulaQuery> existFormulaDB)
        {
            string resourceNames;
            int resourceRecords;
            List<int> pubchemCids;

            tryExistFormulaDbSearch(formulaResult.Formula, existFormulaDB, out resourceNames, out resourceRecords, out pubchemCids);
            formulaResult.ResourceNames = resourceNames;
            formulaResult.ResourceRecords = resourceRecords;
            formulaResult.PubchemResources = pubchemCids;
        }

        private void tryExistFormulaDbSearch(Formula formula, List<ExistFormulaQuery> queryDB, out string resourceNames, out int resourceRecords, out List<int> pubchemCIDs)
        {
            pubchemCIDs = new List<int>();
            resourceNames = string.Empty;
            resourceRecords = 0;

            var cFormula = MolecularFormulaUtility.ConvertTmsMeoxSubtractedFormula(formula);
            var mass = cFormula.Mass;
            var tol = 0.00005;
            var startID = getQueryStartIndex(mass, tol, queryDB);

            for (int i = startID; i < queryDB.Count; i++)
            {
                if (queryDB[i].Formula.Mass < mass - tol) continue;
                if (queryDB[i].Formula.Mass > mass + tol) break;

                var qFormula = queryDB[i].Formula;

                if (MolecularFormulaUtility.isFormulaMatch(cFormula, qFormula))
                {
                    resourceNames = queryDB[i].ResourceNames;
                    resourceRecords = queryDB[i].ResourceNumber;
                    pubchemCIDs = queryDB[i].PubchemCidList;
                    break;
                }
            }
        }

        private int getQueryStartIndex(double mass, double tol, List<ExistFormulaQuery> queryDB)
        {
            if (queryDB == null || queryDB.Count == 0) return 0;
            double targetMass = mass - tol;
            int startIndex = 0, endIndex = queryDB.Count - 1;
            int counter = 0;
            
            while (counter < 10)
            {
                if (queryDB[startIndex].Formula.Mass <= targetMass && targetMass < queryDB[(startIndex + endIndex) / 2].Formula.Mass)
                {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (queryDB[(startIndex + endIndex) / 2].Formula.Mass <= targetMass && targetMass < queryDB[endIndex].Formula.Mass)
                {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        private void setFragmentProperties(FormulaResult formulaResult, List<Peak> refinedPeaklist, List<NeutralLoss> neutralLosslist,
            List<ProductIon> productIonDB, List<NeutralLoss> neutralLossDB, 
           double ms2Tol, MassToleranceType massTolType, AdductIon adductIon)
        {
            if (refinedPeaklist == null || neutralLosslist == null) return;

            formulaResult.ProductIonResult = FragmentAssigner.FastFragmnetAssigner(refinedPeaklist, productIonDB, formulaResult.Formula, ms2Tol, massTolType, adductIon);
            formulaResult.NeutralLossResult = FragmentAssigner.FastNeutralLossAssigner(neutralLosslist, neutralLossDB, formulaResult.Formula, ms2Tol, massTolType, adductIon);

            formulaResult.ProductIonNum = formulaResult.ProductIonResult.Count;
            formulaResult.ProductIonHits = formulaResult.ProductIonResult.Count(n => n.CandidateOntologies.Count > 0);

            formulaResult.NeutralLossHits = getUniqueNeutralLossCount(formulaResult.NeutralLossResult);
            formulaResult.NeutralLossNum = getUniqueNeutralLossCountByMass(neutralLosslist, ms2Tol, massTolType);

            formulaResult.ProductIonScore = Math.Round(Scoring.FragmentHitsScore(refinedPeaklist, formulaResult.ProductIonResult, ms2Tol, massTolType), 3);
            formulaResult.NeutralLossScore = Math.Round(Scoring.NeutralLossScore(formulaResult.NeutralLossHits, formulaResult.NeutralLossNum), 3);
        }


        private int getUniqueNeutralLossCount(List<NeutralLoss> neutralLosses) {

            if (neutralLosses.Count == 0) return 0;

            var formulas = new List<Formula>() { neutralLosses[0].Formula };
            if (neutralLosses.Count == 1) return 1;

            for (int i = 1; i < neutralLosses.Count; i++) {
                var lossFormula = neutralLosses[i].Formula;

                var flg = false;
                foreach (var formula in formulas) {
                    if (MolecularFormulaUtility.isFormulaMatch(formula, lossFormula, true)) {
                        flg = true;
                        break;
                    }
                }
                if (!flg) {
                    formulas.Add(lossFormula);
                }
            }

            return formulas.Count;
        }

        private int getUniqueNeutralLossCountByMass(List<NeutralLoss> neutralLosses, double ms2Tol, MassToleranceType massTolType) {

            if (neutralLosses.Count == 0) return 0;

            var masses = new List<double>() { neutralLosses[0].MassLoss };
            if (neutralLosses.Count == 1) return 1;

            for (int i = 1; i < neutralLosses.Count; i++) {
                var lossMass = neutralLosses[i].MassLoss;
                var massTol = ms2Tol;

                if (massTolType == MassToleranceType.Ppm)
                    massTol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(neutralLosses[i].PrecursorMz, ms2Tol);
                var flg = false;
                foreach (var mass in masses) {
                    if (Math.Abs(mass - lossMass) < massTol) {
                        flg = true;
                        break;
                    }
                }
                if (!flg) {
                    masses.Add(lossMass);
                }
            }
            return masses.Count;
        }

        //private List<FormulaResult> getFormulaSearchResults(List<FormulaResult> formulaCandidate, Formula formulaBean, AnalysisParamOfMsfinder param, 
        //    double mass, double ms1Tol, double m1Intensity, double m2Intensity, AdductIon adduct, bool isotopeCheck, int maxFormulaNum, 
        //    List<ExistFormulaQuery> existFormulaDB, List<Peak> refinedPeaklist, List<NeutralLoss> neutralLosses, List<ProductIon> productIonDB, List<NeutralLoss> neutralLossDB){

        //    if (param.IsTmsMeoxDerivative && formulaBean.Nnum < param.MinimumMeoxCount) return formulaCandidate;

        //    int minHnum = (int)(this.hMinFold * formulaBean.Cnum);
        //    int maxHnum = (int)(this.hMaxFold * formulaBean.Cnum);

        //    int maxFnum = (int)(this.fMaxFold * formulaBean.Cnum);
        //    int maxClnum = (int)(this.clMaxFold * formulaBean.Cnum);
        //    int maxBrnum = (int)(this.brMaxFold * formulaBean.Cnum);
        //    int maxInum = (int)(this.iMaxFold * formulaBean.Cnum);
        //    int maxSinum = (int)(this.siMaxFold * formulaBean.Cnum);

        //    if (!this.fCheck) maxFnum = 0;
        //    if (!this.clCheck) maxClnum = 0;
        //    if (!this.brCheck) maxBrnum = 0;
        //    if (!this.iCheck) maxInum = 0;
        //    if (!this.siCheck) maxSinum = 0;

        //    double maxBrClSiFHmass = maxBrnum * brMass + maxClnum * clMass + maxSinum * siMass + maxFnum * fMass + maxHnum * hMass;
        //    double maxClSiFHmass = maxClnum * clMass + maxSinum * siMass + maxFnum * fMass + maxHnum * hMass;
        //    double maxSiFHmass = maxSinum * siMass + maxFnum * fMass + maxHnum * hMass;
        //    double maxFHmass = maxFnum * fMass + maxHnum * hMass;
        //    double maxHmass = maxHnum * hMass;

        //    var formulaResult = new FormulaResult();

        //    #region
        //    for (int h = 0; h <= maxInum; h++) {
        //        if (formulaBean.Mass + (double)h * iMass + maxBrClSiFHmass < mass - ms1Tol) continue;
        //        if (formulaBean.Mass + (double)h * iMass > mass + ms1Tol) break;

        //        for (int i = 0; i <= maxBrnum; i++) {
        //            if (formulaBean.Mass + (double)h * iMass + (double)i * brMass + maxClSiFHmass < mass - ms1Tol) continue;
        //            if (formulaBean.Mass + (double)h * iMass + (double)i * brMass > mass + ms1Tol) break;

        //            for (int j = 0; j <= maxClnum; j++) {
        //                if (formulaBean.Mass + (double)h * iMass + (double)i * brMass + (double)j * clMass + maxSiFHmass < mass - ms1Tol) continue;
        //                if (formulaBean.Mass + (double)h * iMass + (double)i * brMass + (double)j * clMass > mass + ms1Tol) break;

        //                for (int k = 0; k <= maxSinum; k++) {
        //                    if (param.IsTmsMeoxDerivative && k < param.MinimumTmsCount) continue;
        //                    if (formulaBean.Mass + (double)h * iMass + (double)i * brMass + (double)j * clMass + (double)k * siMass + maxFHmass < mass - ms1Tol) continue;
        //                    if (formulaBean.Mass + (double)h * iMass + (double)i * brMass + (double)j * clMass + (double)k * siMass > mass + ms1Tol) break;

        //                    for (int l = 0; l <= maxFnum; l++) {
        //                        if (formulaBean.Mass + (double)h * iMass + (double)i * brMass + (double)j * clMass + (double)k * siMass + (double)l * fMass + maxHmass < mass - ms1Tol) continue;
        //                        if (formulaBean.Mass + (double)h * iMass + (double)i * brMass + (double)j * clMass + (double)k * siMass + (double)l * fMass > mass + ms1Tol) break;

        //                        for (int m = minHnum; m <= maxHnum; m++) {
        //                            if (formulaBean.Mass + (double)h * iMass + (double)i * brMass + (double)j * clMass + (double)k * siMass + (double)l * fMass + (double)m * hMass < mass - ms1Tol) continue;
        //                            if (formulaBean.Mass + (double)h * iMass + (double)i * brMass + (double)j * clMass + (double)k * siMass + (double)l * fMass + (double)m * hMass > mass + ms1Tol) break;

        //                            if (param.IsTmsMeoxDerivative) {

        //                                var tmsCount = k;
        //                                for (int n = param.MinimumMeoxCount; n <= formulaBean.Nnum; n++) {
        //                                    var meoxCount = n;
        //                                    var formula = getCandidateFormulaBean(formulaBean, m, l, j, i, h, k, tmsCount, meoxCount);

        //                                    if (formula.Cnum - tmsCount * 3 - meoxCount <= 0) continue;
        //                                    var convertedFormula = MolecularFormulaUtility.ConvertTmsMeoxSubtractedFormula(formula);
        //                                    if (!SevenGoldenRulesCheck.Check(convertedFormula, this.valenceCheck, this.coverRange, this.probabilityCheck, adduct)) continue;

        //                                    formulaCandidate = tryGetFormulaResultCandidate(formulaCandidate, formula, param,
        //                                        mass, ms1Tol, m1Intensity, m2Intensity, isotopeCheck, adduct,
        //                                        refinedPeaklist, neutralLosses, existFormulaDB, productIonDB, neutralLossDB);
        //                                }
        //                            }
        //                            else {

        //                                var formula = getCandidateFormulaBean(formulaBean, m, l, j, i, h, k);

        //                                formulaCandidate = tryGetFormulaResultCandidate(formulaCandidate, formula, param,
        //                                        mass, ms1Tol, m1Intensity, m2Intensity, isotopeCheck, adduct,
        //                                        refinedPeaklist, neutralLosses, existFormulaDB, productIonDB, neutralLossDB);
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    #endregion
        //    return formulaCandidate;
        //}

        private List<FormulaResult> getFormulaSearchResults(Formula formulaBean, AnalysisParamOfMsfinder param,
        double mass, double ms1Tol, double m1Intensity, double m2Intensity, AdductIon adduct, bool isotopeCheck, int maxFormulaNum,
        List<ExistFormulaQuery> existFormulaDB, List<Peak> refinedPeaklist, List<NeutralLoss> neutralLosses, List<ProductIon> productIonDB, List<NeutralLoss> neutralLossDB) {

            if (param.IsTmsMeoxDerivative && formulaBean.Nnum < param.MinimumMeoxCount) return new List<FormulaResult>();

            int minHnum = (int)(this.hMinFold * formulaBean.Cnum);
            int maxHnum = (int)(this.hMaxFold * formulaBean.Cnum);

            int maxFnum = (int)(this.fMaxFold * formulaBean.Cnum);
            int maxClnum = (int)(this.clMaxFold * formulaBean.Cnum);
            int maxBrnum = (int)(this.brMaxFold * formulaBean.Cnum);
            int maxInum = (int)(this.iMaxFold * formulaBean.Cnum);
            int maxSinum = (int)(this.siMaxFold * formulaBean.Cnum);

            if (!this.fCheck) maxFnum = 0;
            if (!this.clCheck) maxClnum = 0;
            if (!this.brCheck) maxBrnum = 0;
            if (!this.iCheck) maxInum = 0;
            if (!this.siCheck) maxSinum = 0;

            double maxBrClSiFHmass = maxBrnum * brMass + maxClnum * clMass + maxSinum * siMass + maxFnum * fMass + maxHnum * hMass;
            double maxClSiFHmass = maxClnum * clMass + maxSinum * siMass + maxFnum * fMass + maxHnum * hMass;
            double maxSiFHmass = maxSinum * siMass + maxFnum * fMass + maxHnum * hMass;
            double maxFHmass = maxFnum * fMass + maxHnum * hMass;
            double maxHmass = maxHnum * hMass;

            var formulaCandidates = new List<FormulaResult>();

            #region
            for (int h = 0; h <= maxInum; h++) {
                if (formulaBean.Mass + (double)h * iMass + maxBrClSiFHmass < mass - ms1Tol) continue;
                if (formulaBean.Mass + (double)h * iMass > mass + ms1Tol) break;

                for (int i = 0; i <= maxBrnum; i++) {
                    if (formulaBean.Mass + (double)h * iMass + (double)i * brMass + maxClSiFHmass < mass - ms1Tol) continue;
                    if (formulaBean.Mass + (double)h * iMass + (double)i * brMass > mass + ms1Tol) break;

                    for (int j = 0; j <= maxClnum; j++) {
                        if (formulaBean.Mass + (double)h * iMass + (double)i * brMass + (double)j * clMass + maxSiFHmass < mass - ms1Tol) continue;
                        if (formulaBean.Mass + (double)h * iMass + (double)i * brMass + (double)j * clMass > mass + ms1Tol) break;

                        for (int k = 0; k <= maxSinum; k++) {
                            if (param.IsTmsMeoxDerivative && k < param.MinimumTmsCount) continue;
                            if (formulaBean.Mass + (double)h * iMass + (double)i * brMass + (double)j * clMass + (double)k * siMass + maxFHmass < mass - ms1Tol) continue;
                            if (formulaBean.Mass + (double)h * iMass + (double)i * brMass + (double)j * clMass + (double)k * siMass > mass + ms1Tol) break;

                            for (int l = 0; l <= maxFnum; l++) {
                                if (formulaBean.Mass + (double)h * iMass + (double)i * brMass + (double)j * clMass + (double)k * siMass + (double)l * fMass + maxHmass < mass - ms1Tol) continue;
                                if (formulaBean.Mass + (double)h * iMass + (double)i * brMass + (double)j * clMass + (double)k * siMass + (double)l * fMass > mass + ms1Tol) break;

                                for (int m = minHnum; m <= maxHnum; m++) {
                                    if (formulaBean.Mass + (double)h * iMass + (double)i * brMass + (double)j * clMass + (double)k * siMass + (double)l * fMass + (double)m * hMass < mass - ms1Tol) continue;
                                    if (formulaBean.Mass + (double)h * iMass + (double)i * brMass + (double)j * clMass + (double)k * siMass + (double)l * fMass + (double)m * hMass > mass + ms1Tol) break;

                                    if (param.IsTmsMeoxDerivative) {

                                        var tmsCount = k;
                                        for (int n = param.MinimumMeoxCount; n <= formulaBean.Nnum; n++) {
                                            var meoxCount = n;
                                            var formula = getCandidateFormulaBean(formulaBean, m, l, j, i, h, k, tmsCount, meoxCount);

                                            if (formula.Cnum - tmsCount * 3 - meoxCount <= 0) continue;
                                            var convertedFormula = MolecularFormulaUtility.ConvertTmsMeoxSubtractedFormula(formula);
                                            if (!SevenGoldenRulesCheck.Check(convertedFormula, this.valenceCheck, this.coverRange, this.probabilityCheck, adduct)) continue;

                                            var formulaResult = tryGetFormulaResultCandidate(formula, param,
                                                mass, ms1Tol, m1Intensity, m2Intensity, isotopeCheck, adduct,
                                                refinedPeaklist, neutralLosses, existFormulaDB, productIonDB, neutralLossDB);
                                            if (formulaResult != null)
                                                formulaCandidates = getFormulaResultCandidates(formulaCandidates, formulaResult, 100000000);
                                        }
                                    }
                                    else {
                                        var formula = getCandidateFormulaBean(formulaBean, m, l, j, i, h, k);
                                        var formulaResult = tryGetFormulaResultCandidate(formula, param,
                                                mass, ms1Tol, m1Intensity, m2Intensity, isotopeCheck, adduct,
                                                refinedPeaklist, neutralLosses, existFormulaDB, productIonDB, neutralLossDB);
                                        if (formulaResult != null)
                                            formulaCandidates = getFormulaResultCandidates(formulaCandidates, formulaResult, 100000000);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            #endregion
            return formulaCandidates;
        }

        private FormulaResult tryGetFormulaResultCandidate(Formula formula, AnalysisParamOfMsfinder param,
            double mass, double ms1Tol, double m1Intensity, double m2Intensity, bool isotopeCheck, AdductIon adduct,
            List<Peak> refinedPeaklist, List<NeutralLoss> neutralLosses, List<ExistFormulaQuery> existFormulaDB, List<ProductIon> productIonDB, List<NeutralLoss> neutralLossDB) {

            var ms2Tol = param.Mass2Tolerance;
            var massTolType = param.MassTolType;

            if (SevenGoldenRulesCheck.Check(formula, this.valenceCheck, this.coverRange, this.probabilityCheck, adduct)) {

                var formulaResult = getFormulaResult(formula, param, mass, ms1Tol, m1Intensity, m2Intensity, isotopeCheck, existFormulaDB);
                if (param.CanExcuteMS2AdductSearch) {
                    var precursorMz = (mass * (double)adduct.AdductIonXmer + adduct.AdductIonAccurateMass) / (double)adduct.ChargeNumber;
                    if (adduct.IonMode == IonMode.Positive) {
                        formulaResult.AnnotatedIonResult = FragmentAssigner.GetAnnotatedIon(refinedPeaklist, adduct, param.MS2PositiveAdductIonList, precursorMz, param.Mass2Tolerance, param.MassTolType);
                    }
                    else {
                        formulaResult.AnnotatedIonResult = FragmentAssigner.GetAnnotatedIon(refinedPeaklist, adduct, param.MS2NegativeAdductIonList, precursorMz, param.Mass2Tolerance, param.MassTolType);
                    }
                    refinedPeaklist = FragmentAssigner.GetRefinedPeaklist(refinedPeaklist, precursorMz);
                }
                setFragmentProperties(formulaResult, refinedPeaklist, neutralLosses, productIonDB, neutralLossDB, ms2Tol, massTolType, adduct);
                formulaResult.TotalScore = Math.Round(Scoring.TotalScore(formulaResult), 3);
                return formulaResult;
            }
            else
                return null;
        }



        //private List<FormulaResult> tryGetFormulaResultCandidate(List<FormulaResult> formulaCandidate, Formula formula, AnalysisParamOfMsfinder param, 
        //    double mass, double ms1Tol, double m1Intensity, double m2Intensity, bool isotopeCheck, AdductIon adduct,
        //    List<Peak> refinedPeaklist, List<NeutralLoss> neutralLosses, List<ExistFormulaQuery> existFormulaDB, List<ProductIon> productIonDB, List<NeutralLoss> neutralLossDB) {

        //    var ms2Tol = param.Mass2Tolerance;
        //    var massTolType = param.MassTolType;

        //    if (SevenGoldenRulesCheck.Check(formula, this.valenceCheck, this.coverRange, this.probabilityCheck, adduct)) {

        //        var formulaResult = getFormulaResult(formula, param, mass, ms1Tol, m1Intensity, m2Intensity, isotopeCheck, existFormulaDB);
        //        setFragmentProperties(formulaResult, refinedPeaklist, neutralLosses, productIonDB, neutralLossDB, ms2Tol, massTolType, adduct);
        //        formulaResult.TotalScore = Math.Round(Scoring.TotalScore(formulaResult), 3);
        //        formulaCandidate = getFormulaResultCandidates(formulaCandidate, formulaResult, 100000000);

        //        return formulaCandidate;

        //    }
        //    else return formulaCandidate;
        //}



        private List<FormulaResult> getFormulaResultCandidates(List<FormulaResult> formulaCandidate, FormulaResult formulaResult, int maxFormulaNum)
        {
            if (formulaCandidate.Count < maxFormulaNum - 1)
            {
                formulaCandidate.Add(formulaResult);
            }
            else if (formulaCandidate.Count == maxFormulaNum - 1)
            {
                formulaCandidate.Add(formulaResult);
                formulaCandidate = formulaCandidate.OrderByDescending(n => Math.Abs(n.TotalScore)).ToList();
            }
            else if (formulaCandidate.Count > maxFormulaNum - 1)
            {
                if (formulaCandidate[formulaCandidate.Count - 1].TotalScore < formulaResult.TotalScore)
                {
                    var startID = getFormulaResultStartIndex(formulaResult.TotalScore, 0.01, formulaCandidate);
                    var insertID = getFormulaResultInsertID(formulaCandidate, formulaResult, startID);
                    formulaCandidate.Insert(insertID, formulaResult);
                    formulaCandidate.RemoveAt(formulaCandidate.Count - 1);
                }
            }

            return formulaCandidate;
        }

        private int getFormulaResultStartIndex(double score, double tol, List<FormulaResult> results)
        {
            if (results == null || results.Count == 0) return 0;
            double targetScore = score + tol;
            int startIndex = 0, endIndex = results.Count - 1;
            int counter = 0;

            while (counter < 10)
            {
                if (results[startIndex].TotalScore <= targetScore && targetScore < results[(startIndex + endIndex) / 2].TotalScore)
                {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (results[(startIndex + endIndex) / 2].TotalScore <= targetScore && targetScore < results[endIndex].TotalScore)
                {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        private int getFormulaResultInsertID(List<FormulaResult> formulaCandidate, FormulaResult formulaResult, int startID)
        {
            var maxID = 0;
            for (int i = startID; i >= 0; i--)
            {
                if (formulaCandidate[i].TotalScore < formulaResult.TotalScore) maxID = i;
                else break;
            }
            return maxID;
        }

        private FormulaResult getFormulaResult(Formula formula, AnalysisParamOfMsfinder param, double mass, double ms1MassTol, double m1Intensity, double m2Intensity, bool isotopeCheck, List<ExistFormulaQuery> existFormulaDB)
        {
            var isotopicAbundanceTolerance = param.IsotopicAbundanceTolerance;
            var formulaResult = new FormulaResult();

            formulaResult.Formula = formula;
            formulaResult.MatchedMass = Math.Round(mass, 7);
            formulaResult.MassDiff = Math.Round(formula.Mass - mass, 7);
            formulaResult.M1IsotopicIntensity = Math.Round(SevenGoldenRulesCheck.GetM1IsotopicAbundance(formula), 4);
            formulaResult.M2IsotopicIntensity = Math.Round(SevenGoldenRulesCheck.GetM2IsotopicAbundance(formula), 4);
            formulaResult.M1IsotopicDiff = Math.Round(SevenGoldenRulesCheck.GetIsotopicDifference(formulaResult.M1IsotopicIntensity, m1Intensity), 4);
            formulaResult.M2IsotopicDiff = Math.Round(SevenGoldenRulesCheck.GetIsotopicDifference(formulaResult.M2IsotopicIntensity, m2Intensity), 4);

            formulaResult.MassDiffScore = Math.Round(Scoring.MassDifferenceScore(formulaResult.MassDiff, ms1MassTol), 3);

            if (isotopeCheck == true)
                formulaResult.IsotopicScore = Math.Round(Scoring.IsotopicDifferenceScore(formulaResult.M1IsotopicDiff, formulaResult.M2IsotopicDiff, isotopicAbundanceTolerance * 0.01), 3);
            else
                formulaResult.IsotopicScore = 1;

            setExistFormulaDbInfo(formulaResult, existFormulaDB);

            return formulaResult;
        }

        private Formula getCandidateFormulaBean(Formula originalFormula, int addHnum, int addFnum, int addClnum, int addBrnum, int addInum, int addSinum, int tmsCount = 0, int meoxCount = 0)
        {
            var cNum = originalFormula.Cnum;
            var hNum = originalFormula.Hnum + addHnum;
            var nNum = originalFormula.Nnum;
            var oNum = originalFormula.Onum;
            var pNum = originalFormula.Pnum;
            var sNum = originalFormula.Snum;
            var fNum = originalFormula.Fnum + addFnum;
            var clNum = originalFormula.Clnum + addClnum;
            var brNum = originalFormula.Brnum + addBrnum;
            var iNum = originalFormula.Inum + addInum;
            var siNum = originalFormula.Sinum + addSinum;

            return new Formula(cNum, hNum, nNum, oNum, pNum, sNum, fNum, clNum, brNum, iNum, siNum, tmsCount, meoxCount);
        }
    }
}

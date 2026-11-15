using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Ion;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Parameter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.Common.FormulaGenerator {
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

        private readonly int isCheckedNum;

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

        private readonly bool oCheck;
        private readonly bool nCheck;
        private readonly bool pCheck;
        private readonly bool sCheck;
        private readonly bool fCheck;
        private readonly bool brCheck;
        private readonly bool clCheck;
        private readonly bool iCheck;
        private readonly bool siCheck;
        private readonly bool valenceCheck;
        private readonly bool probabilityCheck;
        private readonly CoverRange coverRange;

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

            MaxFoldInitialize(this.coverRange, this.fCheck, this.clCheck, this.brCheck, this.iCheck, this.siCheck);
        }

        private void MaxFoldInitialize(CoverRange coverRange, bool fCheck, bool clCheck, bool brCheck, bool iCheck, bool siCheck)
        {
            switch (coverRange) {
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
            var refinedPeaklist = FragmentAssigner.GetRefinedPeaklist(ms2Peaklist, relativeAbundanceCutOff, 0.0, (mass * (double)adductIon.AdductIonXmer + adductIon.AdductIonAccurateMass) / (double)adductIon.ChargeNumber, ms2Tol, massTolType, 1000, false, !param.CanExcuteMS2AdductSearch);
            var neutralLosslist = FragmentAssigner.GetNeutralLossList(refinedPeaklist, rawData.PrecursorMz, ms1Tol);

            var fragmentData = new SpectrumFragmentData(refinedPeaklist, neutralLosslist, productIonDB, neutralLossDB, param, adductIon);

            //var endID = getFormulaDbLastIndex(formulaDB, mass + ms1Tol);

            formulaResults = GetFormulaResults(rawData, param, mass, ms1Tol, m1Intensity, m2Intensity, adductIon, isotopeCheck, maxReportNumber,
                existFormulaDB, fragmentData);

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
                formulaResults = [.. formulaResults.OrderByDescending(n => Math.Abs(n.TotalScore))];

                for (int i = 0; i < isCheckedNum; i++) { if (i > formulaResults.Count - 1) break; formulaResults[i].IsSelected = true; }

                while (formulaResults.Count > maxReportNumber) { formulaResults.RemoveAt(formulaResults.Count - 1); }

                // Candidates were ranked on their scores alone; only the ones reported get their product-ion and
                // neutral-loss assignments, which are the same lists the full scoring would have built.
                if (fragmentData.IsPrepared) {
                    var reported = formulaResults;
                    Parallel.For(0, reported.Count, i => SetFragmentProperties(reported[i], fragmentData));
                }
            }

            return formulaResults;
        }

        private List<FormulaResult> GetFormulaResults(RawData rawData, AnalysisParamOfMsfinder param,
            double mass, double ms1Tol, double m1Intensity, double m2Intensity, 
            AdductIon adduct, bool isotopeCheck, int maxReportNumber,
            List<ExistFormulaQuery> existFormulaDB, SpectrumFragmentData fragmentData) {

            var maxCnum = (int)(mass / 12.0);
            var formulaResultsMaster = new List<FormulaResult>();

            // Results are kept per carbon count and joined in carbon order after the loop. They used to be appended
            // in the order the parallel iterations finished, which made the order of candidates with equal scores,
            // and so the selection and truncation among them, differ from run to run.
            var resultsByCarbon = new List<FormulaResult>[Math.Max(maxCnum, 1)];

            var syncObj = new object();
            var sw = new Stopwatch();
            sw.Start();

            Parallel.For(1, maxCnum, (c, state) => {
                var formulaResults = GetFormulaResults(c, rawData, param, mass, ms1Tol, m1Intensity, m2Intensity, adduct, isotopeCheck,
                    maxReportNumber, existFormulaDB, fragmentData);

                if (formulaResults != null && formulaResults.Count != 0) {
                    lock (syncObj) {
                        var lap = sw.ElapsedMilliseconds * 0.001 / 60.0; // min
                        if (param.FormulaPredictionTimeOut > 0 && param.FormulaPredictionTimeOut < lap) {
                            Debug.WriteLine("Calculation stopped.");
                            state.Stop();
                        }
                        resultsByCarbon[c] = formulaResults;
                    }
                }
            });
            sw.Stop();

            foreach (var formulaResults in resultsByCarbon) {
                if (formulaResults == null) continue;
                foreach (var result in formulaResults) {
                    formulaResultsMaster = GetFormulaResultCandidates(formulaResultsMaster, result, 100000000);
                }
            }
            return formulaResultsMaster;
        }

        private List<FormulaResult> GetFormulaResults(int c, RawData rawData, AnalysisParamOfMsfinder param, double mass, double ms1Tol, double m1Intensity, double m2Intensity,
            AdductIon adduct, bool isotopeCheck, int maxReportNumber,
            List<ExistFormulaQuery> existFormulaDB, SpectrumFragmentData fragmentData) {

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

            // Each loop starts at the first count that can still reach the mass window, which is where the former
            // "continue" tests stopped skipping (see EnumerationBound.FirstNotBelow); the visited formulas, their
            // order and the "break" tests are unchanged.
            var lowerMass = mass - ms1Tol;
            var upperMass = mass + ms1Tol;
            var cBaseMass = (double)c * cMass;
            var isHydrogenShiftAdduct = SevenGoldenRulesCheck.IsHydrogenShiftAdduct(adduct);

            // Candidates are collected in enumeration order and scored afterwards (ScorePendingCandidates), in
            // parallel, so that a carbon count with many candidates does not keep one thread busy while the others
            // have finished; the results keep the enumeration order.
            var pending = new List<PendingCandidate>();

            var sw = new Stopwatch();
            sw.Start();
            for (int i = EnumerationBound.FirstNotBelow(cBaseMass, iMass, maxBrClSPSiFONHmass, lowerMass, maxInum); i <= maxInum; i++) {
                var ciMass = cBaseMass + (double)i * iMass;
                if (ciMass > upperMass) break;

                for (int br = EnumerationBound.FirstNotBelow(ciMass, brMass, maxClSPSiFONHmass, lowerMass, maxBrnum); br <= maxBrnum; br++) {
                    var cibrMass = ciMass + (double)br * brMass;
                    if (cibrMass > upperMass) break;

                    for (int cl = EnumerationBound.FirstNotBelow(cibrMass, clMass, maxSPSiFONHmass, lowerMass, maxClnum); cl <= maxClnum; cl++) {
                        var cibrclMass = cibrMass + (double)cl * clMass;
                        if (cibrclMass > upperMass) break;

                        for (int s = EnumerationBound.FirstNotBelow(cibrclMass, sMass, maxPSiFONHmass, lowerMass, maxSnum); s <= maxSnum; s++) {

                            if (rawData.SulfurNumberFromLabeledExperiment >= 0 && s != rawData.SulfurNumberFromLabeledExperiment / xMer) continue;

                            var cibrclsMass = cibrclMass + (double)s * sMass;
                            if (cibrclsMass > upperMass) break;

                            for (int p = EnumerationBound.FirstNotBelow(cibrclsMass, pMass, maxSiFONHmass, lowerMass, maxPnum); p <= maxPnum; p++) {
                                var cibrclspMass = cibrclsMass + (double)p * pMass;
                                if (cibrclspMass > upperMass) break;

                                for (int si = EnumerationBound.FirstNotBelow(cibrclspMass, siMass, maxFONHmass, lowerMass, maxSinum); si <= maxSinum; si++) {
                                    if (param.IsTmsMeoxDerivative && si < param.MinimumTmsCount) continue;

                                    var cibrclspsiMass = cibrclspMass + (double)si * siMass;
                                    if (cibrclspsiMass > upperMass) break;

                                    for (int f = EnumerationBound.FirstNotBelow(cibrclspsiMass, fMass, maxONHmass, lowerMass, maxFnum); f <= maxFnum; f++) {
                                        var cibrclspsifMass = cibrclspsiMass + (double)f * fMass;
                                        if (cibrclspsifMass > upperMass) break;

                                        for (int o = EnumerationBound.FirstNotBelow(cibrclspsifMass, oMass, maxNHmass, lowerMass, maxOnum); o <= maxOnum; o++) {

                                            if (rawData.OxygenNumberFromLabeledExperiment >= 0 && o != rawData.OxygenNumberFromLabeledExperiment / xMer) continue;

                                            var cibrclspsifoMass = cibrclspsifMass + (double)o * oMass;
                                            if (cibrclspsifoMass > upperMass) break;

                                            for (int n = EnumerationBound.FirstNotBelow(cibrclspsifoMass, nMass, maxHmass, lowerMass, maxNnum); n <= maxNnum; n++) {

                                                if (rawData.NitrogenNumberFromLabeledExperiment >= 0 && n != rawData.NitrogenNumberFromLabeledExperiment / xMer) continue;

                                                var cibrclspsifonMass = cibrclspsifoMass + (double)n * nMass;
                                                if (cibrclspsifonMass > upperMass) break;

                                                for (int h = EnumerationBound.FirstNotBelow(cibrclspsifonMass, hMass, 0.0, lowerMass, maxHnum); h <= maxHnum; h++) {
                                                    var cibrclspsifonhMass = cibrclspsifonMass + (double)h * hMass;
                                                    if (cibrclspsifonhMass > upperMass) break;

                                                    if (param.IsTmsMeoxDerivative) {
                                                        for (int meox = param.MinimumMeoxCount; meox <= n; meox++) {
                                                            var formula = new Formula(c, h, n, o, p, s, f, cl, br, i, si, si, meox);
                                                            if (formula.Cnum - si * 3 - meox <= 0) continue;

                                                            var convertedFormula = MolecularFormulaUtility.ConvertTmsMeoxSubtractedFormula(formula);
                                                            if (!SevenGoldenRulesCheck.Check(convertedFormula, this.valenceCheck, this.coverRange, this.probabilityCheck, adduct)) continue;
                                                            // Scoring checks the rules once more, on the formula with the derivative groups.
                                                            pending.Add(new PendingCandidate(formula, true));
                                                        }
                                                    }
                                                    else if (SevenGoldenRulesCheck.Check(c, h, n, o, p, s, f, cl, br, i, si,
                                                        this.valenceCheck, this.coverRange, this.probabilityCheck, isHydrogenShiftAdduct)) {
                                                        // The rules are checked on the counts first, so a Formula (mass and formula string)
                                                        // is built only for the candidates that are scored.
                                                        pending.Add(new PendingCandidate(new Formula(c, h, n, o, p, s, f, cl, br, i, si), false));
                                                    }

                                                    if (IsTimedOut(param, sw)) {
                                                        Debug.WriteLine("Calculation stopped.");
                                                        return ScorePendingCandidates(pending, sw, param, mass, ms1Tol, m1Intensity, m2Intensity, isotopeCheck, adduct,
                                                            existFormulaDB, fragmentData);
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

            return ScorePendingCandidates(pending, sw, param, mass, ms1Tol, m1Intensity, m2Intensity, isotopeCheck, adduct,
                existFormulaDB, fragmentData);
        }

        private struct PendingCandidate {
            public PendingCandidate(Formula formula, bool needsRuleCheck) {
                Formula = formula;
                NeedsRuleCheck = needsRuleCheck;
            }
            public Formula Formula { get; }
            public bool NeedsRuleCheck { get; }
        }

        private static bool IsTimedOut(AnalysisParamOfMsfinder param, Stopwatch sw) {
            var lap = sw.ElapsedMilliseconds * 0.001 / 60.0; // min
            return param.FormulaPredictionTimeOut > 0 && param.FormulaPredictionTimeOut < lap;
        }

        /// <summary>
        /// Scores the candidates of one carbon count in parallel and returns their results in enumeration order.
        /// When the formula prediction time-out passes, scoring stops and the results of the leading candidates
        /// that were scored are returned, as the sequential loop returned the results it had so far.
        /// </summary>
        private List<FormulaResult> ScorePendingCandidates(List<PendingCandidate> pending, Stopwatch sw, AnalysisParamOfMsfinder param,
            double mass, double ms1Tol, double m1Intensity, double m2Intensity, bool isotopeCheck, AdductIon adduct,
            List<ExistFormulaQuery> existFormulaDB, SpectrumFragmentData fragmentData) {

            var scored = new FormulaResult[pending.Count];
            var end = pending.Count;
            if (pending.Count > 0) {
                var loopResult = Parallel.For(0, pending.Count, (k, state) => {
                    if (IsTimedOut(param, sw)) {
                        state.Break();
                        return;
                    }
                    var candidate = pending[k];
                    scored[k] = candidate.NeedsRuleCheck
                        ? TryGetFormulaResultCandidate(candidate.Formula, param, mass, ms1Tol, m1Intensity, m2Intensity, isotopeCheck, adduct, existFormulaDB, fragmentData)
                        : GetScoredFormulaResult(candidate.Formula, param, mass, ms1Tol, m1Intensity, m2Intensity, isotopeCheck, adduct, existFormulaDB, fragmentData);
                });
                if (loopResult.LowestBreakIteration.HasValue) end = (int)loopResult.LowestBreakIteration.Value;
            }
            sw.Stop();

            var formulaResults = new List<FormulaResult>();
            for (int k = 0; k < end; k++) {
                if (scored[k] != null) formulaResults.Add(scored[k]);
            }
            return formulaResults;
        }

        private List<FormulaResult> GetFormulaSearchResults(Formula formula, RawData rawData, AnalysisParamOfMsfinder param, 
            double mass, double ms1Tol, double m1Intensity, double m2Intensity, AdductIon adductIon, 
            bool isotopeCheck, int maxReportNumber, List<ExistFormulaQuery> existFormulaDB,
            List<SpectrumPeak> refinedPeaklist, List<NeutralLoss> neutralLosslist, List<ProductIon> productIonDB,
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


            var tempResults = GetFormulaSearchResults(formula, param, mass, ms1Tol,
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

            formulaResults = GetFormulaResults(rawData, param, mass, ms1Tol, m1Intensity, m2Intensity, adductIon, isotopeCheck, maxReportNumber,
                existFormulaDB, new SpectrumFragmentData(null, null, null, null, param, adductIon));


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
                formulaResults = [.. formulaResults.OrderByDescending(n => Math.Abs(n.TotalScore))];

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
            var formulaResult = GetFormulaResult(formula, param, mass, ms1Tol, subtractedM1Intensity, subtractedM2Intensity, isotopeCheck, existFormulaDB);

            var ms2Peaklist = FragmentAssigner.GetCentroidMsMsSpectrum(rawData);

            if (ms2Peaklist != null && ms2Peaklist.Count != 0) {
                var refinedPeaklist = FragmentAssigner.GetRefinedPeaklist(ms2Peaklist, relativeAbundanceCutOff, 0.0, (mass * (double)adductIon.AdductIonXmer + adductIon.AdductIonAccurateMass) / (double)adductIon.ChargeNumber, ms2Tol, massTolType, 1000, false, !param.CanExcuteMS2AdductSearch);
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
                SetFragmentProperties(formulaResult, refinedPeaklist, neutralLosslist, productIonDB, neutralLossDB, ms2Tol, massTolType, adductIon);
            }

            formulaResult.TotalScore = Math.Round(Scoring.TotalScore(formulaResult), 3);
            formulaResult.IsSelected = true;

            return formulaResult;
        }

        private void SetExistFormulaDbInfo(FormulaResult formulaResult, List<ExistFormulaQuery> existFormulaDB)
        {
            TryExistFormulaDbSearch(formulaResult.Formula, existFormulaDB, out string resourceNames, out int resourceRecords, out List<int> pubchemCids);
            formulaResult.ResourceNames = resourceNames;
            formulaResult.ResourceRecords = resourceRecords;
            formulaResult.PubchemResources = pubchemCids;
        }

        private void TryExistFormulaDbSearch(Formula formula, List<ExistFormulaQuery> queryDB, out string resourceNames, out int resourceRecords, out List<int> pubchemCIDs)
        {
            pubchemCIDs = [];
            resourceNames = string.Empty;
            resourceRecords = 0;

            var cFormula = MolecularFormulaUtility.ConvertTmsMeoxSubtractedFormula(formula);
            var mass = cFormula.Mass;
            var tol = 0.00005;
            var startID = GetQueryStartIndex(mass, tol, queryDB);

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

        private static int GetQueryStartIndex(double mass, double tol, List<ExistFormulaQuery> queryDB)
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

        private void SetFragmentProperties(FormulaResult formulaResult, List<SpectrumPeak> refinedPeaklist, List<NeutralLoss> neutralLosslist,
            List<ProductIon> productIonDB, List<NeutralLoss> neutralLossDB, 
           double ms2Tol, MassToleranceType massTolType, AdductIon adductIon)
        {
            if (refinedPeaklist == null || neutralLosslist == null) return;

            formulaResult.ProductIonResult = FragmentAssigner.FastFragmentAssigner(refinedPeaklist, productIonDB, formulaResult.Formula, ms2Tol, massTolType, adductIon);
            formulaResult.NeutralLossResult = FragmentAssigner.FastNeutralLossAssigner(neutralLosslist, neutralLossDB, formulaResult.Formula, ms2Tol, massTolType, adductIon);

            formulaResult.ProductIonNum = formulaResult.ProductIonResult.Count;
            formulaResult.ProductIonHits = formulaResult.ProductIonResult.Count(n => n.CandidateOntologies.Count > 0);

            formulaResult.NeutralLossHits = GetUniqueNeutralLossCount(formulaResult.NeutralLossResult);
            formulaResult.NeutralLossNum = GetUniqueNeutralLossCountByMass(neutralLosslist, ms2Tol, massTolType);

            formulaResult.ProductIonScore = Math.Round(Scoring.FragmentHitsScore(refinedPeaklist, formulaResult.ProductIonResult, ms2Tol, massTolType), 3);
            formulaResult.NeutralLossScore = Math.Round(Scoring.NeutralLossScore(formulaResult.NeutralLossHits, formulaResult.NeutralLossNum), 3);
        }

        /// <summary>
        /// The same as the overload above, using what <paramref name="fragmentData"/> prepared for the spectrum.
        /// </summary>
        private static void SetFragmentProperties(FormulaResult formulaResult, SpectrumFragmentData fragmentData)
        {
            if (!fragmentData.IsPrepared) return;

            formulaResult.ProductIonResult = FragmentAssigner.FastFragmentAssigner(fragmentData.ProductIonQuery, formulaResult.Formula);
            formulaResult.NeutralLossResult = FragmentAssigner.FastNeutralLossAssigner(fragmentData.NeutralLossQuery, formulaResult.Formula);

            formulaResult.ProductIonNum = formulaResult.ProductIonResult.Count;
            formulaResult.ProductIonHits = formulaResult.ProductIonResult.Count(n => n.CandidateOntologies.Count > 0);

            formulaResult.NeutralLossHits = GetUniqueNeutralLossCount(formulaResult.NeutralLossResult);
            formulaResult.NeutralLossNum = fragmentData.NeutralLossNum;

            formulaResult.ProductIonScore = Math.Round(Scoring.FragmentHitsScore(fragmentData.RefinedPeaklist.Count, fragmentData.MonoisotopicPeakCount, formulaResult.ProductIonResult.Count), 3);
            formulaResult.NeutralLossScore = Math.Round(Scoring.NeutralLossScore(formulaResult.NeutralLossHits, formulaResult.NeutralLossNum), 3);
        }


        private static int GetUniqueNeutralLossCount(List<NeutralLoss> neutralLosses) {

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

        private static int GetUniqueNeutralLossCountByMass(List<NeutralLoss> neutralLosses, double ms2Tol, MassToleranceType massTolType) {

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

        private List<FormulaResult> GetFormulaSearchResults(Formula formulaBean, AnalysisParamOfMsfinder param,
        double mass, double ms1Tol, double m1Intensity, double m2Intensity, AdductIon adduct, bool isotopeCheck, int maxFormulaNum,
        List<ExistFormulaQuery> existFormulaDB, List<SpectrumPeak> refinedPeaklist, List<NeutralLoss> neutralLosses, List<ProductIon> productIonDB, List<NeutralLoss> neutralLossDB) {

            if (param.IsTmsMeoxDerivative && formulaBean.Nnum < param.MinimumMeoxCount) return [];

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
                                            var formula = GetCandidateFormulaBean(formulaBean, m, l, j, i, h, k, tmsCount, meoxCount);

                                            if (formula.Cnum - tmsCount * 3 - meoxCount <= 0) continue;
                                            var convertedFormula = MolecularFormulaUtility.ConvertTmsMeoxSubtractedFormula(formula);
                                            if (!SevenGoldenRulesCheck.Check(convertedFormula, this.valenceCheck, this.coverRange, this.probabilityCheck, adduct)) continue;

                                            var formulaResult = TryGetFormulaResultCandidate(formula, param,
                                                mass, ms1Tol, m1Intensity, m2Intensity, isotopeCheck, adduct,
                                                refinedPeaklist, neutralLosses, existFormulaDB, productIonDB, neutralLossDB);
                                            if (formulaResult != null)
                                                formulaCandidates = GetFormulaResultCandidates(formulaCandidates, formulaResult, 100000000);
                                        }
                                    }
                                    else {
                                        var formula = GetCandidateFormulaBean(formulaBean, m, l, j, i, h, k);
                                        var formulaResult = TryGetFormulaResultCandidate(formula, param,
                                                mass, ms1Tol, m1Intensity, m2Intensity, isotopeCheck, adduct,
                                                refinedPeaklist, neutralLosses, existFormulaDB, productIonDB, neutralLossDB);
                                        if (formulaResult != null)
                                            formulaCandidates = GetFormulaResultCandidates(formulaCandidates, formulaResult, 100000000);
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

        private FormulaResult TryGetFormulaResultCandidate(Formula formula, AnalysisParamOfMsfinder param,
            double mass, double ms1Tol, double m1Intensity, double m2Intensity, bool isotopeCheck, AdductIon adduct,
            List<SpectrumPeak> refinedPeaklist, List<NeutralLoss> neutralLosses, List<ExistFormulaQuery> existFormulaDB, List<ProductIon> productIonDB, List<NeutralLoss> neutralLossDB) {

            var fragmentData = new SpectrumFragmentData(refinedPeaklist, neutralLosses, productIonDB, neutralLossDB, param, adduct);
            var formulaResult = TryGetFormulaResultCandidate(formula, param, mass, ms1Tol, m1Intensity, m2Intensity, isotopeCheck, adduct,
                existFormulaDB, fragmentData);
            if (formulaResult != null) SetFragmentProperties(formulaResult, fragmentData);
            return formulaResult;
        }

        private FormulaResult TryGetFormulaResultCandidate(Formula formula, AnalysisParamOfMsfinder param,
            double mass, double ms1Tol, double m1Intensity, double m2Intensity, bool isotopeCheck, AdductIon adduct,
            List<ExistFormulaQuery> existFormulaDB, SpectrumFragmentData fragmentData) {

            if (!SevenGoldenRulesCheck.Check(formula, this.valenceCheck, this.coverRange, this.probabilityCheck, adduct)) return null;
            return GetScoredFormulaResult(formula, param, mass, ms1Tol, m1Intensity, m2Intensity, isotopeCheck, adduct, existFormulaDB, fragmentData);
        }

        /// <summary>
        /// Scores a candidate formula that has passed the seven golden rules.
        /// </summary>
        private FormulaResult GetScoredFormulaResult(Formula formula, AnalysisParamOfMsfinder param,
            double mass, double ms1Tol, double m1Intensity, double m2Intensity, bool isotopeCheck, AdductIon adduct,
            List<ExistFormulaQuery> existFormulaDB, SpectrumFragmentData fragmentData) {

            var ms2Tol = param.Mass2Tolerance;
            var massTolType = param.MassTolType;

            var formulaResult = GetFormulaResult(formula, param, mass, ms1Tol, m1Intensity, m2Intensity, isotopeCheck, existFormulaDB);
            if (param.CanExcuteMS2AdductSearch) {
                var refinedPeaklist = fragmentData.RefinedPeaklist;
                var precursorMz = (mass * (double)adduct.AdductIonXmer + adduct.AdductIonAccurateMass) / (double)adduct.ChargeNumber;
                if (adduct.IonMode == IonMode.Positive) {
                    formulaResult.AnnotatedIonResult = FragmentAssigner.GetAnnotatedIon(refinedPeaklist, adduct, param.MS2PositiveAdductIonList, precursorMz, param.Mass2Tolerance, param.MassTolType);
                }
                else {
                    formulaResult.AnnotatedIonResult = FragmentAssigner.GetAnnotatedIon(refinedPeaklist, adduct, param.MS2NegativeAdductIonList, precursorMz, param.Mass2Tolerance, param.MassTolType);
                }
                refinedPeaklist = FragmentAssigner.GetRefinedPeaklist(refinedPeaklist, precursorMz);
                SetFragmentProperties(formulaResult, refinedPeaklist, fragmentData.NeutralLosses, fragmentData.ProductIonDB, fragmentData.NeutralLossDB, ms2Tol, massTolType, adduct);
            }
            else {
                SetFragmentScores(formulaResult, fragmentData);
            }
            formulaResult.TotalScore = Math.Round(Scoring.TotalScore(formulaResult), 3);
            return formulaResult;
        }

        /// <summary>
        /// Sets the fragment scores that enter <see cref="Scoring.TotalScore(FormulaResult)"/>, with the values
        /// <see cref="SetFragmentProperties(FormulaResult, SpectrumFragmentData)"/> would set, without building the
        /// product-ion and neutral-loss lists. Most candidates are scored and then dropped, so the lists are built
        /// only for the reported ones (see GetFormulaCandidateList).
        /// </summary>
        private static void SetFragmentScores(FormulaResult formulaResult, SpectrumFragmentData fragmentData)
        {
            if (!fragmentData.IsPrepared) return;

            var productIonNum = FragmentAssigner.CountProductIons(fragmentData.ProductIonQuery, formulaResult.Formula);
            formulaResult.ProductIonNum = productIonNum;
            formulaResult.NeutralLossHits = FragmentAssigner.CountUniqueNeutralLossFormulas(fragmentData.NeutralLossQuery, formulaResult.Formula);
            formulaResult.NeutralLossNum = fragmentData.NeutralLossNum;

            formulaResult.ProductIonScore = Math.Round(Scoring.FragmentHitsScore(fragmentData.RefinedPeaklist.Count, fragmentData.MonoisotopicPeakCount, productIonNum), 3);
            formulaResult.NeutralLossScore = Math.Round(Scoring.NeutralLossScore(formulaResult.NeutralLossHits, formulaResult.NeutralLossNum), 3);
        }

        /// <summary>
        /// The spectrum-level inputs of fragment scoring, with the parts that do not depend on the candidate formula
        /// prepared once instead of once per candidate.
        /// </summary>
        private sealed class SpectrumFragmentData {
            public SpectrumFragmentData(List<SpectrumPeak> refinedPeaklist, List<NeutralLoss> neutralLosses,
                List<ProductIon> productIonDB, List<NeutralLoss> neutralLossDB, AnalysisParamOfMsfinder param, AdductIon adduct) {
                RefinedPeaklist = refinedPeaklist;
                NeutralLosses = neutralLosses;
                ProductIonDB = productIonDB;
                NeutralLossDB = neutralLossDB;

                if (refinedPeaklist == null || neutralLosses == null) return;
                // With the MS2 adduct search the peak list is narrowed for each candidate, so that path prepares nothing.
                if (param.CanExcuteMS2AdductSearch) return;

                ProductIonQuery = FragmentAssigner.PrepareProductIonQuery(refinedPeaklist, productIonDB, param.Mass2Tolerance, param.MassTolType, adduct);
                NeutralLossQuery = FragmentAssigner.PrepareNeutralLossQuery(neutralLosses, neutralLossDB, param.Mass2Tolerance, param.MassTolType, adduct);
                NeutralLossNum = GetUniqueNeutralLossCountByMass(neutralLosses, param.Mass2Tolerance, param.MassTolType);
                MonoisotopicPeakCount = refinedPeaklist.Count(n => n.Comment == "M");
            }

            public List<SpectrumPeak> RefinedPeaklist { get; }
            public List<NeutralLoss> NeutralLosses { get; }
            public List<ProductIon> ProductIonDB { get; }
            public List<NeutralLoss> NeutralLossDB { get; }

            /// <summary>True when the queries below are prepared, so candidates are scored on counts and the reported ones completed later.</summary>
            public bool IsPrepared => ProductIonQuery != null;
            public ProductIonQuery ProductIonQuery { get; }
            public NeutralLossQuery NeutralLossQuery { get; }
            /// <summary>The number of observed neutral losses with distinct masses; it does not depend on the candidate.</summary>
            public int NeutralLossNum { get; }
            public int MonoisotopicPeakCount { get; }
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



        private static List<FormulaResult> GetFormulaResultCandidates(List<FormulaResult> formulaCandidate, FormulaResult formulaResult, int maxFormulaNum)
        {
            if (formulaCandidate.Count < maxFormulaNum - 1)
            {
                formulaCandidate.Add(formulaResult);
            }
            else if (formulaCandidate.Count == maxFormulaNum - 1)
            {
                formulaCandidate.Add(formulaResult);
                formulaCandidate = [.. formulaCandidate.OrderByDescending(n => Math.Abs(n.TotalScore))];
            }
            else if (formulaCandidate.Count > maxFormulaNum - 1)
            {
                if (formulaCandidate[formulaCandidate.Count - 1].TotalScore < formulaResult.TotalScore)
                {
                    var startID = GetFormulaResultStartIndex(formulaResult.TotalScore, 0.01, formulaCandidate);
                    var insertID = GetFormulaResultInsertID(formulaCandidate, formulaResult, startID);
                    formulaCandidate.Insert(insertID, formulaResult);
                    formulaCandidate.RemoveAt(formulaCandidate.Count - 1);
                }
            }

            return formulaCandidate;
        }

        private static int GetFormulaResultStartIndex(double score, double tol, List<FormulaResult> results)
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

        private static int GetFormulaResultInsertID(List<FormulaResult> formulaCandidate, FormulaResult formulaResult, int startID)
        {
            var maxID = 0;
            for (int i = startID; i >= 0; i--)
            {
                if (formulaCandidate[i].TotalScore < formulaResult.TotalScore) maxID = i;
                else break;
            }
            return maxID;
        }

        private FormulaResult GetFormulaResult(Formula formula, AnalysisParamOfMsfinder param, double mass, double ms1MassTol, double m1Intensity, double m2Intensity, bool isotopeCheck, List<ExistFormulaQuery> existFormulaDB) 
        {
            var isotopicAbundanceTolerance = param.IsotopicAbundanceTolerance;
            var formulaResult = new FormulaResult {
                Formula = formula,
                MatchedMass = Math.Round(mass, 7),
                MassDiff = Math.Round(formula.Mass - mass, 7),
                M1IsotopicIntensity = Math.Round(SevenGoldenRulesCheck.GetM1IsotopicAbundance(formula), 4),
                M2IsotopicIntensity = Math.Round(SevenGoldenRulesCheck.GetM2IsotopicAbundance(formula), 4)
            };
            formulaResult.M1IsotopicDiff = Math.Round(SevenGoldenRulesCheck.GetIsotopicDifference(formulaResult.M1IsotopicIntensity, m1Intensity), 4);
            formulaResult.M2IsotopicDiff = Math.Round(SevenGoldenRulesCheck.GetIsotopicDifference(formulaResult.M2IsotopicIntensity, m2Intensity), 4);

            formulaResult.MassDiffScore = Math.Round(Scoring.MassDifferenceScore(formulaResult.MassDiff, ms1MassTol), 3);

            if (isotopeCheck == true)
                formulaResult.IsotopicScore = Math.Round(Scoring.IsotopicDifferenceScore(formulaResult.M1IsotopicDiff, formulaResult.M2IsotopicDiff, isotopicAbundanceTolerance * 0.01), 3);
            else
                formulaResult.IsotopicScore = 1;

            SetExistFormulaDbInfo(formulaResult, existFormulaDB);

            return formulaResult;
        }

        private static Formula GetCandidateFormulaBean(Formula originalFormula, int addHnum, int addFnum, int addClnum, int addBrnum, int addInum, int addSinum, int tmsCount = 0, int meoxCount = 0) 
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

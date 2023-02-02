using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.StructureFinder.Fragmenter;
using Riken.Metabolomics.StructureFinder.Parser;
using Riken.Metabolomics.StructureFinder.Result;
using Riken.Metabolomics.StructureFinder.SpectralAssigner;
using Riken.Metabolomics.StructureFinder.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.StructureFinder
{
    public sealed class MainProcess
    {
        private MainProcess() { }

        /// <summary>
        /// main process when SDF file is used as input.
        /// </summary>
        public static List<FragmenterResult> Fragmenter(string sdfFile, Rfx.Riken.OsakaUniv.RawData rawdata, List<Peak> curatedPeaks, 
            List<Peak> originalPeaks, AdductIon adduct,
            FormulaResult formulaResult, AnalysisParamOfMsfinder param, List<ExistStructureQuery> existStructureDB,
            List<FragmentLibrary> fragmentDB, List<FragmentOntology> fragmentOntologies)
        {
            
            var structures = MoleculeConverter.SdfToStructures(sdfFile, existStructureDB, param,
                formulaResult.Formula.TmsCount, formulaResult.Formula.MeoxCount);

            var results = FragmenterResults(structures, rawdata, curatedPeaks, originalPeaks, adduct,
                formulaResult, param, fragmentDB, fragmentOntologies);
            if (results != null && results.Count > 0) {
                results = results.OrderByDescending(n => n.TotalScore).ToList();
                var fResults = new List<FragmenterResult>();
                for (int i = 0; i < results.Count; i++) {
                    if (i >= param.StructureMaximumReportNumber) break;
                    fResults.Add(results[i]);
                }
                return fResults;
            }
            else {
                return results;
            }
            //return FragmenterResults(structures, rawdata, curatedPeaks, originalPeaks, adduct, 
            //    formulaResult, param, fragmentDB, fragmentOntologies);
        }

        /// <summary>
        /// main process when ExistStructureQueries are used as input.
        /// </summary>
        public static List<FragmenterResult> Fragmenter(List<ExistStructureQuery> queries, Rfx.Riken.OsakaUniv.RawData rawdata,
            List<Peak> curatedPeaks, List<Peak> originalPeaks, AdductIon adduct,
            FormulaResult formulaResult, AnalysisParamOfMsfinder param, 
            List<FragmentLibrary> fragmentDB, List<FragmentOntology> fragmentOntologies)
        {
            var dbQueries = getDabaseQueries(param.DatabaseQuery);
            var structures = MoleculeConverter.QueriesToStructures(queries, dbQueries,
                formulaResult.Formula.TmsCount, formulaResult.Formula.MeoxCount);

            var results = FragmenterResults(structures, rawdata, curatedPeaks, originalPeaks, adduct,
                formulaResult, param, fragmentDB, fragmentOntologies);
            if (results != null && results.Count > 0) {
                results = results.OrderByDescending(n => n.TotalScore).ToList();
                var fResults = new List<FragmenterResult>();
                for (int i = 0; i < results.Count; i++) {
                    if (i >= param.StructureMaximumReportNumber) break;
                    fResults.Add(results[i]);
                }
                return fResults;
            }
            else {
                return results;
            }

            //return FragmenterResults(structures, rawdata, curatedPeaks, originalPeaks, adduct, 
            //    formulaResult, param, fragmentDB, fragmentOntologies);
        }

        public static List<Property.Fragment> InsilicoFragmentGenerator(string smiles) {
            var error = string.Empty;
            var structure = MoleculeConverter.SmilesToStructure(smiles, out error, 0, 0);
            if (structure == null) return null;

            var fragments = FragmentGenerator.GetFragmentCandidates(structure, 2, 0);
            return fragments;
        }

        private static string getDabaseQueries(DatabaseQuery databaseQuery)
        {
            var queryStrings = string.Empty;
            var infoArray = databaseQuery.GetType().GetProperties();
            foreach (var info in infoArray) {
                if ((bool)info.GetValue(databaseQuery, null) == true) {
                    queryStrings += info.Name + ";";
                }
            }
            return queryStrings;
        }

        public static List<FragmenterResult> FragmenterResults(List<Property.Structure> structures, Rfx.Riken.OsakaUniv.RawData rawdata, List<Peak> curatedPeaks, List<Peak> originalPeaks,
            AdductIon adduct, FormulaResult formulaResult, AnalysisParamOfMsfinder param, 
            List<FragmentLibrary> fragmentDB, List<FragmentOntology> fragmentOntologies)
        {
            var results = new List<FragmenterResult>();
            var cutoff = param.StructureScoreCutOff;

            var syncObj = new object();
            var counter = 0;
            var sw = new Stopwatch();
            sw.Start();

            Parallel.ForEach(structures, (structure, state) => {
                var result = GetFragmenterResult(structure, rawdata, curatedPeaks, originalPeaks, adduct,
                    formulaResult, param, fragmentDB, fragmentOntologies);
                if (result != null && result.TotalScore > cutoff) {
                    lock (syncObj) {
                        var lap = sw.ElapsedMilliseconds * 0.001 / 60.0; // min
                        if (param.StructurePredictionTimeOut > 0 && param.StructurePredictionTimeOut < lap) {
                            state.Stop();
                        }
                        results.Add(result);
                        counter++;
                    }
                }
            });
            sw.Stop();
            return results;
        }

        public static FragmenterResult GetFragmenterResult(Property.Structure structure, Rfx.Riken.OsakaUniv.RawData rawdata, List<Peak> curatedPeaks, List<Peak> originalPeaks,
            AdductIon adduct, FormulaResult formulaResult, AnalysisParamOfMsfinder param, 
            List<FragmentLibrary> fragmentDB, List<FragmentOntology> fragmentOntologies)
        {
            FragmenterResult fragmenterResult = null;

            var referenceRt = FragmenterScoring.PredictRetentionTime(rawdata, structure, param);
            var referenceCcs = FragmenterScoring.CollisionCrossSection(rawdata, structure, param);
            var rtSimilarity = FragmenterScoring.CalculateRtSimilarity(rawdata, structure, param);
            var ccsSimilarity = FragmenterScoring.CalculateCcsSimilarity(rawdata, structure, param);
            if (param.IsUseRtForFilteringCandidates) {
                if (rawdata.RetentionTime > 0 && referenceRt >= 0 && Math.Abs(rawdata.RetentionTime - referenceRt) > param.RtToleranceForStructureElucidation) return null;
            }
            if (param.IsUseCcsForFilteringCandidates) {
                if (rawdata.Ccs > 0 && referenceCcs >= 0 && Math.Abs(rawdata.Ccs - referenceCcs) > param.CcsToleranceForStructureElucidation) return null;
            }

            if (curatedPeaks != null && curatedPeaks.Count > 0) {
                var fragments = FragmentGenerator.GetFragmentCandidates(structure, param.TreeDepth, curatedPeaks.Min(n => n.Mz));
                var peakFragmentPairs = FragmentPeakMatcher.GetSpectralAssignmentResult(structure, fragments, curatedPeaks, adduct,
                    param.TreeDepth, param.Mass2Tolerance, param.MassTolType, adduct.IonMode, fragmentDB);

                fragmenterResult = new FragmenterResult(structure, peakFragmentPairs, -1, -1, -1);

                FragmenterScoring.CalculateFragmenterScores(fragmenterResult, originalPeaks.Count);
                fragmenterResult.DatabaseScore = FragmenterScoring.CalculateDatabaseScore(structure.ResourceNames, structure.ResourceNumber, structure.DatabaseQueries);

                var uniqueMsmsOntologiesCount = 0;
                var msmsOntologies = FragmenterScoring.GetMonitoredSubstructureOntologies(formulaResult.ProductIonResult, 
                    formulaResult.NeutralLossResult, fragmentOntologies, out uniqueMsmsOntologiesCount);
                fragmenterResult.SubstructureAssignmentScore = FragmenterScoring.CalculateSubstructureScore(structure, msmsOntologies, uniqueMsmsOntologiesCount);
                fragmenterResult.RetentionTime = referenceRt;
                fragmenterResult.Ccs = referenceCcs;
                fragmenterResult.RtSimilarityScore = rtSimilarity;
                fragmenterResult.CcsSimilarityScore = ccsSimilarity;

                var isEimsSearch = param.IsTmsMeoxDerivative == true ? true : false;
                fragmenterResult.TotalScore = FragmenterScoring.CalculateTotalScore(fragmenterResult, isEimsSearch);
            }
            else {
                fragmenterResult = new FragmenterResult(structure, null, -1, -1, -1);
                fragmenterResult.DatabaseScore = FragmenterScoring.CalculateDatabaseScore(structure.ResourceNames, structure.ResourceNumber, structure.DatabaseQueries);
                fragmenterResult.RetentionTime = referenceRt;
                fragmenterResult.RtSimilarityScore = rtSimilarity;
                fragmenterResult.Ccs = referenceCcs;
                fragmenterResult.CcsSimilarityScore = ccsSimilarity;

                var isEimsSearch = param.IsTmsMeoxDerivative == true ? true : false;
                fragmenterResult.TotalScore = FragmenterScoring.CalculateTotalScore(fragmenterResult, isEimsSearch);
            }
            fragmenterResult.TotalScore += formulaResult.TotalScore;

            return fragmenterResult;
        }
    }
}

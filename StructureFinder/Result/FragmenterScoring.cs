using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Riken.Metabolomics.StructureFinder.Property;
using Rfx.Riken.OsakaUniv;

namespace Riken.Metabolomics.StructureFinder.Result {
    public sealed class FragmenterScoring {
        private FragmenterScoring() { }

        public static double CalculateTotalScore(FragmenterResult result, bool isEimsSearch)
        {
            var fragmenterScore = CalculateFragmenterScore(result);

            var fragFactore = 2.0;
            var subStructureFactor = 1.0;
            var dbFactor = 3.0;
            var rtFactor = 0.0;
            var ccsFactor = 0.0;
            if (result.RtSimilarityScore > 0) rtFactor = 1.0;
            if (result.CcsSimilarityScore > 0) ccsFactor = 1.0;

            if (isEimsSearch) {
                var totalScore = fragFactore * fragmenterScore +
              result.DatabaseScore * dbFactor + result.RtSimilarityScore * rtFactor;
                return totalScore /
                   (fragFactore + dbFactor + rtFactor) * 5.0;
            }
            else {
                var totalScore = fragFactore * fragmenterScore +
                result.SubstructureAssignmentScore * subStructureFactor +
                result.DatabaseScore * dbFactor + 
                result.RtSimilarityScore * rtFactor + result.CcsSimilarityScore * ccsFactor;

                return totalScore /
                    (fragFactore + subStructureFactor + dbFactor + rtFactor + ccsFactor) * 5.0;
            }
        }

        public static double RetentionTimeSimilairty(Rfx.Riken.OsakaUniv.RawData rawdata,
            MspFormatCompoundInformationBean mspRecord, AnalysisParamOfMsfinder param) {

            if (!param.IsUseExperimentalRtForSpectralSearching) return -1;

            var rt = rawdata.RetentionTime;
            var ri = rawdata.RetentionIndex;
            var mspRt = mspRecord.RetentionTime;
            var mspRi = mspRecord.RetentionIndex;

            if (param.RetentionType == RetentionType.RT && rt >= 0 && mspRt >= 0) {
                return BasicMathematics.StandadizedGaussianFunction(Math.Abs(rt - mspRt), param.RtToleranceForSpectralSearching);
            }
            else if (param.RetentionType == RetentionType.RI && ri > 0 && mspRi > 0) {
                return BasicMathematics.StandadizedGaussianFunction(Math.Abs(ri - mspRi), param.RtToleranceForSpectralSearching);
            }
            else {
                return -1;
            }
        }

        public static double CalculateFragmenterScore(FragmenterResult result)
        {
            //var hrFactor = 0.5221;
            //var bcFactor = 1.7930;
            //var maFactor = 0.0;
            //var flFactor = 0.0;
            //var beFactor = 3.0687;

            var hrFactor = 1.0;
            var bcFactor = 1.0;
            var maFactor = 2.0;
            var flFactor = 1.0;
            var beFactor = 3.0;

            var totalScore = result.TotalHrLikelihood * hrFactor +
                result.TotalBcLikelihood * bcFactor +
                result.TotalMaLikelihood * maFactor +
                result.TotalFlLikelihood * flFactor +
                result.TotalBeLikelihood * beFactor;

            return totalScore /
                (hrFactor + bcFactor + maFactor + flFactor + beFactor);
        }

        public static void CalculateFragmenterScores(FragmenterResult result, int peaksCount) {
            if (result.FragmentPics == null || result.FragmentPics.Count == 0) {
                result.TotalHrLikelihood = 0.0;
                result.TotalBcLikelihood = 0.0;
                result.TotalMaLikelihood = 0.0;
                result.TotalFlLikelihood = 0.0;
                result.TotalBeLikelihood = 0.0;
                return;
            }
            var totalHrLikelihood = 0.0;
            var totalBcLikelihood = 0.0;
            var totalMaLikelihood = 0.0;
            var totalFlLikelihood = 0.0;
            var totalBeLikelihood = 0.0;

            var fragPeaks = result.FragmentPics;
            foreach (var frag in fragPeaks) {
                var info = frag.MatchedFragmentInfo;
                totalHrLikelihood += info.HrLikelihood;
                totalBcLikelihood += info.BcLikelihood;
                totalMaLikelihood += info.MaLikelihood;
                totalFlLikelihood += info.FlLikelihood;
                totalBeLikelihood += info.BeLikelihood;
            }
            var count = (double)peaksCount;

            result.TotalHrLikelihood = totalHrLikelihood / count;
            result.TotalBcLikelihood = totalBcLikelihood / count;
            result.TotalMaLikelihood = totalMaLikelihood / count;
            result.TotalFlLikelihood = totalFlLikelihood / count;
            result.TotalBeLikelihood = totalBeLikelihood / count;
        }

        public static double PredictRetentionTime(Rfx.Riken.OsakaUniv.RawData rawdata,
            Structure structure, AnalysisParamOfMsfinder param) {

            if (!param.IsUsePredictedRtForStructureElucidation) return -1.0;
            if (param.IsUseXlogpPrediction) {
                if (param.Coeff_RtPrediction < 0) return -1.0;

                var xlogp = structure.XlogP;
                var predRt = param.Coeff_RtPrediction * xlogp + param.Intercept_RtPrediction;
                if (predRt < 0) return -1.0;
                else
                    return predRt;
            }
            else if (param.IsUseRtInchikeyLibrary) {
                return structure.Retentiontime;
            }
            else {
                return -1.0;
            }
        }

        public static double CollisionCrossSection(Rfx.Riken.OsakaUniv.RawData rawdata,
            Structure structure, AnalysisParamOfMsfinder param) {

            if (!param.IsUsePredictedCcsForStructureElucidation) return -1.0;
           
            var adductString = rawdata.PrecursorType;
            if (structure.AdductToCcs == null) return -1.0;
            if (!structure.AdductToCcs.ContainsKey(adductString)) return -1.0;
            var predCcs = structure.AdductToCcs[adductString];
            if (predCcs <= 0) return -1.0;
            return predCcs;
        }

        public static double CalculateRtSimilarity(Rfx.Riken.OsakaUniv.RawData rawdata, 
            Structure structure, AnalysisParamOfMsfinder param) {
            if (!param.IsUsePredictedRtForStructureElucidation) return -1.0;
            if (param.IsUseXlogpPrediction) {
                if (param.Coeff_RtPrediction < 0) return -1.0;
                if (rawdata.RetentionTime <= 0) return -1.0;

                var expRt = rawdata.RetentionTime;
                var xlogp = structure.XlogP;
                var predRt = param.Coeff_RtPrediction * xlogp + param.Intercept_RtPrediction;
                if (predRt < 0) return -1.0;

                var rtsimilarity = BasicMathematics.StandadizedGaussianFunction(Math.Abs(predRt - expRt), param.RtToleranceForStructureElucidation);
                return rtsimilarity;
            }
            else if (param.IsUseRtInchikeyLibrary) {
                if (rawdata.RetentionTime <= 0) return -1.0;
                var expRt = rawdata.RetentionTime;
                var predRt = structure.Retentiontime;
                if (predRt < 0) return -1.0;

                var rtsimilarity = BasicMathematics.StandadizedGaussianFunction(Math.Abs(predRt - expRt), param.RtToleranceForStructureElucidation);
                return rtsimilarity;
            }
            else {
                return -1.0;
            }
        }

        public static double CalculateCcsSimilarity(Rfx.Riken.OsakaUniv.RawData rawdata,
            Structure structure, AnalysisParamOfMsfinder param) {
            if (!param.IsUsePredictedCcsForStructureElucidation) return -1.0;
            if (rawdata.Ccs <= 0) return -1.0;
            var expCcs = rawdata.Ccs;
            var adductString = rawdata.PrecursorType;
            if (structure.AdductToCcs == null) return -1.0;
            if (!structure.AdductToCcs.ContainsKey(adductString)) return -1.0;
            var predCcs = structure.AdductToCcs[adductString];
            if (predCcs <= 0) return -1.0;

            var ccsSimilarity = BasicMathematics.StandadizedGaussianFunction(Math.Abs(expCcs - predCcs),
                param.RtToleranceForStructureElucidation);
            return ccsSimilarity;
        }


        public static double CalculateDatabaseScore(string databases, int databaseNumber, string queries) 
        {
            if (databaseNumber <= 0) return 0.0;
            var totalScore = 0.0;
            var upperCaseDBs = databases.ToUpper();

            var queryCount = 0;
            var databaseCount = 0;

            if (queries != null && queries != string.Empty) {
                var queryArray = queries.Split(';').ToArray();
                foreach (var query in queryArray) {
                    if (query != null && query != string.Empty) {
                        queryCount++;

                        if (upperCaseDBs.Contains(query.ToUpper())) {
                            databaseCount++;
                        }
                    }
                }
                totalScore += 0.5 * (double)databaseCount / (double)queryCount;
            }

            if (databases.Contains("MINE")) {
                totalScore += 0.2;
                if (databaseNumber == 2 && databases.Contains("UNPD"))
                    totalScore += 0.2;
                else if (databaseNumber >= 2)
                    totalScore += 0.5;
            }
            else if (databaseNumber == 1 && databases.Contains("UNPD")) {
                totalScore += 0.2;
            }
            else if (databaseNumber > 0) {
                totalScore += 0.5;
            }
            //totalScore += (double)databaseNumber / 14.0;
            return totalScore;

            //if (databases.Contains("MINE")) {
            //    totalScore += 0.2;
            //    //if (databaseNumber > 1)
            //    //    totalScore += 0.5 * (1.0 + (double)(databaseNumber - 1) / 14.0);
            //    if (databaseNumber > 1) totalScore += 1.0;
            //}
            //else {
            //    //if (databaseNumber > 0)
            //    //    totalScore += 0.5 * (1.0 + (double)databaseNumber / 14.0);
            //    if (databaseNumber > 0) totalScore += 1.0;
            //}
            //return totalScore;
        }

        //by Jaccard index
        public static double CalculateSubstructureScore(Structure structure, List<string> msmsOntologies, int uniqueOntCount)
        {
            var subOntologies = structure.SubstructureOntologies;

            var intersectionSize = 0;
            foreach (var ontology in subOntologies) {
                if (msmsOntologies.Contains(ontology))
                    intersectionSize++;
            }

            var unionSize = subOntologies.Count + uniqueOntCount - intersectionSize;
            //var score = (double)intersectionSize / (double)unionSize / 0.5556;
            var score = (double)intersectionSize / (double)unionSize;
            if (score >= 1.0) return 1.0;
            else if (score < 0) return 0.0;
            else return score;
        }

        public static List<string> GetMonitoredSubstructureOntologies(List<ProductIon> productIons, List<NeutralLoss> neutralLosses, 
            List<FragmentOntology> fragmentOntlogies, out int uniqueOntologiesCount)
        {
            var monitoredOntologies = new List<string>();
            var uniqueOntologies = new List<string>();
            foreach (var ion in productIons.Where(n => n.CandidateOntologies.Count > 0)) {
                var flg = false;
                foreach (var ontology in ion.CandidateOntologies) {

                    if (!monitoredOntologies.Contains(ontology) && fragmentOntlogies.Count(n => n.ChemOntID == ontology) >= 1)
                        monitoredOntologies.Add(ontology);

                    foreach (var uOntology in uniqueOntologies) {
                        if (uOntology.Contains(ontology)) {
                            flg = true;
                            break;
                        }
                    }
                    if (flg) break;
                }

                if (!flg) {
                    var uOntologyString = string.Empty;
                    foreach (var ontology in ion.CandidateOntologies) {
                        uOntologyString += ontology + ";";
                    }
                    uniqueOntologies.Add(uOntologyString);
                }
            }

            foreach (var ion in neutralLosses) {
                var flg = false;
                foreach (var ontology in ion.CandidateOntologies) {

                    if (!monitoredOntologies.Contains(ontology) && fragmentOntlogies.Count(n => n.ChemOntID == ontology) >= 1)
                        monitoredOntologies.Add(ontology);

                    foreach (var uOntology in uniqueOntologies) {
                        if (uOntology.Contains(ontology)) {
                            flg = true;
                            break;
                        }
                    }
                    if (flg) break;
                }

                if (!flg) {
                    var uOntologyString = string.Empty;
                    foreach (var ontology in ion.CandidateOntologies) {
                        uOntologyString += ontology + ";";
                    }
                    uniqueOntologies.Add(uOntologyString);
                }
            }

            uniqueOntologiesCount = uniqueOntologies.Count;

            return monitoredOntologies;
        }
    }
}

using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Ion;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Mathematics.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.FormulaGenerator.Function {
    public sealed class Scoring
    {
        private Scoring() { }

        public static double MassDifferenceScore(double diff, double devi)
        {
            return BasicMathematics.StandadizedGaussianFunction(diff, devi);
        }

        public static double IsotopicDifferenceScore(double m1Diff, double m2Diff, double devi)
        {
            return (BasicMathematics.StandadizedGaussianFunction(m1Diff, devi) + BasicMathematics.StandadizedGaussianFunction(m2Diff, devi)) * 0.5;
        }

        public static double FragmentHitsScore(List<SpectrumPeak> peaklist, List<ProductIon> productIons, double ms2Tol, MassToleranceType massTolType)
        {
            var devi = 0.0;
            if (peaklist.Count == 0) return 0;

            var monoisotopicCount = peaklist.Count(n => n.Comment == "M");

            if (peaklist != null && peaklist.Count != 0)
            {
                double totalDiff = 0;
                foreach (var fragment in productIons)
                {
                    totalDiff += Math.Abs(fragment.MassDiff);
                    if (massTolType == MassToleranceType.Da)
                        devi += ms2Tol;
                    else
                        devi += MolecularFormulaUtility.ConvertPpmToMassAccuracy(fragment.Mass, ms2Tol);
                }
                if (productIons.Count == 0) return 0;
                else {
                    return (double)productIons.Count / (double)monoisotopicCount;
                    //return (double)productIons.Count / (double)peaklist.Count * BasicMathematics.StandadizedGaussianFunction(totalDiff, devi);
                }
            }
            else
            {
                return 0;
            }
        }

        public static double NeutralLossScore(List<NeutralLoss> neutralLossResult, double ms2Tol, MassToleranceType massTolType, double neutralLossNum)
        {
            var devi = 0.0;
            if (neutralLossResult.Count != 0)
            {
                double totalScore = 0;
                foreach (var nloss in neutralLossResult) {
                    if (massTolType == MassToleranceType.Da)
                        devi = ms2Tol;
                    else
                        devi = MolecularFormulaUtility.ConvertPpmToMassAccuracy(nloss.PrecursorMz, ms2Tol);

                    totalScore += BasicMathematics.StandadizedGaussianFunction(nloss.MassError, devi);
                }
                return (double)neutralLossResult.Count / (double)neutralLossNum;
                //return totalScore / (double)neutralLossNum;
            }
            else
            {
                return 0;
            }
        }

        public static double NeutralLossScore(int hits, int totalCount) {
            if (totalCount <= 0) return 0.0;

            var hitsDouble = (double)hits;
            var countDouble = (double)totalCount;
            return hitsDouble / countDouble;
        }


        public static double DatabaseScore(int recordNum, string recordName)
        {
            double databaseScore = 0.0;
            var isMineIncluded = false; if (recordName != null && recordName.Contains("MINE")) isMineIncluded = true;

            var recordNumber = recordNum; if (isMineIncluded) recordNumber -= 1;

            if (recordNumber > 0)
                databaseScore = 0.5 * (1.0 + (double)recordNum / 21.0);

            if (isMineIncluded) databaseScore += 0.2;
            return databaseScore;
        }

        public static double TotalScore(FormulaResult result)
        {
            //double score = 6.4305 * result.MassDiffScore + 0.3285 * result.IsotopicScore + 2.9103 * result.ProductIonScore + 0.7190 * result.NeutralLossScore + 10.7509 * DatabaseScore(result.ResourceRecords);
            double score = result.MassDiffScore + result.IsotopicScore + result.ProductIonScore + result.NeutralLossScore + DatabaseScore(result.ResourceRecords, result.ResourceNames);
            //double score = DatabaseScore(result.ResourceRecords, result.ResourceNames);
            return score;
        }
    }
}

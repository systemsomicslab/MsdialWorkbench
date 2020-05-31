using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialDimsCore.Common {
    public sealed class AnnotationProcess {
        private AnnotationProcess() { }

        public static void Run(ChromatogramPeakFeature feature, List<MoleculeMsReference> mspDB, MsRefSearchParameterBase param, TargetOmics omics) {
            var mz = feature.PrecursorMz;
            var ms1Tol = param.Ms1Tolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms1Tol));
            #region // practical parameter changes
            if (mz > 500) {
                ms1Tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(mz, ppm);
            }
            #endregion

            var startIndex = DataAccess.GetDatabaseStartIndex(mz, ms1Tol, mspDB);
            for (int i = startIndex; i < mspDB.Count; i++) {
                var query = mspDB[i];
                if (query.PrecursorMz > mz + ms1Tol) break;
                if (query.PrecursorMz < mz - ms1Tol) continue;

                var result = MsScanMatching.CompareMS2LipidomicsScanProperties(feature, query, param, omics);
                if (result.IsSpectrumMatch) {
                    feature.MspIDs.Add(i);
                }
                else {
                    continue;
                }

                //temp method
                var totalscore = result.SimpleDotProduct + result.WeightedDotProduct + result.MatchedPeaksPercentage + result.ReverseDotProduct;
                result.TotalSimilarity = totalscore;

                if (feature.MspBasedMatchResult.TotalSimilarity < totalscore) {
                    feature.MspID = i;
                    feature.MspBasedMatchResult = result;
                }
            }

            if (feature.MspBasedMatchResult.LibraryID >= 0) {
                var mspID = feature.MspID;
                var refQuery = mspDB[mspID];
                feature.Name = refQuery.Name;
                feature.Formula = refQuery.Formula;
                feature.Ontology = refQuery.Ontology;
                feature.SMILES = refQuery.SMILES;
                feature.InChIKey = refQuery.InChIKey;
            }
            else {
                feature.Name = "Unknown";
            }
        }
    }
}

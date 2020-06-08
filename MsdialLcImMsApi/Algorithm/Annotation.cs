using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcImMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialLcImMsApi.Algorithm {
    public class Annotation {
        // mspDB must be sorted by precursor mz
        // textDB must be sorted by precursor mz
        // ccs must be calculated before this processing
        public void MainProcess(List<RawSpectrum> spectrumList, List<RawSpectrum> accumulatedSpecList,
            List<ChromatogramPeakFeature> chromPeakFeatures, List<MSDecResult> msdecResults, List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB,
            MsdialLcImMsParameter param, Action<int> reportAction) {

            Console.WriteLine("Annotation started");
            for (int i = 0; i < chromPeakFeatures.Count; i++) {
                // count of chrompeakfeatures and msdecresults should be same
                var chromPeak = chromPeakFeatures[i];

                if (chromPeak.PeakCharacter.IsotopeWeightNumber == 0) {
                    LcImMsMsMatchMethod(chromPeak, msdecResults, spectrumList, accumulatedSpecList, mspDB, textDB, param);
                }
                Console.WriteLine("Done {0}/{1}", i, chromPeakFeatures.Count);
            }
        }

        private void LcImMsMsMatchMethod(ChromatogramPeakFeature chromPeak, List<MSDecResult> msdecResults, List<RawSpectrum> spectrumList, List<RawSpectrum> accumulatedSpecList,
            List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB, MsdialLcImMsParameter param) {
            var isotopes = DataAccess.GetIsotopicPeaks(accumulatedSpecList, chromPeak.MS1AccumulatedMs1RawSpectrumIdTop, (float)chromPeak.Mass, param.CentroidMs1Tolerance);

            var maxMspMatchedDriftSpotID = -1;
            var maxMspMatchedScore = -1.0;
            var maxTextDBMatchedDriftSpotID = -1;
            var maxTextDBMatchedScore = -1.0;

            foreach (var driftPeak in chromPeak.DriftChromFeatures.OrEmptyIfNull()) {
                var msdecResult = msdecResults[driftPeak.MasterPeakID];
                LcImMsMsMatchMethod(chromPeak, driftPeak, msdecResult, isotopes, mspDB, textDB, param);

                if (driftPeak.MspID >= 0 && maxMspMatchedScore > driftPeak.MspBasedMatchResult.TotalScore) {
                    maxMspMatchedScore = driftPeak.MspBasedMatchResult.TotalScore;
                    maxMspMatchedDriftSpotID = driftPeak.PeakID;
                }

                if (driftPeak.TextDbID >= 0 && maxTextDBMatchedScore > driftPeak.TextDbBasedMatchResult.TotalScore) {
                    maxTextDBMatchedScore = driftPeak.TextDbBasedMatchResult.TotalScore;
                    maxTextDBMatchedDriftSpotID = driftPeak.PeakID;
                }
            }

            if (maxMspMatchedScore >= 0) {
                chromPeak.MspBasedMatchResult = chromPeak.DriftChromFeatures[maxMspMatchedDriftSpotID].MspBasedMatchResult.Clone();
                chromPeak.MspID = chromPeak.DriftChromFeatures[maxMspMatchedDriftSpotID].MspID;
            }

            if (maxTextDBMatchedDriftSpotID >= 0) {
                chromPeak.TextDbBasedMatchResult = chromPeak.DriftChromFeatures[maxTextDBMatchedDriftSpotID].TextDbBasedMatchResult.Clone();
                chromPeak.TextDbID = chromPeak.DriftChromFeatures[maxTextDBMatchedDriftSpotID].TextDbID;
            }
        }

        private void LcImMsMsMatchMethod(ChromatogramPeakFeature chromPeak, ChromatogramPeakFeature driftPeak, MSDecResult msdecResult, 
            List<IsotopicPeak> isotopes, List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB, MsdialLcImMsParameter param) {
            var normMSScanProp = DataAccess.GetNormalizedMSScanProperty(chromPeak, msdecResult, param);

            var mz = chromPeak.Mass;
            var ccs = driftPeak.CollisionCrossSection;
            var ms1Tol = param.MspSearchParam.Ms1Tolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms1Tol));
            if (mz > 500) {
                ms1Tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(mz, ppm);
            }

            if (!mspDB.IsEmptyOrNull()) {
                var startID = SearchCollection.LowerBound(mspDB,
                    new MoleculeMsReference() { PrecursorMz = chromPeak.Mass - ms1Tol },
                    (a, b) => a.PrecursorMz.CompareTo(b.PrecursorMz));
                var candidates = new List<MsScanMatchResult>();
                for (int i = startID; i < mspDB.Count; i++) {
                    var refSpec = mspDB[i];
                    if (refSpec.PrecursorMz > mz + ms1Tol) break;
                    if (refSpec.PrecursorMz < mz - ms1Tol) continue;

                    MsScanMatchResult result = null;
                    if (param.TargetOmics == Common.Enum.TargetOmics.Metablomics) {
                        result = MsScanMatching.CompareIMMS2ScanProperties(msdecResult, refSpec, param.MspSearchParam, ccs, isotopes, refSpec.IsotopicPeaks);
                    }
                    else if (param.TargetOmics == Common.Enum.TargetOmics.Lipidomics) {
                        result = MsScanMatching.CompareIMMS2LipidomicsScanProperties(msdecResult, refSpec, param.MspSearchParam, ccs, isotopes, refSpec.IsotopicPeaks);
                    }
                    if (result.IsSpectrumMatch || result.IsPrecursorMzMatch) {
                        result.LibraryIDWhenOrdered = i;
                        candidates.Add(result);
                    }
                }

                foreach (var (result, index) in candidates.OrEmptyIfNull().OrderByDescending(n => n.TotalScore).WithIndex()) {
                    if (index == 0) {
                        driftPeak.MspBasedMatchResult = result;
                        driftPeak.MspID = result.LibraryID;
                        driftPeak.MspIDWhenOrdered = result.LibraryIDWhenOrdered;
                    }
                    driftPeak.MspIDs.Add(result.LibraryID);
                }
            }

            if (!textDB.IsEmptyOrNull()) {
                var startID = SearchCollection.LowerBound(textDB,
                    new MoleculeMsReference() { PrecursorMz = chromPeak.Mass - ms1Tol },
                    (a, b) => a.PrecursorMz.CompareTo(b.PrecursorMz));
                var candidates = new List<MsScanMatchResult>();
                for (int i = startID; i < mspDB.Count; i++) {
                    var refSpec = mspDB[i];
                    if (refSpec.PrecursorMz > mz + ms1Tol) break;
                    if (refSpec.PrecursorMz < mz - ms1Tol) continue;

                    var result = MsScanMatching.CompareMS2ScanProperties(msdecResult, refSpec, param.MspSearchParam, isotopes, refSpec.IsotopicPeaks);
                    if (result.IsPrecursorMzMatch) {
                        result.LibraryIDWhenOrdered = i;
                        candidates.Add(result);
                    }
                }

                foreach (var (result, index) in candidates.OrEmptyIfNull().OrderByDescending(n => n.TotalScore).WithIndex()) {
                    if (index == 0) {
                        driftPeak.TextDbBasedMatchResult = result;
                        driftPeak.TextDbID = result.LibraryID;
                        driftPeak.TextDbIDWhenOrdered = result.LibraryIDWhenOrdered;
                    }
                    driftPeak.TextDbIDs.Add(result.LibraryID);
                }
            }
        }
    }
}

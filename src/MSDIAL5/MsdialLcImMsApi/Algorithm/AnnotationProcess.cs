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
    public class AnnotationProcess {
        public double InitialProgress { get; set; } = 60.0;
        public double ProgressMax { get; set; } = 30.0;

        public AnnotationProcess(double InitialProgress, double ProgressMax) {
            this.InitialProgress = InitialProgress;
            this.ProgressMax = ProgressMax;
        }
        // mspDB must be sorted by precursor mz
        // textDB must be sorted by precursor mz
        // ccs must be calculated before this processing
        public void MainProcess(List<RawSpectrum> spectrumList, List<RawSpectrum> accumulatedSpecList,
            List<ChromatogramPeakFeature> chromPeakFeatures, List<MSDecResult> msdecResults, List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB,
            MsdialLcImMsParameter param, Action<int> reportAction) {

            Console.WriteLine("Annotation started");
            for (int i = 0; i < chromPeakFeatures.Count; i++) {
                // count of chrompeakfeatures and msdecresults should be same
                LcImMsMsMatchMethod(chromPeakFeatures[i], msdecResults, spectrumList, accumulatedSpecList, mspDB, textDB, param);
                Console.WriteLine("Done {0}/{1}", i, chromPeakFeatures.Count);
            }
        }

        private void LcImMsMsMatchMethod(ChromatogramPeakFeature chromPeak, List<MSDecResult> msdecResults, List<RawSpectrum> spectrumList, List<RawSpectrum> accumulatedSpecList,
            List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB, MsdialLcImMsParameter param) {
            var isotopes = DataAccess.GetIsotopicPeaks(accumulatedSpecList, chromPeak.MS1AccumulatedMs1RawSpectrumIdTop, (float)chromPeak.Mass, param.CentroidMs1Tolerance, param.PeakPickBaseParam.MaxIsotopesDetectedInMs1Spectrum);

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
                var result = chromPeak.DriftChromFeatures[maxMspMatchedDriftSpotID].MspBasedMatchResult.Clone();
                chromPeak.MSRawID2MspBasedMatchResult[msdecResults[chromPeak.MasterPeakID].RawSpectrumID] = result;
                chromPeak.MatchResults.AddMspResult(msdecResults[chromPeak.MasterPeakID].RawSpectrumID, result);
                DataAccess.SetMoleculeMsProperty(chromPeak, mspDB[chromPeak.MspIDWhenOrdered], chromPeak.MspBasedMatchResult);
            }

            if (maxTextDBMatchedDriftSpotID >= 0) {
                var result = chromPeak.DriftChromFeatures[maxTextDBMatchedDriftSpotID].TextDbBasedMatchResult.Clone();
                chromPeak.TextDbBasedMatchResult = result;
                chromPeak.MatchResults.AddTextDbResult(result);
                DataAccess.SetMoleculeMsProperty(chromPeak, textDB[chromPeak.TextDbIDWhenOrdered], chromPeak.TextDbBasedMatchResult, true);
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

                    MsScanMatchResult result = MsScanMatching.CompareMS2ScanProperties(msdecResult, refSpec, param.MspSearchParam,
                       param.TargetOmics, ccs, isotopes, refSpec.IsotopicPeaks, param.AndromedaDelta, param.AndromedaMaxPeaks);
                    //if (param.TargetOmics == Common.Enum.TargetOmics.Metabolomics) {
                    //    result = MsScanMatching.CompareIMMS2ScanProperties(msdecResult, refSpec, param.MspSearchParam, ccs, isotopes, refSpec.IsotopicPeaks);
                    //}
                    //else if (param.TargetOmics == Common.Enum.TargetOmics.Lipidomics) {
                    //    result = MsScanMatching.CompareIMMS2LipidomicsScanProperties(msdecResult, refSpec, param.MspSearchParam, ccs, isotopes, refSpec.IsotopicPeaks);
                    //}
                    result.Source = SourceType.MspDB;
                    if (result.IsSpectrumMatch || result.IsPrecursorMzMatch) {
                        result.LibraryIDWhenOrdered = i;
                        candidates.Add(result);
                    }
                }

                foreach (var (result, index) in candidates.OrEmptyIfNull().OrderByDescending(n => n.TotalScore).WithIndex()) {
                    if (index == 0) {
                        driftPeak.MSRawID2MspBasedMatchResult[msdecResult.RawSpectrumID] = result;
                        driftPeak.MatchResults.AddMspResult(msdecResult.RawSpectrumID, result);
                        DataAccess.SetMoleculeMsProperty(driftPeak, mspDB[result.LibraryIDWhenOrdered], result);

                        driftPeak.MSRawID2MspIDs[msdecResult.RawSpectrumID] = new List<int>();
                    }
                    driftPeak.MSRawID2MspIDs[msdecResult.RawSpectrumID].Add(result.LibraryID);
                }
            }

            if (!textDB.IsEmptyOrNull()) {
                var startID = SearchCollection.LowerBound(textDB,
                    new MoleculeMsReference() { PrecursorMz = chromPeak.Mass - ms1Tol },
                    (a, b) => a.PrecursorMz.CompareTo(b.PrecursorMz));
                var candidates = new List<MsScanMatchResult>();
                for (int i = startID; i < textDB.Count; i++) {
                    var refSpec = textDB[i];
                    if (refSpec.PrecursorMz > mz + ms1Tol) break;
                    if (refSpec.PrecursorMz < mz - ms1Tol) continue;

                    var result = MsScanMatching.CompareMS2ScanProperties(msdecResult, refSpec, param.MspSearchParam);
                    result.Source = SourceType.TextDB;
                    if (result.IsPrecursorMzMatch) {
                        result.LibraryIDWhenOrdered = i;
                        candidates.Add(result);
                    }
                }

                foreach (var (result, index) in candidates.OrEmptyIfNull().OrderByDescending(n => n.TotalScore).WithIndex()) {
                    if (index == 0) {
                        driftPeak.TextDbBasedMatchResult = result;
                        DataAccess.SetMoleculeMsProperty(driftPeak, textDB[result.LibraryIDWhenOrdered], result, true);
                    }
                    driftPeak.TextDbIDs.Add(result.LibraryID);
                    driftPeak.MatchResults.AddTextDbResult(result);
                }
            }
        }
    }
}

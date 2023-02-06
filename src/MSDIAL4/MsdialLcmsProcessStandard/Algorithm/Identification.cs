using Msdial.Lcms.Dataprocess.Utility;
using Msdial.Lcms.Dataprocess.Algorithm;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Msdial.Lcms.Dataprocess.Scoring;
using System.Diagnostics;
using CompMs.Common.DataObj;

namespace Msdial.Lcms.Dataprocess.Algorithm
{
    
    public class MatchedCandidate {
        public int MspID { get; set; }
        public string AnnotatedName { get; set; }
        public string InChIKey { get; set; }
        public float TotalScore { get; set; }
        public float DotProduct { get; set; }
        public float RevDotProduct { get; set; }
        public float PresenseScore { get; set; }
        public float MzSimilarity { get; set; }
        public float RtSimilarity { get; set; }
        public float CcsSimilarity { get; set; }
        public float IsotopicSimilarity { get; set; }

        public bool IsMs1Match { get; set; }
        public bool IsMs2Match { get; set; }
        public bool IsRtMatch { get; set; }
        public bool IsCcsMatch { get; set; }
        public bool IsLipidClassMatch { get; set; }
        public bool IsLipidChainsMatch { get; set; }
        public bool IsLipidPositionMatch { get; set; }
        public bool IsOtherLipidMatch { get; set; }


    }

    public sealed class Identification
    {
        private Identification() { }

        private const double initialProgress = 50.0;
        private const double progressMax = 30.0;

        /// <summary>
        /// This program is to perform compound identifications by means of RT, m/z, isotopic ratio, and MS/MS similarity. See also 'SimilarityScoring.cs'
        /// The identification process is based on the MSP queries.
        /// </summary>
        /// <param name="file">
        /// Add the file path of .DCL file storing the MS/MS spectrum information assigned to each detected peak spot.
        /// </param>
        /// <param name="spectrumCollection"></param>
        /// <param name="peakAreas"></param>
        /// <param name="mspDB"></param>
        /// <param name="param"></param>
        /// <param name="projectProp"></param>
        public static void CompoundIdentification(string file, ObservableCollection<RawSpectrum> spectrumCollection, 
            List<PeakAreaBean> peakAreas, List<MspFormatCompoundInformationBean> mspDB, 
            AnalysisParametersBean param, ProjectPropertyBean projectProp, Action<int> reportAction)
        {
            if (mspDB == null || mspDB.Count == 0) return;

            var ms1Spectra = new ObservableCollection<double[]>();
            var ms2Spectra = new ObservableCollection<double[]>();
            var ms2DecResult = new MS2DecResult();

            var seekpointList = new List<long>();

            using (var fs = File.Open(file, FileMode.Open, FileAccess.ReadWrite))
            {
                seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

    //            mspFormatCompoundInformationBeanList = mspFormatCompoundInformationBeanList.OrderBy(n => n.PrecursorMz).ToList();

                for (int i = 0; i < peakAreas.Count; i++)
                {
                    var peakSpot = peakAreas[i];
                    if (peakSpot.IsotopeWeightNumber != 0) continue;
                    if (param.IsIonMobility)
                        executeIonMobilityIdentificationProcess(fs, seekpointList, spectrumCollection, peakSpot, mspDB, param, projectProp, reportAction);
                    else
                        executeConventionalIdentificationProcess(fs, seekpointList, spectrumCollection, peakSpot, mspDB, param, projectProp, reportAction);
                }
            }

            if (param.OnlyReportTopHitForPostAnnotation) {
                if (param.IsIonMobility)
                    peakAreas = getRefinedPeakAndDriftSpotListForMspSearchResult(peakAreas);
                else
                    peakAreas = getRefinedPeakAreaBeanListForMspSearchResult(peakAreas);
            }

            //       mspFormatCompoundInformationBeanList = mspFormatCompoundInformationBeanList.OrderBy(n => n.Id).ToList();
        }

        public static void executeConventionalIdentificationProcess(FileStream fs, List<long> seekpointList, ObservableCollection<RawSpectrum> spectrumCollection, 
            PeakAreaBean peakSpot, List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param, ProjectPropertyBean projectProp, Action<int> reportAction) {
            //var ms1Spectra = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection, projectProp.DataType, peakAreas[i].Ms1LevelDatapointNumber, param.CentroidMs1Tolerance, param.PeakDetectionBasedCentroid);
            var ms1Spectra = new ObservableCollection<double[]>();
            var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, peakSpot.PeakID);
            if (ms2DecResult.MassSpectra.Count != 0) {
                var ms2Spectra = getNormalizedMs2Spectra(ms2DecResult.MassSpectra, param);
                if (ms2Spectra != null && ms2Spectra.Count != 0)
                    similarityCalculationsMsMsIncluded(peakSpot, ms1Spectra, ms2Spectra, mspDB, param, projectProp);
            }
            else {
                similarityCalculationsWithoutMsMs(peakSpot, ms1Spectra, mspDB, param);
            }
            //Console.WriteLine(peakSpot.MetaboliteName);
            progressReports(peakSpot.PeakID, seekpointList.Count, reportAction);
        }

        public static void executeIonMobilityIdentificationProcess(FileStream fs, List<long> seekpointList,
            ObservableCollection<RawSpectrum> spectrumCollection, PeakAreaBean peakSpot, List<MspFormatCompoundInformationBean> mspDB,
            AnalysisParametersBean param, ProjectPropertyBean projectProp, Action<int> reportAction) {

            //Debug.WriteLine(peakSpot.MasterPeakID);
            //if (peakSpot.MasterPeakID > 1346) {
            //    Debug.WriteLine("check point");
            //}
            //if (Math.Abs(peakSpot.AccurateMass - 791.489) < 0.01 && Math.Abs(peakSpot.RtAtPeakTop - 4.317) < 0.1) {
            //    Debug.WriteLine("check point");
            //}
            // identification process for rt-mz axis peak
            var ms1Spectra = new ObservableCollection<double[]>();
            //var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, peakSpot.MasterPeakID);
            //if (ms2DecResult.MassSpectra.Count != 0) {
            //    var ms2Spectra = getNormalizedMs2Spectra(ms2DecResult.MassSpectra, param);
            //    if (ms2Spectra != null && ms2Spectra.Count != 0)
            //        similarityCalculationsMsMsIncluded(peakSpot, ms1Spectra, ms2Spectra, mspDB, param, projectProp);
            //}
            //else {
            //    similarityCalculationsWithoutMsMs(peakSpot, ms1Spectra, mspDB, param);
            //}

            //progressReports(peakSpot.MasterPeakID, seekpointList.Count, bgWorker);

            var maxIdentScore = -1.0F;
            var maxIdentID = -1;
            var maxAnnotateScore = -1.0F;
            var maxAnnotateID = -1;

            for (int i = 0; i < peakSpot.DriftSpots.Count; i++) {
                var dSpot = peakSpot.DriftSpots[i];
                //Debug.WriteLine(dSpot.MasterPeakID);
                //if (dSpot.MasterPeakID > 1346) {
                //    Debug.WriteLine("check point");
                //}
                var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, dSpot.MasterPeakID);
                if (ms2DecResult.MassSpectra.Count != 0) {
                    var ms2Spectra = getNormalizedMs2Spectra(ms2DecResult.MassSpectra, param);
                    if (ms2Spectra != null && ms2Spectra.Count != 0)
                        similarityCalculationsIonMobilityMsMsIncluded(dSpot, peakSpot, ms1Spectra, ms2Spectra, mspDB, param, projectProp);
                }
                else {
                    similarityCalculationsIonMobilityWithoutMsMs(peakSpot, dSpot, ms1Spectra, mspDB, param);
                }

                if (dSpot.LibraryID >= 0 && !dSpot.MetaboliteName.Contains("w/o")) {
                    if (dSpot.TotalScore > maxIdentScore) {
                        maxIdentScore = dSpot.TotalScore;
                        maxIdentID = i;
                    }
                }

                if (dSpot.LibraryID >= 0 && dSpot.MetaboliteName.Contains("w/o")) {
                    if (dSpot.TotalScore > maxAnnotateScore) {
                        maxAnnotateScore = dSpot.TotalScore;
                        maxAnnotateID = i;
                    }
                }
                progressReports(dSpot.MasterPeakID, seekpointList.Count, reportAction);
            }

            if (maxIdentID >= 0) {
                CopyMaxDriftSpotInfoToPeakSpot(peakSpot.DriftSpots[maxIdentID], peakSpot);
            }
            else if (maxAnnotateID >= 0) {
                CopyMaxDriftSpotInfoToPeakSpot(peakSpot.DriftSpots[maxAnnotateID], peakSpot);
            }
            else {
                setDefaultCompoundInformation(peakSpot);
            }
        }

        public static void CopyMaxDriftSpotInfoToPeakSpot(DriftSpotBean driftSpot, PeakAreaBean peakSpot) {
            peakSpot.MetaboliteName = driftSpot.MetaboliteName;
            peakSpot.LibraryID = driftSpot.LibraryID;
            peakSpot.Inchikey = driftSpot.Inchikey;
            peakSpot.PostIdentificationLibraryId = driftSpot.PostIdentificationLibraryId;
            peakSpot.MassSpectraSimilarityValue = driftSpot.MassSpectraSimilarityValue;
            peakSpot.ReverseSearchSimilarityValue = driftSpot.ReverseSearchSimilarityValue;
            peakSpot.PresenseSimilarityValue = driftSpot.PresenseSimilarityValue;
            peakSpot.AccurateMassSimilarity = driftSpot.AccurateMassSimilarity;
            peakSpot.RtSimilarityValue = driftSpot.RtSimilarityValue;
            peakSpot.CcsSimilarity = driftSpot.CcsSimilarity;
            peakSpot.IsotopeSimilarityValue = driftSpot.IsotopeSimilarityValue;
            peakSpot.TotalScore = driftSpot.TotalScore;
            peakSpot.AdductIonName = driftSpot.AdductIonName;
            peakSpot.AdductIonXmer = driftSpot.AdductIonXmer;
            peakSpot.AdductIonAccurateMass = driftSpot.AdductIonAccurateMass;
            peakSpot.AdductParent = driftSpot.AdductParent;
            peakSpot.AdductIonChargeNumber = driftSpot.AdductIonChargeNumber;
            peakSpot.IsMs1Match = driftSpot.IsMs1Match;
            peakSpot.IsMs2Match = driftSpot.IsMs2Match;
            peakSpot.IsCcsMatch = driftSpot.IsCcsMatch;
            //Console.WriteLine(peakSpot.IsCcsMatch);
            peakSpot.IsRtMatch = driftSpot.IsRtMatch;
            peakSpot.IsLipidClassMatch = driftSpot.IsLipidClassMatch;
            peakSpot.IsLipidChainsMatch = driftSpot.IsLipidChainsMatch;
            peakSpot.IsLipidPositionMatch = driftSpot.IsLipidPositionMatch;
            peakSpot.IsOtherLipidMatch = driftSpot.IsOtherLipidMatch;
        }

        private static List<PeakAreaBean> getRefinedPeakAreaBeanListForMspSearchResult(List<PeakAreaBean> peakAreaBeanList)
        {
            if (peakAreaBeanList.Count <= 1) return peakAreaBeanList;
            peakAreaBeanList = peakAreaBeanList.OrderByDescending(n => n.LibraryID).OrderBy(n => n.MetaboliteName).ToList();

            var currentLibraryId = peakAreaBeanList[0].LibraryID;
            var currentMetName = peakAreaBeanList[0].MetaboliteName;
            var currentPeakId = 0;
            for (int i = 1; i < peakAreaBeanList.Count; i++) {
                if (peakAreaBeanList[i].LibraryID < 0) break;
                if (peakAreaBeanList[i].LibraryID != currentLibraryId || peakAreaBeanList[i].MetaboliteName != currentMetName) {
                    currentLibraryId = peakAreaBeanList[i].LibraryID;
                    currentMetName = peakAreaBeanList[i].MetaboliteName;
                    currentPeakId = i;
                    continue;
                }
                else {
                    if (peakAreaBeanList[currentPeakId].TotalScore < peakAreaBeanList[i].TotalScore) {
                        setDefaultCompoundInformation(peakAreaBeanList[currentPeakId]);
                        currentPeakId = i;
                    }
                    else {
                        setDefaultCompoundInformation(peakAreaBeanList[i]);
                    }
                }
            }

            peakAreaBeanList = peakAreaBeanList.OrderBy(n => n.PeakID).ToList();
            return peakAreaBeanList;
        }

        private static List<PeakAreaBean> getRefinedPeakAndDriftSpotListForMspSearchResult(List<PeakAreaBean> peakSpots) {
            if (peakSpots.Count <= 1) return peakSpots;
            var allDriftSpots = new List<DriftSpotBean>();
            foreach (var peak in peakSpots) {
                foreach (var drift in peak.DriftSpots) {
                    allDriftSpots.Add(drift);
                }
            }

            allDriftSpots = allDriftSpots.OrderByDescending(n => n.LibraryID).ToList();

            var currentLibraryId = allDriftSpots[0].LibraryID;
            var currentPeakId = 0;
            for (int i = 1; i < allDriftSpots.Count; i++) {
                if (allDriftSpots[i].LibraryID < 0) break;
                if (allDriftSpots[i].LibraryID != currentLibraryId) {
                    currentLibraryId = allDriftSpots[i].LibraryID;
                    currentPeakId = i;
                    continue;
                }
                else {
                    if (allDriftSpots[currentPeakId].TotalScore < allDriftSpots[i].TotalScore) {
                        setDefaultCompoundInformation(allDriftSpots[currentPeakId]);
                        currentPeakId = i;
                    }
                    else {
                        setDefaultCompoundInformation(allDriftSpots[i]);
                    }
                }
            }

            foreach (var peakSpot in peakSpots) {
                var maxIdentScore = -1.0F;
                var maxIdentID = -1;
                var maxAnnotateScore = -1.0F;
                var maxAnnotateID = -1;

                for (int i = 0; i < peakSpot.DriftSpots.Count; i++) {
                    var dSpot = peakSpot.DriftSpots[i];
                    if (dSpot.LibraryID >= 0 && !dSpot.MetaboliteName.Contains("w/o")) {
                        if (dSpot.TotalScore > maxIdentScore) {
                            maxIdentScore = dSpot.TotalScore;
                            maxIdentID = i;
                        }
                    }

                    if (dSpot.LibraryID >= 0 && dSpot.MetaboliteName.Contains("w/o")) {
                        if (dSpot.TotalScore > maxAnnotateScore) {
                            maxAnnotateScore = dSpot.TotalScore;
                            maxAnnotateID = i;
                        }
                    }
                }

                if (maxIdentID >= 0) {
                    CopyMaxDriftSpotInfoToPeakSpot(peakSpot.DriftSpots[maxIdentID], peakSpot);
                }
                else if (maxAnnotateID >= 0) {
                    CopyMaxDriftSpotInfoToPeakSpot(peakSpot.DriftSpots[maxAnnotateID], peakSpot);
                }
                else {
                    setDefaultCompoundInformation(peakSpot);
                }
            }

            peakSpots = peakSpots.OrderBy(n => n.PeakID).ToList();
            return peakSpots;
        }

        private static void progressReports(int currentProgress, int maxProgress, Action<int> reportAction)
        {
            var progress = initialProgress + (double)currentProgress / (double)maxProgress * progressMax;
            reportAction?.Invoke((int)progress);
        }

        public static ObservableCollection<double[]> getNormalizedMs2Spectra(List<double[]> spectrum, AnalysisParametersBean param)
        {
            var cutoff = param.RelativeAbundanceCutOff;
            var massSpec = new ObservableCollection<double[]>();
            var maxIntensity = spectrum.Max(n => n[1]);
            foreach (var peak in spectrum) {
                if (peak[1] > maxIntensity * cutoff * 0.01) {
                    massSpec.Add(new double[] { peak[0], peak[1] / maxIntensity * 100 });
                }
            }
            return massSpec;
        }

        private static void similarityCalculationsWithoutMsMs(PeakAreaBean peakAreaBean, 
            ObservableCollection<double[]> ms1Spectra, 
            List<MspFormatCompoundInformationBean> mspDB, 
            AnalysisParametersBean param)
        {
            int maxSimilarityWithoutMsMsLibraryIndex = 0;
            var mz = peakAreaBean.AccurateMass;
            var rt = peakAreaBean.RtAtPeakTop;
            var rtTol = param.RetentionTimeLibrarySearchTolerance;
            var mzTol = param.Ms1LibrarySearchTolerance;
            double totalSimilarity2 = 0, rtSimilarity = 0, isotopeSimilarity = 0, accurateMassSimilarity = 0
                , maxRtSimilarity2 = double.MinValue, maxIsotopeSimilarity2 = double.MinValue
                , maxAccurateMassSimilarity2 = double.MinValue, maxTotalSimilarity2 = double.MinValue;

            var startIndex = DataAccessLcUtility.GetDatabaseStartIndex(mz, mzTol, mspDB);

            for (int i = startIndex; i < mspDB.Count; i++)
            {
                if (mspDB[i].PrecursorMz > mz + mzTol) break;
                if (mspDB[i].PrecursorMz < mz - mzTol) continue;

                rtSimilarity = -1; isotopeSimilarity = -1; accurateMassSimilarity = -1;

                accurateMassSimilarity = LcmsScoring.GetGaussianSimilarity(mz, mspDB[i].PrecursorMz, mzTol);
                if (mspDB[i].RetentionTime >= 0) rtSimilarity = LcmsScoring.GetGaussianSimilarity(peakAreaBean.RtAtPeakTop, mspDB[i].RetentionTime, rtTol);
                if (mspDB[i].IsotopeRatioList.Count != 0) isotopeSimilarity = LcmsScoring.GetIsotopeRatioSimilarity(ms1Spectra, mspDB[i].IsotopeRatioList, mz, rtTol);

                totalSimilarity2 = LcmsScoring.GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, isotopeSimilarity, param.IsUseRetentionInfoForIdentificationScoring);

                if (totalSimilarity2 > maxTotalSimilarity2) {
                    maxTotalSimilarity2 = totalSimilarity2;
                    maxAccurateMassSimilarity2 = accurateMassSimilarity;
                    maxRtSimilarity2 = rtSimilarity;
                    maxIsotopeSimilarity2 = isotopeSimilarity;
                    maxSimilarityWithoutMsMsLibraryIndex = i;
                }
            }

            if (maxTotalSimilarity2 * 100 > param.IdentificationScoreCutOff) {
                var refQuery = mspDB[maxSimilarityWithoutMsMsLibraryIndex];
                var rtDiff = Math.Abs(rt - refQuery.RetentionTime);

                setPeakSpotProperties(peakAreaBean, rtDiff, param, true, false);
                setAnnotatedInformation(peakAreaBean, mspDB[maxSimilarityWithoutMsMsLibraryIndex], maxAccurateMassSimilarity2, maxRtSimilarity2, -1, maxIsotopeSimilarity2, maxTotalSimilarity2);
            }
            else {
                setDefaultCompoundInformation(peakAreaBean);
            }
        }

        private static void similarityCalculationsIonMobilityWithoutMsMs(PeakAreaBean peakSpot, DriftSpotBean driftSpot,
           ObservableCollection<double[]> ms1Spectra,
           List<MspFormatCompoundInformationBean> refQueries,
           AnalysisParametersBean param) {
            int maxSimilarityWithoutMsMsLibraryIndex = 0;
            var mz = driftSpot.AccurateMass;
            var rt = peakSpot.RtAtPeakTop;
            var ccs = driftSpot.Ccs;
            var ms1Tol = param.Ms1LibrarySearchTolerance;
            var rtTol = param.RetentionTimeLibrarySearchTolerance;
            var ccsTol = param.CcsSearchTolerance;

            double totalSimilarity2 = 0, rtSimilarity = 0, ccsSimilarity = 0, isotopeSimilarity = 0, accurateMassSimilarity = 0
                , maxRtSimilarity2 = double.MinValue, maxCcsSimilarity2 = double.MinValue, maxIsotopeSimilarity2 = double.MinValue
                , maxAccurateMassSimilarity2 = double.MinValue, maxTotalSimilarity2 = double.MinValue;

            var startIndex = DataAccessLcUtility.GetDatabaseStartIndex(mz, ms1Tol, refQueries);

            for (int i = startIndex; i < refQueries.Count; i++) {
                if (refQueries[i].PrecursorMz > mz + ms1Tol) break;
                if (refQueries[i].PrecursorMz < mz - ms1Tol) continue;

                rtSimilarity = -1; isotopeSimilarity = -1; accurateMassSimilarity = -1; ccsSimilarity = -1;

                accurateMassSimilarity = LcmsScoring.GetGaussianSimilarity(mz, refQueries[i].PrecursorMz, ms1Tol);
                if (refQueries[i].RetentionTime >= 0) rtSimilarity = LcmsScoring.GetGaussianSimilarity(rt, refQueries[i].RetentionTime, rtTol);
                if (refQueries[i].CollisionCrossSection >= 0) ccsSimilarity = LcmsScoring.GetGaussianSimilarity(ccs, refQueries[i].CollisionCrossSection, ccsTol);
                if (refQueries[i].IsotopeRatioList.Count != 0) isotopeSimilarity = LcmsScoring.GetIsotopeRatioSimilarity(ms1Spectra, refQueries[i].IsotopeRatioList, mz, ms1Tol);

                totalSimilarity2 = LcmsScoring.GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, ccsSimilarity, isotopeSimilarity, param.IsUseRetentionInfoForIdentificationScoring, param.IsUseCcsForIdentificationScoring);

                if (totalSimilarity2 > maxTotalSimilarity2) {
                    maxTotalSimilarity2 = totalSimilarity2;
                    maxAccurateMassSimilarity2 = accurateMassSimilarity;
                    maxRtSimilarity2 = rtSimilarity;
                    maxCcsSimilarity2 = ccsSimilarity;
                    maxIsotopeSimilarity2 = isotopeSimilarity;
                    maxSimilarityWithoutMsMsLibraryIndex = i;
                }
            }

            if (maxTotalSimilarity2 * 100 > param.IdentificationScoreCutOff) {
                var refQuery = refQueries[maxSimilarityWithoutMsMsLibraryIndex];
                var rtDiff = Math.Abs(rt - refQuery.RetentionTime);
                var ccsDiff = Math.Abs(ccs - refQuery.CollisionCrossSection);

                setDriftSpotProperties(driftSpot, rtDiff, ccsDiff, param, true, false);
                setAnnotatedInformation(driftSpot, refQueries[maxSimilarityWithoutMsMsLibraryIndex], maxAccurateMassSimilarity2, maxRtSimilarity2, maxCcsSimilarity2, maxIsotopeSimilarity2, maxTotalSimilarity2);
            }
            else {
                setDefaultCompoundInformation(driftSpot);
            }
        }

        public static void similarityCalculationsMsMsIncluded(PeakAreaBean peakAreaBean, 
            ObservableCollection<double[]> ms1Spectra, ObservableCollection<double[]> ms2Spectra, 
            List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param, ProjectPropertyBean project)
        {
            int maxSpectraSimilarityLibraryIndex = 0, maxSimilarityWithoutMsMsLibraryIndex = 0;
            float mz = peakAreaBean.AccurateMass;
            float rt = peakAreaBean.RtAtPeakTop;

            //if (Math.Abs(mz - 285.0978) < 0.001 && Math.Abs(rt - 2.988167) < 0.01) {
            //    Console.WriteLine();
            //}

            //Console.WriteLine(peakAreaBean.PeakID);

            double maxTotalSimilarity1 = double.MinValue, maxSpectraSimilarity = double.MinValue, maxReverseSimilarity = double.MinValue, maxPresenseSimilarity = double.MinValue
                , maxRtSimilarity1 = double.MinValue, maxRtSimilarity2 = double.MinValue, maxIsotopeSimilarity1 = double.MinValue, maxIsotopeSimilarity2 = double.MinValue
                , maxAccurateMassSimilarity1 = double.MinValue, maxAccurateMassSimilarity2 = double.MinValue, maxTotalSimilarity2 = double.MinValue;

            var ms1Tol = param.Ms1LibrarySearchTolerance;
            var ms2Tol = param.Ms2LibrarySearchTolerance;
            var rtTol = param.RetentionTimeLibrarySearchTolerance;
            var massBegin = param.Ms2MassRangeBegin;
            var massEnd = param.Ms2MassRangeEnd;

            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms1Tol));
            #region // practical parameter changes
            if (mz > 500) {
                ms1Tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(mz, ppm);
            }
            #endregion


            int startIndex = DataAccessLcUtility.GetDatabaseStartIndex(mz, ms1Tol, mspDB);

            //if (peakAreaBean.Comment == "17_RIKEN IMS Sciex Cultured cells_Human cultured cell_HEK293_pos%102") {
            //    Console.WriteLine();
            //}

            for (int i = startIndex; i < mspDB.Count; i++)
            {
                var query = mspDB[i];
                if (query.PrecursorMz > mz + ms1Tol) break;
                if (query.PrecursorMz < mz - ms1Tol) continue;

                //if (peakAreaBean.Comment == "17_RIKEN IMS Sciex Cultured cells_Human cultured cell_HEK293_pos%102" && query.Name == "ACar 4:0; [M]+") {
                //    Console.WriteLine();
                //}
                if (query.RetentionTime >= 0 && param.IsUseRetentionInfoForIdentificationFiltering) {
                    var refRt = query.RetentionTime;
                    if (Math.Abs(refRt - rt) > param.RetentionTimeLibrarySearchTolerance) continue;
                }
                var rtSimilarity = -1.0;
                var isotopeSimilarity = -1.0; 

                var refSpec = query.MzIntensityCommentBeanList;
               
                var spectraSimilarity = LcmsScoring.GetMassSpectraSimilarity(ms2Spectra, refSpec, ms2Tol, massBegin, massEnd);
                var reverseSearchSimilarity = LcmsScoring.GetReverseSearchSimilarity(ms2Spectra, refSpec, ms2Tol, massBegin, massEnd);
                var accurateMassSimilarity = LcmsScoring.GetGaussianSimilarity(mz, query.PrecursorMz, ms1Tol);
                var presenseSimilarity = LcmsScoring.GetFragmentPresenceScore(ms2Spectra, query, ms2Tol, massBegin, massEnd, project.IonMode, project.TargetOmics);

                if (query.RetentionTime >= 0) {
                    rtSimilarity = LcmsScoring.GetGaussianSimilarity(peakAreaBean.RtAtPeakTop, query.RetentionTime, rtTol);
                }
                if (query.IsotopeRatioList.Count != 0) isotopeSimilarity = LcmsScoring.GetIsotopeRatioSimilarity(ms1Spectra, query.IsotopeRatioList, mz, ms1Tol);

                var spectrumPenalty = false;
                if (refSpec != null &&
                    refSpec.Count <= 1) spectrumPenalty = true;

                var totalSimilarity1 = -1.0;
                var totalSimilarity2 = -1.0;
                if (query.Ontology == "PFAS") {
                    totalSimilarity1 = LcmsScoring.GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, isotopeSimilarity, 1.0,
                        1.0, presenseSimilarity, spectrumPenalty, TargetOmics.Lipidomics,
                        param.IsUseRetentionInfoForIdentificationScoring);
                }
                else {
                    var ontology = query.CompoundClass;
                    if (ontology == null || ontology == string.Empty || ontology == "Others" || ontology == "Unknown") {
                        totalSimilarity1 = LcmsScoring.GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, isotopeSimilarity, spectraSimilarity,
                        reverseSearchSimilarity, presenseSimilarity, spectrumPenalty, TargetOmics.Metablomics,
                        param.IsUseRetentionInfoForIdentificationScoring);
                    }
                    else {
                        totalSimilarity1 = LcmsScoring.GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, isotopeSimilarity, spectraSimilarity,
                        reverseSearchSimilarity, presenseSimilarity, spectrumPenalty, project.TargetOmics,
                        param.IsUseRetentionInfoForIdentificationScoring);
                    }
                }
                
               

                totalSimilarity2 = LcmsScoring.GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, isotopeSimilarity,
                    param.IsUseRetentionInfoForIdentificationScoring);

                if (project.MethodType == MethodType.diMSMS && project.TargetOmics == TargetOmics.Lipidomics) {
                    if (totalSimilarity1 > maxTotalSimilarity1) {
                        maxTotalSimilarity1 = totalSimilarity1;
                        maxSpectraSimilarity = spectraSimilarity;
                        maxPresenseSimilarity = presenseSimilarity;
                        maxReverseSimilarity = reverseSearchSimilarity;
                        maxAccurateMassSimilarity1 = accurateMassSimilarity;
                        maxIsotopeSimilarity1 = isotopeSimilarity;
                        maxRtSimilarity1 = rtSimilarity;
                        maxSpectraSimilarityLibraryIndex = i;
                    }
                }
                else {
                    if (totalSimilarity1 > maxTotalSimilarity1 && presenseSimilarity > 0.4) {
                        maxTotalSimilarity1 = totalSimilarity1;
                        maxSpectraSimilarity = spectraSimilarity;
                        maxPresenseSimilarity = presenseSimilarity;
                        maxReverseSimilarity = reverseSearchSimilarity;
                        maxAccurateMassSimilarity1 = accurateMassSimilarity;
                        maxIsotopeSimilarity1 = isotopeSimilarity;
                        maxRtSimilarity1 = rtSimilarity;
                        maxSpectraSimilarityLibraryIndex = i;
                    }
                }

                if (totalSimilarity2 > maxTotalSimilarity2) {
                    maxTotalSimilarity2 = totalSimilarity2;
                    maxAccurateMassSimilarity2 = accurateMassSimilarity;
                    maxRtSimilarity2 = rtSimilarity;
                    maxIsotopeSimilarity2 = isotopeSimilarity;
                    maxSimilarityWithoutMsMsLibraryIndex = i;
                }
            }

            var isMinRequired = maxSpectraSimilarity > 0.15 || maxReverseSimilarity > 0.5 ? true : false;
            if (project.TargetOmics == TargetOmics.Lipidomics && maxSpectraSimilarityLibraryIndex >= 0) {
                var repQuery = mspDB[maxSpectraSimilarityLibraryIndex];
                if ((repQuery.CompoundClass == "EtherTG" || repQuery.CompoundClass == "EtherDG") && maxSpectraSimilarity < 0.15) {
                    isMinRequired = false;
                }
            }
            if (maxTotalSimilarity1 * 100 > param.IdentificationScoreCutOff &&
               (project.MethodType == MethodType.diMSMS || (project.MethodType == MethodType.ddMSMS && isMinRequired))) {
                var repQuery = mspDB[maxSpectraSimilarityLibraryIndex];
                var annotatedName = repQuery.Name;
                var compoundClass = repQuery.CompoundClass;
                var adduct = repQuery.AdductIonBean.AdductIonName;
                var rtDiff = Math.Abs(rt - repQuery.RetentionTime);
                if (project.TargetOmics == TargetOmics.Lipidomics) {
                    var isLipidClassMatched = false;
                    var isLipidChainMatched = false;
                    var isLipidPositionMatched = false;
                    var isOtherLipids = false;
                    annotatedName = LcmsScoring.GetRefinedLipidAnnotationLevel(ms2Spectra, repQuery, ms2Tol, out isLipidClassMatched, out isLipidChainMatched, out isLipidPositionMatched, out isOtherLipids);

                    peakAreaBean.IsLipidClassMatch = isLipidClassMatched;
                    peakAreaBean.IsLipidChainsMatch = isLipidChainMatched;
                    peakAreaBean.IsLipidPositionMatch = isLipidPositionMatched;
                    peakAreaBean.IsOtherLipidMatch = isOtherLipids;
                }

                #region // practical filtering to reduce false positive identification
                if (repQuery.RetentionTime >= 0 && param.IsUseRetentionInfoForIdentificationFiltering) {
                    if (rtDiff > param.RetentionTimeLibrarySearchTolerance)
                        annotatedName = string.Empty;
                }
                #endregion

                if (annotatedName != string.Empty) {
                    setPeakSpotProperties(peakAreaBean, rtDiff, param, true, true);
                    //annotatedName = refinedName;
                }
                else {
                    if (maxTotalSimilarity2 * 100 > param.IdentificationScoreCutOff) {
                        rtDiff = Math.Abs(rt - mspDB[maxSimilarityWithoutMsMsLibraryIndex].RetentionTime);
                        setPeakSpotProperties(peakAreaBean, rtDiff, param, true, false);
                        setAnnotatedInformation(peakAreaBean, mspDB[maxSimilarityWithoutMsMsLibraryIndex], maxAccurateMassSimilarity2, maxRtSimilarity2, -1, maxIsotopeSimilarity2, maxTotalSimilarity2);
                    }
                    else {
                        setDefaultPeakSpotProperties(peakAreaBean);
                        setDefaultCompoundInformation(peakAreaBean);
                    }
                    return;
                }
                setIdentifiedInformation(peakAreaBean, mspDB[maxSpectraSimilarityLibraryIndex], annotatedName,
                    maxSpectraSimilarity, maxReverseSimilarity, maxPresenseSimilarity,
                    maxAccurateMassSimilarity1, maxRtSimilarity1, -1, maxIsotopeSimilarity1, maxTotalSimilarity1);
            }
            else if (maxTotalSimilarity1 * 100 > 60.0) {
                var repQuery = mspDB[maxSpectraSimilarityLibraryIndex];
                var rtDiff = Math.Abs(rt - repQuery.RetentionTime);

                setPeakSpotProperties(peakAreaBean, rtDiff, param, true, false);
                setAnnotatedInformation(peakAreaBean, repQuery,
                    maxAccurateMassSimilarity1, maxRtSimilarity1, -1,
                    maxIsotopeSimilarity1, maxTotalSimilarity1);
            }
            else if (maxTotalSimilarity2 * 100 > param.IdentificationScoreCutOff) {
                var repQuery = mspDB[maxSimilarityWithoutMsMsLibraryIndex];
                var rtDiff = Math.Abs(rt - repQuery.RetentionTime);
                setPeakSpotProperties(peakAreaBean, rtDiff, param, true, false);
                setAnnotatedInformation(peakAreaBean, repQuery, maxAccurateMassSimilarity2, maxRtSimilarity2, -1, maxIsotopeSimilarity2, maxTotalSimilarity2);
            }
            else {
                setDefaultCompoundInformation(peakAreaBean);
            }
        }

        public static List<MatchedCandidate> GetMsMsMatchedCandidatesTest(PeakAreaBean peakAreaBean,
           ObservableCollection<double[]> ms1Spectra, ObservableCollection<double[]> ms2Spectra,
           List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param, ProjectPropertyBean project) {

            var candidates = new List<MatchedCandidate>();

            float mz = peakAreaBean.AccurateMass;
            float rt = peakAreaBean.RtAtPeakTop;

            var ms1Tol = param.Ms1LibrarySearchTolerance;
            var ms2Tol = param.Ms2LibrarySearchTolerance;
            var rtTol = param.RetentionTimeLibrarySearchTolerance;
            var massBegin = param.Ms2MassRangeBegin;
            var massEnd = param.Ms2MassRangeEnd;

            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms1Tol));
            #region // practical parameter changes
            if (mz > 500) {
                ms1Tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(mz, ppm);
            }
            #endregion

            int startIndex = DataAccessLcUtility.GetDatabaseStartIndex(mz, ms1Tol, mspDB);
            for (int i = startIndex; i < mspDB.Count; i++) {
                var query = mspDB[i];
                if (query.PrecursorMz > mz + ms1Tol) break;
                if (query.PrecursorMz < mz - ms1Tol) continue;
                if (query.RetentionTime >= 0 && param.IsUseRetentionInfoForIdentificationFiltering) {
                    var refRt = query.RetentionTime;
                    if (Math.Abs(refRt - rt) > param.RetentionTimeLibrarySearchTolerance) continue;
                }
                var rtSimilarity = -1.0;
                var isotopeSimilarity = -1.0;

                var refSpec = query.MzIntensityCommentBeanList;

                var spectraSimilarity = LcmsScoring.GetMassSpectraSimilarity(ms2Spectra, refSpec, ms2Tol, massBegin, massEnd);
                var reverseSearchSimilarity = LcmsScoring.GetReverseSearchSimilarity(ms2Spectra, refSpec, ms2Tol, massBegin, massEnd);
                var accurateMassSimilarity = LcmsScoring.GetGaussianSimilarity(mz, query.PrecursorMz, ms1Tol);
                var presenseSimilarity = LcmsScoring.GetFragmentPresenceScore(ms2Spectra, query, ms2Tol, massBegin, massEnd, project.IonMode, project.TargetOmics);

                if (query.RetentionTime >= 0) {
                    rtSimilarity = LcmsScoring.GetGaussianSimilarity(peakAreaBean.RtAtPeakTop, query.RetentionTime, rtTol);
                }
                if (query.IsotopeRatioList.Count != 0) isotopeSimilarity = LcmsScoring.GetIsotopeRatioSimilarity(ms1Spectra, query.IsotopeRatioList, mz, ms1Tol);

                var spectrumPenalty = false;
                if (refSpec != null &&
                    refSpec.Count <= 1) spectrumPenalty = true;

                var totalSimilarity1 = -1.0;
                var ontology = query.CompoundClass;
                if (ontology == null || ontology == string.Empty || ontology == "Others" || ontology == "Unknown") {
                    totalSimilarity1 = LcmsScoring.GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, isotopeSimilarity, spectraSimilarity,
                    reverseSearchSimilarity, presenseSimilarity, spectrumPenalty, TargetOmics.Metablomics,
                    param.IsUseRetentionInfoForIdentificationScoring);
                }
                else {
                    totalSimilarity1 = LcmsScoring.GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, isotopeSimilarity, spectraSimilarity,
                    reverseSearchSimilarity, presenseSimilarity, spectrumPenalty, project.TargetOmics,
                    param.IsUseRetentionInfoForIdentificationScoring);
                }

                var isMinRequired = presenseSimilarity > 0.4 && (spectraSimilarity > 0.15 || reverseSearchSimilarity > 0.5) ? true : false;
                if (project.TargetOmics == TargetOmics.Lipidomics) {
                    var repQuery = mspDB[i];
                    if ((repQuery.CompoundClass == "EtherTG" || repQuery.CompoundClass == "EtherDG") && spectraSimilarity < 0.15) {
                        isMinRequired = false;
                    }
                }
                if (totalSimilarity1 * 100 > param.IdentificationScoreCutOff &&
                   (project.MethodType == MethodType.diMSMS || (project.MethodType == MethodType.ddMSMS && isMinRequired))) {
                    var repQuery = mspDB[i];
                    var annotatedName = repQuery.Name;
                    var compoundClass = repQuery.CompoundClass;
                    var adduct = repQuery.AdductIonBean.AdductIonName;
                    var rtDiff = Math.Abs(rt - repQuery.RetentionTime);

                    var candidate = new MatchedCandidate() { MspID = i };
                    if (project.TargetOmics == TargetOmics.Lipidomics) {
                        var isLipidClassMatched = false;
                        var isLipidChainMatched = false;
                        var isLipidPositionMatched = false;
                        var isOtherLipids = false;
                        annotatedName = LcmsScoring.GetRefinedLipidAnnotationLevel(ms2Spectra, repQuery, ms2Tol, out isLipidClassMatched, out isLipidChainMatched, out isLipidPositionMatched, out isOtherLipids);

                        candidate.IsLipidClassMatch = isLipidClassMatched;
                        candidate.IsLipidChainsMatch = isLipidChainMatched;
                        candidate.IsLipidPositionMatch = isLipidPositionMatched;
                        candidate.IsOtherLipidMatch = isOtherLipids;
                    }

                    #region // practical filtering to reduce false positive identification
                    if (repQuery.RetentionTime >= 0 && param.IsUseRetentionInfoForIdentificationFiltering) {
                        if (rtDiff > param.RetentionTimeLibrarySearchTolerance)
                            annotatedName = string.Empty;
                    }
                    #endregion

                    if (annotatedName != string.Empty) {
                        setPeakSpotProperties(candidate, rtDiff, param, true, true);
                    }
                    setIdentifiedInformation(candidate, mspDB[i], annotatedName,
                        spectraSimilarity, reverseSearchSimilarity, presenseSimilarity,
                        accurateMassSimilarity, rtSimilarity, -1, isotopeSimilarity, totalSimilarity1);
                    candidates.Add(candidate);
                }
            }
            if (candidates.Count > 0) candidates = candidates.OrderByDescending(n => n.TotalScore).ToList();
            return candidates;
        }

        public static void similarityCalculationsIonMobilityMsMsIncluded(DriftSpotBean driftSpot, PeakAreaBean peakSpot,
            ObservableCollection<double[]> ms1Spectra, ObservableCollection<double[]> ms2Spectra,
            List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param, ProjectPropertyBean project) {
            int maxSpectraSimilarityLibraryIndex = 0, maxSimilarityWithoutMsMsLibraryIndex = 0;
            float mz = driftSpot.AccurateMass;
            float rt = peakSpot.RtAtPeakTop;
            float dt = driftSpot.DriftTimeAtPeakTop;
            float ccs = driftSpot.Ccs;

            double maxTotalSimilarity1 = double.MinValue, maxSpectraSimilarity = double.MinValue, maxReverseSimilarity = double.MinValue, maxPresenseSimilarity = double.MinValue
                , maxRtSimilarity1 = double.MinValue, maxRtSimilarity2 = double.MinValue, maxIsotopeSimilarity1 = double.MinValue, maxIsotopeSimilarity2 = double.MinValue
                , maxAccurateMassSimilarity1 = double.MinValue, maxAccurateMassSimilarity2 = double.MinValue, maxTotalSimilarity2 = double.MinValue
                , maxCcsSimilarity1 = double.MinValue, maxCcsSimilarity2 = double.MinValue;

            var ms1Tol = param.Ms1LibrarySearchTolerance;
            var ms2Tol = param.Ms2LibrarySearchTolerance;
            var rtTol = param.RetentionTimeLibrarySearchTolerance;
            var ccsTol = param.CcsSearchTolerance;
            var massBegin = param.Ms2MassRangeBegin;
            var massEnd = param.Ms2MassRangeEnd;

            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms1Tol));
            #region // practical parameter changes
            if (mz > 500) {
                ms1Tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(mz, ppm);
            }
            #endregion

           
            int startIndex = DataAccessLcUtility.GetDatabaseStartIndex(mz, ms1Tol, mspDB);

            for (int i = startIndex; i < mspDB.Count; i++) {
                var query = mspDB[i];
                if (query.PrecursorMz > mz + ms1Tol) break;
                if (query.PrecursorMz < mz - ms1Tol) continue;

                var rtSimilarity = -1.0;
                var ccsSimilarity = -1.0;
                var isotopeSimilarity = -1.0;
                var refSpec = query.MzIntensityCommentBeanList;

                var spectraSimilarity = LcmsScoring.GetMassSpectraSimilarity(ms2Spectra, refSpec, ms2Tol, massBegin, massEnd);
                var reverseSearchSimilarity = LcmsScoring.GetReverseSearchSimilarity(ms2Spectra, refSpec, ms2Tol, massBegin, massEnd);
                var accurateMassSimilarity = LcmsScoring.GetGaussianSimilarity(mz, query.PrecursorMz, ms1Tol);
                var presenseSimilarity = LcmsScoring.GetFragmentPresenceScore(ms2Spectra, query, ms2Tol, massBegin, massEnd, project.IonMode, project.TargetOmics);

                //if (driftSpot.MasterPeakID == 1134) {
                //    Debug.WriteLine(query.Name + "\t" + Math.Round(spectraSimilarity, 2) + "\t" + Math.Round(reverseSearchSimilarity, 2) +
                //        "\t" + Math.Round(accurateMassSimilarity, 2) + "\t" + Math.Round(presenseSimilarity, 2));
                //}

                if (query.RetentionTime >= 0) {
                    rtSimilarity = LcmsScoring.GetGaussianSimilarity(rt, query.RetentionTime, rtTol);
                }

                if (query.CollisionCrossSection >= 0) {
                    ccsSimilarity = LcmsScoring.GetGaussianSimilarity(ccs, query.CollisionCrossSection, ccsTol);
                }

                var spectrumPenalty = false;
                if (refSpec != null &&
                    refSpec.Count <= 1) spectrumPenalty = true;

                var totalSimilarity1 = -1.0;
                var totalSimilarity2 = -1.0;

                if (query.Ontology == "PFAS") {
                    totalSimilarity1 = LcmsScoring.GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, ccsSimilarity, isotopeSimilarity, 1.0,
                         1.0, presenseSimilarity, spectrumPenalty, TargetOmics.Lipidomics,
                        param.IsUseRetentionInfoForIdentificationScoring, param.IsUseCcsForIdentificationScoring);
                }
                else {
                    var ontology = query.CompoundClass;
                    if (ontology == null || ontology == string.Empty || ontology == "Others" || ontology == "Unknown") {
                        totalSimilarity1 = LcmsScoring.GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, ccsSimilarity, isotopeSimilarity, spectraSimilarity,
                        reverseSearchSimilarity, presenseSimilarity, spectrumPenalty, TargetOmics.Metablomics,
                        param.IsUseRetentionInfoForIdentificationScoring, param.IsUseCcsForIdentificationScoring);
                    }
                    else {
                        totalSimilarity1 = LcmsScoring.GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, ccsSimilarity, isotopeSimilarity, spectraSimilarity,
                        reverseSearchSimilarity, presenseSimilarity, spectrumPenalty, project.TargetOmics,
                        param.IsUseRetentionInfoForIdentificationScoring, param.IsUseCcsForIdentificationScoring);
                    }
                }
               

                totalSimilarity2 = LcmsScoring.GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, ccsSimilarity, isotopeSimilarity,
                    param.IsUseRetentionInfoForIdentificationScoring, param.IsUseCcsForIdentificationScoring);

                if (project.MethodType == MethodType.diMSMS && project.TargetOmics == TargetOmics.Lipidomics) {
                    if (totalSimilarity1 > maxTotalSimilarity1) {
                        maxTotalSimilarity1 = totalSimilarity1;
                        maxSpectraSimilarity = spectraSimilarity;
                        maxPresenseSimilarity = presenseSimilarity;
                        maxReverseSimilarity = reverseSearchSimilarity;
                        maxAccurateMassSimilarity1 = accurateMassSimilarity;
                        maxIsotopeSimilarity1 = isotopeSimilarity;
                        maxRtSimilarity1 = rtSimilarity;
                        maxCcsSimilarity1 = ccsSimilarity;
                        maxSpectraSimilarityLibraryIndex = i;
                    }
                }
                else {
                    if (totalSimilarity1 > maxTotalSimilarity1 && presenseSimilarity > 0.4) {
                        maxTotalSimilarity1 = totalSimilarity1;
                        maxSpectraSimilarity = spectraSimilarity;
                        maxPresenseSimilarity = presenseSimilarity;
                        maxReverseSimilarity = reverseSearchSimilarity;
                        maxAccurateMassSimilarity1 = accurateMassSimilarity;
                        maxIsotopeSimilarity1 = isotopeSimilarity;
                        maxRtSimilarity1 = rtSimilarity;
                        maxCcsSimilarity1 = ccsSimilarity;
                        maxSpectraSimilarityLibraryIndex = i;
                    }
                }

                if (totalSimilarity2 > maxTotalSimilarity2) {
                    maxTotalSimilarity2 = totalSimilarity2;
                    maxAccurateMassSimilarity2 = accurateMassSimilarity;
                    maxRtSimilarity2 = rtSimilarity;
                    maxCcsSimilarity2 = ccsSimilarity;
                    maxIsotopeSimilarity2 = isotopeSimilarity;
                    maxSimilarityWithoutMsMsLibraryIndex = i;
                }
            }

            var isMinRequired = maxSpectraSimilarity > 0.2 || maxReverseSimilarity > 0.5 ? true : false;
            //var isMinRequired = maxSpectraSimilarity > 0.2 ? true : false;
            if (project.TargetOmics == TargetOmics.Lipidomics && maxSpectraSimilarityLibraryIndex >= 0) {
                var repQuery = mspDB[maxSpectraSimilarityLibraryIndex];
                if ((repQuery.CompoundClass == "EtherTG" || repQuery.CompoundClass == "EtherDG" || repQuery.CompoundClass == "LPE") && maxSpectraSimilarity < 0.15) {
                    isMinRequired = false;
                }
            }
            
            if (maxTotalSimilarity1 * 100 > param.IdentificationScoreCutOff && 
                (project.MethodType == MethodType.diMSMS || (project.MethodType == MethodType.ddMSMS && isMinRequired))) {
                var repQuery = mspDB[maxSpectraSimilarityLibraryIndex];
                var annotatedName = repQuery.Name;
                var rtDiff = Math.Abs(rt - repQuery.RetentionTime);
                var ccsDiff = Math.Abs(ccs - repQuery.CollisionCrossSection);

               // Console.WriteLine("name {0}, ccs {1}, ccs ref {2}", repQuery.Name, ccs, repQuery.CollisionCrossSection);

                if (project.TargetOmics == TargetOmics.Lipidomics) {
                    var isLipidClassMatched = false;
                    var isLipidChainMatched = false;
                    var isLipidPositionMatched = false;
                    var isOtherLipids = false;
                    annotatedName = LcmsScoring.GetRefinedLipidAnnotationLevel(ms2Spectra, repQuery, ms2Tol, out isLipidClassMatched, out isLipidChainMatched, out isLipidPositionMatched, out isOtherLipids);

                    driftSpot.IsLipidClassMatch = isLipidClassMatched;
                    driftSpot.IsLipidChainsMatch = isLipidChainMatched;
                    driftSpot.IsLipidPositionMatch = isLipidPositionMatched;
                    driftSpot.IsOtherLipidMatch = isOtherLipids;
                }

                #region // practical filtering to reduce false positive identification
                //var compoundClass = repQuery.CompoundClass;
                //var adduct = repQuery.AdductIonBean.AdductIonName;

                //if (repQuery.Name == "TAG 53:2; TAG 17:1-17:1-19:0; [M+Na]+") {
                //    Console.WriteLine();
                //}

                if (repQuery.RetentionTime >= 0 && param.IsUseRetentionInfoForIdentificationFiltering) {
                    if (rtDiff > param.RetentionTimeLibrarySearchTolerance) annotatedName = string.Empty;
                }

                if (repQuery.CollisionCrossSection >= 0 && param.IsUseCcsForIdentificationFiltering) {
                    if (ccsDiff > param.CcsSearchTolerance) annotatedName = string.Empty;
                }

                if (annotatedName != string.Empty) {
                    //annotatedName = refinedName;
                    setDriftSpotProperties(driftSpot, rtDiff, ccsDiff, param, true, true);
                }
                else {
                    if (maxTotalSimilarity2 * 100 > param.IdentificationScoreCutOff) {
                        setDriftSpotProperties(driftSpot, rtDiff, ccsDiff, param, true, false);
                        setAnnotatedInformation(driftSpot, mspDB[maxSimilarityWithoutMsMsLibraryIndex], maxAccurateMassSimilarity2, maxRtSimilarity2, maxCcsSimilarity2, maxIsotopeSimilarity2, maxTotalSimilarity2);
                    }
                    else {
                        setDefaultDriftSpotProperties(driftSpot);
                        setDefaultCompoundInformation(driftSpot);
                    }
                    return;
                }
                #endregion

                setIdentifiedInformation(driftSpot, mspDB[maxSpectraSimilarityLibraryIndex], annotatedName,
                    maxSpectraSimilarity, maxReverseSimilarity, maxPresenseSimilarity,
                    maxAccurateMassSimilarity1, maxRtSimilarity1, maxCcsSimilarity1, maxIsotopeSimilarity1, maxTotalSimilarity1);
            }
            else if (maxTotalSimilarity1 * 100 > 60.0) {
                var repQuery = mspDB[maxSpectraSimilarityLibraryIndex];
                var rtDiff = Math.Abs(rt - repQuery.RetentionTime);
                var ccsDiff = Math.Abs(ccs - repQuery.CollisionCrossSection);

                setDriftSpotProperties(driftSpot, rtDiff, ccsDiff, param, true, false);
                setAnnotatedInformation(driftSpot, repQuery,
                    maxAccurateMassSimilarity1, maxRtSimilarity1, maxCcsSimilarity1,
                    maxIsotopeSimilarity1, maxTotalSimilarity1);
            }
            else if (maxTotalSimilarity2 * 100 > param.IdentificationScoreCutOff) {
                var repQuery = mspDB[maxSimilarityWithoutMsMsLibraryIndex];
                var rtDiff = Math.Abs(rt - repQuery.RetentionTime);
                var ccsDiff = Math.Abs(ccs - repQuery.CollisionCrossSection);
                setDriftSpotProperties(driftSpot, rtDiff, ccsDiff, param, true, false);
                setAnnotatedInformation(driftSpot, repQuery, maxAccurateMassSimilarity2, maxRtSimilarity2, maxCcsSimilarity2, maxIsotopeSimilarity2, maxTotalSimilarity2);
            }
            else {
                setDefaultCompoundInformation(driftSpot);
            }
        }

        public static List<MatchedCandidate> GetImMsMsMatchedCandidatesTest(DriftSpotBean driftSpot, PeakAreaBean peakSpot,
            ObservableCollection<double[]> ms1Spectra, ObservableCollection<double[]> ms2Spectra,
            List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param, ProjectPropertyBean project) {
            var candidates = new List<MatchedCandidate>();
            float mz = driftSpot.AccurateMass;
            float rt = peakSpot.RtAtPeakTop;
            float dt = driftSpot.DriftTimeAtPeakTop;
            float ccs = driftSpot.Ccs;
            var ms1Tol = param.Ms1LibrarySearchTolerance;
            var ms2Tol = param.Ms2LibrarySearchTolerance;
            var rtTol = param.RetentionTimeLibrarySearchTolerance;
            var ccsTol = param.CcsSearchTolerance;
            var massBegin = param.Ms2MassRangeBegin;
            var massEnd = param.Ms2MassRangeEnd;

            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms1Tol));
            #region // practical parameter changes
            if (mz > 500) {
                ms1Tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(mz, ppm);
            }
            #endregion


            int startIndex = DataAccessLcUtility.GetDatabaseStartIndex(mz, ms1Tol, mspDB);

            for (int i = startIndex; i < mspDB.Count; i++) {
                var query = mspDB[i];
                if (query.PrecursorMz > mz + ms1Tol) break;
                if (query.PrecursorMz < mz - ms1Tol) continue;

                var rtSimilarity = -1.0;
                var ccsSimilarity = -1.0;
                var isotopeSimilarity = -1.0;
                var refSpec = query.MzIntensityCommentBeanList;

                var spectraSimilarity = LcmsScoring.GetMassSpectraSimilarity(ms2Spectra, refSpec, ms2Tol, massBegin, massEnd);
                var reverseSearchSimilarity = LcmsScoring.GetReverseSearchSimilarity(ms2Spectra, refSpec, ms2Tol, massBegin, massEnd);
                var accurateMassSimilarity = LcmsScoring.GetGaussianSimilarity(mz, query.PrecursorMz, ms1Tol);
                var presenseSimilarity = LcmsScoring.GetFragmentPresenceScore(ms2Spectra, query, ms2Tol, massBegin, massEnd, project.IonMode, project.TargetOmics);

                if (query.RetentionTime >= 0) {
                    rtSimilarity = LcmsScoring.GetGaussianSimilarity(rt, query.RetentionTime, rtTol);
                }

                if (query.CollisionCrossSection >= 0) {
                    ccsSimilarity = LcmsScoring.GetGaussianSimilarity(ccs, query.CollisionCrossSection, ccsTol);
                }

                var spectrumPenalty = false;
                if (refSpec != null &&
                    refSpec.Count <= 1) spectrumPenalty = true;

                var totalSimilarity1 = -1.0;
                var ontology = query.CompoundClass;
                if (ontology == null || ontology == string.Empty || ontology == "Others" || ontology == "Unknown") {
                    totalSimilarity1 = LcmsScoring.GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, ccsSimilarity, isotopeSimilarity, spectraSimilarity,
                    reverseSearchSimilarity, presenseSimilarity, spectrumPenalty, TargetOmics.Metablomics,
                    param.IsUseRetentionInfoForIdentificationScoring, param.IsUseCcsForIdentificationScoring);
                }
                else {
                    totalSimilarity1 = LcmsScoring.GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, ccsSimilarity, isotopeSimilarity, spectraSimilarity,
                    reverseSearchSimilarity, presenseSimilarity, spectrumPenalty, project.TargetOmics,
                    param.IsUseRetentionInfoForIdentificationScoring, param.IsUseCcsForIdentificationScoring);
                }

                var isMinRequired = presenseSimilarity > 0.4 && (spectraSimilarity > 0.2 || reverseSearchSimilarity > 0.5) ? true : false;
                //var isMinRequired = maxSpectraSimilarity > 0.2 ? true : false;
                if (project.TargetOmics == TargetOmics.Lipidomics) {
                    var repQuery = mspDB[i];
                    if ((repQuery.CompoundClass == "EtherTG" || repQuery.CompoundClass == "EtherDG" || repQuery.CompoundClass == "LPE") && spectraSimilarity < 0.2) {
                        isMinRequired = false;
                    }
                }
                if (totalSimilarity1 * 100 > param.IdentificationScoreCutOff &&
                    (project.MethodType == MethodType.diMSMS || (project.MethodType == MethodType.ddMSMS && isMinRequired))) {
                    var repQuery = mspDB[i];
                    var annotatedName = repQuery.Name;
                    var rtDiff = Math.Abs(rt - repQuery.RetentionTime);
                    var ccsDiff = Math.Abs(ccs - repQuery.CollisionCrossSection);

                    // Console.WriteLine("name {0}, ccs {1}, ccs ref {2}", repQuery.Name, ccs, repQuery.CollisionCrossSection);
                    var candidate = new MatchedCandidate() { MspID = i };
                    if (project.TargetOmics == TargetOmics.Lipidomics) {
                        var isLipidClassMatched = false;
                        var isLipidChainMatched = false;
                        var isLipidPositionMatched = false;
                        var isOtherLipids = false;
                        annotatedName = LcmsScoring.GetRefinedLipidAnnotationLevel(ms2Spectra, repQuery, ms2Tol, out isLipidClassMatched, out isLipidChainMatched, out isLipidPositionMatched, out isOtherLipids);

                        candidate.IsLipidClassMatch = isLipidClassMatched;
                        candidate.IsLipidChainsMatch = isLipidChainMatched;
                        candidate.IsLipidPositionMatch = isLipidPositionMatched;
                        candidate.IsOtherLipidMatch = isOtherLipids;
                    }

                    #region // practical filtering to reduce false positive identification

                    if (repQuery.RetentionTime >= 0 && param.IsUseRetentionInfoForIdentificationFiltering) {
                        if (rtDiff > param.RetentionTimeLibrarySearchTolerance) annotatedName = string.Empty;
                    }

                    if (repQuery.CollisionCrossSection >= 0 && param.IsUseCcsForIdentificationFiltering) {
                        if (ccsDiff > param.CcsSearchTolerance) annotatedName = string.Empty;
                    }

                    if (annotatedName != string.Empty) {
                        setDriftSpotProperties(candidate, rtDiff, ccsDiff, param, true, true);
                    }
                    
                    #endregion

                    setIdentifiedInformation(candidate, mspDB[i], annotatedName,
                        spectraSimilarity, reverseSearchSimilarity, presenseSimilarity,
                        accurateMassSimilarity, rtSimilarity, ccsSimilarity, isotopeSimilarity, totalSimilarity1);

                    candidates.Add(candidate);
                }
            }
            if (candidates.Count > 0) candidates = candidates.OrderByDescending(n => n.TotalScore).ToList();
            return candidates;
        }

        private static void setDefaultDriftSpotProperties(DriftSpotBean driftSpot) {
            driftSpot.IsRtMatch = false;
            driftSpot.IsCcsMatch = false;
            driftSpot.IsMs1Match = false;
            driftSpot.IsMs2Match = false;
        }

        private static void setDriftSpotProperties(DriftSpotBean driftSpot, float rtDiff, float ccsDiff, AnalysisParametersBean param, bool isMs1Match, bool isMs2Match) {
            if (rtDiff <= param.RetentionTimeLibrarySearchTolerance) driftSpot.IsRtMatch = true; else driftSpot.IsRtMatch = false;
            if (ccsDiff <= param.CcsSearchTolerance) driftSpot.IsCcsMatch = true; else driftSpot.IsCcsMatch = false;
            driftSpot.IsMs1Match = isMs1Match;
            driftSpot.IsMs2Match = isMs2Match;
        }

        private static void setDriftSpotProperties(MatchedCandidate candidate, float rtDiff, float ccsDiff, AnalysisParametersBean param, bool isMs1Match, bool isMs2Match) {
            if (rtDiff <= param.RetentionTimeLibrarySearchTolerance) candidate.IsRtMatch = true; else candidate.IsRtMatch = false;
            if (ccsDiff <= param.CcsSearchTolerance) candidate.IsCcsMatch = true; else candidate.IsCcsMatch = false;
            candidate.IsMs1Match = isMs1Match;
            candidate.IsMs2Match = isMs2Match;
        }

        private static void setDefaultPeakSpotProperties(PeakAreaBean peakSpot) {
            peakSpot.IsRtMatch = false;
            peakSpot.IsCcsMatch = false;
            peakSpot.IsMs1Match = false;
            peakSpot.IsMs2Match = false;
        }

        private static void setPeakSpotProperties(PeakAreaBean peakSpot, float rtDiff, AnalysisParametersBean param, bool isMs1Match, bool isMs2Match) {
            if (rtDiff <= param.RetentionTimeLibrarySearchTolerance) peakSpot.IsRtMatch = true; else peakSpot.IsRtMatch = false;
            peakSpot.IsCcsMatch = false;
            peakSpot.IsMs1Match = isMs1Match;
            peakSpot.IsMs2Match = isMs2Match;
        }

        private static void setPeakSpotProperties(MatchedCandidate candidate, float rtDiff, AnalysisParametersBean param, bool isMs1Match, bool isMs2Match) {
            if (rtDiff <= param.RetentionTimeLibrarySearchTolerance) candidate.IsRtMatch = true; else candidate.IsRtMatch = false;
            candidate.IsCcsMatch = false;
            candidate.IsMs1Match = isMs1Match;
            candidate.IsMs2Match = isMs2Match;
        }

        private static void setDefaultCompoundInformation(PeakAreaBean peakAreaBean)
        {
            peakAreaBean.MetaboliteName = string.Empty;
            peakAreaBean.LibraryID = -1;
            peakAreaBean.Inchikey = string.Empty;
            peakAreaBean.PostIdentificationLibraryId = -1;
            peakAreaBean.MassSpectraSimilarityValue = -1;
            peakAreaBean.ReverseSearchSimilarityValue = -1;
            peakAreaBean.AccurateMassSimilarity = -1;
            peakAreaBean.RtSimilarityValue = -1;
            peakAreaBean.CcsSimilarity = -1;
            peakAreaBean.IsotopeSimilarityValue = -1;
            peakAreaBean.TotalScore = -1;
            setDefaultPeakSpotProperties(peakAreaBean);
        }

        private static void setDefaultCompoundInformation(DriftSpotBean driftSpot) {
            driftSpot.MetaboliteName = string.Empty;
            driftSpot.LibraryID = -1;
            driftSpot.Inchikey = string.Empty;
            driftSpot.PostIdentificationLibraryId = -1;
            driftSpot.MassSpectraSimilarityValue = -1;
            driftSpot.ReverseSearchSimilarityValue = -1;
            driftSpot.AccurateMassSimilarity = -1;
            driftSpot.RtSimilarityValue = -1;
            driftSpot.CcsSimilarity = -1;
            driftSpot.IsotopeSimilarityValue = -1;
            driftSpot.TotalScore = -1;
            setDefaultDriftSpotProperties(driftSpot);
        }

        private static void setAnnotatedInformation(PeakAreaBean peakSpot, MspFormatCompoundInformationBean refQuery, 
            double maxAccurateMassSimilarity, double maxRtSimilarity, double maxCcsSimilarity, double maxIsotopeSimilarity, double maxTotalSimilarity)
        {
            peakSpot.MetaboliteName = "w/o MS2:" + refQuery.Name;
            peakSpot.LibraryID = refQuery.Id;
            peakSpot.Inchikey = refQuery.InchiKey;
            peakSpot.PostIdentificationLibraryId = -1;
            peakSpot.MassSpectraSimilarityValue = -1;
            peakSpot.ReverseSearchSimilarityValue = -1;
            peakSpot.PresenseSimilarityValue = -1;
            peakSpot.AccurateMassSimilarity = (float)maxAccurateMassSimilarity * 1000;
            peakSpot.RtSimilarityValue = (float)maxRtSimilarity * 1000;
            peakSpot.CcsSimilarity = (float)maxCcsSimilarity * 1000;
            peakSpot.IsotopeSimilarityValue = (float)maxIsotopeSimilarity * 1000;
            peakSpot.TotalScore = (float)maxTotalSimilarity * 1000;
            // setAdductIonInformation(peakAreaBean, mspFormatCompoundInformationBean);
        }

        private static void setAnnotatedInformation(DriftSpotBean driftSpot, MspFormatCompoundInformationBean mspFormatCompoundInformationBean,
            double maxAccurateMassSimilarity, double maxRtSimilarity, double maxCcsSimilarity, double maxIsotopeSimilarity, double maxTotalSimilarity) {
            driftSpot.MetaboliteName = "w/o MS2:" + mspFormatCompoundInformationBean.Name;
            driftSpot.LibraryID = mspFormatCompoundInformationBean.Id;
            driftSpot.Inchikey = mspFormatCompoundInformationBean.InchiKey;
            driftSpot.PostIdentificationLibraryId = -1;
            driftSpot.MassSpectraSimilarityValue = -1;
            driftSpot.ReverseSearchSimilarityValue = -1;
            driftSpot.PresenseSimilarityValue = -1;
            driftSpot.AccurateMassSimilarity = (float)maxAccurateMassSimilarity * 1000;
            driftSpot.RtSimilarityValue = (float)maxRtSimilarity * 1000;
            driftSpot.CcsSimilarity = (float)maxCcsSimilarity * 1000;
            driftSpot.IsotopeSimilarityValue = (float)maxIsotopeSimilarity * 1000;
            driftSpot.TotalScore = (float)maxTotalSimilarity * 1000;
            // setAdductIonInformation(peakAreaBean, mspFormatCompoundInformationBean);
        }

        private static void setIdentifiedInformation(PeakAreaBean peakAreaBean, 
            MspFormatCompoundInformationBean query, string annotatedName,
            double maxSpectraSimilarity, double maxReverseSimilarity, double maxPresenseSimilarity, 
            double maxAccurateMassSimilarity, double maxRtSimilarity, double maxCcsSimilarity, double maxIsotopeSimilarity, double maxTotalSimilarity)
        {
            peakAreaBean.MetaboliteName = annotatedName;
            peakAreaBean.LibraryID = query.Id;
            peakAreaBean.Inchikey = query.InchiKey;
            peakAreaBean.PostIdentificationLibraryId = -1;
            peakAreaBean.MassSpectraSimilarityValue = (float)maxSpectraSimilarity * 1000;
            peakAreaBean.ReverseSearchSimilarityValue = (float)maxReverseSimilarity * 1000;
            peakAreaBean.PresenseSimilarityValue = (float)maxPresenseSimilarity * 1000;
            peakAreaBean.AccurateMassSimilarity = (float)maxAccurateMassSimilarity * 1000;
            peakAreaBean.RtSimilarityValue = (float)maxRtSimilarity * 1000;
            peakAreaBean.CcsSimilarity = (float)maxCcsSimilarity * 1000;
            peakAreaBean.IsotopeSimilarityValue = (float)maxIsotopeSimilarity * 1000;
            peakAreaBean.TotalScore = (float)maxTotalSimilarity * 1000;
            SetAdductIonInformation(peakAreaBean, query);
        }

        private static void setIdentifiedInformation(MatchedCandidate candidate,
           MspFormatCompoundInformationBean query, string annotatedName,
           double dotProduct, double revDotProduct, double presenseScore,
           double mzSimilarity, double rtSimilarity, double ccsSimilarity, double isotopicSimilarity, double totalScore) {
            candidate.AnnotatedName = annotatedName;
            candidate.MspID = query.Id;
            candidate.InChIKey = query.InchiKey;
            candidate.DotProduct = (float)dotProduct * 1000;
            candidate.RevDotProduct = (float)revDotProduct * 1000;
            candidate.PresenseScore = (float)presenseScore * 1000;
            candidate.MzSimilarity = (float)mzSimilarity * 1000;
            candidate.RtSimilarity = (float)rtSimilarity * 1000;
            candidate.CcsSimilarity = (float)ccsSimilarity * 1000;
            candidate.IsotopicSimilarity = (float)isotopicSimilarity * 1000;
            candidate.TotalScore = (float)totalScore * 1000;
        }

        private static void setIdentifiedInformation(DriftSpotBean driftSpot,
           MspFormatCompoundInformationBean query, string annotatedName,
           double maxSpectraSimilarity, double maxReverseSimilarity, double maxPresenseSimilarity,
           double maxAccurateMassSimilarity, double maxRtSimilarity, double maxCcsSimilarity, double maxIsotopeSimilarity, double maxTotalSimilarity) {
            driftSpot.MetaboliteName = annotatedName;
            driftSpot.LibraryID = query.Id;
            driftSpot.Inchikey = query.InchiKey;
            driftSpot.PostIdentificationLibraryId = -1;
            driftSpot.MassSpectraSimilarityValue = (float)maxSpectraSimilarity * 1000;
            driftSpot.ReverseSearchSimilarityValue = (float)maxReverseSimilarity * 1000;
            driftSpot.PresenseSimilarityValue = (float)maxPresenseSimilarity * 1000;
            driftSpot.AccurateMassSimilarity = (float)maxAccurateMassSimilarity * 1000;
            driftSpot.RtSimilarityValue = (float)maxRtSimilarity * 1000;
            driftSpot.CcsSimilarity = (float)maxCcsSimilarity * 1000;
            driftSpot.IsotopeSimilarityValue = (float)maxIsotopeSimilarity * 1000;
            driftSpot.TotalScore = (float)maxTotalSimilarity * 1000;
            SetAdductIonInformation(driftSpot, query);
        }

        public static void SetAdductIonInformation(PeakAreaBean peakAreaBean, MspFormatCompoundInformationBean mspQuery)
        {
            if (mspQuery.AdductIonBean != null && mspQuery.AdductIonBean.FormatCheck == true)
            {
                peakAreaBean.AdductIonName = mspQuery.AdductIonBean.AdductIonName;
                peakAreaBean.AdductIonXmer = mspQuery.AdductIonBean.AdductIonXmer;
                peakAreaBean.AdductIonAccurateMass = mspQuery.AdductIonBean.AdductIonAccurateMass;
                peakAreaBean.AdductParent = -1;
                peakAreaBean.AdductIonChargeNumber = mspQuery.AdductIonBean.ChargeNumber;
            }
        }

        public static void SetAdductIonInformation(DriftSpotBean driftSpot, MspFormatCompoundInformationBean mspQuery) {
            if (mspQuery.AdductIonBean != null && mspQuery.AdductIonBean.FormatCheck == true) {
                driftSpot.AdductIonName = mspQuery.AdductIonBean.AdductIonName;
                driftSpot.AdductIonXmer = mspQuery.AdductIonBean.AdductIonXmer;
                driftSpot.AdductIonAccurateMass = mspQuery.AdductIonBean.AdductIonAccurateMass;
                driftSpot.AdductParent = -1;
                driftSpot.AdductIonChargeNumber = mspQuery.AdductIonBean.ChargeNumber;
            }
        }

        /// <summary>
        /// This method performs the compound identification by the retention time and precursor ion information stored in the user-defined TXT library.
        /// The text library will be retrieved by TextLibraryParcer.cs of Database assembly and will be stored as PostIdentificatioinReferenceBean.cs.
        /// </summary>
        /// <param name="peakSpots"></param>
        /// <param name="textDB"></param>
        /// <param name="param"></param>
        /// <param name="project"></param>
        public static void PostCompoundIdentification(List<PeakAreaBean> peakSpots, List<PostIdentificatioinReferenceBean> textDB, 
            AnalysisParametersBean param, ProjectPropertyBean project, AnalysisFileBean file)
        {
            if (textDB == null || textDB.Count == 0) return;

            textDB = textDB.OrderBy(n => n.AccurateMass).ToList();

            for (int i = 0; i < peakSpots.Count; i++) {
                if (param.IsIonMobility)
                    similarityCalculationsForIonMobility(peakSpots[i], textDB, param, project, file.AnalysisFilePropertyBean.AnalysisFileId);
                else
                    similarityCalculations(peakSpots[i], textDB, param, project);
            }

            if (param.OnlyReportTopHitForPostAnnotation) {
                if (param.IsIonMobility)
                    peakSpots = getRefinedPeakAndDriftSpotListForTextDbResult(peakSpots);
                else
                    peakSpots = getRefinedPeakAreaBeanList(peakSpots);
            }
            textDB = textDB.OrderBy(n => n.ReferenceID).ToList();
        }

        private static List<PeakAreaBean> getRefinedPeakAreaBeanList(List<PeakAreaBean> peakAreaBeanList)
        {
            if (peakAreaBeanList.Count <= 1) return peakAreaBeanList;
            peakAreaBeanList = peakAreaBeanList.OrderByDescending(n => n.PostIdentificationLibraryId).ToList();

            var currentLibraryId = peakAreaBeanList[0].PostIdentificationLibraryId;
            var currentPeakId = 0;
            for (int i = 1; i < peakAreaBeanList.Count; i++)
            {
                if (peakAreaBeanList[i].PostIdentificationLibraryId < 0) break;
                if (peakAreaBeanList[i].PostIdentificationLibraryId != currentLibraryId)
                {
                    currentLibraryId = peakAreaBeanList[i].PostIdentificationLibraryId;
                    currentPeakId = i;
                    continue;
                }
                else
                {
                    if (peakAreaBeanList[currentPeakId].TotalScore < peakAreaBeanList[i].TotalScore)
                    {
                        setDefaultCompoundInformation(peakAreaBeanList[currentPeakId]);
                        currentPeakId = i;
                    }
                    else
                    {
                        setDefaultCompoundInformation(peakAreaBeanList[i]);
                    }
                }
            }

            peakAreaBeanList = peakAreaBeanList.OrderBy(n => n.PeakID).ToList();
            return peakAreaBeanList;
        }

        private static List<PeakAreaBean> getRefinedPeakAndDriftSpotListForTextDbResult(List<PeakAreaBean> peakSpots) {
            if (peakSpots.Count <= 1) return peakSpots;
            var allDriftSpots = new List<DriftSpotBean>();
            foreach (var peak in peakSpots) {
                foreach (var drift in peak.DriftSpots) {
                    allDriftSpots.Add(drift);
                }
            }

            allDriftSpots = allDriftSpots.OrderByDescending(n => n.PostIdentificationLibraryId).ToList();

            var currentLibraryId = allDriftSpots[0].LibraryID;
            var currentPeakId = 0;
            for (int i = 1; i < allDriftSpots.Count; i++) {
                if (allDriftSpots[i].PostIdentificationLibraryId < 0) break;
                if (allDriftSpots[i].PostIdentificationLibraryId != currentLibraryId) {
                    currentLibraryId = allDriftSpots[i].PostIdentificationLibraryId;
                    currentPeakId = i;
                    continue;
                }
                else {
                    if (allDriftSpots[currentPeakId].TotalScore < allDriftSpots[i].TotalScore) {
                        setDefaultCompoundInformation(allDriftSpots[currentPeakId]);
                        currentPeakId = i;
                    }
                    else {
                        setDefaultCompoundInformation(allDriftSpots[i]);
                    }
                }
            }

            foreach (var peakSpot in peakSpots) {
                var maxIdentScore = -1.0F;
                var maxIdentID = -1;
                var maxAnnotateScore = -1.0F;
                var maxAnnotateID = -1;

                for (int i = 0; i < peakSpot.DriftSpots.Count; i++) {
                    var dSpot = peakSpot.DriftSpots[i];
                    if (dSpot.PostIdentificationLibraryId >= 0 && !dSpot.MetaboliteName.Contains("w/o")) {
                        if (dSpot.TotalScore > maxIdentScore) {
                            maxIdentScore = dSpot.TotalScore;
                            maxIdentID = i;
                        }
                    }

                    if (dSpot.PostIdentificationLibraryId >= 0 && dSpot.MetaboliteName.Contains("w/o")) {
                        if (dSpot.TotalScore > maxAnnotateScore) {
                            maxAnnotateScore = dSpot.TotalScore;
                            maxAnnotateID = i;
                        }
                    }
                }

                if (maxIdentID >= 0) {
                    CopyMaxDriftSpotInfoToPeakSpot(peakSpot.DriftSpots[maxIdentID], peakSpot);
                }
                else if (maxAnnotateID >= 0) {
                    CopyMaxDriftSpotInfoToPeakSpot(peakSpot.DriftSpots[maxAnnotateID], peakSpot);
                }
                else {
                    setDefaultCompoundInformation(peakSpot);
                }
            }

            peakSpots = peakSpots.OrderBy(n => n.PeakID).ToList();
            return peakSpots;
        }

        private static void similarityCalculations(PeakAreaBean peakAreaBean, List<PostIdentificatioinReferenceBean> textDB, 
            AnalysisParametersBean param, ProjectPropertyBean project)
        {
            int maxSimilarityLibraryIndex = 0;
            float accurateMass = peakAreaBean.AccurateMass;
            float retentionTime = peakAreaBean.RtAtPeakTop;
            double totalSimilarity = 0, rtSimilarity = 0, accurateMassSimilarity = 0
                , maxRtSimilarity = double.MinValue, maxAccurateMassSimilarity = double.MinValue, maxTotalSimilarity = double.MinValue;

            int startIndex = DataAccessLcUtility.GetDatabaseStartIndex(accurateMass, param.AccurateMassToleranceOfPostIdentification, textDB);

            for (int i = startIndex; i < textDB.Count; i++)
            {
                if (textDB[i].AccurateMass < accurateMass - param.AccurateMassToleranceOfPostIdentification) continue;
                if (textDB[i].AccurateMass > accurateMass + param.AccurateMassToleranceOfPostIdentification) break;

                accurateMassSimilarity = -1; rtSimilarity = -1;
                accurateMassSimilarity = LcmsScoring.GetGaussianSimilarity(accurateMass, textDB[i].AccurateMass, param.AccurateMassToleranceOfPostIdentification);
                if (textDB[i].RetentionTime >= 0) rtSimilarity = LcmsScoring.GetGaussianSimilarity(peakAreaBean.RtAtPeakTop, textDB[i].RetentionTime, param.RetentionTimeToleranceOfPostIdentification);

                if (rtSimilarity < 0)
                    totalSimilarity = accurateMassSimilarity;
                else
                    totalSimilarity = (accurateMassSimilarity + rtSimilarity) * 0.5;

                if (totalSimilarity > maxTotalSimilarity) {
                    maxTotalSimilarity = totalSimilarity;
                    maxAccurateMassSimilarity = accurateMassSimilarity;
                    maxRtSimilarity = rtSimilarity;
                    maxSimilarityLibraryIndex = i;
                }
            }

            if (maxTotalSimilarity * 1000 > param.PostIdentificationScoreCutOff * 10) {
                setPostIdentifiedInformation(peakAreaBean, textDB, maxSimilarityLibraryIndex, maxRtSimilarity, maxAccurateMassSimilarity, maxTotalSimilarity);
                setAdductIonInformation(peakAreaBean, textDB[maxSimilarityLibraryIndex]);
            }
        }

        private static void similarityCalculationsForIonMobility(PeakAreaBean peakAreaBean, 
            List<PostIdentificatioinReferenceBean> textDB,
            AnalysisParametersBean param, ProjectPropertyBean project, int fileID) {
            int maxSimilarityLibraryIndex = 0;
            float accurateMass = peakAreaBean.AccurateMass;
            float retentionTime = peakAreaBean.RtAtPeakTop;
            double totalSimilarity = 0, 
                maxRtSimilarity = double.MinValue, 
                maxAccurateMassSimilarity = double.MinValue, 
                maxTotalSimilarity = double.MinValue,
                maxCcsSimilarity = double.MinValue;

            var ms1Tol = param.AccurateMassToleranceOfPostIdentification;
            var rtTol = param.RetentionTimeToleranceOfPostIdentification;
            var ccsTol = param.CcsSearchTolerance;

            int startIndex = DataAccessLcUtility.GetDatabaseStartIndex(accurateMass, ms1Tol, textDB);

            var maxIdentID = -1;
            var maxIdentScore = double.MinValue;
            var calinfo = param.FileidToCcsCalibrantData[fileID];
            for (int i = 0; i < peakAreaBean.DriftSpots.Count; i++) {
                var driftSpot = peakAreaBean.DriftSpots[i];
                for (int j = startIndex; j < textDB.Count; j++) {
                    if (textDB[j].AccurateMass < accurateMass - ms1Tol) continue;
                    if (textDB[j].AccurateMass > accurateMass + ms1Tol) break;

                    var accurateMassSimilarity = LcmsScoring.GetGaussianSimilarity(accurateMass, textDB[j].AccurateMass, ms1Tol);
                    var rtSimilarity = -1.0;
                    if (textDB[j].RetentionTime >= 0) rtSimilarity = LcmsScoring.GetGaussianSimilarity(peakAreaBean.RtAtPeakTop, textDB[j].RetentionTime, rtTol);
                    var ccsSimilarity = -1.0;
                    var ccs =  driftSpot.Ccs;
                    if (textDB[j].Ccs >= 0) {
                        var charge = getChargeValue(textDB[j], project.IonMode);
                        ccs = (float)IonMobilityUtility.MobilityToCrossSection(param.IonMobilityType, driftSpot.DriftTimeAtPeakTop, 
                            Math.Abs(charge), driftSpot.AccurateMass, calinfo, param.IsAllCalibrantDataImported);
                        ccsSimilarity = LcmsScoring.GetGaussianSimilarity(ccs, textDB[j].Ccs, ccsTol);
                    }

                    if (rtSimilarity < 0 && ccsSimilarity < 0)
                        totalSimilarity = accurateMassSimilarity;
                    else if (ccsSimilarity < 0) {
                        totalSimilarity = (accurateMassSimilarity + rtSimilarity) * 0.5;
                    }
                    else if (rtSimilarity < 0) {
                        totalSimilarity = (accurateMassSimilarity + ccsSimilarity) * 0.5;
                    }
                    else
                        totalSimilarity = (accurateMassSimilarity + rtSimilarity + ccsSimilarity) / 3.0;

                    if (totalSimilarity > maxTotalSimilarity) {
                        maxTotalSimilarity = totalSimilarity;
                        maxAccurateMassSimilarity = accurateMassSimilarity;
                        maxRtSimilarity = rtSimilarity;
                        maxCcsSimilarity = ccsSimilarity;
                        maxSimilarityLibraryIndex = j;
                    }
                }
                if (maxTotalSimilarity * 1000 > param.PostIdentificationScoreCutOff * 10) {
                    setPostIdentifiedInformation(driftSpot, textDB, maxSimilarityLibraryIndex, maxRtSimilarity,
                    maxAccurateMassSimilarity, maxCcsSimilarity, maxTotalSimilarity, project, param, calinfo);
                    setAdductIonInformation(driftSpot, textDB[maxSimilarityLibraryIndex]);
                }

                if (maxIdentScore < maxTotalSimilarity) {
                    maxIdentScore = maxTotalSimilarity;
                    maxIdentID = i;
                }
            }

            if (maxIdentID >= 0 && maxIdentScore * 1000 > param.PostIdentificationScoreCutOff * 10) {
                CopyMaxDriftSpotInfoToPeakSpot(peakAreaBean.DriftSpots[maxIdentID], peakAreaBean);
            }
        }

        private static int getChargeValue(PostIdentificatioinReferenceBean textQuery, IonMode ionMode) {
            var adduct = textQuery.AdductIon;
            if (adduct == null || adduct.FormatCheck == false) {
                if (ionMode == IonMode.Positive) {
                    adduct = AdductIonParcer.GetAdductIonBean("[M+H]+");
                }
                else {
                    adduct = AdductIonParcer.GetAdductIonBean("[M-H]-");
                }
            }

            return adduct.ChargeNumber;
        }

        private static void setPostIdentifiedInformation(PeakAreaBean peakAreaBean, List<PostIdentificatioinReferenceBean> postIdentificationReferenceBeanList, 
            int maxSimilarityLibraryIndex, double maxRtSimilarity, double maxAccurateMassSimilarity, double maxTotalSimilarity)
        {
            peakAreaBean.MetaboliteName = postIdentificationReferenceBeanList[maxSimilarityLibraryIndex].MetaboliteName;
            peakAreaBean.LibraryID = -1;
            peakAreaBean.PostIdentificationLibraryId = postIdentificationReferenceBeanList[maxSimilarityLibraryIndex].ReferenceID;
            peakAreaBean.Inchikey = postIdentificationReferenceBeanList[maxSimilarityLibraryIndex].Inchikey;
            peakAreaBean.MassSpectraSimilarityValue = -1;
            peakAreaBean.ReverseSearchSimilarityValue = -1;
            peakAreaBean.PresenseSimilarityValue = -1;
            peakAreaBean.AccurateMassSimilarity = (float)maxAccurateMassSimilarity * 1000;
            peakAreaBean.RtSimilarityValue = (float)maxRtSimilarity * 1000;
            peakAreaBean.IsotopeSimilarityValue = -1;
            peakAreaBean.TotalScore = (float)maxTotalSimilarity * 1000;

            peakAreaBean.IsMs1Match = true;
            peakAreaBean.IsMs2Match = false;
            if (maxRtSimilarity > 0)
                peakAreaBean.IsRtMatch = true;
        }

        private static void setAdductIonInformation(PeakAreaBean peakAreaBean, PostIdentificatioinReferenceBean postIdentificationReference)
        {
            if (postIdentificationReference.AdductIon != null && postIdentificationReference.AdductIon.FormatCheck == true)
            {
                peakAreaBean.AdductIonName = postIdentificationReference.AdductIon.AdductIonName;
                peakAreaBean.AdductIonXmer = postIdentificationReference.AdductIon.AdductIonXmer;
                peakAreaBean.AdductIonAccurateMass = (float)postIdentificationReference.AdductIon.AdductIonAccurateMass;
                peakAreaBean.AdductParent = -1;
                peakAreaBean.AdductIonChargeNumber = postIdentificationReference.AdductIon.ChargeNumber;
            }
        }

        private static void setPostIdentifiedInformation(DriftSpotBean driftSpot, List<PostIdentificatioinReferenceBean> textDB,
            int maxSimilarityLibraryIndex,
            double maxRtSimilarity, double maxAccurateMassSimilarity, double maxCcsSimilarity, double maxTotalSimilarity, 
            ProjectPropertyBean project, AnalysisParametersBean param, CoefficientsForCcsCalculation calinfo) {

            driftSpot.MetaboliteName = textDB[maxSimilarityLibraryIndex].MetaboliteName;
            driftSpot.LibraryID = -1;
            driftSpot.PostIdentificationLibraryId = textDB[maxSimilarityLibraryIndex].ReferenceID;
            driftSpot.Inchikey = textDB[maxSimilarityLibraryIndex].Inchikey;
            driftSpot.MassSpectraSimilarityValue = -1;
            driftSpot.ReverseSearchSimilarityValue = -1;
            driftSpot.PresenseSimilarityValue = -1;
            driftSpot.AccurateMassSimilarity = (float)maxAccurateMassSimilarity * 1000;
            driftSpot.RtSimilarityValue = (float)maxRtSimilarity * 1000;
            driftSpot.IsotopeSimilarityValue = -1;
            driftSpot.CcsSimilarity = (float)maxCcsSimilarity * 1000;
            driftSpot.TotalScore = (float)maxTotalSimilarity * 1000;
            driftSpot.IsMs1Match = true;
            driftSpot.IsMs2Match = false;

            var adductObj = getAdductObj(textDB[maxSimilarityLibraryIndex].AdductIon, project.IonMode);
            driftSpot.AdductIonAccurateMass = (float)adductObj.AdductIonAccurateMass;
            driftSpot.AdductIonChargeNumber = adductObj.ChargeNumber;
            driftSpot.ChargeNumber = adductObj.ChargeNumber;
            driftSpot.AdductIonName = adductObj.AdductIonName;
            driftSpot.AdductIonXmer = adductObj.AdductIonXmer;

            driftSpot.Ccs = (float)IonMobilityUtility.MobilityToCrossSection(param.IonMobilityType, driftSpot.DriftTimeAtPeakTop,
                            Math.Abs(driftSpot.ChargeNumber), driftSpot.AccurateMass, calinfo, param.IsAllCalibrantDataImported);

            if (maxRtSimilarity > 0)
                driftSpot.IsRtMatch = true;
            if (maxCcsSimilarity > 0)
                driftSpot.IsCcsMatch = true;

        }

        private static Rfx.Riken.OsakaUniv.AdductIon getAdductObj(Rfx.Riken.OsakaUniv.AdductIon adductIon, IonMode ionMode) {
            if (adductIon != null && adductIon.FormatCheck) return adductIon;
            if (ionMode == IonMode.Positive) {
                return AdductIonParcer.GetAdductIonBean("[M+H]+");
            }
            else {
                return AdductIonParcer.GetAdductIonBean("[M-H]-");
            }
        }

        private static void setAdductIonInformation(DriftSpotBean driftSpot, PostIdentificatioinReferenceBean postIdentificationReference) {
            if (postIdentificationReference.AdductIon != null && postIdentificationReference.AdductIon.FormatCheck == true) {
                driftSpot.AdductIonName = postIdentificationReference.AdductIon.AdductIonName;
                driftSpot.AdductIonXmer = postIdentificationReference.AdductIon.AdductIonXmer;
                driftSpot.AdductIonAccurateMass = (float)postIdentificationReference.AdductIon.AdductIonAccurateMass;
                driftSpot.AdductParent = -1;
                driftSpot.AdductIonChargeNumber = postIdentificationReference.AdductIon.ChargeNumber;
            }
        }
    }
}

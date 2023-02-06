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
using CompMs.Common.DataObj;

namespace Msdial.Lcms.Dataprocess.Algorithm
{
    public sealed class IdentificationForAif
    {
        private IdentificationForAif() { }

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
        /// <param name="peakAreaBeanList"></param>
        /// <param name="mspFormatCompoundInformationBeanList"></param>
        /// <param name="analysisParametersBean"></param>
        /// <param name="projectPropertyBean"></param>
        public static void CompoundIdentification(List<string> files, ObservableCollection<RawSpectrum> spectrumCollection,
            List<PeakAreaBean> peakAreaBeanList, List<MspFormatCompoundInformationBean> mspFormatCompoundInformationBeanList,
            AnalysisParametersBean analysisParametersBean, ProjectPropertyBean projectPropertyBean, Action<int> reportAction) {
            if (mspFormatCompoundInformationBeanList == null || mspFormatCompoundInformationBeanList.Count == 0) return;
            if (projectPropertyBean.IsLabPrivateVersionTada) { analysisParametersBean.IsUseSimpleDotScore = true; }
            for (var i = 0; i < projectPropertyBean.Ms2LevelIdList.Count; i++) {
                MainProcess(files[i], spectrumCollection, peakAreaBeanList, mspFormatCompoundInformationBeanList, analysisParametersBean, projectPropertyBean, reportAction, i);
            }
            if (analysisParametersBean.OnlyReportTopHitForPostAnnotation)
                peakAreaBeanList = getRefinedPeakAreaBeanListForMspSearchResult(peakAreaBeanList);
        }

        private static void MainProcess(string file, ObservableCollection<RawSpectrum> spectrumCollection,
            List<PeakAreaBean> peakAreaBeanList, List<MspFormatCompoundInformationBean> mspFormatCompoundInformationBeanList,
            AnalysisParametersBean analysisParametersBean, ProjectPropertyBean projectPropertyBean, Action<int> reportAction, int AifId) { 
            var seekpointList = new List<long>();
            var ms1Spectra = new ObservableCollection<double[]>();
            var ms2Spectra = new ObservableCollection<double[]>();
            var deconvolutionResultBean = new MS2DecResult();

            var numDeconvolution = (double)projectPropertyBean.Ms2LevelIdList.Count;
            var progressMaxAif = progressMax / numDeconvolution;
            var initialProgressAif = initialProgress + progressMaxAif * AifId;

            using (var fs = File.Open(file, FileMode.Open, FileAccess.ReadWrite)) {
                seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
                for (int i = 0; i < peakAreaBeanList.Count; i++) {
                    if (peakAreaBeanList[i].IsotopeWeightNumber != 0) continue;
                    if (AifId == 0) {
                        peakAreaBeanList[i].TotalScore = -1;
                        peakAreaBeanList[i].TotalScoreList = new List<float>();
                        peakAreaBeanList[i].LibraryIDList = new List<int>();
                    }
                    peakAreaBeanList[i].TotalScoreList.Add(-1);
                    peakAreaBeanList[i].LibraryIDList.Add(-1);

                    ms1Spectra = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection, projectPropertyBean.DataType, peakAreaBeanList[i].Ms1LevelDatapointNumber, analysisParametersBean.CentroidMs1Tolerance, analysisParametersBean.PeakDetectionBasedCentroid);
                    deconvolutionResultBean = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, i);

                    if (deconvolutionResultBean.MassSpectra.Count != 0) {
                        ms2Spectra = getMs2Spectra(deconvolutionResultBean.MassSpectra, analysisParametersBean);
                        if (ms2Spectra != null && ms2Spectra.Count != 0)
                            similarityCalculationsMsMsIncluded(peakAreaBeanList[i], ms1Spectra, ms2Spectra, mspFormatCompoundInformationBeanList, analysisParametersBean, projectPropertyBean, AifId);
                    }
                    else {
                        similarityCalculationsWithoutMsMs(peakAreaBeanList[i], ms1Spectra, mspFormatCompoundInformationBeanList, analysisParametersBean, AifId);
                    }
                    progressReports(i, peakAreaBeanList.Count, initialProgressAif, progressMaxAif, reportAction);
                }
            }
        }

        public static void MainProcessForCorrDec(AlignmentResultBean alignmentResult, AlignmentFileBean alignmentFile,
            List<MspFormatCompoundInformationBean> mspFormatCompoundInformationBeanList,
            AnalysisParametersBean analysisParametersBean, ProjectPropertyBean projectProperty, Action<int> reportAction)
        {
            var seekpointList = new List<long>();
            var ms1Spectra = new ObservableCollection<double[]>();
            var ms2Spectra = new ObservableCollection<double[]>();
            var deconvolutionResultBean = new MS2DecResult();
            var alignmentProperties = alignmentResult.AlignmentPropertyBeanCollection;
            if (alignmentProperties.Count == 0) return;
            var fslist = new List<FileStream>();
            var seekpointlist = new List<List<long>>();
            var numMs2Scans = projectProperty.Ms2LevelIdList.Count;
            if (numMs2Scans == 0) numMs2Scans = 1;
                for (var i =0; i < numMs2Scans; i++)
            {
                var filePath = Path.Combine(projectProperty.ProjectFolderPath, alignmentFile.FileName + "_CorrelationBasedDecRes_Raw_" + i + ".cbd");
                var fs = File.Open(filePath, FileMode.Open);
                fslist.Add(fs);
                seekpointlist.Add(CorrDecHandler.ReadSeekPointsOfCorrelDec(fs));
            }

            var reportStepSize = (int)(alignmentProperties.Count / 100);
            var numSamples = alignmentResult.AlignmentPropertyBeanCollection[0].AlignedPeakPropertyBeanCollection.Count;
            for (var alignmentId = 0; alignmentId < alignmentResult.AlignmentPropertyBeanCollection.Count; alignmentId++)
            {
                var spot = alignmentProperties[alignmentId];
                var mz = spot.AlignedPeakPropertyBeanCollection[spot.RepresentativeFileID].AccurateMass;
                var rt = spot.AlignedPeakPropertyBeanCollection[spot.RepresentativeFileID].RetentionTime;
                var pab = new PeakAreaBean() { AccurateMass = mz, RtAtPeakTop = rt, AdductIonName = spot.AdductIonName};

                for (var aifId = 0; aifId < numMs2Scans; aifId++)
                {
                    if (aifId == 0)
                    {
                        pab.TotalScore = -1;
                        pab.TotalScoreList = new List<float>();
                        pab.LibraryIDList = new List<int>();
                    }
                    pab.TotalScoreList.Add(-1);
                    pab.LibraryIDList.Add(-1);

                    var corrDecRes = CorrDecHandler.ReadCorrelDecResult(fslist[aifId], seekpointlist[aifId], alignmentId);
                    var corrDecSpectra = CorrDecHandler.GetCorrDecSpectrum(corrDecRes, analysisParametersBean.AnalysisParamOfMsdialCorrDec, mz, (int)(numSamples * spot.FillParcentage));
                    if (corrDecSpectra == null || corrDecSpectra.Count == 0) continue;
                    similarityCalculationsMsMsIncluded(pab, ms1Spectra, new ObservableCollection<double[]>(corrDecSpectra), mspFormatCompoundInformationBeanList, analysisParametersBean, projectProperty, aifId);
                }
                
                if(pab.SimpleDotProductSimilarity > 0)
                {
                    //Console.WriteLine(alignmentId + "\t" + pab.MetaboliteName + "\t" + pab.SimpleDotProductSimilarity + "\t" + spot.TotalSimilairty + "\t" + pab.TotalScore);
                }

                if (spot.TotalSimilairty < pab.TotalScore &&                   
                    pab.SimpleDotProductSimilarity > 200)
                {
                    spot.LibraryID = pab.LibraryID;
                    spot.AdductIonName = pab.AdductIonName;
                    spot.MetaboliteName = pab.MetaboliteName;
                    spot.TotalSimilairty = pab.TotalScore;
                    spot.MassSpectraSimilarity = pab.MassSpectraSimilarityValue;
                    spot.ReverseSimilarity = pab.ReverseSearchSimilarityValue;
                    spot.FragmentPresencePercentage = pab.PresenseSimilarityValue;
                    spot.RetentionTimeSimilarity = pab.RtSimilarityValue;
                    spot.AccurateMassSimilarity = pab.AccurateMassSimilarity;
                    spot.SimpleDotProductSimilarity = pab.SimpleDotProductSimilarity;

                    spot.IsMs1Match = pab.IsMs1Match;
                    spot.IsMs2Match = pab.IsMs2Match;
                    spot.IsRtMatch = pab.IsRtMatch;

                    spot.IsLipidClassMatch = pab.IsLipidClassMatch;
                    spot.IsLipidChainsMatch = pab.IsLipidChainsMatch;
                    spot.IsLipidPositionMatch = pab.IsLipidPositionMatch;
                    spot.IsOtherLipidMatch = pab.IsOtherLipidMatch;
                    //alignmentProperty.IdentificationRank = pab.IdentificationRank;
                    spot.CorrelBasedlibraryIdList = pab.LibraryIDList;
                }
                if (alignmentId % reportStepSize == 0)
                {
                    reportAction?.Invoke(1);
                }
            }
            for (var i = 0; i < projectProperty.Ms2LevelIdList.Count; i++)
            {
                fslist[i].Dispose();
                fslist[i].Close();
            }
        }

        private static List<PeakAreaBean> getRefinedPeakAreaBeanListForMspSearchResult(List<PeakAreaBean> peakAreaBeanList) {
            if (peakAreaBeanList.Count <= 1) return peakAreaBeanList;
            peakAreaBeanList = peakAreaBeanList.OrderByDescending(n => n.LibraryID).ToList();

            var currentLibraryId = peakAreaBeanList[0].LibraryID;
            var currentPeakId = 0;
            for (int i = 1; i < peakAreaBeanList.Count; i++) {
                if (peakAreaBeanList[i].LibraryID < 0) break;
                if (peakAreaBeanList[i].LibraryID != currentLibraryId) {
                    currentLibraryId = peakAreaBeanList[i].LibraryID;
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

        private static void progressReports(int currentProgress, int maxProgress, double initialProgressAif, double progressMaxAif, Action<int> reportAction) {
            var progress = initialProgressAif + (double)currentProgress / (double)maxProgress * progressMaxAif;
            reportAction?.Invoke((int)progress);
        }

        private static ObservableCollection<double[]> getMs2Spectra(List<double[]> masslist, AnalysisParametersBean analysisParametersBean) {
            float cutoff = analysisParametersBean.RelativeAbundanceCutOff;
            double maxIntensity = double.MinValue;

            var massCollection = new ObservableCollection<double[]>();

            foreach (var peak in masslist) {
                if (peak[1] > maxIntensity) maxIntensity = peak[1];
                massCollection.Add(peak);
            }

            if (cutoff <= 0.0) return massCollection;
            else {
                massCollection = new ObservableCollection<double[]>();
                foreach (var peak in masslist)
                    if (peak[1] > maxIntensity * cutoff * 0.01) massCollection.Add(peak);
                return massCollection;
            }
        }

        private static void similarityCalculationsWithoutMsMs(PeakAreaBean peakAreaBean,
            ObservableCollection<double[]> ms1Spectra,
            List<MspFormatCompoundInformationBean> mspFormatCompoundInformationBeanList,
            AnalysisParametersBean analysisParametersBean, int AifId) {
            int maxSimilarityWithoutMsMsLibraryIndex = 0;
            float accurateMass = peakAreaBean.AccurateMass;
            float retentionTime = peakAreaBean.RtAtPeakTop;
            double totalSimilarity2 = 0, rtSimilarity = 0, isotopeSimilarity = 0, accurateMassSimilarity = 0
                , maxRtSimilarity2 = double.MinValue, maxIsotopeSimilarity2 = double.MinValue
                , maxAccurateMassSimilarity2 = double.MinValue, maxTotalSimilarity2 = double.MinValue;

            int startIndex = DataAccessLcUtility.GetDatabaseStartIndex(accurateMass, analysisParametersBean.Ms1LibrarySearchTolerance, mspFormatCompoundInformationBeanList);

            for (int i = startIndex; i < mspFormatCompoundInformationBeanList.Count; i++) {
                if (mspFormatCompoundInformationBeanList[i].PrecursorMz > accurateMass + analysisParametersBean.Ms1LibrarySearchTolerance) break;
                if (mspFormatCompoundInformationBeanList[i].PrecursorMz < accurateMass - analysisParametersBean.Ms1LibrarySearchTolerance) continue;

                rtSimilarity = -1; isotopeSimilarity = -1; accurateMassSimilarity = -1;

                accurateMassSimilarity = LcmsScoring.GetGaussianSimilarity(accurateMass, mspFormatCompoundInformationBeanList[i].PrecursorMz, analysisParametersBean.Ms1LibrarySearchTolerance);
                if (mspFormatCompoundInformationBeanList[i].RetentionTime >= 0) rtSimilarity = LcmsScoring.GetGaussianSimilarity(peakAreaBean.RtAtPeakTop, mspFormatCompoundInformationBeanList[i].RetentionTime, analysisParametersBean.RetentionTimeLibrarySearchTolerance);
                if (mspFormatCompoundInformationBeanList[i].IsotopeRatioList.Count != 0) isotopeSimilarity = LcmsScoring.GetIsotopeRatioSimilarity(ms1Spectra, mspFormatCompoundInformationBeanList[i].IsotopeRatioList, accurateMass, analysisParametersBean.Ms1LibrarySearchTolerance);

                totalSimilarity2 = LcmsScoring.GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, isotopeSimilarity, analysisParametersBean.IsUseRetentionInfoForIdentificationScoring);

                if (totalSimilarity2 > maxTotalSimilarity2) { maxTotalSimilarity2 = totalSimilarity2; maxAccurateMassSimilarity2 = accurateMassSimilarity; maxRtSimilarity2 = rtSimilarity; maxIsotopeSimilarity2 = isotopeSimilarity; maxSimilarityWithoutMsMsLibraryIndex = i; }
            }

            if (maxTotalSimilarity2 * 100 > analysisParametersBean.IdentificationScoreCutOff)
            {
                var repQuery = mspFormatCompoundInformationBeanList[maxSimilarityWithoutMsMsLibraryIndex];
                var rtDiff = Math.Abs(retentionTime - repQuery.RetentionTime);
                var isRtMatch = rtDiff < analysisParametersBean.RetentionTimeLibrarySearchTolerance;
                setAnnotatedInformation(peakAreaBean, repQuery, maxAccurateMassSimilarity2, maxRtSimilarity2, maxIsotopeSimilarity2, maxTotalSimilarity2, AifId, isRtMatch);
            }
            else setDefaultCompoundInformation(peakAreaBean, AifId);
        }

        private static void similarityCalculationsMsMsIncluded(PeakAreaBean peakAreaBean, ObservableCollection<double[]> ms1Spectra, ObservableCollection<double[]> ms2Spectra,
            List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param, ProjectPropertyBean project,
            int AifId) {

            int maxSpectraSimilarityLibraryIndex = 0, maxSimilarityWithoutMsMsLibraryIndex = 0;
            float mz = peakAreaBean.AccurateMass;
            float rt = peakAreaBean.RtAtPeakTop;
            var massBegin = param.Ms2MassRangeBegin;
            var massEnd = param.Ms2MassRangeEnd;
            var ms2Tol = param.Ms2LibrarySearchTolerance;

            double totalSimilarity1 = 0, totalSimilarity2 = 0, spectraSimilarity = 0, reverseSearchSimilarity = 0, simpleDotProduct = 0, rtSimilarity = 0, isotopeSimilarity = 0, accurateMassSimilarity = 0, presenseSimilarity = 0
                , maxTotalSimilarity1 = double.MinValue, maxSpectraSimilarity = double.MinValue, maxReverseSimilarity = double.MinValue, maxPresenseSimilarity = double.MinValue
                , maxRtSimilarity1 = double.MinValue, maxRtSimilarity2 = double.MinValue, maxIsotopeSimilarity1 = double.MinValue, maxIsotopeSimilarity2 = double.MinValue
                , maxAccurateMassSimilarity1 = double.MinValue, maxAccurateMassSimilarity2 = double.MinValue, maxTotalSimilarity2 = double.MinValue, maxSimpleDotScore = double.MinValue;

            int startIndex = DataAccessLcUtility.GetDatabaseStartIndex(mz, param.Ms1LibrarySearchTolerance, mspDB);

            for (int i = startIndex; i < mspDB.Count; i++) {
                if (mspDB[i].PrecursorMz > mz + param.Ms1LibrarySearchTolerance) break;
                if (mspDB[i].PrecursorMz < mz - param.Ms1LibrarySearchTolerance) continue;
                var query = mspDB[i];
                if (query.RetentionTime >= 0 && param.IsUseRetentionInfoForIdentificationFiltering)
                {
                    var refRt = query.RetentionTime;
                    if (Math.Abs(refRt - rt) > param.RetentionTimeLibrarySearchTolerance) continue;
                }

                rtSimilarity = -1; isotopeSimilarity = -1; spectraSimilarity = -1; reverseSearchSimilarity = -1; accurateMassSimilarity = -1; presenseSimilarity = -1;

                accurateMassSimilarity = LcmsScoring.GetGaussianSimilarity(mz, mspDB[i].PrecursorMz, param.Ms1LibrarySearchTolerance);
                var pres1 = -1.0;
                var pres2 = -1.0;

                var dot1 = LcmsScoring.GetMassSpectraSimilarity(ms2Spectra, mspDB[i].MzIntensityCommentBeanList, ms2Tol, massBegin, massEnd);
                var rev1 = LcmsScoring.GetReverseSearchSimilarity(ms2Spectra, mspDB[i].MzIntensityCommentBeanList, ms2Tol, massBegin, massEnd);
                try
                {
                    pres1 = LcmsScoring.GetFragmentPresenceScore(ms2Spectra, mspDB[i], ms2Tol, massBegin, massEnd, project.IonMode, project.TargetOmics);                
                }
                catch
                {
                    pres1 = -1;
                }
                var simple1 = LcmsScoring.GetSimpleDotProductSimilarity(ms2Spectra, mspDB[i].MzIntensityCommentBeanList, ms2Tol, massBegin, massEnd);

                var dot2 = LcmsScoring.GetMassSpectraSimilarity(ms2Spectra, mspDB[i].MzIntensityCommentBeanList, ms2Tol, massBegin, mz+0.5f);
                var rev2 = LcmsScoring.GetReverseSearchSimilarity(ms2Spectra, mspDB[i].MzIntensityCommentBeanList, ms2Tol, massBegin, mz + 0.5f);
                try
                {
                    pres2 = LcmsScoring.GetFragmentPresenceScore(ms2Spectra, mspDB[i], ms2Tol, massBegin, mz + 0.5f, project.IonMode, project.TargetOmics);
                }
                catch
                {
                    pres2 = -1;
                }
                var simple2 = LcmsScoring.GetSimpleDotProductSimilarity(ms2Spectra, mspDB[i].MzIntensityCommentBeanList, ms2Tol, massBegin, mz + 0.5f);

                if (dot2 + rev2 + pres2 > dot1 + rev1 + pres1)
                {
                    spectraSimilarity = dot2;
                    reverseSearchSimilarity = rev2;
                    presenseSimilarity = pres2;
                }
                else
                {
                    spectraSimilarity = dot1;
                    reverseSearchSimilarity = rev1;
                    presenseSimilarity = pres1;
                }

                simpleDotProduct = simple1 > simple2 ? simple1 : simple2;

                if (mspDB[i].RetentionTime >= 0) rtSimilarity = LcmsScoring.GetGaussianSimilarity(peakAreaBean.RtAtPeakTop, mspDB[i].RetentionTime, param.RetentionTimeLibrarySearchTolerance);
                if (mspDB[i].IsotopeRatioList.Count != 0) isotopeSimilarity = LcmsScoring.GetIsotopeRatioSimilarity(ms1Spectra, mspDB[i].IsotopeRatioList, mz, param.Ms1LibrarySearchTolerance);

                var spectrumPenalty = false;
                if (mspDB[i].MzIntensityCommentBeanList != null && mspDB[i].MzIntensityCommentBeanList.Count <= 1) spectrumPenalty = true;

                if (param.IsUseSimpleDotScore)
                {
                    totalSimilarity1 = LcmsScoring.GetTotalSimilarityUsingSimpleDotProduct(accurateMassSimilarity, rtSimilarity, isotopeSimilarity, simpleDotProduct, spectrumPenalty, project.TargetOmics, param.IsUseRetentionInfoForIdentificationScoring);
                }
                else
                {
                    totalSimilarity1 = LcmsScoring.GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, isotopeSimilarity, spectraSimilarity, reverseSearchSimilarity, presenseSimilarity, spectrumPenalty, project.TargetOmics, param.IsUseRetentionInfoForIdentificationScoring);
                }
                totalSimilarity2 = LcmsScoring.GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, isotopeSimilarity, param.IsUseRetentionInfoForIdentificationScoring);
                if (totalSimilarity1 > maxTotalSimilarity1 && presenseSimilarity > 0.4) {
                    maxTotalSimilarity1 = totalSimilarity1; maxSpectraSimilarity = spectraSimilarity; maxPresenseSimilarity = presenseSimilarity;
                    maxReverseSimilarity = reverseSearchSimilarity; maxAccurateMassSimilarity1 = accurateMassSimilarity;
                    maxIsotopeSimilarity1 = isotopeSimilarity; maxRtSimilarity1 = rtSimilarity; maxSpectraSimilarityLibraryIndex = i;
                    maxSimpleDotScore = simpleDotProduct;
                }
                if (totalSimilarity2 > maxTotalSimilarity2) { maxTotalSimilarity2 = totalSimilarity2; maxAccurateMassSimilarity2 = accurateMassSimilarity; maxRtSimilarity2 = rtSimilarity; maxIsotopeSimilarity2 = isotopeSimilarity; maxSimilarityWithoutMsMsLibraryIndex = i; }
            }

            if (maxTotalSimilarity1 * 100 > param.IdentificationScoreCutOff &&
                (maxSpectraSimilarity > 0.15 || maxReverseSimilarity > 0.5))
            {
                var repQuery = mspDB[maxSpectraSimilarityLibraryIndex];
                var annotatedName = repQuery.Name;
                var compoundClass = repQuery.CompoundClass;
                var adduct = repQuery.AdductIonBean.AdductIonName;
                var rtDiff = Math.Abs(rt - repQuery.RetentionTime);
                var isRtMatch = rtDiff < param.RetentionTimeLibrarySearchTolerance;
                if (project.TargetOmics == TargetOmics.Lipidomics)
                {
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
                if (repQuery.RetentionTime >= 0 && param.IsUseRetentionInfoForIdentificationFiltering)
                {
                    if (rtDiff > param.RetentionTimeLibrarySearchTolerance)
                        annotatedName = string.Empty;
                }

                if (annotatedName != string.Empty)
                {
                    setIdentifiedInformation(peakAreaBean, repQuery, maxSpectraSimilarity, maxReverseSimilarity, maxPresenseSimilarity, maxSimpleDotScore, maxAccurateMassSimilarity1, maxRtSimilarity1, maxIsotopeSimilarity1, maxTotalSimilarity1, AifId, isRtMatch);
                }
                else
                {
                    if (maxTotalSimilarity2 * 100 > param.IdentificationScoreCutOff)
                    {
                        repQuery = mspDB[maxSpectraSimilarityLibraryIndex];
                        rtDiff = Math.Abs(rt - repQuery.RetentionTime);
                        isRtMatch = rtDiff < param.RetentionTimeLibrarySearchTolerance;
                        setAnnotatedInformation(peakAreaBean, mspDB[maxSimilarityWithoutMsMsLibraryIndex], maxAccurateMassSimilarity2, maxRtSimilarity2, maxIsotopeSimilarity2, maxTotalSimilarity2, AifId, isRtMatch);
                    }
                    else
                    {
                        setDefaultCompoundInformation(peakAreaBean, AifId);
                    }
                    return;
                }
                #endregion
            }
            else if (maxTotalSimilarity1 * 100 > 60.0)
            {
                var repQuery = mspDB[maxSpectraSimilarityLibraryIndex];
                var rtDiff = Math.Abs(rt - repQuery.RetentionTime);
                var isRtMatch = rtDiff < param.RetentionTimeLibrarySearchTolerance;
                setAnnotatedInformation(peakAreaBean, mspDB[maxSpectraSimilarityLibraryIndex],
                    maxAccurateMassSimilarity1, maxRtSimilarity1,
                    maxIsotopeSimilarity1, maxTotalSimilarity1, AifId, isRtMatch);
            }
            else if (maxTotalSimilarity2 * 100 > param.IdentificationScoreCutOff)
            {
                var repQuery = mspDB[maxSimilarityWithoutMsMsLibraryIndex];
                var rtDiff = Math.Abs(rt - repQuery.RetentionTime);
                var isRtMatch = rtDiff < param.RetentionTimeLibrarySearchTolerance;
                setAnnotatedInformation(peakAreaBean, repQuery, maxAccurateMassSimilarity2, maxRtSimilarity2, maxIsotopeSimilarity2, maxTotalSimilarity2, AifId, isRtMatch);
            }
            else setDefaultCompoundInformation(peakAreaBean, AifId);
        }

        private static void setDefaultPeakSpotProperties(PeakAreaBean peakSpot)
        {
            peakSpot.IsRtMatch = false;
            peakSpot.IsCcsMatch = false;
            peakSpot.IsMs1Match = false;
            peakSpot.IsMs2Match = false;
        }

        private static void setPeakSpotProperties(PeakAreaBean peakSpot, bool isRtMatch, bool isMs1Match, bool isMs2Match)
        {
            peakSpot.IsRtMatch = isRtMatch;
            peakSpot.IsCcsMatch = false;
            peakSpot.IsMs1Match = isMs1Match;
            peakSpot.IsMs2Match = isMs2Match;
        }


        private static void setDefaultCompoundInformation(PeakAreaBean peakAreaBean) {
            if (peakAreaBean.TotalScoreList != null && peakAreaBean.TotalScoreList.Count > 0) {
                var numDec =  peakAreaBean.TotalScoreList.Count;

                peakAreaBean.TotalScoreList.Add(-1);
                peakAreaBean.LibraryIDList.Add(-1);

            }
            peakAreaBean.MetaboliteName = string.Empty;
            peakAreaBean.LibraryID = -1;
            peakAreaBean.Inchikey = string.Empty;
            peakAreaBean.PostIdentificationLibraryId = -1;
            peakAreaBean.MassSpectraSimilarityValue = -1;
            peakAreaBean.ReverseSearchSimilarityValue = -1;
            peakAreaBean.PresenseSimilarityValue = -1;
            peakAreaBean.SimpleDotProductSimilarity = -1;
            peakAreaBean.AccurateMassSimilarity = -1;
            peakAreaBean.CcsSimilarity = -1;
            peakAreaBean.RtSimilarityValue = -1;
            peakAreaBean.IsotopeSimilarityValue = -1;
            peakAreaBean.TotalScore = -1;
        }

        private static void setDefaultCompoundInformation(PeakAreaBean peakAreaBean,int AifId) {
            peakAreaBean.TotalScoreList[AifId] = -1;
            peakAreaBean.LibraryIDList[AifId] = -1;
            if (peakAreaBean.TotalScore < 0) {
                peakAreaBean.MetaboliteName = string.Empty;
                peakAreaBean.LibraryID = -1;
                peakAreaBean.PostIdentificationLibraryId = -1;
                peakAreaBean.Inchikey = string.Empty;
                peakAreaBean.MassSpectraSimilarityValue = -1;
                peakAreaBean.ReverseSearchSimilarityValue = -1;
                peakAreaBean.PresenseSimilarityValue = -1;
                peakAreaBean.SimpleDotProductSimilarity = -1;
                peakAreaBean.AccurateMassSimilarity = -1;
                peakAreaBean.CcsSimilarity = -1;
                peakAreaBean.RtSimilarityValue = -1;
                peakAreaBean.IsotopeSimilarityValue = -1;
                peakAreaBean.TotalScore = -1;
                setDefaultPeakSpotProperties(peakAreaBean);
            }
        }

        private static void setAnnotatedInformation(PeakAreaBean peakAreaBean, MspFormatCompoundInformationBean mspFormatCompoundInformationBean, double maxAccurateMassSimilarity, 
            double maxRtSimilarity, double maxIsotopeSimilarity, double maxTotalSimilarity, int AifId, bool isRtMatch) {
            peakAreaBean.TotalScoreList[AifId] = (float)maxTotalSimilarity * 1000;
            peakAreaBean.LibraryIDList[AifId] = mspFormatCompoundInformationBean.Id;
            if (peakAreaBean.TotalScore < 0 || peakAreaBean.TotalScore < peakAreaBean.TotalScoreList[AifId]) {
                peakAreaBean.MetaboliteName = "w/o MS2:" + mspFormatCompoundInformationBean.Name;
                peakAreaBean.LibraryID = mspFormatCompoundInformationBean.Id;
                peakAreaBean.Inchikey = mspFormatCompoundInformationBean.InchiKey;
                peakAreaBean.PostIdentificationLibraryId = -1;
                peakAreaBean.MassSpectraSimilarityValue = -1;
                peakAreaBean.ReverseSearchSimilarityValue = -1;
                peakAreaBean.PresenseSimilarityValue = -1;
                peakAreaBean.SimpleDotProductSimilarity = -1;
                peakAreaBean.CcsSimilarity = -1;
                peakAreaBean.AccurateMassSimilarity = (float)maxAccurateMassSimilarity * 1000;
                peakAreaBean.RtSimilarityValue = (float)maxRtSimilarity * 1000;
                peakAreaBean.IsotopeSimilarityValue = (float)maxIsotopeSimilarity * 1000;
                peakAreaBean.TotalScore = (float)maxTotalSimilarity * 1000;
                setPeakSpotProperties(peakAreaBean, isRtMatch, true, false);
                setAdductIonInformation(peakAreaBean, mspFormatCompoundInformationBean);
            }
        }

        private static void setIdentifiedInformation(PeakAreaBean peakAreaBean, MspFormatCompoundInformationBean mspFormatCompoundInformationBean, double maxSpectraSimilarity, 
            double maxReverseSimilarity, double maxPresenseSimilarity, double maxSimpleDotScore, double maxAccurateMassSimilarity, double maxRtSimilarity, double maxIsotopeSimilarity, 
            double maxTotalSimilarity, int AifId, bool isRtMatch) {
                peakAreaBean.TotalScoreList[AifId] = (float)maxTotalSimilarity * 1000;
                peakAreaBean.LibraryIDList[AifId] = mspFormatCompoundInformationBean.Id;

            if (peakAreaBean.TotalScore < 0 || peakAreaBean.TotalScore < peakAreaBean.TotalScoreList[AifId]) {
                peakAreaBean.MetaboliteName = mspFormatCompoundInformationBean.Name;
                peakAreaBean.LibraryID = mspFormatCompoundInformationBean.Id;
                peakAreaBean.Inchikey = mspFormatCompoundInformationBean.InchiKey;
                peakAreaBean.PostIdentificationLibraryId = -1;
                peakAreaBean.MassSpectraSimilarityValue = (float)maxSpectraSimilarity * 1000;
                peakAreaBean.ReverseSearchSimilarityValue = (float)maxReverseSimilarity * 1000;
                peakAreaBean.PresenseSimilarityValue = (float)maxPresenseSimilarity * 1000;
                peakAreaBean.SimpleDotProductSimilarity = (float)maxSimpleDotScore * 1000;
                peakAreaBean.AccurateMassSimilarity = (float)maxAccurateMassSimilarity * 1000;
                peakAreaBean.RtSimilarityValue = (float)maxRtSimilarity * 1000;
                peakAreaBean.IsotopeSimilarityValue = (float)maxIsotopeSimilarity * 1000;
                peakAreaBean.TotalScore = (float)maxTotalSimilarity * 1000;
                peakAreaBean.CcsSimilarity = -1;
                setPeakSpotProperties(peakAreaBean, isRtMatch, true, true);
                setAdductIonInformation(peakAreaBean, mspFormatCompoundInformationBean);
            }
        }

        private static void setAdductIonInformation(PeakAreaBean peakAreaBean, MspFormatCompoundInformationBean mspFormatCompoundInformationBean) {
            if (mspFormatCompoundInformationBean.AdductIonBean != null && mspFormatCompoundInformationBean.AdductIonBean.FormatCheck == true) {
                peakAreaBean.AdductIonName = mspFormatCompoundInformationBean.AdductIonBean.AdductIonName;
                peakAreaBean.AdductIonXmer = mspFormatCompoundInformationBean.AdductIonBean.AdductIonXmer;
                peakAreaBean.AdductIonAccurateMass = mspFormatCompoundInformationBean.AdductIonBean.AdductIonAccurateMass;
                peakAreaBean.AdductParent = -1;
                peakAreaBean.AdductIonChargeNumber = mspFormatCompoundInformationBean.AdductIonBean.ChargeNumber;
            }
        }
    }
}
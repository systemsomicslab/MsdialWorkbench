using CompMs.Common.DataObj;
using Msdial.Lcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Msdial.Lcms.Dataprocess.Algorithm
{
    public sealed class PeakAlignment
    {
        /// <summary>
        /// MS-DIAL first prepares the 'master(reference)' peak list which should include all peak information detected in all sample files.
        /// </summary>
        public static List<PeakAreaBean> GetJointAlignerMasterList(RdamPropertyBean rdamProperty, 
            ObservableCollection<AnalysisFileBean> files, AnalysisParametersBean param)
        {
            var analysisFile = files[param.AlignmentReferenceFileID];
            DataStorageLcUtility.SetPeakAreaBeanCollection(analysisFile, analysisFile.AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

            var masterPeaklist = getInitialMasterPeaklist(new List<PeakAreaBean>(analysisFile.PeakAreaBeanCollection), param);
            DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(analysisFile);

            if (masterPeaklist == null || masterPeaklist.Count == 0) {
                return null;
            }

            for (int i = 0; i < files.Count; i++)
            {
                if (i == param.AlignmentReferenceFileID) continue;

                analysisFile = files[i];

                DataStorageLcUtility.SetPeakAreaBeanCollection(analysisFile, analysisFile.AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);
                Debug.WriteLine(analysisFile.AnalysisFilePropertyBean.AnalysisFileName);
                masterPeaklist = addJointAlignerMasterList(masterPeaklist, new List<PeakAreaBean>(analysisFile.PeakAreaBeanCollection), param);

                DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(files[i]);
            }

            masterPeaklist = masterPeaklist.OrderBy(n => n.AccurateMass).ThenBy(n => n.RtAtPeakTop).ToList();

            return masterPeaklist;
        }

        private static List<PeakAreaBean> getInitialMasterPeaklist(List<PeakAreaBean> peakAreaBeanList, AnalysisParametersBean param) {
            var masterPeaklist = new List<PeakAreaBean>();
            peakAreaBeanList = peakAreaBeanList.OrderBy(n => n.AccurateMass).ThenBy(n => n.RtAtPeakTop).ToList();

            for (int i = 0; i < peakAreaBeanList.Count; i++)
                if (peakAreaBeanList[i].IsotopeWeightNumber == 0 || 
                    (peakAreaBeanList[i].PostIdentificationLibraryId >= 0 && !peakAreaBeanList[i].MetaboliteName.Contains("w/o")) || 
                    param.TrackingIsotopeLabels)
                    masterPeaklist.Add(peakAreaBeanList[i]);

            return masterPeaklist;
        }

        private static List<PeakAreaBean> addJointAlignerMasterList(List<PeakAreaBean> peakAreaBeanMasterList, List<PeakAreaBean> peakAreaBeanList, AnalysisParametersBean param)
        {
            peakAreaBeanMasterList = peakAreaBeanMasterList.OrderBy(n => n.AccurateMass).ThenBy(n => n.RtAtPeakTop).ToList();
            peakAreaBeanList = peakAreaBeanList.OrderBy(n => n.AccurateMass).ThenBy(n => n.RtAtPeakTop).ToList();

            var addedPeakAreaBeanMasterList = new List<PeakAreaBean>();
            var maxIntensity = peakAreaBeanMasterList.Max(n => n.IntensityAtPeakTop);
            var rtTol = param.RetentionTimeAlignmentTolerance * 2.0;
            //var massTol = param.Ms1AlignmentTolerance * 2.0;
            var massTol = param.Ms1AlignmentTolerance;

            if (param.TrackingIsotopeLabels == false) {
                rtTol = param.RetentionTimeAlignmentTolerance;
                massTol = param.Ms1AlignmentTolerance;
            }

            for (int i = 0; i < peakAreaBeanList.Count; i++)
            {
                //if (Math.Abs(9.415 - peakAreaBeanList[i].RtAtPeakTop) < 0.01 && Math.Abs(162.0966 - peakAreaBeanList[i].AccurateMass) < 0.01) {
                //    Console.WriteLine();
                //}

                if (peakAreaBeanList[i].IsotopeWeightNumber == 0 ||
                    (peakAreaBeanList[i].PostIdentificationLibraryId >= 0 && !peakAreaBeanList[i].MetaboliteName.Contains("w/o")) ||
                    param.TrackingIsotopeLabels) {
                    var alignChecker = false;
                    var startIndex = DataAccessLcUtility.GetPeakAreaIntensityListStartIndex(peakAreaBeanMasterList, peakAreaBeanList[i].AccurateMass - param.Ms1AlignmentTolerance);
                    for (int j = startIndex; j < peakAreaBeanMasterList.Count; j++) {
                        if (peakAreaBeanList[i].AccurateMass - massTol > peakAreaBeanMasterList[j].AccurateMass) continue;
                        if (peakAreaBeanList[i].AccurateMass + massTol < peakAreaBeanMasterList[j].AccurateMass) break;
                        if (peakAreaBeanList[i].AccurateMass - massTol <= peakAreaBeanMasterList[j].AccurateMass
                            && peakAreaBeanMasterList[j].AccurateMass <= peakAreaBeanList[i].AccurateMass + massTol
                            && peakAreaBeanList[i].RtAtPeakTop - rtTol <= peakAreaBeanMasterList[j].RtAtPeakTop
                            && peakAreaBeanMasterList[j].RtAtPeakTop <= peakAreaBeanList[i].RtAtPeakTop + rtTol) {
                            alignChecker = true;
                            break;
                        }
                    }

                    if (alignChecker == false && maxIntensity * 0.0001 < peakAreaBeanList[i].IntensityAtPeakTop) {
                        addedPeakAreaBeanMasterList.Add(peakAreaBeanList[i]);
                    }
                }
            }
            if (addedPeakAreaBeanMasterList.Count == 0) return peakAreaBeanMasterList;
            for (int i = 0; i < addedPeakAreaBeanMasterList.Count; i++)
                peakAreaBeanMasterList.Add(addedPeakAreaBeanMasterList[i]);

            return peakAreaBeanMasterList;
        }

        /// <summary>
        /// This program just performs the initianlization of alignment result storage.
        /// AlignmentResultBean will contain the collection of alignment spot including the average RT, m/z, the abundances of each sample.
        /// This program just prepares the meta data of sample files.
        /// </summary>
        public static void JointAlignerResultInitialize(AlignmentResultBean alignmentResultBean, List<PeakAreaBean> integratedPeakAreaBeanList, ObservableCollection<AnalysisFileBean> analysisFileBeanCollection)
        {
            alignmentResultBean.SampleNumber = analysisFileBeanCollection.Count;
            alignmentResultBean.AlignmentIdNumber = integratedPeakAreaBeanList.Count;

            for (int i = 0; i < integratedPeakAreaBeanList.Count; i++)
            {
                var alignmentPropertyBean = new AlignmentPropertyBean() { AlignmentID = i, CentralRetentionTime = integratedPeakAreaBeanList[i].RtAtPeakTop, CentralAccurateMass = integratedPeakAreaBeanList[i].AccurateMass };
                for (int j = 0; j < analysisFileBeanCollection.Count; j++)
                    alignmentPropertyBean.AlignedPeakPropertyBeanCollection.Add(new AlignedPeakPropertyBean() { FileID = analysisFileBeanCollection[j].AnalysisFilePropertyBean.AnalysisFileId, FileName = analysisFileBeanCollection[j].AnalysisFilePropertyBean.AnalysisFileName });

                alignmentResultBean.AlignmentPropertyBeanCollection.Add(alignmentPropertyBean);
            }
        }

        /// <summary>
        /// This is the main procedure of Joint Aligner.
        /// Each peak of each sample file tries to be assigned to one of the master peaklist.
        /// </summary>
        public static void AlignToMasterList(AnalysisFileBean analysisFileBean, List<PeakAreaBean> peakAreaBeanList, List<PeakAreaBean> masterPeaklist, 
            AlignmentResultBean alignmentResultBean, AnalysisParametersBean analysisParametersBean)
        {
            peakAreaBeanList = peakAreaBeanList.OrderBy(n => n.AccurateMass).ThenBy(n => n.RtAtPeakTop).ToList();
            masterPeaklist = masterPeaklist.OrderBy(n => n.AccurateMass).ThenBy(n => n.RtAtPeakTop).ToList();

            var alignmentCollection = alignmentResultBean.AlignmentPropertyBeanCollection;
            var fileID = analysisFileBean.AnalysisFilePropertyBean.AnalysisFileId;
            var fileName = analysisFileBean.AnalysisFilePropertyBean.AnalysisFileName;
            var maxFactors = new double[masterPeaklist.Count];
            var isIsotopeTrack = analysisParametersBean.TrackingIsotopeLabels;

            for (int i = 0; i < peakAreaBeanList.Count; i++)
            {
                var isAnnotatedByPostProcess = peakAreaBeanList[i].PostIdentificationLibraryId >= 0 && !peakAreaBeanList[i].MetaboliteName.Contains("w/o");
                if (peakAreaBeanList[i].IsotopeWeightNumber != 0 && isIsotopeTrack == false && !isAnnotatedByPostProcess) continue;
                
                var startIndex = DataAccessLcUtility.GetPeakAreaIntensityListStartIndex(masterPeaklist, peakAreaBeanList[i].AccurateMass - analysisParametersBean.Ms1AlignmentTolerance);
                var maxMatchId = -1;
                var matchFactor = 0.0;
                var rtTol = analysisParametersBean.RetentionTimeAlignmentTolerance * 2.0;
                // var mzTol = analysisParametersBean.Ms1AlignmentTolerance * 2.0;
                var mzTol = analysisParametersBean.Ms1AlignmentTolerance;
                var maxMatchFactor = double.MinValue;

                //if (Math.Abs(9.415 - peakAreaBeanList[i].RtAtPeakTop) < 0.01 && Math.Abs(162.0966 - peakAreaBeanList[i].AccurateMass) < 0.01) {
                //    Console.WriteLine();
                //}

                for (int j = startIndex; j < masterPeaklist.Count; j++)
                {
                    if (peakAreaBeanList[i].AccurateMass - mzTol > masterPeaklist[j].AccurateMass) continue;
                    if (peakAreaBeanList[i].AccurateMass + mzTol < masterPeaklist[j].AccurateMass) break;
                    if (peakAreaBeanList[i].RtAtPeakTop - rtTol <= masterPeaklist[j].RtAtPeakTop
                        && masterPeaklist[j].RtAtPeakTop <= peakAreaBeanList[i].RtAtPeakTop + rtTol)
                    {
                        matchFactor = analysisParametersBean.RetentionTimeAlignmentFactor *
                            Math.Exp(-0.5 * Math.Pow(peakAreaBeanList[i].RtAtPeakTop - masterPeaklist[j].RtAtPeakTop, 2) 
                            / Math.Pow(analysisParametersBean.RetentionTimeAlignmentTolerance, 2))
                            + analysisParametersBean.Ms1AlignmentFactor * 
                            Math.Exp(-0.5 * Math.Pow(peakAreaBeanList[i].AccurateMass - 
                            masterPeaklist[j].AccurateMass, 2) / 
                            Math.Pow(analysisParametersBean.Ms1AlignmentTolerance, 2));

                        if (maxMatchFactor < matchFactor && maxFactors[j] < matchFactor) 
                        { 
                            maxMatchFactor = matchFactor; 
                            maxMatchId = j; 
                        }
                    }
                }

                if (maxMatchId == -1) continue;

                maxFactors[maxMatchId] = maxMatchFactor;
                setAlignmentResult(alignmentCollection[maxMatchId].AlignedPeakPropertyBeanCollection[fileID], peakAreaBeanList[i], fileID, fileName);
            }
        }

        private static void setAlignmentResult(AlignedPeakPropertyBean alignmentProperty, PeakAreaBean peakAreaBean, int fileID, string fileName)
        {
            alignmentProperty.AccurateMass = peakAreaBean.AccurateMass;
            alignmentProperty.FileID = fileID;
            alignmentProperty.FileName = fileName;
            alignmentProperty.PeakID = peakAreaBean.PeakID;
            alignmentProperty.RetentionTime = peakAreaBean.RtAtPeakTop;
            alignmentProperty.RetentionTimeLeft = peakAreaBean.RtAtLeftPeakEdge;
            alignmentProperty.RetentionTimeRight = peakAreaBean.RtAtRightPeakEdge;
            alignmentProperty.Variable = peakAreaBean.IntensityAtPeakTop;
            alignmentProperty.Area = peakAreaBean.AreaAboveZero;
            alignmentProperty.PeakWidth = peakAreaBean.RtAtRightPeakEdge - peakAreaBean.RtAtLeftPeakEdge;
            alignmentProperty.Ms1ScanNumber = peakAreaBean.Ms1LevelDatapointNumber;
            alignmentProperty.Ms2ScanNumber = peakAreaBean.Ms2LevelDatapointNumber;
            alignmentProperty.MetaboliteName = peakAreaBean.MetaboliteName;
            alignmentProperty.LibraryID = peakAreaBean.LibraryID;
            alignmentProperty.LibraryIdList = peakAreaBean.LibraryIDList;
            alignmentProperty.PostIdentificationLibraryID = peakAreaBean.PostIdentificationLibraryId;
            alignmentProperty.MassSpectraSimilarity = peakAreaBean.MassSpectraSimilarityValue;
            alignmentProperty.ReverseSimilarity = peakAreaBean.ReverseSearchSimilarityValue;
            alignmentProperty.FragmentPresencePercentage = peakAreaBean.PresenseSimilarityValue;
            alignmentProperty.SimpleDotProductSimilarity = peakAreaBean.SimpleDotProductSimilarity;
            alignmentProperty.IsotopeSimilarity = peakAreaBean.IsotopeSimilarityValue;
            alignmentProperty.AdductIonName = peakAreaBean.AdductIonName;
            alignmentProperty.IsotopeNumber = peakAreaBean.IsotopeWeightNumber;
            alignmentProperty.IsotopeParentID = peakAreaBean.IsotopeParentPeakID;
            alignmentProperty.ChargeNumber = peakAreaBean.ChargeNumber;
            alignmentProperty.RetentionTimeSimilarity = peakAreaBean.RtSimilarityValue;
            alignmentProperty.CcsSimilarity = peakAreaBean.CcsSimilarity;
            alignmentProperty.AccurateMassSimilarity = peakAreaBean.AccurateMassSimilarity;
            alignmentProperty.TotalSimilairty = peakAreaBean.TotalScore;
            alignmentProperty.IsLinked = peakAreaBean.IsLinked;
            alignmentProperty.PeakGroupID = peakAreaBean.PeakGroupID;
            alignmentProperty.PeakLinks = peakAreaBean.PeakLinks;
            alignmentProperty.EstimatedNoise = peakAreaBean.EstimatedNoise;
            alignmentProperty.SignalToNoise = peakAreaBean.SignalToNoise;

            alignmentProperty.IsMs1Match = peakAreaBean.IsMs1Match;
            alignmentProperty.IsMs2Match = peakAreaBean.IsMs2Match;
            alignmentProperty.IsRtMatch = peakAreaBean.IsRtMatch;
            alignmentProperty.IsCcsMatch = peakAreaBean.IsCcsMatch;

            alignmentProperty.IsLipidClassMatch = peakAreaBean.IsLipidClassMatch;
            alignmentProperty.IsLipidChainsMatch = peakAreaBean.IsLipidChainsMatch;
            alignmentProperty.IsLipidPositionMatch = peakAreaBean.IsLipidPositionMatch;
            alignmentProperty.IsOtherLipidMatch = peakAreaBean.IsOtherLipidMatch;

        }

        /// <summary>
        /// This program will remove the alignment spot which is not satisfied with the criteria of alignment result:
        /// 1) if an alignment spot does not have any peak information from samples, the spot will be excluded.
        /// 2) (At least QC filter, checked) if all of peaks of quality control samples is not assigned to an alignment spot, the spot will be excluded.
        /// 3) if the percentage of filled (not missing values) is less than the user-defined criteria, the spot will be excluded.
        /// </summary>
        public static void FilteringJointAligner(ProjectPropertyBean project, AnalysisParametersBean param, AlignmentResultBean alignmentResultBean)
        {
            if (alignmentResultBean.AlignmentPropertyBeanCollection == null || alignmentResultBean.AlignmentPropertyBeanCollection.Count == 0) return;
            int maxQcNumber = 0;
            foreach (var value in project.FileID_AnalysisFileType.Values) {
                if (value == AnalysisFileType.QC) maxQcNumber++;
            }

            var masterGroupCountDict = getGroupCountDictionary(project, alignmentResultBean.AlignmentPropertyBeanCollection[0]);

            for (int i = 0; i < alignmentResultBean.AlignmentPropertyBeanCollection.Count; i++)
            {
                int peakCount = 0, qcCount = 0;
                double sumRt = 0, sumMass = 0, sumPeakWidth = 0, minRt = double.MaxValue, maxRt = double.MinValue, minMz = double.MaxValue, maxMz = double.MinValue;
                double maxPeakMz = double.MinValue, maxPeakIntensity = double.MinValue, maxPeakRt = double.MinValue, minPeakIntensity = double.MaxValue;
                double maxNoise = double.MinValue, minNoise = double.MaxValue, sumNoise = 0;
                double maxSN = double.MinValue, minSN = double.MaxValue, sumSN = 0;

                var alignedSpot = alignmentResultBean.AlignmentPropertyBeanCollection[i];
                var localGroupCountDict = new Dictionary<string, int>();
                foreach (var key in masterGroupCountDict.Keys) localGroupCountDict[key] = 0;

                //if (Math.Abs(alignmentResultBean.AlignmentPropertyBeanCollection[i].CentralRetentionTime - 13.123) < 0.02 &&
                //    Math.Abs(alignmentResultBean.AlignmentPropertyBeanCollection[i].CentralAccurateMass - 941.7256) < 0.01) {
                //    Console.WriteLine();
                //}

                //if (Math.Abs(9.415 - alignedSpot.CentralRetentionTime) < 0.1 && Math.Abs(162.0966 - alignedSpot.CentralAccurateMass) < 0.05) {
                //    Console.WriteLine();
                //}

                for (int j = 0; j < alignedSpot.AlignedPeakPropertyBeanCollection.Count; j++)
                {
                    var alignedPeak = alignedSpot.AlignedPeakPropertyBeanCollection[j];
                    if (alignedPeak.PeakID < 0) continue;
                    if (project.FileID_AnalysisFileType[alignedPeak.FileID] == AnalysisFileType.QC)
                        qcCount++;

                    sumRt += alignedPeak.RetentionTime;
                    sumMass += alignedPeak.AccurateMass;
                    sumPeakWidth += alignedPeak.PeakWidth;
                    sumSN += alignedPeak.SignalToNoise;
                    sumNoise += alignedPeak.EstimatedNoise;

                    if (minRt > alignedPeak.RetentionTime) minRt = alignedPeak.RetentionTime;
                    if (maxRt < alignedPeak.RetentionTime) maxRt = alignedPeak.RetentionTime;
                    if (minMz > alignedPeak.AccurateMass) minMz = alignedPeak.AccurateMass;
                    if (maxMz < alignedPeak.AccurateMass) maxMz = alignedPeak.AccurateMass;
                    if (maxPeakIntensity < alignedPeak.Variable) {
                        maxPeakIntensity = alignedPeak.Variable;
                        maxPeakMz = alignedPeak.AccurateMass;
                        maxPeakRt = alignedPeak.RetentionTime;
                    }
                    if (minPeakIntensity > alignedPeak.Variable) minPeakIntensity = alignedPeak.Variable;
                    if (minSN > alignedPeak.SignalToNoise) minSN = alignedPeak.SignalToNoise;
                    if (maxSN < alignedPeak.SignalToNoise) maxSN = alignedPeak.SignalToNoise;
                    if (minNoise > alignedPeak.EstimatedNoise) minNoise = alignedPeak.EstimatedNoise;
                    if (maxNoise < alignedPeak.EstimatedNoise) maxNoise = alignedPeak.EstimatedNoise;

                    peakCount++;
                    var fileId = alignedPeak.FileID;
                    var classID = project.FileID_ClassName[fileId];
                    var filetype = project.FileID_AnalysisFileType[fileId];
                    //if (filetype == AnalysisFileType.Sample)
                    localGroupCountDict[classID]++;
                }

                if (peakCount == 0)
                {
                    alignmentResultBean.AlignmentPropertyBeanCollection.RemoveAt(i);
                    i--;
                    continue;
                }

                if ((float)((float)peakCount / (float)alignmentResultBean.AlignmentPropertyBeanCollection[i].AlignedPeakPropertyBeanCollection.Count) * 100F < param.PeakCountFilter)
                {
                    alignmentResultBean.AlignmentPropertyBeanCollection.RemoveAt(i);
                    i--;
                    continue;
                }

                if (param.QcAtLeastFilter && maxQcNumber != qcCount)
                {
                    alignmentResultBean.AlignmentPropertyBeanCollection.RemoveAt(i);
                    i--;
                    continue;
                }

                var isNpercentDetectedAtOneGroup = false;
                foreach (var pair in localGroupCountDict) {
                    var id = pair.Key;
                    var count = pair.Value;
                    var totalCount = masterGroupCountDict[id];
                    if ((float)count / (float)totalCount * 100 >= param.NPercentDetectedInOneGroup) {
                        isNpercentDetectedAtOneGroup = true;
                        break;
                    }
                }

                if (isNpercentDetectedAtOneGroup == false) {
                    alignmentResultBean.AlignmentPropertyBeanCollection.RemoveAt(i);
                    i--;
                    continue;
                }

                alignedSpot.CentralRetentionTime = (float)(sumRt / peakCount);
                //alignedSpot.CentralAccurateMass = (float)(sumMass / peakCount);
                alignedSpot.CentralAccurateMass = (float)maxPeakMz;
                //alignedSpot.CentralRetentionTime = (float)maxPeakRt;
                alignedSpot.AveragePeakWidth = (float)(sumPeakWidth / peakCount);

                alignedSpot.MaxRt = (float)maxRt;
                alignedSpot.MinRt = (float)minRt;
                alignedSpot.MaxMz = (float)maxMz;
                alignedSpot.MinMz = (float)minMz;
                alignedSpot.MinValiable = (float)minPeakIntensity; // tempolarily
                alignedSpot.MaxValiable = (float)maxPeakIntensity; // tempolarily

                alignedSpot.SignalToNoiseMax = (float)maxSN;
                alignedSpot.SignalToNoiseMin = (float)minSN;
                alignedSpot.SignalToNoiseAve = (float)(sumSN / peakCount);

                alignedSpot.EstimatedNoiseMax = (float)maxNoise;
                alignedSpot.EstimatedNoiseMin = (float)minNoise;
                alignedSpot.EstimatedNoiseAve = (float)(sumNoise / peakCount);
                if (alignedSpot.EstimatedNoiseAve < 1)
                    alignedSpot.EstimatedNoiseAve = 1.0F;
            }
        }

        private static Dictionary<string, int> getGroupCountDictionary(ProjectPropertyBean project, AlignmentPropertyBean alignProp) {
            var groupCountDic = new Dictionary<string, int>();

            foreach (var prop in alignProp.AlignedPeakPropertyBeanCollection) {
                var fileid = prop.FileID;
                var classid = project.FileID_ClassName[fileid];

                if (groupCountDic.ContainsKey(classid))
                    groupCountDic[classid]++;
                else
                    groupCountDic[classid] = 1;
            }
            return groupCountDic;
        }

        /// <summary>
        /// This program performs the interpolation, i.e. gap-filling, for missing values.
        /// </summary>
        public static void GapFillingMethod(ObservableCollection<RawSpectrum> spectrumCollection,
            AlignmentPropertyBean alignmentProperty, AlignedPeakPropertyBean alignedPeakProperty, ProjectPropertyBean projectProperty, AnalysisParametersBean param,
            float centralRT, float centralMZ, float rtTol, float mzTol, float averagePeakWidth)
        {
            gapfillingVS2(spectrumCollection, alignmentProperty, alignedPeakProperty, projectProperty, param,
                        centralRT, centralMZ, rtTol, mzTol, averagePeakWidth);
        }

        private static void gapfillingOld(ObservableCollection<RawSpectrum> spectrumCollection, AlignmentPropertyBean alignmentProperty, AlignedPeakPropertyBean alignedPeakProperty, ProjectPropertyBean projectProperty, AnalysisParametersBean param, float centralRT, float centralMZ, float rtTol, float mzTol, float averagePeakWidth) {
            if (mzTol < 0.005) mzTol = 0.005F;
            if (averagePeakWidth < 0.2) averagePeakWidth = 0.2F;

            //var peaklist = DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectProperty, centralMZ, mzTol, centralRT - rtTol, centralRT + rtTol);
            var peaklist = DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectProperty, centralMZ,
                mzTol, centralRT - averagePeakWidth * 1.5F, centralRT + averagePeakWidth * 1.5F);
            var sPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
            var ssPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(sPeaklist, param.SmoothingMethod, param.SmoothingLevel);
            var sssPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(sPeaklist, param.SmoothingMethod, param.SmoothingLevel);

            alignedPeakProperty.RetentionTime = centralRT;
            alignedPeakProperty.RetentionTimeLeft = centralRT;
            alignedPeakProperty.RetentionTimeRight = centralRT;
            alignedPeakProperty.AccurateMass = centralMZ;
            alignedPeakProperty.Variable = 0;
            alignedPeakProperty.Area = 0;
            alignedPeakProperty.PeakID = -2;

            //Debug.WriteLine(centralRT + "\t" + centralMZ + "\t" + peaklist.Count);
            if (sPeaklist == null || sPeaklist.Count == 0) return;

            //finding local maximum list
            var candidates = new List<PeakAreaBean>();
            var minRtId = -1;
            var minRtDiff = double.MaxValue;
            if (sPeaklist.Count != 0) {
                for (int i = 2; i < sPeaklist.Count - 2; i++) {

                    if (sPeaklist[i][1] < centralRT - rtTol) continue;
                    if (centralRT + rtTol < sPeaklist[i][1]) break;

                    //if ((sPeaklist[i - 2][3] <= sPeaklist[i - 1][3] && 
                    //    sPeaklist[i - 1][3] <= sPeaklist[i][3] && 
                    //    sPeaklist[i][3] > sPeaklist[i + 1][3]) ||
                    //    (sPeaklist[i - 1][3] < sPeaklist[i][3] && 
                    //    sPeaklist[i][3] >= sPeaklist[i + 1][3] &&
                    //    sPeaklist[i + 1][3] >= sPeaklist[i + 2][3])) {

                    //    candidates.Add(new PeakAreaBean() { ScanNumberAtPeakTop = i,
                    //        RtAtPeakTop = (float)sPeaklist[i][1],
                    //        AccurateMass = (float)sPeaklist[i][2],
                    //        IntensityAtPeakTop = (float)sPeaklist[i][3] });
                    //}

                    if ((sssPeaklist[i - 2][3] <= sssPeaklist[i - 1][3] &&
                       sssPeaklist[i - 1][3] <= sssPeaklist[i][3] &&
                       sssPeaklist[i][3] > sssPeaklist[i + 1][3]) ||
                       (sssPeaklist[i - 1][3] < sssPeaklist[i][3] &&
                       sssPeaklist[i][3] >= sssPeaklist[i + 1][3] &&
                       sssPeaklist[i + 1][3] >= sssPeaklist[i + 2][3])) {

                        candidates.Add(new PeakAreaBean() {
                            ScanNumberAtPeakTop = i,
                            RtAtPeakTop = (float)sPeaklist[i][1],
                            AccurateMass = (float)sPeaklist[i][2],
                            IntensityAtPeakTop = (float)sssPeaklist[i][3]
                        });
                    }

                    var diff = Math.Abs(sssPeaklist[i][1] - centralRT);
                    if (diff < minRtDiff) {
                        minRtDiff = diff;
                        minRtId = i;
                    }
                }
            }

            if (minRtId == -1) {
                minRtId = (int)(sPeaklist.Count * 0.5);
            }

            if (candidates.Count == 0) { // meaning really not detected
                return;
                var range = 5;

                // checking left ID
                var leftID = minRtId - 1;
                for (int i = minRtId - 1; i > minRtId - range; i--) {
                    leftID = i;
                    if (i - 1 < 0) {
                        leftID = 0;
                        break;
                    }
                    if (sPeaklist[i][3] < sPeaklist[i - 1][3]) {
                        break;
                    }
                }

                // checking right ID
                var rightID = minRtId + 1;
                for (int i = minRtId + 1; i < minRtId + range; i++) {
                    rightID = i;
                    if (i + 1 > sPeaklist.Count - 1) {
                        rightID = sPeaklist.Count - 1;
                        break;
                    }
                    if (sPeaklist[i][3] < sPeaklist[i + 1][3]) {
                        break;
                    }
                }

                //var leftID = minRtId - 5;
                // var rightID = minRtId + 5;
                //for (int i = minRtId - 5; i <= minRtId + 5; i++) {
                //    if (i < 0) leftID = 0;
                //    if (i > sPeaklist.Count - 1) rightID = sPeaklist.Count - 1;
                //}

                var peakAreaAboveZero = 0.0;
                for (int i = leftID; i <= rightID - 1; i++) {
                    peakAreaAboveZero += (sPeaklist[i][3] + sPeaklist[i + 1][3]) * (sPeaklist[i + 1][1] - sPeaklist[i][1]) * 0.5;
                }

                alignedPeakProperty.RetentionTime = (float)sPeaklist[minRtId][1];
                alignedPeakProperty.AccurateMass = (float)sPeaklist[minRtId][2];
                alignedPeakProperty.Variable = (float)sPeaklist[minRtId][3];
                alignedPeakProperty.Area = (float)peakAreaAboveZero * 60;
                alignedPeakProperty.RetentionTimeLeft = (float)sPeaklist[leftID][1];
                alignedPeakProperty.RetentionTimeRight = (float)sPeaklist[rightID][1];
            }
            else {

                // searching a representative local maximum. Now, the peak having nearest RT from central RT is selected.
                minRtId = -1;
                minRtDiff = double.MaxValue;

                for (int i = 0; i < candidates.Count; i++) {
                    var diff = Math.Abs(candidates[i].RtAtPeakTop - centralRT);
                    if (diff < minRtDiff) {
                        minRtDiff = diff;
                        minRtId = candidates[i].ScanNumberAtPeakTop;
                    }
                }

                // now, drawing new chromatogram using base peak m/z
                //var maxIdMz = sPeaklist[minRtId][2];

                //peaklist = DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectProperty, (float)maxIdMz,
                //    mzTol, centralRT - averagePeakWidth * 1.5F, centralRT + averagePeakWidth * 1.5F);
                //peaklist = DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectProperty, centralMZ,
                //    mzTol, centralRT - averagePeakWidth * 1.5F, centralRT + averagePeakWidth * 1.5F);
                //sPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);

                // here, there is a possibility that local maximum point may be slightly different from the original one, therefore, 
                // after finding local minimum (for left and right), the real top will be searched.

                // local minimum is searched from 5 point left from current local maximum.
                //var margin = 2;
                //var localMinLeft = minRtId - margin;
                //if (localMinLeft < 1) localMinLeft = 0;
                //else {
                //    for (int i = minRtId - margin; i >= 1; i--) {
                //        localMinLeft = i;
                //        if (sPeaklist[i][3] <= sPeaklist[i - 1][3]) {
                //            break;
                //        }
                //    }
                //}

                //var localMinRight = minRtId + margin;
                //if (localMinRight > sPeaklist.Count - 2) localMinRight = sPeaklist.Count - 1;
                //else {
                //    for (int i = minRtId + margin; i < sPeaklist.Count - 1; i++) {
                //        localMinRight = i;
                //        if (sPeaklist[i][3] <= sPeaklist[i + 1][3]) {
                //            break;
                //        }
                //    }
                //}

                var margin = 2;
                var localMinLeft = minRtId - margin;
                if (localMinLeft < 1) localMinLeft = 0;
                else {
                    for (int i = minRtId - margin; i >= 1; i--) {
                        localMinLeft = i;
                        if (sssPeaklist[i][3] <= sssPeaklist[i - 1][3]) {
                            break;
                        }
                    }
                }

                var localMinRight = minRtId + margin;
                if (localMinRight > sPeaklist.Count - 2) localMinRight = sPeaklist.Count - 1;
                else {
                    for (int i = minRtId + margin; i < sPeaklist.Count - 1; i++) {
                        localMinRight = i;
                        if (sssPeaklist[i][3] <= sssPeaklist[i + 1][3]) {
                            break;
                        }
                    }
                }


                if (minRtId - localMinLeft < 2 || localMinRight - minRtId < 2) return;

                var maxIntensity = 0.0;
                var maxID = minRtId;
                for (int i = localMinLeft + 1; i <= localMinRight - 1; i++) {
                    if ((sPeaklist[i - 1][3] <= sPeaklist[i][3] && sPeaklist[i][3] > sPeaklist[i + 1][3]) ||
                       (sPeaklist[i - 1][3] < sPeaklist[i][3] && sPeaklist[i][3] >= sPeaklist[i + 1][3])) {
                        if (maxIntensity < sPeaklist[i][3]) {
                            maxIntensity = sPeaklist[i][3];
                            maxID = i;
                        }
                    }
                }
                //if (sPeaklist[localMinRight][1] - sPeaklist[localMinLeft][1] < averagePeakWidth * 0.4) {
                //    return;
                //}

                //calculating peak area
                var peakAreaAboveZero = 0.0;
                for (int i = localMinLeft; i <= localMinRight - 1; i++) {
                    peakAreaAboveZero += (sPeaklist[i][3] + sPeaklist[i + 1][3]) * (sPeaklist[i + 1][1] - sPeaklist[i][1]) * 0.5;
                }

                alignedPeakProperty.RetentionTime = (float)sPeaklist[maxID][1];
                // alignedPeakProperty.AccurateMass = (float)maxIdMz;
                alignedPeakProperty.AccurateMass = (float)centralMZ;
                alignedPeakProperty.Variable = (float)maxIntensity;
                alignedPeakProperty.Area = (float)peakAreaAboveZero * 60;
                alignedPeakProperty.RetentionTimeLeft = (float)sPeaklist[localMinLeft][1];
                alignedPeakProperty.RetentionTimeRight = (float)sPeaklist[localMinRight][1];
            }

            #region // old method
            //// searching maximum peak's m/z
            //if (sPeaklist.Count != 0) {
            //    var maxID = -1;
            //    var maxInt = double.MinValue;

            //    //var minRtId = -1;
            //    //var minRt = double.MaxValue;

            //    for (int i = 1; i < sPeaklist.Count - 1; i++) {
            //        if (sPeaklist[i][1] < centralRT - rtTol) continue;
            //        if (centralRT + rtTol < sPeaklist[i][1]) break;

            //        if (maxInt < sPeaklist[i][3] &&
            //            sPeaklist[i - 1][3] <= sPeaklist[i][3] &&
            //            sPeaklist[i][3] >= sPeaklist[i + 1][3]) {

            //            maxInt = sPeaklist[i][3];
            //            maxID = i;
            //        }
            //        if (Math.Abs(sPeaklist[i][1] - centralRT) < minRtDiff) {
            //            minRtDiff = sPeaklist[i][1];
            //            minRtId = i;
            //        }
            //    }

            //    if (minRtId == -1) {
            //        minRtId = 0;
            //    }

            //    if (maxID == -1) {
            //        maxID = minRtId;
            //    }

            //    var maxIdMz = sPeaklist[maxID][2];

            //    peaklist = DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectProperty, (float)maxIdMz,
            //        mzTol, centralRT - averagePeakWidth * 1.5F, centralRT + averagePeakWidth * 1.5F);
            //    sPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
            //}

            ////maximum searching withing central RT +- rtTol
            //if (sPeaklist.Count != 0)
            //{
            //    var maxID = -1;
            //    //var minID = 0;
            //    var maxInt = double.MinValue;
            //    //var minInt = double.MaxValue;

            //    //var minRtId = -1;
            //    //var minRt = double.MaxValue;

            //    for (int i = 1; i < sPeaklist.Count - 1; i++)
            //    {
            //        if (sPeaklist[i][1] < centralRT - rtTol) continue;
            //        if (centralRT + rtTol < sPeaklist[i][1]) break;

            //        if (maxInt < sPeaklist[i][3] && 
            //            sPeaklist[i - 1][3] <= sPeaklist[i][3] && 
            //            sPeaklist[i][3] >= sPeaklist[i + 1][3]) {

            //            maxInt = sPeaklist[i][3];
            //            maxID = i;
            //        }
            //        if (Math.Abs(sPeaklist[i][1] - centralRT) < minRtDiff) {
            //            minRtDiff = sPeaklist[i][1];
            //            minRtId = i;
            //        }
            //    }

            //    if (minRtId == -1) {
            //        minRtId = 0;
            //    }

            //    if (maxID == -1) {
            //        maxID = minRtId;
            //        maxInt = sPeaklist[minRtId][3];
            //    }

            //    var peaktopRt = sPeaklist[maxID][1];

            //    //seeking left edge
            //    var minLeftInt = sPeaklist[maxID][3];
            //    var minLeftId = -1;
            //    for (int i = maxID - 1; i >= 0; i--) {

            //        if (i < maxID && minLeftInt < sPeaklist[i][3]) {
            //            break;
            //        }

            //        if (minLeftInt >= sPeaklist[i][3]) {
            //            minLeftInt = sPeaklist[i][3];
            //            minLeftId = i;
            //        } 
            //    }

            //    if (minLeftId == -1) {
            //        if (maxID - 3 >= 0) {
            //            minLeftId = maxID - 3;
            //        }
            //        else if (maxID - 2 >= 0) {
            //            minLeftId = maxID - 2;
            //        }
            //        else if (maxID - 1 >= 0) {
            //            minLeftId = maxID - 1;
            //        }
            //        else {
            //            minLeftId = maxID;
            //        }
            //    }

            //    //seeking right edge
            //    var minRightInt = sPeaklist[maxID][3];
            //    var minRightId = -1;
            //    for (int i = maxID + 1; i < sPeaklist.Count - 1; i++) {

            //        if (i > maxID && minRightInt < sPeaklist[i][3]) {
            //            break;
            //        }
            //        if (minRightInt >= sPeaklist[i][3]) {
            //            minRightInt = sPeaklist[i][3];
            //            minRightId = i;
            //        }
            //    }

            //    if (minRightId == -1) {
            //        if (maxID + 3 <= sPeaklist.Count - 1) {
            //            minRightId = maxID + 3;
            //        }
            //        else if (maxID + 2 <= sPeaklist.Count - 1) {
            //            minRightId = maxID + 2;
            //        }
            //        else if (maxID + 1 <= sPeaklist.Count - 1) {
            //            minRightId = maxID + 1;
            //        }
            //        else {
            //            minRightId = maxID;
            //        }
            //    }

            //    //calculating peak area
            //    var peakAreaAboveZero = 0.0;
            //    for (int i = minLeftId; i <= minRightId - 1; i++) {
            //        peakAreaAboveZero += (sPeaklist[i][3] + sPeaklist[i + 1][3]) * (sPeaklist[i + 1][1] - sPeaklist[i][1]) * 0.5;
            //    }

            //    if (maxInt >= 0) {
            //        alignedPeakProperty.RetentionTime = (float)sPeaklist[maxID][1];
            //        alignedPeakProperty.AccurateMass = (float)sPeaklist[maxID][2];
            //        alignedPeakProperty.Variable = (float)sPeaklist[maxID][3];
            //        alignedPeakProperty.Area = (float)peakAreaAboveZero * 60;
            //        alignedPeakProperty.RetentionTimeLeft = (float)sPeaklist[minLeftId][1];
            //        alignedPeakProperty.RetentionTimeRight = (float)sPeaklist[minRightId][1];
            //        alignedPeakProperty.PeakID = -2;
            //    }
            //}
            #endregion
        }

        private static void gapfillingVS1(ObservableCollection<RawSpectrum> spectrumCollection, AlignmentPropertyBean alignmentProperty,
            AlignedPeakPropertyBean alignedPeakProperty, ProjectPropertyBean projectProperty, AnalysisParametersBean param, 
            float centralRT, float centralMZ, float rtTol, float mzTol, float averagePeakWidth) {
            if (mzTol < 0.005) mzTol = 0.005F;
            if (averagePeakWidth < 0.2) averagePeakWidth = 0.2F;

            var peaklist = DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectProperty, centralMZ,
                mzTol, centralRT - averagePeakWidth * 1.5F, centralRT + averagePeakWidth * 1.5F);
            var sPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
            var ssPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(sPeaklist, param.SmoothingMethod, param.SmoothingLevel);
            var sssPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(ssPeaklist, param.SmoothingMethod, param.SmoothingLevel);
            var isForceInsert = param.IsForceInsertForGapFilling;

            alignedPeakProperty.RetentionTime = centralRT;
            alignedPeakProperty.RetentionTimeLeft = centralRT;
            alignedPeakProperty.RetentionTimeRight = centralRT;
            alignedPeakProperty.AccurateMass = centralMZ;
            alignedPeakProperty.Variable = 0;
            alignedPeakProperty.Area = 0;
            alignedPeakProperty.PeakID = -2;
            alignedPeakProperty.EstimatedNoise = alignmentProperty.EstimatedNoiseAve;
            if (alignedPeakProperty.EstimatedNoise < 1) alignedPeakProperty.EstimatedNoise = 1.0F;
            alignedPeakProperty.SignalToNoise = 0;

            //Debug.WriteLine(centralRT + "\t" + centralMZ + "\t" + peaklist.Count);
            if (sPeaklist == null || sPeaklist.Count == 0) return;

            //finding local maximum list
            var candidates = new List<PeakAreaBean>();
            var minRtId = -1;
            var minRtDiff = double.MaxValue;
            if (sPeaklist.Count != 0) {
                for (int i = 2; i < sPeaklist.Count - 2; i++) {

                    if (sPeaklist[i][1] < centralRT - rtTol) continue;
                    if (centralRT + rtTol < sPeaklist[i][1]) break;

                    if ((sssPeaklist[i - 2][3] <= sssPeaklist[i - 1][3] &&
                       sssPeaklist[i - 1][3] <= sssPeaklist[i][3] &&
                       sssPeaklist[i][3] > sssPeaklist[i + 1][3]) ||
                       (sssPeaklist[i - 1][3] < sssPeaklist[i][3] &&
                       sssPeaklist[i][3] >= sssPeaklist[i + 1][3] &&
                       sssPeaklist[i + 1][3] >= sssPeaklist[i + 2][3])) {

                        candidates.Add(new PeakAreaBean() {
                            ScanNumberAtPeakTop = i,
                            RtAtPeakTop = (float)sssPeaklist[i][1],
                            AccurateMass = (float)sssPeaklist[i][2],
                            IntensityAtPeakTop = (float)sssPeaklist[i][3]
                        });
                    }

                    var diff = Math.Abs(sssPeaklist[i][1] - centralRT);
                    if (diff < minRtDiff) {
                        minRtDiff = diff;
                        minRtId = i;
                    }
                }
            }

            if (minRtId == -1) {
                minRtId = (int)(sPeaklist.Count * 0.5);
            }

            if (candidates.Count == 0) { // meaning really not detected
                if (isForceInsert == false)
                    return;
                else {
                    var range = 5;

                    // checking left ID
                    var leftID = minRtId - 1;
                    for (int i = minRtId - 1; i > minRtId - range; i--) {
                        leftID = i;
                        if (i - 1 < 0) {
                            leftID = 0;
                            break;
                        }
                        if (sssPeaklist[i][3] < sssPeaklist[i - 1][3]) {
                            break;
                        }
                    }

                    // checking right ID
                    var rightID = minRtId + 1;
                    for (int i = minRtId + 1; i < minRtId + range; i++) {
                        rightID = i;
                        if (i + 1 > sPeaklist.Count - 1) {
                            rightID = sPeaklist.Count - 1;
                            break;
                        }
                        if (sssPeaklist[i][3] < sssPeaklist[i + 1][3]) {
                            break;
                        }
                    }

                    var peakAreaAboveZero = 0.0;
                    for (int i = leftID; i <= rightID - 1; i++) {
                        peakAreaAboveZero += (sPeaklist[i][3] + sPeaklist[i + 1][3]) * (sPeaklist[i + 1][1] - sPeaklist[i][1]) * 0.5;
                    }

                    var peakHeight = Math.Max(sPeaklist[minRtId][3] - sPeaklist[leftID][3], sPeaklist[minRtId][3] - sPeaklist[rightID][3]);

                    alignedPeakProperty.RetentionTime = (float)sPeaklist[minRtId][1];
                    alignedPeakProperty.AccurateMass = (float)sPeaklist[minRtId][2];
                    alignedPeakProperty.Variable = (float)sPeaklist[minRtId][3];
                    alignedPeakProperty.Area = (float)peakAreaAboveZero * 60;
                    alignedPeakProperty.RetentionTimeLeft = (float)sPeaklist[leftID][1];
                    alignedPeakProperty.RetentionTimeRight = (float)sPeaklist[rightID][1];
                    alignedPeakProperty.SignalToNoise = 0.0F;
                    //alignedPeakProperty.SignalToNoise = (float)(peakHeight / alignedPeakProperty.EstimatedNoise);
                }
            }
            else {
                // searching a representative local maximum. 
                // Now, the peak having nearest RT from central RT is selected.
                minRtId = -1;
                minRtDiff = double.MaxValue;

                for (int i = 0; i < candidates.Count; i++) {
                    var diff = Math.Abs(candidates[i].RtAtPeakTop - centralRT);
                    if (diff < minRtDiff) {
                        minRtDiff = diff;
                        minRtId = candidates[i].ScanNumberAtPeakTop;
                    }
                }

                // now, drawing new chromatogram using base peak m/z
                // here, there is a possibility that local maximum point may be slightly different from the original one, therefore, 
                // after finding local minimum (for left and right), the real top will be searched.

                // local minimum is searched from margin(2) point left from current local maximum.

                var margin = 2;
                var localMinLeft = minRtId - margin;
                if (localMinLeft < 1) localMinLeft = 0;
                else {
                    for (int i = minRtId - margin; i >= 1; i--) {
                        localMinLeft = i;
                        if (sssPeaklist[i][3] <= sssPeaklist[i - 1][3]) {
                            break;
                        }
                    }
                }

                var localMinRight = minRtId + margin;
                if (localMinRight > sPeaklist.Count - 2) localMinRight = sPeaklist.Count - 1;
                else {
                    for (int i = minRtId + margin; i < sPeaklist.Count - 1; i++) {
                        localMinRight = i;
                        if (sssPeaklist[i][3] <= sssPeaklist[i + 1][3]) {
                            break;
                        }
                    }
                }

                if (isForceInsert == false && (minRtId - localMinLeft < 2 || localMinRight - minRtId < 2)) return;

                var maxIntensity = 0.0;
                var maxID = minRtId;
                for (int i = localMinLeft + 1; i <= localMinRight - 1; i++) {
                    if ((sPeaklist[i - 1][3] <= sPeaklist[i][3] && sPeaklist[i][3] > sPeaklist[i + 1][3]) ||
                       (sPeaklist[i - 1][3] < sPeaklist[i][3] && sPeaklist[i][3] >= sPeaklist[i + 1][3])) {
                        if (maxIntensity < sPeaklist[i][3]) {
                            maxIntensity = sPeaklist[i][3];
                            maxID = i;
                        }
                    }
                }

                //calculating peak area
                var peakAreaAboveZero = 0.0;
                for (int i = localMinLeft; i <= localMinRight - 1; i++) {
                    peakAreaAboveZero += (sPeaklist[i][3] + sPeaklist[i + 1][3]) * (sPeaklist[i + 1][1] - sPeaklist[i][1]) * 0.5;
                }
                
                var peakHeight = Math.Max(sPeaklist[maxID][3] - sPeaklist[localMinLeft][3], 
                    sPeaklist[maxID][3] - sPeaklist[localMinRight][3]);

                alignedPeakProperty.RetentionTime = (float)sPeaklist[maxID][1];
                alignedPeakProperty.AccurateMass = (float)centralMZ;
                alignedPeakProperty.Variable = (float)maxIntensity;
                alignedPeakProperty.Area = (float)peakAreaAboveZero * 60;
                alignedPeakProperty.RetentionTimeLeft = (float)sPeaklist[localMinLeft][1];
                alignedPeakProperty.RetentionTimeRight = (float)sPeaklist[localMinRight][1];
                alignedPeakProperty.SignalToNoise = (float)(peakHeight / alignedPeakProperty.EstimatedNoise);
            }
        }

        private static void gapfillingVS2(ObservableCollection<RawSpectrum> spectrumCollection, AlignmentPropertyBean alignmentProperty, 
            AlignedPeakPropertyBean alignedPeakProperty, ProjectPropertyBean projectProperty, AnalysisParametersBean param, 
            float centralRT, float centralMZ, float rtTol, float mzTol, float averagePeakWidth) {
            if (averagePeakWidth < 0.2) averagePeakWidth = 0.2F;

            var peaklist = DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectProperty, centralMZ,
                mzTol, centralRT - averagePeakWidth * 1.5F, centralRT + averagePeakWidth * 1.5F);
            var sPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
            var isForceInsert = param.IsForceInsertForGapFilling;

            alignedPeakProperty.RetentionTime = centralRT;
            alignedPeakProperty.RetentionTimeLeft = centralRT;
            alignedPeakProperty.RetentionTimeRight = centralRT;
            alignedPeakProperty.AccurateMass = centralMZ;
            alignedPeakProperty.Variable = 0;
            alignedPeakProperty.Area = 0;
            alignedPeakProperty.PeakID = -2;
            alignedPeakProperty.EstimatedNoise = alignmentProperty.EstimatedNoiseAve;
            if (alignedPeakProperty.EstimatedNoise < 1) alignedPeakProperty.EstimatedNoise = 1.0F;
            alignedPeakProperty.SignalToNoise = 0;

            //Debug.WriteLine(centralRT + "\t" + centralMZ + "\t" + peaklist.Count);
            if (sPeaklist == null || sPeaklist.Count == 0) return;
            if (isForceInsert == false) return;

            //finding local maximum list
            var candidates = new List<PeakAreaBean>();
            var minRtId = -1;
            var minRtDiff = double.MaxValue;
            if (sPeaklist.Count != 0) {
                for (int i = 2; i < sPeaklist.Count - 2; i++) {

                    if (sPeaklist[i][1] < centralRT - rtTol) continue;
                    if (centralRT + rtTol < sPeaklist[i][1]) break;

                    if ((sPeaklist[i - 2][3] <= sPeaklist[i - 1][3] &&
                       sPeaklist[i - 1][3] <= sPeaklist[i][3] &&
                       sPeaklist[i][3] > sPeaklist[i + 1][3]) ||
                       (sPeaklist[i - 1][3] < sPeaklist[i][3] &&
                       sPeaklist[i][3] >= sPeaklist[i + 1][3] &&
                       sPeaklist[i + 1][3] >= sPeaklist[i + 2][3])) {

                        candidates.Add(new PeakAreaBean() {
                            ScanNumberAtPeakTop = i,
                            RtAtPeakTop = (float)sPeaklist[i][1],
                            AccurateMass = (float)sPeaklist[i][2],
                            IntensityAtPeakTop = (float)sPeaklist[i][3]
                        });
                    }

                    var diff = Math.Abs(sPeaklist[i][1] - centralRT);
                    if (diff < minRtDiff) {
                        minRtDiff = diff;
                        minRtId = i;
                    }
                }
            }

            if (minRtId == -1) {
                minRtId = (int)(sPeaklist.Count * 0.5);
            }

            if (candidates.Count == 0) { // meaning really not detected
                if (isForceInsert == false)
                    return;
                else {
                    var range = 5;

                    // checking left ID
                    var leftID = minRtId - 1;
                    for (int i = minRtId - 1; i > minRtId - range; i--) {
                        leftID = i;
                        if (i - 1 < 0) {
                            leftID = 0;
                            break;
                        }
                        if (sPeaklist[i][3] < sPeaklist[i - 1][3]) {
                            break;
                        }
                    }

                    // checking right ID
                    var rightID = minRtId + 1;
                    for (int i = minRtId + 1; i < minRtId + range; i++) {
                        rightID = i;
                        if (i + 1 > sPeaklist.Count - 1) {
                            rightID = sPeaklist.Count - 1;
                            break;
                        }
                        if (sPeaklist[i][3] < sPeaklist[i + 1][3]) {
                            break;
                        }
                    }

                    var peakAreaAboveZero = 0.0;
                    for (int i = leftID; i <= rightID - 1; i++) {
                        peakAreaAboveZero += (sPeaklist[i][3] + sPeaklist[i + 1][3]) * (sPeaklist[i + 1][1] - sPeaklist[i][1]) * 0.5;
                    }

                    var peakHeight = Math.Max(sPeaklist[minRtId][3] - sPeaklist[leftID][3], sPeaklist[minRtId][3] - sPeaklist[rightID][3]);

                    alignedPeakProperty.RetentionTime = (float)sPeaklist[minRtId][1];
                    alignedPeakProperty.AccurateMass = (float)sPeaklist[minRtId][2];
                    alignedPeakProperty.Variable = (float)sPeaklist[minRtId][3];
                    alignedPeakProperty.Area = (float)peakAreaAboveZero * 60;
                    alignedPeakProperty.RetentionTimeLeft = (float)sPeaklist[leftID][1];
                    alignedPeakProperty.RetentionTimeRight = (float)sPeaklist[rightID][1];
                    alignedPeakProperty.SignalToNoise = 0.0F;
                    //alignedPeakProperty.SignalToNoise = (float)(peakHeight / alignedPeakProperty.EstimatedNoise);
                }
            }
            else {
                // searching a representative local maximum. 
                // Now, the peak having nearest RT from central RT is selected.
                minRtId = -1;
                minRtDiff = double.MaxValue;

                for (int i = 0; i < candidates.Count; i++) {
                    var diff = Math.Abs(candidates[i].RtAtPeakTop - centralRT);
                    if (diff < minRtDiff) {
                        minRtDiff = diff;
                        minRtId = candidates[i].ScanNumberAtPeakTop;
                    }
                }

                // now, drawing new chromatogram using base peak m/z
                // here, there is a possibility that local maximum point may be slightly different from the original one, therefore, 
                // after finding local minimum (for left and right), the real top will be searched.

                // local minimum is searched from margin(2) point left from current local maximum.

                var margin = 2;
                var localMinLeft = minRtId - margin;
                if (localMinLeft < 1) localMinLeft = 0;
                else {
                    for (int i = minRtId - margin; i >= 1; i--) {
                        localMinLeft = i;
                        if (sPeaklist[i][3] <= sPeaklist[i - 1][3]) {
                            break;
                        }
                    }
                }

                var localMinRight = minRtId + margin;
                if (localMinRight > sPeaklist.Count - 2) localMinRight = sPeaklist.Count - 1;
                else {
                    for (int i = minRtId + margin; i < sPeaklist.Count - 1; i++) {
                        localMinRight = i;
                        if (sPeaklist[i][3] <= sPeaklist[i + 1][3]) {
                            break;
                        }
                    }
                }

                if (isForceInsert == false && (minRtId - localMinLeft < 2 || localMinRight - minRtId < 2)) return;

                var maxIntensity = 0.0;
                var maxID = minRtId;
                for (int i = localMinLeft + 1; i <= localMinRight - 1; i++) {
                    if ((sPeaklist[i - 1][3] <= sPeaklist[i][3] && sPeaklist[i][3] > sPeaklist[i + 1][3]) ||
                       (sPeaklist[i - 1][3] < sPeaklist[i][3] && sPeaklist[i][3] >= sPeaklist[i + 1][3])) {
                        if (maxIntensity < sPeaklist[i][3]) {
                            maxIntensity = sPeaklist[i][3];
                            maxID = i;
                        }
                    }
                }

                //calculating peak area
                var peakAreaAboveZero = 0.0;
                for (int i = localMinLeft; i <= localMinRight - 1; i++) {
                    peakAreaAboveZero += (sPeaklist[i][3] + sPeaklist[i + 1][3]) * (sPeaklist[i + 1][1] - sPeaklist[i][1]) * 0.5;
                }

                var peakHeight = Math.Max(sPeaklist[maxID][3] - sPeaklist[localMinLeft][3],
                    sPeaklist[maxID][3] - sPeaklist[localMinRight][3]);

                alignedPeakProperty.RetentionTime = (float)sPeaklist[maxID][1];
                alignedPeakProperty.AccurateMass = (float)centralMZ;
                alignedPeakProperty.Variable = (float)maxIntensity;
                alignedPeakProperty.Area = (float)peakAreaAboveZero * 60;
                alignedPeakProperty.RetentionTimeLeft = (float)sPeaklist[localMinLeft][1];
                alignedPeakProperty.RetentionTimeRight = (float)sPeaklist[localMinRight][1];
                alignedPeakProperty.SignalToNoise = (float)(peakHeight / alignedPeakProperty.EstimatedNoise);
            }
        }


        /// <summary>
        /// This program performs the assingment of representative MS/MS spectrum assigned to each alignment spot. See the AlignmentFinalizeProcess.cs.
        /// Now the MS/MS spectrum of a sample file having the highest identification score will be assigned to the alignment spot.
        /// In the case that no identification result is assigned to every samples, the MS/MS spectrum of a sample file having the most abundant spectrum will be assigned to the alignment spot.
        /// This process will be performed as ansynchronous process.
        /// </summary>
        public static void FinalizeJointAligner(AlignmentResultBean alignmentResultBean, 
            ObservableCollection<AnalysisFileBean> analysisFileBeanCollection, 
            AnalysisParametersBean param, ProjectPropertyBean projectProperty, IupacReferenceBean iupacRef, ref List<int> newIdList)
        {
            int fileIdOfMaxTotalScore = -1, fileIdOfMaxTotalScoreWithMSMS = -1, fileIdOfMaxIntensity = -1, fileIdOfMaxIntensityWithMSMS = -1;
            double minInt = double.MaxValue, maxInt = double.MinValue, minIntTotal = double.MaxValue, maxIntTotal = double.MinValue;

            var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;

            //Console.WriteLine("Calculating basic properties");
            for (int i = 0; i < alignedSpots.Count; i++)
            {
                setBasicAlignmentProperties(alignedSpots[i], i, param, out minInt, out maxInt, out fileIdOfMaxIntensity, out fileIdOfMaxIntensityWithMSMS, out fileIdOfMaxTotalScore, out fileIdOfMaxTotalScoreWithMSMS);
                setRepresentativeFileID(alignedSpots[i], param, fileIdOfMaxTotalScoreWithMSMS, fileIdOfMaxTotalScore, fileIdOfMaxIntensity, fileIdOfMaxIntensityWithMSMS);
                setRepresentativeIdentificationResult(alignedSpots[i]);

                if (maxIntTotal < maxInt) maxIntTotal = maxInt;
                if (minIntTotal > minInt) minIntTotal = minInt;
            }

            //if (!param.TrackingIsotopeLabels)
            reanalysisOfIsotopesInAlignmentSpots(alignedSpots, param, projectProperty, iupacRef);
            alignedSpots = getRefinedAlignmentPropertyBeanCollection(alignedSpots, param, projectProperty, ref newIdList);

            if (maxIntTotal > 1) maxIntTotal = Math.Log(maxIntTotal, 2); else maxIntTotal = 1;
            if (minIntTotal > 1) minIntTotal = Math.Log(minIntTotal, 2); else minIntTotal = 0;

            for (int i = 0; i < alignedSpots.Count; i++) {
                var relativeValue = (float)((Math.Log((double)alignedSpots[i].MaxValiable, 2) - minIntTotal)
                    / (maxIntTotal - minIntTotal));
                if (relativeValue < 0)
                    relativeValue = 0;
                else if (relativeValue > 1)
                    relativeValue = 1;
                alignedSpots[i].RelativeAmplitudeValue = relativeValue;
            }

            alignmentResultBean.AlignmentPropertyBeanCollection = alignedSpots;
            alignmentResultBean.AlignmentIdNumber = alignedSpots.Count;
        }

        /// <summary>
        /// Our current method ignores the results of deisotoping and adduct finding in peak spotting method.
        /// Now, the aligned spots are newly assessed indepndently for isotope and adduct feature detections.
        /// </summary>
        private static void postCurator(ObservableCollection<AlignmentPropertyBean> alignmentCollection, 
            AnalysisParametersBean param, ProjectPropertyBean projectProperty)
        {
            var rtMargin = 0.025F;
            var alignSpots = new List<AlignmentPropertyBean>(alignmentCollection);

            #region //initialization
            foreach (var spot in alignSpots) {
                #region
                if (spot.LibraryID >= 0 || spot.PostDefinedAdductParentID >= 0) { //identified metabolite must be defined as mono isotopic ions.
                    spot.PostDefinedIsotopeParentID = spot.AlignmentID;
                    spot.PostDefinedIsotopeWeightNumber = 0;
                }
                else if (param.TrackingIsotopeLabels == true) {
                    spot.PostDefinedIsotopeParentID = spot.AlignmentID;
                    spot.PostDefinedIsotopeWeightNumber = 0;
                }
                else {
                    spot.AdductIonName = string.Empty; //othewise, the adduct info is initialized.
                }
                #endregion
            }
            #endregion

            #region //isotope curations
            PostIsotopeCurator(alignSpots, param, projectProperty, rtMargin);
            #endregion

            #region //adduct curation
            PostAdductCurator(alignSpots, param, projectProperty, rtMargin);
            #endregion

            #region //checking alignment spot variable correlations
            alignSpots = alignSpots.OrderBy(n => n.CentralRetentionTime).ToList();
            foreach (var spot in alignSpots) {
                if (spot.PostDefinedIsotopeWeightNumber > 0) continue;
                var spotRt = spot.CentralRetentionTime;
                var startScanIndex = DataAccessLcUtility.GetScanStartIndexByRt(spotRt - rtMargin - 0.01F, alignSpots);

                var searchedSpots = new List<AlignmentPropertyBean>();

                for (int i = startScanIndex; i < alignSpots.Count; i++) {
                    if (spot.AlignmentID == alignSpots[i].AlignmentID) continue;
                    if (alignSpots[i].CentralRetentionTime < spotRt - rtMargin) continue;
                    if (alignSpots[i].PostDefinedIsotopeWeightNumber > 0) continue;
                    if (alignSpots[i].CentralRetentionTime > spotRt + rtMargin) break;

                    searchedSpots.Add(alignSpots[i]);
                }

                alignmentSpotVariableCorrelationSearcher(spot, searchedSpots);
            }
            #endregion
        }



        private static void reanalysisOfIsotopesInAlignmentSpots(ObservableCollection<AlignmentPropertyBean> alignmentCollection,
            AnalysisParametersBean param, ProjectPropertyBean projectProperty, IupacReferenceBean iupacRef) {
            var alignSpots = new List<AlignmentPropertyBean>(alignmentCollection);
            #region //initialization
            foreach (var spot in alignSpots) {
                //spot.PostDefinedIsotopeParentID = spot.AlignmentID;
                //spot.PostDefinedIsotopeWeightNumber = 0;
                #region
                if ((spot.LibraryID >= 0 || spot.PostDefinedAdductParentID >= 0) && !spot.MetaboliteName.Contains("w/o")) { //identified metabolite must be defined as mono isotopic ions.
                    spot.PostDefinedIsotopeParentID = spot.AlignmentID;
                    spot.PostDefinedIsotopeWeightNumber = 0;
                }
                else if ((spot.PostIdentificationLibraryID >= 0 || spot.PostDefinedAdductParentID >= 0) && (spot.AdductIonName != null && spot.AdductIonName != string.Empty)) { //identified metabolite must be defined as mono isotopic ions.
                    spot.PostDefinedIsotopeParentID = spot.AlignmentID;
                    spot.PostDefinedIsotopeWeightNumber = 0;
                }
                else {
                    spot.AdductIonName = string.Empty; //othewise, the adduct info is initialized.
                }

                if (param.TrackingIsotopeLabels == true) {
                    spot.PostDefinedIsotopeParentID = spot.AlignmentID;
                    spot.PostDefinedIsotopeWeightNumber = 0;
                }
                #endregion
            }
            #endregion
            if (param.TrackingIsotopeLabels == true) return;

            #region //isotope curations
            IsotopeEstimator.PostIsotopeCurator(alignSpots, param, projectProperty, iupacRef);
            #endregion

        }

        private static void assignLinksByIonAbundanceCorrelations(List<AlignmentPropertyBean> alignSpots, float rtMargin) {
            if (alignSpots == null || alignSpots.Count == 0) return;
            if (alignSpots[0].AlignedPeakPropertyBeanCollection == null || alignSpots[0].AlignedPeakPropertyBeanCollection.Count == 0) return;

            if (alignSpots[0].AlignedPeakPropertyBeanCollection.Count() > 9) {
                alignSpots = alignSpots.OrderBy(n => n.CentralRetentionTime).ToList();
                foreach (var spot in alignSpots) {
                    if (spot.PostDefinedIsotopeWeightNumber > 0) continue;
                    var spotRt = spot.CentralRetentionTime;
                    var startScanIndex = DataAccessLcUtility.GetScanStartIndexByRt(spotRt - rtMargin - 0.01F, alignSpots);

                    var searchedSpots = new List<AlignmentPropertyBean>();

                    for (int i = startScanIndex; i < alignSpots.Count; i++) {
                        if (spot.AlignmentID == alignSpots[i].AlignmentID) continue;
                        if (alignSpots[i].CentralRetentionTime < spotRt - rtMargin) continue;
                        if (alignSpots[i].PostDefinedIsotopeWeightNumber > 0) continue;
                        if (alignSpots[i].CentralRetentionTime > spotRt + rtMargin) break;

                        searchedSpots.Add(alignSpots[i]);
                    }

                    alignmentSpotVariableCorrelationSearcher(spot, searchedSpots);
                }
            }
        }

        public static void PostIsotopeCurator(List<AlignmentPropertyBean> alignSpots,
            AnalysisParametersBean param, ProjectPropertyBean projectProperty, float rtMargin)
        {
            alignSpots = alignSpots.OrderBy(n => n.CentralAccurateMass).ToList();
            foreach (var spot in alignSpots) {
                #region
                if (spot.PostDefinedIsotopeWeightNumber > 0) continue;
                
                var spotRt = spot.CentralRetentionTime;
                var spotMz = spot.CentralAccurateMass;

                var startScanIndex = DataAccessLcUtility.GetScanStartIndexByMz(spotMz - 0.0001F, alignSpots);
                var isotopeCandidates = new List<AlignmentPropertyBean>() { spot };

                for (int j = startScanIndex; j < alignSpots.Count; j++) {

                    if (alignSpots[j].AlignmentID == spot.AlignmentID) continue;
                    if (alignSpots[j].LibraryID >= 0 || alignSpots[j].PostIdentificationLibraryID >= 0) continue;
                    if (alignSpots[j].CentralRetentionTime < spotRt - rtMargin) continue;
                    if (alignSpots[j].CentralRetentionTime > spotRt + rtMargin) continue;
                    if (alignSpots[j].PostDefinedIsotopeWeightNumber >= 0) continue;
                    if (alignSpots[j].CentralAccurateMass <= spotMz) continue;
                    if (alignSpots[j].CentralAccurateMass > spotMz + 8.1) break;

                    isotopeCandidates.Add(alignSpots[j]);
                }
                isotopeCalculation(isotopeCandidates, param);
                #endregion
            }
        }

        public static void PostAdductCurator(List<AlignmentPropertyBean> alignSpots,
            AnalysisParametersBean param, ProjectPropertyBean projectProperty, float rtMargin)
        {

            alignSpots = alignSpots.OrderBy(n => n.CentralRetentionTime).ToList();
            var searchedAdducts = new List<Rfx.Riken.OsakaUniv.AdductIon>();
            for (int i = 0; i < param.AdductIonInformationBeanList.Count; i++) {
                if (param.AdductIonInformationBeanList[i].Included)
                    searchedAdducts.Add(AdductIonParcer.GetAdductIonBean(param.AdductIonInformationBeanList[i].AdductName));
            }

            //adduct curation from identified metabolite info
            foreach (var spot in alignSpots) {
                if (spot.PostDefinedIsotopeWeightNumber > 0 || spot.IsotopeTrackingWeightNumber > 0) continue;
                #region
                if ((spot.LibraryID >= 0 || spot.PostIdentificationLibraryID >= 0) && 
                    !spot.MetaboliteName.Contains("w/o") &&
                    (spot.AdductIonName != null && spot.AdductIonName != string.Empty)) {

                    spot.PostDefinedAdductParentID = spot.AlignmentID;

                    // if users do not set any adduct types except for proton adduct/loss (searchedAdducts.count == 1),
                    // adduct searcher is not necessary if the peak adduct type is proton adduct/loss.
                    if (searchedAdducts.Count == 1 && (spot.AdductIonName == "[M-H]-" || spot.AdductIonName == "[M+H]+")) continue;

                    var startScanIndex = DataAccessLcUtility.GetScanStartIndexByRt(spot.CentralRetentionTime - rtMargin - 0.01F, alignSpots);
                    var spotRt = spot.CentralRetentionTime;
                    var searchedSpots = new List<AlignmentPropertyBean>();

                    for (int j = startScanIndex; j < alignSpots.Count; j++) {
                        if (spot.AlignmentID == alignSpots[j].AlignmentID) continue;
                        if (alignSpots[j].CentralRetentionTime < spotRt - rtMargin) continue;
                        if (alignSpots[j].PostDefinedIsotopeWeightNumber > 0 || alignSpots[j].IsotopeTrackingWeightNumber > 0) continue;
                        if (alignSpots[j].CentralRetentionTime > spotRt + rtMargin) break;
                        searchedSpots.Add(alignSpots[j]);
                    }

                    searchedSpots = searchedSpots.OrderBy(n => n.CentralAccurateMass).ToList();
                    adductSearcherWithIdentifiedInfo(spot, searchedSpots, searchedAdducts, param, projectProperty.IonMode);
                }
                #endregion
            }

            //adduct curation with no bias way
            if (searchedAdducts.Count > 1) { //adduct searcher will be used when users selected several adducts for searching
                foreach (var spot in alignSpots) {
                    #region
                    if (spot.AdductIonName != string.Empty) continue;
                    if (spot.PostDefinedIsotopeWeightNumber > 0 || spot.IsotopeTrackingWeightNumber > 0) continue;
                    var spotRt = spot.CentralRetentionTime;
                    var startScanIndex = DataAccessLcUtility.GetScanStartIndexByRt(spotRt - rtMargin - 0.01F, alignSpots);

                    var searchedSpots = new List<AlignmentPropertyBean>();

                    for (int i = startScanIndex; i < alignSpots.Count; i++) {
                        if (spot.AlignmentID == alignSpots[i].AlignmentID) continue;
                        if (alignSpots[i].PostDefinedIsotopeWeightNumber > 0 || alignSpots[i].IsotopeTrackingWeightNumber > 0) continue;
                        if (alignSpots[i].CentralRetentionTime < spotRt - rtMargin) continue;
                        if (alignSpots[i].AdductIonName != null && alignSpots[i].AdductIonName != string.Empty) continue;
                        //if (alignSpots[i].LibraryID >= 0 || alignSpots[i].PostIdentificationLibraryID >= 0) continue;
                        if (alignSpots[i].CentralRetentionTime > spotRt + rtMargin) break;

                        searchedSpots.Add(alignSpots[i]);
                    }

                    searchedSpots = searchedSpots.OrderBy(n => n.CentralAccurateMass).ToList();
                    adductSearcher(spot, searchedSpots, searchedAdducts, param, projectProperty.IonMode);
                    #endregion
                }
            }

            //finalization for adduct filter
            alignSpots = alignSpots.OrderBy(n => n.AlignmentID).ToList();
            var defaultAdduct = searchedAdducts[0];
            var defaultAdduct2 = AdductIonParcer.ConvertDifferentChargedAdduct(defaultAdduct, 2);
            foreach (var spot in alignSpots.Where(n => n.PostDefinedIsotopeWeightNumber == 0 || n.IsotopeTrackingWeightNumber == 0)) {
                #region
                if (spot.AdductIonName != string.Empty && spot.PostDefinedAdductParentID >= 0) continue;
                spot.PostDefinedAdductParentID = spot.AlignmentID;

                if (spot.ChargeNumber == 2)
                    spot.AdductIonName = defaultAdduct2.AdductIonName;
                else
                    spot.AdductIonName = defaultAdduct.AdductIonName;
                #endregion
            }

            foreach (var spot in alignSpots) {
                #region
                if (spot.PostDefinedIsotopeWeightNumber > 0 || spot.IsotopeTrackingWeightNumber > 0) {
                    var parentSpot 
                        = param.TrackingIsotopeLabels == false 
                        ? alignSpots[spot.PostDefinedIsotopeParentID] 
                        : alignSpots[spot.IsotopeTrackingParentID];
                    spot.PostDefinedAdductParentID = parentSpot.PostDefinedAdductParentID;
                    spot.AdductIonName = parentSpot.AdductIonName;
                }
                #endregion
            }

            //refine the dependency
            foreach (var spot in alignSpots) {
                if (spot.AlignmentID == spot.PostDefinedAdductParentID) continue;
                var parentID = spot.PostDefinedAdductParentID;

                if (alignSpots[parentID].PostDefinedAdductParentID != alignSpots[parentID].AlignmentID) {
                    var parentParentID = alignSpots[parentID].PostDefinedAdductParentID;
                    if (alignSpots[parentParentID].PostDefinedAdductParentID == alignSpots[parentParentID].AlignmentID)
                        spot.PostDefinedAdductParentID = alignSpots[parentParentID].AlignmentID;
                    else {
                        var parentParentParentID = alignSpots[parentParentID].PostDefinedAdductParentID;
                        if (alignSpots[parentParentParentID].PostDefinedAdductParentID == alignSpots[parentParentParentID].AlignmentID)
                            spot.PostDefinedAdductParentID = alignSpots[parentParentParentID].AlignmentID;
                    }
                }
            }
        }

        private static void alignmentSpotVariableCorrelationSearcher(AlignmentPropertyBean spot, List<AlignmentPropertyBean> searchedSpots)
        {
            var sampleCount = spot.AlignedPeakPropertyBeanCollection.Count;
            var spotPeaks = spot.AlignedPeakPropertyBeanCollection;

            foreach (var searchSpot in searchedSpots) {

                var searchedSpotPeaks = searchSpot.AlignedPeakPropertyBeanCollection;

                double sum1 = 0, sum2 = 0, mean1 = 0, mean2 = 0, covariance = 0, sqrt1 = 0, sqrt2 = 0;
                for (int i = 0; i < sampleCount; i++) {
                    sum1 += spotPeaks[i].Variable;
                    sum2 += spotPeaks[i].Variable;
                }
                mean1 = (double)(sum1 / sampleCount);
                mean2 = (double)(sum2 / sampleCount);

                for (int i = 0; i < sampleCount; i++) {
                    covariance += (spotPeaks[i].Variable - mean1) * (searchedSpotPeaks[i].Variable - mean2);
                    sqrt1 += Math.Pow(spotPeaks[i].Variable - mean1, 2);
                    sqrt2 += Math.Pow(searchedSpotPeaks[i].Variable - mean2, 2);
                }
                if (sqrt1 == 0 || sqrt2 == 0)
                    continue;
                else {
                    var correlation = (double)(covariance / Math.Sqrt(sqrt1 * sqrt2));
                    if (correlation >= 0.95) {
                        spot.AlignmentSpotVariableCorrelations.Add(
                            new AlignmentSpotVariableCorrelation() {
                                CorrelateAlignmentID = searchSpot.AlignmentID,
                                CorrelationScore = (float)correlation
                            });
                        spot.IsLinked = true;
                        spot.PeakLinks.Add(new LinkedPeakFeature() {
                            LinkedPeakID = searchSpot.AlignmentID,
                            Character = PeakLinkFeatureEnum.CorrelSimilar
                        });
                    }
                }
            }

            if (spot.AlignmentSpotVariableCorrelations.Count > 1)
                spot.AlignmentSpotVariableCorrelations = spot.AlignmentSpotVariableCorrelations.OrderBy(n => n.CorrelateAlignmentID).ToList();

        }

        private static void adductSearcher(AlignmentPropertyBean spot, List<AlignmentPropertyBean> searchedSpots,
            List<Rfx.Riken.OsakaUniv.AdductIon> searchedAdducts, AnalysisParametersBean param, IonMode ionMode)
        {
            var flg = false;
            var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.AdductAndIsotopeMassTolerance); //based on m/z 200

            foreach (var centralAdduct in searchedAdducts) {

                var rCentralAdduct = AdductIonParcer.ConvertDifferentChargedAdduct(centralAdduct, spot.ChargeNumber);
                var centralExactMass = MolecularFormulaUtility.ConvertPrecursorMzToExactMass(spot.CentralAccurateMass, rCentralAdduct.AdductIonAccurateMass,
                    rCentralAdduct.ChargeNumber, rCentralAdduct.AdductIonXmer, ionMode);

                var searchedPrecursors = new List<SearchedPrecursor>();
                foreach (var searchedAdduct in searchedAdducts) {
                    if (rCentralAdduct.AdductIonName == searchedAdduct.AdductIonName) continue;
                    var searchedPrecursorMz = MolecularFormulaUtility.ConvertExactMassToPrecursorMz(searchedAdduct, centralExactMass);
                    searchedPrecursors.Add(new SearchedPrecursor() { PrecursorMz = searchedPrecursorMz, AdductIon = searchedAdduct });
                }

                foreach (var searchedPeak in searchedSpots) {
                    foreach (var searchedPrecursor in searchedPrecursors) {

                        var adductTol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(searchedPeak.CentralAccurateMass, ppm);

                        if (Math.Abs(searchedPeak.CentralAccurateMass - searchedPrecursor.PrecursorMz) < adductTol) {

                            var searchedAdduct = searchedPrecursor.AdductIon;

                            searchedPeak.PostDefinedAdductParentID = spot.AlignmentID;
                            searchedPeak.AdductIonName = searchedAdduct.AdductIonName;

                            flg = true;
                            break;
                        }
                    }
                }

                if (flg) {
                    spot.PostDefinedAdductParentID = spot.AlignmentID;
                    spot.AdductIonName = rCentralAdduct.AdductIonName;
                    break;
                }
            }
        }

        private static void adductSearcherWithIdentifiedInfo(AlignmentPropertyBean spot, List<AlignmentPropertyBean> searchedSpots, 
            List<Rfx.Riken.OsakaUniv.AdductIon> searchedAdducts, AnalysisParametersBean param, IonMode ionMode)
        {
            var centralAdduct = AdductIonParcer.GetAdductIonBean(spot.AdductIonName);
            var centralExactMass = MolecularFormulaUtility.ConvertPrecursorMzToExactMass(spot.CentralAccurateMass, centralAdduct.AdductIonAccurateMass,
                centralAdduct.ChargeNumber, centralAdduct.AdductIonXmer, ionMode);
            var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.AdductAndIsotopeMassTolerance); //based on m/z 400

            var searchedPrecursors = new List<SearchedPrecursor>();
            foreach (var searchedAdduct in searchedAdducts) {
                if (centralAdduct.AdductIonName == searchedAdduct.AdductIonName) continue;
                var searchedPrecursorMz = MolecularFormulaUtility.ConvertExactMassToPrecursorMz(searchedAdduct, centralExactMass);
                searchedPrecursors.Add(new SearchedPrecursor() { PrecursorMz = searchedPrecursorMz, AdductIon = searchedAdduct });
            }

            foreach (var searchedPeak in searchedSpots) {
                foreach (var searchedPrecursor in searchedPrecursors) {

                    var adductTol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(searchedPeak.CentralAccurateMass, ppm);

                    if (Math.Abs(searchedPeak.CentralAccurateMass - searchedPrecursor.PrecursorMz) < adductTol) {
                        var searchedAdduct = searchedPrecursor.AdductIon;

                        if (searchedPeak.AdductIonName == null || searchedPeak.AdductIonName == string.Empty) {
                            searchedPeak.PostDefinedAdductParentID = spot.AlignmentID;
                            searchedPeak.AdductIonName = searchedAdduct.AdductIonName;
                        }
                        else if (searchedPeak.AdductIonName == searchedAdduct.AdductIonName) {
                            searchedPeak.PostDefinedAdductParentID = spot.AlignmentID;
                        }

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Make sure, the peak alignment program itself was perfomed by means of mono isotopic ions (from peak spotting method) only.
        /// Therefore, the deisotoping in alignment method is no more needed theoretically. 
        /// However, the real is not. So we have to simply perform the deisotoping again.
        /// </summary>
        private static void isotopeCalculation(List<AlignmentPropertyBean> alignSpots, AnalysisParametersBean param)
        {
            var c13_c12Diff = MassDiffDictionary.C13_C12;  //1.003355F;
            var tolerance = param.CentroidMs1Tolerance;
            var monoIsoPeak = alignSpots[0];
            var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.CentroidMs1Tolerance); //based on m/z 400
            var accuracy = MolecularFormulaUtility.ConvertPpmToMassAccuracy(monoIsoPeak.CentralAccurateMass, ppm);
            tolerance = (float)accuracy;

            var isFinished = false;

            monoIsoPeak.PostDefinedIsotopeWeightNumber = 0;
            monoIsoPeak.PostDefinedIsotopeParentID = monoIsoPeak.AlignmentID;

            var reminderIntensity = monoIsoPeak.AverageValiable;
            var reminderIndex = 1;

            //charge number check at M + 1
            var isDoubleCharged = false;
            var isotopicMassDoubleCharged = (double)monoIsoPeak.CentralAccurateMass + (float)c13_c12Diff * 0.50;

            for (int j = 1; j < alignSpots.Count; j++) {
                var isotopePeak = alignSpots[j];
                if (isotopicMassDoubleCharged - tolerance <= isotopePeak.CentralAccurateMass &&
                    isotopePeak.CentralAccurateMass <= isotopicMassDoubleCharged + tolerance) {

                    if (monoIsoPeak.CentralAccurateMass > 900 || monoIsoPeak.AverageValiable > isotopePeak.AverageValiable) {
                        isDoubleCharged = true;
                    }

                    if (isotopePeak.CentralAccurateMass >= isotopicMassDoubleCharged + tolerance) {
                        break;
                    }
                }
            }

            var chargeCoff = isDoubleCharged == true ? 0.50 : 1.0;
            monoIsoPeak.ChargeNumber = isDoubleCharged == true ? 2 : 1;

            for (int i = 1; i <= 8; i++) {

                var isotopicMass = (double)monoIsoPeak.CentralAccurateMass + (double)i * c13_c12Diff * chargeCoff;

                for (int j = reminderIndex; j < alignSpots.Count; j++) {

                    var isotopePeak = alignSpots[j];

                    if (isotopicMass - tolerance <= isotopePeak.CentralAccurateMass &&
                        isotopePeak.CentralAccurateMass <= isotopicMass + tolerance) {
                        #region // for single charged ions
                        if (monoIsoPeak.CentralAccurateMass < 900) {

                            if (reminderIntensity > isotopePeak.AverageValiable) {

                                isotopePeak.PostDefinedIsotopeParentID = monoIsoPeak.AlignmentID;
                                isotopePeak.PostDefinedIsotopeWeightNumber = i;
                                isotopePeak.ChargeNumber = monoIsoPeak.ChargeNumber;

                                reminderIntensity = isotopePeak.AverageValiable;
                                reminderIndex = j + 1;
                                break;
                            }
                            else {
                                isFinished = true;
                                break;
                            }
                        }
                        else {
                            if (i <= 3) {

                                isotopePeak.PostDefinedIsotopeParentID = monoIsoPeak.AlignmentID;
                                isotopePeak.PostDefinedIsotopeWeightNumber = i;
                                isotopePeak.ChargeNumber = monoIsoPeak.ChargeNumber;

                                reminderIntensity = isotopePeak.AverageValiable;
                                reminderIndex = j + 1;

                                break;
                            }
                            else {
                                if (reminderIntensity > isotopePeak.AverageValiable) {

                                    isotopePeak.PostDefinedIsotopeParentID = monoIsoPeak.AlignmentID;
                                    isotopePeak.PostDefinedIsotopeWeightNumber = i;
                                    isotopePeak.ChargeNumber = monoIsoPeak.ChargeNumber;

                                    reminderIntensity = isotopePeak.AverageValiable;
                                    reminderIndex = j + 1;

                                    break;
                                }
                                else {
                                    isFinished = true;
                                    break;
                                }
                            }
                        }
                        #endregion
                    }
                }
                if (isFinished) break;
            }
        }

        private static void setRepresentativeIdentificationResult(AlignmentPropertyBean alignmentProperty)
        {
            var fileID = alignmentProperty.RepresentativeFileID;
            var alignedPeakCollection = alignmentProperty.AlignedPeakPropertyBeanCollection;

            alignmentProperty.LibraryID = alignedPeakCollection[fileID].LibraryID;
            alignmentProperty.PostIdentificationLibraryID = alignedPeakCollection[fileID].PostIdentificationLibraryID;
            alignmentProperty.AdductIonName = alignedPeakCollection[fileID].AdductIonName;
            alignmentProperty.ChargeNumber = alignedPeakCollection[fileID].ChargeNumber;
            alignmentProperty.MetaboliteName = alignedPeakCollection[fileID].MetaboliteName;
            alignmentProperty.TotalSimilairty = alignedPeakCollection[fileID].TotalSimilairty;
            alignmentProperty.MassSpectraSimilarity = alignedPeakCollection[fileID].MassSpectraSimilarity;
            alignmentProperty.ReverseSimilarity = alignedPeakCollection[fileID].ReverseSimilarity;
            alignmentProperty.FragmentPresencePercentage = alignedPeakCollection[fileID].FragmentPresencePercentage;
            alignmentProperty.IsotopeSimilarity = alignedPeakCollection[fileID].IsotopeSimilarity;
            alignmentProperty.RetentionTimeSimilarity = alignedPeakCollection[fileID].RetentionTimeSimilarity;
            alignmentProperty.CcsSimilarity = alignedPeakCollection[fileID].CcsSimilarity;
            alignmentProperty.AccurateMassSimilarity = alignedPeakCollection[fileID].AccurateMassSimilarity;

            alignmentProperty.IsMs1Match = alignedPeakCollection[fileID].IsMs1Match;
            alignmentProperty.IsMs2Match = alignedPeakCollection[fileID].IsMs2Match;
            alignmentProperty.IsRtMatch = alignedPeakCollection[fileID].IsRtMatch;
            alignmentProperty.IsCcsMatch = alignedPeakCollection[fileID].IsCcsMatch;

            alignmentProperty.IsLipidClassMatch = alignedPeakCollection[fileID].IsLipidClassMatch;
            alignmentProperty.IsLipidChainsMatch = alignedPeakCollection[fileID].IsLipidChainsMatch;
            alignmentProperty.IsLipidPositionMatch = alignedPeakCollection[fileID].IsLipidPositionMatch;
            alignmentProperty.IsOtherLipidMatch = alignedPeakCollection[fileID].IsOtherLipidMatch;
            alignmentProperty.SimpleDotProductSimilarity = alignedPeakCollection[fileID].SimpleDotProductSimilarity;

            //alignmentProperty.IdentificationRank = alignedPeakCollection[fileID].IdentificationRank;
            alignmentProperty.LibraryIdList = alignedPeakCollection[fileID].LibraryIdList;

            if (alignedPeakCollection[fileID].Ms2ScanNumber >= 0)
                alignmentProperty.MsmsIncluded = true;
        }

        private static void setRepresentativeFileID(AlignmentPropertyBean alignmentProperty, AnalysisParametersBean param,
            int fileIdOfMaxTotalScoreWithMSMS, int fileIdOfMaxTotalScore, 
            int fileIdOfMaxIntensity, int fileIdOfMaxIntensityWithMSMS)
        {
            if (fileIdOfMaxTotalScoreWithMSMS != -1) alignmentProperty.RepresentativeFileID = fileIdOfMaxTotalScoreWithMSMS;
            else { 
                if (fileIdOfMaxIntensityWithMSMS != -1) alignmentProperty.RepresentativeFileID = fileIdOfMaxIntensityWithMSMS;
                else alignmentProperty.RepresentativeFileID = fileIdOfMaxIntensity;
            }
            if (param.TrackingIsotopeLabels) alignmentProperty.RepresentativeFileID = param.NonLabeledReferenceID;

            var alignedPeakCollection = alignmentProperty.AlignedPeakPropertyBeanCollection;
            var peakID = alignedPeakCollection[alignmentProperty.RepresentativeFileID].PeakID;

            if (peakID < 0)
            {
                for (int k = 0; k < alignedPeakCollection.Count; k++)
                {
                    if (alignedPeakCollection[k].PeakID >= 0)
                    {
                        alignmentProperty.RepresentativeFileID = alignedPeakCollection[k].FileID;
                        break;
                    }
                }
            }
        }

        private static void setBasicAlignmentProperties(AlignmentPropertyBean alignmentProperty, int alignmnetID, AnalysisParametersBean param, 
            out double minInt, out double maxInt, out int fileIdOfMaxIntensity, out int fileIdOfMaxIntensityWithMSMS, out int fileIdOfMaxTotalScore, out int fileIdOfMaxTotalScoreWithMSMS)
        {
            var alignedPeakCollection = alignmentProperty.AlignedPeakPropertyBeanCollection;

            fileIdOfMaxTotalScore = -1; fileIdOfMaxTotalScoreWithMSMS = -1; fileIdOfMaxIntensity = -1; fileIdOfMaxIntensityWithMSMS = -1;
            minInt = double.MaxValue; maxInt = double.MinValue;

            int countFill = 0, countMonoIsotopic = 0;
            double sumRt = 0, sumMass = 0, sumInt = 0, sumPeakWidth = 0,
                minRt = double.MaxValue, maxRt = double.MinValue, minMz = double.MaxValue, maxMz = double.MinValue,
                maxTotalScore = double.MinValue, maxTotalScoreWithSpec = double.MinValue;
            double maxIntMz = alignmentProperty.CentralAccurateMass;

            for (int j = 0; j < alignedPeakCollection.Count; j++)
            {
                if (alignedPeakCollection[j].PeakID < 0) continue;

                sumRt += alignedPeakCollection[j].RetentionTime;
                sumMass += alignedPeakCollection[j].AccurateMass;
                sumInt += alignedPeakCollection[j].Variable;
                sumPeakWidth += alignedPeakCollection[j].PeakWidth;

                if (minInt > alignedPeakCollection[j].Variable) minInt = alignedPeakCollection[j].Variable;
                if (maxInt < alignedPeakCollection[j].Variable) {
                    maxInt = alignedPeakCollection[j].Variable;
                    fileIdOfMaxIntensity = j;
                    if (alignedPeakCollection[j].Ms2ScanNumber >= 0)
                        fileIdOfMaxIntensityWithMSMS = j;

                    maxIntMz = alignedPeakCollection[j].AccurateMass;
                }
                if (minRt > alignedPeakCollection[j].RetentionTime) minRt = alignedPeakCollection[j].RetentionTime;
                if (maxRt < alignedPeakCollection[j].RetentionTime) maxRt = alignedPeakCollection[j].RetentionTime;
                if (minMz > alignedPeakCollection[j].AccurateMass) minMz = alignedPeakCollection[j].AccurateMass;
                if (maxMz < alignedPeakCollection[j].AccurateMass) maxMz = alignedPeakCollection[j].AccurateMass;
                if (alignedPeakCollection[j].IsotopeNumber == 0) countMonoIsotopic++;
                if (alignedPeakCollection[j].Ms2ScanNumber < 0 && maxTotalScore < alignedPeakCollection[j].TotalSimilairty) {
                    maxTotalScore = alignedPeakCollection[j].TotalSimilairty;
                    fileIdOfMaxTotalScore = j;
                }
                if (alignedPeakCollection[j].Ms2ScanNumber >= 0 
                    && maxTotalScoreWithSpec < alignedPeakCollection[j].TotalSimilairty
                    && !alignedPeakCollection[j].MetaboliteName.Contains("w/o MS2:")) {
                    maxTotalScoreWithSpec = alignedPeakCollection[j].TotalSimilairty;
                    fileIdOfMaxTotalScoreWithMSMS = j;
                }

                countFill++;
            }

            alignmentProperty.AlignmentID = alignmnetID;
            alignmentProperty.CentralRetentionTime = (float)(sumRt / countFill);
            //alignmentProperty.CentralAccurateMass = (float)(sumMass / countFill);
            alignmentProperty.CentralAccurateMass = (float)maxIntMz;
            alignmentProperty.AverageValiable = (float)(sumInt / countFill);
            alignmentProperty.AveragePeakWidth = (float)(sumPeakWidth / countFill);
            alignmentProperty.FillParcentage = (float)countFill / (float)alignedPeakCollection.Count;
            alignmentProperty.MonoIsotopicPercentage = (float)countMonoIsotopic / (float)countFill;
            alignmentProperty.MaxMz = (float)maxMz;
            alignmentProperty.MinMz = (float)minMz;
            alignmentProperty.MaxRt = (float)maxRt;
            alignmentProperty.MinRt = (float)minRt;
            alignmentProperty.MaxValiable = (float)maxInt;
            alignmentProperty.MinValiable = (float)minInt;
        }

        private static ObservableCollection<AlignmentPropertyBean> getRefinedAlignmentPropertyBeanCollection(
            ObservableCollection<AlignmentPropertyBean> alignmentPropertyBeanCollection, 
            AnalysisParametersBean param, ProjectPropertyBean project, ref List<int> newIdList)
        {
            if (alignmentPropertyBeanCollection.Count <= 1) return alignmentPropertyBeanCollection;

            var alignmentSpotList = new List<AlignmentPropertyBean>(alignmentPropertyBeanCollection);
            if (param.OnlyReportTopHitForPostAnnotation) { //to remove duplicate identifications

                alignmentSpotList = alignmentSpotList.OrderByDescending(n => n.LibraryID).ToList();

                var currentLibraryId = alignmentSpotList[0].LibraryID;
                var currentPeakId = 0;

                for (int i = 1; i < alignmentSpotList.Count; i++) {
                    if (alignmentSpotList[i].LibraryID < 0) break;
                    if (alignmentSpotList[i].LibraryID != currentLibraryId) {
                        currentLibraryId = alignmentSpotList[i].LibraryID;
                        currentPeakId = i;
                        continue;
                    }
                    else {
                        if (alignmentSpotList[currentPeakId].TotalSimilairty < alignmentSpotList[i].TotalSimilairty) {
                            setDefaultCompoundInformation(alignmentSpotList[currentPeakId]);
                            currentPeakId = i;
                        }
                        else {
                            setDefaultCompoundInformation(alignmentSpotList[i]);
                        }
                    }
                }

                alignmentSpotList = alignmentSpotList.OrderByDescending(n => n.PostIdentificationLibraryID).ToList();

                currentLibraryId = alignmentSpotList[0].PostIdentificationLibraryID;
                currentPeakId = 0;

                for (int i = 1; i < alignmentSpotList.Count; i++) {
                    if (alignmentSpotList[i].PostIdentificationLibraryID < 0) break;
                    if (alignmentSpotList[i].PostIdentificationLibraryID != currentLibraryId) {
                        currentLibraryId = alignmentSpotList[i].PostIdentificationLibraryID;
                        currentPeakId = i;
                        continue;
                    }
                    else {
                        if (alignmentSpotList[currentPeakId].TotalSimilairty < alignmentSpotList[i].TotalSimilairty) {
                            setDefaultCompoundInformation(alignmentSpotList[currentPeakId]);
                            currentPeakId = i;
                        }
                        else {
                            setDefaultCompoundInformation(alignmentSpotList[i]);
                        }
                    }
                }
            }

            //cleaning duplicate spots
            var cSpots = new List<AlignmentPropertyBean>();
            var donelist = new List<int>();

            foreach (var spot in alignmentSpotList.Where(n => n.LibraryID >= 0 && !n.MetaboliteName.Contains("w/o")).OrderByDescending(n => n.TotalSimilairty)) {
                tryMergeToMaster(spot, cSpots, donelist, param);
            }

            foreach (var spot in alignmentSpotList.Where(n => n.PostIdentificationLibraryID >= 0 && !n.MetaboliteName.Contains("w/o")).OrderByDescending(n => n.TotalSimilairty)) {
                tryMergeToMaster(spot, cSpots, donelist, param);
            }

            foreach (var spot in alignmentSpotList.OrderByDescending(n => n.AverageValiable)) {
                if (spot.LibraryID >= 0 && !spot.MetaboliteName.Contains("w/o")) continue;
                if (spot.PostIdentificationLibraryID >= 0 && !spot.MetaboliteName.Contains("w/o")) continue;
                if (spot.PostDefinedIsotopeWeightNumber > 0) continue;
                tryMergeToMaster(spot, cSpots, donelist, param);
            }

            // further cleaing by blank features
            var fcSpots = new List<AlignmentPropertyBean>();
            int blankNumber = 0;
            int sampleNumber = 0;
            foreach (var value in project.FileID_AnalysisFileType.Values) {
                if (value == AnalysisFileType.Blank) blankNumber++;
                if (value == AnalysisFileType.Sample) sampleNumber++;
            }

            if (blankNumber > 0 && param.IsRemoveFeatureBasedOnPeakHeightFoldChange) {
              
                foreach (var spot in cSpots) {
                    var sampleMax = 0.0;
                    var sampleAve = 0.0;
                    var blankAve = 0.0;
                    var nonMinValue = double.MaxValue;

                    foreach (var peak in spot.AlignedPeakPropertyBeanCollection) {
                        var filetype = project.FileID_AnalysisFileType[peak.FileID];
                        if (filetype == AnalysisFileType.Blank) {
                            blankAve += peak.Variable;
                        }
                        else if (filetype == AnalysisFileType.Sample) {
                            if (peak.Variable > sampleMax)
                                sampleMax = peak.Variable;
                            sampleAve += peak.Variable;
                        }

                        if (nonMinValue > peak.Variable && peak.Variable > 0.0001) {
                            nonMinValue = peak.Variable;
                        }
                    }

                    sampleAve = sampleAve / sampleNumber;
                    blankAve = blankAve / blankNumber;
                    if (blankAve == 0) {
                        if (nonMinValue != double.MaxValue)
                            blankAve = nonMinValue * 0.1;
                        else
                            blankAve = 1.0;
                    }

                    var blankThresh = blankAve * param.FoldChangeForBlankFiltering;
                    var sampleThresh = param.BlankFiltering == BlankFiltering.SampleMaxOverBlankAve ? sampleMax : sampleAve;
                
                    if (sampleThresh < blankThresh) {
                        if (param.IsKeepRemovableFeaturesAndAssignedTagForChecking) {

                            if (param.IsKeepIdentifiedMetaboliteFeatures
                              && (spot.LibraryID >= 0 || spot.PostIdentificationLibraryID >= 0) && !spot.MetaboliteName.Contains("w/o")) {

                            }
                            else if (param.IsKeepAnnotatedMetaboliteFeatures
                              && (spot.LibraryID >= 0 || spot.PostIdentificationLibraryID >= 0) && spot.MetaboliteName.Contains("w/o")) {

                            }
                            else {
                                spot.IsBlankFiltered = true;
                            }
                        }
                        else {

                            if (param.IsKeepIdentifiedMetaboliteFeatures
                             && (spot.LibraryID >= 0 || spot.PostIdentificationLibraryID >= 0) && !spot.MetaboliteName.Contains("w/o")) {

                            }
                            else if (param.IsKeepAnnotatedMetaboliteFeatures
                              && (spot.LibraryID >= 0 || spot.PostIdentificationLibraryID >= 0) && spot.MetaboliteName.Contains("w/o")) {

                            }
                            else {
                                continue;
                            }
                        }
                    }

                    fcSpots.Add(spot);
                }
            }
            else {
                fcSpots = cSpots;
            }

            fcSpots = fcSpots.OrderBy(n => n.CentralAccurateMass).ToList();
            if (param.IsIonMobility) {
                foreach (var spot in fcSpots) {
                    spot.AlignedDriftSpots = new ObservableCollection<AlignedDriftSpotPropertyBean>(spot.AlignedDriftSpots.OrderBy(n => n.CentralDriftTime));
                }
            }

            if (param.IsIonMobility) {
                newIdList = new List<int>();
                foreach (var spot in fcSpots) {
                    newIdList.Add(spot.MasterID);
                    foreach (var dspot in spot.AlignedDriftSpots) {
                        newIdList.Add(dspot.MasterID);
                    }
                }
            }
            else {
                newIdList = fcSpots.Select(x => x.AlignmentID).ToList();
            }
            var masterID = 0;
            for (int i = 0; i < fcSpots.Count; i++) {
                fcSpots[i].AlignmentID = i;
                if (param.IsIonMobility) {
                    fcSpots[i].MasterID = masterID;
                    masterID++;
                    var driftSpots = fcSpots[i].AlignedDriftSpots;
                    for (int j = 0; j < driftSpots.Count; j++) {
                        driftSpots[j].MasterID = masterID;
                        driftSpots[j].AlignmentID = j;
                        driftSpots[j].AlignmentSpotID = i;
                        masterID++;
                    }
                }
            }

            #region //checking alignment spot variable correlations
            var rtMargin = 0.06F;
            assignLinksByIonAbundanceCorrelations(fcSpots, rtMargin);
            #endregion

            #region // assigning peak characters from the identified spots
            assignLinksByIdentifiedIonFeatures(fcSpots);
            #endregion

            #region // assigning peak characters from the representative file information
            fcSpots = fcSpots.OrderByDescending(n => n.AverageValiable).ToList();
            foreach (var fcSpot in fcSpots) {

                var repFileID = fcSpot.RepresentativeFileID;
                var repIntensity = fcSpot.AlignedPeakPropertyBeanCollection[repFileID].Variable;
                for (int i = 0; i < fcSpot.AlignedPeakPropertyBeanCollection.Count; i++) {
                    var peak = fcSpot.AlignedPeakPropertyBeanCollection[i];
                    if (peak.Variable > repIntensity) {
                        repFileID = i;
                        repIntensity = peak.Variable;
                    }
                }

                var repProp = fcSpot.AlignedPeakPropertyBeanCollection[repFileID];
                var repLinks = repProp.PeakLinks;
                foreach (var rLink in repLinks) {
                    var rLinkID = rLink.LinkedPeakID;
                    var rLinkProp = rLink.Character;
                    if (rLinkProp == PeakLinkFeatureEnum.Isotope) continue; // for isotope labeled tracking
                    foreach (var rSpot in fcSpots) {
                        if (rSpot.AlignedPeakPropertyBeanCollection[repFileID].PeakID == rLinkID) {
                            if (rLinkProp == PeakLinkFeatureEnum.Adduct) {
                                if (rSpot.AdductIonName != string.Empty) continue;
                                var adductCharge = AdductIonParcer.GetChargeNumber(rSpot.AlignedPeakPropertyBeanCollection[repFileID].AdductIonName);
                                if (rSpot.ChargeNumber != adductCharge) continue;
                                adductCharge = AdductIonParcer.GetChargeNumber(fcSpot.AlignedPeakPropertyBeanCollection[repFileID].AdductIonName);
                                if (fcSpot.ChargeNumber != adductCharge) continue;

                                registerLinks(fcSpot, rSpot, rLinkProp);
                                rSpot.AdductIonName = rSpot.AlignedPeakPropertyBeanCollection[repFileID].AdductIonName;
                                if (fcSpot.AdductIonName == string.Empty) {
                                    fcSpot.AdductIonName = fcSpot.AlignedPeakPropertyBeanCollection[repFileID].AdductIonName;
                                }
                            }
                            else {
                                registerLinks(fcSpot, rSpot, rLinkProp);
                            }
                            break;
                        }
                    }
                }
            }
            #endregion

            #region // finalize adduct features
            foreach (var fcSpot in fcSpots.Where(n => n.AdductIonName == string.Empty)) {
                var chargeNum = fcSpot.ChargeNumber;
                if (project.IonMode == IonMode.Positive) {
                    if (chargeNum >= 2) {
                        fcSpot.AdductIonName = "[M+" + chargeNum + "H]" + chargeNum + "+";
                    }
                    else {
                        fcSpot.AdductIonName = "[M+H]+";
                    }
                }
                else {
                    if (chargeNum >= 2) {
                        fcSpot.AdductIonName = "[M-" + chargeNum + "H]" + chargeNum + "-";
                    }
                    else {
                        fcSpot.AdductIonName = "[M-H]-";
                    }
                }
            }
            #endregion

            // assign putative group IDs
            fcSpots = fcSpots.OrderBy(n => n.AlignmentID).ToList();
            assignPutativePeakgroupIDs(fcSpots);

            return new ObservableCollection<AlignmentPropertyBean>(fcSpots);
        }

        private static void tryMergeToMaster(AlignmentPropertyBean spot, List<AlignmentPropertyBean> cSpots, List<int> donelist, AnalysisParametersBean param) {
            var spotRt = spot.CentralRetentionTime;
            var spotMz = spot.CentralAccurateMass;

            var flg = false;
            var rtTol = param.RetentionTimeAlignmentTolerance < 0.1 ? param.RetentionTimeAlignmentTolerance : 0.1;
            var ms1Tol = param.Ms1AlignmentTolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms1Tol));
            #region // practical parameter changes
            if (spotMz > 500) {
                ms1Tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(spotMz, ppm);
            }
            #endregion

            foreach (var cSpot in cSpots.Where(n => Math.Abs(n.CentralAccurateMass - spotMz) < ms1Tol)) {
                var cSpotRt = cSpot.CentralRetentionTime;
                if (Math.Abs(cSpotRt - spotRt) < rtTol * 0.5) {
                    flg = true;
                    break;
                }
            }
            if (!flg && !donelist.Contains(spot.AlignmentID)) {
                cSpots.Add(spot);
                donelist.Add(spot.AlignmentID);
            }
        }

        private static void assignLinksByIdentifiedIonFeatures(List<AlignmentPropertyBean> cSpots) {
            foreach (var cSpot in cSpots) {
                if ((cSpot.LibraryID >= 0 || cSpot.PostIdentificationLibraryID >= 0) && !cSpot.MetaboliteName.Contains("w/o")) {

                    var repFileID = cSpot.RepresentativeFileID;
                    var repProp = cSpot.AlignedPeakPropertyBeanCollection[repFileID];
                    var repLinks = repProp.PeakLinks;

                    foreach (var rLink in repLinks) {
                        var rLinkID = rLink.LinkedPeakID;
                        var rLinkProp = rLink.Character;
                        if (rLinkProp == PeakLinkFeatureEnum.Isotope) continue; // for isotope tracking
                        foreach (var rSpot in cSpots) {
                            if (rSpot.AlignedPeakPropertyBeanCollection[repFileID].PeakID == rLinkID) {

                                if ((rSpot.LibraryID >= 0 || rSpot.PostIdentificationLibraryID >= 0) && !rSpot.MetaboliteName.Contains("w/o")) {
                                    if (rLinkProp == PeakLinkFeatureEnum.Adduct) {
                                        if (cSpot.AdductIonName == rSpot.AdductIonName) continue;
                                        registerLinks(cSpot, rSpot, rLinkProp);
                                    }
                                    else {
                                        registerLinks(cSpot, rSpot, rLinkProp);
                                    }
                                }
                                else {
                                    if (rLinkProp == PeakLinkFeatureEnum.Adduct) {
                                        var rAdductCharge = AdductIonParcer.GetChargeNumber(rSpot.AlignedPeakPropertyBeanCollection[repFileID].AdductIonName);
                                        if (rAdductCharge == rSpot.ChargeNumber) {
                                            rSpot.AdductIonName = rSpot.AlignedPeakPropertyBeanCollection[repFileID].AdductIonName;
                                            registerLinks(cSpot, rSpot, rLinkProp);
                                        }
                                    }
                                    else {
                                        registerLinks(cSpot, rSpot, rLinkProp);
                                    }
                                }
                             
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static void registerLinks(AlignmentPropertyBean cSpot, AlignmentPropertyBean rSpot, PeakLinkFeatureEnum rLinkProp) {
            if (cSpot.PeakLinks.Count(n => n.LinkedPeakID == rSpot.AlignmentID && n.Character == rLinkProp) == 0) {
                cSpot.PeakLinks.Add(new LinkedPeakFeature() {
                    LinkedPeakID = rSpot.AlignmentID,
                    Character = rLinkProp
                });
                cSpot.IsLinked = true;
            }
            if (rSpot.PeakLinks.Count(n => n.LinkedPeakID == cSpot.AlignmentID && n.Character == rLinkProp) == 0) {
                rSpot.PeakLinks.Add(new LinkedPeakFeature() {
                    LinkedPeakID = cSpot.AlignmentID,
                    Character = rLinkProp
                });
                rSpot.IsLinked = true;
            }
        }

        // currently, the links for same metabolite, isotope, and adduct are grouped.
        // the others such as found in upper msms and chromatogram correlation are not grouped.
        // in future, I have to create the merge GUI for user side
        private static void assignPutativePeakgroupIDs(List<AlignmentPropertyBean> alignedSpots) {
            var groupID = 0;
            foreach (var spot in alignedSpots) {
                if (spot.PeakGroupID >= 0) continue;
                if (spot.PeakLinks.Count == 0) {
                    spot.PeakGroupID = groupID;
                }
                else {
                    var crawledPeaks = new List<int>();
                    spot.PeakGroupID = groupID;
                    recPeakGroupAssignment(spot, alignedSpots, groupID, crawledPeaks);
                }
                groupID++;
            }
        }

        private static void recPeakGroupAssignment(AlignmentPropertyBean spot, List<AlignmentPropertyBean> alignedSpots, 
            int groupID, List<int> crawledPeaks) {
            if (spot.PeakLinks == null || spot.PeakLinks.Count == 0) return;

            foreach (var linkedPeak in spot.PeakLinks) {
                var linkedPeakID = linkedPeak.LinkedPeakID;
                var character = linkedPeak.Character;
                if (character == PeakLinkFeatureEnum.ChromSimilar) continue;
                if (character == PeakLinkFeatureEnum.CorrelSimilar) continue;
                if (character == PeakLinkFeatureEnum.FoundInUpperMsMs) continue;
                if (crawledPeaks.Contains(linkedPeakID)) continue;

                alignedSpots[linkedPeakID].PeakGroupID = groupID;
                crawledPeaks.Add(linkedPeakID);

                if (isCrawledPeaks(alignedSpots[linkedPeakID].PeakLinks, crawledPeaks, spot.AlignmentID)) continue;
                recPeakGroupAssignment(alignedSpots[linkedPeakID], alignedSpots, groupID, crawledPeaks);
            }
        }

        private static bool isCrawledPeaks(List<LinkedPeakFeature> peakLinks, List<int> crawledPeaks, int peakID) {
            if (peakLinks.Count(n => n.LinkedPeakID != peakID) == 0) return true;
            var frag = false;
            foreach (var linkID in peakLinks.Select(n => n.LinkedPeakID)) {
                if (crawledPeaks.Contains(linkID)) continue;
                frag = true;
                break;
            }
            if (frag == true) return false;
            else return true;
        }

        private static void setDefaultCompoundInformation(AlignmentPropertyBean alignmentPropertyBean)
        {
            alignmentPropertyBean.LibraryID = -1;
            alignmentPropertyBean.PostIdentificationLibraryID = -1;
            alignmentPropertyBean.AdductIonName = string.Empty;
            alignmentPropertyBean.ChargeNumber = 1;
            alignmentPropertyBean.MetaboliteName = string.Empty;
            alignmentPropertyBean.TotalSimilairty = -1;
            alignmentPropertyBean.MassSpectraSimilarity = -1;
            alignmentPropertyBean.ReverseSimilarity = -1;
            alignmentPropertyBean.IsotopeSimilarity = -1;
            alignmentPropertyBean.RetentionTimeSimilarity = -1;
            alignmentPropertyBean.CcsSimilarity = -1;
            alignmentPropertyBean.AccurateMassSimilarity = -1;
            alignmentPropertyBean.IsMs1Match = false;
            alignmentPropertyBean.IsMs2Match = false;
            alignmentPropertyBean.IsRtMatch = false;
            alignmentPropertyBean.IsCcsMatch = false;
            alignmentPropertyBean.IsLipidClassMatch = false;
            alignmentPropertyBean.IsLipidChainsMatch = false;
            alignmentPropertyBean.IsLipidPositionMatch = false;
            alignmentPropertyBean.IsOtherLipidMatch = false;

        }

        #region ion mobility data alignment
        public static List<PeakAreaBean> GetJointALignerMasterListAtIonMobility(RdamPropertyBean rdamProperty, 
            ObservableCollection<AnalysisFileBean> files, AnalysisParametersBean param) {
            var file = files[param.AlignmentReferenceFileID];
            DataStorageLcUtility.SetPeakAreaBeanCollection(file, file.AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

            var masterPeaklist = getInitialMasterPeaklist(new List<PeakAreaBean>(file.PeakAreaBeanCollection), param);
            DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(file);

            if (masterPeaklist == null || masterPeaklist.Count == 0) {
                return null;
            }

            for (int i = 0; i < files.Count; i++) {
                if (i == param.AlignmentReferenceFileID) continue;
                file = files[i];

                var fileProp = file.AnalysisFilePropertyBean;
                var paiFilepath = fileProp.PeakAreaBeanInformationFilePath;
                DataStorageLcUtility.SetPeakAreaBeanCollection(file, paiFilepath);

                var peakSpots = file.PeakAreaBeanCollection;
                if (peakSpots == null || peakSpots.Count == 0) continue;

                masterPeaklist = addJointAlignerMasterListAtIonMobility(masterPeaklist, new List<PeakAreaBean>(peakSpots), param);
                DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(files[i]);
            }

            masterPeaklist = masterPeaklist.OrderBy(n => n.AccurateMass).ThenBy(n => n.RtAtPeakTop).ToList();

            var counter = 0;
            foreach (var peak in masterPeaklist) {
                peak.MasterPeakID = counter;
                counter++;

                peak.DriftSpots = peak.DriftSpots.OrderBy(n => n.DriftTimeAtPeakTop).ToList();
                foreach (var drift in peak.DriftSpots) {
                    drift.MasterPeakID = counter;
                    counter++;
                }

            }
            return masterPeaklist;
        }

        private static List<PeakAreaBean> addJointAlignerMasterListAtIonMobility(List<PeakAreaBean> peakAreaBeanMasterList, List<PeakAreaBean> peakAreaBeanList,
            AnalysisParametersBean param) {
            peakAreaBeanMasterList = peakAreaBeanMasterList.OrderBy(n => n.AccurateMass).ThenBy(n => n.RtAtPeakTop).ToList();
            peakAreaBeanList = peakAreaBeanList.OrderBy(n => n.AccurateMass).ThenBy(n => n.RtAtPeakTop).ToList();

            var addedPeakAreaBeanMasterList = new List<PeakAreaBean>();
            var maxIntensity = peakAreaBeanMasterList.Max(n => n.IntensityAtPeakTop);
            var rtTol = param.RetentionTimeAlignmentTolerance;
            var massTol = param.Ms1AlignmentTolerance;
            var dtTol = param.DriftTimeAlignmentTolerance;

            for (int i = 0; i < peakAreaBeanList.Count; i++) { // first, alignment is executed for rt vs m/z
                var peakSpot = peakAreaBeanList[i];
                if (peakSpot.IsotopeWeightNumber != 0) continue;
                var alignChecker = false;

                var peakSpotMz = peakSpot.AccurateMass;
                var peakSpotRt = peakSpot.RtAtPeakTop;
                var startIndex = DataAccessLcUtility.GetPeakAreaIntensityListStartIndex(peakAreaBeanMasterList, peakSpotMz - massTol);

                for (int j = startIndex; j < peakAreaBeanMasterList.Count; j++) {
                    var masterPeakSpot = peakAreaBeanMasterList[j];
                    var masterPeakMz = masterPeakSpot.AccurateMass;
                    if (masterPeakMz < peakSpotMz - massTol) continue;
                    if (masterPeakMz > peakSpotMz + massTol) break;

                    var masterPeakRt = masterPeakSpot.RtAtPeakTop;
                    if (Math.Abs(masterPeakMz - peakSpotMz) <= massTol && Math.Abs(masterPeakRt - peakSpotRt) <= rtTol) {
                        alignChecker = true;

                        var masterDriftSpots = masterPeakSpot.DriftSpots;
                        var peakDriftSpots = peakSpot.DriftSpots;
                        var addedDriftSpotsToMasterList = new List<DriftSpotBean>();

                        var maxIntensityOnDrift = masterDriftSpots.Max(n => n.IntensityAtPeakTop);
                        foreach (var pDrift in peakDriftSpots) {
                            var alignCheckerOnDrift = false;
                            foreach (var mDrift in masterDriftSpots) {
                                if (Math.Abs(pDrift.DriftTimeAtPeakTop - mDrift.DriftTimeAtPeakTop) < dtTol) {
                                    alignCheckerOnDrift = true;
                                    break;
                                }
                            }
                            if (alignCheckerOnDrift == false && maxIntensityOnDrift * 0.0001 < pDrift.IntensityAtPeakTop) {
                                addedDriftSpotsToMasterList.Add(pDrift);
                            }
                        }
                        foreach (var aDriftSpot in addedDriftSpotsToMasterList) {
                            masterDriftSpots.Add(aDriftSpot);
                        }
                    }
                }

                if (alignChecker == false && maxIntensity * 0.0001 < peakAreaBeanList[i].IntensityAtPeakTop) {
                    addedPeakAreaBeanMasterList.Add(peakAreaBeanList[i]);
                }
            }

            foreach (var aPeakSpot in addedPeakAreaBeanMasterList) {
                peakAreaBeanMasterList.Add(aPeakSpot);
            }

            return peakAreaBeanMasterList;
        }

        /// <summary>
        /// This program is for the initianlization of alignment result storage.
        /// AlignmentResultBean in IMS data contains the collection of alignment spot including the average RT, m/z, dt, the abundances of each sample.
        /// This program just prepares the meta data of sample files.
        /// </summary>
        public static void JointAlignerResultInitializeAtIonMobility(AlignmentResultBean alignmentResult, List<PeakAreaBean> masterPeakSpots, ObservableCollection<AnalysisFileBean> files) {
            alignmentResult.SampleNumber = files.Count;
            // alignmentResult.AlignmentIdNumber = masterPeakSpots.Count;
            var counter = 0;

            for (int i = 0; i < masterPeakSpots.Count; i++) {

                var alignSpot = new AlignmentPropertyBean() {
                    MasterID = counter,
                    AlignmentID = i,
                    CentralRetentionTime = masterPeakSpots[i].RtAtPeakTop,
                    CentralAccurateMass = masterPeakSpots[i].AccurateMass
                };
                for (int j = 0; j < files.Count; j++) {
                    alignSpot.AlignedPeakPropertyBeanCollection.Add(
                        new AlignedPeakPropertyBean() {
                            FileID = files[j].AnalysisFilePropertyBean.AnalysisFileId,
                            FileName = files[j].AnalysisFilePropertyBean.AnalysisFileName
                        });
                }

                counter++;

                var driftSpots = masterPeakSpots[i].DriftSpots;
                for (int j = 0; j < driftSpots.Count; j++) {
                    var alignDriftSpot = new AlignedDriftSpotPropertyBean() {
                        MasterID = counter,
                        AlignmentID = j,
                        CentralDriftTime = driftSpots[j].DriftTimeAtPeakTop,
                        CentralAccurateMass = driftSpots[j].AccurateMass
                    };
                    for (int k = 0; k < files.Count; k++) {
                        alignDriftSpot.AlignedPeakPropertyBeanCollection.Add(new AlignedPeakPropertyBean() {
                            FileID = files[k].AnalysisFilePropertyBean.AnalysisFileId,
                            FileName = files[k].AnalysisFilePropertyBean.AnalysisFileName
                        });
                    }
                    alignSpot.AlignedDriftSpots.Add(alignDriftSpot);

                    counter++;
                }

                alignmentResult.AlignmentPropertyBeanCollection.Add(alignSpot);
            }
            alignmentResult.AlignmentIdNumber = counter;
        }

        public static void AlignToMasterListAtIonMobility(AnalysisFileBean file, List<PeakAreaBean> peakSpots, List<PeakAreaBean> masterPeakSpots,
            AlignmentResultBean alignmentResult, AnalysisParametersBean param) {
            peakSpots = peakSpots.OrderBy(n => n.AccurateMass).ThenBy(n => n.RtAtPeakTop).ToList();
            masterPeakSpots = masterPeakSpots.OrderBy(n => n.AccurateMass).ThenBy(n => n.RtAtPeakTop).ToList();

            var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
            var fileID = file.AnalysisFilePropertyBean.AnalysisFileId;
            var fileName = file.AnalysisFilePropertyBean.AnalysisFileName;
            var maxFactors = new double[alignmentResult.AlignmentIdNumber];

            var rtTol = param.RetentionTimeAlignmentTolerance;
            var dtTol = param.DriftTimeAlignmentTolerance;
            var mzTol = param.Ms1AlignmentTolerance;
            var rtfactor = param.RetentionTimeAlignmentFactor;
            var mzfactor = param.Ms1AlignmentFactor;
            var dtfactor = param.DriftTimeAlignmentFactor;

            for (int i = 0; i < peakSpots.Count; i++) {
                if (peakSpots[i].IsotopeWeightNumber != 0) continue;

                var startIndex = DataAccessLcUtility.GetPeakAreaIntensityListStartIndex(masterPeakSpots, peakSpots[i].AccurateMass - param.Ms1AlignmentTolerance);
                var peakSpotMz = peakSpots[i].AccurateMass;
                var peakSpotRt = peakSpots[i].RtAtPeakTop;
                var driftSpots = peakSpots[i].DriftSpots;

                //Debug.WriteLine(i);


                for (int k = 0; k < driftSpots.Count; k++) {

                    var maxMasterId = -1;
                    var maxMatchIdOnRt = -1;
                    var maxMatchIdOnDt = -1;
                    var matchFactor = 0.0;
                    var maxMatchFactor = double.MinValue;

                    //if (i == 29) {
                    //    Debug.WriteLine(k);
                    //}

                    var drifttime = driftSpots[k].DriftTimeAtPeakTop;

                    for (int j = startIndex; j < masterPeakSpots.Count; j++) {

                        //if (i == 29 && k == 0) {
                        //    Debug.WriteLine(j);
                        //}

                        var masterSpotMz = masterPeakSpots[j].AccurateMass;
                        var masterSpotRt = masterPeakSpots[j].RtAtPeakTop;
                        if (peakSpotMz - mzTol > masterSpotMz) continue;
                        if (peakSpotMz + mzTol < masterSpotMz) break;

                        var mzDiff = Math.Abs(peakSpotMz - masterSpotMz);
                        var rtDiff = Math.Abs(peakSpotRt - masterSpotRt);

                        var masterDriftSpots = masterPeakSpots[j].DriftSpots;
                        for (int p = 0; p < masterDriftSpots.Count; p++) {

                            //if (i == 29 && k == 0 && j == 31) {
                            //    Debug.WriteLine(p);
                            //}

                            var masterDrifttime = masterDriftSpots[p].DriftTimeAtPeakTop;
                            var dtDiff = Math.Abs(masterDrifttime - drifttime);
                            var masterID = masterDriftSpots[p].MasterPeakID;

                            matchFactor
                                = rtfactor * Math.Exp(-0.5 * Math.Pow(rtDiff, 2) / Math.Pow(rtTol, 2))
                                + mzfactor * Math.Exp(-0.5 * Math.Pow(mzDiff, 2) / Math.Pow(mzTol, 2))
                                + dtfactor * Math.Exp(-0.5 * Math.Pow(dtDiff, 2) / Math.Pow(dtTol, 2));

                            if (maxMatchFactor < matchFactor && maxFactors[masterID] < matchFactor) {
                                maxMatchFactor = matchFactor;
                                maxMatchIdOnRt = j;
                                maxMatchIdOnDt = p;
                                maxMasterId = masterID;
                            }
                        }
                    }

                    if (maxMasterId == -1) {
                        Debug.WriteLine("Alignment candidate not found sample {0}, m/z {1}, rt {2}", file.AnalysisFilePropertyBean.AnalysisFileName, peakSpotMz, peakSpotRt);
                        continue;
                    }
                    maxFactors[maxMasterId] = matchFactor;
                    setAlignmentResult(alignedSpots[maxMatchIdOnRt].AlignedPeakPropertyBeanCollection[fileID], peakSpots[i],
                        alignedSpots[maxMatchIdOnRt].AlignedDriftSpots[maxMatchIdOnDt].AlignedPeakPropertyBeanCollection[fileID], driftSpots[k],
                        fileID, fileName);
                }
            }
        }

        private static void setAlignmentResult(AlignedPeakPropertyBean alignmentProperty, PeakAreaBean peakAreaBean,
            AlignedPeakPropertyBean alignmentDriftProperty, DriftSpotBean driftSpotBean,
            int fileID, string fileName) {

            alignmentProperty.AccurateMass = peakAreaBean.AccurateMass;
            alignmentProperty.FileID = fileID;
            alignmentProperty.FileName = fileName;
            alignmentProperty.PeakID = peakAreaBean.PeakID;
            alignmentProperty.MasterPeakID = peakAreaBean.MasterPeakID;
            alignmentProperty.PeakAreaBeanID = peakAreaBean.PeakID;
            alignmentProperty.RetentionTime = peakAreaBean.RtAtPeakTop;
            alignmentProperty.RetentionTimeLeft = peakAreaBean.RtAtLeftPeakEdge;
            alignmentProperty.RetentionTimeRight = peakAreaBean.RtAtRightPeakEdge;
            alignmentProperty.Variable = peakAreaBean.IntensityAtPeakTop;
            alignmentProperty.Area = peakAreaBean.AreaAboveZero;
            alignmentProperty.PeakWidth = peakAreaBean.RtAtRightPeakEdge - peakAreaBean.RtAtLeftPeakEdge;
            alignmentProperty.Ms1ScanNumber = peakAreaBean.Ms1LevelDatapointNumber;
            alignmentProperty.Ms2ScanNumber = peakAreaBean.Ms2LevelDatapointNumber;
            alignmentProperty.MetaboliteName = peakAreaBean.MetaboliteName;
            alignmentProperty.LibraryID = peakAreaBean.LibraryID;
            alignmentProperty.PostIdentificationLibraryID = peakAreaBean.PostIdentificationLibraryId;
            alignmentProperty.MassSpectraSimilarity = peakAreaBean.MassSpectraSimilarityValue;
            alignmentProperty.ReverseSimilarity = peakAreaBean.ReverseSearchSimilarityValue;
            alignmentProperty.FragmentPresencePercentage = peakAreaBean.PresenseSimilarityValue;
            alignmentProperty.IsotopeSimilarity = peakAreaBean.IsotopeSimilarityValue;
            alignmentProperty.AdductIonName = peakAreaBean.AdductIonName;
            alignmentProperty.IsotopeNumber = peakAreaBean.IsotopeWeightNumber;
            alignmentProperty.IsotopeParentID = peakAreaBean.IsotopeParentPeakID;
            alignmentProperty.ChargeNumber = peakAreaBean.ChargeNumber;
            alignmentProperty.RetentionTimeSimilarity = peakAreaBean.RtSimilarityValue;
            alignmentProperty.CcsSimilarity = peakAreaBean.CcsSimilarity;
            alignmentProperty.AccurateMassSimilarity = peakAreaBean.AccurateMassSimilarity;
            alignmentProperty.TotalSimilairty = peakAreaBean.TotalScore;
            alignmentProperty.IsLinked = peakAreaBean.IsLinked;
            alignmentProperty.PeakGroupID = peakAreaBean.PeakGroupID;
            alignmentProperty.PeakLinks = peakAreaBean.PeakLinks;
            alignmentProperty.EstimatedNoise = peakAreaBean.EstimatedNoise;
            alignmentProperty.SignalToNoise = peakAreaBean.SignalToNoise;
            alignmentProperty.IsMs1Match = peakAreaBean.IsMs1Match;
            alignmentProperty.IsMs2Match = peakAreaBean.IsMs2Match;
            alignmentProperty.IsRtMatch = peakAreaBean.IsRtMatch;
            alignmentProperty.IsCcsMatch = peakAreaBean.IsCcsMatch;
            alignmentProperty.IsLipidClassMatch = peakAreaBean.IsLipidClassMatch;
            alignmentProperty.IsLipidChainsMatch = peakAreaBean.IsLipidChainsMatch;
            alignmentProperty.IsLipidPositionMatch = peakAreaBean.IsLipidPositionMatch;
            alignmentProperty.IsOtherLipidMatch = peakAreaBean.IsOtherLipidMatch;


            alignmentDriftProperty.AccurateMass = driftSpotBean.AccurateMass;
            alignmentDriftProperty.FileID = fileID;
            alignmentDriftProperty.FileName = fileName;
            alignmentDriftProperty.PeakID = driftSpotBean.PeakID;
            alignmentDriftProperty.MasterPeakID = driftSpotBean.MasterPeakID;
            alignmentDriftProperty.PeakAreaBeanID = driftSpotBean.PeakAreaBeanID;
            alignmentDriftProperty.DriftTime = driftSpotBean.DriftTimeAtPeakTop;
            alignmentDriftProperty.DriftTimeLeft = driftSpotBean.DriftTimeAtLeftPeakEdge;
            alignmentDriftProperty.DriftTimeRight = driftSpotBean.DriftTimeAtRightPeakEdge;
            alignmentDriftProperty.RetentionTime = peakAreaBean.RtAtPeakTop;
            alignmentDriftProperty.RetentionTimeLeft = peakAreaBean.RtAtLeftPeakEdge;
            alignmentDriftProperty.RetentionTimeRight = peakAreaBean.RtAtRightPeakEdge;
            alignmentDriftProperty.Ccs = driftSpotBean.Ccs;
            alignmentDriftProperty.Variable = driftSpotBean.IntensityAtPeakTop;
            alignmentDriftProperty.Area = driftSpotBean.AreaAboveZero;
            alignmentDriftProperty.PeakWidth = driftSpotBean.DriftTimeAtRightPeakEdge - driftSpotBean.DriftTimeAtLeftPeakEdge;
            alignmentDriftProperty.Ms1ScanNumber = driftSpotBean.Ms1LevelDatapointNumber;
            alignmentDriftProperty.Ms2ScanNumber = driftSpotBean.Ms2LevelDatapointNumber;
            alignmentDriftProperty.MetaboliteName = driftSpotBean.MetaboliteName;
            alignmentDriftProperty.LibraryID = driftSpotBean.LibraryID;
            alignmentDriftProperty.PostIdentificationLibraryID = driftSpotBean.PostIdentificationLibraryId;
            alignmentDriftProperty.MassSpectraSimilarity = driftSpotBean.MassSpectraSimilarityValue;
            alignmentDriftProperty.ReverseSimilarity = driftSpotBean.ReverseSearchSimilarityValue;
            alignmentDriftProperty.FragmentPresencePercentage = driftSpotBean.PresenseSimilarityValue;
            alignmentDriftProperty.IsotopeSimilarity = driftSpotBean.IsotopeSimilarityValue;
            alignmentDriftProperty.AdductIonName = driftSpotBean.AdductIonName;
            alignmentDriftProperty.IsotopeNumber = driftSpotBean.IsotopeWeightNumber;
            alignmentDriftProperty.IsotopeParentID = driftSpotBean.IsotopeParentPeakID;
            alignmentDriftProperty.ChargeNumber = driftSpotBean.ChargeNumber;
            alignmentDriftProperty.RetentionTimeSimilarity = driftSpotBean.RtSimilarityValue;
            alignmentDriftProperty.AccurateMassSimilarity = driftSpotBean.AccurateMassSimilarity;
            alignmentDriftProperty.CcsSimilarity = driftSpotBean.CcsSimilarity;
            alignmentDriftProperty.TotalSimilairty = driftSpotBean.TotalScore;
            //alignmentDriftProperty.IsLinked = driftSpotBean.IsLinked;
            //alignmentDriftProperty.PeakGroupID = driftSpotBean.PeakGroupID;
            //alignmentDriftProperty.PeakLinks = driftSpotBean.PeakLinks;
            alignmentDriftProperty.EstimatedNoise = driftSpotBean.EstimatedNoise;
            alignmentDriftProperty.SignalToNoise = driftSpotBean.SignalToNoise;

            alignmentDriftProperty.IsMs1Match = driftSpotBean.IsMs1Match;
            alignmentDriftProperty.IsMs2Match = driftSpotBean.IsMs2Match;
            alignmentDriftProperty.IsRtMatch = driftSpotBean.IsRtMatch;
            alignmentDriftProperty.IsCcsMatch = driftSpotBean.IsCcsMatch;

            alignmentDriftProperty.IsLipidClassMatch = driftSpotBean.IsLipidClassMatch;
            alignmentDriftProperty.IsLipidChainsMatch = driftSpotBean.IsLipidChainsMatch;
            alignmentDriftProperty.IsLipidPositionMatch = driftSpotBean.IsLipidPositionMatch;
            alignmentDriftProperty.IsOtherLipidMatch = driftSpotBean.IsOtherLipidMatch;
        }

        /// <summary>
        /// This program will remove the alignment spot which is not satisfied with the criteria of alignment result:
        /// 1) if an alignment spot does not have any peak information from samples, the spot will be excluded.
        /// 2) (At least QC filter, checked) if all of peaks of quality control samples is not assigned to an alignment spot, the spot will be excluded.
        /// 3) if the percentage of filled (not missing values) is less than the user-defined criteria, the spot will be excluded.
        /// </summary>
        public static void FilteringJointAlignerAtIonMobility(ProjectPropertyBean project, AnalysisParametersBean param, AlignmentResultBean alignmentResultBean) {
            if (alignmentResultBean.AlignmentPropertyBeanCollection == null || alignmentResultBean.AlignmentPropertyBeanCollection.Count == 0) return;
            int maxQcNumber = 0, sampleCount = 0;
            foreach (var value in project.FileID_AnalysisFileType.Values) {
                if (value == AnalysisFileType.QC) maxQcNumber++;
                sampleCount++;
            }
            var masterGroupCountDict = getGroupCountDictionary(project, alignmentResultBean.AlignmentPropertyBeanCollection[0]);
            var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;

            int peakCount, qcCount;
            double sumRt, sumMass, sumPeakWidth, maxRt, minRt, minMz, maxMz;
            double maxPeakMz, maxPeakIntensity, maxPeakRt, minPeakIntensity;
            double maxNoise, minNoise, sumNoise, maxSN, minSN, sumSN;
            double minCcs, maxCcs, sumCcs;

            for (int i = 0; i < alignedSpots.Count; i++) {
               
                var alignedSpot = alignedSpots[i];
                var driftSpots = alignedSpot.AlignedDriftSpots;
                var isAllExcluded = false;

                for (int j = 0; j < driftSpots.Count; j++) {

                    peakCount = 0; qcCount = 0; // int
                    sumRt = 0; sumMass = 0; sumPeakWidth = 0; minRt = double.MaxValue; maxRt = double.MinValue; minMz = double.MaxValue; maxMz = double.MinValue; // double
                    maxPeakMz = double.MinValue; maxPeakIntensity = double.MinValue; maxPeakRt = double.MinValue; minPeakIntensity = double.MaxValue; // double
                    maxNoise = double.MinValue; minNoise = double.MaxValue; sumNoise = 0; // double
                    maxSN = double.MinValue; minSN = double.MaxValue; sumSN = 0; // double
                    minCcs = double.MaxValue; maxCcs = double.MinValue; sumCcs = 0;

                    var driftSpot = driftSpots[j];
                    var localGroupCountDict = new Dictionary<string, int>();
                    foreach (var key in masterGroupCountDict.Keys) localGroupCountDict[key] = 0;

                    var alignedDriftPeaks = driftSpot.AlignedPeakPropertyBeanCollection;
                    for (int k = 0; k < alignedDriftPeaks.Count; k++) {
                        var alignedPeak = alignedDriftPeaks[k];
                        if (alignedPeak.PeakID < 0) continue;
                        if (project.FileID_AnalysisFileType[alignedPeak.FileID] == AnalysisFileType.QC)
                            qcCount++;

                        sumRt += alignedPeak.DriftTime;
                        sumMass += alignedPeak.AccurateMass;
                        sumPeakWidth += alignedPeak.PeakWidth;
                        sumSN += alignedPeak.SignalToNoise;
                        sumNoise += alignedPeak.EstimatedNoise;
                        sumCcs += alignedPeak.Ccs;

                        if (minRt > alignedPeak.DriftTime) minRt = alignedPeak.DriftTime;
                        if (maxRt < alignedPeak.DriftTime) maxRt = alignedPeak.DriftTime;
                        if (minMz > alignedPeak.AccurateMass) minMz = alignedPeak.AccurateMass;
                        if (maxMz < alignedPeak.AccurateMass) maxMz = alignedPeak.AccurateMass;
                        if (maxPeakIntensity < alignedPeak.Variable) {
                            maxPeakIntensity = alignedPeak.Variable;
                            maxPeakMz = alignedPeak.AccurateMass;
                            maxPeakRt = alignedPeak.DriftTime;
                        }
                        if (minPeakIntensity > alignedPeak.Variable) minPeakIntensity = alignedPeak.Variable;
                        if (minSN > alignedPeak.SignalToNoise) minSN = alignedPeak.SignalToNoise;
                        if (maxSN < alignedPeak.SignalToNoise) maxSN = alignedPeak.SignalToNoise;
                        if (minNoise > alignedPeak.EstimatedNoise) minNoise = alignedPeak.EstimatedNoise;
                        if (maxNoise < alignedPeak.EstimatedNoise) maxNoise = alignedPeak.EstimatedNoise;
                        if (minCcs > alignedPeak.Ccs) minCcs = alignedPeak.Ccs;
                        if (maxCcs < alignedPeak.Ccs) maxCcs = alignedPeak.Ccs;

                        peakCount++;
                        var fileId = alignedPeak.FileID;
                        var classID = project.FileID_ClassName[fileId];
                        var filetype = project.FileID_AnalysisFileType[fileId];
                        //if (filetype == AnalysisFileType.Sample)
                        localGroupCountDict[classID]++;
                    }

                    if (peakCount == 0) {
                        driftSpots.RemoveAt(j);
                        if (driftSpots.Count == 0) {
                            isAllExcluded = true;
                            break;
                        }
                        j--;
                        continue;
                    }

                    if ((float)((float)peakCount / (float)sampleCount) * 100F < param.PeakCountFilter) {
                        driftSpots.RemoveAt(j);
                        if (driftSpots.Count == 0) {
                            isAllExcluded = true;
                            break;
                        }
                        j--;
                        continue;
                    }

                    if (param.QcAtLeastFilter && maxQcNumber != qcCount) {
                        driftSpots.RemoveAt(j);
                        if (driftSpots.Count == 0) {
                            isAllExcluded = true;
                            break;
                        }
                        j--;
                        continue;
                    }

                    var isNpercentDetectedAtOneGroup = false;
                    foreach (var pair in localGroupCountDict) {
                        var id = pair.Key;
                        var count = pair.Value;
                        var totalCount = masterGroupCountDict[id];
                        if ((float)count / (float)totalCount * 100 >= param.NPercentDetectedInOneGroup) {
                            isNpercentDetectedAtOneGroup = true;
                            break;
                        }
                    }

                    if (isNpercentDetectedAtOneGroup == false) {
                        driftSpots.RemoveAt(j);
                        if (driftSpots.Count == 0) {
                            isAllExcluded = true;
                            break;
                        }
                        j--;
                        continue;
                    }

                    driftSpot.CentralDriftTime = (float)(sumRt / peakCount);
                    driftSpot.CentralAccurateMass = (float)maxPeakMz;
                    driftSpot.AveragePeakWidth = (float)(sumPeakWidth / peakCount);
                    driftSpot.CentralCcs = (float)(sumCcs / peakCount);

                    driftSpot.MaxDt = (float)maxRt;
                    driftSpot.MinDt = (float)minRt;
                    driftSpot.MaxMz = (float)maxMz;
                    driftSpot.MinMz = (float)minMz;
                    driftSpot.MinValiable = (float)minPeakIntensity; // tempolarily
                    driftSpot.MaxValiable = (float)maxPeakIntensity; // tempolarily
                    driftSpot.MaxCcs = (float)maxCcs;
                    driftSpot.MinCcs = (float)minCcs;

                    driftSpot.SignalToNoiseMax = (float)maxSN;
                    driftSpot.SignalToNoiseMin = (float)minSN;
                    driftSpot.SignalToNoiseAve = (float)(sumSN / peakCount);

                    driftSpot.EstimatedNoiseMax = (float)maxNoise;
                    driftSpot.EstimatedNoiseMin = (float)minNoise;
                    driftSpot.EstimatedNoiseAve = (float)(sumNoise / peakCount);

                    if (driftSpot.EstimatedNoiseAve < 1)
                        driftSpot.EstimatedNoiseAve = 1.0F;
                }

                if (isAllExcluded) {
                    alignedSpots.RemoveAt(i);
                    i--;
                    continue;
                }

                peakCount = 0; qcCount = 0; // int
                sumRt = 0; sumMass = 0; sumPeakWidth = 0; minRt = double.MaxValue; maxRt = double.MinValue; minMz = double.MaxValue; maxMz = double.MinValue; // double
                maxPeakMz = double.MinValue; maxPeakIntensity = double.MinValue; maxPeakRt = double.MinValue; minPeakIntensity = double.MaxValue; // double
                maxNoise = double.MinValue; minNoise = double.MaxValue; sumNoise = 0; // double
                maxSN = double.MinValue; minSN = double.MaxValue; sumSN = 0; // double


                for (int j = 0; j < alignedSpot.AlignedPeakPropertyBeanCollection.Count; j++) {
                    var alignedPeak = alignedSpot.AlignedPeakPropertyBeanCollection[j];
                    if (alignedPeak.PeakID < 0) continue;

                    sumRt += alignedPeak.RetentionTime;
                    sumMass += alignedPeak.AccurateMass;
                    sumPeakWidth += alignedPeak.PeakWidth;
                    sumSN += alignedPeak.SignalToNoise;
                    sumNoise += alignedPeak.EstimatedNoise;

                    if (minRt > alignedPeak.RetentionTime) minRt = alignedPeak.RetentionTime;
                    if (maxRt < alignedPeak.RetentionTime) maxRt = alignedPeak.RetentionTime;
                    if (minMz > alignedPeak.AccurateMass) minMz = alignedPeak.AccurateMass;
                    if (maxMz < alignedPeak.AccurateMass) maxMz = alignedPeak.AccurateMass;
                    if (maxPeakIntensity < alignedPeak.Variable) {
                        maxPeakIntensity = alignedPeak.Variable;
                        maxPeakMz = alignedPeak.AccurateMass;
                        maxPeakRt = alignedPeak.RetentionTime;
                    }
                    if (minPeakIntensity > alignedPeak.Variable) minPeakIntensity = alignedPeak.Variable;
                    if (minSN > alignedPeak.SignalToNoise) minSN = alignedPeak.SignalToNoise;
                    if (maxSN < alignedPeak.SignalToNoise) maxSN = alignedPeak.SignalToNoise;
                    if (minNoise > alignedPeak.EstimatedNoise) minNoise = alignedPeak.EstimatedNoise;
                    if (maxNoise < alignedPeak.EstimatedNoise) maxNoise = alignedPeak.EstimatedNoise;

                    peakCount++;
                }

                alignedSpot.CentralRetentionTime = (float)(sumRt / peakCount);
                alignedSpot.CentralAccurateMass = (float)maxPeakMz;
                alignedSpot.AveragePeakWidth = (float)(sumPeakWidth / peakCount);

                alignedSpot.MaxRt = (float)maxRt;
                alignedSpot.MinRt = (float)minRt;
                alignedSpot.MaxMz = (float)maxMz;
                alignedSpot.MinMz = (float)minMz;
                alignedSpot.MinValiable = (float)minPeakIntensity; // tempolarily
                alignedSpot.MaxValiable = (float)maxPeakIntensity; // tempolarily

                alignedSpot.SignalToNoiseMax = (float)maxSN;
                alignedSpot.SignalToNoiseMin = (float)minSN;
                alignedSpot.SignalToNoiseAve = (float)(sumSN / peakCount);

                alignedSpot.EstimatedNoiseMax = (float)maxNoise;
                alignedSpot.EstimatedNoiseMin = (float)minNoise;
                alignedSpot.EstimatedNoiseAve = (float)(sumNoise / peakCount);
                if (alignedSpot.EstimatedNoiseAve < 1)
                    alignedSpot.EstimatedNoiseAve = 1.0F;
            }
        }

        /// <summary>
        /// This program performs the interpolation, i.e. gap-filling, for missing values.
        /// </summary>
        public static void GapFillingMethod(ObservableCollection<RawSpectrum> spectrumCollection, AlignmentPropertyBean alignedProp,
            AlignedDriftSpotPropertyBean driftSpotProp, AlignedPeakPropertyBean alignedPeakProperty, ProjectPropertyBean projectProperty, AnalysisParametersBean param,
            float centralRt, float centralDt, float centralMZ, float dtTol, float mzTol, float averagePeakWidth) {
            gapfillingVS2(spectrumCollection, alignedProp, driftSpotProp, alignedPeakProperty, projectProperty, param,
                        centralRt, centralDt, centralMZ, dtTol, mzTol, averagePeakWidth);
        }

        private static void gapfillingVS2(ObservableCollection<RawSpectrum> spectrumCollection, AlignmentPropertyBean alignedProp, AlignedDriftSpotPropertyBean alignmentProperty,
            AlignedPeakPropertyBean alignedPeakProperty, ProjectPropertyBean projectProperty, AnalysisParametersBean param,
            float centralRT, float centralDT, float centralMZ, float dtTol, float mzTol, float averagePeakWidth) {

            if (mzTol < 0.005) mzTol = 0.005F;
            if (averagePeakWidth < 0.2) averagePeakWidth = 0.2F;

            var targetDt = centralDT;
            var peaklist = DataAccessLcUtility.GetDriftChromatogramByRtMz(spectrumCollection, 
                alignedProp.CentralRetentionTime, param.AccumulatedRtRagne, centralMZ, mzTol, targetDt - dtTol, targetDt + dtTol);
            var sPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
            var isForceInsert = param.IsForceInsertForGapFilling;
            var repfileid = alignedPeakProperty.FileID;
            var calinfo = param.FileidToCcsCalibrantData[repfileid];

            alignedPeakProperty.RetentionTime = alignedProp.CentralRetentionTime;
            alignedPeakProperty.RetentionTimeLeft = alignedProp.CentralRetentionTime;
            alignedPeakProperty.RetentionTimeRight = alignedProp.CentralRetentionTime;
            alignedPeakProperty.DriftTime = centralDT;
            alignedPeakProperty.DriftTimeLeft = centralDT;
            alignedPeakProperty.DriftTimeRight = centralDT;
            alignedPeakProperty.AccurateMass = centralMZ;
            alignedPeakProperty.ChargeNumber = alignedProp.ChargeNumber;
            alignedPeakProperty.Variable = 0;
            alignedPeakProperty.Area = 0;
            alignedPeakProperty.PeakID = -2;
            alignedPeakProperty.EstimatedNoise = alignmentProperty.EstimatedNoiseAve;
            if (alignedPeakProperty.EstimatedNoise < 1) alignedPeakProperty.EstimatedNoise = 1.0F;
            alignedPeakProperty.SignalToNoise = 1.0F;

            //Debug.WriteLine(centralRT + "\t" + centralMZ + "\t" + peaklist.Count);
            if (sPeaklist == null || sPeaklist.Count == 0) return;
            if (isForceInsert == false) return;
            //finding local maximum list
            var candidates = new List<DriftSpotBean>();
            var minRtId = -1;
            var minRtDiff = double.MaxValue;
            if (sPeaklist.Count != 0) {
                for (int i = 2; i < sPeaklist.Count - 2; i++) {

                    if (sPeaklist[i][1] < centralDT - dtTol) continue;
                    if (centralDT + dtTol < sPeaklist[i][1]) break;

                    if ((sPeaklist[i - 2][3] <= sPeaklist[i - 1][3] &&
                       sPeaklist[i - 1][3] <= sPeaklist[i][3] &&
                       sPeaklist[i][3] > sPeaklist[i + 1][3]) ||
                       (sPeaklist[i - 1][3] < sPeaklist[i][3] &&
                       sPeaklist[i][3] >= sPeaklist[i + 1][3] &&
                       sPeaklist[i + 1][3] >= sPeaklist[i + 2][3])) {

                        candidates.Add(new DriftSpotBean() {
                            DriftScanAtPeakTop = i,
                            DriftTimeAtPeakTop = (float)sPeaklist[i][1],
                            AccurateMass = (float)sPeaklist[i][2],
                            IntensityAtPeakTop = (float)sPeaklist[i][3]
                        });
                    }

                    var diff = Math.Abs(sPeaklist[i][1] - centralDT);
                    if (diff < minRtDiff) {
                        minRtDiff = diff;
                        minRtId = i;
                    }
                }
            }

            if (minRtId == -1) {
                minRtId = (int)(sPeaklist.Count * 0.5);
            }

            if (candidates.Count == 0) { // meaning really not detected
                if (isForceInsert == false)
                    return;
                else {
                    var range = 5;

                    // checking left ID
                    var leftID = minRtId - 1;
                    for (int i = minRtId - 1; i > minRtId - range; i--) {
                        leftID = i;
                        if (i - 1 < 0) {
                            leftID = 0;
                            break;
                        }
                        if (sPeaklist[i][3] < sPeaklist[i - 1][3]) {
                            break;
                        }
                    }

                    // checking right ID
                    var rightID = minRtId + 1;
                    for (int i = minRtId + 1; i < minRtId + range; i++) {
                        rightID = i;
                        if (i + 1 > sPeaklist.Count - 1) {
                            rightID = sPeaklist.Count - 1;
                            break;
                        }
                        if (sPeaklist[i][3] < sPeaklist[i + 1][3]) {
                            break;
                        }
                    }

                    var peakAreaAboveZero = 0.0;
                    for (int i = leftID; i <= rightID - 1; i++) {
                        peakAreaAboveZero += (sPeaklist[i][3] + sPeaklist[i + 1][3]) * (sPeaklist[i + 1][1] - sPeaklist[i][1]) * 0.5;
                    }

                    var peakHeight = Math.Max(sPeaklist[minRtId][3] - sPeaklist[leftID][3], sPeaklist[minRtId][3] - sPeaklist[rightID][3]);

                    alignedPeakProperty.DriftTime = (float)sPeaklist[minRtId][1];
                    alignedPeakProperty.AccurateMass = (float)sPeaklist[minRtId][2];
                    alignedPeakProperty.Variable = (float)sPeaklist[minRtId][3];
                    alignedPeakProperty.Area = (float)peakAreaAboveZero * 60;
                    alignedPeakProperty.DriftTimeLeft = (float)sPeaklist[leftID][1];
                    alignedPeakProperty.DriftTimeRight = (float)sPeaklist[rightID][1];
                    alignedPeakProperty.Ccs = (float)IonMobilityUtility.MobilityToCrossSection(param.IonMobilityType,
                        alignedPeakProperty.DriftTime, Math.Abs(alignmentProperty.ChargeNumber), alignedPeakProperty.AccurateMass,
                        calinfo, param.IsAllCalibrantDataImported);
                    alignedPeakProperty.SignalToNoise = 1.0F;
                }
            }
            else {
                // searching a representative local maximum. 
                // Now, the peak having nearest RT from central RT is selected.
                minRtId = -1;
                minRtDiff = double.MaxValue;

                for (int i = 0; i < candidates.Count; i++) {
                    var diff = Math.Abs(candidates[i].DriftTimeAtPeakTop - centralDT);
                    if (diff < minRtDiff) {
                        minRtDiff = diff;
                        minRtId = candidates[i].DriftScanAtPeakTop;
                    }
                }

                // now, drawing new chromatogram using base peak m/z
                // here, there is a possibility that local maximum point may be slightly different from the original one, therefore, 
                // after finding local minimum (for left and right), the real top will be searched.

                // local minimum is searched from margin(2) point left from current local maximum.

                var margin = 2;
                var localMinLeft = minRtId - margin;
                if (localMinLeft < 1) localMinLeft = 0;
                else {
                    for (int i = minRtId - margin; i >= 1; i--) {
                        localMinLeft = i;
                        if (sPeaklist[i][3] <= sPeaklist[i - 1][3]) {
                            break;
                        }
                    }
                }

                var localMinRight = minRtId + margin;
                if (localMinRight > sPeaklist.Count - 2) localMinRight = sPeaklist.Count - 1;
                else {
                    for (int i = minRtId + margin; i < sPeaklist.Count - 1; i++) {
                        localMinRight = i;
                        if (sPeaklist[i][3] <= sPeaklist[i + 1][3]) {
                            break;
                        }
                    }
                }

                if (isForceInsert == false && (minRtId - localMinLeft < 2 || localMinRight - minRtId < 2)) return;

                var maxIntensity = 0.0;
                var maxID = minRtId;
                for (int i = localMinLeft + 1; i <= localMinRight - 1; i++) {
                    if ((sPeaklist[i - 1][3] <= sPeaklist[i][3] && sPeaklist[i][3] > sPeaklist[i + 1][3]) ||
                       (sPeaklist[i - 1][3] < sPeaklist[i][3] && sPeaklist[i][3] >= sPeaklist[i + 1][3])) {
                        if (maxIntensity < sPeaklist[i][3]) {
                            maxIntensity = sPeaklist[i][3];
                            maxID = i;
                        }
                    }
                }

                //calculating peak area
                var peakAreaAboveZero = 0.0;
                for (int i = localMinLeft; i <= localMinRight - 1; i++) {
                    peakAreaAboveZero += (sPeaklist[i][3] + sPeaklist[i + 1][3]) * (sPeaklist[i + 1][1] - sPeaklist[i][1]) * 0.5;
                }

                var peakHeight = Math.Max(sPeaklist[maxID][3] - sPeaklist[localMinLeft][3],
                    sPeaklist[maxID][3] - sPeaklist[localMinRight][3]);

                alignedPeakProperty.DriftTime = (float)sPeaklist[maxID][1];
                alignedPeakProperty.AccurateMass = (float)centralMZ;
                alignedPeakProperty.Variable = (float)maxIntensity;
                alignedPeakProperty.Area = (float)peakAreaAboveZero * 60;
                alignedPeakProperty.DriftTimeLeft = (float)sPeaklist[localMinLeft][1];
                alignedPeakProperty.DriftTimeRight = (float)sPeaklist[localMinRight][1];
                alignedPeakProperty.SignalToNoise = (float)(peakHeight / alignedPeakProperty.EstimatedNoise);
                alignedPeakProperty.Ccs = (float)IonMobilityUtility.MobilityToCrossSection(param.IonMobilityType,
                        alignedPeakProperty.DriftTime, Math.Abs(alignmentProperty.ChargeNumber), alignedPeakProperty.AccurateMass,
                        calinfo, param.IsAllCalibrantDataImported);
            }
        }

        /// <summary>
        /// This program performs the assingment of representative MS/MS spectrum assigned to each alignment spot. See the AlignmentFinalizeProcess.cs.
        /// Now the MS/MS spectrum of a sample file having the highest identification score will be assigned to the alignment spot.
        /// In the case that no identification result is assigned to every samples, the MS/MS spectrum of a sample file having the most abundant spectrum will be assigned to the alignment spot.
        /// This process will be performed as ansynchronous process.
        /// </summary>
        public static void FinalizeJointAlignerAtIonMobility(AlignmentResultBean alignmentResult,
            ObservableCollection<AnalysisFileBean> files,
            AnalysisParametersBean param, ProjectPropertyBean projectProperty, IupacReferenceBean iupacRef, ref List<int> newIdList) {

            int fileIdOfMaxTotalScore = -1, fileIdOfMaxTotalScoreWithMSMS = -1, fileIdOfMaxIntensity = -1,
                fileIdOfMaxIntensityWithMSMS = -1;
            double minInt = double.MaxValue, maxInt = double.MinValue, 
                minIntTotal = double.MaxValue, maxIntTotal = double.MinValue;

            var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
            var masterID = 0;
            for (int i = 0; i < alignedSpots.Count; i++) {
                //Debug.WriteLine(i);
                var alignedSpot = alignedSpots[i];

                alignedSpot.MasterID = masterID;
                masterID++;

                setBasicAlignmentProperties(alignedSpots[i], i, param, out minInt, out maxInt, out fileIdOfMaxIntensity, out fileIdOfMaxIntensityWithMSMS, out fileIdOfMaxTotalScore, out fileIdOfMaxTotalScoreWithMSMS);
                setRepresentativeFileID(alignedSpots[i], param, fileIdOfMaxTotalScoreWithMSMS, fileIdOfMaxTotalScore, fileIdOfMaxIntensity, fileIdOfMaxIntensityWithMSMS);
                setRepresentativeIdentificationResult(alignedSpots[i]);

                if (maxIntTotal < maxInt) maxIntTotal = maxInt;
                if (minIntTotal > minInt) minIntTotal = minInt;

                var driftSpots = alignedSpot.AlignedDriftSpots;
                var maxIdentScore = -1.0F;
                var maxIdentID = -1;
                var maxAnnotateScore = -1.0F;
                var maxAnnotateID = -1;
                var maxPostIdentScore = -1.0F;
                var maxPostIdentID = -1;

                for (int j = 0; j < driftSpots.Count; j++) {

                    //if (i == 175) {
                    //    Debug.WriteLine(j);
                    //}

                    var driftSpot = driftSpots[j];

                    driftSpot.MasterID = masterID;
                    driftSpot.AlignmentSpotID = i;
                    masterID++;

                    setBasicAlignmentProperties(driftSpots[j], j, param, out minInt, out maxInt, out fileIdOfMaxIntensity, out fileIdOfMaxIntensityWithMSMS, out fileIdOfMaxTotalScore, out fileIdOfMaxTotalScoreWithMSMS);
                    setRepresentativeFileID(driftSpots[j], param, fileIdOfMaxTotalScoreWithMSMS, fileIdOfMaxTotalScore, fileIdOfMaxIntensity, fileIdOfMaxIntensityWithMSMS);
                    setRepresentativeIdentificationResult(driftSpots[j], param);

                    if (driftSpots[j].LibraryID >= 0 && !driftSpots[j].MetaboliteName.Contains("w/o")) {
                        if (driftSpots[j].TotalSimilairty > maxIdentScore) {
                            maxIdentScore = driftSpots[j].TotalSimilairty;
                            maxIdentID = j;
                        }
                    }

                    if (driftSpots[j].LibraryID >= 0 && driftSpots[j].MetaboliteName.Contains("w/o")) {
                        if (driftSpots[j].TotalSimilairty > maxAnnotateScore) {
                            maxAnnotateScore = driftSpots[j].TotalSimilairty;
                            maxAnnotateID = j;
                        }
                    }

                    if (driftSpots[j].PostIdentificationLibraryID >= 0) {
                        if (driftSpots[j].TotalSimilairty > maxPostIdentScore) {
                            maxPostIdentScore = driftSpots[j].TotalSimilairty;
                            maxPostIdentID = j;
                        }
                    }
                }

                if (maxPostIdentID >= 0) {
                    CopyMaxDriftSpotInfoToPeakSpot(driftSpots[maxPostIdentID], alignedSpot);
                }
                else if (maxIdentID >= 0) {
                    CopyMaxDriftSpotInfoToPeakSpot(driftSpots[maxIdentID], alignedSpot);
                }
                else if (maxAnnotateID >= 0) {
                    CopyMaxDriftSpotInfoToPeakSpot(driftSpots[maxAnnotateID], alignedSpot);
                }
                else {
                    setDefaultCompoundInformation(alignedSpot);
                }

            }

            //if (!param.TrackingIsotopeLabels)
            reanalysisOfIsotopesInAlignmentSpots(alignedSpots, param, projectProperty, iupacRef);
            alignedSpots = getRefinedAlignmentPropertyBeanCollection(alignedSpots, param, projectProperty, ref newIdList);

            if (maxIntTotal > 1) maxIntTotal = Math.Log(maxIntTotal, 2); else maxIntTotal = 1;
            if (minIntTotal > 1) minIntTotal = Math.Log(minIntTotal, 2); else minIntTotal = 0;

            for (int i = 0; i < alignedSpots.Count; i++) {
                var relativeValue = (float)((Math.Log((double)alignedSpots[i].MaxValiable, 2) - minIntTotal)
                    / (maxIntTotal - minIntTotal));
                if (relativeValue < 0)
                    relativeValue = 0;
                else if (relativeValue > 1)
                    relativeValue = 1;
                alignedSpots[i].RelativeAmplitudeValue = relativeValue;
            }

            alignmentResult.AlignmentPropertyBeanCollection = alignedSpots;
            alignmentResult.AlignmentIdNumber = newIdList.Count;
        }

        private static void CopyMaxDriftSpotInfoToPeakSpot(AlignedDriftSpotPropertyBean alignedDriftSpotPropertyBean, AlignmentPropertyBean alignmentProperty) {
            alignmentProperty.LibraryID = alignedDriftSpotPropertyBean.LibraryID;
            alignmentProperty.PostIdentificationLibraryID = alignedDriftSpotPropertyBean.PostIdentificationLibraryID;
            alignmentProperty.AdductIonName = alignedDriftSpotPropertyBean.AdductIonName;
            alignmentProperty.ChargeNumber = alignedDriftSpotPropertyBean.ChargeNumber;
            alignmentProperty.MetaboliteName = alignedDriftSpotPropertyBean.MetaboliteName;
            alignmentProperty.TotalSimilairty = alignedDriftSpotPropertyBean.TotalSimilairty;
            alignmentProperty.MassSpectraSimilarity = alignedDriftSpotPropertyBean.MassSpectraSimilarity;
            alignmentProperty.ReverseSimilarity = alignedDriftSpotPropertyBean.ReverseSimilarity;
            alignmentProperty.CcsSimilarity = alignedDriftSpotPropertyBean.CcsSimilarity;
            alignmentProperty.FragmentPresencePercentage = alignedDriftSpotPropertyBean.FragmentPresencePercentage;
            alignmentProperty.IsotopeSimilarity = alignedDriftSpotPropertyBean.IsotopeSimilarity;
            alignmentProperty.RetentionTimeSimilarity = alignedDriftSpotPropertyBean.RetentionTimeSimilarity;
            alignmentProperty.AccurateMassSimilarity = alignedDriftSpotPropertyBean.AccurateMassSimilarity;
            alignmentProperty.MsmsIncluded = alignedDriftSpotPropertyBean.MsmsIncluded;
            alignmentProperty.IsMs1Match = alignedDriftSpotPropertyBean.IsMs1Match;
            alignmentProperty.IsMs2Match = alignedDriftSpotPropertyBean.IsMs2Match;
            alignmentProperty.IsRtMatch = alignedDriftSpotPropertyBean.IsRtMatch;
            alignmentProperty.IsCcsMatch = alignedDriftSpotPropertyBean.IsCcsMatch;
            alignmentProperty.IsLipidClassMatch = alignedDriftSpotPropertyBean.IsLipidClassMatch;
            alignmentProperty.IsLipidChainsMatch = alignedDriftSpotPropertyBean.IsLipidChainsMatch;
            alignmentProperty.IsLipidPositionMatch = alignedDriftSpotPropertyBean.IsLipidPositionMatch;
            alignmentProperty.IsOtherLipidMatch = alignedDriftSpotPropertyBean.IsOtherLipidMatch;
        }

        private static void setRepresentativeIdentificationResult(AlignedDriftSpotPropertyBean alignmentProperty, AnalysisParametersBean param) {

            var fileID = alignmentProperty.RepresentativeFileID;
            var alignedPeakCollection = alignmentProperty.AlignedPeakPropertyBeanCollection;
            var calinfo = param.FileidToCcsCalibrantData[fileID];

            alignmentProperty.LibraryID = alignedPeakCollection[fileID].LibraryID;
            alignmentProperty.PostIdentificationLibraryID = alignedPeakCollection[fileID].PostIdentificationLibraryID;
            alignmentProperty.AdductIonName = alignedPeakCollection[fileID].AdductIonName;
            alignmentProperty.ChargeNumber = alignedPeakCollection[fileID].ChargeNumber;
            alignmentProperty.MetaboliteName = alignedPeakCollection[fileID].MetaboliteName;
            alignmentProperty.TotalSimilairty = alignedPeakCollection[fileID].TotalSimilairty;
            alignmentProperty.MassSpectraSimilarity = alignedPeakCollection[fileID].MassSpectraSimilarity;
            alignmentProperty.ReverseSimilarity = alignedPeakCollection[fileID].ReverseSimilarity;
            alignmentProperty.FragmentPresencePercentage = alignedPeakCollection[fileID].FragmentPresencePercentage;
            alignmentProperty.IsotopeSimilarity = alignedPeakCollection[fileID].IsotopeSimilarity;
            alignmentProperty.RetentionTimeSimilarity = alignedPeakCollection[fileID].RetentionTimeSimilarity;
            alignmentProperty.CcsSimilarity = alignedPeakCollection[fileID].CcsSimilarity;
            alignmentProperty.AccurateMassSimilarity = alignedPeakCollection[fileID].AccurateMassSimilarity;

            alignmentProperty.IsMs1Match = alignedPeakCollection[fileID].IsMs1Match;
            alignmentProperty.IsMs2Match = alignedPeakCollection[fileID].IsMs2Match;
            alignmentProperty.IsRtMatch = alignedPeakCollection[fileID].IsRtMatch;
            alignmentProperty.IsCcsMatch = alignedPeakCollection[fileID].IsCcsMatch;

            alignmentProperty.IsLipidClassMatch = alignedPeakCollection[fileID].IsLipidClassMatch;
            alignmentProperty.IsLipidChainsMatch = alignedPeakCollection[fileID].IsLipidChainsMatch;
            alignmentProperty.IsLipidPositionMatch = alignedPeakCollection[fileID].IsLipidPositionMatch;
            alignmentProperty.IsOtherLipidMatch = alignedPeakCollection[fileID].IsOtherLipidMatch;
            //alignmentProperty.IdentificationRank = alignedPeakCollection[fileID].IdentificationRank;
            //alignmentProperty.LibraryIdList = alignedPeakCollection[fileID].LibraryIdList;

            // calculate ccs here
            var adduct = AdductIonParcer.GetAdductIonBean(alignmentProperty.AdductIonName);
            //var exactmass = MolecularFormulaUtility.ConvertPrecursorMzToExactMass(adduct, alignmentProperty.CentralAccurateMass);
            alignmentProperty.CentralCcs = (float)IonMobilityUtility.MobilityToCrossSection(param.IonMobilityType, alignmentProperty.CentralDriftTime, 
                Math.Abs(alignmentProperty.ChargeNumber), alignmentProperty.CentralAccurateMass, calinfo, param.IsAllCalibrantDataImported);
            alignmentProperty.MinCcs = (float)IonMobilityUtility.MobilityToCrossSection(param.IonMobilityType, alignmentProperty.MinDt, Math.Abs(alignmentProperty.ChargeNumber), alignmentProperty.CentralAccurateMass, calinfo, param.IsAllCalibrantDataImported);
            alignmentProperty.MaxCcs = (float)IonMobilityUtility.MobilityToCrossSection(param.IonMobilityType, alignmentProperty.MaxDt, Math.Abs(alignmentProperty.ChargeNumber), alignmentProperty.CentralAccurateMass, calinfo, param.IsAllCalibrantDataImported);


            if (alignedPeakCollection[fileID].Ms2ScanNumber >= 0)
                alignmentProperty.MsmsIncluded = true;
        }

        private static void setRepresentativeFileID(AlignedDriftSpotPropertyBean alignmentProperty, AnalysisParametersBean param,
            int fileIdOfMaxTotalScoreWithMSMS, int fileIdOfMaxTotalScore,
            int fileIdOfMaxIntensity, int fileIdOfMaxIntensityWithMSMS) {
            if (fileIdOfMaxTotalScoreWithMSMS != -1) alignmentProperty.RepresentativeFileID = fileIdOfMaxTotalScoreWithMSMS;
            else {
                if (fileIdOfMaxIntensityWithMSMS != -1) alignmentProperty.RepresentativeFileID = fileIdOfMaxIntensityWithMSMS;
                else alignmentProperty.RepresentativeFileID = fileIdOfMaxIntensity;
            }
            if (param.TrackingIsotopeLabels) alignmentProperty.RepresentativeFileID = param.NonLabeledReferenceID;

            var alignedPeakCollection = alignmentProperty.AlignedPeakPropertyBeanCollection;
            var peakID = alignedPeakCollection[alignmentProperty.RepresentativeFileID].PeakID;

            if (peakID < 0) {
                for (int k = 0; k < alignedPeakCollection.Count; k++) {
                    if (alignedPeakCollection[k].PeakID >= 0) {
                        alignmentProperty.RepresentativeFileID = alignedPeakCollection[k].FileID;
                        break;
                    }
                }
            }
        }

        private static void setBasicAlignmentProperties(AlignedDriftSpotPropertyBean alignmentProperty, int alignmnetID, 
            AnalysisParametersBean param, out double minInt, out double maxInt, out int fileIdOfMaxIntensity, out int fileIdOfMaxIntensityWithMSMS, out int fileIdOfMaxTotalScore, out int fileIdOfMaxTotalScoreWithMSMS) {
            var alignedPeakCollection = alignmentProperty.AlignedPeakPropertyBeanCollection;

            fileIdOfMaxTotalScore = -1; fileIdOfMaxTotalScoreWithMSMS = -1; fileIdOfMaxIntensity = -1; fileIdOfMaxIntensityWithMSMS = -1;
            minInt = double.MaxValue; maxInt = double.MinValue;

            int countFill = 0, countMonoIsotopic = 0;
            double sumRt = 0, sumMass = 0, sumInt = 0, sumPeakWidth = 0,
                minRt = double.MaxValue, maxRt = double.MinValue, minMz = double.MaxValue, maxMz = double.MinValue,
                maxTotalScore = double.MinValue, maxTotalScoreWithSpec = double.MinValue, sumCcs = 0, maxCcs = double.MinValue, minCcs = double.MaxValue;
            double maxIntMz = alignmentProperty.CentralAccurateMass;

            for (int j = 0; j < alignedPeakCollection.Count; j++) {
                if (alignedPeakCollection[j].PeakID < 0) continue;

                sumRt += alignedPeakCollection[j].DriftTime;
                sumMass += alignedPeakCollection[j].AccurateMass;
                sumInt += alignedPeakCollection[j].Variable;
                sumPeakWidth += alignedPeakCollection[j].PeakWidth;
                sumCcs += alignedPeakCollection[j].Ccs;

                if (minInt > alignedPeakCollection[j].Variable) minInt = alignedPeakCollection[j].Variable;
                if (maxInt < alignedPeakCollection[j].Variable) {
                    maxInt = alignedPeakCollection[j].Variable;
                    fileIdOfMaxIntensity = j;
                    if (alignedPeakCollection[j].Ms2ScanNumber >= 0)
                        fileIdOfMaxIntensityWithMSMS = j;

                    maxIntMz = alignedPeakCollection[j].AccurateMass;
                }
                if (minRt > alignedPeakCollection[j].RetentionTime) minRt = alignedPeakCollection[j].RetentionTime;
                if (maxRt < alignedPeakCollection[j].RetentionTime) maxRt = alignedPeakCollection[j].RetentionTime;
                if (minMz > alignedPeakCollection[j].AccurateMass) minMz = alignedPeakCollection[j].AccurateMass;
                if (maxMz < alignedPeakCollection[j].AccurateMass) maxMz = alignedPeakCollection[j].AccurateMass;
                if (minCcs > alignedPeakCollection[j].Ccs) minCcs = alignedPeakCollection[j].Ccs;
                if (maxCcs < alignedPeakCollection[j].Ccs) maxCcs = alignedPeakCollection[j].Ccs;
                if (alignedPeakCollection[j].IsotopeNumber == 0) countMonoIsotopic++;
                if (alignedPeakCollection[j].Ms2ScanNumber < 0 && maxTotalScore < alignedPeakCollection[j].TotalSimilairty) {
                    maxTotalScore = alignedPeakCollection[j].TotalSimilairty;
                    fileIdOfMaxTotalScore = j;
                }
                if (alignedPeakCollection[j].Ms2ScanNumber >= 0
                    && maxTotalScoreWithSpec < alignedPeakCollection[j].TotalSimilairty
                    && !alignedPeakCollection[j].MetaboliteName.Contains("w/o MS2:")) {
                    maxTotalScoreWithSpec = alignedPeakCollection[j].TotalSimilairty;
                    fileIdOfMaxTotalScoreWithMSMS = j;
                }
                countFill++;
            }

            alignmentProperty.AlignmentID = alignmnetID;
            alignmentProperty.CentralDriftTime = (float)(sumRt / countFill);
            //alignmentProperty.CentralAccurateMass = (float)(sumMass / countFill);
            alignmentProperty.CentralAccurateMass = (float)maxIntMz;
            alignmentProperty.AverageValiable = (int)(sumInt / countFill);
            alignmentProperty.AveragePeakWidth = (float)(sumPeakWidth / countFill);
            alignmentProperty.FillParcentage = (float)countFill / (float)alignedPeakCollection.Count;
            alignmentProperty.MonoIsotopicPercentage = (float)countMonoIsotopic / (float)countFill;
            alignmentProperty.MaxMz = (float)maxMz;
            alignmentProperty.MinMz = (float)minMz;
            alignmentProperty.MaxDt = (float)maxRt;
            alignmentProperty.MinDt = (float)minRt;
            alignmentProperty.MaxValiable = (int)maxInt;
            alignmentProperty.MinValiable = (int)minInt;

            alignmentProperty.CentralCcs = (float)(sumCcs / countFill);
            alignmentProperty.MaxCcs = (float)maxCcs;
            alignmentProperty.MinCcs = (float)minCcs;
        }
        #endregion

        public static void SetAdductIonInformation(AlignmentPropertyBean prop, MspFormatCompoundInformationBean mspQuery) {
            if (mspQuery.AdductIonBean != null && mspQuery.AdductIonBean.FormatCheck == true) {
                prop.AdductIonName = mspQuery.AdductIonBean.AdductIonName; 
            }
        }

        public static void SetAdductIonInformation(AlignedDriftSpotPropertyBean prop, MspFormatCompoundInformationBean mspQuery) {
            if (mspQuery.AdductIonBean != null && mspQuery.AdductIonBean.FormatCheck == true) {
                prop.AdductIonName = mspQuery.AdductIonBean.AdductIonName;
            }
        }
    }
}

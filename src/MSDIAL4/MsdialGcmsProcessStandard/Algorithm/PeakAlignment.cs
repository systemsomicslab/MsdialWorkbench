using edu.ucdavis.fiehnlab.msdial.Writers;
using Msdial.Gcms.Dataprocess.Utility;
using Msdial.Lcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CompMs.Common.DataObj;

namespace Msdial.Gcms.Dataprocess.Algorithm
{
    public sealed class PeakAlignment
    {
        private const int versionNumber = 1;

        public static void JointAligner(ProjectPropertyBean project, ObservableCollection<AnalysisFileBean> files, AnalysisParamOfMsdialGcms param, AlignmentResultBean alignmentResult, 
            List<MspFormatCompoundInformationBean> mspDB, Action<int> reportAction)
        {
            var masterMS1DecResults = PeakAlignment.getJointAlignerMasterList(files, param);
            masterMS1DecResults = getRefinedMS1DecResults(masterMS1DecResults, param.AlignmentIndexType);
            
            jointAlignerResultInitialize(alignmentResult, masterMS1DecResults, files);

            for (int i = 0; i < files.Count; i++)
            {
                var ms1DecResults = DataStorageGcUtility.ReadMS1DecResults(files[i].AnalysisFilePropertyBean.DeconvolutionFilePath);
                if (param.AlignmentIndexType == AlignmentIndexType.RT)
                    alignToMasterListByRetentionTime(files[i], ms1DecResults, masterMS1DecResults, alignmentResult, param, mspDB);
                else
                    alignToMasterListByRetentionIndex(files[i], ms1DecResults, masterMS1DecResults, alignmentResult, param, mspDB);

                reportAction?.Invoke(i + 1);
            }

            filteringJointAligner(project, files, param, alignmentResult);
        }

        private static List<MS1DecResult> getJointAlignerMasterList(ObservableCollection<AnalysisFileBean> files, AnalysisParamOfMsdialGcms param)
        {
            var refFile = files[param.AlignmentReferenceFileID].AnalysisFilePropertyBean.DeconvolutionFilePath;
            var masterMs1DecResults = DataStorageGcUtility.ReadMS1DecResults(refFile);

            for (int i = 0; i < files.Count; i++)
            {
                if (files[i].AnalysisFilePropertyBean.AnalysisFileId == param.AlignmentReferenceFileID) continue;

                var ms1DecResults = DataStorageGcUtility.ReadMS1DecResults(files[i].AnalysisFilePropertyBean.DeconvolutionFilePath);
                if (param.AlignmentIndexType == AlignmentIndexType.RT)
                    masterMs1DecResults = addJointAlignerMasterListByRetentionTime(masterMs1DecResults, ms1DecResults, param);
                else
                    masterMs1DecResults = addJointAlignerMasterListByRetentionIndex(masterMs1DecResults, ms1DecResults, param);
            }

            if (param.AlignmentIndexType == AlignmentIndexType.RT)
                masterMs1DecResults = masterMs1DecResults.OrderBy(n => n.RetentionTime).ThenBy(n => n.BasepeakMz).ToList();
            else
                masterMs1DecResults = masterMs1DecResults.OrderBy(n => n.RetentionIndex).ThenBy(n => n.BasepeakMz).ToList();

            return masterMs1DecResults;
        }

        private static void jointAlignerResultInitialize(AlignmentResultBean alignmentResult, List<MS1DecResult> masterMS1DecResults, ObservableCollection<AnalysisFileBean> files)
        {
            alignmentResult.SampleNumber = files.Count;
            alignmentResult.AlignmentIdNumber = masterMS1DecResults.Count;

            for (int i = 0; i < masterMS1DecResults.Count; i++)
            {
                var alignmentPropertyBean = new AlignmentPropertyBean() 
                { 
                    AlignmentID = i, 
                    CentralRetentionTime = masterMS1DecResults[i].RetentionTime, 
                    CentralRetentionIndex = masterMS1DecResults[i].RetentionIndex,
                    QuantMass = masterMS1DecResults[i].BasepeakMz 
                };
                for (int j = 0; j < files.Count; j++)
                    alignmentPropertyBean.AlignedPeakPropertyBeanCollection.Add(new AlignedPeakPropertyBean() { FileID = files[j].AnalysisFilePropertyBean.AnalysisFileId, FileName = files[j].AnalysisFilePropertyBean.AnalysisFileName });

                alignmentResult.AlignmentPropertyBeanCollection.Add(alignmentPropertyBean);
            }
        }

        private static void alignToMasterListByRetentionTime(AnalysisFileBean file, List<MS1DecResult> ms1DecResults, List<MS1DecResult> masterMs1DecResults, AlignmentResultBean alignmentResult, AnalysisParamOfMsdialGcms param, List<MspFormatCompoundInformationBean> mspDB)
        {
            ms1DecResults = ms1DecResults.OrderBy(n => n.RetentionTime).ThenBy(n => n.BasepeakMz).ToList();
            masterMs1DecResults = masterMs1DecResults.OrderBy(n => n.RetentionTime).ThenBy(n => n.BasepeakMz).ToList();

            var alignmentSpots = alignmentResult.AlignmentPropertyBeanCollection;
            var fileID = file.AnalysisFilePropertyBean.AnalysisFileId;
            var fileName = file.AnalysisFilePropertyBean.AnalysisFileName;
            var rtTol = param.RetentionTimeAlignmentTolerance;
            var maxSimilarities = new double[masterMs1DecResults.Count];

            for (int i = 0; i < ms1DecResults.Count; i++)
            {
                var startIndex = DataAccessGcUtility.GetMS1DecResultListStartIndex(masterMs1DecResults, ms1DecResults[i].RetentionTime, rtTol, false);
                var maxMatchId = -1;
                var maxSimilarity = double.MinValue;

                for (int j = startIndex; j < masterMs1DecResults.Count; j++)
                {
                    if (ms1DecResults[i].RetentionTime - rtTol > masterMs1DecResults[j].RetentionTime) continue;
                    if (ms1DecResults[i].RetentionTime + rtTol < masterMs1DecResults[j].RetentionTime) break;

                    var similarity = getRtAndEiSimilarity(ms1DecResults[i], masterMs1DecResults[j], param);

                    if (maxSimilarity < similarity && maxSimilarities[j] < similarity) {
                        maxSimilarity = similarity;
                        maxMatchId = j;
                    }
                }

                if (maxMatchId == -1) continue;
                
                maxSimilarities[maxMatchId] = maxSimilarity;
                setAlignmentResult(alignmentSpots[maxMatchId].AlignedPeakPropertyBeanCollection[fileID], ms1DecResults[i], fileID, fileName, mspDB);
            }
        }

        private static void alignToMasterListByRetentionIndex(AnalysisFileBean file, List<MS1DecResult> ms1DecResults, List<MS1DecResult> masterMs1DecResults, AlignmentResultBean alignmentResult, AnalysisParamOfMsdialGcms param, List<MspFormatCompoundInformationBean> mspDB)
        {
            ms1DecResults = ms1DecResults.OrderBy(n => n.RetentionIndex).ThenBy(n => n.BasepeakMz).ToList();
            masterMs1DecResults = masterMs1DecResults.OrderBy(n => n.RetentionIndex).ThenBy(n => n.BasepeakMz).ToList();

            var alignmentSpots = alignmentResult.AlignmentPropertyBeanCollection;
            var fileID = file.AnalysisFilePropertyBean.AnalysisFileId;
            var fileName = file.AnalysisFilePropertyBean.AnalysisFileName;
            var riTol = param.RetentionIndexAlignmentTolerance;
            var maxSimilarities = new double[masterMs1DecResults.Count];

            for (int i = 0; i < ms1DecResults.Count; i++)
            {
                var startIndex = DataAccessGcUtility.GetMS1DecResultListStartIndex(masterMs1DecResults, ms1DecResults[i].RetentionIndex, riTol, true);
                var maxMatchId = -1;
                var maxSimilarity = double.MinValue;

                for (int j = startIndex; j < masterMs1DecResults.Count; j++)
                {
                    if (ms1DecResults[i].RetentionIndex - riTol > masterMs1DecResults[j].RetentionIndex) continue;
                    if (ms1DecResults[i].RetentionIndex + riTol < masterMs1DecResults[j].RetentionIndex) break;

                    var similarity = getRiAndEiSimilarity(ms1DecResults[i], masterMs1DecResults[j], param);

                    if (maxSimilarity < similarity && maxSimilarities[j] < similarity) {
                        maxSimilarity = similarity;
                        maxMatchId = j;
                    }
                }

                if (maxMatchId == -1) continue;

                maxSimilarities[maxMatchId] = maxSimilarity;
                setAlignmentResult(alignmentSpots[maxMatchId].AlignedPeakPropertyBeanCollection[fileID], ms1DecResults[i], fileID, fileName, mspDB);
            }
        }

        private static void filteringJointAligner(ProjectPropertyBean project, ObservableCollection<AnalysisFileBean> files, AnalysisParamOfMsdialGcms param, AlignmentResultBean alignmentResult)
        {
            var maxQcNumber = 0;
            foreach (var file in files) if (file.AnalysisFilePropertyBean.AnalysisFileType == AnalysisFileType.QC) maxQcNumber++;

            var masterGroupCountDict = getGroupCountDictionary(project, alignmentResult.AlignmentPropertyBeanCollection[0]);
            var alignmentSpots = alignmentResult.AlignmentPropertyBeanCollection;
         

            for (int i = 0; i < alignmentSpots.Count; i++)
            {
                var peakCount = 0; 
                var qcCount = 0;
                var sumRt = 0.0; 
                var sumRi = 0.0;
                var sumPeakWidth = 0.0;

                double minRt = double.MaxValue, maxRt = double.MinValue, minMz = double.MaxValue, maxMz = double.MinValue, minRi = double.MaxValue, maxRi = double.MinValue, maxIntensity = double.MinValue;
                double maxNoise = double.MinValue, minNoise = double.MaxValue, sumNoise = 0;
                double maxSN = double.MinValue, minSN = double.MaxValue, sumSN = 0;

                var spotProperties = alignmentSpots[i].AlignedPeakPropertyBeanCollection;
                var localGroupCountDict = new Dictionary<string, int>();
                foreach (var key in masterGroupCountDict.Keys) localGroupCountDict[key] = 0;

                for (int j = 0; j < spotProperties.Count; j++)
                {
                    if (spotProperties[j].PeakID < 0) continue;
                    if (files[spotProperties[j].FileID].AnalysisFilePropertyBean.AnalysisFileType == AnalysisFileType.QC) qcCount++;

                    sumRt += spotProperties[j].RetentionTime;
                    sumRi += spotProperties[j].RetentionIndex;
                    sumPeakWidth += spotProperties[j].PeakWidth;
                    sumSN += spotProperties[j].SignalToNoise;
                    sumNoise += spotProperties[j].EstimatedNoise;

                    if (minRt > spotProperties[j].RetentionTime) minRt = spotProperties[j].RetentionTime;
                    if (maxRt < spotProperties[j].RetentionTime) maxRt = spotProperties[j].RetentionTime;
                    if (minRi > spotProperties[j].RetentionIndex) minRi = spotProperties[j].RetentionIndex;
                    if (maxRi < spotProperties[j].RetentionIndex) maxRi = spotProperties[j].RetentionIndex;
                    if (minMz > spotProperties[j].AccurateMass) minMz = spotProperties[j].AccurateMass;
                    if (maxMz < spotProperties[j].AccurateMass) maxMz = spotProperties[j].AccurateMass;
                    if (maxIntensity < spotProperties[j].Variable) maxIntensity = spotProperties[j].Variable;
                    if (minSN > spotProperties[j].SignalToNoise) minSN = spotProperties[j].SignalToNoise;
                    if (maxSN < spotProperties[j].SignalToNoise) maxSN = spotProperties[j].SignalToNoise;
                    if (minNoise > spotProperties[j].EstimatedNoise) minNoise = spotProperties[j].EstimatedNoise;
                    if (maxNoise < spotProperties[j].EstimatedNoise) maxNoise = spotProperties[j].EstimatedNoise;

                    peakCount++;
                    var fileId = spotProperties[j].FileID;
                    var classID = project.FileID_ClassName[fileId];
                    var filetype = project.FileID_AnalysisFileType[fileId];
                    //if (filetype == AnalysisFileType.Sample)
                    localGroupCountDict[classID]++;
                }

                if (peakCount == 0)
                {
                    alignmentResult.AlignmentPropertyBeanCollection.RemoveAt(i);
                    i--;
                    continue;
                }

                if ((float)((float)peakCount / (float)alignmentResult.AlignmentPropertyBeanCollection[i].AlignedPeakPropertyBeanCollection.Count) * 100F < param.PeakCountFilter)
                {
                    alignmentResult.AlignmentPropertyBeanCollection.RemoveAt(i);
                    i--;
                    continue;
                }

                if (param.QcAtLeastFilter && maxQcNumber != qcCount)
                {
                    alignmentResult.AlignmentPropertyBeanCollection.RemoveAt(i);
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
                    alignmentResult.AlignmentPropertyBeanCollection.RemoveAt(i);
                    i--;
                    continue;
                }

                //if (peakCount == 0) {
                //    Debug.WriteLine("");
                //}

                alignmentResult.AlignmentPropertyBeanCollection[i].CentralRetentionTime = (float)(sumRt / peakCount);
                alignmentResult.AlignmentPropertyBeanCollection[i].CentralRetentionIndex = (float)(sumRi / peakCount);
                alignmentResult.AlignmentPropertyBeanCollection[i].AveragePeakWidth = (float)(sumPeakWidth / peakCount);
                alignmentResult.AlignmentPropertyBeanCollection[i].MaxRt = (float)maxRt;
                alignmentResult.AlignmentPropertyBeanCollection[i].MinRt = (float)minRt;
                alignmentResult.AlignmentPropertyBeanCollection[i].MaxRi = (float)maxRi;
                alignmentResult.AlignmentPropertyBeanCollection[i].MinRi = (float)minRi;
                alignmentResult.AlignmentPropertyBeanCollection[i].MaxMz = (float)maxMz;
                alignmentResult.AlignmentPropertyBeanCollection[i].MinMz = (float)minMz;
                alignmentResult.AlignmentPropertyBeanCollection[i].MaxValiable = (float)maxIntensity;

                alignmentResult.AlignmentPropertyBeanCollection[i].SignalToNoiseMax = (float)maxSN;
                alignmentResult.AlignmentPropertyBeanCollection[i].SignalToNoiseMin = (float)minSN;
                alignmentResult.AlignmentPropertyBeanCollection[i].SignalToNoiseAve = (float)(sumSN / peakCount);

                alignmentResult.AlignmentPropertyBeanCollection[i].EstimatedNoiseMax = (float)maxNoise;
                alignmentResult.AlignmentPropertyBeanCollection[i].EstimatedNoiseMin = (float)minNoise;
                alignmentResult.AlignmentPropertyBeanCollection[i].EstimatedNoiseAve = (float)(sumNoise / peakCount);

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

        public static void QuantmassUpdate(RdamPropertyBean rdamProperty, ObservableCollection<AnalysisFileBean> files,
          AnalysisParamOfMsdialGcms param,
          AlignmentResultBean alignmentResult, AlignmentFileBean alignmentFile,
          ProjectPropertyBean projectProp,
          List<MspFormatCompoundInformationBean> mspDB,
          BackgroundWorker bgWorker)
        {
            //aligned eic required fields---
            var alignedEics = new List<AlignedData>();
            var newIdList = new List<int>();
            string eicFilePath = alignmentFile.EicFilePath;
            int flag = 0;

            var dt = projectProp.ProjectDate;
            var dirname = Path.Combine(projectProp.ProjectFolderPath, dt.Month + "_" + dt.Day + "_" + dt.Hour + "_" + dt.Minute + "_" + dt.Second + "_tmpFolder");
            if (!System.IO.Directory.Exists(dirname))
                System.IO.Directory.CreateDirectory(dirname);
            //---

            var fiehnRiDictionary = MspFileParcer.GetFiehnFamesDictionary(); // this is only for Fiehn RI based peak alignment
            for (int i = 0; i < files.Count; i++) {
                var fileID = rdamProperty.RdamFilePath_RdamFileID[files[i].AnalysisFilePropertyBean.AnalysisFilePath];
                var measurementID = rdamProperty.RdamFileContentBeanCollection[fileID].FileID_MeasurementID[files[i].AnalysisFilePropertyBean.AnalysisFileId];
               // Debug.WriteLine(i + " is started");
                using (var rawDataAccess = new RawDataAccess(files[i].AnalysisFilePropertyBean.AnalysisFilePath, measurementID, false, false, true)) {
                    var spectrumList = DataAccessGcUtility.GetRdamSpectrumList(rawDataAccess);
                    var alignmentSpots = alignmentResult.AlignmentPropertyBeanCollection;
                    var ms1DecResults = DataStorageGcUtility.ReadMS1DecResults(files[i].AnalysisFilePropertyBean.DeconvolutionFilePath);


                    //fiehn ri coeff is needed to easily calculate their converted RT and RI
                    var fiehnRiCoeff = new FiehnRiCoefficient();
                    var revFiehnRiCoeff = new FiehnRiCoefficient();

                    //kovats ri coeff is needed to easily calculate their converted RT and RI
                    var carbonRtDict = new Dictionary<int, float>();
                    if (param.AlignmentIndexType == AlignmentIndexType.RI && param.FileIdRiInfoDictionary != null && param.FileIdRiInfoDictionary.Count > 0)
                        carbonRtDict = param.FileIdRiInfoDictionary[files[i].AnalysisFilePropertyBean.AnalysisFileId].RiDictionary;

                    if (param.AlignmentIndexType == AlignmentIndexType.RI && param.RiCompoundType == RiCompoundType.Fames) {
                        fiehnRiCoeff = FiehnRiCalculator.GetFiehnRiCoefficient(fiehnRiDictionary, param.FileIdRiInfoDictionary[fileID].RiDictionary);
                        revFiehnRiCoeff = FiehnRiCalculator.GetFiehnRiCoefficient(param.FileIdRiInfoDictionary[fileID].RiDictionary, fiehnRiDictionary);
                    }

                    //Debug.WriteLine("Peak is retrieved");

                    var alignedPeakSpotInfoList = new List<AlignedPeakSpotInfo>();
                    for (int j = 0; j < alignmentSpots.Count; j++) {
                        var alignmentSpot = alignmentSpots[j];
                        var peakProperty = alignmentSpots[j].AlignedPeakPropertyBeanCollection[i];
                        if (peakProperty.PeakID < 0) {
                            gapFilling(fileID, spectrumList, peakProperty, alignmentSpot,
                                param, fiehnRiCoeff, revFiehnRiCoeff);
                        }
                        else {
                            recalculationByQuantMass(fileID, peakProperty, ms1DecResults[peakProperty.PeakID],
                                spectrumList, alignmentSpot, param, fiehnRiCoeff, revFiehnRiCoeff);
                        }
                        var alignedPeakSpotInfo = new AlignedPeakSpotInfo();
                        if (flag == 0) {
                            var alignedData = getAlignedDataObj(alignmentSpot, param, carbonRtDict, fiehnRiCoeff, revFiehnRiCoeff);
                            alignedEics.Add(alignedData);
                        }
                        var peaklist = getRiCorrectedPeaklist(spectrumList, alignedEics[j], alignmentSpot, param, carbonRtDict, fiehnRiCoeff, revFiehnRiCoeff);
                        alignedPeakSpotInfo.PeakList = peaklist;

                        if (param.AlignmentIndexType == AlignmentIndexType.RI) {
                            alignedPeakSpotInfo.TargetRt = peakProperty.RetentionIndex;
                            alignedPeakSpotInfo.TargetLeftRt = peakProperty.RetentionIndexLeft;
                            alignedPeakSpotInfo.TargetRightRt = peakProperty.RetentionIndexRight;
                        }
                        else {
                            alignedPeakSpotInfo.TargetRt = peakProperty.RetentionTime;
                            alignedPeakSpotInfo.TargetLeftRt = peakProperty.RetentionTimeLeft;
                            alignedPeakSpotInfo.TargetRightRt = peakProperty.RetentionTimeRight;
                        }

                        if (peakProperty.PeakID <= -1) {
                            alignedPeakSpotInfo.GapFilled = true;
                        }
                        else {
                            alignedPeakSpotInfo.GapFilled = false;
                        }
                        alignedPeakSpotInfoList.Add(alignedPeakSpotInfo);
                    }
                    var filename = Path.Combine(dirname, "peaklist_" + i + ".pll");
                    AlignedEic.WritePeakList(alignedPeakSpotInfoList, projectProp, filename);

                   //Debug.WriteLine("Writing aligned EIC");
                }
                if (flag == 0) flag = 1;
                bgWorker.ReportProgress(i + 1);
            }

            FinalizeJointAligner(alignmentResult, files, param, projectProp, ref newIdList);

            AlignedEic.WriteAlignedEic(alignedEics, projectProp, newIdList, files.Count, dirname, eicFilePath);
            System.IO.Directory.Delete(dirname, true);
        }


        public static void QuantAndGapFilling(RdamPropertyBean rdamProperty, ObservableCollection<AnalysisFileBean> files, 
            AnalysisParamOfMsdialGcms param, 
            AlignmentResultBean alignmentResult, AlignmentFileBean alignmentFile,
            ProjectPropertyBean projectProp, 
            List<MspFormatCompoundInformationBean> mspDB,
            Action<int> reportAction)
        {
            // 
            var alignmentSpots = alignmentResult.AlignmentPropertyBeanCollection;
            setAlignmentProperties(alignmentSpots, param);

            definitionOfQuantMasses(alignmentResult, files, param, mspDB);

            //aligned eic required fields---
            var alignedEics = new List<AlignedData>();
            var newIdList = new List<int>();
            string eicFilePath = alignmentFile.EicFilePath;
            int flag = 0;

            var dt = projectProp.ProjectDate;
            var dirname = Path.Combine(projectProp.ProjectFolderPath, "project_" + dt.Year + "_" + dt.Month + "_" + dt.Day + "_" + dt.Hour + "_" + dt.Minute + "_" + dt.Second + "_tmpFolder");
            if (!System.IO.Directory.Exists(dirname))
                System.IO.Directory.CreateDirectory(dirname);
            //---

            var fiehnRiDictionary = MspFileParcer.GetFiehnFamesDictionary(); // this is only for Fiehn RI based peak alignment
            for (int i = 0; i < files.Count; i++)
            {
                var fileID = rdamProperty.RdamFilePath_RdamFileID[files[i].AnalysisFilePropertyBean.AnalysisFilePath];
                var measurementID = rdamProperty.RdamFileContentBeanCollection[fileID].FileID_MeasurementID[files[i].AnalysisFilePropertyBean.AnalysisFileId];
                //Debug.WriteLine(i);
                using (var rawDataAccess = new RawDataAccess(files[i].AnalysisFilePropertyBean.AnalysisFilePath, measurementID, false, false, true))
                {
                    var spectrumList = DataAccessGcUtility.GetRdamSpectrumList(rawDataAccess);
                    var ms1DecResults = DataStorageGcUtility.ReadMS1DecResults(files[i].AnalysisFilePropertyBean.DeconvolutionFilePath);

                    //fiehn ri coeff is needed to easily calculate their converted RT and RI
                    var fiehnRiCoeff = new FiehnRiCoefficient();
                    var revFiehnRiCoeff = new FiehnRiCoefficient();

                    //kovats ri coeff is needed to easily calculate their converted RT and RI
                    var carbonRtDict = new Dictionary<int, float>();
                    if (param.AlignmentIndexType == AlignmentIndexType.RI && param.FileIdRiInfoDictionary != null && param.FileIdRiInfoDictionary.Count > 0)
                        carbonRtDict = param.FileIdRiInfoDictionary[files[i].AnalysisFilePropertyBean.AnalysisFileId].RiDictionary; 

                    if (param.AlignmentIndexType == AlignmentIndexType.RI && param.RiCompoundType == RiCompoundType.Fames) {
                        fiehnRiCoeff = FiehnRiCalculator.GetFiehnRiCoefficient(fiehnRiDictionary, param.FileIdRiInfoDictionary[fileID].RiDictionary);
                        revFiehnRiCoeff = FiehnRiCalculator.GetFiehnRiCoefficient(param.FileIdRiInfoDictionary[fileID].RiDictionary, fiehnRiDictionary);
                    }
                    var alignedPeakSpotInfoList = new List<AlignedPeakSpotInfo>();
                    for (int j = 0; j < alignmentSpots.Count; j++)
                    {
                        //Debug.WriteLine(j);
                        //if (i == 35 && j == 358) {
                        //    Console.WriteLine();
                        //}

                        var alignmentSpot = alignmentSpots[j];
                        var peakProperty = alignmentSpots[j].AlignedPeakPropertyBeanCollection[i];
                        if (peakProperty.PeakID < 0)
                        {
                            gapFilling(fileID, spectrumList, peakProperty, alignmentSpot, 
                                param, fiehnRiCoeff, revFiehnRiCoeff);
                        }
                        else
                        {
                            recalculationByQuantMass(fileID, peakProperty, ms1DecResults[peakProperty.PeakID], 
                                spectrumList, alignmentSpot, param, fiehnRiCoeff, revFiehnRiCoeff);
                        }

                        var alignedPeakSpotInfo = new AlignedPeakSpotInfo();
                        if (flag == 0) {
                            var alignedData = getAlignedDataObj(alignmentSpot, param, carbonRtDict, fiehnRiCoeff, revFiehnRiCoeff);
                            alignedEics.Add(alignedData);
                        }
                        var peaklist = getRiCorrectedPeaklist(spectrumList, alignedEics[j], alignmentSpot, param, carbonRtDict, fiehnRiCoeff, revFiehnRiCoeff);
                        alignedPeakSpotInfo.PeakList = peaklist;

                        if (param.AlignmentIndexType == AlignmentIndexType.RI) {
                            alignedPeakSpotInfo.TargetRt = peakProperty.RetentionIndex;
                            alignedPeakSpotInfo.TargetLeftRt = peakProperty.RetentionIndexLeft;
                            alignedPeakSpotInfo.TargetRightRt = peakProperty.RetentionIndexRight;
                        }
                        else {
                            alignedPeakSpotInfo.TargetRt = peakProperty.RetentionTime;
                            alignedPeakSpotInfo.TargetLeftRt = peakProperty.RetentionTimeLeft;
                            alignedPeakSpotInfo.TargetRightRt = peakProperty.RetentionTimeRight;
                        }

                        if (peakProperty.PeakID <= -1) {
                            alignedPeakSpotInfo.GapFilled = true;
                        }
                        else {
                            alignedPeakSpotInfo.GapFilled = false;
                        }
                        alignedPeakSpotInfoList.Add(alignedPeakSpotInfo);

                    }
                    var filename = Path.Combine(dirname, "peaklist_" + i + ".pll");
                    AlignedEic.WritePeakList(alignedPeakSpotInfoList, projectProp, filename);
                }
                if (flag == 0) flag = 1;
                reportAction?.Invoke(i + 1);
            }

            FinalizeJointAligner(alignmentResult, files, param, projectProp, ref newIdList);

            AlignedEic.WriteAlignedEic(alignedEics, projectProp, newIdList, files.Count, dirname, eicFilePath);
            System.IO.Directory.Delete(dirname, true);
        }

        private static void setAlignmentProperties(ObservableCollection<AlignmentPropertyBean> alignmentSpots, AnalysisParamOfMsdialGcms param) {
            int fileIdOfMaxSimilarityScore = -1, fileIdOfMaxIntensity = -1;
            double minInt = double.MaxValue, maxInt = double.MinValue, minIntTotal = double.MaxValue, maxIntTotal = double.MinValue;

            for (int i = 0; i < alignmentSpots.Count; i++) {

                setBasicAlignmentProperties(alignmentSpots[i], i, param, out minInt, out maxInt, out fileIdOfMaxIntensity, out fileIdOfMaxSimilarityScore);
                setRepresentativeFileID(alignmentSpots[i], fileIdOfMaxSimilarityScore, fileIdOfMaxIntensity);
                setRepresentativeIdentificationResult(alignmentSpots[i]);

                if (maxIntTotal < maxInt) maxIntTotal = maxInt;
                if (minIntTotal > minInt) minIntTotal = minInt;
            }
        }

        private static void setAlignedPeakSpotInfo(AlignedPeakSpotInfo alignedPeakSpotInfo, MS1DecResult ms1DecResult, 
            AnalysisParamOfMsdialGcms param, Dictionary<int, float> carbonRtDict, 
            FiehnRiCoefficient fiehnRiCoeff, FiehnRiCoefficient revFiehnRiCoeff) {
            if (param.AlignmentIndexType == AlignmentIndexType.RT) {
                alignedPeakSpotInfo.TargetRt = ms1DecResult.RetentionTime;
                alignedPeakSpotInfo.TargetLeftRt 
                    = (float)ms1DecResult.BasepeakChromatogram[0].RetentionTime;
                alignedPeakSpotInfo.TargetRightRt 
                    = (float)ms1DecResult.BasepeakChromatogram[ms1DecResult.BasepeakChromatogram.Count - 1].RetentionTime;
            }
            else {
                if (param.RiCompoundType == RiCompoundType.Alkanes) {
                    alignedPeakSpotInfo.TargetRt = ms1DecResult.RetentionIndex;
                    alignedPeakSpotInfo.TargetLeftRt
                        = GcmsScoring.GetRetentionIndexByAlkanes(carbonRtDict, (float)ms1DecResult.BasepeakChromatogram[0].RetentionTime);
                    alignedPeakSpotInfo.TargetRightRt
                        = GcmsScoring.GetRetentionIndexByAlkanes(carbonRtDict, 
                        (float)ms1DecResult.BasepeakChromatogram[ms1DecResult.BasepeakChromatogram.Count - 1].RetentionTime);
                }
                else {
                    alignedPeakSpotInfo.TargetRt = ms1DecResult.RetentionIndex;
                    alignedPeakSpotInfo.TargetLeftRt
                        = FiehnRiCalculator.CalculateFiehnRi(fiehnRiCoeff, (float)ms1DecResult.BasepeakChromatogram[0].RetentionTime);
                    alignedPeakSpotInfo.TargetRightRt
                        = FiehnRiCalculator.CalculateFiehnRi(fiehnRiCoeff,
                        (float)ms1DecResult.BasepeakChromatogram[ms1DecResult.BasepeakChromatogram.Count - 1].RetentionTime);
                }
            }
        }

        private static List<double[]> getRiCorrectedPeaklist(List<RawSpectrum> spectrumList, 
            AlignedData alignedData, AlignmentPropertyBean alignmentProp, AnalysisParamOfMsdialGcms param,
            Dictionary<int, float> carbonRtDict, FiehnRiCoefficient fiehnRiCoeff, FiehnRiCoefficient revFiehnRiCoeff) {

            if (param.AlignmentIndexType == AlignmentIndexType.RT) {
                var peaklist = DataAccessGcUtility.GetBaselineCorrectedPeaklistByMassAccuracy(spectrumList, alignedData.Rt, alignedData.MinRt,
                    alignedData.MaxRt, alignmentProp.QuantMass, param);

                var peakArray = new List<double[]>();
                foreach (var peak in peaklist) {
                    peakArray.Add(new double[] { peak.ScanNumber, peak.RetentionTime, peak.Mz, peak.Intensity });
                }
                return peakArray;
            }
            else {
                var centralRi = alignedData.Rt;
                var minRi = alignedData.MinRt; //here,Rt in alignedData means retention index
                var maxRi = alignedData.MaxRt;
                var centralRt = -1.0F;
                var minRt = -1.0F;
                var maxRt = -1.0F;

                if (param.RiCompoundType == RiCompoundType.Alkanes) {
                    centralRt = GcmsScoring.ConvertKovatsRiToRetentiontime(carbonRtDict, centralRi);
                    minRt = GcmsScoring.ConvertKovatsRiToRetentiontime(carbonRtDict, minRi);
                    maxRt = GcmsScoring.ConvertKovatsRiToRetentiontime(carbonRtDict, maxRi);
                }
                else {
                    centralRt = FiehnRiCalculator.ConvertFiehnRiToRetentionTime(revFiehnRiCoeff, centralRi);
                    minRt = FiehnRiCalculator.ConvertFiehnRiToRetentionTime(revFiehnRiCoeff, minRi);
                    maxRt = FiehnRiCalculator.ConvertFiehnRiToRetentionTime(revFiehnRiCoeff, maxRi);
                }

                var peaklist = DataAccessGcUtility.GetBaselineCorrectedPeaklistByMassAccuracy(spectrumList, centralRt, minRt,
                    maxRt, alignmentProp.QuantMass, param);
                var peakArray = new List<double[]>();
                foreach (var peak in peaklist) {
                    var rt = peak.RetentionTime;
                    var ri = 0.0;
                    if (param.RiCompoundType == RiCompoundType.Alkanes) {
                        ri = GcmsScoring.GetRetentionIndexByAlkanes(carbonRtDict, (float)rt);
                    }
                    else {
                        ri = FiehnRiCalculator.CalculateFiehnRi(fiehnRiCoeff, (float)rt);
                    }
                    if (ri == double.NaN) {
                        //Console.WriteLine();
                    }
                    peakArray.Add(new double[] { peak.ScanNumber, ri, peak.Mz, peak.Intensity });
                }
                return peakArray;
            }
        }

        private static AlignedData getAlignedDataObj(AlignmentPropertyBean alignedSpotProp, 
            AnalysisParamOfMsdialGcms param,  
            Dictionary<int, float> carbonRtDict, FiehnRiCoefficient fiehnRiCoeff, FiehnRiCoefficient revFiehnRiCoeff) {

            var alignedData = new AlignedData() { Mz = alignedSpotProp.QuantMass };
            if (param.AlignmentIndexType == AlignmentIndexType.RT) {
                alignedData.Rt = alignedSpotProp.CentralRetentionTime;
                alignedData.MinRt = alignedSpotProp.CentralRetentionTime - param.RetentionTimeAlignmentTolerance * 2.0F;
                alignedData.MaxRt = alignedSpotProp.CentralRetentionTime + param.RetentionTimeAlignmentTolerance * 2.0F;
            }
            else {

                alignedData.Rt = alignedSpotProp.CentralRetentionIndex;
                alignedData.MinRt = alignedSpotProp.CentralRetentionIndex - param.RetentionIndexAlignmentTolerance * 2.0F;
                alignedData.MaxRt = alignedSpotProp.CentralRetentionIndex + param.RetentionIndexAlignmentTolerance * 2.0F;
            }

            return alignedData;
        }

        private static void recalculationByQuantMass(int fileID, AlignedPeakPropertyBean peakProperty, MS1DecResult ms1DecResult, 
            List<RawSpectrum> spectrumList, AlignmentPropertyBean spotProperty, AnalysisParamOfMsdialGcms param, FiehnRiCoefficient fiehnRiCoeff, FiehnRiCoefficient revFiehnRiCoeff)
        {
            //test code
            #region
            var centralRT = spotProperty.CentralRetentionTime;
            var centralRI = spotProperty.CentralRetentionIndex;

            var peakRt = peakProperty.RetentionTime;
            var peakRi = peakProperty.RetentionIndex;

            var maxRt = centralRT + param.RetentionTimeAlignmentTolerance;
            var maxRi = centralRI + param.RetentionIndexAlignmentTolerance;
            var minRt = centralRT - param.RetentionTimeAlignmentTolerance;
            var minRi = centralRI - param.RetentionIndexAlignmentTolerance;

            var isForceInserted = param.IsForceInsertForGapFilling;

            #region // RT conversion if needed
            if (param.AlignmentIndexType == AlignmentIndexType.RI) {
                var riDictionary = param.FileIdRiInfoDictionary[fileID].RiDictionary;
                if (param.RiCompoundType == RiCompoundType.Alkanes) {
                    centralRT = GcmsScoring.ConvertKovatsRiToRetentiontime(riDictionary, centralRI);
                    maxRt = GcmsScoring.ConvertKovatsRiToRetentiontime(riDictionary, maxRi);
                    minRt = GcmsScoring.ConvertKovatsRiToRetentiontime(riDictionary, minRi);

                    peakRt = GcmsScoring.ConvertKovatsRiToRetentiontime(riDictionary, peakRi);
                }
                else {
                    centralRT = FiehnRiCalculator.ConvertFiehnRiToRetentionTime(revFiehnRiCoeff, centralRI);
                    maxRt = FiehnRiCalculator.ConvertFiehnRiToRetentionTime(revFiehnRiCoeff, maxRi);
                    minRt = FiehnRiCalculator.ConvertFiehnRiToRetentionTime(revFiehnRiCoeff, minRi);

                    peakRt = FiehnRiCalculator.ConvertFiehnRiToRetentionTime(revFiehnRiCoeff, peakRi);
                }
            }
            #endregion

            if (maxRt < minRt) {
                maxRt = centralRT + 0.05F;
                minRt = centralRT - 0.05F;
            }
            var chromatogram = ms1DecResult.BasepeakChromatogram;
            var quantMass = spotProperty.QuantMass;
            var peakWidth = chromatogram[chromatogram.Count - 1].RetentionTime - chromatogram[0].RetentionTime;
            if (peakWidth < 0.02) peakWidth = 0.02;

            var rtTol = peakWidth;
            var chromRtTol = (maxRt - minRt) * 0.5F;

            var peaklist = DataAccessGcUtility.GetBaselineCorrectedPeaklistByMassAccuracy(
                spectrumList, centralRT, centralRT - chromRtTol * 2.0F, centralRT + chromRtTol * 2.0F, quantMass, param);
            var sPeaklist = DataAccessGcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);

            //finding local maximum list
            var candidates = new List<PeakAreaBean>();
            var minRtId = -1;
            var minRtDiff = double.MaxValue;
            if (sPeaklist.Count != 0) {
                for (int i = 1; i < sPeaklist.Count - 1; i++) {

                    if (sPeaklist[i].RetentionTime < peakRt - rtTol) continue;
                    if (peakRt + rtTol < sPeaklist[i].RetentionTime) break;

                    if ((sPeaklist[i - 1].Intensity <= sPeaklist[i].Intensity && sPeaklist[i].Intensity > sPeaklist[i + 1].Intensity) ||
                        (sPeaklist[i - 1].Intensity < sPeaklist[i].Intensity && sPeaklist[i].Intensity >= sPeaklist[i + 1].Intensity)) {

                        candidates.Add(new PeakAreaBean() {
                            ScanNumberAtPeakTop = i,
                            RtAtPeakTop = (float)sPeaklist[i].RetentionTime,
                            IntensityAtPeakTop = (float)sPeaklist[i].Intensity
                        });
                    }

                    var diff = Math.Abs(sPeaklist[i].RetentionTime - peakRt);
                    if (diff < minRtDiff) {
                        minRtDiff = diff;
                        minRtId = i;
                    }
                }
            }

            if (minRtId == -1) {
                minRtId = (int)(sPeaklist.Count * 0.5);
            }

            if (candidates.Count == 0) { // tempolary, +-5 data point region is inserted.
                var leftID = minRtId;
                var rightID = minRtId;
                if (isForceInserted == false) {
                    peakProperty.RetentionTime = (float)sPeaklist[minRtId].RetentionTime;
                    peakProperty.Variable = 0.0F;
                    peakProperty.Area = 0.0F;
                    peakProperty.RetentionTimeLeft = (float)sPeaklist[minRtId].RetentionTime;
                    peakProperty.RetentionTimeRight = (float)sPeaklist[minRtId].RetentionTime;
                   // peakProperty.PeakID = -2;
                    peakProperty.EstimatedNoise = spotProperty.EstimatedNoiseAve;
                    peakProperty.SignalToNoise = 0;
                }
                else {
                    var range = 5;

                    // checking left ID
                    leftID = minRtId - 1;
                    for (int i = minRtId - 1; i > minRtId - range; i--) {
                        leftID = i;
                        if (i - 1 < 0) {
                            leftID = 0;
                            break;
                        }
                        if (sPeaklist[i].Intensity < sPeaklist[i - 1].Intensity) {
                            break;
                        }
                    }

                    // checking right ID
                    rightID = minRtId + 1;
                    for (int i = minRtId + 1; i < minRtId + range; i++) {
                        rightID = i;
                        if (i + 1 > sPeaklist.Count - 1) {
                            rightID = sPeaklist.Count - 1;
                            break;
                        }
                        if (sPeaklist[i].Intensity < sPeaklist[i + 1].Intensity) {
                            break;
                        }
                    }

                    var peakAreaAboveZero = 0.0;
                    for (int i = leftID; i <= rightID - 1; i++) {
                        peakAreaAboveZero += (sPeaklist[i].Intensity + sPeaklist[i + 1].Intensity) * (sPeaklist[i + 1].RetentionTime - sPeaklist[i].RetentionTime) * 0.5;
                    }

                    var peakHeight = Math.Max(sPeaklist[minRtId].Intensity - sPeaklist[leftID].Intensity, sPeaklist[minRtId].Intensity - sPeaklist[rightID].Intensity);

                    peakProperty.RetentionTime = (float)sPeaklist[minRtId].RetentionTime;
                    peakProperty.Variable = (float)sPeaklist[minRtId].Intensity;
                    peakProperty.Area = (float)peakAreaAboveZero * 60;
                    peakProperty.RetentionTimeLeft = (float)sPeaklist[leftID].RetentionTime;
                    peakProperty.RetentionTimeRight = (float)sPeaklist[rightID].RetentionTime;
                   // peakProperty.PeakID = -2;
                    //peakProperty.SignalToNoise = (float)(peakHeight / spotProperty.EstimatedNoiseAve);
                    peakProperty.SignalToNoise = 0.0F;
                }

                if (param.AlignmentIndexType == AlignmentIndexType.RI) {
                    peakProperty.RetentionIndex = getRetentionIndex(fileID, param, (float)sPeaklist[minRtId].RetentionTime, fiehnRiCoeff);
                    peakProperty.RetentionIndexLeft = getRetentionIndex(fileID, param, (float)sPeaklist[leftID].RetentionTime, fiehnRiCoeff);
                    peakProperty.RetentionIndexRight = getRetentionIndex(fileID, param, (float)sPeaklist[rightID].RetentionTime, fiehnRiCoeff);
                }
            }
            else {

                // searching a representative local maximum. Now, the peak having nearest RT from central RT is selected.
                minRtId = -1;
                minRtDiff = double.MaxValue;

                for (int i = 0; i < candidates.Count; i++) {
                    var diff = Math.Abs(candidates[i].RtAtPeakTop - peakRt);
                    if (diff < minRtDiff) {
                        minRtDiff = diff;
                        minRtId = candidates[i].ScanNumberAtPeakTop;
                    }
                }

                // local minimum is searched from 5 point left from current local maximum.
                var localMinLeft = minRtId - 5;
                if (localMinLeft < 1) localMinLeft = 0;
                else {
                    for (int i = minRtId - 5; i >= 1; i--) {
                        localMinLeft = i;
                        if (sPeaklist[i].Intensity <= sPeaklist[i - 1].Intensity) {
                            break;
                        }
                    }
                }

                var localMinRight = minRtId + 5;
                if (localMinRight > sPeaklist.Count - 2) localMinRight = sPeaklist.Count - 1;
                else {
                    for (int i = minRtId + 5; i < sPeaklist.Count - 1; i++) {
                        localMinRight = i;
                        if (sPeaklist[i].Intensity <= sPeaklist[i + 1].Intensity) {
                            break;
                        }
                    }
                }

                var maxIntensity = 0.0;
                var maxID = minRtId;
                for (int i = localMinLeft + 1; i <= localMinRight - 1; i++) {
                    if ((sPeaklist[i - 1].Intensity <= sPeaklist[i].Intensity && sPeaklist[i].Intensity > sPeaklist[i + 1].Intensity) ||
                       (sPeaklist[i - 1].Intensity < sPeaklist[i].Intensity && sPeaklist[i].Intensity >= sPeaklist[i + 1].Intensity)) {
                        if (maxIntensity < sPeaklist[i].Intensity) {
                            maxIntensity = sPeaklist[i].Intensity;
                            maxID = i;
                        }
                    }
                }

                //calculating peak area
                var peakAreaAboveZero = 0.0;
                for (int i = localMinLeft; i <= localMinRight - 1; i++) {
                    peakAreaAboveZero += (sPeaklist[i].Intensity + sPeaklist[i + 1].Intensity) * (sPeaklist[i + 1].RetentionTime - sPeaklist[i].RetentionTime) * 0.5;
                }

                var peakHeight = Math.Max(sPeaklist[maxID].Intensity - sPeaklist[localMinLeft].Intensity, 
                    sPeaklist[maxID].Intensity - sPeaklist[localMinRight].Intensity);

                peakProperty.RetentionTime = (float)sPeaklist[maxID].RetentionTime;
                peakProperty.Variable = (float)maxIntensity;
                peakProperty.Area = (float)peakAreaAboveZero * 60;
                peakProperty.RetentionTimeLeft = (float)sPeaklist[localMinLeft].RetentionTime;
                peakProperty.RetentionTimeRight = (float)sPeaklist[localMinRight].RetentionTime;
                peakProperty.SignalToNoise = (float)(peakHeight / peakProperty.EstimatedNoise);

                if (param.AlignmentIndexType == AlignmentIndexType.RI) {
                    peakProperty.RetentionIndex = getRetentionIndex(fileID, param, (float)sPeaklist[maxID].RetentionTime, fiehnRiCoeff);
                    peakProperty.RetentionIndexLeft = getRetentionIndex(fileID, param, (float)sPeaklist[localMinLeft].RetentionTime, fiehnRiCoeff);
                    peakProperty.RetentionIndexRight = getRetentionIndex(fileID, param, (float)sPeaklist[localMinRight].RetentionTime, fiehnRiCoeff);
                }
            }
            #endregion
        }

        private static void gapFilling(int fileID, List<RawSpectrum> spectrumList, AlignedPeakPropertyBean alignedPeakProperty, 
            AlignmentPropertyBean alignmentSpot, AnalysisParamOfMsdialGcms param, FiehnRiCoefficient fiehnRiCoeff, FiehnRiCoefficient revFiehnRiCoeff)
        {
            gapfillingVS1(fileID, spectrumList, alignedPeakProperty, alignmentSpot, param, fiehnRiCoeff, revFiehnRiCoeff);
        }

        private static void gapfillingVS1(int fileID, List<RawSpectrum> spectrumList, AlignedPeakPropertyBean alignedPeakProperty, 
            AlignmentPropertyBean alignmentSpot, AnalysisParamOfMsdialGcms param, FiehnRiCoefficient fiehnRiCoeff, FiehnRiCoefficient revFiehnRiCoeff) {
            
            var centralRT = alignmentSpot.CentralRetentionTime;
            var centralRI = alignmentSpot.CentralRetentionIndex;
            var maxRt = centralRT + param.RetentionTimeAlignmentTolerance;
            var maxRi = centralRI + param.RetentionIndexAlignmentTolerance;
            var minRt = centralRT - param.RetentionTimeAlignmentTolerance;
            var minRi = centralRI - param.RetentionIndexAlignmentTolerance;

            var quantMass = alignmentSpot.QuantMass;

            #region // RT conversion if needed
            if (param.AlignmentIndexType == AlignmentIndexType.RI) {
                var riDictionary = param.FileIdRiInfoDictionary[fileID].RiDictionary;
                if (param.RiCompoundType == RiCompoundType.Alkanes) {
                    centralRT = GcmsScoring.ConvertKovatsRiToRetentiontime(riDictionary, centralRI);
                    maxRt = GcmsScoring.ConvertKovatsRiToRetentiontime(riDictionary, maxRi);
                    minRt = GcmsScoring.ConvertKovatsRiToRetentiontime(riDictionary, minRi);
                }
                else {
                    centralRT = FiehnRiCalculator.ConvertFiehnRiToRetentionTime(revFiehnRiCoeff, centralRI);
                    maxRt = FiehnRiCalculator.ConvertFiehnRiToRetentionTime(revFiehnRiCoeff, maxRi);
                    minRt = FiehnRiCalculator.ConvertFiehnRiToRetentionTime(revFiehnRiCoeff, minRi);
                }
            }
            #endregion

            var rtTol = (maxRt - minRt) * 0.5F;

            var peaklist = DataAccessGcUtility.GetBaselineCorrectedPeaklistByMassAccuracy(
                spectrumList, centralRT,
                centralRT - rtTol * 3.0F,
                centralRT + rtTol * 3.0F, quantMass, param);
            var sPeaklist = DataAccessGcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
            var ssPeaklist = DataAccessGcUtility.GetSmoothedPeaklist(sPeaklist, param.SmoothingMethod, param.SmoothingLevel);
            var sssPeaklist = DataAccessGcUtility.GetSmoothedPeaklist(ssPeaklist, param.SmoothingMethod, param.SmoothingLevel);
            var isForceInsert = param.IsForceInsertForGapFilling;

            alignedPeakProperty.RetentionTime = centralRT;
            alignedPeakProperty.RetentionTimeLeft = centralRT;
            alignedPeakProperty.RetentionTimeRight = centralRT;
            alignedPeakProperty.RetentionIndex = centralRI;
            alignedPeakProperty.RetentionIndexLeft = centralRI;
            alignedPeakProperty.RetentionIndexRight = centralRI;
            alignedPeakProperty.QuantMass = quantMass;
            alignedPeakProperty.Variable = 0;
            alignedPeakProperty.Area = 0;
            alignedPeakProperty.PeakID = -2;
            alignedPeakProperty.EstimatedNoise = alignmentSpot.EstimatedNoiseAve;

            if (sPeaklist == null || sPeaklist.Count == 0) return;

            //finding local maximum list
            var candidates = new List<PeakAreaBean>();
            var minRtId = -1;
            var minRtDiff = double.MaxValue;
            if (sPeaklist.Count != 0) {
                for (int i = 2; i < sPeaklist.Count - 2; i++) {

                    if (sPeaklist[i].RetentionTime < centralRT - rtTol) continue;
                    if (centralRT + rtTol < sPeaklist[i].RetentionTime) break;

                    if ((sssPeaklist[i - 2].Intensity <= sssPeaklist[i - 1].Intensity &&
                        sssPeaklist[i - 1].Intensity <= sssPeaklist[i].Intensity &&
                        sssPeaklist[i].Intensity > sssPeaklist[i + 1].Intensity) ||
                        (sssPeaklist[i - 1].Intensity < sssPeaklist[i].Intensity &&
                        sssPeaklist[i].Intensity >= sssPeaklist[i + 1].Intensity &&
                        sssPeaklist[i + 1].Intensity >= sssPeaklist[i + 2].Intensity)) {

                        candidates.Add(new PeakAreaBean() {
                            ScanNumberAtPeakTop = i,
                            RtAtPeakTop = (float)sssPeaklist[i].RetentionTime,
                            IntensityAtPeakTop = (float)sssPeaklist[i].Intensity
                        });
                    }

                    var diff = Math.Abs(sssPeaklist[i].RetentionTime - centralRT);
                    if (diff < minRtDiff) {
                        minRtDiff = diff;
                        minRtId = i;
                    }
                }
            }
            if (minRtId == -1) {
                minRtId = (int)(sPeaklist.Count * 0.5);
            }

            if (candidates.Count == 0) { // tempolary, +-5 data point region is inserted.

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
                        if (sssPeaklist[i].Intensity < sssPeaklist[i - 1].Intensity) {
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
                        if (sssPeaklist[i].Intensity < sssPeaklist[i + 1].Intensity) {
                            break;
                        }
                    }

                    var peakAreaAboveZero = 0.0;
                    for (int i = leftID; i <= rightID - 1; i++) {
                        peakAreaAboveZero += (sPeaklist[i].Intensity + sPeaklist[i + 1].Intensity) * (sPeaklist[i + 1].RetentionTime - sPeaklist[i].RetentionTime) * 0.5;
                    }
                    var peakHeightFromBaseline = Math.Max(sPeaklist[minRtId].Intensity - sPeaklist[leftID].Intensity,
                        sPeaklist[minRtId].Intensity - sPeaklist[rightID].Intensity);

                    alignedPeakProperty.RetentionTime = (float)sPeaklist[minRtId].RetentionTime;
                    alignedPeakProperty.Variable = (float)sPeaklist[minRtId].Intensity;
                    alignedPeakProperty.Area = (float)peakAreaAboveZero * 60;
                    alignedPeakProperty.RetentionTimeLeft = (float)sPeaklist[leftID].RetentionTime;
                    alignedPeakProperty.RetentionTimeRight = (float)sPeaklist[rightID].RetentionTime;
                    alignedPeakProperty.SignalToNoise = 0.0F;

                    if (param.AlignmentIndexType == AlignmentIndexType.RI) {
                        alignedPeakProperty.RetentionIndex = getRetentionIndex(fileID, param, (float)sPeaklist[minRtId].RetentionTime, fiehnRiCoeff);
                        alignedPeakProperty.RetentionIndexLeft = getRetentionIndex(fileID, param, (float)sPeaklist[leftID].RetentionTime, fiehnRiCoeff);
                        alignedPeakProperty.RetentionIndexRight = getRetentionIndex(fileID, param, (float)sPeaklist[rightID].RetentionTime, fiehnRiCoeff);
                    }
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
                        if (sssPeaklist[i].Intensity <= sssPeaklist[i - 1].Intensity) {
                            break;
                        }
                    }
                }

                var localMinRight = minRtId + margin;
                if (localMinRight > sPeaklist.Count - 2) localMinRight = sPeaklist.Count - 1;
                else {
                    for (int i = minRtId + margin; i < sPeaklist.Count - 1; i++) {
                        localMinRight = i;
                        if (sssPeaklist[i].Intensity <= sssPeaklist[i + 1].Intensity) {
                            break;
                        }
                    }
                }

                if (isForceInsert == false && (minRtId - localMinLeft < 2 || localMinRight - minRtId < 2)) return;

                var maxIntensity = 0.0;
                var maxID = minRtId;
                for (int i = localMinLeft + 1; i <= localMinRight - 1; i++) {
                    if ((sPeaklist[i - 1].Intensity <= sPeaklist[i].Intensity && sPeaklist[i].Intensity > sPeaklist[i + 1].Intensity) ||
                       (sPeaklist[i - 1].Intensity < sPeaklist[i].Intensity && sPeaklist[i].Intensity >= sPeaklist[i + 1].Intensity)) {
                        if (maxIntensity < sPeaklist[i].Intensity) {
                            maxIntensity = sPeaklist[i].Intensity;
                            maxID = i;
                        }
                    }
                }
                //calculating peak area
                var peakAreaAboveZero = 0.0;
                for (int i = localMinLeft; i <= localMinRight - 1; i++) {
                    peakAreaAboveZero += (sPeaklist[i].Intensity + sPeaklist[i + 1].Intensity) * (sPeaklist[i + 1].RetentionTime - sPeaklist[i].RetentionTime) * 0.5;
                }

                var peakHeightFromBaseline = Math.Max(sPeaklist[maxID].Intensity - sPeaklist[localMinLeft].Intensity,
                        sPeaklist[maxID].Intensity - sPeaklist[localMinRight].Intensity);

                alignedPeakProperty.RetentionTime = (float)sPeaklist[maxID].RetentionTime;
                alignedPeakProperty.Variable = (float)maxIntensity;
                alignedPeakProperty.Area = (float)peakAreaAboveZero * 60;
                alignedPeakProperty.RetentionTimeLeft = (float)sPeaklist[localMinLeft].RetentionTime;
                alignedPeakProperty.RetentionTimeRight = (float)sPeaklist[localMinRight].RetentionTime;
                alignedPeakProperty.SignalToNoise = (float)(peakHeightFromBaseline / alignedPeakProperty.EstimatedNoise);

                if (param.AlignmentIndexType == AlignmentIndexType.RI) {
                    alignedPeakProperty.RetentionIndex = getRetentionIndex(fileID, param, (float)sPeaklist[maxID].RetentionTime, fiehnRiCoeff);
                    alignedPeakProperty.RetentionIndexLeft = getRetentionIndex(fileID, param, (float)sPeaklist[localMinLeft].RetentionTime, fiehnRiCoeff);
                    alignedPeakProperty.RetentionIndexRight = getRetentionIndex(fileID, param, (float)sPeaklist[localMinRight].RetentionTime, fiehnRiCoeff);
                }
            }
        }

        private static float getRetentionIndex(int fileID, AnalysisParamOfMsdialGcms param, float rt, FiehnRiCoefficient fiehnRiCoeff)
        {
            if (param.AlignmentIndexType == AlignmentIndexType.RT) return -1;

            var carbonRtDict = param.FileIdRiInfoDictionary[fileID].RiDictionary;
            if (param.RiCompoundType == RiCompoundType.Alkanes) {
                return GcmsScoring.GetRetentionIndexByAlkanes(carbonRtDict, rt);
            }
            else {
                return (float)Math.Round(FiehnRiCalculator.CalculateFiehnRi(fiehnRiCoeff, rt), 1);
            }
        }

        private static void definitionOfQuantMasses(AlignmentResultBean alignmentResult, ObservableCollection<AnalysisFileBean> files, AnalysisParamOfMsdialGcms param, List<MspFormatCompoundInformationBean> mspDB)
        {
            var isReplaceMode = false;
            if (param.IsReplaceQuantmassByUserDefinedValue == true && mspDB != null && mspDB.Count > 0)
                isReplaceMode = true;
                
            var alignmentSpots = alignmentResult.AlignmentPropertyBeanCollection;
            var bin = 2; if (param.AccuracyType == AccuracyType.IsNominal) bin = 0;
            foreach (var spot in alignmentSpots)
            {
                //Console.WriteLine("Spot ID {0}", spot.AlignmentID);
                var isDetermined = false;
                if (spot.LibraryID >= 0 && spot.LibraryID < mspDB.Count && isReplaceMode == true) {
                    var quantmass = mspDB[spot.LibraryID].QuantMass;
                    if (quantmass >= param.MassRangeBegin && quantmass <= param.MassRangeEnd) {
                        spot.QuantMass = quantmass;
                        isDetermined = true;
                    }
                }
                if (isDetermined == true) continue;

                var fileID = spot.RepresentativeFileID;
                var ms1DecSeekpoint = spot.AlignedPeakPropertyBeanCollection[fileID].SeekPoint;
                var filePath = files[fileID].AnalysisFilePropertyBean.DeconvolutionFilePath;
                var ms1DecResult = DataStorageGcUtility.ReadMS1DecResult(filePath, ms1DecSeekpoint);
                var spectrum = ms1DecResult.Spectrum;

                var quantMassDict = new Dictionary<float, List<float>>();
                var spotIntensityMax = spot.MaxValiable;
                var repQuantMass = (float)Math.Round(spot.AlignedPeakPropertyBeanCollection[fileID].QuantMass, bin);
                foreach (var peakProp in spot.AlignedPeakPropertyBeanCollection)
                {
                    if (peakProp.PeakID < 0) continue;
                    if (peakProp.Variable < spotIntensityMax * 0.1) continue;
                    
                    var quantMass = (float)Math.Round(peakProp.QuantMass, bin);
                    if (!quantMassDict.Keys.Contains(quantMass))
                        quantMassDict[quantMass] = new List<float>() { peakProp.QuantMass };
                    else
                        quantMassDict[quantMass].Add(peakProp.QuantMass);
                }

                var maxQuant = 0.0F; var maxCount = 0;
                foreach (var dict in quantMassDict)
                    if (dict.Value.Count > maxCount) { maxCount = dict.Value.Count; maxQuant = dict.Key; }

                var quantMassCandidate = quantMassDict[maxQuant].Average();
                var basepeakMz = 0.0;
                var basepeakIntensity = 0.0;
                var isQuantMassExist = isQuantMassExistInSpectrum(quantMassCandidate, spectrum, param.MassAccuracy, 10.0F, out basepeakMz, out basepeakIntensity);

                //if (Math.Abs(basepeakMz - 489.2) < 0.25) {
                //    Console.WriteLine();
                //}
                if (param.IsRepresentativeQuantMassBasedOnBasePeakMz) {
                   // Console.WriteLine("Spot ID {0} and Quant mass {1}", spot.AlignmentID, basepeakMz);

                    spot.QuantMass = (float)basepeakMz;
                    continue;
                }

                if (isQuantMassExist) {
                    spot.QuantMass = quantMassCandidate;
                }
                else {

                    var isSuitableQuantMassExist = false;
                    foreach (var peak in spectrum) {
                        if (peak.Mz < repQuantMass - bin) continue;
                        if (peak.Mz > repQuantMass + bin) break;
                        var diff = Math.Abs(peak.Mz - repQuantMass);
                        if (diff <= bin && peak.Intensity > basepeakIntensity * 10.0 * 0.01) {
                            isSuitableQuantMassExist = true;
                            break;
                        }
                    }

                    if (isSuitableQuantMassExist)
                        spot.QuantMass = repQuantMass;
                    else
                        spot.QuantMass = (float)basepeakMz;
                }
            }
        }

        // spectrum should be ordered by m/z value
        private static bool isQuantMassExistInSpectrum(float quantMass, List<Peak> spectrum, float bin, float threshold, 
            out double basepeakMz, out double basepeakIntensity) {

            basepeakMz = 0.0;
            basepeakIntensity = 0.0;
            foreach (var peak in spectrum) {
                if (peak.Intensity > basepeakIntensity) {
                    basepeakIntensity = peak.Intensity;
                    basepeakMz = peak.Mz;
                }
            }

            var maxIntensity = basepeakIntensity;
            foreach (var peak in spectrum) {
                if (peak.Mz < quantMass - bin) continue;
                if (peak.Mz > quantMass + bin) break;
                var diff = Math.Abs(peak.Mz - quantMass);
                if (diff <= bin && peak.Intensity > maxIntensity * threshold * 0.01) {
                    return true;
                }
            }
            return false;
        }

        //private static List<Peak> getTrimedAndSmoothedPeaklist(List<RAW_Spectrum> spectrumList, float centralRT, 
        //    float quantMass, AnalysisParamOfMsdialGcms param, float rtTol)
        //{
        //    var peaklist = new List<Peak>();
        //    if (rtTol < 0.025) rtTol = 0.025F;

        //    var scanPolarity = param.IonMode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;
        //    var sliceWidth = param.MassSliceWidth;

        //    for (int i = 0; i < spectrumList.Count; i++)
        //    {
        //        var spectrum = spectrumList[i];

        //        if (spectrum.MsLevel > 1) continue;
        //        if (spectrum.ScanPolarity != scanPolarity) continue;
        //        if (spectrum.ScanStartTime < centralRT - rtTol) continue;
        //        if (spectrum.ScanStartTime > centralRT + rtTol) break;

        //        var massSpectra = spectrum.Spectrum;

        //        var sum = 0.0;
        //        var maxIntensityMz = double.MinValue;
        //        var maxMass = quantMass;
        //        var startIndex = DataAccessGcUtility.GetMs1StartIndex(quantMass, sliceWidth, massSpectra);

        //        for (int j = startIndex; j < massSpectra.Length; j++)
        //        {
        //            if (massSpectra[j].Mz < quantMass - sliceWidth) continue;
        //            else if (quantMass - sliceWidth <= massSpectra[j].Mz && massSpectra[j].Mz < quantMass + sliceWidth) { sum += massSpectra[j].Intensity; if (maxIntensityMz < massSpectra[j].Intensity) { maxIntensityMz = massSpectra[j].Intensity; maxMass = (float)massSpectra[j].Mz; } }
        //            else if (massSpectra[j].Mz >= quantMass + sliceWidth) break;
        //        }
        //        peaklist.Add(new Peak() { ScanNumber = spectrum.ScanNumber, RetentionTime = spectrum.ScanStartTime, Mz = maxMass, Intensity = sum });
        //    }

        //    var smoothedPeaklist = DataAccessGcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
        //    var minLeftIntensity = double.MaxValue;
        //    var minRightIntensity = double.MaxValue;
        //    var minLeftID = 0;
        //    var minRightID = smoothedPeaklist.Count - 1;

        //    //searching local left minimum
        //    for (int i = 0; i < smoothedPeaklist.Count - 1; i++) {
        //        var peak = smoothedPeaklist[i];
        //        if (peak.RetentionTime >= centralRT) break;
        //        if (peak.Intensity < minLeftIntensity) {
        //            minLeftIntensity = peak.Intensity;
        //            minLeftID = i;
        //        }
        //    }

        //    //searching local right minimum
        //    for (int i = smoothedPeaklist.Count - 1; i >= 0; i--) {
        //        var peak = smoothedPeaklist[i];
        //        if (peak.RetentionTime <= centralRT) break;
        //        if (peak.Intensity < minRightIntensity) {
        //            minRightIntensity = peak.Intensity;
        //            minRightID = i;
        //        }
        //    }

        //    var baselineCorrectedPeaklist = getBaselineCorrectedPeaklist(smoothedPeaklist, minLeftID, minRightID);

        //    return baselineCorrectedPeaklist;
        //}

        //private static List<Peak> getBaselineCorrectedPeaklist(List<Peak> peaklist, int minLeftID, int minRightID)
        //{
        //    var baselineCorrectedPeaklist = new List<Peak>();
        //    if (peaklist == null || peaklist.Count == 0) return baselineCorrectedPeaklist;

        //    double coeff = (peaklist[minRightID].Intensity - peaklist[minLeftID].Intensity) / 
        //        (peaklist[minRightID].RetentionTime - peaklist[minLeftID].RetentionTime);
        //    double intercept = (peaklist[minRightID].RetentionTime * peaklist[minLeftID].Intensity - 
        //        peaklist[minLeftID].RetentionTime * peaklist[minRightID].Intensity) / 
        //        (peaklist[minRightID].RetentionTime - peaklist[minLeftID].RetentionTime);
        //    double correctedIntensity = 0;
        //    for (int i = 0; i < peaklist.Count; i++) {
        //        correctedIntensity = peaklist[i].Intensity - (int)(coeff * peaklist[i].RetentionTime + intercept);
        //        if (correctedIntensity >= 0)
        //            baselineCorrectedPeaklist.Add(
        //                new Peak() {
        //                    ScanNumber = peaklist[i].ScanNumber, RetentionTime = peaklist[i].RetentionTime, Mz = peaklist[i].Mz, Intensity = correctedIntensity
        //                });
        //        else
        //            baselineCorrectedPeaklist.Add(
        //                new Peak() {
        //                    ScanNumber = peaklist[i].ScanNumber, RetentionTime = peaklist[i].RetentionTime, Mz = peaklist[i].Mz, Intensity = 0
        //                });
        //    }

        //    return baselineCorrectedPeaklist;


        //    //double coeff = (peaklist[peaklist.Count - 1].Intensity - peaklist[0].Intensity) / (peaklist[peaklist.Count - 1].RetentionTime - peaklist[0].RetentionTime);
        //    //double intercept = (peaklist[peaklist.Count - 1].RetentionTime * peaklist[0].Intensity - peaklist[0].RetentionTime * peaklist[peaklist.Count - 1].Intensity) / (peaklist[peaklist.Count - 1].RetentionTime - peaklist[0].RetentionTime);
        //    //double correctedIntensity = 0;
        //    //for (int i = 0; i < peaklist.Count; i++)
        //    //{
        //    //    correctedIntensity = peaklist[i].Intensity - (int)(coeff * peaklist[i].RetentionTime + intercept);
        //    //    if (correctedIntensity >= 0)
        //    //        baselineCorrectedPeaklist.Add(new Peak() { ScanNumber = peaklist[i].ScanNumber, RetentionTime = peaklist[i].RetentionTime, Mz = peaklist[i].Mz, Intensity = correctedIntensity });
        //    //    else
        //    //        baselineCorrectedPeaklist.Add(new Peak() { ScanNumber = peaklist[i].ScanNumber, RetentionTime = peaklist[i].RetentionTime, Mz = peaklist[i].Mz, Intensity = 0 }); ;
        //    //}

        //    //return baselineCorrectedPeaklist;
        //}

        private static void setAlignmentResult(AlignedPeakPropertyBean alignmentProperty, 
            MS1DecResult ms1DecResult, int fileID, string fileName, 
            List<MspFormatCompoundInformationBean> mspDB)
        {
            alignmentProperty.MetaboliteName = getCompoundName(ms1DecResult.MspDbID, mspDB);
            alignmentProperty.QuantMass = ms1DecResult.BasepeakMz;
            alignmentProperty.FileID = fileID;
            alignmentProperty.FileName = fileName;
            alignmentProperty.PeakID = ms1DecResult.Ms1DecID;
            alignmentProperty.Ms1ScanNumber = ms1DecResult.ScanNumber;
            alignmentProperty.RetentionTime = ms1DecResult.RetentionTime;
            alignmentProperty.RetentionIndex = ms1DecResult.RetentionIndex;
            alignmentProperty.PeakWidth = (float)(ms1DecResult.BasepeakChromatogram[ms1DecResult.BasepeakChromatogram.Count - 1].RetentionTime - ms1DecResult.BasepeakChromatogram[0].RetentionTime);
            alignmentProperty.SeekPoint = ms1DecResult.SeekPoint;
            alignmentProperty.Variable = ms1DecResult.BasepeakHeight;
            alignmentProperty.Area = ms1DecResult.BasepeakArea;
            alignmentProperty.LibraryID = ms1DecResult.MspDbID;
            alignmentProperty.MassSpectraSimilarity = ms1DecResult.DotProduct;
            alignmentProperty.ReverseSimilarity = ms1DecResult.ReverseDotProduct;
            alignmentProperty.FragmentPresencePercentage = ms1DecResult.PresencePersentage;
            alignmentProperty.EiSpectrumSimilarity = ms1DecResult.EiSpectrumSimilarity;
            alignmentProperty.RetentionTimeSimilarity = ms1DecResult.RetentionTimeSimilarity;
            alignmentProperty.RetentionIndexSimilarity = ms1DecResult.RetentionIndexSimilarity;
            alignmentProperty.TotalSimilairty = ms1DecResult.TotalSimilarity;
            alignmentProperty.EstimatedNoise = ms1DecResult.EstimatedNoise;
            alignmentProperty.SignalToNoise = ms1DecResult.SignalNoiseRatio;
        }

        private static string getCompoundName(int id, List<MspFormatCompoundInformationBean> mspDB)
        {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return string.Empty;
            else return mspDB[id].Name;
        }

        private static List<MS1DecResult> addJointAlignerMasterListByRetentionTime(List<MS1DecResult> masterMs1DecResults, List<MS1DecResult> ms1DecResults, AnalysisParamOfMsdialGcms param)
        {
            masterMs1DecResults = masterMs1DecResults.OrderBy(n => n.RetentionTime).ThenBy(n => n.BasepeakMz).ToList();
            ms1DecResults = ms1DecResults.OrderBy(n => n.RetentionTime).ThenBy(n => n.BasepeakMz).ToList();

            var addedMs1DecResults = new List<MS1DecResult>();
            var rtTol = param.RetentionTimeAlignmentTolerance;
            var maxIntensity = masterMs1DecResults.Max(n => n.BasepeakHeight);
            var maxSimilarities = new double[masterMs1DecResults.Count];

            for (int i = 0; i < ms1DecResults.Count; i++)
            {
                var alignChecker = false;
                var startIndex = DataAccessGcUtility.GetMS1DecResultListStartIndex(masterMs1DecResults, ms1DecResults[i].RetentionTime, rtTol, false);
                var maxSimilarity = double.MinValue;
                var maxMatchId = -1;

                for (int j = startIndex; j < masterMs1DecResults.Count; j++)
                {
                    if (ms1DecResults[i].RetentionTime - rtTol > masterMs1DecResults[j].RetentionTime) continue;
                    if (ms1DecResults[i].RetentionTime + rtTol < masterMs1DecResults[j].RetentionTime) break;

                    var similarity = getRtAndEiSimilarity(ms1DecResults[i], masterMs1DecResults[j], param);
                    if (similarity * 100 < 70) continue;
                    alignChecker = true;

                    if (ms1DecResults[i].MspDbID == masterMs1DecResults[j].MspDbID && ms1DecResults[i].MspDbID >= 0) {
                        maxSimilarity = similarity;
                        maxMatchId = j;
                        break;
                    }
                    else if (maxSimilarity < similarity && maxSimilarities[j] < similarity) {
                        maxSimilarity = similarity;
                        maxMatchId = j;
                    }
                }

                if (alignChecker == false && maxIntensity * 0.001 < ms1DecResults[i].BasepeakHeight)
                    addedMs1DecResults.Add(ms1DecResults[i]);
                else if (alignChecker == true && maxMatchId >= 0 &&
                    masterMs1DecResults[maxMatchId].BasepeakHeight < ms1DecResults[i].BasepeakHeight && ms1DecResults[i].MspDbID >= 0) {
                    maxSimilarities[maxMatchId] = maxSimilarity;
                    masterMs1DecResults.RemoveAt(maxMatchId);
                    masterMs1DecResults.Insert(maxMatchId, ms1DecResults[i]);
                }
            }

            if (addedMs1DecResults.Count == 0) return masterMs1DecResults;
            for (int i = 0; i < addedMs1DecResults.Count; i++) masterMs1DecResults.Add(addedMs1DecResults[i]);

            return masterMs1DecResults;
        }

        private static List<MS1DecResult> addJointAlignerMasterListByRetentionIndex(List<MS1DecResult> masterMs1DecResults, List<MS1DecResult> ms1DecResults, AnalysisParamOfMsdialGcms param)
        {
            masterMs1DecResults = masterMs1DecResults.OrderBy(n => n.RetentionIndex).ThenBy(n => n.BasepeakMz).ToList();
            ms1DecResults = ms1DecResults.OrderBy(n => n.RetentionIndex).ThenBy(n => n.BasepeakMz).ToList();

            var addedMs1DecResults = new List<MS1DecResult>();
            var riTol = param.RetentionIndexAlignmentTolerance;
            var maxIntensity = masterMs1DecResults.Max(n => n.BasepeakHeight);
            var maxSimilarities = new double[masterMs1DecResults.Count];

            for (int i = 0; i < ms1DecResults.Count; i++)
            {
                var alignChecker = false;
                var startIndex = DataAccessGcUtility.GetMS1DecResultListStartIndex(masterMs1DecResults, ms1DecResults[i].RetentionIndex, riTol, true);
                var maxSimilarity = double.MinValue;
                var maxMatchId = -1;

                for (int j = startIndex; j < masterMs1DecResults.Count; j++)
                {
                    if (ms1DecResults[i].RetentionIndex - riTol > masterMs1DecResults[j].RetentionIndex) continue;
                    if (ms1DecResults[i].RetentionIndex + riTol < masterMs1DecResults[j].RetentionIndex) break;

                    var similarity = getRiAndEiSimilarity(ms1DecResults[i], masterMs1DecResults[j], param);
                    if (similarity * 100 < 70) continue;
                    alignChecker = true;

                    if (ms1DecResults[i].MspDbID == masterMs1DecResults[j].MspDbID && ms1DecResults[i].MspDbID >= 0) {
                        maxSimilarity = similarity;
                        maxMatchId = j;
                        break;
                    }
                    else if (maxSimilarity < similarity && maxSimilarities[j] < similarity) {
                        maxSimilarity = similarity;
                        maxMatchId = j;
                    }
                }

                if (alignChecker == false && maxIntensity * 0.001 < ms1DecResults[i].BasepeakHeight)
                    addedMs1DecResults.Add(ms1DecResults[i]);
                else if (alignChecker == true && maxMatchId >= 0 &&
                    masterMs1DecResults[maxMatchId].BasepeakHeight < ms1DecResults[i].BasepeakHeight && ms1DecResults[i].MspDbID >= 0) {
                    maxSimilarities[maxMatchId] = maxSimilarity;
                    masterMs1DecResults.RemoveAt(maxMatchId);
                    masterMs1DecResults.Insert(maxMatchId, ms1DecResults[i]);
                }
            }

            if (addedMs1DecResults.Count == 0) return masterMs1DecResults;
            for (int i = 0; i < addedMs1DecResults.Count; i++) masterMs1DecResults.Add(addedMs1DecResults[i]);

            return masterMs1DecResults;
        }

        private static double getRtAndEiSimilarity(MS1DecResult ms1DecResultA, MS1DecResult ms1DecResultB, AnalysisParamOfMsdialGcms param)
        {
            var rtSimilarity = GcmsScoring.GetGaussianSimilarity(ms1DecResultA.RetentionTime, ms1DecResultB.RetentionTime, param.RetentionTimeAlignmentTolerance);
            var eiSimilarity = getEiSimilarity(ms1DecResultA.Spectrum, ms1DecResultB.Spectrum, param.MzLibrarySearchTolerance, param.MassRangeBegin, param.MassRangeEnd);
            return rtSimilarity * param.RetentionTimeAlignmentFactor + eiSimilarity * param.EiSimilarityAlignmentFactor;
        }

        private static double getRiAndEiSimilarity(MS1DecResult ms1DecResultA, MS1DecResult ms1DecResultB, AnalysisParamOfMsdialGcms param)
        {
            var riSimilarity = GcmsScoring.GetGaussianSimilarity(ms1DecResultA.RetentionIndex, ms1DecResultB.RetentionIndex, param.RetentionIndexAlignmentTolerance);
            var eiSimilarity = getEiSimilarity(ms1DecResultA.Spectrum, ms1DecResultB.Spectrum, param.MzLibrarySearchTolerance, param.MassRangeBegin, param.MassRangeEnd);
            return riSimilarity * param.RetentionTimeAlignmentFactor + eiSimilarity * param.EiSimilarityAlignmentFactor;
        }

        private static double getEiSimilarity(List<Peak> spectrumA, List<Peak> spectrumB, float bin, double userMinMass, double userMaxMass)
        {
            return GcmsScoring.GetEiSpectraSimilarity(spectrumA, spectrumB, bin, userMinMass, userMaxMass);
        }

        public static void FinalizeJointAligner(AlignmentResultBean alignmentResult,
            ObservableCollection<AnalysisFileBean> files, 
            AnalysisParamOfMsdialGcms param, ProjectPropertyBean projectProp, ref List<int> newIdList)
        {
            int fileIdOfMaxSimilarityScore = -1, fileIdOfMaxIntensity = -1;
            double minInt = double.MaxValue, maxInt = double.MinValue, minIntTotal = double.MaxValue, maxIntTotal = double.MinValue;

            var alignmentSpots = alignmentResult.AlignmentPropertyBeanCollection;

            //foreach (var spot in alignmentSpots) {
            //    Debug.WriteLine(spot.LibraryID);
            //}

            for (int i = 0; i < alignmentSpots.Count; i++)
            {
                //Debug.WriteLine(i);
                //if (i == 37) {
                //    Debug.WriteLine(i);
                //}
                setBasicAlignmentProperties(alignmentSpots[i], i, param, out minInt, out maxInt, out fileIdOfMaxIntensity, out fileIdOfMaxSimilarityScore);
                if (!param.IsRepresentativeQuantMassBasedOnBasePeakMz)
                    setRepresentativeFileID(alignmentSpots[i], fileIdOfMaxSimilarityScore, fileIdOfMaxIntensity);
                setRepresentativeIdentificationResult(alignmentSpots[i]);

                if (maxIntTotal < maxInt) maxIntTotal = maxInt;
                if (minIntTotal > minInt) minIntTotal = minInt;
            }

            alignmentSpots = getRefinedAlignmentPropertyBeanCollection(alignmentSpots, param, projectProp, ref newIdList);

            if (maxIntTotal > 1) maxIntTotal = Math.Log(maxIntTotal, 2); else maxIntTotal = 1;
            if (minIntTotal > 1) minIntTotal = Math.Log(minIntTotal, 2); else minIntTotal = 0;

            for (int i = 0; i < alignmentSpots.Count; i++) {


                var relativeValue = (float)((Math.Log((double)alignmentSpots[i].MaxValiable, 2) - minIntTotal)
                    / (maxIntTotal - minIntTotal));
               // Console.WriteLine(alignmentSpots[i].MaxValiable + "\t" + (maxIntTotal - minIntTotal) + "\t" + relativeValue);
                if (relativeValue < 0)
                    relativeValue = 0;
                else if (relativeValue > 1)
                    relativeValue = 1;
                alignmentSpots[i].RelativeAmplitudeValue = relativeValue;
            }

            alignmentResult.AlignmentPropertyBeanCollection = alignmentSpots;
            alignmentResult.AlignmentIdNumber = alignmentSpots.Count;
        }

        private static void setRepresentativeIdentificationResult(AlignmentPropertyBean alignmentProperty)
        {
            var fileID = alignmentProperty.RepresentativeFileID;
            var peakProperties = alignmentProperty.AlignedPeakPropertyBeanCollection;

            alignmentProperty.LibraryID = peakProperties[fileID].LibraryID;
            alignmentProperty.MetaboliteName = peakProperties[fileID].MetaboliteName;
            alignmentProperty.TotalSimilairty = peakProperties[fileID].TotalSimilairty;
            alignmentProperty.MassSpectraSimilarity = peakProperties[fileID].MassSpectraSimilarity;
            alignmentProperty.ReverseSimilarity = peakProperties[fileID].ReverseSimilarity;
            alignmentProperty.FragmentPresencePercentage = peakProperties[fileID].FragmentPresencePercentage;
            alignmentProperty.EiSpectrumSimilarity = peakProperties[fileID].EiSpectrumSimilarity;
            alignmentProperty.RetentionTimeSimilarity = peakProperties[fileID].RetentionTimeSimilarity;
            alignmentProperty.RetentionIndexSimilarity = peakProperties[fileID].RetentionIndexSimilarity;
        }

        private static void setRepresentativeFileID(AlignmentPropertyBean alignmentProperty, int fileIdOfMaxTotalScore, int fileIdOfMaxIntensity)
        {
            if (fileIdOfMaxTotalScore != -1) alignmentProperty.RepresentativeFileID = fileIdOfMaxTotalScore;
            else alignmentProperty.RepresentativeFileID = fileIdOfMaxIntensity;

            var peakProperties = alignmentProperty.AlignedPeakPropertyBeanCollection;
            var peakID = peakProperties[alignmentProperty.RepresentativeFileID].PeakID;

            if (peakID < 0)
            {
                for (int i = 0; i < peakProperties.Count; i++)
                {
                    if (peakProperties[i].PeakID >= 0)
                    {
                        alignmentProperty.RepresentativeFileID = peakProperties[i].FileID;
                        break;
                    }
                }
            }
        }

        private static void setBasicAlignmentProperties(AlignmentPropertyBean alignmentSpot, int alignmnetID, AnalysisParamOfMsdialGcms param, out double minInt, out double maxInt, out int fileIdOfMaxIntensity, out int fileIdOfMaxTotalScore)
        {
            var peakProperties = alignmentSpot.AlignedPeakPropertyBeanCollection;

            fileIdOfMaxTotalScore = -1; fileIdOfMaxIntensity = -1;
            minInt = double.MaxValue; maxInt = double.MinValue;

            var countFill = 0;
            double sumRt = 0, sumRi = 0, sumInt = 0, sumAveragePeakWidth = 0,
                minRt = double.MaxValue, maxRt = double.MinValue, minRi = double.MaxValue, maxRi = double.MinValue, maxTotalScore = double.MinValue;
            
            for (int j = 0; j < peakProperties.Count; j++)
            {
                if (peakProperties[j].PeakID < 0) continue;

                sumRt += peakProperties[j].RetentionTime;
                sumRi += peakProperties[j].RetentionIndex;
                sumInt += peakProperties[j].Variable;
                sumAveragePeakWidth += peakProperties[j].PeakWidth;

                if (minInt > peakProperties[j].Variable) minInt = peakProperties[j].Variable;
                if (maxInt < peakProperties[j].Variable) { maxInt = peakProperties[j].Variable; fileIdOfMaxIntensity = j; }
                if (minRt > peakProperties[j].RetentionTime) minRt = peakProperties[j].RetentionTime;
                if (maxRt < peakProperties[j].RetentionTime) maxRt = peakProperties[j].RetentionTime;
                if (minRi > peakProperties[j].RetentionIndex) minRi = peakProperties[j].RetentionIndex;
                if (maxRi < peakProperties[j].RetentionIndex) maxRi = peakProperties[j].RetentionIndex;
                if (maxTotalScore < peakProperties[j].TotalSimilairty) { maxTotalScore = peakProperties[j].TotalSimilairty; fileIdOfMaxTotalScore = j; }
                countFill++;
            }
            //if (alignmnetID == 37) {
            //    Debug.WriteLine("now");
            //}

            alignmentSpot.AlignmentID = alignmnetID;
            alignmentSpot.CentralRetentionTime = (float)(sumRt / countFill);
            alignmentSpot.CentralRetentionIndex = (float)(sumRi / countFill);
            alignmentSpot.AverageValiable = (float)(sumInt / countFill);
            alignmentSpot.AveragePeakWidth = (float)(sumAveragePeakWidth / countFill);
            alignmentSpot.FillParcentage = (float)countFill / (float)peakProperties.Count;
            alignmentSpot.MaxRi = (float)maxRi;
            alignmentSpot.MinRi = (float)minRi;
            alignmentSpot.MaxRt = (float)maxRt;
            alignmentSpot.MinRt = (float)minRt;
            alignmentSpot.MaxValiable = (float)maxInt;
            alignmentSpot.MinValiable = (float)minInt;
        }

        private static ObservableCollection<AlignmentPropertyBean> getRefinedAlignmentPropertyBeanCollection(
            ObservableCollection<AlignmentPropertyBean> alignmentSpots, AnalysisParamOfMsdialGcms param, ProjectPropertyBean project,
            ref List<int> newIdList)
        {
            if (alignmentSpots.Count <= 1) return alignmentSpots;

            //foreach (var spot in alignmentSpots) {
            //    Debug.WriteLine(spot.LibraryID);
            //}

            var alignmentSpotList = new List<AlignmentPropertyBean>(alignmentSpots);
            alignmentSpotList = alignmentSpotList.OrderByDescending(n => n.LibraryID).ToList();

            //foreach (var spot in alignmentSpotList) {
            //    Debug.WriteLine(spot.LibraryID);
            //}

            var currentLibraryId = alignmentSpotList[0].LibraryID;
            var currentPeakId = 0;

            //remove duplicate identifications
            if (param.IsOnlyTopHitReport) {
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
            }

            //cleaning duplicate spots
            var cSpots = new List<AlignmentPropertyBean>();
            foreach (var spot in alignmentSpotList.Where(n => n.LibraryID >= 0)) {
                cSpots.Add(spot); // first, identifid spots are stored for this priority.
            }

            //if both Quant mass and Retention is same, exclude the spot information.
            foreach (var aSpot in alignmentSpotList.Where(n => n.LibraryID < 0)) {
                var aSpotRt = aSpot.CentralRetentionTime;
                var aSpotRi = aSpot.CentralRetentionIndex;
                var aSpotMass = aSpot.QuantMass;

                var flg = false;
                foreach (var cSpot in cSpots.Where(n => Math.Abs(n.QuantMass - aSpotMass) < param.MassAccuracy)) {
                    var cSpotRt = cSpot.CentralRetentionTime;
                    var cSpotRi = cSpot.CentralRetentionIndex;

                    #region checking ri/rt similarity
                    if (param.AlignmentIndexType == AlignmentIndexType.RI) {
                        if (param.RiCompoundType == RiCompoundType.Alkanes) {
                            if (Math.Abs(cSpotRi - aSpotRi) < 2.5) {
                                flg = true;
                                break;
                            }
                        }
                        else {
                            if (Math.Abs(cSpotRi - aSpotRi) < 1000) {
                                flg = true;
                                break;
                            }
                        }
                    }
                    else {
                        if (Math.Abs(cSpotRt - aSpotRt) < 0.025) {
                            flg = true;
                            break;
                        }
                    }
                    #endregion
                }
                if (!flg) cSpots.Add(aSpot);
            }

            //// Replace true zero values with 1/2 of minimum peak height over all samples
            //if (param.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples) {
            //    foreach (var spot in cSpots) {
            //        var nonZeroMin = double.MaxValue;
            //        var isZeroExist = false;
            //        foreach (var peak in spot.AlignedPeakPropertyBeanCollection) {
            //            if (peak.Variable > 0.0001 && nonZeroMin > peak.Variable)
            //                nonZeroMin = peak.Variable;
            //            if (peak.Variable < 0.0001)
            //                isZeroExist = true;
            //        }

            //        if (isZeroExist && nonZeroMin != double.MaxValue) {
            //            foreach (var peak in spot.AlignedPeakPropertyBeanCollection) {
            //                if (peak.Variable < 0.0001)
            //                    peak.Variable = (float)(nonZeroMin * 0.5);
            //            }
            //        }
            //    }
            //}

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



            if (param.AlignmentIndexType == AlignmentIndexType.RT)
                fcSpots = fcSpots.OrderBy(n => n.CentralRetentionTime).ThenBy(n => n.QuantMass).ToList();
            else
                fcSpots = fcSpots.OrderBy(n => n.CentralRetentionIndex).ThenBy(n => n.QuantMass).ToList();
            newIdList = fcSpots.Select(x => x.AlignmentID).ToList();
            for (int i = 0; i < fcSpots.Count; i++) {
                fcSpots[i].AlignmentID = i;
            }

            //alignmentSpotList = alignmentSpotList.OrderBy(n => n.AlignmentID).ToList();
            return new ObservableCollection<AlignmentPropertyBean>(fcSpots);
        }

        private static List<MS1DecResult> getRefinedMS1DecResults(List<MS1DecResult> ms1DecResults, AlignmentIndexType indexType)
        {
            if (ms1DecResults.Count <= 1) return ms1DecResults;

            ms1DecResults = ms1DecResults.OrderByDescending(n => n.MspDbID).ToList();

            var currentLibraryId = ms1DecResults[0].MspDbID;
            var currentPeakId = 0;

            for (int i = 1; i < ms1DecResults.Count; i++) {
                if (ms1DecResults[i].MspDbID < 0) break;
                if (ms1DecResults[i].MspDbID != currentLibraryId) {
                    currentLibraryId = ms1DecResults[i].MspDbID;
                    currentPeakId = i;
                    continue;
                }
                else {
                    if (ms1DecResults[currentPeakId].TotalSimilarity < ms1DecResults[i].TotalSimilarity) {
                        setDefaultCompoundInformation(ms1DecResults[currentPeakId]);
                        currentPeakId = i;
                    }
                    else {
                        setDefaultCompoundInformation(ms1DecResults[i]);
                    }
                }
            }

            if (indexType == AlignmentIndexType.RT)
                ms1DecResults = ms1DecResults.OrderBy(n => n.RetentionTime).ThenBy(n => n.BasepeakMz).ToList();
            else
                ms1DecResults = ms1DecResults.OrderBy(n => n.RetentionIndex).ThenBy(n => n.BasepeakMz).ToList();

            return ms1DecResults;
        }

        private static void setDefaultCompoundInformation(MS1DecResult mS1DecResult)
        {
            mS1DecResult.MetaboliteName = string.Empty;
            mS1DecResult.MspDbID = -1;
            mS1DecResult.RetentionTimeSimilarity = -1;
            mS1DecResult.RetentionIndexSimilarity = -1;
            mS1DecResult.DotProduct = -1;
            mS1DecResult.ReverseDotProduct = -1;
            mS1DecResult.PresencePersentage = -1;
            mS1DecResult.EiSpectrumSimilarity = -1;
            mS1DecResult.TotalSimilarity = -1;
        }

        private static void setDefaultCompoundInformation(AlignmentPropertyBean alignmentPropertyBean)
        {
            alignmentPropertyBean.MetaboliteName = string.Empty;
            alignmentPropertyBean.LibraryID = -1;
            alignmentPropertyBean.TotalSimilairty = -1;
            alignmentPropertyBean.MassSpectraSimilarity = -1;
            alignmentPropertyBean.ReverseSimilarity = -1;
            alignmentPropertyBean.FragmentPresencePercentage = -1;
            alignmentPropertyBean.EiSpectrumSimilarity = -1;
            alignmentPropertyBean.RetentionIndexSimilarity = -1;
            alignmentPropertyBean.RetentionTimeSimilarity = -1;
        }

        public static void WriteAlignedSpotMs1DecResults(ObservableCollection<AnalysisFileBean> analysisFiles, string spectraFilePath, 
            AlignmentResultBean alignmentResult, AnalysisParamOfMsdialGcms param, List<MspFormatCompoundInformationBean> mspDB,
            Action<int> reportAction)
        {
            if (param.IsIdentificationOnlyPerformedForAlignmentFile && mspDB != null && mspDB.Count > 0) {
                if (param.RetentionType == RetentionType.RT)
                    mspDB = mspDB.OrderBy(n => n.RetentionTime).ToList();
                else
                    mspDB = mspDB.OrderBy(n => n.RetentionIndex).ToList();
            }

            using (var fs = File.Open(spectraFilePath, FileMode.Create, FileAccess.ReadWrite))
            {
                var seekPointer = new List<long>();
                var alignmentSpots = alignmentResult.AlignmentPropertyBeanCollection;
                var totalPeakNumber = alignmentSpots.Count;

                DataStorageGcUtility.WriteHeaders(fs, seekPointer, totalPeakNumber);
                for (int i = 0; i < alignmentResult.AlignmentPropertyBeanCollection.Count; i++)
                {
                    var seekpoint = fs.Position;

                    seekPointer.Add(seekpoint);

                    var fileID = alignmentSpots[i].RepresentativeFileID;
                    var ms1DecSeekpoint = alignmentSpots[i].AlignedPeakPropertyBeanCollection[fileID].SeekPoint;
                    var filePath = analysisFiles[fileID].AnalysisFilePropertyBean.DeconvolutionFilePath;
                    var ms1DecResult = DataStorageGcUtility.ReadMS1DecResult(filePath, ms1DecSeekpoint);

                    if (param.IsIdentificationOnlyPerformedForAlignmentFile && mspDB != null) {
                        processCompountIdentificationForAlignmentSpot(alignmentResult.AlignmentPropertyBeanCollection[i], ms1DecResult, param, mspDB);
                    }

                    ms1DecResult.SeekPoint = seekpoint;
                    ms1DecResult.Ms1DecID = i;

                    GCDecWriterVer1.Write(fs, ms1DecResult, i, alignmentResult.AlignmentPropertyBeanCollection[i]);

                    reportAction?.Invoke((int)((double)(i + 1) / (double)alignmentResult.AlignmentPropertyBeanCollection.Count * 100));
                }
                DataStorageGcUtility.WriteSeekpointer(fs, seekPointer);
            }

            if (param.IsIdentificationOnlyPerformedForAlignmentFile && mspDB != null && mspDB.Count > 0) //reminder
                mspDB = mspDB.OrderBy(n => n.Id).ToList();
        }

        private static void processCompountIdentificationForAlignmentSpot(AlignmentPropertyBean alignmentProperty, MS1DecResult result, 
            AnalysisParamOfMsdialGcms param, List<MspFormatCompoundInformationBean> mspDB) {

            var maxMspScore = double.MinValue;
            var maxEiSpecScore = -1.0;
            var maxRtSimScore = -1.0;
            var maxRiSimScore = -1.0;
            var maxDotproduct = -1.0;
            var maxRevDotproduct = -1.0;
            var maxPresencePercentage = -1.0;
            var maxMspID = -1;

            Identification.MspBasedNewProcess(result, mspDB, param, out maxMspID, out maxMspScore,
                       out maxEiSpecScore, out maxRtSimScore, out maxRiSimScore,
                       out maxDotproduct, out maxRevDotproduct, out maxPresencePercentage);

            if (maxMspID >= 0) {
                result.MetaboliteName = mspDB[maxMspID].Name;
                result.MspDbID = mspDB[maxMspID].Id;
                result.RetentionTimeSimilarity = (float)maxRtSimScore * 1000;
                result.RetentionIndexSimilarity = (float)maxRiSimScore * 1000;
                result.DotProduct = (float)maxDotproduct * 1000;
                result.ReverseDotProduct = (float)maxRevDotproduct * 1000;
                result.PresencePersentage = (float)maxPresencePercentage * 1000;
                result.EiSpectrumSimilarity = (float)maxEiSpecScore * 1000;
                result.TotalSimilarity = (float)maxMspScore * 1000;

                alignmentProperty.LibraryID = result.MspDbID;
                alignmentProperty.MetaboliteName = result.MetaboliteName;
                alignmentProperty.TotalSimilairty = result.TotalSimilarity;
                alignmentProperty.MassSpectraSimilarity = result.DotProduct;
                alignmentProperty.ReverseSimilarity = result.ReverseDotProduct;
                alignmentProperty.FragmentPresencePercentage = result.PresencePersentage;
                alignmentProperty.EiSpectrumSimilarity = result.EiSpectrumSimilarity;
                alignmentProperty.RetentionTimeSimilarity = result.RetentionTimeSimilarity;
                alignmentProperty.RetentionIndexSimilarity = result.RetentionIndexSimilarity;
            }
        }
    }
}

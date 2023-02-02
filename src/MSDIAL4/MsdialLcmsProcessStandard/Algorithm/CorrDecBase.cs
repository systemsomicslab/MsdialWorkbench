using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CompMs.RawDataHandler.Core;
using Rfx.Riken.OsakaUniv;
using Msdial.Lcms.Dataprocess.Utility;
using CompMs.Common.DataObj;

namespace Msdial.Lcms.Dataprocess.Algorithm
{
    public class CorrDecBase {

        public static string TemporaryDirectoryHandler(ProjectPropertyBean projectPropertyBean, bool isCreate = false, bool isRemove = false)
        {
            var dt = projectPropertyBean.ProjectDate;
            var dirname = Path.Combine(projectPropertyBean.ProjectFolderPath, "project_" + dt.ToString("yyyy_MM_dd_HH_mm_ss") + "Tmp");
            if (isCreate && !Directory.Exists(dirname))
                Directory.CreateDirectory(dirname);
            if (isRemove && Directory.Exists(dirname))
                Directory.Delete(dirname, true);

            return dirname;
        }

        public static void GenerateTemporalMs2SpectraFiles(ProjectPropertyBean projectPropertyBean, RdamPropertyBean rdamPropertyBean,
            ObservableCollection<AnalysisFileBean> analysisFileBeanCollection, AnalysisParametersBean param,
            AlignmentResultBean alignmentResult, AlignmentFileBean alignmentFile, Action<float> reportAction) {

            var dirname = TemporaryDirectoryHandler(projectPropertyBean, isCreate: true, isRemove: false);
            var progress = 100 / (analysisFileBeanCollection.Count);
            var stepSize = analysisFileBeanCollection.Count / 100;
            var isCentroid = projectPropertyBean.DataTypeMS2 == DataType.Centroid;
            // use all files including blanks and QCs
            Parallel.For(0, analysisFileBeanCollection.Count, new ParallelOptions() { MaxDegreeOfParallelism = param.NumThreads }, (i) =>
            {
                DataStorageLcUtility.SetPeakAreaBeanCollection(analysisFileBeanCollection[i], analysisFileBeanCollection[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);
                var fileID = rdamPropertyBean.RdamFilePath_RdamFileID[analysisFileBeanCollection[i].AnalysisFilePropertyBean.AnalysisFilePath];
                var measurementID = rdamPropertyBean.RdamFileContentBeanCollection[fileID].FileID_MeasurementID[analysisFileBeanCollection[i].AnalysisFilePropertyBean.AnalysisFileId];

                using (var rawDataAccess = new RawDataAccess(analysisFileBeanCollection[i].AnalysisFilePropertyBean.AnalysisFilePath, measurementID, false, false, true, analysisFileBeanCollection[i].RetentionTimeCorrectionBean.PredictedRt))
                {
                    var spectrumCollection = DataAccessLcUtility.GetRdamSpectrumCollection(projectPropertyBean, rdamPropertyBean, analysisFileBeanCollection[i]);
                    var rawPeakListList = new List<PeakListForGrouping>();
                    if (projectPropertyBean.Ms2LevelIdList.Count > 0)
                    {
                        for (var numDec = 0; numDec < projectPropertyBean.Ms2LevelIdList.Count; numDec++)
                        {
                            rawPeakListList.Add(new PeakListForGrouping(alignmentResult.AlignmentPropertyBeanCollection.Count));
                        }
                        for (int j = 0; j < alignmentResult.AlignmentPropertyBeanCollection.Count; j++)
                        {
                            var alignedSpotProp = alignmentResult.AlignmentPropertyBeanCollection[j]; ;
                            if (alignedSpotProp.AlignedPeakPropertyBeanCollection[i].RetentionTime <= 0) continue;
                            if (alignedSpotProp.AlignedPeakPropertyBeanCollection[i].IsManuallyModified || alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID >= 0)
                            {

                                var ms2datapointNumberList = new List<int>();
                                if (alignedSpotProp.AlignedPeakPropertyBeanCollection[i].IsManuallyModified || alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID < 0)
                                {
                                    ms2datapointNumberList = Utility.DataAccessLcUtility.GetMs2DatapointNumberFromRtAif(spectrumCollection, projectPropertyBean.ExperimentID_AnalystExperimentInformationBean, alignedSpotProp.AlignedPeakPropertyBeanCollection[i].RetentionTime);
                                }
                                else
                                {
                                    var peakAreaBean = analysisFileBeanCollection[i].PeakAreaBeanCollection[alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID];
                                    ms2datapointNumberList = peakAreaBean.Ms2LevelDatapointNumberList;
                                }
 
                                for (var numDec = 0; numDec < projectPropertyBean.Ms2LevelIdList.Count; numDec++)
                                {
                                    rawPeakListList[numDec].PeakList[j] = getTargetPeaks(spectrumCollection, ms2datapointNumberList[numDec], param.AnalysisParamOfMsdialCorrDec.MinMS2Intensity, isCentroid);
                                }
                            }
                        }
                        for (var numDec = 0; numDec < projectPropertyBean.Ms2LevelIdList.Count; numDec++)
                        {
                            var filename = Path.Combine(dirname, alignmentFile.FileName + "_peaklist_RawMs" + numDec + "_File" + i + ".pll");
                            CreateTemporaryFileForMsGrouping.WriteMsPeakList(rawPeakListList[numDec].PeakList, projectPropertyBean, filename);
                        }
                    }
                    else
                    {
                        var numDec = 0;
                        rawPeakListList.Add(new PeakListForGrouping(alignmentResult.AlignmentPropertyBeanCollection.Count));
                        for (int j = 0; j < alignmentResult.AlignmentPropertyBeanCollection.Count; j++)
                        {
                            var alignedSpotProp = alignmentResult.AlignmentPropertyBeanCollection[j]; ;
                            if (alignedSpotProp.AlignedPeakPropertyBeanCollection[i].RetentionTime <= 0) continue;
                            if (projectPropertyBean.MethodType == MethodType.diMSMS && alignedSpotProp.AlignedPeakPropertyBeanCollection[i].IsManuallyModified)
                            {
                                var ms2datapointNumber = Utility.DataAccessLcUtility.GetMs2DatapointNumberFromRt(spectrumCollection, projectPropertyBean.ExperimentID_AnalystExperimentInformationBean, alignedSpotProp.AlignedPeakPropertyBeanCollection[i].RetentionTime, alignedSpotProp.AlignedPeakPropertyBeanCollection[i].AccurateMass);
                                if (ms2datapointNumber < 0) continue;
                                rawPeakListList[0].PeakList[j] = getTargetPeaks(spectrumCollection, ms2datapointNumber, param.AnalysisParamOfMsdialCorrDec.MinMS2Intensity, isCentroid);
                            }
                            else if (alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID >= 0)
                            {
                                var peakAreaBean = analysisFileBeanCollection[i].PeakAreaBeanCollection[alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID];
                                if (peakAreaBean.Ms2LevelDatapointNumber < 0) continue;
                                rawPeakListList[0].PeakList[j] = getTargetPeaks(spectrumCollection, peakAreaBean.Ms2LevelDatapointNumber, param.AnalysisParamOfMsdialCorrDec.MinMS2Intensity, isCentroid);
                            }
                        }
                        var filename = Path.Combine(dirname, alignmentFile.FileName + "_peaklist_RawMs" + numDec + "_File" + i + ".pll");
                        CreateTemporaryFileForMsGrouping.WriteMsPeakList(rawPeakListList[numDec].PeakList, projectPropertyBean, filename);
                    }
                }
                DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(analysisFileBeanCollection[i]);
                reportAction?.Invoke(1f);
             });
        }

        public static void CreateMs2SpectraGroup(ProjectPropertyBean projectPropertyBean, AlignmentFileBean alignmentFile, ObservableCollection<AnalysisFileBean> analysisFileBeanCollection, AlignmentResultBean alignmentResult, AnalysisParametersBean param, Action<float> reportAction)
        {
            var dirname = TemporaryDirectoryHandler(projectPropertyBean, isCreate: false, isRemove: false);
            //System.Threading.Tasks.Parallel.For(0, projectPropertyBean.Ms2LevelIdList.Count, numDec => {
            for (var numDec = 0; numDec < projectPropertyBean.Ms2LevelIdList.Count; numDec++) {
                var filename_in = Path.Combine(dirname, alignmentFile.FileName + "_peaklist_RawMs" + numDec + "_File");
                var filename_out = Path.Combine(projectPropertyBean.ProjectFolderPath, alignmentFile.FileName + "_MsGrouping_Raw_" + numDec + ".mfg");
                CreateMzIntGroupListFile.WriteMsGroupListList(filename_out, filename_in, analysisFileBeanCollection.Count,
                    alignmentResult.AlignmentPropertyBeanCollection.Count, param.AnalysisParamOfMsdialCorrDec.MinNumberOfSample, param.CentroidMs2Tolerance,
                    param.AnalysisParamOfMsdialCorrDec.MinMS2Intensity, param.AnalysisParamOfMsdialCorrDec.MinMS2RelativeIntensity, reportAction);
            }
            if(projectPropertyBean.Ms2LevelIdList.Count == 0)
            {
                var numDec = 0;
                var filename_in = Path.Combine(dirname, alignmentFile.FileName + "_peaklist_RawMs" + numDec + "_File");
                var filename_out = Path.Combine(projectPropertyBean.ProjectFolderPath, alignmentFile.FileName + "_MsGrouping_Raw_" + numDec + ".mfg");
                CreateMzIntGroupListFile.WriteMsGroupListList(filename_out, filename_in, analysisFileBeanCollection.Count,
                    alignmentResult.AlignmentPropertyBeanCollection.Count, param.AnalysisParamOfMsdialCorrDec.MinNumberOfSample, param.CentroidMs2Tolerance,
                    param.AnalysisParamOfMsdialCorrDec.MinMS2Intensity, param.AnalysisParamOfMsdialCorrDec.MinMS2RelativeIntensity, reportAction);
            }
            //});
        }

        
        private static List<float[]> getTargetPeaks(ObservableCollection<RawSpectrum> spectrumCollection, int datapointId, float IntensityThreshold, bool isCentroid = true) {
            var targetPeaks = new List<float[]>();
            var spectra = spectrumCollection[datapointId];
            if (isCentroid)
            {
                foreach (var peak in spectra.Spectrum)
                    if (peak.Intensity > IntensityThreshold)
                    {
                        targetPeaks.Add(new float[] { (float)peak.Mz, (float)peak.Intensity });
                    }
            }
            else
            {
                var rawSpectra = new List<double[]>();
                foreach (var peak in spectra.Spectrum)
                {
                    rawSpectra.Add(new double[] { peak.Mz, peak.Intensity });
                }
                var centroidSpectra = SpectralCentroiding.PeakDetectionBasedCentroid(rawSpectra);
                if (centroidSpectra == null) return targetPeaks;
                foreach (var peak in centroidSpectra)
                {
                    targetPeaks.Add(new float[] { (float)peak[0], (float)peak[1] });
                }
            }
            return targetPeaks;
        }

        public static List<CorrDecResult> CorrDecSingleAlignmentSpot(ProjectPropertyBean projectPropertyBean, RdamPropertyBean rdamPropertyBean,
            ObservableCollection<AnalysisFileBean> analysisFileBeanCollection, AnalysisParametersBean param,
            AlignmentResultBean alignmentResult, int alignmentID, Action<float> reportAction = null)
        {

            var dirname = TemporaryDirectoryHandler(projectPropertyBean, isCreate: true, isRemove: false);

            var rawPeakListList = new List<PeakListForGrouping>();
            var isCentroid = projectPropertyBean.DataTypeMS2 == DataType.Centroid;
            // AIF or not
            if (projectPropertyBean.Ms2LevelIdList.Count > 0)
            {
                for (var numDec = 0; numDec < projectPropertyBean.Ms2LevelIdList.Count; numDec++)
                {
                    rawPeakListList.Add(new PeakListForGrouping(analysisFileBeanCollection.Count));
                }
            }
            else
            {
                rawPeakListList.Add(new PeakListForGrouping(analysisFileBeanCollection.Count));
            }

            Parallel.For(0, analysisFileBeanCollection.Count, id => {
                int i = id;
                DataStorageLcUtility.SetPeakAreaBeanCollection(analysisFileBeanCollection[i], analysisFileBeanCollection[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);
                var fileID = rdamPropertyBean.RdamFilePath_RdamFileID[analysisFileBeanCollection[i].AnalysisFilePropertyBean.AnalysisFilePath];
                var measurementID = rdamPropertyBean.RdamFileContentBeanCollection[fileID].FileID_MeasurementID[analysisFileBeanCollection[i].AnalysisFilePropertyBean.AnalysisFileId];

                using (var rawDataAccess = new RawDataAccess(analysisFileBeanCollection[i].AnalysisFilePropertyBean.AnalysisFilePath, measurementID, false, false, true, analysisFileBeanCollection[i].RetentionTimeCorrectionBean.PredictedRt))
                {
                    var spectrumCollection = DataAccessLcUtility.GetRdamSpectrumCollection(rawDataAccess);
                    var j = alignmentID;
                    var alignedSpotProp = alignmentResult.AlignmentPropertyBeanCollection[j]; ;
                    if (alignedSpotProp.AlignedPeakPropertyBeanCollection[i].IsManuallyModified || alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID >= 0)
                    {
                        if (alignedSpotProp.AlignedPeakPropertyBeanCollection[i].RetentionTime > 0)
                        {
                            if (projectPropertyBean.Ms2LevelIdList.Count > 0)
                            {
                                var ms2datapointNumberList = new List<int>();
                                if (alignedSpotProp.AlignedPeakPropertyBeanCollection[i].IsManuallyModified || alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID < 0)
                                {
                                    ms2datapointNumberList = Utility.DataAccessLcUtility.GetMs2DatapointNumberFromRtAif(spectrumCollection, projectPropertyBean.ExperimentID_AnalystExperimentInformationBean, alignedSpotProp.AlignedPeakPropertyBeanCollection[i].RetentionTime);

                                }
                                else
                                {
                                    var peakAreaBean = analysisFileBeanCollection[i].PeakAreaBeanCollection[alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID];
                                    ms2datapointNumberList = peakAreaBean.Ms2LevelDatapointNumberList;
                                }

                                for (var numDec = 0; numDec < projectPropertyBean.Ms2LevelIdList.Count; numDec++)
                                {
                                    rawPeakListList[numDec].PeakList[i] = getTargetPeaks(spectrumCollection, ms2datapointNumberList[numDec], param.AnalysisParamOfMsdialCorrDec.MinMS2Intensity, isCentroid);
                                }
                            }
                            else
                            {
                                if (projectPropertyBean.MethodType == MethodType.diMSMS && alignedSpotProp.AlignedPeakPropertyBeanCollection[i].IsManuallyModified)
                                {
                                    var ms2datapointNumber = Utility.DataAccessLcUtility.GetMs2DatapointNumberFromRt(spectrumCollection, projectPropertyBean.ExperimentID_AnalystExperimentInformationBean, alignedSpotProp.AlignedPeakPropertyBeanCollection[i].RetentionTime, alignedSpotProp.AlignedPeakPropertyBeanCollection[i].AccurateMass);
                                    if (ms2datapointNumber < 0)
                                        rawPeakListList[0].PeakList[i] = getTargetPeaks(spectrumCollection, ms2datapointNumber, param.AnalysisParamOfMsdialCorrDec.MinMS2Intensity, isCentroid);
                                }
                                else if (alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID >= 0)
                                {
                                    var peakAreaBean = analysisFileBeanCollection[i].PeakAreaBeanCollection[alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID];
                                    if (peakAreaBean.Ms2LevelDatapointNumber > 0) 
                                        rawPeakListList[0].PeakList[i] = getTargetPeaks(spectrumCollection, peakAreaBean.Ms2LevelDatapointNumber, param.AnalysisParamOfMsdialCorrDec.MinMS2Intensity, isCentroid);
                                }
                            }
                        }
                    }
                }
                reportAction?.Invoke(1f);
                DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(analysisFileBeanCollection[i]);
            });

            var numMs2Scan = projectPropertyBean.Ms2LevelIdList.Count;
            var corrDecResultArr = new CorrDecResult[numMs2Scan];
            var corrDecParam = param.AnalysisParamOfMsdialCorrDec;
            var correlMaxDif_MS1 = corrDecParam.CorrDiff_MS1;
            var correlMaxDif_MS2 = corrDecParam.CorrDiff_MS2;
            var correlThreshold = corrDecParam.MinCorr_MS2;
            var minCorrelPrecursors = corrDecParam.MinCorr_MS1;
            int minCountCoverage = (int)(corrDecParam.MinDetectedPercentToVisualize * analysisFileBeanCollection.Count);
            int minNumSamples = corrDecParam.MinNumberOfSample;

            // multiple CEs
            if (numMs2Scan > 0)
            {
                System.Threading.Tasks.Parallel.For(0, projectPropertyBean.Ms2LevelIdList.Count, numDec =>
                {
                    var mzIntGroupList = CreateMzIntGroupListFile.ExcuteMzIntGrouping(rawPeakListList[numDec].PeakList, analysisFileBeanCollection.Count,
                        corrDecParam.MinNumberOfSample, corrDecParam.MS2Tolerance, corrDecParam.MinMS2Intensity, corrDecParam.MinMS2RelativeIntensity);
                    var corrDecResult = new CorrDecResult();
                    corrDecResult.PeakMatrix = CorrDecHandler.SingleSpotCorrelationCalculation(mzIntGroupList, analysisFileBeanCollection.Count, alignmentID,
                        alignmentResult.AlignmentPropertyBeanCollection, minCorrelPrecursors, correlMaxDif_MS1, correlMaxDif_MS2, correlThreshold,
                        minNumSamples, minCountCoverage, corrDecParam.RemoveAfterPrecursor);
                    corrDecResultArr[numDec] = corrDecResult;
                });
            }
            else
            {
                var mzIntGroupList = CreateMzIntGroupListFile.ExcuteMzIntGrouping(rawPeakListList[0].PeakList, analysisFileBeanCollection.Count,
                    corrDecParam.MinNumberOfSample, corrDecParam.MS2Tolerance, corrDecParam.MinMS2Intensity, corrDecParam.MinMS2RelativeIntensity);
                var corrDecResult = new CorrDecResult();
                corrDecResult.PeakMatrix = CorrDecHandler.SingleSpotCorrelationCalculation(mzIntGroupList, analysisFileBeanCollection.Count, alignmentID,
                    alignmentResult.AlignmentPropertyBeanCollection, minCorrelPrecursors, correlMaxDif_MS1, correlMaxDif_MS2, correlThreshold,
                    minNumSamples, minCountCoverage, corrDecParam.RemoveAfterPrecursor);
                return new List<CorrDecResult>() { corrDecResult };
            }
            return corrDecResultArr.ToList();
        }

        #region old programs
        /*    public static void Execute_CreateMsGroupWithMs2Dec(ProjectPropertyBean projectPropertyBean, RdamPropertyBean rdamPropertyBean,
                 ObservableCollection<AnalysisFileBean> analysisFileBeanCollection, AnalysisParametersBean analysisParametersBean,
                 AlignmentResultBean alignmentResult, AlignmentFileBean alignmentFile) {

                 var dt = projectPropertyBean.ProjectDate;
                 var dirname = projectPropertyBean.ProjectFolderPath + "\\" + "project_" + dt.ToString("yyyy_MM_dd_HH_mm_ss") + "Tmp";
                 if (!System.IO.Directory.Exists(dirname))
                     System.IO.Directory.CreateDirectory(dirname);


                 System.Threading.Tasks.Parallel.For(0, analysisFileBeanCollection.Count, i => {
                     DataStorageLcUtility.SetPeakAreaBeanCollection(analysisFileBeanCollection[i], analysisFileBeanCollection[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);
                     var fileID = rdamPropertyBean.RdamFilePath_RdamFileID[analysisFileBeanCollection[i].AnalysisFilePropertyBean.AnalysisFilePath];
                     var measurementID = rdamPropertyBean.RdamFileContentBeanCollection[fileID].FileID_MeasurementID[analysisFileBeanCollection[i].AnalysisFilePropertyBean.AnalysisFileId];
                     var fss = new List<FileStream>();
                     var seekpoints = new List<List<long>>();

                     for (var numDec = 0; numDec < projectPropertyBean.Ms2LevelIdList.Count; numDec++) {
                         fss.Add(File.Open(analysisFileBeanCollection[i].AnalysisFilePropertyBean.DeconvolutionFilePathList[numDec], FileMode.Open, FileAccess.ReadWrite));
                         seekpoints.Add(SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fss[numDec]));
                     }

                     using (var rawDataAccess = new RawDataAccess(analysisFileBeanCollection[i].AnalysisFilePropertyBean.AnalysisFilePath, measurementID, analysisFileBeanCollection[i].RetentionTimeCorrectionBean.PredictedRt)) {

                         var spectrumCollection = DataAccessLcUtility.GetRdamSpectrumCollection(rawDataAccess);
                         var rawPeakListList = new List<PeakListForGrouping>();
                         var decPeakListList = new List<PeakListForGrouping>();
                         for (var numDec = 0; numDec < projectPropertyBean.Ms2LevelIdList.Count; numDec++) {
                             rawPeakListList.Add(new PeakListForGrouping(alignmentResult.AlignmentPropertyBeanCollection.Count));
                             decPeakListList.Add(new PeakListForGrouping(alignmentResult.AlignmentPropertyBeanCollection.Count));
                         }

                         for (int j = 0; j < alignmentResult.AlignmentPropertyBeanCollection.Count; j++) {
                             var alignedSpotProp = alignmentResult.AlignmentPropertyBeanCollection[j]; ;
                             if (alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID >= 0) {
                                 var peakAreaBean = analysisFileBeanCollection[i].PeakAreaBeanCollection[alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID];
                                 for (var numDec = 0; numDec < projectPropertyBean.Ms2LevelIdList.Count; numDec++) {
                                     var ms2DecRes = SpectralDeconvolution.ReadMS2DecResult(fss[numDec], seekpoints[numDec], alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID);
                                     rawPeakListList[numDec].PeakList[j] = getTargetPeaks(spectrumCollection, peakAreaBean.Ms2LevelDatapointNumberList[numDec], 1000);
                                     decPeakListList[numDec].PeakList[j] = getTargetPeaksFromDec(ms2DecRes, 1000);
                                 }
                             }
                         }
                         for (var numDec = 0; numDec < projectPropertyBean.Ms2LevelIdList.Count; numDec++) {
                             var filename = dirname + "\\" + alignmentFile.FileName + "_peaklist_RawMs" + numDec + "_File" + i + ".pll";
                             CreateTemporaryFileForMsGrouping.WriteMsPeakList(rawPeakListList[numDec].PeakList, projectPropertyBean, filename);

                             filename = dirname + "\\" + alignmentFile.FileName + "_peaklist_DecMs" + numDec + "_File" + i + ".pll";
                             CreateTemporaryFileForMsGrouping.WriteMsPeakList(decPeakListList[numDec].PeakList, projectPropertyBean, filename);
                         }

                         DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(analysisFileBeanCollection[i]);
                         for (var numDec = 0; numDec < projectPropertyBean.Ms2LevelIdList.Count; numDec++) {
                             fss[numDec].Dispose(); fss[numDec].Close();
                             seekpoints[numDec] = null;
                         }
                     }
                 });

                 System.Threading.Tasks.Parallel.For(0, projectPropertyBean.Ms2LevelIdList.Count, numDec => {
                     var filename_in = dirname + "\\" + alignmentFile.FileName + "_peaklist_RawMs" + numDec + "_File";
                     var filename_out = projectPropertyBean.ProjectFolderPath + "\\" + alignmentFile.FileName + "_MsGrouping_Raw_" + numDec + ".mfg";
                     CreateMsGroupListFile.WriteMsGroupListList(projectPropertyBean, analysisFileBeanCollection.Count, alignmentResult.AlignmentPropertyBeanCollection.Count, filename_in, filename_out, (int)1, 0.01f);
                     filename_in = dirname + "\\" + alignmentFile.FileName + "_peaklist_DecMs" + numDec + "_File";
                     filename_out = projectPropertyBean.ProjectFolderPath + "\\" + alignmentFile.FileName + "_MsGrouping_Dec_" + numDec + ".mfg";
                     CreateMsGroupListFile.WriteMsGroupListList(projectPropertyBean, analysisFileBeanCollection.Count, alignmentResult.AlignmentPropertyBeanCollection.Count, filename_in, filename_out, (int)(analysisFileBeanCollection.Count * 0.7), 0.01f);
                 });
             }

             public static void Execute_CreateMsGroupOnlyMs2Dec(ProjectPropertyBean projectPropertyBean, RdamPropertyBean rdamPropertyBean,
                 ObservableCollection<AnalysisFileBean> analysisFileBeanCollection, AnalysisParametersBean analysisParametersBean,
                 AlignmentResultBean alignmentResult) {

                 var dt = projectPropertyBean.ProjectDate;
                 var dirname = projectPropertyBean.ProjectFolderPath + "\\" + "project_" + dt.ToString("yyyy_MM_dd_HH_mm_ss") + "Tmp";
                 if (!System.IO.Directory.Exists(dirname))
                     System.IO.Directory.CreateDirectory(dirname);

                 for (int i = 0; i < analysisFileBeanCollection.Count; i++) {
                     var fss = new List<FileStream>();
                     var seekpoints = new List<List<long>>();

                     for (var numDec = 0; numDec < projectPropertyBean.Ms2LevelIdList.Count; numDec++) {
                         fss.Add(File.Open(analysisFileBeanCollection[i].AnalysisFilePropertyBean.DeconvolutionFilePathList[numDec], FileMode.Open, FileAccess.ReadWrite));
                         seekpoints.Add(SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fss[numDec]));
                     }

                     var decPeakListList = new List<PeakListForGrouping>();
                     for (var numDec = 0; numDec < projectPropertyBean.Ms2LevelIdList.Count; numDec++) {
                         decPeakListList.Add(new PeakListForGrouping(alignmentResult.AlignmentPropertyBeanCollection.Count));
                     }
                     for (int j = 0; j < alignmentResult.AlignmentPropertyBeanCollection.Count; j++) {
                         var alignedSpotProp = alignmentResult.AlignmentPropertyBeanCollection[j]; ;
                         if (alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID >= 0) {
                             for (var numDec = 0; numDec < projectPropertyBean.Ms2LevelIdList.Count; numDec++) {
                                 var ms2DecRes = SpectralDeconvolution.ReadMS2DecResult(fss[numDec], seekpoints[numDec], alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID);
                                 decPeakListList[numDec].PeakList[j] = getTargetPeaksFromDec(ms2DecRes, 1000);
                             }
                         }
                     }
                     for (var numDec = 0; numDec < projectPropertyBean.Ms2LevelIdList.Count; numDec++) {
                         var filename = dirname + "\\peaklist_DecMs" + numDec + "_File" + i + ".pll";
                         CreateTemporaryFileForMsGrouping.WriteMsPeakList(decPeakListList[numDec].PeakList, projectPropertyBean, filename);
                     }
                     for (var numDec = 0; numDec < projectPropertyBean.Ms2LevelIdList.Count; numDec++) {
                         fss[numDec].Dispose(); fss[numDec].Close();
                         seekpoints[numDec] = null;
                     }
                 }

                 for (var numDec = 0; numDec < projectPropertyBean.Ms2LevelIdList.Count; numDec++) {
                     var filename_in = dirname + "\\peaklist_DecMs" + numDec + "_File";
                     var filename_out = projectPropertyBean.ProjectFolderPath + "\\MsGrouping_Dec_" + numDec + ".mfg";
                     CreateMsGroupListFile.WriteMsGroupListList(projectPropertyBean, analysisFileBeanCollection.Count, alignmentResult.AlignmentPropertyBeanCollection.Count, filename_in, filename_out, (int)(analysisFileBeanCollection.Count * 0.7), 0.01f);
                 }
             }


             private static List<float[]> getTargetPeaksFromDec(MS2DecResult ms2DecRes, float IntensityThreshold) {
                 var spectra = ms2DecRes.MassSpectra.Where(x => x[1] > IntensityThreshold).ToList();
                 var targetPeaks = new List<float[]>();
                 foreach (var s in spectra)
                     targetPeaks.Add(new float[] { (float)s[0], (float)s[1] });
                 return targetPeaks;
             }

             public static List<CorrelDecRes> Execute_particularSpot(ProjectPropertyBean projectPropertyBean, RdamPropertyBean rdamPropertyBean,
                 ObservableCollection<AnalysisFileBean> analysisFileBeanCollection, AnalysisParametersBean analysisParametersBean,
                 AlignmentResultBean alignmentResult, int alignmentID) {

                 var dt = projectPropertyBean.ProjectDate;
                 var dirname = projectPropertyBean.ProjectFolderPath + "\\" + "project_" + dt.ToString("yyyy_MM_dd_HH_mm_ss") + "Tmp";
                 if (!System.IO.Directory.Exists(dirname))
                     System.IO.Directory.CreateDirectory(dirname);

                 var rawPeakListList = new List<PeakListForGrouping>();
                 for (var numDec = 0; numDec < projectPropertyBean.Ms2LevelIdList.Count; numDec++) {
                     rawPeakListList.Add(new PeakListForGrouping(analysisFileBeanCollection.Count));
                 }


                 //for (int i = 0; i < analysisFileBeanCollection.Count; i++) {
                 System.Threading.Tasks.Parallel.For(0, analysisFileBeanCollection.Count, i => {
                     DataStorageLcUtility.SetPeakAreaBeanCollection(analysisFileBeanCollection[i], analysisFileBeanCollection[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);
                     var fileID = rdamPropertyBean.RdamFilePath_RdamFileID[analysisFileBeanCollection[i].AnalysisFilePropertyBean.AnalysisFilePath];
                     var measurementID = rdamPropertyBean.RdamFileContentBeanCollection[fileID].FileID_MeasurementID[analysisFileBeanCollection[i].AnalysisFilePropertyBean.AnalysisFileId];

                     using (var rawDataAccess = new RawDataAccess(analysisFileBeanCollection[i].AnalysisFilePropertyBean.AnalysisFilePath, measurementID, analysisFileBeanCollection[i].RetentionTimeCorrectionBean.PredictedRt)) {

                         var spectrumCollection = DataAccessLcUtility.GetRdamSpectrumCollection(rawDataAccess);

                         var j = alignmentID;
                         var alignedSpotProp = alignmentResult.AlignmentPropertyBeanCollection[j]; ;
                         if (alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID >= 0) {
                             var peakAreaBean = analysisFileBeanCollection[i].PeakAreaBeanCollection[alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID];
                             for (var numDec = 0; numDec < projectPropertyBean.Ms2LevelIdList.Count; numDec++) {
                                 rawPeakListList[numDec].PeakList[i] = getTargetPeaks(spectrumCollection, peakAreaBean.Ms2LevelDatapointNumberList[numDec], 1000);
                             }
                         }
                     }
                     DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(analysisFileBeanCollection[i]);
                 });
                 var counter = 0;
                 foreach (var spot in alignmentResult.AlignmentPropertyBeanCollection[alignmentID].AlignedPeakPropertyBeanCollection) {
                     if (spot.PeakID > 0) counter++;
                 }
                 var outputs2 = new List<CorrelDecRes>();
                 var correlMaxDif = 0.1f; var correlThreshold = 0.4f; var MinCorrelPrecursors = 0.9f; int minCountCoverage = (int)(counter * 0.8f);
                 if (minCountCoverage < 4) { foreach (var i in projectPropertyBean.Ms2LevelIdList) { outputs2.Add(new CorrelDecRes()); } return outputs2; }
                 //System.Threading.Tasks.Parallel.For(0, projectPropertyBean.Ms2LevelIdList.Count, numDec => {
                 for (var numDec = 0; numDec < projectPropertyBean.Ms2LevelIdList.Count; numDec++) {
                     var msGroupList = CreateMsGroupListFile.ExcuteMsGrouping(rawPeakListList[numDec].PeakList, analysisFileBeanCollection.Count, (int)(counter * 0.7), 0.01f);
                     var output = new CorrelDecRes();
                     // output.Peaks = CorrelDecHandler.SingleSpotCorrelationCalculation(msGroupList, analysisFileBeanCollection.Count, alignmentID, alignmentResult.AlignmentPropertyBeanCollection, MinCorrelPrecursors, correlMaxDif, correlThreshold, minCountCoverage, (bool)(numDec == 0));
                     outputs2.Add(output);
                 }
                 return outputs2;
             }
             */
        #endregion

    }

    #region Class for storage; MzIntGroup, PeakListForGrouping, CorrelDec
    public class MzIntGroup
    {
        public int Counter { get; set; }
        public float Mz { get; set; }
        public float MinMz { get; set; }
        public float MaxMz { get; set; }
        public float MedianMz { get; set; }
        public float MinIntensity { get; set; }
        public float MaxIntensity { get; set; }
        public float MedianIntensity { get; set; }
        public float[] MzArr { get; set; }
        public float[] IntArr { get; set; }
        public Dictionary<int, float[]> Peak { get; set; }
        public MzIntGroup() { }
        public MzIntGroup(int files, float mz, float intensity, int index) {
            this.Peak = new Dictionary<int, float[]> {
                { index, new float[] { mz, intensity }}
                };
            this.Mz = mz;
            this.Counter = 1;
        }
    }

    public class PeakListForGrouping
    {
        public List<List<float[]>> PeakList { get; set; }
        public List<MzIntGroup> MzIntGroupList { get; set; }
        public PeakListForGrouping() { }
        public PeakListForGrouping(int num) {
            this.PeakList = new List<List<float[]>>();
            for(var i = 0; i < num; i++) {
                PeakList.Add(new List<float[]>());
            }
        }
    }

    public class CorrDecPeak
    {
        public double Mz { get; set; }
        public double Intensity { get; set; }
        public double Correlation { get; set; }
        public double Count { get; set; }
        public double Stdev { get; set; }
        public double CV { get; set; }
        public double StDevRatio { get; set; }
        public double MaxCorrelatedMS1 { get; set; }
        public double MaxCorrel { get; set; }
        public int Score { get; set; }
        public string Comment { get; set; }
    }


    public class CorrDecResult {
        // [mz, int, freq, correl]
        public List<double[]> Peaks {
            get {
                var tmp = new List<double[]>();
                foreach (var p in PeakMatrix) {
                    tmp.Add(new double[] { p.Mz, p.Intensity });
                }
                return tmp;
            }
        }
        public List<CorrDecPeak> PeakMatrix {set;get;}
        public CorrDecResult() { }
    }
    #endregion

    #region CreateTemporaryFileForMsGrouping
    public class CreateTemporaryFileForMsGrouping
    {
        public CreateTemporaryFileForMsGrouping() { }

        public static int versionNum = 1;

        #region // writer for peak info
        public static void WriteMsPeakList(List<List<float[]>> peaklistlist, ProjectPropertyBean projectPropertyBean, string filename) {
            var seekPointer = new List<long>();
            var totalAlignedSpotNum = peaklistlist.Count;

            var dt = projectPropertyBean.ProjectDate;
            if (System.IO.File.Exists(filename))
                System.IO.File.Delete(filename);

            using (var fs = File.Open(filename, FileMode.Create, FileAccess.ReadWrite)) {
                //first header
                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes('V'), 0, 2);
                fs.Write(BitConverter.GetBytes(versionNum), 0, 4);

                //second header
                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes(totalAlignedSpotNum), 0, 4);

                //third header
                var buffer = new byte[totalAlignedSpotNum * 8];
                seekPointer.Add(fs.Position);
                fs.Write(buffer, 0, buffer.Length);

                //from forth header
                WritePeakTmp(fs, seekPointer, peaklistlist);

                //Finalize
                fs.Seek(seekPointer[2], SeekOrigin.Begin);
                for (int i = 3; i < seekPointer.Count; i++) { // Debug.Write(seekPointer[i] + " ");
                    fs.Write(BitConverter.GetBytes(seekPointer[i]), 0, 8);
                }
            }
        }

        public static void WritePeakTmp(FileStream fs, List<long> seekPointer, List<List<float[]>> peaklistlist) {
            for (int i = 0; i < peaklistlist.Count; i++) {
                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes((int)peaklistlist[i].Count), 0, 4);
                for (int k = 0; k < peaklistlist[i].Count; k++) {
                    fs.Write(BitConverter.GetBytes(peaklistlist[i][k][0]), 0, 4); // Rt time
                    fs.Write(BitConverter.GetBytes(peaklistlist[i][k][1]), 0, 4); // Intensity
                }
            }
        }
        #endregion

        #region// reader
        public static List<float[]> ReadPeaklistlist(FileStream fs, List<long> seekpointList, int peakID) {
            var buffer = new byte[6];
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(buffer, 0, 6);
            return GetPeaklistlistVer1(fs, seekpointList, peakID);
        }
        private static List<float[]> GetPeaklistlistVer1(FileStream fs, List<long> seekpointList, int peakID) {
            var peaks = new List<float[]>();
            fs.Seek(seekpointList[peakID], SeekOrigin.Begin); // go to the target seek point
            var buffer = new byte[4];
            fs.Read(buffer, 0, buffer.Length); // store the meta field buffer
            var numPeaks = BitConverter.ToInt32(buffer, 0);

            buffer = new byte[(int)(numPeaks * 8)];
            fs.Read(buffer, 0, buffer.Length);
            for (int j = 0; j < numPeaks; j++) {
                var mz = BitConverter.ToSingle(buffer, 8 * j);
                var Int = BitConverter.ToSingle(buffer, 8 * j + 4);
                peaks.Add(new float[] { mz, Int});
            }
            return peaks;
        }

        public static List<long> ReadSeekPointsOfPeaklistlist(FileStream fs) {
            var buffer = new byte[6];
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(buffer, 0, 6);
            return GetSeekpointListForPeaklistVer1(fs);
        }

        private static List<long> GetSeekpointListForPeaklistVer1(FileStream fs) {
            var seekpointList = new List<long>();
            int totalPeakNumber;
            byte[] buffer = new byte[4];

            fs.Seek(6, SeekOrigin.Begin);
            fs.Read(buffer, 0, 4);

            totalPeakNumber = BitConverter.ToInt32(buffer, 0);
            buffer = new byte[8 * totalPeakNumber];
            fs.Read(buffer, 0, buffer.Length);
            for (int i = 0; i < totalPeakNumber; i++) {
                seekpointList.Add((long)BitConverter.ToInt64(buffer, 8 * i));
            }
            return seekpointList;
        }
        #endregion
    }
    #endregion


    public class CorrDecHandler
    {
        public static int versionNum = 5;

        #region Writer
        public static void WriteCorrelationDecRes(AnalysisParamOfMsdialCorrDec analysisParamOfMsdialCorrDec, ProjectPropertyBean projectProperty, ObservableCollection<AlignmentPropertyBean> alignmentResult, int numFiles, string filePath, string decFilePath, Action<float> reportAction = null) {
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
            ExcuteCorrelDec(analysisParamOfMsdialCorrDec, filePath, decFilePath, alignmentResult, numFiles, reportAction);
        }

        private static void ExcuteCorrelDec(AnalysisParamOfMsdialCorrDec analysisParamOfMsdialCorrDec, string FilePath, string decFilePath, ObservableCollection<AlignmentPropertyBean> alignmentResult, int numFiles, Action<float> reportAction = null) {
            var seekPointer = new List<long>();
            var totalAlignedSpotNum = alignmentResult.Count();
            using (var fs = File.Open(FilePath, FileMode.Create, FileAccess.ReadWrite)) {
                //first header
                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes('V'), 0, 2);
                fs.Write(BitConverter.GetBytes(versionNum), 0, 4);

                //second header
                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes(totalAlignedSpotNum), 0, 4);

                //third header
                var buffer = new byte[totalAlignedSpotNum * 8];
                seekPointer.Add(fs.Position);
                fs.Write(buffer, 0, buffer.Length);

                //from forth header
                WriteCorrelationDec(analysisParamOfMsdialCorrDec, fs, seekPointer, alignmentResult, decFilePath, totalAlignedSpotNum, numFiles, reportAction);

                //Finalize
                fs.Seek(seekPointer[2], SeekOrigin.Begin);
                for (int i = 3; i < seekPointer.Count; i++) {
                    fs.Write(BitConverter.GetBytes(seekPointer[i]), 0, 8);
                }
            }
        }

        private static void WriteCorrelationDec(AnalysisParamOfMsdialCorrDec analysisParamOfMsdialCorrDec, FileStream fs, List<long> seekPointer, ObservableCollection<AlignmentPropertyBean> alignmentResult, string decFilePath, int numAlignment, int numFiles, Action<float> reportAction = null) {
            var correlMatrixRes = GetCorrelMatrixList(alignmentResult, analysisParamOfMsdialCorrDec, decFilePath, numAlignment, numFiles, reportAction);
            CheckCoelutedPeaks(correlMatrixRes, alignmentResult, analysisParamOfMsdialCorrDec);
            for (var id = 0; id < numAlignment; id++) {
                seekPointer.Add(fs.Position);
                var CorrelTable = correlMatrixRes[id];
                var numPeaks = CorrelTable.Count;
                if (numPeaks > 0) {
                    var newCorrelTable = CorrelTable.Where(x => x.Correlation > -1).ToList();
                    numPeaks = newCorrelTable.Count;
                    if (numPeaks > 0) {
                        fs.Write(BitConverter.GetBytes(numPeaks), 0, 4);
                        foreach (var peaks in newCorrelTable) {
                            fs.Write(BitConverter.GetBytes((float)peaks.Mz), 0, 4);
                            fs.Write(BitConverter.GetBytes((float)peaks.Intensity), 0, 4);
                            fs.Write(BitConverter.GetBytes((float)peaks.Correlation), 0, 4);
                            fs.Write(BitConverter.GetBytes((float)peaks.Count), 0, 4);
                            fs.Write(BitConverter.GetBytes((float)peaks.StDevRatio), 0, 4);
                            fs.Write(BitConverter.GetBytes((float)peaks.Stdev), 0, 4);
                            fs.Write(BitConverter.GetBytes((float)peaks.MaxCorrel), 0, 4);
                            fs.Write(BitConverter.GetBytes((float)peaks.MaxCorrelatedMS1), 0, 4);
                            fs.Write(BitConverter.GetBytes(peaks.Score), 0, 4);
                        }
                    }
                    else {
                        fs.Write(BitConverter.GetBytes(0), 0, 4);
                    }
                }
                else
                    fs.Write(BitConverter.GetBytes(0), 0, 4);
            }
        }
        #endregion

        #region Reader

        public static CorrDecResult ReadCorrelDecResult(FileStream fs, List<long> seekpointList, int peakID) {
            var buffer = new byte[6];
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(buffer, 0, 6);
            if (BitConverter.ToChar(buffer, 0).Equals('V') && BitConverter.ToInt32(buffer, 2) == 5)
                return GetCorrelDecVer5(fs, seekpointList, peakID);
            else if (BitConverter.ToChar(buffer, 0).Equals('V') && BitConverter.ToInt32(buffer, 2) == 4)
                return GetCorrelDecVer4(fs, seekpointList, peakID);
            else
                return GetCorrelDecVer3(fs, seekpointList, peakID);
        }

        private static CorrDecResult GetCorrelDecVer5(FileStream fs, List<long> seekpointList, int peakID) {

            var CorrelDec = new CorrDecResult();
            fs.Seek(seekpointList[peakID], SeekOrigin.Begin); // go to the target seek point

            var buffer = new byte[4];
            fs.Read(buffer, 0, buffer.Length);
            var numPeaks = BitConverter.ToInt32(buffer, 0);
            //    Debug.Write("numPeaks: " + numPeaks);
            buffer = new byte[numPeaks * 36];
            fs.Read(buffer, 0, buffer.Length);
            CorrelDec.PeakMatrix = new List<CorrDecPeak>();
            for (int j = 0; j < numPeaks; j++) {
                var mz = BitConverter.ToSingle(buffer, 36 * j);
                var relInt = BitConverter.ToSingle(buffer, 36 * j + 4);
                var correlation = BitConverter.ToSingle(buffer, 36 * j + 8);
                var counter = BitConverter.ToSingle(buffer, 36 * j + 12);
                var stdRatio = BitConverter.ToSingle(buffer, 36 * j + 16);
                var stdev = BitConverter.ToSingle(buffer, 36 * j + 20);
                var maxCorrel = BitConverter.ToSingle(buffer, 36 * j + 24);
                var maxMz = BitConverter.ToSingle(buffer, 36 * j + 28);
                var score = BitConverter.ToInt32(buffer, 36 * j + 32);
                CorrelDec.PeakMatrix.Add(new CorrDecPeak() { Mz = mz, Intensity = relInt, Correlation = correlation, Count = counter, StDevRatio = stdRatio, Score = score, Stdev = stdev, MaxCorrelatedMS1 = maxMz, MaxCorrel = maxCorrel});
            }

            return CorrelDec;
        }

        private static CorrDecResult GetCorrelDecVer4(FileStream fs, List<long> seekpointList, int peakID) {

            var CorrelDec = new CorrDecResult();
            fs.Seek(seekpointList[peakID], SeekOrigin.Begin); // go to the target seek point

            var buffer = new byte[4];
            fs.Read(buffer, 0, buffer.Length);
            var numPeaks = BitConverter.ToInt32(buffer, 0);
            //    Debug.Write("numPeaks: " + numPeaks);
            buffer = new byte[numPeaks * 24];
            fs.Read(buffer, 0, buffer.Length);
            CorrelDec.PeakMatrix = new List<CorrDecPeak>();
            for (int j = 0; j < numPeaks; j++) {
                var mz = BitConverter.ToSingle(buffer, 24 * j);
                var relInt = BitConverter.ToSingle(buffer, 24 * j + 4);
                var correlation = BitConverter.ToSingle(buffer, 24 * j + 8);
                var counter = BitConverter.ToSingle(buffer, 24 * j + 12);
                var stdRatio = BitConverter.ToSingle(buffer, 24 * j + 16);
                var score = BitConverter.ToInt32(buffer, 24 * j + 20);
                CorrelDec.PeakMatrix.Add(new CorrDecPeak() { Mz = mz, Intensity = relInt, Correlation = correlation, Count = counter, StDevRatio = stdRatio, Score = score });
            }

            return CorrelDec;
        }

        private static CorrDecResult GetCorrelDecVer3(FileStream fs, List<long> seekpointList, int peakID) {

            var CorrelDec = new CorrDecResult();
            fs.Seek(seekpointList[peakID], SeekOrigin.Begin); // go to the target seek point

            var buffer = new byte[4];
            fs.Read(buffer, 0, buffer.Length);
            var numPeaks = BitConverter.ToInt32(buffer, 0);
            //    Debug.Write("numPeaks: " + numPeaks);
            buffer = new byte[numPeaks * 20];
            fs.Read(buffer, 0, buffer.Length);
            CorrelDec.PeakMatrix = new List<CorrDecPeak>();
            for (int j = 0; j < numPeaks; j++) {
                var mz = BitConverter.ToSingle(buffer, 20 * j);
                var relInt = BitConverter.ToSingle(buffer, 20 * j + 4);
                var correlation = BitConverter.ToSingle(buffer, 20 * j + 8);
                var counter = BitConverter.ToSingle(buffer, 20 * j + 12);
                var score = BitConverter.ToInt32(buffer, 20 * j + 16);
                CorrelDec.PeakMatrix.Add(new CorrDecPeak() { Mz = mz, Intensity = relInt, Correlation = correlation, Count = counter, Score = score });
            }
            return CorrelDec;
        }
        public static List<long> ReadSeekPointsOfCorrelDec(FileStream fs) {
            var buffer = new byte[6];
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(buffer, 0, 6);

            if (BitConverter.ToChar(buffer, 0).Equals('V') && (BitConverter.ToInt32(buffer, 2) == 1))
                return GetSeekpointListVer1(fs);
            else
                return GetSeekpointListVer1(fs);
        }

        private static List<long> GetSeekpointListVer1(FileStream fs) {
            var seekpointList = new List<long>();
            int totalPeakNumber;
            byte[] buffer = new byte[4];

            fs.Seek(6, SeekOrigin.Begin);
            fs.Read(buffer, 0, 4);

            totalPeakNumber = BitConverter.ToInt32(buffer, 0);
            Debug.WriteLine("seekPoint: " + totalPeakNumber + "total" + totalPeakNumber);
            buffer = new byte[8 * totalPeakNumber];
            fs.Read(buffer, 0, buffer.Length);
            for (int i = 0; i < totalPeakNumber; i++) {
                seekpointList.Add((long)BitConverter.ToInt64(buffer, 8 * i));
                //    Debug.Write(seekpointList[i] + " ");
            }
            return seekpointList;
        }

        #endregion

        #region Main Program
        public static List<List<CorrDecPeak>> GetCorrelMatrixList(ObservableCollection<AlignmentPropertyBean> alignmentResult, AnalysisParamOfMsdialCorrDec analysisParamOfMsdialCorrDec, string decFilePath, int numAlignment, int numFiles, Action<float> reportAction = null) {
            var correlMaxDif_MS1 = analysisParamOfMsdialCorrDec.CorrDiff_MS1;
            var correlMaxDif_MS2 = analysisParamOfMsdialCorrDec.CorrDiff_MS2;
            var correlThreshold = analysisParamOfMsdialCorrDec.MinCorr_MS2;
            var minCorrelPrecursors = analysisParamOfMsdialCorrDec.MinCorr_MS1;
            int minCountCoverage = (int)(analysisParamOfMsdialCorrDec.MinDetectedPercentToVisualize * numFiles);
            int minNumSamples = analysisParamOfMsdialCorrDec.MinNumberOfSample;
            var stepSize = numAlignment / 100 + 1;

            var correlMatrixList = new List<List<CorrDecPeak>>();
            using (var fs = File.Open(decFilePath, FileMode.Open, FileAccess.ReadWrite)) {
                var sp = CreateMzIntGroupListFile.ReadSeekPointsOfMsGroup(fs);
                for (var id = 0; id < numAlignment; id++) {
                    var msGroupRes = CreateMzIntGroupListFile.ReadMsGroupingResults(fs, sp, id);
                    var tmp = SingleSpotCorrelationCalculation(msGroupRes, numFiles, id, alignmentResult, minCorrelPrecursors, correlMaxDif_MS1, correlMaxDif_MS2, correlThreshold, minNumSamples, minCountCoverage, analysisParamOfMsdialCorrDec.RemoveAfterPrecursor);
                    correlMatrixList.Add(tmp);
                    if (id % stepSize == 0) reportAction?.Invoke(1f);
                }
            }
            return correlMatrixList;
        }

        public static void CheckCoelutedPeaks(List<List<CorrDecPeak>> originalTable, ObservableCollection<AlignmentPropertyBean> alignmentResult, AnalysisParamOfMsdialCorrDec analysisParamOfMsdialCorrDec)
        {
            var dataMatrix = new Dictionary<double, Dictionary<int, double>>();
            var mzValues = new List<double>();
            var j = 0;
            foreach(var table in originalTable)
            {
                foreach(var peak in table)
                {
                    if (peak.Score <= 1) continue;
                    var checker = -1.0;
                    foreach(var mz in mzValues)
                    {
                        if (Math.Abs(mz - peak.Mz) < analysisParamOfMsdialCorrDec.MS2Tolerance)
                        {
                            checker = mz;
                            break;
                        }
                    }
                    if (checker > 0)
                    {
                        if (dataMatrix[checker].ContainsKey(j))
                        {
                            if(dataMatrix[checker][j] < peak.Correlation)
                            {
                                dataMatrix[checker][j] = peak.Correlation;
                            }
                            Console.WriteLine(j + " has a two values for the peak " + checker);
                        }
                        else
                        {
                            dataMatrix[checker].Add(j, peak.Correlation);
                            peak.MaxCorrelatedMS1 = checker;
                        }
                    }
                    else
                    {
                        var tmp = new Dictionary<int, double>();
                        tmp.Add(j, peak.Correlation);
                        dataMatrix.Add(peak.Mz, tmp);
                        mzValues.Add(peak.Mz);
                        peak.MaxCorrelatedMS1 = peak.Mz;
                    }
                }
                j++;
            }

            for (var i = 0; i < alignmentResult.Count; i++)
            {
                var table = originalTable[i];
                var spot = alignmentResult[i];
                foreach (var peak in table) {
                    if (peak.Score <= 1) continue;
                    Dictionary<int, double> tmp;
                    if (peak.MaxCorrelatedMS1 > 0)
                    {
                        tmp = dataMatrix[peak.MaxCorrelatedMS1];
                    }
                    else
                    {
                        var checker = -1.0;
                        foreach (var mz in mzValues)
                        {
                            if (Math.Abs(mz - peak.Mz) < analysisParamOfMsdialCorrDec.MS2Tolerance)
                            {
                                checker = mz;
                                break;
                            }
                        }
                        tmp = dataMatrix[checker];
                    }
                    var correlationhreshold = peak.Correlation + analysisParamOfMsdialCorrDec.CorrDiff_MS1;
                    foreach(var p in tmp)
                    {
                        if(p.Value > correlationhreshold)
                        {
                            if(Math.Abs(alignmentResult[p.Key].CentralRetentionTime - spot.CentralRetentionTime) < 1)
                            {
                                peak.Score = 2;
                                peak.MaxCorrel = p.Value;
                                peak.MaxCorrelatedMS1 = alignmentResult[p.Key].CentralAccurateMass;
                                break;
                            }
                        }
                    }
                }
            }
        }

        public static List<CorrDecPeak> SingleSpotCorrelationCalculation(List<MzIntGroup> msGroupRes, int numFiles, int alignmentId,
           ObservableCollection<AlignmentPropertyBean> alignmentResult, float MinCorrelPrecursors, float correlMaxDif_MS1, float correlMaxDif_MS2, float correlThreshold, int minNumSamples, int minCount, bool removeOverPrecursor)
        {
            var alignedSpot = alignmentResult[alignmentId];
            minCount = Math.Max(3, (int)(alignedSpot.FillParcentage * minCount));
            var Ms2Dict = CreateMsGroupDictionary(msGroupRes, numFiles, minNumSamples);
            if (Ms2Dict.Count == 0)
                return new List<CorrDecPeak>();

            var targetInt = alignedSpot.AlignedPeakPropertyBeanCollection.Select(x => (double)x.Variable).ToArray();
            for (var i = 0; i < alignedSpot.AlignedPeakPropertyBeanCollection.Count; i++)
            {
                if (!alignedSpot.AlignedPeakPropertyBeanCollection[i].IsManuallyModified && alignedSpot.AlignedPeakPropertyBeanCollection[i].PeakID < 0)
                {
                    targetInt[i] = 0;
                }
            }
            var correlations = CalcCorrelationDictArray(Ms2Dict, targetInt);
            var maxCorrel = correlations.Max();

            var outputs = new List<CorrDecPeak>();
            var j = 0;
            foreach (var res in Ms2Dict)
            {
                var output = new CorrDecPeak();
                var values = new List<double>();

                var key = res.Key;
                if (removeOverPrecursor && alignmentResult[alignmentId].CentralAccurateMass + 0.1 < key)
                {
                    j++;
                    continue;
                }

                var arr2 = Ms2Dict[key];
                var arr = targetInt;
                for (var i = 0; i < arr.Length; i++)
                {
                    var val = arr[i] > 0 ? Math.Round(arr2[i] / arr[i], 3) : 0;
                    if (val > 0) values.Add(val);
                }
                if (values.Count > 0)
                {
                    output.Mz = key;
                    output.Intensity = BasicMathematics.Median(values.ToArray());
                    output.Correlation = correlations[j];
                    output.Count = values.Count;
                    output.StDevRatio = getStdRatio(arr, arr2, out var std);
                    output.Stdev = std;
                    if (values.Count >= minCount)
                    {
                        output.Score = GetScore(correlations[j], maxCorrel, arr2, MinCorrelPrecursors, correlMaxDif_MS1, correlMaxDif_MS2, correlThreshold);
                    }
                    else
                    {
                        output.Score = 0;
                    }
                    outputs.Add(output);
                }
                j++;
            }
            if (outputs.Count > 0)
            {
                var maxScore = outputs.Max(x => x.Score);
                var scoreCutoff = maxScore > 3 ? 3 : maxScore;
                var maxIon = outputs.Where(x => x.Score >= scoreCutoff).Max(x => x.Intensity);
                foreach (var v in outputs) { v.Intensity = ((double)v.Intensity / (double)maxIon) * 100; }
            }
            return outputs.OrderBy(x => x.Mz).ToList();
        }
        public static List<CorrDecPeak> SingleSpotCorrelationCalculationOld(List<MzIntGroup> msGroupRes, int numFiles, int alignmentId,
            ObservableCollection<AlignmentPropertyBean> alignmentResult, float MinCorrelPrecursors, float correlMaxDif_MS1, float correlMaxDif_MS2, float correlThreshold, int minNumSamples, int minCount, bool removeOverPrecursor) {

            var alignedSpot = alignmentResult[alignmentId];
            minCount = Math.Max(3, (int)(alignedSpot.FillParcentage * minCount));
            var Ms2Dict = CreateMsGroupDictionary(msGroupRes, numFiles, minNumSamples);
            if (Ms2Dict.Count == 0)
                return new List<CorrDecPeak>();

            var targetInt = alignedSpot.AlignedPeakPropertyBeanCollection.OrderBy(x => x.FileID).Select(x => (double)x.Variable).ToArray();
            for (var i = 0; i < alignedSpot.AlignedPeakPropertyBeanCollection.Count; i++) {
                if (!alignedSpot.AlignedPeakPropertyBeanCollection[i].IsManuallyModified && alignedSpot.AlignedPeakPropertyBeanCollection[i].PeakID < 0) {
                    targetInt[i] = 0;
                }
            }
            var correlations = CalcCorrelationDictArray(Ms2Dict, targetInt);
            var maxCorrel = correlations.Max();
            int targetId = 0;
            var aroundPeakSpotIntensityList = GetMs1Correlation(alignedSpot, alignmentResult, MinCorrelPrecursors, ref targetId, out var targetAlignmentList);


            var outputs = new List<CorrDecPeak>();
            var j = 0;
            foreach (var res in Ms2Dict) {
                var output = new CorrDecPeak();
                var values = new List<double>();

                var key = res.Key;
                if (removeOverPrecursor && alignmentResult[alignmentId].CentralAccurateMass + 0.1 < key) {
                    j++;
                    continue;
                }

                var arr2 = Ms2Dict[key];
                var arr = targetInt;
                for (var i = 0; i < arr.Length; i++) {
                    var val = arr[i] > 0 ? Math.Round(arr2[i] / arr[i], 3) : 0;
                    if (val > 0) values.Add(val);
                }
                if (values.Count > 0) {
                    output.Mz = key;
                    output.Intensity = BasicMathematics.Median(values.ToArray());
                    output.Correlation = correlations[j];
                    output.Count = values.Count;
                    output.StDevRatio = getStdRatio(arr, arr2, out var std);
                    output.Stdev = std;
                    if (values.Count >= minCount) {
                        output.Score = GetScore(correlations[j], maxCorrel, targetAlignmentList, aroundPeakSpotIntensityList, arr2, MinCorrelPrecursors, correlMaxDif_MS1, correlMaxDif_MS2, correlThreshold, out var maxMz, out var maxMS2Correl);
                        output.MaxCorrelatedMS1 = maxMz;
                        output.MaxCorrel = maxMS2Correl;
                    }
                    else {
                        output.Score = 0;
                        output.MaxCorrelatedMS1 = -1;
                        output.MaxCorrel = -1;
                    }
                    outputs.Add(output);
                }
                j++;
            }
            if (outputs.Count > 0) {
                var maxScore = outputs.Max(x => x.Score);
                var scoreCutoff = maxScore > 3 ? 3 : maxScore;
                var maxIon = outputs.Where(x => x.Score >= scoreCutoff).Max(x => x.Intensity);
                foreach (var v in outputs) { v.Intensity = ((double)v.Intensity / (double)maxIon) * 100; }
            }
            return outputs.OrderBy(x => x.Mz).ToList();
        }

        public static Dictionary<double, double[]> CreateMsGroupDictionary(List<MzIntGroup> msGroupListIn, int numFiles, int minCount) {
            var newDic = new Dictionary<double, double[]>();
            foreach (var msGroup in msGroupListIn) {
                if (msGroup.Counter >= minCount) {

                    var reslist = new double[numFiles];
                    for (var i = 0; i < numFiles; i++) {
                        if (!msGroup.Peak.ContainsKey(i)) reslist[i] = 0;
                        else reslist[i] = msGroup.Peak[i][1];
                    }
                    if (newDic.ContainsKey(msGroup.MedianMz)) {
                        var pre = newDic[msGroup.MedianMz];
                        var newlist = new double[pre.Length];
                        for (var i = 0; i < pre.Length; i++)
                            newlist[i] = pre[i] + reslist[i];
                        newDic[msGroup.MedianMz] = newlist;
                    }
                    else
                        newDic.Add(msGroup.MedianMz, reslist);
                }
            }
            return newDic;
        }


        // calculate the ratio of standard deviation (MS2 peaks/MS1 peaks)
        private static double getStdRatio(double[] ms1int, double[] ms2int, out double ms2std) {
            double stdRatio = 0;
            ms2std = 0.0;
            var xlist = new List<double>();
            var ylist = new List<double>();
            for (var i = 0; i < ms1int.Length; i++) {
                if (ms1int[i] > 0 && ms2int[i] > 0) {
                    xlist.Add(ms1int[i]);
                    ylist.Add(ms2int[i]);
                }
            }
            if (xlist.Count < 3) return stdRatio;
            var ms1std = BasicMathematics.Stdev(xlist.ToArray());
            ms2std = BasicMathematics.Stdev(ylist.ToArray());

            if (ms1std == 0) return stdRatio;

            stdRatio = ms2std / ms1std;
            return stdRatio;
        }

        // scoring 0 -> 4
        public static int GetScore(double correlation, double maxCorrel, List<AlignmentPropertyBean> alignmentProperties, List<double[]> aroundPeakSpotIntensityList, 
            double[] ms2Intensities, float minCorrelPrecursors, float correlMaxDif_MS1, float correlMaxDif_MS2, float correlThreshold, out double maxMz, out double maxMS2Correl) {
            int score = -1;
            maxMz = -1;
            maxMS2Correl = -1;
            if (correlation < correlThreshold) {
                return 0; //"0: noise or other fragments, low correlation";
            }
            else if (correlation > 1 - correlMaxDif_MS2) {
                return 4; // "4: fragments";
            }
            else if (correlation < maxCorrel - correlMaxDif_MS2) {
                return 1;
            }
            else {
                for (var i = 0; i < aroundPeakSpotIntensityList.Count; i++) {
                    var corr = CalcCorrelation(ms2Intensities, aroundPeakSpotIntensityList[i]);
                    if(corr > maxMS2Correl) {
                        maxMS2Correl = corr;
                        maxMz = alignmentProperties[i].CentralAccurateMass;
                    }
                }
                if (correlation > maxMS2Correl - correlMaxDif_MS1) {
                    score = 3;// "3: good candidates";
                }
                else {
                    score = 2;  //"2: other fragments, max: " + Math.Round(max, 2);
                }
                return score;
            }
        }

        public static int GetScore(double correlation, double maxCorrel,
            double[] ms2Intensities, float minCorrelPrecursors, float correlMaxDif_MS1, float correlMaxDif_MS2, float correlThreshold)
        {
            int score = -1;
            if (correlation < correlThreshold)
            {
                return 0; //"0: noise or other fragments, low correlation";
            }
            else if (correlation > 1 - correlMaxDif_MS2)
            {
                return 4; // "4: fragments";
            }
            else if (correlation < maxCorrel - correlMaxDif_MS2)
            {
                return 1;
            }
            else
            {
                score = 3;  //"2: other fragments, max: " + Math.Round(max, 2);
            }
            return score;            
        }


        public static double[] CalcCorrelationDictArray(Dictionary<double, double[]> dict, double[] arr) {
            var resultArr = new double[dict.Keys.Count];
            int i = 0;
            var tmpList1 = new List<double>(); var tmpList2 = new List<double>();
            foreach (var key in dict.Keys) {
                var dic1values = dict[key];
                // remove 0 values; currently they are in CalcCorrelation
                /*var list = arr.Select((p, k) => new { Content = p, Index = k })
                    .Where(ano => ano.Content != 0)
                    .Select(ano => ano.Index).ToList();
                var in1 = new double[list.Count];
                var in2 = new double[list.Count];
                var j = 0;
                foreach (var index in list) {
                    in1[j] = dic1values[index];
                    in2[j] = arr[index];
                    j++;
                }
                */
                resultArr[i] = CalcCorrelation(arr, dic1values);
                i++;

            }
            return resultArr;
        }

        public static double CalcCorrelation(double[] xpre, double[] ypre) {
            double sum1 = 0, sum2 = 0, mean1 = 0, mean2 = 0, covariance = 0, sqrt1 = 0, sqrt2 = 0, correlation = 0;

            // filtering 
            var xlist = new List<double>();
            var ylist = new List<double>();
            for (var i = 0; i < xpre.Length; i++) {
                if (xpre[i] > 0 && ypre[i] > 0) {
                    xlist.Add(xpre[i]);
                    ylist.Add(ypre[i]);
                }
            }
            var x = xlist.ToArray();
            var y = ylist.ToArray();

            if (x.Length < 4) return correlation;
            if (x.Length != y.Length) { Debug.WriteLine("miss-match, x.Length = " + x.Length + ", y.Length = " + y.Length); return correlation; }
            var sampleCount = x.Length;
            for (int i = 0; i < sampleCount; i++) {
                sum1 += x[i];
                sum2 += y[i];
            }
            mean1 = (double)(sum1 / sampleCount);
            mean2 = (double)(sum2 / sampleCount);

            for (int i = 0; i < sampleCount; i++) {
                covariance += (x[i] - mean1) * (y[i] - mean2);
                sqrt1 += Math.Pow(x[i] - mean1, 2);
                sqrt2 += Math.Pow(y[i] - mean2, 2);
            }
            if (sqrt1 == 0 || sqrt2 == 0) return correlation;
            else
                correlation = (double)(covariance / Math.Sqrt(sqrt1 * sqrt2));

            return correlation;
        }


            #region MS1 correlation check; to detect related peaks

        public static List<double[]> GetMs1Correlation(AlignmentPropertyBean alignedSpot, ObservableCollection<AlignmentPropertyBean> alignmentResult, float MinCorrelPrecursors, ref int targetId, out List<AlignmentPropertyBean> output) {
            var ms1correl = new List<double[]>();
            var targetAlignmentResult = new List<AlignmentPropertyBean>();
            output = targetAlignmentResult;

            var targetPrecursorIntensityList = new List<double[]>();
            var remListByHighCorrel = new List<AlignmentPropertyBean>(); var remListCorrel = new List<double>();

            var targetAlignmentResultTmp = GetAroundPeakSpot(alignedSpot, alignmentResult);
            if (targetAlignmentResultTmp == null || targetAlignmentResultTmp.Count == 0) return new List<double[]>();
            var targetPrecursorIntensityListTmp = GetAroundPeakSpotIntensity(targetAlignmentResultTmp);

            var actualPrecursorIndex = CheckActualPrecursorIndex(targetAlignmentResultTmp, alignedSpot.AlignmentID);

            for (var i = 0; i < targetAlignmentResultTmp.Count; i++) {
                var cor = CalcCorrelation(targetPrecursorIntensityListTmp[i], targetPrecursorIntensityListTmp[actualPrecursorIndex]);
                ms1correl.Add(new double[] { targetAlignmentResultTmp[i].CentralAccurateMass, cor });
                if (i == actualPrecursorIndex) { targetAlignmentResult.Add(targetAlignmentResultTmp[i]); targetPrecursorIntensityList.Add(targetPrecursorIntensityListTmp[i]); continue; }
                if (cor < MinCorrelPrecursors) { targetAlignmentResult.Add(targetAlignmentResultTmp[i]); targetPrecursorIntensityList.Add(targetPrecursorIntensityListTmp[i]); }
                else {
                    Debug.WriteLine("remove at: " + targetAlignmentResultTmp[i].CentralAccurateMass + " " + cor);
                    remListByHighCorrel.Add(targetAlignmentResultTmp[i]); remListCorrel.Add(cor);
                }
            }

            targetId = CheckActualPrecursorIndex(targetAlignmentResult, alignedSpot.AlignmentID);
            output = targetAlignmentResult;
            return targetPrecursorIntensityList;
        }

        private static int CheckActualPrecursorIndex(List<AlignmentPropertyBean> alignSpots, int id) {
            int index = 0;
            foreach (var tag in alignSpots) { if (tag.AlignmentID == id) { break; } else { index++; } }
            return index;
        }

        public static List<AlignmentPropertyBean> GetAroundPeakSpot(AlignmentPropertyBean alignedSpot, ObservableCollection<AlignmentPropertyBean> alignmentResult) {
            var result = new List<AlignmentPropertyBean>();
            var rtTol = alignedSpot.AveragePeakWidth;
            var output = new List<double[]>();
            var numFiles = alignedSpot.AlignedPeakPropertyBeanCollection.Count;
            var rt = alignedSpot.CentralRetentionTime;
            var rtS = alignedSpot.MinRt - rtTol;
            var rtE = alignedSpot.MaxRt + rtTol;

            foreach (var target in alignmentResult) {
                if (target.CentralRetentionTime < rtS) continue;
                if (target.CentralRetentionTime > rtE) continue;
                result.Add(target);
            }

            return result;
        }
        public static List<double[]> GetAroundPeakSpotIntensity(List<AlignmentPropertyBean> result) {
            var output = new List<double[]>();

            var numFiles = result[0].AlignedPeakPropertyBeanCollection.Count;
            foreach (var spot in result) {
                double[] precursorInt = new double[numFiles];
                for (var i = 0; i < numFiles; i++)
                    if (spot.AlignedPeakPropertyBeanCollection[i].PeakID > -1)
                        precursorInt[i] = spot.AlignedPeakPropertyBeanCollection[i].Variable;
                    else
                        precursorInt[i] = 0;
                output.Add(precursorInt);
            }

            return output;
        }

        #endregion

        public static List<double[]> GetCorrDecSpectrum(CorrDecResult res, AnalysisParamOfMsdialCorrDec param, float precursorMz, int numDetectedSamples) {
            var spectrum = new List<double[]>();
            var spectrumFiltered = new List<double[]>();
            if (res.PeakMatrix == null || res.PeakMatrix.Count == 0) return spectrum;

            var maxCorrel = res.PeakMatrix.Max(x => x.Correlation);
            var maxPeakCount = res.PeakMatrix.Max(x => x.Count);
            if(param == null)
            {
                param = new AnalysisParamOfMsdialCorrDec();
            }
            foreach(var peak in res.PeakMatrix) {
                if (param.RemoveAfterPrecursor && peak.Mz > precursorMz + 0.5) continue;
                if (peak.Correlation < param.MinCorr_MS2) continue;
                if (peak.Correlation < maxCorrel - param.CorrDiff_MS1) continue;
                if (precursorMz != peak.MaxCorrelatedMS1 && peak.Correlation < peak.MaxCorrel - param.CorrDiff_MS2) continue;
                if (peak.Count < param.MinDetectedPercentToVisualize * maxPeakCount) continue;
                spectrum.Add(new double[] { peak.Mz, peak.Intensity });
            }
            if (spectrum.Count > 0) {
                var maxIntensity = spectrum.Max(x => x[1]);
                foreach(var p in spectrum) {
                    p[1] = (p[1] / maxIntensity) * 100.0;
                    if (p[1] > param.MinMS2RelativeIntensity)
                        spectrumFiltered.Add(p);
                }
            }
            return spectrumFiltered;
        }

        public static List<Peak> GetCorrDecSpectrumWithComment(CorrDecResult res, AnalysisParamOfMsdialCorrDec param, float precursorMz, int numDetectedSamples) {
            var spectrum = new List<Peak>();
            var spectrumFiltered = new List<Peak>();
            if (res.PeakMatrix == null || res.PeakMatrix.Count == 0) return spectrum;

            var maxCorrel = res.PeakMatrix.Max(x => x.Correlation);
            foreach (var peak in res.PeakMatrix) {
                if (param.RemoveAfterPrecursor && peak.Mz > precursorMz + 0.5) continue;
                if (peak.Correlation < param.MinCorr_MS2) continue;
                if (peak.Correlation < maxCorrel - param.CorrDiff_MS1) continue;
                if (precursorMz != peak.MaxCorrelatedMS1 && peak.Correlation < peak.MaxCorrel - param.CorrDiff_MS2) continue;
                if (peak.Count < param.MinDetectedPercentToVisualize * numDetectedSamples) continue;
                spectrum.Add(new Peak { Mz = peak.Mz, Intensity = peak.Intensity, Comment = "r=" + Math.Round(peak.Correlation,2) + " (" + peak.Count + " samples)"  });
            }
            if (spectrum.Count > 0) {
                var maxIntensity = spectrum.Max(x => x.Intensity);
                foreach (var p in spectrum) {
                    p.Intensity = (p.Intensity / maxIntensity) * 100.0;
                    if (p.Intensity > param.MinMS2RelativeIntensity)
                        spectrumFiltered.Add(p);
                }
            }
            return spectrumFiltered;
        }

        #region old programs and utilities
        public static List<double[]> SingleSpotCorrelationCalculation2(string FilePath, int numFiles, int alignmentId,
    ObservableCollection<AlignmentPropertyBean> alignmentResult, float MinCorrelPrecursors, float correlMaxDif, float correlThreshold, int minCount) {
            List<MzIntGroup> msGroupRes;
            using (var fs = File.Open(FilePath, FileMode.Open, FileAccess.ReadWrite)) {
                var sp = CreateMzIntGroupListFile.ReadSeekPointsOfMsGroup(fs);
                msGroupRes = CreateMzIntGroupListFile.ReadMsGroupingResults(fs, sp, alignmentId);
            }
            var alignedSpot = alignmentResult[alignmentId];
            var Ms2Dict = CreateMsGroupDictionary(msGroupRes, numFiles, minCount);
            var targetInt = alignedSpot.AlignedPeakPropertyBeanCollection.Select(x => (double)x.Variable).ToArray();
            for (var i = 0; i < alignedSpot.AlignedPeakPropertyBeanCollection.Count; i++) {
                if (alignedSpot.AlignedPeakPropertyBeanCollection[i].PeakID < 0) {
                    targetInt[i] = 0;
                }
            }
            var correlations = CalcCorrelationDictArray(Ms2Dict, targetInt);
            var outputs = new List<double[]>();
            var j = 0;
            foreach (var res in Ms2Dict) {
                var output = new double[4];
                var values = new List<double>();

                var key = res.Key;
                var arr2 = Ms2Dict[key];
                var arr = targetInt;
                for (var i = 0; i < arr.Length; i++) {
                    var val = arr[i] > 0 ? Math.Round(arr2[i] / arr[i], 3) : 0;
                    if (val > 0) values.Add(val);
                }
                if (values.Count > 0) {
                    output[0] = key;
                    output[1] = BasicMathematics.Median(values.ToArray());
                    output[2] = correlations[j];
                    output[3] = values.Count;
                }
                j++;
                outputs.Add(output);
            }
            if (outputs.Count > 0) {
                var maxIon = outputs.Max(x => x[1]);
                foreach (var v in outputs) { v[1] = ((double)v[1] / (double)maxIon) * 100; }
            }
            return outputs;
        }


        public static List<double[]> SingleSpotCorrelationCalculation(string FilePath, int numFiles, int alignmentId,
            ObservableCollection<AlignmentPropertyBean> alignmentResult, float MinCorrelPrecursors, float correlMaxDif, float correlThreshold, int minCount) {
            List<MzIntGroup> msGroupRes;
            using (var fs = File.Open(FilePath, FileMode.Open, FileAccess.ReadWrite)) {
                var sp = CreateMzIntGroupListFile.ReadSeekPointsOfMsGroup(fs);
                msGroupRes = CreateMzIntGroupListFile.ReadMsGroupingResults(fs, sp, alignmentId);
            }
            var alignedSpot = alignmentResult[alignmentId];
            var Ms2Dict = CreateMsGroupDictionary(msGroupRes, numFiles, minCount);
            var targetId = 0;
            var aroundPeakSpotIntensityList = GetMs1Correlation(alignedSpot, alignmentResult, MinCorrelPrecursors, ref targetId, out var test);
            var correlations = new List<double[]>();
            for (var i = 0; i < aroundPeakSpotIntensityList.Count; i++) {
                correlations.Add(CalcCorrelationDictArray(Ms2Dict, aroundPeakSpotIntensityList[i]));
            }


            var MzCorrelInfoList = new List<double[]>(); var loopCounter = 0;
            while (MzCorrelInfoList.Count == 0) {
                MzCorrelInfoList = GetHighlyCorrelatedValues(Ms2Dict, correlations, aroundPeakSpotIntensityList[targetId], alignedSpot.CentralAccurateMass, targetId, correlMaxDif + loopCounter * 0.01, correlThreshold - loopCounter * 0.05);
                loopCounter++;
                if (loopCounter > 10) break;
            }
            return MzCorrelInfoList;
        }

        public static List<double[]> GetHighlyCorrelatedValues(Dictionary<double, double[]> Ms2Dict, List<double[]> correlations, double[] arr, double mass, int targetId, double correlMaxDif, double correlThreshold) {
            var result = new List<double[]>();
            var mzlist = Ms2Dict.Keys.ToList();

            for (var i = 0; i < correlations[0].Length; i++) {
                var maxVal = 0.0;
                if (Math.Abs(mzlist[i] - mass) < 0.01 && correlations[targetId][i] > correlThreshold)
                    result.Add(new double[] { (double)mzlist[i], correlations[targetId][i] });

                else {
                    for (var j = 0; j < correlations.Count; j++) {
                        if (maxVal < correlations[j][i]) maxVal = correlations[j][i];
                    }
                    if (maxVal < (correlations[targetId][i] + correlMaxDif) && correlations[targetId][i] > correlThreshold) result.Add(new double[] { (double)mzlist[i], correlations[targetId][i] });
                }
            }
            if (result == null || result.Count == 0) return result;

            var outputs = new List<double[]>();
            foreach (var res in result) {
                var output = new double[6];
                var values = new List<double>();

                var key = res[0];
                var arr2 = Ms2Dict[key];
                for (var i = 0; i < arr.Length; i++) {
                    var val = arr[i] > 0 ? Math.Round(arr2[i] / arr[i], 3) : 0;
                    if (val > 0) values.Add(val);
                }
                if (values.Count > 0) {
                    output[0] = res[0];
                    output[1] = BasicMathematics.Median(values.ToArray());
                    output[2] = res[1];
                    output[3] = values.Count;
                }
                outputs.Add(output);
            }
            if (outputs.Count > 0) {
                var maxIon = outputs.Max(x => x[1]);
                foreach (var v in outputs) { v[1] = ((double)v[1] / (double)maxIon) * 100; }
            }
            return outputs;
        }


        //public static void CopyRawData(Dictionary<double, double[]> dict, double[] arr) {
        //    Console.WriteLine("start copy method");
        //    var output = "\t";
        //    output = "\t" + string.Join("\t", arr);
        //    Console.WriteLine("done 1");
        //    output = output + "\n";
        //    foreach (var key in dict.Keys) {
        //        var dic1values = dict[key];
        //        output = output + key + "\t";
        //        output = output + string.Join("\t", dic1values);
        //        output = output + "\n";
        //    }
        //    Console.WriteLine("done all methods");
        //    System.Windows.Clipboard.SetText(output);
        //    Console.WriteLine("done copy");
        //}

        #endregion

        #endregion
    }

    #region CreateMsGroupListFile
    public class CreateMzIntGroupListFile
    {
        public CreateMzIntGroupListFile() { }
        public static int versionNum = 1;

        #region writer 
        public static void WriteMsGroupListList(List<PeakListForGrouping> groupList, string outputFilePath,Action<float> reportAction) {
            var seekPointer = new List<long>();
            var totalAlignedSpotNum = groupList.Count;
            if (System.IO.File.Exists(outputFilePath))
                System.IO.File.Delete(outputFilePath);

            using (var fs = File.Open(outputFilePath, FileMode.Create, FileAccess.ReadWrite)) {
                //first header
                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes('V'), 0, 2);
                fs.Write(BitConverter.GetBytes(versionNum), 0, 4);

                //second header
                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes(totalAlignedSpotNum), 0, 4);

                //third header
                var buffer = new byte[totalAlignedSpotNum * 8];
                seekPointer.Add(fs.Position);
                fs.Write(buffer, 0, buffer.Length);

                //from forth header
                WriteMsGroupRsult(fs, seekPointer, groupList, reportAction);

                //Finalize
                fs.Seek(seekPointer[2], SeekOrigin.Begin);
                for (int i = 3; i < seekPointer.Count; i++) { //Debug.Write(seekPointer[i] + " ");
                    fs.Write(BitConverter.GetBytes(seekPointer[i]), 0, 8);
                }
            }
        }

        public static void WriteMsGroupRsult(FileStream fs, List<long> seekPointer, List<PeakListForGrouping> peakListForGroupings, Action<float> reportAction) {
            for (int i = 0; i < peakListForGroupings.Count; i++) {
                var mzIntGroupList = peakListForGroupings[i].MzIntGroupList;
                var numCount = mzIntGroupList.Count;

                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes((int)numCount), 0, 4); // num mz
                //   Debug.WriteLine("Mz: " + alignedSpotInfo[i][0].ToString() + ", Rt: " + alignedSpotInfo[i][1].ToString() + ", NumScan: " + alignedSpotInfo[i][2].ToString() +
                //      ", StartScan: " + alignedSpotInfo[i][3].ToString() + ", EndScan" + alignedSpotInfo[i][4].ToString());

                for (int k = 0; k < numCount; k++) {
                    fs.Write(BitConverter.GetBytes((int)mzIntGroupList[k].Counter), 0, 4); // Mz count
                    foreach (var key in mzIntGroupList[k].Peak.Keys) {
                        fs.Write(BitConverter.GetBytes((int)key), 0, 4);
                        fs.Write(BitConverter.GetBytes((float)mzIntGroupList[k].Peak[key][0]), 0, 4); // Mz 
                        fs.Write(BitConverter.GetBytes((float)mzIntGroupList[k].Peak[key][1]), 0, 4); // Intensity
                    }
                }
                reportAction?.Invoke(1);
            }
        }

        public static List<PeakListForGrouping> CalcMzIntGroupList(string peakFilePrefix, int numAnalysisFiles, int numAlignment, int minFileNum, float mzTol, float minPeakInt, float minRelativePeakInt, Action<float> reportAction)
        {
            var mzIntListForGrouping = new PeakListForGrouping[numAlignment];
            var fslist = new List<FileStream>();
            var seekpointerList = new List<List<long>>();

            #region open files
            for (int j = 0; j < numAnalysisFiles; j++)
            {
                fslist.Add(File.Open(peakFilePrefix + j + ".pll", FileMode.Open, FileAccess.ReadWrite));
                seekpointerList.Add(CreateTemporaryFileForMsGrouping.ReadSeekPointsOfPeaklistlist(fslist[j]));
            }
            #endregion

            for (int i = 0; i < numAlignment; i++){
                var peaklistlist = getPeakListFromFiles(fslist, seekpointerList, numAnalysisFiles, i);
                mzIntListForGrouping[i] = new PeakListForGrouping();
                mzIntListForGrouping[i].MzIntGroupList = ExcuteMzIntGrouping(peaklistlist, numAnalysisFiles, minFileNum, mzTol, minPeakInt, minRelativePeakInt);
                reportAction?.Invoke(1);
            }

            #region dispose
            for (int i = 0; i < numAnalysisFiles; i++)
            {
                fslist[i].Dispose(); fslist[i].Close();
                seekpointerList[i] = null;
            }
            #endregion
            return mzIntListForGrouping.ToList();
        }


        public static void WriteMsGroupListList(string outputFilePath, string peakFilePrefix, int numAnalysisFiles, int numAlignment, int minFileNum, float mzTol, float minPeakInt, float minRelativePeakInt, Action<float> reportAction)
        {
            var seekPointer = new List<long>();
            var totalAlignedSpotNum = numAlignment;
            if (System.IO.File.Exists(outputFilePath))
                System.IO.File.Delete(outputFilePath);

            using (var fs = File.Open(outputFilePath, FileMode.Create, FileAccess.ReadWrite))
            {
                //first header
                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes('V'), 0, 2);
                fs.Write(BitConverter.GetBytes(versionNum), 0, 4);

                //second header
                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes(totalAlignedSpotNum), 0, 4);

                //third header
                var buffer = new byte[totalAlignedSpotNum * 8];
                seekPointer.Add(fs.Position);
                fs.Write(buffer, 0, buffer.Length);

                //from forth header
                WriteMsGroupRsult(fs, seekPointer, peakFilePrefix, numAnalysisFiles, numAlignment, minFileNum, mzTol, minPeakInt, minRelativePeakInt, reportAction);

                //Finalize
                fs.Seek(seekPointer[2], SeekOrigin.Begin);
                for (int i = 3; i < seekPointer.Count; i++)
                { //Debug.Write(seekPointer[i] + " ");
                    fs.Write(BitConverter.GetBytes(seekPointer[i]), 0, 8);
                }
            }
        }

        public static void WriteMsGroupRsult(FileStream fs, List<long> seekPointer, string peakFilePrefix, int numAnalysisFiles, int numAlignment, int minFileNum, float mzTol, float minPeakInt, float minRelativePeakInt, Action<float> reportAction)
        {
            var fslist = new List<FileStream>();
            var seekpointerList = new List<List<long>>();

            #region open files
            for (int j = 0; j < numAnalysisFiles; j++)
            {
                fslist.Add(File.Open(peakFilePrefix + j + ".pll", FileMode.Open, FileAccess.ReadWrite));
                seekpointerList.Add(CreateTemporaryFileForMsGrouping.ReadSeekPointsOfPeaklistlist(fslist[j]));
            }
            #endregion

            for (int i = 0; i < numAlignment; i++)
            {
                var peaklistlist = getPeakListFromFiles(fslist, seekpointerList, numAnalysisFiles, i);
                var mzIntGroupList = ExcuteMzIntGrouping(peaklistlist, numAnalysisFiles, minFileNum, mzTol, minPeakInt, minRelativePeakInt);
                var numCount = mzIntGroupList.Count;

                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes((int)numCount), 0, 4); // num mz
                //   Debug.WriteLine("Mz: " + alignedSpotInfo[i][0].ToString() + ", Rt: " + alignedSpotInfo[i][1].ToString() + ", NumScan: " + alignedSpotInfo[i][2].ToString() +
                //      ", StartScan: " + alignedSpotInfo[i][3].ToString() + ", EndScan" + alignedSpotInfo[i][4].ToString());

                for (int k = 0; k < numCount; k++)
                {
                    fs.Write(BitConverter.GetBytes((int)mzIntGroupList[k].Counter), 0, 4); // Mz count
                    foreach (var key in mzIntGroupList[k].Peak.Keys)
                    {
                        fs.Write(BitConverter.GetBytes((int)key), 0, 4);
                        fs.Write(BitConverter.GetBytes((float)mzIntGroupList[k].Peak[key][0]), 0, 4); // Mz 
                        fs.Write(BitConverter.GetBytes((float)mzIntGroupList[k].Peak[key][1]), 0, 4); // Intensity
                    }
                }
                reportAction?.Invoke(1);
            }

            #region dispose
            for (int i = 0; i < numAnalysisFiles; i++)
            {
                fslist[i].Dispose(); fslist[i].Close();
                seekpointerList[i] = null;
            }
            #endregion

        }

        private static List<List<float[]>> getPeakListFromFiles(List<FileStream> fslist, List<List<long>> seekpointerList, int numAnalysisFiles, int target) {
            var peaklistlist = new List<List<float[]>>();
            for (int i = 0; i < numAnalysisFiles; i++)
                peaklistlist.Add(CreateTemporaryFileForMsGrouping.ReadPeaklistlist(fslist[i], seekpointerList[i], target));
            return peaklistlist;
        }
        #endregion

        #region For msGrouping
        public static List<MzIntGroup> ExcuteMzIntGrouping(List<List<float[]>> peakList, int numFiles, int minFileNum, float mzTol, float minPeakInt, float minRelativePeakInt) {
            var mzIntGroup = new List<MzIntGroup>();
            var num = peakList.Count;
            // <m/z bin (m/z*10), set(ids)>
            var mzBinIdDic = new Dictionary<int, HashSet<int>>();
            for (var i = 0; i < num; i++) {
                var peaks = peakList[i].OrderBy(x => x[0]).ToList();
                if (peaks.Count == 0) continue;
                var maxInt = peaks.Max(x => x[1]);
                foreach (var peak in peaks) {
                    CheckPeak(mzIntGroup, peak, mzBinIdDic, num, i, maxInt, mzTol, minPeakInt, minRelativePeakInt);
                }
            }
            if (mzIntGroup.Count == 0) return mzIntGroup;
            finalizer(mzIntGroup, minFileNum);
            mzIntGroup = getRefinedMsGroupList(mzIntGroup.OrderBy(x => x.MedianMz).ToList(), numFiles);
            return mzIntGroup;
        }

        public static void CheckPeak(List<MzIntGroup> mzIntGroupList, float[] peak, Dictionary<int, HashSet<int>> mzBinIdDic, int num, int id, float maxIntensity, float mzTol, float minPeakInt, float minRelativePeakInt) {
            var checker = false;
            var relInt = 100 * peak[1] / maxIntensity;
            if (peak[1] < minPeakInt && relInt < minRelativePeakInt) return;
            var mzBin = (int)(peak[0] * 10 + 0.5);
            if (mzBinIdDic.ContainsKey(mzBin))
            {
                foreach (var mzIntGroupId in mzBinIdDic[mzBin])
                {
                    var mzIntGroup = mzIntGroupList[mzIntGroupId];
                    if (Math.Abs(mzIntGroup.Mz - peak[0]) < mzTol)
                    {
                        checker = true;
                        if (mzIntGroup.Peak.ContainsKey(id))
                        {
                            // Debug.WriteLine("there are two peaks with in a MsGroup: " + msGroup.Mz + ", pre: " + msGroup.Peak[id][0] + ", now: " + peak[0] + ", pre: " + msGroup.Peak[id][1] + ", now:" + peak[1]);
                            var mz = peak[1] > mzIntGroup.Peak[id][1] ? peak[0] : mzIntGroup.Peak[id][0];
                            mzIntGroup.Peak[id][1] += peak[1];
                        }
                        else
                        {
                            mzIntGroup.Peak.Add(id, new float[] { peak[0], peak[1] });
                            mzIntGroup.Counter++;
                        }
                        break;
                    }
                }
                if (checker == false)
                {
                    mzIntGroupList.Add(new MzIntGroup(num, peak[0], peak[1], id));
                    mzBinIdDic[mzBin].Add(mzIntGroupList.Count - 1);
                }
            }
            else {
                mzIntGroupList.Add(new MzIntGroup(num, peak[0], peak[1], id));
                mzBinIdDic.Add(mzBin, new HashSet<int>() { mzIntGroupList.Count - 1 });
            }
        }

        private static void finalizer(List<MzIntGroup> mzIntGroupList, int minFileNum) {
            foreach (var msGroup in mzIntGroupList) {
                finalizer(msGroup);
            }
            mzIntGroupList = mzIntGroupList.Where(x => x.Counter > minFileNum).ToList();
            // mzIntGroupList = mzIntGroupList.Where(x => x.MaxIntensity > maxIntensityCutoff).ToList();
        }

        private static void finalizer(List<MzIntGroup> mzIntGroupList) {
            foreach (var msGroup in mzIntGroupList) {
                finalizer(msGroup);
            }
        }



        private static void finalizer(MzIntGroup msGroup) {
            msGroup.MzArr = msGroup.Peak.Values.Select(x => x[0]).ToArray();
            msGroup.IntArr = msGroup.Peak.Values.Select(x => x[1]).ToArray();

            msGroup.MinMz = msGroup.MzArr.Where(x => x > 0).Min();
            msGroup.MaxMz = msGroup.MzArr.Max();
            msGroup.MinIntensity = msGroup.IntArr.Where(x => x > 0).Min();
            msGroup.MaxIntensity = msGroup.IntArr.Max();
            msGroup.MedianIntensity = BasicMathematics.Median(msGroup.IntArr.Where(x => x > 0).ToArray());
            msGroup.MedianMz = BasicMathematics.Median(msGroup.MzArr.Where(x => x > 0).ToArray());
        }

        private static List<MzIntGroup> getRefinedMsGroupList(List<MzIntGroup> mzIntGroupList, int numFiles) {
            var msGroupList = new List<MzIntGroup>();
            var preMz = 0.0;
            var preGroup = mzIntGroupList[0];
            foreach (var mzIntGroup in mzIntGroupList) {
                if (Math.Abs(mzIntGroup.MedianMz - preMz) <= 0.01) {
                  //Debug.WriteLine("## Refined ## : " + msGroup.MedianMz + ", pre: " + preMz);

                    var newMzIntGroup = new MzIntGroup() {
                        Counter = 0,
                        Mz = mzIntGroup.MedianMz,
                        Peak = new Dictionary<int, float[]>()
                    };
                    for (var i = 0; i < numFiles; i++) {
                        if (mzIntGroup.Peak.ContainsKey(i)) {
                            if (preGroup.Peak.ContainsKey(i)) {
                                if (mzIntGroup.Peak[i][0] == preGroup.Peak[i][0]) {
                                    newMzIntGroup.Peak.Add(i, new float[] { mzIntGroup.Peak[i][0], mzIntGroup.Peak[i][1] });
                                    newMzIntGroup.Counter++;
                                }
                                else if (mzIntGroup.Peak[i][1] > preGroup.Peak[i][1]) {
                                    newMzIntGroup.Peak.Add(i, new float[] { mzIntGroup.Peak[i][0], mzIntGroup.Peak[i][1] + preGroup.Peak[i][1] });
                                    newMzIntGroup.Counter++;
                                }
                                else {
                                    newMzIntGroup.Peak.Add(i, new float[] { preGroup.Peak[i][0], mzIntGroup.Peak[i][1] + preGroup.Peak[i][1] });
                                    newMzIntGroup.Counter++;
                                }
                            }
                            else {
                                newMzIntGroup.Peak.Add(i, new float[] { mzIntGroup.Peak[i][0], mzIntGroup.Peak[i][1] });
                                newMzIntGroup.Counter++;
                            }
                        }
                        else if (preGroup.Peak.ContainsKey(i)) {
                            newMzIntGroup.Peak.Add(i, new float[] { preGroup.Peak[i][0], preGroup.Peak[i][1] });
                            newMzIntGroup.Counter++;
                        }
                    }
                    finalizer(newMzIntGroup);
                    preGroup = newMzIntGroup;
                }
                else {
                    msGroupList.Add(preGroup);
                    preGroup = mzIntGroup;
                }
                preMz = preGroup.MedianMz;
            }
            if (msGroupList[msGroupList.Count - 1].MedianMz != preGroup.MedianMz) msGroupList.Add(preGroup);
            return msGroupList;
        }

        #endregion

        #region reader
        public static List<MzIntGroup> ReadMsGroupingResults(FileStream fs, List<long> seekpointList, int peakID) {
            var msGroupList = new List<MzIntGroup>();
            var buffer = new byte[6];
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(buffer, 0, 6);
            msGroupList = GetMsGroupingResultVer1(fs, seekpointList, peakID);
            finalizer(msGroupList);
            return msGroupList;
        }

        private static List<MzIntGroup> GetMsGroupingResultVer1(FileStream fs, List<long> seekpointList, int peakID) {
            var result = new List<MzIntGroup>();

            fs.Seek(seekpointList[peakID], SeekOrigin.Begin); // go to the target seek point

            var buffer = new byte[4];
            fs.Read(buffer, 0, buffer.Length); // store the meta field buffer

            var numCount = BitConverter.ToInt32(buffer, 0);
            for (int i = 0; i < numCount; i++) {
                result.Add(new MzIntGroup());
                buffer = new byte[4];
                fs.Read(buffer, 0, buffer.Length);
                var counter = BitConverter.ToInt32(buffer, 0);
                result[i].Counter = counter;
                result[i].Peak = new Dictionary<int, float[]>();
                buffer = new byte[(int)counter * 12];
                fs.Read(buffer, 0, buffer.Length);
                for (int j = 0; j < counter; j++) {
                    var key = BitConverter.ToInt32(buffer, 12 * j);
                    var mz = BitConverter.ToSingle(buffer, 12 * j + 4);
                    var intensity = BitConverter.ToSingle(buffer, 12 * j + 8);
                    result[i].Peak.Add(key, new float[] { mz, intensity });
                }
            }
            return result;
        }

        public static List<long> ReadSeekPointsOfMsGroup(FileStream fs) {
            var buffer = new byte[6];
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(buffer, 0, 6);

            if (BitConverter.ToChar(buffer, 0).Equals('V') && (BitConverter.ToInt32(buffer, 2) == 1))
                return GetSeekpointListVer1(fs);
            else
                return GetSeekpointListVer1(fs);
        }

        private static List<long> GetSeekpointListVer1(FileStream fs) {
            var seekpointList = new List<long>();
            int totalPeakNumber;
            byte[] buffer = new byte[4];

            fs.Seek(6, SeekOrigin.Begin);
            fs.Read(buffer, 0, 4);

            totalPeakNumber = BitConverter.ToInt32(buffer, 0);
            buffer = new byte[8 * totalPeakNumber];
            fs.Read(buffer, 0, buffer.Length);
            for (int i = 0; i < totalPeakNumber; i++) {
                seekpointList.Add((long)BitConverter.ToInt64(buffer, 8 * i));
            }
            return seekpointList;
        }
        #endregion        
    }
}
    #endregion


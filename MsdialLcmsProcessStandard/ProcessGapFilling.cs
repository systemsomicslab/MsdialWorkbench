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
using CompMs.RawDataHandler.Core;
using System.Diagnostics;

namespace Msdial.Lcms.DataProcess
{
    public sealed class ProcessGapFilling
    {
        private ProcessGapFilling() { }

        public static void Execute(ProjectPropertyBean projectProp, RdamPropertyBean rdamProperty, 
            ObservableCollection<AnalysisFileBean> files, AlignmentFileBean alignmentFile, 
            AnalysisParametersBean param, IupacReferenceBean iupacRef,
            AlignmentResultBean alignmentResult, Action<int> reportAction)
        {
            if (param.IsIonMobility) {
                ExecuteAtIonMobility(projectProp, rdamProperty, files, alignmentFile, param, alignmentResult, iupacRef, reportAction);
                return;
            }
            var alignedEics = new List<AlignedData>();
            var newIdList = new List<int>();
            string eicFilePath = alignmentFile.EicFilePath;
            int flag = 0;

            var dt = projectProp.ProjectDate;
            var dirname = System.IO.Path.Combine(projectProp.ProjectFolderPath, "project_" + dt.Year + "_" + dt.Month + "_" + dt.Day + "_" + dt.Hour + "_" + dt.Minute + "_" + dt.Second + "_tmpFolder");
            if (!System.IO.Directory.Exists(dirname))
                System.IO.Directory.CreateDirectory(dirname);

            for (int i = 0; i < files.Count; i++)
            {
                DataStorageLcUtility.SetPeakAreaBeanCollection(files[i], files[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);
                var fileID = rdamProperty.RdamFilePath_RdamFileID[files[i].AnalysisFilePropertyBean.AnalysisFilePath];
                var measurementID = rdamProperty.RdamFileContentBeanCollection[fileID].FileID_MeasurementID[files[i].AnalysisFilePropertyBean.AnalysisFileId];

                using (var rawDataAccess = new RawDataAccess(files[i].AnalysisFilePropertyBean.AnalysisFilePath, measurementID, false, false, true, files[i].RetentionTimeCorrectionBean.PredictedRt))
                {
                    var spectrumCollection = DataAccessLcUtility.GetRdamSpectrumCollection(rawDataAccess);
                    var alignedPeakSpotInfoList = new List<AlignedPeakSpotInfo>();

                    for (int j = 0; j < alignmentResult.AlignmentPropertyBeanCollection.Count; j++)
                    {
                        var alignedSpotProp = alignmentResult.AlignmentPropertyBeanCollection[j];
                        var alignedPeakSpotInfo = new AlignedPeakSpotInfo();

                        if (flag == 0) {
                            alignedEics.Add(new AlignedData(alignedSpotProp));
                        }

                        //if (Math.Abs(alignedSpotProp.CentralRetentionTime - 6.3842) < 0.01 && Math.Abs(alignedSpotProp.CentralAccurateMass - 599.3209) < 0.001) {
                        //    Console.WriteLine();
                        //}

                        if (alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID <= -1) {
                            PeakAlignment.GapFillingMethod(spectrumCollection, alignedSpotProp,
                                alignedSpotProp.AlignedPeakPropertyBeanCollection[i], projectProp, param,
                               alignedSpotProp.CentralRetentionTime, alignedSpotProp.CentralAccurateMass,
                               param.RetentionTimeAlignmentTolerance,
                               param.CentroidMs1Tolerance,
                               alignedSpotProp.AveragePeakWidth);
                        }

                        alignedPeakSpotInfo.PeakList = 
                            DataAccessLcUtility.GetMs1Peaklist(spectrumCollection,
                            projectProp, alignedSpotProp.AlignedPeakPropertyBeanCollection[i].AccurateMass, 
                            param.CentroidMs1Tolerance,
                            alignedEics[j].MinRt, alignedEics[j].MaxRt);

                        alignedPeakSpotInfo.TargetRt = alignedSpotProp.AlignedPeakPropertyBeanCollection[i].RetentionTime;
                        alignedPeakSpotInfo.TargetLeftRt = alignedSpotProp.AlignedPeakPropertyBeanCollection[i].RetentionTimeLeft;
                        alignedPeakSpotInfo.TargetRightRt = alignedSpotProp.AlignedPeakPropertyBeanCollection[i].RetentionTimeRight;

                        if (alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID <= -1) {
                            alignedPeakSpotInfo.GapFilled = true;
                        }
                        else {
                            alignedPeakSpotInfo.GapFilled = false;
                        }

                        alignedPeakSpotInfoList.Add(alignedPeakSpotInfo);

                        //if (alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID != -1) {
                        //    var peak = analysisFileBeanCollection[i].PeakAreaBeanCollection[alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID];
                        //    alignedPeakSpotInfo.TargetRt = peak.RtAtPeakTop;
                        //    alignedPeakSpotInfo.TargetLeftRt = peak.RtAtLeftPeakEdge;
                        //    alignedPeakSpotInfo.TargetRightRt = peak.RtAtRightPeakEdge;
                        //    alignedPeakSpotInfoList.Add(alignedPeakSpotInfo);
                        //    continue;
                        //}

                        //alignedPeakSpotInfoList.Add(alignedPeakSpotInfo);

                        
                    }
                    var filename = Path.Combine(dirname, "peaklist_" + i + ".pll");
                    AlignedEic.WritePeakList(alignedPeakSpotInfoList, projectProp, filename);
                }
                if (flag == 0) flag = 1;
                reportAction?.Invoke(i + 1);
                Console.WriteLine(i + " finished");
                DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(files[i]);
            }
            Console.WriteLine("Start finalization");
            PeakAlignment.FinalizeJointAligner(alignmentResult, files, param, projectProp, iupacRef, ref newIdList);
            Console.WriteLine("End finalization");


            Console.WriteLine("Start aligned eic");
            AlignedEic.WriteAlignedEic(alignedEics, projectProp, newIdList, files.Count, dirname, eicFilePath);
            Console.WriteLine("End aligned eic");
            System.IO.Directory.Delete(dirname, true);
        }

        public static void ExecuteAtIonMobility(ProjectPropertyBean projectProperty, RdamPropertyBean rdamProperty, 
            ObservableCollection<AnalysisFileBean> files, AlignmentFileBean alignmentFile, AnalysisParametersBean param, 
            AlignmentResultBean alignmentResult, IupacReferenceBean iupacRef, Action<int> reportAction) {
            var alignedEics = new List<AlignedData>();
            var newIdList = new List<int>();
            var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;

            string eicFilePath = alignmentFile.EicFilePath;
            int flag = 0;

            var dt = projectProperty.ProjectDate;
            var dirname = System.IO.Path.Combine(projectProperty.ProjectFolderPath, "project_" + dt.Year + "_" + dt.Month + "_" + dt.Day + "_" + dt.Hour + "_" + dt.Minute + "_" + dt.Second + "_tmpFolder");
            if (!System.IO.Directory.Exists(dirname))
                System.IO.Directory.CreateDirectory(dirname);

            for (int i = 0; i < files.Count; i++) {

                var file = files[i];
                var fileProp = file.AnalysisFilePropertyBean;

                DataStorageLcUtility.SetPeakAreaBeanCollection(file, fileProp.PeakAreaBeanInformationFilePath);
                var fileID = rdamProperty.RdamFilePath_RdamFileID[fileProp.AnalysisFilePath];
                var measurementID = rdamProperty.RdamFileContentBeanCollection[fileID].FileID_MeasurementID[fileProp.AnalysisFileId];

                using (var rawDataAccess = new RawDataAccess(fileProp.AnalysisFilePath, measurementID, false, false, true, file.RetentionTimeCorrectionBean.PredictedRt)) {
                    var raw_measurment = DataAccessLcUtility.GetRawDataMeasurement(rawDataAccess);
                    var accumulatedMs1SpecCollection = DataAccessLcUtility.GetAccumulatedMs1SpectrumCollection(raw_measurment);
                    var allSpecCollection = DataAccessLcUtility.GetAllSpectrumCollection(raw_measurment);
                    var alignedPeakSpotInfoList = new List<AlignedPeakSpotInfo>();
                    var counter = 0;
                    for (int j = 0; j < alignedSpots.Count; j++) {
                        // for rt and m/z axis
                        #region
                        var alignedSpotProp = alignedSpots[j];
                        var alignedPeakSpotInfo = new AlignedPeakSpotInfo();

                        if (flag == 0) {
                            alignedEics.Add(new AlignedData(alignedSpotProp));
                        }

                        if (alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID <= -1) {
                            PeakAlignment.GapFillingMethod(accumulatedMs1SpecCollection, alignedSpotProp,
                                alignedSpotProp.AlignedPeakPropertyBeanCollection[i], projectProperty, param,
                               alignedSpotProp.CentralRetentionTime, alignedSpotProp.CentralAccurateMass,
                               param.RetentionTimeAlignmentTolerance,
                               param.CentroidMs1Tolerance,
                               alignedSpotProp.AveragePeakWidth);
                        }

                        alignedPeakSpotInfo.PeakList =
                            DataAccessLcUtility.GetMs1Peaklist(accumulatedMs1SpecCollection,
                            projectProperty, alignedSpotProp.AlignedPeakPropertyBeanCollection[i].AccurateMass,
                            param.CentroidMs1Tolerance,
                            alignedEics[counter].MinRt, 
                            alignedEics[counter].MaxRt);

                        alignedPeakSpotInfo.TargetRt = alignedSpotProp.AlignedPeakPropertyBeanCollection[i].RetentionTime;
                        alignedPeakSpotInfo.TargetLeftRt = alignedSpotProp.AlignedPeakPropertyBeanCollection[i].RetentionTimeLeft;
                        alignedPeakSpotInfo.TargetRightRt = alignedSpotProp.AlignedPeakPropertyBeanCollection[i].RetentionTimeRight;

                        if (alignedSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID <= -1) {
                            alignedPeakSpotInfo.GapFilled = true;
                        }
                        else {
                            alignedPeakSpotInfo.GapFilled = false;
                        }

                        var parentMinRt = alignedPeakSpotInfo.TargetLeftRt;
                        var parentMaxRt = alignedPeakSpotInfo.TargetRightRt;

                        alignedPeakSpotInfoList.Add(alignedPeakSpotInfo);
                        counter++;
                        #endregion

                        //if (Math.Abs(alignedSpots[j].CentralAccurateMass - 764.527) < 0.01 && Math.Abs(alignedSpots[j].CentralRetentionTime - 4.32) < 0.1) {
                        //    Debug.WriteLine("check");
                        //}

                        // for dt and m/z axis
                        #region
                        var driftSpots = alignedSpotProp.AlignedDriftSpots;
                        for (int k = 0; k < driftSpots.Count; k++) {
                            var driftSpotProp = driftSpots[k];
                            var driftSpotInfo = new AlignedPeakSpotInfo();

                            if (flag == 0) {
                                alignedEics.Add(new AlignedData(driftSpotProp));
                            }

                            if (driftSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID <= -1) {
                                PeakAlignment.GapFillingMethod(allSpecCollection, alignedSpotProp, driftSpotProp,
                                    driftSpotProp.AlignedPeakPropertyBeanCollection[i], projectProperty, param,
                                    alignedSpotProp.CentralRetentionTime, 
                                    driftSpotProp.CentralDriftTime, 
                                    driftSpotProp.CentralAccurateMass,
                                    param.DriftTimeAlignmentTolerance,
                                    param.CentroidMs1Tolerance,
                                    driftSpotProp.AveragePeakWidth);
                            }

                            driftSpotInfo.PeakList =
                               DataAccessLcUtility.GetDriftChromatogramByRtMz(allSpecCollection,
                               alignedSpotProp.CentralRetentionTime,
                               param.AccumulatedRtRagne,
                               alignedSpotProp.AlignedPeakPropertyBeanCollection[i].AccurateMass, param.CentroidMs1Tolerance,
                               alignedEics[counter].MinRt, alignedEics[counter].MaxRt);

                            driftSpotInfo.TargetRt = driftSpotProp.AlignedPeakPropertyBeanCollection[i].DriftTime;
                            driftSpotInfo.TargetLeftRt = driftSpotProp.AlignedPeakPropertyBeanCollection[i].DriftTimeLeft;
                            driftSpotInfo.TargetRightRt = driftSpotProp.AlignedPeakPropertyBeanCollection[i].DriftTimeRight;

                            if (driftSpotProp.AlignedPeakPropertyBeanCollection[i].PeakID <= -1) {
                                driftSpotInfo.GapFilled = true;
                            }
                            else {
                                driftSpotInfo.GapFilled = false;
                            }

                            alignedPeakSpotInfoList.Add(driftSpotInfo);
                            counter++;
                        }
                        #endregion

                    }

                    var filename = Path.Combine(dirname, "peaklist_" + i + ".pll");
                    AlignedEic.WritePeakList(alignedPeakSpotInfoList, projectProperty, filename);
                }

                if (flag == 0) flag = 1;
                reportAction?.Invoke(i + 1);
                DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(files[i]);
            }
            PeakAlignment.FinalizeJointAlignerAtIonMobility(alignmentResult, files, param, projectProperty, iupacRef, ref newIdList);

            AlignedEic.WriteAlignedEic(alignedEics, projectProperty, newIdList, files.Count, dirname, eicFilePath);
            System.IO.Directory.Delete(dirname, true);
        }

    }
}

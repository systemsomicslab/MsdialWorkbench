using Microsoft.Win32;
using Msdial.Gcms.Dataprocess.Algorithm;
using Msdial.Lcms.Dataprocess.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class MsDialToExternalApps
    {
        private MsDialToExternalApps() { }

        public static void SendToMsFinderProgram(MainWindow mainWindow, ExportspectraType exportMsType)
        {
            var iniField = mainWindow.MsdialIniField;
            if (!checkFilePath(iniField)) return;

            var msdialTempDir = System.IO.Path.GetDirectoryName(iniField.MsfinderFilePath) + "\\MSDIAL_TEMP";
            folderCheckInMsFinder(msdialTempDir);

            var dt = DateTime.Now;
            var timeString = dt.Year.ToString() + "_" + dt.Month.ToString() + "_" + dt.Day.ToString() + "_" + dt.Hour.ToString() + dt.Minute.ToString();

            var param = mainWindow.AnalysisParamForLC;
            var peakArea = mainWindow.AnalysisFiles[mainWindow.FocusedFileID].PeakAreaBeanCollection[mainWindow.FocusedPeakID];
            var id = peakArea.PeakID;
            DriftSpotBean driftSpot = null;
            var dtString = string.Empty;
            if (param.IsIonMobility) {
                driftSpot = mainWindow.DriftSpotBeanList[mainWindow.FocusedDriftSpotID];
                dtString = "_" + Math.Round(driftSpot.DriftTimeAtPeakTop, 2).ToString();
                id = driftSpot.MasterPeakID;
            }

            var fileString = mainWindow.AnalysisFiles[mainWindow.FocusedFileID].AnalysisFilePropertyBean.AnalysisFileName;
            var filePath = msdialTempDir + "\\" + timeString + "_" + fileString + "_" + id + "_" + Math.Round(peakArea.RtAtPeakTop, 2).ToString() + "_" + Math.Round(peakArea.AccurateMass, 2).ToString() + dtString + "." + SaveFileFormat.mat;

            DataExportLcUtility.MsAnnotationTagExport(mainWindow, filePath, peakArea, driftSpot, exportMsType, MatExportOption.OnlyFocusedPeak);

            var process = new Process();
            process.StartInfo.FileName = iniField.MsfinderFilePath;
            process.StartInfo.Arguments = msdialTempDir;
            process.Start();
        }

        public static void SendToMsFinderProgram(MainWindow mainWindow, MS1DecResult ms1DecResult)
        {
            var iniField = mainWindow.MsdialIniField;
            if (!checkFilePath(iniField)) return;

            var msdialTempDir = System.IO.Path.GetDirectoryName(iniField.MsfinderFilePath) + "\\MSDIAL_TEMP";
            folderCheckInMsFinder(msdialTempDir);

            var dt = DateTime.Now;
            var timeString = dt.Year.ToString() + "_" + dt.Month.ToString() + "_" + dt.Day.ToString() + "_" + dt.Hour.ToString() + dt.Minute.ToString();
            var fileString = mainWindow.AnalysisFiles[mainWindow.FocusedFileID].AnalysisFilePropertyBean.AnalysisFileName;
            var filePath = msdialTempDir + "\\" + timeString + "_" + fileString + "_" + ms1DecResult.Ms1DecID + "_" + Math.Round(ms1DecResult.RetentionTime, 2).ToString() + "_" + Math.Round(ms1DecResult.RetentionIndex, 2).ToString() + "." + SaveFileFormat.mat;

            DataExportGcUtility.MsfinderTagExport(mainWindow, filePath, ms1DecResult);

            var process = new Process();
            process.StartInfo.FileName = iniField.MsfinderFilePath;
            process.StartInfo.Arguments = msdialTempDir;
            process.Start();
        }

        public static void SendToMsFinderProgram(MainWindow mainWindow)
        {
            var iniField = mainWindow.MsdialIniField;
            if (!checkFilePath(iniField)) return;

            var msdialTempDir = System.IO.Path.GetDirectoryName(iniField.MsfinderFilePath) + "\\MSDIAL_TEMP";
            folderCheckInMsFinder(msdialTempDir);

            var ms2DecResult = mainWindow.AlignViewMS2DecResult;
            var param = mainWindow.AnalysisParamForLC;


            var alignmentFileID = mainWindow.FocusedAlignmentFileID;
            var alignmentSpotID = mainWindow.FocusedAlignmentPeakID;
            var alignmentProperty = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection[alignmentSpotID];

            var id = alignmentSpotID;
            AlignedDriftSpotPropertyBean driftSpot = null;
            var driftString = string.Empty;
            if (param.IsIonMobility) {
                driftSpot = mainWindow.AlignedDriftSpotBeanList[mainWindow.FocusedAlignmentDriftID];
                driftString = "_" + Math.Round(driftSpot.CentralDriftTime, 2).ToString();
                id = driftSpot.MasterID;
            }
            
            var dt = DateTime.Now;
            var timeString = dt.Year.ToString() + "_" + dt.Month.ToString() + "_" + dt.Day.ToString() + "_" + dt.Hour.ToString() + dt.Minute.ToString();
            var fileString = mainWindow.AlignmentFiles[alignmentFileID].FileName;
            var filePath = msdialTempDir + "\\" + timeString + "_" + fileString + "_" + id + "_" + 
                Math.Round(alignmentProperty.CentralRetentionTime, 2).ToString() + "_" + Math.Round(alignmentProperty.CentralAccurateMass, 2).ToString() + driftString + "." + SaveFileFormat.mat;

            if (param != null && param.TrackingIsotopeLabels) {
                var targetIsotopeTrackFileID = param.NonLabeledReferenceID;
                if (targetIsotopeTrackFileID < 0) {
                    MessageBox.Show("Non labeled reference file is not set correctly.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var alignedSpots = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection.Where(n => n.IsotopeTrackingParentID == alignmentProperty.IsotopeTrackingParentID).ToList();
                if (alignedSpots.Count < 2) {
                    if (MessageBox.Show("The target spot is not grouped in any cluster. Do you want to export the result without the element information?", "Message", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes) {
                        DataExportLcUtility.MsAnnotationTagExport(mainWindow, filePath, ms2DecResult, alignmentProperty, null);
                    }
                    else {
                        return;
                    }
                }
                else {

                    var nonlabelID = param.NonLabeledReferenceID;
                    var labeledID = param.FullyLabeledReferenceID;

                    //reminder
                    var filteredSpots = new ObservableCollection<AlignmentPropertyBean>();
                    ResultExportLcUtility.StoringTempSpotsToMaster(new ObservableCollection<AlignmentPropertyBean>(alignedSpots), filteredSpots, param.SetFullyLabeledReferenceFile, labeledID, nonlabelID);

                    if (filteredSpots.Count < 2) {
                        if (MessageBox.Show("The target spot is not grouped in any cluster. Do you want to export the result without the element information?",
                            "Message", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes) {
                            DataExportLcUtility.MsAnnotationTagExport(mainWindow, filePath, ms2DecResult, alignmentProperty, null);
                        }
                        else {
                            return;
                        }
                    }
                    else {
                        if (filteredSpots[0].AlignmentID != alignmentSpotID) {
                            if (MessageBox.Show("The target spot is not recognized as monoisotopic ion. Do you want to export the result without the element information?", 
                                "Message", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes) {
                                DataExportLcUtility.MsAnnotationTagExport(mainWindow, filePath, ms2DecResult, alignmentProperty, null);
                            }
                            else {
                                return;
                            }
                        }
                        else {
                            DataExportLcUtility.MsAnnotationTagExport(mainWindow, msdialTempDir, ms2DecResult, alignmentProperty, param, filteredSpots, mainWindow.MspDB);
                        }
                    }
                }
            }
            else {
                DataExportLcUtility.MsAnnotationTagExport(mainWindow, filePath, ms2DecResult, alignmentProperty, driftSpot);
            }

            var process = new Process();
            process.StartInfo.FileName = iniField.MsfinderFilePath;
            process.StartInfo.Arguments = msdialTempDir;
            process.Start();

            //System.Diagnostics.Process.Start("notepad.exe", @"""C:\Users\hiroshi.tsugawa\Desktop\1.txt""");
            //var pInfo = new ProcessStartInfo() {
            //    FileName = iniField.MsfinderFilePath,
            //    Arguments = msdialTempDir
            //};
            //Process.Start(pInfo);
        }

        public static void SendToMsFinderProgramSelectedAlignmentSpots(MainWindow mainWindow, string folderPath, IEnumerable<AlignmentPropertyBean> targets)
        {
            var iniField = MsDialIniParcer.Read();
            if (!checkFilePath(iniField)) return;

            var msdialTempDir = folderPath;
            folderCheckInMsFinder(msdialTempDir);
            foreach (var spot in targets)
            {
                var ms2Dec = Msdial.Lcms.Dataprocess.Algorithm.SpectralDeconvolution.ReadMS2DecResult(mainWindow.AlignViewDecFS, mainWindow.AlignViewDecSeekPoints, spot.AlignmentID);
                var filePath = folderPath + "\\ID" + spot.AlignmentID.ToString("00000") + "_" + Math.Round(spot.CentralRetentionTime, 2).ToString() + "_" + Math.Round(spot.CentralAccurateMass, 2).ToString() + "." + SaveFileFormat.mat;
                if (filePath.Length < 220)
                {
                    using (var sw = new System.IO.StreamWriter(filePath, false, Encoding.ASCII))
                    {
                        Msdial.Common.Export.ExportMassSpectrum.WriteSpectrumFromAlignment(sw, mainWindow.ProjectProperty, mainWindow.MspDB, ms2Dec, spot, mainWindow.AnalysisFiles, false);
                    }
                }
            }
            var process = new Process();
            process.StartInfo.FileName = iniField.MsfinderFilePath;
            process.StartInfo.Arguments = msdialTempDir;
            process.Start();
            MessageBox.Show("Exported.", "Notice", MessageBoxButton.OK);
        }

        public static void SendToMsFinderProgramSelectedPeakSpots(MainWindow mainWindow, string folderPath, IEnumerable<PeakAreaBean> targets)
        {
            var iniField = MsDialIniParcer.Read();
            if (!checkFilePath(iniField)) return;

            var msdialTempDir = folderPath;
            folderCheckInMsFinder(msdialTempDir);
            foreach (var spot in targets)
            {
                var ms2Dec = Msdial.Lcms.Dataprocess.Algorithm.SpectralDeconvolution.ReadMS2DecResult(mainWindow.PeakViewDecFS, mainWindow.PeakViewDecSeekPoints, spot.PeakID);
                var filePath = folderPath + "\\ID" + spot.PeakID.ToString("00000") + "_" + Math.Round(spot.RtAtPeakTop, 2).ToString() + "_" + Math.Round(spot.AccurateMass, 2).ToString() + "." + SaveFileFormat.mat;
                if (filePath.Length < 260)
                {
                    using (var sw = new System.IO.StreamWriter(filePath, false, Encoding.ASCII))
                    {
                        Msdial.Common.Export.ExportMassSpectrum.WriteSpectrumFromPeakSpot(sw, mainWindow.ProjectProperty, mainWindow.MspDB, mainWindow.AnalysisParamForLC, ms2Dec, spot, mainWindow.LcmsSpectrumCollection, mainWindow.AnalysisFiles[mainWindow.FocusedFileID]);
                    }
                }
            }
            var process = new Process();
            process.StartInfo.FileName = iniField.MsfinderFilePath;
            process.StartInfo.Arguments = msdialTempDir;
            process.Start();
            MessageBox.Show("Exported.", "Notice", MessageBoxButton.OK);
        }


        private static void folderCheckInMsFinder(string msdialTempDir)
        {
            if (System.IO.Directory.Exists(msdialTempDir)) return;
            else
            {
                var di = System.IO.Directory.CreateDirectory(msdialTempDir);
            }
        }

        private static bool checkFilePath(MsDialIniField iniField)
        {
            var path = iniField.MsfinderFilePath;
            while (!System.IO.File.Exists(path))
            {
                if (MessageBox.Show("The file path of MS-FINDER program is not correct. Please select the correct path.", "Error", MessageBoxButton.OKCancel, MessageBoxImage.Error) == MessageBoxResult.Cancel)
                    return false;
                
                var ofd = new OpenFileDialog();
                ofd.Filter = "MS-FINDER Exe (*.exe)|*.exe";
                ofd.Title = "Select MS-FINDER Exe File";
                ofd.RestoreDirectory = true;

                if (ofd.ShowDialog() == true)
                    path = ofd.FileName;
                else
                    return false;
            }
            
            iniField.MsfinderFilePath = path;
            MsDialIniParcer.Write(iniField);

            return true;
        }

    }
}

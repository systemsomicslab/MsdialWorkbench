using Msdial.Lcms.Dataprocess.Algorithm;
using Msdial.Lcms.Dataprocess.Utility;
using Riken.Metabolomics.Lipoquality;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Riken.Metabolomics.Annotation;
using CompMs.RawDataHandler.Core;
using System.Diagnostics;
using Riken.Metabolomics.Lipidomics;
using CompMs.Common.MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    public class ExportableLcPeakEdge
    {
        public int SourceID { get; set; }
        public int TargetID { get; set; }
        public string Type { get; set; }
        public string Score { get; set; }
        public string Annotation { get; set; }
        public string EdgeID { get; set; }
        public string EdgeIdShort { get; set; }
    }
    public sealed class DataExportLcUtility
    {
        private DataExportLcUtility() { }

        public static void PeaklistExport(MainWindow mainWindow, string exportFolderPath,
            ObservableCollection<AnalysisFileBean> files,
            ExportSpectraFileFormat exportSpectraFileFormat, ExportspectraType exportSpectraType,
            float isotopeExportMax)
        {
            int focusedFileID = mainWindow.FocusedFileID;
            int focusedPeakID = mainWindow.FocusedPeakID;

            mainWindow.PeakViewDataAccessRefresh();

            mainWindow.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            if (exportSpectraFileFormat == ExportSpectraFileFormat.mgf)
                peaklistMgfExport(mainWindow, exportFolderPath, files, mainWindow.ProjectProperty.MethodType, exportSpectraType);
            else if (exportSpectraFileFormat == ExportSpectraFileFormat.msp)
                peaklistMspExport(mainWindow, exportFolderPath, files, mainWindow.ProjectProperty.MethodType, exportSpectraType);
            else if (exportSpectraFileFormat == ExportSpectraFileFormat.mat)
                peaklistMatExport(mainWindow, exportFolderPath, files, mainWindow.ProjectProperty.MethodType, exportSpectraType, isotopeExportMax);
            else if (exportSpectraFileFormat == ExportSpectraFileFormat.ms)
                peaklistSriusMsExport(mainWindow, exportFolderPath, files, mainWindow.ProjectProperty.MethodType, exportSpectraType, isotopeExportMax);
            else if (exportSpectraFileFormat == ExportSpectraFileFormat.txt)
                peaklistTxtExport(mainWindow, exportFolderPath, files, mainWindow.ProjectProperty.MethodType, exportSpectraType, isotopeExportMax);

            mainWindow.PeakViewerForLcRefresh(focusedFileID);
            ((PairwisePlotPeakViewUI)mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.SelectedPlotId = focusedPeakID;

            Mouse.OverrideCursor = null;
            mainWindow.IsEnabled = true;
        }

        public static void PeaklistExportPrivate(MainWindow mainWindow, string exportFolderPath,
           ObservableCollection<AnalysisFileBean> files,
           ExportSpectraFileFormat exportSpectraFileFormat, ExportspectraType exportSpectraType,
           float isotopeExportMax)
        {
            int focusedFileID = mainWindow.FocusedFileID;
            int focusedPeakID = mainWindow.FocusedPeakID;

            mainWindow.PeakViewDataAccessRefresh();

            mainWindow.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            for (int i = 0; i < files.Count; i++)
            {
                var filePath = exportFolderPath + "\\" + files[i].AnalysisFilePropertyBean.AnalysisFileName + "." + ExportSpectraFileFormat.txt;

                using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
                {
                    //set List<PeakAreaBean> on AnalysisFileBean
                    DataStorageLcUtility.SetPeakAreaBeanCollection(files[i], files[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);
                    //set raw spectra collection to spectrumCollection
                    var peakSpots = files[i].PeakAreaBeanCollection.Where(n => n.IsotopeWeightNumber == 0).ToList();

                    sw.WriteLine("mz\trt");
                    for (int j = 0; j < peakSpots.Count; j++)
                    {
                        var peakAreaBean = peakSpots[j];
                        sw.WriteLine(peakAreaBean.AccurateMass + "\t" + peakAreaBean.RtAtPeakTop);
                    }

                    //refresh
                    DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(files[i]);
                }
            }

            mainWindow.PeakViewerForLcRefresh(focusedFileID);
            ((PairwisePlotPeakViewUI)mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.SelectedPlotId = focusedPeakID;

            Mouse.OverrideCursor = null;
            mainWindow.IsEnabled = true;
        }

        public static void AlignmentResultExport(MainWindow mainWindow, int alignmentID, int targetFileID)
        {
            AlignmentResultBean alignmentResult = MessagePackHandler.LoadFromFile<AlignmentResultBean>(mainWindow.AlignmentFiles[alignmentID].FilePath);
            //AlignmentResultBean alignmentResultBean = (AlignmentResultBean)DataStorageLcUtility.LoadFromXmlFile(mainWindow.AlignmentFiles[selectedAlignmentFileID].FilePath, typeof(AlignmentResultBean));
            var analysisFiles = mainWindow.AnalysisFiles;
            var projectProp = mainWindow.ProjectProperty;
            var exportFolderPath = projectProp.ExportFolderPath;
            var isRawData = projectProp.RawDatamatrix;
            var isNormalizedData = projectProp.NormalizedDatamatrix;
            var isAreaData = projectProp.PeakareaMatrix;
            var isIdData = projectProp.PeakIdMatrix;
            var isRtData = projectProp.RetentionTimeMatrix;
            var isMzData = projectProp.MzMatrix;
            var isMsmsIncluded = projectProp.MsmsIncludedMatrix;
            var snMatrix = projectProp.SnMatrixExport;
            var isUniqueMs = projectProp.UniqueMs;
            var blankFilter = projectProp.BlankFilter;
            var replaceZeroToHalf = projectProp.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples;
            var isRepSpectra = projectProp.RepresentativeSpectra;
            var isParamExport = projectProp.Parameter;
            var molecularnetworkingedges = projectProp.MolecularNetworkingExport;
            var exportSpectraFileFormat = projectProp.ExportSpectraFileFormat;
            var isExportedAsMzTabM = projectProp.IsExportedAsMzTabM;


            mainWindow.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            int focusedFileID = mainWindow.FocusedFileID;
            int focusedPeakID = mainWindow.FocusedPeakID;

            int focusedAlignmentFileID = mainWindow.FocusedAlignmentFileID;
            int focusedAlignmentPeakID = mainWindow.FocusedAlignmentPeakID;

            mainWindow.PeakViewDataAccessRefresh();
            mainWindow.AlignmentViewDataAccessRefresh();

            var mspDB = mainWindow.MspDB;
            var txtDB = mainWindow.PostIdentificationTxtDB;

            DateTime dt = DateTime.Now;
            string heightFile = exportFolderPath + "\\" + "Height_" + alignmentID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string normalizedFile = exportFolderPath + "\\" + "Normalized_" + alignmentID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string areaFile = exportFolderPath + "\\" + "Area_" + alignmentID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string idFile = exportFolderPath + "\\" + "PeakID_" + alignmentID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string rtFile = exportFolderPath + "\\" + "RT_" + alignmentID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string mzFile = exportFolderPath + "\\" + "Mz_" + alignmentID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string msmsIncludedFile = exportFolderPath + "\\" + "MsmsIncluded_" + alignmentID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string ms2AreaFile = exportFolderPath + "\\" + "Ms2Area_" + alignmentID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string uniqueMsFile = exportFolderPath + "\\" + "UniqueMass_" + alignmentID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string paramFile = exportFolderPath + "\\" + "Parameter_" + alignmentID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string edgeFile = exportFolderPath + "\\" + "MolecularNetworkingEdges_" + alignmentID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string snFile = exportFolderPath + "\\" + "SN_" + alignmentID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            // string sampleAxisDeconvoExportFolderPath = exportFolderPath + "\\" + "SampleAxisDec_" + alignmentID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute;
            //////Mztab exporter
            string heightMztabFile = exportFolderPath + "\\" + "Height_" + alignmentID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".mzTab.txt";
            string normalizedMztabFile = exportFolderPath + "\\" + "Normalized_" + alignmentID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".mzTab.txt";
            string areaMztabFile = exportFolderPath + "\\" + "Area_" + alignmentID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".mzTab.txt";

            if (isNormalizedData == true && alignmentResult.Normalized == false)
            {
                MessageBox.Show("Data is not normalized yet. If you want to export the normalized data matrix, please at first perform data normalization methods from statistical analysis procedure.", "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                isNormalizedData = false;
            }

            if (isRawData == true)
            {
                MatrixExportWithAveStd(mainWindow, alignmentID, heightFile, alignmentResult, mspDB, txtDB, analysisFiles, "Height", targetFileID, blankFilter, replaceZeroToHalf);
                if (isExportedAsMzTabM)
                {
                    exportResultMztabFormet(mainWindow, alignmentID, heightMztabFile, alignmentResult, mspDB, txtDB, analysisFiles, "Height", targetFileID, blankFilter, replaceZeroToHalf);
                }
            }
            if (isNormalizedData == true)
            {
                MatrixExportWithAveStd(mainWindow, alignmentID, normalizedFile, alignmentResult, mspDB, txtDB, analysisFiles, "Normalized", targetFileID, blankFilter, replaceZeroToHalf);
                if (isExportedAsMzTabM)
                {
                    exportResultMztabFormet(mainWindow, alignmentID, normalizedMztabFile, alignmentResult, mspDB, txtDB, analysisFiles, "Normalized", targetFileID, blankFilter, replaceZeroToHalf);
                }
            }
            if (isAreaData == true)
            {
                MatrixExportWithAveStd(mainWindow, alignmentID, areaFile, alignmentResult, mspDB, txtDB, analysisFiles, "Area", targetFileID, blankFilter, replaceZeroToHalf);
                if (isExportedAsMzTabM)
                {
                    exportResultMztabFormet(mainWindow, alignmentID, areaMztabFile, alignmentResult, mspDB, txtDB, analysisFiles, "Area", targetFileID, blankFilter, replaceZeroToHalf);
                }
            }
            if (isIdData == true) MatrixExport(mainWindow, alignmentID, idFile, alignmentResult, mspDB, txtDB, analysisFiles, "Id", targetFileID, blankFilter, replaceZeroToHalf);
            if (isRtData == true) MatrixExport(mainWindow, alignmentID, rtFile, alignmentResult, mspDB, txtDB, analysisFiles, "RT", targetFileID, blankFilter, replaceZeroToHalf);
            if (isMzData == true) MatrixExport(mainWindow, alignmentID, mzFile, alignmentResult, mspDB, txtDB, analysisFiles, "MZ", targetFileID, blankFilter, replaceZeroToHalf);
            if (isMsmsIncluded == true) MatrixExport(mainWindow, alignmentID, msmsIncludedFile, alignmentResult, mspDB, txtDB, analysisFiles, "MSMS", targetFileID, blankFilter, replaceZeroToHalf);
            if (snMatrix == true) MatrixExport(mainWindow, alignmentID, snFile, alignmentResult, mspDB, txtDB, analysisFiles, "SN", targetFileID, blankFilter, replaceZeroToHalf);
            if (isUniqueMs == true) exportUniqueMsDataMatrix(mainWindow, alignmentID, uniqueMsFile, alignmentResult, mspDB, txtDB, analysisFiles, blankFilter);
            if (isRepSpectra == true)
                exportSpectra(mainWindow, exportFolderPath, alignmentID,
                    alignmentResult, exportSpectraFileFormat, targetFileID, blankFilter);

            //if (sampleAxisDeconvolution == true) exportSampleAxisDeconvolution(mainWindow, sampleAxisDeconvoExportFolderPath, alignmentResultBean, massTolerance);

            if (isParamExport == true)
                exportParameter(paramFile, alignmentResult, analysisFiles, mainWindow.ProjectProperty);

            if (molecularnetworkingedges == true)
                exportMolecularNetworkingEdges(edgeFile, mainWindow, exportFolderPath, alignmentID, alignmentResult, blankFilter);

            mainWindow.PeakViewerForLcRefresh(focusedFileID);
            ((PairwisePlotPeakViewUI)mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.SelectedPlotId = focusedPeakID;

            if (focusedAlignmentFileID < 0)
            {
                Mouse.OverrideCursor = null;
                mainWindow.IsEnabled = true;
                return;
            }

            mainWindow.AlignmentViewerForLcRefresh(focusedAlignmentFileID);
            ((PairwisePlotAlignmentViewUI)mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.SelectedPlotId = focusedAlignmentPeakID;

            Mouse.OverrideCursor = null;
            mainWindow.IsEnabled = true;
        }

        public static void AlignmentResultLipidomicsExport(MainWindow mainWindow, int selectedAlignmentFileID, int targetAnalysisFileID)
        {
            AlignmentResultBean alignmentResultBean = MessagePackHandler.LoadFromFile<AlignmentResultBean>(mainWindow.AlignmentFiles[selectedAlignmentFileID].FilePath);
            //AlignmentResultBean alignmentResultBean = (AlignmentResultBean)DataStorageLcUtility.LoadFromXmlFile(mainWindow.AlignmentFiles[selectedAlignmentFileID].FilePath, typeof(AlignmentResultBean));
            var analysisFiles = mainWindow.AnalysisFiles;
            var projectProp = mainWindow.ProjectProperty;
            var exportFolderPath = projectProp.ExportFolderPath;
            var isRawData = projectProp.RawDatamatrix;
            var isNormalizedData = projectProp.NormalizedDatamatrix;
            var isAreaData = projectProp.PeakareaMatrix;
            var isIdData = projectProp.PeakIdMatrix;
            var isRtData = projectProp.RetentionTimeMatrix;
            var isMzData = projectProp.MzMatrix;
            var isMsmsIncluded = projectProp.MsmsIncludedMatrix;
            var snMatrix = projectProp.SnMatrixExport;
            var isUniqueMs = projectProp.UniqueMs;
            var blankFilter = projectProp.BlankFilter;
            var replaceZeroToHalf = projectProp.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples;
            var isRepSpectra = projectProp.RepresentativeSpectra;
            var isParamExport = projectProp.Parameter;
            var molecularnetworkingedges = projectProp.MolecularNetworkingExport;
            var exportSpectraFileFormat = projectProp.ExportSpectraFileFormat;
            var isExportedAsMzTabM = projectProp.IsExportedAsMzTabM;
            if (isNormalizedData == true && alignmentResultBean.Normalized == false)
            {
                MessageBox.Show("Data is not normalized yet. If you want to export the normalized data matrix, please at first perform data normalization methods from statistical analysis procedure.", "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                isNormalizedData = false;
            }

            mainWindow.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            int focusedFileID = mainWindow.FocusedFileID;
            int focusedPeakID = mainWindow.FocusedPeakID;

            int focusedAlignmentFileID = mainWindow.FocusedAlignmentFileID;
            int focusedAlignmentPeakID = mainWindow.FocusedAlignmentPeakID;

            mainWindow.PeakViewDataAccessRefresh();
            mainWindow.AlignmentViewDataAccessRefresh();

            var mspDB = mainWindow.MspDB;
            var txtDB = mainWindow.PostIdentificationTxtDB;

            DateTime dt = DateTime.Now;
            string heightFile = exportFolderPath + "\\" + "Height_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string normalizedFile = exportFolderPath + "\\" + "Normalized_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string areaFile = exportFolderPath + "\\" + "Area_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string sampleAxisDeconvoExportFolderPath = exportFolderPath + "\\" + "SampleAxisDec_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute;
            string idFile = exportFolderPath + "\\" + "PeakID_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string retentiontimeMatrixExportFilePath = exportFolderPath + "\\" + "RT_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string mzFile = exportFolderPath + "\\" + "Mz_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string msmsIncludedFile = exportFolderPath + "\\" + "MsmsIncluded_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string ms2AreaFile = exportFolderPath + "\\" + "Ms2Area_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string uniqueMsFile = exportFolderPath + "\\" + "UniqueMass_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string paramFile = exportFolderPath + "\\" + "Parameter_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string edgeFile = exportFolderPath + "\\" + "MolecularNetworkingEdges_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string snFile = exportFolderPath + "\\" + "SN_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";

            if (isRawData == true)
                exportLipidomicsRawDataMatrix(mainWindow, selectedAlignmentFileID, heightFile,
                alignmentResultBean, mspDB, txtDB, analysisFiles, targetAnalysisFileID, blankFilter, replaceZeroToHalf);

            mainWindow.PeakViewerForLcRefresh(focusedFileID);
            ((PairwisePlotPeakViewUI)mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.SelectedPlotId = focusedPeakID;

            if (focusedAlignmentFileID < 0)
            {
                Mouse.OverrideCursor = null;
                mainWindow.IsEnabled = true;
                return;
            }

            mainWindow.AlignmentViewerForLcRefresh(focusedAlignmentFileID);
            ((PairwisePlotAlignmentViewUI)mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.SelectedPlotId = focusedAlignmentPeakID;

            Mouse.OverrideCursor = null;
            mainWindow.IsEnabled = true;
        }

        public static void AlignmentResultExportAtIonMobility(MainWindow mainWindow, int alignmentFileID, int targetAnalysisFileID)
        {

            AlignmentResultBean alignmentResult = MessagePackHandler.LoadFromFile<AlignmentResultBean>(mainWindow.AlignmentFiles[alignmentFileID].FilePath);
            //AlignmentResultBean alignmentResultBean = (AlignmentResultBean)DataStorageLcUtility.LoadFromXmlFile(mainWindow.AlignmentFiles[selectedAlignmentFileID].FilePath, typeof(AlignmentResultBean));
            var analysisFiles = mainWindow.AnalysisFiles;
            var projectProp = mainWindow.ProjectProperty;
            var exportFolderPath = projectProp.ExportFolderPath;
            var isRawData = projectProp.RawDatamatrix;
            var isNormalizedData = projectProp.NormalizedDatamatrix;
            var isAreaData = projectProp.PeakareaMatrix;
            var isIdData = projectProp.PeakIdMatrix;
            var isRtData = projectProp.RetentionTimeMatrix;
            var isMzData = projectProp.MzMatrix;
            var isMsmsIncluded = projectProp.MsmsIncludedMatrix;
            var snMatrix = projectProp.SnMatrixExport;
            var isUniqueMs = projectProp.UniqueMs;
            var blankFilter = projectProp.BlankFilter;
            var replaceZeroToHalf = projectProp.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples;
            var isRepSpectra = projectProp.RepresentativeSpectra;
            var isParamExport = projectProp.Parameter;
            var molecularnetworkingedges = projectProp.MolecularNetworkingExport;
            var exportSpectraFileFormat = projectProp.ExportSpectraFileFormat;
            var isExportedAsMzTabM = projectProp.IsExportedAsMzTabM;
            if (isNormalizedData == true && alignmentResult.Normalized == false)
            {
                MessageBox.Show("Data is not normalized yet. If you want to export the normalized data matrix, please at first perform data normalization methods from statistical analysis procedure.", "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                isNormalizedData = false;
            }

            mainWindow.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            int focusedFileID = mainWindow.FocusedFileID;
            int focusedPeakID = mainWindow.FocusedPeakID;

            int focusedAlignmentFileID = mainWindow.FocusedAlignmentFileID;
            int focusedAlignmentPeakID = mainWindow.FocusedAlignmentPeakID;

            mainWindow.PeakViewDataAccessRefresh();
            mainWindow.AlignmentViewDataAccessRefresh();

            var mspDB = mainWindow.MspDB;
            var txtDB = mainWindow.PostIdentificationTxtDB;

            DateTime dt = DateTime.Now;
            string heightFile = exportFolderPath + "\\" + "Height_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string normalizedFile = exportFolderPath + "\\" + "Normalized_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string areaFile = exportFolderPath + "\\" + "Area_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string sampleAxisDeconvoExportFolderPath = exportFolderPath + "\\" + "SampleAxisDec_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute;
            string idFile = exportFolderPath + "\\" + "PeakID_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string retentiontimeMatrixExportFilePath = exportFolderPath + "\\" + "RT_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string mzFile = exportFolderPath + "\\" + "Mz_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string msmsIncludedFile = exportFolderPath + "\\" + "MsmsIncluded_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string ms2AreaFile = exportFolderPath + "\\" + "Ms2Area_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string uniqueMsFile = exportFolderPath + "\\" + "UniqueMass_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string paramFile = exportFolderPath + "\\" + "Parameter_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string edgeFile = exportFolderPath + "\\" + "MolecularNetworkingEdges_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string snFile = exportFolderPath + "\\" + "SN_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";

            ///Mztab exporter
            string heightMztabFile = exportFolderPath + "\\" + "Height_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".mzTab.txt";
            string normalizedMztabFile = exportFolderPath + "\\" + "Normalized_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".mzTab.txt";
            string areaMztabFile = exportFolderPath + "\\" + "Area_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".mzTab.txt";

            if (isRawData == true)
            {
                MatrixExportIonMobilityWithAveStd(heightFile, mainWindow, alignmentFileID, alignmentResult, analysisFiles, mspDB, txtDB, "Height", blankFilter, replaceZeroToHalf);
                if (isExportedAsMzTabM)
                {
                    exportResultMztabFormet(mainWindow, alignmentFileID, heightMztabFile, alignmentResult, mspDB, txtDB, analysisFiles, "Height", 0, blankFilter, replaceZeroToHalf);
                }
            }
            if (isNormalizedData == true)
            {
                MatrixExportIonMobilityWithAveStd(normalizedFile, mainWindow, alignmentFileID, alignmentResult, analysisFiles, mspDB, txtDB, "Normalized", blankFilter, replaceZeroToHalf);
                if (isExportedAsMzTabM)
                {
                    exportResultMztabFormet(mainWindow, alignmentFileID, normalizedMztabFile, alignmentResult, mspDB, txtDB, analysisFiles, "Normalized", 0, blankFilter, replaceZeroToHalf);
                }
            }
            if (isAreaData == true)
            {
                MatrixExportIonMobilityWithAveStd(areaFile, mainWindow, alignmentFileID, alignmentResult, analysisFiles, mspDB, txtDB, "Area", blankFilter, replaceZeroToHalf);
                if (isExportedAsMzTabM)
                {
                    exportResultMztabFormet(mainWindow, alignmentFileID, areaMztabFile, alignmentResult, mspDB, txtDB, analysisFiles, "Area", 0, blankFilter, replaceZeroToHalf);
                }
            }
            if (isIdData == true) MatrixExportIonMobility(idFile, mainWindow, alignmentFileID, alignmentResult, analysisFiles, mspDB, txtDB, "Id", blankFilter, replaceZeroToHalf);
            if (isRtData == true) MatrixExportIonMobility(retentiontimeMatrixExportFilePath, mainWindow, alignmentFileID, alignmentResult, analysisFiles, mspDB, txtDB, "RT", blankFilter, replaceZeroToHalf);
            if (isMzData == true) MatrixExportIonMobility(mzFile, mainWindow, alignmentFileID, alignmentResult, analysisFiles, mspDB, txtDB, "MZ", blankFilter, replaceZeroToHalf);
            if (snMatrix == true) MatrixExportIonMobility(snFile, mainWindow, alignmentFileID, alignmentResult, analysisFiles, mspDB, txtDB, "SN", blankFilter, replaceZeroToHalf);
            if (isRepSpectra == true)
                exportSpectra(mainWindow, exportFolderPath, alignmentFileID,
                    alignmentResult, exportSpectraFileFormat, targetAnalysisFileID, blankFilter);

            if (isParamExport) {
                ParameterExport(analysisFiles, mainWindow.AnalysisParamForLC, mainWindow.ProjectProperty, paramFile);
            }
            mainWindow.PeakViewerForLcRefresh(focusedFileID);

            if (focusedAlignmentFileID < 0)
            {
                Mouse.OverrideCursor = null;
                mainWindow.IsEnabled = true;
                return;
            }

            mainWindow.AlignmentViewerForLcRefresh(focusedAlignmentFileID);

            Mouse.OverrideCursor = null;
            mainWindow.IsEnabled = true;
        }

        public static void AlignmentResultLipidomicsExportAtIonMobility(MainWindow mainWindow, int alignmentFileID, int targetAnalysisFileID)
        {

            AlignmentResultBean alignmentResult = MessagePackHandler.LoadFromFile<AlignmentResultBean>(mainWindow.AlignmentFiles[alignmentFileID].FilePath);
            //AlignmentResultBean alignmentResultBean = (AlignmentResultBean)DataStorageLcUtility.LoadFromXmlFile(mainWindow.AlignmentFiles[selectedAlignmentFileID].FilePath, typeof(AlignmentResultBean));
            var analysisFiles = mainWindow.AnalysisFiles;
            var projectProp = mainWindow.ProjectProperty;
            var exportFolderPath = projectProp.ExportFolderPath;
            var isRawData = projectProp.RawDatamatrix;
            var isNormalizedData = projectProp.NormalizedDatamatrix;
            var isAreaData = projectProp.PeakareaMatrix;
            var isIdData = projectProp.PeakIdMatrix;
            var isRtData = projectProp.RetentionTimeMatrix;
            var isMzData = projectProp.MzMatrix;
            var isMsmsIncluded = projectProp.MsmsIncludedMatrix;
            var snMatrix = projectProp.SnMatrixExport;
            var isUniqueMs = projectProp.UniqueMs;
            var blankFilter = projectProp.BlankFilter;
            var replaceZeroToHalf = projectProp.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples;
            var isRepSpectra = projectProp.RepresentativeSpectra;
            var isParamExport = projectProp.Parameter;
            var molecularnetworkingedges = projectProp.MolecularNetworkingExport;
            var exportSpectraFileFormat = projectProp.ExportSpectraFileFormat;
            var isExportedAsMzTabM = projectProp.IsExportedAsMzTabM;

            if (isNormalizedData == true && alignmentResult.Normalized == false)
            {
                MessageBox.Show("Data is not normalized yet. If you want to export the normalized data matrix, please at first perform data normalization methods from statistical analysis procedure.", "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                isNormalizedData = false;
            }

            mainWindow.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            int focusedFileID = mainWindow.FocusedFileID;
            int focusedPeakID = mainWindow.FocusedPeakID;

            int focusedAlignmentFileID = mainWindow.FocusedAlignmentFileID;
            int focusedAlignmentPeakID = mainWindow.FocusedAlignmentPeakID;

            mainWindow.PeakViewDataAccessRefresh();
            mainWindow.AlignmentViewDataAccessRefresh();

            var mspDB = mainWindow.MspDB;
            var txtDB = mainWindow.PostIdentificationTxtDB;

            DateTime dt = DateTime.Now;
            string heightFile = exportFolderPath + "\\" + "Height_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string normalizedFile = exportFolderPath + "\\" + "Normalized_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string areaFile = exportFolderPath + "\\" + "Area_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string sampleAxisDeconvoExportFolderPath = exportFolderPath + "\\" + "SampleAxisDec_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute;
            string idFile = exportFolderPath + "\\" + "PeakID_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string retentiontimeMatrixExportFilePath = exportFolderPath + "\\" + "RT_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string mzFile = exportFolderPath + "\\" + "Mz_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string msmsIncludedFile = exportFolderPath + "\\" + "MsmsIncluded_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string ms2AreaFile = exportFolderPath + "\\" + "Ms2Area_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string uniqueMsFile = exportFolderPath + "\\" + "UniqueMass_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string paramFile = exportFolderPath + "\\" + "Parameter_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string edgeFile = exportFolderPath + "\\" + "MolecularNetworkingEdges_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string snFile = exportFolderPath + "\\" + "SN_" + alignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";

            if (isRawData == true) exportIonMobilityLipidomicsAlignmentResult(heightFile, mainWindow, alignmentFileID, alignmentResult, analysisFiles, mspDB, txtDB, "Height", blankFilter, replaceZeroToHalf);

            mainWindow.PeakViewerForLcRefresh(focusedFileID);
            // ((PairwisePlotPeakViewUI)mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.SelectedPlotId = focusedPeakID;

            if (focusedAlignmentFileID < 0)
            {
                Mouse.OverrideCursor = null;
                mainWindow.IsEnabled = true;
                return;
            }

            mainWindow.AlignmentViewerForLcRefresh(focusedAlignmentFileID);
            // ((PairwisePlotAlignmentViewUI)mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.SelectedPlotId = focusedAlignmentPeakID;

            Mouse.OverrideCursor = null;
            mainWindow.IsEnabled = true;
        }

        private static void MatrixExportIonMobility(string outputfile, MainWindow mainWindow, int alignmentFileID, AlignmentResultBean alignmentResult, ObservableCollection<AnalysisFileBean> analysisFiles,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> textDB, string exportType, bool blankFilter, bool replaceZeroToHalf)
        {

            var dclfilepath = mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath;
            var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
            using (var fs = new FileStream(dclfilepath, FileMode.Open, FileAccess.ReadWrite))
            {
                var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
                using (StreamWriter sw = new StreamWriter(outputfile, false, Encoding.ASCII))
                {
                    //Header
                    ResultExportLcUtility.WriteDataMatrixHeaderAtIonMobility(sw, analysisFiles);
                    for (int i = 0; i < alignedSpots.Count; i++)
                    {
                        if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                        ResultExportLcUtility.WriteDataMatrixMetaDataAtIonMobility(sw, alignedSpots[i], new AlignedDriftSpotPropertyBean(), alignedSpots, mspDB, textDB, fs, seekpointList, mainWindow.AnalysisParamForLC);

                        // Replace true zero values with 1/2 of minimum peak height over all samples
                        var nonZeroMin = getInterpolatedValueForMissingValue(alignedSpots[i].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                        ResultExportLcUtility.WriteData(sw, alignedSpots[i].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin);

                        var driftSpots = alignedSpots[i].AlignedDriftSpots;
                        for (int j = 0; j < driftSpots.Count; j++)
                        {
                            if (blankFilter && driftSpots[j].IsBlankFiltered) continue;
                            ResultExportLcUtility.WriteDataMatrixMetaDataAtIonMobility(sw, alignedSpots[i], driftSpots[j], alignedSpots, mspDB, textDB, fs, seekpointList, mainWindow.AnalysisParamForLC);

                            // Replace true zero values with 1/2 of minimum peak height over all samples
                            nonZeroMin = getInterpolatedValueForMissingValue(driftSpots[j].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                            ResultExportLcUtility.WriteData(sw, driftSpots[j].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin);
                        }
                    }
                }
            }
        }

        private static void MatrixExportIonMobilityWithAveStd(string outputfile, MainWindow mainWindow, int alignmentFileID, AlignmentResultBean alignmentResult, ObservableCollection<AnalysisFileBean> analysisFiles,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> textDB, string exportType, bool blankFilter, bool replaceZeroToHalf)
        {

            var dclfilepath = mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath;
            if (exportType != "Height" && exportType != "Normalized" && exportType != "Area")
            {
                MatrixExportIonMobility(outputfile, mainWindow, alignmentFileID, alignmentResult, analysisFiles, mspDB, textDB, exportType, blankFilter, replaceZeroToHalf);
                return;
            }

            var isNormalized = exportType == "Normalized" ? true : false;
            var mode = isNormalized ? BarChartDisplayMode.NormalizedHeight
                                    : exportType == "Height" ? BarChartDisplayMode.OriginalHeight
                                                             : BarChartDisplayMode.OriginalArea;

            var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
            if (alignedSpots == null || alignedSpots.Count == 0) return;

            var projectProp = mainWindow.ProjectProperty;
            var tempClassArray = MsDialStatistics.AverageStdevProperties(alignedSpots[0], analysisFiles, projectProp, mode, false);
            if (tempClassArray.Count == analysisFiles.Count)
            { // meaining no class properties are set.
                MatrixExportIonMobility(outputfile, mainWindow, alignmentFileID, alignmentResult, analysisFiles, mspDB, textDB, exportType, blankFilter, replaceZeroToHalf);
                return;
            }
            using (var fs = new FileStream(dclfilepath, FileMode.Open, FileAccess.ReadWrite))
            {
                var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
                using (StreamWriter sw = new StreamWriter(outputfile, false, Encoding.ASCII))
                {
                    //Header
                    ResultExportLcUtility.WriteDataMatrixHeaderAtIonMobility(sw, analysisFiles, tempClassArray);
                    for (int i = 0; i < alignedSpots.Count; i++)
                    {
                        if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                        ResultExportLcUtility.WriteDataMatrixMetaDataAtIonMobility(sw, alignedSpots[i], new AlignedDriftSpotPropertyBean(), alignedSpots, mspDB, textDB, fs, seekpointList, mainWindow.AnalysisParamForLC);
                        // Replace true zero values with 1/2 of minimum peak height over all samples
                        var nonZeroMin = getInterpolatedValueForMissingValue(alignedSpots[i].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                        var statsList = MsDialStatistics.AverageStdevProperties(alignedSpots[i], analysisFiles, projectProp, mode, false);
                        ResultExportLcUtility.WriteData(sw, alignedSpots[i].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin, statsList);

                        var driftSpots = alignedSpots[i].AlignedDriftSpots;
                        for (int j = 0; j < driftSpots.Count; j++)
                        {
                            if (blankFilter && driftSpots[j].IsBlankFiltered) continue;
                            ResultExportLcUtility.WriteDataMatrixMetaDataAtIonMobility(sw, alignedSpots[i], driftSpots[j], alignedSpots, mspDB, textDB, fs, seekpointList, mainWindow.AnalysisParamForLC);

                            // Replace true zero values with 1/2 of minimum peak height over all samples
                            nonZeroMin = getInterpolatedValueForMissingValue(driftSpots[j].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                            statsList = MsDialStatistics.AverageStdevProperties(driftSpots[j], analysisFiles, projectProp, mode, false);
                            ResultExportLcUtility.WriteData(sw, driftSpots[j].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin, statsList);
                        }
                    }
                }
            }
        }

        private static void exportIonMobilityLipidomicsAlignmentResult(string heightFile, MainWindow mainWindow, int alignmentFileID, AlignmentResultBean alignmentResult,
            ObservableCollection<AnalysisFileBean> analysisFiles,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> textDB, string exportType, bool blankFilter, bool replaceZeroToHalf)
        {
            var fs = File.Open(mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite);
            var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

            var param = mainWindow.AnalysisParamForLC;
            var project = mainWindow.ProjectProperty;
            var filename = System.IO.Path.GetFileNameWithoutExtension(heightFile);
            var directory = System.IO.Path.GetDirectoryName(heightFile);
            var filepath_all = directory + "\\" + filename + "-all.txt";
            var filepath_named = directory + "\\" + filename + "-named.txt";
            var filepath_quant = directory + "\\" + filename + "-quant.txt";
            var mspfile = directory + "\\" + filename + ".msp";

            var lipidlist = new List<string[]>();
            if (project.IonMode == IonMode.Positive)
            {
                lipidlist = new List<string[]>() {
                    new string[] { "ACar", "[M]+" },
                    new string[] { "ADGGA", "[M+NH4]+" },
                    new string[] { "AHexCAS", "[M+NH4]+" },
                    new string[] { "AHexCS", "[M+NH4]+" },
                    new string[] { "AHexSIS", "[M+NH4]+" },
                    new string[] { "BMP", "[M+NH4]+" },
                    new string[] { "BRSE", "[M+NH4]+" },
                    new string[] { "CASE", "[M+NH4]+" },
                    new string[] { "BRSE", "[M+NH4]+" },
                    new string[] { "CE", "[M+NH4]+" },
                    new string[] { "CerP", "[M+H]+" },
                    new string[] { "Cholesterol", "[M-H2O+H]+" },
                    new string[] { "CL", "[M+NH4]+" },
                    new string[] { "CoQ", "[M+H]+" },
                    new string[] { "DAG", "[M+NH4]+" },
                    new string[] { "DCAE", "[M+NH4]+" },
                    new string[] { "DGCC", "[M+H]+" },
                    new string[] { "DGTS", "[M+H]+" },
                    new string[] { "DGTA", "[M+H]+" },
                    new string[] { "EtherDAG", "[M+NH4]+" },
                    new string[] { "EtherLPC", "[M+H]+" },
                    new string[] { "EtherLPE", "[M+H]+" },
                    new string[] { "EtherPE", "[M+H]+" },
                    new string[] { "EtherTAG", "[M+NH4]+" },
                    new string[] { "GDCAE", "[M+NH4]+" },
                    new string[] { "GLCAE", "[M+NH4]+" },
                    new string[] { "GM3", "[M+NH4]+" },
                    new string[] { "HBMP", "[M+NH4]+" },
                    new string[] { "Hex2Cer", "[M+H]+" },
                    new string[] { "Hex3Cer", "[M+H]+" },
                    new string[] { "LDGCC", "[M+H]+" },
                    new string[] { "LDGTS", "[M+H]+" },
                    new string[] { "LDGTA", "[M+H]+" },
                    new string[] { "LPC", "[M+H]+" },
                    new string[] { "LPE", "[M+H]+" },
                    new string[] { "MAG", "[M+NH4]+" },
                    new string[] { "NAAG", "[M+H]+" },
                    new string[] { "NAAGS", "[M+H]+" },
                    new string[] { "NAAO", "[M+H]+" },
                    new string[] { "NAE", "[M+H]+" },
                    new string[] { "Phytosphingosine", "[M+H]+" },
                    new string[] { "SHex", "[M+NH4]+" },
                    new string[] { "SHexCer", "[M+H]+" },
                    new string[] { "SISE", "[M+NH4]+" },
                    new string[] { "SL", "[M+H]+" },
                    new string[] { "Sphinganine", "[M+H]+" },
                    new string[] { "Sphingosine", "[M+H]+" },
                    new string[] { "SQDG", "[M+NH4]+" },
                    new string[] { "TAG", "[M+NH4]+" },
                    new string[] { "TDCAE", "[M+NH4]+" },
                    new string[] { "TLCAE", "[M+NH4]+" },
                    new string[] { "VAE", "[M+Na]+" },
                    new string[] { "Vitamine", "[M+H]+" },
                };
            }
            else
            {
                lipidlist = new List<string[]>() {
                    new string[] { "AHexCer", "[M+CH3COO]-" },
                    new string[] { "ASM", "[M+CH3COO]-" },
                    new string[] { "BASulfate", "[M-H]-" },
                    new string[] { "BileAcid", "[M-H]-" },
                    new string[] { "Cer-ADS", "[M+CH3COO]-" },
                    new string[] { "Cer-AP", "[M+CH3COO]-" },
                    new string[] { "Cer-AS", "[M+CH3COO]-" },
                    new string[] { "Cer-BDS", "[M+CH3COO]-" },
                    new string[] { "Cer-BS", "[M+CH3COO]-" },
                    new string[] { "Cer-EBDS", "[M+CH3COO]-" },
                    new string[] { "Cer-EOS", "[M+CH3COO]-" },
                    new string[] { "Cer-EODS", "[M+CH3COO]-" },
                    new string[] { "Cer-HDS", "[M+CH3COO]-" },
                    new string[] { "Cer-HS", "[M+CH3COO]-" },
                    new string[] { "Cer-NDS", "[M+CH3COO]-" },
                    new string[] { "Cer-NP", "[M+CH3COO]-" },
                    new string[] { "Cer-NS", "[M+CH3COO]-" },
                    new string[] { "CL", "[M-H]-" },
                    new string[] { "DGDG", "[M+CH3COO]-" },
                    new string[] { "DGGA", "[M-H]-" },
                    new string[] { "DLCL", "[M-H]-" },
                    new string[] { "EtherLPG", "[M-H]-" },
                    new string[] { "EtherMGDG", "[M+CH3COO]-" },
                    new string[] { "EtherDGDG", "[M+CH3COO]-" },
                    new string[] { "EtherOxPE", "[M-H]-" },
                    new string[] { "EtherPC", "[M+CH3COO]-" },
                    new string[] { "EtherPE", "[M-H]-" },
                    new string[] { "EtherPG", "[M-H]-" },
                    new string[] { "EtherPI", "[M-H]-" },
                    new string[] { "EtherPS", "[M-H]-" },
                    new string[] { "FA", "[M-H]-" },
                    new string[] { "FAHFA", "[M-H]-" },
                    new string[] { "GM3", "[M-H]-" },
                    new string[] { "HBMP", "[M-H]-" },
                    new string[] { "HexCer-AP", "[M+CH3COO]-" },
                    new string[] { "HexCer-EOS", "[M+CH3COO]-" },
                    new string[] { "HexCer-HDS", "[M+CH3COO]-" },
                    new string[] { "HexCer-HS", "[M+CH3COO]-" },
                    new string[] { "HexCer-NDS", "[M+CH3COO]-" },
                    new string[] { "HexCer-NS", "[M+CH3COO]-" },
                    new string[] { "LCL", "[M-H]-" },
                    new string[] { "LNAPE", "[M-H]-" },
                    new string[] { "LNAPS", "[M-H]-" },
                    new string[] { "LPA", "[M-H]-" },
                    new string[] { "LPG", "[M-H]-" },
                    new string[] { "LPI", "[M-H]-" },
                    new string[] { "LPS", "[M-H]-" },
                    new string[] { "MGDG", "[M+CH3COO]-" },
                    new string[] { "OxPC", "[M+CH3COO]-" },
                    new string[] { "OxPE", "[M-H]-" },
                    new string[] { "OxPG", "[M-H]-" },
                    new string[] { "OxPI", "[M-H]-" },
                    new string[] { "OxPS", "[M-H]-" },
                    new string[] { "PA", "[M-H]-" },
                    new string[] { "PC", "[M+CH3COO]-" },
                    new string[] { "PE", "[M-H]-" },
                    new string[] { "PE-Cer", "[M-H]-" },
                    new string[] { "PEtOH", "[M-H]-" },
                    new string[] { "PG", "[M-H]-" },
                    new string[] { "PI", "[M-H]-" },
                    new string[] { "PI-Cer", "[M-H]-" },
                    new string[] { "PMeOH", "[M-H]-" },
                    new string[] { "PS", "[M-H]-" },
                    new string[] { "PT", "[M-H]-" },
                    new string[] { "SHexCer", "[M-H]-" },
                    new string[] { "SM", "[M-H]-" },
                    new string[] { "SQDG", "[M-H]-" },
                    new string[] { "SSulfate", "[M-H]-" },
                    new string[] { "Vitamine", "[M-H]-" },
                };
            }

            var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection.ToList();
            using (StreamWriter sw = new StreamWriter(filepath_all, false, Encoding.ASCII))
            {
                //Header
                ResultExportLcUtility.WriteDataMatrixHeaderAtIonMobility(sw, analysisFiles);
                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                    ResultExportLcUtility.WriteLipidomicsDataMatrixMetaDataAtIonMobility(sw, alignedSpots[i], new AlignedDriftSpotPropertyBean(),
                        alignedSpots, mspDB, textDB, fs, seekpointList, mainWindow.AnalysisParamForLC);

                    // Replace true zero values with 1/2 of minimum peak height over all samples
                    var nonZeroMin = getInterpolatedValueForMissingValue(alignedSpots[i].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                    ResultExportLcUtility.WriteData(sw, alignedSpots[i].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin);

                    var driftSpots = alignedSpots[i].AlignedDriftSpots;
                    for (int j = 0; j < driftSpots.Count; j++)
                    {
                        if (blankFilter && driftSpots[j].IsBlankFiltered) continue;
                        ResultExportLcUtility.WriteLipidomicsDataMatrixMetaDataAtIonMobility(sw, alignedSpots[i], driftSpots[j], alignedSpots, mspDB, textDB, fs, seekpointList, mainWindow.AnalysisParamForLC);

                        // Replace true zero values with 1/2 of minimum peak height over all samples
                        nonZeroMin = getInterpolatedValueForMissingValue(driftSpots[j].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                        ResultExportLcUtility.WriteData(sw, driftSpots[j].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin);
                    }
                }
            }

            alignedSpots = alignedSpots.OrderBy(n => n.MetaboliteName).ToList();

            using (StreamWriter sw = new StreamWriter(filepath_named, false, Encoding.ASCII))
            {
                //Header
                ResultExportLcUtility.WriteDataMatrixHeaderAtIonMobility(sw, analysisFiles);
                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                    if (alignedSpots[i].MetaboliteName.Contains("w/o MS2:")) continue;
                    if (alignedSpots[i].MetaboliteName.Contains("Unsettled:")) continue;
                    if (alignedSpots[i].MetaboliteName.Contains("Unknown")) continue;
                    if (alignedSpots[i].MetaboliteName == string.Empty) continue;

                    ResultExportLcUtility.WriteLipidomicsDataMatrixMetaDataAtIonMobility(sw, alignedSpots[i], new AlignedDriftSpotPropertyBean(),
                        alignedSpots, mspDB, textDB, fs, seekpointList, mainWindow.AnalysisParamForLC);

                    // Replace true zero values with 1/2 of minimum peak height over all samples
                    var nonZeroMin = getInterpolatedValueForMissingValue(alignedSpots[i].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                    ResultExportLcUtility.WriteData(sw, alignedSpots[i].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin);

                    var driftSpots = alignedSpots[i].AlignedDriftSpots;
                    for (int j = 0; j < driftSpots.Count; j++)
                    {
                        if (blankFilter && driftSpots[j].IsBlankFiltered) continue;
                        if (driftSpots[j].MetaboliteName.Contains("w/o MS2:")) continue;
                        if (driftSpots[j].MetaboliteName.Contains("Unsettled:")) continue;
                        if (driftSpots[j].MetaboliteName.Contains("Unknown")) continue;
                        if (driftSpots[j].MetaboliteName == string.Empty) continue;
                        ResultExportLcUtility.WriteLipidomicsDataMatrixMetaDataAtIonMobility(sw, alignedSpots[i], driftSpots[j], alignedSpots, mspDB, textDB, fs, seekpointList, mainWindow.AnalysisParamForLC);

                        // Replace true zero values with 1/2 of minimum peak height over all samples
                        nonZeroMin = getInterpolatedValueForMissingValue(driftSpots[j].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                        ResultExportLcUtility.WriteData(sw, driftSpots[j].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin);
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter(filepath_quant, false, Encoding.ASCII))
            {
                //Header
                ResultExportLcUtility.WriteDataMatrixHeaderAtIonMobility(sw, analysisFiles);
                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                    if (alignedSpots[i].MetaboliteName.Contains("w/o MS2:")) continue;
                    if (alignedSpots[i].MetaboliteName.Contains("Unsettled:")) continue;
                    if (alignedSpots[i].MetaboliteName.Contains("Unknown")) continue;
                    if (alignedSpots[i].MetaboliteName == string.Empty) continue;

                    var comment = alignedSpots[i].Comment;
                    var isQuantified = checkifQuantifiedList(alignedSpots[i].MetaboliteName, alignedSpots[i].AdductIonName, comment, project.IonMode, lipidlist);
                    if (isQuantified == false) continue;

                    ResultExportLcUtility.WriteLipidomicsDataMatrixMetaDataAtIonMobility(sw, alignedSpots[i], new AlignedDriftSpotPropertyBean(),
                        alignedSpots, mspDB, textDB, fs, seekpointList, mainWindow.AnalysisParamForLC);

                    // Replace true zero values with 1/2 of minimum peak height over all samples
                    var nonZeroMin = getInterpolatedValueForMissingValue(alignedSpots[i].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                    ResultExportLcUtility.WriteData(sw, alignedSpots[i].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin);

                    var driftSpots = alignedSpots[i].AlignedDriftSpots;
                    for (int j = 0; j < driftSpots.Count; j++)
                    {
                        if (blankFilter && driftSpots[j].IsBlankFiltered) continue;
                        if (driftSpots[j].MetaboliteName.Contains("w/o MS2:")) continue;
                        if (driftSpots[j].MetaboliteName.Contains("Unsettled:")) continue;
                        if (driftSpots[j].MetaboliteName.Contains("Unknown")) continue;
                        if (driftSpots[j].MetaboliteName == string.Empty) continue;

                        comment = driftSpots[j].Comment;
                        isQuantified = checkifQuantifiedList(driftSpots[j].MetaboliteName, driftSpots[j].AdductIonName, comment, project.IonMode, lipidlist);
                        if (isQuantified == false) continue;
                        ResultExportLcUtility.WriteLipidomicsDataMatrixMetaDataAtIonMobility(sw, alignedSpots[i], driftSpots[j], alignedSpots, mspDB, textDB, fs, seekpointList, mainWindow.AnalysisParamForLC);

                        // Replace true zero values with 1/2 of minimum peak height over all samples
                        nonZeroMin = getInterpolatedValueForMissingValue(driftSpots[j].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                        ResultExportLcUtility.WriteData(sw, driftSpots[j].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin);
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter(mspfile, false, Encoding.ASCII))
            {

                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (!alignedSpots[i].MsmsIncluded) continue;
                    var driftSpots = alignedSpots[i].AlignedDriftSpots;
                    for (int j = 0; j < driftSpots.Count; j++)
                    {
                        if (!driftSpots[j].MsmsIncluded) continue;
                        ResultExportLcUtility.WriteMs2DecDataAsMsp(sw, alignedSpots[i], driftSpots[j], mspDB, textDB,
                        fs, seekpointList, param);
                    }
                }
            }

            fs.Dispose();
            fs.Close();
        }

        private static float getInterpolatedValueForMissingValue(ObservableCollection<AlignedPeakPropertyBean> alignedPeakPropertyBeanCollection,
            bool replaceZeroToHalf, string exportType)
        {
            var nonZeroMin = float.MaxValue;
            if (replaceZeroToHalf)
            {
                foreach (var peak in alignedPeakPropertyBeanCollection)
                {

                    var variable = 0.0F;
                    switch (exportType)
                    {
                        case "Height":
                            variable = peak.Variable;
                            break;
                        case "Normalized":
                            variable = peak.NormalizedVariable;
                            break;
                        case "Area":
                            variable = peak.Area;
                            break;
                    }

                    if (variable > 0 && nonZeroMin > variable)
                        nonZeroMin = variable;
                }

                if (nonZeroMin == double.MaxValue)
                    nonZeroMin = 1;
            }
            return nonZeroMin;
        }

        public static void GnpsResultExport(MainWindow mainWindow, int selectedAlignmentFileID, int selectedAnalysisFileID)
        {
            var alignmentResultBean = MessagePackHandler.LoadFromFile<AlignmentResultBean>(mainWindow.AlignmentFiles[selectedAlignmentFileID].FilePath);
            var analysisFiles = mainWindow.AnalysisFiles;
            var projectProp = mainWindow.ProjectProperty;
            var exportFolderPath = projectProp.ExportFolderPath;
            var isRawData = projectProp.RawDatamatrix;
            var isNormalizedData = projectProp.NormalizedDatamatrix;
            var isAreaData = projectProp.PeakareaMatrix;
            var isIdData = projectProp.PeakIdMatrix;
            var isRtData = projectProp.RetentionTimeMatrix;
            var isMzData = projectProp.MzMatrix;
            var isMsmsIncluded = projectProp.MsmsIncludedMatrix;
            var snMatrix = projectProp.SnMatrixExport;
            var isUniqueMs = projectProp.UniqueMs;
            var blankFilter = projectProp.BlankFilter;
            var replaceZeroToHalf = projectProp.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples;
            var isRepSpectra = projectProp.RepresentativeSpectra;
            var isParamExport = projectProp.Parameter;
            var molecularnetworkingedges = projectProp.MolecularNetworkingExport;
            var exportSpectraFileFormat = projectProp.ExportSpectraFileFormat;
            var isExportedAsMzTabM = projectProp.IsExportedAsMzTabM;
            mainWindow.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            int focusedFileID = mainWindow.FocusedFileID;
            int focusedPeakID = mainWindow.FocusedPeakID;

            int focusedAlignmentFileID = mainWindow.FocusedAlignmentFileID;
            int focusedAlignmentPeakID = mainWindow.FocusedAlignmentPeakID;

            mainWindow.PeakViewDataAccessRefresh();
            mainWindow.AlignmentViewDataAccessRefresh();

            DateTime dt = DateTime.Now;
            string gnpsTableFile = exportFolderPath + "\\" + "GnpsTable_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string gnpsMgfFile = exportFolderPath + "\\" + "GnpsMgf_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".mgf";
            string gnpsEdgeFile = exportFolderPath + "\\" + "GnpsEdge_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";

            exportGnpsDataMatrix(mainWindow, selectedAlignmentFileID, gnpsTableFile, alignmentResultBean, mainWindow.MspDB, mainWindow.PostIdentificationTxtDB, analysisFiles, blankFilter, replaceZeroToHalf);
            exportGnpsSpectra(mainWindow, gnpsMgfFile, selectedAlignmentFileID, alignmentResultBean, blankFilter);
            exportGnpsEdgeTable(mainWindow, selectedAlignmentFileID, gnpsEdgeFile, alignmentResultBean, mainWindow.MspDB, mainWindow.PostIdentificationTxtDB, analysisFiles, blankFilter, replaceZeroToHalf);

            mainWindow.PeakViewerForLcRefresh(focusedFileID);
            ((PairwisePlotPeakViewUI)mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.SelectedPlotId = focusedPeakID;

            if (focusedAlignmentFileID < 0)
            {
                Mouse.OverrideCursor = null;
                mainWindow.IsEnabled = true;
                return;
            }

            mainWindow.AlignmentViewerForLcRefresh(focusedAlignmentFileID);
            ((PairwisePlotAlignmentViewUI)mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.SelectedPlotId = focusedAlignmentPeakID;

            Mouse.OverrideCursor = null;
            mainWindow.IsEnabled = true;
        }


        private static void exportGnpsSpectra(MainWindow mainWindow, string gnpsMgfFile,
            int alignmentFileID, AlignmentResultBean alignmentResultBean, bool blankFilter)
        {

            DateTime dt = DateTime.Now;

            var param = mainWindow.AnalysisParamForLC;
            if (param == null) return;
            var ionMobility = param.IsIonMobility;

            using (StreamWriter sw = new StreamWriter(gnpsMgfFile, false, Encoding.ASCII))
            {
                var fs = File.Open(mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite);
                var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

                if (ionMobility)
                {
                    var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
                    var counter = 0;
                    for (int i = 0; i < alignedSpots.Count; i++)
                    {
                        if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                        if (!alignedSpots[i].MsmsIncluded) continue;
                        if (alignedSpots[i].MasterID == 0) continue;

                        var driftSpots = alignedSpots[i].AlignedDriftSpots;
                        for (int j = 0; j < driftSpots.Count; j++)
                        {
                            if (blankFilter && driftSpots[j].IsBlankFiltered) continue;
                            if (!driftSpots[j].MsmsIncluded) continue;

                            //ResultExportLcUtility.WriteDeconvolutedGnpsMgf(sw, fs, seekpointList, alignedSpots[i], driftSpots[j], counter + 1);
                            ResultExportLcUtility.WriteDeconvolutedGnpsMgf(sw, fs, seekpointList, alignedSpots[i], driftSpots[j], driftSpots[j].MasterID);
                            counter++;
                        }
                    }
                }
                else
                {
                    List<AlignmentPropertyBean> alignedSpots = null;
                    //if (blankFilter)
                    //    alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection.Where(n => n.MsmsIncluded && n.IsBlankFiltered == false).ToList();
                    //else
                    //    alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection.Where(n => n.MsmsIncluded).ToList();

                    alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection.ToList();
                    //if (blankFilter)
                    //    alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection.Where(n => n.IsBlankFiltered == false).ToList();
                    //else
                    //    alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection.ToList();

                    for (int i = 0; i < alignedSpots.Count; i++)
                    {
                        if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                        if (alignedSpots[i].AlignmentID == 0) continue;

                        //ResultExportLcUtility.WriteDeconvolutedGnpsMgf(sw, fs, seekpointList, alignedSpots[i], i + 1);
                        ResultExportLcUtility.WriteDeconvolutedGnpsMgf(sw, fs, seekpointList, alignedSpots[i], alignedSpots[i].AlignmentID);
                    }
                }


                fs.Dispose();
                fs.Close();
            }
        }

        private static void exportGnpsDataMatrix(MainWindow mainWindow, int alignmentFileID, string gnpsTableFile,
            AlignmentResultBean alignmentResultBean, List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> textDB,
            ObservableCollection<AnalysisFileBean> analysisFiles, bool blankFilter, bool replaceZeroToHalf)
        {

            var fs = File.Open(mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite);
            var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
            var param = mainWindow.AnalysisParamForLC;
            if (param == null) return;
            var isIonMobility = param.IsIonMobility;

            using (var sw = new StreamWriter(gnpsTableFile, false, Encoding.ASCII))
            {
                //Header
                if (isIonMobility)
                    ResultExportLcUtility.WriteGnpsIonmobilityDataMatrixHeader(sw, analysisFiles);
                else
                    ResultExportLcUtility.WriteGnpsDataMatrixHeader(sw, analysisFiles);

                if (isIonMobility)
                {
                    var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
                    var counter = 0;
                    for (int i = 0; i < alignedSpots.Count; i++)
                    {
                        if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                        if (!alignedSpots[i].MsmsIncluded) continue;
                        if (alignedSpots[i].MasterID == 0) continue;

                        var driftSpots = alignedSpots[i].AlignedDriftSpots;
                        for (int j = 0; j < driftSpots.Count; j++)
                        {
                            if (blankFilter && driftSpots[j].IsBlankFiltered) continue;
                            if (!driftSpots[j].MsmsIncluded) continue;

                            //ResultExportLcUtility.WriteGnpsDataMatrixMetaDataAtIonMobility(sw, alignedSpots[i], driftSpots[j], 
                            //    alignedSpots, mspDB, textDB, fs, seekpointList, mainWindow.AnalysisParamForLC, counter + 1);

                            ResultExportLcUtility.WriteGnpsDataMatrixMetaDataAtIonMobility(sw, alignedSpots[i], driftSpots[j],
                                alignedSpots, mspDB, textDB, fs, seekpointList, mainWindow.AnalysisParamForLC, driftSpots[j].MasterID);

                            counter++;

                            // Replace true zero values with 1/2 of minimum peak height over all samples
                            var nonZeroMin = getInterpolatedValueForMissingValue(driftSpots[j].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, "Height");
                            ResultExportLcUtility.WriteData(sw, driftSpots[j].AlignedPeakPropertyBeanCollection, "Height", replaceZeroToHalf, nonZeroMin);
                        }
                    }
                }
                else
                {
                    List<AlignmentPropertyBean> alignedSpots = null;
                    //if (blankFilter)
                    //    alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection.Where(n => n.MsmsIncluded && n.IsBlankFiltered == false).ToList();
                    //else
                    //    alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection.Where(n => n.MsmsIncluded).ToList();
                    alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection.ToList();
                    //if (blankFilter)
                    //    alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection.Where(n => n.IsBlankFiltered == false).ToList();
                    //else
                    //    alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection.ToList();

                    //From the second
                    for (int i = 0; i < alignedSpots.Count; i++)
                    {
                        if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                        if (alignedSpots[i].AlignmentID == 0) continue;
                        //ResultExportLcUtility.WriteDataMatrixMetaDataForGnps(sw, alignedSpots[i], alignedSpots, mspDB, textDB, fs, seekpointList, i + 1, mainWindow.AnalysisParamForLC);
                        ResultExportLcUtility.WriteDataMatrixMetaDataForGnps(sw, alignedSpots[i], alignedSpots, mspDB, textDB, fs, seekpointList, alignedSpots[i].AlignmentID, mainWindow.AnalysisParamForLC);

                        // Replace true zero values with 1/2 of minimum peak height over all samples
                        var nonZeroMin = double.MaxValue;
                        if (replaceZeroToHalf)
                        {
                            foreach (var peak in alignedSpots[i].AlignedPeakPropertyBeanCollection)
                            {
                                if (peak.Variable > 0.0001 && nonZeroMin > peak.Variable)
                                    nonZeroMin = peak.Variable;
                            }

                            if (nonZeroMin == double.MaxValue)
                                nonZeroMin = 1;
                        }

                        for (int j = 0; j < alignedSpots[i].AlignedPeakPropertyBeanCollection.Count; j++)
                        {
                            var value = Math.Round(alignedSpots[i].AlignedPeakPropertyBeanCollection[j].Variable, 0);
                            if (replaceZeroToHalf && value <= 0.0001)
                            {
                                value = nonZeroMin * 0.1;
                            }
                            if (j == alignedSpots[i].AlignedPeakPropertyBeanCollection.Count - 1)
                                sw.WriteLine(value);
                            else
                                sw.Write(value + "\t");
                        }
                    }
                }
            }

            fs.Dispose();
            fs.Close();
        }

        private static void exportGnpsEdgeTable(MainWindow mainWindow, int alignmentFileID, string gnpsTableFile,
           AlignmentResultBean alignmentResultBean, List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> textDB,
           ObservableCollection<AnalysisFileBean> analysisFiles, bool blankFilter, bool replaceZeroToHalf)
        {
            var edges = new List<ExportableLcPeakEdge>();
            using (var fs = File.Open(mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
                var param = mainWindow.AnalysisParamForLC;
                if (param == null) return;
                var isIonMobility = param.IsIonMobility;

                if (isIonMobility)
                {
                    var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
                    foreach (var alignedSpot in alignedSpots)
                    {
                        if (alignedSpot.MasterID == 0) continue;
                        if (blankFilter && alignedSpot.IsBlankFiltered) continue;
                        if (alignedSpot.PeakLinks != null && alignedSpot.PeakLinks.Count > 0)
                        {
                            foreach (var peak in alignedSpot.PeakLinks)
                            {
                                var linkedID = peak.LinkedPeakID;
                                var linkedProp = peak.Character;
                                var linkedSpot = alignedSpots[linkedID];
                                if (linkedSpot.MasterID == 0) continue;

                                var sourceID = Math.Min(alignedSpot.AlignmentID, linkedSpot.AlignmentID);
                                var targetID = Math.Max(alignedSpot.AlignmentID, linkedSpot.AlignmentID);
                                var sourceMasterID = Math.Min(alignedSpot.MasterID, linkedSpot.MasterID);
                                var targetMasterID = Math.Max(alignedSpot.MasterID, linkedSpot.MasterID);
                                var type = "Chromatogram-based annotation";
                                var annotation = "Similar chromatogram";
                                var score = 1.0;
                                var mzdiff = Math.Round(Math.Abs(alignedSpots[sourceID].CentralAccurateMass - alignedSpots[targetID].CentralAccurateMass), 5);

                                if (linkedProp == PeakLinkFeatureEnum.CorrelSimilar)
                                {
                                    type = "Alignment-based annotation";
                                    annotation = "Ion correlation among samples";
                                    foreach (var corr in alignedSpot.AlignmentSpotVariableCorrelations.OrEmptyIfNull())
                                    {
                                        if (corr.CorrelateAlignmentID == linkedSpot.AlignmentID)
                                        {
                                            score = Math.Round(corr.CorrelationScore, 3);
                                        }
                                    }
                                }
                                else if (linkedProp == PeakLinkFeatureEnum.Adduct)
                                {
                                    type = "Adduct annotation";
                                    annotation = alignedSpots[sourceID].AdductIonName + "_" + alignedSpots[targetID].AdductIonName +
                                        "_dm/z" + mzdiff;
                                }
                                else if (linkedProp == PeakLinkFeatureEnum.FoundInUpperMsMs && alignedSpot.CentralAccurateMass < linkedSpot.CentralAccurateMass)
                                {
                                    type = "MS2-based annotation";
                                    annotation = "The precursor ion in higher m/z's MS/MS; " + "_dm/z" + mzdiff;
                                }

                                var uniquestring = sourceMasterID + "_" + targetMasterID + "_" + annotation;
                                var uniquestringShort = sourceMasterID + "_" + targetMasterID;
                                edges.Add(new ExportableLcPeakEdge()
                                {
                                    SourceID = sourceMasterID,
                                    TargetID = targetMasterID,
                                    Annotation = annotation,
                                    Type = type,
                                    Score = score.ToString(),
                                    EdgeID = uniquestring,
                                    EdgeIdShort = uniquestringShort
                                });

                                foreach (var drift in alignedSpot.AlignedDriftSpots)
                                {
                                    sourceMasterID = Math.Min(drift.MasterID, alignedSpot.MasterID);
                                    targetMasterID = Math.Max(drift.MasterID, alignedSpot.MasterID);
                                    type = "RT-Mobility link";
                                    annotation = "Parent " + alignedSpot.MasterID + "_Mobility " + drift.MasterID;
                                    uniquestring = sourceMasterID + "_" + targetMasterID + "_" + annotation;
                                    uniquestringShort = sourceMasterID + "_" + targetMasterID + "_" + annotation;
                                    edges.Add(new ExportableLcPeakEdge()
                                    {
                                        SourceID = sourceMasterID,
                                        TargetID = targetMasterID,
                                        Annotation = annotation,
                                        Type = type,
                                        Score = score.ToString(),
                                        EdgeID = uniquestring,
                                        EdgeIdShort = uniquestringShort
                                    });
                                }
                            }
                        }
                    }
                    //if (edges.Count > 0) {
                    //    edges = edges.GroupBy(n => n.EdgeID).Select(g => g.First()).ToList();
                    //}
                }
                else
                {
                    var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;

                    //From the second
                    foreach (var alignedSpot in alignedSpots)
                    {
                        if (alignedSpot.AlignmentID == 0) continue;
                        if (blankFilter && alignedSpot.IsBlankFiltered) continue;
                        if (alignedSpot.PeakLinks != null && alignedSpot.PeakLinks.Count > 0)
                        {
                            foreach (var peak in alignedSpot.PeakLinks)
                            {
                                var linkedID = peak.LinkedPeakID;
                                var linkedProp = peak.Character;
                                var linkedSpot = alignedSpots[linkedID];
                                if (linkedSpot.AlignmentID == 0) continue;

                                var sourceID = Math.Min(alignedSpot.AlignmentID, linkedSpot.AlignmentID);
                                var targetID = Math.Max(alignedSpot.AlignmentID, linkedSpot.AlignmentID);
                                var type = "Chromatogram-based annotation";
                                var annotation = "Similar chromatogram";
                                var score = 1.0;
                                var mzdiff = Math.Round(Math.Abs(alignedSpots[sourceID].CentralAccurateMass - alignedSpots[targetID].CentralAccurateMass), 5);

                                if (linkedProp == PeakLinkFeatureEnum.CorrelSimilar)
                                {
                                    type = "Alignment-based annotation";
                                    annotation = "Ion correlation among samples";
                                    foreach (var corr in alignedSpot.AlignmentSpotVariableCorrelations.OrEmptyIfNull())
                                    {
                                        if (corr.CorrelateAlignmentID == linkedSpot.AlignmentID)
                                        {
                                            score = Math.Round(corr.CorrelationScore, 3);
                                        }
                                    }
                                }
                                else if (linkedProp == PeakLinkFeatureEnum.Adduct)
                                {
                                    type = "Adduct annotation";
                                    annotation = alignedSpots[sourceID].AdductIonName + " " + alignedSpots[targetID].AdductIonName +
                                        " dm/z" + mzdiff;
                                }
                                else if (linkedProp == PeakLinkFeatureEnum.FoundInUpperMsMs && alignedSpot.CentralAccurateMass < linkedSpot.CentralAccurateMass)
                                {
                                    type = "MS2-based annotation";
                                    annotation = "The precursor ion in higher m/z's MS/MS; " + "_dm/z" + mzdiff;
                                }

                                var uniquestring = sourceID + "_" + targetID + "_" + annotation;
                                var uniquestringShort = sourceID + "_" + targetID;
                                edges.Add(new ExportableLcPeakEdge()
                                {
                                    SourceID = sourceID,
                                    TargetID = targetID,
                                    Annotation = annotation,
                                    Type = type,
                                    Score = score.ToString(),
                                    EdgeID = uniquestring,
                                    EdgeIdShort = uniquestringShort
                                });
                            }
                        }
                    }
                }
            }

            if (edges.Count > 0)
            {
                edges = edges.GroupBy(n => n.EdgeID).Select(g => g.First()).ToList();
            }

            var filename = System.IO.Path.GetFileNameWithoutExtension(gnpsTableFile);
            var directory = System.IO.Path.GetDirectoryName(gnpsTableFile);
            var edge_peakshape = directory + "\\" + filename + "_peakshape.csv";
            var edge_ioncorrelation = directory + "\\" + filename + "_ioncorrelation.csv";
            var edge_adduct = directory + "\\" + filename + "_adduct.csv";
            var edge_insource = directory + "\\" + filename + "_insource.csv";

            using (var sw = new StreamWriter(edge_peakshape, false, Encoding.ASCII))
            {
                //Header
                var header = new List<string>() { "ID1", "ID2", "EdgeType", "Score", "Annotation" };
                sw.WriteLine(String.Join(",", header.ToArray()));
                foreach (var edge in edges.Where(n => n.Type == "Chromatogram-based annotation"))
                {
                    var field = new List<string>() { edge.SourceID.ToString(), edge.TargetID.ToString(), edge.Type, edge.Score, edge.Annotation };
                    sw.WriteLine(String.Join(",", field));
                }
            }

            using (var sw = new StreamWriter(edge_ioncorrelation, false, Encoding.ASCII))
            {
                //Header
                var header = new List<string>() { "ID1", "ID2", "EdgeType", "Score", "Annotation" };
                sw.WriteLine(String.Join(",", header.ToArray()));
                foreach (var edge in edges.Where(n => n.Type == "Alignment-based annotation"))
                {
                    var field = new List<string>() { edge.SourceID.ToString(), edge.TargetID.ToString(), edge.Type, edge.Score, edge.Annotation };
                    sw.WriteLine(String.Join(",", field));
                }
            }

            using (var sw = new StreamWriter(edge_insource, false, Encoding.ASCII))
            {
                //Header
                var header = new List<string>() { "ID1", "ID2", "EdgeType", "Score", "Annotation" };
                sw.WriteLine(String.Join(",", header.ToArray()));
                foreach (var edge in edges.Where(n => n.Type == "MS2-based annotation"))
                {
                    var field = new List<string>() { edge.SourceID.ToString(), edge.TargetID.ToString(), edge.Type, edge.Score, edge.Annotation };
                    sw.WriteLine(String.Join(",", field));
                }
            }

            using (var sw = new StreamWriter(edge_adduct, false, Encoding.ASCII))
            {
                //Header
                var header = new List<string>() { "ID1", "ID2", "EdgeType", "Score", "Annotation" };
                sw.WriteLine(String.Join(",", header.ToArray()));
                foreach (var edge in edges.Where(n => n.Type == "Adduct annotation"))
                {
                    var field = new List<string>() { edge.SourceID.ToString(), edge.TargetID.ToString(), edge.Type, edge.Score, edge.Annotation };
                    sw.WriteLine(String.Join(",", field));
                }
            }
        }

        public static void MsAnnotationTagExport(MainWindow mainWindow, ObservableCollection<PeakAreaBean> peakAreaCollection,
            int peakID, ExportspectraType spectraType, MatExportOption exportOption)
        {
            var dt = DateTime.Now;
            var timeString = dt.Year.ToString() + "_" + dt.Month.ToString() + "_" + dt.Day.ToString() + "_" + dt.Hour.ToString() + dt.Minute.ToString();
            var fileString = mainWindow.AnalysisFiles[mainWindow.FocusedFileID].AnalysisFilePropertyBean.AnalysisFileName;
            var param = mainWindow.AnalysisParamForLC;

            for (int i = 0; i < peakAreaCollection.Count; i++)
            {
                var peakArea = peakAreaCollection[i];
                var isIdentified = (peakArea.LibraryID >= 0 || peakArea.PostIdentificationLibraryId >= 0) && !peakArea.MetaboliteName.Contains("w/o") ? true : false;
                var isMonoisotopic = peakArea.IsotopeWeightNumber == 0 ? true : false;
                var isMsmsAcquired = peakArea.Ms2LevelDatapointNumber >= 0 ? true : false;

                switch (exportOption)
                {
                    case MatExportOption.AllPeaks: break;
                    case MatExportOption.OnlyFocusedPeak:
                        if (!param.IsIonMobility && peakID != peakArea.PeakID) continue;
                        break;
                    case MatExportOption.IdentifiedPeaks:
                        if (!isIdentified) continue;
                        break;
                    case MatExportOption.MonoisotopicAndMsmsPeaks:
                        if (!(isMonoisotopic && isMsmsAcquired)) continue;
                        break;
                    case MatExportOption.MsmsPeaks:
                        if (!isMsmsAcquired) continue;
                        break;
                    case MatExportOption.UnknownPeaks:
                        if (isIdentified) continue;
                        break;
                    case MatExportOption.UnknownPeaksWithoutIsotope:
                        if (isIdentified || !isMonoisotopic) continue;
                        break;
                }

                if (!param.IsIonMobility)
                {
                    var filePath = mainWindow.ProjectProperty.MsAnnotationTagsFolderPath + "\\" + "Peak ID " + peakArea.PeakID + "_"
                        + Math.Round(peakArea.RtAtPeakTop, 2).ToString() + "_" + Math.Round(peakArea.AccurateMass, 4).ToString()
                        + "_" + timeString + "_" + fileString + "." + SaveFileFormat.mat;
                    MsAnnotationTagExport(mainWindow, filePath, peakArea, null, spectraType, exportOption);
                }
                else
                {
                    foreach (var driftSpot in peakArea.DriftSpots)
                    {
                        isIdentified = (driftSpot.LibraryID >= 0 || driftSpot.PostIdentificationLibraryId >= 0) && !driftSpot.MetaboliteName.Contains("w/o") ? true : false;
                        isMonoisotopic = driftSpot.IsotopeWeightNumber == 0 ? true : false;
                        isMsmsAcquired = driftSpot.Ms2LevelDatapointNumber >= 0 ? true : false;

                        switch (exportOption)
                        {
                            case MatExportOption.AllPeaks: break;
                            case MatExportOption.OnlyFocusedPeak:
                                if (peakID != driftSpot.MasterPeakID) continue;
                                break;
                            case MatExportOption.IdentifiedPeaks:
                                if (!isIdentified) continue;
                                break;
                            case MatExportOption.MonoisotopicAndMsmsPeaks:
                                if (!(isMonoisotopic && isMsmsAcquired)) continue;
                                break;
                            case MatExportOption.MsmsPeaks:
                                if (!isMsmsAcquired) continue;
                                break;
                            case MatExportOption.UnknownPeaks:
                                if (isIdentified) continue;
                                break;
                            case MatExportOption.UnknownPeaksWithoutIsotope:
                                if (isIdentified || !isMonoisotopic) continue;
                                break;
                        }

                        var filePath = mainWindow.ProjectProperty.MsAnnotationTagsFolderPath + "\\" + "Peak ID " + driftSpot.MasterPeakID + "_"
                       + Math.Round(peakArea.RtAtPeakTop, 2).ToString() + "_" + Math.Round(peakArea.AccurateMass, 4).ToString() + "_" + Math.Round(driftSpot.DriftTimeAtPeakTop, 2).ToString()
                       + "_" + timeString + "_" + fileString + "." + SaveFileFormat.mat;
                        MsAnnotationTagExport(mainWindow, filePath, peakArea, driftSpot, spectraType, exportOption);
                    }
                }
            }
        }

        public static void MsAnnotationTagExport(MainWindow mainWindow, ObservableCollection<AlignmentPropertyBean> alignmentProperties, int spotID, MatExportOption exportOption)
        {
            var dt = DateTime.Now;
            var timeString = dt.Year.ToString() + "_" + dt.Month.ToString() + "_" + dt.Day.ToString() + "_" + dt.Hour.ToString() + dt.Minute.ToString();
            var fileString = mainWindow.AlignmentFiles[mainWindow.FocusedAlignmentFileID].FileName;
            var param = mainWindow.AnalysisParamForLC;
            var currentSpotID = param.IsIonMobility ? mainWindow.FocusedAlignmentMasterID : mainWindow.FocusedAlignmentPeakID;
            var fs = mainWindow.AlignViewDecFS;
            var seekPoints = mainWindow.AlignViewDecSeekPoints;

            for (int i = 0; i < alignmentProperties.Count; i++)
            {
                var property = alignmentProperties[i];

                var isIdentified = (property.LibraryID >= 0 || property.PostIdentificationLibraryID >= 0) && !property.MetaboliteName.Contains("w/o") ? true : false;
                var isMonoisotopic = true;
                if (property.IsotopeTrackingParentID >= 0 && property.IsotopeTrackingWeightNumber != 0) isMonoisotopic = false;
                if (property.PostDefinedAdductParentID >= 0 && property.PostDefinedIsotopeWeightNumber != 0) isMonoisotopic = false;

                switch (exportOption)
                {
                    case MatExportOption.AllPeaks: break;
                    case MatExportOption.OnlyFocusedPeak:
                        if (!param.IsIonMobility && spotID != property.AlignmentID) continue;
                        break;
                    case MatExportOption.IdentifiedPeaks:
                        if (!isIdentified) continue;
                        break;
                    case MatExportOption.UnknownPeaks:
                        if (isIdentified) continue;
                        break;
                    case MatExportOption.UnknownPeaksWithoutIsotope:
                        if (isIdentified || !isMonoisotopic) continue;
                        break;
                }

                if (!param.IsIonMobility)
                {

                    var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekPoints, property.AlignmentID);
                    var isMsmsAcquired = ms2DecResult.MassSpectra.Count > 0 ? true : false;

                    switch (exportOption)
                    {
                        case MatExportOption.MonoisotopicAndMsmsPeaks:
                            if (!(isMonoisotopic && isMsmsAcquired)) continue;
                            break;
                        case MatExportOption.MsmsPeaks:
                            if (!isMsmsAcquired) continue;
                            break;
                    }

                    var filePath = mainWindow.ProjectProperty.MsAnnotationTagsFolderPath + "\\"
                        + "AlignmentID " + property.AlignmentID + "_" + Math.Round(property.CentralRetentionTime, 2).ToString() + "_"
                        + Math.Round(property.CentralAccurateMass, 4).ToString() + "_" + timeString + "_" + fileString + "." + SaveFileFormat.mat;

                    MsAnnotationTagExport(mainWindow, filePath, ms2DecResult, property, null);
                }
                else
                {
                    foreach (var driftSpot in property.AlignedDriftSpots)
                    {
                        isIdentified = (driftSpot.LibraryID >= 0 || driftSpot.PostIdentificationLibraryID >= 0) && !driftSpot.MetaboliteName.Contains("w/o") ? true : false;
                        isMonoisotopic = true;

                        switch (exportOption)
                        {
                            case MatExportOption.AllPeaks: break;
                            case MatExportOption.OnlyFocusedPeak:
                                if (spotID != driftSpot.MasterID) continue;
                                break;
                            case MatExportOption.IdentifiedPeaks:
                                if (!isIdentified) continue;
                                break;
                            case MatExportOption.UnknownPeaks:
                                if (isIdentified) continue;
                                break;
                            case MatExportOption.UnknownPeaksWithoutIsotope:
                                if (isIdentified || !isMonoisotopic) continue;
                                break;
                        }

                        var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekPoints, driftSpot.MasterID);
                        var isMsmsAcquired = ms2DecResult.MassSpectra.Count > 0 ? true : false;

                        switch (exportOption)
                        {
                            case MatExportOption.MonoisotopicAndMsmsPeaks:
                                if (!(isMonoisotopic && isMsmsAcquired)) continue;
                                break;
                            case MatExportOption.MsmsPeaks:
                                if (!isMsmsAcquired) continue;
                                break;
                        }

                        var filePath = mainWindow.ProjectProperty.MsAnnotationTagsFolderPath + "\\"
                            + "AlignmentID " + property.MasterID + "_" + Math.Round(property.CentralRetentionTime, 2).ToString() + "_"
                            + Math.Round(property.CentralAccurateMass, 4).ToString() + "_"
                            + Math.Round(driftSpot.CentralDriftTime, 2).ToString() + "_" + timeString + "_" + fileString + "." + SaveFileFormat.mat;

                        MsAnnotationTagExport(mainWindow, filePath, ms2DecResult, property, driftSpot);
                    }
                }

            }

            if (param.IsIonMobility)
                mainWindow.FocusedAlignmentMasterID = currentSpotID;
            else
                mainWindow.FocusedAlignmentPeakID = currentSpotID;
        }

        public static void MsAnnotationTagExport(MainWindow mainWindow, string filePath, MS2DecResult ms2DecResult,
            AlignmentPropertyBean alignmentProperty, AlignedDriftSpotPropertyBean driftSpotProperty)
        {
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
            {
                writeMsAnnotationTag(sw, mainWindow, ms2DecResult, alignmentProperty, driftSpotProperty);
            }
        }

        public static void MsAnnotationTagExport(MainWindow mainWindow, string folderpath, MS2DecResult ms2DecResult,
            AlignmentPropertyBean alignmentProperty, AnalysisParametersBean param,
            ObservableCollection<AlignmentPropertyBean> tempSpots, List<MspFormatCompoundInformationBean> mspDB)
        {

            var filename = tempSpots[0].IsotopeTrackingParentID + "_"
             + Math.Round(tempSpots[0].CentralRetentionTime, 2) + "_"
             + Math.Round(tempSpots[0].CentralAccurateMass, 5) + "_"
             + Math.Round(tempSpots[tempSpots.Count - 1].CentralAccurateMass, 5) + "_"
             + tempSpots[tempSpots.Count - 1].IsotopeTrackingWeightNumber;
            var filepath = folderpath + "\\" + filename + ".mat";

            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {
                var projectProp = mainWindow.ProjectProperty;
                var basicProperty = tempSpots[0];
                var isotopeLabel = param.IsotopeTrackingDictionary;
                var labelType = isotopeLabel.IsotopeElements[isotopeLabel.SelectedID].ElementName;

                var name = basicProperty.MetaboliteName;
                if (name == string.Empty || name.Contains("w/o"))
                    name = "Unknown";
                var adduct = basicProperty.AdductIonName;
                if ((adduct == null || adduct == string.Empty) && projectProp.IonMode == IonMode.Positive)
                    adduct = "[M+H]+";
                else if ((adduct == null || adduct == string.Empty) && projectProp.IonMode == IonMode.Negative)
                    adduct = "[M-H]-";

                var libraryID = basicProperty.LibraryID;
                if (name == "Unknown")
                    libraryID = -1;

                sw.WriteLine("NAME: " + filename);
                sw.WriteLine("SCANNUMBER: " + basicProperty.AlignmentID);
                sw.WriteLine("RETENTIONTIME: " + ms2DecResult.PeakTopRetentionTime);
                sw.WriteLine("PRECURSORMZ: " + ms2DecResult.Ms1AccurateMass);
                sw.WriteLine("PRECURSORTYPE: " + adduct);
                sw.WriteLine("IONMODE: " + projectProp.IonMode);
                sw.WriteLine("SPECTRUMTYPE: Centroid");
                sw.WriteLine("INTENSITY: " + ms2DecResult.Ms1PeakHeight);
                sw.WriteLine("INCHIKEY: " + MspDataRetrieve.GetInChIKey(libraryID, mspDB));
                sw.WriteLine("SMILES: " + MspDataRetrieve.GetSMILES(libraryID, mspDB));
                sw.WriteLine("FORMULA: " + MspDataRetrieve.GetFormula(libraryID, mspDB));

                if (projectProp.FinalSavedDate != null)
                {
                    sw.WriteLine("DATE: " + projectProp.FinalSavedDate.Date);
                }

                if (projectProp.Authors != null && projectProp.Authors != string.Empty)
                {
                    sw.WriteLine("AUTHORS: " + projectProp.Authors);
                }

                if (projectProp.License != null && projectProp.License != string.Empty)
                {
                    sw.WriteLine("LICENSE: " + projectProp.License);
                }

                if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty)
                {
                    sw.WriteLine("COLLISIONENERGY: " + projectProp.CollisionEnergy);
                }

                if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty)
                {
                    sw.WriteLine("INSTRUMENTTYPE: " + projectProp.InstrumentType);
                }

                if (projectProp.Instrument != null && projectProp.Instrument != string.Empty)
                {
                    sw.WriteLine("INSTRUMENT: " + projectProp.Instrument);
                }

                sw.WriteLine("#Specific field for labeled experiment");
                switch (labelType)
                {
                    case "13C":
                        sw.WriteLine("CarbonCount: " + tempSpots[tempSpots.Count - 1].IsotopeTrackingWeightNumber);
                        break;
                    case "15N":
                        sw.WriteLine("NitrogenCount: " + tempSpots[tempSpots.Count - 1].IsotopeTrackingWeightNumber);
                        break;
                    case "34S":
                        sw.WriteLine("SulfurCount: " + tempSpots[tempSpots.Count - 1].IsotopeTrackingWeightNumber);
                        break;
                    case "18O":
                        sw.WriteLine("OxygenCount: " + tempSpots[tempSpots.Count - 1].IsotopeTrackingWeightNumber);
                        break;
                    case "13C+15N":
                        sw.WriteLine("CarbonNitrogenCount: " + tempSpots[tempSpots.Count - 1].IsotopeTrackingWeightNumber);
                        break;
                    case "13C+34S":
                        sw.WriteLine("CarbonSulfurCount: " + tempSpots[tempSpots.Count - 1].IsotopeTrackingWeightNumber);
                        break;
                    case "15N+34S":
                        sw.WriteLine("NitrogenSulfurCount: " + tempSpots[tempSpots.Count - 1].IsotopeTrackingWeightNumber);
                        break;
                    case "13C+15N+34S":
                        sw.WriteLine("CarbonNitrogenSulfurCount: " + tempSpots[tempSpots.Count - 1].IsotopeTrackingWeightNumber);
                        break;
                }

                sw.WriteLine("Comment: annotated in MS-DIAL as " + name);

                sw.Write("MSTYPE: ");
                sw.WriteLine("MS1");

                sw.WriteLine("Num Peaks: 3");
                sw.WriteLine(Math.Round(ms2DecResult.Ms1AccurateMass, 5) + "\t" + ms2DecResult.Ms1PeakHeight);
                sw.WriteLine(Math.Round(ms2DecResult.Ms1AccurateMass + 1.00335, 5) + "\t" + ms2DecResult.Ms1IsotopicIonM1PeakHeight);
                sw.WriteLine(Math.Round(ms2DecResult.Ms1AccurateMass + 2.00671, 5) + "\t" + ms2DecResult.Ms1IsotopicIonM2PeakHeight);

                sw.Write("MSTYPE: ");
                sw.WriteLine("MS2");

                sw.Write("Num Peaks: ");
                sw.WriteLine(ms2DecResult.MassSpectra.Count);

                for (int i = 0; i < ms2DecResult.MassSpectra.Count; i++)
                    sw.WriteLine(Math.Round(ms2DecResult.MassSpectra[i][0], 5) + "\t" + Math.Round(ms2DecResult.MassSpectra[i][1], 0));
            }
        }

        public static void MsAnnotationTagExport(MainWindow mainWindow, string filePath,
            PeakAreaBean peakArea, DriftSpotBean driftSpot, ExportspectraType spectraType, MatExportOption exportOption)
        {
            var ms1Spectrum = getMs1SpectrumCollection(mainWindow, peakArea, driftSpot, spectraType);
            var ms2Spectrum = getMs2SpectrumCollection(mainWindow, peakArea, driftSpot, spectraType);

            if ((exportOption == MatExportOption.MsmsPeaks || exportOption == MatExportOption.MonoisotopicAndMsmsPeaks) && ms2Spectrum.Count <= 0) return;

            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
            {
                writeMsAnnotationTag(sw, peakArea, driftSpot, ms1Spectrum, ms2Spectrum, mainWindow, spectraType);
            }
        }

        public static void ParameterExport(ObservableCollection<AnalysisFileBean> analysisFiles, AnalysisParametersBean param,
            ProjectPropertyBean project, string filePath)
        {
            var binaryfile = System.IO.Path.GetDirectoryName(filePath) + "\\" +
                System.IO.Path.GetFileNameWithoutExtension(filePath) + ".med";
            MessagePackHandler.SaveToFile<AnalysisParametersBean>(param, binaryfile);
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
            {
                sw.WriteLine(param.MsdialVersionNumber);
                sw.WriteLine();
                sw.WriteLine("#Project");
                sw.WriteLine("MS1 Data type\t" + project.DataType);
                sw.WriteLine("MS2 Data type\t" + project.DataTypeMS2);
                sw.WriteLine("Ion mode\t" + project.IonMode);
                sw.WriteLine("Target\t" + project.TargetOmics);
                sw.WriteLine("Mode\t" + project.MethodType);
                sw.WriteLine();

                sw.WriteLine("#Data collection parameters");
                sw.WriteLine("Retention time begin\t" + param.RetentionTimeBegin);
                sw.WriteLine("Retention time end\t" + param.RetentionTimeEnd);
                sw.WriteLine("Mass range begin\t" + param.MassRangeBegin);
                sw.WriteLine("Mass range end\t" + param.MassRangeEnd);
                sw.WriteLine("MS2 mass range begin\t" + param.Ms2MassRangeBegin);
                sw.WriteLine("MS2 mass range end\t" + param.Ms2MassRangeEnd);
                sw.WriteLine();

                sw.WriteLine("#Centroid parameters");
                sw.WriteLine("MS1 tolerance\t" + param.CentroidMs1Tolerance);
                sw.WriteLine("MS2 tolerance\t" + param.CentroidMs2Tolerance);
                //sw.WriteLine("Peak detection-based\t" + param.PeakDetectionBasedCentroid.ToString());
                sw.WriteLine();

                sw.WriteLine("#Isotope recognition");
                sw.WriteLine("Maximum charged number\t" + param.MaxChargeNumber);
                sw.WriteLine();

                sw.WriteLine("#Data processing");
                sw.WriteLine("Number of threads\t" + param.NumThreads.ToString());
                sw.WriteLine();

                sw.WriteLine("#Peak detection parameters");
                sw.WriteLine("Smoothing method\t" + param.SmoothingMethod.ToString());
                sw.WriteLine("Smoothing level\t" + param.SmoothingLevel);
                sw.WriteLine("Minimum peak width\t" + param.MinimumDatapoints);
                sw.WriteLine("Minimum peak height\t" + param.MinimumAmplitude);
                sw.WriteLine();

                sw.WriteLine("#Peak spotting parameters");
                sw.WriteLine("Mass slice width\t" + param.MassSliceWidth);
                sw.WriteLine("Exclusion mass list (mass & tolerance)");
                foreach (var mass in param.ExcludedMassList) { sw.WriteLine(mass.ExcludedMass + "\t" + mass.MassTolerance); }
                sw.WriteLine();

                sw.WriteLine("#Deconvolution parameters");
                //sw.WriteLine("Peak consideration\t" + param.DeconvolutionType.ToString());
                sw.WriteLine("Sigma window value\t" + param.SigmaWindowValue);
                sw.WriteLine("MS2Dec amplitude cut off\t" + param.AmplitudeCutoff);
                sw.WriteLine("Exclude after precursor\t" + param.RemoveAfterPrecursor.ToString());
                sw.WriteLine("Keep isotope until\t" + param.KeptIsotopeRange);
                sw.WriteLine("Keep original precursor isotopes\t" + param.KeepOriginalPrecursorIsotopes);

                sw.WriteLine();

                sw.WriteLine("#MSP file and MS/MS identification setting");
                sw.WriteLine("MSP file\t" + project.LibraryFilePath);
                sw.WriteLine("Retention time tolerance\t" + param.RetentionTimeLibrarySearchTolerance);
                sw.WriteLine("Accurate mass tolerance (MS1)\t" + param.Ms1LibrarySearchTolerance);
                sw.WriteLine("Accurate mass tolerance (MS2)\t" + param.Ms2LibrarySearchTolerance);
                sw.WriteLine("Identification score cut off\t" + param.IdentificationScoreCutOff);
                sw.WriteLine("Using retention time for scoring\t" + param.IsUseRetentionInfoForIdentificationScoring);
                sw.WriteLine("Using retention time for filtering\t" + param.IsUseRetentionInfoForIdentificationFiltering);
                if (project.TargetOmics == TargetOmics.Lipidomics)
                {
                    sw.WriteLine();
                    sw.WriteLine("#Selected lipid types");
                    foreach (var lQuery in param.LipidQueryBean.LbmQueries)
                    {
                        if (lQuery.IsSelected == true && lQuery.IonMode == project.IonMode)
                        {
                            if (param.LipidQueryBean.SolventType == SolventType.CH3COONH4)
                            {
                                if (project.IonMode == IonMode.Negative &&
                                    (lQuery.AdductIon.AdductIonName == "[M+FA-H]-" || lQuery.AdductIon.AdductIonName == "[M+HCOO]-"))
                                    continue;
                            }
                            else
                            {
                                if (project.IonMode == IonMode.Negative &&
                                   (lQuery.AdductIon.AdductIonName == "[M+Hac-H]-" || lQuery.AdductIon.AdductIonName == "[M+CH3COO]-"))
                                    continue;
                            }
                            sw.WriteLine(lQuery.LbmClass + "\t" + lQuery.AdductIon.AdductIonName);
                        }
                    }
                }
                sw.WriteLine();

                sw.WriteLine("#Text file and post identification (retention time and accurate mass based) setting");
                sw.WriteLine("Text file\t" + project.PostIdentificationLibraryFilePath);
                sw.WriteLine("Retention time tolerance\t" + param.RetentionTimeToleranceOfPostIdentification);
                sw.WriteLine("Accurate mass tolerance\t" + param.AccurateMassToleranceOfPostIdentification);
                sw.WriteLine("Identification score cut off\t" + param.PostIdentificationScoreCutOff);
                sw.WriteLine();

                sw.WriteLine("#Advanced setting for identification");
                sw.WriteLine("Relative abundance cut off\t" + param.RelativeAbundanceCutOff);
                sw.WriteLine("Top candidate report\t" + param.OnlyReportTopHitForPostAnnotation);
                sw.WriteLine();

                sw.WriteLine("#Adduct ion setting");
                foreach (var adduct in param.AdductIonInformationBeanList) { if (adduct.Included == true) sw.WriteLine(adduct.AdductName); }
                sw.WriteLine();

                sw.WriteLine("#Alignment parameters setting");
                if (analysisFiles != null && analysisFiles.Count > 0)
                    sw.WriteLine("Reference file\t" + analysisFiles[param.AlignmentReferenceFileID].AnalysisFilePropertyBean.AnalysisFilePath);
                sw.WriteLine("Retention time tolerance\t" + param.RetentionTimeAlignmentTolerance);
                sw.WriteLine("MS1 tolerance\t" + param.Ms1AlignmentTolerance);
                sw.WriteLine("Retention time factor\t" + param.RetentionTimeAlignmentFactor);
                sw.WriteLine("MS1 factor\t" + param.Ms1AlignmentFactor);
                sw.WriteLine("Peak count filter\t" + param.PeakCountFilter);
                sw.WriteLine("N% detected in at least one group\t" + param.NPercentDetectedInOneGroup);
                //sw.WriteLine("QC at least filter\t" + param.QcAtLeastFilter.ToString());
                sw.WriteLine("Remove feature based on peak height fold-change\t" + param.IsRemoveFeatureBasedOnPeakHeightFoldChange);
                sw.WriteLine("Sample max / blank average\t" + param.SampleMaxOverBlankAverage);
                sw.WriteLine("Sample average / blank average\t" + param.SampleAverageOverBlankAverage);
                sw.WriteLine("Keep identified and annotated metabolites\t" + param.IsKeepIdentifiedMetaboliteFeatures);
                sw.WriteLine("Keep removable features and assign the tag for checking\t" + param.IsKeepRemovableFeaturesAndAssignedTagForChecking);
                //sw.WriteLine("Replace true zero values with 1/2 of minimum peak height over all samples\t" + param.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples);
                sw.WriteLine("Gap filling by compulsion\t" + param.IsForceInsertForGapFilling);

                sw.WriteLine();

                sw.WriteLine("#Tracking of isotope labels");
                if (param.TrackingIsotopeLabels)
                {
                    sw.WriteLine("Tracking of isotopic labels\tTRUE");
                    sw.WriteLine("Labeled element\t" + param.IsotopeTrackingDictionary.IsotopeElements[param.IsotopeTrackingDictionary.SelectedID].ElementName);
                    if (analysisFiles != null && analysisFiles.Count > 0)
                        sw.WriteLine("Non labeled reference file\t" + analysisFiles[param.NonLabeledReferenceID].AnalysisFilePropertyBean.AnalysisFilePath);
                    else
                        sw.WriteLine("Non labeled reference file\tNot found");

                    if (param.UseTargetFormulaLibrary)
                    {
                        sw.WriteLine("Use target formula library\t" + param.UseTargetFormulaLibrary);
                        if (analysisFiles != null && analysisFiles.Count > 0)
                            sw.WriteLine("Target formula library file path\t" + project.TargetFormulaLibraryFilePath);
                        else
                            sw.WriteLine("Fully labeled reference file\tNot found");
                    }
                }
                else
                {
                    sw.WriteLine("Tracking of isotopic labels\tFALSE");
                }
                sw.WriteLine();

                sw.WriteLine("#Ion mobility");
                if (param.IsIonMobility)
                {
                    sw.WriteLine("Mobility type\t" + param.IonMobilityType);
                    sw.WriteLine("Accumulated RT ragne\t" + param.AccumulatedRtRagne);
                    sw.WriteLine("CCS search Tolerance\t" + param.CcsSearchTolerance);
                    sw.WriteLine("Use CCS for identification scoring\t" + param.IsUseCcsForIdentificationScoring);
                    sw.WriteLine("Use CCS for identification filtering\t" + param.IsUseCcsForIdentificationFiltering);
                    sw.WriteLine("Mobility axis alignment tolerance\t" + param.DriftTimeAlignmentTolerance);
                }
                else
                {
                    sw.WriteLine("Ion mobility data\tFALSE");
                }
            }
        }

        public static void ExportAsLipoqualityDatabaseFormat(string directory, MainWindow mainWindow, int selectedAlignmentFileID)
        {
            var alignedResult = MessagePackHandler.LoadFromFile<AlignmentResultBean>(mainWindow.AlignmentFiles[selectedAlignmentFileID].FilePath);
            //var alignedResult = (AlignmentResultBean)DataStorageLcUtility.LoadFromXmlFile(mainWindow.AlignmentFiles[selectedAlignmentFileID].FilePath, typeof(AlignmentResultBean));
            var files = mainWindow.AnalysisFiles;
            var mspDB = mainWindow.MspDB;

            mainWindow.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            int focusedFileID = mainWindow.FocusedFileID;
            int focusedPeakID = mainWindow.FocusedPeakID;

            int focusedAlignmentFileID = mainWindow.FocusedAlignmentFileID;
            int focusedAlignmentPeakID = mainWindow.FocusedAlignmentPeakID;

            mainWindow.PeakViewDataAccessRefresh();
            mainWindow.AlignmentViewDataAccessRefresh();

            var alignedSpots = alignedResult.AlignmentPropertyBeanCollection;
            if (alignedSpots == null || alignedSpots.Count == 0) return;

            var classIdAnalysisFilesDictionary = AnalysisFileClassUtility.GetClassIdAnalysisFileBeansDictionary(files);
            var ionmode = mainWindow.ProjectProperty.IonMode.ToString();

            using (var fs = File.Open(mainWindow.AlignmentFiles[selectedAlignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

                foreach (var dict in classIdAnalysisFilesDictionary)
                {
                    var key = dict.Key;
                    var value = dict.Value;

                    var outputPath = directory + "\\" + key + ".txt";
                    var sampleInfo = new SampleInfomation()
                    { // temporary
                        SampleName = key,
                        SampleSource = "Algae",
                        SampleType = "Algae",
                        MutantType = "Wild type",
                        MetabolomeExtractionMethod = "Matyash",
                        ChromatogramCondition = "Reverse phase LC",
                        MassspecCondition = "QTOF",
                        IonMode = "Positive/Negative",
                        Quantification = "Relative",
                        Date = "2018/9/20",
                        MethodLink = "http://metabolonote.kazusa.or.jp/SE110:/MS2",
                    };
                    var annotations = LipoqualityDatabaseManager.GetLipoqualityDatabaseAnnotations(value, alignedSpots, mspDB);

                    using (var sw = new StreamWriter(outputPath, false, Encoding.ASCII))
                    {

                        writeLipoqualityDatabaseMetadataFields(sw, sampleInfo);
                        writeLipoqualityDatabaseHeaderInfo(sw);

                        foreach (var annotation in annotations)
                        {
                            var alignmentID = annotation.SpotID;
                            var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, alignmentID);
                            var massSpectra = ms2DecResult.MassSpectra;

                            writeLipoqualityDatabaseAnnotation(sw, annotation, massSpectra);
                        }
                    }
                }

                fs.Dispose();
                fs.Close();
            }

            mainWindow.PeakViewerForLcRefresh(focusedFileID);
            ((PairwisePlotPeakViewUI)mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.SelectedPlotId = focusedPeakID;

            if (focusedAlignmentFileID < 0)
            {
                Mouse.OverrideCursor = null;
                mainWindow.IsEnabled = true;
                return;
            }

            mainWindow.AlignmentViewerForLcRefresh(focusedAlignmentFileID);
            ((PairwisePlotAlignmentViewUI)mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.SelectedPlotId = focusedAlignmentPeakID;

            Mouse.OverrideCursor = null;
            mainWindow.IsEnabled = true;
        }

        public static void ExportAsLipoqualityDatabaseFormatVS2(string directory, MainWindow mainWindow, int selectedAlignmentFileID)
        {
            var alignedResult = MessagePackHandler.LoadFromFile<AlignmentResultBean>(mainWindow.AlignmentFiles[selectedAlignmentFileID].FilePath);
            //var alignedResult = (AlignmentResultBean)DataStorageLcUtility.LoadFromXmlFile(mainWindow.AlignmentFiles[selectedAlignmentFileID].FilePath, typeof(AlignmentResultBean));
            var files = mainWindow.AnalysisFiles;
            var mspDB = mainWindow.MspDB;

            mainWindow.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            int focusedFileID = mainWindow.FocusedFileID;
            int focusedPeakID = mainWindow.FocusedPeakID;

            int focusedAlignmentFileID = mainWindow.FocusedAlignmentFileID;
            int focusedAlignmentPeakID = mainWindow.FocusedAlignmentPeakID;

            mainWindow.PeakViewDataAccessRefresh();
            mainWindow.AlignmentViewDataAccessRefresh();

            var alignedSpots = alignedResult.AlignmentPropertyBeanCollection;
            if (alignedSpots == null || alignedSpots.Count == 0) return;

            var classIdAnalysisFilesDictionary = AnalysisFileClassUtility.GetClassIdAnalysisFileBeansDictionary(files);
            var ionmode = mainWindow.ProjectProperty.IonMode;
            var outputPath = directory + "\\Result-" + ionmode + ".txt";
            using (var fs = File.Open(mainWindow.AlignmentFiles[selectedAlignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
                using (var sw = new StreamWriter(outputPath, false, Encoding.ASCII))
                {
                    // first header
                    sw.Write("\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t");
                    sw.WriteLine(String.Join("\t", files.Select(n => n.AnalysisFilePropertyBean.AnalysisFileClass)));

                    // second
                    sw.Write("Lipid super class\tLipid class\tTotal chain\tSN1 chain\tSN2 chain\tSN3 chain\tSN4 chain\tFormula\tRepresentative InChIKey\tRepresentative SMILES\tRT[min]\tm/z\tAdduct\tAverage intensity\tMSMS\tSaturated\tMono unsaturated\tArachidonic acid\tEicosapentaenoic acid\tDocosapentaenoic acid\tPUFAs\t");
                    sw.WriteLine(String.Join("\t", files.Select(n => n.AnalysisFilePropertyBean.AnalysisFileName)));

                    var lqAnnotations = LipoqualityDatabaseManager.GetLipoqualityDatabaseAnnotations(files.ToList(), alignedSpots, mspDB);
                    foreach (var annotation in lqAnnotations)
                    {
                        if (ionmode == IonMode.Positive)
                        {
                            if (annotation.LipidClass != "TAG" && annotation.LipidClass != "DAG"
                                 && annotation.LipidClass != "MAG" && annotation.LipidClass != "ACar"
                                 && annotation.LipidClass != "CE" && annotation.LipidClass != "BMP"
                                 && annotation.LipidClass != "SQDG" && annotation.LipidClass != "DGTS"
                                 && annotation.LipidClass != "LDGTS" && annotation.LipidClass != "DGTA"
                                 && annotation.LipidClass != "LDGTA"
                                 )
                            {
                                continue;
                            }
                            if (annotation.Adduct.AdductIonName == "[M+Na]+") continue;
                        }
                        else
                        {

                        }
                        var isSaturated = isContainedSpecificFattyAcids(annotation, ":0", false);
                        var isMufa = isContainedSpecificFattyAcids(annotation, ":1", false);
                        var isAA = isContainedSpecificFattyAcids(annotation, "20:4", true);
                        var isEPA = isContainedSpecificFattyAcids(annotation, "20:5", true);
                        var isDHA = isContainedSpecificFattyAcids(annotation, "22:6", true);
                        var isPUFA = isPolyunsaturatedFattyAcids(annotation);
                        var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, annotation.SpotID);
                        var massSpectra = ms2DecResult.MassSpectra;
                        var specString = ResultExportLcUtility.GetSpectrumString(massSpectra);
                        if (annotation.LipidSuperClass == null || annotation.LipidClass == null || annotation.LipidClass == string.Empty) continue;
                        sw.Write(annotation.LipidSuperClass + "\t" + annotation.LipidClass + "\t" + annotation.TotalChain + "\t" +
                            annotation.Sn1AcylChain + "\t" + annotation.Sn2AcylChain + "\t" + annotation.Sn3AcylChain + "\t" + annotation.Sn4AcylChain + "\t" +
                            annotation.Formula + "\t" + annotation.Inchikey + "\t" + annotation.Smiles + "\t" +
                            annotation.Rt + "\t" + annotation.Mz + "\t" + annotation.Adduct.AdductIonName + "\t" + annotation.AveragedIntensity + "\t" + specString + "\t" +
                            isSaturated + "\t" + isMufa + "\t" + isAA + "\t" + isEPA + "\t" + isDHA + "\t" + isPUFA + "\t");
                        sw.WriteLine(String.Join("\t", annotation.Intensities));
                    }
                }
            }

            mainWindow.PeakViewerForLcRefresh(focusedFileID);
            ((PairwisePlotPeakViewUI)mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.SelectedPlotId = focusedPeakID;

            if (focusedAlignmentFileID < 0)
            {
                Mouse.OverrideCursor = null;
                mainWindow.IsEnabled = true;
                return;
            }

            mainWindow.AlignmentViewerForLcRefresh(focusedAlignmentFileID);
            ((PairwisePlotAlignmentViewUI)mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.SelectedPlotId = focusedAlignmentPeakID;

            Mouse.OverrideCursor = null;
            mainWindow.IsEnabled = true;
        }

        private static bool isPolyunsaturatedFattyAcids(LipoqualityAnnotation annotation)
        {

            if (isContainedSpecificFattyAcids(annotation, ":2", false)) return true;
            if (isContainedSpecificFattyAcids(annotation, ":3", false)) return true;
            if (isContainedSpecificFattyAcids(annotation, ":4", false)) return true;
            if (isContainedSpecificFattyAcids(annotation, ":5", false)) return true;
            if (isContainedSpecificFattyAcids(annotation, ":6", false)) return true;
            if (isContainedSpecificFattyAcids(annotation, ":7", false)) return true;
            if (isContainedSpecificFattyAcids(annotation, ":8", false)) return true;
            if (isContainedSpecificFattyAcids(annotation, ":9", false)) return true;
            if (isContainedSpecificFattyAcids(annotation, ":10", false)) return true;
            if (isContainedSpecificFattyAcids(annotation, ":11", false)) return true;
            return false;
        }

        private static bool isContainedSpecificFattyAcids(LipoqualityAnnotation annotation, string searchString, bool isCompleteMatch)
        {
            var sn1 = annotation.Sn1AcylChain;
            var sn2 = annotation.Sn2AcylChain;
            var sn3 = annotation.Sn3AcylChain;
            var sn4 = annotation.Sn4AcylChain;

            if (isCompleteMatch)
            {
                if (sn1 == searchString) return true;
                if (sn2 == searchString) return true;
                if (sn3 == searchString) return true;
                if (sn4 == searchString) return true;
            }
            else
            {
                if (!sn1.Contains("e") && !sn1.Contains("d") && !sn1.Contains("t") && sn1.Contains(searchString)) return true;
                if (!sn2.Contains("e") && !sn2.Contains("d") && !sn2.Contains("t") && sn2.Contains(searchString)) return true;
                if (!sn3.Contains("e") && !sn3.Contains("d") && !sn3.Contains("t") && sn3.Contains(searchString)) return true;
                if (!sn4.Contains("e") && !sn4.Contains("d") && !sn4.Contains("t") && sn4.Contains(searchString)) return true;
            }
            return false;
        }

        private static void writeLipoqualityDatabaseAnnotation(StreamWriter sw, LipoqualityAnnotation annotation, List<double[]> massSpectra)
        {
            if (annotation.Adduct.IonMode == IonMode.Positive)
            {
                if (annotation.LipidClass != "TAG" && annotation.LipidClass != "DAG"
                    && annotation.LipidClass != "MAG" && annotation.LipidClass != "ACar"
                    && annotation.LipidClass != "CE" && annotation.LipidClass != "BMP"
                    && annotation.LipidClass != "SQDG" && annotation.LipidClass != "DGTS"
                    && annotation.LipidClass != "LDGTS" && annotation.LipidClass != "DGTA"
                    && annotation.LipidClass != "LDGTA"
                    )
                    return;

                if (annotation.LipidClass == "TAG" && annotation.Adduct.AdductIonName == "[M+Na]+")
                    return;
            }
            else if (annotation.Adduct.IonMode == IonMode.Negative)
            {
                if (annotation.LipidClass == "SQDG") return;
            }
            //else if (annotation.LipidClass == "PMeOH" || annotation.LipidClass == "PEtOH" || annotation.LipidClass == "PBtOH") {
            //    return;
            //}
            else if (annotation.LipidClass == "Unassigned lipid") return;

            sw.Write(annotation.LipidSuperClass + "\t");
            sw.Write(annotation.LipidClass + "\t");
            sw.Write(annotation.TotalChain + "\t");
            sw.Write(annotation.Sn1AcylChain + "\t");
            sw.Write(annotation.Sn2AcylChain + "\t");
            sw.Write(annotation.Sn3AcylChain + "\t");
            sw.Write(annotation.Sn4AcylChain + "\t");
            sw.Write(annotation.Formula + "\t");
            sw.Write(annotation.Inchikey + "\t");
            sw.Write(annotation.Smiles + "\t");
            sw.Write(Math.Round(annotation.Rt, 2) + "\t");
            sw.Write(Math.Round(annotation.Mz, 5) + "\t");
            sw.Write(Math.Round(annotation.AveragedIntensity, 0) + "\t");
            sw.Write(Math.Round(annotation.StandardDeviation, 0) + "\t");
            sw.Write(annotation.SpotID + "\t");
            sw.Write(annotation.Adduct.AdductIonName + "\t");

            var spectrum = string.Empty;
            for (int i = 0; i < massSpectra.Count; i++)
            {
                if (i != massSpectra.Count - 1)
                    spectrum += Math.Round(massSpectra[i][0], 4) + " " + Math.Round(massSpectra[i][1], 0) + ", ";
                else
                    spectrum += Math.Round(massSpectra[i][0], 4) + " " + Math.Round(massSpectra[i][1], 0);
            }
            sw.WriteLine(spectrum);
        }

        private static void writeLipoqualityDatabaseHeaderInfo(StreamWriter sw)
        {
            sw.WriteLine("Lipid super class\tLipid class\tTotal chain\tSN1 chain\tSN2 chain\tSN3 chain\tSN4 chain\tFormula\tInChIKey\tSMILES\t" +
              "RT[min]\tm/z\tIntensity\tStandard deviation\tAlignment ID\tAdduct\tMSMS");
        }

        private static void writeLipoqualityDatabaseMetadataFields(StreamWriter sw, SampleInfomation info)
        {
            sw.WriteLine("Sample name\t" + info.SampleName);
            sw.WriteLine("Sample source\t" + info.SampleSource);
            sw.WriteLine("Sample type\t" + info.SampleType);
            sw.WriteLine("Mutant type\t" + info.MutantType);
            sw.WriteLine("Extraction\t" + info.MetabolomeExtractionMethod);
            sw.WriteLine("LC type\t" + info.ChromatogramCondition);
            sw.WriteLine("MS type\t" + info.MassspecCondition);
            sw.WriteLine("Ion mode\t" + info.IonMode);
            sw.WriteLine("Quantification\t" + info.Quantification);
            sw.WriteLine("Date\t" + info.Date);
            sw.WriteLine("Method link\t" + info.MethodLink);
        }

        private static ObservableCollection<double[]> getMs1SpectrumCollection(MainWindow mainWindow, PeakAreaBean peakAreaBean, DriftSpotBean driftSpot, ExportspectraType spectrumType)
        {
            var param = mainWindow.AnalysisParamForLC;
            var ms1Spectrum = new ObservableCollection<double[]>();

            if (param.IsIonMobility)
            {
                if (spectrumType == ExportspectraType.profile)
                {
                    ms1Spectrum = DataAccessLcUtility.GetProfileMassSpectra(mainWindow.LcmsSpectrumCollection, driftSpot.Ms1LevelDatapointNumber);
                }
                else
                {
                    ms1Spectrum = DataAccessLcUtility.GetCentroidMassSpectra(mainWindow.LcmsSpectrumCollection, mainWindow.ProjectProperty.DataType, driftSpot.Ms1LevelDatapointNumber, param.CentroidMs1Tolerance, param.PeakDetectionBasedCentroid);
                }
            }
            else
            {
                if (spectrumType == ExportspectraType.profile)
                {
                    ms1Spectrum = DataAccessLcUtility.GetProfileMassSpectra(mainWindow.LcmsSpectrumCollection, peakAreaBean.Ms1LevelDatapointNumber);
                }
                else
                {
                    ms1Spectrum = DataAccessLcUtility.GetCentroidMassSpectra(mainWindow.LcmsSpectrumCollection, mainWindow.ProjectProperty.DataType, peakAreaBean.Ms1LevelDatapointNumber, param.CentroidMs1Tolerance, param.PeakDetectionBasedCentroid);
                }
                //if (spectrumType != ExportspectraType.profile && mainWindow.ProjectProperty.DataType == DataType.Profile)
                //    ms1Spectrum = DataAccessLcUtility.GetCentroidMassSpectra(mainWindow.LcmsSpectrumCollection, mainWindow.ProjectProperty.DataType, peakAreaBean.Ms1LevelDatapointNumber, mainWindow.AnalysisParamForLC.CentroidMs1Tolerance, mainWindow.AnalysisParamForLC.PeakDetectionBasedCentroid);
                //else ms1Spectrum = DataAccessLcUtility.GetProfileMassSpectra(mainWindow.LcmsSpectrumCollection, peakAreaBean.Ms1LevelDatapointNumber);
            }

            return ms1Spectrum;
        }

        private static ObservableCollection<double[]> getMs2SpectrumCollection(MainWindow mainWindow, PeakAreaBean peakAreaBean, DriftSpotBean driftSpot, ExportspectraType spectrumType)
        {
            var ms2Spectrum = new ObservableCollection<double[]>();
            var param = mainWindow.AnalysisParamForLC;
            if (param.IsIonMobility)
            {
                if (spectrumType == ExportspectraType.profile)
                {
                    ms2Spectrum = DataAccessLcUtility.GetProfileMassSpectra(mainWindow.LcmsSpectrumCollection, driftSpot.Ms2LevelDatapointNumber);
                }
                else if (spectrumType == ExportspectraType.centroid && mainWindow.ProjectProperty.MethodType == MethodType.diMSMS)
                {
                    ms2Spectrum = DataAccessLcUtility.GetCentroidMassSpectra(mainWindow.LcmsSpectrumCollection, mainWindow.ProjectProperty.DataTypeMS2, driftSpot.Ms2LevelDatapointNumber, param.CentroidMs2Tolerance, param.PeakDetectionBasedCentroid);
                }
                else
                {
                    var ms2List = SpectralDeconvolution.ReadMS2DecResult(mainWindow.PeakViewDecFS, mainWindow.PeakViewDecSeekPoints, driftSpot.MasterPeakID).MassSpectra;
                    if (ms2List == null || ms2List.Count == 0) return new ObservableCollection<double[]>();
                    else return new ObservableCollection<double[]>(ms2List);
                }
            }
            else
            {
                if (spectrumType == ExportspectraType.profile)
                {
                    ms2Spectrum = DataAccessLcUtility.GetProfileMassSpectra(mainWindow.LcmsSpectrumCollection, peakAreaBean.Ms2LevelDatapointNumber);
                }
                else if (spectrumType == ExportspectraType.centroid && mainWindow.ProjectProperty.MethodType == MethodType.diMSMS)
                {
                    ms2Spectrum = DataAccessLcUtility.GetCentroidMassSpectra(mainWindow.LcmsSpectrumCollection, mainWindow.ProjectProperty.DataTypeMS2, peakAreaBean.Ms2LevelDatapointNumber, param.CentroidMs2Tolerance, param.PeakDetectionBasedCentroid);
                }
                else
                {
                    var ms2List = SpectralDeconvolution.ReadMS2DecResult(mainWindow.PeakViewDecFS, mainWindow.PeakViewDecSeekPoints, peakAreaBean.PeakID).MassSpectra;
                    if (ms2List == null || ms2List.Count == 0) return new ObservableCollection<double[]>();
                    else return new ObservableCollection<double[]>(ms2List);
                }
            }

            //if (spectrumType == ExportspectraType.deconvoluted)
            //{
            //    var ms2List = SpectralDeconvolution.ReadMS2DecResult(mainWindow.PeakViewDecFS, mainWindow.PeakViewDecSeekPoints, peakAreaBean.PeakID).MassSpectra;
            //    if (ms2List == null || ms2List.Count == 0) return new ObservableCollection<double[]>();
            //    else return new ObservableCollection<double[]>(ms2List);
            //}
            //else
            //{
            //    if (spectrumType == ExportspectraType.centroid && mainWindow.ProjectProperty.MethodType == MethodType.ddMSMS)
            //    {
            //        var ms2List = SpectralDeconvolution.ReadMS2DecResult(mainWindow.PeakViewDecFS, mainWindow.PeakViewDecSeekPoints, peakAreaBean.PeakID).MassSpectra;
            //        if (ms2List == null || ms2List.Count == 0) return new ObservableCollection<double[]>();
            //        else return new ObservableCollection<double[]>(ms2List);
            //    }
            //    else if (spectrumType == ExportspectraType.centroid && mainWindow.ProjectProperty.MethodType == MethodType.diMSMS)
            //    {
            //        ms2Spectrum = DataAccessLcUtility.GetCentroidMassSpectra(mainWindow.LcmsSpectrumCollection, mainWindow.ProjectProperty.DataTypeMS2, peakAreaBean.Ms2LevelDatapointNumber, mainWindow.AnalysisParamForLC.CentroidMs2Tolerance, mainWindow.AnalysisParamForLC.PeakDetectionBasedCentroid);
            //    }
            //    else
            //    {
            //        ms2Spectrum = DataAccessLcUtility.GetProfileMassSpectra(mainWindow.LcmsSpectrumCollection, peakAreaBean.Ms2LevelDatapointNumber);
            //    }
            //}

            return ms2Spectrum;
        }

        private static void writeMsAnnotationTag(StreamWriter sw, MainWindow mainWindow, MS2DecResult ms2DecResult,
            AlignmentPropertyBean alignmentProperty, AlignedDriftSpotPropertyBean driftProperty)
        {
            var projectProp = mainWindow.ProjectProperty;
            var mspDB = mainWindow.MspDB;
            var txtDB = mainWindow.PostIdentificationTxtDB;
            var param = mainWindow.AnalysisParamForLC;

            var name = param.IsIonMobility
                ? driftProperty.MetaboliteName
                : alignmentProperty.MetaboliteName;

            //var name = string.Empty;
            //if (param.IsIonMobility) {
            //    name = driftProperty.MetaboliteName;
            //}
            //else {
            //    name = alignmentProperty.MetaboliteName;
            //}

            if (name == string.Empty || name.Contains("w/o")) name = "Unknown";
            var adduct = param.IsIonMobility ? driftProperty.AdductIonName : alignmentProperty.AdductIonName;
            if ((adduct == null || adduct == string.Empty) && mainWindow.ProjectProperty.IonMode == IonMode.Positive) adduct = "[M+H]+";
            else if ((adduct == null || adduct == string.Empty) && mainWindow.ProjectProperty.IonMode == IonMode.Negative) adduct = "[M-H]-";

            if (!param.IsIonMobility && alignmentProperty.AdductIonNameFromAmalgamation != null && alignmentProperty.AdductIonNameFromAmalgamation != string.Empty)
            {
                adduct = alignmentProperty.AdductIonNameFromAmalgamation;
            }

            var alignmentID = param.IsIonMobility ? driftProperty.MasterID : alignmentProperty.AlignmentID;
            var mspID = param.IsIonMobility ? driftProperty.LibraryID : alignmentProperty.LibraryID;
            var txtID = param.IsIonMobility ? driftProperty.PostIdentificationLibraryID : alignmentProperty.PostIdentificationLibraryID;
            var comment = param.IsIonMobility ? driftProperty.Comment : alignmentProperty.Comment;

            sw.WriteLine("NAME: " + name);
            sw.WriteLine("SCANNUMBER: " + alignmentID);
            sw.WriteLine("RETENTIONTIME: " + alignmentProperty.CentralRetentionTime);
            sw.WriteLine("PRECURSORMZ: " + alignmentProperty.CentralAccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + adduct);
            if (param.IsIonMobility)
            {
                sw.WriteLine("MOBILITY: " + driftProperty.CentralDriftTime);
                sw.WriteLine("CCS: " + driftProperty.CentralCcs);
            }
            sw.WriteLine("IONMODE: " + mainWindow.ProjectProperty.IonMode);
            sw.WriteLine("SPECTRUMTYPE: Centroid");
            sw.WriteLine("INTENSITY: " + ms2DecResult.Ms1PeakHeight);
            sw.WriteLine("INCHIKEY: " + RefDataRetrieve.GetInChIKey(mspID, mspDB, txtID, txtDB));
            sw.WriteLine("SMILES: " + RefDataRetrieve.GetSMILES(mspID, mspDB, txtID, txtDB));
            sw.WriteLine("FORMULA: " + RefDataRetrieve.GetFormula(mspID, mspDB, txtID, txtDB));
            sw.WriteLine("ONTOLOGY: " + RefDataRetrieve.GetOntology(mspID, mspDB, txtID, txtDB));

            if (projectProp.FinalSavedDate != null)
            {
                sw.WriteLine("DATE: " + projectProp.FinalSavedDate.Date);
            }

            if (projectProp.Authors != null && projectProp.Authors != string.Empty)
            {
                sw.WriteLine("AUTHORS: " + projectProp.Authors);
            }

            if (projectProp.License != null && projectProp.License != string.Empty)
            {
                sw.WriteLine("LICENSE: " + projectProp.License);
            }

            if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty)
            {
                sw.WriteLine("COLLISIONENERGY: " + projectProp.CollisionEnergy);
            }

            if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty)
            {
                sw.WriteLine("INSTRUMENTTYPE: " + projectProp.InstrumentType);
            }

            if (projectProp.Instrument != null && projectProp.Instrument != string.Empty)
            {
                sw.WriteLine("INSTRUMENT: " + projectProp.Instrument);
            }

            //if (projectProp.Comment != null && projectProp.Comment != string.Empty) {
            //    sw.WriteLine("COMMENT: " + projectProp.Comment);
            //}
            sw.WriteLine("COMMENT: " + comment);

            sw.Write("MSTYPE: ");
            sw.WriteLine("MS1");

            sw.WriteLine("Num Peaks: 3");
            sw.WriteLine(Math.Round(ms2DecResult.Ms1AccurateMass, 5) + "\t" + ms2DecResult.Ms1PeakHeight);
            sw.WriteLine(Math.Round(ms2DecResult.Ms1AccurateMass + 1.00335, 5) + "\t" + ms2DecResult.Ms1IsotopicIonM1PeakHeight);
            sw.WriteLine(Math.Round(ms2DecResult.Ms1AccurateMass + 2.00671, 5) + "\t" + ms2DecResult.Ms1IsotopicIonM2PeakHeight);

            sw.Write("MSTYPE: ");
            sw.WriteLine("MS2");

            sw.Write("Num Peaks: ");
            sw.WriteLine(ms2DecResult.MassSpectra.Count);

            for (int i = 0; i < ms2DecResult.MassSpectra.Count; i++)
                sw.WriteLine(Math.Round(ms2DecResult.MassSpectra[i][0], 5) + "\t" + Math.Round(ms2DecResult.MassSpectra[i][1], 0));
        }

        private static void writeMsAnnotationTag(StreamWriter sw, PeakAreaBean peakAreaBean, DriftSpotBean driftSpot,
            ObservableCollection<double[]> ms1Spectrum, ObservableCollection<double[]> ms2Spectrum,
            MainWindow mainWindow, ExportspectraType spectraType)
        {
            var projectProp = mainWindow.ProjectProperty;
            var mspDB = mainWindow.MspDB;
            var txtDB = mainWindow.PostIdentificationTxtDB;
            var param = mainWindow.AnalysisParamForLC;
            var isIonMobility = param.IsIonMobility;
            var name = isIonMobility ? driftSpot.MetaboliteName : peakAreaBean.MetaboliteName;
            if (name == string.Empty || name.Contains("w/o")) name = "Unknown";

            var adduct = isIonMobility ? driftSpot.AdductIonName : peakAreaBean.AdductIonName;
            if ((adduct == null || adduct == string.Empty) && mainWindow.ProjectProperty.IonMode == IonMode.Positive) adduct = "[M+H]+";
            else if ((adduct == null || adduct == string.Empty) && mainWindow.ProjectProperty.IonMode == IonMode.Negative) adduct = "[M-H]-";

            if (!isIonMobility && peakAreaBean.AdductFromAmalgamation != null && peakAreaBean.AdductFromAmalgamation.FormatCheck)
            {
                adduct = peakAreaBean.AdductFromAmalgamation.AdductIonName;
            }

            var scanNum = isIonMobility ? driftSpot.MasterPeakID : peakAreaBean.PeakID;
            var mspID = isIonMobility ? driftSpot.LibraryID : peakAreaBean.LibraryID;
            var txtID = isIonMobility ? driftSpot.PostIdentificationLibraryId : peakAreaBean.PostIdentificationLibraryId;
            var intensity = isIonMobility ? driftSpot.IntensityAtPeakTop : peakAreaBean.IntensityAtPeakTop;
            var comment = isIonMobility ? driftSpot.Comment : peakAreaBean.Comment;

            sw.WriteLine("NAME: " + name);
            sw.WriteLine("SCANNUMBER: " + scanNum);
            sw.WriteLine("RETENTIONTIME: " + peakAreaBean.RtAtPeakTop);
            sw.WriteLine("PRECURSORMZ: " + peakAreaBean.AccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + adduct);
            if (isIonMobility)
            {
                sw.WriteLine("MOBILITY: " + driftSpot.DriftTimeAtPeakTop);
                sw.WriteLine("CCS: " + driftSpot.Ccs);

            }
            sw.WriteLine("IONMODE: " + mainWindow.ProjectProperty.IonMode);
            sw.WriteLine("INCHIKEY: " + RefDataRetrieve.GetInChIKey(mspID, mspDB, txtID, txtDB));
            sw.WriteLine("SMILES: " + RefDataRetrieve.GetSMILES(mspID, mspDB, txtID, txtDB));
            sw.WriteLine("FORMULA: " + RefDataRetrieve.GetFormula(mspID, mspDB, txtID, txtDB));
            sw.WriteLine("ONTOLOGY: " + RefDataRetrieve.GetOntology(mspID, mspDB, txtID, txtDB));


            sw.Write("SPECTRUMTYPE: ");
            if (spectraType != ExportspectraType.profile) sw.WriteLine("Centroid");
            else sw.WriteLine("Profile");

            sw.WriteLine("INTENSITY: " + intensity);

            if (projectProp.FinalSavedDate != null)
            {
                sw.WriteLine("DATE: " + projectProp.FinalSavedDate.Date);
            }

            if (projectProp.Authors != null && projectProp.Authors != string.Empty)
            {
                sw.WriteLine("AUTHORS: " + projectProp.Authors);
            }

            if (projectProp.License != null && projectProp.License != string.Empty)
            {
                sw.WriteLine("LICENSE: " + projectProp.License);
            }

            if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty)
            {
                sw.WriteLine("COLLISIONENERGY: " + projectProp.CollisionEnergy);
            }

            if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty)
            {
                sw.WriteLine("INSTRUMENTTYPE: " + projectProp.InstrumentType);
            }

            if (projectProp.Instrument != null && projectProp.Instrument != string.Empty)
            {
                sw.WriteLine("INSTRUMENT: " + projectProp.Instrument);
            }

            //if (projectProp.Comment != null && projectProp.Comment != string.Empty) {
            //    sw.WriteLine("COMMENT: " + projectProp.Comment);
            //}
            sw.WriteLine("COMMENT: " + comment);

            sw.WriteLine("MSTYPE: MS1");
            sw.WriteLine("Num Peaks: " + ms1Spectrum.Count);

            for (int i = 0; i < ms1Spectrum.Count; i++)
                sw.WriteLine(Math.Round(ms1Spectrum[i][0], 5) + "\t" + Math.Round(ms1Spectrum[i][1], 0));

            sw.WriteLine("MSTYPE: MS2");
            sw.WriteLine("Num Peaks: " + ms2Spectrum.Count);

            for (int i = 0; i < ms2Spectrum.Count; i++)
                sw.WriteLine(Math.Round(ms2Spectrum[i][0], 5) + "\t" + Math.Round(ms2Spectrum[i][1], 0));
        }

        /// <summary>
        /// This is the export function for detected peaks in a sample
        /// </summary>
        /// <param name="mainWindow"></param>
        /// <param name="folderpath"></param>
        /// <param name="files"></param>
        /// <param name="methodType"></param>
        /// <param name="exportSpectraType"></param>
        private static void peaklistTxtExport(MainWindow mainWindow, string folderpath,
            ObservableCollection<AnalysisFileBean> files,
            MethodType methodType, ExportspectraType exportSpectraType, float isotopeExportMax)
        {
            var rdamProp = mainWindow.RdamProperty;
            var rdamFileToID = rdamProp.RdamFilePath_RdamFileID;
            var rdamFileCollection = rdamProp.RdamFileContentBeanCollection;
            var param = mainWindow.AnalysisParamForLC;

            for (int i = 0; i < files.Count; i++)
            {
                var fileProp = files[i].AnalysisFilePropertyBean;
                var filePath = folderpath + "\\" + fileProp.AnalysisFileName + "." + ExportSpectraFileFormat.txt;
                using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
                {
                    var fileID = rdamFileToID[fileProp.AnalysisFilePath];
                    var measurementID = rdamFileCollection[fileID].FileID_MeasurementID[fileProp.AnalysisFileId];
                    using (var rawDataAccess = new RawDataAccess(fileProp.AnalysisFilePath, measurementID, false, false, true, files[i].RetentionTimeCorrectionBean.PredictedRt))
                    { // open rdam stream
                        using (var fs = File.Open(files[i].AnalysisFilePropertyBean.DeconvolutionFilePath, FileMode.Open, FileAccess.ReadWrite))
                        { // open dcl stream

                            //set seekpoint to retrieve MS2DecResult
                            var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

                            //set List<PeakAreaBean> on AnalysisFileBean
                            DataStorageLcUtility.SetPeakAreaBeanCollection(files[i], files[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

                            //set raw spectra collection to spectrumCollection
                            var mes = DataAccessLcUtility.GetRawDataMeasurement(rawDataAccess);
                            var spectrumCollection = DataAccessLcUtility.GetAllSpectrumCollection(mes);
                            var accumulatedSpectra = DataAccessLcUtility.GetAccumulatedMs1SpectrumCollection(mes);

                            ResultExportLcUtility.WritePeaklistTextHeader(sw, param.IsIonMobility); // write header

                            var peakSpots = files[i].PeakAreaBeanCollection;

                            for (int j = 0; j < peakSpots.Count; j++)
                            {
                                var peakAreaBean = peakSpots[j];
                                if (param.IsIonMobility)
                                {
                                    ResultExportLcUtility.WriteMs2decResultAsTxt(sw, accumulatedSpectra, spectrumCollection, fs, seekpointList,
                                              peakAreaBean, peakSpots, null, peakAreaBean.DriftSpots, mainWindow.MspDB, mainWindow.PostIdentificationTxtDB, mainWindow.AnalysisParamForLC, mainWindow.ProjectProperty, isotopeExportMax);

                                    foreach (var drift in peakAreaBean.DriftSpots)
                                    {
                                        ResultExportLcUtility.WriteMs2decResultAsTxt(sw, accumulatedSpectra, spectrumCollection, fs, seekpointList,
                                             peakAreaBean, peakSpots, drift, peakAreaBean.DriftSpots, mainWindow.MspDB, mainWindow.PostIdentificationTxtDB, mainWindow.AnalysisParamForLC, mainWindow.ProjectProperty, isotopeExportMax);
                                    }
                                }
                                else
                                {
                                    if (exportSpectraType == ExportspectraType.profile)
                                    {
                                        ResultExportLcUtility.WriteProfileTxt(sw, spectrumCollection, peakAreaBean, peakSpots, mainWindow.MspDB, mainWindow.PostIdentificationTxtDB, param, isotopeExportMax);
                                    }
                                    else if ((exportSpectraType == ExportspectraType.centroid && methodType == MethodType.ddMSMS) || (exportSpectraType == ExportspectraType.deconvoluted && methodType == MethodType.diMSMS))
                                    {
                                        ResultExportLcUtility.WriteMs2decResultAsTxt(sw, spectrumCollection, fs, seekpointList,
                                            peakAreaBean, peakSpots, mainWindow.MspDB, mainWindow.PostIdentificationTxtDB, mainWindow.AnalysisParamForLC, mainWindow.ProjectProperty, isotopeExportMax);
                                    }
                                    else
                                    {
                                        ResultExportLcUtility.WriteCentroidTxt(sw, spectrumCollection, peakAreaBean, peakSpots, mainWindow.MspDB, mainWindow.PostIdentificationTxtDB,
                                            mainWindow.AnalysisParamForLC, mainWindow.ProjectProperty, isotopeExportMax);
                                    }
                                }
                            }

                            //refresh
                            DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(files[i]);
                        }
                    }
                }
            }
        }

        private static void peaklistMspExport(MainWindow mainWindow, string folderpath, ObservableCollection<AnalysisFileBean> files, MethodType methodType, ExportspectraType exportSpectraType)
        {

            var rdamProp = mainWindow.RdamProperty;
            var rdamFileToID = rdamProp.RdamFilePath_RdamFileID;
            var rdamFileCollection = rdamProp.RdamFileContentBeanCollection;
            var param = mainWindow.AnalysisParamForLC;
            var project = mainWindow.ProjectProperty;

            for (int i = 0; i < files.Count; i++)
            {
                var fileProp = files[i].AnalysisFilePropertyBean;
                var filePath = folderpath + "\\" + fileProp.AnalysisFileName + "." + ExportSpectraFileFormat.msp;
                var correctedRTs = files[i].RetentionTimeCorrectionBean.PredictedRt;

                using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
                {
                    var fileID = rdamFileToID[files[i].AnalysisFilePropertyBean.AnalysisFilePath];
                    var measurementID = rdamFileCollection[fileID].FileID_MeasurementID[fileProp.AnalysisFileId];

                    using (var rawDataAccess = new RawDataAccess(files[i].AnalysisFilePropertyBean.AnalysisFilePath, measurementID, false, false, true, correctedRTs))
                    {
                        //set List<PeakAreaBean> on AnalysisFileBean
                        DataStorageLcUtility.SetPeakAreaBeanCollection(files[i], files[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

                        //set raw spectra collection to spectrumCollection
                        var mes = DataAccessLcUtility.GetRawDataMeasurement(rawDataAccess);
                        var spectrumCollection = DataAccessLcUtility.GetAllSpectrumCollection(mes);
                        var accumulatedSpectra = DataAccessLcUtility.GetAccumulatedMs1SpectrumCollection(mes);

                        using (var fs = File.Open(fileProp.DeconvolutionFilePath, FileMode.Open, FileAccess.ReadWrite))
                        { // open dcl stream

                            //set seekpoint to retrieve MS2DecResult
                            var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

                            foreach (var peakspot in files[i].PeakAreaBeanCollection)
                            {
                                if (param.IsIonMobility)
                                {
                                    foreach (var driftspot in peakspot.DriftSpots)
                                    {
                                        if (exportSpectraType == ExportspectraType.profile)
                                        {
                                            ResultExportLcUtility.WriteProfileMsp(sw, spectrumCollection, peakspot, driftspot);
                                        }
                                        else if (exportSpectraType == ExportspectraType.centroid && methodType == MethodType.diMSMS)
                                        {
                                            ResultExportLcUtility.WriteCentroidMsp(sw, spectrumCollection, peakspot,
                                             driftspot, mainWindow.ProjectProperty.DataTypeMS2, param);
                                        }
                                        else
                                        {
                                            ResultExportLcUtility.WriteDeconvolutedMsp(sw, fs, seekpointList, peakspot, driftspot);
                                        }
                                    }
                                }
                                else
                                {
                                    if (exportSpectraType == ExportspectraType.profile)
                                    {
                                        ResultExportLcUtility.WriteProfileMsp(sw, spectrumCollection, peakspot);
                                    }
                                    else if (exportSpectraType == ExportspectraType.centroid && methodType == MethodType.diMSMS)
                                    {
                                        ResultExportLcUtility.WriteCentroidMsp(sw, spectrumCollection,
                                             peakspot, mainWindow.ProjectProperty.DataTypeMS2, param);
                                    }
                                    else
                                    {
                                        ResultExportLcUtility.WriteDeconvolutedMsp(sw, fs, seekpointList, peakspot);
                                    }
                                }
                            }
                        }
                        DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(files[i]);
                    }

                }
            }

            #region old
            //string filePath;
            //for (int i = 0; i < files.Count; i++)
            //{
            //    filePath = folderpath + "\\" + files[i].AnalysisFilePropertyBean.AnalysisFileName + "." + ExportSpectraFileFormat.msp;
            //    using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
            //    {
            //        if (exportSpectraType == ExportspectraType.centroid && methodType == MethodType.diMSMS)
            //        {
            //            var fileID = mainWindow.RdamProperty.RdamFilePath_RdamFileID[files[i].AnalysisFilePropertyBean.AnalysisFilePath];
            //            var measurementID = mainWindow.RdamProperty.RdamFileContentBeanCollection[fileID].FileID_MeasurementID[files[i].AnalysisFilePropertyBean.AnalysisFileId];

            //            using (var rawDataAccess = new RawDataAccess(files[i].AnalysisFilePropertyBean.AnalysisFilePath, measurementID, true, files[i].RetentionTimeCorrectionBean.PredictedRt))
            //            {
            //                var spectrumCollection = DataAccessLcUtility.GetRdamSpectrumCollection(rawDataAccess);

            //                DataStorageLcUtility.SetPeakAreaBeanCollection(files[i], files[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

            //                for (int j = 0; j < files[i].PeakAreaBeanCollection.Count; j++)
            //                    ResultExportLcUtility.WriteProfileMsp(sw, spectrumCollection, files[i].PeakAreaBeanCollection[j]);

            //                DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(files[i]);
            //            }
            //        }
            //        else if (exportSpectraType == ExportspectraType.centroid && methodType == MethodType.diMSMS)
            //        {
            //            var fileID = mainWindow.RdamProperty.RdamFilePath_RdamFileID[files[i].AnalysisFilePropertyBean.AnalysisFilePath];
            //            var measurementID = mainWindow.RdamProperty.RdamFileContentBeanCollection[fileID].FileID_MeasurementID[files[i].AnalysisFilePropertyBean.AnalysisFileId];

            //            using (var rawDataAccess = new RawDataAccess(files[i].AnalysisFilePropertyBean.AnalysisFilePath, measurementID, true, files[i].RetentionTimeCorrectionBean.PredictedRt))
            //            {
            //                var spectrumCollection = DataAccessLcUtility.GetRdamSpectrumCollection(rawDataAccess);

            //                DataStorageLcUtility.SetPeakAreaBeanCollection(files[i], files[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

            //                for (int j = 0; j < files[i].PeakAreaBeanCollection.Count; j++)
            //                    ResultExportLcUtility.WriteCentroidMsp(sw, spectrumCollection,
            //                        files[i].PeakAreaBeanCollection[j], mainWindow.ProjectProperty.DataTypeMS2, mainWindow.AnalysisParamForLC);

            //                DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(files[i]);
            //            }
            //        }
            //        else
            //        {
            //            var fs = File.Open(files[i].AnalysisFilePropertyBean.DeconvolutionFilePath, FileMode.Open, FileAccess.ReadWrite);
            //            var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

            //            DataStorageLcUtility.SetPeakAreaBeanCollection(files[i], files[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

            //            for (int j = 0; j < files[i].PeakAreaBeanCollection.Count; j++)
            //            {
            //                if (methodType == MethodType.diMSMS)
            //                    ResultExportLcUtility.WriteDeconvolutedMsp(sw, fs, seekpointList, files[i].PeakAreaBeanCollection[j]);
            //                else
            //                    ResultExportLcUtility.WriteCentroidedMsp(sw, fs, seekpointList, files[i].PeakAreaBeanCollection[j]);
            //            }

            //            DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(files[i]);

            //            fs.Dispose();
            //            fs.Close();
            //        }
            //    }
            //}
            #endregion
        }

        private static void peaklistMgfExport(MainWindow mainWindow, string folderpath,
            ObservableCollection<AnalysisFileBean> files, MethodType methodType, ExportspectraType exportSpectraType)
        {
            var rdamProp = mainWindow.RdamProperty;
            var rdamFileToID = rdamProp.RdamFilePath_RdamFileID;
            var rdamFileCollection = rdamProp.RdamFileContentBeanCollection;
            var param = mainWindow.AnalysisParamForLC;
            var project = mainWindow.ProjectProperty;

            for (int i = 0; i < files.Count; i++)
            {
                var fileProp = files[i].AnalysisFilePropertyBean;
                var filePath = folderpath + "\\" + fileProp.AnalysisFileName + "." + ExportSpectraFileFormat.mgf;
                var correctedRTs = files[i].RetentionTimeCorrectionBean.PredictedRt;

                using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
                {
                    var fileID = rdamFileToID[files[i].AnalysisFilePropertyBean.AnalysisFilePath];
                    var measurementID = rdamFileCollection[fileID].FileID_MeasurementID[fileProp.AnalysisFileId];

                    using (var rawDataAccess = new RawDataAccess(files[i].AnalysisFilePropertyBean.AnalysisFilePath, measurementID, false, false, true, correctedRTs))
                    {
                        //set List<PeakAreaBean> on AnalysisFileBean
                        DataStorageLcUtility.SetPeakAreaBeanCollection(files[i], files[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

                        //set raw spectra collection to spectrumCollection
                        var mes = DataAccessLcUtility.GetRawDataMeasurement(rawDataAccess);
                        var spectrumCollection = DataAccessLcUtility.GetAllSpectrumCollection(mes);
                        var accumulatedSpectra = DataAccessLcUtility.GetAccumulatedMs1SpectrumCollection(mes);

                        using (var fs = File.Open(fileProp.DeconvolutionFilePath, FileMode.Open, FileAccess.ReadWrite))
                        { // open dcl stream

                            //set seekpoint to retrieve MS2DecResult
                            var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

                            foreach (var peakspot in files[i].PeakAreaBeanCollection)
                            {
                                if (param.IsIonMobility)
                                {
                                    foreach (var driftspot in peakspot.DriftSpots)
                                    {
                                        if (exportSpectraType == ExportspectraType.profile)
                                        {
                                            ResultExportLcUtility.WriteProfileMgf(sw, spectrumCollection, peakspot, driftspot);
                                        }
                                        else if (exportSpectraType == ExportspectraType.centroid && methodType == MethodType.diMSMS)
                                        {
                                            ResultExportLcUtility.WriteCentroidMgf(sw, spectrumCollection, project, peakspot, driftspot, param);
                                        }
                                        else
                                        {
                                            ResultExportLcUtility.WriteDeconvolutedMgf(sw, fs, seekpointList, peakspot, driftspot);
                                        }
                                    }
                                }
                                else
                                {
                                    if (exportSpectraType == ExportspectraType.profile)
                                    {
                                        ResultExportLcUtility.WriteProfileMgf(sw, spectrumCollection, peakspot);
                                    }
                                    else if (exportSpectraType == ExportspectraType.centroid && methodType == MethodType.diMSMS)
                                    {
                                        ResultExportLcUtility.WriteCentroidMgf(sw, spectrumCollection, mainWindow.ProjectProperty.DataTypeMS2, peakspot, mainWindow.AnalysisParamForLC);
                                    }
                                    else
                                    {
                                        ResultExportLcUtility.WriteDeconvolutedMgf(sw, fs, seekpointList, peakspot);
                                    }
                                }
                            }
                        }
                        DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(files[i]);
                    }
                    #region old

                    //if (exportSpectraType == ExportspectraType.profile) {
                    //    var fileID = rdamFileToID[fileProp.AnalysisFilePath];
                    //    var measurementID = rdamFileCollection[fileID].FileID_MeasurementID[fileProp.AnalysisFileId];

                    //    using (var rawDataAccess = new RawDataAccess(fileProp.AnalysisFilePath, measurementID, true, correctedRTs)) {
                    //        //set List<PeakAreaBean> on AnalysisFileBean
                    //        DataStorageLcUtility.SetPeakAreaBeanCollection(files[i], files[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

                    //        //set raw spectra collection to spectrumCollection
                    //        var mes = DataAccessLcUtility.GetRawDataMeasurement(rawDataAccess);
                    //        var spectrumCollection = DataAccessLcUtility.GetAllSpectrumCollection(mes);
                    //        var accumulatedSpectra = DataAccessLcUtility.GetAccumulatedMs1SpectrumCollection(mes);

                    //        foreach (var peakspot in files[i].PeakAreaBeanCollection) {
                    //            if (param.IsIonMobility) {
                    //                foreach (var driftspot in peakspot.DriftSpots) {
                    //                    ResultExportLcUtility.WriteProfileMgf(sw, spectrumCollection, driftspot);
                    //                }
                    //            }
                    //            else {
                    //                ResultExportLcUtility.WriteProfileMgf(sw, spectrumCollection, peakspot);
                    //            }
                    //        }

                    //        DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(files[i]);
                    //    }
                    //}
                    //else if (exportSpectraType == ExportspectraType.centroid && methodType == MethodType.diMSMS) {
                    //    var fileID = rdamFileToID[files[i].AnalysisFilePropertyBean.AnalysisFilePath];
                    //    var measurementID = rdamFileCollection[fileID].FileID_MeasurementID[fileProp.AnalysisFileId];

                    //    using (var rawDataAccess = new RawDataAccess(files[i].AnalysisFilePropertyBean.AnalysisFilePath, measurementID, true, correctedRTs)) {
                    //        //set List<PeakAreaBean> on AnalysisFileBean
                    //        DataStorageLcUtility.SetPeakAreaBeanCollection(files[i], files[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

                    //        //set raw spectra collection to spectrumCollection
                    //        var mes = DataAccessLcUtility.GetRawDataMeasurement(rawDataAccess);
                    //        var spectrumCollection = DataAccessLcUtility.GetAllSpectrumCollection(mes);
                    //        var accumulatedSpectra = DataAccessLcUtility.GetAccumulatedMs1SpectrumCollection(mes);

                    //        foreach (var peakspot in files[i].PeakAreaBeanCollection) {
                    //            if (param.IsIonMobility) {
                    //                foreach (var driftspot in peakspot.DriftSpots) {
                    //                    ResultExportLcUtility.WriteCentroidMgf(sw, spectrumCollection, project, peakspot, driftspot, param);
                    //                }
                    //            }
                    //            else {
                    //                ResultExportLcUtility.WriteCentroidMgf(sw, spectrumCollection, mainWindow.ProjectProperty.DataTypeMS2,
                    //                    peakspot, mainWindow.AnalysisParamForLC);
                    //            }
                    //        }
                    //        DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(files[i]);
                    //    }
                    //}
                    //else {

                    //    var fileID = rdamFileToID[files[i].AnalysisFilePropertyBean.AnalysisFilePath];
                    //    var measurementID = rdamFileCollection[fileID].FileID_MeasurementID[fileProp.AnalysisFileId];

                    //    using (var rawDataAccess = new RawDataAccess(files[i].AnalysisFilePropertyBean.AnalysisFilePath, measurementID, true, correctedRTs)) {
                    //        //set List<PeakAreaBean> on AnalysisFileBean
                    //        DataStorageLcUtility.SetPeakAreaBeanCollection(files[i], files[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

                    //        //set raw spectra collection to spectrumCollection
                    //        var mes = DataAccessLcUtility.GetRawDataMeasurement(rawDataAccess);
                    //        var spectrumCollection = DataAccessLcUtility.GetAllSpectrumCollection(mes);
                    //        var accumulatedSpectra = DataAccessLcUtility.GetAccumulatedMs1SpectrumCollection(mes);

                    //        using (var fs = File.Open(fileProp.DeconvolutionFilePath, FileMode.Open, FileAccess.ReadWrite)) { // open dcl stream

                    //            //set seekpoint to retrieve MS2DecResult
                    //            var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

                    //            foreach (var peakspot in files[i].PeakAreaBeanCollection) {
                    //                if (param.IsIonMobility) {
                    //                    foreach (var driftspot in peakspot.DriftSpots) {

                    //                        if (exportSpectraType == ExportspectraType.profile) {
                    //                            ResultExportLcUtility.WriteProfileMgf(sw, spectrumCollection, driftspot);
                    //                        }
                    //                        else if (exportSpectraType == ExportspectraType.centroid && methodType == MethodType.diMSMS) {
                    //                            ResultExportLcUtility.WriteCentroidMgf(sw, spectrumCollection, project, peakspot, driftspot, param);
                    //                        }
                    //                        else {
                    //                            ResultExportLcUtility.WriteDeconvolutedMgf(sw, fs, seekpointList, driftspot);
                    //                        }
                    //                    }
                    //                }
                    //                else {
                    //                    if (exportSpectraType == ExportspectraType.profile) {
                    //                        ResultExportLcUtility.WriteProfileMgf(sw, spectrumCollection, peakspot);
                    //                    }
                    //                    else if (exportSpectraType == ExportspectraType.centroid && methodType == MethodType.diMSMS) {
                    //                        ResultExportLcUtility.WriteCentroidMgf(sw, spectrumCollection, mainWindow.ProjectProperty.DataTypeMS2, peakspot, mainWindow.AnalysisParamForLC);
                    //                    }
                    //                    else {
                    //                        ResultExportLcUtility.WriteDeconvolutedMgf(sw, fs, seekpointList, peakspot);
                    //                    }
                    //                }
                    //            }
                    //        }
                    //        DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(files[i]);
                    //    }
                    //}
                    #endregion
                }
            }
        }

        private static void peaklistSriusMsExport(MainWindow mainWindow, string folderpath,
            ObservableCollection<AnalysisFileBean> files,
            MethodType methodType, ExportspectraType exportSpectraType, float isotopeExportMax)
        {

            var rdamProp = mainWindow.RdamProperty;
            var rdamFileToID = rdamProp.RdamFilePath_RdamFileID;
            var rdamFileCollection = rdamProp.RdamFileContentBeanCollection;
            var param = mainWindow.AnalysisParamForLC;
            var project = mainWindow.ProjectProperty;
            var dt = DateTime.Now;
            var timeString = dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + dt.Hour.ToString() + dt.Minute.ToString();
            for (int i = 0; i < files.Count; i++)
            {
                var fileProp = files[i].AnalysisFilePropertyBean;
                var output_dir = System.IO.Path.Combine(folderpath, fileProp.AnalysisFileName);
                if (!Directory.Exists(output_dir))
                {
                    Directory.CreateDirectory(output_dir);
                }

                var correctedRTs = files[i].RetentionTimeCorrectionBean.PredictedRt;
                var fileID = rdamFileToID[fileProp.AnalysisFilePath];
                var measurementID = rdamFileCollection[fileID].FileID_MeasurementID[fileProp.AnalysisFileId];
                using (var rawDataAccess = new RawDataAccess(fileProp.AnalysisFilePath, measurementID, false, false, true, correctedRTs))
                { // open rdam stream
                    using (var fs = File.Open(files[i].AnalysisFilePropertyBean.DeconvolutionFilePath, FileMode.Open, FileAccess.ReadWrite))
                    { // open dcl stream

                        //set seekpoint to retrieve MS2DecResult
                        var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

                        //set List<PeakAreaBean> on AnalysisFileBean
                        DataStorageLcUtility.SetPeakAreaBeanCollection(files[i], files[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

                        //set raw spectra collection to spectrumCollection
                        var mes = DataAccessLcUtility.GetRawDataMeasurement(rawDataAccess);
                        var spectrumCollection = DataAccessLcUtility.GetAllSpectrumCollection(mes);
                        var accumulatedSpectra = DataAccessLcUtility.GetAccumulatedMs1SpectrumCollection(mes);

                        foreach (var peakspot in files[i].PeakAreaBeanCollection)
                        {
                            if (param.IsIonMobility)
                            {
                                foreach (var driftspot in peakspot.DriftSpots)
                                {

                                    var filename = "fID_" + fileID + "_pID_" + peakspot.PeakID + "_" + Math.Round(peakspot.RtAtPeakTop, 2).ToString() + "_"
                                        + Math.Round(driftspot.DriftTimeAtPeakTop, 2).ToString() + "_"
                                        + Math.Round(peakspot.AccurateMass, 4).ToString() + "_" + timeString + "." + SaveFileFormat.ms;
                                    var filePath = System.IO.Path.Combine(output_dir, filename);

                                    using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
                                    {
                                        if (exportSpectraType == ExportspectraType.profile)
                                        {
                                            ResultExportLcUtility.WriteProfileAsSiriusMs(sw, spectrumCollection, peakspot, driftspot, mainWindow.MspDB, isotopeExportMax);
                                        }
                                        else
                                        {
                                            ResultExportLcUtility.WriteMs2DecAsSiriusMs(sw, spectrumCollection, fs, seekpointList,
                                               peakspot, driftspot, mainWindow.MspDB, mainWindow.AnalysisParamForLC, mainWindow.ProjectProperty, isotopeExportMax);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                var filename = "fID_" + fileID + "_pID_" + peakspot.PeakID + "_" + Math.Round(peakspot.RtAtPeakTop, 2).ToString() + "_"
                                    + Math.Round(peakspot.AccurateMass, 4).ToString() + "_" + timeString + "." + SaveFileFormat.ms;
                                var filePath = System.IO.Path.Combine(output_dir, filename);
                                using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
                                {
                                    if (exportSpectraType == ExportspectraType.profile)
                                    {
                                        ResultExportLcUtility.WriteProfileAsSiriusMs(sw, spectrumCollection, peakspot, mainWindow.MspDB, isotopeExportMax);
                                    }
                                    else
                                    {
                                        ResultExportLcUtility.WriteMs2DecAsSiriusMs(sw, spectrumCollection, fs, seekpointList,
                                           peakspot, mainWindow.MspDB, mainWindow.AnalysisParamForLC, mainWindow.ProjectProperty, isotopeExportMax);
                                    }
                                }
                            }
                        }

                        //refresh
                        DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(files[i]);
                    }
                }
            }
        }

        private static void peaklistMatExport(MainWindow mainWindow, string folderpath,
            ObservableCollection<AnalysisFileBean> files,
            MethodType methodType, ExportspectraType exportSpectraType, float isotopeExportMax)
        {
            var rdamProp = mainWindow.RdamProperty;
            var rdamFileToID = rdamProp.RdamFilePath_RdamFileID;
            var rdamFileCollection = rdamProp.RdamFileContentBeanCollection;
            var param = mainWindow.AnalysisParamForLC;
            var project = mainWindow.ProjectProperty;
            var dt = DateTime.Now;
            var timeString = dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + dt.Hour.ToString() + dt.Minute.ToString();
            for (int i = 0; i < files.Count; i++)
            {
                var fileProp = files[i].AnalysisFilePropertyBean;
                //var filePath = folderpath + "\\" + fileProp.AnalysisFileName + "." + ExportSpectraFileFormat.mat;

                var output_dir = System.IO.Path.Combine(folderpath, fileProp.AnalysisFileName);
                if (!Directory.Exists(output_dir))
                {
                    Directory.CreateDirectory(output_dir);
                }

                var correctedRTs = files[i].RetentionTimeCorrectionBean.PredictedRt;
                var fileID = rdamFileToID[fileProp.AnalysisFilePath];
                var measurementID = rdamFileCollection[fileID].FileID_MeasurementID[fileProp.AnalysisFileId];
                using (var rawDataAccess = new RawDataAccess(fileProp.AnalysisFilePath, measurementID, false, false, true, correctedRTs))
                { // open rdam stream
                    using (var fs = File.Open(files[i].AnalysisFilePropertyBean.DeconvolutionFilePath, FileMode.Open, FileAccess.ReadWrite))
                    { // open dcl stream

                        //set seekpoint to retrieve MS2DecResult
                        var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

                        //set List<PeakAreaBean> on AnalysisFileBean
                        DataStorageLcUtility.SetPeakAreaBeanCollection(files[i], files[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

                        //set raw spectra collection to spectrumCollection
                        var mes = DataAccessLcUtility.GetRawDataMeasurement(rawDataAccess);
                        var spectrumCollection = DataAccessLcUtility.GetAllSpectrumCollection(mes);
                        var accumulatedSpectra = DataAccessLcUtility.GetAccumulatedMs1SpectrumCollection(mes);

                        foreach (var peakspot in files[i].PeakAreaBeanCollection)
                        {
                            if (param.IsIonMobility)
                            {
                                foreach (var driftspot in peakspot.DriftSpots)
                                {

                                    var filename = "fID_" + fileID + "_pID_" + peakspot.PeakID + "_" + Math.Round(peakspot.RtAtPeakTop, 2).ToString() + "_"
                                        + Math.Round(driftspot.DriftTimeAtPeakTop, 2).ToString() + "_"
                                        + Math.Round(peakspot.AccurateMass, 4).ToString() + "_" + timeString + "." + SaveFileFormat.mat;
                                    var filePath = System.IO.Path.Combine(output_dir, filename);

                                    using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
                                    {
                                        if (exportSpectraType == ExportspectraType.profile)
                                        {
                                            ResultExportLcUtility.WriteProfileAsMat(sw, spectrumCollection, peakspot, driftspot, mainWindow.MspDB, isotopeExportMax);
                                        }
                                        else
                                        {
                                            ResultExportLcUtility.WriteMs2DecAsMat(sw, spectrumCollection, fs, seekpointList,
                                               peakspot, driftspot, mainWindow.MspDB, mainWindow.AnalysisParamForLC, mainWindow.ProjectProperty, isotopeExportMax);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                var filename = "fID_" + fileID + "_pID_" + peakspot.PeakID + "_" + Math.Round(peakspot.RtAtPeakTop, 2).ToString() + "_"
                                    + Math.Round(peakspot.AccurateMass, 4).ToString() + "_" + timeString + "." + SaveFileFormat.mat;
                                var filePath = System.IO.Path.Combine(output_dir, filename);
                                using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
                                {
                                    if (exportSpectraType == ExportspectraType.profile)
                                    {
                                        ResultExportLcUtility.WriteProfileAsMat(sw, spectrumCollection, peakspot, mainWindow.MspDB, isotopeExportMax);
                                    }
                                    else
                                    {
                                        ResultExportLcUtility.WriteMs2DecAsMat(sw, spectrumCollection, fs, seekpointList,
                                           peakspot, mainWindow.MspDB, mainWindow.AnalysisParamForLC, mainWindow.ProjectProperty, isotopeExportMax);
                                    }
                                }
                            }
                        }

                        //refresh
                        DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(files[i]);
                    }
                }






                //using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
                //{
                //    var fileID = rdamFileToID[fileProp.AnalysisFilePath];
                //    var measurementID = rdamFileCollection[fileID].FileID_MeasurementID[fileProp.AnalysisFileId];

                //    using (var rawDataAccess = new RawDataAccess(fileProp.AnalysisFilePath, measurementID, true, correctedRTs))
                //    { // open rdam stream

                //        using (var fs = File.Open(files[i].AnalysisFilePropertyBean.DeconvolutionFilePath, FileMode.Open, FileAccess.ReadWrite))
                //        { // open dcl stream

                //            //set seekpoint to retrieve MS2DecResult
                //            var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

                //            //set List<PeakAreaBean> on AnalysisFileBean
                //            DataStorageLcUtility.SetPeakAreaBeanCollection(files[i], files[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

                //            //set raw spectra collection to spectrumCollection
                //            var mes = DataAccessLcUtility.GetRawDataMeasurement(rawDataAccess);
                //            var spectrumCollection = DataAccessLcUtility.GetAllSpectrumCollection(mes);
                //            var accumulatedSpectra = DataAccessLcUtility.GetAccumulatedMs1SpectrumCollection(mes);

                //            foreach (var peakspot in files[i].PeakAreaBeanCollection)
                //            {
                //                if (param.IsIonMobility)
                //                {
                //                    foreach (var driftspot in peakspot.DriftSpots)
                //                    {
                //                        if (exportSpectraType == ExportspectraType.profile)
                //                        {
                //                            ResultExportLcUtility.WriteProfileAsMat(sw, spectrumCollection, peakspot, driftspot, mainWindow.MspDB, isotopeExportMax);
                //                        }
                //                        else
                //                        {
                //                            ResultExportLcUtility.WriteMs2DecAsMat(sw, spectrumCollection, fs, seekpointList,
                //                               peakspot, driftspot, mainWindow.MspDB, mainWindow.AnalysisParamForLC, mainWindow.ProjectProperty, isotopeExportMax);
                //                        }
                //                    }
                //                }
                //                else
                //                {
                //                    if (exportSpectraType == ExportspectraType.profile)
                //                    {
                //                        ResultExportLcUtility.WriteProfileAsMat(sw, spectrumCollection, peakspot, mainWindow.MspDB, isotopeExportMax);
                //                    }
                //                    else
                //                    {
                //                        ResultExportLcUtility.WriteMs2DecAsMat(sw, spectrumCollection, fs, seekpointList,
                //                           peakspot, mainWindow.MspDB, mainWindow.AnalysisParamForLC, mainWindow.ProjectProperty, isotopeExportMax);
                //                    }
                //                }
                //            }

                //            //refresh
                //            DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(files[i]);
                //        }
                //    }
                //}
            }
        }

        private static void exportUniqueMsDataMatrix(MainWindow mainWindow, int alignmentFileID, string uniqueMsMatrixExportFilePath,
            AlignmentResultBean alignmentResultBean, List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> txtDB, ObservableCollection<AnalysisFileBean> analysisFiles, bool blankFilter)
        {
            var deconvolutedUniqueMsArrayList = getUniqueMsArrayList(mainWindow, alignmentResultBean);

            var fs = File.Open(mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite);
            var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

            using (StreamWriter sw = new StreamWriter(uniqueMsMatrixExportFilePath, false, Encoding.ASCII))
            {
                //Header
                ResultExportLcUtility.WriteDataMatrixHeader(sw, analysisFiles);

                //From the second
                var aignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
                for (int i = 0; i < aignedSpots.Count; i++)
                {
                    if (blankFilter && aignedSpots[i].IsBlankFiltered) continue;
                    ResultExportLcUtility.WriteDataMatrixMetaData(sw, aignedSpots[i], aignedSpots, mspDB, txtDB, fs, seekpointList, mainWindow.AnalysisParamForLC);

                    for (int j = 0; j < alignmentResultBean.AlignmentPropertyBeanCollection[i].AlignedPeakPropertyBeanCollection.Count; j++)
                    {
                        if (j == alignmentResultBean.AlignmentPropertyBeanCollection[i].AlignedPeakPropertyBeanCollection.Count - 1)
                            sw.WriteLine(deconvolutedUniqueMsArrayList[j][i]);
                        else
                            sw.Write(deconvolutedUniqueMsArrayList[j][i] + "\t");
                    }
                }
            }

            fs.Dispose();
            fs.Close();
        }

        private static List<double[]> getUniqueMsArrayList(MainWindow mainWindow, AlignmentResultBean alignmentResultBean)
        {
            int alignmentIdLength = alignmentResultBean.AlignmentPropertyBeanCollection.Count;
            int sampleNumber = alignmentResultBean.AlignmentPropertyBeanCollection[0].AlignedPeakPropertyBeanCollection.Count;

            List<double[]> uniqueMsArrayList = new List<double[]>();
            double[] uniqueMsArray;

            AnalysisFileBean analysisFileBean;
            List<long> seekpointList;
            MS2DecResult deconvolutionResultBean;

            for (int i = 0; i < sampleNumber; i++)
            {
                uniqueMsArray = new double[alignmentIdLength];
                analysisFileBean = mainWindow.AnalysisFiles[i];

                DataStorageLcUtility.SetPeakAreaBeanCollection(analysisFileBean, analysisFileBean.AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

                using (FileStream deconvolutionFS = File.Open(analysisFileBean.AnalysisFilePropertyBean.DeconvolutionFilePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    seekpointList = new List<long>();
                    seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(deconvolutionFS);

                    for (int j = 0; j < alignmentIdLength; j++)
                    {
                        if (alignmentResultBean.AlignmentPropertyBeanCollection[j].AlignedPeakPropertyBeanCollection[i].PeakID < 0)
                        {
                            uniqueMsArray[j] = -1;
                        }
                        else
                        {
                            deconvolutionResultBean = SpectralDeconvolution.ReadMS2DecResult(deconvolutionFS, seekpointList, alignmentResultBean.AlignmentPropertyBeanCollection[j].AlignedPeakPropertyBeanCollection[i].PeakID);
                            uniqueMsArray[j] = deconvolutionResultBean.UniqueMs;
                        }
                    }
                }

                uniqueMsArrayList.Add(uniqueMsArray);
                DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(analysisFileBean);
            }

            return uniqueMsArrayList;
        }

        private static void exportDeconvolutedPeakAreaDataMatrix(MainWindow mainWindow, int alignmentFileID,
            string deconvolutedPeakAreaDataMatrixExportFilePath, AlignmentResultBean alignmentResultBean,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> txtDB,
            ObservableCollection<AnalysisFileBean> analysisFiles, bool blankFilter)
        {
            var deconvolutedPeakAreaArrayList = getDeconvolutedPeakAreaArrayList(mainWindow, alignmentResultBean);

            var fs = File.Open(mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite);
            var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

            using (StreamWriter sw = new StreamWriter(deconvolutedPeakAreaDataMatrixExportFilePath, false, Encoding.ASCII))
            {
                //Header
                ResultExportLcUtility.WriteDataMatrixHeader(sw, analysisFiles);

                //From the second
                for (int i = 0; i < alignmentResultBean.AlignmentPropertyBeanCollection.Count; i++)
                {
                    if (blankFilter && alignmentResultBean.AlignmentPropertyBeanCollection[i].IsBlankFiltered) continue;

                    ResultExportLcUtility.WriteDataMatrixMetaData(sw, alignmentResultBean.AlignmentPropertyBeanCollection[i],
                        alignmentResultBean.AlignmentPropertyBeanCollection, mspDB, txtDB, fs, seekpointList, mainWindow.AnalysisParamForLC);

                    for (int j = 0; j < alignmentResultBean.AlignmentPropertyBeanCollection[i].AlignedPeakPropertyBeanCollection.Count; j++)
                    {
                        if (j == alignmentResultBean.AlignmentPropertyBeanCollection[i].AlignedPeakPropertyBeanCollection.Count - 1)
                            sw.WriteLine(deconvolutedPeakAreaArrayList[j][i]);
                        else
                            sw.Write(deconvolutedPeakAreaArrayList[j][i] + "\t");
                    }
                }
            }

            fs.Dispose();
            fs.Close();
        }

        private static List<double[]> getDeconvolutedPeakAreaArrayList(MainWindow mainWindow, AlignmentResultBean alignmentResultBean)
        {
            int alignmentIdLength = alignmentResultBean.AlignmentPropertyBeanCollection.Count;
            int sampleNumber = alignmentResultBean.AlignmentPropertyBeanCollection[0].AlignedPeakPropertyBeanCollection.Count;

            List<double[]> deconvolutedPeakAreaArrayList = new List<double[]>();
            double[] deconvolutedPeakAreaArray;

            AnalysisFileBean analysisFileBean;
            List<long> seekpointList;
            MS2DecResult deconvolutionResultBean;


            for (int i = 0; i < sampleNumber; i++)
            {
                deconvolutedPeakAreaArray = new double[alignmentIdLength];
                analysisFileBean = mainWindow.AnalysisFiles[i];

                DataStorageLcUtility.SetPeakAreaBeanCollection(analysisFileBean, analysisFileBean.AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

                using (FileStream deconvolutionFS = File.Open(analysisFileBean.AnalysisFilePropertyBean.DeconvolutionFilePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    seekpointList = new List<long>();
                    seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(deconvolutionFS);

                    for (int j = 0; j < alignmentIdLength; j++)
                    {
                        if (alignmentResultBean.AlignmentPropertyBeanCollection[j].AlignedPeakPropertyBeanCollection[i].PeakID < 0)
                        {
                            deconvolutedPeakAreaArray[j] = -1;
                        }
                        else
                        {
                            deconvolutionResultBean = SpectralDeconvolution.ReadMS2DecResult(deconvolutionFS, seekpointList, alignmentResultBean.AlignmentPropertyBeanCollection[j].AlignedPeakPropertyBeanCollection[i].PeakID);
                            deconvolutedPeakAreaArray[j] = deconvolutionResultBean.Ms2DecPeakArea;
                        }
                    }
                }

                deconvolutedPeakAreaArrayList.Add(deconvolutedPeakAreaArray);
                DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(analysisFileBean);

            }

            return deconvolutedPeakAreaArrayList;
        }

        private static void exportSampleAxisDeconvolution(MainWindow mainWindow, string sampleAxisDeconvoExportFolderPath, AlignmentResultBean alignmentResultBean, float massTolerance)
        {
            int focusedFileID = mainWindow.FocusedFileID;
            int focusedPeakID = mainWindow.FocusedPeakID;

            DataSummaryBean dataSummaryBean = mainWindow.AnalysisFiles[focusedFileID].DataSummaryBean;

            mainWindow.PeakViewDataAccessRefresh();

            DirectoryInfo dl = Directory.CreateDirectory(sampleAxisDeconvoExportFolderPath);

            string spectraFilePath;
            for (int i = 0; i < alignmentResultBean.AlignmentPropertyBeanCollection.Count; i++)
            {
                if (alignmentResultBean.AlignmentPropertyBeanCollection[i].MetaboliteName == string.Empty) continue;

                spectraFilePath = sampleAxisDeconvoExportFolderPath + "\\" + "alignmentID_" + alignmentResultBean.AlignmentPropertyBeanCollection[i].AlignmentID + ".txt";

                List<double[]> massSpectraList = getMatchedMsMsSpectraInformation(mainWindow, alignmentResultBean.AlignmentPropertyBeanCollection[i], massTolerance, dataSummaryBean);

                using (StreamWriter sw = new StreamWriter(spectraFilePath, false, Encoding.ASCII))
                {
                    sw.Write("m/z vs Sample information\t");
                    for (int j = 0; j < alignmentResultBean.AlignmentPropertyBeanCollection[i].AlignedPeakPropertyBeanCollection.Count; j++)
                    {
                        if (j == alignmentResultBean.AlignmentPropertyBeanCollection[i].AlignedPeakPropertyBeanCollection.Count - 1) sw.WriteLine(alignmentResultBean.AlignmentPropertyBeanCollection[i].AlignedPeakPropertyBeanCollection[j].FileName);
                        else sw.Write(alignmentResultBean.AlignmentPropertyBeanCollection[i].AlignedPeakPropertyBeanCollection[j].FileName + "\t");
                    }

                    for (int j = 0; j < massSpectraList.Count; j++)
                    {
                        double sum = 0;
                        for (int k = 1; k < massSpectraList[j].Length; k++)
                            sum += massSpectraList[j][k];
                        if (sum / (massSpectraList[j].Length - 1) < 10) continue;

                        for (int k = 0; k < massSpectraList[j].Length; k++)
                        {
                            if (k == massSpectraList[j].Length - 1) sw.WriteLine(massSpectraList[j][k]);
                            else sw.Write(massSpectraList[j][k] + "\t");
                        }
                    }
                }
            }

            mainWindow.PeakViewerForLcRefresh(focusedFileID);
            ((PairwisePlotPeakViewUI)mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.SelectedPlotId = focusedFileID;
        }

        private static List<double[]> getMatchedMsMsSpectraInformation(MainWindow mainWindow, AlignmentPropertyBean alignmentPropertyBean, float massTolerance, DataSummaryBean dataSummaryBean)
        {
            List<double[]> massSpectraList = new List<double[]>();
            MS2DecResult deconvolutionResultBean;
            List<long> seekpointList = new List<long>();
            List<double[]> massSpectra = new List<double[]>();

            //Initialize
            float focusedMass = dataSummaryBean.MinMass;
            double[] massSpectraInformation = new double[alignmentPropertyBean.AlignedPeakPropertyBeanCollection.Count + 1];
            while (focusedMass <= dataSummaryBean.MaxMass)
            {
                massSpectraInformation = new double[alignmentPropertyBean.AlignedPeakPropertyBeanCollection.Count + 1];
                massSpectraInformation[0] = focusedMass;
                massSpectra.Add(massSpectraInformation);

                focusedMass += massTolerance;
            }

            double[] maxIntensityArray = new double[alignmentPropertyBean.AlignedPeakPropertyBeanCollection.Count];
            double sum = 0, maxIntensity = double.MinValue;
            int counter = 0;
            for (int i = 0; i < alignmentPropertyBean.AlignedPeakPropertyBeanCollection.Count; i++)
            {
                if (alignmentPropertyBean.AlignedPeakPropertyBeanCollection[i].PeakID == -1 || alignmentPropertyBean.AlignedPeakPropertyBeanCollection[i].PeakID == -2)
                {

                }
                else
                {
                    using (FileStream fs = File.Open(mainWindow.AnalysisFiles[i].AnalysisFilePropertyBean.DeconvolutionFilePath, FileMode.Open, FileAccess.ReadWrite))
                    {
                        seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
                        deconvolutionResultBean = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, alignmentPropertyBean.AlignedPeakPropertyBeanCollection[i].PeakID);
                        massSpectraList = deconvolutionResultBean.MassSpectra.OrderBy(n => n[0]).ToList();

                        focusedMass = dataSummaryBean.MinMass;
                        counter = 0;
                        maxIntensity = double.MinValue;
                        while (focusedMass <= dataSummaryBean.MaxMass)
                        {
                            sum = 0;
                            for (int j = 0; j < deconvolutionResultBean.MassSpectra.Count; j++)
                            {
                                if (massSpectraList[j][0] < focusedMass) continue;
                                else if (focusedMass - massTolerance * 0.5 <= massSpectraList[j][0] && massSpectraList[j][0] <= focusedMass + massTolerance * 0.5)
                                {
                                    sum += massSpectraList[j][1];
                                }
                                else break;

                            }
                            massSpectra[counter][i + 1] = sum;
                            focusedMass += massTolerance;
                            counter++;

                            if (sum > maxIntensity) maxIntensity = sum;
                        }

                        maxIntensityArray[i] = maxIntensity;
                    }
                }
            }

            for (int i = 0; i < maxIntensityArray.Length; i++)
            {
                if (maxIntensityArray[i] <= 0) continue;
                for (int j = 0; j < massSpectra.Count; j++)
                {
                    massSpectra[j][i + 1] = massSpectra[j][i + 1] / maxIntensityArray[i] * 1000;
                }
            }

            return massSpectra;
        }

        private static void exportSpectra(MainWindow mainWindow, string exportFolderPath, int alignmentFileID,
            AlignmentResultBean alignmentResultBean, ExportSpectraFileFormat exportSpectraFileFormat, int targetIsotopeTrackFileID, bool blankFilter)
        {
            if (exportSpectraFileFormat == ExportSpectraFileFormat.mgf)
                representativeSpectraMgfExport(mainWindow, exportFolderPath, alignmentFileID, alignmentResultBean, targetIsotopeTrackFileID, blankFilter);
            else if (exportSpectraFileFormat == ExportSpectraFileFormat.msp)
                representativeSpectraMspExport(mainWindow, exportFolderPath, alignmentFileID, alignmentResultBean, targetIsotopeTrackFileID, blankFilter);
            else if (exportSpectraFileFormat == ExportSpectraFileFormat.txt)
                representativeSpectraTxtExport(mainWindow, exportFolderPath, alignmentFileID, alignmentResultBean, targetIsotopeTrackFileID, blankFilter);
        }

        private static void representativeSpectraTxtExport(MainWindow mainWindow, string exportFolderPath,
            int alignmentFileID, AlignmentResultBean alignmentResultBean, int targetIsotopeTrackFileID, bool blankFilter)
        {
            DateTime dt = DateTime.Now;
            string spectraFilePath = exportFolderPath + "\\" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + "_spectra_" + alignmentFileID + "." + ExportSpectraFileFormat.txt;

            using (StreamWriter sw = new StreamWriter(spectraFilePath, false, Encoding.ASCII))
            {
                var fs = File.Open(mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite);
                var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

                var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
                if (targetIsotopeTrackFileID >= 0)
                    alignedSpots = ResultExportLcUtility.GetFilteredAlignedSpotsByIsotpeTrackingResult(alignedSpots, mainWindow.AnalysisParamForLC, targetIsotopeTrackFileID);

                sw.Write("Name\tRT[min]\tPrecursor\tMetaboliteName\tAdductIon\tSpectraCount\tSpectra\tDotProduct\tReverseDotProduct\tAccurateMassSimilarity\tIsotopeSimilarity\tRtSimilarity\tTotalSimilarity\tExactMass\tRef.RT\tFormula\t");

                for (int i = 0; i < alignedSpots[0].AlignedPeakPropertyBeanCollection.Count; i++)
                {
                    if (i == alignedSpots[0].AlignedPeakPropertyBeanCollection.Count - 1) sw.WriteLine(alignedSpots[0].AlignedPeakPropertyBeanCollection[i].FileName);
                    else sw.Write(alignedSpots[0].AlignedPeakPropertyBeanCollection[i].FileName + "\t");
                }

                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                    ResultExportLcUtility.WriteDeconvolutedTxt(sw, fs, seekpointList, alignedSpots[i], mainWindow.MspDB);
                }

                fs.Dispose();
                fs.Close();
            }
        }

        private static void representativeSpectraMspExport(MainWindow mainWindow, string exportFolderPath,
            int alignmentFileID, AlignmentResultBean alignmentResultBean, int targetIsotopeTrackFileID, bool blankFilter)
        {
            DateTime dt = DateTime.Now;
            var spectraFilePath = exportFolderPath + "\\" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + "_spectra_" + alignmentFileID + "." + ExportSpectraFileFormat.msp;
            var param = mainWindow.AnalysisParamForLC;
            var project = mainWindow.ProjectProperty;

            using (StreamWriter sw = new StreamWriter(spectraFilePath, false, Encoding.ASCII))
            {
                using (var fs = File.Open(mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
                    var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
                    if (targetIsotopeTrackFileID >= 0)
                        alignedSpots = ResultExportLcUtility.GetFilteredAlignedSpotsByIsotpeTrackingResult(alignedSpots, mainWindow.AnalysisParamForLC, targetIsotopeTrackFileID);

                    foreach (var alignedSpot in alignedSpots)
                    {
                        if (blankFilter && alignedSpot.IsBlankFiltered) continue;
                        if (param.IsIonMobility)
                        {
                            foreach (var driftSpot in alignedSpot.AlignedDriftSpots)
                            {
                                if (blankFilter && driftSpot.IsBlankFiltered) continue;
                                ResultExportLcUtility.WriteDeconvolutedMsp(sw, fs, seekpointList, alignedSpot, driftSpot);
                            }
                        }
                        else
                        {
                            ResultExportLcUtility.WriteDeconvolutedMsp(sw, fs, seekpointList, alignedSpot);
                        }
                    }
                }
            }
        }

        private static void exportMolecularNetworkingEdges(string edgeFile, MainWindow mainWindow, string exportFolderPath,
            int alignmentFileID, AlignmentResultBean alignmentResultBean, bool blankFilter)
        {
            var dt = DateTime.Now;
            var mspfilepath = exportFolderPath + "\\" + "MolecularNetworking_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + "_" + alignmentFileID + "." + ExportSpectraFileFormat.msp;


            using (StreamWriter sw = new StreamWriter(mspfilepath, false, Encoding.ASCII))
            {
                var fs = File.Open(mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite);
                var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

                var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;

                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                    ResultExportLcUtility.WriteDeconvolutedMsp(sw, fs, seekpointList, alignedSpots[i]);
                }

                fs.Dispose();
                fs.Close();
            }

            var process = new Process();
            process.StartInfo.FileName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\MsdialMolecularNetworkingConsoleApp.exe";
            process.StartInfo.Arguments = "msms -i \"" + mspfilepath + "\" -o \"" + edgeFile + "\" -t " + mainWindow.AnalysisParamForLC.CentroidMs2Tolerance;
            process.Start();
        }


        private static void representativeSpectraMgfExport(MainWindow mainWindow, string exportFolderPath, int alignmentFileID,
            AlignmentResultBean alignmentResultBean, int targetIsotopeTrackFileID, bool blankFilter)
        {
            DateTime dt = DateTime.Now;
            string spectraFilePath = exportFolderPath + "\\" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + "_spectra_" + alignmentFileID + "." + ExportSpectraFileFormat.mgf;

            var param = mainWindow.AnalysisParamForLC;
            var project = mainWindow.ProjectProperty;

            using (StreamWriter sw = new StreamWriter(spectraFilePath, false, Encoding.ASCII))
            {
                using (var fs = File.Open(mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite))
                {

                    var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
                    var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
                    if (targetIsotopeTrackFileID >= 0)
                        alignedSpots = ResultExportLcUtility.GetFilteredAlignedSpotsByIsotpeTrackingResult(alignedSpots, mainWindow.AnalysisParamForLC, targetIsotopeTrackFileID);

                    foreach (var alignedSpot in alignedSpots)
                    {
                        if (blankFilter && alignedSpot.IsBlankFiltered) continue;
                        if (param.IsIonMobility)
                        {
                            foreach (var driftSpot in alignedSpot.AlignedDriftSpots)
                            {
                                if (blankFilter && driftSpot.IsBlankFiltered) continue;
                                ResultExportLcUtility.WriteDeconvolutedMgf(sw, fs, seekpointList, alignedSpot, driftSpot);
                            }
                        }
                        else
                        {
                            ResultExportLcUtility.WriteDeconvolutedMgf(sw, fs, seekpointList, alignedSpot);
                        }
                    }
                }
            }
        }

        private static void exportPeakIdMatrix(MainWindow mainWindow, int alignmentFileID, string peakIdMatrixExportFilePath, AlignmentResultBean alignmentResultBean,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> txtDB,
            ObservableCollection<AnalysisFileBean> analysisFiles, int targetIsotopeTrackFileID, bool blankFilter)
        {
            var fs = File.Open(mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite);
            var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

            using (StreamWriter sw = new StreamWriter(peakIdMatrixExportFilePath, false, Encoding.ASCII))
            {
                //Header
                ResultExportLcUtility.WriteDataMatrixHeader(sw, analysisFiles);

                var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
                if (targetIsotopeTrackFileID >= 0)
                    alignedSpots = ResultExportLcUtility.GetFilteredAlignedSpotsByIsotpeTrackingResult(alignedSpots, mainWindow.AnalysisParamForLC, targetIsotopeTrackFileID);

                //From the second
                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                    ResultExportLcUtility.WriteDataMatrixMetaData(sw, alignedSpots[i],
                        alignmentResultBean.AlignmentPropertyBeanCollection, mspDB, txtDB,
                        fs, seekpointList, mainWindow.AnalysisParamForLC);

                    for (int j = 0; j < alignedSpots[i].AlignedPeakPropertyBeanCollection.Count; j++)
                    {
                        if (j == alignedSpots[i].AlignedPeakPropertyBeanCollection.Count - 1)
                            sw.WriteLine(alignedSpots[i].AlignedPeakPropertyBeanCollection[j].PeakID);
                        else
                            sw.Write(alignedSpots[i].AlignedPeakPropertyBeanCollection[j].PeakID + "\t");
                    }
                }
            }

            fs.Dispose();
            fs.Close();
        }

        private static void exportRetentionTimeMatrix(MainWindow mainWindow, int alignmentFileID, string retentiontimeMatrixExportFilePath, AlignmentResultBean alignmentResultBean,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> txtDB,
            ObservableCollection<AnalysisFileBean> analysisFiles, int targetIsotopeTrackFileID, bool blankFilter)
        {
            var fs = File.Open(mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite);
            var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

            using (StreamWriter sw = new StreamWriter(retentiontimeMatrixExportFilePath, false, Encoding.ASCII))
            {
                //Header
                ResultExportLcUtility.WriteDataMatrixHeader(sw, analysisFiles);

                var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
                if (targetIsotopeTrackFileID >= 0)
                    alignedSpots = ResultExportLcUtility.GetFilteredAlignedSpotsByIsotpeTrackingResult(alignedSpots, mainWindow.AnalysisParamForLC, targetIsotopeTrackFileID);

                //From the second
                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                    ResultExportLcUtility.WriteDataMatrixMetaData(sw, alignedSpots[i], alignmentResultBean.AlignmentPropertyBeanCollection,
                        mspDB, txtDB, fs, seekpointList, mainWindow.AnalysisParamForLC);

                    for (int j = 0; j < alignedSpots[i].AlignedPeakPropertyBeanCollection.Count; j++)
                    {
                        if (j == alignedSpots[i].AlignedPeakPropertyBeanCollection.Count - 1)
                            sw.WriteLine(Math.Round(alignedSpots[i].AlignedPeakPropertyBeanCollection[j].RetentionTime, 3));
                        else
                            sw.Write(Math.Round(alignedSpots[i].AlignedPeakPropertyBeanCollection[j].RetentionTime, 3) + "\t");
                    }
                }
            }

            fs.Dispose();
            fs.Close();
        }

        private static void exportMassMatrix(MainWindow mainWindow, int alignmentFileID, string massMatrixExportFilePath, AlignmentResultBean alignmentResultBean,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> txtDB,
            ObservableCollection<AnalysisFileBean> analysisFiles, int targetIsotopeTrackFileID, bool blankFilter)
        {
            var fs = File.Open(mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite);
            var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

            using (StreamWriter sw = new StreamWriter(massMatrixExportFilePath, false, Encoding.ASCII))
            {
                //Header
                ResultExportLcUtility.WriteDataMatrixHeader(sw, analysisFiles);

                var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
                if (targetIsotopeTrackFileID >= 0)
                    alignedSpots = ResultExportLcUtility.GetFilteredAlignedSpotsByIsotpeTrackingResult(alignedSpots, mainWindow.AnalysisParamForLC, targetIsotopeTrackFileID);

                //From the second
                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                    ResultExportLcUtility.WriteDataMatrixMetaData(sw, alignedSpots[i], alignmentResultBean.AlignmentPropertyBeanCollection,
                        mspDB, txtDB, fs, seekpointList, mainWindow.AnalysisParamForLC);

                    for (int j = 0; j < alignedSpots[i].AlignedPeakPropertyBeanCollection.Count; j++)
                    {
                        if (j == alignedSpots[i].AlignedPeakPropertyBeanCollection.Count - 1)
                            sw.WriteLine(Math.Round(alignedSpots[i].AlignedPeakPropertyBeanCollection[j].AccurateMass, 3));
                        else
                            sw.Write(Math.Round(alignedSpots[i].AlignedPeakPropertyBeanCollection[j].AccurateMass, 3) + "\t");
                    }
                }
            }

            fs.Dispose();
            fs.Close();
        }

        private static void exportMsmsIncludedMatrix(MainWindow mainWindow, int alignmentFileID, string msmsIncludedMatrixExportFilePath, AlignmentResultBean alignmentResultBean,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> txtDB,
            ObservableCollection<AnalysisFileBean> analysisFiles, int targetIsotopeTrackFileID, bool blankFilter)
        {
            var fs = File.Open(mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite);
            var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

            using (StreamWriter sw = new StreamWriter(msmsIncludedMatrixExportFilePath, false, Encoding.ASCII))
            {
                //Header
                ResultExportLcUtility.WriteDataMatrixHeader(sw, analysisFiles);

                var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
                if (targetIsotopeTrackFileID >= 0)
                    alignedSpots = ResultExportLcUtility.GetFilteredAlignedSpotsByIsotpeTrackingResult(alignedSpots, mainWindow.AnalysisParamForLC, targetIsotopeTrackFileID);

                //From the second
                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                    ResultExportLcUtility.WriteDataMatrixMetaData(sw, alignedSpots[i], alignmentResultBean.AlignmentPropertyBeanCollection,
                        mspDB, txtDB, fs, seekpointList, mainWindow.AnalysisParamForLC);

                    for (int j = 0; j < alignedSpots[i].AlignedPeakPropertyBeanCollection.Count; j++)
                    {
                        if (j == alignedSpots[i].AlignedPeakPropertyBeanCollection.Count - 1)
                            sw.WriteLine(alignedSpots[i].AlignedPeakPropertyBeanCollection[j].Ms2ScanNumber);
                        else
                            sw.Write(alignedSpots[i].AlignedPeakPropertyBeanCollection[j].Ms2ScanNumber + "\t");
                    }
                }
            }

            fs.Dispose();
            fs.Close();
        }

        private static void exportSnMatrix(MainWindow mainWindow, int alignmentFileID, string msmsIncludedMatrixExportFilePath, AlignmentResultBean alignmentResultBean,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> txtDB,
            ObservableCollection<AnalysisFileBean> analysisFiles, int targetIsotopeTrackFileID, bool blankFilter)
        {
            var fs = File.Open(mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite);
            var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

            using (StreamWriter sw = new StreamWriter(msmsIncludedMatrixExportFilePath, false, Encoding.ASCII))
            {
                //Header
                ResultExportLcUtility.WriteDataMatrixHeader(sw, analysisFiles);

                var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
                if (targetIsotopeTrackFileID >= 0)
                    alignedSpots = ResultExportLcUtility.GetFilteredAlignedSpotsByIsotpeTrackingResult(alignedSpots, mainWindow.AnalysisParamForLC, targetIsotopeTrackFileID);

                //From the second
                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                    ResultExportLcUtility.WriteDataMatrixMetaData(sw, alignedSpots[i], alignmentResultBean.AlignmentPropertyBeanCollection,
                        mspDB, txtDB, fs, seekpointList, mainWindow.AnalysisParamForLC);

                    for (int j = 0; j < alignedSpots[i].AlignedPeakPropertyBeanCollection.Count; j++)
                    {
                        if (j == alignedSpots[i].AlignedPeakPropertyBeanCollection.Count - 1)
                            sw.WriteLine(alignedSpots[i].AlignedPeakPropertyBeanCollection[j].SignalToNoise);
                        else
                            sw.Write(alignedSpots[i].AlignedPeakPropertyBeanCollection[j].SignalToNoise + "\t");
                    }
                }
            }

            fs.Dispose();
            fs.Close();
        }

        private static void exportNormalizedDataMatrix(MainWindow mainWindow, int alignmentFileID, string normalizedMatrixExportFilePath, AlignmentResultBean alignmentResultBean,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> txtDB,
            ObservableCollection<AnalysisFileBean> analysisFiles, int targetIsotopeTrackFileID, bool blankFilter, bool replaceZeroToHalf)
        {
            var fs = File.Open(mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite);
            var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

            using (StreamWriter sw = new StreamWriter(normalizedMatrixExportFilePath, false, Encoding.ASCII))
            {
                //Header
                ResultExportLcUtility.WriteDataMatrixHeader(sw, analysisFiles);

                var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
                if (targetIsotopeTrackFileID >= 0)
                    alignedSpots = ResultExportLcUtility.GetFilteredAlignedSpotsByIsotpeTrackingResult(alignedSpots, mainWindow.AnalysisParamForLC, targetIsotopeTrackFileID);

                //From the second
                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;

                    ResultExportLcUtility.WriteDataMatrixMetaData(sw, alignedSpots[i], alignmentResultBean.AlignmentPropertyBeanCollection,
                        mspDB, txtDB, fs, seekpointList, mainWindow.AnalysisParamForLC, true);

                    // Replace true zero values with 1/2 of minimum peak height over all samples
                    var nonZeroMin = double.MaxValue;
                    if (replaceZeroToHalf)
                    {
                        foreach (var peak in alignedSpots[i].AlignedPeakPropertyBeanCollection)
                        {
                            if (peak.NormalizedVariable > 0 && nonZeroMin > peak.NormalizedVariable)
                                nonZeroMin = peak.NormalizedVariable;
                        }

                        if (nonZeroMin == double.MaxValue)
                            nonZeroMin = 1;
                    }

                    for (int j = 0; j < alignedSpots[i].AlignedPeakPropertyBeanCollection.Count; j++)
                    {
                        var value = alignedSpots[i].AlignedPeakPropertyBeanCollection[j].NormalizedVariable;
                        if (replaceZeroToHalf && value == 0)
                        {
                            value = (float)(nonZeroMin * 0.1);
                        }

                        if (j == alignedSpots[i].AlignedPeakPropertyBeanCollection.Count - 1)
                            sw.WriteLine(value);
                        else
                            sw.Write(value + "\t");
                    }
                }
            }

            fs.Dispose();
            fs.Close();
        }

        private static void MatrixExport(MainWindow mainWindow, int alignmentFileID,
            string outputfile, AlignmentResultBean alignmentResultBean,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> txtDB,
            ObservableCollection<AnalysisFileBean> analysisFiles, string exportType,
            int targetIsotopeTrackFileID, bool blankFilter, bool replaceZeroToHalf)
        {

            var dclfilepath = mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath;
            using (var fs = new FileStream(dclfilepath, FileMode.Open, FileAccess.ReadWrite))
            {
                var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
                using (var sw = new StreamWriter(outputfile, false, Encoding.ASCII))
                {

                    var isNormalized = exportType == "Normalized" ? true : false;
                    //Header
                    ResultExportLcUtility.WriteDataMatrixHeader(sw, analysisFiles);
                    var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
                    if (targetIsotopeTrackFileID >= 0)
                        alignedSpots = ResultExportLcUtility.GetFilteredAlignedSpotsByIsotpeTrackingResult(alignedSpots, mainWindow.AnalysisParamForLC, targetIsotopeTrackFileID);

                    //From the second
                    for (int i = 0; i < alignedSpots.Count; i++)
                    {
                        if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                        ResultExportLcUtility.WriteDataMatrixMetaData(sw, alignedSpots[i], alignmentResultBean.AlignmentPropertyBeanCollection,
                            mspDB, txtDB, fs, seekpointList, mainWindow.AnalysisParamForLC, isNormalized);

                        // Replace true zero values with 1/10 of minimum peak height over all samples
                        var nonZeroMin = getInterpolatedValueForMissingValue(alignedSpots[i].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                        ResultExportLcUtility.WriteData(sw, alignedSpots[i].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin);
                    }
                }
            }
        }

        private static void MatrixExportWithAveStd(MainWindow mainWindow, int alignmentFileID,
            string outputfile, AlignmentResultBean alignmentResultBean,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> txtDB,
            ObservableCollection<AnalysisFileBean> analysisFiles, string exportType,
            int targetIsotopeTrackFileID, bool blankFilter, bool replaceZeroToHalf)
        {

            var projectProp = mainWindow.ProjectProperty;
            var dclfilepath = mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath;
            if (exportType != "Height" && exportType != "Normalized" && exportType != "Area")
            {
                MatrixExport(mainWindow, alignmentFileID, outputfile, alignmentResultBean, mspDB, txtDB, analysisFiles, exportType, targetIsotopeTrackFileID, blankFilter, replaceZeroToHalf);
                return;
            }

            var isNormalized = exportType == "Normalized" ? true : false;
            var mode = isNormalized ? BarChartDisplayMode.NormalizedHeight
                                    : exportType == "Height" ? BarChartDisplayMode.OriginalHeight
                                                             : BarChartDisplayMode.OriginalArea;

            var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
            if (targetIsotopeTrackFileID >= 0)
                alignedSpots = ResultExportLcUtility.GetFilteredAlignedSpotsByIsotpeTrackingResult(alignedSpots, mainWindow.AnalysisParamForLC, targetIsotopeTrackFileID);
            if (alignedSpots == null || alignedSpots.Count == 0) return;

            var tempClassArray = MsDialStatistics.AverageStdevProperties(alignedSpots[0], analysisFiles, projectProp, mode, false);
            if (tempClassArray.Count == analysisFiles.Count)
            { // meaining no class properties are set.
                MatrixExport(mainWindow, alignmentFileID, outputfile, alignmentResultBean, mspDB, txtDB, analysisFiles, exportType, targetIsotopeTrackFileID, blankFilter, replaceZeroToHalf);
                return;
            }

            using (var fs = new FileStream(dclfilepath, FileMode.Open, FileAccess.ReadWrite))
            {
                var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
                using (var sw = new StreamWriter(outputfile, false, Encoding.ASCII))
                {

                    if (alignedSpots != null && alignedSpots.Count > 0)
                    {

                        //Header
                        ResultExportLcUtility.WriteDataMatrixHeader(sw, analysisFiles, tempClassArray);
                        //From the second
                        for (int i = 0; i < alignedSpots.Count; i++)
                        {
                            if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                            ResultExportLcUtility.WriteDataMatrixMetaData(sw, alignedSpots[i], alignmentResultBean.AlignmentPropertyBeanCollection,
                                mspDB, txtDB, fs, seekpointList, mainWindow.AnalysisParamForLC, isNormalized);

                            // Replace true zero values with 1/10 of minimum peak height over all samples
                            var nonZeroMin = getInterpolatedValueForMissingValue(alignedSpots[i].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                            var statsList = MsDialStatistics.AverageStdevProperties(alignedSpots[i], analysisFiles, projectProp, mode, false);
                            ResultExportLcUtility.WriteData(sw, alignedSpots[i].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin, statsList);
                        }
                    }
                }
            }
        }



        private static void exportRawDataMatrix(MainWindow mainWindow, int alignmentFileID,
            string rawdataMatrixExportFilePath, AlignmentResultBean alignmentResultBean,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> txtDB,
            ObservableCollection<AnalysisFileBean> analysisFiles,
            int targetIsotopeTrackFileID, bool blankFilter, bool replaceZeroToHalf)
        {
            var fs = File.Open(mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite);
            var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

            using (StreamWriter sw = new StreamWriter(rawdataMatrixExportFilePath, false, Encoding.ASCII))
            {
                //Header
                ResultExportLcUtility.WriteDataMatrixHeader(sw, analysisFiles);

                var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
                if (targetIsotopeTrackFileID >= 0)
                    alignedSpots = ResultExportLcUtility.GetFilteredAlignedSpotsByIsotpeTrackingResult(alignedSpots, mainWindow.AnalysisParamForLC, targetIsotopeTrackFileID);

                //From the second
                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                    ResultExportLcUtility.WriteDataMatrixMetaData(sw, alignedSpots[i], alignmentResultBean.AlignmentPropertyBeanCollection,
                        mspDB, txtDB, fs, seekpointList, mainWindow.AnalysisParamForLC);

                    // Replace true zero values with 1/10 of minimum peak height over all samples
                    var nonZeroMin = double.MaxValue;
                    if (replaceZeroToHalf)
                    {
                        foreach (var peak in alignedSpots[i].AlignedPeakPropertyBeanCollection)
                        {
                            if (peak.Variable > 0.0001 && nonZeroMin > peak.Variable)
                                nonZeroMin = peak.Variable;
                        }

                        if (nonZeroMin == double.MaxValue)
                            nonZeroMin = 1;
                    }

                    for (int j = 0; j < alignedSpots[i].AlignedPeakPropertyBeanCollection.Count; j++)
                    {
                        var value = Math.Round(alignedSpots[i].AlignedPeakPropertyBeanCollection[j].Variable, 0);
                        if (replaceZeroToHalf && value <= 0.0001)
                        {
                            value = nonZeroMin * 0.1;
                        }

                        if (j == alignedSpots[i].AlignedPeakPropertyBeanCollection.Count - 1)
                            sw.WriteLine(value);
                        else
                            sw.Write(value + "\t");
                    }
                }
            }

            fs.Dispose();
            fs.Close();
        }

        private static void exportLipidomicsRawDataMatrix(MainWindow mainWindow, int alignmentFileID,
            string filepath, AlignmentResultBean alignmentResultBean,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> txtDB,
            ObservableCollection<AnalysisFileBean> analysisFiles,
            int targetIsotopeTrackFileID, bool blankFilter, bool replaceZeroToHalf)
        {

            var fs = File.Open(mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite);
            var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
            var param = mainWindow.AnalysisParamForLC;
            var project = mainWindow.ProjectProperty;
            var filename = System.IO.Path.GetFileNameWithoutExtension(filepath);
            var directory = System.IO.Path.GetDirectoryName(filepath);
            var filepath_all = directory + "\\" + filename + "-all.txt";
            var filepath_named = directory + "\\" + filename + "-named.txt";
            var filepath_quant = directory + "\\" + filename + "-quant.txt";
            var mspfile = directory + "\\" + filename + ".msp";

            var lipidlist = new List<string[]>();
            if (project.IonMode == IonMode.Positive)
            {
                lipidlist = new List<string[]>() {
                    new string[] { "ACar", "[M]+" },
                    new string[] { "ADGGA", "[M+NH4]+" },
                    new string[] { "AHexCAS", "[M+NH4]+" },
                    new string[] { "AHexCS", "[M+NH4]+" },
                    new string[] { "AHexSIS", "[M+NH4]+" },
                    new string[] { "BMP", "[M+NH4]+" },
                    new string[] { "BRSE", "[M+NH4]+" },
                    new string[] { "CASE", "[M+NH4]+" },
                    new string[] { "BRSPE", "[M+H]+" },
                    new string[] { "CASPE", "[M+H]+" },
                    new string[] { "SISPE", "[M+H]+" },
                    new string[] { "CE", "[M+NH4]+" },
                    new string[] { "CerP", "[M+H]+" },
                    new string[] { "Cholesterol", "[M-H2O+H]+" },
                    new string[] { "CL", "[M+NH4]+" },
                    new string[] { "CoQ", "[M+H]+" },
                    new string[] { "DAG", "[M+NH4]+" },
                    new string[] { "DCAE", "[M+NH4]+" },
                    new string[] { "DGCC", "[M+H]+" },
                    new string[] { "DGTS", "[M+H]+" },
                    new string[] { "DGTA", "[M+H]+" },
                    new string[] { "EtherDAG", "[M+NH4]+" },
                    new string[] { "EtherLPC", "[M+H]+" },
                    new string[] { "EtherLPE", "[M+H]+" },
                    new string[] { "EtherPE", "[M+H]+" },
                    new string[] { "EtherTAG", "[M+NH4]+" },
                    new string[] { "GDCAE", "[M+NH4]+" },
                    new string[] { "GLCAE", "[M+NH4]+" },
                    new string[] { "GM3", "[M+NH4]+" },
                    new string[] { "HBMP", "[M+NH4]+" },
                    new string[] { "Hex2Cer", "[M+H]+" },
                    new string[] { "Hex3Cer", "[M+H]+" },
                    new string[] { "LDGCC", "[M+H]+" },
                    new string[] { "LDGTS", "[M+H]+" },
                    new string[] { "LDGTA", "[M+H]+" },
                    new string[] { "LPC", "[M+H]+" },
                    new string[] { "LPE", "[M+H]+" },
                    new string[] { "MAG", "[M+NH4]+" },
                    new string[] { "NAAG", "[M+H]+" },
                    new string[] { "NAAGS", "[M+H]+" },
                    new string[] { "NAAO", "[M+H]+" },
                    new string[] { "NAE", "[M+H]+" },
                    new string[] { "Phytosphingosine", "[M+H]+" },
                    new string[] { "SHex", "[M+NH4]+" },
                    new string[] { "SHexCer", "[M+H]+" },
                    new string[] { "SISE", "[M+NH4]+" },
                    new string[] { "SL", "[M+H]+" },
                    new string[] { "Sphinganine", "[M+H]+" },
                    new string[] { "Sphingosine", "[M+H]+" },
                    new string[] { "SQDG", "[M+NH4]+" },
                    new string[] { "TAG", "[M+NH4]+" },
                    new string[] { "TDCAE", "[M+NH4]+" },
                    new string[] { "TLCAE", "[M+NH4]+" },
                    new string[] { "VAE", "[M+Na]+" },
                    new string[] { "Vitamine", "[M+H]+" },
                };
            }
            else
            {
                lipidlist = new List<string[]>() {
                    new string[] { "AHexCer", "[M+CH3COO]-" },
                    new string[] { "ASM", "[M+CH3COO]-" },
                    new string[] { "BASulfate", "[M-H]-" },
                    new string[] { "BileAcid", "[M-H]-" },
                    new string[] { "BRSPE", "[M-H]-" },
                    new string[] { "CASPE", "[M-H]-" },
                    new string[] { "SISPE", "[M-H]-" },
                    new string[] { "Cer-ADS", "[M+CH3COO]-" },
                    new string[] { "Cer-AP", "[M+CH3COO]-" },
                    new string[] { "Cer-AS", "[M+CH3COO]-" },
                    new string[] { "Cer-BDS", "[M+CH3COO]-" },
                    new string[] { "Cer-BS", "[M+CH3COO]-" },
                    new string[] { "Cer-EBDS", "[M+CH3COO]-" },
                    new string[] { "Cer-EOS", "[M+CH3COO]-" },
                    new string[] { "Cer-EODS", "[M+CH3COO]-" },
                    new string[] { "Cer-HDS", "[M+CH3COO]-" },
                    new string[] { "Cer-HS", "[M+CH3COO]-" },
                    new string[] { "Cer-NDS", "[M+CH3COO]-" },
                    new string[] { "Cer-NP", "[M+CH3COO]-" },
                    new string[] { "Cer-NS", "[M+CH3COO]-" },
                    new string[] { "CL", "[M-H]-" },
                    new string[] { "DGDG", "[M+CH3COO]-" },
                    new string[] { "DGGA", "[M-H]-" },
                    new string[] { "DLCL", "[M-H]-" },
                    new string[] { "EtherLPG", "[M-H]-" },
                    new string[] { "EtherMGDG", "[M+CH3COO]-" },
                    new string[] { "EtherDGDG", "[M+CH3COO]-" },
                    new string[] { "EtherOxPE", "[M-H]-" },
                    new string[] { "EtherPC", "[M+CH3COO]-" },
                    new string[] { "EtherPE", "[M-H]-" },
                    new string[] { "EtherPG", "[M-H]-" },
                    new string[] { "EtherPI", "[M-H]-" },
                    new string[] { "EtherPS", "[M-H]-" },
                    new string[] { "FA", "[M-H]-" },
                    new string[] { "FAHFA", "[M-H]-" },
                    new string[] { "GM3", "[M-H]-" },
                    new string[] { "HBMP", "[M-H]-" },
                    new string[] { "HexCer-AP", "[M+CH3COO]-" },
                    new string[] { "HexCer-EOS", "[M+CH3COO]-" },
                    new string[] { "HexCer-HDS", "[M+CH3COO]-" },
                    new string[] { "HexCer-HS", "[M+CH3COO]-" },
                    new string[] { "HexCer-NDS", "[M+CH3COO]-" },
                    new string[] { "HexCer-NS", "[M+CH3COO]-" },
                    new string[] { "LCL", "[M-H]-" },
                    new string[] { "LNAPE", "[M-H]-" },
                    new string[] { "LNAPS", "[M-H]-" },
                    new string[] { "LPA", "[M-H]-" },
                    new string[] { "LPG", "[M-H]-" },
                    new string[] { "LPI", "[M-H]-" },
                    new string[] { "LPS", "[M-H]-" },
                    new string[] { "MGDG", "[M+CH3COO]-" },
                    new string[] { "OxPC", "[M+CH3COO]-" },
                    new string[] { "OxPE", "[M-H]-" },
                    new string[] { "OxPG", "[M-H]-" },
                    new string[] { "OxPI", "[M-H]-" },
                    new string[] { "OxPS", "[M-H]-" },
                    new string[] { "PA", "[M-H]-" },
                    new string[] { "PC", "[M+CH3COO]-" },
                    new string[] { "PE", "[M-H]-" },
                    new string[] { "PE-Cer", "[M-H]-" },
                    new string[] { "PEtOH", "[M-H]-" },
                    new string[] { "PG", "[M-H]-" },
                    new string[] { "PI", "[M-H]-" },
                    new string[] { "PI-Cer", "[M-H]-" },
                    new string[] { "PMeOH", "[M-H]-" },
                    new string[] { "PS", "[M-H]-" },
                    new string[] { "PT", "[M-H]-" },
                    new string[] { "SHexCer", "[M-H]-" },
                    new string[] { "SM", "[M-H]-" },
                    new string[] { "SQDG", "[M-H]-" },
                    new string[] { "SSulfate", "[M-H]-" },
                    new string[] { "Vitamine", "[M-H]-" },
                };
            }


            var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection.ToList();
            using (StreamWriter sw = new StreamWriter(filepath_all, false, Encoding.ASCII))
            {
                //Header
                ResultExportLcUtility.WriteDataMatrixHeader(sw, analysisFiles);
                //From the second
                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                    ResultExportLcUtility.WriteLipidomicsDataMatrixMetaData(sw,
                        alignedSpots[i], alignedSpots,
                        mspDB, txtDB, fs, seekpointList, param);

                    // Replace true zero values with 1/10 of minimum peak height over all samples
                    var nonZeroMin = double.MaxValue;
                    if (replaceZeroToHalf)
                    {
                        foreach (var peak in alignedSpots[i].AlignedPeakPropertyBeanCollection)
                        {
                            if (peak.Variable > 0.0001 && nonZeroMin > peak.Variable)
                                nonZeroMin = peak.Variable;
                        }

                        if (nonZeroMin == double.MaxValue)
                            nonZeroMin = 1;
                    }

                    for (int j = 0; j < alignedSpots[i].AlignedPeakPropertyBeanCollection.Count; j++)
                    {
                        var value = Math.Round(alignedSpots[i].AlignedPeakPropertyBeanCollection[j].Variable, 0);
                        if (replaceZeroToHalf && value <= 0.0001)
                        {
                            value = nonZeroMin * 0.1;
                        }

                        if (j == alignedSpots[i].AlignedPeakPropertyBeanCollection.Count - 1)
                            sw.WriteLine(value);
                        else
                            sw.Write(value + "\t");
                    }
                }
            }

            alignedSpots = alignedSpots.OrderBy(n => n.MetaboliteName).ToList();

            using (StreamWriter sw = new StreamWriter(filepath_named, false, Encoding.ASCII))
            {
                //Header
                ResultExportLcUtility.WriteDataMatrixHeader(sw, analysisFiles);
                //From the second
                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                    if (alignedSpots[i].MetaboliteName.Contains("w/o MS2:")) continue;
                    if (alignedSpots[i].MetaboliteName.Contains("Unsettled:")) continue;
                    if (alignedSpots[i].MetaboliteName.Contains("Unknown")) continue;
                    if (alignedSpots[i].MetaboliteName == string.Empty) continue;

                    ResultExportLcUtility.WriteLipidomicsDataMatrixMetaData(sw,
                        alignedSpots[i], alignedSpots,
                        mspDB, txtDB, fs, seekpointList, param);

                    // Replace true zero values with 1/10 of minimum peak height over all samples
                    var nonZeroMin = double.MaxValue;
                    if (replaceZeroToHalf)
                    {
                        foreach (var peak in alignedSpots[i].AlignedPeakPropertyBeanCollection)
                        {
                            if (peak.Variable > 0.0001 && nonZeroMin > peak.Variable)
                                nonZeroMin = peak.Variable;
                        }

                        if (nonZeroMin == double.MaxValue)
                            nonZeroMin = 1;
                    }

                    for (int j = 0; j < alignedSpots[i].AlignedPeakPropertyBeanCollection.Count; j++)
                    {
                        var value = Math.Round(alignedSpots[i].AlignedPeakPropertyBeanCollection[j].Variable, 0);
                        if (replaceZeroToHalf && value <= 0.0001)
                        {
                            value = nonZeroMin * 0.1;
                        }

                        if (j == alignedSpots[i].AlignedPeakPropertyBeanCollection.Count - 1)
                            sw.WriteLine(value);
                        else
                            sw.Write(value + "\t");
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter(filepath_quant, false, Encoding.ASCII))
            {
                //Header
                ResultExportLcUtility.WriteDataMatrixHeader(sw, analysisFiles);
                //From the second
                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                    if (alignedSpots[i].MetaboliteName.Contains("w/o MS2:")) continue;
                    if (alignedSpots[i].MetaboliteName.Contains("Unsettled:")) continue;
                    if (alignedSpots[i].MetaboliteName.Contains("Unknown")) continue;
                    if (alignedSpots[i].MetaboliteName == string.Empty) continue;

                    var comment = alignedSpots[i].Comment;
                    var isQuantified = checkifQuantifiedList(alignedSpots[i].MetaboliteName, alignedSpots[i].AdductIonName, comment, project.IonMode, lipidlist);
                    if (isQuantified == false) continue;

                    ResultExportLcUtility.WriteLipidomicsDataMatrixMetaData(sw,
                        alignedSpots[i], alignedSpots,
                        mspDB, txtDB, fs, seekpointList, param);

                    // Replace true zero values with 1/10 of minimum peak height over all samples
                    var nonZeroMin = double.MaxValue;
                    if (replaceZeroToHalf)
                    {
                        foreach (var peak in alignedSpots[i].AlignedPeakPropertyBeanCollection)
                        {
                            if (peak.Variable > 0.0001 && nonZeroMin > peak.Variable)
                                nonZeroMin = peak.Variable;
                        }

                        if (nonZeroMin == double.MaxValue)
                            nonZeroMin = 1;
                    }

                    for (int j = 0; j < alignedSpots[i].AlignedPeakPropertyBeanCollection.Count; j++)
                    {
                        var value = Math.Round(alignedSpots[i].AlignedPeakPropertyBeanCollection[j].Variable, 0);
                        if (replaceZeroToHalf && value <= 0.0001)
                        {
                            value = nonZeroMin * 0.1;
                        }

                        if (j == alignedSpots[i].AlignedPeakPropertyBeanCollection.Count - 1)
                            sw.WriteLine(value);
                        else
                            sw.Write(value + "\t");
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter(mspfile, false, Encoding.ASCII))
            {
                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (!alignedSpots[i].MsmsIncluded) continue;
                    ResultExportLcUtility.WriteMs2DecDataAsMsp(sw, alignedSpots[i], mspDB, txtDB,
                        fs, seekpointList, param);
                }
            }

            fs.Dispose();
            fs.Close();
        }

        private static bool checkifQuantifiedList(string lipidname, string adduct, string comment,
            IonMode ionMode, List<string[]> lipidlist)
        {
            if (comment.ToLower().Contains("is (y") || comment.ToLower().Contains("is(y"))
                return true;
            var lipidclass = lipidname.Split(' ')[0];
            if (lipidname.Contains("e;") || lipidname.Contains("p;"))
                lipidclass = "Ether" + lipidclass;
            if (ionMode == IonMode.Negative && lipidclass == "PG" && comment.Contains("BMP")) return false;
            if (ionMode == IonMode.Positive && lipidclass == "EtherPE" && !comment.Contains("p;")) return false;
            if (ionMode == IonMode.Negative && lipidclass == "EtherPE" && comment.Contains("p;")) return false;

            foreach (var query in lipidlist)
            {
                var qLipid = query[0];
                var qAdduct = query[1];
                if (qLipid == lipidclass && qAdduct == adduct) return true;
            }
            return false;
        }

        public static void ExportMsdialIsotopeGroupingResult_PrivateMethod(MainWindow mainWindow, int alignmentFileID,
           string rawdataMatrixExportFilePath, AlignmentResultBean alignmentResultBean,
           List<MspFormatCompoundInformationBean> mspDB, ObservableCollection<AnalysisFileBean> analysisFiles, int targetIsotopeTrackFileID)
        {
            var fs = File.Open(mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite);
            var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

            using (StreamWriter sw = new StreamWriter(rawdataMatrixExportFilePath, false, Encoding.ASCII))
            {
                //Header
                sw.WriteLine("Alignment ID\tm/z\tRT\tCarbon count");
                var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
                if (targetIsotopeTrackFileID >= 0)
                    alignedSpots = ResultExportLcUtility.GetFilteredAlignedSpotsByIsotpeTrackingResult(alignedSpots, mainWindow.AnalysisParamForLC, targetIsotopeTrackFileID);

                //From the second
                var lastParentId = alignedSpots[0].IsotopeTrackingParentID;
                var lastSpot = alignedSpots[0];
                var alignmentID = alignedSpots[0].AlignmentID;
                var mz = alignedSpots[0].CentralAccurateMass;
                var rt = alignedSpots[0].CentralRetentionTime;

                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (alignedSpots[i].IsotopeTrackingParentID == lastParentId)
                    {
                        lastSpot = alignedSpots[i];
                    }
                    else
                    {
                        sw.WriteLine(alignmentID + "\t" + mz + "\t" + rt + "\t" + lastSpot.IsotopeTrackingWeightNumber);

                        lastParentId = alignedSpots[i].IsotopeTrackingParentID;
                        lastSpot = alignedSpots[i];
                        alignmentID = alignedSpots[i].AlignmentID;
                        mz = alignedSpots[i].CentralAccurateMass;
                        rt = alignedSpots[i].CentralRetentionTime;
                    }
                }
                sw.WriteLine(alignmentID + "\t" + mz + "\t" + rt + "\t" + lastSpot.IsotopeTrackingWeightNumber);
            }

            fs.Dispose();
            fs.Close();
        }

        private static void exportPeakAreaMatrix(MainWindow mainWindow, int alignmentFileID, string file, AlignmentResultBean alignmentResultBean,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> txtDB,
            ObservableCollection<AnalysisFileBean> analysisFiles, int targetIsotopeTrackFileID, bool blankFilter, bool replaceZeroToHalf)
        {
            var fs = File.Open(mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite);
            var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

            using (StreamWriter sw = new StreamWriter(file, false, Encoding.ASCII))
            {
                //Header
                ResultExportLcUtility.WriteDataMatrixHeader(sw, analysisFiles);

                var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
                if (targetIsotopeTrackFileID >= 0)
                    alignedSpots = ResultExportLcUtility.GetFilteredAlignedSpotsByIsotpeTrackingResult(alignedSpots, mainWindow.AnalysisParamForLC, targetIsotopeTrackFileID);

                //From the second
                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;

                    ResultExportLcUtility.WriteDataMatrixMetaData(sw, alignedSpots[i], alignmentResultBean.AlignmentPropertyBeanCollection,
                        mspDB, txtDB, fs, seekpointList, mainWindow.AnalysisParamForLC);

                    // Replace true zero values with 1/2 of minimum peak height over all samples
                    var nonZeroMin = double.MaxValue;
                    if (replaceZeroToHalf)
                    {
                        foreach (var peak in alignedSpots[i].AlignedPeakPropertyBeanCollection)
                        {
                            if (peak.Area > 0.0001 && nonZeroMin > peak.Area)
                                nonZeroMin = peak.Area;
                        }

                        if (nonZeroMin == double.MaxValue)
                            nonZeroMin = 1;
                    }

                    for (int j = 0; j < alignedSpots[i].AlignedPeakPropertyBeanCollection.Count; j++)
                    {
                        var value = Math.Round(alignedSpots[i].AlignedPeakPropertyBeanCollection[j].Area, 0);
                        if (replaceZeroToHalf && value <= 0.0001)
                        {
                            value = nonZeroMin * 0.1;
                        }

                        if (j == alignedSpots[i].AlignedPeakPropertyBeanCollection.Count - 1)
                            sw.WriteLine(value);
                        else
                            sw.Write(value + "\t");
                    }
                }
            }

            fs.Dispose();
            fs.Close();
        }


        private static void exportParameter(string paramFile, AlignmentResultBean alignmentResultBean, ObservableCollection<AnalysisFileBean> analysisFiles, ProjectPropertyBean project)
        {
            var param = alignmentResultBean.AnalysisParamForLC;
            if (param == null)
            {
                MessageBox.Show("Parameter is not saved at MS-DIAL version 2.40 or former.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ParameterExport(analysisFiles, param, project, paramFile);
        }

        /// <summary>
        /// This is the function to export a target peak spot as Mrmprobs reference format.
        /// The format will be copied to clipboard.
        /// The parameters are based on the current setting.
        /// /// </summary>
        public static void CopyToClipboardMrmprobsRef(MS2DecResult ms2DecResult, PeakAreaBean peakSpot, List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param, ProjectPropertyBean projectProp)
        {
            if (param.MpMs1Tolerance == 0)
            {
                param.MpMs1Tolerance = 0.005F;
                param.MpMs2Tolerance = 0.01F;
                param.MpRtTolerance = 0.5F;
                param.MpTopN = 5;
                param.MpIsIncludeMsLevel1 = true;
                param.MpIsUseMs1LevelForQuant = false;
                param.MpIsFocusedSpotOutput = true;
                param.MpIsReferenceBaseOutput = true;
                param.MpIsExportOtherCandidates = false;
                param.MpIdentificationScoreCutOff = 80;
            }

            var mpIsReferenceMsms = param.MpIsReferenceBaseOutput;
            var mpRtTol = (float)Math.Round(param.MpRtTolerance, 4);
            var mpMs1Tol = (float)Math.Round(param.MpMs1Tolerance, 6);
            var mpMs2Tol = (float)Math.Round(param.MpMs2Tolerance, 6);
            var mpTopN = param.MpTopN;
            var mpIsIncludeMsLevel1 = param.MpIsIncludeMsLevel1;
            var mpIsUseMslevel1ForQuant = param.MpIsUseMs1LevelForQuant;
            var mpIsExportOtherCandidates = param.MpIsExportOtherCandidates;
            if (param.MpIdentificationScoreCutOff <= 0)
                param.MpIdentificationScoreCutOff = 80;
            var mpIdentificationScoreCutoff = param.MpIdentificationScoreCutOff;

            var rtBegin = Math.Max(Math.Round(peakSpot.RtAtPeakTop - mpRtTol, 2), 0);
            var rtEnd = Math.Round(peakSpot.RtAtPeakTop + mpRtTol, 2);
            var rt = Math.Round(peakSpot.RtAtPeakTop, 2);

            var clipboardText = string.Empty;
            if (mpIsReferenceMsms)
            {

                if (peakSpot.LibraryID < 0 || mspDB.Count == 0 || peakSpot.MetaboliteName.Contains("w/o")) return;

                var mspID = peakSpot.LibraryID;
                var name = stringReplaceForWindowsAcceptableCharacters(mspDB[mspID].Name + "_" + peakSpot.PeakID);
                var precursorMz = Math.Round(mspDB[mspID].PrecursorMz, 5);

                copyMrmprobsRefByReferenceMsms(ref clipboardText, name, precursorMz, rt, rtBegin, rtEnd, mpMs1Tol, mpMs2Tol, mpTopN, mpIsIncludeMsLevel1, mpIsUseMslevel1ForQuant, mspDB[mspID]);

                if (mpIsExportOtherCandidates)
                {

                    mspDB = mspDB.OrderBy(n => n.PrecursorMz).ToList();

                    var ms2Dec = ms2DecResult;
                    var spectrum = ms2Dec.MassSpectra;
                    if (spectrum != null && spectrum.Count > 0)
                        spectrum = spectrum.OrderBy(n => n[0]).ToList();

                    var otherCandidateMspIDs = SpectralSimilarity.GetHighSimilarityMspIDs(peakSpot.AccurateMass, param.Ms1LibrarySearchTolerance,
                        peakSpot.RtAtPeakTop, param.RetentionTimeLibrarySearchTolerance, param.Ms2LibrarySearchTolerance,
                        spectrum, mspDB, mpIdentificationScoreCutoff, projectProp.TargetOmics, param.IsUseRetentionInfoForIdentificationScoring);

                    mspDB = mspDB.OrderBy(n => n.Id).ToList();

                    foreach (var id in otherCandidateMspIDs)
                    {
                        if (id == peakSpot.LibraryID) continue;

                        mspID = id;

                        name = stringReplaceForWindowsAcceptableCharacters(mspDB[mspID].Name + "_" + peakSpot.PeakID);
                        precursorMz = Math.Round(mspDB[mspID].PrecursorMz, 5);
                        rtBegin = Math.Max(Math.Round(peakSpot.RtAtPeakTop - mpRtTol, 2), 0);
                        rtEnd = Math.Round(peakSpot.RtAtPeakTop + mpRtTol, 2);
                        rt = Math.Round(peakSpot.RtAtPeakTop, 2);

                        copyMrmprobsRefByReferenceMsms(ref clipboardText, name, precursorMz, rt, rtBegin, rtEnd, mpMs1Tol, mpMs2Tol,
                            mpTopN, mpIsIncludeMsLevel1, mpIsUseMslevel1ForQuant, mspDB[mspID]);
                    }
                }
            }
            else
            {

                var name = stringReplaceForWindowsAcceptableCharacters(peakSpot.MetaboliteName + "_" + peakSpot.PeakID);
                var precursorMz = Math.Round(peakSpot.AccurateMass, 5);

                copyMrmprobsRefByExperimentalMsms(name, precursorMz, rt, rtBegin, rtEnd, mpMs1Tol, mpMs2Tol, mpTopN, mpIsIncludeMsLevel1, mpIsUseMslevel1ForQuant, ms2DecResult);
            }
        }

        private static void copyMrmprobsRefByExperimentalMsms(string name, double precursorMz, double rt, double rtBegin, double rtEnd, float mpMs1Tol, float mpMs2Tol, int mpTopN, bool mpIsIncludeMsLevel1, bool mpIsUseMslevel1ForQuant, MS2DecResult ms2DecResult)
        {
            if ((mpIsIncludeMsLevel1 == false || mpIsUseMslevel1ForQuant == true) && ms2DecResult.MassSpectra.Count == 0) return;

            var massSpec = ms2DecResult.MassSpectra.OrderByDescending(n => n[1]).ToList();
            var baseIntensity = 0.0;
            if (mpIsUseMslevel1ForQuant) baseIntensity = ms2DecResult.Ms1PeakHeight;
            else baseIntensity = massSpec[0][1];

            var text = string.Empty;

            if (mpIsIncludeMsLevel1)
            {
                var tqRatio = Math.Round(ms2DecResult.Ms1PeakHeight / baseIntensity * 100, 0);
                if (mpIsUseMslevel1ForQuant) tqRatio = 100;
                if (tqRatio == 100 && !mpIsUseMslevel1ForQuant) tqRatio = 99; // 100 is used just once for the target (quantified) m/z trace. Otherwise, non-100 value should be used.

                text += name + "\t" + precursorMz + "\t" + precursorMz + "\t" + rt + "\t" + tqRatio + "\t" + rtBegin + "\t" + rtEnd + "\t" + mpMs1Tol + "\t" + mpMs2Tol + "\t" + 1 + "\t" + "NA" + "\r\n";
            }

            if (mpTopN == 1 && mpIsIncludeMsLevel1)
            {
                Clipboard.SetDataObject(text, true);
            }
            else
            {
                for (int i = 0; i < massSpec.Count; i++)
                {
                    if (i > mpTopN - 1) break;
                    var productMz = Math.Round(massSpec[i][0], 5);
                    var tqRatio = Math.Round(massSpec[i][1] / baseIntensity * 100, 0);

                    if (mpIsUseMslevel1ForQuant && i == 0) tqRatio = 99;
                    else if (!mpIsUseMslevel1ForQuant && i == 0) tqRatio = 100;
                    else if (i != 0 && tqRatio == 100) tqRatio = 99;  // 100 is used just once for the target (quantified) m/z trace. Otherwise, non-100 value should be used.

                    if (tqRatio == 0) tqRatio = 1;

                    text += name + "\t" + precursorMz + "\t" + productMz + "\t" + rt + "\t" + tqRatio + "\t" + rtBegin + "\t" + rtEnd + "\t" + mpMs1Tol + "\t" + mpMs2Tol + "\t" + 2 + "\t" + "NA" + "\r\n";
                }
                Clipboard.SetDataObject(text, true);
            }
        }

        private static void copyMrmprobsRefByReferenceMsms(ref string text, string name, double precursorMz, double rt, double rtBegin, double rtEnd, float mpMs1Tol, float mpMs2Tol,
            int mpTopN, bool mpIsIncludeMsLevel1, bool mpIsUseMslevel1ForQuant, MspFormatCompoundInformationBean mspQuery)
        {
            if ((mpIsIncludeMsLevel1 == false || mpIsUseMslevel1ForQuant == true) && mspQuery.MzIntensityCommentBeanList.Count == 0) return;

            var massSpec = mspQuery.MzIntensityCommentBeanList.OrderByDescending(n => n.Intensity).ToList();
            var compClass = mspQuery.CompoundClass;
            if (compClass == null || compClass == string.Empty) compClass = "NA";

            if (mpIsIncludeMsLevel1)
            {
                var tqRatio = 100;
                if (!mpIsUseMslevel1ForQuant) tqRatio = 150;
                // Since we cannot calculate the real QT ratio from the reference DB and the real MS1 value (actually I can calculate them from the raw data with the m/z matching),
                //currently the ad hoc value 150 is used.

                text += name + "\t" + precursorMz + "\t" + precursorMz + "\t" + rt + "\t" + tqRatio + "\t" + rtBegin + "\t" + rtEnd + "\t" + mpMs1Tol + "\t" + mpMs2Tol + "\t" + 1 + "\t" + compClass + "\r\n";
            }

            if ((mpTopN == 1 && mpIsIncludeMsLevel1) || mspQuery.MzIntensityCommentBeanList.Count == 1)
            {
                Clipboard.SetDataObject(text, true);
            }
            else
            {
                var basePeak = massSpec[0].Intensity;
                for (int i = 0; i < massSpec.Count; i++)
                {
                    if (i > mpTopN - 1) break;
                    var productMz = Math.Round(massSpec[i].Mz, 5);
                    var tqRatio = Math.Round(massSpec[i].Intensity / basePeak * 100, 0);

                    if (mpIsUseMslevel1ForQuant && i == 0) tqRatio = 99;
                    else if (!mpIsUseMslevel1ForQuant && i == 0) tqRatio = 100;
                    else if (i != 0 && tqRatio == 100) tqRatio = 99;  // 100 is used just once for the target (quantified) m/z trace. Otherwise, non-100 value should be used.

                    if (tqRatio == 0) tqRatio = 1;

                    text += name + "\t" + precursorMz + "\t" + productMz + "\t" + rt + "\t" + tqRatio + "\t" + rtBegin + "\t" + rtEnd + "\t" + mpMs1Tol + "\t" + mpMs2Tol + "\t" + 2 + "\t" + compClass + "\r\n";
                }
                Clipboard.SetDataObject(text, true);
            }
        }

        /// <summary>
        /// This is the function to export the MRMPROBS reference format from MS-DIAL. 
        /// Here, mspDB info is used for the export.
        /// Currently, 150 is used for the representation of precursor intensity
        /// </summary>
        public static void ExportReferenceMsmsAsMrmprobsFormat(string filepath, FileStream fs, List<long> seekpoints, PeakAreaBean peakSpot, List<MspFormatCompoundInformationBean> mspDB,
            AnalysisParametersBean param, ProjectPropertyBean projectProp)
        {
            var rtTolerance = param.MpRtTolerance;
            var ms1Tolerance = param.MpMs1Tolerance;
            var ms2Tolerance = param.MpMs2Tolerance;
            var topN = param.MpTopN;
            var isIncludeMslevel1 = param.MpIsIncludeMsLevel1;
            var isUseMs1LevelForQuant = param.MpIsUseMs1LevelForQuant;
            var isExportOtherCanidates = param.MpIsExportOtherCandidates;
            var identificationScoreCutoff = param.MpIdentificationScoreCutOff;

            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {

                writeHeaderAsMrmprobsReferenceFormat(sw);

                if (peakSpot.LibraryID < 0 || mspDB.Count == 0 || peakSpot.MetaboliteName.Contains("w/o")) return;

                var mspID = peakSpot.LibraryID;
                var name = stringReplaceForWindowsAcceptableCharacters(mspDB[mspID].Name + "_" + peakSpot.PeakID);
                var precursorMz = Math.Round(mspDB[mspID].PrecursorMz, 5);
                var rtBegin = Math.Max(Math.Round(peakSpot.RtAtPeakTop - rtTolerance, 2), 0);
                var rtEnd = Math.Round(peakSpot.RtAtPeakTop + rtTolerance, 2);
                var rt = Math.Round(peakSpot.RtAtPeakTop, 2);

                writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, mspDB[mspID]);

                if (isExportOtherCanidates)
                {

                    mspDB = mspDB.OrderBy(n => n.PrecursorMz).ToList();

                    var ms2Dec = SpectralDeconvolution.ReadMS2DecResult(fs, seekpoints, peakSpot.PeakID);
                    var spectrum = ms2Dec.MassSpectra;
                    if (spectrum != null && spectrum.Count > 0)
                        spectrum = spectrum.OrderBy(n => n[0]).ToList();

                    var otherCandidateMspIDs = SpectralSimilarity.GetHighSimilarityMspIDs(peakSpot.AccurateMass, param.Ms1LibrarySearchTolerance,
                        peakSpot.RtAtPeakTop, param.RetentionTimeLibrarySearchTolerance, param.Ms2LibrarySearchTolerance,
                        spectrum, mspDB, identificationScoreCutoff, projectProp.TargetOmics, param.IsUseRetentionInfoForIdentificationScoring);

                    mspDB = mspDB.OrderBy(n => n.Id).ToList();

                    foreach (var id in otherCandidateMspIDs)
                    {
                        if (id == peakSpot.LibraryID) continue;

                        mspID = id;

                        name = stringReplaceForWindowsAcceptableCharacters(mspDB[mspID].Name + "_" + peakSpot.PeakID);
                        precursorMz = Math.Round(mspDB[mspID].PrecursorMz, 5);
                        rtBegin = Math.Max(Math.Round(peakSpot.RtAtPeakTop - rtTolerance, 2), 0);
                        rtEnd = Math.Round(peakSpot.RtAtPeakTop + rtTolerance, 2);
                        rt = Math.Round(peakSpot.RtAtPeakTop, 2);

                        writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance,
                            ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, mspDB[mspID]);
                    }
                }
            }
        }

        /// <summary>
        /// This is the batch export function to provide the MRMPROBS reference format from MS-DIAL. 
        /// Here, mspDB info is used for the export.
        /// Currently, 150 is used for the representation of precursor intensity
        /// </summary>
        public static void ExportReferenceMsmsAsMrmprobsFormat(string filepath, FileStream fs, List<long> seekpoints, ObservableCollection<PeakAreaBean> peakSpots, List<MspFormatCompoundInformationBean> mspDB,
           AnalysisParametersBean param, ProjectPropertyBean projectProp)
        {
            var rtTolerance = param.MpRtTolerance;
            var ms1Tolerance = param.MpMs1Tolerance;
            var ms2Tolerance = param.MpMs2Tolerance;
            var topN = param.MpTopN;
            var isIncludeMslevel1 = param.MpIsIncludeMsLevel1;
            var isUseMs1LevelForQuant = param.MpIsUseMs1LevelForQuant;
            var isExportOtherCanidates = param.MpIsExportOtherCandidates;
            var identificationScoreCutoff = param.MpIdentificationScoreCutOff;

            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {

                writeHeaderAsMrmprobsReferenceFormat(sw);

                foreach (var peak in peakSpots)
                {
                    if (peak.LibraryID < 0 || mspDB.Count == 0 || peak.MetaboliteName.Contains("w/o")) continue;

                    var mspID = peak.LibraryID;

                    var name = stringReplaceForWindowsAcceptableCharacters(mspDB[mspID].Name + "_" + peak.PeakID);
                    var precursorMz = Math.Round(mspDB[mspID].PrecursorMz, 5);
                    var rtBegin = Math.Max(Math.Round(peak.RtAtPeakTop - rtTolerance, 2), 0);
                    var rtEnd = Math.Round(peak.RtAtPeakTop + rtTolerance, 2);
                    var rt = Math.Round(peak.RtAtPeakTop, 2);

                    writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, mspDB[mspID]);

                    if (isExportOtherCanidates)
                    {

                        mspDB = mspDB.OrderBy(n => n.PrecursorMz).ToList();

                        var ms2Dec = SpectralDeconvolution.ReadMS2DecResult(fs, seekpoints, peak.PeakID);
                        var spectrum = ms2Dec.MassSpectra;
                        if (spectrum != null && spectrum.Count > 0)
                            spectrum = spectrum.OrderBy(n => n[0]).ToList();

                        var otherCandidateMspIDs = SpectralSimilarity.GetHighSimilarityMspIDs(peak.AccurateMass, param.Ms1LibrarySearchTolerance,
                            peak.RtAtPeakTop, param.RetentionTimeLibrarySearchTolerance, param.Ms2LibrarySearchTolerance,
                            spectrum, mspDB, identificationScoreCutoff, projectProp.TargetOmics, param.IsUseRetentionInfoForIdentificationScoring);

                        mspDB = mspDB.OrderBy(n => n.Id).ToList();

                        foreach (var id in otherCandidateMspIDs)
                        {
                            if (id == peak.LibraryID) continue;

                            mspID = id;

                            name = stringReplaceForWindowsAcceptableCharacters(mspDB[mspID].Name + "_" + peak.PeakID);
                            precursorMz = Math.Round(mspDB[mspID].PrecursorMz, 5);
                            rtBegin = Math.Max(Math.Round(peak.RtAtPeakTop - rtTolerance, 2), 0);
                            rtEnd = Math.Round(peak.RtAtPeakTop + rtTolerance, 2);
                            rt = Math.Round(peak.RtAtPeakTop, 2);

                            writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance,
                                ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, mspDB[mspID]);
                        }
                    }
                }
            }

        }

        private static void writeFieldsAsMrmprobsReferenceFormat(StreamWriter sw, string name, double precursorMz, double rt, double rtBegin, double rtEnd, double ms1Tolerrance,
            double ms2Tolerance, int topN, bool isIncludeMslevel1, bool isUseMs1LevelForQuant, MspFormatCompoundInformationBean mspQuery)
        {
            if ((isIncludeMslevel1 == false || isUseMs1LevelForQuant == true) && mspQuery.MzIntensityCommentBeanList.Count == 0) return;

            var massSpec = mspQuery.MzIntensityCommentBeanList.OrderByDescending(n => n.Intensity).ToList();
            var compClass = mspQuery.CompoundClass;
            if (compClass == null || compClass == string.Empty) compClass = "NA";

            if (isIncludeMslevel1)
            {
                var tqRatio = 100;
                if (!isUseMs1LevelForQuant) tqRatio = 150;
                // Since we cannot calculate the real QT ratio from the reference DB and the real MS1 value (actually I can calculate them from the raw data with the m/z matching),
                //currently the ad hoc value 150 is used.

                writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, precursorMz, rt, tqRatio, rtBegin, rtEnd, ms1Tolerrance, ms2Tolerance, 1, compClass);
            }

            if (topN == 1 && isIncludeMslevel1) return;
            if (mspQuery.MzIntensityCommentBeanList.Count == 0) return;
            var basePeak = massSpec[0].Intensity;
            for (int i = 0; i < massSpec.Count; i++)
            {
                if (i > topN - 1) break;
                var productMz = Math.Round(massSpec[i].Mz, 5);
                var tqRatio = Math.Round(massSpec[i].Intensity / basePeak * 100, 0);
                if (isUseMs1LevelForQuant && i == 0) tqRatio = 99;
                else if (!isUseMs1LevelForQuant && i == 0) tqRatio = 100;
                else if (i != 0 && tqRatio == 100) tqRatio = 99;  // 100 is used just once for the target (quantified) m/z trace. Otherwise, non-100 value should be used.

                if (tqRatio == 0) tqRatio = 1;

                writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, productMz, rt, tqRatio, rtBegin, rtEnd, ms1Tolerrance, ms2Tolerance, 2, compClass);
            }
        }

        /// <summary>
        /// This is the export function for the output of MRMRPROBS reference format for all spots.
        /// The reference is made from the information of exprerimental MS/MS instead of the reference MS/MS.
        /// </summary>
        public static void ExportExperimentalMsmsAsMrmprobsFormat(string filepath, ObservableCollection<PeakAreaBean> peakSpots, FileStream fs, List<long> seekpoints,
            double rtTolerance, double ms1Tolerance, double ms2Tolerance, int topN = 5, bool isIncludeMslevel1 = true, bool isUseMs1LevelForQuant = true)
        {
            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {

                writeHeaderAsMrmprobsReferenceFormat(sw);

                foreach (var peak in peakSpots)
                {
                    //if (peak.LibraryID < 0 && peak.PostIdentificationLibraryId < 0) continue;

                    var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpoints, peak.PeakID);

                    var name = stringReplaceForWindowsAcceptableCharacters(peak.MetaboliteName + "_" + peak.PeakID);
                    var precursorMz = Math.Round(peak.AccurateMass, 5);
                    var rtBegin = Math.Max(Math.Round(peak.RtAtPeakTop - rtTolerance, 2), 0);
                    var rtEnd = Math.Round(peak.RtAtPeakTop + rtTolerance, 2);
                    var rt = Math.Round(peak.RtAtPeakTop, 2);

                    writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd,
                        ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, ms2DecResult);
                }
            }
        }

        /// <summary>
        /// Export function for the output of MRMPROBS reference format.
        /// </summary>
        /// <param name="topN">default 5, and the number of MS/MS fragment ions is defined. Currently, the priority is based on the ions having higher abundances in MSMS.</param>
        public static void ExportExperimentalMsmsAsMrmprobsFormat(string filepath, MS2DecResult ms2DecResult, PeakAreaBean peakSpot,
            double rtTolerance, double ms1Tolerance, double ms2Tolerance, int topN = 5, bool isIncludeMslevel1 = true, bool isUseMs1LevelForQuant = true)
        {
            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {

                writeHeaderAsMrmprobsReferenceFormat(sw);

                var name = stringReplaceForWindowsAcceptableCharacters(peakSpot.MetaboliteName + "_" + peakSpot.PeakID);
                var precursorMz = Math.Round(peakSpot.AccurateMass, 5);
                var rtBegin = Math.Max(Math.Round(peakSpot.RtAtPeakTop - rtTolerance, 2), 0);
                var rtEnd = Math.Round(peakSpot.RtAtPeakTop + rtTolerance, 2);
                var rt = Math.Round(peakSpot.RtAtPeakTop, 2);

                writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd,
                    ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, ms2DecResult);
            }
        }

        private static void writeFieldsAsMrmprobsReferenceFormat(StreamWriter sw, string name, double precursorMz, double rt, double rtBegin, double rtEnd, double ms1Tolerrance, double ms2Tolerance, int topN, bool isIncludeMslevel1, bool isUseMs1LevelForQuant, MS2DecResult ms2DecResult)
        {
            if (isIncludeMslevel1 == false && ms2DecResult.MassSpectra.Count == 0) return;
            if (isIncludeMslevel1)
            {
                var tqRatio = 99;
                if (isUseMs1LevelForQuant) tqRatio = 100;
                if (tqRatio == 100 && !isUseMs1LevelForQuant) tqRatio = 99; // 100 is used just once for the target (quantified) m/z trace. Otherwise, non-100 value should be used.
                writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, precursorMz, rt, tqRatio, rtBegin, rtEnd, ms1Tolerrance, ms2Tolerance, 1, "NA");
            }

            if (topN == 1 && isIncludeMslevel1) return;
            if (ms2DecResult.MassSpectra == null || ms2DecResult.MassSpectra.Count == 0) return;

            var massSpec = ms2DecResult.MassSpectra.OrderByDescending(n => n[1]).ToList();
            var baseIntensity = 0.0;

            if (isUseMs1LevelForQuant) baseIntensity = ms2DecResult.Ms1PeakHeight;
            else baseIntensity = massSpec[0][1];

            for (int i = 0; i < massSpec.Count; i++)
            {
                if (i > topN - 1) break;
                var productMz = Math.Round(massSpec[i][0], 5);
                var tqRatio = Math.Round(massSpec[i][1] / baseIntensity * 100, 0);
                if (isUseMs1LevelForQuant && i == 0) tqRatio = 99;
                else if (!isUseMs1LevelForQuant && i == 0) tqRatio = 100;
                else if (i != 0 && tqRatio == 100) tqRatio = 99;  // 100 is used just once for the target (quantified) m/z trace. Otherwise, non-100 value should be used.

                if (tqRatio == 0) tqRatio = 1;

                writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, productMz, rt, tqRatio, rtBegin, rtEnd, ms1Tolerrance, ms2Tolerance, 2, "NA");
            }
        }

        private static void writeFieldsAsMrmprobsReferenceFormat(StreamWriter sw, string name, double precursorMz, double productMz, double rt, double tqRatio, double rtBegin, double rtEnd, double ms1Tolerance, double ms2Tolerance, int msLevel, string compoundClass)
        {
            sw.Write(name + "\t");
            sw.Write(precursorMz + "\t");
            sw.Write(productMz + "\t");
            sw.Write(rt + "\t");
            sw.Write(tqRatio + "\t");
            sw.Write(rtBegin + "\t");
            sw.Write(rtEnd + "\t");
            sw.Write(ms1Tolerance + "\t");
            sw.Write(ms2Tolerance + "\t");
            sw.Write(msLevel + "\t");
            sw.WriteLine(compoundClass);
        }

        private static void writeHeaderAsMrmprobsReferenceFormat(StreamWriter sw)
        {
            sw.WriteLine("Compound name\tPrecursor mz\tProduct mz\tRT min\tTQ Ratio\tRT begin\tRT end\tMS1 tolerance\tMS2 tolerance\tMS level\tClass");
        }

        private static string stringReplaceForWindowsAcceptableCharacters(string name)
        {
            var chars = Path.GetInvalidFileNameChars();
            return new string(name.Select(c => chars.Contains(c) ? '_' : c).ToArray());
        }

        public static void ExportReferenceMsmsAsMrmprobsFormat(string filepath, FileStream fs, List<long> seekpoints, AlignmentPropertyBean alignedSpot,
            List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param, ProjectPropertyBean projectProp)
        {
            var rtTolerance = param.MpRtTolerance;
            var ms1Tolerance = param.MpMs1Tolerance;
            var ms2Tolerance = param.MpMs2Tolerance;
            var topN = param.MpTopN;
            var isIncludeMslevel1 = param.MpIsIncludeMsLevel1;
            var isUseMs1LevelForQuant = param.MpIsUseMs1LevelForQuant;
            var isExportOtherCanidates = param.MpIsExportOtherCandidates;
            var identificationScoreCutoff = param.MpIdentificationScoreCutOff;

            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {

                writeHeaderAsMrmprobsReferenceFormat(sw);

                if (alignedSpot.LibraryID < 0 || mspDB.Count == 0 || alignedSpot.MetaboliteName.Contains("w/o")) return;

                var mspID = alignedSpot.LibraryID;
                var name = stringReplaceForWindowsAcceptableCharacters(mspDB[mspID].Name + "_" + alignedSpot.AlignmentID);
                var precursorMz = Math.Round(mspDB[mspID].PrecursorMz, 5);
                var rtBegin = Math.Max(Math.Round(alignedSpot.CentralRetentionTime - rtTolerance, 2), 0);
                var rtEnd = Math.Round(alignedSpot.CentralRetentionTime + rtTolerance, 2);
                var rt = Math.Round(alignedSpot.CentralRetentionTime, 2);

                writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, mspDB[mspID]);

                if (isExportOtherCanidates)
                {

                    mspDB = mspDB.OrderBy(n => n.PrecursorMz).ToList();

                    var ms2Dec = SpectralDeconvolution.ReadMS2DecResult(fs, seekpoints, alignedSpot.AlignmentID);
                    var spectrum = ms2Dec.MassSpectra;
                    if (spectrum != null && spectrum.Count > 0)
                        spectrum = spectrum.OrderBy(n => n[0]).ToList();

                    var otherCandidateMspIDs = SpectralSimilarity.GetHighSimilarityMspIDs(alignedSpot.CentralAccurateMass, param.Ms1LibrarySearchTolerance,
                        alignedSpot.CentralRetentionTime, param.RetentionTimeLibrarySearchTolerance, param.Ms2LibrarySearchTolerance,
                        spectrum, mspDB, identificationScoreCutoff, projectProp.TargetOmics, param.IsUseRetentionInfoForIdentificationScoring);

                    mspDB = mspDB.OrderBy(n => n.Id).ToList();

                    foreach (var id in otherCandidateMspIDs)
                    {
                        if (id == alignedSpot.LibraryID) continue;

                        mspID = id;

                        name = stringReplaceForWindowsAcceptableCharacters(mspDB[mspID].Name + "_" + alignedSpot.AlignmentID);
                        precursorMz = Math.Round(mspDB[mspID].PrecursorMz, 5);
                        rtBegin = Math.Max(Math.Round(alignedSpot.CentralRetentionTime - rtTolerance, 2), 0);
                        rtEnd = Math.Round(alignedSpot.CentralRetentionTime + rtTolerance, 2);
                        rt = Math.Round(alignedSpot.CentralRetentionTime, 2);

                        writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd,
                            ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, mspDB[mspID]);

                    }
                }
            }
        }

        public static void ExportReferenceMsmsAsMrmprobsFormat(string filepath, FileStream fs, List<long> seekpoints, ObservableCollection<AlignmentPropertyBean> alignedSpots,
            List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param, ProjectPropertyBean projectProp)
        {
            var rtTolerance = param.MpRtTolerance;
            var ms1Tolerance = param.MpMs1Tolerance;
            var ms2Tolerance = param.MpMs2Tolerance;
            var topN = param.MpTopN;
            var isIncludeMslevel1 = param.MpIsIncludeMsLevel1;
            var isUseMs1LevelForQuant = param.MpIsUseMs1LevelForQuant;
            var isExportOtherCanidates = param.MpIsExportOtherCandidates;
            var identificationScoreCutoff = param.MpIdentificationScoreCutOff;

            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {

                writeHeaderAsMrmprobsReferenceFormat(sw);

                foreach (var spot in alignedSpots)
                {
                    if (spot.LibraryID < 0 || mspDB.Count == 0 || spot.MetaboliteName.Contains("w/o")) continue;
                    if (spot.Comment != null && spot.Comment != string.Empty && spot.Comment.ToLower().Contains("unk")) continue;

                    //internal
                    // if (spot.Comment == null || spot.Comment == string.Empty) continue;

                    var mspID = spot.LibraryID;

                    var name = stringReplaceForWindowsAcceptableCharacters(mspDB[mspID].Name + "_" + spot.AlignmentID);
                    var precursorMz = Math.Round(mspDB[mspID].PrecursorMz, 5);
                    var rtBegin = Math.Max(Math.Round(spot.CentralRetentionTime - rtTolerance, 2), 0);
                    var rtEnd = Math.Round(spot.CentralRetentionTime + rtTolerance, 2);
                    var rt = Math.Round(spot.CentralRetentionTime, 2);

                    writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd,
                        ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, mspDB[mspID]);

                    if (isExportOtherCanidates)
                    {

                        mspDB = mspDB.OrderBy(n => n.PrecursorMz).ToList();

                        var ms2Dec = SpectralDeconvolution.ReadMS2DecResult(fs, seekpoints, spot.AlignmentID);
                        var spectrum = ms2Dec.MassSpectra;
                        if (spectrum != null && spectrum.Count > 0)
                            spectrum = spectrum.OrderBy(n => n[0]).ToList();

                        var otherCandidateMspIDs = SpectralSimilarity.GetHighSimilarityMspIDs(spot.CentralAccurateMass, param.Ms1LibrarySearchTolerance,
                            spot.CentralRetentionTime, param.RetentionTimeLibrarySearchTolerance, param.Ms2LibrarySearchTolerance,
                            spectrum, mspDB, identificationScoreCutoff, projectProp.TargetOmics, param.IsUseRetentionInfoForIdentificationScoring);

                        mspDB = mspDB.OrderBy(n => n.Id).ToList();

                        foreach (var id in otherCandidateMspIDs)
                        {
                            if (id == spot.LibraryID) continue;

                            mspID = id;

                            name = stringReplaceForWindowsAcceptableCharacters(mspDB[mspID].Name + "_" + spot.AlignmentID);
                            precursorMz = Math.Round(mspDB[mspID].PrecursorMz, 5);
                            rtBegin = Math.Max(Math.Round(spot.CentralRetentionTime - rtTolerance, 2), 0);
                            rtEnd = Math.Round(spot.CentralRetentionTime + rtTolerance, 2);
                            rt = Math.Round(spot.CentralRetentionTime, 2);

                            writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd,
                                ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, mspDB[mspID]);

                        }
                    }
                }
            }
        }


        public static void ExportExperimentalMsmsAsMrmprobsFormat(string filepath, MS2DecResult ms2DecResult, AlignmentPropertyBean spotProp,
            double rtTolerance, double ms1Tolerance, double ms2Tolerance, int topN = 5, bool isIncludeMslevel1 = true, bool isUseMs1LevelForQuant = true)
        {
            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {

                writeHeaderAsMrmprobsReferenceFormat(sw);

                var name = stringReplaceForWindowsAcceptableCharacters(spotProp.MetaboliteName + "_" + spotProp.AlignmentID);
                var precursorMz = Math.Round(spotProp.CentralAccurateMass, 5);
                var rtBegin = Math.Max(Math.Round(spotProp.CentralRetentionTime - rtTolerance, 2), 0);
                var rtEnd = Math.Round(spotProp.CentralRetentionTime + rtTolerance, 2);
                var rt = Math.Round(spotProp.CentralRetentionTime, 2);

                writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd,
                    ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, ms2DecResult);
            }
        }

        public static void ExportExperimentalMsmsAsMrmprobsFormat(string filepath, ObservableCollection<AlignmentPropertyBean> alignmentSpots,
            FileStream fs, List<long> seekpoints, double rtTolerance, double ms1Tolerance, double ms2Tolerance, int topN = 5, bool isIncludeMslevel1 = true, bool isUseMs1LevelForQuant = true)
        {
            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {

                writeHeaderAsMrmprobsReferenceFormat(sw);

                foreach (var spot in alignmentSpots)
                {
                    //if (peak.LibraryID < 0 || peak.PostIdentificationLibraryID < 0) continue;

                    var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpoints, spot.AlignmentID);

                    var name = stringReplaceForWindowsAcceptableCharacters(spot.MetaboliteName + "_" + spot.AlignmentID);
                    var precursorMz = Math.Round(spot.CentralAccurateMass, 5);
                    var rtBegin = Math.Max(Math.Round(spot.CentralRetentionTime - rtTolerance, 2), 0);
                    var rtEnd = Math.Round(spot.CentralRetentionTime + rtTolerance, 2);
                    var rt = Math.Round(spot.CentralRetentionTime, 2);

                    writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd,
                        ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, ms2DecResult);
                }
            }
        }

        public static void CopyToClipboardMrmprobsRef(MS2DecResult ms2DecResult, AlignmentPropertyBean alignmentSpot, List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param, ProjectPropertyBean projectProp)
        {
            if (param.MpMs1Tolerance == 0)
            {
                param.MpMs1Tolerance = 0.005F;
                param.MpMs2Tolerance = 0.01F;
                param.MpRtTolerance = 0.5F;
                param.MpTopN = 5;
                param.MpIsIncludeMsLevel1 = true;
                param.MpIsUseMs1LevelForQuant = false;
                param.MpIsFocusedSpotOutput = true;
                param.MpIsReferenceBaseOutput = true;
                param.MpIsExportOtherCandidates = false;
                param.MpIdentificationScoreCutOff = 80;
            }

            var mpIsReferenceMsms = param.MpIsReferenceBaseOutput;
            var mpRtTol = (float)Math.Round(param.MpRtTolerance, 4);
            var mpMs1Tol = (float)Math.Round(param.MpMs1Tolerance, 6);
            var mpMs2Tol = (float)Math.Round(param.MpMs2Tolerance, 6);
            var mpTopN = param.MpTopN;
            var mpIsIncludeMsLevel1 = param.MpIsIncludeMsLevel1;
            var mpIsUseMslevel1ForQuant = param.MpIsUseMs1LevelForQuant;
            var mpIsExportOtherCandidates = param.MpIsExportOtherCandidates;
            if (param.MpIdentificationScoreCutOff <= 0)
                param.MpIdentificationScoreCutOff = 80;
            var mpIdentificationScoreCutoff = param.MpIdentificationScoreCutOff;

            var rtBegin = Math.Max(Math.Round(alignmentSpot.CentralRetentionTime - mpRtTol, 2), 0);
            var rtEnd = Math.Round(alignmentSpot.CentralRetentionTime + mpRtTol, 2);
            var rt = Math.Round(alignmentSpot.CentralRetentionTime, 2);
            var clipboardText = string.Empty;

            if (mpIsReferenceMsms)
            {

                if (alignmentSpot.LibraryID < 0 || mspDB.Count == 0 || alignmentSpot.MetaboliteName.Contains("w/o")) return;

                var mspID = alignmentSpot.LibraryID;
                var name = stringReplaceForWindowsAcceptableCharacters(mspDB[mspID].Name + "_" + alignmentSpot.AlignmentID);
                var precursorMz = Math.Round(mspDB[mspID].PrecursorMz, 5);

                copyMrmprobsRefByReferenceMsms(ref clipboardText, name, precursorMz, rt, rtBegin, rtEnd, mpMs1Tol, mpMs2Tol, mpTopN, mpIsIncludeMsLevel1, mpIsUseMslevel1ForQuant, mspDB[mspID]);

                if (mpIsExportOtherCandidates)
                {

                    mspDB = mspDB.OrderBy(n => n.PrecursorMz).ToList();

                    var ms2Dec = ms2DecResult;
                    var spectrum = ms2Dec.MassSpectra;
                    if (spectrum != null && spectrum.Count > 0)
                        spectrum = spectrum.OrderBy(n => n[0]).ToList();

                    var otherCandidateMspIDs = SpectralSimilarity.GetHighSimilarityMspIDs(alignmentSpot.CentralAccurateMass, param.Ms1LibrarySearchTolerance,
                        alignmentSpot.CentralRetentionTime, param.RetentionTimeLibrarySearchTolerance, param.Ms2LibrarySearchTolerance,
                        spectrum, mspDB, mpIdentificationScoreCutoff, projectProp.TargetOmics, param.IsUseRetentionInfoForIdentificationScoring);

                    mspDB = mspDB.OrderBy(n => n.Id).ToList();

                    foreach (var id in otherCandidateMspIDs)
                    {
                        if (id == alignmentSpot.LibraryID) continue;

                        mspID = id;

                        name = stringReplaceForWindowsAcceptableCharacters(mspDB[mspID].Name + "_" + alignmentSpot.AlignmentID);
                        precursorMz = Math.Round(mspDB[mspID].PrecursorMz, 5);
                        rtBegin = Math.Max(Math.Round(alignmentSpot.CentralRetentionTime - mpRtTol, 2), 0);
                        rtEnd = Math.Round(alignmentSpot.CentralRetentionTime + mpRtTol, 2);
                        rt = Math.Round(alignmentSpot.CentralRetentionTime, 2);

                        copyMrmprobsRefByReferenceMsms(ref clipboardText, name, precursorMz, rt, rtBegin, rtEnd, mpMs1Tol, mpMs2Tol,
                            mpTopN, mpIsIncludeMsLevel1, mpIsUseMslevel1ForQuant, mspDB[mspID]);
                    }
                }
            }
            else
            {

                var name = stringReplaceForWindowsAcceptableCharacters(alignmentSpot.MetaboliteName + "_" + alignmentSpot.AlignmentID);
                var precursorMz = Math.Round(alignmentSpot.CentralAccurateMass, 5);

                copyMrmprobsRefByExperimentalMsms(name, precursorMz, rt, rtBegin, rtEnd, mpMs1Tol, mpMs2Tol, mpTopN, mpIsIncludeMsLevel1, mpIsUseMslevel1ForQuant, ms2DecResult);
            }
        }


        private static void exportResultMztabFormet(MainWindow mainWindow, int alignmentFileID,
            string outputfile, AlignmentResultBean alignmentResultBean,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> txtDB,
            ObservableCollection<AnalysisFileBean> analysisFiles, string exportType,
            int targetIsotopeTrackFileID, bool blankFilter, bool replaceZeroToHalf)
        {
            var analysisParamForLC = mainWindow.AnalysisParamForLC;
            var fileCount = 1;
            if (exportType == "Normalized" && analysisParamForLC.IsNormalizeSplash)
            {
                fileCount = 2;
            }
            for (int splashQuant = 0; splashQuant < fileCount; splashQuant++)
            {
                var ionAbundanceUnit = new List<string>(alignmentResultBean.AlignmentPropertyBeanCollection.Select(n => n.IonAbundanceUnit.ToString())).Distinct();
                if (ionAbundanceUnit.Count() == 1)
                {
                    splashQuant = 1;
                }
                if (exportType == "Normalized" && analysisParamForLC.IsNormalizeSplash && splashQuant == 1)
                {
                    outputfile = outputfile.Replace(".mzTab", "-SPLASHquant.mzTab");
                }


                using (StreamWriter sw = new StreamWriter(outputfile, false, Encoding.ASCII))
                {
                    //Meta data section 
                    var mtdPrefix = "MTD";
                    var commentPrefix = "COM";
                    var mztabVersion = "2.0.0-M"; //Fixed

                    var mzTabExporterVerNo = "1.07";
                    var mzTabExporterName = "MS-DIAL mzTab exporter";
                    var mztabExporter = "[,, " + mzTabExporterName + ", " + mzTabExporterVerNo + "]";

                    var projectProp = mainWindow.ProjectProperty;
                    var dclfilepath = mainWindow.AlignmentFiles[alignmentFileID].SpectraFilePath;

                    var ionMobility = projectProp.SeparationType.ToString() == "IonMobility" ? true : false;
                    var ionMobilityType = analysisParamForLC.IonMobilityType.ToString().ToUpper();

                    var mztabId = Path.GetFileNameWithoutExtension(outputfile); //  using file name

                    var softwareVerNumber = analysisParamForLC.MsdialVersionNumber.Replace("MS-DIAL ver.", "");
                    var software = "[MS, MS:1003082, MS-DIAL, " + softwareVerNumber + "]";  //Fixed
                    var quantificationMethod = "[MS, MS:1002019, Label-free raw feature quantitation, ]";  // 

                    var cvList = new List<List<string>>(); // cv list
                    var cvItem1 = new List<string>() { "MS", "PSI-MS controlled vocabulary", "20-06-2018", "https://www.ebi.ac.uk/ols/ontologies/ms" };
                    cvList.Add(cvItem1);
                    var cvItem2 = new List<string>() { "UO", "Units of Measurement Ontology", "2017-09-25", "http://purl.obolibrary.org/obo/uo.owl" };
                    if (ionMobility == true && ionMobilityType != "TIMS")
                    {
                        cvList.Add(cvItem2);
                    }
                    else if (analysisParamForLC.IsNormalizeSplash && splashQuant == 1)
                    {
                        if (ionAbundanceUnit.Contains("pmol") || ionAbundanceUnit.Contains("fmol") || ionAbundanceUnit.Contains("pg") || ionAbundanceUnit.Contains("ng"))
                        {
                            cvList.Add(cvItem2);
                        }
                    }

                    //var msRunFormat = "[,, ABF(Analysis Base File) file, ]"; // need to consider
                    //var msRunIDFormat = "[,, ABF file Datapoint Number, ]"; // need to consider

                    //if (ionMobility == true)
                    //{
                    //    msRunFormat = "[,, IBF file, ]"; // need to consider
                    //    msRunIDFormat = "[,, IBF file Datapoint Number, ]"; // need to consider
                    //}
                    var msRunFormat = "";
                    var msRunIDFormat = "";
                    var analysisFilePath = analysisFiles[0].AnalysisFilePropertyBean.AnalysisFilePath;
                    var analysisFileExtention = Path.GetExtension(analysisFilePath).ToUpper();

                    switch (analysisFileExtention)
                    {
                        case (".ABF"):
                            msRunFormat = "[,, ABF(Analysis Base File) file, ]";
                            msRunIDFormat = "[,, ABF file Datapoint Number, ]";
                            break;
                        case (".IBF"):
                            msRunFormat = "[,, IBF file, ]";
                            msRunIDFormat = "[,, IBF file Datapoint Number, ]";
                            break;
                        case (".WIFF"):
                        case (".WIFF2"):
                            msRunFormat = "[MS, MS:1000562, ABI WIFF format, ]";
                            msRunIDFormat = "[MS, MS:1000770, WIFF nativeID format, ]";
                            break;
                        case (".D"):
                            msRunFormat = "[MS, MS:1001509, Agilent MassHunter format, ]";
                            msRunIDFormat = "[MS, MS:1001508, Agilent MassHunter nativeID format, ]";
                            break;
                        case (".RAW"):
                            var isDirectory = File.GetAttributes(analysisFilePath).HasFlag(FileAttributes.Directory);
                            if (isDirectory)
                            {
                                msRunFormat = "[MS, MS:1000526, Waters raw format, ]";
                                msRunIDFormat = "[MS, MS:1000769, Waters nativeID format, ]";
                            }
                            else
                            {
                                msRunFormat = "[MS, MS:1000563, Thermo RAW format, ]";
                                msRunIDFormat = "[MS, MS:1000768, Thermo nativeID format, ]";
                            }
                            break;
                        case (".CDF"):
                            msRunFormat = "[EDAM, format:3650, netCDF, ]";
                            msRunIDFormat = "[MS, MS:1000776, scan number only nativeID format, ]";
                            var cvItem3 = new List<string>() { "EDAM", "Bioscientific data analysis ontology", "20-06-2020", "http://edamontology.org/" };
                            cvList.Add(cvItem3);

                            break;
                        case (".MZML"):
                            msRunFormat = "[MS, MS:1000584, mzML format, ]";
                            msRunIDFormat = "[MS, MS:1000776, scan number only nativeID format, ]";
                            break;
                        case (".LRP"):
                            msRunFormat = "[,, LRP file, ]";
                            msRunIDFormat = "[,, LRP file Datapoint Number, ]";
                            break;
                    };

                    var database = new List<List<string>>();
                    var defaultDatabase = new List<string>();
                    var databaseItem = new List<string>();
                    var libraryFileExtension = Path.GetExtension(projectProp.LibraryFilePath);
                    var libraryFileName = Path.GetFileNameWithoutExtension(projectProp.LibraryFilePath);
                    if (libraryFileExtension == ".msp" || libraryFileExtension == ".MSP")
                    {
                        defaultDatabase = new List<string>() { "[,, User-defined MSP library file, ]", "MSP", "Unknown", "file://" + projectProp.LibraryFilePath.Replace("\\", "/").Replace(" ", "%20") }; // 
                    }
                    else if (libraryFileExtension == ".lbm" || libraryFileExtension == ".LBM" || libraryFileExtension == ".lbm2" || libraryFileExtension == ".LBM2")
                    {
                        var lbmVer = libraryFileName;
                        //var lbmVer = libraryFileName.Substring(libraryFileName.IndexOf("-") + 1, libraryFileName.LastIndexOf("-") - libraryFileName.IndexOf("-") - 1);
                        defaultDatabase = new List<string>() { "[,, MS-DIAL LipidsMsMs database, ]", "lbm", lbmVer, "file://" + projectProp.LibraryFilePath.Replace("\\", "/").Replace(" ", "%20") }; // 
                    }
                    else
                    {
                        defaultDatabase = new List<string>() { "[,, Unknown database, null ]", "null", "Unknown", "null" }; // no database
                    };
                    database.Add(defaultDatabase);

                    var unmatchedDBName = new List<string>() { "[,, no database, null ]", "null", "Unknown", "null" }; //  
                    database.Add(unmatchedDBName);

                    if (projectProp.PostIdentificationLibraryFilePath != "" && projectProp.PostIdentificationLibraryFilePath != null)
                    {
                        databaseItem = new List<string>() { "[,, User-defined rt-mz text library, ]", "USR", "Unknown", "file://" + projectProp.PostIdentificationLibraryFilePath.Replace("\\", "/").Replace(" ", "%20") }; // post identification setting file
                        database.Add(databaseItem);
                    }

                    var idConfidenceDefault = "[,, MS-DIAL algorithm matching score, ]";
                    var idConfidenceMeasure = new List<string>() {                              //  must be fixed order!!
                    idConfidenceDefault, "[,, Retention time similarity, ]", "[,, Dot product, ]", "[,, Reverse dot product, ]", "[,, Fragment presence (%), ]" };
                    if (ionMobility == true)
                    {
                        idConfidenceMeasure.Insert(3, "[,, CCS similarity, ]");
                    }
                    var idConfidenceManual = "";
                    // if manual curation is true
                    var manuallyAnnotation = new List<bool>(alignmentResultBean.AlignmentPropertyBeanCollection.Select(n => n.IsManuallyModifiedForAnnotation));
                    if (manuallyAnnotation.Contains(true))
                    {
                        idConfidenceManual = "[MS, MS:1001058, quality estimation by manual validation, ]";
                        //idConfidenceMeasure.Add(idConfidenceManual); //if using manual cration score 
                    }

                    var smallMoleculeIdentificationReliability = "[MS, MS:1003032, compound identification confidence code in MS-DIAL, ]"; // new define on psi-ms.obo

                    // write line
                    //sw.WriteLine("COM\tMeta data section");
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "mzTab-version", mztabVersion }));
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "mzTab-ID", mztabId }));
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "software[1]", software }));
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "software[2]", mztabExporter }));

                    var msRunLocation = new List<string>(analysisFiles.Select(n => n.AnalysisFilePropertyBean.AnalysisFilePath));
                    for (int i = 0; i < analysisFiles.Count; i++)
                    {
                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "ms_run[" + (i + 1) + "]-location", "file://" + msRunLocation[i].Replace("\\", "/").Replace(" ", "%20") })); // filePath
                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "ms_run[" + (i + 1) + "]-format", msRunFormat }));
                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "ms_run[" + (i + 1) + "]-id_format", msRunIDFormat }));

                        var ionMode = projectProp.IonMode.ToString();
                        if (ionMode == "Positive")
                        {
                            sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "ms_run[" + (i + 1) + "]-scan_polarity[1]", "[MS,MS:1000130,positive scan,]" }));
                        }
                        else if (ionMode == "Negative")
                        {
                            sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "ms_run[" + (i + 1) + "]-scan_polarity[1]", "[MS, MS:1000129, negative scan, ]" }));
                        }
                    };

                    var assay = new List<string>(analysisFiles.Select(n => n.AnalysisFilePropertyBean.AnalysisFileName));
                    for (int i = 0; i < analysisFiles.Count; i++)
                    {
                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "assay[" + (i + 1) + "]", assay[i] })); //fileName
                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "assay[" + (i + 1) + "]-ms_run_ref", "ms_run[" + (i + 1) + "]" }));
                    };

                    var studyVariable = new List<string>(projectProp.ClassnameToOrder.Keys);  // Class ID
                    var studyVariableAssayRef = new List<string>(analysisFiles.Select(n => n.AnalysisFilePropertyBean.AnalysisFileClass));
                    //var studyVariableDescription = "sample";

                    for (int i = 0; i < studyVariable.Count; i++)
                    {
                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "study_variable[" + (i + 1) + "]", studyVariable[i] }));

                        var studyVariableAssayRefGroup = new List<string>();

                        for (int j = 0; j < analysisFiles.Count; j++)
                        {
                            if (studyVariableAssayRef[j] == studyVariable[i])
                            {
                                studyVariableAssayRefGroup.Add("assay[" + (j + 1) + "] ");
                            };
                        };
                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "study_variable[" + (i + 1) + "]-assay_refs", string.Join("| ", studyVariableAssayRefGroup) }));
                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "study_variable[" + (i + 1) + "]-description", studyVariable[i] }));
                    }

                    for (int i = 0; i < cvList.Count; i++)
                    {
                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "cv[" + (i + 1) + "]-label", cvList[i][0] }));
                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "cv[" + (i + 1) + "]-full_name", cvList[i][1] }));
                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "cv[" + (i + 1) + "]-version", cvList[i][2] }));
                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "cv[" + (i + 1) + "]-uri", cvList[i][3] }));
                    }

                    for (int i = 0; i < database.Count; i++)
                    {
                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "database[" + (i + 1) + "]", database[i][0] }));
                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "database[" + (i + 1) + "]-prefix", database[i][1] }));
                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "database[" + (i + 1) + "]-version", database[i][2] }));
                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "database[" + (i + 1) + "]-uri", database[i][3] }));
                    }

                    var quantCvString = "";
                    var normalizedComment = "";
                    switch (exportType)
                    {
                        case "Height":
                            quantCvString = "[,,precursor intensity (peak height), ]";
                            break;
                        case "Normalized":
                            quantCvString = "[,,Normalised Abundance, ]";
                            if (analysisParamForLC.IsNormalizeSplash)
                            {
                                quantCvString = "[,,precursor intensity (peak height), ]";

                                if (splashQuant == 1)
                                {
                                    normalizedComment = "Data is normalized by SPLASH internal standard";
                                    //IEnumerable<string>ionAbundanceUnit = new List<string>(alignmentResultBean.AlignmentPropertyBeanCollection.Select(n => n.IonAbundanceUnit.ToString())).Distinct();
                                    if (ionAbundanceUnit.Contains("pmol"))
                                    {
                                        quantCvString = "[UO,UO:0000066,picomolar, ]";
                                    }
                                    else if (ionAbundanceUnit.Contains("fmol"))
                                    {
                                        quantCvString = "[UO,UO:0000073,femtomolar, ]";
                                    }
                                    else if (ionAbundanceUnit.Contains("pg"))
                                    {
                                        quantCvString = "[UO,UO:0000025,picogram, ]";
                                    }
                                    else if (ionAbundanceUnit.Contains("ng"))
                                    {
                                        quantCvString = "[UO,UO:0000024,nanogram, ]";
                                    }
                                    else if (ionAbundanceUnit.Contains("nmol_per_microL_plasma"))
                                    {
                                        quantCvString = "[,, nmol/microliter plasma, ]";
                                    }
                                    else if (ionAbundanceUnit.Contains("pmol_per_microL_plasma"))
                                    {
                                        quantCvString = "[,, pmol/microliter plasma, ]";
                                    }
                                    else if (ionAbundanceUnit.Contains("fmol_per_microL_plasma"))
                                    {
                                        quantCvString = "[,, fmol/microliter plasma, ]";
                                    }
                                    else if (ionAbundanceUnit.Contains("nmol_per_mg_tissue"))
                                    {
                                        quantCvString = "[,, nmol/mg tissue, ]";
                                    }
                                    else if (ionAbundanceUnit.Contains("nmol_per_mg_tissue"))
                                    {
                                        quantCvString = "[,, nmol/mg tissue, ]";
                                    }
                                    else if (ionAbundanceUnit.Contains("pmol_per_mg_tissue"))
                                    {
                                        quantCvString = "[,, pmol/mg tissue, ]";
                                    }
                                    else if (ionAbundanceUnit.Contains("fmol_per_mg_tissue"))
                                    {
                                        quantCvString = "[,, fmol/mg tissue, ]";
                                    }
                                    else if (ionAbundanceUnit.Contains("nmol_per_10E6_cells"))
                                    {
                                        quantCvString = "[,, nmol/10^6 cells, ]";
                                    }
                                    else if (ionAbundanceUnit.Contains("pmol_per_10E6_cells"))
                                    {
                                        quantCvString = "[,, pmol/10^6 cells, ]";
                                    }
                                    else if (ionAbundanceUnit.Contains("fmol_per_10E6_cells"))
                                    {
                                        quantCvString = "[,, fmol/10^6 cells, ]";
                                    }

                                }
                            }
                            if (analysisParamForLC.IsNormalizeIS)
                            {
                                normalizedComment = "Data is normalized by internal standerd SML ID(alighnment ID)";
                            }

                            break;
                        case "Area":
                            quantCvString = "[,,XIC area,]";
                            break;
                    }

                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "small_molecule-quantification_unit", quantCvString }));
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "small_molecule_feature-quantification_unit", quantCvString }));
                    if (normalizedComment != "")
                    {
                        sw.WriteLine(String.Join("\t", new string[] { commentPrefix, normalizedComment }));
                    }

                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "small_molecule-identification_reliability", smallMoleculeIdentificationReliability }));

                    for (int i = 0; i < idConfidenceMeasure.Count; i++)
                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "id_confidence_measure[" + (i + 1) + "]", idConfidenceMeasure[i] }));

                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "quantification_method", quantificationMethod }));

                    if (ionMobility == true)
                    {
                        var optMobilityUnit = "";
                        var optMobilityComment = "";
                        switch (ionMobilityType)
                        {
                            case "TIMS":
                                optMobilityUnit = "opt_global_Mobility=[,, 1/k0,]";
                                optMobilityComment = "Ion Mobility type = Trapped Ion Mobility Spectrometry";
                                break;
                            case "DTIMS":
                                optMobilityUnit = "opt_global_Mobility=[UO, UO: 0000028, millisecond,]";
                                optMobilityComment = "Ion Mobility type = Drift-Time Ion Mobility Spectrometry";
                                break;
                            case "TWIMS":
                                optMobilityUnit = "opt_global_Mobility=[UO, UO: 0000028, millisecond,]";
                                optMobilityComment = "Ion Mobility type = Travelling-Wave Ion Mobility Spectrometry";
                                break;
                        }

                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "colunit-small_molecule", optMobilityUnit }));
                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "colunit-small_molecule_feature", optMobilityUnit }));
                        sw.WriteLine(String.Join("\t", new string[] { commentPrefix, optMobilityComment }));

                    }

                    sw.WriteLine("");

                    //SML section

                    var quantitiveNormalizationBool = false;
                    if (exportType == "Normalized")
                    {
                        if (analysisParamForLC.IsNormalizeIS || analysisParamForLC.IsNormalizeSplash)
                        {
                            quantitiveNormalizationBool = true;
                        }
                    }

                    //Header
                    var headers = new List<string>() {
                        "SMH","SML_ID","SMF_ID_REFS","database_identifier","chemical_formula","smiles","inchi",
                          "chemical_name","uri","theoretical_neutral_mass","adduct_ions","reliability","best_id_confidence_measure",
                          "best_id_confidence_value"
                        };
                    for (int i = 0; i < headers.Count; i++) sw.Write(headers[i] + "\t");
                    for (int i = 0; i < analysisFiles.Count; i++) sw.Write("abundance_assay[" + (i + 1) + "]" + "\t");
                    for (int i = 0; i < studyVariable.Count; i++) sw.Write("abundance_study_variable[" + (i + 1) + "]" + "\t");
                    for (int i = 0; i < studyVariable.Count; i++) sw.Write("abundance_variation_study_variable[" + (i + 1) + "]" + "\t");

                    // if want to add optional column, header discribe here 
                    if (ionMobility == true)
                    {
                        sw.Write("opt_global_Mobility" + "\t" + "opt_global_CCS_values" + "\t");
                    }
                    //

                    if (quantitiveNormalizationBool == true)
                    {
                        if (analysisParamForLC.IsNormalizeIS || splashQuant == 1)
                        {
                            sw.Write("opt_global_internalStanderdSMLID" + "\t"
                                   + "opt_global_internalStanderdMetaboliteName" + "\t"
                                   );
                        }
                    }
                    //

                    sw.WriteLine("");

                    //header end

                    var isNormalized = exportType == "Normalized" ? true : false;
                    var mode = isNormalized ? BarChartDisplayMode.NormalizedHeight
                                            : exportType == "Height" ? BarChartDisplayMode.OriginalHeight
                                                                     : BarChartDisplayMode.OriginalArea;

                    var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
                    if (targetIsotopeTrackFileID >= 0)
                        alignedSpots = ResultExportLcUtility.GetFilteredAlignedSpotsByIsotpeTrackingResult(alignedSpots, mainWindow.AnalysisParamForLC, targetIsotopeTrackFileID);

                    if (alignedSpots != null && alignedSpots.Count > 0)
                        for (int i = 0; i < alignedSpots.Count; i++)
                        {
                            if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                            //if (alignedSpots[i].MetaboliteName == "") continue;
                            //if (analysisParamForLC.IsNormalizeSplash && splashQuant == 0 && alignedSpots[i].InternalStandardAlignmentID != -1){ continue; }
                            //else if (analysisParamForLC.IsNormalizeSplash && splashQuant == 1 && alignedSpots[i].InternalStandardAlignmentID == -1) { continue; }
                            if (analysisParamForLC.IsNormalizeSplash)
                            {
                                if (splashQuant == 0 && alignedSpots[i].InternalStandardAlignmentID != -1)
                                {
                                    continue;
                                }
                                else if (splashQuant == 1 && alignedSpots[i].InternalStandardAlignmentID == -1)
                                {
                                    continue;
                                }
                            }


                            if (ionMobility == true)
                            {
                                var masterIdDic = new Dictionary<int, string>();
                                for (int j = 0; j < alignedSpots.Count; j++)
                                {
                                    var alignedDriftSpots = alignedSpots[j].AlignedDriftSpots;
                                    masterIdDic[alignedSpots[j].MasterID] = alignedSpots[j].MetaboliteName;
                                    for (int k = 0; k < alignedDriftSpots.Count; k++)
                                    {
                                        masterIdDic[alignedDriftSpots[k].MasterID] = alignedDriftSpots[k].MetaboliteName;
                                    }
                                }

                                //parent data line(MS1 only)
                                ResultExportLcUtility.WriteDataMatrixIonmobilityMztabSMLData(sw, alignedSpots[i], new AlignedDriftSpotPropertyBean(), mspDB, txtDB, mainWindow.AnalysisParamForLC, database, idConfidenceDefault, idConfidenceManual);
                                // Replace true zero values with 1/10 of minimum peak height over all samples
                                var nonZeroMin = getInterpolatedValueForMissingValue(alignedSpots[i].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                                var statsList = MsDialStatistics.AverageStdevProperties(alignedSpots[i], analysisFiles, projectProp, mode);
                                ResultExportLcUtility.WriteDataMztab(sw, alignedSpots[i].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin, statsList);
                                sw.Write("\t");
                                // if want to add optional column, add data here 
                                //opt_global_Mobility
                                var dtString = "null";
                                sw.Write(dtString + "\t");
                                //opt_global_CCS_values
                                var ccsString = "null";
                                sw.Write(ccsString + "\t");
                                // opt_global_internalStanderdSMLID, opt_global_internalStanderdMetaboliteName
                                if (quantitiveNormalizationBool == true)
                                {
                                    if (analysisParamForLC.IsNormalizeIS || splashQuant == 1)
                                    {
                                        var isIdString = "null";
                                        var isName = "null";
                                        if (alignedSpots[i].InternalStandardAlignmentID != -1)
                                        {
                                            int isId = alignedSpots[i].InternalStandardAlignmentID;
                                            isIdString = isId.ToString();
                                            if (masterIdDic[isId] != "")
                                            {
                                                isName = masterIdDic[isId];
                                            }
                                        }
                                        sw.Write(isIdString + "\t" + isName + "\t");
                                    }
                                }
                                //
                                sw.WriteLine("");
                                //MSMS data line
                                var driftSpots = alignedSpots[i].AlignedDriftSpots;
                                for (int j = 0; j < driftSpots.Count; j++)
                                {
                                    if (blankFilter && driftSpots[j].IsBlankFiltered) continue;
                                    //if (driftSpots[j].MetaboliteName == "") continue;
                                    ResultExportLcUtility.WriteDataMatrixIonmobilityMztabSMLData(sw, alignedSpots[i], driftSpots[j],
                                        mspDB, txtDB, mainWindow.AnalysisParamForLC, database, idConfidenceDefault, idConfidenceManual);
                                    // Replace true zero values with 1/10 of minimum peak height over all samples
                                    nonZeroMin = getInterpolatedValueForMissingValue(driftSpots[j].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                                    statsList = MsDialStatistics.AverageStdevProperties(driftSpots[j], analysisFiles, projectProp, mode);
                                    ResultExportLcUtility.WriteDataMztab(sw, driftSpots[j].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin, statsList);
                                    sw.Write("\t");

                                    // if want to add optional column, add data here 
                                    //opt_global_Mobility and opt_global_CCS_values
                                    ResultExportLcUtility.ExportIonmobilityDtCcsData(sw, alignedSpots[i], driftSpots[j], mainWindow.AnalysisParamForLC);
                                    // opt_global_internalStanderdSMLID, opt_global_internalStanderdMetaboliteName
                                    if (quantitiveNormalizationBool == true)
                                    {
                                        if (analysisParamForLC.IsNormalizeIS || splashQuant == 1)
                                        {
                                            var isIdString = "null";
                                            var isName = "null";
                                            if (alignedSpots[i].InternalStandardAlignmentID != -1)
                                            {
                                                int isId = alignedSpots[i].InternalStandardAlignmentID;
                                                isIdString = isId.ToString();
                                                if (masterIdDic[isId] != "")
                                                {
                                                    isName = masterIdDic[isId];
                                                }
                                            }
                                            sw.Write(isIdString + "\t" + isName + "\t");
                                        }
                                    }
                                    //
                                    sw.WriteLine("");

                                }
                            }
                            else
                            {
                                ResultExportLcUtility.WriteDataMatrixMztabSMLData(sw, alignedSpots[i],
                                        mspDB, txtDB, mainWindow.AnalysisParamForLC, database, idConfidenceDefault, idConfidenceManual);
                                // Replace true zero values with 1/10 of minimum peak height over all samples
                                var nonZeroMin = getInterpolatedValueForMissingValue(alignedSpots[i].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                                var statsList = MsDialStatistics.AverageStdevProperties(alignedSpots[i], analysisFiles, projectProp, mode);
                                ResultExportLcUtility.WriteDataMztab(sw, alignedSpots[i].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin, statsList);
                                sw.Write("\t");
                                // if want to add optional column, add data here 

                                // opt_global_internalStanderdSMLID, opt_global_internalStanderdMetaboliteName
                                if (quantitiveNormalizationBool == true)
                                {
                                    if (analysisParamForLC.IsNormalizeIS || splashQuant == 1)
                                    {
                                        var isIdString = "null";
                                        var isName = "null";
                                        if (alignedSpots[i].InternalStandardAlignmentID != -1)
                                        {
                                            int isId = alignedSpots[i].InternalStandardAlignmentID;
                                            isIdString = isId.ToString();
                                            if (alignedSpots[isId].MetaboliteName != "")
                                            {
                                                isName = alignedSpots[isId].MetaboliteName;
                                            }
                                        }
                                        sw.Write(isIdString + "\t" + isName + "\t");
                                    }
                                }
                                //
                                sw.WriteLine("");
                            }
                        }

                    //SML section end
                    sw.WriteLine("");

                    //SMF section
                    //header
                    headers = new List<string>() {
                "SFH","SMF_ID","SME_ID_REFS","SME_ID_REF_ambiguity_code","adduct_ion","isotopomer","exp_mass_to_charge",
                  "charge","retention_time_in_seconds","retention_time_in_seconds_start","retention_time_in_seconds_end"
                };
                    for (int i = 0; i < headers.Count; i++) sw.Write(headers[i] + "\t");
                    for (int i = 0; i < analysisFiles.Count; i++) sw.Write("abundance_assay[" + (i + 1) + "]" + "\t");

                    // add optional column, header discribe here 
                    if (ionMobility == true)
                    {
                        sw.Write("opt_global_Mobility" + "\t" + "opt_global_CCS_values" + "\t");
                    }
                    // opt_global_internalStanderdSMLID, opt_global_internalStanderdMetaboliteName
                    if (quantitiveNormalizationBool == true)
                    {
                        if (analysisParamForLC.IsNormalizeIS || splashQuant == 1)
                        {
                            sw.Write("opt_global_internalStanderdSMLID" + "\t"
                                   + "opt_global_internalStanderdMetaboliteName" + "\t"
                                   );
                        }
                    }

                    //
                    sw.WriteLine("");

                    //header end
                    for (int i = 0; i < alignedSpots.Count; i++)
                    {
                        if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                        //if (alignedSpots[i].MetaboliteName == "") continue;
                        //if (analysisParamForLC.IsNormalizeSplash && splashQuant == 0 && alignedSpots[i].InternalStandardAlignmentID != -1){ continue; }
                        //else if (analysisParamForLC.IsNormalizeSplash && splashQuant == 1 && alignedSpots[i].InternalStandardAlignmentID == -1) { continue; }
                        if (analysisParamForLC.IsNormalizeSplash)
                        {
                            if (splashQuant == 0 && alignedSpots[i].InternalStandardAlignmentID != -1)
                            {
                                continue;
                            }
                            else if (splashQuant == 1 && alignedSpots[i].InternalStandardAlignmentID == -1)
                            {
                                continue;
                            }
                        }

                        if (ionMobility == true)
                        {
                            var masterIdDic = new Dictionary<int, string>();
                            for (int j = 0; j < alignedSpots.Count; j++)
                            {
                                var alignedDriftSpots = alignedSpots[j].AlignedDriftSpots;
                                masterIdDic[alignedSpots[j].MasterID] = alignedSpots[j].MetaboliteName;
                                for (int k = 0; k < alignedDriftSpots.Count; k++)
                                {
                                    masterIdDic[alignedDriftSpots[k].MasterID] = alignedDriftSpots[k].MetaboliteName;
                                }
                            }

                            ResultExportLcUtility.WriteDataMatrixIonmobilityMztabSMFData(sw, alignedSpots[i], new AlignedDriftSpotPropertyBean(), mainWindow.ProjectProperty.IonMode.ToString());
                            // Replace true zero values with 1/10 of minimum peak height over all samples
                            var nonZeroMin = getInterpolatedValueForMissingValue(alignedSpots[i].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                            ResultExportLcUtility.WriteDataMztab(sw, alignedSpots[i].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin);
                            sw.Write("\t");
                            // if want to add optional column, add data here 
                            //opt_global_Mobility
                            var dtString = "null";
                            sw.Write(dtString + "\t");
                            //opt_global_CCS_values
                            var ccsString = "null";
                            sw.Write(ccsString + "\t");
                            // opt_global_internalStanderdSMLID, opt_global_internalStanderdMetaboliteName
                            if (quantitiveNormalizationBool == true)
                            {
                                if (analysisParamForLC.IsNormalizeIS || splashQuant == 1)
                                {
                                    var isIdString = "null";
                                    var isName = "null";
                                    if (alignedSpots[i].InternalStandardAlignmentID != -1)
                                    {
                                        int isId = alignedSpots[i].InternalStandardAlignmentID;
                                        isIdString = isId.ToString();
                                        if (masterIdDic[isId] != "")
                                        {
                                            isName = masterIdDic[isId];
                                        }
                                    }
                                    sw.Write(isIdString + "\t" + isName + "\t");
                                }
                            }
                            //
                            sw.WriteLine("");

                            var driftSpots = alignedSpots[i].AlignedDriftSpots;
                            for (int j = 0; j < driftSpots.Count; j++)
                            {
                                if (blankFilter && driftSpots[j].IsBlankFiltered) continue;
                                //if (driftSpots[j].MetaboliteName == "") continue;
                                ResultExportLcUtility.WriteDataMatrixIonmobilityMztabSMFData(sw, alignedSpots[i], driftSpots[j], mainWindow.ProjectProperty.IonMode.ToString());
                                // Replace true zero values with 1/10 of minimum peak height over all samples
                                nonZeroMin = getInterpolatedValueForMissingValue(driftSpots[j].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                                ResultExportLcUtility.WriteDataMztab(sw, driftSpots[j].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin);
                                sw.Write("\t");
                                // if want to add optional column, add data here 
                                //opt_global_Mobility and opt_global_CCS_values
                                ResultExportLcUtility.ExportIonmobilityDtCcsData(sw, alignedSpots[i], driftSpots[j], mainWindow.AnalysisParamForLC);

                                //
                                // opt_global_internalStanderdSMLID, opt_global_internalStanderdMetaboliteName
                                if (quantitiveNormalizationBool == true)
                                {
                                    if (analysisParamForLC.IsNormalizeIS || splashQuant == 1)
                                    {
                                        var isIdString = "null";
                                        var isName = "null";
                                        if (alignedSpots[i].InternalStandardAlignmentID != -1)
                                        {
                                            int isId = alignedSpots[i].InternalStandardAlignmentID;
                                            isIdString = isId.ToString();
                                            if (masterIdDic[isId] != "")
                                            {
                                                isName = masterIdDic[isId];
                                            }
                                        }
                                        sw.Write(isIdString + "\t" + isName + "\t");
                                    }
                                }
                                sw.WriteLine("");
                            }
                        }
                        else
                        {
                            var alignedSpot = alignedSpots[i];
                            var smfPrefix = "SMF";
                            var smfID = alignedSpots[i].AlignmentID;
                            var smeIDrefs = "null";
                            var smeIDrefAmbiguity_code = "null";
                            var isotopomer = "null";
                            var expMassToCharge = alignedSpot.CentralAccurateMass.ToString(); // OK
                            var retentionTimeSeconds = alignedSpot.CentralRetentionTime * 60;
                            var retentionTimeStart = (alignedSpot.CentralRetentionTime - (alignedSpot.AveragePeakWidth / 2)) * 60;
                            var retentionTimeEnd = (alignedSpot.CentralRetentionTime + (alignedSpot.AveragePeakWidth / 2)) * 60;
                            var adductIons = alignedSpot.AdductIonName;
                            if (alignedSpot.AdductIonName.Substring(alignedSpot.AdductIonName.Length - 2, 1) == "]")
                            {
                                adductIons = alignedSpot.AdductIonName.Substring(0, alignedSpot.AdductIonName.IndexOf("]") + 1) + alignedSpot.ChargeNumber + alignedSpot.AdductIonName.Substring(alignedSpot.AdductIonName.Length - 1, 1);
                            }
                            if (alignedSpots[i].MetaboliteName != "" && alignedSpot.IsMs2Match == true) smeIDrefs = alignedSpot.AlignmentID.ToString();

                            var charge = alignedSpot.ChargeNumber.ToString();

                            if (mainWindow.ProjectProperty.IonMode == IonMode.Negative)
                            {
                                charge = "-" + alignedSpot.ChargeNumber.ToString();
                            }

                            var metadata = new List<string>() {
                        smfPrefix,smfID.ToString(), smeIDrefs.ToString(), smeIDrefAmbiguity_code,
                            adductIons, isotopomer, expMassToCharge, charge , retentionTimeSeconds.ToString(),retentionTimeStart.ToString(),retentionTimeEnd.ToString()
                        };
                            var metadata2 = new List<string>();
                            foreach (string item in metadata)
                            {
                                var metadataMember = item;
                                if (metadataMember == "")
                                {
                                    metadataMember = "null";
                                }
                                metadata2.Add(metadataMember);
                            }
                            sw.Write(String.Join("\t", metadata2) + "\t");

                            //sw.Write(String.Join("\t", metadata) + "\t");

                            // Replace true zero values with 1/2 of minimum peak height over all samples
                            var nonZeroMin = getInterpolatedValueForMissingValue(alignedSpots[i].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                            var statsList = MsDialStatistics.AverageStdevProperties(alignedSpots[i], analysisFiles, projectProp, mode);
                            ResultExportLcUtility.WriteDataMztab(sw, alignedSpots[i].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin);
                            sw.Write("\t");
                            // if want to add optional column, discribe here 
                            // opt_global_internalStanderdSMLID, opt_global_internalStanderdMetaboliteName
                            if (quantitiveNormalizationBool == true)
                            {
                                if (analysisParamForLC.IsNormalizeIS || splashQuant == 1)
                                {
                                    var isIdString = "null";
                                    var isName = "null";
                                    if (alignedSpots[i].InternalStandardAlignmentID != -1)
                                    {
                                        int isId = alignedSpots[i].InternalStandardAlignmentID;
                                        isIdString = isId.ToString();
                                        if (alignedSpots[isId].MetaboliteName != "")
                                        {
                                            isName = alignedSpots[isId].MetaboliteName;
                                        }
                                    }
                                    sw.Write(isIdString + "\t" + isName + "\t");
                                }
                            }

                            //
                            sw.WriteLine("");
                        }
                    }
                    // comment line
                    sw.WriteLine("COM\t \"retention_time_in_seconds_start\" and \"retention_time_in_seconds_end\" were calculated using average peak width");
                    //
                    //SMF section end
                    sw.WriteLine("");

                    // SME section

                    var ms2Match = new List<bool>(alignedSpots.Select(n => n.IsMs2Match));
                    if (ms2Match.Contains(true))
                    {
                        //header
                        var headersSME01 = new List<string>() {
                    "SEH","SME_ID","evidence_input_id","database_identifier","chemical_formula","smiles","inchi",
                      "chemical_name","uri","derivatized_form","adduct_ion","exp_mass_to_charge","charge", "theoretical_mass_to_charge",
                      "spectra_ref","identification_method","ms_level"
                    };
                        for (int i = 0; i < headersSME01.Count; i++) sw.Write(headersSME01[i] + "\t");
                        for (int i = 0; i < idConfidenceMeasure.Count; i++) sw.Write("id_confidence_measure[" + (i + 1) + "]" + "\t");
                        sw.Write("rank");
                        // add optional column, header discribe here 


                        //
                        sw.WriteLine("");

                        //header end
                        for (int i = 0; i < alignedSpots.Count; i++)
                        {
                            if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;

                            //if (analysisParamForLC.IsNormalizeSplash && splashQuant == 0 && alignedSpots[i].InternalStandardAlignmentID != -1){ continue; }
                            //else if (analysisParamForLC.IsNormalizeSplash && splashQuant == 1 && alignedSpots[i].InternalStandardAlignmentID == -1) { continue; }
                            if (analysisParamForLC.IsNormalizeSplash)
                            {
                                if (splashQuant == 0 && alignedSpots[i].InternalStandardAlignmentID != -1)
                                {
                                    continue;
                                }
                                else if (splashQuant == 1 && alignedSpots[i].InternalStandardAlignmentID == -1)
                                {
                                    continue;
                                }
                            }

                            if (ionMobility == true)
                            {
                                var driftSpots = alignedSpots[i].AlignedDriftSpots;
                                for (int j = 0; j < driftSpots.Count; j++)
                                {
                                    if (blankFilter && driftSpots[j].IsBlankFiltered) continue;
                                    if (driftSpots[j].IsMs2Match != true) continue;
                                    if (driftSpots[j].MetaboliteName == "") continue;
                                    ResultExportLcUtility.WriteDataMatrixIonmobilityMztabSMEData(sw, alignedSpots[i], driftSpots[j], mspDB, txtDB, projectProp, database, idConfidenceDefault, idConfidenceManual, analysisFiles);

                                    sw.Write("\t");
                                    // if want to add optional column, add data here 

                                    //
                                    sw.WriteLine("");

                                }
                            }
                            else
                            {
                                if (alignedSpots[i].MetaboliteName == "") continue;
                                if (alignedSpots[i].IsMs2Match != true) continue;
                                ResultExportLcUtility.WriteDataMatrixMztabSMEData(sw, alignedSpots[i], mspDB, txtDB, projectProp, database, idConfidenceDefault, idConfidenceManual, analysisFiles);
                                // if want to add optional column, discribe here 

                                //
                                sw.WriteLine("");
                            }

                        }
                        //SME section end
                    }
                }
            }
        }
    }
}

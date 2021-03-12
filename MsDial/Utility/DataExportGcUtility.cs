using CompMs.Common.MessagePack;
using Msdial.Gcms.Dataprocess.Algorithm;
using Msdial.Gcms.Dataprocess.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
	public class DataExportGcUtility
    {
        public static void PeaklistExport(MainWindow mainWindow, string exportFolderPath, ObservableCollection<AnalysisFileBean> files, ExportSpectraFileFormat exportSpectraFileFormat)
        {
            int focusedFileID = mainWindow.FocusedFileID;
            int focusedPeakID = mainWindow.FocusedPeakID;

            mainWindow.PeakViewDataAccessRefresh();

            mainWindow.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            peaklistExport(mainWindow, exportFolderPath, files, exportSpectraFileFormat);

            mainWindow.PeakViewerForGcRefresh(focusedFileID);
            ((PairwisePlotPeakViewUI)mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.SelectedPlotId = focusedPeakID;

            Mouse.OverrideCursor = null;
            mainWindow.IsEnabled = true;
        }

        private static void peaklistExport(MainWindow mainWindow, string exportFolderPath, ObservableCollection<AnalysisFileBean> files, ExportSpectraFileFormat exportFormat)
        {
            var filePath = string.Empty;
            var extention = exportFormat.ToString();
            var projectProp = mainWindow.ProjectProperty;
            for (int i = 0; i < files.Count; i++)
            {
                filePath = exportFolderPath + "\\" + files[i].AnalysisFilePropertyBean.AnalysisFileName + "." + extention;
                using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
                {
                    if (exportFormat == ExportSpectraFileFormat.txt) ResultExportGcUtility.WriteTxtHeader(sw);

                    var ms1DecResults = DataStorageGcUtility.ReadMS1DecResults(files[i].AnalysisFilePropertyBean.DeconvolutionFilePath);
                    for (int j = 0; j < ms1DecResults.Count; j++)
                    {
                        switch (exportFormat)
                        {
                            case ExportSpectraFileFormat.msp: ResultExportGcUtility.WriteAsMsp(sw, ms1DecResults[j].Ms1DecID, ms1DecResults[j], mainWindow.MspDB); break;
                            case ExportSpectraFileFormat.mgf: ResultExportGcUtility.WriteAsMgf(sw, ms1DecResults[j], mainWindow.MspDB); break;
                            case ExportSpectraFileFormat.txt: ResultExportGcUtility.WriteAsTxt(sw, ms1DecResults[j], mainWindow.MspDB); break;
                            default: break;
                        }
                    }
                }
            }
        }

        public static void AlignmentResultExport(MainWindow mainWindow, int selectedAlignmentFileID)
        {
            var alignmentFile = mainWindow.AlignmentFiles[selectedAlignmentFileID];
            var alignmentResult = MessagePackHandler.LoadFromFile<AlignmentResultBean>(alignmentFile.FilePath);
            //var alignmentResult = (AlignmentResultBean)DataStorageGcUtility.LoadFromXmlFile(alignmentFile.FilePath, typeof(AlignmentResultBean));
            var ms1DecResults = DataStorageGcUtility.ReadMS1DecResults(alignmentFile.SpectraFilePath);
            var analysisFiles = mainWindow.AnalysisFiles;
            var param = mainWindow.AnalysisParamForGC;
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

            if (isNormalizedData == true && alignmentResult.Normalized == false) {
                MessageBox.Show("Data is not normalized yet. If you want to export the normalized data matrix, please at first perform data normalization methods from statistical analysis procedure.", "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                isNormalizedData = false;
            }

            mainWindow.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            int focusedFileID = mainWindow.FocusedFileID;
            int focusedPeakID = mainWindow.FocusedPeakID;
            int focusedAlignmentFileID = mainWindow.FocusedAlignmentFileID;
            int focusedAlignmentMs1DecID = mainWindow.FocusedAlignmentMs1DecID;

            mainWindow.PeakViewDataAccessRefresh();
            mainWindow.AlignmentViewDataAccessRefresh();

            DateTime dt = DateTime.Now;
            string heightFile = exportFolderPath + "\\" + "Height_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string normalizedFile = exportFolderPath + "\\" + "Normalized_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string areaFile = exportFolderPath + "\\" + "Area_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string idFile = exportFolderPath + "\\" + "PeakID_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string rtFile = exportFolderPath + "\\" + "RT_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string riFile = exportFolderPath + "\\" + "RI_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string snFile = exportFolderPath + "\\" + "SN_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string spectrumFile = exportFolderPath + "\\" + "Spectrum_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + "." + exportSpectraFileFormat.ToString();
            string paramFile = exportFolderPath + "\\" + "Parameter_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            string heightMztabFile = exportFolderPath + "\\" + "Height_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".mzTab.txt";
            string normalizedMztabFile = exportFolderPath + "\\" + "Normalized_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".mzTab.txt";
            string areaMztabFile = exportFolderPath + "\\" + "Area_" + selectedAlignmentFileID + "_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".mzTab.txt";

            if (isRawData == true) {
                exportAlignmentResultWithAveStd(heightFile, alignmentResult, projectProp, ms1DecResults, analysisFiles, mainWindow.MspDB, param, "Height", blankFilter, replaceZeroToHalf);
                if (isExportedAsMzTabM) {
                    exportResultMztabFormet(heightMztabFile, alignmentResult, projectProp, ms1DecResults, analysisFiles, mainWindow.MspDB, param, "Height", blankFilter, replaceZeroToHalf);
                }
            }
            if (isNormalizedData == true) {
                exportAlignmentResultWithAveStd(normalizedFile, alignmentResult, projectProp, ms1DecResults, analysisFiles, mainWindow.MspDB, param, "Normalized", blankFilter, replaceZeroToHalf);
                if (isExportedAsMzTabM) {
                    exportResultMztabFormet(normalizedMztabFile, alignmentResult, projectProp, ms1DecResults, analysisFiles, mainWindow.MspDB, param, "Normalized", blankFilter, replaceZeroToHalf);
                }
            }
            if (isAreaData == true) {
                exportAlignmentResultWithAveStd(areaFile, alignmentResult, projectProp, ms1DecResults, analysisFiles, mainWindow.MspDB, param, "Area", blankFilter, replaceZeroToHalf);
                if (isExportedAsMzTabM) {
                    exportResultMztabFormet(areaMztabFile, alignmentResult, projectProp, ms1DecResults, analysisFiles, mainWindow.MspDB, param, "Area", blankFilter, replaceZeroToHalf);
                }
            }
            if (isIdData == true) exportAlignmentResult(idFile, alignmentResult, ms1DecResults, analysisFiles, mainWindow.MspDB, param, "Id", blankFilter, replaceZeroToHalf);
            if (snMatrix == true) exportAlignmentResult(snFile, alignmentResult, ms1DecResults, analysisFiles, mainWindow.MspDB, param, "SN", blankFilter, replaceZeroToHalf);
            if (isRtData == true) {
                exportAlignmentResult(rtFile, alignmentResult, ms1DecResults, analysisFiles, mainWindow.MspDB, param, "RT", blankFilter, replaceZeroToHalf);
                if (alignmentResult.AnalysisParamForGC.RetentionType == RetentionType.RI) {
                    exportAlignmentResult(riFile, alignmentResult, ms1DecResults, analysisFiles, mainWindow.MspDB, param, "RI", blankFilter, replaceZeroToHalf);
                }
            }
            if (isMzData == true) exportAlignmentResult(spectrumFile, alignmentResult, ms1DecResults, analysisFiles, mainWindow.MspDB, param, "QuantMass", blankFilter, replaceZeroToHalf);
            if (isRepSpectra == true) exportSpectraFile(spectrumFile, ms1DecResults, alignmentResult, mainWindow.MspDB, exportSpectraFileFormat, blankFilter);
            if (isParamExport == true) exportParameter(paramFile, alignmentResult, analysisFiles);

            mainWindow.PeakViewerForGcRefresh(focusedFileID);
            ((PairwisePlotPeakViewUI)mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.SelectedPlotId = focusedPeakID;

            if (focusedAlignmentFileID < 0) {
                Mouse.OverrideCursor = null;
                mainWindow.IsEnabled = true;
                return;
            }

            mainWindow.AlignmentViewerForGcRefresh(focusedAlignmentFileID);
            ((PairwisePlotAlignmentViewUI)mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.SelectedPlotId = focusedAlignmentMs1DecID;

            Mouse.OverrideCursor = null;
            mainWindow.IsEnabled = true;
        }

        private static void exportParameter(string paramFile, AlignmentResultBean alignmentResult, ObservableCollection<AnalysisFileBean> analysisFiles)
        {
            var param = alignmentResult.AnalysisParamForGC;
            if (param == null) {
                MessageBox.Show("Parameter is not saved at MS-DIAL version 2.40 or former.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ParameterExport(analysisFiles, param, paramFile);
        }

        private static void exportAlignmentResult(string file, AlignmentResultBean alignmentResult,
            List<MS1DecResult> ms1DecResults, ObservableCollection<AnalysisFileBean> analysisFiles, 
            List<MspFormatCompoundInformationBean> mspDB, AnalysisParamOfMsdialGcms param, string exportType, bool blankFilter, bool replaceZeroToHalf)
        {
            using (StreamWriter sw = new StreamWriter(file, false, Encoding.ASCII))
            {
                //Header
                ResultExportGcUtility.WriteDataMatrixHeader(sw, analysisFiles);

                var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
                //From the second
                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                    ResultExportGcUtility.WriteDataMatrixMetaData(sw, alignedSpots[i], ms1DecResults[i], mspDB, param);

                    // Replace true zero values with 1/2 of minimum peak height over all samples
                    //var nonZeroMin = double.MaxValue;
                    var nonZeroMin = getInterpolatedValueForMissingValue(alignedSpots[i].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                    ResultExportGcUtility.WriteData(sw, alignedSpots[i].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin);

                    //if (replaceZeroToHalf) {
                    //    foreach (var peak in alignedSpots[i].AlignedPeakPropertyBeanCollection) {

                    //        var variable = 0.0;
                    //        switch (exportType) {
                    //            case "Height":
                    //                variable = peak.Variable;
                    //                break;
                    //            case "Normalized":
                    //                variable = peak.NormalizedVariable;
                    //                break;
                    //            case "Area":
                    //                variable = peak.Area;
                    //                break;
                    //        }

                    //        if (variable > 0 && nonZeroMin > variable)
                    //            nonZeroMin = variable;
                    //    }

                    //    if (nonZeroMin == double.MaxValue)
                    //        nonZeroMin = 1;
                    //}

                    //for (int j = 0; j < alignedSpots[i].AlignedPeakPropertyBeanCollection.Count; j++)
                    //{
                    //    var spotValue = ResultExportGcUtility.GetSpotValue(alignedSpots[i].AlignedPeakPropertyBeanCollection[j], exportType);

                    //    //converting
                    //    if (replaceZeroToHalf && (exportType == "Height" || exportType == "Normalized" || exportType == "Area")) {
                    //        double doublevalue = 0.0;
                    //        double.TryParse(spotValue, out doublevalue);
                    //        if (doublevalue == 0)
                    //            doublevalue = nonZeroMin * 0.1;
                    //        spotValue = doublevalue.ToString();
                    //    }

                    //    if (j == alignedSpots[i].AlignedPeakPropertyBeanCollection.Count - 1)
                    //        sw.WriteLine(spotValue);
                    //    else
                    //        sw.Write(spotValue + "\t");
                    //}
                }
            }
        }

        private static void exportAlignmentResultWithAveStd(string file, AlignmentResultBean alignmentResult, ProjectPropertyBean projectProp,
            List<MS1DecResult> ms1DecResults, ObservableCollection<AnalysisFileBean> analysisFiles,
            List<MspFormatCompoundInformationBean> mspDB, AnalysisParamOfMsdialGcms param, string exportType, bool blankFilter, bool replaceZeroToHalf) {

            if (exportType != "Height" && exportType != "Normalized" && exportType != "Area") {
                exportAlignmentResult(file, alignmentResult, ms1DecResults, analysisFiles, mspDB, param, exportType, blankFilter, replaceZeroToHalf);
                return;
            }

            var isNormalized = exportType == "Normalized" ? true : false;
            var mode = isNormalized ? BarChartDisplayMode.NormalizedHeight
                                    : exportType == "Height" ? BarChartDisplayMode.OriginalHeight
                                                             : BarChartDisplayMode.OriginalArea;

            var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
            if (alignedSpots == null || alignedSpots.Count == 0) return;

            var tempClassArray = MsDialStatistics.AverageStdevProperties(alignedSpots[0], analysisFiles, projectProp, mode, false);
            if (tempClassArray.Count == analysisFiles.Count) { // meaining no class properties are set.
                exportAlignmentResult(file, alignmentResult, ms1DecResults, analysisFiles, mspDB, param, exportType, blankFilter, replaceZeroToHalf);
                return;
            }

            using (StreamWriter sw = new StreamWriter(file, false, Encoding.ASCII)) {
                //Header
                ResultExportGcUtility.WriteDataMatrixHeader(sw, analysisFiles, tempClassArray);

                //From the second
                for (int i = 0; i < alignedSpots.Count; i++) {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                    ResultExportGcUtility.WriteDataMatrixMetaData(sw, alignedSpots[i], ms1DecResults[i], mspDB, param);

                    // Replace true zero values with 1/2 of minimum peak height over all samples
                    var nonZeroMin = getInterpolatedValueForMissingValue(alignedSpots[i].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                    var statsList = MsDialStatistics.AverageStdevProperties(alignedSpots[i], analysisFiles, projectProp, mode, false);
                    ResultExportGcUtility.WriteData(sw, alignedSpots[i].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin, statsList);
                }
            }
        }

        private static float getInterpolatedValueForMissingValue(ObservableCollection<AlignedPeakPropertyBean> alignedPeakPropertyBeanCollection,
            bool replaceZeroToHalf, string exportType) {
            var nonZeroMin = float.MaxValue;
            if (replaceZeroToHalf) {
                foreach (var peak in alignedPeakPropertyBeanCollection) {

                    var variable = 0.0F;
                    switch (exportType) {
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

        private static void exportSpectraFile(string spectrumFile, List<MS1DecResult> ms1DecResults, AlignmentResultBean alignmentResult,
            List<MspFormatCompoundInformationBean> mspDB, ExportSpectraFileFormat exportFormat, bool blankFilter)
        {
            var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
            using (StreamWriter sw = new StreamWriter(spectrumFile, false, Encoding.ASCII))
            {
                if (exportFormat == ExportSpectraFileFormat.txt) ResultExportGcUtility.WriteTxtHeader(sw);

                for (int j = 0; j < ms1DecResults.Count; j++)
                {
                    if (blankFilter && alignedSpots[j].IsBlankFiltered) continue;
                    switch (exportFormat)
                    {
                        case ExportSpectraFileFormat.msp: ResultExportGcUtility.WriteAsMsp(sw, alignedSpots[j].AlignmentID, ms1DecResults[j], mspDB, alignedSpots[j]); break;
                        case ExportSpectraFileFormat.mgf: ResultExportGcUtility.WriteAsMgf(sw, ms1DecResults[j], mspDB, alignedSpots[j]); break;
                        case ExportSpectraFileFormat.txt: ResultExportGcUtility.WriteAsTxt(sw, ms1DecResults[j], mspDB, alignedSpots[j]); break;
                        default: break;
                    }
                }
            }
        }

        public static void ParameterExport(ObservableCollection<AnalysisFileBean> analysisFiles, AnalysisParamOfMsdialGcms param, string filePath)
        {
            var binaryfile = System.IO.Path.GetDirectoryName(filePath) + "\\" +
                System.IO.Path.GetFileNameWithoutExtension(filePath) + ".med";
            MessagePackHandler.SaveToFile<AnalysisParamOfMsdialGcms>(param, binaryfile);
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
            {
                sw.WriteLine(param.MsdialVersionNumber);
                sw.WriteLine();
                sw.WriteLine("#Data type");
                sw.WriteLine("Data type\t" + param.DataType);
                sw.WriteLine("Ion mode\t" + param.IonMode);
                sw.WriteLine("Accuracy type\t" + param.AccuracyType);

                sw.WriteLine("#Data collection parameters");
                sw.WriteLine("Retention time begin\t" + param.RetentionTimeBegin);
                sw.WriteLine("Retention time end\t" + param.RetentionTimeEnd);
                sw.WriteLine("Mass range begin\t" + param.MassRangeBegin);
                sw.WriteLine("Mass range end\t" + param.MassRangeEnd);
                sw.WriteLine();

                sw.WriteLine("#Data processing");
                sw.WriteLine("Number of threads\t" + param.NumThreads.ToString());
                sw.WriteLine();

                sw.WriteLine("#Peak detection parameters");
                sw.WriteLine("Smoothing method\t" + param.SmoothingMethod.ToString());
                sw.WriteLine("Smoothing level\t" + param.SmoothingLevel);
                sw.WriteLine("Average peak width\t" + param.AveragePeakWidth);
                sw.WriteLine("Minimum peak height\t" + param.MinimumAmplitude);
                sw.WriteLine("Mass slice width\t" + param.MassSliceWidth);
                sw.WriteLine("Mass accuracy\t" + param.MassAccuracy);
                sw.WriteLine();

                sw.WriteLine("#MS1Dec parameters");
                sw.WriteLine("Sigma window value\t" + param.SigmaWindowValue);
                sw.WriteLine("Amplitude cut off\t" + param.AmplitudeCutoff);
                sw.WriteLine();

                sw.WriteLine("#identification");
                sw.WriteLine("MSP file\t" + param.MspFilePath);
                sw.WriteLine("RI index file\tsee last");
                sw.WriteLine("Retention type\t" + param.RetentionType);
                sw.WriteLine("RI compound\t" + param.RiCompoundType);
                sw.WriteLine("Retention time tolerance\t" + param.RetentionTimeLibrarySearchTolerance);
                sw.WriteLine("Retention index tolerance\t" + param.RetentionIndexLibrarySearchTolerance);
                sw.WriteLine("EI similarity library tolerance\t" + param.EiSimilarityLibrarySearchCutOff);
                sw.WriteLine("Identification score cut off\t" + param.IdentificationScoreCutOff);
                sw.WriteLine("Use retention information for scoring\t" + param.IsUseRetentionInfoForIdentificationScoring);
                sw.WriteLine("Use retention information for filtering\t" + param.IsUseRetentionInfoForIdentificationFiltering);
                sw.WriteLine("Use quant masses defined in MSP format file\t" + param.IsReplaceQuantmassByUserDefinedValue);
                sw.WriteLine();

                sw.WriteLine("#Alignment parameters setting");
                if (analysisFiles != null && analysisFiles.Count > 0)
                    sw.WriteLine("Reference file\t" + analysisFiles[param.AlignmentReferenceFileID].AnalysisFilePropertyBean.AnalysisFilePath);
                sw.WriteLine("Retention time\t" + param.AlignmentIndexType);
                sw.WriteLine("Retention index tolerance\t" + param.RetentionIndexAlignmentTolerance);
                sw.WriteLine("Retention time tolerance\t" + param.RetentionTimeAlignmentTolerance);
                sw.WriteLine("EI similarity tolerance\t" + param.EiSimilarityAlignmentCutOff);
                sw.WriteLine("Retention time factor\t" + param.RetentionTimeAlignmentFactor);
                sw.WriteLine("EI similarity factor\t" + param.EiSimilarityAlignmentFactor);
                sw.WriteLine("Identification after alignment\t" + param.IsIdentificationOnlyPerformedForAlignmentFile);
                sw.WriteLine("Gap filling by compulsion\t" + param.IsForceInsertForGapFilling);
                sw.WriteLine("Basepeak mz selected as the representative quant mass\t" + param.IsRepresentativeQuantMassBasedOnBasePeakMz);
                sw.WriteLine();

                sw.WriteLine("#Filtering setting");
                sw.WriteLine("Peak count filter\t" + param.PeakCountFilter);
                sw.WriteLine("N% detected in at least one group\t" + param.NPercentDetectedInOneGroup);
                //sw.WriteLine("QC at least filter\t" + param.QcAtLeastFilter.ToString());
                sw.WriteLine("Remove feature based on peak height fold-change\t" + param.IsRemoveFeatureBasedOnPeakHeightFoldChange);
                sw.WriteLine("Sample max / blank average\t" + param.SampleMaxOverBlankAverage);
                sw.WriteLine("Sample average / blank average\t" + param.SampleAverageOverBlankAverage);
                sw.WriteLine("Keep identified and annotated metabolites\t" + param.IsKeepIdentifiedMetaboliteFeatures);
                sw.WriteLine("Keep removable features and assign the tag for checking\t" + param.IsKeepRemovableFeaturesAndAssignedTagForChecking);
                //sw.WriteLine("Replace true zero values with 1/2 of minimum peak height over all samples\t" + param.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples);

                sw.WriteLine();


               

                sw.WriteLine("#RI dictionary (analysis file path / ri dictionary file path)");
                foreach (var query in param.FileIdRiInfoDictionary) {
                    var key = query.Key;
                    var filepath = analysisFiles.Where(n => n.AnalysisFilePropertyBean.AnalysisFileId == key).ToList()[0];
                    sw.WriteLine(filepath + "\t" + query.Value.DictionaryFilePath);
                }
                sw.WriteLine();
            }
        }

        public static void MsfinderTagExport(MainWindow mainWindow, List<MS1DecResult> ms1DecResults, int ms1DecID, MatExportOption exportOption)
        {
            var dt = DateTime.Now;
            var timeString = dt.Year.ToString() + "_" + dt.Month.ToString() + "_" + dt.Day.ToString() + "_" + dt.Hour.ToString() + dt.Minute.ToString();
            var fileString = mainWindow.AlignmentFiles[mainWindow.FocusedAlignmentFileID].FileName;

            var currentSpotID = mainWindow.FocusedAlignmentPeakID;
            var fs = mainWindow.AlignViewDecFS;
            var seekPoints = mainWindow.AlignViewDecSeekPoints;

            for (int i = 0; i < ms1DecResults.Count; i++)
            {
                var result = ms1DecResults[i];

                if (exportOption == MatExportOption.OnlyFocusedPeak && ms1DecID != i) continue;
                if (exportOption == MatExportOption.UnknownPeaks && result.MspDbID >= 0) continue;
                if (exportOption == MatExportOption.IdentifiedPeaks && result.MspDbID < 0) continue;

                var filePath = mainWindow.ProjectProperty.MsAnnotationTagsFolderPath + "\\" + 
                    "MS1DecID " + result.Ms1DecID + "_" + Math.Round(result.RetentionTime, 2).ToString() + "_" + 
                    Math.Round(result.RetentionIndex, 4).ToString() + "_" + timeString + "_" + fileString + "." + SaveFileFormat.mat;

                msfinderTagExport(mainWindow, filePath, result);
            }

            mainWindow.FocusedAlignmentMs1DecID = currentSpotID;
        }

        public static void MsfinderTagExport(MainWindow mainWindow, string filePath, MS1DecResult ms1DecResult)
        {
            msfinderTagExport(mainWindow, filePath, ms1DecResult);
        }

        private static void msfinderTagExport(MainWindow mainWindow, string filePath, MS1DecResult result)
        {
            var projectProp = mainWindow.ProjectProperty;
            var mspDB = mainWindow.MspDB;

            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
            {
                sw.Write("NAME: ");
                sw.WriteLine(result.ScanNumber + "-" + result.RetentionTime + "-" + result.RetentionIndex + "-" + result.BasepeakMz);

                sw.WriteLine("RETENTIONTIME: " + result.RetentionTime);
                sw.WriteLine("RETENTIONINDEX: " + result.RetentionIndex);
                sw.WriteLine("QUANTMASS: " + result.BasepeakMz);

                var precursorMz = result.BasepeakMz;
                if (result.Spectrum.Count > 0) precursorMz = (float)result.Spectrum.Max(n => n.Mz);

                sw.WriteLine("PRECURSORMZ: " + precursorMz);

                sw.WriteLine("PRECURSORTYPE: " + "[M-CH3]+.");

                sw.WriteLine("IONMODE: " + "Positive");
                sw.WriteLine("SPECTRUMTYPE: Centroid");
                sw.WriteLine("INTENSITY: " + result.BasepeakHeight);

                sw.WriteLine("INCHIKEY: " + MspDataRetrieve.GetInChIKey(result.MspDbID, mspDB));
                sw.WriteLine("SMILES: " + MspDataRetrieve.GetSMILES(result.MspDbID, mspDB));
                sw.WriteLine("FORMULA: " + MspDataRetrieve.GetFormula(result.MspDbID, mspDB));

                if (projectProp.FinalSavedDate != null) {
                    sw.WriteLine("DATE: " + projectProp.FinalSavedDate.Date);
                }

                if (projectProp.Authors != null && projectProp.Authors != string.Empty) {
                    sw.WriteLine("AUTHORS: " + projectProp.Authors);
                }

                if (projectProp.License != null && projectProp.License != string.Empty) {
                    sw.WriteLine("LICENSE: " + projectProp.License);
                }

                if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty) {
                    sw.WriteLine("COLLISIONENERGY: " + projectProp.CollisionEnergy);
                }

                if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty) {
                    sw.WriteLine("INSTRUMENTTYPE: " + projectProp.InstrumentType);
                }

                if (projectProp.Instrument != null && projectProp.Instrument != string.Empty) {
                    sw.WriteLine("INSTRUMENT: " + projectProp.Instrument);
                }

                if (projectProp.Comment != null && projectProp.Comment != string.Empty) {
                    sw.WriteLine("COMMENT: " + projectProp.Comment);
                }

                sw.WriteLine("MSTYPE: MS1");
                sw.WriteLine("Num Peaks: " + result.Spectrum.Count);

                for (int i = 0; i < result.Spectrum.Count; i++)
                    sw.WriteLine(Math.Round(result.Spectrum[i].Mz, 5) + "\t" + Math.Round(result.Spectrum[i].Intensity, 0));

                sw.WriteLine("MSTYPE: MS2");
                sw.WriteLine("Num Peaks: " + result.Spectrum.Count);

                for (int i = 0; i < result.Spectrum.Count; i++)
                    sw.WriteLine(Math.Round(result.Spectrum[i].Mz, 5) + "\t" + Math.Round(result.Spectrum[i].Intensity, 0));
            }
        }

        /// <summary>
        /// This is the function to export a target peak spot as Mrmprobs reference format.
        /// The format will be copied to clipboard.
        /// The parameters are based on the current setting.
        /// /// </summary>
        public static void CopyToClipboardMrmprobsRef(MS1DecResult ms1DecResult, List<MspFormatCompoundInformationBean> mspDB, AnalysisParamOfMsdialGcms param)
        {
            if (param.MpMs1Tolerance == 0) {
                param.MpMs1Tolerance = 0.2F;
                param.MpMs2Tolerance = 0.2F;
                param.MpRtTolerance = 0.5F;
                param.MpTopN = 5;
                param.MpIsIncludeMsLevel1 = true;
                param.MpIsUseMs1LevelForQuant = true;
                param.MpIsFocusedSpotOutput = true;
                param.MpIsReferenceBaseOutput = true;
            }

            if (ms1DecResult.MspDbID < 0) return;

            var mpRtTol = (float)Math.Round(param.MpRtTolerance, 4);
            var mpMs1Tol = (float)Math.Round(param.MpMs1Tolerance, 6);
            var mpTopN = param.MpTopN;

            var compName = MspDataRetrieve.GetCompoundName(ms1DecResult.MspDbID, mspDB);
            var probsName = stringReplaceForWindowsAcceptableCharacters(compName);
            var rtBegin = Math.Max(Math.Round(ms1DecResult.RetentionTime - mpRtTol, 2), 0);
            var rtEnd = Math.Round(ms1DecResult.RetentionTime + mpRtTol, 2);
            var rt = Math.Round(ms1DecResult.RetentionTime, 2);

            if (param.MpIsReferenceBaseOutput) {
                if (mspDB == null || mspDB.Count - 1 < ms1DecResult.MspDbID) return;
                copyMrmprobsReferenceFormat(probsName, rt, rtBegin, rtEnd, mpMs1Tol, mpTopN, mspDB[ms1DecResult.MspDbID]);
            }
            else {
                copyMrmprobsReferenceFormat(probsName, rt, rtBegin, rtEnd, mpMs1Tol, mpTopN, ms1DecResult);
            }
        }

        private static void copyMrmprobsReferenceFormat(string name, double rt, double rtBegin, double rtEnd,
            double ms1Tolerance, int topN, MS1DecResult result)
        {
            if (result.Spectrum.Count == 0) return;

            var quantMass = Math.Round(result.BasepeakMz, 4);
            var quantIntensity = result.BasepeakHeight;

            var text = string.Empty;

            text += name + "\t" + quantMass + "\t" + quantMass + "\t" + rt
                + "\t" + "100" + "\t" + rtBegin + "\t" + rtEnd + "\t" + ms1Tolerance
                + "\t" + ms1Tolerance + "\t" + 1 + "\t" + "NA" + "\r\n";

            var massSpec = result.Spectrum.OrderByDescending(n => n.Intensity).ToList();
            for (int i = 0; i < massSpec.Count; i++) {

                if (i > topN - 1) break;

                var mass = Math.Round(massSpec[i].Mz, 4);
                var intensity = massSpec[i].Intensity;

                if (Math.Abs(mass - quantMass) < ms1Tolerance) continue;

                var tqRatio = Math.Round(intensity / quantIntensity * 100, 0);
                if (tqRatio < 0.5) tqRatio = 1;
                if (tqRatio == 100) tqRatio = 99; // 100 is used just once for the target (quantified) m/z trace. Otherwise, non-100 value should be used.

                text += name + "\t" + mass + "\t" + mass + "\t" + rt + "\t"
                    + tqRatio + "\t" + rtBegin + "\t" + rtEnd + "\t" + ms1Tolerance + "\t"
                    + ms1Tolerance + "\t" + 1 + "\t" + "NA" + "\r\n";
            }
            Clipboard.SetDataObject(text, true);
        }

        private static void copyMrmprobsReferenceFormat(string name, double rt, double rtBegin, double rtEnd,
            double ms1Tolerance, int topN, MspFormatCompoundInformationBean mspLib)
        {
            if (mspLib.MzIntensityCommentBeanList.Count == 0) return;

            var massSpec = mspLib.MzIntensityCommentBeanList.OrderByDescending(n => n.Intensity).ToList();
            var quantMass = Math.Round(massSpec[0].Mz, 4);
            var quantIntensity = massSpec[0].Intensity;

            var text = string.Empty;

            text += name + "\t" + quantMass + "\t" + quantMass + "\t" + rt
                + "\t" + "100" + "\t" + rtBegin + "\t" + rtEnd + "\t" + ms1Tolerance
                + "\t" + ms1Tolerance + "\t" + 1 + "\t" + "NA" + "\r\n";

            
            for (int i = 0; i < massSpec.Count; i++) {

                if (i > topN - 1) break;

                var mass = Math.Round(massSpec[i].Mz, 4);
                var intensity = massSpec[i].Intensity;

                if (Math.Abs(mass - quantMass) < ms1Tolerance) continue;

                var tqRatio = Math.Round(intensity / quantIntensity * 100, 0);
                if (tqRatio < 0.5) tqRatio = 1;
                if (tqRatio == 100) tqRatio = 99; // 100 is used just once for the target (quantified) m/z trace. Otherwise, non-100 value should be used.

                text += name + "\t" + mass + "\t" + mass + "\t" + rt + "\t"
                    + tqRatio + "\t" + rtBegin + "\t" + rtEnd + "\t" + ms1Tolerance + "\t"
                    + ms1Tolerance + "\t" + 1 + "\t" + "NA" + "\r\n";
            }
            Clipboard.SetDataObject(text, true);
        }

        /// <summary>
        /// This is the export function for the output of MRMRPROBS reference format for identified spots.
        /// The reference is made from the information of exprerimental spectra instead of the reference spectra.
        /// </summary>
        public static void ExportSpectraAsMrmprobsFormat(string filepath, List<MS1DecResult> ms1DecResults, int focusedMs1DecID,
            double rtTolerance, double ms1Tolerance, List<MspFormatCompoundInformationBean> mspDB, int topN = 5, bool isReferenceBase = true)
        {
            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII)) {

                writeHeaderAsMrmprobsReferenceFormat(sw);

                if (isReferenceBase == true) {
                    if (mspDB == null || mspDB.Count == 0) return;
                    if (focusedMs1DecID == -1) { // it means all of identified spots will be exported.
                        foreach (var result in ms1DecResults) {
                            if (result.MspDbID < 0) continue;

                            var compName = MspDataRetrieve.GetCompoundName(result.MspDbID, mspDB);
                            var probsName = stringReplaceForWindowsAcceptableCharacters(compName + "_" + result.Ms1DecID);
                            var rtBegin = Math.Max(Math.Round(result.RetentionTime - rtTolerance, 2), 0);
                            var rtEnd = Math.Round(result.RetentionTime + rtTolerance, 2);
                            var rt = Math.Round(result.RetentionTime, 2);

                            writeFieldsAsMrmprobsReferenceFormat(sw, probsName, rt, rtBegin, rtEnd, ms1Tolerance, topN, mspDB[result.MspDbID]);
                        }
                    }
                    else {
                        var result = ms1DecResults[focusedMs1DecID];
                        if (result.MspDbID < 0) return;
                        var compName = MspDataRetrieve.GetCompoundName(result.MspDbID, mspDB);
                        var probsName = stringReplaceForWindowsAcceptableCharacters(compName + "_" + result.Ms1DecID);
                        var rtBegin = Math.Max(Math.Round(result.RetentionTime - rtTolerance, 2), 0);
                        var rtEnd = Math.Round(result.RetentionTime + rtTolerance, 2);
                        var rt = Math.Round(result.RetentionTime, 2);

                        writeFieldsAsMrmprobsReferenceFormat(sw, probsName, rt, rtBegin, rtEnd, ms1Tolerance, topN, mspDB[result.MspDbID]);
                    }
                }
                else {
                    if (focusedMs1DecID == -1) { // it means all of identified spots will be exported.
                        foreach (var result in ms1DecResults) {
                            //if (result.MspDbID < 0) continue;

                            var compName = MspDataRetrieve.GetCompoundName(result.MspDbID, mspDB);
                            var probsName = stringReplaceForWindowsAcceptableCharacters(compName + "_" + result.Ms1DecID);
                            var rtBegin = Math.Max(Math.Round(result.RetentionTime - rtTolerance, 2), 0);
                            var rtEnd = Math.Round(result.RetentionTime + rtTolerance, 2);
                            var rt = Math.Round(result.RetentionTime, 2);

                            writeFieldsAsMrmprobsReferenceFormat(sw, probsName, rt, rtBegin, rtEnd, ms1Tolerance, topN, result);
                        }
                    }
                    else {
                        var result = ms1DecResults[focusedMs1DecID];
                        var compName = MspDataRetrieve.GetCompoundName(result.MspDbID, mspDB);
                        var probsName = stringReplaceForWindowsAcceptableCharacters(compName + "_" + result.Ms1DecID);
                        var rtBegin = Math.Max(Math.Round(result.RetentionTime - rtTolerance, 2), 0);
                        var rtEnd = Math.Round(result.RetentionTime + rtTolerance, 2);
                        var rt = Math.Round(result.RetentionTime, 2);

                        writeFieldsAsMrmprobsReferenceFormat(sw, probsName, rt, rtBegin, rtEnd, ms1Tolerance, topN, result);
                    }
                }
            }
        }


        private static void writeFieldsAsMrmprobsReferenceFormat(StreamWriter sw, string name, double rt, double rtBegin, double rtEnd,
            double ms1Tolerance, int topN, MS1DecResult result)
        {
            if (result.Spectrum.Count == 0) return;

            var quantMass = Math.Round(result.BasepeakMz, 4);
            var quantIntensity = result.BasepeakHeight;

            writeAsMrmprobsReferenceFormat(sw, name, quantMass, quantMass, rt, 100, rtBegin, rtEnd, ms1Tolerance, ms1Tolerance, 1, "NA");

            var massSpec = result.Spectrum.OrderByDescending(n => n.Intensity).ToList();
            for (int i = 0; i < massSpec.Count; i++) {
                
                if (i > topN - 1) break;

                var mass = Math.Round(massSpec[i].Mz, 4);
                var intensity = massSpec[i].Intensity;

                if (Math.Abs(mass - quantMass) < ms1Tolerance) continue;

                var tqRatio = Math.Round(intensity / quantIntensity * 100, 0);
                if (tqRatio < 0.5) tqRatio = 1;
                if (tqRatio == 100) tqRatio = 99; // 100 is used just once for the target (quantified) m/z trace. Otherwise, non-100 value should be used.
                writeAsMrmprobsReferenceFormat(sw, name, mass, mass, rt, tqRatio, rtBegin, rtEnd, ms1Tolerance, ms1Tolerance, 1, "NA");
            }
        }

        private static void writeFieldsAsMrmprobsReferenceFormat(StreamWriter sw, string name, double rt, double rtBegin, double rtEnd,
            double ms1Tolerance, int topN, MspFormatCompoundInformationBean mspLib)
        {
            if (mspLib.MzIntensityCommentBeanList.Count == 0) return;
            var massSpec = mspLib.MzIntensityCommentBeanList.OrderByDescending(n => n.Intensity).ToList();

            var quantMass = Math.Round(massSpec[0].Mz, 4);
            var quantIntensity = massSpec[0].Intensity;

            writeAsMrmprobsReferenceFormat(sw, name, quantMass, quantMass, rt, 100, rtBegin, rtEnd, ms1Tolerance, ms1Tolerance, 1, "NA");

            for (int i = 1; i < massSpec.Count; i++) {

                if (i > topN - 1) break;

                var mass = Math.Round(massSpec[i].Mz, 4);
                var intensity = massSpec[i].Intensity;

                if (Math.Abs(mass - quantMass) < ms1Tolerance) continue;

                var tqRatio = Math.Round(intensity / quantIntensity * 100, 0);
                if (tqRatio < 0.5) tqRatio = 1;
                if (tqRatio == 100) tqRatio = 99; // 100 is used just once for the target (quantified) m/z trace. Otherwise, non-100 value should be used.
                writeAsMrmprobsReferenceFormat(sw, name, mass, mass, rt, tqRatio, rtBegin, rtEnd, ms1Tolerance, ms1Tolerance, 1, "NA");
            }
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

        private static void writeAsMrmprobsReferenceFormat(StreamWriter sw, string name, double precursorMz, double productMz, double rt, double tqRatio, double rtBegin, double rtEnd, double ms1Tolerance, double ms2Tolerance, int msLevel, string compoundClass)
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

        private static double getCollisionEnergy(string ce)
        {
            if (ce == null || ce == string.Empty) return -1;
            string figure = string.Empty;
            double ceValue = 0.0;
            for (int i = 0; i < ce.Length; i++) {
                if (Char.IsNumber(ce[i]) || ce[i] == '.') {
                    figure += ce[i];
                }
                else {
                    double.TryParse(figure, out ceValue);
                    return ceValue;
                }
            }
            double.TryParse(figure, out ceValue);
            return ceValue;
        }

        private static void exportResultMztabFormet(string file, AlignmentResultBean alignmentResult, ProjectPropertyBean projectProp,
        List<MS1DecResult> ms1DecResults, ObservableCollection<AnalysisFileBean> analysisFiles,
        List<MspFormatCompoundInformationBean> mspDB, AnalysisParamOfMsdialGcms param, string exportType, bool blankFilter, bool replaceZeroToHalf)
        {
            using (StreamWriter sw = new StreamWriter(file, false, Encoding.ASCII))
            {
                //Meta data section 
                var mtdPrefix = "MTD";
                var commentPrefix = "COM";
                var mztabVersion = "2.0.0-M"; //Fixed

                var mzTabExporterVerNo = "1.05";
                var mzTabExporterName = "MS-DIAL mzTab exporter";
                var mztabExporter = "[,, " + mzTabExporterName + ", " + mzTabExporterVerNo + "]";

                var mztabId = Path.GetFileNameWithoutExtension(file); // using file name 
                var software = "[MS, MS:1003082, MS-DIAL, " + param.MsdialVersionNumber.Replace("MS-DIAL ver.", "") + "]";  //Fixed
                var quantificationMethod = "[,, Label-free raw feature quantitation, ]";  //  GC 

                var cvList = new List<List<string>>(); // cv list
                var cvItem1 = new List<string>() { "MS", "PSI-MS controlled vocabulary", "20-06-2018", "https://www.ebi.ac.uk/ols/ontologies/ms" };
                cvList.Add(cvItem1);
                //var cvItem2 = new List<string>() { "UO", "Units of Measurement Ontology", "2017-09-25", "http://purl.obolibrary.org/obo/uo.owl" };
                //cvList.Add(cvItem2);

                var database1 = new List<string>() { "[,, User-defined MSP library file, ]", "MSP", "Unknown", "file://" + param.MspFilePath.Replace("\\", "/").Replace(" ", "%20")}; // MSP DB match 
                var database2 = new List<string>() { "[,, no database, null ]", "null", "Unknown", "null" };  

                var database = new List<List<string>>();
                database.Add(database1);
                database.Add(database2);

                var idConfidenceDefault = "[,, MS-DIAL algorithm matching score, ]";
                var idConfidenceManual = "";

                var idConfidenceMeasure = new List<string>() {
                    idConfidenceDefault, "[,, Retention time similarity, ]", "[,, Retention index similarity, ]", "[,, Total spectrum similarity, ]",
                    "[,, Dot product, ]", "[,, Reverse dot product, ]", "[,, Fragment presence (%), ]" }; // The order must be fixed !!

                // if manual curation is true
                var manuallyAnnotation = new List<bool>(alignmentResult.AlignmentPropertyBeanCollection.Select(n => n.IsManuallyModifiedForAnnotation));
                if (manuallyAnnotation.Contains(true))
                {
                    idConfidenceManual = "[MS, MS:1001058, quality estimation by manual validation, ]";
                    //idConfidenceMeasure.Add(idConfidenceManual);
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
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "ms_run[" + (i + 1) + "]-scan_polarity[1]", "[MS, MS: 1000130, positive scan,]" })); // GC case
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
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "study_variable[" + (i + 1) + "]-assay_refs", string.Join("| ", studyVariableAssayRefGroup )}));
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
                        if (param.IsNormalizeIS)
                        {
                            normalizedComment = "Data is normalized by internal standerd SML ID(alighnment ID)";
                        }
                        if (param.IsNormalizeSplash)
                        {
                            normalizedComment = "Data is normalized by SPLASH internal standard";
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
                sw.WriteLine("");

                //SML section
                var quantitiveNormalizationBool = false;
                if (exportType == "Normalized")
                {
                    if (param.IsNormalizeIS || param.IsNormalizeSplash)
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
                for (int i = 0; i < analysisFiles.Count; i++) sw.Write("abundance_assay[" + (i + 1)+"]"+"\t");
                for (int i = 0; i < studyVariable.Count; i++) sw.Write("abundance_study_variable[" + (i + 1) + "]" + "\t");
                for (int i = 0; i < studyVariable.Count; i++) sw.Write("abundance_variation_study_variable[" + (i + 1) + "]" + "\t");

                // if want to add optional column, header discribe here 
                //internalStanderdAlighnmentID

                if (quantitiveNormalizationBool)
                {
                        sw.Write("opt_global_internalStanderdSMLID" + "\t");
                }
                //


                sw.WriteLine("");

                //header end
                //SML data section

                var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
                if (alignedSpots == null || alignedSpots.Count == 0) return;

                var isNormalized = exportType == "Normalized" ? true : false;
                var mode = isNormalized ? BarChartDisplayMode.NormalizedHeight
                        : exportType == "Height" ? BarChartDisplayMode.OriginalHeight
                                                 : BarChartDisplayMode.OriginalArea;


                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                    // if (alignedSpots[i].MetaboliteName == "") continue;  // without unknown

                    ResultExportGcUtility.WriteDataMatrixMztabSMLMetaData(sw, alignedSpots[i], mspDB, param, database, idConfidenceDefault, idConfidenceManual);

                    // Replace true zero values with 1/2 of minimum peak height over all samples
                    var nonZeroMin = getInterpolatedValueForMissingValue(alignedSpots[i].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                    var statsList = MsDialStatistics.AverageStdevProperties(alignedSpots[i], analysisFiles, projectProp, mode);
                    ResultExportGcUtility.WriteMztabSMLData(sw, alignedSpots[i].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin, statsList);
                    // if want to add optional column, header discribe here 
                    //internalStanderdAlighnmentID
                    if (quantitiveNormalizationBool)
                    {
                            var isID = "null";
                            if (alignedSpots[i].InternalStandardAlignmentID != -1)
                            {
                                isID = alignedSpots[i].InternalStandardAlignmentID.ToString();
                            }
                            sw.Write(isID + "\t");
                    }
                    //
                    sw.WriteLine("");

                }
                //
                sw.WriteLine("");
                //SML section end

                //SMF section
                //header
                headers = new List<string>() {
                "SFH","SMF_ID","SME_ID_REFS","SME_ID_REF_ambiguity_code","adduct_ion","isotopomer","exp_mass_to_charge",
                  "charge","retention_time_in_seconds","retention_time_in_seconds_start","retention_time_in_seconds_end"
                };
                for (int i = 0; i < headers.Count; i++) sw.Write(headers[i] + "\t");
                for (int i = 0; i < analysisFiles.Count; i++) sw.Write("abundance_assay[" + (i + 1) + "]" + "\t");

                sw.WriteLine("");

                //header end
                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                    // if (alignedSpots[i].MetaboliteName == "") continue;  // without unknown

                    var smfPrefix = "SMF";
                    var smfID = alignedSpots[i].AlignmentID;
                    var smeIDrefs = "null";
                    var smeIDrefAmbiguity_code = "null";
                    var adductIons = "[M]1+"; //GC case
                    var isotopomer = "null";
                    var expMassToCharge = alignedSpots[i].QuantMass.ToString(); // ???
                    var charge = "1"; //GC case
                    var retentionTimeSeconds = alignedSpots[i].CentralRetentionTime * 60;
                    var retentionTimeStart = (alignedSpots[i].CentralRetentionTime - (alignedSpots[i].AveragePeakWidth / 2)) * 60;
                    var retentionTimeEnd = (alignedSpots[i].CentralRetentionTime + (alignedSpots[i].AveragePeakWidth / 2)) * 60;

                    if (alignedSpots[i].MetaboliteName != "")
                    {
                        smeIDrefs = alignedSpots[i].AlignmentID.ToString();
                    }
                    var metadata = new List<string>() {
                    smfPrefix,smfID.ToString(), smeIDrefs.ToString(), smeIDrefAmbiguity_code,
                    adductIons, isotopomer, expMassToCharge, charge , retentionTimeSeconds.ToString(),retentionTimeStart.ToString(),retentionTimeEnd.ToString()
                    };
                    sw.Write(String.Join("\t", metadata) + "\t");

                    // Replace true zero values with 1/2 of minimum peak height over all samples
                    var nonZeroMin = getInterpolatedValueForMissingValue(alignedSpots[i].AlignedPeakPropertyBeanCollection, replaceZeroToHalf, exportType);
                    var statsList = MsDialStatistics.AverageStdevProperties(alignedSpots[i], analysisFiles, projectProp, mode);
                    ResultExportGcUtility.WriteData(sw, alignedSpots[i].AlignedPeakPropertyBeanCollection, exportType, replaceZeroToHalf, nonZeroMin);
                }
                //comment
                sw.WriteLine("COM\t \"retention_time_in_seconds_start\" and \"retention_time_in_seconds_end\" were calculated using average peak width");

                sw.WriteLine("");

                //SMF section end

                // SME section
                    //header
                var headersSME01 = new List<string>() {
                "SEH","SME_ID","evidence_input_id","database_identifier","chemical_formula","smiles","inchi",
                  "chemical_name","uri","derivatized_form","adduct_ion","exp_mass_to_charge","charge", "theoretical_mass_to_charge",
                  "spectra_ref","identification_method","ms_level"
                };
                for (int i = 0; i < headersSME01.Count; i++) sw.Write(headersSME01[i] + "\t");
                for (int i = 0; i < idConfidenceMeasure.Count; i++) sw.Write("id_confidence_measure[" + (i + 1) + "]" + "\t");
                sw.WriteLine("rank");
                    //header end
                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (blankFilter && alignedSpots[i].IsBlankFiltered) continue;
                    if (alignedSpots[i].MetaboliteName == "") continue;

                    var repfileId = alignedSpots[i].RepresentativeFileID;
                    //var spectraRefscanNo = ms1DecResults[i].ScanNumber;
                    var spectraRefscanNo = alignedSpots[i].AlignedPeakPropertyBeanCollection[repfileId].Ms1ScanNumber;

                    //// to get scanNo in aligned spots
                    ///var spot = alignedSpots[i];
                    ///var properties = spot.AlignedPeakPropertyBeanCollection;
                    ///var repfileId = spot.RepresentativeFileID;
                    ///var ms1DecSeekpoint = spot.AlignedPeakPropertyBeanCollection[repfileId].SeekPoint;
                    ///var filePath = analysisFiles[repfileId].AnalysisFilePropertyBean.DeconvolutionFilePath;
                    ///var ms1DecResult = DataStorageGcUtility.ReadMS1DecResult(filePath, ms1DecSeekpoint);
                    ///var scanNumber = ms1DecResult.ScanNumber;

                    ResultExportGcUtility.WriteDataMatrixMztabSMEData(sw, alignedSpots[i], mspDB, param, database, spectraRefscanNo, analysisFiles, idConfidenceDefault, idConfidenceManual);

                    sw.WriteLine("");

                }
                //SME section end
            }
        }
    }
}

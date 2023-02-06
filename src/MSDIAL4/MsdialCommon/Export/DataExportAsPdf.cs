using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Msdial.Lcms.Dataprocess.Utility;
using Msdial.Lcms.Dataprocess.Algorithm;
// using ChartDrawing;
using System.Windows.Media;
using Rfx.Riken.OsakaUniv;
using Rfx.Riken.OsakaUniv.RetentionTimeCorrection;
using PdfExporter;
using Msdial.Common.Utility;

namespace Msdial.Common.Export
{
    public class DataExportAsPdf
    {
        #region Retention time correction
        // export all retention time correction results
        public static void ExportRetentionTimeCorrectionAll(string path, List<AnalysisFileBean> analysisFiles, AnalysisParametersBean param, RetentionTimeCorrectionParam rtParam,
             List<CommonStdData> detectedStdList) {

            var colorDict = Utility.MsdialDataHandleUtility.GetClassIdColorDictionary(analysisFiles, MsdialDataHandleUtility.MsdialDefaultSolidColorBrushList);
            var analysisFileCollection = new ObservableCollection<AnalysisFileBean>(analysisFiles);
            // A4, landscape, 300 DPI
            var filePath_all = path + "_RetentionTimeCorrection_all.pdf";
            var filePath_summary = path + "_RetentionTimeCorrection_summary.pdf";
            var exporter = ExporterUtility.InitialSetting(filePath_all);
            
            // overlayed RT difference curve
            var dv = RtCorrection.GetDrawVisualOverlayedRtDiff(analysisFiles, param);
            dv.ChangeChartArea(800, 500);
            ExporterUtility.ExportSingleDrawingVisual(exporter, dv.GetChart(), "Overview; First file: " + analysisFiles[0].AnalysisFilePropertyBean.AnalysisFileName, 800, 500);

            foreach (var commonStd in detectedStdList) {
                var dv3 = RtCorrection.GetDrawVisualEachCompoundIntensityPlot(analysisFileCollection, commonStd, colorDict);
                var dv4 = RtCorrection.GetDrawVisualOverlayedEIC(analysisFileCollection, commonStd, param, colorDict);
                var dv5 = RtCorrection.GetDrawVisualOverlayedCorrectedEIC(analysisFileCollection, commonStd, param, colorDict);
                dv3.ChangeChartArea(800, 250);
                dv4.ChangeChartArea(350, 250);
                dv5.ChangeChartArea(350, 250);

                exporter.AddPage();
                exporter.DrawTextToPage(commonStd.Reference.MetaboliteName, 20, 25, 18);
                exporter.DrawFigureFromDrawVisual(dv3.GetChart(), 20, 50, 800, 250, 300, 300);
                exporter.DrawFigureFromDrawVisual(dv4.GetChart(), 20, 320, 350, 300, 300, 300);
                exporter.DrawFigureFromDrawVisual(dv5.GetChart(), 441, 320, 350, 300, 300, 300);
                //   ExporterUtility.ExportDrawingVisual_2columns_1row(exporter, dv3.GetChart(), dv4.GetChart(), commonStd.Reference.MetaboliteName, 800, 500);
            }

            // RT difference curve in each sample
            foreach (var analysisFile in analysisFiles) {
                var dv2 = RtCorrection.GetDrawingSingleRtDiff(analysisFile, param, rtParam, detectedStdList, RtDiffLabel.name, Brushes.Black, 0, 0);
                dv2.ChangeChartArea(800, 500);
                ExporterUtility.ExportSingleDrawingVisual(exporter, dv2.GetChart(), analysisFile.AnalysisFilePropertyBean.AnalysisFileName, 800, 500);
            }

            exporter.SaveDocument();
            exporter.OpenPdf();

            //Export_RetentionTimeCorrection_Summary(filePath_summary, analysisFiles, param, rtParam, detectedStdList);
        }

        // summary exporter, still developing
        public static void Export_RetentionTimeCorrection_Summary(string path, List<AnalysisFileBean> analysisFiles, AnalysisParametersBean param, RetentionTimeCorrectionParam rtParam,
          List<CommonStdData> detectedStdList) {

            if (detectedStdList.Count > 5) return; 
            var colorDict = Utility.MsdialDataHandleUtility.GetClassIdColorDictionary(analysisFiles, MsdialDataHandleUtility.MsdialDefaultSolidColorBrushList);
            var analysisFileCollection = new ObservableCollection<AnalysisFileBean>(analysisFiles);
            // A4, landscape, 300 DPI
            var exporter = ExporterUtility.InitialSetting(path, ExporterUtility.PdfSize.A4, ExporterUtility.Orientation.Portrait);

            // overlayed RT difference curve
            var dv = RtCorrection.GetDrawVisualOverlayedRtDiff(analysisFiles, param);
            dv.ChangeChartArea(500, 200);
            exporter.AddPage();
            exporter.DrawFigureFromDrawVisual(dv.GetChart(), 20, 0, 500, 200, 300, 300);
            var height = 600 / detectedStdList.Count;
            var counter = 0;
            foreach (var commonStd in detectedStdList) {
                var dv4 = RtCorrection.GetDrawVisualOverlayedEIC(analysisFileCollection, commonStd, param, colorDict);
                var dv5 = RtCorrection.GetDrawVisualOverlayedCorrectedEIC(analysisFileCollection, commonStd, param, colorDict);
                dv4.ChangeChartArea(250, height);
                dv5.ChangeChartArea(250, height);

                exporter.DrawFigureFromDrawVisual(dv4.GetChart(), 10, 200 + counter* height, 250, height, 300, 300);
                exporter.DrawFigureFromDrawVisual(dv5.GetChart(), 260, 200 + counter * height, 250, height, 300, 300);
                //   ExporterUtility.ExportDrawingVisual_2columns_1row(exporter, dv3.GetChart(), dv4.GetChart(), commonStd.Reference.MetaboliteName, 800, 500);
                counter++;
            }
            exporter.SaveDocument();
            exporter.OpenPdf();
        }
        #endregion

        public static void ExportNormalizationResults(string path, List<AlignmentPropertyBean> spots, IReadOnlyList<AnalysisFileBean> analysisFiles)
        {
            var width = 800;
            var height = 500;
            var width2 = 650;
            var height2 = 270;
            var dpiX = 120;
            var dpiY = 120;
            var exporter = ExporterUtility.InitialSetting(path, ExporterUtility.PdfSize.A4, ExporterUtility.Orientation.Landscape);
            var fileIdOrderDict = new Dictionary<int, int>();
            var counter = 0;
            foreach (var file in analysisFiles.OrderBy(x => x.AnalysisFilePropertyBean.AnalysisBatch)
                .ThenBy(x => x.AnalysisFilePropertyBean.AnalysisFileAnalyticalOrder)
                .Where(n => n.AnalysisFilePropertyBean.AnalysisFileIncluded))
            {
                counter++;
                fileIdOrderDict[file.AnalysisFilePropertyBean.AnalysisFileId] = counter;
            }

            var isCollected = false;
            foreach (var spot in spots)
            {
                if (spot.MetaboliteName == "" && spot.Comment == "") continue;
                if (spot.MetaboliteName.Contains("w/o")) continue;
                if (spot.MetaboliteName.Contains("RIKEN")) continue;
                //if (spot.AlignmentID > 200) break;

                VariousDrawVisual.GetDrawVisualNormalizationPlot(spot, analysisFiles, fileIdOrderDict, "Normalized intensities plot", "log (ion intensity)", out var dv1, out var dv2,
                    out float qcOriCV, out float qcNormCV, out float sampleOriCV, out float sampleNormCV, out float logQcOri, out float logQcNorm, out float logSampleOri, out float logSampleNorm);
                dv1.ChangeChartArea(width2, height2);
                dv2.ChangeChartArea(width2, height2);
                exporter.AddPage();
                exporter.DrawTextToPage("ID: " + spot.AlignmentID + ", " + spot.MetaboliteName, 40, 20, 15);

                exporter.DrawTextToPage("Non-transformed", 40 + width2, 60, 13);
                exporter.DrawTextToPage("QC CV: " + qcNormCV + "%", 40 + width2, 80, 13);
                exporter.DrawTextToPage("Sample CV: " + sampleNormCV + "%", 40 + width2, 100, 13);

                exporter.DrawTextToPage("Log-transformed", 40 + width2, 140, 13);
                exporter.DrawTextToPage("QC CV: " + logQcNorm + "%", 40 + width2, 160, 13);
                exporter.DrawTextToPage("Sample CV: " + logSampleNorm + "%", 40 + width2, 180, 13);


                exporter.DrawTextToPage("Non-transformed", 40 + width2, 340, 13);
                exporter.DrawTextToPage("QC CV: " + qcOriCV + "%", 40 + width2, 360, 13);
                exporter.DrawTextToPage("Sample CV: " + sampleOriCV + "%", 40 + width2, 380, 13);

                exporter.DrawTextToPage("Log-transformed", 40 + width2, 420, 13);
                exporter.DrawTextToPage("QC CV: " + logQcOri + "%", 40 + width2, 440, 13);
                exporter.DrawTextToPage("Sample CV: " + logSampleOri + "%", 40 + width2, 460, 13);

                //exporter.DrawTextToPage("QC CV: " + qcNormCV+"%", 40 + width2, 60, 13);
                //exporter.DrawTextToPage("Sample CV: " + sampleNormCV + "%", 40 + width2, 80, 13);
                //exporter.DrawTextToPage("QC CV: " + qcOriCV + "%", 40 + width2, 340, 13);
                //exporter.DrawTextToPage("Sample CV: " + sampleOriCV + "%", 40 + width2, 360, 13);
                exporter.DrawFigureFromDrawVisual(dv1.GetChart(), 20, 20, width2, height2, dpiX, dpiY);
                exporter.DrawFigureFromDrawVisual(dv2.GetChart(), 20, 300, width2, height2, dpiX, dpiY);
                
                // GC.Collect();
                //GC.WaitForPendingFinalizers();
                isCollected = true;
            }

            // if isccollected = false, top 100 aligned spots are exported.
            counter = 0;

            var tempSpots = new List<AlignmentPropertyBean>();
            foreach (var spot in spots.OrderByDescending(n => n.AverageValiable)) {
                if ((spot.LibraryID >= 0 || spot.PostIdentificationLibraryID >= 0) && !spot.MetaboliteName.Contains("w/o")) continue;
                tempSpots.Add(spot);
                counter++;
                if (counter > 100) break;
            }
            if (tempSpots.Count > 0) {
                foreach (var spot in tempSpots.OrderBy(n => n.AlignmentID)) {
                    VariousDrawVisual.GetDrawVisualNormalizationPlot(spot, analysisFiles, fileIdOrderDict, "Normalized intensities plot", "log (ion intensity)", out var dv1, out var dv2,
                    out float qcOriCV, out float qcNormCV, out float sampleOriCV, out float sampleNormCV, out float logQcOri, out float logQcNorm, out float logSampleOri, out float logSampleNorm);
                    dv1.ChangeChartArea(width2, height2);
                    dv2.ChangeChartArea(width2, height2);
                    exporter.AddPage();
                    exporter.DrawTextToPage("ID: " + spot.AlignmentID + ", " + spot.MetaboliteName, 40, 20, 15);
                    
                    exporter.DrawTextToPage("Non-transformed", 40 + width2, 60, 13);
                    exporter.DrawTextToPage("QC CV: " + qcNormCV + "%", 40 + width2, 80, 13);
                    exporter.DrawTextToPage("Sample CV: " + sampleNormCV + "%", 40 + width2, 100, 13);

                    exporter.DrawTextToPage("Log-transformed", 40 + width2, 140, 13);
                    exporter.DrawTextToPage("QC CV: " + logQcNorm + "%", 40 + width2, 160, 13);
                    exporter.DrawTextToPage("Sample CV: " + logSampleNorm + "%", 40 + width2, 180, 13);


                    exporter.DrawTextToPage("Non-transformed", 40 + width2, 340, 13);
                    exporter.DrawTextToPage("QC CV: " + qcOriCV + "%", 40 + width2, 360, 13);
                    exporter.DrawTextToPage("Sample CV: " + sampleOriCV + "%", 40 + width2, 380, 13);

                    exporter.DrawTextToPage("Log-transformed", 40 + width2, 420, 13);
                    exporter.DrawTextToPage("QC CV: " + logQcOri + "%", 40 + width2, 440, 13);
                    exporter.DrawTextToPage("Sample CV: " + logSampleOri + "%", 40 + width2, 460, 13);


                    exporter.DrawFigureFromDrawVisual(dv1.GetChart(), 20, 20, width2, height2, dpiX, dpiY);
                    exporter.DrawFigureFromDrawVisual(dv2.GetChart(), 20, 300, width2, height2, dpiX, dpiY);
                }
            }

            exporter.SaveDocument();
            exporter.OpenPdf();            
        }

        public static void ExportPeakAreaAndHightInTargetMode(string path, List<AlignmentPropertyBean> spots, string Name) {
            var dv = VariousDrawVisual.GetDrawVisualIntensityPlot(spots);
            dv.ChangeChartArea(800, 500);
            var exporter = ExporterUtility.InitialSetting(path, ExporterUtility.PdfSize.A4, ExporterUtility.Orientation.Landscape);
            ExporterUtility.ExportSingleDrawingVisual(exporter, dv.GetChart(), "Peak Intensity plot; " + Name, 800, 500);
            exporter.SaveDocument();
            exporter.OpenPdf();
        }
    }
}

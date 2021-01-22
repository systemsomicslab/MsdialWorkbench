using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompMs.Graphics.Core.Base;
using System.Windows.Media;
using Rfx.Riken.OsakaUniv;

namespace Msdial.Common.Utility
{
    public class VariousDrawVisual
    {
        #region peak area and peak height plot for target mode
        public static DrawVisual GetDrawVisualIntensityPlot(List<AlignmentPropertyBean> spots) {
            var area = CompMs.Graphics.Core.Base.Utility.GetDefaultAreaV1("Samples", "Ion intensity");
            area.LabelSpace.Top = 15;
            var title = CompMs.Graphics.Core.Base.Utility.GetDefaultTitleV1(13, "Intensity plot over samples");
            var slist = new SeriesList();
            var numFiles = spots.Count;
            for (int i = 0; i < numFiles; i++) {
                var f = spots[i];
                var brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(250, (byte)(255 * i / numFiles), (byte)(255 * (1 - Math.Abs(i / numFiles - 0.5))), (byte)(255 - 255 * i / numFiles)));
                var s = new Series() {
                    ChartType = ChartType.Line,
                    MarkerType = MarkerType.Circle,
                    MarkerSize = new System.Windows.Size(2, 2),
                    Brush = brush,
                    Pen = new Pen(brush, 1.0),
                    Legend = new Legend() { IsVisible = true, MaxWidth = 500, Position = Position.Right, Text = f.MetaboliteName, InGraphicArea = false }
                };
                foreach(var j in f.AlignedPeakPropertyBeanCollection) {
                    s.AddPoint(j.FileID + 1, j.Variable);
                }
                if (s.Points.Count > 0)
                    slist.Series.Add(s);
            }
            //slist.SetValues();
            var drawing = new DrawVisual(area, title, slist);
            CompMs.Graphics.Core.Base.Utility.SetDrawingMinAndMaxXYConstValue(drawing, 0, slist.MaxX + 1, 0);
            CompMs.Graphics.Core.Base.Utility.SetDrawingMaxYRatio(drawing, 0.05f);
            drawing.Initialize();
            return drawing;
        }

        #endregion

        #region Normalization results
        public static void GetDrawVisualNormalizationPlot(AlignmentPropertyBean spot, IReadOnlyList<AnalysisFileBean> analysisFiles, Dictionary<int, int> fileIdOrderDict,
            string titleLabel, string yaxis, out DrawVisual dv1, out DrawVisual dv2,
            out float qcOri, out float qcNorm, out float sampleOri, out float sampleNorm,
            out float logQcOri, out float logQcNorm, out float logSampleOri, out float logSampleNorm) {
            var area = CompMs.Graphics.Core.Base.Utility.GetDefaultAreaV1("Injection order", yaxis);
            area.LabelSpace.Top = 15;
            var title = CompMs.Graphics.Core.Base.Utility.GetDefaultTitleV1(13, titleLabel);
            var title2 = CompMs.Graphics.Core.Base.Utility.GetDefaultTitleV1(13, "Original intensity plot");
            var slist = new SeriesList();
            var slistOriginal = new SeriesList();
            var markerSize = new System.Windows.Size(3, 3);

            var qcList = new List<AlignedPeakPropertyBean>();
            var blankList = new List<AlignedPeakPropertyBean>();
            var sampleList = new List<AlignedPeakPropertyBean>();
            var sampleListDict = new Dictionary<int, List<AlignedPeakPropertyBean>>();
            foreach (var j in spot.AlignedPeakPropertyBeanCollection) {
                var fileProp = analysisFiles[j.FileID].AnalysisFilePropertyBean;
                if (fileProp.AnalysisFileType == AnalysisFileType.QC) {
                    qcList.Add(j);
                }
                else if (fileProp.AnalysisFileType == AnalysisFileType.Blank) {
                    blankList.Add(j);
                }
                else if (sampleListDict.ContainsKey(fileProp.AnalysisBatch)) {
                    sampleListDict[fileProp.AnalysisBatch].Add(j);
                    sampleList.Add(j);
                }
                else {
                    sampleListDict[fileProp.AnalysisBatch] = new List<AlignedPeakPropertyBean>();
                    sampleList.Add(j);
                }
            }
            var qcArr = qcList.Select(x => (double)x.Variable).ToArray();
            var qcNormArr = qcList.Select(x => (double)x.NormalizedVariable).ToArray();
            var qcOriAve = BasicMathematics.Mean(qcArr);
            var qcNormAve = BasicMathematics.Mean(qcNormArr);
            var qcOriStd = BasicMathematics.Stdev(qcArr);
            var qcNormStd = BasicMathematics.Stdev(qcNormArr);


            qcOri = (float)(Math.Round(qcOriStd / qcOriAve * 100, 2));
            qcNorm = (float)(Math.Round(qcNormStd / qcNormAve * 100, 2));

            var sampleArr = sampleList.Select(x => (double)x.Variable).ToArray();
            var sampleNormArr = sampleList.Select(x => (double)x.NormalizedVariable).ToArray();
            var sampleOriAve = BasicMathematics.Mean(sampleArr);
            var sampleNormAve = BasicMathematics.Mean(sampleNormArr);
            var sampleOriStd = BasicMathematics.Stdev(sampleArr);
            var sampleNormStd = BasicMathematics.Stdev(sampleNormArr);
            sampleOri = (float)(Math.Round(sampleOriStd / sampleOriAve * 100, 2));
            sampleNorm = (float)(Math.Round(sampleNormStd / sampleNormAve * 100, 2));

            var logQcArr = qcArr.Select(x => Math.Log10(x)).ToArray();
            var logQcNormArr = qcNormArr.Select(x => Math.Log10(x)).ToArray();
            var logSampleArr = sampleArr.Select(x => Math.Log10(x)).ToArray();
            var logSampleNormArr = sampleNormArr.Select(x => Math.Log10(x)).ToArray();

            logQcOri = (float)Math.Round(Math.Sqrt(Math.Pow(10, Math.Log(10) * Math.Pow(BasicMathematics.Stdev(logQcArr), 2)) - 1) * 100, 2);
            logQcNorm = (float)Math.Round(Math.Sqrt(Math.Pow(10, Math.Log(10) * Math.Pow(BasicMathematics.Stdev(logQcNormArr), 2)) - 1) * 100, 2);

            logSampleOri = (float)Math.Round(Math.Sqrt(Math.Pow(10, Math.Log(10) * Math.Pow(BasicMathematics.Stdev(logSampleArr), 2)) - 1) * 100, 2);
            logSampleNorm = (float)Math.Round(Math.Sqrt(Math.Pow(10, Math.Log(10) * Math.Pow(BasicMathematics.Stdev(logSampleNormArr), 2)) - 1) * 100, 2);

            var brush = Utility.MsdialDataHandleUtility.MsdialDefaultSolidColorBrushList[0];
            Series s = new Series() {
                ChartType = ChartType.Point,
                MarkerType = MarkerType.Cross,
                MarkerSize = markerSize,
                Brush = brush,
                Pen = new Pen(brush, 1.0),
            };

            Series s2 = new Series() {
                ChartType = ChartType.Point,
                MarkerType = MarkerType.Cross,
                MarkerSize = markerSize,
                Brush = brush,
                Pen = new Pen(brush, 1.0),
            };

            foreach (var peak in qcList) {
                if (!fileIdOrderDict.ContainsKey(peak.FileID)) continue;
                s.AddPoint((float)fileIdOrderDict[peak.FileID], convert2log10(peak.NormalizedVariable));
                s2.AddPoint((float)fileIdOrderDict[peak.FileID], convert2log10(peak.Variable));
            }
            if (s.Points.Count > 0)
                slist.Series.Add(s);
            if (s2.Points.Count > 0)
                slistOriginal.Series.Add(s2);



            brush = Utility.MsdialDataHandleUtility.MsdialDefaultSolidColorBrushList[1];
            s = new Series() {
                ChartType = ChartType.Point,
                MarkerType = MarkerType.Square,
                MarkerSize = markerSize,
                Brush = brush,
                Pen = new Pen(brush, 1.0),
                //Legend = new Legend() { IsVisible = true, MaxWidth = 500, Position = Position.Right, Text = f.MetaboliteName, InGraphicArea = false }
            };

            s2 = new Series() {
                ChartType = ChartType.Point,
                MarkerType = MarkerType.Square,
                MarkerSize = markerSize,
                Brush = brush,
                Pen = new Pen(brush, 1.0),
                //Legend = new Legend() { IsVisible = true, MaxWidth = 500, Position = Position.Right, Text = f.MetaboliteName, InGraphicArea = false }
            };

            foreach (var peak in blankList) {
                if (!fileIdOrderDict.ContainsKey(peak.FileID)) continue;
                s.AddPoint((float)fileIdOrderDict[peak.FileID], convert2log10(peak.NormalizedVariable));
                s2.AddPoint((float)fileIdOrderDict[peak.FileID], convert2log10(peak.Variable));
            }
            if (s.Points.Count > 0)
                slist.Series.Add(s);
            if (s2.Points.Count > 0)
                slistOriginal.Series.Add(s2);


            foreach (var j in sampleListDict.Keys) {
                var sampleList2 = sampleListDict[j];
                var brush2 = Utility.MsdialDataHandleUtility.MsdialDefaultSolidColorBrushList[j + 2];
                s = new Series() {
                    ChartType = ChartType.Line,
                    MarkerType = MarkerType.Circle,
                    MarkerSize = markerSize,
                    Brush = brush2,
                    Pen = new Pen(brush2, 0.5)
                };

                s2 = new Series() {
                    ChartType = ChartType.Line,
                    MarkerType = MarkerType.Circle,
                    MarkerSize = markerSize,
                    Brush = brush2,
                    Pen = new Pen(brush2, 0.5)
                };
                foreach (var peak in sampleList2) {
                    if (!fileIdOrderDict.ContainsKey(peak.FileID)) continue;
                    s.AddPoint((float)fileIdOrderDict[peak.FileID], convert2log10(peak.NormalizedVariable));
                    s2.AddPoint((float)fileIdOrderDict[peak.FileID], convert2log10(peak.Variable));
                }
                if (s.Points.Count > 0)
                    slist.Series.Add(s);
                if (s2.Points.Count > 0)
                    slistOriginal.Series.Add(s2);


            }
            //slist.SetValues();
            dv1 = new DrawVisual(area, title, slist);
            CompMs.Graphics.Core.Base.Utility.SetDrawingMinAndMaxXYConstValue(dv1, 0, slist.MaxX + 1, slist.MinY - 0.5f, slist.MaxY + 0.5f);
            dv1.Initialize();

            dv2 = new DrawVisual(area, title2, slistOriginal);
            CompMs.Graphics.Core.Base.Utility.SetDrawingMinAndMaxXYConstValue(dv2, 0, slistOriginal.MaxX + 1, slistOriginal.MinY - 0.5f, slistOriginal.MaxY + 0.5f);
            // CompMs.Graphics.Core.Base.Utility.SetDrawingMaxYRatio(drawing, 0.05f);
            dv2.Initialize();


        }

        private static float convert2log10(float val)
        {
            if (val <= 1)
                return 0;
            else
                return (float)Math.Log(val);
        }

        #endregion
    }
}

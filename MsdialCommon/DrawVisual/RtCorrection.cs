using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Msdial.Lcms.Dataprocess.Utility;
using Msdial.Lcms.Dataprocess.Algorithm;
using CompMs.Graphics.Core.Base;
using System.Windows.Media;
using Rfx.Riken.OsakaUniv;
using Rfx.Riken.OsakaUniv.RetentionTimeCorrection;
using PdfExporter;

namespace Msdial.Common.Utility
{
    public enum RtDiffLabel { id, rt, name };

    public class RtCorrection
    {
        #region make DrawVisual class

        #region Demonstration data 
        public static DrawVisual GetDemoData(RetentionTimeCorrectionParam param, float usersetting) {
            var area = CompMs.Graphics.Core.Base.Utility.GetDefaultAreaV1();
            var title = CompMs.Graphics.Core.Base.Utility.GetDefaultTitleV1(13, "Demo");
            area.LabelSpace.Top = 13;

            var refVal = new List<double>() { 2, 4, 6, 8, 10 };

            var xVal = new List<double>() { 2, 4.1, 6.2, 8.1, 10.3 };
            var xVal2 = new List<double>() { 2.1, 4.2, 6.3, 8.4, 10.6 };

            var yVal = new List<double>();
            var yVal2 = new List<double>();
            var input1 = new double[refVal.Count + 2];
            var input2 = new double[refVal.Count + 2];

            if (param.RtDiffCalcMethod == RtDiffCalcMethod.SampleMinusReference) {
                for (var i = 0; i < refVal.Count; i++) {
                    yVal.Add(xVal[i] - refVal[i]);
                    yVal2.Add(xVal2[i] - refVal[i]);
                    input1[i + 1] = xVal[i];
                    input2[i + 1] = xVal2[i];
                }
            }
            else {
                for (var i = 0; i < refVal.Count; i++) {
                    var ave = (xVal[i] + xVal2[i]) / 2;
                    yVal.Add(xVal[i] - ave);
                    yVal2.Add(xVal2[i] - ave);
                    input1[i + 1] = xVal[i];
                    input2[i + 1] = xVal2[i];
                }
            }

            // set last RT
            input1[input1.Length - 1] = 15;
            input2[input2.Length - 1] = 15;

            var rt1 = Msdial.Lcms.Dataprocess.Algorithm.RetentionTimeCorrection.GetRetentionTimeCorrectionBeanUsingLinearMethod(param, null, xVal, yVal, input1);
            var rt2 = Msdial.Lcms.Dataprocess.Algorithm.RetentionTimeCorrection.GetRetentionTimeCorrectionBeanUsingLinearMethod(param, null, xVal2, yVal2, input2);

            var slist = new SeriesList();
            var s = new Series() {
                ChartType = ChartType.Line,
                MarkerType = MarkerType.Circle,
                MarkerSize = new System.Windows.Size(3, 3),
                Brush = Brushes.Blue,
                Pen = new Pen(Brushes.Blue, 1.5),
            };
            for (var i = 0; i < rt1.OriginalRt.Count; i++) {
                s.AddPoint((float)rt1.OriginalRt[i], (float)rt1.RtDiff[i] * 60);
            }
            var s2 = new Series() {
                ChartType = ChartType.Line,
                MarkerType = MarkerType.Circle,
                MarkerSize = new System.Windows.Size(3, 3),
                Brush = Brushes.Green,
                Pen = new Pen(Brushes.Green, 1.5),
            };
            for (var i = 0; i < rt2.OriginalRt.Count; i++) {
                s2.AddPoint((float)rt2.OriginalRt[i], (float)rt2.RtDiff[i] * 60);
            }
            if (s.Points.Count > 0)
                slist.Series.Add(s);
            if (s2.Points.Count > 0)
                slist.Series.Add(s2);
            var drawing = new DrawVisual(area, title, slist);
            CompMs.Graphics.Core.Base.Utility.SetDrawingMinAndMaxXYConstValue(drawing);
            CompMs.Graphics.Core.Base.Utility.SetDrawingMaxYRatio(drawing, 0.05f);
            CompMs.Graphics.Core.Base.Utility.SetDrawingMinYRatio(drawing, 0.05f);
            drawing.Initialize();
            return drawing;
        }
        #endregion

        #region Retention time difference, overlayed 
        public static DrawVisual GetDrawVisualOverlayedRtDiff(List<AnalysisFileBean> analysisFiles, AnalysisParametersBean param) {
            var area = CompMs.Graphics.Core.Base.Utility.GetDefaultAreaV1();
            area.LabelSpace.Top = 15;
            var title = CompMs.Graphics.Core.Base.Utility.GetDefaultTitleV1();
            var slist = new SeriesList();
            var numFiles = analysisFiles.Count;
            var targetId = 0; var maxCount = 0;
            for (int i = 0; i < numFiles; i++) {
                var f = analysisFiles[i];
                var brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(250, (byte)(255 * i / numFiles), (byte)(255 * (1 - Math.Abs(i / numFiles - 0.5))), (byte)(255 - 255 * i / numFiles)));

                var numHit = f.RetentionTimeCorrectionBean.StandardList.Count(x => x.SamplePeakAreaBean.RtAtPeakTop > 0);
                if (maxCount < numHit) {
                    maxCount = numHit;
                    targetId = i;
                }
                var s = new Series() {
                    ChartType = ChartType.Line,
                    MarkerType = MarkerType.None,
                    MarkerSize = new System.Windows.Size(2, 2),
                    Brush = brush,
                    Pen = new Pen(brush, 1.0),
                };
                if (f.RetentionTimeCorrectionBean.RtDiff != null)
                    for (var j = 0; j < f.RetentionTimeCorrectionBean.RtDiff.Count; j++) {
                        if (f.RetentionTimeCorrectionBean.OriginalRt[j] < param.RetentionTimeBegin) continue;
                        if (f.RetentionTimeCorrectionBean.OriginalRt[j] > param.RetentionTimeEnd) break;
                        s.AddPoint((float)f.RetentionTimeCorrectionBean.OriginalRt[j], (float)f.RetentionTimeCorrectionBean.RtDiff[j] * 60);
                    }
                if (s.Points.Count > 0)
                    slist.Series.Add(s);
            }

            var drawing = new DrawVisual(area, title, slist);
            CompMs.Graphics.Core.Base.Utility.SetDrawingMinAndMaxXYConstValue(drawing, param.RetentionTimeBegin, Math.Min(param.RetentionTimeEnd, drawing.SeriesList.MaxX));
            CompMs.Graphics.Core.Base.Utility.SetDrawingMaxYRatio(drawing, 0.05f);
            CompMs.Graphics.Core.Base.Utility.SetDrawingMinYRatio(drawing, 0.05f);
            drawing.Initialize();
            return drawing;
        }

        #endregion
        


        #region Retention time difference, each sample
        public static DrawVisual GetDrawingSingleRtDiff(AnalysisFileBean analysisFile, AnalysisParametersBean param, RetentionTimeCorrectionParam rtParam, 
            List<CommonStdData> detectedStdList,  RtDiffLabel label, SolidColorBrush brush, float ymin, float ymax) {
            var area = CompMs.Graphics.Core.Base.Utility.GetDefaultAreaV1("Retention time (min)", "RT diff (sec)");
            area.LabelSpace.Top = 20;
            area.LabelSpace.Bottom = 5;
            var title = CompMs.Graphics.Core.Base.Utility.GetDefaultTitleV1(13, "RetentionTime difference: " + analysisFile.AnalysisFilePropertyBean.AnalysisFileName);
            var slist = new SeriesList();

            var s = new Series() {
                ChartType = ChartType.Line,
                MarkerType = MarkerType.None,
                Brush = brush,
                Pen = new Pen(brush, 1.0),
                FontSize = 14,
            };
            if (analysisFile.RetentionTimeCorrectionBean.RtDiff == null) return new DrawVisual(new Area(), title, slist);
            for (var j = 0; j < analysisFile.RetentionTimeCorrectionBean.RtDiff.Count; j++) {
                if (analysisFile.RetentionTimeCorrectionBean.OriginalRt[j] < param.RetentionTimeBegin) continue;
                if (analysisFile.RetentionTimeCorrectionBean.OriginalRt[j] > param.RetentionTimeEnd) break;
                s.AddPoint((float)analysisFile.RetentionTimeCorrectionBean.OriginalRt[j], (float)analysisFile.RetentionTimeCorrectionBean.RtDiff[j] * 60);
            }
            if (s.Points.Count > 0)
                slist.Series.Add(s);

            SetLabelRtDiffV1(analysisFile, slist, rtParam, detectedStdList, label, brush);

            var drawing = new DrawVisual(area, title, slist);
            CompMs.Graphics.Core.Base.Utility.SetDrawingMinAndMaxXYConstValue(drawing, param.RetentionTimeBegin, Math.Min(param.RetentionTimeEnd, drawing.SeriesList.MaxX));
            CompMs.Graphics.Core.Base.Utility.SetDrawingMaxYRatio(drawing, 0.05f);
            CompMs.Graphics.Core.Base.Utility.SetDrawingMinYRatio(drawing, 0.05f);
            
            drawing.Initialize();
            return drawing;
        }

        #endregion

        #region Retention time difference, private function to show label
        private static void SetLabelRtDiffV1(AnalysisFileBean analysisFile, SeriesList slist, RetentionTimeCorrectionParam rtParam, List<CommonStdData> commonStdList, RtDiffLabel label, SolidColorBrush brush) {
            var labelList = new List<string>();
            var point = new Series() {
                ChartType = ChartType.Point,
                MarkerType = MarkerType.Circle,
                MarkerSize = new System.Windows.Size(3, 3),
                Brush = brush,
                Pen = new Pen(brush, 1.0),
                FontSize = 13,
                IsLabelVisible = true
            };
            if (rtParam.RtDiffCalcMethod == RtDiffCalcMethod.SampleMinusReference) {
                if (label == RtDiffLabel.id)
                    for (var i = 0; i < analysisFile.RetentionTimeCorrectionBean.StandardList.Count; i++) {
                        if (analysisFile.RetentionTimeCorrectionBean.StandardList[i].SamplePeakAreaBean.RtAtPeakTop == 0) continue;
                        point.AddPoint((float)analysisFile.RetentionTimeCorrectionBean.StandardList[i].SamplePeakAreaBean.RtAtPeakTop, (float)analysisFile.RetentionTimeCorrectionBean.StandardList[i].RtDiff * 60, analysisFile.RetentionTimeCorrectionBean.StandardList[i].Reference.ReferenceId.ToString());
                    }
                else if (label == RtDiffLabel.name)
                    for (var i = 0; i < analysisFile.RetentionTimeCorrectionBean.StandardList.Count; i++) {
                        if (analysisFile.RetentionTimeCorrectionBean.StandardList[i].SamplePeakAreaBean.RtAtPeakTop == 0) continue;
                        point.AddPoint((float)analysisFile.RetentionTimeCorrectionBean.StandardList[i].SamplePeakAreaBean.RtAtPeakTop, (float)analysisFile.RetentionTimeCorrectionBean.StandardList[i].RtDiff * 60, analysisFile.RetentionTimeCorrectionBean.StandardList[i].Reference.MetaboliteName);
                    }
                else if (label == RtDiffLabel.rt)
                    for (var i = 0; i < analysisFile.RetentionTimeCorrectionBean.StandardList.Count; i++) {
                        if (analysisFile.RetentionTimeCorrectionBean.StandardList[i].SamplePeakAreaBean.RtAtPeakTop == 0) continue;
                        point.AddPoint((float)analysisFile.RetentionTimeCorrectionBean.StandardList[i].SamplePeakAreaBean.RtAtPeakTop, (float)analysisFile.RetentionTimeCorrectionBean.StandardList[i].RtDiff * 60, analysisFile.RetentionTimeCorrectionBean.StandardList[i].Reference.RetentionTime.ToString());
                    }
            }
            else {
                if (label == RtDiffLabel.id)
                    for (var i = 0; i < analysisFile.RetentionTimeCorrectionBean.StandardList.Count; i++) {
                        if (analysisFile.RetentionTimeCorrectionBean.StandardList[i].SamplePeakAreaBean.RtAtPeakTop == 0) continue;
                        point.AddPoint((float)analysisFile.RetentionTimeCorrectionBean.StandardList[i].SamplePeakAreaBean.RtAtPeakTop, CalcRtDiff_SampleMinusAverage(analysisFile, commonStdList, i), analysisFile.RetentionTimeCorrectionBean.StandardList[i].Reference.ReferenceId.ToString());
                    }
                else if (label == RtDiffLabel.name)
                    for (var i = 0; i < analysisFile.RetentionTimeCorrectionBean.StandardList.Count; i++) {
                        if (analysisFile.RetentionTimeCorrectionBean.StandardList[i].SamplePeakAreaBean.RtAtPeakTop == 0) continue;
                        point.AddPoint((float)analysisFile.RetentionTimeCorrectionBean.StandardList[i].SamplePeakAreaBean.RtAtPeakTop, CalcRtDiff_SampleMinusAverage(analysisFile, commonStdList, i), analysisFile.RetentionTimeCorrectionBean.StandardList[i].Reference.MetaboliteName);
                    }
                else if (label == RtDiffLabel.rt)
                    for (var i = 0; i < analysisFile.RetentionTimeCorrectionBean.StandardList.Count; i++) {
                        if (analysisFile.RetentionTimeCorrectionBean.StandardList[i].SamplePeakAreaBean.RtAtPeakTop == 0) continue;
                        point.AddPoint((float)analysisFile.RetentionTimeCorrectionBean.StandardList[i].SamplePeakAreaBean.RtAtPeakTop, CalcRtDiff_SampleMinusAverage(analysisFile, commonStdList, i), analysisFile.RetentionTimeCorrectionBean.StandardList[i].Reference.RetentionTime.ToString());
                    }

            }
            if (point.Points.Count > 0)
                slist.Series.Add(point);

        }

        private static float CalcRtDiff_SampleMinusAverage(AnalysisFileBean file, List<CommonStdData> list, int i) {
            return (float)(file.RetentionTimeCorrectionBean.StandardList[i].SamplePeakAreaBean.RtAtPeakTop - (float)list[i].AverageRetentionTime) * 60f;
        }
        #endregion

        #region Intensity plot
        public static DrawVisual GetDrawVisualEachCompoundIntensityPlot(ObservableCollection<AnalysisFileBean> analysisFiles, CommonStdData commonStd, Dictionary<string, SolidColorBrush> brushDict) {
            var area = CompMs.Graphics.Core.Base.Utility.GetDefaultAreaV1("Sample ID", "Absolute Intensity");
            area.LabelSpace.Top = 15;
            var title = CompMs.Graphics.Core.Base.Utility.GetDefaultTitleV1(13, "Peak Height: " + commonStd.Reference.MetaboliteName);
            var slist = new SeriesList();
            for (var i = 0; i < commonStd.PeakAreaList.Count; i++) {
                var brush = brushDict[analysisFiles[i].AnalysisFilePropertyBean.AnalysisFileClass];
                var point = new Series() {
                    ChartType = ChartType.Point,
                    MarkerType = MarkerType.Circle,
                    MarkerSize = new System.Windows.Size(3, 3),
                    Brush = brush,
                    Pen = new Pen(brush, 1.0),
                };
                point.AddPoint((float)i + 1, (float)commonStd.PeakHeightList[i]);
                slist.Series.Add(point);
            }
            var drawing = new DrawVisual(area, title, slist);
            CompMs.Graphics.Core.Base.Utility.SetDrawingMinAndMaxXYConstValue(drawing, 0, analysisFiles.Count + 1, 0);
            CompMs.Graphics.Core.Base.Utility.SetDrawingMaxYRatio(drawing, 0.05f);
            drawing.Initialize();
            return drawing;
        }
        #endregion

        #region overlayed EIC, left side (original RT)
        public static DrawVisual GetDrawVisualOverlayedEIC(ObservableCollection<AnalysisFileBean> analysisFiles, CommonStdData commonStd, AnalysisParametersBean param, Dictionary<string, SolidColorBrush> brushDict) {
            var area = CompMs.Graphics.Core.Base.Utility.GetDefaultAreaV1("Retention Time [min]", "Absolute Intensity");
            area.Margin.Top = 25;
            var title = CompMs.Graphics.Core.Base.Utility.GetDefaultTitleV1(13, "Original EIC");
            var slist = new SeriesList();
            var commonStdExists = commonStd.RetentionTimeList.Where(x => x > 0);
            var minRTtmp = commonStdExists.Count() > 0 ? commonStdExists.Min() : 100;
            var minRT = Math.Min(commonStd.Reference.RetentionTime, minRTtmp);
            var maxRT = Math.Max(commonStd.Reference.RetentionTime, commonStd.RetentionTimeList.Max());
            var widhtList = commonStd.PeakWidthList.Where(x => x > 0);
            var rtTol = widhtList.Count() > 0 ? widhtList.Average() : 2;
            var minRTrange = Math.Max(0, (minRT - rtTol));
            for (var i = 0; i < commonStd.PeakAreaList.Count; i++) {
                if (commonStd.Chromatograms[i] == null || commonStd.Chromatograms[i].Count == 0) continue;
                var brush = brushDict[analysisFiles[i].AnalysisFilePropertyBean.AnalysisFileClass];
                var point = new Series() {
                    ChartType = ChartType.Line,
                    MarkerType = MarkerType.None,
                    Pen = new Pen(brush, 1.0),
                };
                var smoothedChromatogram = DataAccessLcUtility.GetSmoothedPeaklist(commonStd.Chromatograms[i], param.SmoothingMethod, param.SmoothingLevel);
                foreach (var peak in smoothedChromatogram) {
                    if (peak[1] < minRTrange) continue;
                    if (peak[1] > maxRT + rtTol) break;
                    point.AddPoint((float)peak[1], (float)peak[3]);
                }
                if (point.Points.Count > 0)
                    slist.Series.Add(point);
            }
            var drawing = new DrawVisual(area, title, slist);
            CompMs.Graphics.Core.Base.Utility.SetDrawingMinAndMaxXYConstValue(drawing, (float)(minRTrange), (float)(maxRT + rtTol), 0);
            CompMs.Graphics.Core.Base.Utility.SetDrawingMaxYRatio(drawing, 0.05f);

            drawing.Initialize();
            return drawing;
        }

        #endregion

        #region overlayed EIC, right side (corrected RT)
        public static DrawVisual GetDrawVisualOverlayedCorrectedEIC(ObservableCollection<AnalysisFileBean> analysisFiles, CommonStdData commonStd, AnalysisParametersBean param, Dictionary<string, SolidColorBrush> brushDict) {
            var area = CompMs.Graphics.Core.Base.Utility.GetDefaultAreaV1("Retention Time [min]", "Absolute Intensity");
            area.Margin.Top = 25;
            var title = CompMs.Graphics.Core.Base.Utility.GetDefaultTitleV1(13, "Corrected EIC");
            var slist = new SeriesList();
            var commonStdExists = commonStd.RetentionTimeList.Where(x => x > 0);
            var minRTtmp = commonStdExists.Count() > 0 ? commonStdExists.Min() : 100;
            var minRT = Math.Min(commonStd.Reference.RetentionTime, minRTtmp);
            var maxRT = Math.Max(commonStd.Reference.RetentionTime, commonStd.RetentionTimeList.Max());
            var widhtList = commonStd.PeakWidthList.Where(x => x > 0);
            var rtTol = widhtList.Count() > 0 ? widhtList.Average() : 2;
            var minRTrange = Math.Max(0, (minRT - rtTol));
            for (var i = 0; i < commonStd.PeakAreaList.Count; i++) {
                if (commonStd.Chromatograms[i] == null || commonStd.Chromatograms[i].Count == 0) continue;
                var brush = brushDict[analysisFiles[i].AnalysisFilePropertyBean.AnalysisFileClass];
                var point = new Series() {
                    ChartType = ChartType.Line,
                    MarkerType = MarkerType.None,
                    Pen = new Pen(brush, 1.0),
                };
                var rtList = GetSmoothedRetentionTime(analysisFiles[i].RetentionTimeCorrectionBean, param, commonStd.Chromatograms[i]);
                for (var j = 0; j < rtList.Count; j++) {
                    var peak = rtList[j];
                    if (peak[1] < minRTrange) continue;
                    if (peak[1] > maxRT + rtTol) break;
                    point.AddPoint((float)peak[1], (float)peak[3]);
                }
                if (point.Points.Count > 0)
                    slist.Series.Add(point);
            }
            var drawing = new DrawVisual(area, title, slist);
            CompMs.Graphics.Core.Base.Utility.SetDrawingMinAndMaxXYConstValue(drawing, (float)(minRTrange), (float)(maxRT + rtTol), 0);
            CompMs.Graphics.Core.Base.Utility.SetDrawingMaxYRatio(drawing, 0.05f);

            drawing.Initialize();
            return drawing;
        }

        #endregion

        #region Overlayed EIC, get smoothed peak list

        private static List<double[]> GetSmoothedRetentionTime(RetentionTimeCorrectionBean bean, AnalysisParametersBean param, List<double[]> peaks) {
            var correctedPeakList = new List<double[]>();
            for (var i = 0; i < peaks.Count; i++) {
                correctedPeakList.Add(new double[] { i, bean.PredictedRt[(int)peaks[i][0]], peaks[i][2], peaks[i][3] });
            }
            return DataAccessLcUtility.GetSmoothedPeaklist(correctedPeakList, param.SmoothingMethod, param.SmoothingLevel);
        }
        #endregion

        #region public methods to make DrawVisual component
        public static List<CommonStdData> MakeCommonStdList(ObservableCollection<AnalysisFileBean> analysisFiles, List<TextFormatCompoundInformationBean> iStdList) {
            var commonStdList = new List<CommonStdData>();
            var tmpStdList = iStdList.Where(x => x.IsTarget).OrderBy(x => x.RetentionTime);
            foreach (var std in tmpStdList) {
                commonStdList.Add(new CommonStdData(std));
            }
            for (var i = 0; i < analysisFiles.Count; i++) {
                for (var j = 0; j < commonStdList.Count; j++) {
                    commonStdList[j].SetStandard(analysisFiles[i].RetentionTimeCorrectionBean.StandardList[j]);
                }
            }
            foreach (var d in commonStdList) {
                d.CalcAverageRetentionTime();
            }
            return commonStdList;
        }

        #endregion

        #endregion
    }
}

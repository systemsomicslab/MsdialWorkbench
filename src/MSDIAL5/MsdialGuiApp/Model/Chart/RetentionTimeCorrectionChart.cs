using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart {
    public class RetentionTimeCorrectionChart {
        /*
        public static void ExportRtCorrectionRes(MainWindow mainWindow) {
            var path = mainWindow.ProjectProperty.ProjectFolderPath + "\\" + System.IO.Path.GetFileNameWithoutExtension(mainWindow.ProjectProperty.ProjectFilePath) + "_RtCorrection.pdf";
            CreatePdf.ExportRtCorrectionRes(path, mainWindow.AnalysisFiles, 800, 500, 200, 200);
        }
        */


        #region make DrawVisual class

        #region Demonstration data 
        public static DrawVisual GetDemoData(RetentionTimeCorrectionParam param, float usersetting) {
            var area = GetAreaV1();
            var title = GetTitleV1(13, "Demo");
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

            var rt1 = RetentionTimeCorrection.GetRetentionTimeCorrectionBeanUsingLinearMethod(param, null, xVal, yVal, input1);
            var rt2 = RetentionTimeCorrection.GetRetentionTimeCorrectionBeanUsingLinearMethod(param, null, xVal2, yVal2, input2);

            var slist = new SeriesList();
            var s = new Series() {
                ChartType = ChartType.Line,
                MarkerType = MarkerType.Circle,
                MarkerSize = new System.Windows.Size(3, 3),
                Brush = Brushes.Blue,
                Pen = new Pen(Brushes.Blue, 1.5),
            };
            for (var i = 0; i < rt1.originalRt.Count; i++) {
                s.AddPoint((float)rt1.originalRt[i], (float)rt1.rtDiff[i] * 60);
            }
            var s2 = new Series() {
                ChartType = ChartType.Line,
                MarkerType = MarkerType.Circle,
                MarkerSize = new System.Windows.Size(3, 3),
                Brush = Brushes.Green,
                Pen = new Pen(Brushes.Green, 1.5),
            };
            for (var i = 0; i < rt2.originalRt.Count; i++) {
                s2.AddPoint((float)rt2.originalRt[i], (float)rt2.rtDiff[i] * 60);
            }
            if (s.Points.Count > 0)
                slist.Series.Add(s);
            if (s2.Points.Count > 0)
                slist.Series.Add(s2);
            var drawing = new DrawVisual(area, title, slist);
            SetDrawingMinAndMaxXY_constValue(drawing);
            SetDrawingMaxY_ratio(drawing, 0.05f);
            SetDrawingMinY_ratio(drawing, 0.05f);
            drawing.Initialize();
            return drawing;
        }
        #endregion

        #region Retention time difference, overlayed 
        public static DrawVisual GetDrawing_RtDiff_OverView(List<AnalysisFileBean> analysisFiles, ParameterBase param, RetentionTimeCorrectionParam rtParam, List<CommonStdData> detectedStdList, RtDiffLabel label, List<SolidColorBrush> solidColorBrushList) {
            var area = GetAreaV1();
            area.LabelSpace.Top = 15;
            var title = GetTitleV1();
            var slist = new SeriesList();
            var numFiles = analysisFiles.Count;
            var targetId = 0; var maxCount = 0;
            for (int i = 0; i < numFiles; i++) {
                var f = analysisFiles[i];
                var brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(250, (byte)(255 * i / numFiles), (byte)(255 * (1 - Math.Abs(i / numFiles - 0.5))), (byte)(255 - 255 * i / numFiles)));

                var numHit = f.RetentionTimeCorrectionBean.StandardList.Count(x => x.SamplePeakAreaBean.ChromXs.RT.Value > 0);
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
            SetLabel_RtDiff_V1(analysisFiles[targetId], slist, rtParam, detectedStdList, label, Brushes.Black);

            var drawing = new DrawVisual(area, title, slist);
            SetDrawingMinAndMaxXY_constValue(drawing, param.RetentionTimeBegin, Math.Min(param.RetentionTimeEnd, drawing.SeriesList.MaxX));
            SetDrawingMaxY_ratio(drawing, 0.05f);
            SetDrawingMinY_ratio(drawing, 0.05f);
            drawing.Initialize();
            return drawing;
        }
        #endregion

        #region Retention time difference, each sample
        public static DrawVisual GetDrawing_RtDiff_Each(AnalysisFileBean analysisFile, ParameterBase param, RetentionTimeCorrectionParam rtParam, List<CommonStdData> detectedStdList, RtDiffLabel label, SolidColorBrush brush, float ymin, float ymax) {
            var area = GetAreaV1("Retention time (min)", "RT diff (sec)");
            area.LabelSpace.Top = 20;
            area.LabelSpace.Bottom = 5;
            var title = GetTitleV1(13, "RetentionTime difference: " + analysisFile.AnalysisFileName);
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

            SetLabel_RtDiff_V1(analysisFile, slist, rtParam, detectedStdList, label, brush);

            var drawing = new DrawVisual(area, title, slist);
            SetDrawingMinAndMaxXY_constValue(drawing, param.RetentionTimeBegin, Math.Min(param.RetentionTimeEnd, drawing.SeriesList.MaxX), ymin * 60, ymax * 60);
            SetDrawingMaxY_ratio(drawing, 0.05f);
            SetDrawingMinY_ratio(drawing, 0.05f);
            drawing.Initialize();
            return drawing;
        }

        #endregion

        #region Retention time difference, private function to show label
        private static void SetLabel_RtDiff_V1(AnalysisFileBean analysisFile, SeriesList slist, RetentionTimeCorrectionParam rtParam, List<CommonStdData> commonStdList, 
            RtDiffLabel label, SolidColorBrush brush) {
            var commonStdLookup = CreateCommonStdLookup(commonStdList);
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
                        if (analysisFile.RetentionTimeCorrectionBean.StandardList[i].SamplePeakAreaBean.ChromXs.RT.Value == 0) continue;
                        point.AddPoint((float)analysisFile.RetentionTimeCorrectionBean.StandardList[i].SamplePeakAreaBean.ChromXs.RT.Value, (float)analysisFile.RetentionTimeCorrectionBean.StandardList[i].RtDiff * 60, analysisFile.RetentionTimeCorrectionBean.StandardList[i].Reference.ScanID.ToString());
                    }
                else if (label == RtDiffLabel.name)
                    for (var i = 0; i < analysisFile.RetentionTimeCorrectionBean.StandardList.Count; i++) {
                        if (analysisFile.RetentionTimeCorrectionBean.StandardList[i].SamplePeakAreaBean.ChromXs.RT.Value == 0) continue;
                        point.AddPoint((float)analysisFile.RetentionTimeCorrectionBean.StandardList[i].SamplePeakAreaBean.ChromXs.RT.Value, (float)analysisFile.RetentionTimeCorrectionBean.StandardList[i].RtDiff * 60, analysisFile.RetentionTimeCorrectionBean.StandardList[i].Reference.Name);
                    }
                else if (label == RtDiffLabel.rt)
                    for (var i = 0; i < analysisFile.RetentionTimeCorrectionBean.StandardList.Count; i++) {
                        if (analysisFile.RetentionTimeCorrectionBean.StandardList[i].SamplePeakAreaBean.ChromXs.RT.Value == 0) continue;
                        point.AddPoint((float)analysisFile.RetentionTimeCorrectionBean.StandardList[i].SamplePeakAreaBean.ChromXs.RT.Value, (float)analysisFile.RetentionTimeCorrectionBean.StandardList[i].RtDiff * 60, analysisFile.RetentionTimeCorrectionBean.StandardList[i].Reference.ChromXs.RT.Value.ToString());
                    }
            }
            else {
                if (label == RtDiffLabel.id)
                    for (var i = 0; i < analysisFile.RetentionTimeCorrectionBean.StandardList.Count; i++) {
                        var standard = analysisFile.RetentionTimeCorrectionBean.StandardList[i];
                        if (standard.SamplePeakAreaBean.ChromXs.RT.Value == 0) continue;
                        if (!commonStdLookup.ContainsKey(standard.Reference.ScanID)) continue;
                        point.AddPoint((float)standard.SamplePeakAreaBean.ChromXs.RT.Value, CalcRtDiff_SampleMinusAverage(standard, commonStdLookup), standard.Reference.ScanID.ToString());
                    }
                else if (label == RtDiffLabel.name)
                    for (var i = 0; i < analysisFile.RetentionTimeCorrectionBean.StandardList.Count; i++) {
                        var standard = analysisFile.RetentionTimeCorrectionBean.StandardList[i];
                        if (standard.SamplePeakAreaBean.ChromXs.RT.Value == 0) continue;
                        if (!commonStdLookup.ContainsKey(standard.Reference.ScanID)) continue;
                        point.AddPoint((float)standard.SamplePeakAreaBean.ChromXs.RT.Value, CalcRtDiff_SampleMinusAverage(standard, commonStdLookup), standard.Reference.Name);
                    }
                else if (label == RtDiffLabel.rt)
                    for (var i = 0; i < analysisFile.RetentionTimeCorrectionBean.StandardList.Count; i++) {
                        var standard = analysisFile.RetentionTimeCorrectionBean.StandardList[i];
                        if (standard.SamplePeakAreaBean.ChromXs.RT.Value == 0) continue;
                        if (!commonStdLookup.ContainsKey(standard.Reference.ScanID)) continue;
                        point.AddPoint((float)standard.SamplePeakAreaBean.ChromXs.RT.Value, CalcRtDiff_SampleMinusAverage(standard, commonStdLookup), standard.Reference.ChromXs.RT.Value.ToString());
                    }

            }
            if (point.Points.Count > 0)
                slist.Series.Add(point);

        }

        /// <summary>
        /// Calculates the RT difference for the provided standard using ScanID-based lookup.
        /// </summary>
        /// <param name="standard">The standard pair that contains the sample RT.</param>
        /// <param name="commonStdLookup">The per-standard average RT lookup.</param>
        /// <returns>The RT difference in seconds.</returns>
        internal static float CalcRtDiff_SampleMinusAverage(StandardPair standard, IReadOnlyDictionary<int, CommonStdData> commonStdLookup) {
            if (!commonStdLookup.TryGetValue(standard.Reference.ScanID, out var commonStd)) {
                return 0f;
            }
            return (float)(standard.SamplePeakAreaBean.ChromXs.RT.Value - commonStd.AverageRetentionTime) * 60f;
        }

        /// <summary>
        /// Builds a ScanID lookup for RT correction summary rows.
        /// </summary>
        /// <param name="commonStdList">The summary rows to index.</param>
        /// <returns>A dictionary keyed by ScanID.</returns>
        private static IReadOnlyDictionary<int, CommonStdData> CreateCommonStdLookup(IEnumerable<CommonStdData> commonStdList) {
            var lookup = new Dictionary<int, CommonStdData>();
            foreach (var commonStd in commonStdList ?? Enumerable.Empty<CommonStdData>()) {
                if (!lookup.ContainsKey(commonStd.Reference.ScanID)) {
                    lookup.Add(commonStd.Reference.ScanID, commonStd);
                }
            }
            return lookup;
        }
        #endregion

        #region Intensity plot
        public static DrawVisual GetDrawVisual_IntensityPlot_EachCompound(List<AnalysisFileBean> analysisFiles, CommonStdData commonStd, Dictionary<string, SolidColorBrush> brushDict) {
            var area = GetAreaV1("Sample ID", "Absolute Intensity");
            area.LabelSpace.Top = 15;
            var title = GetTitleV1(13, "Peak Height: " + commonStd.Reference.Name);
            var slist = new SeriesList();
            for (var i = 0; i < commonStd.PeakAreaList.Count; i++) {
                var brush = brushDict[analysisFiles[i].AnalysisFileClass];
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
            SetDrawingMinAndMaxXY_constValue(drawing, 0, analysisFiles.Count() + 1, 0);
            SetDrawingMaxY_ratio(drawing, 0.05f);
            drawing.Initialize();
            return drawing;
        }
        #endregion

        #region overlayed EIC, left side (original RT)
        public static DrawVisual GetDrawVisual_EIC_Overview(List<AnalysisFileBean> analysisFiles, CommonStdData commonStd, ParameterBase param, Dictionary<string, SolidColorBrush> brushDict) {
            var area = GetAreaV1("Retention Time [min]", "Absolute Intensity");
            area.Margin.Top = 25;
            var title = GetTitleV1(13, "Original EIC");
            var slist = new SeriesList();
            var (minRT, maxRT) = GetEicDisplayRange(commonStd);
            for (var i = 0; i < commonStd.PeakAreaList.Count; i++) {
                if (commonStd.Chromatograms[i] == null || commonStd.Chromatograms[i].Count == 0) continue;
                var brush = brushDict[analysisFiles[i].AnalysisFileClass];
                var point = new Series() {
                    ChartType = ChartType.Chromatogram,
                    MarkerType = MarkerType.None,
                    Pen = new Pen(brush, 1.0),
                    Accessory = new Accessory(),
                };
                using var smoothedChromatogram = new Chromatogram(commonStd.Chromatograms[i], ChromXType.RT, ChromXUnit.Min).ChromatogramSmoothing(param.SmoothingMethod, param.SmoothingLevel);
                var peaks = smoothedChromatogram.AsPeakArray();
                foreach (var peak in peaks) {
                    if (peak.ChromXs.RT.Value < minRT) continue;
                    if (peak.ChromXs.RT.Value > maxRT) break;
                    point.AddPoint((float)peak.ChromXs.RT.Value, (float)peak.Intensity);
                }
                if (point.Points.Count > 0)
                    slist.Series.Add(point);
                var referenceRt = (float)commonStd.RetentionTimeList[i];
                AddReferenceRtMarker(slist, referenceRt, GetChromatogramIntensityAtRt(commonStd.Chromatograms[i], referenceRt));
            }
            var drawing = new DrawVisual(area, title, slist);
            SetDrawingMinAndMaxXY_constValue(drawing, minRT, maxRT, 0);
            SetDrawingMaxY_ratio(drawing, 0.05f);

            drawing.Initialize();
            return drawing;
        }

        #endregion

        #region overlayed EIC, right side (corrected RT)
        public static DrawVisual GetDrawVisual_correctedEIC_Overview(List<AnalysisFileBean> analysisFiles, CommonStdData commonStd, ParameterBase param, Dictionary<string, SolidColorBrush> brushDict) {
            var area = GetAreaV1("Retention Time [min]", "Absolute Intensity");
            area.Margin.Top = 25;
            var title = GetTitleV1(13, "Corrected EIC");
            var slist = new SeriesList();
            var (minRT, maxRT) = GetEicDisplayRange(commonStd);
            for (var i = 0; i < commonStd.PeakAreaList.Count; i++) {
                if (commonStd.Chromatograms[i] == null || commonStd.Chromatograms[i].Count == 0) continue;
                var brush = brushDict[analysisFiles[i].AnalysisFileClass];
                var point = new Series() {
                    ChartType = ChartType.Chromatogram,
                    MarkerType = MarkerType.None,
                    Pen = new Pen(brush, 1.0),
                    Accessory = new Accessory(),
                };
                var rtList = GetSmoothedRetentionTime(analysisFiles[i].RetentionTimeCorrectionBean, param, commonStd.Chromatograms[i]);
                for (var j = 0; j < rtList.Count; j++) {
                    var peak = rtList[j];
                    if (peak.ChromXs.RT.Value < minRT) continue;
                    if (peak.ChromXs.RT.Value > maxRT) break;
                    point.AddPoint((float)peak.ChromXs.RT.Value, (float)peak.Intensity);
                }
                if (point.Points.Count > 0)
                    slist.Series.Add(point);
                var referenceRt = GetCorrectedRt(analysisFiles[i], commonStd.RetentionTimeList[i]);
                AddReferenceRtMarker(slist, referenceRt, GetChromatogramIntensityAtRt(commonStd.Chromatograms[i], (float)commonStd.RetentionTimeList[i]));
            }
            var drawing = new DrawVisual(area, title, slist);
            SetDrawingMinAndMaxXY_constValue(drawing, minRT, maxRT, 0);
            SetDrawingMaxY_ratio(drawing, 0.05f);

            drawing.Initialize();
            return drawing;
        }

        #endregion

        #region Overlayed EIC, get smoothed peak list

        private static List<ChromatogramPeak> GetSmoothedRetentionTime(RetentionTimeCorrectionBean bean, ParameterBase param, IReadOnlyList<IChromatogramPeak> peaks) {
            var correctedPeakList = new List<ChromatogramPeak>();
            for (var i = 0; i < peaks.Count; i++) {
                correctedPeakList.Add(ChromatogramPeak.Create(peaks[i].ID, peaks[i].Mass, peaks[i].Intensity, new RetentionTime(bean.PredictedRt[peaks[i].ID])));
            }
            using var smoothed = new Chromatogram(correctedPeakList, ChromXType.RT, ChromXUnit.Min).ChromatogramSmoothing(param.SmoothingMethod, param.SmoothingLevel);
            return smoothed.AsPeakArray();
        }

        private static void AddReferenceRtMarker(SeriesList slist, float referenceRt, float topIntensity) {
            var markerTop = topIntensity > 0f ? topIntensity : 1f;
            var marker = new Series() {
                ChartType = ChartType.Line,
                MarkerType = MarkerType.None,
                Brush = Brushes.Red,
                Pen = new Pen(Brushes.Red, 1.0)
                {
                    DashStyle = DashStyles.DashDot,
                },
            };
            marker.AddPoint(referenceRt, 0f);
            marker.AddPoint(referenceRt, markerTop);
            slist.Series.Add(marker);
        }

        private static float GetCorrectedRt(AnalysisFileBean analysisFile, double baseRt) {
            var originalRt = baseRt;
            var bean = analysisFile.RetentionTimeCorrectionBean;
            if (bean?.OriginalRt is null || bean.OriginalRt.Count == 0 || bean.PredictedRt is null || bean.PredictedRt.Count == 0) {
                return (float)originalRt;
            }

            var originalIndex = bean.OriginalRt.BinarySearch(originalRt);
            if (originalIndex < 0) {
                originalIndex = ~originalIndex - 1;
            }
            if (originalIndex < 0) {
                return (float)originalRt;
            }

            if (originalIndex >= bean.PredictedRt.Count) {
                return (float)originalRt;
            }

            return (float)bean.PredictedRt[originalIndex];
        }

        private static float GetChromatogramIntensityAtRt(IReadOnlyList<IChromatogramPeak> chromatogram, float rt) {
            if (chromatogram is null || chromatogram.Count == 0) {
                return 1f;
            }
            var intensity = (float)chromatogram.OrderBy(peak => Math.Abs((float)peak.ChromXs.RT.Value - rt)).First().Intensity;
            return intensity > 0f ? intensity : 1f;
        }

        /// <summary>
        /// Computes the RT display window by expanding the reference RT window to include detected peak windows.
        /// </summary>
        /// <param name="commonStd">The standard row that contains the reference RT and per-sample detection results.</param>
        /// <returns>The lower and upper RT bounds for the chart.</returns>
        internal static (float minX, float maxX) GetEicDisplayRange(CommonStdData commonStd) {
            var referenceRt = (float)commonStd.Reference.ChromXs.RT.Value;
            var referenceTolerance = commonStd.Reference.RetentionTimeTolerance > 0
                ? commonStd.Reference.RetentionTimeTolerance
                : 2f;
            var referenceMin = Math.Max(0f, referenceRt - referenceTolerance);
            var referenceMax = referenceRt + referenceTolerance;

            var minX = referenceMin;
            var maxX = referenceMax;
            var peakWidth = commonStd.PeakWidthList.Where(x => x > 0).DefaultIfEmpty(2f).Average();
            for (var i = 0; i < commonStd.RetentionTimeList.Count; i++) {
                var peakRt = commonStd.RetentionTimeList[i];
                if (peakRt <= 0) {
                    continue;
                }

                minX = Math.Min(minX, (float)(peakRt - peakWidth));
                maxX = Math.Max(maxX, (float)(peakRt + peakWidth));
            }

            if (minX >= maxX) {
                maxX = minX + 0.01f;
            }
            return (minX, maxX);
        }
        #endregion

        #region XY axis setting in Draw Visual to set fixed max and min value

        public static void SetDrawingMinAndMaxXY_constValue(DrawVisual drawing, float minX = float.MinValue, float maxX = float.MinValue, float minY = float.MinValue, float maxY = float.MinValue) {
            if (minX > float.MinValue) drawing.MinX = minX;
            if (maxX > float.MinValue) drawing.MaxX = maxX;
            if (minY > float.MinValue) drawing.MinY = minY;
            if (maxY > float.MinValue) drawing.MaxY = maxY;
        }

        public static void SetDrawingMinX_ratio(DrawVisual drawing, float ratio) {
            drawing.MinX -= (drawing.SeriesList.MaxX - drawing.SeriesList.MinX) * ratio;
        }

        public static void SetDrawingMinY_ratio(DrawVisual drawing, float ratio) {
            drawing.MinY -= (drawing.SeriesList.MaxY - drawing.SeriesList.MinY) * ratio;
        }

        public static void SetDrawingMaxX_ratio(DrawVisual drawing, float ratio) {
            drawing.MaxX += (drawing.SeriesList.MaxX - drawing.SeriesList.MinX) * ratio;
        }

        public static void SetDrawingMaxY_ratio(DrawVisual drawing, float ratio) {
            drawing.MaxY += (drawing.SeriesList.MaxY - drawing.SeriesList.MinY) * ratio;
        }
        #endregion

        #region public methods to make DrawVisual component
        public static Area GetAreaV1(string xlabel = "Retention time (min)", string ylabel = "RT diff (Sample - Reference) (sec)") {
            var area = new Area() {
                AxisX = new AxisX() { AxisLabel = xlabel, Pen = new Pen(Brushes.Black, 0.5), FontSize = 12 },
                AxisY = new AxisY() { AxisLabel = ylabel, Pen = new Pen(Brushes.Black, 0.5), FontSize = 12 }
            };
            return area;
        }

        public static Area GetAreaV2(string xlabel = "Retention time (min)") {
            var area = new Area() {
                AxisX = new AxisX() { AxisLabel = xlabel, Pen = new Pen(Brushes.DarkGray, 0.5), FontSize = 12 },
                AxisY = new AxisY() { AxisLabel = "", Pen = new Pen(Brushes.DarkGray, 0.5), FontSize = 12 },
                Margin = new Margin(20, 30, 10, 40)
            };
            return area;
        }

        public static Title GetTitleV1(int fontsize = 13, string label = "Overview: Retention time correction") {
            return new Title() { FontSize = fontsize, Label = label };
        }

        #endregion

        #endregion
    }
}

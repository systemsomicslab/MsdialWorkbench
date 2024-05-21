using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Algorithm {
    public static class RetentionTimeCorrection {
        public static void Execute(
            AnalysisFileBean analysisFile, ParameterBase param, IDataProvider provider) {
            var iStandardLibrary = param.RetentionTimeCorrectionCommon.StandardLibrary;
            var rtParam = param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam;
            analysisFile.RetentionTimeCorrectionBean = DetectStdPeaks(provider, param, iStandardLibrary, analysisFile, rtParam);
            return;
        }


        public static RetentionTimeCorrectionBean DetectStdPeaks(IDataProvider provider, ParameterBase param,
            List<MoleculeMsReference> iStdLib, AnalysisFileBean property, RetentionTimeCorrectionParam rtParam) {
            System.Diagnostics.Debug.WriteLine("num lib: " + iStdLib.Count);
            
            var stdList = GetStdPair(property, provider, param, iStdLib);

            // original RT array
            var spectrumList = provider.LoadMsSpectrums();
            var originalRTs = spectrumList.Select(x => x.ScanStartTime).ToList();
            return new RetentionTimeCorrectionBean(property.RetentionTimeCorrectionBean.RetentionTimeCorrectionResultFilePath, originalRTs) { StandardList = stdList };
        }

        private static List<StandardPair> GetStdPair(
            AnalysisFileBean file,
            IDataProvider provider, ParameterBase param, List<MoleculeMsReference> iStdLib) {
            if (iStdLib.IsEmptyOrNull()) return new List<StandardPair>();

            var targetList = new List<StandardPair>();
            var peakpickCore = new PeakSpottingCore(param);
            var rawSpectra = new RawSpectra(provider, param.IonMode, file.AcquisitionType);
            var chromatogramRange = new ChromatogramRange(param.RetentionTimeBegin, param.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            foreach (var i in iStdLib) {
                var startMass = i.PrecursorMz;
                var endMass = i.PrecursorMz + i.MassTolerance;
                var pabCollection = peakpickCore.GetChromatogramPeakFeatures(rawSpectra, provider, (float)startMass, chromatogramRange);
                
                ChromatogramPeakFeature pab = null;
                if (pabCollection != null) {
                    foreach (var p in pabCollection) {
                        if (Math.Abs(p.ChromXs.RT.Value - i.ChromXs.RT.Value) < i.RetentionTimeTolerance && p.PeakHeightTop > i.MinimumPeakHeight)
                            if (pab == null)
                                pab = p;
                            else
                                if (pab.PeakHeightTop < p.PeakHeightTop) pab = p;

                    }
                }
                if (pab == null) pab = new ChromatogramPeakFeature() { PrecursorMz = i.PrecursorMz, ChromXs = new ChromXs(0) };
                var chromatogram = rawSpectra.GetMS1ExtractedChromatogram(new MzRange(startMass, i.MassTolerance), chromatogramRange);
                var peaklist = ((Chromatogram)chromatogram).AsPeakArray().Select(peak => peak ?? ChromatogramPeak.Create(peak.ID, peak.Mass, peak.Intensity, peak.ChromXs.RT)).ToList();
                targetList.Add(new StandardPair() { SamplePeakAreaBean = pab, Reference = i, Chromatogram = peaklist });
            }
            /*   foreach(var t in targetList) {
                   t.WriteSet();
               }
              */
            return targetList.OrderBy(x => x.Reference.ChromXs.RT.Value).ToList();
        }


        public static (List<double> originalRt, List<double> rtDiff, List<double> predictedRt) GetRetentionTimeCorrectionBean_SampleMinusAverage(
            RetentionTimeCorrectionParam rtParam, List<StandardPair> stdList,
            double[] xOriginal, List<CommonStdData> commonStdList) {
            var xList = new List<double>();
            var yList = new List<double>();
            for (var i = 0; i < stdList.Count; i++) {
                var val = stdList[i];
                if (val.SamplePeakAreaBean.ChromXs.RT.Value == 0) continue;
                var x = val.SamplePeakAreaBean.ChromXs.RT.Value;
                var y = x - commonStdList[i].AverageRetentionTime;
                xList.Add(x); yList.Add(y);
            }
            if (yList.Count == 0) return new (xOriginal.ToList(), xOriginal.ToList(), xOriginal.ToList());
            if (rtParam.InterpolationMethod == InterpolationMethod.Linear)
                return GetRetentionTimeCorrectionBeanUsingLinearMethod(rtParam, stdList, xList, yList, xOriginal);
            else {
                return new(xOriginal.ToList(), xOriginal.ToList(), xOriginal.ToList());
            }
        }

        public static (List<double> originalRt, List<double> rtDiff, List<double> predictedRt) GetRetentionTimeCorrectionBean_SampleMinusReference(RetentionTimeCorrectionParam rtParam, List<StandardPair> stdList,
            double[] xOriginal) {
            var xList = new List<double>();
            var yList = new List<double>();
            for (var i = 0; i < stdList.Count; i++) {
                var val = stdList[i];
                if (val.SamplePeakAreaBean.ChromXs.RT.Value == 0) continue;
                xList.Add(val.SamplePeakAreaBean.ChromXs.RT.Value);
                yList.Add(val.RtDiff);
            }
            var originalRt = xOriginal.ToList();
            if (yList.Count == 0) return new(xOriginal.ToList(), xOriginal.ToList(), xOriginal.ToList());
            if (rtParam.InterpolationMethod == InterpolationMethod.Linear)
                return GetRetentionTimeCorrectionBeanUsingLinearMethod(rtParam, stdList, xList, yList, xOriginal);
            else {
                return new(xOriginal.ToList(), xOriginal.ToList(), xOriginal.ToList());
            }
        }

        public static (List<double> originalRt, List<double> rtDiff, List<double> predictedRt) GetRetentionTimeCorrectionBeanUsingLinearMethod(RetentionTimeCorrectionParam rtParam, List<StandardPair>? stdList,
            List<double> xList, List<double> yList, double[] xOriginal) {
            List<double> predictedRts;

            // extrapolation from 0 to first iSTD
            if (rtParam.ExtrapolationMethodBegin == ExtrapolationMethodBegin.FirstPoint) {
                xList.Insert(0, 0);
                yList.Insert(0, yList[0]);
            }
            else if (rtParam.ExtrapolationMethodBegin == ExtrapolationMethodBegin.UserSetting) {
                xList.Insert(0, 0);
                yList.Insert(0, rtParam.UserSettingIntercept);
            }

            // extrapolation from last iSTD to end
            if (rtParam.ExtrapolationMethodEnd == ExtrapolationMethodEnd.LastPoint) {
                xList.Add(xOriginal[xOriginal.Length - 1]);
                yList.Add(yList[yList.Count - 1]);
            }

            if (xList.Count == 1) {
                xList.Insert(0, 0);
                yList.Insert(0, yList[0]);
            }

            var xArr = xList.ToArray();
            var yArr = yList.ToArray();

            var xOriginalList = xOriginal.ToList();
            var predictedRtDiffList = LinearInterpolation(xArr, yArr, xOriginal);
            if (rtParam.doSmoothing)
                predictedRtDiffList = Smoothing(xOriginalList, predictedRtDiffList);
            predictedRts = CalcPredictedRT(xOriginalList, predictedRtDiffList);
            return (xOriginalList, predictedRtDiffList, predictedRts);
        }

        public static List<double> CalcPredictedRT(List<double> originalRTs, List<double> rtDiff, double minRtDiff = 0.00001) {
            var predictedRt = new List<double>();
            var preRt = 0.0;
            for (var i = 0; i < originalRTs.Count; i++) {
                var currentRt = originalRTs[i] - rtDiff[i];
                if (preRt >= currentRt) currentRt = preRt + minRtDiff;
                predictedRt.Add(currentRt);
                preRt = currentRt;
            }

            return predictedRt;
        }

        public static List<double> Smoothing(List<double> x, List<double> y) {
            var peaks = new List<ChromatogramPeak>();
            for (var i = 0; i < x.Count; i++) {
                peaks.Add(ChromatogramPeak.Create(i, 0, y[i], new RetentionTime(x[i])));
            }
            var speaklist = new Chromatogram(peaks, ChromXType.RT, ChromXUnit.Min).ChromatogramSmoothing(SmoothingMethod.SimpleMovingAverage, 50).AsPeakArray();
            return speaklist.Select(z => z.Intensity).ToList();
        }

        public static List<double> LinearInterpolation(double[] x, double[] y, double[] xOriginal) {
            if (x.Length != y.Length) { Debug.WriteLine("error, x length and y length must be same."); return null; }
            int i = 0;
            var predList = new List<double>();
            foreach (var xOri in xOriginal) {
                if (xOri >= x[i + 1]) {
                    if (i < x.Length - 2) {
                        predList.Add(y[i + 1]); i++;
                    }
                    else {
                        predList.Add(LinearInterpolation(x[i], x[i + 1], y[i], y[i + 1], xOri));
                    }
                }
                else {
                    predList.Add(LinearInterpolation(x[i], x[i + 1], y[i], y[i + 1], xOri));
                }
            }
            return predList;
        }

        public static double LinearInterpolation(double x0, double x1, double y0, double y1, double xVal) {
            return y0 + (xVal - x0) / (x1 - x0) * (y1 - y0);
        }
    }
}

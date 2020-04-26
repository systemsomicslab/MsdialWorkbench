using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;
using Rfx.Riken.OsakaUniv;
using Msdial.Lcms.Dataprocess.Utility;
using CompMs.RawDataHandler.Core;
using Rfx.Riken.OsakaUniv.RetentionTimeCorrection;
using CompMs.Common.DataObj;

namespace Msdial.Lcms.Dataprocess.Algorithm
{
    public static class RetentionTimeCorrection
    {
        public static void Execute(ProjectPropertyBean projectProperty, RdamPropertyBean rdamProperty,
            AnalysisFileBean analysisFile, AnalysisParametersBean param, List<TextFormatCompoundInformationBean> iStandardLibrary, RetentionTimeCorrectionParam rtParam) {

            var spectrumCollection = DataAccessLcUtility.GetRdamSpectrumCollection(projectProperty, rdamProperty, analysisFile);
            if (spectrumCollection == null || spectrumCollection.Count == 0) { analysisFile.RetentionTimeCorrectionBean = new RetentionTimeCorrectionBean(); }

            analysisFile.RetentionTimeCorrectionBean = DetectStdPeaks(spectrumCollection, projectProperty, param, iStandardLibrary, analysisFile.AnalysisFilePropertyBean, rtParam);
            return;
        }


        public static RetentionTimeCorrectionBean DetectStdPeaks(ObservableCollection<RawSpectrum> spectrumCollection, ProjectPropertyBean projectProperty, AnalysisParametersBean param, 
            List<TextFormatCompoundInformationBean> iStdLib, AnalysisFilePropertyBean property, RetentionTimeCorrectionParam rtParam) {
            System.Diagnostics.Debug.WriteLine("num lib: " + iStdLib.Count);
            var stdList = GetStdPair(spectrumCollection, projectProperty, param, iStdLib);

            // original RT array
            var originalRTs = spectrumCollection.Select(x => x.ScanStartTime).ToArray();
            return new RetentionTimeCorrectionBean() { StandardList = stdList, OriginalRt = originalRTs.ToList() };
        }

        private static List<StandardPair> GetStdPair(ObservableCollection<RawSpectrum> spectrumCollection, ProjectPropertyBean projectProperty, AnalysisParametersBean param, List<TextFormatCompoundInformationBean> iStdLib) {            
            var targetList = new List<StandardPair>();
            foreach (var i in iStdLib) {
                var startMass = i.AccurateMass;
                var endMass = i.AccurateMass + i.AccurateMassTolerance;
                var pabCollection = PeakSpotting.GetPeakAreaBeanCollectionTargetMass(spectrumCollection, param, projectProperty, startMass, i.AccurateMassTolerance, progress => { });
                PeakAreaBean pab = null;
                if (pabCollection != null) {
                    foreach (var p in pabCollection) {
                        if (Math.Abs(p.RtAtPeakTop - i.RetentionTime) < i.RetentionTimeTolerance && p.IntensityAtPeakTop > i.MinimumPeakHeight)
                            if (pab == null)
                                pab = p;
                            else
                                if (pab.IntensityAtPeakTop < p.IntensityAtPeakTop) pab = p;

                    }
                }
                if (pab == null) pab = new PeakAreaBean() { AccurateMass = i.AccurateMass, RtAtPeakTop = 0 };
                var peaklist = DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectProperty, i.AccurateMass, i.AccurateMassTolerance, param.RetentionTimeBegin, param.RetentionTimeEnd);
                targetList.Add(new StandardPair() { SamplePeakAreaBean = pab, Reference = i, Chromatogram = peaklist });
            }
            /*   foreach(var t in targetList) {
                   t.WriteSet();
               }
              */
            return targetList.OrderBy(x => x.Reference.RetentionTime).ToList();
        }


        public static RetentionTimeCorrectionBean GetRetentionTimeCorrectionBean_SampleMinusAverage(RetentionTimeCorrectionParam rtParam, List<StandardPair> stdList,
            double[] xOriginal, List<CommonStdData> commonStdList) {
            var xList = new List<double>();
            var yList = new List<double>();
            for (var i = 0; i < stdList.Count; i++) {
                var val = stdList[i];
                if(val.SamplePeakAreaBean.RtAtPeakTop == 0) continue;
                var x = val.SamplePeakAreaBean.RtAtPeakTop;
                var y = x - commonStdList[i].AverageRetentionTime;
                xList.Add(x); yList.Add(y);
            }
            if (yList.Count == 0) return new RetentionTimeCorrectionBean() { StandardList = stdList, OriginalRt = xOriginal.ToList(), PredictedRt = xOriginal.ToList() };
            if (rtParam.InterpolationMethod == InterpolationMethod.Linear) 
                return GetRetentionTimeCorrectionBeanUsingLinearMethod(rtParam, stdList, xList, yList, xOriginal);
            else {
                return new RetentionTimeCorrectionBean();
            }
        }

        public static RetentionTimeCorrectionBean GetRetentionTimeCorrectionBean_SampleMinusReference(RetentionTimeCorrectionParam rtParam, List<StandardPair> stdList,
            double[] xOriginal) {
            var xList = new List<double>();
            var yList = new List<double>();
            for (var i = 0; i < stdList.Count; i++) {
                var val = stdList[i];
                if (val.SamplePeakAreaBean.RtAtPeakTop == 0) continue;
                xList.Add(val.SamplePeakAreaBean.RtAtPeakTop);
                yList.Add(val.RtDiff);
            }
            if (yList.Count == 0) return new RetentionTimeCorrectionBean() { StandardList = stdList, OriginalRt = xOriginal.ToList(), PredictedRt = xOriginal.ToList() };
            if (rtParam.InterpolationMethod == InterpolationMethod.Linear)
                return GetRetentionTimeCorrectionBeanUsingLinearMethod(rtParam, stdList, xList, yList, xOriginal);
            else {
                return new RetentionTimeCorrectionBean();
            }
        }

        public static RetentionTimeCorrectionBean GetRetentionTimeCorrectionBeanUsingLinearMethod(RetentionTimeCorrectionParam rtParam, List<StandardPair> stdList, 
            List<double> xList, List<double> yList, double[] xOriginal) {
            List<double> predictedRts;

            // extrapolation from 0 to first iSTD
            if (rtParam.ExtrapolationMethodBegin == ExtrapolationMethodBegin.FirstPoint) {
                xList.Insert(0, 0);
                yList.Insert(0, yList[0]);
            } else if (rtParam.ExtrapolationMethodBegin == ExtrapolationMethodBegin.UserSetting) {
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
            var bean = new RetentionTimeCorrectionBean() {
                OriginalRt = xOriginalList,
                RtDiff = predictedRtDiffList,
                PredictedRt = predictedRts,
                StandardList = stdList
            };
            return bean;
        }

        public static List<double> CalcPredictedRT(List<double> originalRTs, List<double> rtDiff, double minRtDiff = 0.00001) {
            var predictedRt = new List<double>();
            var preRt = 0.0;
            for(var i = 0; i < originalRTs.Count; i++) {
                var currentRt = originalRTs[i] - rtDiff[i];
                if (preRt >= currentRt) currentRt = preRt + minRtDiff;
                predictedRt.Add(currentRt);
                preRt = currentRt;
            }

            return predictedRt;
        }

        public static List<double> Smoothing(List<double> x, List<double> y) {
            var peaks = new List<double[]>();
            for(var i = 0; i < x.Count; i++) {
                peaks.Add(new double[] { i, x[i], 0, y[i] });
            }
            var speaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaks, SmoothingMethod.SimpleMovingAverage, 50);
            return speaklist.Select(z => z[3]).ToList();
        }

        public static List<double> LinearInterpolation(double[] x, double[] y, double[] xOriginal) {
            if(x.Length != y.Length) { Debug.WriteLine("error, x length and y length must be same."); return null; }
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

        #region Using spline
        /*
                public static RetentionTimeCorrectionBean CorrectRTbySpline(ObservableCollection<RAW_Spectrum> spectrumCollection, ProjectPropertyBean projectProperty, AnalysisParametersBean param, List<TextFormatCompoundInformationBean> iStdLib, AnalysisFilePropertyBean property) {
                    double minRtDiff = 0.02;

                    var stdList = GetStdPair(spectrumCollection, projectProperty, param, iStdLib);
                    if (stdList == null || stdList.Count < 3) {
                        return new RetentionTimeCorrectionBean();
                    }

                    var xList = stdList.Select(x => (double)x.Pab.RtAtPeakTop).ToList();
                    var yList = stdList.Select(x => x.RtDiff).ToList();
                    var originalRTs = spectrumCollection.Select(x => x.ScanStartTime).ToArray();
                    var predictedRtDiffArr = new double[originalRTs.Length];

                    var ini = xList[0];
                    xList.Insert(0, (ini / 1.5));
                    //            xList.Insert(0, (ini / 2.0));
                    //            xList.Insert(0, (ini / 4.0));
                    xList.Insert(0, 0);
                    xList.Add(xList[xList.Count - 1] + 1);

                    ini = yList[0];
                    yList.Insert(0, (ini / 2.0));
                    //            yList.Insert(0, (ini / 3.0));
                    //            yList.Insert(0, (ini / 5.0));
                    yList.Insert(0, 0);
                    yList.Add(yList[yList.Count - 1]);

                    var xArr = xList.ToArray();
                    var yArr = yList.ToArray();

                    var ySplineDeviArray = SmootherMathematics.Spline(xArr, yArr, double.MaxValue, double.MaxValue);
                    var lastRt = -1.0; var isLastRt = true;
                    var preRt = 0.0;
                    var predictedRts = new List<double>();
                    for (var i = 0; i < originalRTs.Length; i++) {
                        if (originalRTs[i] < xArr[xArr.Length - 2])
                            predictedRtDiffArr[i] = SmootherMathematics.Splint(xArr, yArr, ySplineDeviArray, originalRTs[i]);
                        else {
                            if (isLastRt) {
                                lastRt = SmootherMathematics.Splint(xArr, yArr, ySplineDeviArray, originalRTs[i]);
                                isLastRt = false;
                            }
                            predictedRtDiffArr[i] = lastRt;
                        }
                        var currentRt = originalRTs[i] - predictedRtDiffArr[i];
                        if (preRt >= currentRt) currentRt = preRt + minRtDiff;
                        predictedRts.Add(currentRt);
                        preRt = currentRt;
                    }

                    var bean = new RetentionTimeCorrectionBean() {
                        OriginalRt = originalRTs.ToList(),
                        RtDiff = predictedRtDiffArr.ToList(),
                        StdList = stdList,
                        PredictedRt = predictedRts,
                    };
                    return bean;
                }
                */

        #endregion

    }

}
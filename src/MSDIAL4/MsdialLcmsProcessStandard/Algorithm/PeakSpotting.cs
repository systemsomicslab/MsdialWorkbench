using Msdial.Lcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using CompMs.Common.DataObj;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Msdial.Lcms.Dataprocess.Algorithm
{
    public sealed class PeakSpotting
    {
        private PeakSpotting() { }

        private const double initialProgress = 0.0;
        private const double progressMax = 30.0;

        /// <summary>
        /// This is the peak spotting method to detect the peaks in retention time and m/z axies.
        /// </summary>
        /// <param name="spectrumCollection"></param>
        /// <param name="analysisParametersBean"></param>
        /// <param name="projectPropertyBean"></param>
        /// <returns></returns>
        public static ObservableCollection<PeakAreaBean> GetPeakAreaBeanCollection(ObservableCollection<RawSpectrum> spectrumCollection,
            AnalysisParametersBean analysisParametersBean,
            ProjectPropertyBean projectPropertyBean, Action<int> reportAction = null)
        {
            //foreach (var spec in spectrumCollection) {
            //    var precursor = spec.Precursor == null ? -1 : spec.Precursor.SelectedIonMz;
            //    if (spec.MsLevel == 1) {
            //        Debug.WriteLine("Scan {0}, MS level {1}, RT {2}, Precursor {3}, SpecCount {4}", spec.ScanNumber, spec.MsLevel, spec.ScanStartTime, precursor, spec.Spectrum.Length);
            //    }
            //    else if (spec.MsLevel == 2) {
            //        Debug.WriteLine("Scan {0}, MS level {1}, RT {2}, Precursor {3}, SpecCount {4}", spec.ScanNumber, spec.MsLevel, spec.ScanStartTime, precursor, spec.Spectrum.Length);
            //    }
            //}

            var peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();

            if (projectPropertyBean.MethodType == MethodType.ddMSMS) {
                if (analysisParametersBean.CompoundListInTargetMode == null || analysisParametersBean.CompoundListInTargetMode.Count == 0)
                    peakAreaBeanCollection = getPeakAreaBeanCollectionOnDataDependentAcqusitiion(spectrumCollection, analysisParametersBean, projectPropertyBean, reportAction);
                else {
                    var peakAreaBeanCollectionList = new List<ObservableCollection<PeakAreaBean>>();
                    foreach (var targetComp in analysisParametersBean.CompoundListInTargetMode) {
                        peakAreaBeanCollectionList.Add(getTargetPeakAreaBeanCollectionOnDDA(spectrumCollection, analysisParametersBean, projectPropertyBean, targetComp.AccurateMass, targetComp.AccurateMassTolerance, reportAction));
                    }
                    peakAreaBeanCollection = getCombinedPeakAreaBeanList(peakAreaBeanCollectionList);
                    var peakAreaBeanList = recalculatePeakAreaByBasePeakMzAndMs1MassTolerance(new List<PeakAreaBean>(peakAreaBeanCollection), projectPropertyBean, spectrumCollection, analysisParametersBean);
                    peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>(getPeakAreaBeanProperties(peakAreaBeanList, projectPropertyBean, spectrumCollection, analysisParametersBean));
                }
            }
            else if (projectPropertyBean.MethodType == MethodType.diMSMS) {
                if (analysisParametersBean.CompoundListInTargetMode == null || analysisParametersBean.CompoundListInTargetMode.Count == 0) {
                    peakAreaBeanCollection = getPeakAreaBeanCollectionOnDataIndependentAcqusitiion(spectrumCollection, analysisParametersBean, projectPropertyBean, reportAction);
                }
                else {
                    var peakAreaBeanCollectionList = new List<ObservableCollection<PeakAreaBean>>();
                    foreach (var targetComp in analysisParametersBean.CompoundListInTargetMode) {
                        peakAreaBeanCollectionList.Add(getTargetPeakAreaBeanCollectionOnDIA(spectrumCollection, analysisParametersBean, projectPropertyBean, targetComp.AccurateMass, targetComp.AccurateMassTolerance, reportAction));
                    }
                    peakAreaBeanCollection = getCombinedPeakAreaBeanList(peakAreaBeanCollectionList);
                    var peakAreaBeanList = recalculatePeakAreaByBasePeakMzAndMs1MassTolerance(new List<PeakAreaBean>(peakAreaBeanCollection), projectPropertyBean, spectrumCollection, analysisParametersBean);
                    peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>(getPeakAreaBeanProperties(peakAreaBeanList, projectPropertyBean, spectrumCollection, analysisParametersBean));
                }
            }
            return peakAreaBeanCollection;
        }

        #region Ion mobility data processing
        public static ObservableCollection<PeakAreaBean> GetPeakAreaBeanCollectionAtIonMobilityData(
            ObservableCollection<RawSpectrum> accumulatedMs1Spectrum,
            ObservableCollection<RawSpectrum> allSpectrum,
            AnalysisParametersBean param,
            ProjectPropertyBean project, Action<int> reportAction) {

            var peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();
            peakAreaBeanCollection = getPeakAreaBeanCollectionOnIonMobilityData(accumulatedMs1Spectrum, allSpectrum, param, project, reportAction);

            return peakAreaBeanCollection;
        }

        //peak spotting for ion mobility data analysis
        private static ObservableCollection<PeakAreaBean> getPeakAreaBeanCollectionOnIonMobilityData(
            ObservableCollection<RawSpectrum> accumulatedMs1Spectrum,
            ObservableCollection<RawSpectrum> allSpectrum,
            AnalysisParametersBean param, ProjectPropertyBean projectProp, Action<int> reportAction) {
            var peaklist = new List<double[]>();
            var peakAreaBeanListList = new List<List<PeakAreaBean>>();
            var peakAreaBeanList = new List<PeakAreaBean>();

            float[] mzRange = DataAccessLcUtility.GetMs1Range(accumulatedMs1Spectrum, projectProp.IonMode);
            float startMass = mzRange[0]; if (startMass < param.MassRangeBegin) startMass = param.MassRangeBegin;
            float endMass = mzRange[1]; if (endMass > param.MassRangeEnd) endMass = param.MassRangeEnd;
            float focusedMass = startMass, massStep = param.MassSliceWidth;
            while (focusedMass < endMass) {
                if (focusedMass < param.MassRangeBegin) { focusedMass += massStep; continue; }
                if (focusedMass > param.MassRangeEnd) break;
                //get EIC chromatogram

                peaklist = DataAccessLcUtility.GetMs1Peaklist(accumulatedMs1Spectrum, projectProp, focusedMass, param.MassSliceWidth, param.RetentionTimeBegin, param.RetentionTimeEnd);
                if (peaklist.Count == 0) { focusedMass += massStep; progressReports(focusedMass, endMass, reportAction); continue; }

                //get peak detection result
                peakAreaBeanList = getPeakAreaBeanListOnIonMobilityData(peaklist, accumulatedMs1Spectrum, allSpectrum, param, focusedMass, projectProp);
                if (peakAreaBeanList == null || peakAreaBeanList.Count == 0) { focusedMass += massStep; progressReports(focusedMass, endMass, reportAction); continue; }

                //filtering out noise peaks considering smoothing effects
                peakAreaBeanList = filteringPeaksByRawchromatogram(peakAreaBeanList, peaklist);
                if (peakAreaBeanList == null || peakAreaBeanList.Count == 0) { focusedMass += massStep; progressReports(focusedMass, endMass, reportAction); continue; }

                //filtering out noise peaks considering baseline effects
                peakAreaBeanList = getBackgroundSubtractPeaks(peakAreaBeanList, peaklist, param.BackgroundSubtraction);
                if (peakAreaBeanList == null || peakAreaBeanList.Count == 0) { focusedMass += massStep; progressReports(focusedMass, endMass, reportAction); continue; }

                //removing peak spot redundancies among slices
                peakAreaBeanList = removePeakAreaBeanRedundancy(peakAreaBeanListList, peakAreaBeanList, massStep);
                if (peakAreaBeanList == null || peakAreaBeanList.Count == 0) { focusedMass += massStep; progressReports(focusedMass, endMass, reportAction); continue; }

                peakAreaBeanList = executePeakDetectionOnDriftTimeAxis(peakAreaBeanList, allSpectrum, param, projectProp);
                //if (peakAreaBeanList.Count > 0) {
                //    foreach (var peak in peakAreaBeanList) {
                //        if (peak.RtAtRightPeakEdge - peak.RtAtLeftPeakEdge < 0) {
                //            Console.WriteLine();
                //        }
                //    }
                //}

                peakAreaBeanListList.Add(peakAreaBeanList);
                focusedMass += massStep;
                progressReports(focusedMass, endMass, reportAction);
            }

            peakAreaBeanList = getCombinedPeakAreaBeanList(peakAreaBeanListList);
            peakAreaBeanList = recalculatePeakAreaByBasePeakMzAndMs1MassTolerance(peakAreaBeanList, projectProp, accumulatedMs1Spectrum, param);

            peakAreaBeanList = getPeakAreaBeanProperties(peakAreaBeanList, projectProp, allSpectrum, param);

            return new ObservableCollection<PeakAreaBean>(peakAreaBeanList);
        }


        private static List<PeakAreaBean> executePeakDetectionOnDriftTimeAxis(List<PeakAreaBean> peakAreaBeanList, ObservableCollection<RawSpectrum> spectrumCollection,
            AnalysisParametersBean param, ProjectPropertyBean projectProp) {
            var newSpots = new List<PeakAreaBean>();
            foreach (var peakSpot in peakAreaBeanList) {
                peakSpot.DriftSpots = new List<DriftSpotBean>();

                var scanID = peakSpot.Ms1LevelDatapointNumber;
                var rt = peakSpot.RtAtPeakTop;
                var mz = peakSpot.AccurateMass;

                var rtWidth = peakSpot.RtAtRightPeakEdge - peakSpot.RtAtLeftPeakEdge;
                if (rtWidth > 0.6) rtWidth = 0.6F;
                if (rtWidth < 0.2) rtWidth = 0.2F;
                var mztol = param.CentroidMs1Tolerance;

                var peaklist = DataAccessLcUtility.GetDriftChromatogramByScanRtMz(spectrumCollection, scanID, rt, param.AccumulatedRtRagne, mz, mztol);
                //var peaklist = DataAccessLcUtility.GetDriftChromatogramByScanRtMz(spectrumCollection, scanID, rt, rtWidth, mz, mztol);
                //Console.WriteLine(mz + "\t" + peaklist.Count);
                if (peaklist.Count == 0) continue;
                var peaksOnDriftTime = getPeakAreaBeanListOnDriftTimeAxis(peaklist, peakSpot,
                    spectrumCollection, param, mz, projectProp);
                if (peaksOnDriftTime == null || peaksOnDriftTime.Count == 0) continue;
                peakSpot.DriftSpots = peaksOnDriftTime;
                newSpots.Add(peakSpot);
            }
            return newSpots;
        }

        private static List<DriftSpotBean> getPeakAreaBeanListOnDriftTimeAxis(List<double[]> peaklist,
            PeakAreaBean peakSpot,
            ObservableCollection<RawSpectrum> spectrumCollection,
            AnalysisParametersBean param, float focusedMass,
            ProjectPropertyBean projectProp) {

            var smoothedPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
            var minDatapoints = param.MinimumDatapoints;
            var minAmps = param.MinimumAmplitude * 0.25;
            var detectedPeaks = PeakDetection.PeakDetectionVS1(minDatapoints, minAmps, smoothedPeaklist);

            if (detectedPeaks == null || detectedPeaks.Count == 0) return null;

            var peaks = new List<DriftSpotBean>();
            var counter = 0;

            var peakleftRt = peakSpot.RtAtLeftPeakEdge;
            var peaktopRt = peakSpot.RtAtPeakTop;
            var peakrightRt = peakSpot.RtAtRightPeakEdge;

            var maxIntensityAtPeaks = detectedPeaks.Max(n => n.IntensityAtPeakTop);

            for (int i = 0; i < detectedPeaks.Count; i++) {
                if (detectedPeaks[i].IntensityAtPeakTop <= 0) continue;
                var edgeIntensity = (detectedPeaks[i].IntensityAtLeftPeakEdge + detectedPeaks[i].IntensityAtRightPeakEdge) * 0.5;
                var peakheightFromEdge = detectedPeaks[i].IntensityAtPeakTop - edgeIntensity;
                if (peakheightFromEdge < maxIntensityAtPeaks * 0.1) continue;
                var peak = DataAccessLcUtility.GetPeakAreaForDriftTime(detectedPeaks[i]);
                peak.PeakID = counter;
                peak.AccurateMass = peakSpot.AccurateMass;
                peak.PeakAreaBeanID = peakSpot.PeakID;

                //assign the scan number of MS1 and MS/MS for precursor ion's peaks
                peak.Ms1LevelDatapointNumber = (int)peaklist[detectedPeaks[i].ScanNumAtPeakTop][0];

                var driftleftRt = peak.DriftTimeAtLeftPeakEdge;
                var drifttopRt = peak.DriftTimeAtPeakTop;
                var driftrightRt = peak.DriftTimeAtRightPeakEdge;

                var startID = DataAccessLcUtility.GetRtStartIndex(peakleftRt - 0.01, spectrumCollection);
                var maxIntensity = double.MinValue;
                var maxIntID = -1;
                if (projectProp.MethodType == MethodType.ddMSMS)
                {
                    for (int j = startID; j < spectrumCollection.Count; j++)
                    {
                        var spec = spectrumCollection[j];

                        if (spec.ScanStartTime < peakleftRt) continue;
                        if (spec.ScanStartTime > peakrightRt) break;
                        if (spec.MsLevel != 2) continue;

                        if (spec.DriftTime > driftleftRt && spec.DriftTime < driftrightRt &&
                            Math.Abs(spec.Precursor.SelectedIonMz - peak.AccurateMass) < param.CentroidMs2Tolerance)
                        {
                            if (maxIntensity < spec.BasePeakIntensity)
                            {
                                maxIntensity = spec.BasePeakIntensity;
                                maxIntID = j;
                            }
                        }
                    }
                    if (maxIntID >= 0)
                        peak.Ms2LevelDatapointNumber = maxIntID;
                }
                else
                {
                    var ms2datapointNumber = -1;
                    for (int j = peak.Ms1LevelDatapointNumber; j < spectrumCollection.Count; j++)
                    {
                        var spec = spectrumCollection[j];

                        if (spec.MsLevel <= 1) continue;
                        if (spec.ScanStartTime > peakrightRt) break;

                        //Console.WriteLine(spec.DriftTime + "\t" + drifttopRt);

                        var IsMassInWindow = spec.Precursor.SelectedIonMz - spec.Precursor.IsolationWindowLowerOffset <= peak.AccurateMass && peak.AccurateMass < spec.Precursor.SelectedIonMz + spec.Precursor.IsolationWindowUpperOffset
                            ? true : false;
                        var IsDtInWindow = Math.Min(spec.Precursor.TimeBegin, spec.Precursor.TimeEnd) <= drifttopRt && drifttopRt < Math.Max(spec.Precursor.TimeBegin, spec.Precursor.TimeEnd)
                            ? true : false; // used for diapasef
                        if (spec.Precursor.TimeBegin == spec.Precursor.TimeEnd && spec.DriftTime == drifttopRt) IsDtInWindow = true; // normal dia

                        if (IsMassInWindow && IsDtInWindow)
                        {
                            //if(Math.Abs(spec.ScanStartTime - peaktopRt) > 0.1)
                            //{
                            //    Console.WriteLine("## RT shift ##");
                            //}
                            //if(spec.ScanStartTime == peaktopRt)
                            //{
                            //    Console.WriteLine("## Correct ##");
                            //}
                            ms2datapointNumber = j;
                            break;
                        }
                    }
                    if(ms2datapointNumber > 0)
                    {
                        peak.Ms2LevelDatapointNumber = ms2datapointNumber;
                    }
                    else
                    {
                        Console.WriteLine("## No ms2 datapoint number ##");
                    }
                }
                //peak.Ms2LevelDatapointNumber = DataAccessLcUtility.GetMs2DatapointNumber((int)peaklist[detectedPeaks[i].ScanNumAtLeftPeakEdge][0],
                //       (int)peaklist[detectedPeaks[i].ScanNumAtRightPeakEdge][0], (float)peaklist[detectedPeaks[i].ScanNumAtPeakTop][2],
                //       param, spectrumCollection, projectProp.IonMode);
                peaks.Add(peak);

                counter++;
            }

            return peaks;
        }

        #endregion

        #region TargetMass (Search Standard compound, or etc)
        // for target m/z
        public static ObservableCollection<PeakAreaBean> GetPeakAreaBeanCollectionTargetMass(ObservableCollection<RawSpectrum> spectrumCollection,
             AnalysisParametersBean analysisParametersBean, ProjectPropertyBean projectPropertyBean,
              float focusedMass, float ms1Tol, Action<int> reportAction) {

            var peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();

            if (projectPropertyBean.MethodType == MethodType.ddMSMS) {
                peakAreaBeanCollection = getTargetPeakAreaBeanCollectionOnDDA(spectrumCollection, analysisParametersBean, projectPropertyBean, focusedMass, ms1Tol, reportAction);
            }
            else if (projectPropertyBean.MethodType == MethodType.diMSMS) {
                peakAreaBeanCollection = getTargetPeakAreaBeanCollectionOnDIA(spectrumCollection, analysisParametersBean, projectPropertyBean, focusedMass, ms1Tol, reportAction);
            }
            return peakAreaBeanCollection;
        }
        #endregion

        #region Data dependent MS/MS acquisition
        //managing peak spotting functions based on mass slice method
        private static ObservableCollection<PeakAreaBean> getPeakAreaBeanCollectionOnDataDependentAcqusitiion(ObservableCollection<RawSpectrum> spectrumCollection,
            AnalysisParametersBean param, ProjectPropertyBean projectProp, Action<int> reportAction)
        {
            var peaklist = new List<double[]>();
            var peakAreaBeanListList = new List<List<PeakAreaBean>>();
            var peakAreaBeanList = new List<PeakAreaBean>();

            float[] mzRange = DataAccessLcUtility.GetMs1Range(spectrumCollection, projectProp.IonMode);
            float startMass = mzRange[0]; if (startMass < param.MassRangeBegin) startMass = param.MassRangeBegin;
            float endMass = mzRange[1]; if (endMass > param.MassRangeEnd) endMass = param.MassRangeEnd;
            float focusedMass = startMass, massStep = param.MassSliceWidth;

            while (focusedMass < endMass)
            {
                if (focusedMass < param.MassRangeBegin) { focusedMass += massStep; continue; }
                if (focusedMass > param.MassRangeEnd) break;

                //if (Math.Abs(focusedMass - 1275.58667) < 0.05) {
                //    Console.WriteLine();
                //}

				//get EIC chromatogram
                peaklist = DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectProp, focusedMass, param.MassSliceWidth, param.RetentionTimeBegin, param.RetentionTimeEnd);
                if (peaklist.Count == 0) { focusedMass += massStep; progressReports(focusedMass, endMass, reportAction); continue; }

				//get peak detection result
                peakAreaBeanList = getPeakAreaBeanListOnDataDependentAcqusitiion(peaklist, spectrumCollection, param, focusedMass, projectProp);
                if (peakAreaBeanList == null || peakAreaBeanList.Count == 0) { focusedMass += massStep; progressReports(focusedMass, endMass, reportAction); continue; }

				//filtering out noise peaks considering smoothing effects
                peakAreaBeanList = filteringPeaksByRawchromatogram(peakAreaBeanList, peaklist);
                if (peakAreaBeanList == null || peakAreaBeanList.Count == 0) { focusedMass += massStep; progressReports(focusedMass, endMass, reportAction); continue; }

				//filtering out noise peaks considering baseline effects
                peakAreaBeanList = getBackgroundSubtractPeaks(peakAreaBeanList, peaklist, param.BackgroundSubtraction);
                if (peakAreaBeanList == null || peakAreaBeanList.Count == 0) { focusedMass += massStep; progressReports(focusedMass, endMass, reportAction); continue; }

				//removing peak spot redundancies among slices
                peakAreaBeanList = removePeakAreaBeanRedundancy(peakAreaBeanListList, peakAreaBeanList, massStep);
                if (peakAreaBeanList == null || peakAreaBeanList.Count == 0) { focusedMass += massStep; progressReports(focusedMass, endMass, reportAction); continue; }

                //if (param.IsIonMobility) {
                //    executePeakDetectionOnDriftTimeAxis(peakAreaBeanList, spectrumCollection, param, projectProp);
                //}

                peakAreaBeanListList.Add(peakAreaBeanList);
                focusedMass += massStep;
                progressReports(focusedMass, endMass, reportAction);
            }

            peakAreaBeanList = getCombinedPeakAreaBeanList(peakAreaBeanListList);
            peakAreaBeanList = recalculatePeakAreaByBasePeakMzAndMs1MassTolerance(peakAreaBeanList, projectProp, spectrumCollection, param);

            peakAreaBeanList = getPeakAreaBeanProperties(peakAreaBeanList, projectProp, spectrumCollection, param);

            return new ObservableCollection<PeakAreaBean>(peakAreaBeanList);
        }

      
        private static List<PeakAreaBean> recalculatePeakAreaByBasePeakMzAndMs1MassTolerance(List<PeakAreaBean> peakSpots, 
            ProjectPropertyBean projectProp, ObservableCollection<RawSpectrum> spectrumCollection,
            AnalysisParametersBean param) {

            var recalculatedPeakspots = new List<PeakAreaBean>();
            var minDatapoint = 3;
           // var counter = 0;
            foreach (var spot in peakSpots) {
                //counter++;
                //Debug.WriteLine(counter);
                //if (counter == 258) {
                //    Console.WriteLine();
                //}

                //get EIC chromatogram
                var peakWidth = spot.RtAtRightPeakEdge - spot.RtAtLeftPeakEdge;
                List<double[]> peaklist;
                if (projectProp.MethodType == MethodType.ddMSMS) {
                    peaklist = DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectProp,
                        spot.AccurateMass, param.CentroidMs1Tolerance,
                        spot.RtAtLeftPeakEdge - peakWidth * 0.5F, spot.RtAtRightPeakEdge + peakWidth * 0.5F);
                }
                else {
                    peaklist = DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectProp,
                        spot.AccurateMass, param.CentroidMs1Tolerance,
                        param.RetentionTimeBegin, param.RetentionTimeEnd
                        );
                }

                if (peaklist == null || peaklist.Count <= 5) continue;
                //if (Math.Abs(spot.RtAtPeakTop - 18.503) < 0.1 && Math.Abs(spot.AccurateMass - 279.1598) < 0.01) {
                //    Console.WriteLine();
                //}

                var sPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
                var maxID = -1;
                var maxInt = double.MinValue;
                var minRtId = -1;
                var minRtValue = double.MaxValue;
                var peakAreaAboveZero = 0.0;
                var peakAreaAboveBaseline = 0.0;
                for (int i = 0; i < sPeaklist.Count - 1; i++) {
                    if (Math.Abs(sPeaklist[i][1] - spot.RtAtPeakTop) < minRtValue) {
                        minRtValue = Math.Abs(sPeaklist[i][1] - spot.RtAtPeakTop);
                        minRtId = i;
                    }
                }

                //finding local maximum within -2 ~ +2
                for (int i = minRtId - 2; i <= minRtId + 2; i++) {
                    if (i - 1 < 0) continue;
                    if (i > sPeaklist.Count - 2) break;
                    if (sPeaklist[i][3] > maxInt && 
                        sPeaklist[i - 1][3] <= sPeaklist[i][3] &&
                        sPeaklist[i][3] >= sPeaklist[i + 1][3]) {
                        maxInt = sPeaklist[i][3];
                        maxID = i;
                    }
                }

                //for (int i = minRtId - 2; i <= minRtId + 2; i++) {
                //    if (i < 0) continue;
                //    if (i > sPeaklist.Count - 1) break;
                //    if (sPeaklist[i][3] > maxInt) {
                //        maxInt = sPeaklist[i][3];
                //        maxID = i;
                //    }
                //}

                if (maxID == -1) {
                    maxInt = sPeaklist[minRtId][3];
                    maxID = minRtId;
                }

                //finding left edge;
                //seeking left edge
                var minLeftInt = sPeaklist[maxID][3];
                var minLeftId = -1;
                for (int i = maxID - minDatapoint; i >= 0; i--) {

                    if (i < maxID && minLeftInt < sPeaklist[i][3]) {
                        break;
                    }
                    if (sPeaklist[maxID][1] - peakWidth > sPeaklist[i][1]) break;

                    if (minLeftInt >= sPeaklist[i][3]) {
                        minLeftInt = sPeaklist[i][3];
                        minLeftId = i;
                    }
                }
                if (minLeftId == -1) {

                    var minOriginalLeftRtDiff = double.MaxValue;
                    var minOriginalLeftID = maxID - minDatapoint;
                    if (minOriginalLeftID < 0) minOriginalLeftID = 0;
                    for (int i = maxID; i >= 0; i--) {
                        var diff = Math.Abs(sPeaklist[i][1] - spot.RtAtLeftPeakEdge);
                        if (diff < minOriginalLeftRtDiff) {
                            minOriginalLeftRtDiff = diff;
                            minOriginalLeftID = i;
                        }
                    }

                    minLeftId = minOriginalLeftID;
                }

                //finding right edge;
                var minRightInt = sPeaklist[maxID][3];
                var minRightId = -1;
                for (int i = maxID + minDatapoint; i < sPeaklist.Count - 1; i++) {

                    if (i > maxID && minRightInt < sPeaklist[i][3]) {
                        break;
                    }
                    if (sPeaklist[maxID][1] + peakWidth < sPeaklist[i][1]) break;
                    if (minRightInt >= sPeaklist[i][3]) {
                        minRightInt = sPeaklist[i][3];
                        minRightId = i;
                    }
                }
                if (minRightId == -1) {

                    var minOriginalRightRtDiff = double.MaxValue;
                    var minOriginalRightID = maxID + minDatapoint;
                    if (minOriginalRightID > sPeaklist.Count - 1) minOriginalRightID = sPeaklist.Count - 1;
                    for (int i = maxID; i < sPeaklist.Count; i++) {
                        var diff = Math.Abs(sPeaklist[i][1] - spot.RtAtRightPeakEdge);
                        if (diff < minOriginalRightRtDiff) {
                            minOriginalRightRtDiff = diff;
                            minOriginalRightID = i;
                        }
                    }

                    minRightId = minOriginalRightID;
                }

                if (Math.Max(sPeaklist[minLeftId][3], sPeaklist[minRightId][3]) >= sPeaklist[maxID][3]) continue;
                if (sPeaklist[maxID][3] - Math.Min(sPeaklist[minLeftId][3], sPeaklist[minRightId][3]) < param.MinimumAmplitude) continue;

                //calculating peak area and finding real max ID
                var realMaxInt = double.MinValue;
                var realMaxID = maxID;
                for (int i = minLeftId; i <= minRightId - 1; i++) {
                    if (realMaxInt < sPeaklist[i][3]) {
                        realMaxInt = sPeaklist[i][3];
                        realMaxID = i;
                    }

                    peakAreaAboveZero += (sPeaklist[i][3] + sPeaklist[i + 1][3]) * (sPeaklist[i + 1][1] - sPeaklist[i][1]) * 0.5;
                }


                maxID = realMaxID;

                peakAreaAboveBaseline = peakAreaAboveZero - (sPeaklist[minLeftId][3] + sPeaklist[minRightId][3]) * 
                    (sPeaklist[minRightId][1] - sPeaklist[minLeftId][1]) / 2;

                spot.AreaAboveBaseline = (float)(peakAreaAboveBaseline * 60.0);
                spot.AreaAboveZero = (float)(peakAreaAboveZero * 60.0);

                spot.RtAtPeakTop = (float)sPeaklist[maxID][1];
                spot.RtAtLeftPeakEdge = (float)sPeaklist[minLeftId][1];
                spot.RtAtRightPeakEdge = (float)sPeaklist[minRightId][1];

                spot.IntensityAtPeakTop = (float)sPeaklist[maxID][3];
                spot.IntensityAtLeftPeakEdge = (float)sPeaklist[minLeftId][3];
                spot.IntensityAtRightPeakEdge = (float)sPeaklist[minRightId][3];

                if (projectProp.MethodType == MethodType.ddMSMS) {
                    spot.ScanNumberAtPeakTop = (int)peaklist[maxID][0];
                    spot.ScanNumberAtLeftPeakEdge = (int)peaklist[minLeftId][0];
                    spot.ScanNumberAtRightPeakEdge = (int)peaklist[minRightId][0];
                }
                else {
                    spot.ScanNumberAtPeakTop = (int)sPeaklist[maxID][0];
                    spot.ScanNumberAtLeftPeakEdge = (int)sPeaklist[minLeftId][0];
                    spot.ScanNumberAtRightPeakEdge = (int)sPeaklist[minRightId][0];
                }

                var peakHeightFromBaseline = Math.Max(sPeaklist[maxID][3] - sPeaklist[minLeftId][3], sPeaklist[maxID][3] - sPeaklist[minRightId][3]);
                spot.SignalToNoise = (float)(peakHeightFromBaseline / spot.EstimatedNoise);

                spot.Ms1LevelDatapointNumber = (int)peaklist[maxID][0];
                if (!param.IsIonMobility)
                {
                    if (projectProp.MethodType == MethodType.ddMSMS)
                    {
                        spot.Ms2LevelDatapointNumber = DataAccessLcUtility.GetMs2DatapointNumber(spot.ScanNumberAtLeftPeakEdge, spot.ScanNumberAtRightPeakEdge,
                            spot.AccurateMass, param, spectrumCollection, projectProp.IonMode);
                    }
                    else
                    {
                        DataAccessLcUtility.GetMs2DatapointNumberDIA(projectProp.ExperimentID_AnalystExperimentInformationBean, spot, projectProp.CheckAIF);
                    }
                }
                recalculatedPeakspots.Add(spot);
            }
            return recalculatedPeakspots;
        }

        private static List<PeakAreaBean> getPeakAreaBeanListOnDataDependentAcqusitiion(List<double[]> peaklist, 
            ObservableCollection<RawSpectrum> spectrumCollection, AnalysisParametersBean param, 
            float focusedMass, ProjectPropertyBean projectPropertyBean)
        {
            var smoothedPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
            //var detectedPeaks = PeakDetection.GetDetectedPeakInformationCollectionFromDifferentialBasedPeakDetectionAlgorithm(analysisParametersBean.MinimumDatapoints, analysisParametersBean.MinimumAmplitude, analysisParametersBean.AmplitudeNoiseFactor, analysisParametersBean.SlopeNoiseFactor, analysisParametersBean.PeaktopNoiseFactor, smoothedPeaklist);
            var minDatapoints = param.MinimumDatapoints;
            var minAmps = param.MinimumAmplitude;
            var detectedPeaks = PeakDetection.PeakDetectionVS1(minDatapoints, minAmps, smoothedPeaklist);
            if (detectedPeaks == null || detectedPeaks.Count == 0) return null;

            var peakAreaBeanList = new List<PeakAreaBean>();
            var excludeChecker = false;

            for (int i = 0; i < detectedPeaks.Count; i++)
            {
                if (detectedPeaks[i].IntensityAtPeakTop <= 0) continue;

				//this method is currently used in LC/MS project.
				//Users can prepare their-own 'exclusion mass' list to exclude unwanted peak features
                excludeChecker = false;
                if (param.ExcludedMassList != null && param.ExcludedMassList.Count != 0)
                    for (int j = 0; j < param.ExcludedMassList.Count; j++)
                        if (param.ExcludedMassList[j].ExcludedMass - param.ExcludedMassList[j].MassTolerance <= (float)peaklist[detectedPeaks[i].ScanNumAtPeakTop][2] && (float)peaklist[detectedPeaks[i].ScanNumAtPeakTop][2] <= param.ExcludedMassList[j].ExcludedMass + param.ExcludedMassList[j].MassTolerance) { excludeChecker = true; break; }
                if (excludeChecker) continue;
                //if (detectedPeaks[i].RtAtRightPeakEdge < detectedPeaks[i].RtAtLeftPeakEdge) {
                //    Console.WriteLine();
                //}
                var peakAreaBean = DataAccessLcUtility.GetPeakAreaBean(detectedPeaks[i]);
                peakAreaBean.AccurateMass = (float)peaklist[detectedPeaks[i].ScanNumAtPeakTop][2];

				//assign the scan number of MS1 and MS/MS for precursor ion's peaks
				peakAreaBean.Ms1LevelDatapointNumber = (int)peaklist[detectedPeaks[i].ScanNumAtPeakTop][0];
                peakAreaBean.Ms2LevelDatapointNumber = DataAccessLcUtility.GetMs2DatapointNumber((int)peaklist[detectedPeaks[i].ScanNumAtLeftPeakEdge][0], (int)peaklist[detectedPeaks[i].ScanNumAtRightPeakEdge][0], (float)peaklist[detectedPeaks[i].ScanNumAtPeakTop][2], param, spectrumCollection, projectPropertyBean.IonMode);
                peakAreaBeanList.Add(peakAreaBean);

            }

            return peakAreaBeanList;
        }

        private static List<PeakAreaBean> getPeakAreaBeanListOnIonMobilityData(List<double[]> peaklist, ObservableCollection<RawSpectrum> accumulatedMs1Spectrum,
            ObservableCollection<RawSpectrum> allSpectrum, AnalysisParametersBean param, float focusedMass, ProjectPropertyBean projectProp) {

            var smoothedPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
            //var detectedPeaks = PeakDetection.GetDetectedPeakInformationCollectionFromDifferentialBasedPeakDetectionAlgorithm(param.MinimumDatapoints,
            //    param.MinimumAmplitude, param.AmplitudeNoiseFactor, param.SlopeNoiseFactor, param.PeaktopNoiseFactor, smoothedPeaklist);
            var minDatapoints = param.MinimumDatapoints;
            var minAmps = param.MinimumAmplitude;
            var detectedPeaks = PeakDetection.PeakDetectionVS1(minDatapoints, minAmps, smoothedPeaklist);
            if (detectedPeaks == null || detectedPeaks.Count == 0) return null;

            var peakAreaBeanList = new List<PeakAreaBean>();
            var excludeChecker = false;

            for (int i = 0; i < detectedPeaks.Count; i++) {
                if (detectedPeaks[i].IntensityAtPeakTop <= 0) continue;

                //this method is currently used in LC/MS project.
                //Users can prepare their-own 'exclusion mass' list to exclude unwanted peak features
                excludeChecker = false;
                if (param.ExcludedMassList != null && param.ExcludedMassList.Count != 0)
                    for (int j = 0; j < param.ExcludedMassList.Count; j++)
                        if (param.ExcludedMassList[j].ExcludedMass - param.ExcludedMassList[j].MassTolerance <=
                            (float)peaklist[detectedPeaks[i].ScanNumAtPeakTop][2] &&
                            (float)peaklist[detectedPeaks[i].ScanNumAtPeakTop][2] <= param.ExcludedMassList[j].ExcludedMass + param.ExcludedMassList[j].MassTolerance) {
                            excludeChecker = true;
                            break;
                        }
                if (excludeChecker) continue;

                var peakAreaBean = DataAccessLcUtility.GetPeakAreaBean(detectedPeaks[i]);
                peakAreaBean.AccurateMass = (float)peaklist[detectedPeaks[i].ScanNumAtPeakTop][2];

                //assign the scan number of MS1 and MS/MS for precursor ion's peaks

                var originalIndex = accumulatedMs1Spectrum[(int)peaklist[detectedPeaks[i].ScanNumAtPeakTop][0]].OriginalIndex;
                peakAreaBean.Ms1LevelDatapointNumber = originalIndex;
                //peakAreaBean.Ms1LevelDatapointNumberAtAcculateMs1 = accumulatedMs1Spectrum[(int)peaklist[detectedPeaks[i].ScanNumAtPeakTop][0]].ScanNumber;
                peakAreaBean.Ms1LevelDatapointNumberAtAcculateMs1 = (int)peaklist[detectedPeaks[i].ScanNumAtPeakTop][0];

                var originalLeftScanID = accumulatedMs1Spectrum[(int)peaklist[detectedPeaks[i].ScanNumAtLeftPeakEdge][0]].OriginalIndex;
                var originalRightScanID = accumulatedMs1Spectrum[(int)peaklist[detectedPeaks[i].ScanNumAtRightPeakEdge][0]].OriginalIndex;

                //Ms2LevelDatapointNumber will be stored in DriftSpotBean
                // peakAreaBean.Ms2LevelDatapointNumber = DataAccessLcUtility.GetMs2DatapointNumber(originalLeftScanID,
                //    originalRightScanID,
                //    (float)peaklist[detectedPeaks[i].ScanNumAtPeakTop][2],
                //    param,
                //    allSpectrum,
                //    projectProp.IonMode);

                peakAreaBean.Ms2LevelDatapointNumber = -1;

                peakAreaBeanList.Add(peakAreaBean);
            }

            return peakAreaBeanList;
        }

        #endregion

        #region Data independent MS/MS acquisition
        private static ObservableCollection<PeakAreaBean> getPeakAreaBeanCollectionOnDataIndependentAcqusitiion(ObservableCollection<RawSpectrum> spectrumCollection, AnalysisParametersBean analysisParametersBean, ProjectPropertyBean projectPropertyBean, Action<int> reportAction)
        {
            var peaklist = new List<double[]>();
            var peakAreaBeanListList = new List<List<PeakAreaBean>>();
            var peakAreaBeanList = new List<PeakAreaBean>();

            int ms1LevelId = 0, ms2LevelId = 0;
            foreach (var value in projectPropertyBean.ExperimentID_AnalystExperimentInformationBean) { if (value.Value.MsType == MsType.SCAN) { ms1LevelId = value.Key; break; } }

            float startMass = projectPropertyBean.ExperimentID_AnalystExperimentInformationBean[ms1LevelId].StartMz, endMass = projectPropertyBean.ExperimentID_AnalystExperimentInformationBean[ms1LevelId].EndMz;
            float focusedMass = startMass, massStep = analysisParametersBean.MassSliceWidth;

            while (focusedMass < endMass)
            {
                if (focusedMass < analysisParametersBean.MassRangeBegin) { focusedMass += massStep; continue; }
                if (focusedMass > analysisParametersBean.MassRangeEnd) break;

				//get peak detection result
                peaklist = DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectPropertyBean, focusedMass, analysisParametersBean.MassSliceWidth, analysisParametersBean.RetentionTimeBegin, analysisParametersBean.RetentionTimeEnd);

                if (peaklist.Count == 0) { focusedMass += massStep; progressReports(focusedMass, endMass, reportAction); continue; }

				//This method is required in DIA-MS to know the scan number of respective precursor-window regeion 
				//with respect to the 'fucusedMass (precursor ion)' info.  
                foreach (var value in projectPropertyBean.ExperimentID_AnalystExperimentInformationBean) { if (value.Value.MsType == MsType.SWATH && value.Value.StartMz < focusedMass && focusedMass <= value.Value.EndMz) { ms2LevelId = value.Key; break; } }

				//get peak detection result
                peakAreaBeanList = getPeakAreaBeanListOnDataIndependentAcqusitiion(peaklist, analysisParametersBean, ms1LevelId, ms2LevelId, focusedMass, projectPropertyBean.ExperimentID_AnalystExperimentInformationBean, projectPropertyBean.CheckAIF);
                if (peakAreaBeanList == null || peakAreaBeanList.Count == 0) { focusedMass += massStep; progressReports(focusedMass, endMass, reportAction); continue; }

				//filtering out noise peaks considering smoothing effects
                peakAreaBeanList = filteringPeaksByRawchromatogram(peakAreaBeanList, peaklist);
                if (peakAreaBeanList == null || peakAreaBeanList.Count == 0) { focusedMass += massStep; progressReports(focusedMass, endMass, reportAction); continue; }

				//filtering out noise peaks considering baseline effects
                peakAreaBeanList = getBackgroundSubtractPeaks(peakAreaBeanList, peaklist, analysisParametersBean.BackgroundSubtraction);
                if (peakAreaBeanList == null || peakAreaBeanList.Count == 0) { focusedMass += massStep; progressReports(focusedMass, endMass, reportAction); continue; }

				//removing peak spot redundancies among slices
                peakAreaBeanList = removePeakAreaBeanRedundancy(peakAreaBeanListList, peakAreaBeanList, massStep);
                if (peakAreaBeanList != null && peakAreaBeanList.Count != 0) peakAreaBeanListList.Add(peakAreaBeanList);

                focusedMass += massStep;
                progressReports(focusedMass, endMass, reportAction);
            }

            peakAreaBeanList = getCombinedPeakAreaBeanList(peakAreaBeanListList);
            peakAreaBeanList = recalculatePeakAreaByBasePeakMzAndMs1MassTolerance(peakAreaBeanList, projectPropertyBean, spectrumCollection, analysisParametersBean);

            peakAreaBeanList = getPeakAreaBeanProperties(peakAreaBeanList, projectPropertyBean, spectrumCollection, analysisParametersBean);

            return new ObservableCollection<PeakAreaBean>(peakAreaBeanList);
        }

        private static List<PeakAreaBean> getPeakAreaBeanListOnDataIndependentAcqusitiion(List<double[]> peaklist, 
            AnalysisParametersBean param, int ms1LevelId, int ms2LevelId, float focusedMass,
            Dictionary<int, AnalystExperimentInformationBean> experimentID_AnalystExperimentInformationBean, bool checkAIF)
        {
            var smoothedPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
            //var detectedPeaks = PeakDetection.GetDetectedPeakInformationCollectionFromDifferentialBasedPeakDetectionAlgorithm(analysisParametersBean.MinimumDatapoints, analysisParametersBean.MinimumAmplitude, analysisParametersBean.AmplitudeNoiseFactor, analysisParametersBean.SlopeNoiseFactor, analysisParametersBean.PeaktopNoiseFactor, smoothedPeaklist);
            var minDatapoints = param.MinimumDatapoints;
            var minAmps = param.MinimumAmplitude;
            var detectedPeaks = PeakDetection.PeakDetectionVS1(minDatapoints, minAmps, smoothedPeaklist);
            if (detectedPeaks == null || detectedPeaks.Count == 0) return null;

            var peakAreaBeanList = new List<PeakAreaBean>();
            bool excludeChecker = false;

            for (int i = 0; i < detectedPeaks.Count; i++)
            {
                if (detectedPeaks[i].IntensityAtPeakTop <= 0) continue;

				//this method is currently used in LC/MS project.
				//Users can prepare their-own 'exclusion mass' list to exclude unwanted peak features
                excludeChecker = false;
                if (param.ExcludedMassList != null && param.ExcludedMassList.Count != 0)
                    for (int j = 0; j < param.ExcludedMassList.Count; j++)
                        if (param.ExcludedMassList[j].ExcludedMass - param.ExcludedMassList[j].MassTolerance <= (float)peaklist[detectedPeaks[i].ScanNumAtPeakTop][2] && (float)peaklist[detectedPeaks[i].ScanNumAtPeakTop][2] <= param.ExcludedMassList[j].ExcludedMass + param.ExcludedMassList[j].MassTolerance) { excludeChecker = true; break; }
                if (excludeChecker) continue;

                var peakAreaBean = DataAccessLcUtility.GetPeakAreaBean(detectedPeaks[i]);
                peakAreaBean.AccurateMass = (float)peaklist[detectedPeaks[i].ScanNumAtPeakTop][2];
                peakAreaBean.Ms1LevelDatapointNumber = (int)peaklist[detectedPeaks[i].ScanNumAtPeakTop][0];

                DataAccessLcUtility.GetMs2DatapointNumberDIA(experimentID_AnalystExperimentInformationBean, peakAreaBean, checkAIF);
                peakAreaBeanList.Add(peakAreaBean);
            }

            return peakAreaBeanList;
        }

        #endregion

        #region Target m/z peak picking in DDA project
        private static ObservableCollection<PeakAreaBean> getTargetPeakAreaBeanCollectionOnDDA(ObservableCollection<RawSpectrum> spectrumCollection,
            AnalysisParametersBean param, ProjectPropertyBean projectProp, float focusedMass, float ms1Tol, Action<int> reportAction) {
        
            var peaklist = new List<double[]>();
            var peakAreaBeanListList = new List<List<PeakAreaBean>>();
            var peakAreaBeanList = new List<PeakAreaBean>();

            if (focusedMass > param.MassRangeEnd) { return null; }

            float[] mzRange = DataAccessLcUtility.GetMs1Range(spectrumCollection, projectProp.IonMode);

            //get EIC chromatogram
            peaklist = DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectProp, focusedMass, ms1Tol, param.RetentionTimeBegin, param.RetentionTimeEnd);
            if (peaklist.Count == 0) { return null; }

            //get peak detection result
            peakAreaBeanList = getPeakAreaBeanListOnDataDependentAcqusitiion(peaklist, spectrumCollection, param, focusedMass, projectProp);
            if (peakAreaBeanList == null || peakAreaBeanList.Count == 0) { return null; }

            //filtering out noise peaks considering smoothing effects
            peakAreaBeanList = filteringPeaksByRawchromatogram(peakAreaBeanList, peaklist);
            if (peakAreaBeanList == null || peakAreaBeanList.Count == 0) { return null; }

            //filtering out noise peaks considering baseline effects
            peakAreaBeanList = getBackgroundSubtractPeaks(peakAreaBeanList, peaklist, param.BackgroundSubtraction);
            if (peakAreaBeanList == null || peakAreaBeanList.Count == 0) { return null; }

            peakAreaBeanList = recalculatePeakAreaByBasePeakMzAndMs1MassTolerance(peakAreaBeanList, projectProp, spectrumCollection, param);

            peakAreaBeanList = getPeakAreaBeanProperties(peakAreaBeanList, projectProp, spectrumCollection, param);

            return new ObservableCollection<PeakAreaBean>(peakAreaBeanList);
        }
        #endregion

        #region Target m/z peak picking in DIA project
        private static ObservableCollection<PeakAreaBean> getTargetPeakAreaBeanCollectionOnDIA(ObservableCollection<RawSpectrum> spectrumCollection, AnalysisParametersBean analysisParametersBean, ProjectPropertyBean projectPropertyBean, float focusedMass, float ms1Tol, Action<int> reportAction) {
            var peaklist = new List<double[]>();
            var peakAreaBeanListList = new List<List<PeakAreaBean>>();
            var peakAreaBeanList = new List<PeakAreaBean>();
            if (focusedMass > analysisParametersBean.MassRangeEnd) { return null; }

            int ms1LevelId = 0, ms2LevelId = 0;
            foreach (var value in projectPropertyBean.ExperimentID_AnalystExperimentInformationBean) { if (value.Value.MsType == MsType.SCAN) { ms1LevelId = value.Key; break; } }

            //get peak detection result
            peaklist = DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectPropertyBean, focusedMass, ms1Tol, analysisParametersBean.RetentionTimeBegin, analysisParametersBean.RetentionTimeEnd);
            if (peaklist.Count == 0) { return null; }

            //This method is required in DIA-MS to know the scan number of respective precursor-window regeion 
            //with respect to the 'fucusedMass (precursor ion)' info.  
            foreach (var value in projectPropertyBean.ExperimentID_AnalystExperimentInformationBean) { if (value.Value.MsType == MsType.SWATH && value.Value.StartMz < focusedMass && focusedMass <= value.Value.EndMz) { ms2LevelId = value.Key; break; } }

            //get peak detection result
            peakAreaBeanList = getPeakAreaBeanListOnDataIndependentAcqusitiion(peaklist, analysisParametersBean, ms1LevelId, ms2LevelId, focusedMass, projectPropertyBean.ExperimentID_AnalystExperimentInformationBean, projectPropertyBean.CheckAIF);
            if (peakAreaBeanList == null || peakAreaBeanList.Count == 0) { return null; }

            //filtering out noise peaks considering smoothing effects
            peakAreaBeanList = filteringPeaksByRawchromatogram(peakAreaBeanList, peaklist);
            if (peakAreaBeanList == null || peakAreaBeanList.Count == 0) { return null; }

            //filtering out noise peaks considering baseline effects
            peakAreaBeanList = getBackgroundSubtractPeaks(peakAreaBeanList, peaklist, analysisParametersBean.BackgroundSubtraction);
            if (peakAreaBeanList == null || peakAreaBeanList.Count == 0) { return null; }

            return new ObservableCollection<PeakAreaBean>(peakAreaBeanList);
        }
        #endregion


        #region Common
        private static List<PeakAreaBean> getCombinedPeakAreaBeanList(List<List<PeakAreaBean>> peakAreaBeanListList)
        {
            var combinedPeakAreaBeanList = new List<PeakAreaBean>();

            for (int i = 0; i < peakAreaBeanListList.Count; i++)
            {
                if (peakAreaBeanListList[i].Count == 0) continue;
                for (int j = 0; j < peakAreaBeanListList[i].Count; j++)
                    combinedPeakAreaBeanList.Add(peakAreaBeanListList[i][j]);
            }

            return combinedPeakAreaBeanList;
        }

        private static ObservableCollection<PeakAreaBean> getCombinedPeakAreaBeanList(List<ObservableCollection<PeakAreaBean>> peakAreaBeanCollectionList) {
            var combinedPeakAreaBeanList = new ObservableCollection<PeakAreaBean>();

            for (int i = 0; i < peakAreaBeanCollectionList.Count; i++) {
                if (peakAreaBeanCollectionList[i] == null || peakAreaBeanCollectionList[i].Count == 0) continue;
                for (int j = 0; j < peakAreaBeanCollectionList[i].Count; j++)
                    combinedPeakAreaBeanList.Add(peakAreaBeanCollectionList[i][j]);
            }

            return combinedPeakAreaBeanList;
        }


        private static List<PeakAreaBean> removePeakAreaBeanRedundancy(List<List<PeakAreaBean>> peakAreaBeanListList, 
            List<PeakAreaBean> peakAreaBeanList, float massStep)
        {
            if (peakAreaBeanListList == null || peakAreaBeanListList.Count == 0) return peakAreaBeanList;

            var parentPeakAreaBeanList = peakAreaBeanListList[peakAreaBeanListList.Count - 1];

            for (int i = 0; i < peakAreaBeanList.Count; i++)
            {
                for (int j = 0; j < parentPeakAreaBeanList.Count; j++)
                {
                    if (Math.Abs(parentPeakAreaBeanList[j].AccurateMass - peakAreaBeanList[i].AccurateMass) <=
                        massStep * 0.5) {

                        var isOverlaped = isOverlapedChecker(parentPeakAreaBeanList[j], peakAreaBeanList[i]);
                        if (!isOverlaped) continue;
                        var hwhm = ((parentPeakAreaBeanList[j].RtAtRightPeakEdge - parentPeakAreaBeanList[j].RtAtLeftPeakEdge) +
                            (peakAreaBeanList[i].RtAtRightPeakEdge - peakAreaBeanList[i].RtAtLeftPeakEdge)) * 0.25;

                        var tolerance = Math.Min(hwhm, 0.03);
                        if (Math.Abs(parentPeakAreaBeanList[j].RtAtPeakTop - peakAreaBeanList[i].RtAtPeakTop) <= tolerance) {
                            if (peakAreaBeanList[i].IntensityAtPeakTop > parentPeakAreaBeanList[j].IntensityAtPeakTop) {
                                parentPeakAreaBeanList.RemoveAt(j);
                                j--;
                                continue;
                            }
                            else {
                                peakAreaBeanList.RemoveAt(i);
                                i--;
                                break;
                            }
                        }
                    }
                }
                if (parentPeakAreaBeanList == null || parentPeakAreaBeanList.Count == 0) return peakAreaBeanList;
                if (peakAreaBeanList == null || peakAreaBeanList.Count == 0) return null;
            }
            return peakAreaBeanList;
        }

        private static bool isOverlapedChecker(PeakAreaBean peak1, PeakAreaBean peak2)
        {
            if (peak1.RtAtPeakTop > peak2.RtAtPeakTop) {
                if (peak1.RtAtLeftPeakEdge < peak2.RtAtPeakTop) return true;
            }
            else {
                if (peak2.RtAtLeftPeakEdge < peak1.RtAtPeakTop) return true;
            }
            return false;
        }

        private static List<PeakAreaBean> getPeakAreaBeanProperties(List<PeakAreaBean> peakAreaBeanList,
            ProjectPropertyBean projectProperty, ObservableCollection<RawSpectrum> spectrumCollection, 
            AnalysisParametersBean param)
        {
            peakAreaBeanList = peakAreaBeanList.OrderBy(n => n.RtAtPeakTop).ThenBy(n => n.AccurateMass).ToList();

            var masterPeakID = 0;
            for (int i = 0; i < peakAreaBeanList.Count; i++)
            {
                var peakSpot = peakAreaBeanList[i];
                peakSpot.PeakID = i;
                peakSpot.MasterPeakID = masterPeakID;
                masterPeakID++;

                if (param.IsIonMobility) {
                    foreach (var driftSpot in peakSpot.DriftSpots.OrEmptyIfNull()) {
                        driftSpot.PeakAreaBeanID = i;
                        driftSpot.MasterPeakID = masterPeakID;
                        masterPeakID++;
                    }
                }
                setIsotopicIonInformation(peakAreaBeanList[i], projectProperty, spectrumCollection, param);
            }

            peakAreaBeanList = peakAreaBeanList.OrderBy(n => n.IntensityAtPeakTop).ToList();

            if (peakAreaBeanList.Count - 1 > 0)
                for (int i = 0; i < peakAreaBeanList.Count; i++)
                    peakAreaBeanList[i].AmplitudeScoreValue = (float)((double)i / (double)(peakAreaBeanList.Count - 1));

            return peakAreaBeanList.OrderBy(n => n.PeakID).ToList();
        }

        private static void setIsotopicIonInformation(PeakAreaBean peakAreaBean, ProjectPropertyBean projectProperty, 
            ObservableCollection<RawSpectrum> spectrumCollection, AnalysisParametersBean param)
        {
            var specID = peakAreaBean.Ms1LevelDatapointNumber;
            var tol = param.CentroidMs1Tolerance;
            var centroidFlg = param.PeakDetectionBasedCentroid;
            var spectrum = spectrumCollection[specID].Spectrum;
            var precursorMz = peakAreaBean.AccurateMass;
            var startID = DataAccessLcUtility.GetMs1StartIndex(precursorMz, tol, spectrum);

            double ms1IsotopicIonM1PeakHeight = 0.0, ms1IsotopicIonM2PeakHeight = 0.0;

            for (int i = startID; i < spectrum.Length; i++) {
                if (spectrum[i].Mz <= precursorMz - 0.00632 - tol) continue;
                if (spectrum[i].Mz >= precursorMz + 2.00671 + 0.005844 + tol) break;

                if (spectrum[i].Mz > precursorMz + 1.00335 - 0.00632 - tol && spectrum[i].Mz < precursorMz + 1.00335 + 0.00292 + tol) ms1IsotopicIonM1PeakHeight += spectrum[i].Intensity;
                else if (spectrum[i].Mz > precursorMz + 2.00671 - 0.01264 - tol && spectrum[i].Mz < precursorMz + 2.00671 + 0.00584 + tol) ms1IsotopicIonM2PeakHeight += spectrum[i].Intensity;
            }

            peakAreaBean.Ms1IsotopicIonM1PeakHeight = (float)ms1IsotopicIonM1PeakHeight;
            peakAreaBean.Ms1IsotopicIonM2PeakHeight = (float)ms1IsotopicIonM2PeakHeight;

            if (param.IsIonMobility) {
                foreach (var drift in peakAreaBean.DriftSpots) {
                    drift.Ms1IsotopicIonM1PeakHeight = (float)ms1IsotopicIonM1PeakHeight;
                    drift.Ms1IsotopicIonM2PeakHeight = (float)ms1IsotopicIonM2PeakHeight;
                }
            }
        }


        private static List<PeakAreaBean> filteringPeaksByRawchromatogram(List<PeakAreaBean> peakAreaBeanList, List<double[]> peaklist)
        {
            var newPeakAreas = new List<PeakAreaBean>();

            foreach (var peak in peakAreaBeanList)
            {
                var scanNum = peak.ScanNumberAtPeakTop;
                if (scanNum - 1 < 0 && scanNum + 1 > peaklist.Count - 1) continue;
                //if (peaklist[scanNum - 1][3] <= 0 || peaklist[scanNum + 1][3] <= 0) continue;

                newPeakAreas.Add(peak);
            }

            return newPeakAreas;
        }

        private static List<PeakAreaBean> getBackgroundSubtractPeaks(List<PeakAreaBean> peakAreaBeanList, List<double[]> peaklist, bool backgroundSubtraction)
        {
            if (backgroundSubtraction == false) return peakAreaBeanList;

            var counterThreshold = 4;
            var sPeakAreaList = new List<PeakAreaBean>();

            foreach (var peakArea in peakAreaBeanList)
            {
                var peakTop = peakArea.ScanNumberAtPeakTop;
                var peakLeft = peakArea.ScanNumberAtLeftPeakEdge;
                var peakRight = peakArea.ScanNumberAtRightPeakEdge;
                var trackingNumber = 10 * (peakRight - peakLeft); if (trackingNumber > 50) trackingNumber = 50;

                var ampDiff = Math.Max(peakArea.IntensityAtPeakTop - peakArea.IntensityAtLeftPeakEdge, peakArea.IntensityAtPeakTop - peakArea.IntensityAtRightPeakEdge);
                var counter = 0;

                double spikeMax = -1, spikeMin = -1;
                for (int i = peakLeft - trackingNumber; i <= peakLeft; i++)
                {
                    if (i - 1 < 0) continue;

                    if (peaklist[i - 1][3] < peaklist[i][3] && peaklist[i][3] > peaklist[i + 1][3])
                        spikeMax = peaklist[i][3];
                    else if (peaklist[i - 1][3] > peaklist[i][3] && peaklist[i][3] < peaklist[i + 1][3])
                        spikeMin = peaklist[i][3];

                    if (spikeMax != -1 && spikeMin != -1)
                    {
                        var noise = 0.5 * Math.Abs(spikeMax - spikeMin);
                        if (noise * 3 > ampDiff) counter++;
                        spikeMax = -1; spikeMin = -1;
                    }
                }

                for (int i = peakRight; i <= peakRight + trackingNumber; i++)
                {
                    if (i + 1 > peaklist.Count - 1) break;

                    if (peaklist[i - 1][3] < peaklist[i][3] && peaklist[i][3] > peaklist[i + 1][3])
                        spikeMax = peaklist[i][3];
                    else if (peaklist[i - 1][3] > peaklist[i][3] && peaklist[i][3] < peaklist[i + 1][3])
                        spikeMin = peaklist[i][3];

                    if (spikeMax != -1 && spikeMin != -1)
                    {
                        var noise = 0.5 * Math.Abs(spikeMax - spikeMin);
                        if (noise * 3 > ampDiff) counter++;
                        spikeMax = -1; spikeMin = -1;
                    }
                }

                if (counter < counterThreshold) sPeakAreaList.Add(peakArea);
            }
            return sPeakAreaList;
        }

        private static void progressReports(float focusedMass, float endMass, Action<int> reportAction)
        {
            var progress = initialProgress + focusedMass / endMass * progressMax;
            reportAction?.Invoke(((int)progress));
        }

        #endregion
    }
}

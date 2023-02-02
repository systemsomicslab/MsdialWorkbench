using CompMs.Common.DataObj;
using Msdial.Gcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Msdial.Gcms.Dataprocess.Algorithm
{
    public sealed class PeakSpotting
    {
        private PeakSpotting() { }

        private const double initialProgress = 0.0;
        private const double progressMax = 30.0;

        public static List<PeakAreaBean> GetPeakSpots(List<RawSpectrum> spectrumList, AnalysisParamOfMsdialGcms param, Action<int> reportAction)
        {
            var peaklist = new List<double[]>();
            var detectedPeaksList = new List<List<PeakAreaBean>>();
            var detectedPeaks = new List<PeakAreaBean>();
            var mzRange = DataAccessGcUtility.GetMs1ScanRange(spectrumList, param.IonMode);

            float startMass = mzRange[0], endMass = mzRange[1];
            float focusedMass = startMass, massStep = param.MassSliceWidth, sliceWidth = param.MassSliceWidth;
            if (param.AccuracyType == AccuracyType.IsNominal) { focusedMass = (int)focusedMass; massStep = 1.0F; sliceWidth = 0.5F; }

            Debug.WriteLine("Start {0}, End {1}", startMass, endMass);
            while (focusedMass < endMass)
            {
                if (focusedMass < param.MassRangeBegin) { focusedMass += massStep; continue; }
                if (focusedMass > param.MassRangeEnd) break;
                //Console.WriteLine(focusedMass);
                peaklist = DataAccessGcUtility.GetMs1SlicePeaklist(spectrumList, focusedMass, sliceWidth, param.RetentionTimeBegin, param.RetentionTimeEnd, param.IonMode);

                if (peaklist.Count == 0) { focusedMass += massStep; continue; }

                detectedPeaks = getPeakAreaBeanList(peaklist, spectrumList, param, focusedMass);
                if (detectedPeaks == null || detectedPeaks.Count == 0) { focusedMass += massStep; progressReports(focusedMass, endMass, reportAction); continue; }

                detectedPeaks = filteringPeaksByRawchromatogram(detectedPeaks, peaklist);
                if (detectedPeaks == null || detectedPeaks.Count == 0) { focusedMass += massStep; progressReports(focusedMass, endMass, reportAction); continue; }

                detectedPeaks = getBackgroundSubtractPeaks(detectedPeaks, peaklist, param.BackgroundSubtraction);
                if (detectedPeaks == null || detectedPeaks.Count == 0) { focusedMass += massStep; progressReports(focusedMass, endMass, reportAction); continue; }
                if (param.AccuracyType == AccuracyType.IsAccurate)
                {
                    detectedPeaks = removePeakAreaBeanRedundancy(detectedPeaksList, detectedPeaks, massStep, param);
                    if (detectedPeaks == null || detectedPeaks.Count == 0) { focusedMass += massStep; progressReports(focusedMass, endMass, reportAction); continue; }
                }

                // Debug.WriteLine("{0} is ok", focusedMass);
                
                detectedPeaksList.Add(detectedPeaks);
                focusedMass += massStep;
                progressReports(focusedMass, endMass, reportAction);
            }

            detectedPeaks = getCombinedPeakAreaBeanList(detectedPeaksList);
            detectedPeaks = getPeakAreaBeanProperties(detectedPeaks, spectrumList, param);

            IsotopeEstimator.SetIsotopeInformation(detectedPeaks, param);

            return detectedPeaks;
        }

        private static List<PeakAreaBean> getPeakAreaBeanList(List<double[]> peaklist, List<RawSpectrum> spectrumList, AnalysisParamOfMsdialGcms param, float focusedMass)
        {
            var smoothedPeaklist = DataAccessGcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);

            var minDatapoints = param.MinimumDatapoints;
            var minAmps = param.MinimumAmplitude;
            var detectedPeaks = PeakDetection.PeakDetectionVS1(minDatapoints, minAmps, smoothedPeaklist);
            //var detectedPeaks = PeakDetection.GetDetectedPeakInformationListFromDifferentialBasedPeakDetectionAlgorithm(param.MinimumDatapoints, param.MinimumAmplitude, param.AmplitudeNoiseFactor, param.SlopeNoiseFactor, param.PeaktopNoiseFactor, param.AveragePeakWidth, smoothedPeaklist);

            if (detectedPeaks == null || detectedPeaks.Count == 0) return null;

            var peakAreaBeanList = new List<PeakAreaBean>();

            for (int i = 0; i < detectedPeaks.Count; i++)
            {
                if (detectedPeaks[i].IntensityAtPeakTop <= 0) continue;
                var excludeChecker = false;
                if (param.ExcludedMassList != null && param.ExcludedMassList.Count != 0)
                    for (int j = 0; j < param.ExcludedMassList.Count; j++)
                        if (param.ExcludedMassList[j].ExcludedMass - param.ExcludedMassList[j].MassTolerance <= (float)peaklist[detectedPeaks[i].ScanNumAtPeakTop][2] && (float)peaklist[detectedPeaks[i].ScanNumAtPeakTop][2] <= param.ExcludedMassList[j].ExcludedMass + param.ExcludedMassList[j].MassTolerance) { excludeChecker = true; break; }
                if (excludeChecker) continue;

                var peakAreaBean = DataAccessGcUtility.GetPeakAreaBean(detectedPeaks[i]);
                peakAreaBean.AccurateMass = (float)peaklist[detectedPeaks[i].ScanNumAtPeakTop][2];
                peakAreaBean.Ms1LevelDatapointNumber = (int)peaklist[detectedPeaks[i].ScanNumAtPeakTop][0];
                peakAreaBeanList.Add(peakAreaBean);
            }

            return peakAreaBeanList;
        }

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

        private static List<PeakAreaBean> removePeakAreaBeanRedundancy(List<List<PeakAreaBean>> peakAreaBeanListList, List<PeakAreaBean> peakAreaBeanList, 
            float massStep, AnalysisParamOfMsdialGcms param)
        {
            if (peakAreaBeanListList == null || peakAreaBeanListList.Count == 0) return peakAreaBeanList;

            var parentPeakAreaBeanList = peakAreaBeanListList[peakAreaBeanListList.Count - 1];
            for (int i = 0; i < peakAreaBeanList.Count; i++) {
                for (int j = 0; j < parentPeakAreaBeanList.Count; j++) {
                    if (Math.Abs(parentPeakAreaBeanList[j].AccurateMass - peakAreaBeanList[i].AccurateMass) <=
                        massStep * 0.5) {

                        var isOverlaped = isOverlapedChecker(parentPeakAreaBeanList[j], peakAreaBeanList[i]);
                        if (!isOverlaped) continue;
                        var hwhm = ((parentPeakAreaBeanList[j].RtAtRightPeakEdge - parentPeakAreaBeanList[j].RtAtLeftPeakEdge) +
                            (peakAreaBeanList[i].RtAtRightPeakEdge - peakAreaBeanList[i].RtAtLeftPeakEdge)) * 0.25;

                        var tolerance = Math.Min(hwhm, 0.025);
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

        private static List<PeakAreaBean> getPeakAreaBeanProperties(List<PeakAreaBean> peakAreaBeanList, List<RawSpectrum> spectrumList, AnalysisParamOfMsdialGcms param)
        {
            peakAreaBeanList = peakAreaBeanList.OrderBy(n => n.RtAtPeakTop).ThenBy(n => n.AccurateMass).ToList();

            for (int i = 0; i < peakAreaBeanList.Count; i++)
            {
                peakAreaBeanList[i].PeakID = i;
                setIsotopicIonInformation(peakAreaBeanList[i], spectrumList, param);
            }

            peakAreaBeanList = peakAreaBeanList.OrderBy(n => n.IntensityAtPeakTop).ToList();

            if (peakAreaBeanList.Count - 1 > 0)
                for (int i = 0; i < peakAreaBeanList.Count; i++)
                    peakAreaBeanList[i].AmplitudeScoreValue = (float)((double)i / (double)(peakAreaBeanList.Count - 1));

            return peakAreaBeanList.OrderBy(n => n.PeakID).ToList();
        }

        private static void setIsotopicIonInformation(PeakAreaBean peakAreaBean, List<RawSpectrum> spectrumList, AnalysisParamOfMsdialGcms param)
        {
            var specID = peakAreaBean.Ms1LevelDatapointNumber;
            var tol = param.MassAccuracy; if (param.AccuracyType == AccuracyType.IsNominal) tol = 0.5F;
            var spectrum = DataAccessGcUtility.GetCentroidMasasSpectra(spectrumList, param.DataType, specID, tol, param.AmplitudeCutoff, param.MassRangeBegin, param.MassRangeEnd);
            var precursorMz = peakAreaBean.AccurateMass;
            var startID = DataAccessGcUtility.GetMs1StartIndex(precursorMz, tol, spectrum);

            double ms1IsotopicIonM1PeakHeight = 0.0, ms1IsotopicIonM2PeakHeight = 0.0;

            for (int i = startID; i < spectrum.Count; i++)
            {
                if (spectrum[i][0] <= precursorMz - 0.00632 - tol) continue;
                if (spectrum[i][0] >= precursorMz + 2.00671 + 0.005844 + tol) break;

                if (spectrum[i][0] > precursorMz + 1.00335 - 0.00632 - tol && spectrum[i][0] < precursorMz + 1.00335 + 0.00292 + tol) ms1IsotopicIonM1PeakHeight += spectrum[i][1];
                else if (spectrum[i][0] > precursorMz + 2.00671 - 0.01264 - tol && spectrum[i][0] < precursorMz + 2.00671 + 0.00584 + tol) ms1IsotopicIonM2PeakHeight += spectrum[i][1];
            }

            peakAreaBean.Ms1IsotopicIonM1PeakHeight = (float)ms1IsotopicIonM1PeakHeight;
            peakAreaBean.Ms1IsotopicIonM2PeakHeight = (float)ms1IsotopicIonM2PeakHeight;
        }

        private static List<PeakAreaBean> filteringPeaksByRawchromatogram(List<PeakAreaBean> peakAreaBeanList, List<double[]> peaklist)
        {
            var newPeakAreas = new List<PeakAreaBean>();

            foreach (var peak in peakAreaBeanList)
            {
                var scanNum = peak.ScanNumberAtPeakTop;
                if (scanNum - 1 < 0 || scanNum + 1 > peaklist.Count - 1) continue;
                if (peaklist[scanNum - 1][3] <= 0 || peaklist[scanNum + 1][3] <= 0) continue;

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
            reportAction?.Invoke((int)progress);
        }
    }
}

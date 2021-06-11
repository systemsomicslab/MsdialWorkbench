using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Algorithm {
    public class PeakSpottingCore {

        public double InitialProgress { get; set; } = 0.0;
        public double ProgressMax { get; set; } = 30.0;

        // feature detection for rt (or ion mobility), m/z, and intensity (3D) data 
        // this method can be used in GC-MS, LC-MS, and IM-MS project
        public List<ChromatogramPeakFeature> Execute3DFeatureDetection(RawMeasurement rawObj, ParameterBase param,
            float chromBegin, float chromEnd, ChromXType type, ChromXUnit unit,
            Action<int> reportAction) {
            var isTargetedMode = !param.CompoundListInTargetMode.IsEmptyOrNull();
            if (isTargetedMode) {
                return Execute3DFeatureDetectionTargetMode(rawObj, param, chromBegin, chromEnd, type, unit, reportAction);
            }
            var spectrumList = rawObj.SpectrumList;
            var chromPeakFeaturesList = new List<List<ChromatogramPeakFeature>>();

            float[] mzRange = DataAccess.GetMs1Range(spectrumList, param.IonMode);
            float startMass = mzRange[0]; if (startMass < param.MassRangeBegin) startMass = param.MassRangeBegin;
            float endMass = mzRange[1]; if (endMass > param.MassRangeEnd) endMass = param.MassRangeEnd;
            float focusedMass = startMass, massStep = param.MassSliceWidth;

            if (param.AccuracyType == AccuracyType.IsNominal) { massStep = 1.0F; }
            while (focusedMass < endMass) {
                if (focusedMass < param.MassRangeBegin) { focusedMass += massStep; continue; }
                if (focusedMass > param.MassRangeEnd) break;
                //Console.WriteLine(focusedMass);
                var chromPeakFeatures = GetChromatogramPeakFeatures(rawObj, focusedMass, param, type, unit, chromBegin, chromEnd);
                if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) { focusedMass += massStep; ReportProgress.Show(InitialProgress, ProgressMax, focusedMass, endMass, reportAction); continue; }

                //removing peak spot redundancies among slices
                chromPeakFeatures = RemovePeakAreaBeanRedundancy(chromPeakFeaturesList, chromPeakFeatures, massStep);
                if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) { focusedMass += massStep; ReportProgress.Show(InitialProgress, ProgressMax, focusedMass, endMass, reportAction); continue; }

                chromPeakFeaturesList.Add(chromPeakFeatures);
                focusedMass += massStep;
                ReportProgress.Show(InitialProgress, ProgressMax, focusedMass, endMass, reportAction);
            }

            var cmbinedFeatures = GetCombinedChromPeakFeatures(chromPeakFeaturesList);
            cmbinedFeatures = GetRecalculatedChromPeakFeaturesByMs1MsTolerance(cmbinedFeatures, rawObj, param, type, unit);
            cmbinedFeatures = GetOtherChromPeakFeatureProperties(cmbinedFeatures, spectrumList, param);

            return cmbinedFeatures;
        }

        public List<ChromatogramPeakFeature> Execute3DFeatureDetectionTargetMode(RawMeasurement rawObj, ParameterBase param,
            float chromBegin, float chromEnd, ChromXType type, ChromXUnit unit,
            Action<int> reportAction) {
            var spectrumList = rawObj.SpectrumList;
            var chromPeakFeaturesList = new List<List<ChromatogramPeakFeature>>();
            var targetedScans = param.CompoundListInTargetMode;
            if (targetedScans.IsEmptyOrNull()) return null;
            foreach (var targetComp in targetedScans) {
                var chromPeakFeatures = GetChromatogramPeakFeatures(rawObj, (float)targetComp.PrecursorMz, param, type, unit, chromBegin, chromEnd);
                if (!chromPeakFeatures.IsEmptyOrNull())
                    chromPeakFeaturesList.Add(chromPeakFeatures);
            }

            var cmbinedFeatures = GetCombinedChromPeakFeatures(chromPeakFeaturesList);
            cmbinedFeatures = GetRecalculatedChromPeakFeaturesByMs1MsTolerance(cmbinedFeatures, rawObj, param, type, unit);
            cmbinedFeatures = GetOtherChromPeakFeatureProperties(cmbinedFeatures, spectrumList, param);

            return cmbinedFeatures;
        }

        public List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(RawMeasurement rawObj, float focusedMass, 
            ParameterBase param, ChromXType type, ChromXUnit unit, float chromBegin, float chromEnd) {

            var spectrumList = rawObj.SpectrumList;
            //get EIC chromatogram
            var peaklist = DataAccess.GetMs1Peaklist(spectrumList, focusedMass, param.MassSliceWidth, param.IonMode, type, unit, chromBegin, chromEnd);
            if (peaklist.Count == 0) return null;

            //get peak detection result
            var chromPeakFeatures = GetChromatogramPeakFeatures(peaklist, param, type, unit);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;
            SetRawDataAccessID2ChromatogramPeakFeatures(chromPeakFeatures, rawObj, peaklist, param);

            //filtering out noise peaks considering smoothing effects and baseline effects
            chromPeakFeatures = GetBackgroundSubtractedPeaks(chromPeakFeatures, peaklist);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;

            return chromPeakFeatures;
        }

        #region ion mobility utilities
        public static List<ChromatogramPeakFeature> ExecutePeakDetectionOnDriftTimeAxis(List<ChromatogramPeakFeature> chromPeakFeatures, RawMeasurement rawObj,
           ParameterBase param, float accumulatedRtRange) {
            var newSpots = new List<ChromatogramPeakFeature>();
            var spectrumList = rawObj.SpectrumList;
            foreach (var peakSpot in chromPeakFeatures) {
                peakSpot.DriftChromFeatures = new List<ChromatogramPeakFeature>();

                var scanID = peakSpot.MS1RawSpectrumIdTop;
                var rt = peakSpot.ChromXs.Value;
                var mz = peakSpot.Mass;
                //var rtWidth = peakSpot.PeakWidth();
                //if (rtWidth > 0.6) rtWidth = 0.6F;
                //if (rtWidth < 0.2) rtWidth = 0.2F;
                var mztol = param.CentroidMs1Tolerance;

                // accumulatedRtRange can be replaced by rtWidth actually, but for alignment results, we have to adjust the RT range to equally estimate the peaks on drift axis
                var peaklist = DataAccess.GetDriftChromatogramByScanRtMz(spectrumList, scanID, (float)rt, accumulatedRtRange, (float)mz, mztol);
                if (peaklist.Count == 0) continue;
                var peaksOnDriftTime = GetPeakAreaBeanListOnDriftTimeAxis(peaklist, peakSpot, rawObj, param);
                if (peaksOnDriftTime == null || peaksOnDriftTime.Count == 0) continue;
                peakSpot.DriftChromFeatures = peaksOnDriftTime;
                newSpots.Add(peakSpot);
            }
            return newSpots;
        }

        private static List<ChromatogramPeakFeature> GetPeakAreaBeanListOnDriftTimeAxis(List<ChromatogramPeak> peaklist,
          ChromatogramPeakFeature rtPeakFeature, RawMeasurement rawObj, ParameterBase param) {

            var spectrumList = rawObj.SpectrumList;
            var smoothedPeaklist = DataAccess.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
            var minDatapoints = param.MinimumDatapoints;
            var minAmps = param.MinimumAmplitude * 0.25;
            var detectedPeaks = PeakDetection.PeakDetectionVS1(smoothedPeaklist, minDatapoints, minAmps);
            var ceTargets = rawObj.CollisionEnergyTargets;

            if (detectedPeaks == null || detectedPeaks.Count == 0) return null;

            var peaks = new List<ChromatogramPeakFeature>();
            var counter = 0;

            var rtPeakLeftScanID = rtPeakFeature.MS1RawSpectrumIdLeft;
            var rtPeakTopScanID = rtPeakFeature.MS1RawSpectrumIdTop;
            var rtPeakRightScanID = rtPeakFeature.MS1RawSpectrumIdRight;

            var maxIntensityAtPeaks = detectedPeaks.Max(n => n.IntensityAtPeakTop);
            var scanPolarity = param.IonMode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;
            var mass = rtPeakFeature.Mass;
            var ms2Tol = param.CentroidMs2Tolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms2Tol));
            #region // practical parameter changes
            if (mass > 500) {
                ms2Tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ppm);
            }
            #endregion

            foreach (var result in detectedPeaks.Where(n => n.IntensityAtPeakTop > 0)) {
                var edgeIntensity = (result.IntensityAtLeftPeakEdge + result.IntensityAtRightPeakEdge) * 0.5;
                var peakheightFromEdge = result.IntensityAtPeakTop - edgeIntensity;
                if (peakheightFromEdge < maxIntensityAtPeaks * 0.1) continue;

                var driftFeature = DataAccess.GetChromatogramPeakFeature(result, ChromXType.Drift, ChromXUnit.Msec, mass);

                driftFeature.PeakID = counter;
                driftFeature.ParentPeakID = rtPeakFeature.PeakID;

                //assign the scan number of MS1 and MS/MS for precursor ion's peaks
                driftFeature.MS1RawSpectrumIdTop = peaklist[result.ScanNumAtPeakTop].ID;
                driftFeature.MS1RawSpectrumIdLeft = peaklist[result.ScanNumAtLeftPeakEdge].ID;
                driftFeature.MS1RawSpectrumIdRight = peaklist[result.ScanNumAtRightPeakEdge].ID;

                var dt = driftFeature.ChromXs.Drift.Value;
                var scanTop = driftFeature.MS1RawSpectrumIdTop;
                var minDiff = int.MaxValue;
                var ce2MinDiff = new Dictionary<double, double>(); // ce to diff
                if (!ceTargets.IsEmptyOrNull()) {
                    foreach (var ce in ceTargets) ce2MinDiff[ce] = double.MaxValue;
                }

                for (int i = rtPeakLeftScanID; i <= rtPeakRightScanID; i++) {
                    var spec = spectrumList[i];
                    if (spec.MsLevel != 2 || scanPolarity != spec.ScanPolarity) continue;

                    var IsMassInWindow = IsInMassWindow(mass, spec, ms2Tol, param.AcquisitionType);
                    var IsDtInWindow = Math.Min(spec.Precursor.TimeBegin, spec.Precursor.TimeEnd) <= dt && dt < Math.Max(spec.Precursor.TimeBegin, spec.Precursor.TimeEnd)
                        ? true : false; // used for diapasef
                    if (spec.Precursor.TimeBegin == spec.Precursor.TimeEnd) IsDtInWindow = true; // normal dia

                    if (IsMassInWindow && IsMassInWindow) {
                        var ce = spec.CollisionEnergy;
                        if (param.AcquisitionType == AcquisitionType.AIF) {
                            var ceRounded = Math.Round(ce, 2); // must be rounded by 2 decimal points
                            if (ce2MinDiff[ceRounded] > Math.Abs(spec.OriginalIndex - scanTop)) {
                                ce2MinDiff[ceRounded] = Math.Abs(spec.OriginalIndex - scanTop);
                                driftFeature.MS2RawSpectrumID2CE[spec.OriginalIndex] = ce;
                            }
                        }
                        else {
                            driftFeature.MS2RawSpectrumID2CE[spec.OriginalIndex] = ce;
                        }
                        if (minDiff > Math.Abs(spec.OriginalIndex - scanTop)) {
                            minDiff = Math.Abs(spec.OriginalIndex - scanTop);
                            driftFeature.MS2RawSpectrumID = spec.OriginalIndex;
                        }
                    }
                }

                peaks.Add(driftFeature);
                counter++;
            }
            return peaks;
        }
        #endregion

        #region peak detection utilities
        public List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(List<ChromatogramPeak> peaklist, ParameterBase param,
            ChromXType type, ChromXUnit unit) {
            var smoothedPeaklist = DataAccess.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
            //var detectedPeaks = PeakDetection.GetDetectedPeakInformationCollectionFromDifferentialBasedPeakDetectionAlgorithm(analysisParametersBean.MinimumDatapoints, analysisParametersBean.MinimumAmplitude, analysisParametersBean.AmplitudeNoiseFactor, analysisParametersBean.SlopeNoiseFactor, analysisParametersBean.PeaktopNoiseFactor, smoothedPeaklist);
            var minDatapoints = param.MinimumDatapoints;
            var minAmps = param.MinimumAmplitude;
            var detectedPeaks = PeakDetection.PeakDetectionVS1(smoothedPeaklist, minDatapoints, minAmps);
            if (detectedPeaks == null || detectedPeaks.Count == 0) return null;

            var chromPeakFeatures = new List<ChromatogramPeakFeature>();

            foreach (var result in detectedPeaks) {
                if (result.IntensityAtPeakTop <= 0) continue;
                var mass = peaklist[result.ScanNumAtPeakTop].Mass;

                //option
                //this method is currently used in LC/MS project.
                //Users can prepare their-own 'exclusion mass' list to exclude unwanted peak features
                var excludeChecker = false;
                foreach (var pair in param.ExcludedMassList.OrEmptyIfNull()) {
                    if (Math.Abs(pair.Mass - mass) < pair.MassTolerance) {
                        excludeChecker = true;
                        break;
                    }
                }
                if (excludeChecker) continue;
                var chromPeakFeature = DataAccess.GetChromatogramPeakFeature(result, type, unit, mass);
                chromPeakFeatures.Add(chromPeakFeature);
            }
            return chromPeakFeatures;
        }

        /// <summary>
        /// peak list should contain the original raw spectrum ID
        /// </summary>
        /// <param name="chromPeakFeatures"></param>
        /// <param name="accSpecList"></param>
        /// <param name="peaklist"></param>
        /// <param name="param"></param>
        public void SetRawDataAccessID2ChromatogramPeakFeaturesFor4DChromData(List<ChromatogramPeakFeature> chromPeakFeatures, List<RawSpectrum> accSpecList,
            List<ChromatogramPeak> peaklist, ParameterBase param) {
            foreach (var feature in chromPeakFeatures) {
                SetRawDataAccessID2ChromatogramPeakFeatureFor4DChromData(feature, accSpecList, peaklist, param);
            }
        }

        private void SetRawDataAccessID2ChromatogramPeakFeatureFor4DChromData(ChromatogramPeakFeature feature, List<RawSpectrum> accSpecList, 
            List<ChromatogramPeak> peaklist, ParameterBase param) {

            var chromLeftID = feature.ChromScanIdLeft;
            var chromTopID = feature.ChromScanIdTop;
            var chromRightID = feature.ChromScanIdRight;

            feature.MS1AccumulatedMs1RawSpectrumIdLeft = peaklist[chromLeftID].ID;
            feature.MS1AccumulatedMs1RawSpectrumIdTop = peaklist[chromTopID].ID;
            feature.MS1AccumulatedMs1RawSpectrumIdRight = peaklist[chromRightID].ID;

            feature.MS1RawSpectrumIdLeft = accSpecList[peaklist[chromLeftID].ID].OriginalIndex;
            feature.MS1RawSpectrumIdTop = accSpecList[peaklist[chromTopID].ID].OriginalIndex;
            feature.MS1RawSpectrumIdRight = accSpecList[peaklist[chromRightID].ID].OriginalIndex;

            feature.MS2RawSpectrumID = -1; // at this moment, zero must be inserted for deconvolution process

        }

        /// <summary>
        /// peak list should contain the original raw spectrum ID
        /// </summary>
        /// <param name="chromPeakFeatures"></param>
        /// <param name="spectrumList"></param>
        /// <param name="peaklist"></param>
        /// <param name="param"></param>
        public void SetRawDataAccessID2ChromatogramPeakFeatures(List<ChromatogramPeakFeature> chromPeakFeatures, RawMeasurement rawObj,
            List<ChromatogramPeak> peaklist, ParameterBase param) {
            foreach (var feature in chromPeakFeatures) {
                SetRawDataAccessID2ChromatogramPeakFeature(feature, rawObj, peaklist, param);
            }
        }

        public void SetRawDataAccessID2ChromatogramPeakFeature(ChromatogramPeakFeature feature, RawMeasurement rawObj, List<ChromatogramPeak> peaklist, ParameterBase param) {
            var chromLeftID = feature.ChromScanIdLeft;
            var chromTopID = feature.ChromScanIdTop;
            var chromRightID = feature.ChromScanIdRight;

            feature.MS1RawSpectrumIdLeft = peaklist[chromLeftID].ID;
            feature.MS1RawSpectrumIdTop = peaklist[chromTopID].ID;
            feature.MS1RawSpectrumIdRight = peaklist[chromRightID].ID;

            SetMS2RawSpectrumIDs2ChromatogramPeakFeature(feature, rawObj, param, peaklist[chromLeftID].ID, peaklist[chromTopID].ID, peaklist[chromRightID].ID);
        }

        public void SetMS2RawSpectrumIDs2ChromatogramPeakFeature(ChromatogramPeakFeature feature, RawMeasurement rawObj, ParameterBase param,
            int scanBegin, int scanTop, int scanEnd) {

            var mass = feature.Mass;
            var minDiff = int.MaxValue;
            var spectrumList = rawObj.SpectrumList;
            var ceTargets = rawObj.CollisionEnergyTargets;
            var ce2MinDiff = new Dictionary<double, double>(); // ce to diff
            if (!ceTargets.IsEmptyOrNull()) {
                foreach (var ce in ceTargets) ce2MinDiff[ce] = double.MaxValue;
            }

            var scanPolarity = param.IonMode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;
            if (scanBegin < 0) scanBegin = 0;
            var ms2Tol = param.CentroidMs2Tolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms2Tol));
            #region // practical parameter changes
            if (mass > 500) {
                ms2Tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ppm);
            }
            #endregion

            for (int i = scanBegin; i <= scanEnd; i++) {
                var spec = spectrumList[i];
                if (spec.MsLevel <= 1) continue;
                if (spec.MsLevel == 2 && spec.Precursor != null && scanPolarity == spec.ScanPolarity) {
                    if (IsInMassWindow(mass, spec, ms2Tol, param.AcquisitionType)) {
                        var ce = spec.CollisionEnergy;

                        if (param.AcquisitionType == AcquisitionType.AIF) {
                            var ceRounded = Math.Round(ce, 2); // must be rounded by 2 decimal points
                            if (ce2MinDiff[ceRounded] > Math.Abs(spec.ScanNumber - scanTop)) {
                                ce2MinDiff[ceRounded] = Math.Abs(spec.ScanNumber - scanTop);
                                feature.MS2RawSpectrumID2CE[spec.ScanNumber] = ce;
                            }
                        }
                        else {
                            feature.MS2RawSpectrumID2CE[spec.ScanNumber] = ce;
                        }

                        if (minDiff > Math.Abs(spec.ScanNumber - scanTop)) {
                            minDiff = Math.Abs(spec.ScanNumber - scanTop);
                            feature.MS2RawSpectrumID = spec.ScanNumber;
                        }
                    }
                }
            }
        }

        public List<ChromatogramPeakFeature> GetBackgroundSubtractedPeaks(List<ChromatogramPeakFeature> chromPeakFeatures, List<ChromatogramPeak> peaklist) {
            var counterThreshold = 4;
            var sPeakAreaList = new List<ChromatogramPeakFeature>();

            foreach (var feature in chromPeakFeatures) {
                var peakTop = feature.ChromScanIdTop;
                var peakLeft = feature.ChromScanIdLeft;
                var peakRight = feature.ChromScanIdRight;

                if (peakTop - 1 < 0 || peakTop + 1 > peaklist.Count - 1) continue;
                if (peaklist[peakTop - 1].Intensity <= 0 || peaklist[peakTop + 1].Intensity <= 0) continue;

                var trackingNumber = 10 * (peakRight - peakLeft); if (trackingNumber > 50) trackingNumber = 50;

                var ampDiff = Math.Max(feature.PeakHeightTop - feature.PeakHeightLeft, feature.PeakHeightTop - feature.PeakHeightRight);
                var counter = 0;

                double spikeMax = -1, spikeMin = -1;
                for (int i = peakLeft - trackingNumber; i <= peakLeft; i++) {
                    if (i - 1 < 0) continue;

                    if (peaklist[i - 1].Intensity < peaklist[i].Intensity && peaklist[i].Intensity > peaklist[i + 1].Intensity)
                        spikeMax = peaklist[i].Intensity;
                    else if (peaklist[i - 1].Intensity > peaklist[i].Intensity && peaklist[i].Intensity < peaklist[i + 1].Intensity)
                        spikeMin = peaklist[i].Intensity;

                    if (spikeMax != -1 && spikeMin != -1) {
                        var noise = 0.5 * Math.Abs(spikeMax - spikeMin);
                        if (noise * 3 > ampDiff) counter++;
                        spikeMax = -1; spikeMin = -1;
                    }
                }

                for (int i = peakRight; i <= peakRight + trackingNumber; i++) {
                    if (i + 1 > peaklist.Count - 1) break;

                    if (peaklist[i - 1].Intensity < peaklist[i].Intensity && peaklist[i].Intensity > peaklist[i + 1].Intensity)
                        spikeMax = peaklist[i].Intensity;
                    else if (peaklist[i - 1].Intensity > peaklist[i].Intensity && peaklist[i].Intensity < peaklist[i + 1].Intensity)
                        spikeMin = peaklist[i].Intensity;

                    if (spikeMax != -1 && spikeMin != -1) {
                        var noise = 0.5 * Math.Abs(spikeMax - spikeMin);
                        if (noise * 3 > ampDiff) counter++;
                        spikeMax = -1; spikeMin = -1;
                    }
                }

                if (counter < counterThreshold) sPeakAreaList.Add(feature);
            }
            return sPeakAreaList;
        }

        public List<ChromatogramPeakFeature> RemovePeakAreaBeanRedundancy(List<List<ChromatogramPeakFeature>> chromPeakFeaturesList,
           List<ChromatogramPeakFeature> chromPeakFeatures, float massStep) {
            if (chromPeakFeaturesList == null || chromPeakFeaturesList.Count == 0) return chromPeakFeatures;

            var parentPeakAreaBeanList = chromPeakFeaturesList[chromPeakFeaturesList.Count - 1];

            for (int i = 0; i < chromPeakFeatures.Count; i++) {
                for (int j = 0; j < parentPeakAreaBeanList.Count; j++) {
                    if (Math.Abs(parentPeakAreaBeanList[j].Mass - chromPeakFeatures[i].Mass) <=
                        massStep * 0.5) {

                        var isOverlaped = isOverlapedChecker(parentPeakAreaBeanList[j], chromPeakFeatures[i]);
                        if (!isOverlaped) continue;
                        var hwhm = ((parentPeakAreaBeanList[j].ChromXsRight.Value - parentPeakAreaBeanList[j].ChromXsLeft.Value) +
                            (chromPeakFeatures[i].ChromXsRight.Value - chromPeakFeatures[i].ChromXsLeft.Value)) * 0.25;

                        var tolerance = Math.Min(hwhm, 0.03);
                        if (Math.Abs(parentPeakAreaBeanList[j].ChromXs.Value - chromPeakFeatures[i].ChromXs.Value) <= tolerance) {
                            if (chromPeakFeatures[i].PeakHeightTop > parentPeakAreaBeanList[j].PeakHeightTop) {
                                parentPeakAreaBeanList.RemoveAt(j);
                                j--;
                                continue;
                            }
                            else {
                                chromPeakFeatures.RemoveAt(i);
                                i--;
                                break;
                            }
                        }
                    }
                }
                if (parentPeakAreaBeanList == null || parentPeakAreaBeanList.Count == 0) return chromPeakFeatures;
                if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;
            }
            return chromPeakFeatures;
        }

        private bool isOverlapedChecker(ChromatogramPeakFeature peak1, ChromatogramPeakFeature peak2) {
            if (peak1.ChromXs.Value > peak2.ChromXs.Value) {
                if (peak1.ChromXsLeft.Value < peak2.ChromXs.Value) return true;
            }
            else {
                if (peak2.ChromXsLeft.Value < peak1.ChromXs.Value) return true;
            }
            return false;
        }

        public List<ChromatogramPeakFeature> GetCombinedChromPeakFeatures(List<List<ChromatogramPeakFeature>> chromPeakFeaturesList) {
            var combinedFeatures = new List<ChromatogramPeakFeature>();

            for (int i = 0; i < chromPeakFeaturesList.Count; i++) {
                if (chromPeakFeaturesList[i].Count == 0) continue;
                for (int j = 0; j < chromPeakFeaturesList[i].Count; j++)
                    combinedFeatures.Add(chromPeakFeaturesList[i][j]);
            }

            return combinedFeatures;
        }

        public List<ChromatogramPeakFeature> GetRecalculatedChromPeakFeaturesByMs1MsTolerance(List<ChromatogramPeakFeature> chromPeakFeatures,
            RawMeasurement rawObj, ParameterBase param, ChromXType type, ChromXUnit unit) {
            var spectrumList = param.MachineCategory == MachineCategory.LCIMMS ? rawObj.AccumulatedSpectrumList : rawObj.SpectrumList;
            var recalculatedPeakspots = new List<ChromatogramPeakFeature>();
            var minDatapoint = 3;
            // var counter = 0;
            foreach (var spot in chromPeakFeatures) {
                //get EIC chromatogram
                var peakWidth = spot.PeakWidth();
                var peakWidthMargin = spot.PeakWidth() * 0.5;
                var peaklist = DataAccess.GetMs1Peaklist(spectrumList, (float)spot.Mass, param.CentroidMs1Tolerance, param.IonMode, type, unit,
                    (float)(spot.ChromXsLeft.Value - peakWidthMargin), (float)(spot.ChromXsRight.Value + peakWidthMargin));

                var sPeaklist = DataAccess.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
                var maxID = -1;
                var maxInt = double.MinValue;
                var minRtId = -1;
                var minRtValue = double.MaxValue;
                var peakAreaAboveZero = 0.0;
                var peakAreaAboveBaseline = 0.0;
                for (int i = 0; i < sPeaklist.Count - 1; i++) {
                    if (Math.Abs(sPeaklist[i].ChromXs.Value - spot.ChromXs.Value) < minRtValue) {
                        minRtValue = Math.Abs(sPeaklist[i].ChromXs.Value - spot.ChromXs.Value);
                        minRtId = i;
                    }
                }

                //finding local maximum within -2 ~ +2
                for (int i = minRtId - 2; i <= minRtId + 2; i++) {
                    if (i - 1 < 0) continue;
                    if (i > sPeaklist.Count - 2) break;
                    if (sPeaklist[i].Intensity > maxInt &&
                        sPeaklist[i - 1].Intensity <= sPeaklist[i].Intensity &&
                        sPeaklist[i].Intensity >= sPeaklist[i + 1].Intensity) {
                        maxInt = sPeaklist[i].Intensity;
                        maxID = i;
                    }
                }

                //for (int i = minRtId - 2; i <= minRtId + 2; i++) {
                //    if (i < 0) continue;
                //    if (i > sPeaklist.Count - 1) break;
                //    if (sPeaklist[i].Intensity > maxInt) {
                //        maxInt = sPeaklist[i].Intensity;
                //        maxID = i;
                //    }
                //}

                if (maxID == -1) {
                    maxInt = sPeaklist[minRtId].Intensity;
                    maxID = minRtId;
                }

                //finding left edge;
                //seeking left edge
                var minLeftInt = sPeaklist[maxID].Intensity;
                var minLeftId = -1;
                for (int i = maxID - minDatapoint; i >= 0; i--) {

                    if (i < maxID && minLeftInt < sPeaklist[i].Intensity) {
                        break;
                    }
                    if (sPeaklist[maxID].ChromXs.Value - peakWidth > sPeaklist[i].ChromXs.Value) break;

                    if (minLeftInt >= sPeaklist[i].Intensity) {
                        minLeftInt = sPeaklist[i].Intensity;
                        minLeftId = i;
                    }
                }
                if (minLeftId == -1) {

                    var minOriginalLeftRtDiff = double.MaxValue;
                    var minOriginalLeftID = maxID - minDatapoint;
                    if (minOriginalLeftID < 0) minOriginalLeftID = 0;
                    for (int i = maxID; i >= 0; i--) {
                        var diff = Math.Abs(sPeaklist[i].ChromXs.Value - spot.ChromXsLeft.Value);
                        if (diff < minOriginalLeftRtDiff) {
                            minOriginalLeftRtDiff = diff;
                            minOriginalLeftID = i;
                        }
                    }

                    minLeftId = minOriginalLeftID;
                }

                //finding right edge;
                var minRightInt = sPeaklist[maxID].Intensity;
                var minRightId = -1;
                for (int i = maxID + minDatapoint; i < sPeaklist.Count - 1; i++) {

                    if (i > maxID && minRightInt < sPeaklist[i].Intensity) {
                        break;
                    }
                    if (sPeaklist[maxID].ChromXs.Value + peakWidth < sPeaklist[i].ChromXs.Value) break;
                    if (minRightInt >= sPeaklist[i].Intensity) {
                        minRightInt = sPeaklist[i].Intensity;
                        minRightId = i;
                    }
                }
                if (minRightId == -1) {

                    var minOriginalRightRtDiff = double.MaxValue;
                    var minOriginalRightID = maxID + minDatapoint;
                    if (minOriginalRightID > sPeaklist.Count - 1) minOriginalRightID = sPeaklist.Count - 1;
                    for (int i = maxID; i < sPeaklist.Count; i++) {
                        var diff = Math.Abs(sPeaklist[i].ChromXs.Value - spot.ChromXsRight.Value);
                        if (diff < minOriginalRightRtDiff) {
                            minOriginalRightRtDiff = diff;
                            minOriginalRightID = i;
                        }
                    }

                    minRightId = minOriginalRightID;
                }

                if (Math.Max(sPeaklist[minLeftId].Intensity, sPeaklist[minRightId].Intensity) >= sPeaklist[maxID].Intensity) continue;
                if (sPeaklist[maxID].Intensity - Math.Min(sPeaklist[minLeftId].Intensity, sPeaklist[minRightId].Intensity) < param.MinimumAmplitude) continue;

                //calculating peak area and finding real max ID
                var realMaxInt = double.MinValue;
                var realMaxID = maxID;
                for (int i = minLeftId; i <= minRightId - 1; i++) {
                    if (realMaxInt < sPeaklist[i].Intensity) {
                        realMaxInt = sPeaklist[i].Intensity;
                        realMaxID = i;
                    }

                    peakAreaAboveZero += (sPeaklist[i].Intensity + sPeaklist[i + 1].Intensity) * (sPeaklist[i + 1].ChromXs.Value - sPeaklist[i].ChromXs.Value) * 0.5;
                }


                maxID = realMaxID;

                peakAreaAboveBaseline = peakAreaAboveZero - (sPeaklist[minLeftId].Intensity + sPeaklist[minRightId].Intensity) *
                    (sPeaklist[minRightId].ChromXs.Value - sPeaklist[minLeftId].ChromXs.Value) / 2;

                spot.PeakAreaAboveBaseline = peakAreaAboveBaseline * 60.0;
                spot.PeakAreaAboveZero = peakAreaAboveZero * 60.0;

                spot.ChromXs = sPeaklist[maxID].ChromXs;
                spot.ChromXsTop = sPeaklist[maxID].ChromXs;
                spot.ChromXsLeft = sPeaklist[minLeftId].ChromXs;
                spot.ChromXsRight = sPeaklist[minRightId].ChromXs;

                spot.PeakHeightTop = sPeaklist[maxID].Intensity;
                spot.PeakHeightLeft = sPeaklist[minLeftId].Intensity;
                spot.PeakHeightRight = sPeaklist[minRightId].Intensity;

                spot.ChromScanIdTop = sPeaklist[maxID].ID;
                spot.ChromScanIdLeft = sPeaklist[minLeftId].ID;
                spot.ChromScanIdRight = sPeaklist[minRightId].ID;

                spot.MS1RawSpectrumIdTop = peaklist[maxID].ID;
                spot.MS1RawSpectrumIdLeft = peaklist[minLeftId].ID;
                spot.MS1RawSpectrumIdRight = peaklist[minRightId].ID;

                var peakHeightFromBaseline = Math.Max(sPeaklist[maxID].Intensity - sPeaklist[minLeftId].Intensity, sPeaklist[maxID].Intensity - sPeaklist[minRightId].Intensity);
                spot.PeakShape.SignalToNoise = (float)(peakHeightFromBaseline / spot.PeakShape.EstimatedNoise);

                if (spot.DriftChromFeatures == null) { // meaning not ion mobility data
                    SetMS2RawSpectrumIDs2ChromatogramPeakFeature(spot, rawObj, param,
                        spot.MS1RawSpectrumIdLeft, spot.MS1RawSpectrumIdTop, spot.MS1RawSpectrumIdRight);
                }
                recalculatedPeakspots.Add(spot);
            }
            return recalculatedPeakspots;
        }

        public List<ChromatogramPeakFeature> GetOtherChromPeakFeatureProperties(List<ChromatogramPeakFeature> chromPeakFeatures, List<RawSpectrum> spectrumList, ParameterBase param) {
            chromPeakFeatures = chromPeakFeatures.OrderBy(n => n.ChromXs.Value).ThenBy(n => n.Mass).ToList();

            var masterPeakID = 0; // used for LC-IM-MS/MS project
            for (int i = 0; i < chromPeakFeatures.Count; i++) {
                var peakSpot = chromPeakFeatures[i];
                peakSpot.PeakID = i;
                peakSpot.MasterPeakID = masterPeakID;
                masterPeakID++;

                if (peakSpot.DriftChromFeatures != null && peakSpot.DriftChromFeatures.Count > 0) {
                    foreach (var driftSpot in peakSpot.DriftChromFeatures) {
                        driftSpot.ParentPeakID = i;
                        driftSpot.MasterPeakID = masterPeakID;
                        masterPeakID++;
                    }
                }
            }

            chromPeakFeatures = chromPeakFeatures.OrderBy(n => n.PeakHeightTop).ToList();

            if (chromPeakFeatures.Count - 1 > 0) {
                for (int i = 0; i < chromPeakFeatures.Count; i++) {
                    chromPeakFeatures[i].PeakShape.AmplitudeScoreValue = (float)((double)i / (double)(chromPeakFeatures.Count - 1));
                    chromPeakFeatures[i].PeakShape.AmplitudeOrderValue = i;
                }
            }

            return chromPeakFeatures.OrderBy(n => n.PeakID).ToList();
        }
        #endregion

        private static bool IsInMassWindow(double mass, RawSpectrum spec, double ms2Tol, AcquisitionType type) {
            var specPrecMz = spec.Precursor.SelectedIonMz;
            switch (type) {
                case AcquisitionType.AIF:
                case AcquisitionType.SWATH:
                    var lowerOffset = spec.Precursor.IsolationWindowLowerOffset;
                    var upperOffset = spec.Precursor.IsolationWindowUpperOffset;
                    return specPrecMz - lowerOffset - ms2Tol < mass && mass < specPrecMz + upperOffset + ms2Tol;
                case AcquisitionType.DDA:
                    return Math.Abs(specPrecMz - mass) < ms2Tol;
                default:
                    throw new NotSupportedException(nameof(type));
            }
        }
    }
}

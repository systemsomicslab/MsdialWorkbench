using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcImMsApi.Parameter;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialLcImMsApi.Algorithm {
    public class PeakSpotting {

        public double InitialProgress { get; set; } = 0.0;
        public double ProgressMax { get; set; } = 30.0;

        public PeakSpottingCore PeakSpottingCore = new PeakSpottingCore();

        public PeakSpotting(double initialProgress, double progressMax) {
            InitialProgress = initialProgress;
            ProgressMax = progressMax;
        }

        // feature detection for rt, ion mobility, m/z, and intensity (3D) data 
        public List<ChromatogramPeakFeature> Execute4DFeatureDetection(
            RawMeasurement rawObj, MsdialLcImMsParameter param, Action<int> reportAction) {

            var accumulatedMs1Spectrum = rawObj.AccumulatedSpectrumList;// used for rt, mz, intensity (3D) data
            var allSpectrum = rawObj.SpectrumList; // each snap shot contains rt, dt, spec data
            var chromPeakFeaturesList = new List<List<ChromatogramPeakFeature>>();
            var isTargetedMode = !param.CompoundListInTargetMode.IsEmptyOrNull();
            if (isTargetedMode) {
                var targetedScans = param.CompoundListInTargetMode;
                foreach (var targetComp in targetedScans) {
                    var chromPeakFeatures = GetChromatogramPeakFeatures(rawObj, (float)targetComp.PrecursorMz, param);
                    if (!chromPeakFeatures.IsEmptyOrNull())
                        chromPeakFeaturesList.Add(chromPeakFeatures);
                }
            }
            else {
                float[] mzRange = DataAccess.GetMs1Range(accumulatedMs1Spectrum, param.IonMode);
                float startMass = mzRange[0]; if (startMass < param.MassRangeBegin) startMass = param.MassRangeBegin;
                float endMass = mzRange[1]; if (endMass > param.MassRangeEnd) endMass = param.MassRangeEnd;
                float focusedMass = startMass, massStep = param.MassSliceWidth;

                while (focusedMass < endMass) {
                    if (focusedMass < param.MassRangeBegin) { focusedMass += massStep; continue; }
                    if (focusedMass > param.MassRangeEnd) break;

                    var chromPeakFeatures = GetChromatogramPeakFeatures(rawObj, focusedMass, param);
                    if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) { focusedMass += massStep; ReportProgress.Show(InitialProgress, ProgressMax, focusedMass, endMass, reportAction); continue; }

                    //removing peak spot redundancies among slices
                    chromPeakFeatures = PeakSpottingCore.RemovePeakAreaBeanRedundancy(chromPeakFeaturesList, chromPeakFeatures, massStep);
                    if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) { focusedMass += massStep; ReportProgress.Show(InitialProgress, ProgressMax, focusedMass, endMass, reportAction); continue; }

                    chromPeakFeaturesList.Add(chromPeakFeatures);
                    focusedMass += massStep;
                    ReportProgress.Show(InitialProgress, ProgressMax, focusedMass, endMass, reportAction);
                }
            }

            var cmbinedFeatures = PeakSpottingCore.GetCombinedChromPeakFeatures(chromPeakFeaturesList);
            cmbinedFeatures = PeakSpottingCore.GetRecalculatedChromPeakFeaturesByMs1MsTolerance(cmbinedFeatures, rawObj, param, ChromXType.RT, ChromXUnit.Min);
            cmbinedFeatures = PeakSpottingCore.GetOtherChromPeakFeatureProperties(cmbinedFeatures, allSpectrum, param);

            return cmbinedFeatures;
        }

        public List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(RawMeasurement rawObj, float focusedMass, MsdialLcImMsParameter param) {

            var accumulatedMs1Spectrum = rawObj.AccumulatedSpectrumList;
            var allSpectrum = rawObj.SpectrumList;

            //get EIC chromatogram
            var peaklist = DataAccess.GetMs1Peaklist(accumulatedMs1Spectrum, focusedMass, param.MassSliceWidth, param.IonMode, ChromXType.RT, ChromXUnit.Min, param.RetentionTimeBegin, param.RetentionTimeEnd);
            if (peaklist.Count == 0) return null;

            //get peak detection result
            var chromPeakFeatures = PeakSpottingCore.GetChromatogramPeakFeatures(peaklist, param, ChromXType.RT, ChromXUnit.Min);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;
            PeakSpottingCore.SetRawDataAccessID2ChromatogramPeakFeaturesFor4DChromData(chromPeakFeatures, accumulatedMs1Spectrum, peaklist, param);

            //filtering out noise peaks considering smoothing effects and baseline effects
            chromPeakFeatures = PeakSpottingCore.GetBackgroundSubtractedPeaks(chromPeakFeatures, peaklist);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;

            chromPeakFeatures = PeakSpottingCore.ExecutePeakDetectionOnDriftTimeAxis(chromPeakFeatures, rawObj, param, param.AccumulatedRtRagne);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;
            return chromPeakFeatures;
        }
    }
}

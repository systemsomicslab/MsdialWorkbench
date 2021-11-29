using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcImMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public List<ChromatogramPeakFeature> Execute4DFeatureDetection(IDataProvider spectrumProvider, IDataProvider accSpectrumProvider, MsdialLcImMsParameter param, Action<int> reportAction) {

            // used for rt, mz, intensity (3D) data
            var chromPeakFeaturesList = new List<List<ChromatogramPeakFeature>>();
            var isTargetedMode = !param.CompoundListInTargetMode.IsEmptyOrNull();
            if (isTargetedMode) {
                chromPeakFeaturesList = param.CompoundListInTargetMode
                    .Select(targetComp => GetChromatogramPeakFeatures(spectrumProvider, accSpectrumProvider, (float)targetComp.PrecursorMz, param))
                    .Where(chromPeakFeatures => !chromPeakFeatures.IsEmptyOrNull())
                    .ToList();
            }
            else {
                float[] mzRange = DataAccess.GetMs1Range(accSpectrumProvider.LoadMs1Spectrums(), param.IonMode);
                float startMass = Math.Max(mzRange[0], param.MassRangeBegin);
                float endMass = Math.Min(mzRange[1], param.MassRangeEnd);
                float massStep = param.MassSliceWidth;

                for (var focusedMass = startMass; focusedMass < endMass; focusedMass += massStep, ReportProgress.Show(InitialProgress, ProgressMax, focusedMass, endMass, reportAction)) {
                    var chromPeakFeatures = GetChromatogramPeakFeatures(spectrumProvider, accSpectrumProvider, focusedMass, param);
                    if (chromPeakFeatures.IsEmptyOrNull()) {
                        continue;
                    }

                    //removing peak spot redundancies among slices
                    chromPeakFeatures = PeakSpottingCore.RemovePeakAreaBeanRedundancy(chromPeakFeaturesList, chromPeakFeatures, massStep);
                    if (chromPeakFeatures.IsEmptyOrNull()) {
                        continue;
                    }

                    chromPeakFeaturesList.Add(chromPeakFeatures);
                }
            }

            var cmbinedFeatures = PeakSpottingCore.GetCombinedChromPeakFeatures(chromPeakFeaturesList);
            cmbinedFeatures = PeakSpottingCore.GetRecalculatedChromPeakFeaturesByMs1MsTolerance(cmbinedFeatures, accSpectrumProvider, param, ChromXType.RT, ChromXUnit.Min);
            cmbinedFeatures = PeakSpottingCore.GetOtherChromPeakFeatureProperties(cmbinedFeatures);

            return cmbinedFeatures;
        }

        public List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(IDataProvider spectrumProvider, IDataProvider accSpectrumProvider, float focusedMass, MsdialLcImMsParameter param) {


            //get EIC chromatogram
            var peaklist = DataAccess.GetMs1Peaklist(accSpectrumProvider.LoadMsSpectrums(), focusedMass, param.MassSliceWidth, param.IonMode, ChromXType.RT, ChromXUnit.Min, param.RetentionTimeBegin, param.RetentionTimeEnd);
            if (peaklist.Count == 0) return null;

            //get peak detection result
            var chromPeakFeatures = PeakSpottingCore.GetChromatogramPeakFeatures(peaklist, param, ChromXType.RT, ChromXUnit.Min);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;
            PeakSpottingCore.SetRawDataAccessID2ChromatogramPeakFeaturesFor4DChromData(chromPeakFeatures, accSpectrumProvider.LoadMsSpectrums(), peaklist, param);

            //filtering out noise peaks considering smoothing effects and baseline effects
            chromPeakFeatures = PeakSpottingCore.GetBackgroundSubtractedPeaks(chromPeakFeatures, peaklist);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;

            chromPeakFeatures = PeakSpottingCore.ExecutePeakDetectionOnDriftTimeAxis(chromPeakFeatures, spectrumProvider.LoadMsSpectrums(), param, param.AccumulatedRtRange);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;
            return chromPeakFeatures;
        }
    }
}

using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CompMs.MsdialLcImMsApi.Algorithm {
    public class PeakSpotting {
        private readonly MsdialLcmsParameter _parameter;
        private readonly PeakSpottingCore _peakSpottingCore;

        public PeakSpotting(double initialProgress, double progressMax, MsdialLcmsParameter parameter) {
            InitialProgress = initialProgress;
            ProgressMax = progressMax;
            _parameter = parameter;
            _peakSpottingCore = new PeakSpottingCore(parameter);
        }

        public double InitialProgress { get; set; } = 0.0;
        public double ProgressMax { get; set; } = 30.0;


        // feature detection for rt, ion mobility, m/z, and intensity (3D) data 
        public List<ChromatogramPeakFeature> Execute4DFeatureDetection(
            IDataProvider spectrumProvider, IDataProvider accSpectrumProvider, 
            MsdialLcImMsParameter param, int numThreads, CancellationToken token, Action<int> reportAction) {

            // used for rt, mz, intensity (3D) data
            var isTargetedMode = !param.CompoundListInTargetMode.IsEmptyOrNull();
            if (isTargetedMode) {
                if (numThreads <= 1) {
                    return Execute4DFeatureDetectionTargetMode(spectrumProvider, accSpectrumProvider, param);
                }
                else {
                    return Execute4DFeatureDetectionTargetModeByMultiThread(spectrumProvider, accSpectrumProvider, param, numThreads, token, reportAction);
                }
            }
            else {
                if (numThreads <= 1) {
                    return Execute4DFeatureDetectionNormalMode(spectrumProvider, accSpectrumProvider, param, reportAction);
                }
                else {
                    return Execute4DFeatureDetectionNormalModeByMultiThread(spectrumProvider, accSpectrumProvider, param, numThreads, token, reportAction);
                }
            }
        }

        private List<ChromatogramPeakFeature> Execute4DFeatureDetectionNormalMode(
            IDataProvider spectrumProvider, IDataProvider accSpectrumProvider, MsdialLcImMsParameter param, Action<int> reportAction) {

            var chromPeakFeaturesList = new List<List<ChromatogramPeakFeature>>();

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
                chromPeakFeatures = _peakSpottingCore.RemovePeakAreaBeanRedundancy(chromPeakFeaturesList, chromPeakFeatures, massStep);
                if (chromPeakFeatures.IsEmptyOrNull()) {
                    continue;
                }

                chromPeakFeaturesList.Add(chromPeakFeatures);
            }
            return _peakSpottingCore.GetCombinedChromPeakFeatures(chromPeakFeaturesList, accSpectrumProvider);
        }

        private List<ChromatogramPeakFeature> Execute4DFeatureDetectionNormalModeByMultiThread(
            IDataProvider spectrumProvider, IDataProvider accSpectrumProvider, MsdialLcImMsParameter param, int numThreads, CancellationToken token, Action<int> reportAction) {

            float[] mzRange = DataAccess.GetMs1Range(accSpectrumProvider.LoadMs1Spectrums(), param.IonMode);
            float startMass = mzRange[0]; if (startMass < param.MassRangeBegin) startMass = param.MassRangeBegin;
            float endMass = mzRange[1]; if (endMass > param.MassRangeEnd) endMass = param.MassRangeEnd;
            float focusedMass = startMass, massStep = param.MassSliceWidth;

            if (param.AccuracyType == AccuracyType.IsNominal) { massStep = 1.0F; }
            var targetMasses = _peakSpottingCore.GetFocusedMassList(startMass, endMass, massStep, param.MassRangeBegin, param.MassRangeEnd);
            var syncObj = new object();
            var counter = 0;
            var chromPeakFeaturesArray = targetMasses
                .AsParallel()
                .AsOrdered()
                .WithCancellation(token)
                .WithDegreeOfParallelism(numThreads)
                .Select(targetMass => {
                    var chromPeakFeatures = GetChromatogramPeakFeatures(spectrumProvider, accSpectrumProvider, targetMass, param);
                    lock (syncObj) {
                        counter++;
                        ReportProgress.Show(InitialProgress, ProgressMax, counter, targetMasses.Count, reportAction);
                    }
                    return chromPeakFeatures;
                })
                .ToArray();

            // finalization
            return _peakSpottingCore.FinalizePeakSpottingResult(chromPeakFeaturesArray, massStep, accSpectrumProvider);
        }

        private List<ChromatogramPeakFeature> Execute4DFeatureDetectionTargetMode(IDataProvider spectrumProvider, IDataProvider accSpectrumProvider, MsdialLcImMsParameter param) {
            var chromPeakFeaturesList = param.CompoundListInTargetMode
                    .Select(targetComp => GetChromatogramPeakFeatures(spectrumProvider, accSpectrumProvider, (float)targetComp.PrecursorMz, param))
                    .Where(chromPeakFeatures => !chromPeakFeatures.IsEmptyOrNull())
                    .ToList();
            return _peakSpottingCore.GetCombinedChromPeakFeatures(chromPeakFeaturesList, accSpectrumProvider);
        }

        private List<ChromatogramPeakFeature> Execute4DFeatureDetectionTargetModeByMultiThread(
            IDataProvider spectrumProvider, IDataProvider accSpectrumProvider, MsdialLcImMsParameter param,
            int numThreads, CancellationToken token, Action<int> reportAction) {
            var targetedScans = param.CompoundListInTargetMode;
            if (targetedScans.IsEmptyOrNull()) return null;
            var chromPeakFeaturesList = targetedScans
                .AsParallel()
                .AsOrdered()
                .WithCancellation(token)
                .WithDegreeOfParallelism(numThreads)
                .Select(targetedScan => GetChromatogramPeakFeatures(spectrumProvider, accSpectrumProvider, (float)targetedScan.PrecursorMz, param))
                .Where(features => !features.IsEmptyOrNull())
                .ToList();
            return _peakSpottingCore.GetCombinedChromPeakFeatures(chromPeakFeaturesList, accSpectrumProvider);
        }

        public List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(IDataProvider spectrumProvider, IDataProvider accSpectrumProvider, float focusedMass, MsdialLcImMsParameter param) {
            //get EIC chromatogram
            var accSpectra = new RawSpectra(accSpectrumProvider.LoadMsSpectrums(), param.IonMode, param.AcquisitionType);
            var chromatogramRange = new ChromatogramRange(param.RetentionTimeBegin, param.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            var chromatogram = accSpectra.GetMs1ExtractedChromatogram(focusedMass, param.MassSliceWidth, chromatogramRange);
            if (chromatogram.IsEmpty) return null;

            //get peak detection result
            var chromPeakFeatures = _peakSpottingCore.GetChromatogramPeakFeatures(chromatogram);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;
            _peakSpottingCore.SetRawDataAccessID2ChromatogramPeakFeaturesFor4DChromData(chromPeakFeatures, accSpectrumProvider.LoadMsSpectrums(), chromatogram.Peaks);

            //filtering out noise peaks considering smoothing effects and baseline effects
            chromPeakFeatures = _peakSpottingCore.GetBackgroundSubtractedPeaks(chromPeakFeatures, chromatogram.Peaks);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;

            var rawSpectra = new RawSpectra(spectrumProvider.LoadMsSpectrums(), param.IonMode, param.AcquisitionType);
            chromPeakFeatures = _peakSpottingCore.ExecutePeakDetectionOnDriftTimeAxis(chromPeakFeatures, rawSpectra, param.AccumulatedRtRange);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;
            return chromPeakFeatures;
        }
    }
}

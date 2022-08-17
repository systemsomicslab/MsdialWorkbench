using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcImMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CompMs.MsdialLcImMsApi.Algorithm {
    public class PeakSpotting {
        private readonly MsdialLcImMsParameter _parameter;
        private readonly PeakSpottingCore _peakSpottingCore;

        public PeakSpotting(double initialProgress, double progressMax, MsdialLcImMsParameter parameter) {
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
            int numThreads, CancellationToken token, Action<int> reportAction) {

            // used for rt, mz, intensity (3D) data
            var isTargetedMode = !_parameter.CompoundListInTargetMode.IsEmptyOrNull();
            if (isTargetedMode) {
                if (numThreads <= 1) {
                    return Execute4DFeatureDetectionTargetMode(spectrumProvider, accSpectrumProvider);
                }
                else {
                    return Execute4DFeatureDetectionTargetModeByMultiThread(spectrumProvider, accSpectrumProvider, numThreads, token, reportAction);
                }
            }
            else {
                if (numThreads <= 1) {
                    return Execute4DFeatureDetectionNormalMode(spectrumProvider, accSpectrumProvider, reportAction);
                }
                else {
                    return Execute4DFeatureDetectionNormalModeByMultiThread(spectrumProvider, accSpectrumProvider, numThreads, token, reportAction);
                }
            }
        }

        private List<ChromatogramPeakFeature> Execute4DFeatureDetectionNormalMode(IDataProvider spectrumProvider, IDataProvider accSpectrumProvider, Action<int> reportAction) {

            var chromPeakFeaturesList = new List<List<ChromatogramPeakFeature>>();
            var mzRange = accSpectrumProvider.GetMs1Range(_parameter.IonMode);
            float startMass = Math.Max(mzRange.Min, _parameter.MassRangeBegin);
            float endMass = Math.Min(mzRange.Max, _parameter.MassRangeEnd);
            float massStep = _parameter.MassSliceWidth;

            var accSpectra = new RawSpectra(accSpectrumProvider, _parameter.IonMode, _parameter.AcquisitionType);
            var rawSpectra = new RawSpectra(spectrumProvider, _parameter.IonMode, _parameter.AcquisitionType);
            var chromatogramRange = new ChromatogramRange(_parameter.RetentionTimeBegin, _parameter.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            var peakDetector = new PeakDetection(_parameter.MinimumDatapoints, _parameter.MinimumAmplitude);
            for (var focusedMass = startMass; focusedMass < endMass; ReportProgress.Show(InitialProgress, ProgressMax, focusedMass += massStep, endMass, reportAction)) {
                var chromPeakFeatures = GetChromatogramPeakFeatures(rawSpectra, accSpectra, accSpectrumProvider, focusedMass, chromatogramRange, peakDetector);
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

        private List<ChromatogramPeakFeature> Execute4DFeatureDetectionNormalModeByMultiThread(IDataProvider spectrumProvider, IDataProvider accSpectrumProvider, int numThreads, CancellationToken token, Action<int> reportAction) {

            var mzRange = accSpectrumProvider.GetMs1Range(_parameter.IonMode);
            float startMass = mzRange.Min < _parameter.MassRangeBegin ? _parameter.MassRangeBegin : mzRange.Min;
            float endMass = mzRange.Max > _parameter.MassRangeEnd ? _parameter.MassRangeEnd : mzRange.Max;
            float focusedMass = startMass, massStep = _parameter.MassSliceWidth;
            if (_parameter.AccuracyType == AccuracyType.IsNominal) {
                massStep = 1.0F;
            }

            var targetMasses = _peakSpottingCore.GetFocusedMassList(startMass, endMass, massStep, _parameter.MassRangeBegin, _parameter.MassRangeEnd);
            var rawSpectra = new RawSpectra(spectrumProvider, _parameter.IonMode, _parameter.AcquisitionType);
            var accSpectra = new RawSpectra(accSpectrumProvider, _parameter.IonMode, _parameter.AcquisitionType);
            var chromatogramRange = new ChromatogramRange(_parameter.RetentionTimeBegin, _parameter.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            var peakDetector = new PeakDetection(_parameter.MinimumDatapoints, _parameter.MinimumAmplitude);
            var syncObj = new object();
            var counter = 0;
            var chromPeakFeaturesArray = targetMasses
                .AsParallel()
                .AsOrdered()
                .WithCancellation(token)
                .WithDegreeOfParallelism(numThreads)
                .Select(targetMass => {
                    var chromPeakFeatures = GetChromatogramPeakFeatures(rawSpectra, accSpectra, accSpectrumProvider, targetMass, chromatogramRange, peakDetector);
                    Interlocked.Increment(ref counter);
                    lock (syncObj) {
                        ReportProgress.Show(InitialProgress, ProgressMax, counter, targetMasses.Count, reportAction);
                    }
                    return chromPeakFeatures;
                })
                .ToArray();

            // finalization
            return _peakSpottingCore.FinalizePeakSpottingResult(chromPeakFeaturesArray, massStep, accSpectrumProvider);
        }

        private List<ChromatogramPeakFeature> Execute4DFeatureDetectionTargetMode(IDataProvider spectrumProvider, IDataProvider accSpectrumProvider) {
            var rawSpectra = new RawSpectra(spectrumProvider, _parameter.IonMode, _parameter.AcquisitionType);
            var accSpectra = new RawSpectra(accSpectrumProvider, _parameter.IonMode, _parameter.AcquisitionType);
            var chromatogramRange = new ChromatogramRange(_parameter.RetentionTimeBegin, _parameter.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            var peakDetector = new PeakDetection(_parameter.MinimumDatapoints, _parameter.MinimumAmplitude);
            var chromPeakFeaturesList = _parameter.CompoundListInTargetMode
                    .Select(targetComp => GetChromatogramPeakFeatures(rawSpectra, accSpectra, accSpectrumProvider, (float)targetComp.PrecursorMz, chromatogramRange, peakDetector))
                    .Where(chromPeakFeatures => !chromPeakFeatures.IsEmptyOrNull())
                    .ToList();
            return _peakSpottingCore.GetCombinedChromPeakFeatures(chromPeakFeaturesList, accSpectrumProvider);
        }

        private List<ChromatogramPeakFeature> Execute4DFeatureDetectionTargetModeByMultiThread(
            IDataProvider spectrumProvider, IDataProvider accSpectrumProvider, int numThreads,
            CancellationToken token, Action<int> reportAction) {
            var targetedScans = _parameter.CompoundListInTargetMode;
            if (targetedScans.IsEmptyOrNull()) return null;
            var rawSpectra = new RawSpectra(spectrumProvider, _parameter.IonMode, _parameter.AcquisitionType);
            var accSpectra = new RawSpectra(accSpectrumProvider, _parameter.IonMode, _parameter.AcquisitionType);
            var chromatogramRange = new ChromatogramRange(_parameter.RetentionTimeBegin, _parameter.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            var peakDetector = new PeakDetection(_parameter.MinimumDatapoints, _parameter.MinimumAmplitude);
            var chromPeakFeaturesList = targetedScans
                .AsParallel()
                .AsOrdered()
                .WithCancellation(token)
                .WithDegreeOfParallelism(numThreads)
                .Select(targetedScan => GetChromatogramPeakFeatures(rawSpectra, accSpectra, accSpectrumProvider, (float)targetedScan.PrecursorMz, chromatogramRange, peakDetector))
                .Where(features => !features.IsEmptyOrNull())
                .ToList();
            return _peakSpottingCore.GetCombinedChromPeakFeatures(chromPeakFeaturesList, accSpectrumProvider);
        }

        private List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(RawSpectra rawSpectra, RawSpectra accSpectra, IDataProvider accSpectrumProvider, float focusedMass, ChromatogramRange chromatogramRange, PeakDetection peakDetector) {
            //get EIC chromatogram
            var chromatogram = accSpectra.GetMs1ExtractedChromatogram_temp2(focusedMass, _parameter.MassSliceWidth, chromatogramRange);
            if (chromatogram.IsEmpty) return null;
            var chromPeakFeatures = _peakSpottingCore.GetChromatogramPeakFeatures(chromatogram, peakDetector);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;
            _peakSpottingCore.SetRawDataAccessID2ChromatogramPeakFeaturesFor4DChromData(chromPeakFeatures, accSpectrumProvider, chromatogram.Peaks);

            //filtering out noise peaks considering smoothing effects and baseline effects
            var backgroundSubtracted = _peakSpottingCore.GetBackgroundSubtractedPeaks(chromPeakFeatures, chromatogram.Peaks);
            if (backgroundSubtracted == null || backgroundSubtracted.Count == 0) return null;

            var driftAxisPeaks = _peakSpottingCore.ExecutePeakDetectionOnDriftTimeAxis(backgroundSubtracted, rawSpectra, _parameter.AccumulatedRtRange);
            if (driftAxisPeaks == null || driftAxisPeaks.Count == 0) return null;
            return driftAxisPeaks;
        }
    }
}

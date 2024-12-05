using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
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
            AnalysisFileBean file, IDataProvider spectrumProvider,
            IDataProvider accSpectrumProvider, int numThreads, IProgress<int>? progress, CancellationToken token) {

            // used for rt, mz, intensity (3D) data
            var isTargetedMode = !_parameter.CompoundListInTargetMode.IsEmptyOrNull();
            if (isTargetedMode) {
                if (numThreads <= 1) {
                    return Execute4DFeatureDetectionTargetMode(file, spectrumProvider, accSpectrumProvider);
                }
                else {
                    return Execute4DFeatureDetectionTargetModeByMultiThread(file, spectrumProvider, accSpectrumProvider, numThreads, token);
                }
            }
            else {
                if (numThreads <= 1) {
                    return Execute4DFeatureDetectionNormalMode(file, spectrumProvider, accSpectrumProvider, progress);
                }
                else {
                    return Execute4DFeatureDetectionNormalModeByMultiThread(file, spectrumProvider, accSpectrumProvider, numThreads, progress, token);
                }
            }
        }

        private List<ChromatogramPeakFeature> Execute4DFeatureDetectionNormalMode(AnalysisFileBean file, IDataProvider spectrumProvider, IDataProvider accSpectrumProvider, IProgress<int>? progress) {
            var chromPeakFeaturesList = new List<List<ChromatogramPeakFeature>>();
            var mzRange = accSpectrumProvider.GetMs1Range(_parameter.IonMode);
            float startMass = Math.Max(mzRange.Min, _parameter.MassRangeBegin);
            float endMass = Math.Min(mzRange.Max, _parameter.MassRangeEnd);
            float massStep = _parameter.MassSliceWidth;

            var accSpectra = new RawSpectra(accSpectrumProvider, _parameter.IonMode, file.AcquisitionType);
            var rawSpectra = new RawSpectra(spectrumProvider, _parameter.IonMode, file.AcquisitionType);
            var chromatogramRange = new ChromatogramRange(_parameter.RetentionTimeBegin, _parameter.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            var peakDetector = new PeakDetection(_parameter.MinimumDatapoints, _parameter.MinimumAmplitude);
            ReportProgress reporter = ReportProgress.FromLength(progress, InitialProgress, ProgressMax);
            for (var focusedMass = startMass; focusedMass < endMass; reporter.Report(focusedMass += massStep, endMass)) {
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
            return _peakSpottingCore.GetCombinedChromPeakFeatures(chromPeakFeaturesList, accSpectrumProvider, file.AcquisitionType);
        }

        private List<ChromatogramPeakFeature> Execute4DFeatureDetectionNormalModeByMultiThread(AnalysisFileBean file, IDataProvider spectrumProvider, IDataProvider accSpectrumProvider, int numThreads, IProgress<int>? progress, CancellationToken token) {
            var (mzMin, mzMax) = accSpectrumProvider.GetMs1Range(_parameter.IonMode);
            float startMass = mzMin < _parameter.MassRangeBegin ? _parameter.MassRangeBegin : mzMin;
            float endMass = mzMax > _parameter.MassRangeEnd ? _parameter.MassRangeEnd : mzMax;
            float focusedMass = startMass, massStep = _parameter.MassSliceWidth;
            if (_parameter.AccuracyType == AccuracyType.IsNominal) {
                massStep = 1.0F;
            }

            var targetMasses = _peakSpottingCore.GetFocusedMassList(startMass, endMass, massStep);
            var rawSpectra = new RawSpectra(spectrumProvider, _parameter.IonMode, file.AcquisitionType);
            var accSpectra = new RawSpectra(accSpectrumProvider, _parameter.IonMode, file.AcquisitionType);
            var chromatogramRange = new ChromatogramRange(_parameter.RetentionTimeBegin, _parameter.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            var peakDetector = new PeakDetection(_parameter.MinimumDatapoints, _parameter.MinimumAmplitude);
            var counter = 0;
            ReportProgress reporter = ReportProgress.FromLength(progress, InitialProgress, ProgressMax);
            var chromPeakFeaturesArray = targetMasses
                .AsParallel()
                .AsOrdered()
                .WithCancellation(token)
                .WithDegreeOfParallelism(numThreads)
                .Select(targetMass => {
                    var chromPeakFeatures = GetChromatogramPeakFeatures(rawSpectra, accSpectra, accSpectrumProvider, (float)targetMass, chromatogramRange, peakDetector);
                    reporter.Report(Interlocked.Increment(ref counter), targetMasses.Count);
                    return chromPeakFeatures;
                })
                .ToArray();

            // finalization
            return _peakSpottingCore.FinalizePeakSpottingResult(chromPeakFeaturesArray, massStep, accSpectrumProvider, file.AcquisitionType);
        }

        private List<ChromatogramPeakFeature> Execute4DFeatureDetectionTargetMode(AnalysisFileBean file, IDataProvider spectrumProvider, IDataProvider accSpectrumProvider) {
            var rawSpectra = new RawSpectra(spectrumProvider, _parameter.IonMode, file.AcquisitionType);
            var accSpectra = new RawSpectra(accSpectrumProvider, _parameter.IonMode, file.AcquisitionType);
            var chromatogramRange = new ChromatogramRange(_parameter.RetentionTimeBegin, _parameter.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            var peakDetector = new PeakDetection(_parameter.MinimumDatapoints, _parameter.MinimumAmplitude);
            var chromPeakFeaturesList = _parameter.CompoundListInTargetMode
                    .Select(targetComp => GetChromatogramPeakFeatures(rawSpectra, accSpectra, accSpectrumProvider, (float)targetComp.PrecursorMz, chromatogramRange, peakDetector))
                    .Where(chromPeakFeatures => !chromPeakFeatures.IsEmptyOrNull())
                    .ToList();
            return _peakSpottingCore.GetCombinedChromPeakFeatures(chromPeakFeaturesList, accSpectrumProvider, file.AcquisitionType);
        }

        private List<ChromatogramPeakFeature> Execute4DFeatureDetectionTargetModeByMultiThread(
            AnalysisFileBean file, IDataProvider spectrumProvider, IDataProvider accSpectrumProvider,
            int numThreads, CancellationToken token) {
            var targetedScans = _parameter.CompoundListInTargetMode;
            if (targetedScans.IsEmptyOrNull()) return null;
            var rawSpectra = new RawSpectra(spectrumProvider, _parameter.IonMode, file.AcquisitionType);
            var accSpectra = new RawSpectra(accSpectrumProvider, _parameter.IonMode, file.AcquisitionType);
            var chromatogramRange = new ChromatogramRange(_parameter.RetentionTimeBegin, _parameter.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            var peakDetector = new PeakDetection(_parameter.MinimumDatapoints, _parameter.MinimumAmplitude);
            var chromPeakFeaturesList = targetedScans
                .AsParallel()
                .AsOrdered()
                .WithCancellation(token)
                .WithDegreeOfParallelism(numThreads)
                .Select(targetedScan => GetChromatogramPeakFeatures(rawSpectra, accSpectra, accSpectrumProvider, targetedScan.PrecursorMz, chromatogramRange, peakDetector))
                .Where(features => !features.IsEmptyOrNull())
                .ToList();
            return _peakSpottingCore.GetCombinedChromPeakFeatures(chromPeakFeaturesList, accSpectrumProvider, file.AcquisitionType);
        }

        private List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(RawSpectra rawSpectra, RawSpectra accSpectra, IDataProvider accSpectrumProvider, double focusedMass, ChromatogramRange chromatogramRange, PeakDetection peakDetector) {
            //get EIC chromatogram
            var chromatogram = accSpectra.GetMS1ExtractedChromatogram(new MzRange(focusedMass, _parameter.PeakPickBaseParam.MassSliceWidth), chromatogramRange);
            if (chromatogram.IsEmpty) return null;
            var chromPeakFeatures = _peakSpottingCore.GetChromatogramPeakFeatures(chromatogram, peakDetector);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;
            _peakSpottingCore.SetRawDataAccessID2ChromatogramPeakFeaturesFor4DChromData(chromPeakFeatures, accSpectrumProvider);

            //filtering out noise peaks considering smoothing effects and baseline effects
            var backgroundSubtracted = _peakSpottingCore.GetBackgroundSubtractedPeaks(chromPeakFeatures, chromatogram);
            if (backgroundSubtracted == null || backgroundSubtracted.Count == 0) return null;

            var driftAxisPeaks = _peakSpottingCore.ExecutePeakDetectionOnDriftTimeAxis(backgroundSubtracted, rawSpectra, _parameter.AccumulatedRtRange);
            if (driftAxisPeaks == null || driftAxisPeaks.Count == 0) return null;
            return driftAxisPeaks;
        }
    }
}

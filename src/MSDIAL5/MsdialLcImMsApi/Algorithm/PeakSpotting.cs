using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.Raw.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialLcImMsApi.Algorithm
{
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
        public async Task<List<ChromatogramPeakFeature>> Execute4DFeatureDetectionAsync(
            AnalysisFileBean file, IDataProvider spectrumProvider,
            IDataProvider accSpectrumProvider, int numThreads, IProgress<int>? progress, CancellationToken token = default) {

            // used for rt, mz, intensity (3D) data
            var isTargetedMode = !_parameter.CompoundListInTargetMode.IsEmptyOrNull();
            if (isTargetedMode) {
                if (numThreads <= 1) {
                    return await Execute4DFeatureDetectionTargetModeAsync(file, spectrumProvider, accSpectrumProvider, token).ConfigureAwait(false);
                }
                else {
                    return await Execute4DFeatureDetectionTargetModeByMultiThreadAsync(file, spectrumProvider, accSpectrumProvider, numThreads, token).ConfigureAwait(false);
                }
            }
            else {
                if (numThreads <= 1) {
                    return await Execute4DFeatureDetectionNormalMode(file, spectrumProvider, accSpectrumProvider, progress, token).ConfigureAwait(false);
                }
                else {
                    return await Execute4DFeatureDetectionNormalModeByMultiThreadAsync(file, spectrumProvider, accSpectrumProvider, numThreads, progress, token).ConfigureAwait(false);
                }
            }
        }

        private async Task<List<ChromatogramPeakFeature>> Execute4DFeatureDetectionNormalMode(AnalysisFileBean file, IDataProvider spectrumProvider, IDataProvider accSpectrumProvider, IProgress<int>? progress, CancellationToken token = default) {
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
                token.ThrowIfCancellationRequested();
                var chromPeakFeatures = await GetChromatogramPeakFeaturesAsync(rawSpectra, accSpectra, accSpectrumProvider, focusedMass, chromatogramRange, peakDetector, token).ConfigureAwait(false);
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
            return await _peakSpottingCore.GetCombinedChromPeakFeaturesAsync(chromPeakFeaturesList, accSpectrumProvider, file.AcquisitionType, token).ConfigureAwait(false);
        }

        private async Task<List<ChromatogramPeakFeature>> Execute4DFeatureDetectionNormalModeByMultiThreadAsync(AnalysisFileBean file, IDataProvider spectrumProvider, IDataProvider accSpectrumProvider, int numThreads, IProgress<int>? progress, CancellationToken token = default) {
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
            var chromPeakFeaturesTasks = targetMasses
                .AsParallel()
                .AsOrdered()
                .WithCancellation(token)
                .WithDegreeOfParallelism(numThreads)
                .Select(async targetMass => {
                    var chromPeakFeatures = await GetChromatogramPeakFeaturesAsync(rawSpectra, accSpectra, accSpectrumProvider, (float)targetMass, chromatogramRange, peakDetector, token).ConfigureAwait(false);
                    reporter.Report(Interlocked.Increment(ref counter), targetMasses.Count);
                    return chromPeakFeatures;
                })
                .ToArray();
            var chromPeakFeaturesArray = await Task.WhenAll(chromPeakFeaturesTasks).ConfigureAwait(false);
 
            // finalization
            return await _peakSpottingCore.FinalizePeakSpottingResultAsync(chromPeakFeaturesArray, massStep, accSpectrumProvider, file.AcquisitionType, token).ConfigureAwait(false);
        }

        private async Task<List<ChromatogramPeakFeature>> Execute4DFeatureDetectionTargetModeAsync(AnalysisFileBean file, IDataProvider spectrumProvider, IDataProvider accSpectrumProvider, CancellationToken token = default) {
            var rawSpectra = new RawSpectra(spectrumProvider, _parameter.IonMode, file.AcquisitionType);
            var accSpectra = new RawSpectra(accSpectrumProvider, _parameter.IonMode, file.AcquisitionType);
            var chromatogramRange = new ChromatogramRange(_parameter.RetentionTimeBegin, _parameter.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            var peakDetector = new PeakDetection(_parameter.MinimumDatapoints, _parameter.MinimumAmplitude);
            var chromPeakFeaturesList = new List<List<ChromatogramPeakFeature>>();
            foreach (var targetComp in _parameter.CompoundListInTargetMode) {
                var chromPeakFeatures = await GetChromatogramPeakFeaturesAsync(rawSpectra, accSpectra, accSpectrumProvider, (float)targetComp.PrecursorMz, chromatogramRange, peakDetector, token).ConfigureAwait(false);
                if (chromPeakFeatures.IsEmptyOrNull()) {
                    continue;
                }
                chromPeakFeaturesList.Add(chromPeakFeatures);
            }
            return await _peakSpottingCore.GetCombinedChromPeakFeaturesAsync(chromPeakFeaturesList, accSpectrumProvider, file.AcquisitionType, token).ConfigureAwait(false);
        }

        private async Task<List<ChromatogramPeakFeature>> Execute4DFeatureDetectionTargetModeByMultiThreadAsync(
            AnalysisFileBean file, IDataProvider spectrumProvider, IDataProvider accSpectrumProvider,
            int numThreads, CancellationToken token = default) {
            var targetedScans = _parameter.CompoundListInTargetMode;
            if (targetedScans.IsEmptyOrNull()) return null;
            var rawSpectra = new RawSpectra(spectrumProvider, _parameter.IonMode, file.AcquisitionType);
            var accSpectra = new RawSpectra(accSpectrumProvider, _parameter.IonMode, file.AcquisitionType);
            var chromatogramRange = new ChromatogramRange(_parameter.RetentionTimeBegin, _parameter.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            var peakDetector = new PeakDetection(_parameter.MinimumDatapoints, _parameter.MinimumAmplitude);
            var chromPeakFeaturesListTasks = targetedScans
                .AsParallel()
                .AsOrdered()
                .WithCancellation(token)
                .WithDegreeOfParallelism(numThreads)
                .Select(targetedScan => GetChromatogramPeakFeaturesAsync(rawSpectra, accSpectra, accSpectrumProvider, targetedScan.PrecursorMz, chromatogramRange, peakDetector, token))
                .ToArray();

            var chromPeakFeaturesList = await Task.WhenAll(chromPeakFeaturesListTasks).ConfigureAwait(false);
            return await _peakSpottingCore.GetCombinedChromPeakFeaturesAsync(chromPeakFeaturesList, accSpectrumProvider, file.AcquisitionType, token).ConfigureAwait(false);
        }

        private async Task<List<ChromatogramPeakFeature>?> GetChromatogramPeakFeaturesAsync(RawSpectra rawSpectra, RawSpectra accSpectra, IDataProvider accSpectrumProvider, double focusedMass, ChromatogramRange chromatogramRange, PeakDetection peakDetector, CancellationToken token = default) {
            //get EIC chromatogram
            using var chromatogram = await accSpectra.GetMS1ExtractedChromatogramAsync(new MzRange(focusedMass, _parameter.PeakPickBaseParam.MassSliceWidth), chromatogramRange, token).ConfigureAwait(false);
            if (chromatogram.IsEmpty) return null;
            var chromPeakFeatures = _peakSpottingCore.GetChromatogramPeakFeatures(chromatogram, peakDetector);
            if (chromPeakFeatures is null || chromPeakFeatures.Count == 0) return null;
            _peakSpottingCore.SetRawDataAccessID2ChromatogramPeakFeaturesFor4DChromData(chromPeakFeatures, accSpectrumProvider);

            //filtering out noise peaks considering smoothing effects and baseline effects
            var backgroundSubtracted = _peakSpottingCore.GetBackgroundSubtractedPeaks(chromPeakFeatures, chromatogram);
            if (backgroundSubtracted is null || backgroundSubtracted.Count == 0) return null;

            var driftAxisPeaks = await _peakSpottingCore.ExecutePeakDetectionOnDriftTimeAxisAsync(backgroundSubtracted, rawSpectra, _parameter.AccumulatedRtRange, token).ConfigureAwait(false);
            if (driftAxisPeaks is null || driftAxisPeaks.Count == 0) return null;
            return driftAxisPeaks;
        }
    }
}

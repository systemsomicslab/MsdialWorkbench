using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm {
    public sealed class PeakSpottingCore {
        private readonly ParameterBase _parameter;

        public PeakSpottingCore(ParameterBase parameter) {
            _parameter = parameter;
        }

        // feature detection for rt (or ion mobility), m/z, and intensity (3D) data 
        // this method can be used in GC-MS, LC-MS, and IM-MS project
        public List<ChromatogramPeakFeature> Execute3DFeatureDetection(IDataProvider provider, int numThreads, CancellationToken token, ReportProgress reporter, ChromatogramRange chromatogramRange) {
            var isTargetedMode = !_parameter.CompoundListInTargetMode.IsEmptyOrNull();
            if (isTargetedMode) {
                return Execute3DFeatureDetectionTargetMode(provider, numThreads, token, chromatogramRange);
            }
            return Execute3DFeatureDetectionNormalMode(provider, numThreads, token, reporter, chromatogramRange);
        }

        public List<ChromatogramPeakFeature> Execute3DFeatureDetectionTargetMode(IDataProvider provider, int numThreads, CancellationToken token, ChromatogramRange chromatogramRange) {
            if (numThreads <= 1) {
                return Execute3DFeatureDetectionTargetModeBySingleThread(provider, chromatogramRange);
            }
            else {
                return Execute3DFeatureDetectionTargetModeByMultiThread(provider, numThreads, token, chromatogramRange);
            }
        }

        public List<ChromatogramPeakFeature> Execute3DFeatureDetectionNormalMode(IDataProvider provider, int numThreads, CancellationToken token, ReportProgress reporter, ChromatogramRange chromatogramRange) {
            if (numThreads <= 1) {
                return Execute3DFeatureDetectionNormalModeBySingleThread(provider, reporter, chromatogramRange);
            }
            else {
                return Execute3DFeatureDetectionNormalModeByMultiThread(provider, numThreads, token, reporter, chromatogramRange);
            }
        }

        public List<ChromatogramPeakFeature> GetCombinedChromPeakFeatures(List<List<ChromatogramPeakFeature>> featuresList, IDataProvider provider) {
            var cmbinedFeatures = GetCombinedChromPeakFeatures(featuresList);
            cmbinedFeatures = GetRecalculatedChromPeakFeaturesByMs1MsTolerance(cmbinedFeatures, provider);

            // test
            cmbinedFeatures = cmbinedFeatures.OrderBy(n => n.Mass).ThenBy(n => n.ChromXs.Value).ToList();
            cmbinedFeatures = GetFurtherCleanupedChromPeakFeatures(cmbinedFeatures);
            //cmbinedFeatures = GetFurtherCleanupedChromPeakFeatures(cmbinedFeatures, param, type, unit);
            cmbinedFeatures = GetOtherChromPeakFeatureProperties(cmbinedFeatures);

            return cmbinedFeatures;
        }

        private List<ChromatogramPeakFeature> Execute3DFeatureDetectionNormalModeBySingleThread(IDataProvider provider, ReportProgress reporter, ChromatogramRange chromatogramRange) {

            var chromPeakFeaturesList = new List<List<ChromatogramPeakFeature>>();
            (float startMass, float endMass) = provider.GetMs1Range(_parameter.IonMode);
            if (startMass < _parameter.MassRangeBegin) startMass = _parameter.MassRangeBegin;
            if (endMass > _parameter.MassRangeEnd) endMass = _parameter.MassRangeEnd;
            float focusedMass = startMass, massStep = _parameter.MassSliceWidth;
            if (_parameter.AccuracyType == AccuracyType.IsNominal) { massStep = 1.0F; }
            var rawSpectra = new RawSpectra(provider.LoadMs1Spectrums(), _parameter.IonMode, _parameter.AcquisitionType);
            var detector = new PeakDetection(_parameter.MinimumDatapoints, _parameter.MinimumAmplitude);
            while (focusedMass < endMass) {
                if (focusedMass < _parameter.MassRangeBegin) { focusedMass += massStep; continue; }
                if (focusedMass > _parameter.MassRangeEnd) break;
                //var chromPeakFeatures = GetChromatogramPeakFeatures(rawSpectra, provider, focusedMass, chromatogramRange);
                //var chromPeakFeatures = GetChromatogramPeakFeatures_Temp(rawSpectra, provider, focusedMass, chromatogramRange);
                var chromPeakFeatures = GetChromatogramPeakFeatures_Temp2(rawSpectra, provider, focusedMass, chromatogramRange, detector);
                if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) {
                    focusedMass += massStep;
                    reporter?.Show(focusedMass - startMass, endMass - startMass);
                    continue;
                }

                //removing peak spot redundancies among slices
                chromPeakFeatures = RemovePeakAreaBeanRedundancy(chromPeakFeaturesList, chromPeakFeatures, massStep);
                if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) {
                    focusedMass += massStep;
                    reporter?.Show(focusedMass - startMass, endMass - startMass);
                    continue;
                }

                chromPeakFeaturesList.Add(chromPeakFeatures);
                focusedMass += massStep;
                reporter?.Show(focusedMass - startMass, endMass - startMass);
            }
            return GetCombinedChromPeakFeatures(chromPeakFeaturesList, provider);
        }

        private List<ChromatogramPeakFeature> Execute3DFeatureDetectionNormalModeByMultiThread(IDataProvider provider, int numThreads, CancellationToken token, ReportProgress reporter, ChromatogramRange chromatogramRange) {

            var mzRange = provider.GetMs1Range(_parameter.IonMode);
            float startMass = mzRange.Min; if (startMass < _parameter.MassRangeBegin) startMass = _parameter.MassRangeBegin;
            float endMass = mzRange.Max; if (endMass > _parameter.MassRangeEnd) endMass = _parameter.MassRangeEnd;
            float focusedMass = startMass, massStep = _parameter.MassSliceWidth;

            if (_parameter.AccuracyType == AccuracyType.IsNominal) { massStep = 1.0F; }
            var targetMasses = GetFocusedMassList(startMass, endMass, massStep, _parameter.MassRangeBegin, _parameter.MassRangeEnd);
            var syncObj = new object();
            var counter = 0;
            var rawSpectra = new RawSpectra(provider.LoadMs1Spectrums(), _parameter.IonMode, _parameter.AcquisitionType);
            var chromPeakFeaturesArray = new List<ChromatogramPeakFeature>[targetMasses.Count];
            var queue = new ConcurrentQueue<(float, int)>(targetMasses.Select((targetMass, i) => (targetMass, i)));
            var tasks = new Task[numThreads];
            for (int i = 0; i < numThreads; i++) {
                tasks[i] = Task.Run(() => zzz(
                    rawSpectra,
                    provider,
                    chromatogramRange,
                    new PeakDetection(_parameter.MinimumDatapoints, _parameter.MinimumAmplitude),
                    () => { Interlocked.Increment(ref counter); reporter?.Show(counter, targetMasses.Count); },
                    targetMasses,
                    chromPeakFeaturesArray,
                    queue),
                    token);
            }
            Task.WaitAll(tasks);
            // var chromPeakFeaturesArray = targetMasses
            //     .AsParallel()
            //     .AsOrdered()
            //     .WithCancellation(token)
            //     .WithDegreeOfParallelism(numThreads)
            //     .Select(targetMass => {
            //         //var chromPeakFeatures = GetChromatogramPeakFeatures(rawSpectra, provider, targetMass, chromatogramRange);
            //         //var chromPeakFeatures = GetChromatogramPeakFeatures_Temp(rawSpectra, provider, targetMass, chromatogramRange);
            //         var chromPeakFeatures = GetChromatogramPeakFeatures_Temp2(rawSpectra, provider, targetMass, chromatogramRange, new PeakDetection(_parameter.MinimumDatapoints, _parameter.MinimumAmplitude));
            //         Interlocked.Increment(ref counter);
            //         reporter?.Show(counter, targetMasses.Count);
            //         return chromPeakFeatures;
            //     })
            //     .ToArray();

            // finalization
            return FinalizePeakSpottingResult(chromPeakFeaturesArray, massStep, provider);
        }

        private void zzz(RawSpectra rawSpectra, IDataProvider provider, ChromatogramRange range, PeakDetection detector, Action report, List<float> targetMasses, List<ChromatogramPeakFeature>[] chromPeakFeaturesArray, ConcurrentQueue<(float, int)> queue) {
            System.Diagnostics.Debug.Assert(targetMasses.Count == chromPeakFeaturesArray.Length);
            while (queue.TryDequeue(out var pair)) {
                var (targetMass, index) = pair;
                var chromPeakFeatures = GetChromatogramPeakFeatures_Temp2(rawSpectra, provider, targetMass, range, detector);
                chromPeakFeaturesArray[index] = chromPeakFeatures;
                report?.Invoke();
            }
        }

        private async Task<List<ChromatogramPeakFeature>> Execute3DFeatureDetectionNormalModeByMultiThreadAsync(IDataProvider provider, int numThreads, CancellationToken token, ReportProgress reporter, ChromatogramRange chromatogramRange) {

            var mzRange = provider.GetMs1Range(_parameter.IonMode);
            float startMass = mzRange.Min; if (startMass < _parameter.MassRangeBegin) startMass = _parameter.MassRangeBegin;
            float endMass = mzRange.Max; if (endMass > _parameter.MassRangeEnd) endMass = _parameter.MassRangeEnd;
            float focusedMass = startMass, massStep = _parameter.MassSliceWidth;

            if (_parameter.AccuracyType == AccuracyType.IsNominal) { massStep = 1.0F; }
            var targetMasses = GetFocusedMassList(startMass, endMass, massStep, _parameter.MassRangeBegin, _parameter.MassRangeEnd);
            var counter = 0;
            var chromPeakFeaturesArray = new List<ChromatogramPeakFeature>[targetMasses.Count];
            using (var sem = new SemaphoreSlim(numThreads)) {
                var tasks = new List<Task>();
                var rawSpectra = new RawSpectra(provider.LoadMs1Spectrums(), _parameter.IonMode, _parameter.AcquisitionType);
                for (int i = 0; i < targetMasses.Count; i++) {
                    var v = Task.Run(async () => {
                        await sem.WaitAsync();
                        try {
                            var chromPeakFeatures = await Task.Run(() => GetChromatogramPeakFeatures(rawSpectra, provider, targetMasses[i], chromatogramRange), token);
                            chromPeakFeaturesArray[i] = chromPeakFeatures;
                        }
                        finally {
                            sem.Release();
                            Interlocked.Increment(ref counter);
                            reporter?.Show(counter, targetMasses.Count);
                        }
                    });
                    tasks.Add(v);
                }
                await Task.WhenAll(tasks);
            }
            return FinalizePeakSpottingResult(chromPeakFeaturesArray, massStep, provider);
        }

        public List<ChromatogramPeakFeature> FinalizePeakSpottingResult(List<ChromatogramPeakFeature>[] chromPeakFeaturesArray, float massStep, IDataProvider provider) {
            var chromPeakFeaturesList = new List<List<ChromatogramPeakFeature>>();
            foreach (var features in chromPeakFeaturesArray.OrEmptyIfNull()) {
                if (features.IsEmptyOrNull()) {
                    continue;
                }

                var filteredFeatures = RemovePeakAreaBeanRedundancy(chromPeakFeaturesList, features, massStep);
                if (filteredFeatures.IsEmptyOrNull()) {
                    continue;
                }
                chromPeakFeaturesList.Add(filteredFeatures);
            }
            return GetCombinedChromPeakFeatures(chromPeakFeaturesList, provider);
        }

        public List<float> GetFocusedMassList(float startMass, float endMass, float massStep, float massBegin, float massEnd) {
            var massList = new List<float>();
            var focusedMass = startMass;
            while (focusedMass < endMass) {
                if (focusedMass < massBegin) { focusedMass += massStep; continue; }
                if (focusedMass > massEnd) break;
                massList.Add(focusedMass);
                focusedMass += massStep;
            }

            return massList;
        }

        public List<ChromatogramPeakFeature> Execute3DFeatureDetectionTargetModeBySingleThread(IDataProvider provider, ChromatogramRange chromatogramRange) {
            return Execute3DFeatureDetectionTargetMode(provider, _parameter.CompoundListInTargetMode, chromatogramRange);
        }

        public List<ChromatogramPeakFeature> Execute3DFeatureDetectionTargetMode(IDataProvider provider, List<MoleculeMsReference> targetedScans, ChromatogramRange chromatogramRange) {
            var chromPeakFeaturesList = new List<List<ChromatogramPeakFeature>>();
            if (targetedScans.IsEmptyOrNull()) return null;
            var rawSpectra = new RawSpectra(provider.LoadMs1Spectrums(), _parameter.IonMode, _parameter.AcquisitionType);
            foreach (var targetComp in targetedScans) {
                var chromPeakFeatures = GetChromatogramPeakFeatures(rawSpectra, provider, (float)targetComp.PrecursorMz, chromatogramRange);
                if (!chromPeakFeatures.IsEmptyOrNull())
                    chromPeakFeaturesList.Add(chromPeakFeatures);
            }

            return GetCombinedChromPeakFeatures(chromPeakFeaturesList, provider);
        }

        public List<ChromatogramPeakFeature> Execute3DFeatureDetectionTargetModeByMultiThread(IDataProvider provider, int numThreads, CancellationToken token, ChromatogramRange chromatogramRange) {
            var targetedScans = _parameter.CompoundListInTargetMode;
            return Execute3DFeatureDetectionTargetModeByMultiThread(provider, targetedScans, numThreads, token, chromatogramRange);
        }

        public List<ChromatogramPeakFeature> Execute3DFeatureDetectionTargetModeByMultiThread(IDataProvider provider, List<MoleculeMsReference> targetedScans, int numThreads, CancellationToken token, ChromatogramRange chromatogramRange) {
            var spectrumList = provider.LoadMs1Spectrums();
            if (targetedScans.IsEmptyOrNull()) return null;
            var rawSpectra = new RawSpectra(provider.LoadMs1Spectrums(), _parameter.IonMode, _parameter.AcquisitionType);
            var chromPeakFeaturesList = targetedScans
                .AsParallel()
                .AsOrdered()
                .WithCancellation(token)
                .WithDegreeOfParallelism(numThreads)
                .Select(targetedScan => GetChromatogramPeakFeatures(rawSpectra, provider, (float)targetedScan.PrecursorMz, chromatogramRange))
                .Where(features => !features.IsEmptyOrNull())
                .ToList();
            return GetCombinedChromPeakFeatures(chromPeakFeaturesList, provider);
        }

        private async Task<List<ChromatogramPeakFeature>> Execute3DFeatureDetectionTargetModeByMultiThreadAsync(IDataProvider provider, int numThreads, CancellationToken token, ReportProgress reporter, ChromatogramRange chromatogramRange) {
            var chromPeakFeaturesList = new List<List<ChromatogramPeakFeature>>();
            var targetedScans = _parameter.CompoundListInTargetMode;
            if (targetedScans.IsEmptyOrNull()) return null;
            var syncObj = new object();
            var spectrumList = provider.LoadMs1Spectrums();
            using (var sem = new SemaphoreSlim(numThreads)) {
                var tasks = new List<Task>();
                var count = 0;
                var rawSpectra = new RawSpectra(provider.LoadMs1Spectrums(), _parameter.IonMode, _parameter.AcquisitionType);
                for (int i = 0; i < targetedScans.Count; i++) {
                    var v = Task.Run(async () => {
                        await sem.WaitAsync();
                        try {
                            var chromPeakFeatures = await Task.Run(() => GetChromatogramPeakFeatures(rawSpectra, provider, (float)targetedScans[i].PrecursorMz, chromatogramRange), token);
                            if (!chromPeakFeatures.IsEmptyOrNull()) {
                                lock (syncObj) {
                                    chromPeakFeaturesList.Add(chromPeakFeatures);
                                }
                                Interlocked.Increment(ref count);
                                reporter.Show(count, targetedScans.Count);
                            }
                        }
                        finally {
                            sem.Release();
                        }
                    });
                    tasks.Add(v);
                }
                await Task.WhenAll(tasks);
            }
           
            return GetCombinedChromPeakFeatures(chromPeakFeaturesList, provider);
        }

        public List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(RawSpectra rawSpectra, IDataProvider provider, float focusedMass, ChromatogramRange chromatogramRange) {

            //get EIC chromatogram
            var chromatogram = rawSpectra.GetMs1ExtractedChromatogram(focusedMass, _parameter.MassSliceWidth, chromatogramRange);
            if (chromatogram.IsEmpty) return null;

            //get peak detection result
            var chromPeakFeatures = GetChromatogramPeakFeatures(chromatogram);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;
            SetRawDataAccessID2ChromatogramPeakFeatures(chromPeakFeatures, provider, chromatogram.Peaks);

            //filtering out noise peaks considering smoothing effects and baseline effects
            chromPeakFeatures = GetBackgroundSubtractedPeaks(chromPeakFeatures, chromatogram.Peaks);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;

            return chromPeakFeatures;
        }

        public List<ChromatogramPeakFeature> GetChromatogramPeakFeatures_Temp(RawSpectra rawSpectra, IDataProvider provider, float focusedMass, ChromatogramRange chromatogramRange) {

            //get EIC chromatogram
            var chromatogram = rawSpectra.GetMs1ExtractedChromatogram_temp(focusedMass, _parameter.MassSliceWidth, chromatogramRange);
            if (chromatogram.IsEmpty) return null;

            //get peak detection result
            var chromPeakFeatures = GetChromatogramPeakFeatures(chromatogram);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;
            SetRawDataAccessID2ChromatogramPeakFeatures(chromPeakFeatures, provider, chromatogram.Peaks);

            //filtering out noise peaks considering smoothing effects and baseline effects
            chromPeakFeatures = GetBackgroundSubtractedPeaks(chromPeakFeatures, chromatogram.Peaks);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;

            return chromPeakFeatures;
        }

        public List<ChromatogramPeakFeature> GetChromatogramPeakFeatures_Temp2(RawSpectra rawSpectra, IDataProvider provider, float focusedMass, ChromatogramRange chromatogramRange, PeakDetection detector) {

            //get EIC chromatogram
            var chromatogram = rawSpectra.GetMs1ExtractedChromatogram_temp2(focusedMass, _parameter.MassSliceWidth, chromatogramRange);
            if (chromatogram.IsEmpty) return null;

            //get peak detection result
            var chromPeakFeatures = GetChromatogramPeakFeatures(chromatogram, detector);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;
            SetRawDataAccessID2ChromatogramPeakFeatures(chromPeakFeatures, provider, chromatogram.Peaks);

            //filtering out noise peaks considering smoothing effects and baseline effects
            chromPeakFeatures = GetBackgroundSubtractedPeaks(chromPeakFeatures, chromatogram.Peaks);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;

            return chromPeakFeatures;
        }

        #region ion mobility utilities
        public List<ChromatogramPeakFeature> ExecutePeakDetectionOnDriftTimeAxis(List<ChromatogramPeakFeature> chromPeakFeatures, RawSpectra rawSpectra, float accumulatedRtRange) {
            var newSpots = new List<ChromatogramPeakFeature>();
            foreach (var peakSpot in chromPeakFeatures) {
                peakSpot.DriftChromFeatures = new List<ChromatogramPeakFeature>();

                //var rtWidth = peakSpot.PeakWidth();
                //if (rtWidth > 0.6) rtWidth = 0.6F;
                //if (rtWidth < 0.2) rtWidth = 0.2F;

                // accumulatedRtRange can be replaced by rtWidth actually, but for alignment results, we have to adjust the RT range to equally estimate the peaks on drift axis
                var chromatogram = rawSpectra.GetDriftChromatogramByScanRtMz(peakSpot.MS1RawSpectrumIdTop, (float)peakSpot.ChromXs.Value, accumulatedRtRange, (float)peakSpot.Mass, _parameter.CentroidMs1Tolerance);
                if (chromatogram.IsEmpty) continue;
                var peaksOnDriftTime = GetPeakAreaBeanListOnDriftTimeAxis(chromatogram, peakSpot, rawSpectra);
                if (peaksOnDriftTime == null || peaksOnDriftTime.Count == 0) continue;
                peakSpot.DriftChromFeatures = peaksOnDriftTime;
                newSpots.Add(peakSpot);
            }
            return newSpots;
        }

        private List<ChromatogramPeakFeature> GetPeakAreaBeanListOnDriftTimeAxis(Chromatogram chromatogram, ChromatogramPeakFeature rtPeakFeature, RawSpectra rawSpectra) {

            var smoothedPeaklist = chromatogram.Smoothing(_parameter.SmoothingMethod, _parameter.SmoothingLevel);
            var detectedPeaks = PeakDetection.PeakDetectionVS1(smoothedPeaklist, _parameter.MinimumDatapoints, _parameter.MinimumAmplitude * 0.25) ?? new List<PeakDetectionResult>(0);
            var maxIntensityAtPeaks = detectedPeaks.DefaultIfEmpty().Max(n => n?.IntensityAtPeakTop) ?? 0d;

            var peaks = new List<ChromatogramPeakFeature>();
            var counter = 0;
            foreach (var result in detectedPeaks) {
                if (result.IsWeakCompareTo(maxIntensityAtPeaks)) {
                    continue;
                }

                var driftFeature = BuildDriftPeakFeature(result, rtPeakFeature, counter, chromatogram, rawSpectra);
                peaks.Add(driftFeature);
                counter++;
            }
            return peaks;
        }

        private ChromatogramPeakFeature BuildDriftPeakFeature(PeakDetectionResult result, ChromatogramPeakFeature rtPeakFeature, int peakId, Chromatogram chromatogram, RawSpectra rawSpectra) {
            var driftFeature = ChromatogramPeakFeature.FromPeakDetectionResult(result, chromatogram, rtPeakFeature.Mass);
            driftFeature.PeakID = peakId;
            driftFeature.ParentPeakID = rtPeakFeature.PeakID;
            driftFeature.IonMode = rtPeakFeature.IonMode;
            var ms2Tol = MolecularFormulaUtility.CalculateMassToleranceBasedOn500Da(_parameter.CentroidMs2Tolerance, rtPeakFeature.Mass);
            var spectra = rawSpectra.GetPeakMs2Spectra(rtPeakFeature, ms2Tol, _parameter.AcquisitionType, driftFeature.ChromXs.Drift);
            driftFeature.SetMs2SpectrumId(spectra);
            return driftFeature;
        }
        #endregion

        #region peak detection utilities
        public List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(Chromatogram chromatogram) {
            var smoothedPeaklist = chromatogram.Smoothing(_parameter.SmoothingMethod, _parameter.SmoothingLevel);
            //var detectedPeaks = PeakDetection.GetDetectedPeakInformationCollectionFromDifferentialBasedPeakDetectionAlgorithm(analysisParametersBean.MinimumDatapoints, analysisParametersBean.MinimumAmplitude, analysisParametersBean.AmplitudeNoiseFactor, analysisParametersBean.SlopeNoiseFactor, analysisParametersBean.PeaktopNoiseFactor, smoothedPeaklist);
            var minDatapoints = _parameter.MinimumDatapoints;
            var minAmps = _parameter.MinimumAmplitude;
            var detectedPeaks = PeakDetection.PeakDetectionVS1(smoothedPeaklist, minDatapoints, minAmps);
            if (detectedPeaks == null || detectedPeaks.Count == 0) return null;

            var chromPeakFeatures = new List<ChromatogramPeakFeature>();

            foreach (var result in detectedPeaks) {
                if (result.IntensityAtPeakTop <= 0) continue;
                var mass = chromatogram.Peaks[result.ScanNumAtPeakTop].Mass;

                //option
                //this method is currently used in LC/MS project.
                //Users can prepare their-own 'exclusion mass' list to exclude unwanted peak features
                var excludeChecker = false;
                foreach (var pair in _parameter.ExcludedMassList.OrEmptyIfNull()) {
                    if (Math.Abs(pair.Mass - mass) < pair.MassTolerance) {
                        excludeChecker = true;
                        break;
                    }
                }
                if (excludeChecker) continue;
                var chromPeakFeature = ChromatogramPeakFeature.FromPeakDetectionResult(result, chromatogram, mass);
                chromPeakFeature.IonMode = _parameter.IonMode;
                chromPeakFeatures.Add(chromPeakFeature);
            }
            return chromPeakFeatures;
        }

        public List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(Chromatogram_temp chromatogram) {
            var smoothedPeaklist = chromatogram.Smoothing(_parameter.SmoothingMethod, _parameter.SmoothingLevel);
            //var detectedPeaks = PeakDetection.GetDetectedPeakInformationCollectionFromDifferentialBasedPeakDetectionAlgorithm(analysisParametersBean.MinimumDatapoints, analysisParametersBean.MinimumAmplitude, analysisParametersBean.AmplitudeNoiseFactor, analysisParametersBean.SlopeNoiseFactor, analysisParametersBean.PeaktopNoiseFactor, smoothedPeaklist);
            var minDatapoints = _parameter.MinimumDatapoints;
            var minAmps = _parameter.MinimumAmplitude;
            var detectedPeaks = PeakDetection.PeakDetectionVS1(smoothedPeaklist, minDatapoints, minAmps);
            if (detectedPeaks == null || detectedPeaks.Count == 0) return null;

            var chromPeakFeatures = new List<ChromatogramPeakFeature>();

            foreach (var result in detectedPeaks) {
                if (result.IntensityAtPeakTop <= 0) continue;
                var mass = chromatogram.Peaks[result.ScanNumAtPeakTop][2];

                //option
                //this method is currently used in LC/MS project.
                //Users can prepare their-own 'exclusion mass' list to exclude unwanted peak features
                var excludeChecker = false;
                foreach (var pair in _parameter.ExcludedMassList.OrEmptyIfNull()) {
                    if (Math.Abs(pair.Mass - mass) < pair.MassTolerance) {
                        excludeChecker = true;
                        break;
                    }
                }
                if (excludeChecker) continue;
                var chromPeakFeature = ChromatogramPeakFeature.FromPeakDetectionResult(result, chromatogram, mass);
                chromPeakFeature.IonMode = _parameter.IonMode;
                chromPeakFeatures.Add(chromPeakFeature);
            }
            return chromPeakFeatures;
        }

        public List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(Chromatogram_temp2 chromatogram, PeakDetection detector) {
            var smoothedPeaklist = chromatogram.Smoothing(_parameter.SmoothingMethod, _parameter.SmoothingLevel);
            //var detectedPeaks = PeakDetection.GetDetectedPeakInformationCollectionFromDifferentialBasedPeakDetectionAlgorithm(analysisParametersBean.MinimumDatapoints, analysisParametersBean.MinimumAmplitude, analysisParametersBean.AmplitudeNoiseFactor, analysisParametersBean.SlopeNoiseFactor, analysisParametersBean.PeaktopNoiseFactor, smoothedPeaklist);
            //var minDatapoints = _parameter.MinimumDatapoints;
            //var minAmps = _parameter.MinimumAmplitude;
            //var detectedPeaks = PeakDetection.PeakDetectionVS1(smoothedPeaklist, minDatapoints, minAmps);
            var detectedPeaks = detector.PeakDetectionVS1(smoothedPeaklist);
            if (detectedPeaks == null || detectedPeaks.Count == 0) return null;

            var chromPeakFeatures = new List<ChromatogramPeakFeature>();

            foreach (var result in detectedPeaks) {
                if (result.IntensityAtPeakTop <= 0) continue;
                var mass = chromatogram.Peaks[result.ScanNumAtPeakTop].Mz;

                //option
                //this method is currently used in LC/MS project.
                //Users can prepare their-own 'exclusion mass' list to exclude unwanted peak features
                var excludeChecker = false;
                foreach (var pair in _parameter.ExcludedMassList.OrEmptyIfNull()) {
                    if (Math.Abs(pair.Mass - mass) < pair.MassTolerance) {
                        excludeChecker = true;
                        break;
                    }
                }
                if (excludeChecker) continue;
                var chromPeakFeature = ChromatogramPeakFeature.FromPeakDetectionResult(result, chromatogram, mass);
                chromPeakFeature.IonMode = _parameter.IonMode;
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
        public void SetRawDataAccessID2ChromatogramPeakFeaturesFor4DChromData(List<ChromatogramPeakFeature> chromPeakFeatures, IReadOnlyList<RawSpectrum> accSpecList, IReadOnlyList<IChromatogramPeak> peaklist) {
            foreach (var feature in chromPeakFeatures) {
                SetRawDataAccessID2ChromatogramPeakFeatureFor4DChromData(feature, accSpecList, peaklist);
            }
        }

        public void SetRawDataAccessID2ChromatogramPeakFeaturesFor4DChromData(List<ChromatogramPeakFeature> chromPeakFeatures, IDataProvider provider, IReadOnlyList<ValuePeak> peaklist) {
            foreach (var feature in chromPeakFeatures) {
                SetRawDataAccessID2ChromatogramPeakFeatureFor4DChromData(feature, provider, peaklist);
            }
        }

        private void SetRawDataAccessID2ChromatogramPeakFeatureFor4DChromData(ChromatogramPeakFeature feature, IReadOnlyList<RawSpectrum> accSpecList, IReadOnlyList<IChromatogramPeak> peaklist) {

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

        private void SetRawDataAccessID2ChromatogramPeakFeatureFor4DChromData(ChromatogramPeakFeature feature, IDataProvider provider, IReadOnlyList<ValuePeak> peaklist) {

            var chromLeftID = feature.ChromScanIdLeft;
            var chromTopID = feature.ChromScanIdTop;
            var chromRightID = feature.ChromScanIdRight;

            feature.MS1AccumulatedMs1RawSpectrumIdLeft = peaklist[chromLeftID].IdOrIndex;
            feature.MS1AccumulatedMs1RawSpectrumIdTop = peaklist[chromTopID].IdOrIndex;
            feature.MS1AccumulatedMs1RawSpectrumIdRight = peaklist[chromRightID].IdOrIndex;

            feature.MS1RawSpectrumIdLeft = provider.LoadMsSpectrumFromIndex(peaklist[chromLeftID].IdOrIndex).OriginalIndex;
            feature.MS1RawSpectrumIdTop = provider.LoadMsSpectrumFromIndex(peaklist[chromTopID].IdOrIndex).OriginalIndex;
            feature.MS1RawSpectrumIdRight = provider.LoadMsSpectrumFromIndex(peaklist[chromRightID].IdOrIndex).OriginalIndex;

            feature.MS2RawSpectrumID = -1; // at this moment, zero must be inserted for deconvolution process
        }

        /// <summary>
        /// peak list should contain the original raw spectrum ID
        /// </summary>
        /// <param name="chromPeakFeatures"></param>
        /// <param name="spectrumList"></param>
        /// <param name="peaklist"></param>
        /// <param name="param"></param>
        public void SetRawDataAccessID2ChromatogramPeakFeatures(List<ChromatogramPeakFeature> chromPeakFeatures, IDataProvider provider, IReadOnlyList<IChromatogramPeak> peaklist) {
            foreach (var feature in chromPeakFeatures) {
                SetRawDataAccessID2ChromatogramPeakFeature(feature, provider, peaklist);
            }
        }

        public void SetRawDataAccessID2ChromatogramPeakFeatures(List<ChromatogramPeakFeature> chromPeakFeatures, IDataProvider provider, IReadOnlyList<double[]> peaklist) {
            foreach (var feature in chromPeakFeatures) {
                SetRawDataAccessID2ChromatogramPeakFeature(feature, provider, peaklist);
            }
        }

        public void SetRawDataAccessID2ChromatogramPeakFeatures(List<ChromatogramPeakFeature> chromPeakFeatures, IDataProvider provider, IReadOnlyList<ValuePeak> peaklist) {
            foreach (var feature in chromPeakFeatures) {
                SetRawDataAccessID2ChromatogramPeakFeature(feature, provider, peaklist);
            }
        }

        public void SetRawDataAccessID2ChromatogramPeakFeature(ChromatogramPeakFeature feature, IDataProvider provider, IReadOnlyList<IChromatogramPeak> peaklist) {
            var chromLeftID = feature.ChromScanIdLeft;
            var chromTopID = feature.ChromScanIdTop;
            var chromRightID = feature.ChromScanIdRight;

            feature.MS1RawSpectrumIdLeft = peaklist[chromLeftID].ID;
            feature.MS1RawSpectrumIdTop = peaklist[chromTopID].ID;
            feature.MS1RawSpectrumIdRight = peaklist[chromRightID].ID;

            SetMS2RawSpectrumIDs2ChromatogramPeakFeature(feature, provider, peaklist[chromLeftID].ID, peaklist[chromTopID].ID, peaklist[chromRightID].ID);
        }

        public void SetRawDataAccessID2ChromatogramPeakFeature(ChromatogramPeakFeature feature, IDataProvider provider, IReadOnlyList<double[]> peaklist) {
            var chromLeftID = feature.ChromScanIdLeft;
            var chromTopID = feature.ChromScanIdTop;
            var chromRightID = feature.ChromScanIdRight;

            feature.MS1RawSpectrumIdLeft = (int)peaklist[chromLeftID][0];
            feature.MS1RawSpectrumIdTop = (int)peaklist[chromTopID][0];
            feature.MS1RawSpectrumIdRight = (int)peaklist[chromRightID][0];

            SetMS2RawSpectrumIDs2ChromatogramPeakFeature(feature, provider, feature.MS1RawSpectrumIdLeft, feature.MS1RawSpectrumIdTop, feature.MS1RawSpectrumIdRight);
        }

        public void SetRawDataAccessID2ChromatogramPeakFeature(ChromatogramPeakFeature feature, IDataProvider provider, IReadOnlyList<ValuePeak> peaklist) {
            var chromLeftID = feature.ChromScanIdLeft;
            var chromTopID = feature.ChromScanIdTop;
            var chromRightID = feature.ChromScanIdRight;

            feature.MS1RawSpectrumIdLeft = peaklist[chromLeftID].IdOrIndex;
            feature.MS1RawSpectrumIdTop = peaklist[chromTopID].IdOrIndex;
            feature.MS1RawSpectrumIdRight = peaklist[chromRightID].IdOrIndex;

            SetMS2RawSpectrumIDs2ChromatogramPeakFeature(feature, provider, feature.MS1RawSpectrumIdLeft, feature.MS1RawSpectrumIdTop, feature.MS1RawSpectrumIdRight);
        }

        public void SetMS2RawSpectrumIDs2ChromatogramPeakFeature(ChromatogramPeakFeature feature, IDataProvider provider, int scanBegin, int scanTop, int scanEnd) {

            var mass = feature.Mass;
            var minDiff = int.MaxValue;
            var ms1SpectrumList = provider.LoadMs1Spectrums();
            var ms2SpectrumList = provider.LoadMsNSpectrums(level:2);
            var ce2MinDiff = new Dictionary<double, double>(); // ce to diff

            var scanPolarity = _parameter.IonMode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;
            if (scanBegin < 0) scanBegin = 0;
            var ms2Tol = _parameter.CentroidMs2Tolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms2Tol));
            #region // practical parameter changes
            if (mass > 500) {
                ms2Tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ppm);
            }
            #endregion

            var ms1Begin = SearchCollection.LowerBound(ms2SpectrumList, ms1SpectrumList[scanBegin], (a, b) => a.ScanNumber.CompareTo(b.ScanNumber));
            var ms1End = SearchCollection.UpperBound(ms2SpectrumList, ms1SpectrumList[scanEnd], (a, b) => a.ScanNumber.CompareTo(b.ScanNumber));
            var ms1TopScanNumber = ms1SpectrumList[scanTop].ScanNumber;
            for (int i = ms1Begin; i < ms1End; i++) {
                var spec = ms2SpectrumList[i];
                if (spec.MsLevel <= 1) continue;
                if (spec.MsLevel == 2 && spec.Precursor != null && scanPolarity == spec.ScanPolarity) {
                    if (IsInMassWindow(mass, spec, ms2Tol, _parameter.AcquisitionType)) {
                        var ce = spec.CollisionEnergy;

                        if (_parameter.AcquisitionType == AcquisitionType.AIF) {
                            var ceRounded = Math.Round(ce, 2); // must be rounded by 2 decimal points
                            if (!ce2MinDiff.ContainsKey(ceRounded) || ce2MinDiff[ceRounded] > Math.Abs(spec.ScanNumber - ms1TopScanNumber)) {
                                ce2MinDiff[ceRounded] = Math.Abs(spec.ScanNumber - ms1TopScanNumber);
                                feature.MS2RawSpectrumID2CE[spec.ScanNumber] = ce;
                            }
                        }
                        else {
                            feature.MS2RawSpectrumID2CE[spec.ScanNumber] = ce;
                        }

                        if (minDiff > Math.Abs(spec.ScanNumber - ms1TopScanNumber)) {
                            minDiff = Math.Abs(spec.ScanNumber - ms1TopScanNumber);
                            feature.MS2RawSpectrumID = spec.ScanNumber;
                        }
                    }
                }
            }
        }

        public List<ChromatogramPeakFeature> GetBackgroundSubtractedPeaks(List<ChromatogramPeakFeature> chromPeakFeatures, IReadOnlyList<IChromatogramPeak> peaklist) {
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

        public List<ChromatogramPeakFeature> GetBackgroundSubtractedPeaks(List<ChromatogramPeakFeature> chromPeakFeatures, IReadOnlyList<double[]> peaklist) {
            var counterThreshold = 4;
            var sPeakAreaList = new List<ChromatogramPeakFeature>();

            foreach (var feature in chromPeakFeatures) {
                var peakTop = feature.ChromScanIdTop;
                var peakLeft = feature.ChromScanIdLeft;
                var peakRight = feature.ChromScanIdRight;

                if (peakTop - 1 < 0 || peakTop + 1 > peaklist.Count - 1) continue;
                if (peaklist[peakTop - 1][3] <= 0 || peaklist[peakTop + 1][3] <= 0) continue;

                var trackingNumber = 10 * (peakRight - peakLeft); if (trackingNumber > 50) trackingNumber = 50;

                var ampDiff = Math.Max(feature.PeakHeightTop - feature.PeakHeightLeft, feature.PeakHeightTop - feature.PeakHeightRight);
                var counter = 0;

                double spikeMax = -1, spikeMin = -1;
                for (int i = peakLeft - trackingNumber; i <= peakLeft; i++) {
                    if (i - 1 < 0) continue;

                    if (peaklist[i - 1][3] < peaklist[i][3] && peaklist[i][3] > peaklist[i + 1][3])
                        spikeMax = peaklist[i][3];
                    else if (peaklist[i - 1][3] > peaklist[i][3] && peaklist[i][3] < peaklist[i + 1][3])
                        spikeMin = peaklist[i][3];

                    if (spikeMax != -1 && spikeMin != -1) {
                        var noise = 0.5 * Math.Abs(spikeMax - spikeMin);
                        if (noise * 3 > ampDiff) counter++;
                        spikeMax = -1; spikeMin = -1;
                    }
                }

                for (int i = peakRight; i <= peakRight + trackingNumber; i++) {
                    if (i + 1 > peaklist.Count - 1) break;

                    if (peaklist[i - 1][3] < peaklist[i][3] && peaklist[i][3] > peaklist[i + 1][3])
                        spikeMax = peaklist[i][3];
                    else if (peaklist[i - 1][3] > peaklist[i][3] && peaklist[i][3] < peaklist[i + 1][3])
                        spikeMin = peaklist[i][3];

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

        public List<ChromatogramPeakFeature> GetBackgroundSubtractedPeaks(List<ChromatogramPeakFeature> chromPeakFeatures, IReadOnlyList<ValuePeak> peaklist) {
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

        public List<ChromatogramPeakFeature> GetFurtherCleanupedChromPeakFeatures(List<ChromatogramPeakFeature> cmbinedFeatures) {
            if (cmbinedFeatures.IsEmptyOrNull()) return cmbinedFeatures;
            var curatedSpots = new List<ChromatogramPeakFeature>();
            var massTolerance = _parameter.CentroidMs1Tolerance * 0.5;
            var rtTolerance = 0.03;
            var excludeList = new List<int>();
            for (int i = 0; i < cmbinedFeatures.Count; i++) {
                var targetSpot = cmbinedFeatures[i];
                for (int j = i + 1; j < cmbinedFeatures.Count; j++) {
                    var searchedSpot = cmbinedFeatures[j];
                    if ((searchedSpot.Mass - targetSpot.Mass) > massTolerance) break;
                    if (Math.Abs(targetSpot.ChromXs.Value - searchedSpot.ChromXs.Value) < rtTolerance) {
                        if ((targetSpot.PeakHeightTop - searchedSpot.PeakHeightTop) > 0) {
                            if (!excludeList.Contains(j))
                                excludeList.Add(j);
                        }
                        else {
                            if (!excludeList.Contains(i))
                                excludeList.Add(i);
                        }
                    }
                }
            }

            for (int i = 0; i < cmbinedFeatures.Count; i++) {
                if (excludeList.Contains(i)) continue;
                curatedSpots.Add(cmbinedFeatures[i]);
            }
            return curatedSpots;
        }

        public List<ChromatogramPeakFeature> RemovePeakAreaBeanRedundancy(List<List<ChromatogramPeakFeature>> chromPeakFeaturesList, List<ChromatogramPeakFeature> chromPeakFeatures, float massStep) {
            if (chromPeakFeaturesList == null || chromPeakFeaturesList.Count == 0) return chromPeakFeatures;

            var parentPeakAreaBeanList = chromPeakFeaturesList[chromPeakFeaturesList.Count - 1];

            for (int i = 0; i < chromPeakFeatures.Count; i++) {
                //if (Math.Abs(chromPeakFeatures[i].Mass - 443.99816) < 0.01 && Math.Abs(chromPeakFeatures[i].ChromXs.RT.Value - 100.021) < 0.05) {
                //    Console.WriteLine();
                //}
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

        public List<ChromatogramPeakFeature> GetRecalculatedChromPeakFeaturesByMs1MsTolerance(List<ChromatogramPeakFeature> chromPeakFeatures, IDataProvider provider) {
            // var spectrumList = param.MachineCategory == MachineCategory.LCIMMS ? rawObj.AccumulatedSpectrumList : rawObj.SpectrumList;
            var recalculatedPeakspots = new List<ChromatogramPeakFeature>();
            var minDatapoint = 3;
            // var counter = 0;
            var rawSpectra = new RawSpectra(provider.LoadMs1Spectrums(), _parameter.IonMode, _parameter.AcquisitionType);
            foreach (var spot in chromPeakFeatures) {
                //get EIC chromatogram
                var peakWidth = spot.PeakWidth();
                var peakWidthMargin = spot.PeakWidth() * 0.5;
                var chromatogramRange = new ChromatogramRange(spot.ChromXsLeft.Value - peakWidthMargin, spot.ChromXsRight.Value + peakWidthMargin, spot.ChromXs.Type, spot.ChromXs.Unit);
                var chromatogram = rawSpectra.GetMs1ExtractedChromatogram(spot.Mass, _parameter.CentroidMs1Tolerance, chromatogramRange);

                var sPeaklist = chromatogram.Smoothing(_parameter.SmoothingMethod, _parameter.SmoothingLevel);
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
                if (sPeaklist[maxID].Intensity - Math.Min(sPeaklist[minLeftId].Intensity, sPeaklist[minRightId].Intensity) < _parameter.MinimumAmplitude) continue;

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

                spot.MS1RawSpectrumIdTop = chromatogram.Peaks[maxID].ID;
                spot.MS1RawSpectrumIdLeft = chromatogram.Peaks[minLeftId].ID;
                spot.MS1RawSpectrumIdRight = chromatogram.Peaks[minRightId].ID;

                var peakHeightFromBaseline = Math.Max(sPeaklist[maxID].Intensity - sPeaklist[minLeftId].Intensity, sPeaklist[maxID].Intensity - sPeaklist[minRightId].Intensity);
                spot.PeakShape.SignalToNoise = (float)(peakHeightFromBaseline / spot.PeakShape.EstimatedNoise);

                if (spot.DriftChromFeatures == null) { // meaning not ion mobility data
                    SetMS2RawSpectrumIDs2ChromatogramPeakFeature(spot, provider, spot.MS1RawSpectrumIdLeft, spot.MS1RawSpectrumIdTop, spot.MS1RawSpectrumIdRight);
                }
                recalculatedPeakspots.Add(spot);
            }
            return recalculatedPeakspots;
        }

        public List<ChromatogramPeakFeature> GetOtherChromPeakFeatureProperties(List<ChromatogramPeakFeature> chromPeakFeatures) {
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

            return chromPeakFeatures.OrderBy(n => n.MasterPeakID).ToList();
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

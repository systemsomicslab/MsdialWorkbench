using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using CompMs.Raw.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm
{
    public sealed class PeakSpottingCore {
        private readonly ParameterBase _parameter;

        public PeakSpottingCore(ParameterBase parameter) {
            _parameter = parameter;
        }

        // feature detection for rt (or ion mobility), m/z, and intensity (3D) data 
        // this method can be used in GC-MS, LC-MS, and IM-MS project
        public Task<List<ChromatogramPeakFeature>> Execute3DFeatureDetectionAsync(AnalysisFileBean file, IDataProvider provider, ChromatogramRange chromatogramRange, int numThreads, ReportProgress reporter, CancellationToken token = default) {
            var isTargetedMode = !_parameter.CompoundListInTargetMode.IsEmptyOrNull();
            return isTargetedMode
                ? Execute3DFeatureDetectionTargetModeAsync(file, provider, numThreads, chromatogramRange, token)
                : Execute3DFeatureDetectionNormalModeAsync(file, provider, numThreads, reporter, chromatogramRange, token);
        }

        public Task<List<ChromatogramPeakFeature>> Execute3DFeatureDetectionTargetModeAsync(AnalysisFileBean file, IDataProvider provider, int numThreads, ChromatogramRange chromatogramRange, CancellationToken token = default) {
            return numThreads switch
            {
                <= 1 => Execute3DFeatureDetectionTargetModeBySingleThreadAsync(file, provider, chromatogramRange, token),
                _ => Execute3DFeatureDetectionTargetModeByMultiThreadAsync(file, provider, numThreads, chromatogramRange, token)
            };
        }

        public Task<List<ChromatogramPeakFeature>> Execute3DFeatureDetectionNormalModeAsync(AnalysisFileBean file, IDataProvider provider, int numThreads, ReportProgress reporter, ChromatogramRange chromatogramRange, CancellationToken token = default) {
            return numThreads switch
            {
                <= 1 => Execute3DFeatureDetectionNormalModeBySingleThreadAsync(file, provider, reporter, chromatogramRange, token),
                _ => Execute3DFeatureDetectionNormalModeByMultiThreadAsync(file, provider, numThreads, reporter, chromatogramRange, token)
            };
        }

        public async Task<List<ChromatogramPeakFeature>> GetCombinedChromPeakFeaturesAsync(IReadOnlyList<List<ChromatogramPeakFeature>> featuresList, IDataProvider provider, AcquisitionType type, CancellationToken token = default) {
            var cmbinedFeatures = GetCombinedChromPeakFeatures(featuresList);

            var rawSpectra = new RawSpectra(provider, _parameter.IonMode, type);
            cmbinedFeatures = GetRecalculatedChromPeakFeaturesByMs1MsTolerance(cmbinedFeatures, rawSpectra);
            await SetRawDataAccessID2ChromatogramPeakFeaturesAsync(cmbinedFeatures, provider, type, token).ConfigureAwait(false);

            // test
            cmbinedFeatures = cmbinedFeatures.OrderBy(n => n.PeakFeature.Mass).ThenBy(n => n.ChromXs.Value).ToList();
            cmbinedFeatures = GetFurtherCleanupedChromPeakFeatures(cmbinedFeatures);
            cmbinedFeatures = GetOtherChromPeakFeatureProperties(cmbinedFeatures);

            return cmbinedFeatures;
        }

        private async Task<List<ChromatogramPeakFeature>> Execute3DFeatureDetectionNormalModeBySingleThreadAsync(AnalysisFileBean file, IDataProvider provider, ReportProgress reporter, ChromatogramRange chromatogramRange, CancellationToken token = default) {
            var chromPeakFeaturesList = new List<List<ChromatogramPeakFeature>>();
            (float startMass, float endMass) = provider.GetMs1Range(_parameter.IonMode);
            if (startMass < _parameter.MassRangeBegin) startMass = _parameter.MassRangeBegin;
            if (endMass > _parameter.MassRangeEnd) endMass = _parameter.MassRangeEnd;
            float focusedMass = startMass, massStep = _parameter.MassSliceWidth;
            if (_parameter.AccuracyType == AccuracyType.IsNominal) {
                massStep = 1.0F;
                System.Diagnostics.Debug.Assert(_parameter.MassSliceWidth == .5f);
                startMass = (float)Math.Floor(startMass);
                endMass = (float)Math.Ceiling(endMass);
                focusedMass = startMass;
            }
            var rawSpectra = new RawSpectra(provider, _parameter.IonMode, file.AcquisitionType);
            var detector = new PeakDetection(_parameter.MinimumDatapoints, _parameter.MinimumAmplitude);
            while (focusedMass < endMass) {
                if (focusedMass < _parameter.MassRangeBegin) { focusedMass += massStep; continue; }
                if (focusedMass > _parameter.MassRangeEnd) break;

                //get EIC chromatogram
                var mzRange = new MzRange(focusedMass, _parameter.MassSliceWidth);
                using ExtractedIonChromatogram chromatogram = await rawSpectra.GetMS1ExtractedChromatogramAsync(mzRange, chromatogramRange, token).ConfigureAwait(false);
                var chromPeakFeatures = GetChromatogramPeakFeatures(detector, chromatogram);
                if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) {
                    focusedMass += massStep;
                    reporter?.Report(focusedMass - startMass, endMass - startMass);
                    continue;
                }

                //removing peak spot redundancies among slices
                chromPeakFeatures = RemovePeakAreaBeanRedundancy(chromPeakFeaturesList, chromPeakFeatures, massStep);
                if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) {
                    focusedMass += massStep;
                    reporter?.Report(focusedMass - startMass, endMass - startMass);
                    continue;
                }

                chromPeakFeaturesList.Add(chromPeakFeatures);
                focusedMass += massStep;
                reporter?.Report(focusedMass - startMass, endMass - startMass);
            }
            return await GetCombinedChromPeakFeaturesAsync(chromPeakFeaturesList, provider, file.AcquisitionType, token).ConfigureAwait(false);
        }

        private async Task<List<ChromatogramPeakFeature>> Execute3DFeatureDetectionNormalModeByMultiThreadAsync(AnalysisFileBean file, IDataProvider provider, int numThreads, ReportProgress reporter, ChromatogramRange chromatogramRange, CancellationToken token = default) {
            var (startMass, endMass) = provider.GetMs1Range(_parameter.IonMode);
            startMass = Math.Max(startMass, _parameter.MassRangeBegin);
            endMass = Math.Min(endMass, _parameter.MassRangeEnd);
            float massStep = _parameter.MassSliceWidth;
            if (_parameter.AccuracyType == AccuracyType.IsNominal) {
                massStep = 1.0F;
                System.Diagnostics.Debug.Assert(_parameter.MassSliceWidth == .5f);
                startMass = (float)Math.Floor(startMass);
                endMass = (float)Math.Ceiling(endMass);
            }

            var targetMasses = GetFocusedMassList(startMass, endMass, massStep);
            var chromPeakFeaturesArray = new List<ChromatogramPeakFeature>[targetMasses.Count];

            numThreads = Math.Max(2, numThreads);
            using (var bc = new BlockingCollection<(ExtractedIonChromatogram, int)>(numThreads * 4)) {
                var counter = 0;
                var rawSpectra = new RawSpectra(provider, _parameter.IonMode, file.AcquisitionType);
                var tasks = new Task[numThreads];
                tasks[0] = ProduceChromatogramAsync(bc, rawSpectra, chromatogramRange, targetMasses, token);
                for (int i = 1; i < numThreads; i++) {
                    var detector = new PeakDetection(_parameter.MinimumDatapoints, _parameter.MinimumAmplitude);
                    tasks[i] = ConsumeChromatogramAsync(bc, detector, chromPeakFeaturesArray, () => reporter?.Report(Interlocked.Increment(ref counter), targetMasses.Count), token);
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            // finalization
            return await FinalizePeakSpottingResultAsync(chromPeakFeaturesArray, massStep, provider, file.AcquisitionType, token).ConfigureAwait(false);
        }

        private Task ProduceChromatogramAsync(BlockingCollection<(ExtractedIonChromatogram, int)> bc, RawSpectra rawSpectra, ChromatogramRange range, IEnumerable<double> targetMasses, CancellationToken token) {
            return Task.Run(() =>
            {
                foreach (var (chromatogram, index) in rawSpectra.GetMS1ExtractedChromatograms(targetMasses, _parameter.MassSliceWidth, range).WithIndex()) {
                    bc.Add((chromatogram, index));
                }
                bc.CompleteAdding();
            }, token);
        }

        private Task ConsumeChromatogramAsync(BlockingCollection<(ExtractedIonChromatogram, int)> bc, PeakDetection detector, List<ChromatogramPeakFeature>[] chromPeakFeaturesArray, Action report, CancellationToken token) {
            return Task.Run(() =>
            {
                foreach (var (chromatogram, index) in bc.GetConsumingEnumerable(token)) {
                    using (chromatogram) {
                        chromPeakFeaturesArray[index] = GetChromatogramPeakFeatures(detector, chromatogram);
                        report?.Invoke();
                    }
                }
            }, token);
        }

        public async Task<List<ChromatogramPeakFeature>> FinalizePeakSpottingResultAsync(List<ChromatogramPeakFeature>[] chromPeakFeaturesArray, float massStep, IDataProvider provider, AcquisitionType type, CancellationToken token = default) {
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
            return await GetCombinedChromPeakFeaturesAsync(chromPeakFeaturesList, provider, type, token).ConfigureAwait(false);
        }

        public List<double> GetFocusedMassList(float startMass, float endMass, float massStep) {
            var massList = new List<double>();
            var i = 0;
            while (startMass + i * massStep < endMass) {
                massList.Add(startMass + i * massStep);
                ++i;
            }
            return massList;
        }

        public Task<List<ChromatogramPeakFeature>> Execute3DFeatureDetectionTargetModeBySingleThreadAsync(AnalysisFileBean file, IDataProvider provider, ChromatogramRange chromatogramRange, CancellationToken token = default) {
            return Execute3DFeatureDetectionTargetModeAsync(file, provider, _parameter.CompoundListInTargetMode, chromatogramRange, token);
        }

        public async Task<List<ChromatogramPeakFeature>> Execute3DFeatureDetectionTargetModeAsync(AnalysisFileBean file, IDataProvider provider, List<MoleculeMsReference> targetedScans, ChromatogramRange chromatogramRange, CancellationToken token = default) {
            if (targetedScans.IsEmptyOrNull()) {
                return null;
            }

            var chromPeakFeaturesList = new List<List<ChromatogramPeakFeature>>();
            var rawSpectra = new RawSpectra(provider, _parameter.IonMode, file.AcquisitionType);
            foreach (var targetComp in targetedScans) {
                var chromPeakFeatures = await GetChromatogramPeakFeaturesAsync(rawSpectra, provider, (float)targetComp.PrecursorMz, chromatogramRange, token).ConfigureAwait(false);
                if (!chromPeakFeatures.IsEmptyOrNull()) {
                    chromPeakFeaturesList.Add(chromPeakFeatures);
                }
            }

            return await GetCombinedChromPeakFeaturesAsync(chromPeakFeaturesList, provider, file.AcquisitionType, token).ConfigureAwait(false);
        }

        public Task<List<ChromatogramPeakFeature>> Execute3DFeatureDetectionTargetModeByMultiThreadAsync(AnalysisFileBean file, IDataProvider provider, int numThreads, ChromatogramRange chromatogramRange, CancellationToken token = default) {
            var targetedScans = _parameter.CompoundListInTargetMode;
            return Execute3DFeatureDetectionTargetModeByMultiThreadAsync(file, provider, targetedScans, numThreads, chromatogramRange, token);
        }

        public async Task<List<ChromatogramPeakFeature>> Execute3DFeatureDetectionTargetModeByMultiThreadAsync(AnalysisFileBean file, IDataProvider provider, List<MoleculeMsReference> targetedScans, int numThreads, ChromatogramRange chromatogramRange, CancellationToken token = default) {
            if (targetedScans.IsEmptyOrNull()) {
                return null;
            }
            var rawSpectra = new RawSpectra(provider, _parameter.IonMode, file.AcquisitionType);
            var chromPeakFeaturesListTask = targetedScans
                .AsParallel()
                .AsOrdered()
                .WithCancellation(token)
                .WithDegreeOfParallelism(numThreads)
                .Select(targetedScan => GetChromatogramPeakFeaturesAsync(rawSpectra, provider, (float)targetedScan.PrecursorMz, chromatogramRange, token))
                .ToArray();
            var chromPeakFeaturesList = await Task.WhenAll(chromPeakFeaturesListTask).ConfigureAwait(false);
            return await GetCombinedChromPeakFeaturesAsync(chromPeakFeaturesList, provider, file.AcquisitionType, token).ConfigureAwait(false);
        }

        public async Task<List<ChromatogramPeakFeature>> GetChromatogramPeakFeaturesAsync(RawSpectra rawSpectra, IDataProvider provider, float focusedMass, ChromatogramRange chromatogramRange, CancellationToken token = default) {

            //get EIC chromatogram
            using var chromatogram = await rawSpectra.GetMS1ExtractedChromatogramAsync(new MzRange(focusedMass, _parameter.MassSliceWidth), chromatogramRange, token).ConfigureAwait(false);
            if (chromatogram.IsEmpty) return null;

            //get peak detection result
            var chromPeakFeatures = GetChromatogramPeakFeatures(chromatogram);
            if (chromPeakFeatures is null || chromPeakFeatures.Count == 0) return null;
            var peaks = ((Chromatogram)chromatogram).AsPeakArray();
            await SetRawDataAccessID2ChromatogramPeakFeaturesAsync(chromPeakFeatures, provider, peaks, rawSpectra.AcquisitionType, token).ConfigureAwait(false);

            //filtering out noise peaks considering smoothing effects and baseline effects
            chromPeakFeatures = GetBackgroundSubtractedPeaks(chromPeakFeatures, peaks);
            if (chromPeakFeatures is null || chromPeakFeatures.Count == 0) return null;

            return chromPeakFeatures;
        }

        public List<ChromatogramPeakFeature>? GetChromatogramPeakFeatures(PeakDetection detector, ExtractedIonChromatogram chromatogram) {
            if (chromatogram.IsEmpty) return null;

            //get peak detection result
            var chromPeakFeatures = GetChromatogramPeakFeatures(chromatogram, detector);
            if (chromPeakFeatures is null || chromPeakFeatures.Count == 0) return null;

            //filtering out noise peaks considering smoothing effects and baseline effects
            chromPeakFeatures = GetBackgroundSubtractedPeaks(chromPeakFeatures, chromatogram);
            if (chromPeakFeatures is null || chromPeakFeatures.Count == 0) return null;

            return chromPeakFeatures;
        }

        #region ion mobility utilities
        public async Task<List<ChromatogramPeakFeature>> ExecutePeakDetectionOnDriftTimeAxisAsync(List<ChromatogramPeakFeature> chromPeakFeatures, RawSpectra rawSpectra, float accumulatedRtRange, CancellationToken token = default) {
            var newSpots = new List<ChromatogramPeakFeature>();
            foreach (var peakSpot in chromPeakFeatures) {
                peakSpot.DriftChromFeatures = [];

                // accumulatedRtRange can be replaced by rtWidth actually, but for alignment results, we have to adjust the RT range to equally estimate the peaks on drift axis
                using var chromatogram = await rawSpectra.GetDriftChromatogramByScanRtMzAsync(peakSpot.MS1RawSpectrumIdTop, (float)peakSpot.ChromXs.Value, accumulatedRtRange, (float)peakSpot.PeakFeature.Mass, _parameter.CentroidMs1Tolerance, token).ConfigureAwait(false);
                if (chromatogram.IsEmpty) continue;
                var peaksOnDriftTime = await GetPeakAreaBeanListOnDriftTimeAxisAsync(chromatogram, peakSpot, rawSpectra, accumulatedRtRange).ConfigureAwait(false);
                if (peaksOnDriftTime is null || peaksOnDriftTime.Count == 0) continue;
                peakSpot.DriftChromFeatures = peaksOnDriftTime;
                newSpots.Add(peakSpot);
            }
            return newSpots;
        }

        private async Task<List<ChromatogramPeakFeature>> GetPeakAreaBeanListOnDriftTimeAxisAsync(Chromatogram chromatogram, ChromatogramPeakFeature rtPeakFeature, RawSpectra rawSpectra, double accumulatedRtRange, CancellationToken token = default) {
            using Chromatogram smoothed = chromatogram.ChromatogramSmoothing(_parameter.SmoothingMethod, _parameter.SmoothingLevel);
            var smoothedPeaklist = smoothed.AsPeakArray();
            var detectedPeaks = PeakDetection.PeakDetectionVS1(smoothedPeaklist, _parameter.MinimumDatapoints, _parameter.MinimumAmplitude * 0.25) ?? [];
            var maxIntensityAtPeaks = detectedPeaks.DefaultIfEmpty().Max(n => n?.IntensityAtPeakTop) ?? 0d;

            var peaks = new List<ChromatogramPeakFeature>();
            var counter = 0;
            foreach (var result in detectedPeaks) {
                if (result.IsWeakCompareTo(maxIntensityAtPeaks)) {
                    continue;
                }

                var driftFeature = await BuildDriftPeakFeatureAsync(result, rtPeakFeature, counter, chromatogram, rawSpectra, accumulatedRtRange, token).ConfigureAwait(false);
                peaks.Add(driftFeature);
                counter++;
            }
            peaks = GetBackgroundSubtractedPeaks(peaks, chromatogram);
            if (peaks == null || peaks.Count == 0) return null;
            return peaks;
        }

        private async Task<ChromatogramPeakFeature> BuildDriftPeakFeatureAsync(PeakDetectionResult result, ChromatogramPeakFeature rtPeakFeature, int peakId, Chromatogram chromatogram, RawSpectra rawSpectra, double accumulatedRtRange, CancellationToken token = default) {
            var driftFeature = ChromatogramPeakFeature.FromPeakDetectionResult(result, chromatogram, rtPeakFeature.PeakFeature.Mass);
            driftFeature.PeakID = peakId;
            driftFeature.ParentPeakID = rtPeakFeature.PeakID;
            driftFeature.IonMode = rtPeakFeature.IonMode;
            driftFeature.PeakFeature.ChromXsLeft.RT = rtPeakFeature.PeakFeature.ChromXsTop.RT - accumulatedRtRange * 0.5;
            driftFeature.PeakFeature.ChromXsTop.RT = rtPeakFeature.PeakFeature.ChromXsTop.RT;
            driftFeature.PeakFeature.ChromXsRight.RT = rtPeakFeature.PeakFeature.ChromXsTop.RT + accumulatedRtRange * 0.5;
            var ms2Tol = MolecularFormulaUtility.FixMassTolerance(_parameter.CentroidMs2Tolerance, rtPeakFeature.PeakFeature.Mass);
            var spectra = await rawSpectra.GetPeakMs2SpectraAsync(rtPeakFeature, ms2Tol, rawSpectra.AcquisitionType, driftFeature.ChromXs.Drift, token).ConfigureAwait(false);
            driftFeature.SetMs2SpectrumId(spectra);
            return driftFeature;
        }
        #endregion

        #region peak detection utilities
        public List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(Chromatogram chromatogram) {
            using Chromatogram smoothed = chromatogram.ChromatogramSmoothing(_parameter.SmoothingMethod, _parameter.SmoothingLevel);
            var smoothedPeaklist = smoothed.AsPeakArray();
            //var detectedPeaks = PeakDetection.GetDetectedPeakInformationCollectionFromDifferentialBasedPeakDetectionAlgorithm(analysisParametersBean.MinimumDatapoints, analysisParametersBean.MinimumAmplitude, analysisParametersBean.AmplitudeNoiseFactor, analysisParametersBean.SlopeNoiseFactor, analysisParametersBean.PeaktopNoiseFactor, smoothedPeaklist);
            var minDatapoints = _parameter.MinimumDatapoints;
            var minAmps = _parameter.MinimumAmplitude;
            var detectedPeaks = PeakDetection.PeakDetectionVS1(smoothedPeaklist, minDatapoints, minAmps);
            if (detectedPeaks is null || detectedPeaks.Count == 0) return null;

            var chromPeakFeatures = new List<ChromatogramPeakFeature>();
            foreach (var result in detectedPeaks) {
                if (result.IntensityAtPeakTop <= 0) continue;
                var mass = chromatogram.Mz(result.ScanNumAtPeakTop);

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
                if (excludeChecker) {
                    continue;
                }

                var chromPeakFeature = ChromatogramPeakFeature.FromPeakDetectionResult(result, chromatogram, mass);
                chromPeakFeature.IonMode = _parameter.IonMode;
                chromPeakFeatures.Add(chromPeakFeature);
            }
            return chromPeakFeatures;
        }

        public List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(ExtractedIonChromatogram chromatogram, PeakDetection detector) {
            using ExtractedIonChromatogram smoothedChromatogram = chromatogram.ChromatogramSmoothing(_parameter.SmoothingMethod, _parameter.SmoothingLevel);
            var detectedPeaks = detector.PeakDetectionVS1(smoothedChromatogram);
            if (detectedPeaks is null || detectedPeaks.Count == 0) return null;

            var chromPeakFeatures = new List<ChromatogramPeakFeature>();
            foreach (var result in detectedPeaks) {
                if (result.IntensityAtPeakTop <= 0) continue;
                var mass = smoothedChromatogram.Mz(result.ScanNumAtPeakTop);

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
                var chromPeakFeature = ChromatogramPeakFeature.FromPeakDetectionResult(result, chromatogram, mass, _parameter.IonMode);
                chromPeakFeatures.Add(chromPeakFeature);
            }
            return chromPeakFeatures;
        }

        /// <summary>
        /// peak list should contain the original raw spectrum ID
        /// </summary>
        /// <param name="chromPeakFeatures"></param>
        /// <param name="provider"></param>
        public void SetRawDataAccessID2ChromatogramPeakFeaturesFor4DChromData(List<ChromatogramPeakFeature> chromPeakFeatures, IDataProvider provider) {
            foreach (var feature in chromPeakFeatures) {
                SetRawDataAccessID2ChromatogramPeakFeatureFor4DChromData(feature, provider);
            }
        }

        private void SetRawDataAccessID2ChromatogramPeakFeatureFor4DChromData(ChromatogramPeakFeature feature, IDataProvider provider) {
            var chromLeftID = feature.PeakFeature.ChromScanIdLeft;
            var chromTopID = feature.PeakFeature.ChromScanIdTop;
            var chromRightID = feature.PeakFeature.ChromScanIdRight;

            RawSpectrum peakLeft = provider.LoadMs1SpectrumFromIndex(chromLeftID);
            RawSpectrum peakTop= provider.LoadMs1SpectrumFromIndex(chromTopID);
            RawSpectrum peakRight = provider.LoadMs1SpectrumFromIndex(chromRightID);

            feature.MS1AccumulatedMs1RawSpectrumIdLeft = (int)peakLeft.RawSpectrumID.ID;
            feature.MS1AccumulatedMs1RawSpectrumIdTop = (int)peakTop.RawSpectrumID.ID;
            feature.MS1AccumulatedMs1RawSpectrumIdRight = (int)peakRight.RawSpectrumID.ID;
            feature.AccumulatedDataIDType = peakTop.RawSpectrumID.IDType;

            feature.MS1RawSpectrumIdLeft = (int)((IModifiedSpectrumIdentifier)peakLeft.RawSpectrumID).OriginalIDs[0].ID;
            feature.MS1RawSpectrumIdTop = (int)((IModifiedSpectrumIdentifier)peakTop.RawSpectrumID).OriginalIDs[0].ID;
            feature.MS1RawSpectrumIdRight = (int)((IModifiedSpectrumIdentifier)peakRight.RawSpectrumID).OriginalIDs[0].ID;
            feature.RawDataIDType = ((IModifiedSpectrumIdentifier)peakTop.RawSpectrumID).OriginalIDs[0].IDType;

            feature.MS2RawSpectrumID = -1; // at this moment, zero must be inserted for deconvolution process
        }

        /// <summary>
        /// peak list should contain the original raw spectrum ID
        /// </summary>
        /// <param name="chromPeakFeatures"></param>
        /// <param name="provider"></param>
        /// <param name="peaklist"></param>
        /// <param name="type"></param>
        /// <param name="token"></param>
        public Task SetRawDataAccessID2ChromatogramPeakFeaturesAsync(List<ChromatogramPeakFeature> chromPeakFeatures, IDataProvider provider, IReadOnlyList<IChromatogramPeak> peaklist, AcquisitionType type, CancellationToken token = default) {
            foreach (var feature in chromPeakFeatures) {
                var chromLeftID = feature.PeakFeature.ChromScanIdLeft;
                var chromTopID = feature.PeakFeature.ChromScanIdTop;
                var chromRightID = feature.PeakFeature.ChromScanIdRight;

                feature.MS1RawSpectrumIdLeft = peaklist[chromLeftID].ID;
                feature.MS1RawSpectrumIdTop = peaklist[chromTopID].ID;
                feature.MS1RawSpectrumIdRight = peaklist[chromRightID].ID;

                feature.PeakFeature.ChromXsLeft = peaklist[chromLeftID].ChromXs;
                feature.PeakFeature.ChromXsTop = peaklist[chromTopID].ChromXs;
                feature.PeakFeature.ChromXsRight = peaklist[chromRightID].ChromXs;
            }
            return SetRawDataAccessID2ChromatogramPeakFeaturesAsync(chromPeakFeatures, provider, type, token);
        }

        private async Task SetRawDataAccessID2ChromatogramPeakFeaturesAsync(List<ChromatogramPeakFeature> chromPeakFeatures, IDataProvider provider, AcquisitionType type, CancellationToken token = default) {
            var sorted = chromPeakFeatures.OrderBy(p => p.PeakFeature.ChromXsLeft.RT.Value).ToList();
            var queries = sorted.Select(p => {
                var mass = p.PeakFeature.Mass;
                var ms2Tol = MolecularFormulaUtility.FixMassTolerance(_parameter.CentroidMs2Tolerance, mass);

                var query = new SpectraLoadingQuery
                {
                    ScanTimeRange = new ScanTimeRange { Start = p.PeakFeature.ChromXsLeft.RT.Value, End = p.PeakFeature.ChromXsRight.RT.Value, },
                    PrecursorMzRange = new PrecursorMzRange { Mz = mass, Tolerance = ms2Tol, },
                    MSLevel = 2,
                    EnableQ1Deconvolution = type == AcquisitionType.ZTScan,
                };
                return query;
            }).ToArray();

            var scanPolarity = _parameter.IonMode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;
            await foreach (var (spectra, feature, query) in ZipAsync(provider.LoadMSSpectraAsync(queries, token), sorted, queries)) {
                SetMS2RawSpectrumIDs(feature, feature.PeakFeature.ChromXsTop.RT.Value, type, query.PrecursorMzRange.Mz, scanPolarity, query.PrecursorMzRange.Tolerance, spectra);
            }
        }

        private static async IAsyncEnumerable<(T, U, V)> ZipAsync<T, U, V>(IAsyncEnumerable<T> first, IEnumerable<U> second, IEnumerable<V> third) {
            using var secondEnumerator = second.GetEnumerator();
            using var thirdEnumerator = third.GetEnumerator();
            await foreach (var item in first) {
                if (!secondEnumerator.MoveNext() || !thirdEnumerator.MoveNext()) {
                    break;
                }
                yield return (item, secondEnumerator.Current, thirdEnumerator.Current);
            }
        }

        private static void SetMS2RawSpectrumIDs(ChromatogramPeakFeature feature, double scanTopTime, AcquisitionType type, double mass, ScanPolarity scanPolarity, double ms2Tol, RawSpectrum[] spectra) {
            var ce2MinDiff = new Dictionary<double, double>(); // ce to diff
            var minDiff = double.MaxValue;
            foreach (var spec in spectra) {
                if (spec.MsLevel != 2 || spec.Precursor is null || scanPolarity != spec.ScanPolarity || !spec.Precursor.ContainsMz(mass, ms2Tol, type) || spec.Spectrum.Length == 0) {
                    continue;
                }

                var ce = spec.CollisionEnergy;

                if (type == AcquisitionType.AIF) {
                    var ceRounded = Math.Round(ce, 2); // must be rounded by 2 decimal points
                    if (!ce2MinDiff.ContainsKey(ceRounded) || ce2MinDiff[ceRounded] > Math.Abs(spec.ScanStartTime - scanTopTime)) {
                        ce2MinDiff[ceRounded] = Math.Abs(spec.ScanStartTime - scanTopTime);
                        feature.MS2RawSpectrumID2CE[(int)spec.RawSpectrumID.ID] = ce;
                    }
                }
                else {
                    feature.MS2RawSpectrumID2CE[(int)spec.RawSpectrumID.ID] = ce;
                }

                if (minDiff > Math.Abs(spec.ScanStartTime - scanTopTime)) {
                    minDiff = Math.Abs(spec.ScanStartTime - scanTopTime);
                    feature.MS2RawSpectrumID = (int)spec.RawSpectrumID.ID;
                }
            }
        }

        public List<ChromatogramPeakFeature> GetBackgroundSubtractedPeaks(List<ChromatogramPeakFeature> chromPeakFeatures, IReadOnlyList<IChromatogramPeak> peaklist) {
            var counterThreshold = 4;
            var sPeakAreaList = new List<ChromatogramPeakFeature>();

            foreach (var feature in chromPeakFeatures) {
                var peakTop = feature.PeakFeature.ChromScanIdTop;
                var peakLeft = feature.PeakFeature.ChromScanIdLeft;
                var peakRight = feature.PeakFeature.ChromScanIdRight;

                if (peakTop - 1 < 0 || peakTop + 1 > peaklist.Count - 1) continue;
                if (peaklist[peakTop - 1].Intensity <= 0 || peaklist[peakTop + 1].Intensity <= 0) continue;

                var trackingNumber = 10 * (peakRight - peakLeft); if (trackingNumber > 50) trackingNumber = 50;

                var ampDiff = Math.Max(feature.PeakFeature.PeakHeightTop - feature.PeakFeature.PeakHeightLeft, feature.PeakFeature.PeakHeightTop - feature.PeakFeature.PeakHeightRight);
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
                var peakTop = feature.PeakFeature.ChromScanIdTop;
                var peakLeft = feature.PeakFeature.ChromScanIdLeft;
                var peakRight = feature.PeakFeature.ChromScanIdRight;

                if (peakTop - 1 < 0 || peakTop + 1 > peaklist.Count - 1) continue;
                if (peaklist[peakTop - 1][3] <= 0 || peaklist[peakTop + 1][3] <= 0) continue;

                var trackingNumber = 10 * (peakRight - peakLeft); if (trackingNumber > 50) trackingNumber = 50;

                var ampDiff = Math.Max(feature.PeakFeature.PeakHeightTop - feature.PeakFeature.PeakHeightLeft, feature.PeakFeature.PeakHeightTop - feature.PeakFeature.PeakHeightRight);
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

        public List<ChromatogramPeakFeature> GetBackgroundSubtractedPeaks(List<ChromatogramPeakFeature> chromPeakFeatures, Chromatogram chromatogram) {
            var counterThreshold = 4;
            var sPeakAreaList = new List<ChromatogramPeakFeature>();

            foreach (var feature in chromPeakFeatures) {
                var peakTop = feature.PeakFeature.ChromScanIdTop;
                var peakLeft = feature.PeakFeature.ChromScanIdLeft;
                var peakRight = feature.PeakFeature.ChromScanIdRight;

                if (!chromatogram.IsValidPeakTop(peakTop)) continue;

                var trackingNumber = 10 * (peakRight - peakLeft); if (trackingNumber > 50) trackingNumber = 50;
                var ampDiff = Math.Max(feature.PeakFeature.PeakHeightTop - feature.PeakFeature.PeakHeightLeft, feature.PeakFeature.PeakHeightTop - feature.PeakFeature.PeakHeightRight);
                var counter = 0;
                counter += chromatogram.CountSpikes(peakLeft - trackingNumber, peakLeft, ampDiff / 3d);
                counter += chromatogram.CountSpikes(peakRight, peakRight + trackingNumber, ampDiff / 3d);

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
                    if ((searchedSpot.PeakFeature.Mass - targetSpot.PeakFeature.Mass) > massTolerance) break;
                    if (Math.Abs(targetSpot.ChromXs.Value - searchedSpot.ChromXs.Value) < rtTolerance) {
                        if ((targetSpot.PeakFeature.PeakHeightTop - searchedSpot.PeakFeature.PeakHeightTop) > 0) {
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
                    if (Math.Abs(parentPeakAreaBeanList[j].PeakFeature.Mass - chromPeakFeatures[i].PeakFeature.Mass) <=
                        massStep * 0.5) {

                        var isOverlaped = isOverlapedChecker(parentPeakAreaBeanList[j], chromPeakFeatures[i]);
                        if (!isOverlaped) continue;
                        var hwhm = ((parentPeakAreaBeanList[j].PeakFeature.ChromXsRight.Value - parentPeakAreaBeanList[j].PeakFeature.ChromXsLeft.Value) +
                            (chromPeakFeatures[i].PeakFeature.ChromXsRight.Value - chromPeakFeatures[i].PeakFeature.ChromXsLeft.Value)) * 0.25;

                        var tolerance = Math.Min(hwhm, 0.03);
                        if (Math.Abs(parentPeakAreaBeanList[j].ChromXs.Value - chromPeakFeatures[i].ChromXs.Value) <= tolerance) {
                            if (chromPeakFeatures[i].PeakFeature.PeakHeightTop > parentPeakAreaBeanList[j].PeakFeature.PeakHeightTop) {
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
                if (peak1.PeakFeature.ChromXsLeft.Value < peak2.ChromXs.Value) return true;
            }
            else {
                if (peak2.PeakFeature.ChromXsLeft.Value < peak1.ChromXs.Value) return true;
            }
            return false;
        }

        private List<ChromatogramPeakFeature> GetCombinedChromPeakFeatures(IReadOnlyList<List<ChromatogramPeakFeature>> chromPeakFeaturesList) {
            var combinedFeatures = new List<ChromatogramPeakFeature>();
            for (int i = 0; i < chromPeakFeaturesList.Count; i++) {
                if (chromPeakFeaturesList[i] is null) {
                    continue;
                }
                combinedFeatures.AddRange(chromPeakFeaturesList[i]);
            }
            return combinedFeatures;
        }

        public List<ChromatogramPeakFeature> GetRecalculatedChromPeakFeaturesByMs1MsTolerance(List<ChromatogramPeakFeature> chromPeakFeatures, RawSpectra rawSpectra) {
            if (chromPeakFeatures is not { Count: > 0}) {
                return chromPeakFeatures;
            }
            // var spectrumList = param.MachineCategory == MachineCategory.LCIMMS ? rawObj.AccumulatedSpectrumList : rawObj.SpectrumList;
            var recalculatedPeakspots = new List<ChromatogramPeakFeature>();
            var minDatapoint = 3;
            chromPeakFeatures = chromPeakFeatures.OrderBy(p => p.PeakFeature.Mass).ToList();
            IReadOnlyList<IChromatogramPeakFeature> peaks = chromPeakFeatures;
            var mzs = peaks.Select(p => p.Mass);
            var range = peaks.Aggregate((ChromatogramRange)null, (acc, p) => ChromatogramRange.FromTimes(p.ChromXsLeft.GetRepresentativeXAxis().Add(-p.PeakWidth() * .5), p.ChromXsRight.GetRepresentativeXAxis().Add(p.PeakWidth() * .5)).Union(acc));
            foreach (var (chromatogram, peakFeature) in rawSpectra.GetMS1ExtractedChromatograms(mzs, _parameter.CentroidMs1Tolerance, range).ZipInternal(chromPeakFeatures)) {
                IChromatogramPeakFeature peak = peakFeature;
                //get EIC chromatogram
                var peakWidth = peak.PeakWidth();
                var peakWidthMargin = peakWidth * .5;
                var chromatogramRange = new ChromatogramRange(peak.ChromXsLeft.Value - peakWidthMargin, peak.ChromXsRight.Value + peakWidthMargin, peak.ChromXsTop.Type, peak.ChromXsTop.Unit);
                using var trimmedChromatogram = chromatogram.GetTrimmedChromatogram(chromatogramRange.Begin, chromatogramRange.End);
                using var smoothedChromatogram = trimmedChromatogram.ChromatogramSmoothing(_parameter.SmoothingMethod, _parameter.SmoothingLevel);

                var sPeaklist = smoothedChromatogram.AsPeakArray();
                var maxID = -1;
                var maxInt = double.MinValue;
                var minRtId = -1;
                var minRtValue = double.MaxValue;
                var peakAreaAboveZero = 0.0;
                var peakAreaAboveBaseline = 0.0;
                for (int i = 0; i < sPeaklist.Count - 1; i++) {
                    if (Math.Abs(sPeaklist[i].ChromXs.Value - peak.ChromXsTop.Value) < minRtValue) {
                        minRtValue = Math.Abs(sPeaklist[i].ChromXs.Value - peak.ChromXsTop.Value);
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
                        var diff = Math.Abs(sPeaklist[i].ChromXs.Value - peak.ChromXsLeft.Value);
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
                        var diff = Math.Abs(sPeaklist[i].ChromXs.Value - peak.ChromXsRight.Value);
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

                if (sPeaklist[maxID].ChromXs.MainType == ChromXType.RT) {
                    peakAreaAboveBaseline *= 60.0;
                    peakAreaAboveZero *= 60.0;
                }
                peak.PeakAreaAboveBaseline = peakAreaAboveBaseline;
                peak.PeakAreaAboveZero = peakAreaAboveZero;

                peak.ChromXsTop = sPeaklist[maxID].ChromXs;
                peak.ChromXsLeft = sPeaklist[minLeftId].ChromXs;
                peak.ChromXsRight = sPeaklist[minRightId].ChromXs;

                peak.PeakHeightTop = sPeaklist[maxID].Intensity;
                peak.PeakHeightLeft = sPeaklist[minLeftId].Intensity;
                peak.PeakHeightRight = sPeaklist[minRightId].Intensity;

                peak.ChromScanIdTop = sPeaklist[maxID].ID;
                peak.ChromScanIdLeft = sPeaklist[minLeftId].ID;
                peak.ChromScanIdRight = sPeaklist[minRightId].ID;

                var peakHeightFromBaseline = Math.Max(sPeaklist[maxID].Intensity - sPeaklist[minLeftId].Intensity, sPeaklist[maxID].Intensity - sPeaklist[minRightId].Intensity);
                peakFeature.PeakShape.SignalToNoise = (float)(peakHeightFromBaseline / peakFeature.PeakShape.EstimatedNoise);
                peakFeature.MS1RawSpectrumIdLeft = sPeaklist[minLeftId].ID;
                peakFeature.MS1RawSpectrumIdTop = sPeaklist[maxID].ID;
                peakFeature.MS1RawSpectrumIdRight = sPeaklist[minRightId].ID;

                recalculatedPeakspots.Add(peakFeature);

                chromatogram.Dispose();
            }
            return recalculatedPeakspots;
        }

        public List<ChromatogramPeakFeature> GetOtherChromPeakFeatureProperties(List<ChromatogramPeakFeature> chromPeakFeatures) {
            chromPeakFeatures = chromPeakFeatures.OrderBy(n => n.ChromXs.Value).ThenBy(n => n.PeakFeature.Mass).ToList();

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

            chromPeakFeatures = chromPeakFeatures.OrderBy(n => n.PeakFeature.PeakHeightTop).ToList();

            if (chromPeakFeatures.Count - 1 > 0) {
                for (int i = 0; i < chromPeakFeatures.Count; i++) {
                    chromPeakFeatures[i].PeakShape.AmplitudeScoreValue = (float)((double)i / (double)(chromPeakFeatures.Count - 1));
                    chromPeakFeatures[i].PeakShape.AmplitudeOrderValue = i;
                }
            }

            return chromPeakFeatures.OrderBy(n => n.MasterPeakID).ToList();
        }
        #endregion
    }
}

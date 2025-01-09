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
        public List<ChromatogramPeakFeature> Execute3DFeatureDetection(AnalysisFileBean file, IDataProvider provider, int numThreads, CancellationToken token, ReportProgress reporter, ChromatogramRange chromatogramRange) {
            var isTargetedMode = !_parameter.CompoundListInTargetMode.IsEmptyOrNull();
            if (isTargetedMode) {
                return Execute3DFeatureDetectionTargetMode(file, provider, numThreads, token, chromatogramRange);
            }
            return Execute3DFeatureDetectionNormalMode(file, provider, numThreads, token, reporter, chromatogramRange);
        }

        public List<ChromatogramPeakFeature> Execute3DFeatureDetectionTargetMode(AnalysisFileBean file, IDataProvider provider, int numThreads, CancellationToken token, ChromatogramRange chromatogramRange) {
            if (numThreads <= 1) {
                return Execute3DFeatureDetectionTargetModeBySingleThread(file, provider, chromatogramRange);
            }
            else {
                return Execute3DFeatureDetectionTargetModeByMultiThread(file, provider, numThreads, token, chromatogramRange);
            }
        }

        public List<ChromatogramPeakFeature> Execute3DFeatureDetectionNormalMode(AnalysisFileBean file, IDataProvider provider, int numThreads, CancellationToken token, ReportProgress reporter, ChromatogramRange chromatogramRange) {
            if (numThreads <= 1) {
                return Execute3DFeatureDetectionNormalModeBySingleThread(file, provider, reporter, chromatogramRange);
            }
            else {
                return Execute3DFeatureDetectionNormalModeByMultiThread(file, provider, numThreads, token, reporter, chromatogramRange);
            }
        }

        public List<ChromatogramPeakFeature> GetCombinedChromPeakFeatures(List<List<ChromatogramPeakFeature>> featuresList, IDataProvider provider, AcquisitionType type) {
            var cmbinedFeatures = GetCombinedChromPeakFeatures(featuresList);
            cmbinedFeatures = GetRecalculatedChromPeakFeaturesByMs1MsTolerance(cmbinedFeatures, provider, type);

            // test
            cmbinedFeatures = cmbinedFeatures.OrderBy(n => n.PeakFeature.Mass).ThenBy(n => n.ChromXs.Value).ToList();
            cmbinedFeatures = GetFurtherCleanupedChromPeakFeatures(cmbinedFeatures);
            cmbinedFeatures = GetOtherChromPeakFeatureProperties(cmbinedFeatures);

            return cmbinedFeatures;
        }

        private List<ChromatogramPeakFeature> Execute3DFeatureDetectionNormalModeBySingleThread(AnalysisFileBean file, IDataProvider provider, ReportProgress reporter, ChromatogramRange chromatogramRange) {

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
                ExtractedIonChromatogram chromatogram = rawSpectra.GetMS1ExtractedChromatogram(new MzRange(focusedMass, _parameter.MassSliceWidth), chromatogramRange);
                var chromPeakFeatures = GetChromatogramPeakFeatures_Temp2(provider, detector, chromatogram, file.AcquisitionType);
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
            return GetCombinedChromPeakFeatures(chromPeakFeaturesList, provider, file.AcquisitionType);
        }

        private List<ChromatogramPeakFeature> Execute3DFeatureDetectionNormalModeByMultiThread(AnalysisFileBean file, IDataProvider provider, int numThreads, CancellationToken token, ReportProgress reporter, ChromatogramRange chromatogramRange) {
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
                    tasks[i] = ConsumeChromatogramAsync(bc, provider, detector, chromPeakFeaturesArray, file.AcquisitionType, () => reporter?.Report(Interlocked.Increment(ref counter), targetMasses.Count), token);
                }
                Task.WaitAll(tasks);
            }

            // finalization
            return FinalizePeakSpottingResult(chromPeakFeaturesArray, massStep, provider, file.AcquisitionType);
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

        private Task ConsumeChromatogramAsync(BlockingCollection<(ExtractedIonChromatogram, int)> bc, IDataProvider provider, PeakDetection detector, List<ChromatogramPeakFeature>[] chromPeakFeaturesArray, AcquisitionType type, Action report, CancellationToken token) {
            return Task.Run(() =>
            {
                foreach (var (chromatogram, index) in bc.GetConsumingEnumerable(token)) {
                    using (chromatogram) {
                        chromPeakFeaturesArray[index] = GetChromatogramPeakFeatures_Temp2(provider, detector, chromatogram, type);
                        report?.Invoke();
                    }
                }
            });
        }

        public List<ChromatogramPeakFeature> FinalizePeakSpottingResult(List<ChromatogramPeakFeature>[] chromPeakFeaturesArray, float massStep, IDataProvider provider, AcquisitionType type) {
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
            return GetCombinedChromPeakFeatures(chromPeakFeaturesList, provider, type);
        }

        public List<double> GetFocusedMassList(float startMass, float endMass, float massStep) {
            var massList = new List<double>();
            var focusedMass = startMass;
            while (focusedMass < endMass) {
                massList.Add(focusedMass);
                focusedMass += massStep;
            }
            return massList;
        }

        public List<ChromatogramPeakFeature> Execute3DFeatureDetectionTargetModeBySingleThread(AnalysisFileBean file, IDataProvider provider, ChromatogramRange chromatogramRange) {
            return Execute3DFeatureDetectionTargetMode(file, provider, _parameter.CompoundListInTargetMode, chromatogramRange);
        }

        public List<ChromatogramPeakFeature> Execute3DFeatureDetectionTargetMode(AnalysisFileBean file, IDataProvider provider, List<MoleculeMsReference> targetedScans, ChromatogramRange chromatogramRange) {
            var chromPeakFeaturesList = new List<List<ChromatogramPeakFeature>>();
            if (targetedScans.IsEmptyOrNull()) return null;
            var rawSpectra = new RawSpectra(provider, _parameter.IonMode, file.AcquisitionType);
            foreach (var targetComp in targetedScans) {
                var chromPeakFeatures = GetChromatogramPeakFeatures(rawSpectra, provider, (float)targetComp.PrecursorMz, chromatogramRange);
                if (!chromPeakFeatures.IsEmptyOrNull())
                    chromPeakFeaturesList.Add(chromPeakFeatures);
            }

            return GetCombinedChromPeakFeatures(chromPeakFeaturesList, provider, file.AcquisitionType);
        }

        public List<ChromatogramPeakFeature> Execute3DFeatureDetectionTargetModeByMultiThread(AnalysisFileBean file, IDataProvider provider, int numThreads, CancellationToken token, ChromatogramRange chromatogramRange) {
            var targetedScans = _parameter.CompoundListInTargetMode;
            return Execute3DFeatureDetectionTargetModeByMultiThread(file, provider, targetedScans, numThreads, token, chromatogramRange);
        }

        public List<ChromatogramPeakFeature> Execute3DFeatureDetectionTargetModeByMultiThread(AnalysisFileBean file, IDataProvider provider, List<MoleculeMsReference> targetedScans, int numThreads, CancellationToken token, ChromatogramRange chromatogramRange) {
            var spectrumList = provider.LoadMs1Spectrums();
            if (targetedScans.IsEmptyOrNull()) return null;
            var rawSpectra = new RawSpectra(provider, _parameter.IonMode, file.AcquisitionType);
            var chromPeakFeaturesList = targetedScans
                .AsParallel()
                .AsOrdered()
                .WithCancellation(token)
                .WithDegreeOfParallelism(numThreads)
                .Select(targetedScan => GetChromatogramPeakFeatures(rawSpectra, provider, (float)targetedScan.PrecursorMz, chromatogramRange))
                .Where(features => !features.IsEmptyOrNull())
                .ToList();
            return GetCombinedChromPeakFeatures(chromPeakFeaturesList, provider, file.AcquisitionType);
        }

        public List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(RawSpectra rawSpectra, IDataProvider provider, float focusedMass, ChromatogramRange chromatogramRange) {

            //get EIC chromatogram
            var chromatogram = rawSpectra.GetMS1ExtractedChromatogram(new MzRange(focusedMass, _parameter.MassSliceWidth), chromatogramRange);
            if (chromatogram.IsEmpty) return null;

            //get peak detection result
            var chromPeakFeatures = GetChromatogramPeakFeatures(chromatogram);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;
            var peaks = ((Chromatogram)chromatogram).AsPeakArray();
            SetRawDataAccessID2ChromatogramPeakFeatures(chromPeakFeatures, provider, peaks, rawSpectra.AcquisitionType);

            //filtering out noise peaks considering smoothing effects and baseline effects
            chromPeakFeatures = GetBackgroundSubtractedPeaks(chromPeakFeatures, peaks);
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;

            return chromPeakFeatures;
        }

        public ExtractedIonChromatogram GetChromatogram(RawSpectra rawSpectra, float focusedMass, ChromatogramRange chromatogramRange) {
            //get EIC chromatogram
            return rawSpectra.GetMS1ExtractedChromatogram(new MzRange(focusedMass, _parameter.MassSliceWidth), chromatogramRange);
        }

        public List<ChromatogramPeakFeature>? GetChromatogramPeakFeatures_Temp2(IDataProvider provider, PeakDetection detector, ExtractedIonChromatogram chromatogram, AcquisitionType type) {
            if (chromatogram.IsEmpty) return null;

            //get peak detection result
            var chromPeakFeatures = GetChromatogramPeakFeatures(chromatogram, detector);
            if (chromPeakFeatures is null || chromPeakFeatures.Count == 0) return null;
            SetRawDataAccessID2ChromatogramPeakFeatures(chromPeakFeatures, provider, type);

            //filtering out noise peaks considering smoothing effects and baseline effects
            chromPeakFeatures = GetBackgroundSubtractedPeaks(chromPeakFeatures, chromatogram);
            if (chromPeakFeatures is null || chromPeakFeatures.Count == 0) return null;

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
                var chromatogram = rawSpectra.GetDriftChromatogramByScanRtMz(peakSpot.MS1RawSpectrumIdTop, (float)peakSpot.ChromXs.Value, accumulatedRtRange, (float)peakSpot.PeakFeature.Mass, _parameter.CentroidMs1Tolerance);
                if (chromatogram.IsEmpty) continue;
                var peaksOnDriftTime = GetPeakAreaBeanListOnDriftTimeAxis(chromatogram, peakSpot, rawSpectra, accumulatedRtRange);
                if (peaksOnDriftTime == null || peaksOnDriftTime.Count == 0) continue;
                peakSpot.DriftChromFeatures = peaksOnDriftTime;
                newSpots.Add(peakSpot);
            }
            return newSpots;
        }

        private List<ChromatogramPeakFeature> GetPeakAreaBeanListOnDriftTimeAxis(Chromatogram chromatogram, ChromatogramPeakFeature rtPeakFeature, RawSpectra rawSpectra, double accumulatedRtRange) {

            var smoothedPeaklist = chromatogram.ChromatogramSmoothing(_parameter.SmoothingMethod, _parameter.SmoothingLevel).AsPeakArray();
            var detectedPeaks = PeakDetection.PeakDetectionVS1(smoothedPeaklist, _parameter.MinimumDatapoints, _parameter.MinimumAmplitude * 0.25) ?? new List<PeakDetectionResult>(0);
            var maxIntensityAtPeaks = detectedPeaks.DefaultIfEmpty().Max(n => n?.IntensityAtPeakTop) ?? 0d;

            var peaks = new List<ChromatogramPeakFeature>();
            var counter = 0;
            foreach (var result in detectedPeaks) {
                if (result.IsWeakCompareTo(maxIntensityAtPeaks)) {
                    continue;
                }

                var driftFeature = BuildDriftPeakFeature(result, rtPeakFeature, counter, chromatogram, rawSpectra, accumulatedRtRange);
                peaks.Add(driftFeature);
                counter++;
            }
            peaks = GetBackgroundSubtractedPeaks(peaks, chromatogram);
            if (peaks == null || peaks.Count == 0) return null;
            return peaks;
        }

        private ChromatogramPeakFeature BuildDriftPeakFeature(PeakDetectionResult result, ChromatogramPeakFeature rtPeakFeature, int peakId, Chromatogram chromatogram, RawSpectra rawSpectra, double accumulatedRtRange) {
            var driftFeature = ChromatogramPeakFeature.FromPeakDetectionResult(result, chromatogram, rtPeakFeature.PeakFeature.Mass);
            driftFeature.PeakID = peakId;
            driftFeature.ParentPeakID = rtPeakFeature.PeakID;
            driftFeature.IonMode = rtPeakFeature.IonMode;
            driftFeature.PeakFeature.ChromXsLeft.RT = new RetentionTime(rtPeakFeature.PeakFeature.ChromXsTop.RT.Value - accumulatedRtRange * 0.5, unit: driftFeature.PeakFeature.ChromXsLeft.RT.Unit);
            driftFeature.PeakFeature.ChromXsTop.RT = new RetentionTime(rtPeakFeature.PeakFeature.ChromXsTop.RT.Value, unit: driftFeature.PeakFeature.ChromXsTop.RT.Unit);
            driftFeature.PeakFeature.ChromXsRight.RT = new RetentionTime(rtPeakFeature.PeakFeature.ChromXsTop.RT.Value + accumulatedRtRange * 0.5, unit: driftFeature.PeakFeature.ChromXsRight.RT.Unit);
            var ms2Tol = MolecularFormulaUtility.FixMassTolerance(_parameter.CentroidMs2Tolerance, rtPeakFeature.PeakFeature.Mass);
            var spectra = rawSpectra.GetPeakMs2Spectra(rtPeakFeature, ms2Tol, rawSpectra.AcquisitionType, driftFeature.ChromXs.Drift);
            driftFeature.SetMs2SpectrumId(spectra);
            return driftFeature;
        }
        #endregion

        #region peak detection utilities
        public List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(Chromatogram chromatogram) {
            var smoothedPeaklist = chromatogram.ChromatogramSmoothing(_parameter.SmoothingMethod, _parameter.SmoothingLevel).AsPeakArray();
            //var detectedPeaks = PeakDetection.GetDetectedPeakInformationCollectionFromDifferentialBasedPeakDetectionAlgorithm(analysisParametersBean.MinimumDatapoints, analysisParametersBean.MinimumAmplitude, analysisParametersBean.AmplitudeNoiseFactor, analysisParametersBean.SlopeNoiseFactor, analysisParametersBean.PeaktopNoiseFactor, smoothedPeaklist);
            var minDatapoints = _parameter.MinimumDatapoints;
            var minAmps = _parameter.MinimumAmplitude;
            var detectedPeaks = PeakDetection.PeakDetectionVS1(smoothedPeaklist, minDatapoints, minAmps);
            if (detectedPeaks == null || detectedPeaks.Count == 0) return null;

            var chromPeakFeatures = new List<ChromatogramPeakFeature>();

            var peaks = chromatogram.AsPeakArray();
            foreach (var result in detectedPeaks) {
                if (result.IntensityAtPeakTop <= 0) continue;
                var mass = peaks[result.ScanNumAtPeakTop].Mass;

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

        public List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(ExtractedIonChromatogram chromatogram, PeakDetection detector) {
            using ExtractedIonChromatogram smoothedChromatogram = chromatogram.ChromatogramSmoothing(_parameter.SmoothingMethod, _parameter.SmoothingLevel);
            var detectedPeaks = detector.PeakDetectionVS1(smoothedChromatogram);
            if (detectedPeaks == null || detectedPeaks.Count == 0) return null;

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
        /// <param name="spectrumList"></param>
        /// <param name="peaklist"></param>
        /// <param name="param"></param>
        public void SetRawDataAccessID2ChromatogramPeakFeatures(List<ChromatogramPeakFeature> chromPeakFeatures, IDataProvider provider, IReadOnlyList<IChromatogramPeak> peaklist, AcquisitionType type) {
            foreach (var feature in chromPeakFeatures) {
                SetRawDataAccessID2ChromatogramPeakFeature(feature, provider, peaklist, type);
            }
        }

        public void SetRawDataAccessID2ChromatogramPeakFeatures(List<ChromatogramPeakFeature> chromPeakFeatures, IDataProvider provider, AcquisitionType type) {
            foreach (var feature in chromPeakFeatures) {
                SetMS2RawSpectrumIDs2ChromatogramPeakFeature(feature, provider, feature.PeakFeature.ChromXsLeft.RT.Value, feature.PeakFeature.ChromXsTop.RT.Value, feature.PeakFeature.ChromXsRight.RT.Value, type);
            }
        }

        public void SetRawDataAccessID2ChromatogramPeakFeature(ChromatogramPeakFeature feature, IDataProvider provider, IReadOnlyList<IChromatogramPeak> peaklist, AcquisitionType type) {
            var chromLeftID = feature.PeakFeature.ChromScanIdLeft;
            var chromTopID = feature.PeakFeature.ChromScanIdTop;
            var chromRightID = feature.PeakFeature.ChromScanIdRight;

            feature.MS1RawSpectrumIdLeft = peaklist[chromLeftID].ID;
            feature.MS1RawSpectrumIdTop = peaklist[chromTopID].ID;
            feature.MS1RawSpectrumIdRight = peaklist[chromRightID].ID;

            SetMS2RawSpectrumIDs2ChromatogramPeakFeature(feature, provider, peaklist[chromLeftID].ChromXs.RT.Value, peaklist[chromTopID].ChromXs.RT.Value, peaklist[chromRightID].ChromXs.RT.Value, type);
        }

        public void SetMS2RawSpectrumIDs2ChromatogramPeakFeature(ChromatogramPeakFeature feature, IDataProvider provider, double scanBeginTime, double scanTopTime, double scanEndTime, AcquisitionType type) {
            var mass = feature.PeakFeature.Mass;
            var minDiff = double.MaxValue;

            var scanPolarity = _parameter.IonMode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;
            var ms2Tol = MolecularFormulaUtility.FixMassTolerance(_parameter.CentroidMs2Tolerance, mass);

            var ce2MinDiff = new Dictionary<double, double>(); // ce to diff
            var spectra = provider.LoadMs2SpectraWithRtRangeAsync(scanBeginTime, scanEndTime, default).Result;
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

        public List<ChromatogramPeakFeature> GetCombinedChromPeakFeatures(List<List<ChromatogramPeakFeature>> chromPeakFeaturesList) {
            var combinedFeatures = new List<ChromatogramPeakFeature>();

            for (int i = 0; i < chromPeakFeaturesList.Count; i++) {
                if (chromPeakFeaturesList[i].Count == 0) continue;
                for (int j = 0; j < chromPeakFeaturesList[i].Count; j++)
                    combinedFeatures.Add(chromPeakFeaturesList[i][j]);
            }

            return combinedFeatures;
        }

        public List<ChromatogramPeakFeature> GetRecalculatedChromPeakFeaturesByMs1MsTolerance(List<ChromatogramPeakFeature> chromPeakFeatures, IDataProvider provider, AcquisitionType type) {
            if (chromPeakFeatures is not { Count: > 0}) {
                return chromPeakFeatures;
            }
            // var spectrumList = param.MachineCategory == MachineCategory.LCIMMS ? rawObj.AccumulatedSpectrumList : rawObj.SpectrumList;
            var recalculatedPeakspots = new List<ChromatogramPeakFeature>();
            var minDatapoint = 3;
            // var counter = 0;
            var rawSpectra = new RawSpectra(provider, _parameter.IonMode, type);
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

                var sPeaklist = trimmedChromatogram.ChromatogramSmoothing(_parameter.SmoothingMethod, _parameter.SmoothingLevel).AsPeakArray();
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

                var peakArray = chromatogram.AsPeakArray();
                if (peakFeature.DriftChromFeatures is null) { // meaning not ion mobility data
                    SetMS2RawSpectrumIDs2ChromatogramPeakFeature(peakFeature, provider, peakArray[minLeftId].Time, peakArray[maxID].Time, peakArray[minRightId].Time, type);
                }

                peakFeature.MS1RawSpectrumIdLeft = peakArray[minLeftId].Id;
                peakFeature.MS1RawSpectrumIdTop = peakArray[maxID].Id;
                peakFeature.MS1RawSpectrumIdRight = peakArray[minRightId].Id;

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

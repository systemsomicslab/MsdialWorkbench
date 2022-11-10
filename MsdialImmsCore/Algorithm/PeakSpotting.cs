using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialImmsCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialImmsCore.Algorithm
{
    public sealed class PeakSpotting
    {
        private readonly MsdialImmsParameter _parameter;

        public PeakSpotting(MsdialImmsParameter parameter) {
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        }

        public ChromatogramPeakFeatureCollection Run(
            IDataProvider provider,
            double initialProgress,
            double progressMax,
            Action<int> reportAction = null) {

            if (_parameter.AdvancedProcessOptionBaseParam.IsTargetMode) {
                return Execute3DFeatureDetectionTargetMode(provider, _parameter.DriftTimeBegin, _parameter.DriftTimeEnd);
            }
            return Execute3DFeatureDetectionNormalMode(provider, _parameter.DriftTimeBegin, _parameter.DriftTimeEnd, initialProgress, progressMax, reportAction);
        }

        private ChromatogramPeakFeatureCollection Execute3DFeatureDetectionNormalMode(
            IDataProvider provider,
            float chromBegin,
            float chromEnd, double initialProgress,
            double progressMax, Action<int> reportAction) {

            var (mzMin, mzMax) = provider.GetMs1Range(_parameter.ProjectParam.IonMode);
            var startMass = Math.Max(mzMin, _parameter.PeakPickBaseParam.MassRangeBegin);
            var endMass = Math.Min(mzMax, _parameter.PeakPickBaseParam.MassRangeEnd);
            var massStep = _parameter.ChromDecBaseParam.AccuracyType == AccuracyType.IsNominal ? 1f : _parameter.PeakPickBaseParam.MassSliceWidth;
            var rawSpectra = new RawSpectra(provider, _parameter.ProjectParam.IonMode, _parameter.ProjectParam.AcquisitionType);
            var chromatogramRange = new ChromatogramRange(chromBegin, chromEnd, ChromXType.Drift, ChromXUnit.Msec);

            var chromPeakFeaturesList = new List<List<ChromatogramPeakFeature>>();
            for (var focusedMass = startMass; focusedMass < endMass; focusedMass += massStep) {
                ReportProgress.Show(initialProgress, progressMax, focusedMass, endMass, reportAction);

                var chromPeakFeatures = GetChromatogramPeakFeatures(rawSpectra, provider, focusedMass, chromatogramRange);
                if (chromPeakFeatures.IsEmptyOrNull()) {
                    continue;
                }

                var removedPeakFeatures = RemovePeakAreaBeanRedundancy(chromPeakFeatures, chromPeakFeaturesList.LastOrDefault(), massStep);
                if (removedPeakFeatures.IsEmptyOrNull()) {
                    continue;
                }

                chromPeakFeaturesList.Add(removedPeakFeatures);
            }

            var combinedFeatures = GetRecalculatedChromPeakFeaturesByMs1MsTolerance(chromPeakFeaturesList.SelectMany(features => features).ToList(), provider);

            var collection = new ChromatogramPeakFeatureCollection(combinedFeatures.OrderBy(item => item.ChromXs.Value).ThenBy(item => item.PeakFeature.Mass).ToList());
            collection.ResetAmplitudeScore();
            collection.ResetPeakID();

            return collection;
        }

        private ChromatogramPeakFeatureCollection Execute3DFeatureDetectionTargetMode(IDataProvider provider, float chromBegin, float chromEnd) {
            if (!_parameter.AdvancedProcessOptionBaseParam.IsTargetMode) {
                return new ChromatogramPeakFeatureCollection(new List<ChromatogramPeakFeature>());
            }

            var chromatogramRange = new ChromatogramRange(chromBegin, chromEnd, ChromXType.Drift, ChromXUnit.Msec);
            var rawSpectra = new RawSpectra(provider, _parameter.ProjectParam.IonMode, _parameter.ProjectParam.AcquisitionType);

            var chromPeakFeaturesList = new List<List<ChromatogramPeakFeature>>();
            foreach (var targetComp in _parameter.AdvancedProcessOptionBaseParam.CompoundListInTargetMode) {
                var chromPeakFeatures = GetChromatogramPeakFeatures(rawSpectra, provider, (float)targetComp.PrecursorMz, chromatogramRange);
                if (!chromPeakFeatures.IsEmptyOrNull()) {
                    chromPeakFeaturesList.Add(chromPeakFeatures);
                }
            }

            var combinedFeatures = chromPeakFeaturesList.SelectMany(features => features).ToList();
            var recalculatedPeaks = GetRecalculatedChromPeakFeaturesByMs1MsTolerance(combinedFeatures, provider);

            var collection = new ChromatogramPeakFeatureCollection(recalculatedPeaks.OrderBy(item => item.ChromXs.Value).ThenBy(item => item.PeakFeature.Mass).ToList());
            collection.ResetAmplitudeScore();
            collection.ResetPeakID();

            return collection;
        }

        private List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(RawSpectra rawSpectra, IDataProvider provider, float focusedMass, ChromatogramRange chromatogramRange) {

            var chromatogram = rawSpectra.GetMs1ExtractedChromatogram_temp2(focusedMass, _parameter.PeakPickBaseParam.MassSliceWidth, chromatogramRange);
            if (chromatogram.IsEmpty) {
                return new List<ChromatogramPeakFeature>();
            }

            var chromPeakFeatures = GetChromatogramPeakFeatures(chromatogram);
            if (chromPeakFeatures.IsEmptyOrNull()) {
                return new List<ChromatogramPeakFeature>();
            }

            SetRawDataAccessID2ChromatogramPeakFeatures(chromPeakFeatures, provider, chromatogram.Peaks);

            var subtractedFeatures = GetBackgroundSubtractedPeaks(chromPeakFeatures, chromatogram);
            if (subtractedFeatures.IsEmptyOrNull()) {
                return new List<ChromatogramPeakFeature>();
            }

            return subtractedFeatures;
        }

        private List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(Chromatogram_temp2 chromatogram) {
            var smoothedPeaklist = chromatogram.Smoothing(_parameter.PeakPickBaseParam.SmoothingMethod, _parameter.PeakPickBaseParam.SmoothingLevel);
            var detectedPeaks = PeakDetection.PeakDetectionVS1(smoothedPeaklist, _parameter.PeakPickBaseParam.MinimumDatapoints, _parameter.PeakPickBaseParam.MinimumAmplitude);
            if (detectedPeaks.IsEmptyOrNull()) {
                return new List<ChromatogramPeakFeature>();
            }

            var chromPeakFeatures = new List<ChromatogramPeakFeature>();
            foreach (var result in detectedPeaks) {
                if (result.IntensityAtPeakTop <= 0) {
                    continue;
                }

                var mass = chromatogram.Peaks[result.ScanNumAtPeakTop].Mz;

                //option
                //Users can prepare their-own 'exclusion mass' list to exclude unwanted peak features
                if (_parameter.PeakPickBaseParam.ShouldExclude(mass)) {
                    continue;
                }

                var chromPeakFeature = DataAccess.GetChromatogramPeakFeature(result, ChromXType.Drift, ChromXUnit.Msec, mass, _parameter.ProjectParam.IonMode);
                chromPeakFeatures.Add(chromPeakFeature);
            }
            return chromPeakFeatures;
        }

        private void SetRawDataAccessID2ChromatogramPeakFeatures(List<ChromatogramPeakFeature> chromPeakFeatures, IDataProvider provider, IReadOnlyList<ValuePeak> peaklist) {
            var collection = new ChromatogramPeakFeatureCollection(chromPeakFeatures);
            collection.SetRawMs1Id(peaklist);
            foreach (var feature in collection.Items) {
                SetMs2RawSpectrumIDs2ChromatogramPeakFeature(feature, provider);
            }
        }

        private void SetMs2RawSpectrumIDs2ChromatogramPeakFeature(ChromatogramPeakFeature feature, IDataProvider provider) {
            var spectrumList = provider.LoadMsNSpectrums(level: 2);
            if (spectrumList.IsEmptyOrNull()) {
                return;
            }

            var scanPolarity = _parameter.ProjectParam.IonMode.ToPolarity();
            var peakFeature = feature.PeakFeature;
            var mass = peakFeature.Mass;
            var ms2Tol = MolecularFormulaUtility.FixMassTolerance(_parameter.PeakPickBaseParam.CentroidMs2Tolerance, mass);
            var dt = peakFeature.ChromXsTop.Drift;
            var dtStart = peakFeature.ChromXsLeft.Drift;
            var dtEnd = peakFeature.ChromXsRight.Drift;
            var specs = new List<RawSpectrum>();
            // TODO: slow. improve search algorithm.
            foreach (var spec in spectrumList) {
                if (spec.Precursor != null && spec.ScanPolarity == scanPolarity) {

                    var IsMassInWindow = spec.Precursor.ContainsMz(mass, ms2Tol, _parameter.ProjectParam.AcquisitionType);
                    var IsDtInWindow = spec.Precursor.ContainsDriftTime(dt) // used for diapasef
                        || (spec.Precursor.IsNotDiapasefData && spec.IsInDriftTimeRange(dtStart, dtEnd)); // normal dia

                    if (IsMassInWindow && IsDtInWindow) {
                        specs.Add(spec);
                    }
                }
            }

            var representatives = specs
                .GroupBy(spec => Math.Round(spec.CollisionEnergy, 2))  // grouping by ce
                .Select(group => group.Argmax(spec => spec.TotalIonCurrent)) // choose largest ion current, for each ce
                .ToList();

            if (representatives.Any()) {
                feature.MS2RawSpectrumID2CE = representatives.ToDictionary(spec => spec.Index, spec => spec.CollisionEnergy);
                feature.MS2RawSpectrumID = representatives.Argmax(spec => spec.TotalIonCurrent).Index;
            }
        }

        private List<ChromatogramPeakFeature> GetRecalculatedChromPeakFeaturesByMs1MsTolerance(List<ChromatogramPeakFeature> chromPeakFeatures, IDataProvider provider) {
            var recalculatedPeakspots = new List<ChromatogramPeakFeature>();
            var minDatapoint = 3;
            var rawSpectra = new RawSpectra(provider, _parameter.ProjectParam.IonMode, _parameter.ProjectParam.AcquisitionType);
            foreach (var spot in chromPeakFeatures) {
                //get EIC chromatogram
                var peakFeature = spot.PeakFeature;
                var peakRange = new ChromatogramRange(peakFeature, ChromXType.Drift, ChromXUnit.Msec);
                var chromatogram = rawSpectra.GetMs1ExtractedChromatogram(peakFeature.Mass, _parameter.PeakPickBaseParam.CentroidMs1Tolerance, peakRange.Extend(0.5d));
                var smoothedChromatogram = chromatogram.SmoothedChromatogram(_parameter.PeakPickBaseParam.SmoothingMethod, _parameter.PeakPickBaseParam.SmoothingLevel);
                var peakOfChromatogram = smoothedChromatogram.FindPeak(minDatapoint, peakRange.Width, spot.PeakFeature);
                if (!peakOfChromatogram.IsValid(_parameter.PeakPickBaseParam.MinimumAmplitude)) {
                    continue;
                }

                spot.SetPeakProperties(peakOfChromatogram);
                if (!spot.IsMultiLayeredData()) {
                    SetMs2RawSpectrumIDs2ChromatogramPeakFeature(spot, provider);
                }
                recalculatedPeakspots.Add(spot);
            }
            return recalculatedPeakspots;
        }

        private static List<ChromatogramPeakFeature> GetBackgroundSubtractedPeaks(List<ChromatogramPeakFeature> chromPeakFeatures, Chromatogram_temp2 chromatogram) {
            const int counterThreshold = 4;

            var sPeakAreaList = new List<ChromatogramPeakFeature>();
            foreach (var feature in chromPeakFeatures) {
                var peakFeature = feature.PeakFeature;
                var peakTop = peakFeature.ChromScanIdTop;
                var peakLeft = peakFeature.ChromScanIdLeft;
                var peakRight = peakFeature.ChromScanIdRight;

                if (!chromatogram.IsValidPeakTop(peakTop)) {
                    continue;
                }

                var trackingNumber = Math.Min(10 * (peakRight - peakLeft), 50);
                var ampDiff = Math.Max(peakFeature.PeakHeightTop - peakFeature.PeakHeightLeft, peakFeature.PeakHeightTop - peakFeature.PeakHeightRight);

                var counter = 0;
                counter += CountLargeIntensityChange(chromatogram, ampDiff, peakLeft - trackingNumber, peakLeft);
                counter += CountLargeIntensityChange(chromatogram, ampDiff, peakRight, peakRight + trackingNumber);

                if (counter < counterThreshold) {
                    sPeakAreaList.Add(feature);
                }
            }
            return sPeakAreaList;
        }

        private static int CountLargeIntensityChange(Chromatogram_temp2 chromatogram, double threshold, int left, int right) {
            var leftBound = Math.Max(left, 1);
            var rightBound = Math.Min(right, chromatogram.Peaks.Count - 2);

            var counter = 0;
            double? spikeMax = null, spikeMin = null;
            for (int i = leftBound; i <= rightBound; i++) {

                if (chromatogram.IsPeakTop(i)) {
                    spikeMax = chromatogram.Peaks[i].Intensity;
                }
                else if (chromatogram.IsBottom(i)) {
                    spikeMin = chromatogram.Peaks[i].Intensity;
                }

                if (spikeMax.HasValue && spikeMin.HasValue) {
                    var noise = 0.5 * Math.Abs(spikeMax.Value - spikeMin.Value);
                    if (noise * 3 > threshold) {
                        counter++;
                    }

                    spikeMax = null; spikeMin = null;
                }
            }

            return counter;
        }

        private static List<ChromatogramPeakFeature> RemovePeakAreaBeanRedundancy(List<ChromatogramPeakFeature> chromPeakFeatures, List<ChromatogramPeakFeature> parentPeakFeatures, float massStep) {

            if (chromPeakFeatures is null) {
                return new List<ChromatogramPeakFeature>();
            }

            if (parentPeakFeatures is null) {
                return chromPeakFeatures;
            }

            for (int i = 0; i < chromPeakFeatures.Count; i++) {
                for (int j = 0; j < parentPeakFeatures.Count; j++) {

                    var pearentPeakFeature = parentPeakFeatures[j].PeakFeature;
                    var chromPeakFeature = chromPeakFeatures[i].PeakFeature;
                    if (Math.Abs(pearentPeakFeature.Mass - chromPeakFeature.Mass) > massStep * 0.5) {
                        continue;
                    }

                    if (!IsOverlaped(pearentPeakFeature, chromPeakFeature)) {
                        continue;
                    }

                    var hwhm = (pearentPeakFeature.ChromXsRight.Value - pearentPeakFeature.ChromXsLeft.Value +
                        (chromPeakFeature.ChromXsRight.Value - chromPeakFeature.ChromXsLeft.Value)) * 0.25;
                    var tolerance = Math.Min(hwhm, 0.03);

                    if (Math.Abs(pearentPeakFeature.ChromXsTop.Value - chromPeakFeature.ChromXsTop.Value) <= tolerance) {
                        if (pearentPeakFeature.PeakHeightTop < chromPeakFeature.PeakHeightTop) {
                            // TODO: should not remove from list.
                            parentPeakFeatures.RemoveAt(j);
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
                if (parentPeakFeatures == null || parentPeakFeatures.Count == 0) return chromPeakFeatures;
                if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;
            }
            return chromPeakFeatures;
        }

        private static bool IsOverlaped(IChromatogramPeakFeature peak1, IChromatogramPeakFeature peak2) {
            if (peak1.ChromXsTop.Value > peak2.ChromXsTop.Value) {
                if (peak1.ChromXsLeft.Value < peak2.ChromXsTop.Value) {
                    return true;
                }
            }
            else {
                if (peak2.ChromXsLeft.Value < peak1.ChromXsTop.Value) {
                    return true;
                }
            }
            return false;
        }

        private static void SetPeakProperty(ChromatogramPeakFeature chromPeakFeature, PeakOfChromatogram peakOfChromatogram) {
            var peak = chromPeakFeature.PeakFeature;
            var peakTop = peakOfChromatogram.Top;
            var peakLeft = peakOfChromatogram.Left;
            var peakRight = peakOfChromatogram.Right;

            // calculating peak area 
            peak.PeakAreaAboveZero = peakOfChromatogram.CalculateArea();
            peak.PeakAreaAboveBaseline = peak.PeakAreaAboveZero - peakOfChromatogram.CalculateBaseLineArea();

            peak.ChromXsLeft = peakLeft.ChromXs;
            peak.ChromXsTop = peakTop.ChromXs;
            peak.ChromXsRight = peakRight.ChromXs;

            peak.PeakHeightLeft = peakLeft.Intensity;
            peak.PeakHeightTop = peakTop.Intensity;
            peak.PeakHeightRight = peakRight.Intensity;

            peak.ChromScanIdLeft = peakLeft.ID;
            peak.ChromScanIdTop = peakTop.ID;
            peak.ChromScanIdRight = peakRight.ID;

            chromPeakFeature.PeakShape.SignalToNoise = (float)(peakOfChromatogram.CalculatePeakAmplitude() / chromPeakFeature.PeakShape.EstimatedNoise);

            chromPeakFeature.MS1RawSpectrumIdTop = peak.ChromScanIdTop;
            chromPeakFeature.MS1RawSpectrumIdLeft = peak.ChromScanIdLeft;
            chromPeakFeature.MS1RawSpectrumIdRight = peak.ChromScanIdRight;
        }
    }
}

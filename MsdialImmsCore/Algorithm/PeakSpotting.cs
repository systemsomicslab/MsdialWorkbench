using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Query;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialImmsCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialImmsCore.Algorithm
{
    public class PeakSpotting
    {

        private readonly double InitialProgress;
        private readonly double ProgressMax;

        public PeakSpotting(double initialProgress, double progressMax) {
            InitialProgress = initialProgress;
            ProgressMax = progressMax;
        }

        public List<ChromatogramPeakFeature> Run(
            IDataProvider provider,
            MsdialImmsParameter param,
            Action<int> reportAction = null, System.Threading.CancellationToken token = default) {

            var isTargetedMode = !param.CompoundListInTargetMode.IsEmptyOrNull();
            if (isTargetedMode) {
                return Execute3DFeatureDetectionTargetMode(provider, param, param.DriftTimeBegin, param.DriftTimeEnd);
            }
            return Execute3DFeatureDetectionNormalMode(provider, param, param.DriftTimeBegin, param.DriftTimeEnd, InitialProgress, ProgressMax, reportAction, token);
        }

        private static List<ChromatogramPeakFeature> Execute3DFeatureDetectionNormalMode(
            IDataProvider provider,
            MsdialImmsParameter param,
            float chromBegin, float chromEnd,
            double initialProgress, double progressMax,
            Action<int> reportAction,
            System.Threading.CancellationToken token) {

            var ms1Spectrum = provider.LoadMs1Spectrums();

            var mzRange = DataAccess.GetMs1Range(ms1Spectrum, param.IonMode);
            var startMass = Math.Max(mzRange[0], param.MassRangeBegin);
            var endMass = Math.Min(mzRange[1], param.MassRangeEnd);
            var massStep = param.AccuracyType == AccuracyType.IsNominal ? 1f : param.MassSliceWidth;

            var chromPeakFeaturesList = new List<List<ChromatogramPeakFeature>>();
            for (var focusedMass = startMass; focusedMass < endMass; focusedMass += massStep) {
                ReportProgress.Show(initialProgress, progressMax, focusedMass, endMass, reportAction);

                var chromPeakFeatures = GetChromatogramPeakFeatures(ms1Spectrum, provider, focusedMass, param, chromBegin, chromEnd);
                if (chromPeakFeatures.IsEmptyOrNull())
                    continue;

                chromPeakFeatures = RemovePeakAreaBeanRedundancy(chromPeakFeatures, chromPeakFeaturesList.LastOrDefault(), massStep);
                if (chromPeakFeatures.IsEmptyOrNull())
                    continue;

                chromPeakFeaturesList.Add(chromPeakFeatures);
            }

            var combinedFeatures = GetCombinedChromPeakFeatures(chromPeakFeaturesList);
            combinedFeatures = GetRecalculatedChromPeakFeaturesByMs1MsTolerance(combinedFeatures, provider, param, ChromXType.Drift, ChromXUnit.Msec);
            SetAmplitudeScore(combinedFeatures);
            SetPeakID(combinedFeatures);

            return combinedFeatures.OrderBy(feature => feature.MasterPeakID).ToList();
        }

        private static List<ChromatogramPeakFeature> Execute3DFeatureDetectionTargetMode(
            IDataProvider provider, 
            MsdialImmsParameter param,
            float chromBegin, float chromEnd) {

            var ms1Spectrum = provider.LoadMs1Spectrums();
            var chromPeakFeaturesList = new List<List<ChromatogramPeakFeature>>();
            var targetedScans = param.CompoundListInTargetMode;
            if (targetedScans.IsEmptyOrNull())
                return new List<ChromatogramPeakFeature>();

            foreach (var targetComp in targetedScans) {
                var chromPeakFeatures = GetChromatogramPeakFeatures(ms1Spectrum, provider, (float)targetComp.PrecursorMz, param, chromBegin, chromEnd);
                if (!chromPeakFeatures.IsEmptyOrNull())
                    chromPeakFeaturesList.Add(chromPeakFeatures);
            }

            var combinedFeatures = GetCombinedChromPeakFeatures(chromPeakFeaturesList);
            combinedFeatures = GetRecalculatedChromPeakFeaturesByMs1MsTolerance(combinedFeatures, provider, param, ChromXType.Drift, ChromXUnit.Msec);
            SetAmplitudeScore(combinedFeatures);
            SetPeakID(combinedFeatures);

            return combinedFeatures.OrderBy(feature => feature.MasterPeakID).ToList();
        }

        private static List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(
            IReadOnlyList<RawSpectrum> spectrum,
            IDataProvider provider,
            float focusedMass,
            MsdialImmsParameter param,
            float chromBegin, float chromEnd) {

            var peaklist = DataAccess.GetMs1Peaklist(spectrum, focusedMass, param.MassSliceWidth, param.IonMode, ChromXType.Drift, ChromXUnit.Msec, chromBegin, chromEnd);
            if (peaklist.IsEmptyOrNull())
                return new List<ChromatogramPeakFeature>();

            var chromPeakFeatures = GetChromatogramPeakFeatures(peaklist, param);
            if (chromPeakFeatures.IsEmptyOrNull())
                return new List<ChromatogramPeakFeature>();
            SetRawDataAccessID2ChromatogramPeakFeatures(chromPeakFeatures, provider, peaklist, param);

            var subtractedFeatures = GetBackgroundSubtractedPeaks(chromPeakFeatures, peaklist);
            if (subtractedFeatures.IsEmptyOrNull())
                return new List<ChromatogramPeakFeature>();

            return subtractedFeatures;
        }

        private static List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(List<ChromatogramPeak> peaklist, MsdialImmsParameter param) {

            var smoothedPeaklist = DataAccess.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
            var detectedPeaks = PeakDetection.PeakDetectionVS1(smoothedPeaklist, param.MinimumDatapoints, param.MinimumAmplitude);
            if (detectedPeaks.IsEmptyOrNull())
                return new List<ChromatogramPeakFeature>();

            var chromPeakFeatures = new List<ChromatogramPeakFeature>();
            foreach (var result in detectedPeaks) {
                if (result.IntensityAtPeakTop <= 0) continue;
                var mass = peaklist[result.ScanNumAtPeakTop].Mass;

                //option
                //Users can prepare their-own 'exclusion mass' list to exclude unwanted peak features
                if (ExistsSimilarMass(mass, param.ExcludedMassList.OrEmptyIfNull())) {
                    continue;
                }

                var chromPeakFeature = DataAccess.GetChromatogramPeakFeature(result, ChromXType.Drift, ChromXUnit.Msec, mass);
                chromPeakFeatures.Add(chromPeakFeature);
            }
            return chromPeakFeatures;
        }

        private static bool ExistsSimilarMass(double mass, IEnumerable<MzSearchQuery> excludes) {
            foreach (var query in excludes) {
                if (Math.Abs(query.Mass - mass) < query.MassTolerance) {
                    return true;
                }
            }
            return false;
        }

        private static void SetRawDataAccessID2ChromatogramPeakFeatures(
            List<ChromatogramPeakFeature> chromPeakFeatures,
            IDataProvider provider,
            List<ChromatogramPeak> peaklist,
            MsdialImmsParameter param) {

            foreach (var feature in chromPeakFeatures) {
                SetRawDataAccessID2ChromatogramPeakFeature(feature, peaklist);
                SetMs2RawSpectrumIDs2ChromatogramPeakFeature(feature, provider, param);
            }
        }

        private static void SetRawDataAccessID2ChromatogramPeakFeature(
            ChromatogramPeakFeature feature,
            List<ChromatogramPeak> peaklist) {

            var chromLeftID = feature.ChromScanIdLeft;
            var chromTopID = feature.ChromScanIdTop;
            var chromRightID = feature.ChromScanIdRight;

            feature.MS1RawSpectrumIdLeft = peaklist[chromLeftID].ID;
            feature.MS1RawSpectrumIdTop = peaklist[chromTopID].ID;
            feature.MS1RawSpectrumIdRight = peaklist[chromRightID].ID;
        }

        private static void SetMs2RawSpectrumIDs2ChromatogramPeakFeature(
            ChromatogramPeakFeature feature,
            IDataProvider provider,
            MsdialImmsParameter param) {

            var spectrumList = provider.LoadMsNSpectrums(level: 2);

            var mass = feature.Mass;
            var dt = feature.ChromXsTop.Drift.Value;
            var dtStart = feature.ChromXsLeft.Value;
            var dtEnd = feature.ChromXsRight.Value;
            var scanPolarity = param.IonMode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;
            var ms2Tol = FixMassTolerance(param.CentroidMs2Tolerance, mass);

            var specs = new List<RawSpectrum>();

            // TODO: slow. improve search algorithm.
            foreach (var spec in spectrumList) {
                if (spec.Precursor != null && spec.ScanPolarity == scanPolarity) {
                    var specPrecMz = spec.Precursor.SelectedIonMz;
                    var lowerOffset = spec.Precursor.IsolationWindowLowerOffset;
                    var upperOffset = spec.Precursor.IsolationWindowUpperOffset;

                    var IsMassInWindow = specPrecMz - lowerOffset - ms2Tol < mass && mass < specPrecMz + upperOffset + ms2Tol
                           ? true : false;
                    var IsDtInWindow = Math.Min(spec.Precursor.TimeBegin, spec.Precursor.TimeEnd) <= dt && dt < Math.Max(spec.Precursor.TimeBegin, spec.Precursor.TimeEnd)
                        ? true : false; // used for diapasef

                    if (spec.Precursor.TimeBegin == spec.Precursor.TimeEnd) { // meaning normal dia data
                        if (dtStart <= spec.DriftTime && spec.DriftTime <= dtEnd) {
                            IsDtInWindow = true;
                        }
                    }
                    if (IsMassInWindow && IsMassInWindow) {
                        specs.Add(spec);
                    }
                    //if (specPrecMz - lowerOffset - ms2Tol < mass & mass < specPrecMz + upperOffset + ms2Tol) {
                    //    if (dtStart <= spec.DriftTime && spec.DriftTime <= dtEnd) {
                    //        specs.Add(spec);
                    //    }
                    //}
                }
            }

            var representatives = specs
                .GroupBy(spec => Math.Round(spec.CollisionEnergy, 2))  // grouping by ce
                .Select(group => group.Argmax(spec => spec.TotalIonCurrent)) // choose largest ion current, for each ce
                .ToList();

            if (representatives.Any()) {
                feature.MS2RawSpectrumID2CE = representatives.ToDictionary(spec => spec.OriginalIndex, spec => spec.CollisionEnergy);
                feature.MS2RawSpectrumID = representatives.Argmax(spec => spec.TotalIonCurrent).OriginalIndex;
            }
        }

        private static double FixMassTolerance(double tolerance, double mass) {
            if (mass > 500) {
                var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500d, 500d + tolerance));
                return MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ppm);
            }
            return tolerance;
        }

        private static List<ChromatogramPeakFeature> GetBackgroundSubtractedPeaks(List<ChromatogramPeakFeature> chromPeakFeatures, List<ChromatogramPeak> peaklist) {
            const int counterThreshold = 4;

            var sPeakAreaList = new List<ChromatogramPeakFeature>();
            foreach (var feature in chromPeakFeatures) {
                var peakTop = feature.ChromScanIdTop;
                var peakLeft = feature.ChromScanIdLeft;
                var peakRight = feature.ChromScanIdRight;

                if (peakTop - 1 < 0 || peakTop + 1 > peaklist.Count - 1
                    || peaklist[peakTop - 1].Intensity <= 0 || peaklist[peakTop + 1].Intensity <= 0)
                    continue;

                var trackingNumber = Math.Min(10 * (peakRight - peakLeft), 50);
                var ampDiff = Math.Max(feature.PeakHeightTop - feature.PeakHeightLeft, feature.PeakHeightTop - feature.PeakHeightRight);

                var counter = 0;
                counter += CountLargeIntensityChange(peaklist, ampDiff, peakLeft - trackingNumber, peakLeft);
                counter += CountLargeIntensityChange(peaklist, ampDiff, peakRight, peakRight + trackingNumber);

                if (counter < counterThreshold)
                    sPeakAreaList.Add(feature);
            }
            return sPeakAreaList;
        }

        private static int CountLargeIntensityChange(List<ChromatogramPeak> peaklist, double threshold, int left, int right) {
            var leftBound = Math.Max(left, 1);
            var rightBound = Math.Min(right, peaklist.Count - 2);

            var counter = 0;
            double? spikeMax = null, spikeMin = null;
            for (int i = leftBound; i <= rightBound; i++) {

                if (IsPeak(peaklist[i - 1].Intensity, peaklist[i].Intensity, peaklist[i + 1].Intensity))
                    spikeMax = peaklist[i].Intensity;
                else if (IsBottom(peaklist[i - 1].Intensity, peaklist[i].Intensity, peaklist[i + 1].Intensity))
                    spikeMin = peaklist[i].Intensity;

                if (spikeMax.HasValue && spikeMin.HasValue) {
                    var noise = 0.5 * Math.Abs(spikeMax.Value - spikeMin.Value);
                    if (noise * 3 > threshold)
                        counter++;
                    spikeMax = null; spikeMin = null;
                }
            }

            return counter;
        }

        private static bool IsPeak(double left, double center, double right) {
            return left <= center && center >= right;
        }

        private static bool IsBottom(double left, double center, double right) {
            return left >= center && center <= right;
        }

        private static List<ChromatogramPeakFeature> RemovePeakAreaBeanRedundancy(
            List<ChromatogramPeakFeature> chromPeakFeatures,
            List<ChromatogramPeakFeature> parentPeakFeatures,
            float massStep) {

            if (chromPeakFeatures == null)
                return new List<ChromatogramPeakFeature>();
            if (parentPeakFeatures == null)
                return chromPeakFeatures;

            for (int i = 0; i < chromPeakFeatures.Count; i++) {
                for (int j = 0; j < parentPeakFeatures.Count; j++) {

                    if (Math.Abs(parentPeakFeatures[j].Mass - chromPeakFeatures[i].Mass) > massStep * 0.5)
                        continue;

                    if (!IsOverlaped(parentPeakFeatures[j], chromPeakFeatures[i]))
                        continue;

                    var hwhm = ((parentPeakFeatures[j].ChromXsRight.Value - parentPeakFeatures[j].ChromXsLeft.Value) +
                        (chromPeakFeatures[i].ChromXsRight.Value - chromPeakFeatures[i].ChromXsLeft.Value)) * 0.25;
                    var tolerance = Math.Min(hwhm, 0.03);

                    if (Math.Abs(parentPeakFeatures[j].ChromXs.Value - chromPeakFeatures[i].ChromXs.Value) <= tolerance) {
                        if (parentPeakFeatures[j].PeakHeightTop < chromPeakFeatures[i].PeakHeightTop) {
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

        private static bool IsOverlaped(ChromatogramPeakFeature peak1, ChromatogramPeakFeature peak2) {
            if (peak1.ChromXs.Value > peak2.ChromXs.Value) {
                if (peak1.ChromXsLeft.Value < peak2.ChromXs.Value)
                    return true;
            }
            else {
                if (peak2.ChromXsLeft.Value < peak1.ChromXs.Value)
                    return true;
            }
            return false;
        }

        private static List<ChromatogramPeakFeature> GetCombinedChromPeakFeatures(List<List<ChromatogramPeakFeature>> chromPeakFeaturesList) {
            return chromPeakFeaturesList.SelectMany(features => features).ToList();
        }

        private static  List<ChromatogramPeakFeature> GetRecalculatedChromPeakFeaturesByMs1MsTolerance(
            List<ChromatogramPeakFeature> chromPeakFeatures,
            IDataProvider provider, MsdialImmsParameter param,
            ChromXType type, ChromXUnit unit) {

            var spectrumList = provider.LoadMs1Spectrums();
            var recalculatedPeakspots = new List<ChromatogramPeakFeature>();

            var minDatapoint = 3;
            foreach (var spot in chromPeakFeatures) {
                //get EIC chromatogram

                var peakWidth = spot.PeakWidth();
                var peakWidthMargin = peakWidth * 0.5;

                var peaklist = DataAccess.GetMs1Peaklist(spectrumList, (float)spot.Mass, param.CentroidMs1Tolerance, param.IonMode, type, unit,
                    (float)(spot.ChromXsLeft.Value - peakWidthMargin), (float)(spot.ChromXsRight.Value + peakWidthMargin));
                var sPeaklist = DataAccess.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);

                var minRtId = SearchNearestPoint(spot.ChromXs, sPeaklist);

                var maxID = SearchPeakTop(sPeaklist, minRtId);
                var minLeftId = SearchLeftEdge(sPeaklist, spot, maxID, minDatapoint, peakWidth);
                var minRightId = SearchRightEdge(sPeaklist, spot, maxID, minDatapoint, peakWidth);

                if (Math.Max(sPeaklist[minLeftId].Intensity, sPeaklist[minRightId].Intensity) >= sPeaklist[maxID].Intensity) continue;
                if (sPeaklist[maxID].Intensity - Math.Min(sPeaklist[minLeftId].Intensity, sPeaklist[minRightId].Intensity) < param.MinimumAmplitude) continue;

                maxID = SearchHighestIntensity(sPeaklist, maxID, minLeftId, minRightId);

                SetPeakProperty(spot, sPeaklist, maxID, minLeftId, minRightId);
                SetRawPeakProperty(spot, peaklist, maxID, minLeftId, minRightId);

                if (spot.DriftChromFeatures == null) { // meaning not ion mobility data
                    SetMs2RawSpectrumIDs2ChromatogramPeakFeature(spot, provider, param);
                }

                recalculatedPeakspots.Add(spot);
            }
            return recalculatedPeakspots;
        }

        private static int SearchNearestPoint(ChromXs chrom, IEnumerable<ChromatogramPeak> peaklist) {
            return peaklist
                .Select(peak => Math.Abs(peak.ChromXs.Value - chrom.Value))
                .Argmin();
        }

        private static int SearchPeakTop(List<ChromatogramPeak> peaklist, int center) {
            var maxID = center;
            var maxInt = double.MinValue;
            //finding local maximum within -2 ~ +2
            for (int i = center - 2; i <= center + 2; i++) {
                if (i - 1 < 0) continue;
                if (i > peaklist.Count - 2) break;
                if (peaklist[i].Intensity > maxInt && IsPeak(peaklist[i - 1].Intensity, peaklist[i].Intensity, peaklist[i + 1].Intensity)) {

                    maxInt = peaklist[i].Intensity;
                    maxID = i;
                }
            }
            return maxID;
        }

        private static int SearchLeftEdge(List<ChromatogramPeak> sPeaklist, ChromatogramPeakFeature spot, int center, int minDatapoint, double peakWidth) {
            //finding left edge;
            //seeking left edge
            int? minLeftId = null;
            var minLeftInt = sPeaklist[center].Intensity;
            for (int i = center - minDatapoint; i >= 0; i--) {

                if (minLeftInt < sPeaklist[i].Intensity) {
                    break;
                }
                if (sPeaklist[center].ChromXs.Value - peakWidth > sPeaklist[i].ChromXs.Value) {
                    break;
                }

                minLeftInt = sPeaklist[i].Intensity;
                minLeftId = i;
            }

            if (minLeftId.HasValue) {
                return minLeftId.Value;
            }

            return SearchNearestPoint(spot.ChromXsLeft, sPeaklist.Take(center + 1));
        }

        private static int SearchRightEdge(List<ChromatogramPeak> sPeaklist, ChromatogramPeakFeature spot, int center, int minDatapoint, double peakWidth) {
            //finding right edge;
            int? minRightId = null;
            var minRightInt = sPeaklist[center].Intensity;
            for (int i = center + minDatapoint; i < sPeaklist.Count - 1; i++) {

                if (i > center && minRightInt < sPeaklist[i].Intensity) {
                    break;
                }
                if (sPeaklist[center].ChromXs.Value + peakWidth < sPeaklist[i].ChromXs.Value) break;
                if (minRightInt >= sPeaklist[i].Intensity) {
                    minRightInt = sPeaklist[i].Intensity;
                    minRightId = i;
                }
            }
            if (minRightId.HasValue) {
                return minRightId.Value;
            }

            return center + SearchNearestPoint(spot.ChromXsRight, sPeaklist.Skip(center));
        }

        private static int SearchHighestIntensity(List<ChromatogramPeak> sPeaklist, int maxID, int minLeftId, int minRightId) {
            var realMaxInt = double.MinValue;
            var realMaxID = maxID;
            for (int i = minLeftId; i < minRightId; i++) {
                if (realMaxInt < sPeaklist[i].Intensity) {
                    realMaxInt = sPeaklist[i].Intensity;
                    realMaxID = i;
                }
            }
            return realMaxID;
        }

        private static void SetPeakProperty(ChromatogramPeakFeature spot, List<ChromatogramPeak> sPeaklist, int maxID, int minLeftId, int minRightId) {
            // calculating peak area 
            var peakAreaAboveZero = 0.0;
            for (int i = minLeftId; i <= minRightId - 1; i++) {
                peakAreaAboveZero += (sPeaklist[i].Intensity + sPeaklist[i + 1].Intensity) * (sPeaklist[i + 1].ChromXs.Value - sPeaklist[i].ChromXs.Value) * 0.5;
            }

            var peakAreaAboveBaseline = peakAreaAboveZero - (sPeaklist[minLeftId].Intensity + sPeaklist[minRightId].Intensity) *
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

            var peakHeightFromBaseline = Math.Max(sPeaklist[maxID].Intensity - sPeaklist[minLeftId].Intensity, sPeaklist[maxID].Intensity - sPeaklist[minRightId].Intensity);
            spot.PeakShape.SignalToNoise = (float)(peakHeightFromBaseline / spot.PeakShape.EstimatedNoise);
        }

        private static void SetRawPeakProperty(ChromatogramPeakFeature spot, List<ChromatogramPeak> peaklist, int maxID, int minLeftId, int minRightId) {
            spot.MS1RawSpectrumIdTop = peaklist[maxID].ID;
            spot.MS1RawSpectrumIdLeft = peaklist[minLeftId].ID;
            spot.MS1RawSpectrumIdRight = peaklist[minRightId].ID;
        }

        private static void SetAmplitudeScore(List<ChromatogramPeakFeature> chromPeakFeatures) {

            var num = chromPeakFeatures.Count;
            if (num - 1 > 0) {
                var ordered = chromPeakFeatures.OrderBy(n => n.PeakHeightTop).ToList();
                for (var i = 0; i < num; i++) {
                    chromPeakFeatures[i].PeakShape.AmplitudeScoreValue = (float)((double)i / (double)(num - 1));
                    chromPeakFeatures[i].PeakShape.AmplitudeOrderValue = i;
                }
            }
        }

        private static void SetPeakID(List<ChromatogramPeakFeature> chromPeakFeatures) {
            var ordered = chromPeakFeatures.OrderBy(n => n.ChromXs.Value).ThenBy(n => n.Mass).ToList();

            var masterPeakID = 0;
            for (int i = 0; i < ordered.Count; i++) {
                var peakSpot = ordered[i];
                peakSpot.PeakID = i;
                peakSpot.MasterPeakID = masterPeakID;
                masterPeakID++;

                if (!peakSpot.DriftChromFeatures.IsEmptyOrNull()) {
                    for (int j = 0; j < peakSpot.DriftChromFeatures.Count; j++) {
                        var driftSpot = peakSpot.DriftChromFeatures[j];
                        driftSpot.MasterPeakID = masterPeakID;
                        driftSpot.PeakID = j;
                        driftSpot.ParentPeakID = i;
                        masterPeakID++;
                    }
                }
            }
        }
    }
}

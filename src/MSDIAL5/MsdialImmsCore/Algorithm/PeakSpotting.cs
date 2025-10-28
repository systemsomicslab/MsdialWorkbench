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
        private readonly ChromatogramRange _chromatogramRange;

        public PeakSpotting(MsdialImmsParameter parameter) {
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _chromatogramRange = new ChromatogramRange(_parameter.DriftTimeBegin, _parameter.DriftTimeEnd, ChromXType.Drift, ChromXUnit.Msec);
        }

        public ChromatogramPeakFeatureCollection Run(AnalysisFileBean file, IDataProvider provider, ReportProgress? reporter) {
            var rawSpectra = new RawSpectra(provider, _parameter.ProjectParam.IonMode, file.AcquisitionType);
            var detector = new PeakDetection(_parameter.PeakPickBaseParam.MinimumDatapoints, _parameter.PeakPickBaseParam.MinimumAmplitude);
            IEnumerable<ChromatogramPeakFeature> chromPeakFeatures;
            if (_parameter.AdvancedProcessOptionBaseParam.IsTargetMode) {
                chromPeakFeatures = DetectChromatogramPeaksUsingTargetCompound(rawSpectra, detector);
            }
            else {
                chromPeakFeatures = DetectChromatogramPeaks(provider, reporter, rawSpectra, detector);
            }
            var reevaluatedPeaks = ReevaluateChromPeakFeatures(chromPeakFeatures, provider, file);
            var collection = new ChromatogramPeakFeatureCollection(reevaluatedPeaks.OrderBy(item => item.ChromXs.Value).ThenBy(item => item.PeakFeature.Mass).ToList());
            collection.ResetAmplitudeScore();
            collection.ResetPeakID();
            return collection;
        }

        private IEnumerable<ChromatogramPeakFeature> DetectChromatogramPeaks(IDataProvider provider, ReportProgress? reporter, RawSpectra rawSpectra, PeakDetection detector) {
            var massStep = _parameter.ChromDecBaseParam.AccuracyType == AccuracyType.IsNominal ? 1f : _parameter.PeakPickBaseParam.MassSliceWidth;
            var chromPeakFeaturesList = new List<List<ChromatogramPeakFeature>>();
            foreach (var focusedMass in EnumerateMzToSearch(provider, massStep, reporter)) {
                var chromatogramPeakFeatures = DetectChromatogramPeaks(massStep, rawSpectra, chromPeakFeaturesList.LastOrDefault(), detector, focusedMass);
                if (chromatogramPeakFeatures.Any()) {
                    chromPeakFeaturesList.Add(chromatogramPeakFeatures);
                }
            }
            return chromPeakFeaturesList.SelectMany(features => features);
        }

        private IEnumerable<float> EnumerateMzToSearch(IDataProvider provider, float step, ReportProgress? reporter) {
            var (mzMin, mzMax) = provider.GetMs1Range(_parameter.ProjectParam.IonMode);
            var startMass = Math.Max(mzMin, _parameter.PeakPickBaseParam.MassRangeBegin);
            var endMass = Math.Min(mzMax, _parameter.PeakPickBaseParam.MassRangeEnd);
            for (var focusedMass = startMass; focusedMass < endMass; focusedMass += step) {
                reporter?.Report(focusedMass, endMass);
                yield return focusedMass;
            }
        }

        private List<ChromatogramPeakFeature> DetectChromatogramPeaks(float massStep, RawSpectra rawSpectra, List<ChromatogramPeakFeature> previousChromPeakFeatures, PeakDetection detector, float focusedMass) {
            var chromPeakFeatures = GetChromatogramPeakFeatures(rawSpectra, focusedMass, detector);
            var removedPeakFeatures = RemovePeakAreaBeanRedundancy(chromPeakFeatures, previousChromPeakFeatures, massStep);
            return removedPeakFeatures;
        }

        private IEnumerable<ChromatogramPeakFeature> DetectChromatogramPeaksUsingTargetCompound(RawSpectra rawSpectra, PeakDetection detector) {
            var result = new List<ChromatogramPeakFeature>();
            foreach (var targetComp in _parameter.AdvancedProcessOptionBaseParam.CompoundListInTargetMode) {
                var chromPeakFeatures = GetChromatogramPeakFeatures(rawSpectra, (float)targetComp.PrecursorMz, detector);
                result.AddRange(chromPeakFeatures);
            }
            return result;
        }

        private List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(RawSpectra rawSpectra, float focusedMass, PeakDetection peakDetector) {
            var chromatogram = rawSpectra.GetMS1ExtractedChromatogram(new MzRange(focusedMass, _parameter.PeakPickBaseParam.MassSliceWidth), _chromatogramRange);
            if (chromatogram.IsEmpty) {
                return new List<ChromatogramPeakFeature>(0);
            }

            ExtractedIonChromatogram smoothedChromatogram = chromatogram.ChromatogramSmoothing(_parameter.PeakPickBaseParam.SmoothingMethod, _parameter.PeakPickBaseParam.SmoothingLevel);
            var chromPeakFeatures = GetChromatogramPeakFeatures(chromatogram, peakDetector, smoothedChromatogram);
            var subtractedFeatures = GetBackgroundSubtractedPeaks(chromPeakFeatures, chromatogram);
            var collection = new ChromatogramPeakFeatureCollection(subtractedFeatures);
            collection.SetRawMs1Id(smoothedChromatogram);
            return subtractedFeatures;
        }

        private List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(ExtractedIonChromatogram chromatogram, PeakDetection peakDetector, ExtractedIonChromatogram smoothedChromatogram) {
            var detectedPeaks = peakDetector.PeakDetectionVS1(smoothedChromatogram);
            var chromPeakFeatures = new List<ChromatogramPeakFeature>();
            foreach (var result in detectedPeaks) {
                if (result.IntensityAtPeakTop <= 0) {
                    continue;
                }
                var mass = smoothedChromatogram.Mz(result.ScanNumAtPeakTop);
                //option
                //Users can prepare their-own 'exclusion mass' list to exclude unwanted peak features
                if (_parameter.PeakPickBaseParam.ShouldExclude(mass)) {
                    continue;
                }
                chromPeakFeatures.Add(ChromatogramPeakFeature.FromPeakDetectionResult(result, chromatogram, mass, _parameter.ProjectParam.IonMode));
            }
            return chromPeakFeatures;
        }

        private void SetMs2RawSpectrumIDs2ChromatogramPeakFeature(ChromatogramPeakFeature feature, IDataProvider provider, AnalysisFileBean analysisFile) {
            var spectrumList = provider.LoadMsNSpectrums(level: 2);
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
                if (spec.Precursor is null || spec.ScanPolarity != scanPolarity) {
                    continue;
                }
                var IsMassInWindow = spec.Precursor.ContainsMz(mass, ms2Tol, analysisFile.AcquisitionType);
                var IsDtInWindow = spec.Precursor.ContainsDriftTime(dt) // used for diapasef
                    || (spec.Precursor.IsNotDiapasefData && spec.IsInDriftTimeRange(dtStart, dtEnd)); // normal dia
                if (IsMassInWindow && IsDtInWindow) {
                    specs.Add(spec);
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

        private List<ChromatogramPeakFeature> ReevaluateChromPeakFeatures(IEnumerable<ChromatogramPeakFeature> chromPeakFeatures, IDataProvider provider, AnalysisFileBean analysisFile) {
            var recalculatedPeakspots = new List<ChromatogramPeakFeature>();
            var minDatapoint = 3;
            var rawSpectra = new RawSpectra(provider, _parameter.ProjectParam.IonMode, analysisFile.AcquisitionType);
            foreach (var spot in chromPeakFeatures) {
                //get EIC chromatogram
                var peakFeature = spot.PeakFeature;
                var peakRange = new ChromatogramRange(peakFeature, ChromXType.Drift, ChromXUnit.Msec);
                var chromatogram = rawSpectra.GetMS1ExtractedChromatogram(new MzRange(peakFeature.Mass, _parameter.PeakPickBaseParam.CentroidMs1Tolerance), peakRange.ExtendRelative(0.5d));
                var smoothedChromatogram = chromatogram.ChromatogramSmoothing(_parameter.PeakPickBaseParam.SmoothingMethod, _parameter.PeakPickBaseParam.SmoothingLevel);
                var peakOfChromatogram = smoothedChromatogram.FindPeak(minDatapoint, peakRange.Width, peakFeature);
                if (!peakOfChromatogram.IsValid(_parameter.PeakPickBaseParam.MinimumAmplitude)) {
                    continue;
                }

                spot.SetPeakProperties(peakOfChromatogram);
                if (!spot.IsMultiLayeredData()) {
                    SetMs2RawSpectrumIDs2ChromatogramPeakFeature(spot, provider, analysisFile);
                }
                recalculatedPeakspots.Add(spot);
            }
            return recalculatedPeakspots;
        }

        private const int SPIKE_COUNT_THRESHOLD = 4;
        private static List<ChromatogramPeakFeature> GetBackgroundSubtractedPeaks(List<ChromatogramPeakFeature> chromPeakFeatures, ExtractedIonChromatogram chromatogram) {
            var sPeakAreaList = new List<ChromatogramPeakFeature>();
            foreach (var feature in chromPeakFeatures) {
                var peakFeature = feature.PeakFeature;
                if (!chromatogram.IsValidPeakTop(peakFeature.ChromScanIdTop)) {
                    continue;
                }

                var peakLeft = peakFeature.ChromScanIdLeft;
                var peakRight = peakFeature.ChromScanIdRight;
                var trackingNumber = Math.Min(10 * (peakRight - peakLeft), 50);
                var ampDiff = peakFeature.PeakHeightTop - Math.Min(peakFeature.PeakHeightLeft, peakFeature.PeakHeightRight);

                var counter = chromatogram.CountSpikes(peakLeft - trackingNumber, peakLeft, ampDiff / 3d)
                    + chromatogram.CountSpikes(peakRight, peakRight + trackingNumber, ampDiff / 3d);
                if (counter < SPIKE_COUNT_THRESHOLD) {
                    sPeakAreaList.Add(feature);
                }
            }
            return sPeakAreaList;
        }

        private static List<ChromatogramPeakFeature> RemovePeakAreaBeanRedundancy(List<ChromatogramPeakFeature> chromPeakFeatures, List<ChromatogramPeakFeature> parentPeakFeatures, float massStep) {
            if (chromPeakFeatures is null) {
                return new List<ChromatogramPeakFeature>(0);
            }
            if (parentPeakFeatures is null) {
                return chromPeakFeatures;
            }

            for (int i = 0; i < chromPeakFeatures.Count; i++) {
                for (int j = 0; j < parentPeakFeatures.Count; j++) {

                    var parentPeakFeature = parentPeakFeatures[j].PeakFeature;
                    var chromPeakFeature = chromPeakFeatures[i].PeakFeature;
                    if (Math.Abs(parentPeakFeature.Mass - chromPeakFeature.Mass) > massStep * 0.5) {
                        continue;
                    }

                    if (!parentPeakFeature.IsOverlaped(chromPeakFeature)) {
                        continue;
                    }

                    var hwhm = (parentPeakFeature.ChromXsRight.Value - parentPeakFeature.ChromXsLeft.Value +
                        (chromPeakFeature.ChromXsRight.Value - chromPeakFeature.ChromXsLeft.Value)) * 0.25;
                    var tolerance = Math.Min(hwhm, 0.03);

                    if (Math.Abs(parentPeakFeature.ChromXsTop.Value - chromPeakFeature.ChromXsTop.Value) <= tolerance) {
                        if (parentPeakFeature.PeakHeightTop < chromPeakFeature.PeakHeightTop) {
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
                if (parentPeakFeatures.IsEmptyOrNull()) {
                    return chromPeakFeatures;
                }
            }
            return chromPeakFeatures;
        }
    }
}

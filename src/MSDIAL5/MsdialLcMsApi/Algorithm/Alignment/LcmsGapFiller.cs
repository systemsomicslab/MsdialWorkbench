using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment;

public class LcmsGapFiller : IGapFiller
{
    private SmoothingMethod _smoothingMethod;
    private int _smoothingLevel;
    private bool _isForceInsert; 
    private readonly double _mzTol, _rtTol;

    public LcmsGapFiller(double rtTol, double mzTol, SmoothingMethod smoothingMethod, int smoothingLevel, bool isForceInsert) {
        _rtTol = rtTol;
        _mzTol = mzTol;
        _smoothingMethod = smoothingMethod;
        _smoothingLevel = smoothingLevel;
        _isForceInsert = isForceInsert;
    }

    public LcmsGapFiller(MsdialLcmsParameter param)
        : this(param.RetentionTimeAlignmentTolerance, param.CentroidMs1Tolerance, param.SmoothingMethod,
              param.SmoothingLevel, param.IsForceInsertForGapFilling) { }

    public void GapFill(Ms1Spectra ms1Spectra, RawSpectra rawSpectra, IReadOnlyList<RawSpectrum> spectra, AlignmentSpotProperty spot, int fileID) {
        var peaks = spot.AlignedPeakProperties;
        var detected = peaks.Where(peak => peak.PeakID >= 0).ToArray();

        var target = peaks.First(peak => peak?.FileID == fileID);
        target.PeakShape.EstimatedNoise = GetEstimatedNoise(detected);

        var chromXCenter = GetCenter(spot, detected);
        var peakWidth = GetPeakWidth(detected);
        GapFillCore(chromXCenter, target, ms1Spectra, peakWidth);
    }

    public bool NeedsGapFill(AlignmentSpotProperty spot, AnalysisFileBean analysisFile) {
        return spot.AlignedPeakProperties.First(p => p.FileID == analysisFile.AnalysisFileId).MasterPeakID < 0;
    }

    private ChromXs GetCenter(AlignmentSpotProperty spot, IEnumerable<AlignmentChromPeakFeature> peaks) {
        return new ChromXs(peaks.Average(peak => peak.ChromXsTop.RT.Value), ChromXType.RT, ChromXUnit.Min)
        {
            Mz = new MzValue(peaks.Argmax(peak => peak.PeakHeightTop).Mass),
        };
    }

    private double GetPeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks) {
        return peaks.Max(peak => peak.PeakWidth(ChromXType.RT));
    }

    private Chromatogram GetPeaks(Ms1Spectra ms1Spectra, ChromXs center, double peakWidth, SmoothingMethod smoothingMethod, int smoothingLevel) {
        var chromatogramRange = ChromatogramRange.FromTimes(center.RT, Math.Max(peakWidth, 0.2f)).ExtendRelative(1d);
        var mzRange = new MzRange(center.Mz.Value, _mzTol);

        using var peaklist = ms1Spectra.GetMs1ExtractedChromatogram(mzRange, chromatogramRange);
        return peaklist.ChromatogramSmoothing(smoothingMethod, smoothingLevel);
    }

    private float GetEstimatedNoise(IEnumerable<AlignmentChromPeakFeature> peaks) {
        return peaks.Max(n => n.PeakShape.EstimatedNoise);
    }

    private void GapFillCore(ChromXs center, AlignmentChromPeakFeature alignmentChromPeakFeature, Ms1Spectra ms1Spectra, double peakWidth) {
        using var smoothed = GetPeaks(ms1Spectra, center, peakWidth, _smoothingMethod, _smoothingLevel);

        if (smoothed.Length == 0) {
            SetDefaultValueToAlignmentChromPeakFeature(alignmentChromPeakFeature, center.Mz.Value);
            return;
        }
        if (alignmentChromPeakFeature.MasterPeakID < 0) {
            alignmentChromPeakFeature.MasterPeakID = -2;
            alignmentChromPeakFeature.PeakID = -2;
        }

        (var id, var leftId, var rightId) = GetNearestPeak(smoothed, center.Value);
        if (id == -1 || leftId == -1 || rightId == -1) {
            SetDefaultValueToAlignmentChromPeakFeature(alignmentChromPeakFeature, center.Mz.Value);
            return;
        }
        SetAlignmentChromPeakFeature(alignmentChromPeakFeature, center, smoothed, id, leftId, rightId);
    }

    private (int id, int leftId, int rightId) GetNearestPeak(Chromatogram chromatogram, double centralAx) {
        var nearestTopId = -1;
        var minId = chromatogram.Length / 2;

        for (int i = 0; i < chromatogram.Length; i++) {
            if (i - 2 < 0 || i + 2 >= chromatogram.Length) continue;
            if (chromatogram.Time(i) < centralAx - _rtTol) continue;
            if (centralAx + _rtTol < chromatogram.Time(i)) break;

            if (chromatogram.IsBroadPeakTop(i)
                && (nearestTopId < 0
                    || Math.Abs(chromatogram.Time(nearestTopId) - centralAx) > Math.Abs(chromatogram.Time(i) - centralAx))) {
                nearestTopId = i;
            }

            var diff = Math.Abs(chromatogram.Time(i) - centralAx);
            if (diff < Math.Abs(chromatogram.Time(minId) - centralAx)) {
                minId = i;
            }
        }

        if (nearestTopId == -1) {
            if (!_isForceInsert) return (-1, -1, -1);
            var range = 5;

            var leftId = Math.Max(minId - 1, 0);
            var limit = Math.Max(minId - range, 0);
            while (limit < leftId) {
                if (chromatogram.Intensity(leftId - 1) > chromatogram.Intensity(leftId)) {
                    break;
                }
                --leftId;
            }

            var rightId = Math.Min(minId + 1, chromatogram.Length - 1);
            limit = Math.Min(minId + range, chromatogram.Length - 1);
            while (rightId < limit) {
                if (chromatogram.Intensity(rightId) < chromatogram.Intensity(rightId + 1)) {
                    break;
                }
                ++rightId;
            }
            return (id: minId, leftId, rightId);
        }
        else {
            var margin = 2;

            int leftId = Math.Max(nearestTopId - margin, 0);
            while (0 < leftId) {
                if (chromatogram.Intensity(leftId - 1) >= chromatogram.Intensity(leftId)) {
                    break;
                }
                --leftId;
            }

            int rightId = Math.Min(nearestTopId + margin, chromatogram.Length - 1);
            while (rightId < chromatogram.Length - 1) {
                if (chromatogram.Intensity(rightId) <= chromatogram.Intensity(rightId + 1)) {
                    break;
                }
                ++rightId;
            }

            if (!_isForceInsert && (nearestTopId - leftId < 2 || rightId - nearestTopId < 2)) {
                return (-1, -1, -1);
            }

            int id = nearestTopId;
            for(int i = leftId + 1; i <= rightId - 1; i++) {
                if (chromatogram.IsPeakTop(i) && chromatogram.Intensity(id) < chromatogram.Intensity(i)) {
                    id = i;
                }
            }
            return (id, leftId, rightId);
        }
    }

    private static void SetAlignmentChromPeakFeature(AlignmentChromPeakFeature result, ChromXs center, Chromatogram chromatogram, int id, int leftId, int rightId) {
        double peakAreaAboveZero = 0d;
        for (int i = leftId; i < rightId; i++) {
            peakAreaAboveZero += (chromatogram.Intensity(i) + chromatogram.Intensity(i + 1)) / 2 * (chromatogram.Time(i + 1) - chromatogram.Time(i));
        }
        peakAreaAboveZero *= 60d;

        var baseline = (chromatogram.Intensity(leftId) + chromatogram.Intensity(rightId)) * (chromatogram.Time(rightId) - chromatogram.Time(leftId)) / 2;
        baseline *= 60d;

        if (result.MasterPeakID < 0) {
            result.Mass = center.Mz.Value;
        }
        result.ChromScanIdTop = id;
        result.ChromScanIdLeft = leftId;
        result.ChromScanIdRight = rightId;
        result.ChromXsTop = chromatogram.PeakChromXs(id);
        result.ChromXsLeft = chromatogram.PeakChromXs(leftId);
        result.ChromXsRight = chromatogram.PeakChromXs(rightId);
        result.PeakHeightTop = chromatogram.Intensity(id);
        result.PeakHeightLeft = chromatogram.Intensity(leftId);
        result.PeakHeightRight = chromatogram.Intensity(rightId);
        result.PeakAreaAboveZero = peakAreaAboveZero;
        result.PeakAreaAboveBaseline = peakAreaAboveZero - baseline;
        result.PeakShape.SignalToNoise = (float)result.PeakHeightTop / result.PeakShape.EstimatedNoise;
    }

    private static void SetDefaultValueToAlignmentChromPeakFeature(AlignmentChromPeakFeature result, double mz) {
        result.Mass = mz;
        result.ChromScanIdTop = -1;
        result.ChromScanIdLeft = -1;
        result.ChromScanIdRight = -1;
        result.ChromXsTop = new ChromXs(0, ChromXType.Mz, ChromXUnit.Mz);
        result.ChromXsLeft = new ChromXs(0, ChromXType.Mz, ChromXUnit.Mz);
        result.ChromXsRight = new ChromXs(0, ChromXType.Mz, ChromXUnit.Mz);
        result.PeakHeightTop = 0;
        result.PeakHeightLeft = 0;
        result.PeakHeightRight = 0;
        result.PeakAreaAboveZero = 0;
        result.PeakAreaAboveBaseline = 0;
        result.PeakShape.SignalToNoise = 0;
    }
}

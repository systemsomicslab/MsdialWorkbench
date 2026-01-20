using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
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
        GapFill(ms1Spectra, spot, fileID);
    }

    public void GapFill(Ms1Spectra ms1Spectra, AlignmentSpotProperty spot, int fileID) {
        var peaks = spot.AlignedPeakProperties;
        var detected = peaks.Where(peak => peak.PeakID >= 0).ToArray();

        var target = peaks.First(peak => peak?.FileID == fileID);
        target.PeakShape.EstimatedNoise = GetEstimatedNoise(detected);

        var rtCenter = new ChromXs(new RetentionTime(detected.Average(peak => peak.ChromXsTop.RT.Value), ChromXUnit.Min))
        {
            Mz = new MzValue(detected.Argmax(peak => peak.PeakHeightTop).Mass),
        };
        var peakWidth = detected.Max(peak => peak.PeakWidth(ChromXType.RT));
        GapFillCore(target, ms1Spectra, rtCenter, peakWidth);
    }

    public bool NeedsGapFill(AlignmentSpotProperty spot, AnalysisFileBean analysisFile) {
        return spot.AlignedPeakProperties.First(p => p.FileID == analysisFile.AnalysisFileId).MasterPeakID < 0;
    }

    [Obsolete("zzz")]
    private Chromatogram GetPeaks(Ms1Spectra ms1Spectra, ChromXs center, double peakWidth, SmoothingMethod smoothingMethod, int smoothingLevel) {
        var chromatogramRange = ChromatogramRange.FromTimes(center.RT, Math.Max(peakWidth, 0.2f)).ExtendRelative(1d);
        var mzRange = new MzRange(center.Mz.Value, _mzTol);

        using var peaklist = ms1Spectra.GetMS1ExtractedChromatogramAsync(mzRange, chromatogramRange, default).Result;
        return peaklist.ChromatogramSmoothing(smoothingMethod, smoothingLevel);
    }

    private float GetEstimatedNoise(IEnumerable<AlignmentChromPeakFeature> peaks) {
        return peaks.Max(n => n.PeakShape.EstimatedNoise);
    }

    private void GapFillCore(AlignmentChromPeakFeature alignmentChromPeakFeature, Ms1Spectra ms1Spectra, ChromXs center, double peakWidth) {
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

        for (int i = 0; i < chromatogram.Length; i++) {
            if (i - 2 < 0 || i + 2 >= chromatogram.Length) continue;
            if (chromatogram.Time(i) < centralAx - _rtTol) continue;
            if (centralAx + _rtTol < chromatogram.Time(i)) break;

            if (chromatogram.IsBroadPeakTop(i)
                && (nearestTopId < 0
                    || Math.Abs(chromatogram.Time(nearestTopId) - centralAx) > Math.Abs(chromatogram.Time(i) - centralAx))) {
                nearestTopId = i;
            }
        }

        if (nearestTopId == -1) {
            if (!_isForceInsert) return (-1, -1, -1);

            var minId = chromatogram.SearchNearestPoint(new RetentionTime(centralAx));
            if (minId < 0) {
                minId = chromatogram.Length / 2;
            }

            var range = 5;
            var leftId = chromatogram.SearchLeftEdge(Math.Max(minId - 1, 0), Math.Max(minId - range, 0));
            var rightId = chromatogram.SearchRightEdge(Math.Min(minId + 1, chromatogram.Length - 1), Math.Min(minId + range, chromatogram.Length - 1));

            return (id: minId, leftId, rightId);
        }
        else {
            var margin = 2;

            var leftId = chromatogram.SearchLeftEdgeHard(Math.Max(nearestTopId - margin, 0), 0);
            var rightId = chromatogram.SearchRightEdgeHard(Math.Min(nearestTopId + margin, chromatogram.Length - 1), chromatogram.Length - 1);

            if (!_isForceInsert && (nearestTopId - leftId < 2 || rightId - nearestTopId < 2)) {
                return (-1, -1, -1);
            }

            int id = nearestTopId;
            for(int i = leftId + 1; i <= rightId - 1; i++) {
                if (chromatogram.IsPeakTop(i) && chromatogram.IntensityDifference(id, i) < 0d) {
                    id = i;
                }
            }
            return (id, leftId, rightId);
        }
    }

    private static void SetAlignmentChromPeakFeature(AlignmentChromPeakFeature result, ChromXs center, Chromatogram chromatogram, int id, int leftId, int rightId) {
        double peakAreaAboveZero = 0d;
        for (int i = leftId; i < rightId; i++) {
            peakAreaAboveZero += chromatogram.CalculateArea(i, i + 1);
        }
        peakAreaAboveZero *= 60d;

        var baseline = chromatogram.CalculateArea(leftId, rightId) * 60d;

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

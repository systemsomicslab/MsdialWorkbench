using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialGcMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Algorithm.Alignment;

public abstract class GcmsGapFiller : IGapFiller
{
    private readonly MsdialGcmsParameter _parameter;
    protected readonly SmoothingMethod _smoothingMethod;
    protected readonly int _smoothingLevel;
    protected readonly bool _isForceInsert; 

    protected abstract double AxTol { get; }

    private int Bin => _parameter?.AccuracyType == AccuracyType.IsAccurate ? 2 : 0;

    public GcmsGapFiller(MsdialGcmsParameter parameter) {
        _parameter = parameter;

        _smoothingMethod = parameter.SmoothingMethod;
        _smoothingLevel = parameter.SmoothingLevel;
        _isForceInsert = parameter.IsForceInsertForGapFilling;
    }

    public bool NeedsGapFill(AlignmentSpotProperty spot, AnalysisFileBean analysisFile) {
        var peak = spot.AlignedPeakProperties.First(p => p.FileID == analysisFile.AnalysisFileId);
        return peak.Mass != spot.QuantMass;
    }

    public void GapFill(Ms1Spectra ms1Spectra, RawSpectra rawSpectra, IReadOnlyList<RawSpectrum> spectra, AlignmentSpotProperty spot, int fileID) {
        var peaks = spot.AlignedPeakProperties;
        var joined = GetJoinedPeaks(peaks);

        var target = peaks.First(peak => peak?.FileID == fileID);
        target.PeakShape.EstimatedNoise = GetEstimatedNoise(joined);

        var located = GetLocatedPeaks(joined);
        var chromXCenter = GetCenter(spot, located);
        var peakWidth = GetPeakWidth(located);
        var peaklist = GetPeaks(rawSpectra, chromXCenter, peakWidth, fileID, _smoothingMethod, _smoothingLevel);
        target.Mass = spot.QuantMass;
        GapFillCore(peaklist, chromXCenter, AxTol, target);
        UpdateDetection(spot, target, fileID);
    }

    public void UpdateQuantMass(RawSpectra rawSpectra, AlignmentSpotProperty spot, int fileID) {
        var peaks = spot.AlignedPeakProperties;
        var joined = GetJoinedPeaks(peaks);

        var target = peaks.First(peak => peak?.FileID == fileID);

        var located = GetLocatedPeaks(joined);
        var chromXCenter = GetCenter(spot, located);
        var peakWidth = GetPeakWidth(located);
        var peaklist = GetPeaks(rawSpectra, chromXCenter, peakWidth, fileID, _smoothingMethod, _smoothingLevel);
        target.Mass = spot.QuantMass;
        GapFillCore(peaklist, chromXCenter, AxTol, target);
        UpdateDetection(spot, target, fileID);
    }

    /// <summary>
    /// The peaks joined to the spot from their own file, whether or not they currently count as detected (see
    /// <see cref="UpdateDetection"/>). The center, width and noise used to re-extract a file are taken from this set, at
    /// alignment and at every later re-quantification alike, so that whether one file counts as detected never moves
    /// the values of the others.
    /// </summary>
    private static AlignmentChromPeakFeature[] GetJoinedPeaks(IEnumerable<AlignmentChromPeakFeature> peaks) {
        return peaks.Where(peak => peak.MasterPeakID >= 0).ToArray();
    }

    /// <summary>
    /// The joined peaks that the center and width are taken from: those that still have a position. A peak whose
    /// re-extraction found no peak (an empty chromatogram, or, when gap filling is not forced, no local maximum near
    /// the center) was reset to RT and RI -1 (<see cref="SetDefaultValueToAlignmentChromPeakFeature"/>). Averaged in,
    /// it would pull the center away from every other file, so that the files re-extracted after it could miss their
    /// peak and be reset in turn. When no joined peak has a position, all of them are used, as before.
    /// </summary>
    private static AlignmentChromPeakFeature[] GetLocatedPeaks(AlignmentChromPeakFeature[] joined) {
        var located = joined.Where(peak => peak.ChromXsTop.RT.Value >= 0).ToArray();
        return located.Length > 0 ? located : joined;
    }

    /// <summary>
    /// Decides whether a peak joined to the spot counts as detected at the spot's current quant mass.
    /// </summary>
    /// <remarks>
    /// A peak detected in its file joins the spot on its EI spectrum compared at nominal mass, but it is quantified on
    /// the spot's quant mass within the centroid tolerance. With accurate-mass data the file's peak may carry no ion
    /// there, and the re-extraction then gives no signal. Such a peak counts as not detected, like a gap-filled peak
    /// (PeakID -2), so that it does not raise the fill percentage or lower the spot's height statistics. MasterPeakID
    /// keeps the link to the file's own peak (its deconvoluted spectrum and annotation), and it records that the file
    /// had a joined peak, so the decision is made again on every re-quantification: a peak demoted at one quant mass is
    /// restored at another that it carries, and the reverse. A gap-filled cell (MasterPeakID below 0) is left as it is,
    /// and so is the representative file's peak: the spot takes its name, spectrum and annotation from it.
    /// </remarks>
    private static void UpdateDetection(AlignmentSpotProperty spot, AlignmentChromPeakFeature target, int fileID) {
        if (target.MasterPeakID < 0 || spot.RepresentativeFileID == fileID) {
            return;
        }
        target.PeakID = target.PeakHeightTop > 0 ? target.MasterPeakID : -2;
    }

    protected abstract ChromXs GetCenter(AlignmentSpotProperty spot, IEnumerable<AlignmentChromPeakFeature> peaks); // TODO: change this to run only once per spot
    protected abstract double GetPeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks);
    protected abstract List<ChromatogramPeak> GetPeaks(RawSpectra rawSpectra, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel);

    private float GetEstimatedNoise(IEnumerable<AlignmentChromPeakFeature> peaks) {
        return peaks.Max(n => n.PeakShape.EstimatedNoise);
    }

    private void GapFillCore(List<ChromatogramPeak> peaklist, ChromXs center, double axTol, AlignmentChromPeakFeature alignmentChromPeakFeature) {
        if (peaklist is null || peaklist.Count == 0) {
            SetDefaultValueToAlignmentChromPeakFeature(alignmentChromPeakFeature, center.Mz.Value);
            return;
        }
        var result = alignmentChromPeakFeature;
        if (result.MasterPeakID < 0) {
            result.MasterPeakID = -2;
            result.PeakID = -2;
        }

        var centralAx = center.Value;
        (var candidates, var minId) = GetPeakTopCandidates(peaklist, centralAx, axTol);

        var isForceInsert = this._isForceInsert;
        (var id, var leftId, var rightId) = GetPeakRange(candidates, peaklist, minId, centralAx, isForceInsert);
        if (id == -1 || leftId == -1 || rightId == -1) {
            SetDefaultValueToAlignmentChromPeakFeature(result, center.Mz.Value);
            return;
        }

        SetAlignmentChromPeakFeature(result, center, peaklist, id, leftId, rightId);
    }

    private (List<(ChromatogramPeak, int)>, int) GetPeakTopCandidates(List<ChromatogramPeak> sPeaklist, double centralAx, double axTol) {
        var candidates = new List<(ChromatogramPeak, int)>();
        var minId = -1;
        var minDiff = double.MaxValue;

        var start = sPeaklist.LowerBound(centralAx - axTol, (a, b) => a.ChromXs.Value.CompareTo(b));
        for (int i = start; i < sPeaklist.Count; i++) {
            if (i - 2 < 0 || i + 2 >= sPeaklist.Count) continue;
            if (sPeaklist[i].ChromXs.Value < centralAx - axTol) continue;
            if (centralAx + axTol < sPeaklist[i].ChromXs.Value) break;

            if (   sPeaklist[i-2].Intensity <= sPeaklist[i-1].Intensity && sPeaklist[i-1].Intensity <= sPeaklist[i].Intensity && sPeaklist[i].Intensity > sPeaklist[i+1].Intensity
                || sPeaklist[i-1].Intensity < sPeaklist[i].Intensity && sPeaklist[i].Intensity >= sPeaklist[i+1].Intensity && sPeaklist[i+1].Intensity >= sPeaklist[i+2].Intensity) {
                candidates.Add((sPeaklist[i], i));
            }

            var diff = Math.Abs(sPeaklist[i].ChromXs.Value - centralAx);
            if (diff < minDiff) {
                minDiff = diff;
                minId = i;
            }
        }

        if (minId == -1) minId = sPeaklist.Count / 2;

        return (candidates, minId);
    }

    private (int, int, int) GetPeakRange(List<(ChromatogramPeak Peak, int Index)> candidates, List<ChromatogramPeak> sPeaklist, int minId, double centralAx, bool isForceInsert) {
        int id, leftId, rightId;

        if (candidates.Count == 0) {
            if (!isForceInsert) return (-1, -1, -1);
            var range = 5;

            id = minId;
            leftId = Math.Max(id - 1, 0);
            rightId = Math.Min(id + 1, sPeaklist.Count - 1);

            var limit = Math.Max(id - range, 0);
            while (limit < leftId) {
                if (sPeaklist[leftId - 1].Intensity > sPeaklist[leftId].Intensity) break;
                --leftId;
            }
            limit = Math.Min(id + range, sPeaklist.Count - 1);
            while (rightId < limit) {
                if (sPeaklist[rightId].Intensity < sPeaklist[rightId + 1].Intensity) break;
                ++rightId;
            }
        }
        else {
            id = candidates.Argmin(cand => Math.Abs(cand.Peak.ChromXs.Value - centralAx)).Index;

            var margin = 2;

            leftId = Math.Max(id - margin, 0);
            while (0 < leftId) {
                if (sPeaklist[leftId - 1].Intensity >= sPeaklist[leftId].Intensity) break;
                --leftId;
            }

            rightId = Math.Min(id + margin, sPeaklist.Count - 1);
            while (rightId < sPeaklist.Count - 1) {
                if (sPeaklist[rightId].Intensity <= sPeaklist[rightId + 1].Intensity) break;
                ++rightId;
            }

            if (!isForceInsert && (id - leftId < 2 || rightId - id < 2)) return (-1, -1, -1);

            for(int i = leftId + 1; i <= rightId - 1; i++) {
                if (sPeaklist[i - 1].Intensity <= sPeaklist[i].Intensity && sPeaklist[i].Intensity > sPeaklist[i + 1].Intensity
                    || sPeaklist[i - 1].Intensity < sPeaklist[i].Intensity && sPeaklist[i].Intensity >= sPeaklist[i + 1].Intensity) {
                    if (sPeaklist[id].Intensity < sPeaklist[i].Intensity) {
                        id = i;
                    }
                }
            }
        }

        return (id, leftId, rightId);
    }

    private static void SetAlignmentChromPeakFeature(AlignmentChromPeakFeature result, ChromXs center, List<ChromatogramPeak> sPeaklist, int id, int leftId, int rightId) {
        double peakAreaAboveZero = 0d, baseline = 0d;
        if (center.Type == ChromXType.RI || center.Type == ChromXType.RT) {
            for (int i = leftId; i < rightId; i++) {
                peakAreaAboveZero += (sPeaklist[i].Intensity + sPeaklist[i + 1].Intensity) / 2 * (sPeaklist[i + 1].ChromXs.RT.Value - sPeaklist[i].ChromXs.RT.Value);
            }
            peakAreaAboveZero *= 60d;

            baseline = (sPeaklist[leftId].Intensity + sPeaklist[rightId].Intensity) * (sPeaklist[rightId].ChromXs.RT.Value - sPeaklist[leftId].ChromXs.RT.Value) / 2;
            baseline *= 60d;
        }
        else {
            for (int i = leftId; i < rightId; i++) {
                peakAreaAboveZero += (sPeaklist[i].Intensity + sPeaklist[i + 1].Intensity) / 2 * (sPeaklist[i + 1].ChromXs.Value - sPeaklist[i].ChromXs.Value);
            }
            baseline = (sPeaklist[leftId].Intensity + sPeaklist[rightId].Intensity) * (sPeaklist[rightId].ChromXs.Value - sPeaklist[leftId].ChromXs.Value) / 2;
        }

        System.Diagnostics.Debug.Assert(sPeaklist[id].ChromXs != null);
        System.Diagnostics.Debug.Assert(sPeaklist[leftId].ChromXs != null);
        System.Diagnostics.Debug.Assert(sPeaklist[rightId].ChromXs != null);

        if (result.MasterPeakID < 0) {
            result.Mass = center.Mz.Value;
        }
        result.ChromScanIdTop = id;
        result.ChromScanIdLeft = leftId;
        result.ChromScanIdRight = rightId;
        result.ChromXsTop = sPeaklist[id].ChromXs;
        result.ChromXsLeft = sPeaklist[leftId].ChromXs;
        result.ChromXsRight = sPeaklist[rightId].ChromXs;
        result.PeakHeightTop = sPeaklist[id].Intensity;
        result.PeakHeightLeft = sPeaklist[leftId].Intensity;
        result.PeakHeightRight = sPeaklist[rightId].Intensity;
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

public class GcmsRTGapFiller : GcmsGapFiller
{
    private readonly MsdialGcmsParameter _parameter;


    private readonly double _rtTol;
    private readonly Dictionary<int, RetentionIndexHandler> _fileIdToHandler;
    protected override double AxTol => _rtTol;

    public GcmsRTGapFiller(MsdialGcmsParameter parameter) : base(parameter) {
        _parameter = parameter;
        _rtTol = parameter.RetentionTimeAlignmentTolerance;
        _fileIdToHandler = parameter.GetRIHandlers();
    }

    protected override ChromXs GetCenter(AlignmentSpotProperty spot, IEnumerable<AlignmentChromPeakFeature> peaks) {
        var peaklist = peaks as AlignmentChromPeakFeature[] ?? peaks.ToArray();
        return new ChromXs(peaklist.Average(peak => peak.ChromXsTop.RT.Value), ChromXType.RT, ChromXUnit.Min)
        {
            RI = new RetentionIndex(peaklist.Average(peak => peak.ChromXsTop.RI.Value)),
            Mz = new MzValue(spot.QuantMass),
        };
    }

    protected override double GetPeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks) {
        return peaks.Max(peak => peak.PeakWidth(ChromXType.RT));
    }

    /// <summary>
    /// peak width is RT range
    /// </summary>
    /// <param name="center"></param>
    /// <param name="peakWidth"></param>
    /// <param name="spectrumList"></param>
    /// <param name="fileID"></param>
    /// <returns></returns>
    protected override List<ChromatogramPeak> GetPeaks(RawSpectra rawSpectra, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {
        var centralMz = center.Mz.Value;
        // RT conversion
        var centralRT = center.RT;
        var maxRt = centralRT + peakWidth * 2d; // temp
        var minRt = centralRT - peakWidth * 2d; // temp

        var range = ChromatogramRange.FromTimes(minRt, maxRt);
        using (var chromatogram = rawSpectra.GetMS1ExtractedChromatogram(new MzRange(centralMz, _parameter.PeakPickBaseParam.CentroidMs1Tolerance), range))
        using (Chromatogram smoothed = chromatogram.ChromatogramSmoothing(smoothingMethod, smoothingLevel)) {
            var peaks = smoothed.AsPeakArray();

            if (_fileIdToHandler.TryGetValue(fileID, out var riHandler)) {
                // RT conversion
                foreach (var peak in peaks) {
                    peak.ChromXs.RI = riHandler.Convert(peak.ChromXs.RT);
                }
            }
            return peaks;
        }
    }
}

public class GcmsRIGapFiller : GcmsGapFiller
{
    private readonly MsdialGcmsParameter _parameter;

    private readonly double _riTol;
    private readonly Dictionary<int, RetentionIndexHandler> _fileIdToHandler;

    protected override double AxTol => _riTol;

    public GcmsRIGapFiller(MsdialGcmsParameter parameter) : base(parameter) {
        _parameter = parameter;
        _riTol = parameter.RetentionIndexAlignmentTolerance;
        _fileIdToHandler = parameter.GetRIHandlers();
    }

    protected override ChromXs GetCenter(AlignmentSpotProperty spot, IEnumerable<AlignmentChromPeakFeature> peaks) {
        var peaklist = peaks as AlignmentChromPeakFeature[] ?? peaks.ToArray();
        return new ChromXs(peaklist.Average(peak => peak.ChromXsTop.RI.Value), ChromXType.RI, ChromXUnit.None)
        {
            RT = new RetentionTime(peaklist.Average(peak => peak.ChromXsTop.RT.Value)),
            Mz = new MzValue(spot.QuantMass),
        };
    }

    protected override double GetPeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks) {
        return peaks.Max(peak => peak.PeakWidth(ChromXType.RI));
    }

    /// <summary>
    /// peak width is RI range
    /// </summary>
    /// <param name="rawSpectra"></param>
    /// <param name="center"></param>
    /// <param name="peakWidth"></param>
    /// <param name="fileID"></param>
    /// <param name="smoothingMethod"></param>
    /// <param name="smoothingLevel"></param>
    /// <returns></returns>
    protected override List<ChromatogramPeak> GetPeaks(RawSpectra rawSpectra, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {
        var centralMz = center.Mz.Value;
        // RT conversion
        var riHandler = _fileIdToHandler[fileID];
        var maxRt = riHandler.ConvertBack(center.RI + peakWidth * 2d); // temp
        var minRt = riHandler.ConvertBack(center.RI - peakWidth * 2d); // temp

        var range = ChromatogramRange.FromTimes(minRt, maxRt);
        using (var chromatogram = rawSpectra.GetMS1ExtractedChromatogram(new MzRange(centralMz, _parameter.PeakPickBaseParam.CentroidMs1Tolerance), range))
        using (Chromatogram smoothed = chromatogram.ChromatogramSmoothing(smoothingMethod, smoothingLevel)) {
            var peaks = smoothed.AsPeakArray();
            foreach (var peak in peaks) {
                peak.ChromXs.RI = riHandler.Convert(peak.ChromXs.RT);
                peak.ChromXs.MainType = ChromXType.RI;
            }
            return peaks;
        }
    }
}

using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment;

/// <summary>
/// The RT axes of one alignment result: whether its aligned RTs are on another axis than the
/// per-file RTs, and how to move a peak's RTs from one to the other.
/// </summary>
/// <remarks>
/// <para>
/// When the automatic alignment RT correction ran, a per-file peak list, the raw data and the
/// chromatograms kept for each spot are on each file's measured RT axis, while the aligned peaks
/// and the spot RT are on the reference file's axis. The map the alignment used is saved beside
/// the result (<see cref="AlignmentRetentionTimeCorrectionMapFile"/>) and read here.
/// </para>
/// <para>
/// A result without that file whose project says the correction ran (made by a Console or GUI
/// that did not save it) is moved peak by peak through the peak's stored measured bounds instead
/// (<see cref="AlignedPeakRtAxisSource.StoredPeakBounds"/>); a result of a project without the
/// correction is <see cref="NotCorrected"/>, and every caller keeps its old behaviour.
/// </para>
/// </remarks>
public sealed class AlignmentRtAxis {
    private AlignmentRtAxis(bool isCorrected, AlignmentRetentionTimeCorrectionCollection? correction, string referenceFileName, string note) {
        IsCorrected = isCorrected;
        Correction = correction;
        ReferenceFileName = referenceFileName;
        Note = note;
    }

    public static AlignmentRtAxis NotCorrected { get; } = new AlignmentRtAxis(false, null, string.Empty, string.Empty);

    /// <summary>True when the aligned RTs may lie on another axis than the per-file RTs.</summary>
    public bool IsCorrected { get; }

    /// <summary>The maps the alignment used, when they were saved with it.</summary>
    public AlignmentRetentionTimeCorrectionCollection? Correction { get; }

    /// <summary>The reference file's name, when known.</summary>
    public string ReferenceFileName { get; }

    /// <summary>What a person should know about the axes of this result; empty when nothing.</summary>
    public string Note { get; }

    /// <summary>
    /// The axes of an alignment result: the saved map when there is one, else, when
    /// <paramref name="correctionRequested"/> (the project's "Execute automatic RT correction"),
    /// the peaks' stored bounds, else <see cref="NotCorrected"/>.
    /// </summary>
    public static AlignmentRtAxis Load(AlignmentFileBean alignmentFile, IReadOnlyList<AnalysisFileBean> files, bool correctionRequested) {
        if (alignmentFile is null) throw new ArgumentNullException(nameof(alignmentFile));
        var path = AlignmentRetentionTimeCorrectionMapFile.PathFor(alignmentFile);
        string? problem = null;
        if (File.Exists(path)) {
            try {
                return FromCorrection(AlignmentRetentionTimeCorrectionMapFile.Read(path), files);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException) {
                problem = $"The RT correction map {Path.GetFileName(path)} could not be read ({ex.Message}).";
            }
        }
        if (problem is null && !correctionRequested) {
            return NotCorrected;
        }
        var note = (problem is null ? $"No RT correction map ({Path.GetFileName(path)}) was saved with this alignment." : problem)
            + " Each peak is moved between the measured and the aligned RT axis through its own stored bounds; a peak edited by hand cannot be, and is not written.";
        return new AlignmentRtAxis(true, null, string.Empty, note);
    }

    /// <summary>The axes of a result aligned with <paramref name="correction"/>.</summary>
    public static AlignmentRtAxis FromCorrection(AlignmentRetentionTimeCorrectionCollection correction, IReadOnlyList<AnalysisFileBean>? files) {
        if (correction is null) throw new ArgumentNullException(nameof(correction));
        var reference = files?.FirstOrDefault(file => file.AnalysisFileId == correction.ReferenceFileId)?.AnalysisFileName ?? string.Empty;
        return new AlignmentRtAxis(true, correction, reference, string.Empty);
    }

    /// <summary>
    /// The map of one aligned peak. <paramref name="storedPeakInfo"/> is the peak's entry in the
    /// spot's chromatogram file, whose bounds are on the measured axis.
    /// </summary>
    public AlignedPeakRtAxisMap ForPeak(int fileId, ChromatogramPeakInfo? storedPeakInfo, double alignedLeft, double alignedTop, double alignedRight, bool isManuallyModified) {
        if (!IsCorrected) {
            return AlignedPeakRtAxisMap.NotCorrected;
        }
        if (Correction is not null) {
            return AlignedPeakRtAxisMap.FromCorrection(Correction, fileId);
        }
        if (storedPeakInfo is null) {
            return AlignedPeakRtAxisMap.Unavailable("The peak's measured bounds are not stored, so its aligned RT cannot be moved onto the measured axis.");
        }
        double Value(Common.Components.ChromXs? chromXs) => chromXs?.RT?.Value ?? double.NaN;
        var originalTop = Value(storedPeakInfo.ChromXsTop);
        if (isManuallyModified) {
            return AlignedPeakRtAxisMap.Unavailable(
                "This peak was edited by hand and no RT correction map was saved with the alignment, so where it lies on the measured axis cannot be told; it is shown at its stored bounds and is not written.",
                alignedTop - originalTop);
        }
        return AlignedPeakRtAxisMap.FromStoredBounds(
            Value(storedPeakInfo.ChromXsLeft), originalTop, Value(storedPeakInfo.ChromXsRight),
            alignedLeft, alignedTop, alignedRight);
    }

    /// <summary>
    /// The maps of one spot's aligned peaks, keyed by file ID. <paramref name="storedPeakInfos"/> are
    /// the spot's entries in its chromatogram file (any order); <paramref name="alignedPeaks"/> its
    /// aligned peaks as they are now.
    /// </summary>
    public AlignedSpotRtAxes ForSpot(IEnumerable<ChromatogramPeakInfo>? storedPeakInfos, IEnumerable<AlignmentChromPeakFeature>? alignedPeaks) {
        return ForSpot(storedPeakInfos, (alignedPeaks ?? Enumerable.Empty<AlignmentChromPeakFeature>()).Select(peak => (
            peak.FileID,
            peak.ChromXsLeft?.RT?.Value ?? double.NaN,
            peak.ChromXsTop?.RT?.Value ?? double.NaN,
            peak.ChromXsRight?.RT?.Value ?? double.NaN,
            peak.IsManuallyModifiedForQuant)));
    }

    /// <summary>
    /// <see cref="ForSpot(IEnumerable{ChromatogramPeakInfo}?, IEnumerable{AlignmentChromPeakFeature}?)"/>
    /// for aligned peaks given by their file ID, aligned RT bounds and whether they were edited by hand.
    /// </summary>
    public AlignedSpotRtAxes ForSpot(
        IEnumerable<ChromatogramPeakInfo>? storedPeakInfos,
        IEnumerable<(int FileId, double AlignedLeft, double AlignedTop, double AlignedRight, bool IsManuallyModified)> alignedPeaks) {
        if (!IsCorrected) {
            return AlignedSpotRtAxes.NotCorrected;
        }
        var infos = (storedPeakInfos ?? Enumerable.Empty<ChromatogramPeakInfo>())
            .Where(info => info is not null)
            .GroupBy(info => info.FileID)
            .ToDictionary(group => group.Key, group => group.First());
        var maps = new Dictionary<int, AlignedPeakRtAxisMap>();
        foreach (var peak in alignedPeaks) {
            infos.TryGetValue(peak.FileId, out var info);
            maps[peak.FileId] = ForPeak(peak.FileId, info, peak.AlignedLeft, peak.AlignedTop, peak.AlignedRight, peak.IsManuallyModified);
        }
        return new AlignedSpotRtAxes(this, maps);
    }

    /// <summary>
    /// The measured RT of an aligned RT of a file, through the saved map; null without one. For
    /// exports that name the measured RT beside the aligned one.
    /// </summary>
    public double? ToOriginal(int fileId, double alignedRt) {
        return Correction is null || alignedRt < 0d ? null : Correction.Restore(fileId, alignedRt);
    }

    /// <summary>
    /// The note an export carries when its RTs are aligned RTs on another axis than the per-file
    /// RTs; null when they are not. Plain ASCII, without tabs or commas, so it fits a header cell
    /// and an mzTab-M parameter value.
    /// </summary>
    public string? ExportNote() {
        if (!IsCorrected) {
            return null;
        }
        var reference = string.IsNullOrEmpty(ReferenceFileName)
            ? (Correction is null ? "the reference file" : $"reference file ID {Correction.ReferenceFileId}")
            : $"reference file {ReferenceFileName}";
        var text = $"RT axis: aligned RTs are on the RT axis of {reference} (automatic alignment RT correction); per-file peak lists and RT-match evidence are on each file's measured RT axis";
        var builder = new System.Text.StringBuilder(text.Length);
        foreach (var c in text) {
            builder.Append(c is '\t' or '\r' or '\n' or ',' ? ' ' : (c < 32 || c > 126 ? '?' : c));
        }
        return builder.ToString();
    }
}

/// <summary>The RT axis maps of one spot's aligned peaks.</summary>
public sealed class AlignedSpotRtAxes {
    private readonly IReadOnlyDictionary<int, AlignedPeakRtAxisMap> _maps;

    public AlignedSpotRtAxes(AlignmentRtAxis axis, IReadOnlyDictionary<int, AlignedPeakRtAxisMap> maps) {
        Axis = axis ?? throw new ArgumentNullException(nameof(axis));
        _maps = maps ?? throw new ArgumentNullException(nameof(maps));
    }

    public static AlignedSpotRtAxes NotCorrected { get; } = new AlignedSpotRtAxes(AlignmentRtAxis.NotCorrected, new Dictionary<int, AlignedPeakRtAxisMap>());

    public AlignmentRtAxis Axis { get; }

    /// <summary>True when the spot's aligned RTs may lie on another axis than its files' measured RTs.</summary>
    public bool IsCorrected => Axis.IsCorrected;

    /// <summary>The map of the file's aligned peak.</summary>
    public AlignedPeakRtAxisMap For(int fileId) {
        if (_maps.TryGetValue(fileId, out var map)) {
            return map;
        }
        return Axis.IsCorrected
            ? AlignedPeakRtAxisMap.Unavailable("The aligned peak of this file was not loaded with the spot.")
            : AlignedPeakRtAxisMap.NotCorrected;
    }
}

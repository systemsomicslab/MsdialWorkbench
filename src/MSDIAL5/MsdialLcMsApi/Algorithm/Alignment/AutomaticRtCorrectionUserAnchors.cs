using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment;

/// <summary>Whether an anchor was chosen from the detected peaks or named in the anchor library.</summary>
public enum AutomaticRtCorrectionAnchorSource {
    Automatic,
    User,
}

/// <summary>
/// The peak a user anchor was detected as in one file, on that file's original RT axis.
/// </summary>
public sealed class AutomaticRtCorrectionUserAnchorObservation {
    public AutomaticRtCorrectionUserAnchorObservation(double rt, double mz, double height) {
        Rt = rt;
        Mz = mz;
        Height = height;
    }

    public double Rt { get; }
    public double Mz { get; }
    public double Height { get; }
}

/// <summary>
/// What the automatic RT correction reads from one file's raw data, in one reading: the MS1 scan
/// times and the peak each user anchor was detected as.
/// </summary>
public sealed class AutomaticRtCorrectionRawFileReading {
    public AutomaticRtCorrectionRawFileReading(
        Ms1CycleProfile cycleProfile,
        IReadOnlyList<AutomaticRtCorrectionUserAnchorObservation?> userAnchorObservations,
        IReadOnlyList<string>? userAnchorDetectionNotes = null) {
        CycleProfile = cycleProfile ?? Ms1CycleProfile.Empty;
        UserAnchorObservations = userAnchorObservations ?? throw new ArgumentNullException(nameof(userAnchorObservations));
        if (userAnchorDetectionNotes is not null && userAnchorDetectionNotes.Count != userAnchorObservations.Count) {
            throw new ArgumentException("One detection note is needed per user anchor.", nameof(userAnchorDetectionNotes));
        }
        UserAnchorDetectionNotes = userAnchorDetectionNotes;
    }

    public Ms1CycleProfile CycleProfile { get; }

    /// <summary>One entry per user anchor, in the order they were given; null when it was not detected.</summary>
    public IReadOnlyList<AutomaticRtCorrectionUserAnchorObservation?> UserAnchorObservations { get; }

    /// <summary>
    /// One entry per user anchor: how its peak was chosen, or why none was (see
    /// <see cref="AutomaticRtCorrectionUserAnchorDetection.Notes"/>); null when not recorded.
    /// </summary>
    public IReadOnlyList<string>? UserAnchorDetectionNotes { get; }
}

/// <summary>
/// The anchor library of the automatic alignment RT correction: the seven-column text of the
/// user-defined RT correction (Name, RT, RT tolerance, m/z, m/z tolerance, minimum height, T/F).
/// </summary>
/// <remarks>
/// The columns mean what they mean there, and one more thing: in Hybrid mode a False row is an
/// exclusion. The user-defined correction skips a False row; here it also keeps every automatic
/// candidate within the row's m/z and RT tolerances out of the selection, so that a candidate a
/// person rejected on review is not chosen again by the next run. Its RT is read on the
/// reference file's axis, the axis the review showed.
/// <para>
/// Because a False row now does the opposite of a True row, the library is read more strictly
/// than the user-defined correction reads it: the first line is the header; every other line
/// that is not blank needs all seven columns; the numbers are read with the invariant culture
/// (a decimal point, as the audit writes them); and the seventh column must say True, False, T
/// or F, in any letter case. The user-defined correction's reader takes anything else as True
/// and stops at the first blank line, which would turn a mistyped exclusion into a forced
/// anchor and drop every row after a blank line without a word.
/// </para>
/// </remarks>
public static class AutomaticRtCorrectionAnchorLibrary {
    /// <summary>Every row of the library, True and False, in file order; refused with the line at fault.</summary>
    public static List<MoleculeMsReference> Load(string path) {
        if (path.IsEmptyOrNull()) {
            throw new InvalidDataException("Automatic RT correction anchor library file path is required when the anchor mode is UserOnly or Hybrid.");
        }
        if (!File.Exists(path)) {
            throw new FileNotFoundException("The automatic RT correction anchor library was not found.", path);
        }
        return Parse(File.ReadAllLines(path, Encoding.UTF8));
    }

    /// <summary>The rows of a library's lines, as <see cref="Load(string)"/> reads them.</summary>
    public static List<MoleculeMsReference> Parse(IReadOnlyList<string> lines) {
        if (lines is null) throw new ArgumentNullException(nameof(lines));
        var header = lines.Select((text, index) => (Text: text, Number: index + 1))
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line.Text));
        if (header.Text is null) {
            throw new InvalidDataException("The anchor library is empty.");
        }
        var headerCells = header.Text.Split('\t');
        if (headerCells.Length >= 2 && double.TryParse(headerCells[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _)) {
            throw new InvalidDataException(
                $"Anchor library line {header.Number} looks like a row, not the header. The first line names the columns: Name, RT(min), RT tol.(min), m/z, m/z tol., Minimum height, T/F.");
        }

        var rows = new List<MoleculeMsReference>();
        for (var index = header.Number; index < lines.Count; index++) {
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line)) {
                continue;
            }
            var number = index + 1;
            var cells = line.Split('\t');
            if (cells.Length < 7) {
                throw new InvalidDataException(
                    $"Anchor library line {number} has {cells.Length} column(s); each row needs seven: Name, RT(min), RT tol.(min), m/z, m/z tol., Minimum height, T/F.");
            }
            rows.Add(new MoleculeMsReference {
                ScanID = rows.Count,
                Name = cells[0].Trim(),
                ChromXs = new ChromXs(Number(cells[1], "RT", number), ChromXType.RT, ChromXUnit.Min),
                RetentionTimeTolerance = (float)Number(cells[2], "RT tolerance", number),
                PrecursorMz = Number(cells[3], "m/z", number),
                MassTolerance = (float)Number(cells[4], "m/z tolerance", number),
                MinimumPeakHeight = (float)Number(cells[5], "minimum height", number),
                IsTargetMolecule = Flag(cells[6], number),
            });
        }
        return rows;
    }

    private static double Number(string text, string what, int line) {
        if (double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            && !double.IsNaN(value) && !double.IsInfinity(value)) {
            return value;
        }
        throw new InvalidDataException($"Anchor library line {line}: '{text}' is not a number for {what}; write it with a decimal point.");
    }

    private static bool Flag(string text, int line) {
        switch (text.Trim().ToLowerInvariant()) {
            case "true":
            case "t":
                return true;
            case "false":
            case "f":
                return false;
            default:
                throw new InvalidDataException($"Anchor library line {line}: the T/F column says '{text}'; write True (an anchor) or False (an exclusion).");
        }
    }

    /// <summary>
    /// Refuses a library the anchor mode cannot use: UserOnly needs at least the minimum anchor
    /// count of True rows, Hybrid at least one row; every row it reads needs a positive m/z, m/z
    /// tolerance and RT tolerance, and a True row a minimum height of 0 or more. Automatic mode
    /// reads no library and accepts anything.
    /// </summary>
    public static void Check(IReadOnlyList<MoleculeMsReference> rows, AutomaticAlignmentRetentionTimeCorrectionParameter parameter) {
        if (rows is null) throw new ArgumentNullException(nameof(rows));
        if (parameter is null) throw new ArgumentNullException(nameof(parameter));
        switch (parameter.AnchorMode) {
            case AutomaticRtCorrectionAnchorMode.Automatic:
                return;
            case AutomaticRtCorrectionAnchorMode.UserOnly:
                var anchorCount = rows.Count(row => row.IsTargetMolecule);
                if (anchorCount < parameter.MinimumAnchorCount) {
                    throw new InvalidDataException(
                        $"The anchor library has {anchorCount} True row(s); UserOnly needs at least the minimum anchor count, {parameter.MinimumAnchorCount}.");
                }
                break;
            case AutomaticRtCorrectionAnchorMode.Hybrid:
                if (rows.Count == 0) {
                    throw new InvalidDataException("The anchor library has no rows.");
                }
                break;
            default:
                throw new InvalidDataException($"Unknown automatic RT correction anchor mode {parameter.AnchorMode}.");
        }
        foreach (var row in rows) {
            if (!row.IsTargetMolecule && parameter.AnchorMode == AutomaticRtCorrectionAnchorMode.UserOnly) {
                continue;
            }
            var what = $"Anchor library row {row.ScanID + 1} ({row.Name})";
            if (!(row.PrecursorMz > 0d)) throw new InvalidDataException($"{what}: m/z must be positive.");
            if (!(row.MassTolerance > 0d)) throw new InvalidDataException($"{what}: m/z tolerance must be positive.");
            if (!(row.RetentionTimeTolerance > 0d)) throw new InvalidDataException($"{what}: RT tolerance must be positive.");
            if (!(row.ChromXs.RT.Value >= 0d)) throw new InvalidDataException($"{what}: RT must be 0 or more.");
            if (row.IsTargetMolecule && !(row.MinimumPeakHeight >= 0d)) throw new InvalidDataException($"{what}: minimum height must be 0 or more.");
        }
    }

    /// <summary>The library's anchors, its True rows, in library order.</summary>
    public static List<MoleculeMsReference> Anchors(IReadOnlyList<MoleculeMsReference> rows) {
        return rows.Where(row => row.IsTargetMolecule).ToList();
    }

    /// <summary>The library's exclusions, its False rows, in library order.</summary>
    public static List<MoleculeMsReference> Exclusions(IReadOnlyList<MoleculeMsReference> rows) {
        return rows.Where(row => !row.IsTargetMolecule).ToList();
    }
}

/// <summary>What <see cref="AutomaticRtCorrectionUserAnchorDetector.Detect"/> found for each anchor it was given.</summary>
public sealed class AutomaticRtCorrectionUserAnchorDetection {
    public AutomaticRtCorrectionUserAnchorDetection(
        IReadOnlyList<AutomaticRtCorrectionUserAnchorObservation?> observations,
        IReadOnlyList<string> notes) {
        Observations = observations ?? throw new ArgumentNullException(nameof(observations));
        Notes = notes ?? throw new ArgumentNullException(nameof(notes));
    }

    /// <summary>The peak each anchor was detected as, in the order given; null when none was taken.</summary>
    public IReadOnlyList<AutomaticRtCorrectionUserAnchorObservation?> Observations { get; }

    /// <summary>
    /// For each anchor, how many peaks its window held and how one was chosen, or why none was:
    /// for a reviewer deciding whether to keep an anchor or change its tolerances.
    /// </summary>
    public IReadOnlyList<string> Notes { get; }
}

/// <summary>
/// Finds user anchors in a file's raw data, as the user-defined RT correction finds its anchors:
/// a peak is detected on the MS1 extracted ion chromatogram of the anchor's m/z and m/z
/// tolerance, and one peak within the anchor's RT tolerance and above its minimum height is
/// taken. The detected peak list of the run is not used, so an anchor below the run's minimum
/// peak height is still found.
/// </summary>
/// <remarks>
/// Each anchor is detected on its own, with its own minimum height as the detection amplitude,
/// so a row's result does not depend on the other rows of the library. (The user-defined
/// correction detects every anchor at the lowest minimum height in its library, which would let
/// an edit to one row change the peak another row takes between a preview and the run after it.)
/// Peaks at an m/z of the run's excluded mass list are not detected, as in the run itself.
/// </remarks>
public static class AutomaticRtCorrectionUserAnchorDetector {
    public static AutomaticRtCorrectionUserAnchorDetection Detect(
        AnalysisFileBean file,
        IDataProvider provider,
        ParameterBase parameter,
        IReadOnlyList<MoleculeMsReference> anchors,
        RetentionTimeCorrectionPeakSelectionMode selectionMode,
        double rtWeight) {
        if (file is null) throw new ArgumentNullException(nameof(file));
        if (provider is null) throw new ArgumentNullException(nameof(provider));
        if (parameter is null) throw new ArgumentNullException(nameof(parameter));
        if (anchors is null) throw new ArgumentNullException(nameof(anchors));
        var observations = new AutomaticRtCorrectionUserAnchorObservation?[anchors.Count];
        var notes = new string[anchors.Count];
        if (anchors.Count == 0) {
            return new AutomaticRtCorrectionUserAnchorDetection(observations, notes);
        }

        var peakSpotting = new PeakSpottingCore(parameter);
        var rawSpectra = new RawSpectra(provider, parameter.IonMode, file.AcquisitionType);
        var chromatogramRange = new ChromatogramRange(parameter.RetentionTimeBegin, parameter.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
        for (var index = 0; index < anchors.Count; index++) {
            var anchor = anchors[index];
            var detector = new PeakDetection(parameter.MinimumDatapoints, anchor.MinimumPeakHeight);
            using var chromatogram = rawSpectra.GetMS1ExtractedChromatogram(new MzRange(anchor.PrecursorMz, anchor.MassTolerance), chromatogramRange);
            var noSignal = chromatogram.IsEmpty || chromatogram.AsPeakArray().All(point => point.Intensity <= 0d);
            var peaks = peakSpotting.GetChromatogramPeakFeatures_Temp2(provider, detector, chromatogram, file.AcquisitionType);
            var selection = RetentionTimeCorrectionPeakSelector.Select(anchor, peaks, selectionMode, rtWeight);
            if (selection.SelectedPeak is { } peak) {
                observations[index] = new AutomaticRtCorrectionUserAnchorObservation(
                    peak.PeakFeature.ChromXsTop.RT.Value,
                    peak.PrecursorMz,
                    peak.PeakFeature.PeakHeightTop);
            }
            // The run drops a peak whose m/z lies within an excluded window. Whether the anchor's
            // peak was such a peak is known for certain only when the anchor's whole window lies
            // inside one; when the windows only overlap it may or may not have been.
            var excludedLists = parameter.ExcludedMassList ?? new List<CompMs.Common.Query.MzSearchQuery>();
            var inside = excludedLists.FirstOrDefault(query => Math.Abs(query.Mass - anchor.PrecursorMz) + anchor.MassTolerance <= query.MassTolerance);
            var overlapping = inside ?? excludedLists.FirstOrDefault(query => Math.Abs(query.Mass - anchor.PrecursorMz) < query.MassTolerance + anchor.MassTolerance);
            notes[index] = Note(selection, noSignal, inside is not null, overlapping?.Mass);
        }
        return new AutomaticRtCorrectionUserAnchorDetection(observations, notes);
    }

    private static string Note(RetentionTimeCorrectionPeakSelectionResult selection, bool noSignal, bool windowExcluded, double? overlappingExcludedMass) {
        var accepted = selection.Candidates.Count(candidate => candidate.IsAccepted);
        switch (selection.SelectedReason) {
            case RetentionTimeCorrectionPeakSelectionReason.SelectedSingleCandidate:
                return "The only peak in the window.";
            case RetentionTimeCorrectionPeakSelectionReason.SelectedByHighestPeakHeight:
                return $"The highest of {accepted} peaks in the window.";
            case RetentionTimeCorrectionPeakSelectionReason.SelectedByClosestReferenceRt:
                return $"The closest to the library RT of {accepted} peaks in the window.";
            case RetentionTimeCorrectionPeakSelectionReason.SelectedByWeightedScore:
                return $"The best height and RT score of {accepted} peaks in the window.";
        }
        if (selection.Candidates.Count == 0) {
            if (noSignal) {
                return "No signal at the anchor's m/z.";
            }
            if (windowExcluded) {
                return "The anchor's m/z window lies within an excluded mass, where no peak is detected.";
            }
            if (overlappingExcludedMass is { } excludedMass) {
                return string.Format(CultureInfo.InvariantCulture,
                    "No peak was found: its peak may have been dropped by the excluded mass {0}, or none rises above the minimum height.",
                    excludedMass);
            }
            // The detection amplitude is the anchor's own minimum height, so a lower peak is not
            // among the candidates at all.
            return "No peak on the extracted ion chromatogram rises above the minimum height.";
        }
        var nearest = selection.Candidates.OrderBy(candidate => candidate.RtDifference).First();
        var reasons = new List<string>();
        if (nearest.RejectReason.HasFlag(RetentionTimeCorrectionPeakRejectReason.RetentionTimeTolerance)) reasons.Add("outside the RT window");
        if (nearest.RejectReason.HasFlag(RetentionTimeCorrectionPeakRejectReason.MinimumPeakHeight)) reasons.Add("below the minimum height");
        if (nearest.RejectReason.HasFlag(RetentionTimeCorrectionPeakRejectReason.MassTolerance)) reasons.Add("outside the m/z tolerance");
        return string.Format(CultureInfo.InvariantCulture,
            "No peak in the window; the nearest, at {0:0.###} min and height {1:G6}, is {2}.",
            nearest.Peak.PeakFeature.ChromXsTop.RT.Value,
            nearest.Peak.PeakFeature.PeakHeightTop,
            string.Join(" and ", reasons));
    }
}

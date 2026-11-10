using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment;

/// <summary>
/// Where the automatic alignment RT correction says what it did: the Console writes
/// <see cref="Info"/> to its standard output and <see cref="Error"/> to its standard error; the
/// GUI collects both and shows them to the person.
/// </summary>
public interface IAutomaticRtCorrectionReporter {
    void Info(string line);
    void Error(string line);
}

/// <summary>A reporter that keeps every line, in order, for a caller that shows them later.</summary>
public sealed class CollectingAutomaticRtCorrectionReporter : IAutomaticRtCorrectionReporter {
    private readonly List<(bool IsError, string Line)> _lines = new();

    public IReadOnlyList<(bool IsError, string Line)> Lines => _lines;
    public IEnumerable<string> Errors => _lines.Where(line => line.IsError).Select(line => line.Line);
    public bool HasWarnings => _lines.Any(line => line.IsError || line.Line.StartsWith("Warning:", StringComparison.Ordinal));

    public void Info(string line) => _lines.Add((false, line));
    public void Error(string line) => _lines.Add((true, line));
}

/// <summary>What the automatic correction reads before any file is read: the anchor library and its identity.</summary>
public sealed class AutomaticRtCorrectionInputs {
    public AutomaticRtCorrectionInputs(IReadOnlyList<MoleculeMsReference> anchorLibrary, string anchorLibraryName, string anchorLibrarySha256) {
        AnchorLibrary = anchorLibrary ?? throw new ArgumentNullException(nameof(anchorLibrary));
        AnchorLibraryName = anchorLibraryName ?? string.Empty;
        AnchorLibrarySha256 = anchorLibrarySha256 ?? string.Empty;
    }

    /// <summary>Every row of the anchor library; empty in Automatic mode.</summary>
    public IReadOnlyList<MoleculeMsReference> AnchorLibrary { get; }
    /// <summary>The library's file name, for the audit; empty in Automatic mode.</summary>
    public string AnchorLibraryName { get; }
    /// <summary>The library's SHA-256, for the audit; empty in Automatic mode.</summary>
    public string AnchorLibrarySha256 { get; }

    public static AutomaticRtCorrectionInputs None { get; } = new AutomaticRtCorrectionInputs(Array.Empty<MoleculeMsReference>(), string.Empty, string.Empty);
}

/// <summary>
/// The automatic alignment RT correction as a run applies it, shared by the Console and the GUI so
/// that both check the same settings, read the same anchor library, learn the same maps and write
/// the same audit.
/// </summary>
/// <remarks>
/// This is the code the Console's LcmsProcess carried, moved without changing its logic. What is
/// Console-only stays there: "Together with alignment", the preview-only switch and the preview's
/// scratch folder.
/// </remarks>
public static class AutomaticAlignmentRtCorrectionWorkflow {
    /// <summary>
    /// The message with which a run that asks for both the user-defined RT correction and the
    /// automatic alignment RT correction is refused, or null when it does not ask for both.
    /// </summary>
    public static string? ExclusiveWithLegacyRtCorrection(ParameterBase parameter) {
        if (parameter.AlignmentBaseParam.AutomaticRtCorrection.Execute
            && parameter.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection) {
            return "User-defined RT correction and automatic alignment RT correction cannot be enabled together because that would correct the RT axis twice.";
        }
        return null;
    }

    /// <summary>
    /// Checks the settings and, in UserOnly and Hybrid mode, reads and checks the anchor library,
    /// says what it holds, and returns it with its identity; null, with the reason given to
    /// <paramref name="reporter"/>, when the run must be refused. When the correction is off, a
    /// library path left in place is warned about and <see cref="AutomaticRtCorrectionInputs.None"/>
    /// is returned.
    /// </summary>
    public static AutomaticRtCorrectionInputs? PrepareInputs(MsdialLcmsParameter parameter, IAutomaticRtCorrectionReporter reporter) {
        var correction = parameter.AlignmentBaseParam.AutomaticRtCorrection;
        var anchorLibraryPath = correction.AnchorLibraryFilePath;
        if (correction.Execute) {
            try {
                AutomaticAlignmentRetentionTimeCorrection.ValidateParameter(correction, parameter.Ms1AlignmentTolerance);
            }
            catch (ArgumentException ex) {
                reporter.Error($"Automatic alignment RT correction settings cannot be used: {ex.Message}");
                return null;
            }
        }
        if (correction.Execute && correction.AnchorMode != AutomaticRtCorrectionAnchorMode.Automatic) {
            List<MoleculeMsReference> library;
            try {
                library = AutomaticRtCorrectionAnchorLibrary.Load(anchorLibraryPath);
                AutomaticRtCorrectionAnchorLibrary.Check(library, correction);
            }
            catch (Exception ex) {
                reporter.Error($"Automatic RT correction anchor library could not be used: {ex.Message}");
                return null;
            }
            var (name, sha256) = ReportAnchorLibrary(parameter, anchorLibraryPath, library, reporter);
            return new AutomaticRtCorrectionInputs(library, name, sha256);
        }
        if (!anchorLibraryPath.IsEmptyOrNull()) {
            var reason = correction.Execute
                ? "the anchor mode is Automatic"
                : "'Execute automatic RT correction for alignment' is False";
            reporter.Info($"Warning: 'Automatic RT correction anchor library file path' is set ({anchorLibraryPath}) but {reason}, so the library is not used.");
        }
        return AutomaticRtCorrectionInputs.None;
    }

    /// <summary>
    /// Checks the settings and the anchor library as <see cref="PrepareInputs"/> does, without
    /// saying anything but the first reason to refuse them; null when they can be used. For a
    /// settings page that checks what the person typed before a run starts.
    /// </summary>
    public static string? CheckSettings(MsdialLcmsParameter parameter) {
        return CheckSettings(
            parameter.AlignmentBaseParam.AutomaticRtCorrection,
            parameter.Ms1AlignmentTolerance,
            parameter.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection);
    }

    /// <summary>
    /// <see cref="CheckSettings(MsdialLcmsParameter)"/> for settings not yet in a parameter: the
    /// correction's own, the MS1 tolerance for alignment, and whether the user-defined RT
    /// correction is on.
    /// </summary>
    public static string? CheckSettings(AutomaticAlignmentRetentionTimeCorrectionParameter correction, double ms1AlignmentTolerance, bool userDefinedRtCorrection) {
        if (correction is null || !correction.Execute) {
            return null;
        }
        if (userDefinedRtCorrection) {
            return "User-defined RT correction and automatic alignment RT correction cannot be enabled together because that would correct the RT axis twice.";
        }
        try {
            AutomaticAlignmentRetentionTimeCorrection.ValidateParameter(correction, ms1AlignmentTolerance);
        }
        catch (ArgumentException ex) {
            return $"Automatic alignment RT correction settings cannot be used: {ex.Message}";
        }
        if (correction.AnchorMode != AutomaticRtCorrectionAnchorMode.Automatic) {
            try {
                var library = AutomaticRtCorrectionAnchorLibrary.Load(correction.AnchorLibraryFilePath);
                AutomaticRtCorrectionAnchorLibrary.Check(library, correction);
            }
            catch (Exception ex) {
                return $"Automatic RT correction anchor library could not be used: {ex.Message}";
            }
        }
        return null;
    }

    /// <summary>
    /// One file's raw data, read once: its MS1 scan times (the outlier test's scale floor) and its
    /// user anchors, detected on the raw extracted ion chromatograms.
    /// </summary>
    public static AutomaticRtCorrectionRawFileReading ReadRawFile(
        AnalysisFileBean file,
        IDataProvider provider,
        MsdialLcmsParameter parameter,
        IReadOnlyList<MoleculeMsReference> anchors) {
        var correction = parameter.AlignmentBaseParam.AutomaticRtCorrection;
        var detection = AutomaticRtCorrectionUserAnchorDetector.Detect(
            file, provider, parameter, anchors, correction.AnchorPeakSelectionMode, correction.AnchorPeakSelectionRtWeight);
        return new AutomaticRtCorrectionRawFileReading(
            Ms1CycleProfile.FromSpectra(provider.LoadMsSpectrums(), parameter.IonMode),
            detection.Observations,
            detection.Notes);
    }

    /// <summary>
    /// Learns the automatic alignment RT correction and writes its audit to
    /// <paramref name="auditFolder"/>; null, with the reason given to <paramref name="reporter"/>,
    /// when it fails.
    /// </summary>
    /// <remarks>
    /// An earlier run's audit in the folder is removed first, so that a failure cannot leave it
    /// looking like this run's. When the correction fails after the raw data was read, what was
    /// read (each file, each user anchor's detection, the exclusions) is written as the audit, with
    /// the reason in every file's note: the person can see which anchor was not found where. The
    /// summary names the kind of run and the anchor library by file name and SHA-256.
    /// </remarks>
    public static AutomaticAlignmentRetentionTimeCorrectionResult? Build(
        IReadOnlyList<AnalysisFileBean> files,
        MsdialLcmsParameter parameter,
        AutomaticRtCorrectionInputs inputs,
        string auditFolder,
        Func<AnalysisFileBean, List<ChromatogramPeakFeature>> peakLoader,
        Func<AnalysisFileBean, IReadOnlyList<MoleculeMsReference>, AutomaticRtCorrectionRawFileReading> rawFileReader,
        string run,
        IAutomaticRtCorrectionReporter reporter) {
        try {
            AutomaticAlignmentRetentionTimeCorrectionResult.RemoveAudit(auditFolder);
            reporter.Info("Automatic alignment RT correction: selecting anchors after peak picking.");
            var result = AutomaticAlignmentRetentionTimeCorrection.Build(
                files,
                parameter.AlignmentBaseParam.AutomaticRtCorrection,
                parameter.Ms1AlignmentTolerance,
                peakLoader,
                inputs.AnchorLibrary,
                rawFileReader);
            result.WriteAudit(auditFolder, run, inputs.AnchorLibraryName, inputs.AnchorLibrarySha256);
            Report(result, reporter);
            return result;
        }
        catch (AutomaticRtCorrectionFailedException ex) {
            reporter.Error($"Automatic alignment RT correction failed: {ex.Message}");
            try {
                ex.PartialAudit.WriteAudit(auditFolder, run, inputs.AnchorLibraryName, inputs.AnchorLibrarySha256);
                reporter.Error($"What was read before it failed, each user anchor's detection in each file included, is in {AutomaticAlignmentRetentionTimeCorrectionResult.SummaryFileName} and {AutomaticAlignmentRetentionTimeCorrectionResult.AnchorsFileName}.");
            }
            catch (Exception writeError) {
                reporter.Error($"The audit of the failed correction could not be written: {writeError.Message}");
            }
            return null;
        }
        catch (Exception ex) {
            reporter.Error($"Automatic alignment RT correction failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// The folder the audit of an alignment result's correction is written to when the result is
    /// aligned by the GUI: beside the result, named after it, holding the Console's three file names.
    /// </summary>
    public static string AuditFolderFor(AlignmentFileBean alignmentFile) {
        if (alignmentFile is null) throw new ArgumentNullException(nameof(alignmentFile));
        var folder = Path.GetDirectoryName(alignmentFile.FilePath) ?? string.Empty;
        return Path.Combine(folder, alignmentFile.FileName + "_automatic_rt_correction");
    }

    /// <summary>
    /// The correction of an alignment the GUI runs, made as the Console makes it: the peaks of each
    /// file's saved peak list, the raw data read through <paramref name="providerFactory"/>, the
    /// audit in <see cref="AuditFolderFor"/>. On success the alignment reference becomes the
    /// correction's reference, as in the Console, and the map is written beside the alignment
    /// result (<see cref="AlignmentRetentionTimeCorrectionMapFile"/>); null, with the reason given
    /// to <paramref name="reporter"/>, when it fails, and then neither is changed.
    /// </summary>
    public static AutomaticAlignmentRetentionTimeCorrectionResult? RunForAlignment(
        IReadOnlyList<AnalysisFileBean> files,
        MsdialLcmsParameter parameter,
        AutomaticRtCorrectionInputs inputs,
        AlignmentFileBean alignmentFile,
        IDataProviderFactory<AnalysisFileBean> providerFactory,
        IAutomaticRtCorrectionReporter reporter) {
        if (providerFactory is null) throw new ArgumentNullException(nameof(providerFactory));
        var auditFolder = AuditFolderFor(alignmentFile);
        Directory.CreateDirectory(auditFolder);
        var result = Build(
            files,
            parameter,
            inputs,
            auditFolder,
            file => MsdialCore.Parser.MsdialPeakSerializer.LoadChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath),
            (file, anchors) => ReadRawFile(file, providerFactory.Create(file), parameter, anchors),
            "Run",
            reporter);
        if (result is null) {
            return null;
        }
        parameter.AlignmentReferenceFileID = result.Correction.ReferenceFileId;
        var mapPath = AlignmentRetentionTimeCorrectionMapFile.PathFor(alignmentFile);
        try {
            AlignmentRetentionTimeCorrectionMapFile.Write(mapPath, result.Correction, files);
            reporter.Info($"Automatic alignment RT correction map: {mapPath}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException) {
            reporter.Info($"Warning: the automatic alignment RT correction map {mapPath} could not be written ({ex.Message}); the curation windows will move RTs between the axes through each peak's stored bounds.");
        }
        reporter.Info($"Automatic alignment RT correction audit folder: {auditFolder}");
        return result;
    }

    /// <summary>
    /// Removes the map an earlier alignment left under this alignment result's name, so that an
    /// alignment made without the correction is never read through another alignment's maps.
    /// </summary>
    public static void RemoveMap(AlignmentFileBean alignmentFile) {
        var path = AlignmentRetentionTimeCorrectionMapFile.PathFor(alignmentFile);
        if (File.Exists(path)) {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Says what the audit says and a person must not miss: the reference, the non-Blank files
    /// left on their original RT, the user anchors that are no anchor, and the exclusions that
    /// excluded nothing.
    /// </summary>
    public static void Report(AutomaticAlignmentRetentionTimeCorrectionResult result, IAutomaticRtCorrectionReporter reporter) {
        var reference = result.Files.FirstOrDefault(file => file.FileId == result.Correction.ReferenceFileId);
        reporter.Info($"Automatic alignment RT correction reference file ID: {result.Correction.ReferenceFileId}{(reference is null ? string.Empty : $" ({reference.FileName})")}");
        if (!string.IsNullOrEmpty(result.ReferenceSelection)) {
            reporter.Info($"Automatic alignment RT correction reference selection: {result.ReferenceSelection}");
        }
        var uncorrected = result.Files
            .Where(file => file.FileType != AnalysisFileType.Blank && file.ModelSource == AutomaticRtCorrectionModelSource.Uncorrected)
            .ToList();
        if (uncorrected.Count > 0) {
            reporter.Info($"Warning: {uncorrected.Count} non-Blank file(s) keep their original RT, too few usable anchors: "
                + string.Join(", ", uncorrected.Select(file => $"{file.FileName} ({file.MatchedAnchorCount} matched)"))
                + ". They are aligned uncorrected.");
        }
        if (reference is not null) {
            foreach (var anchor in result.Anchors.Where(anchor => anchor.FileId == reference.FileId && anchor.Status is "MissingInReference" or "Duplicate")) {
                reporter.Info(anchor.Status == "Duplicate"
                    ? $"Warning: user anchor {anchor.AnchorName} is not used: {anchor.Detection}"
                    : $"Warning: user anchor {anchor.AnchorName} was not detected in the reference file and is not used. {anchor.Detection}");
            }
        }
        foreach (var group in result.Anchors.Where(anchor => anchor.OutlierFlagged).GroupBy(anchor => anchor.AnchorName)) {
            reporter.Info($"Note: the outlier test disagrees with user anchor {group.Key} in {group.Count()} file(s); a user anchor is not rejected for that.");
        }
        foreach (var exclusion in result.Exclusions.Where(exclusion => exclusion.ExcludedCandidates == 0)) {
            reporter.Info($"Warning: exclusion {exclusion.AnchorName} (m/z {exclusion.Mass.ToString(CultureInfo.InvariantCulture)}, RT {exclusion.Rt.ToString(CultureInfo.InvariantCulture)} min) matched no automatic candidate of reference file {exclusion.ReferenceFileName}.");
        }
        reporter.Info(result.Exclusions.Count > 0
            ? $"Automatic alignment RT correction audit: {AutomaticAlignmentRetentionTimeCorrectionResult.SummaryFileName}, {AutomaticAlignmentRetentionTimeCorrectionResult.AnchorsFileName} and {AutomaticAlignmentRetentionTimeCorrectionResult.ExclusionsFileName}"
            : $"Automatic alignment RT correction audit: {AutomaticAlignmentRetentionTimeCorrectionResult.SummaryFileName} and {AutomaticAlignmentRetentionTimeCorrectionResult.AnchorsFileName}");
    }

    // Says what the library holds and where it comes from, warns about a row the run's other
    // settings will keep from being found, and returns the library's file name and SHA-256 for
    // the audit.
    private static (string Name, string Sha256) ReportAnchorLibrary(MsdialLcmsParameter param, string path, IReadOnlyList<MoleculeMsReference> library, IAutomaticRtCorrectionReporter reporter) {
        var invariant = CultureInfo.InvariantCulture;
        var correction = param.AlignmentBaseParam.AutomaticRtCorrection;
        var anchors = AutomaticRtCorrectionAnchorLibrary.Anchors(library);
        var exclusions = AutomaticRtCorrectionAnchorLibrary.Exclusions(library);
        string checksum;
        using (var sha = System.Security.Cryptography.SHA256.Create())
        using (var stream = File.OpenRead(path)) {
            checksum = string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2")));
        }
        reporter.Info($"Automatic alignment RT correction anchor mode {correction.AnchorMode}: {anchors.Count} user anchor(s) and {exclusions.Count} exclusion(s) from {path} (SHA-256 {checksum}).");
        reporter.Info($"User anchors: {(anchors.Count == 0 ? "none" : string.Join(", ", anchors.Select(anchor => anchor.Name)))}.");
        if (exclusions.Count > 0) {
            reporter.Info($"Exclusions: {string.Join(", ", exclusions.Select(exclusion => exclusion.Name))}.");
        }
        if (correction.AnchorMode == AutomaticRtCorrectionAnchorMode.UserOnly && exclusions.Count > 0) {
            reporter.Info($"Warning: the anchor library's {exclusions.Count} False row(s) have no effect in UserOnly mode; they exclude automatic candidates only in Hybrid mode.");
        }
        foreach (var anchor in anchors) {
            // The run drops a detected peak whose m/z lies within an excluded window
            // (PeakSpottingCore). An anchor whose whole m/z window lies inside one is never
            // found; one whose window only reaches into it is found unless its peak's m/z falls
            // inside.
            var covering = param.ExcludedMassList.FirstOrDefault(query => Math.Abs(query.Mass - anchor.PrecursorMz) + anchor.MassTolerance <= query.MassTolerance);
            var touching = covering ?? param.ExcludedMassList.FirstOrDefault(query => Math.Abs(query.Mass - anchor.PrecursorMz) < query.MassTolerance + anchor.MassTolerance);
            if (covering is not null) {
                reporter.Info($"Warning: user anchor {anchor.Name} (m/z {anchor.PrecursorMz.ToString(invariant)}) lies within the excluded mass {covering.Mass.ToString(invariant)}; peaks there are not detected, so it will be missing in every file.");
            }
            else if (touching is not null) {
                reporter.Info($"Warning: user anchor {anchor.Name} (m/z {anchor.PrecursorMz.ToString(invariant)}) reaches into the excluded mass {touching.Mass.ToString(invariant)}; it will be missing in a file where its peak's m/z falls inside the excluded window.");
            }
        }
        // The user-defined RT correction's peak selection keys do not reach the new pipeline,
        // which reads its own. A method file that set them and moved from one pipeline to the
        // other would choose another peak per anchor without a word; one that left them at their
        // defaults says nothing it needs to hear.
        var legacy = param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam;
        var legacyDefaults = new RetentionTimeCorrectionParam();
        var legacySet = legacy.PeakSelectionMode != legacyDefaults.PeakSelectionMode
            || Math.Abs(legacy.PeakSelectionRtWeight - legacyDefaults.PeakSelectionRtWeight) > 1e-6;
        var differs = legacy.PeakSelectionMode != correction.AnchorPeakSelectionMode
            || (legacy.PeakSelectionMode == RetentionTimeCorrectionPeakSelectionMode.Weighted && Math.Abs(legacy.PeakSelectionRtWeight - correction.AnchorPeakSelectionRtWeight) > 1e-6);
        if (legacySet && differs) {
            reporter.Info($"Warning: 'RT correction peak selection mode' is {legacy.PeakSelectionMode}, but user anchors are chosen by 'Automatic RT correction anchor peak selection mode', which is {correction.AnchorPeakSelectionMode}"
                + (correction.AnchorPeakSelectionMode == RetentionTimeCorrectionPeakSelectionMode.Weighted ? $" (RT weight {correction.AnchorPeakSelectionRtWeight.ToString(invariant)})" : string.Empty)
                + "; the user-defined RT correction's keys have no effect here.");
        }
        return (Path.GetFileName(path), checksum);
    }
}

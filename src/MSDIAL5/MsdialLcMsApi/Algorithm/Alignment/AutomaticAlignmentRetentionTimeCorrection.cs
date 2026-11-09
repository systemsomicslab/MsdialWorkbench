using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment;

public enum AutomaticRtCorrectionModelSource {
    Reference,
    DetectedAnchors,
    InterpolatedBlank,
    NearestBlank,
    Uncorrected,
}

public sealed class AutomaticRtCorrectionFileAudit {
    public int FileId { get; internal set; }
    public string FileName { get; internal set; } = string.Empty;
    public AnalysisFileType FileType { get; internal set; }
    public int AnalyticalOrder { get; internal set; }
    public int CandidateCount { get; internal set; }
    public int MatchedAnchorCount { get; internal set; }
    public int UsedAnchorCount { get; internal set; }
    public AutomaticRtCorrectionModelSource ModelSource { get; internal set; }
    public double ReferenceScore { get; internal set; }
    public double MedianAbsoluteOffset { get; internal set; }
    public double MaximumAbsoluteOffset { get; internal set; }
    public string Note { get; internal set; } = string.Empty;
    /// <summary>
    /// The file's MS1 cycle time (min) over the whole run, for information: the time-weighted median
    /// spacing of consecutive MS1 scans of the analysed polarity, MS2 scans excluded (see
    /// <see cref="Ms1CycleProfile.CycleTime"/>). 0 when unknown. The outlier test does not use it;
    /// each anchor is floored at the cycle around it, <see cref="AutomaticRtCorrectionAnchorAudit.Ms1CycleTime"/>.
    /// </summary>
    public double EstimatedScanInterval { get; internal set; }
    public double? FirstAnchorRt { get; internal set; }
    public double? LastAnchorRt { get; internal set; }
    /// <summary>Peaks whose original RT lies before the first used anchor; their offset is held constant.</summary>
    public int PeaksBeforeFirstAnchor { get; internal set; }
    /// <summary>Peaks whose original RT lies after the last used anchor; their offset is held constant.</summary>
    public int PeaksAfterLastAnchor { get; internal set; }
    /// <summary>
    /// The anchor library's True rows detected in this file's raw data, a row whose peak lies
    /// within two MS1 scans of an earlier row's counted once; null when the anchor mode reads no
    /// library.
    /// </summary>
    public int? UserAnchorsDetected { get; internal set; }
    /// <summary>
    /// On the reference file only: its automatic candidates that the anchor library's False rows
    /// kept out of the selection. Null on the other files and when the mode reads no exclusions.
    /// </summary>
    public int? ExcludedAutomaticCandidates { get; internal set; }
}

public sealed class AutomaticRtCorrectionAnchorAudit {
    public int FileId { get; internal set; }
    public string FileName { get; internal set; } = string.Empty;
    public int AnchorId { get; internal set; }
    /// <summary>The anchor's m/z: the reference file's peak for an automatic anchor, the library's for a user anchor.</summary>
    public double Mass { get; internal set; }
    /// <summary>
    /// The anchor's RT in the reference file, the RT the correction maps to; NaN for a user
    /// anchor that was not detected in the reference file (status MissingInReference).
    /// </summary>
    public double ReferenceRt { get; internal set; }
    public double? OriginalRt { get; internal set; }
    /// <summary>The automatic candidate's peak quality; 0 for a user anchor, which is not scored.</summary>
    public double QualityScore { get; internal set; }
    public double SampleCoverage { get; internal set; }
    public bool Used { get; internal set; }
    public string Status { get; internal set; } = string.Empty;
    /// <summary>"Local", "Global", or empty when the anchor was not judged (unmatched, reference, Blank).</summary>
    public string OutlierTest { get; internal set; } = string.Empty;
    /// <summary>
    /// The compounds matched in the file within the local support window of the anchor, other
    /// than the anchor's own: co-eluting candidates (isotope peaks, adducts) count once.
    /// </summary>
    public int LocalSupportCount { get; internal set; }
    /// <summary>Offset the anchor was judged against: the local support median, or the file's anchor median.</summary>
    public double? ExpectedOffset { get; internal set; }
    /// <summary>Robust scale of the test, max(1.4826 MAD, <see cref="Ms1CycleTime"/>), in minutes.</summary>
    public double? OutlierScale { get; internal set; }
    /// <summary>
    /// The file's MS1 cycle time (min) around the anchor's original RT, the floor of the outlier
    /// scale (see <see cref="Ms1CycleProfile.CycleTimeAt(double)"/>); 0 when unknown, null when the
    /// anchor was not matched in the file.
    /// </summary>
    public double? Ms1CycleTime { get; internal set; }
    public AutomaticRtCorrectionAnchorSource Source { get; internal set; }
    /// <summary>The anchor library's name for a user anchor; empty for an automatic anchor.</summary>
    public string AnchorName { get; internal set; } = string.Empty;
    /// <summary>The RT the anchor library gives a user anchor, the centre of its search window; null for an automatic anchor.</summary>
    public double? LibraryRt { get; internal set; }
    /// <summary>
    /// A user anchor whose offset the outlier test would have rejected. A user anchor is kept
    /// regardless, and this says the test disagreed with it.
    /// </summary>
    public bool OutlierFlagged { get; internal set; }
    /// <summary>
    /// The height of the peak the anchor was matched to in the file: the detected peak's for an
    /// automatic anchor, the raw extracted ion chromatogram peak's for a user anchor. Null when
    /// it was not matched.
    /// </summary>
    public double? PeakHeight { get; internal set; }
    /// <summary>The m/z of the peak the anchor was matched to in the file; null when it was not matched.</summary>
    public double? ObservedMz { get; internal set; }
    /// <summary>
    /// For a user anchor, how its peak in this file was chosen or why none was (how many peaks its
    /// window held, the nearest rejected peak); empty for an automatic anchor.
    /// </summary>
    public string Detection { get; internal set; } = string.Empty;
}

/// <summary>
/// One False row of the anchor library in Hybrid mode: the automatic candidates of the reference
/// file it kept out of the selection.
/// </summary>
public sealed class AutomaticRtCorrectionExclusionAudit {
    public string AnchorName { get; internal set; } = string.Empty;
    public double Mass { get; internal set; }
    public double Rt { get; internal set; }
    public double MassTolerance { get; internal set; }
    public double RtTolerance { get; internal set; }
    /// <summary>The file whose RT axis the row's RT was compared on.</summary>
    public int ReferenceFileId { get; internal set; }
    public string ReferenceFileName { get; internal set; } = string.Empty;
    /// <summary>
    /// The reference file's automatic candidates within the row's m/z and RT tolerances. 0 means
    /// the row excluded nothing: the candidate it was written for is not where it says, perhaps
    /// because the reference file is not the one it was reviewed on.
    /// </summary>
    public int ExcludedCandidates { get; internal set; }
}

/// <summary>
/// The automatic alignment RT correction could not be built. <see cref="PartialAudit"/> holds what
/// was read before it failed, to be written as the audit, so that a person can see which anchors
/// were found where.
/// </summary>
public sealed class AutomaticRtCorrectionFailedException : InvalidOperationException {
    public AutomaticRtCorrectionFailedException(string message, AutomaticAlignmentRetentionTimeCorrectionResult partialAudit, Exception innerException)
        : base(message, innerException) {
        PartialAudit = partialAudit;
    }

    /// <summary>
    /// Every file's row, with the reason in its note, and every user anchor's detection in every
    /// file. Its correction maps no file.
    /// </summary>
    public AutomaticAlignmentRetentionTimeCorrectionResult PartialAudit { get; }
}

/// <summary>
/// The retention times of a file's MS1 scans of one polarity, the scans the MS1 chromatogram and
/// so the peak-top RTs come from. A peak-top RT moves in steps of the MS1 cycle at that RT, which
/// in data-dependent acquisition changes along the run: MS1 scans follow each other closely where
/// no precursor triggers MS2 (void, wash, re-equilibration) and are spread out where many do.
/// </summary>
public sealed class Ms1CycleProfile {
    /// <summary>Half width (min) of the RT window <see cref="CycleTimeAt(double)"/> reads.</summary>
    public const double LocalHalfWindow = 0.5d;

    private readonly double[] _times;

    public Ms1CycleProfile(IEnumerable<double> ms1ScanTimes) {
        if (ms1ScanTimes is null) throw new ArgumentNullException(nameof(ms1ScanTimes));
        _times = ms1ScanTimes.Where(time => !double.IsNaN(time) && !double.IsInfinity(time)).ToArray();
        Array.Sort(_times);
        CycleTime = TimeWeightedMedianSpacing(0, _times.Length - 1);
    }

    /// <summary>No MS1 scans: every cycle time is 0, unknown.</summary>
    public static Ms1CycleProfile Empty { get; } = new Ms1CycleProfile(Array.Empty<double>());

    /// <summary>
    /// The MS1 scans (MsLevel 1) of <paramref name="ionMode"/>'s polarity, as
    /// RetentionTimeTypedSpectra selects them for the MS1 chromatogram. MS2 scans between them are
    /// ignored, so DDA and DIA/SWATH files give their MS1 cycle, not the spacing of the raw
    /// spectrum list.
    /// </summary>
    /// <remarks>
    /// The peaks' ChromScanId values cannot give this: peak spotting sets them to the raw spectrum
    /// index (ValuePeak.Id), which counts every spectrum, MS2 included.
    /// </remarks>
    public static Ms1CycleProfile FromSpectra(IEnumerable<RawSpectrum> spectra, IonMode ionMode) {
        if (spectra is null) throw new ArgumentNullException(nameof(spectra));
        var polarity = ionMode == IonMode.Negative ? ScanPolarity.Negative : ScanPolarity.Positive;
        return new Ms1CycleProfile(spectra
            .Where(spectrum => spectrum.MsLevel == 1 && spectrum.ScanPolarity == polarity)
            .Select(spectrum => spectrum.ScanStartTime));
    }

    public int ScanCount => _times.Length;

    /// <summary>The time-weighted median MS1 spacing (min) over the whole file; 0 with fewer than two scans.</summary>
    public double CycleTime { get; }

    /// <summary>
    /// The MS1 cycle time (min) around <paramref name="rt"/>: the time-weighted median spacing of
    /// the consecutive MS1 scans within <see cref="LocalHalfWindow"/> of it, together with the
    /// nearest scan outside each side of that window. 0 with fewer than two scans in the file.
    /// </summary>
    /// <remarks>
    /// The median is weighted by duration because a median over scans counts a dense region more
    /// times than a sparse one of the same length: 0.2 min of 0.0047-min idle scans inside the
    /// window outnumber 0.8 min of 0.021-min scans, and the plain median would floor an anchor
    /// that sits in the 0.021-min region at the idle cycle. Weighted by duration, the result is
    /// the spacing in effect over most of the window. Adding the two scans outside the window
    /// gives a cycle even where the window holds fewer than two scans.
    /// </remarks>
    public double CycleTimeAt(double rt) {
        if (_times.Length < 2 || double.IsNaN(rt) || double.IsInfinity(rt)) return 0d;
        // The last scan before the window, and the first scan after it.
        var first = Math.Max(0, FirstIndexWhere(time => time >= rt - LocalHalfWindow) - 1);
        var last = Math.Min(_times.Length - 1, FirstIndexWhere(time => time > rt + LocalHalfWindow));
        if (last <= first) {
            // The window lies wholly before or after the scans: use the two scans at that end.
            last = Math.Min(_times.Length - 1, first + 1);
            first = last - 1;
        }
        return TimeWeightedMedianSpacing(first, last);
    }

    /// <summary>
    /// The position, among the file's MS1 scans in RT order, of the scan nearest
    /// <paramref name="rt"/>; -1 when the file has no MS1 scans or <paramref name="rt"/> is NaN.
    /// A peak top lies on an MS1 scan, so the difference of two peak tops' positions is the number
    /// of MS1 scan steps between them, whatever the spacing of those scans.
    /// </summary>
    public int NearestScanIndex(double rt) {
        if (_times.Length == 0 || double.IsNaN(rt)) return -1;
        var next = FirstIndexWhere(time => time >= rt);
        if (next == 0) return 0;
        if (next == _times.Length) return _times.Length - 1;
        return rt - _times[next - 1] <= _times[next] - rt ? next - 1 : next;
    }

    // Binary search over the sorted times for the first index whose time satisfies a monotone
    // predicate; _times.Length when none does.
    private int FirstIndexWhere(Func<double, bool> predicate) {
        int low = 0, high = _times.Length;
        while (low < high) {
            var middle = low + (high - low) / 2;
            if (predicate(_times[middle])) high = middle;
            else low = middle + 1;
        }
        return low;
    }

    // Over the scans first..last (inclusive): the smallest positive spacing d such that the
    // spacings of at most d cover at least half the time between consecutive scans.
    private double TimeWeightedMedianSpacing(int first, int last) {
        if (last - first < 1) return 0d;
        var spacings = new List<double>(last - first);
        for (var index = first + 1; index <= last; index++) {
            var spacing = _times[index] - _times[index - 1];
            if (spacing > 0d) spacings.Add(spacing);
        }
        if (spacings.Count == 0) return 0d;
        spacings.Sort();
        var half = spacings.Sum() / 2d;
        var covered = 0d;
        foreach (var spacing in spacings) {
            covered += spacing;
            if (covered >= half) return spacing;
        }
        return spacings[spacings.Count - 1];
    }
}

public sealed class AutomaticAlignmentRetentionTimeCorrectionResult {
    public AutomaticAlignmentRetentionTimeCorrectionResult(
        AlignmentRetentionTimeCorrectionCollection correction,
        IReadOnlyList<AutomaticRtCorrectionFileAudit> files,
        IReadOnlyList<AutomaticRtCorrectionAnchorAudit> anchors,
        AutomaticRtCorrectionAnchorMode anchorMode = AutomaticRtCorrectionAnchorMode.Automatic,
        IReadOnlyList<AutomaticRtCorrectionExclusionAudit>? exclusions = null) {
        Correction = correction;
        Files = files;
        Anchors = anchors;
        AnchorMode = anchorMode;
        Exclusions = exclusions ?? Array.Empty<AutomaticRtCorrectionExclusionAudit>();
    }

    public const string SummaryFileName = "automatic_alignment_rt_correction_summary.tsv";
    public const string AnchorsFileName = "automatic_alignment_rt_correction_anchors.tsv";
    public const string ExclusionsFileName = "automatic_alignment_rt_correction_exclusions.tsv";

    public AlignmentRetentionTimeCorrectionCollection Correction { get; }
    public IReadOnlyList<AutomaticRtCorrectionFileAudit> Files { get; }
    public IReadOnlyList<AutomaticRtCorrectionAnchorAudit> Anchors { get; }
    public AutomaticRtCorrectionAnchorMode AnchorMode { get; }
    /// <summary>The anchor library's False rows in Hybrid mode; empty otherwise.</summary>
    public IReadOnlyList<AutomaticRtCorrectionExclusionAudit> Exclusions { get; }

    /// <summary>
    /// Removes the audit files of an earlier run from <paramref name="outputFolder"/>, so that a run
    /// that fails before it writes its own cannot leave them looking like its own. Only these
    /// three file names are removed. Returns how many there were.
    /// </summary>
    public static int RemoveAudit(string outputFolder) {
        var removed = 0;
        foreach (var name in new[] { SummaryFileName, AnchorsFileName, ExclusionsFileName }) {
            var path = Path.Combine(outputFolder, name);
            if (File.Exists(path)) {
                File.Delete(path);
                removed++;
            }
        }
        return removed;
    }

    /// <param name="run">
    /// What the run was, in every summary row: "Run" for a run that goes on to align, "Preview"
    /// for one that stops after the audit, so an audit read later says which it came from.
    /// </param>
    /// <param name="anchorLibrary">The anchor library's file name, not its path; empty when none was read.</param>
    /// <param name="anchorLibrarySha256">The anchor library's SHA-256, so the audit names the exact library its anchors came from.</param>
    public void WriteAudit(string outputFolder, string run = "Run", string anchorLibrary = "", string anchorLibrarySha256 = "") {
        Directory.CreateDirectory(outputFolder);
        RemoveAudit(outputFolder);
        var summaryPath = Path.Combine(outputFolder, SummaryFileName);
        using (var writer = new StreamWriter(summaryPath, false, new UTF8Encoding(false))) {
            // Columns after "Note" were added later; readers that address columns by name are unaffected.
            writer.WriteLine("File ID\tFile name\tFile type\tAnalytical order\tCandidate count\tMatched anchors\tUsed anchors\tModel source\tReference score\tMedian absolute offset (min)\tMaximum absolute offset (min)\tNote"
                + "\tEstimated scan interval (min)\tFirst used anchor RT (min)\tLast used anchor RT (min)\tPeaks before first used anchor\tPeaks after last used anchor"
                + "\tAnchor mode\tUser anchors detected\tExcluded automatic candidates\tRun\tAnchor library\tAnchor library SHA-256");
            foreach (var file in Files.OrderBy(file => file.AnalyticalOrder).ThenBy(file => file.FileId)) {
                writer.WriteLine(string.Join("\t",
                    file.FileId.ToString(CultureInfo.InvariantCulture),
                    Cell(file.FileName),
                    file.FileType.ToString(),
                    file.AnalyticalOrder.ToString(CultureInfo.InvariantCulture),
                    file.CandidateCount.ToString(CultureInfo.InvariantCulture),
                    file.MatchedAnchorCount.ToString(CultureInfo.InvariantCulture),
                    file.UsedAnchorCount.ToString(CultureInfo.InvariantCulture),
                    file.ModelSource.ToString(),
                    file.ReferenceScore.ToString("G17", CultureInfo.InvariantCulture),
                    file.MedianAbsoluteOffset.ToString("G17", CultureInfo.InvariantCulture),
                    file.MaximumAbsoluteOffset.ToString("G17", CultureInfo.InvariantCulture),
                    Cell(file.Note),
                    file.EstimatedScanInterval.ToString("G17", CultureInfo.InvariantCulture),
                    file.FirstAnchorRt?.ToString("G17", CultureInfo.InvariantCulture) ?? string.Empty,
                    file.LastAnchorRt?.ToString("G17", CultureInfo.InvariantCulture) ?? string.Empty,
                    file.FirstAnchorRt.HasValue ? file.PeaksBeforeFirstAnchor.ToString(CultureInfo.InvariantCulture) : string.Empty,
                    file.LastAnchorRt.HasValue ? file.PeaksAfterLastAnchor.ToString(CultureInfo.InvariantCulture) : string.Empty,
                    AnchorMode.ToString(),
                    file.UserAnchorsDetected?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    file.ExcludedAutomaticCandidates?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    Cell(run),
                    Cell(anchorLibrary),
                    Cell(anchorLibrarySha256)));
            }
        }

        var anchorPath = Path.Combine(outputFolder, AnchorsFileName);
        using (var writer = new StreamWriter(anchorPath, false, new UTF8Encoding(false))) {
            writer.WriteLine("File ID\tFile name\tAnchor ID\tm/z\tReference RT (min)\tOriginal RT (min)\tOffset (min)\tQuality score\tNon-Blank sample coverage\tUsed\tStatus"
                + "\tOutlier test\tLocal support count\tExpected offset (min)\tOutlier scale (min)\tMS1 cycle at anchor (min)"
                + "\tAnchor source\tAnchor name\tLibrary RT (min)\tOutlier flagged\tPeak height\tObserved m/z\tDetection");
            foreach (var anchor in Anchors.OrderBy(anchor => anchor.FileId).ThenBy(anchor => anchor.AnchorId)) {
                var hasReferenceRt = !double.IsNaN(anchor.ReferenceRt);
                var offset = anchor.OriginalRt.HasValue && hasReferenceRt ? anchor.ReferenceRt - anchor.OriginalRt.Value : (double?)null;
                var isUser = anchor.Source == AutomaticRtCorrectionAnchorSource.User;
                writer.WriteLine(string.Join("\t",
                    anchor.FileId.ToString(CultureInfo.InvariantCulture),
                    anchor.FileName,
                    anchor.AnchorId.ToString(CultureInfo.InvariantCulture),
                    anchor.Mass.ToString("G17", CultureInfo.InvariantCulture),
                    hasReferenceRt ? anchor.ReferenceRt.ToString("G17", CultureInfo.InvariantCulture) : string.Empty,
                    anchor.OriginalRt?.ToString("G17", CultureInfo.InvariantCulture) ?? string.Empty,
                    offset?.ToString("G17", CultureInfo.InvariantCulture) ?? string.Empty,
                    isUser ? string.Empty : anchor.QualityScore.ToString("G17", CultureInfo.InvariantCulture),
                    anchor.SampleCoverage.ToString("G17", CultureInfo.InvariantCulture),
                    anchor.Used.ToString(),
                    anchor.Status,
                    anchor.OutlierTest,
                    string.IsNullOrEmpty(anchor.OutlierTest) ? string.Empty : anchor.LocalSupportCount.ToString(CultureInfo.InvariantCulture),
                    anchor.ExpectedOffset?.ToString("G17", CultureInfo.InvariantCulture) ?? string.Empty,
                    anchor.OutlierScale?.ToString("G17", CultureInfo.InvariantCulture) ?? string.Empty,
                    anchor.Ms1CycleTime?.ToString("G17", CultureInfo.InvariantCulture) ?? string.Empty,
                    anchor.Source.ToString(),
                    Cell(anchor.AnchorName),
                    anchor.LibraryRt?.ToString("G17", CultureInfo.InvariantCulture) ?? string.Empty,
                    anchor.OutlierFlagged.ToString(),
                    anchor.PeakHeight?.ToString("G17", CultureInfo.InvariantCulture) ?? string.Empty,
                    anchor.ObservedMz?.ToString("G17", CultureInfo.InvariantCulture) ?? string.Empty,
                    Cell(anchor.Detection)));
            }
        }

        if (Exclusions.Count > 0) {
            using var writer = new StreamWriter(Path.Combine(outputFolder, ExclusionsFileName), false, new UTF8Encoding(false));
            writer.WriteLine("Anchor name\tm/z\tRT (min)\tm/z tolerance\tRT tolerance (min)\tReference file ID\tReference file name\tExcluded automatic candidates");
            foreach (var exclusion in Exclusions) {
                writer.WriteLine(string.Join("\t",
                    Cell(exclusion.AnchorName),
                    exclusion.Mass.ToString("G17", CultureInfo.InvariantCulture),
                    exclusion.Rt.ToString("G17", CultureInfo.InvariantCulture),
                    exclusion.MassTolerance.ToString("G17", CultureInfo.InvariantCulture),
                    exclusion.RtTolerance.ToString("G17", CultureInfo.InvariantCulture),
                    exclusion.ReferenceFileId.ToString(CultureInfo.InvariantCulture),
                    exclusion.ReferenceFileName,
                    exclusion.ExcludedCandidates.ToString(CultureInfo.InvariantCulture)));
            }
        }
    }

    // A free-text cell: a tab or a line break would shift or split the row.
    private static string Cell(string text) {
        return (text ?? string.Empty).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
    }
}

/// <summary>
/// Learns a file-specific RT map after peak picking and annotation. Only compact peak properties
/// are retained, and each .pai file is read at most twice, which keeps the setup independent of
/// the spots-by-files object graph created by alignment.
/// </summary>
public static class AutomaticAlignmentRetentionTimeCorrection {
    private const int CandidateLimitPerRtBin = 3;
    // An anchor is judged against its local neighbours only when at least this many other
    // compounds (co-eluting groups of reference candidates, see GroupCoElutingSupport) were
    // matched in the same file within the local support window.
    private const int MinimumLocalSupportCount = 3;
    // Two matched reference candidates are one compound (isotope peaks, adducts, in-source
    // fragments) when their peak tops lie within this many MS1 scan steps of each other in the
    // reference file and in the judged file. The limit counts scans, not minutes: in
    // data-dependent acquisition an abundant compound triggers MS2 at its apex, so the MS1 step
    // there is longer than the median cycle around it, and a limit in median cycles can be
    // shorter than one real step. Two steps absorb a weak isotope peak or adduct whose top noise
    // moves a scan or two from the compound's.
    private const int CoElutionScans = 2;
    // A user anchor judged against the median of the file's anchor offsets (the global test) is
    // marked as an outlier only when at least this many offsets, its own included, were judged.
    // With three, the median is one of them, and the anchor at the far end of a drift that grows
    // with RT is marked in every file however right it is.
    private const int MinimumOffsetsToFlagUserAnchor = 4;
    // UserOnly and Hybrid: the anchors a file other than the reference needs to be corrected.
    private const int MinimumUserModeAnchorsPerFile = 2;

    /// <param name="ms1CycleProfileLoader">
    /// A file's MS1 scan times (<see cref="Ms1CycleProfile.FromSpectra(IEnumerable{RawSpectrum}, IonMode)"/>).
    /// Each anchor's outlier scale is floored at the MS1 cycle around the anchor's RT in that file.
    /// Return <see cref="Ms1CycleProfile.Empty"/> when the scans are unknown: the scale is then the
    /// MAD alone, and a MAD of 0 judges nothing.
    /// </param>
    public static AutomaticAlignmentRetentionTimeCorrectionResult Build(
        IReadOnlyList<AnalysisFileBean> files,
        AutomaticAlignmentRetentionTimeCorrectionParameter parameter,
        double mzTolerance,
        Func<AnalysisFileBean, Ms1CycleProfile> ms1CycleProfileLoader) {
        return Build(files, parameter, mzTolerance, file => MsdialPeakSerializer.LoadChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath), ms1CycleProfileLoader);
    }

    public static AutomaticAlignmentRetentionTimeCorrectionResult Build(
        IReadOnlyList<AnalysisFileBean> files,
        AutomaticAlignmentRetentionTimeCorrectionParameter parameter,
        double mzTolerance,
        Func<AnalysisFileBean, List<ChromatogramPeakFeature>> peakLoader,
        Func<AnalysisFileBean, Ms1CycleProfile> ms1CycleProfileLoader) {
        if (ms1CycleProfileLoader is null) throw new ArgumentNullException(nameof(ms1CycleProfileLoader));
        return Build(
            files,
            parameter,
            mzTolerance,
            peakLoader,
            Array.Empty<MoleculeMsReference>(),
            (file, anchors) => new AutomaticRtCorrectionRawFileReading(
                ms1CycleProfileLoader(file),
                new AutomaticRtCorrectionUserAnchorObservation?[anchors.Count]));
    }

    /// <summary>
    /// Learns the RT maps with the anchors <see cref="AutomaticAlignmentRetentionTimeCorrectionParameter.AnchorMode"/> asks for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Automatic: anchors are chosen from the reference file's detected peaks, as they always were.
    /// </para>
    /// <para>
    /// UserOnly: the anchors are the library's True rows. Each is detected in every file on the
    /// raw extracted ion chromatogram, within its own m/z tolerance, RT tolerance and minimum
    /// height, and its target RT is the RT it was detected at in the reference file; the
    /// library's RT only centres the search. The reference is the non-Blank file with the most
    /// of them detected, at least the minimum anchor count, then the highest reference score, in
    /// which the closeness of its anchors to their median RT over the files takes the place of
    /// the automatic centrality.
    /// </para>
    /// <para>
    /// Hybrid: the reference is chosen as in Automatic mode, the True rows detected in it are
    /// anchors whatever their coverage, and the automatic selection fills the RT bins they leave
    /// empty, up to the maximum anchor count in all; it never takes a candidate a False row
    /// covers or one that is a user anchor's own peak.
    /// </para>
    /// <para>
    /// A user anchor is never rejected by the outlier test, which marks it instead (Outlier
    /// flagged), and two anchors out of RT order lose the automatic one first; of two user
    /// anchors, the one whose offset lies farther from the file's median offset. Two anchors at
    /// one original RT are out of order too when either is a user anchor. A True row not detected
    /// in the reference file has no target RT and is reported as MissingInReference; a True row
    /// whose reference peak is an earlier row's (within two MS1 scans) is that row's anchor
    /// again, reported as Duplicate, and user anchors are counted by their distinct peaks
    /// wherever they are counted against the minimum anchor count.
    /// </para>
    /// <para>
    /// The reference file needs the minimum anchor count in every mode, counted in UserOnly and
    /// Hybrid as distinct RTs. A non-Blank file other than the reference is corrected when its
    /// usable anchors lie at enough distinct original RTs, the points its map is drawn through:
    /// two in UserOnly and Hybrid, the minimum anchor count in Automatic mode.
    /// </para>
    /// <para>
    /// When the correction cannot be built once the raw data has been read, the
    /// <see cref="AutomaticRtCorrectionFailedException"/> thrown carries what was read, for the
    /// audit.
    /// </para>
    /// </remarks>
    /// <param name="anchorLibrary">
    /// Every row of the anchor library (<see cref="AutomaticRtCorrectionAnchorLibrary.Load(string)"/>);
    /// not read in Automatic mode.
    /// </param>
    /// <param name="rawFileReader">
    /// Reads one file's raw data once: its MS1 scan times
    /// (<see cref="Ms1CycleProfile.FromSpectra(IEnumerable{RawSpectrum}, IonMode)"/>), at which each
    /// anchor's outlier scale is floored, and the peak each anchor it is given was detected as
    /// (<see cref="AutomaticRtCorrectionUserAnchorDetector"/>). It is given the library's True
    /// rows in library order, none in Automatic mode, and is called for one file after another,
    /// once each, before anything else is read.
    /// </param>
    public static AutomaticAlignmentRetentionTimeCorrectionResult Build(
        IReadOnlyList<AnalysisFileBean> files,
        AutomaticAlignmentRetentionTimeCorrectionParameter parameter,
        double mzTolerance,
        Func<AnalysisFileBean, List<ChromatogramPeakFeature>> peakLoader,
        IReadOnlyList<MoleculeMsReference> anchorLibrary,
        Func<AnalysisFileBean, IReadOnlyList<MoleculeMsReference>, AutomaticRtCorrectionRawFileReading> rawFileReader) {
        Validate(files, parameter, mzTolerance, peakLoader);
        if (anchorLibrary is null) throw new ArgumentNullException(nameof(anchorLibrary));
        if (rawFileReader is null) throw new ArgumentNullException(nameof(rawFileReader));
        AutomaticRtCorrectionAnchorLibrary.Check(anchorLibrary, parameter);

        var mode = parameter.AnchorMode;
        var libraryAnchors = mode == AutomaticRtCorrectionAnchorMode.Automatic
            ? new List<MoleculeMsReference>()
            : AutomaticRtCorrectionAnchorLibrary.Anchors(anchorLibrary);
        var exclusions = mode == AutomaticRtCorrectionAnchorMode.Hybrid
            ? AutomaticRtCorrectionAnchorLibrary.Exclusions(anchorLibrary)
            : new List<MoleculeMsReference>();

        var summaries = files.Select(file => CreateSummary(file, peakLoader(file), parameter, mzTolerance)).ToList();
        var readings = new Dictionary<int, AutomaticRtCorrectionRawFileReading>();
        void ReadRawData() {
            foreach (var file in files) {
                var reading = rawFileReader(file, libraryAnchors)
                    ?? throw new InvalidOperationException($"Automatic alignment RT correction read nothing from the raw data of {file.AnalysisFileName}.");
                if (reading.UserAnchorObservations.Count != libraryAnchors.Count) {
                    throw new InvalidOperationException(
                        $"Automatic alignment RT correction read {reading.UserAnchorObservations.Count} anchor observation(s) from {file.AnalysisFileName} for {libraryAnchors.Count} anchor(s).");
                }
                readings[file.AnalysisFileId] = reading;
            }
        }
        try {
            // Automatic mode chooses its reference from the picked peaks alone, so a run with no
            // eligible file fails before any raw data is read, as it always did; the anchor modes
            // need the user anchors' detections to choose it.
            FileSummary reference;
            if (mode == AutomaticRtCorrectionAnchorMode.Automatic) {
                reference = SelectAutomaticReference(summaries, parameter, mzTolerance, _ => 0);
                ReadRawData();
            }
            else {
                ReadRawData();
                reference = mode == AutomaticRtCorrectionAnchorMode.UserOnly
                    ? SelectUserReference(summaries, libraryAnchors, readings, parameter)
                    : SelectAutomaticReference(summaries, parameter, mzTolerance, summary => DistinctUserAnchorsDetected(readings[summary.File.AnalysisFileId]));
            }
            return BuildFromReadings(files, parameter, mzTolerance, peakLoader, mode, libraryAnchors, exclusions, readings, summaries, reference);
        }
        catch (Exception ex) when (ex is (InvalidOperationException or ArgumentException) and not AutomaticRtCorrectionFailedException) {
            throw new AutomaticRtCorrectionFailedException(
                ex.Message,
                PartialAudit(files, summaries, mode, libraryAnchors, readings, ex.Message, null),
                ex);
        }
    }

    private static AutomaticAlignmentRetentionTimeCorrectionResult BuildFromReadings(
        IReadOnlyList<AnalysisFileBean> files,
        AutomaticAlignmentRetentionTimeCorrectionParameter parameter,
        double mzTolerance,
        Func<AnalysisFileBean, List<ChromatogramPeakFeature>> peakLoader,
        AutomaticRtCorrectionAnchorMode mode,
        IReadOnlyList<MoleculeMsReference> libraryAnchors,
        IReadOnlyList<MoleculeMsReference> exclusions,
        IReadOnlyDictionary<int, AutomaticRtCorrectionRawFileReading> readings,
        IReadOnlyList<FileSummary> summaries,
        FileSummary reference) {
        int UserAnchorsDetected(AnalysisFileBean file) => DistinctUserAnchorsDetected(readings[file.AnalysisFileId]);

        var referenceReading = readings[reference.File.AnalysisFileId];
        var userPoints = libraryAnchors
            .Select((anchor, index) => UserReferencePoint(anchor, index, referenceReading.UserAnchorObservations[index]))
            .ToList();
        // Two True rows whose peaks in the reference file lie within CoElutionScans MS1 scans of
        // each other (one row entered twice, two adducts of one standard, two windows that took
        // one peak, or two standards that co-elute) are one anchor: they carry one RT. The earlier
        // row is kept, and the later one is reported as Duplicate in every file and counted
        // nowhere. DistinctUserAnchorsDetected counts the same way.
        var userReferences = new List<PeakPoint>();
        var duplicateUserReferences = new List<PeakPoint>();
        foreach (var point in userPoints.Where(point => !double.IsNaN(point.Rt))) {
            var kept = userReferences.FirstOrDefault(other => SameScan(point.Rt, other.Rt, referenceReading.CycleProfile));
            if (kept is null) {
                userReferences.Add(point);
            }
            else {
                point.DuplicateOf = kept;
                duplicateUserReferences.Add(point);
            }
        }
        var unusedUserReferences = userPoints
            .Where(point => double.IsNaN(point.Rt) || point.DuplicateOf is not null)
            .ToList();
        var automaticCandidates = mode == AutomaticRtCorrectionAnchorMode.UserOnly
            ? new List<PeakPoint>()
            : reference.Candidates;

        var matchesByFile = new Dictionary<int, List<AnchorMatch>>();
        var unusedUserByFile = new Dictionary<int, List<AnchorMatch>>();
        var cycleProfiles = new Dictionary<int, Ms1CycleProfile>();
        var sortedPeakRts = new Dictionary<int, double[]>();
        foreach (var file in files) {
            var features = peakLoader(file);
            var reading = readings[file.AnalysisFileId];
            cycleProfiles[file.AnalysisFileId] = reading.CycleProfile;
            var peaks = CreatePeakPoints(features);
            var rts = peaks.Select(peak => peak.Rt).ToArray();
            Array.Sort(rts);
            sortedPeakRts[file.AnalysisFileId] = rts;
            matchesByFile[file.AnalysisFileId] = automaticCandidates
                .Select(candidate => Match(candidate, peaks, parameter.MatchRtTolerance, mzTolerance))
                .Concat(userReferences.Select(point => MatchUser(point, reading)))
                .ToList();
            unusedUserByFile[file.AnalysisFileId] = unusedUserReferences
                .Select(point => MatchUser(point, reading))
                .ToList();
        }

        var nonBlankCount = Math.Max(1, files.Count(file => file.AnalysisFileType != AnalysisFileType.Blank));
        var minimumCoverageCount = Math.Max(1, (int)Math.Ceiling(Clamp01(parameter.MinimumSampleCoverage) * nonBlankCount));
        foreach (var candidate in automaticCandidates.Concat(userPoints)) {
            var detectedCount = files
                .Where(file => file.AnalysisFileType != AnalysisFileType.Blank)
                .Count(file => matchesByFile[file.AnalysisFileId].Concat(unusedUserByFile[file.AnalysisFileId])
                    .Any(match => ReferenceEquals(match.Reference, candidate) && match.Peak is not null));
            candidate.SampleCoverage = (double)detectedCount / nonBlankCount;
        }
        var coveredCandidates = automaticCandidates
            .Where(candidate => candidate.SampleCoverage * nonBlankCount >= minimumCoverageCount)
            .ToList();
        var exclusionAudits = exclusions
            .Select(exclusion => new AutomaticRtCorrectionExclusionAudit {
                AnchorName = exclusion.Name ?? string.Empty,
                Mass = exclusion.PrecursorMz,
                Rt = exclusion.ChromXs.RT.Value,
                MassTolerance = exclusion.MassTolerance,
                RtTolerance = exclusion.RetentionTimeTolerance,
                ReferenceFileId = reference.File.AnalysisFileId,
                ReferenceFileName = reference.File.AnalysisFileName,
                ExcludedCandidates = automaticCandidates.Count(candidate => IsExcludedBy(candidate, exclusion)),
            })
            .ToList();
        var excludedCandidates = new HashSet<PeakPoint>(automaticCandidates
            .Where(candidate => exclusions.Any(exclusion => IsExcludedBy(candidate, exclusion))));
        // A candidate that is a user anchor's own peak would be the same anchor twice: at the m/z
        // the anchor was observed at (within the larger of the alignment and the anchor's own m/z
        // tolerance) and within the match RT tolerance of it. It is neither an anchor nor support.
        var userAnchorPoints = userReferences.Concat(duplicateUserReferences).ToList();
        var ownPeakCandidates = new HashSet<PeakPoint>(coveredCandidates.Where(candidate => userAnchorPoints.Any(user =>
            Math.Abs(candidate.Mz - user.ObservedMz) <= Math.Max(mzTolerance, user.MassTolerance)
            && Math.Abs(candidate.Rt - user.Rt) <= parameter.MatchRtTolerance)));
        // A candidate whose peak top lies within CoElutionScans MS1 scans of a user anchor's,
        // whatever its m/z (the anchor's isotope peak, an adduct, a compound co-eluting with it),
        // adds nothing to the map but a second point at the anchor's RT, so it is no anchor. It
        // stays support for the outlier test: in a file where the user anchor's window took a
        // wrong peak it is the best evidence against it, and GroupCoElutingSupport already counts
        // a compound that does co-elute with the anchor there as the anchor's own group.
        var coElutingCandidates = new HashSet<PeakPoint>(coveredCandidates.Where(candidate => !ownPeakCandidates.Contains(candidate)
            && userAnchorPoints.Any(user => SameScan(candidate.Rt, user.Rt, referenceReading.CycleProfile))));
        var supportPool = coveredCandidates
            .Where(candidate => !excludedCandidates.Contains(candidate) && !ownPeakCandidates.Contains(candidate))
            .ToList();
        var automaticPool = supportPool.Where(candidate => !coElutingCandidates.Contains(candidate)).ToList();
        if (mode == AutomaticRtCorrectionAnchorMode.Hybrid) {
            // Beside user anchors, two automatic candidates on one peak top (an ion and its
            // isotope) are one anchor too; the better is offered. Automatic mode keeps its
            // selection as it always was.
            automaticPool = OnePerPeakTop(automaticPool, referenceReading.CycleProfile);
        }
        var selectedAnchors = mode switch {
            AutomaticRtCorrectionAnchorMode.UserOnly => userReferences.OrderBy(point => point.Rt).ToList(),
            AutomaticRtCorrectionAnchorMode.Hybrid => SelectDistributedAnchors(automaticPool, parameter.MaximumAnchorCount, parameter.MinimumAnchorCount, userReferences),
            _ => SelectDistributedAnchors(automaticPool, parameter.MaximumAnchorCount),
        };
        // With anchors a person named, the reference needs the minimum count of distinct RTs, the
        // points its map is drawn through; Automatic mode counts its anchors as it always did.
        var selectedCount = mode == AutomaticRtCorrectionAnchorMode.Automatic
            ? selectedAnchors.Count
            : selectedAnchors.Select(point => point.Rt).Distinct().Count();
        if (selectedCount < parameter.MinimumAnchorCount) {
            var atUserPeaks = ownPeakCandidates.Count + coElutingCandidates.Count(candidate => !excludedCandidates.Contains(candidate));
            var message = mode switch {
                AutomaticRtCorrectionAnchorMode.UserOnly =>
                    $"Automatic alignment RT correction detected only {selectedCount} distinct user anchor(s) in the reference file; at least {parameter.MinimumAnchorCount} are required.",
                AutomaticRtCorrectionAnchorMode.Hybrid =>
                    $"Automatic alignment RT correction retained only {selectedCount} anchor(s) at distinct RTs, {userReferences.Count} of them user anchors; of the {coveredCandidates.Count} automatic candidate(s) that passed cross-sample coverage, "
                    + $"{coveredCandidates.Count(excludedCandidates.Contains)} were excluded by the library's False rows and {atUserPeaks} lie at a user anchor's peak; at least {parameter.MinimumAnchorCount} are required.",
                _ =>
                    $"Automatic alignment RT correction retained only {selectedCount} anchor(s) after cross-sample coverage filtering; at least {parameter.MinimumAnchorCount} are required.",
            };
            // The exclusions go into the audit with the failure, since they may be its cause.
            throw new AutomaticRtCorrectionFailedException(
                message,
                PartialAudit(files, summaries, mode, libraryAnchors, readings, message, exclusionAudits),
                new InvalidOperationException(message));
        }
        for (var index = 0; index < selectedAnchors.Count; index++) {
            selectedAnchors[index].AnchorId = index + 1;
        }
        for (var index = 0; index < unusedUserReferences.Count; index++) {
            unusedUserReferences[index].AnchorId = selectedAnchors.Count + index + 1;
        }

        var supportReferences = new HashSet<PeakPoint>(supportPool.Concat(userReferences));
        // The reference needs the minimum anchor count. With anchors a person named, a file
        // other than the reference is corrected from two, the fewest a map can be drawn through:
        // three internal standards are a common set, and losing one in a file must not leave the
        // file on its original axis. Automatic mode keeps one minimum for both.
        var minimumAnchorsPerFile = mode == AutomaticRtCorrectionAnchorMode.Automatic
            ? parameter.MinimumAnchorCount
            : Math.Min(parameter.MinimumAnchorCount, MinimumUserModeAnchorsPerFile);
        var models = new Dictionary<int, IAlignmentRetentionTimeCorrectionModel>();
        var fileAudits = new List<AutomaticRtCorrectionFileAudit>();
        var anchorAudits = new List<AutomaticRtCorrectionAnchorAudit>();
        foreach (var summary in summaries) {
            var file = summary.File;
            var relevantMatches = matchesByFile[file.AnalysisFileId]
                .Where(match => selectedAnchors.Contains(match.Reference))
                .OrderBy(match => match.Reference.Rt)
                .ToList();
            var fileAudit = new AutomaticRtCorrectionFileAudit {
                FileId = file.AnalysisFileId,
                FileName = file.AnalysisFileName,
                FileType = file.AnalysisFileType,
                AnalyticalOrder = file.AnalysisFileAnalyticalOrder,
                CandidateCount = summary.Candidates.Count,
                MatchedAnchorCount = relevantMatches.Count(match => match.Peak is not null),
                ReferenceScore = summary.ReferenceScore,
                ModelSource = AutomaticRtCorrectionModelSource.Uncorrected,
                EstimatedScanInterval = cycleProfiles[file.AnalysisFileId].CycleTime,
                UserAnchorsDetected = mode == AutomaticRtCorrectionAnchorMode.Automatic ? null : UserAnchorsDetected(file),
                ExcludedAutomaticCandidates = mode == AutomaticRtCorrectionAnchorMode.Hybrid && file.AnalysisFileId == reference.File.AnalysisFileId
                    ? excludedCandidates.Count
                    : null,
            };

            if (file.AnalysisFileId == reference.File.AnalysisFileId) {
                foreach (var match in relevantMatches) {
                    match.Peak = match.Reference;
                    match.Status = "Reference";
                    match.Used = true;
                }
                var points = relevantMatches.Select(ToControlPoint).ToList();
                if (points.Select(point => point.OriginalRt).Distinct().Count() < 2) {
                    throw new InvalidOperationException("Automatic alignment RT correction found the reference file's anchors at fewer than two distinct RTs.");
                }
                var model = new PiecewiseLinearAlignmentRetentionTimeCorrectionModel(points);
                MarkPointsTheModelDropped(relevantMatches, model);
                models[file.AnalysisFileId] = model;
                SetAuditFromPoints(fileAudit, model.ControlPoints, AutomaticRtCorrectionModelSource.Reference, sortedPeakRts[file.AnalysisFileId]);
            }
            else if (file.AnalysisFileType != AnalysisFileType.Blank) {
                // Every reference candidate that passed the coverage filter and was matched
                // uniquely in this file is independent evidence of the file's local RT offset, and
                // so is every user anchor detected in it.
                var support = matchesByFile[file.AnalysisFileId]
                    .Where(match => match.Peak is not null && supportReferences.Contains(match.Reference))
                    .ToList();
                var accepted = RejectOutliersAndNonMonotonic(
                    relevantMatches,
                    support,
                    parameter.OutlierMadThreshold,
                    parameter.LocalSupportRtWindow,
                    cycleProfiles[file.AnalysisFileId],
                    cycleProfiles[reference.File.AnalysisFileId]);
                // Two automatic anchors can still share an original RT, and the model keeps one of
                // them: the anchors a file has are the RTs the map is drawn through.
                var distinctRts = accepted.Select(match => match.Peak!.Rt).Distinct().Count();
                if (distinctRts >= minimumAnchorsPerFile && distinctRts >= 2) {
                    foreach (var match in accepted) {
                        match.Status = "Used";
                        match.Used = true;
                    }
                    var model = new PiecewiseLinearAlignmentRetentionTimeCorrectionModel(accepted.Select(ToControlPoint));
                    MarkPointsTheModelDropped(accepted, model);
                    models[file.AnalysisFileId] = model;
                    SetAuditFromPoints(fileAudit, model.ControlPoints, AutomaticRtCorrectionModelSource.DetectedAnchors, sortedPeakRts[file.AnalysisFileId]);
                }
                else {
                    foreach (var match in accepted) {
                        match.Status = "InsufficientAnchors";
                        match.Used = false;
                    }
                    fileAudit.Note = $"Only {distinctRts} usable anchor(s); original RT is retained.";
                }
            }
            else {
                foreach (var match in relevantMatches.Where(match => match.Peak is not null)) {
                    match.Status = parameter.InterpolateBlankByAnalyticalOrder
                        ? "BlankInterpolateByOrder"
                        : "BlankNotCorrected";
                }
                fileAudit.Note = parameter.InterpolateBlankByAnalyticalOrder
                    ? "Blank model is estimated from neighboring non-Blank injections."
                    : "Blank interpolation by analytical order is disabled; original RT is retained.";
            }

            foreach (var match in unusedUserByFile[file.AnalysisFileId]) {
                match.Used = false;
                if (match.Reference.DuplicateOf is { } kept) {
                    match.Status = "Duplicate";
                    match.Note = $"Its reference peak lies within two MS1 scans of {kept.Name}'s; that row is the anchor. {match.Note}".TrimEnd();
                }
                else {
                    match.Status = "MissingInReference";
                }
            }

            fileAudits.Add(fileAudit);
            anchorAudits.AddRange(relevantMatches
                .Concat(unusedUserByFile[file.AnalysisFileId])
                .Select(match => ToAudit(file, match, cycleProfiles[file.AnalysisFileId])));
        }

        if (parameter.InterpolateBlankByAnalyticalOrder) {
            InterpolateBlankModels(files, models, fileAudits);
        }

        return new AutomaticAlignmentRetentionTimeCorrectionResult(
            new AlignmentRetentionTimeCorrectionCollection(reference.File.AnalysisFileId, models),
            fileAudits,
            anchorAudits,
            mode,
            exclusionAudits);
    }

    // What was read before the correction failed, for the person to see why: every file's row,
    // the failure in its note, every user anchor's detection in every file whose raw data was
    // read, and the exclusions when the reference was known. It maps no file.
    private static AutomaticAlignmentRetentionTimeCorrectionResult PartialAudit(
        IReadOnlyList<AnalysisFileBean> files,
        IReadOnlyList<FileSummary> summaries,
        AutomaticRtCorrectionAnchorMode mode,
        IReadOnlyList<MoleculeMsReference> libraryAnchors,
        IReadOnlyDictionary<int, AutomaticRtCorrectionRawFileReading> readings,
        string reason,
        IReadOnlyList<AutomaticRtCorrectionExclusionAudit>? exclusions) {
        AutomaticRtCorrectionRawFileReading? ReadingOf(AnalysisFileBean file) => readings.TryGetValue(file.AnalysisFileId, out var reading) ? reading : null;
        var nonBlank = files.Where(file => file.AnalysisFileType != AnalysisFileType.Blank).ToList();
        var fileAudits = summaries
            .Select(summary => new AutomaticRtCorrectionFileAudit {
                FileId = summary.File.AnalysisFileId,
                FileName = summary.File.AnalysisFileName,
                FileType = summary.File.AnalysisFileType,
                AnalyticalOrder = summary.File.AnalysisFileAnalyticalOrder,
                CandidateCount = summary.Candidates.Count,
                ReferenceScore = summary.ReferenceScore,
                ModelSource = AutomaticRtCorrectionModelSource.Uncorrected,
                EstimatedScanInterval = ReadingOf(summary.File)?.CycleProfile.CycleTime ?? 0d,
                UserAnchorsDetected = mode == AutomaticRtCorrectionAnchorMode.Automatic || ReadingOf(summary.File) is null
                    ? null
                    : DistinctUserAnchorsDetected(ReadingOf(summary.File)!),
                Note = $"Not corrected: {reason}",
            })
            .ToList();
        // In each file, a row whose peak lies within CoElutionScans MS1 scans of an earlier row's
        // is that row again, as the counts above and the reference's anchors have it.
        var sameAs = new Dictionary<(int FileId, int Index), string>();
        foreach (var file in files) {
            if (ReadingOf(file) is not { } reading) {
                continue;
            }
            var kept = new List<int>();
            for (var index = 0; index < libraryAnchors.Count; index++) {
                if (reading.UserAnchorObservations[index] is not { } observation) {
                    continue;
                }
                var earlier = kept
                    .Where(other => SameScan(observation.Rt, reading.UserAnchorObservations[other]!.Rt, reading.CycleProfile))
                    .Cast<int?>()
                    .FirstOrDefault();
                if (earlier is null) {
                    kept.Add(index);
                }
                else {
                    sameAs[(file.AnalysisFileId, index)] = libraryAnchors[earlier.Value].Name ?? string.Empty;
                }
            }
        }
        var anchorAudits = new List<AutomaticRtCorrectionAnchorAudit>();
        for (var index = 0; index < libraryAnchors.Count; index++) {
            var anchor = libraryAnchors[index];
            var coverage = (double)nonBlank.Count(file => ReadingOf(file)?.UserAnchorObservations[index] is not null)
                / Math.Max(1, nonBlank.Count);
            foreach (var file in files) {
                var reading = ReadingOf(file);
                if (reading is null) {
                    continue;
                }
                var observation = reading.UserAnchorObservations[index];
                var note = reading.UserAnchorDetectionNotes?[index] ?? string.Empty;
                var duplicateOf = sameAs.TryGetValue((file.AnalysisFileId, index), out var name) ? name : null;
                anchorAudits.Add(new AutomaticRtCorrectionAnchorAudit {
                    FileId = file.AnalysisFileId,
                    FileName = file.AnalysisFileName,
                    AnchorId = index + 1,
                    Mass = anchor.PrecursorMz,
                    ReferenceRt = double.NaN,
                    OriginalRt = observation?.Rt,
                    SampleCoverage = coverage,
                    Status = observation is null ? "Missing" : duplicateOf is null ? "Detected" : "Duplicate",
                    Ms1CycleTime = observation is null ? null : reading.CycleProfile.CycleTimeAt(observation.Rt),
                    Source = AutomaticRtCorrectionAnchorSource.User,
                    AnchorName = anchor.Name ?? string.Empty,
                    LibraryRt = anchor.ChromXs.RT.Value,
                    PeakHeight = observation?.Height,
                    ObservedMz = observation?.Mz,
                    Detection = duplicateOf is null
                        ? note
                        : $"Its peak lies within two MS1 scans of {duplicateOf}'s in this file, so it counts once. {note}".TrimEnd(),
                });
            }
        }
        return new AutomaticAlignmentRetentionTimeCorrectionResult(
            new AlignmentRetentionTimeCorrectionCollection(-1, new Dictionary<int, IAlignmentRetentionTimeCorrectionModel>()),
            fileAudits,
            anchorAudits,
            mode,
            exclusions);
    }

    // Of candidates whose peak tops lie within CoElutionScans MS1 scans of each other, the one of
    // highest coverage, then quality, stands for them all.
    private static List<PeakPoint> OnePerPeakTop(IReadOnlyList<PeakPoint> candidates, Ms1CycleProfile cycleProfile) {
        var kept = new List<PeakPoint>();
        foreach (var candidate in candidates.OrderByDescending(candidate => candidate.SampleCoverage).ThenByDescending(candidate => candidate.QualityScore).ThenBy(candidate => candidate.Rt)) {
            if (!kept.Any(other => SameScan(candidate.Rt, other.Rt, cycleProfile))) {
                kept.Add(candidate);
            }
        }
        return kept.OrderBy(candidate => candidate.Rt).ToList();
    }

    // The model keeps one control point per original RT; a matched anchor whose point it dropped
    // is not used, and says so, so the anchor rows agree with the file's used-anchor count.
    private static void MarkPointsTheModelDropped(IEnumerable<AnchorMatch> matches, IAlignmentRetentionTimeCorrectionModel model) {
        var kept = new HashSet<int>(model.ControlPoints.Select(point => point.AnchorId));
        foreach (var match in matches.Where(match => match.Used && !kept.Contains(match.Reference.AnchorId))) {
            match.Used = false;
            match.Status = "SameOriginalRt";
        }
    }

    private static void Validate(
        IReadOnlyList<AnalysisFileBean> files,
        AutomaticAlignmentRetentionTimeCorrectionParameter parameter,
        double mzTolerance,
        Delegate peakLoader) {
        if (files is null || files.Count == 0) throw new ArgumentException("At least one analysis file is required.", nameof(files));
        if (peakLoader is null) throw new ArgumentNullException(nameof(peakLoader));
        ValidateParameter(parameter, mzTolerance);
    }

    /// <summary>
    /// Refuses settings the correction cannot run with, naming the setting. Build
    /// calls it first; a caller can call it before it reads anything heavy, so a mistyped
    /// setting stops a run in seconds rather than after peak picking.
    /// </summary>
    public static void ValidateParameter(AutomaticAlignmentRetentionTimeCorrectionParameter parameter, double mzTolerance) {
        if (parameter is null) throw new ArgumentNullException(nameof(parameter));
        Require(mzTolerance > 0d, nameof(mzTolerance), mzTolerance, "The MS1 tolerance for alignment must be positive.");
        Require(parameter.RtBinWidth > 0d, nameof(parameter.RtBinWidth), parameter.RtBinWidth, "The RT bin width must be positive.");
        Require(parameter.MatchRtTolerance > 0d, nameof(parameter.MatchRtTolerance), parameter.MatchRtTolerance, "The match RT tolerance must be positive.");
        Require(parameter.MinimumAnchorCount >= 2, nameof(parameter.MinimumAnchorCount), parameter.MinimumAnchorCount, "The minimum anchor count must be 2 or more: a map needs two anchors.");
        Require(parameter.MaximumAnchorCount >= parameter.MinimumAnchorCount, nameof(parameter.MaximumAnchorCount), parameter.MaximumAnchorCount, "The maximum anchor count must not be below the minimum anchor count.");
        Require(parameter.IntensityQuantile >= 0d && parameter.IntensityQuantile <= 1d, nameof(parameter.IntensityQuantile), parameter.IntensityQuantile, "The intensity quantile must be between 0 and 1.");
        Require(parameter.MaximumPeakWidthQuantile >= 0d && parameter.MaximumPeakWidthQuantile <= 1d, nameof(parameter.MaximumPeakWidthQuantile), parameter.MaximumPeakWidthQuantile, "The maximum peak width quantile must be between 0 and 1.");
        Require(parameter.MinimumSampleCoverage >= 0d && parameter.MinimumSampleCoverage <= 1d, nameof(parameter.MinimumSampleCoverage), parameter.MinimumSampleCoverage, "The minimum sample coverage must be between 0 and 1.");
        Require(parameter.ReferenceCentralityWeight >= 0d && parameter.ReferenceCentralityWeight <= 1d, nameof(parameter.ReferenceCentralityWeight), parameter.ReferenceCentralityWeight, "The reference centrality weight must be between 0 and 1.");
        Require(parameter.LocalSupportRtWindow >= 0d, nameof(parameter.LocalSupportRtWindow), parameter.LocalSupportRtWindow, "The local support RT window must be 0 or more.");
        Require(parameter.AnchorPeakSelectionRtWeight >= 0d && parameter.AnchorPeakSelectionRtWeight <= 1d, nameof(parameter.AnchorPeakSelectionRtWeight), parameter.AnchorPeakSelectionRtWeight, "The anchor peak selection RT weight must be between 0 and 1.");
    }

    // NaN fails every comparison above, so a NaN setting is refused too.
    private static void Require(bool condition, string name, object value, string message) {
        if (!condition) throw new ArgumentOutOfRangeException(name, value, message);
    }

    // The user anchors detected in a file, counted as the reference file's are kept: in library
    // order, a row whose peak lies within CoElutionScans MS1 scans of a row already counted is a
    // duplicate of it.
    private static int DistinctUserAnchorsDetected(AutomaticRtCorrectionRawFileReading reading) {
        var kept = new List<double>();
        foreach (var observation in reading.UserAnchorObservations) {
            if (observation is not null && !kept.Any(rt => SameScan(observation.Rt, rt, reading.CycleProfile))) {
                kept.Add(observation.Rt);
            }
        }
        return kept.Count;
    }

    // Whether two peak tops lie within CoElutionScans MS1 scan steps of each other; with the
    // file's scans unknown, whether they are at the same RT.
    private static bool SameScan(double rt, double other, Ms1CycleProfile cycleProfile) {
        var scan = cycleProfile.NearestScanIndex(rt);
        var otherScan = cycleProfile.NearestScanIndex(other);
        if (scan < 0 || otherScan < 0) return Math.Abs(rt - other) <= 1e-9;
        return Math.Abs(scan - otherScan) <= CoElutionScans;
    }

    private static FileSummary CreateSummary(
        AnalysisFileBean file,
        IReadOnlyList<ChromatogramPeakFeature> features,
        AutomaticAlignmentRetentionTimeCorrectionParameter parameter,
        double mzTolerance) {
        var peaks = CreatePeakPoints(features);
        if (peaks.Count == 0) {
            return new FileSummary(file, new List<PeakPoint>(), 0d);
        }
        var intensityThreshold = Quantile(peaks.Select(peak => peak.Height), parameter.IntensityQuantile);
        var widths = peaks.Where(peak => peak.Width > 0d).Select(peak => peak.Width).ToList();
        var widthThreshold = widths.Count == 0 ? double.PositiveInfinity : Quantile(widths, parameter.MaximumPeakWidthQuantile);
        var isolated = FindIsolatedPeakIndexes(peaks, mzTolerance, parameter.MatchRtTolerance);
        var candidates = peaks
            .Where(peak => peak.Height >= intensityThreshold
                && peak.Width > 0d
                && peak.Width <= widthThreshold
                && peak.SignalToNoise >= parameter.MinimumSignalToNoise
                && peak.GaussianSimilarity >= parameter.MinimumGaussianSimilarity
                && peak.IdealSlope >= parameter.MinimumIdealSlope
                && isolated.Contains(peak.Index))
            .GroupBy(peak => (int)Math.Floor(peak.Rt / parameter.RtBinWidth))
            // Retain a small shortlist until cross-sample coverage is known. Keeping only the
            // best peak here can discard a ubiquitous anchor in favor of a stronger
            // biology-specific feature from the same RT bin.
            .SelectMany(group => group
                .OrderByDescending(peak => peak.QualityScore)
                .Take(CandidateLimitPerRtBin))
            .OrderBy(peak => peak.Rt)
            .ToList();
        var minRt = peaks.Min(peak => peak.Rt);
        var maxRt = peaks.Max(peak => peak.Rt);
        var totalBins = Math.Max(1, (int)Math.Ceiling((maxRt - minRt) / parameter.RtBinWidth));
        var occupiedBins = candidates
            .Select(peak => (int)Math.Floor(peak.Rt / parameter.RtBinWidth))
            .Distinct()
            .Count();
        return new FileSummary(file, candidates, Math.Min(1d, (double)occupiedBins / totalBins));
    }

    private static List<PeakPoint> CreatePeakPoints(IReadOnlyList<ChromatogramPeakFeature> features) {
        var raw = features
            .Select((feature, index) => new PeakPoint {
                Index = index,
                PeakId = feature.PeakID,
                Mz = feature.PrecursorMz,
                Rt = feature.ChromXs.RT.Value,
                Width = feature.PeakWidth(ChromXType.RT),
                Height = feature.PeakFeature.PeakHeightTop,
                SignalToNoise = feature.PeakShape.SignalToNoise,
                GaussianSimilarity = feature.PeakShape.GaussianSimilarityValue,
                IdealSlope = feature.PeakShape.IdealSlopeValue,
                Symmetry = feature.PeakShape.SymmetryValue,
            })
            .Where(peak => peak.Mz > 0d && peak.Rt >= 0d && peak.Height > 0d)
            .ToList();
        if (raw.Count == 0) return raw;
        var maximumLogHeight = Math.Max(1d, raw.Max(peak => Math.Log10(peak.Height + 1d)));
        var medianWidth = Median(raw.Where(peak => peak.Width > 0d).Select(peak => peak.Width));
        foreach (var peak in raw) {
            var intensity = Math.Log10(peak.Height + 1d) / maximumLogHeight;
            var sn = Math.Max(0d, peak.SignalToNoise) / (Math.Max(0d, peak.SignalToNoise) + 10d);
            var width = medianWidth > 0d ? medianWidth / (medianWidth + Math.Max(0d, peak.Width)) : 0d;
            peak.QualityScore = 0.35d * Clamp01(intensity)
                + 0.20d * Clamp01(sn)
                + 0.15d * Clamp01(peak.GaussianSimilarity)
                + 0.15d * Clamp01(peak.IdealSlope)
                + 0.10d * Clamp01(peak.Symmetry)
                + 0.05d * Clamp01(width);
        }
        return raw;
    }

    private static HashSet<int> FindIsolatedPeakIndexes(IReadOnlyList<PeakPoint> peaks, double mzTolerance, double rtTolerance) {
        var ordered = peaks.OrderBy(peak => peak.Mz).ToList();
        var isolated = new HashSet<int>(ordered.Select(peak => peak.Index));
        var left = 0;
        for (var right = 0; right < ordered.Count; right++) {
            while (ordered[right].Mz - ordered[left].Mz > mzTolerance) left++;
            for (var index = left; index < right; index++) {
                if (Math.Abs(ordered[right].Rt - ordered[index].Rt) <= rtTolerance) {
                    isolated.Remove(ordered[right].Index);
                    isolated.Remove(ordered[index].Index);
                }
            }
        }
        return isolated;
    }

    private static double CalculateCentrality(
        FileSummary target,
        IReadOnlyList<FileSummary> summaries,
        AutomaticAlignmentRetentionTimeCorrectionParameter parameter,
        double mzTolerance) {
        var deviations = new List<double>();
        foreach (var candidate in target.Candidates) {
            var retentionTimes = new List<double> { candidate.Rt };
            foreach (var summary in summaries) {
                if (summary.File.AnalysisFileId == target.File.AnalysisFileId) continue;
                var matches = summary.Candidates
                    .Where(peak => Math.Abs(peak.Mz - candidate.Mz) <= mzTolerance
                        && Math.Abs(peak.Rt - candidate.Rt) <= parameter.MatchRtTolerance)
                    .ToList();
                if (matches.Count == 1) retentionTimes.Add(matches[0].Rt);
            }
            if (retentionTimes.Count >= 2) deviations.Add(Math.Abs(candidate.Rt - Median(retentionTimes)));
        }
        if (deviations.Count == 0) return 0d;
        return 1d - Math.Min(1d, Median(deviations) / parameter.MatchRtTolerance);
    }

    // Automatic and Hybrid: a non-Blank file with enough anchor candidates, its own automatic
    // candidates and, in Hybrid mode, the user anchors detected in it, scored by coverage and
    // centrality.
    private static FileSummary SelectAutomaticReference(
        IReadOnlyList<FileSummary> summaries,
        AutomaticAlignmentRetentionTimeCorrectionParameter parameter,
        double mzTolerance,
        Func<FileSummary, int> userAnchorsDetected) {
        var eligibleReferences = summaries
            .Where(summary => summary.File.AnalysisFileType != AnalysisFileType.Blank
                && summary.Candidates.Count + userAnchorsDetected(summary) >= parameter.MinimumAnchorCount)
            .ToList();
        if (eligibleReferences.Count == 0) {
            throw new InvalidOperationException("Automatic alignment RT correction could not find a non-Blank file with enough high-quality anchor candidates.");
        }

        foreach (var summary in eligibleReferences) {
            summary.Centrality = CalculateCentrality(summary, eligibleReferences, parameter, mzTolerance);
            var weight = Clamp01(parameter.ReferenceCentralityWeight);
            summary.ReferenceScore = summary.Coverage * (1d - weight) + summary.Centrality * weight;
        }
        return SelectReference(eligibleReferences, parameter.ReferenceFileId);
    }

    // UserOnly: a non-Blank file with at least the minimum anchor count of user anchors detected.
    // The most detected wins, because an anchor missing from the reference has no target RT and
    // corrects no file; then the reference score, in which the share of anchors detected takes
    // the place of RT coverage and the closeness of the file's anchor RTs to their median over the
    // non-Blank files, in units of each anchor's RT tolerance, the place of centrality.
    private static FileSummary SelectUserReference(
        IReadOnlyList<FileSummary> summaries,
        IReadOnlyList<MoleculeMsReference> anchors,
        IReadOnlyDictionary<int, AutomaticRtCorrectionRawFileReading> readings,
        AutomaticAlignmentRetentionTimeCorrectionParameter parameter) {
        IReadOnlyList<AutomaticRtCorrectionUserAnchorObservation?> ObservationsOf(FileSummary summary) => readings[summary.File.AnalysisFileId].UserAnchorObservations;
        // Two rows that took one peak are one anchor, here as in the reference file.
        int Detected(FileSummary summary) => DistinctUserAnchorsDetected(readings[summary.File.AnalysisFileId]);
        var nonBlank = summaries.Where(summary => summary.File.AnalysisFileType != AnalysisFileType.Blank).ToList();
        var eligible = nonBlank.Where(summary => Detected(summary) >= parameter.MinimumAnchorCount).ToList();
        if (eligible.Count == 0) {
            throw new InvalidOperationException(
                $"Automatic alignment RT correction found no non-Blank file in which at least {parameter.MinimumAnchorCount} of the {anchors.Count} user anchors were detected.");
        }

        var medianRts = anchors
            .Select((_, index) => nonBlank
                .Select(summary => ObservationsOf(summary)[index])
                .Where(observation => observation is not null)
                .Select(observation => observation!.Rt)
                .ToList())
            .Select(rts => rts.Count == 0 ? double.NaN : Median(rts))
            .ToList();
        var weight = Clamp01(parameter.ReferenceCentralityWeight);
        foreach (var summary in eligible) {
            var observations = ObservationsOf(summary);
            var deviations = new List<double>();
            for (var index = 0; index < anchors.Count; index++) {
                if (observations[index] is { } observation) {
                    deviations.Add(Math.Abs(observation.Rt - medianRts[index]) / anchors[index].RetentionTimeTolerance);
                }
            }
            summary.Centrality = 1d - Math.Min(1d, Median(deviations));
            summary.ReferenceScore = (double)Detected(summary) / anchors.Count * (1d - weight) + summary.Centrality * weight;
        }

        if (parameter.ReferenceFileId >= 0) {
            return eligible.FirstOrDefault(summary => summary.File.AnalysisFileId == parameter.ReferenceFileId)
                ?? throw new InvalidOperationException(
                    $"Automatic alignment RT correction reference file ID {parameter.ReferenceFileId} is missing, Blank, or has fewer than {parameter.MinimumAnchorCount} user anchors detected.");
        }
        var medianOrder = Median(eligible.Select(summary => (double)summary.File.AnalysisFileAnalyticalOrder));
        return eligible
            .OrderByDescending(Detected)
            .ThenByDescending(summary => summary.ReferenceScore)
            .ThenBy(summary => Math.Abs(summary.File.AnalysisFileAnalyticalOrder - medianOrder))
            .ThenBy(summary => summary.File.AnalysisFileId)
            .First();
    }

    private static PeakPoint UserReferencePoint(MoleculeMsReference anchor, int index, AutomaticRtCorrectionUserAnchorObservation? observation) {
        // The target RT is where the anchor was detected in the reference file, NaN when it was
        // not; the library's RT only centred the search. QualityScore 1 is not a score: it makes
        // the model keep a user anchor over an automatic one at the same original RT.
        return new PeakPoint {
            Index = -1,
            PeakId = -1,
            Mz = anchor.PrecursorMz,
            ObservedMz = observation?.Mz ?? double.NaN,
            MassTolerance = anchor.MassTolerance,
            Rt = observation?.Rt ?? double.NaN,
            Height = observation?.Height ?? 0d,
            QualityScore = 1d,
            IsUser = true,
            UserIndex = index,
            Name = anchor.Name ?? string.Empty,
            LibraryRt = anchor.ChromXs.RT.Value,
        };
    }

    private static AnchorMatch MatchUser(PeakPoint reference, AutomaticRtCorrectionRawFileReading reading) {
        var observation = reading.UserAnchorObservations[reference.UserIndex];
        var note = reading.UserAnchorDetectionNotes?[reference.UserIndex] ?? string.Empty;
        if (observation is null) return new AnchorMatch(reference, null, "Missing") { Note = note };
        return new AnchorMatch(reference, new PeakPoint {
            Index = -1,
            PeakId = -1,
            Mz = observation.Mz,
            ObservedMz = observation.Mz,
            Rt = observation.Rt,
            Height = observation.Height,
            QualityScore = 1d,
            IsUser = true,
            UserIndex = reference.UserIndex,
        }, "Matched") { Note = note };
    }

    // A False row covers a candidate within its m/z and RT tolerances, its RT read on the
    // reference file's axis, where the review that wrote it saw the candidate.
    private static bool IsExcludedBy(PeakPoint candidate, MoleculeMsReference exclusion) {
        return Math.Abs(candidate.Mz - exclusion.PrecursorMz) <= exclusion.MassTolerance
            && Math.Abs(candidate.Rt - exclusion.ChromXs.RT.Value) <= exclusion.RetentionTimeTolerance;
    }

    private static FileSummary SelectReference(IReadOnlyList<FileSummary> eligible, int requestedFileId) {
        if (requestedFileId >= 0) {
            var requested = eligible.FirstOrDefault(summary => summary.File.AnalysisFileId == requestedFileId);
            if (requested is null) {
                throw new InvalidOperationException($"Automatic alignment RT correction reference file ID {requestedFileId} is missing, Blank, or has too few anchor candidates.");
            }
            return requested;
        }
        var medianOrder = Median(eligible.Select(summary => (double)summary.File.AnalysisFileAnalyticalOrder));
        return eligible
            .OrderByDescending(summary => summary.ReferenceScore)
            .ThenByDescending(summary => summary.Candidates.Count)
            .ThenBy(summary => Math.Abs(summary.File.AnalysisFileAnalyticalOrder - medianOrder))
            .ThenBy(summary => summary.File.AnalysisFileId)
            .First();
    }

    private static AnchorMatch Match(PeakPoint reference, IReadOnlyList<PeakPoint> peaks, double rtTolerance, double mzTolerance) {
        var candidates = peaks
            .Where(peak => Math.Abs(peak.Mz - reference.Mz) <= mzTolerance
                && Math.Abs(peak.Rt - reference.Rt) <= rtTolerance)
            .OrderBy(peak => Math.Abs(peak.Mz - reference.Mz) / mzTolerance
                + Math.Abs(peak.Rt - reference.Rt) / rtTolerance)
            .ToList();
        if (candidates.Count == 0) return new AnchorMatch(reference, null, "Missing");
        if (candidates.Count > 1) return new AnchorMatch(reference, null, "Ambiguous");
        return new AnchorMatch(reference, candidates[0], "Matched");
    }

    private static List<PeakPoint> SelectDistributedAnchors(IReadOnlyList<PeakPoint> candidates, int maximumCount) {
        if (candidates.Count <= maximumCount) return candidates.OrderBy(candidate => candidate.Rt).ToList();
        var ordered = candidates.OrderBy(candidate => candidate.Rt).ToList();
        var minimumRt = ordered[0].Rt;
        var span = Math.Max(double.Epsilon, ordered[ordered.Count - 1].Rt - minimumRt);
        var selected = ordered
            .GroupBy(candidate => Math.Min(maximumCount - 1, (int)Math.Floor((candidate.Rt - minimumRt) / span * maximumCount)))
            .Select(group => group
                .OrderByDescending(candidate => candidate.SampleCoverage)
                .ThenByDescending(candidate => candidate.QualityScore)
                .First())
            .ToList();

        // Time bins can be empty when candidates cluster in only part of a chromatogram. Fill
        // those holes with the candidate farthest from the anchors already selected, with peak
        // quality as a tie breaker. This keeps the configured anchor count while still spanning
        // the observed RT range.
        while (selected.Count < maximumCount) {
            var next = ordered
                .Where(candidate => !selected.Contains(candidate))
                .OrderByDescending(candidate => candidate.SampleCoverage)
                .ThenByDescending(candidate => selected.Min(anchor => Math.Abs(candidate.Rt - anchor.Rt)) / span)
                .ThenByDescending(candidate => candidate.QualityScore)
                .First();
            selected.Add(next);
        }
        return selected.OrderBy(candidate => candidate.Rt).ToList();
    }

    /// <summary>
    /// Hybrid mode: every one of <paramref name="fixedAnchors"/>, the user anchors, and automatic
    /// candidates up to <paramref name="maximumCount"/> anchors in all. When every candidate fits,
    /// every one is taken, as in the automatic selection. Otherwise the RT span of both is cut into
    /// <paramref name="maximumCount"/> bins; a bin holding a user anchor takes no automatic one,
    /// and each other bin offers its best candidate, as in the automatic selection. The offers,
    /// then the other candidates outside the user anchors' bins, are taken one at a time, by
    /// coverage, then by distance from the anchors already taken, then by quality. Only when that
    /// leaves fewer than <paramref name="minimumCount"/> anchors are candidates in the user
    /// anchors' bins taken, the same way, up to <paramref name="minimumCount"/>: a run with room
    /// for anchors is not left uncorrected for the sake of the bins.
    /// </summary>
    private static List<PeakPoint> SelectDistributedAnchors(IReadOnlyList<PeakPoint> candidates, int maximumCount, int minimumCount, IReadOnlyList<PeakPoint> fixedAnchors) {
        var selected = fixedAnchors.ToList();
        var automaticCount = maximumCount - selected.Count;
        if (automaticCount <= 0 || candidates.Count == 0) return selected.OrderBy(anchor => anchor.Rt).ToList();
        if (selected.Count == 0) return SelectDistributedAnchors(candidates, maximumCount);
        if (candidates.Count <= automaticCount) return selected.Concat(candidates).OrderBy(anchor => anchor.Rt).ToList();

        var all = candidates.Concat(selected).OrderBy(point => point.Rt).ToList();
        var minimumRt = all[0].Rt;
        var span = Math.Max(double.Epsilon, all[all.Count - 1].Rt - minimumRt);
        int Bin(PeakPoint point) => Math.Min(maximumCount - 1, (int)Math.Floor((point.Rt - minimumRt) / span * maximumCount));
        var userBins = new HashSet<int>(selected.Select(Bin));
        var open = candidates.Where(candidate => !userBins.Contains(Bin(candidate))).ToList();
        var offers = open
            .GroupBy(Bin)
            .Select(group => group
                .OrderByDescending(candidate => candidate.SampleCoverage)
                .ThenByDescending(candidate => candidate.QualityScore)
                .First())
            .ToList();
        var crowded = candidates.Where(candidate => userBins.Contains(Bin(candidate))).ToList();
        var pools = new[] {
            (Pool: offers, Limit: maximumCount),
            (Pool: open.Except(offers).ToList(), Limit: maximumCount),
            (Pool: crowded, Limit: Math.Min(maximumCount, minimumCount)),
        };
        foreach (var (pool, limit) in pools) {
            while (selected.Count < limit && pool.Count > 0) {
                var next = pool
                    .OrderByDescending(candidate => candidate.SampleCoverage)
                    .ThenByDescending(candidate => selected.Min(anchor => Math.Abs(candidate.Rt - anchor.Rt)) / span)
                    .ThenByDescending(candidate => candidate.QualityScore)
                    .First();
                pool.Remove(next);
                selected.Add(next);
            }
        }
        return selected.OrderBy(anchor => anchor.Rt).ToList();
    }

    /// <summary>
    /// Rejects anchor matches whose offset disagrees with the file's own RT drift, then removes
    /// matches that would make the map non-monotonic.
    /// <para>
    /// An anchor is judged against the median offset of the other compounds matched in the same
    /// file within <paramref name="localWindow"/> minutes of it (status LocalOutlier).
    /// Judging locally keeps an anchor whose offset follows an RT-dependent drift that independent
    /// peaks around it share, which a single median over the whole run would reject. Only when
    /// fewer than <see cref="MinimumLocalSupportCount"/> such neighbours exist is the anchor judged
    /// against the median of all of the file's anchors, as before (status MadOutlier).
    /// </para>
    /// <para>
    /// A neighbour is a compound, not a reference candidate: a compound's isotope peaks and
    /// adducts are isolated by m/z and so are candidates of their own, but they share its peak-top
    /// RT and its offset, also when the match is wrong. Candidates whose peak tops coincide in
    /// both files (<see cref="GroupCoElutingSupport"/>) count once, at their median offset, and
    /// the judged anchor's own group is no support for it. Otherwise a wrong match and its isotope
    /// peaks outvote a true neighbour where fewer than three compounds lie within the window.
    /// </para>
    /// <para>
    /// The robust scale is max(1.4826 MAD, floor), the floor being the file's MS1 cycle time
    /// around the judged anchor's RT (<see cref="Ms1CycleProfile.CycleTimeAt(double)"/>). Offsets
    /// are quantised to whole scans, so a MAD of zero must not disable the test, and a one-scan
    /// deviation must not count as an outlier. The cycle is read where the anchor is because a
    /// data-dependent file's MS1 cycle changes along the run; a file-wide value can be the short
    /// cycle of MS1-only stretches and would again reject one-scan offsets in the gradient.
    /// </para>
    /// <para>
    /// A user anchor is judged the same way but never rejected (<see cref="Reject"/>), and wins
    /// an RT-order conflict with an automatic anchor (<see cref="NonMonotonicLoser"/>).
    /// </para>
    /// </summary>
    private static List<AnchorMatch> RejectOutliersAndNonMonotonic(
        List<AnchorMatch> matches,
        IReadOnlyList<AnchorMatch> support,
        double madThreshold,
        double localWindow,
        Ms1CycleProfile cycleProfile,
        Ms1CycleProfile referenceCycleProfile) {
        var accepted = matches.Where(match => match.Peak is not null).ToList();
        var offsets = accepted.Select(Offset).ToList();
        if (offsets.Count > 0 && madThreshold > 0d) {
            var groups = localWindow > 0d
                ? GroupCoElutingSupport(support, cycleProfile, referenceCycleProfile)
                : new Dictionary<AnchorMatch, int>();
            var globalMedian = Median(offsets);
            var globalMad = 1.4826d * Median(offsets.Select(value => Math.Abs(value - globalMedian)));
            var rejected = new List<AnchorMatch>();
            foreach (var match in accepted) {
                var offset = Offset(match);
                var scaleFloor = cycleProfile.CycleTimeAt(match.Peak!.Rt);
                match.Ms1CycleTime = scaleFloor;
                var ownGroup = groups.TryGetValue(match, out var group) ? group : -1;
                // One offset per co-eluting group: a compound is one neighbour however many of its
                // ions are candidates, and the anchor's own group is no neighbour of it.
                var neighbours = localWindow > 0d
                    ? support
                        .Where(other => !ReferenceEquals(other.Reference, match.Reference)
                            && groups[other] != ownGroup
                            && Math.Abs(other.Peak!.Rt - match.Peak!.Rt) <= localWindow)
                        .GroupBy(other => groups[other])
                        .Select(members => Median(members.Select(Offset)))
                        .ToList()
                    : new List<double>();
                match.LocalSupportCount = neighbours.Count;
                if (neighbours.Count >= MinimumLocalSupportCount) {
                    var localMedian = Median(neighbours);
                    var localScale = RobustScale(neighbours, localMedian, scaleFloor);
                    match.OutlierTest = "Local";
                    match.ExpectedOffset = localMedian;
                    match.OutlierScale = localScale;
                    if (localScale > 0d && Math.Abs(offset - localMedian) / localScale > madThreshold) {
                        Reject(match, "LocalOutlier", rejected);
                    }
                }
                else {
                    var globalScale = Math.Max(globalMad, scaleFloor);
                    match.OutlierTest = "Global";
                    match.ExpectedOffset = globalMedian;
                    match.OutlierScale = globalScale;
                    // A zero scale (MAD of zero and no MS1 cycle time known) cannot judge anything.
                    if (globalScale > 0d && Math.Abs(offset - globalMedian) / globalScale > madThreshold) {
                        // Of three offsets the median is one of them, and the anchor at the far end
                        // of a drift that grows with RT is the odd one out however right it is; a
                        // user anchor is marked only when more offsets than that judged it.
                        Reject(match, "MadOutlier", rejected, flagUserAnchor: offsets.Count >= MinimumOffsetsToFlagUserAnchor);
                    }
                }
            }
            foreach (var match in rejected) {
                accepted.Remove(match);
            }
        }

        accepted = accepted.OrderBy(match => match.Peak!.Rt).ToList();
        var changed = true;
        while (changed) {
            changed = false;
            for (var index = 1; index < accepted.Count; index++) {
                var earlier = accepted[index - 1];
                var later = accepted[index];
                // Two anchors at one original RT cannot both hold either, and the model would keep
                // one of them without a word. A user anchor's window can take the peak another
                // row's took; two automatic anchors keep the rule they always had.
                var userInvolved = earlier.Reference.IsUser || later.Reference.IsUser;
                if (later.Reference.Rt > earlier.Reference.Rt && (later.Peak!.Rt > earlier.Peak!.Rt || !userInvolved)) continue;
                var remove = NonMonotonicLoser(earlier, later, accepted);
                remove.Status = "NonMonotonic";
                accepted.Remove(remove);
                changed = true;
                break;
            }
        }
        return accepted;
    }

    // An automatic anchor the test rejects is dropped; a user anchor is kept, and marked when the
    // test had enough to go on.
    private static void Reject(AnchorMatch match, string status, List<AnchorMatch> rejected, bool flagUserAnchor = true) {
        if (match.Reference.IsUser) {
            match.OutlierFlagged = flagUserAnchor;
            return;
        }
        match.Status = status;
        rejected.Add(match);
    }

    // Which of two anchors out of RT order goes: the automatic one when only one is a user
    // anchor; of two user anchors, the one whose offset lies farther from the median offset of
    // the anchors still accepted, the later one on a tie; of two automatic anchors, the one of
    // lower quality, the earlier one on a tie.
    private static AnchorMatch NonMonotonicLoser(AnchorMatch earlier, AnchorMatch later, IReadOnlyList<AnchorMatch> accepted) {
        if (earlier.Reference.IsUser != later.Reference.IsUser) {
            return earlier.Reference.IsUser ? later : earlier;
        }
        if (earlier.Reference.IsUser) {
            var median = Median(accepted.Select(Offset));
            return Math.Abs(Offset(earlier) - median) > Math.Abs(Offset(later) - median) ? earlier : later;
        }
        return later.Peak!.QualityScore < earlier.Peak!.QualityScore ? later : earlier;
    }

    /// <summary>
    /// Numbers the support matches so that matches whose peak tops lie within
    /// <see cref="CoElutionScans"/> MS1 scan steps of each other both in the reference file and in
    /// the judged file share a number (single linkage). A peak top is placed at the file's nearest
    /// MS1 scan (<see cref="Ms1CycleProfile.NearestScanIndex(double)"/>), so the limit follows the
    /// spacing of the scans at the peak tops, not a median cycle. Where a file's scans are unknown
    /// only identical peak-top RTs are grouped. Merging two compounds that do elute a scan or two
    /// apart in both files only lowers the support count, which at worst returns the anchor to the
    /// run-wide test.
    /// </summary>
    private static Dictionary<AnchorMatch, int> GroupCoElutingSupport(
        IReadOnlyList<AnchorMatch> support,
        Ms1CycleProfile cycleProfile,
        Ms1CycleProfile referenceCycleProfile) {
        var ordered = support.OrderBy(match => match.Peak!.Rt).ToList();
        var fileRts = ordered.Select(match => match.Peak!.Rt).ToArray();
        var referenceRts = ordered.Select(match => match.Reference.Rt).ToArray();
        var fileScans = fileRts.Select(cycleProfile.NearestScanIndex).ToArray();
        var referenceScans = referenceRts.Select(referenceCycleProfile.NearestScanIndex).ToArray();
        var parent = Enumerable.Range(0, ordered.Count).ToArray();
        int Root(int index) {
            while (parent[index] != index) {
                parent[index] = parent[parent[index]];
                index = parent[index];
            }
            return index;
        }
        for (var i = 0; i < ordered.Count; i++) {
            for (var j = i + 1; j < ordered.Count; j++) {
                // Ordered by the judged file's RT, so its nearest-scan positions do not decrease:
                // once j is too far from i in the judged file, every later j is too.
                if (!CoElute(fileRts, fileScans, i, j)) break;
                if (!CoElute(referenceRts, referenceScans, i, j)) continue;
                parent[Root(j)] = Root(i);
            }
        }
        var groups = new Dictionary<AnchorMatch, int>();
        for (var index = 0; index < ordered.Count; index++) {
            groups[ordered[index]] = Root(index);
        }
        return groups;
    }

    // Whether peak tops i and j lie within CoElutionScans MS1 scan steps of each other; with the
    // file's scans unknown (position -1), whether they are at the same RT.
    private static bool CoElute(double[] rts, int[] scans, int i, int j) {
        if (scans[i] < 0 || scans[j] < 0) return Math.Abs(rts[j] - rts[i]) <= 1e-9;
        return Math.Abs(scans[j] - scans[i]) <= CoElutionScans;
    }

    private static double Offset(AnchorMatch match) {
        return match.Reference.Rt - match.Peak!.Rt;
    }

    private static double RobustScale(IReadOnlyList<double> values, double median, double floor) {
        var mad = Median(values.Select(value => Math.Abs(value - median)));
        return Math.Max(1.4826d * mad, Math.Max(0d, floor));
    }

    private static AlignmentRetentionTimeCorrectionControlPoint ToControlPoint(AnchorMatch match) {
        return new AlignmentRetentionTimeCorrectionControlPoint(
            match.Reference.AnchorId,
            match.Reference.Mz,
            match.Peak!.Rt,
            match.Reference.Rt,
            match.Peak.QualityScore);
    }

    private static AutomaticRtCorrectionAnchorAudit ToAudit(AnalysisFileBean file, AnchorMatch match, Ms1CycleProfile cycleProfile) {
        return new AutomaticRtCorrectionAnchorAudit {
            FileId = file.AnalysisFileId,
            FileName = file.AnalysisFileName,
            AnchorId = match.Reference.AnchorId,
            Mass = match.Reference.Mz,
            ReferenceRt = match.Reference.Rt,
            OriginalRt = match.Peak?.Rt,
            QualityScore = match.Reference.IsUser ? 0d : match.Peak?.QualityScore ?? 0d,
            SampleCoverage = match.Reference.SampleCoverage,
            Used = match.Used,
            Status = match.Status,
            OutlierTest = match.OutlierTest,
            LocalSupportCount = match.LocalSupportCount,
            ExpectedOffset = match.ExpectedOffset,
            OutlierScale = match.OutlierScale,
            // Judged anchors carry the floor they were judged with; the reference file's and Blank
            // files' anchors are read here, for the record.
            Ms1CycleTime = match.Peak is null ? null : match.Ms1CycleTime ?? cycleProfile.CycleTimeAt(match.Peak.Rt),
            Source = match.Reference.IsUser ? AutomaticRtCorrectionAnchorSource.User : AutomaticRtCorrectionAnchorSource.Automatic,
            AnchorName = match.Reference.Name,
            LibraryRt = match.Reference.LibraryRt,
            OutlierFlagged = match.OutlierFlagged,
            PeakHeight = match.Peak?.Height,
            ObservedMz = match.Peak is null
                ? null
                : match.Reference.IsUser ? NullIfNaN(match.Peak.ObservedMz) : match.Peak.Mz,
            Detection = match.Note,
        };
    }

    private static double? NullIfNaN(double value) => double.IsNaN(value) ? null : value;

    private static void SetAuditFromPoints(
        AutomaticRtCorrectionFileAudit audit,
        IReadOnlyList<AlignmentRetentionTimeCorrectionControlPoint> points,
        AutomaticRtCorrectionModelSource source,
        double[] sortedPeakRts) {
        var absoluteOffsets = points.Select(point => Math.Abs(point.Offset)).ToList();
        audit.UsedAnchorCount = points.Count;
        audit.ModelSource = source;
        audit.MedianAbsoluteOffset = Median(absoluteOffsets);
        audit.MaximumAbsoluteOffset = absoluteOffsets.Count == 0 ? 0d : absoluteOffsets.Max();
        if (points.Count > 0) {
            var first = points.Min(point => point.OriginalRt);
            var last = points.Max(point => point.OriginalRt);
            audit.FirstAnchorRt = first;
            audit.LastAnchorRt = last;
            audit.PeaksBeforeFirstAnchor = sortedPeakRts.Count(rt => rt < first);
            audit.PeaksAfterLastAnchor = sortedPeakRts.Count(rt => rt > last);
        }
    }

    private static void InterpolateBlankModels(
        IReadOnlyList<AnalysisFileBean> files,
        IDictionary<int, IAlignmentRetentionTimeCorrectionModel> models,
        IReadOnlyList<AutomaticRtCorrectionFileAudit> audits) {
        var orderedNonBlankModels = files
            .Where(file => file.AnalysisFileType != AnalysisFileType.Blank && models.ContainsKey(file.AnalysisFileId))
            .OrderBy(file => file.AnalysisFileAnalyticalOrder)
            .ToList();
        foreach (var blank in files.Where(file => file.AnalysisFileType == AnalysisFileType.Blank)) {
            var audit = audits.First(item => item.FileId == blank.AnalysisFileId);
            var left = orderedNonBlankModels.LastOrDefault(file => file.AnalysisFileAnalyticalOrder <= blank.AnalysisFileAnalyticalOrder);
            var right = orderedNonBlankModels.FirstOrDefault(file => file.AnalysisFileAnalyticalOrder >= blank.AnalysisFileAnalyticalOrder);
            if (left is not null && right is not null && left.AnalysisFileId != right.AnalysisFileId) {
                var denominator = right.AnalysisFileAnalyticalOrder - left.AnalysisFileAnalyticalOrder;
                var weight = denominator == 0 ? 0.5d : (double)(blank.AnalysisFileAnalyticalOrder - left.AnalysisFileAnalyticalOrder) / denominator;
                models[blank.AnalysisFileId] = new BlendedAlignmentRetentionTimeCorrectionModel(
                    models[left.AnalysisFileId], models[right.AnalysisFileId], weight);
                audit.ModelSource = AutomaticRtCorrectionModelSource.InterpolatedBlank;
                audit.Note = $"Interpolated from file IDs {left.AnalysisFileId} and {right.AnalysisFileId}.";
            }
            else {
                var nearest = left ?? right;
                if (nearest is not null) {
                    models[blank.AnalysisFileId] = models[nearest.AnalysisFileId];
                    audit.ModelSource = AutomaticRtCorrectionModelSource.NearestBlank;
                    audit.Note = $"Used nearest non-Blank model from file ID {nearest.AnalysisFileId}.";
                }
                else {
                    audit.Note = "No neighboring valid model; original RT is retained.";
                }
            }
        }
    }

    private static double Quantile(IEnumerable<double> values, double quantile) {
        var ordered = values.OrderBy(value => value).ToList();
        if (ordered.Count == 0) return 0d;
        if (ordered.Count == 1) return ordered[0];
        var position = Clamp01(quantile) * (ordered.Count - 1);
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        if (lower == upper) return ordered[lower];
        return ordered[lower] + (position - lower) * (ordered[upper] - ordered[lower]);
    }

    private static double Median(IEnumerable<double> values) {
        return Quantile(values, 0.5d);
    }

    private static double Clamp01(double value) {
        return Math.Max(0d, Math.Min(1d, value));
    }

    private sealed class FileSummary {
        public FileSummary(AnalysisFileBean file, List<PeakPoint> candidates, double coverage) {
            File = file;
            Candidates = candidates;
            Coverage = coverage;
        }
        public AnalysisFileBean File { get; }
        public List<PeakPoint> Candidates { get; }
        public double Coverage { get; }
        public double Centrality { get; set; }
        public double ReferenceScore { get; set; }
    }

    private sealed class PeakPoint {
        public int Index { get; set; }
        public int PeakId { get; set; }
        public int AnchorId { get; set; }
        public double Mz { get; set; }
        public double Rt { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double SignalToNoise { get; set; }
        public double GaussianSimilarity { get; set; }
        public double IdealSlope { get; set; }
        public double Symmetry { get; set; }
        public double QualityScore { get; set; }
        public double SampleCoverage { get; set; }
        public bool IsUser { get; set; }
        /// <summary>A user anchor's position among the library's True rows.</summary>
        public int UserIndex { get; set; } = -1;
        public string Name { get; set; } = string.Empty;
        public double? LibraryRt { get; set; }
        /// <summary>A user anchor's: the m/z its peak was observed at; NaN when it was not detected.</summary>
        public double ObservedMz { get; set; } = double.NaN;
        /// <summary>A user anchor's: the library's m/z tolerance.</summary>
        public double MassTolerance { get; set; }
        /// <summary>A user anchor whose reference peak is an earlier row's: that row.</summary>
        public PeakPoint? DuplicateOf { get; set; }
    }

    private sealed class AnchorMatch {
        public AnchorMatch(PeakPoint reference, PeakPoint? peak, string status) {
            Reference = reference;
            Peak = peak;
            Status = status;
        }
        public PeakPoint Reference { get; }
        public PeakPoint? Peak { get; set; }
        public string Status { get; set; }
        public bool Used { get; set; }
        public string OutlierTest { get; set; } = string.Empty;
        public int LocalSupportCount { get; set; }
        public double? ExpectedOffset { get; set; }
        public double? OutlierScale { get; set; }
        public double? Ms1CycleTime { get; set; }
        public bool OutlierFlagged { get; set; }
        /// <summary>A user anchor's detection note in the file.</summary>
        public string Note { get; set; } = string.Empty;
    }
}

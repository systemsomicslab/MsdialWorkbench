using CompMs.Common.Components;
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
}

public sealed class AutomaticRtCorrectionAnchorAudit {
    public int FileId { get; internal set; }
    public string FileName { get; internal set; } = string.Empty;
    public int AnchorId { get; internal set; }
    public double Mass { get; internal set; }
    public double ReferenceRt { get; internal set; }
    public double? OriginalRt { get; internal set; }
    public double QualityScore { get; internal set; }
    public double SampleCoverage { get; internal set; }
    public bool Used { get; internal set; }
    public string Status { get; internal set; } = string.Empty;
}

public sealed class AutomaticAlignmentRetentionTimeCorrectionResult {
    public AutomaticAlignmentRetentionTimeCorrectionResult(
        AlignmentRetentionTimeCorrectionCollection correction,
        IReadOnlyList<AutomaticRtCorrectionFileAudit> files,
        IReadOnlyList<AutomaticRtCorrectionAnchorAudit> anchors) {
        Correction = correction;
        Files = files;
        Anchors = anchors;
    }

    public AlignmentRetentionTimeCorrectionCollection Correction { get; }
    public IReadOnlyList<AutomaticRtCorrectionFileAudit> Files { get; }
    public IReadOnlyList<AutomaticRtCorrectionAnchorAudit> Anchors { get; }

    public void WriteAudit(string outputFolder) {
        Directory.CreateDirectory(outputFolder);
        var summaryPath = Path.Combine(outputFolder, "automatic_alignment_rt_correction_summary.tsv");
        using (var writer = new StreamWriter(summaryPath, false, new UTF8Encoding(false))) {
            writer.WriteLine("File ID\tFile name\tFile type\tAnalytical order\tCandidate count\tMatched anchors\tUsed anchors\tModel source\tReference score\tMedian absolute offset (min)\tMaximum absolute offset (min)\tNote");
            foreach (var file in Files.OrderBy(file => file.AnalyticalOrder).ThenBy(file => file.FileId)) {
                writer.WriteLine(string.Join("\t",
                    file.FileId.ToString(CultureInfo.InvariantCulture),
                    file.FileName,
                    file.FileType.ToString(),
                    file.AnalyticalOrder.ToString(CultureInfo.InvariantCulture),
                    file.CandidateCount.ToString(CultureInfo.InvariantCulture),
                    file.MatchedAnchorCount.ToString(CultureInfo.InvariantCulture),
                    file.UsedAnchorCount.ToString(CultureInfo.InvariantCulture),
                    file.ModelSource.ToString(),
                    file.ReferenceScore.ToString("G17", CultureInfo.InvariantCulture),
                    file.MedianAbsoluteOffset.ToString("G17", CultureInfo.InvariantCulture),
                    file.MaximumAbsoluteOffset.ToString("G17", CultureInfo.InvariantCulture),
                    file.Note));
            }
        }

        var anchorPath = Path.Combine(outputFolder, "automatic_alignment_rt_correction_anchors.tsv");
        using (var writer = new StreamWriter(anchorPath, false, new UTF8Encoding(false))) {
            writer.WriteLine("File ID\tFile name\tAnchor ID\tm/z\tReference RT (min)\tOriginal RT (min)\tOffset (min)\tQuality score\tNon-Blank sample coverage\tUsed\tStatus");
            foreach (var anchor in Anchors.OrderBy(anchor => anchor.FileId).ThenBy(anchor => anchor.AnchorId)) {
                var offset = anchor.OriginalRt.HasValue ? anchor.ReferenceRt - anchor.OriginalRt.Value : (double?)null;
                writer.WriteLine(string.Join("\t",
                    anchor.FileId.ToString(CultureInfo.InvariantCulture),
                    anchor.FileName,
                    anchor.AnchorId.ToString(CultureInfo.InvariantCulture),
                    anchor.Mass.ToString("G17", CultureInfo.InvariantCulture),
                    anchor.ReferenceRt.ToString("G17", CultureInfo.InvariantCulture),
                    anchor.OriginalRt?.ToString("G17", CultureInfo.InvariantCulture) ?? string.Empty,
                    offset?.ToString("G17", CultureInfo.InvariantCulture) ?? string.Empty,
                    anchor.QualityScore.ToString("G17", CultureInfo.InvariantCulture),
                    anchor.SampleCoverage.ToString("G17", CultureInfo.InvariantCulture),
                    anchor.Used.ToString(),
                    anchor.Status));
            }
        }
    }
}

/// <summary>
/// Learns a file-specific RT map after peak picking and annotation. Only compact peak properties
/// are retained, and each .pai file is read at most twice, which keeps the setup independent of
/// the spots-by-files object graph created by alignment.
/// </summary>
public static class AutomaticAlignmentRetentionTimeCorrection {
    private const int CandidateLimitPerRtBin = 3;

    public static AutomaticAlignmentRetentionTimeCorrectionResult Build(
        IReadOnlyList<AnalysisFileBean> files,
        AutomaticAlignmentRetentionTimeCorrectionParameter parameter,
        double mzTolerance) {
        return Build(files, parameter, mzTolerance, file => MsdialPeakSerializer.LoadChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath));
    }

    public static AutomaticAlignmentRetentionTimeCorrectionResult Build(
        IReadOnlyList<AnalysisFileBean> files,
        AutomaticAlignmentRetentionTimeCorrectionParameter parameter,
        double mzTolerance,
        Func<AnalysisFileBean, List<ChromatogramPeakFeature>> peakLoader) {
        Validate(files, parameter, mzTolerance, peakLoader);

        var summaries = files.Select(file => CreateSummary(file, peakLoader(file), parameter, mzTolerance)).ToList();
        var eligibleReferences = summaries
            .Where(summary => summary.File.AnalysisFileType != AnalysisFileType.Blank
                && summary.Candidates.Count >= parameter.MinimumAnchorCount)
            .ToList();
        if (eligibleReferences.Count == 0) {
            throw new InvalidOperationException("Automatic alignment RT correction could not find a non-Blank file with enough high-quality anchor candidates.");
        }

        foreach (var summary in eligibleReferences) {
            summary.Centrality = CalculateCentrality(summary, eligibleReferences, parameter, mzTolerance);
            var weight = Clamp01(parameter.ReferenceCentralityWeight);
            summary.ReferenceScore = summary.Coverage * (1d - weight) + summary.Centrality * weight;
        }
        var reference = SelectReference(eligibleReferences, parameter.ReferenceFileId);

        var matchesByFile = new Dictionary<int, List<AnchorMatch>>();
        foreach (var file in files) {
            var peaks = CreatePeakPoints(peakLoader(file));
            matchesByFile[file.AnalysisFileId] = reference.Candidates
                .Select(candidate => Match(candidate, peaks, parameter.MatchRtTolerance, mzTolerance))
                .ToList();
        }

        var nonBlankCount = Math.Max(1, files.Count(file => file.AnalysisFileType != AnalysisFileType.Blank));
        var minimumCoverageCount = Math.Max(1, (int)Math.Ceiling(Clamp01(parameter.MinimumSampleCoverage) * nonBlankCount));
        foreach (var candidate in reference.Candidates) {
            var detectedCount = files
                .Where(file => file.AnalysisFileType != AnalysisFileType.Blank)
                .Count(file => matchesByFile[file.AnalysisFileId]
                    .Any(match => ReferenceEquals(match.Reference, candidate) && match.Peak is not null));
            candidate.SampleCoverage = (double)detectedCount / nonBlankCount;
        }
        var coveredCandidates = reference.Candidates
            .Where(candidate => candidate.SampleCoverage * nonBlankCount >= minimumCoverageCount)
            .ToList();
        var selectedAnchors = SelectDistributedAnchors(coveredCandidates, parameter.MaximumAnchorCount);
        if (selectedAnchors.Count < parameter.MinimumAnchorCount) {
            throw new InvalidOperationException(
                $"Automatic alignment RT correction retained only {selectedAnchors.Count} anchor(s) after cross-sample coverage filtering; at least {parameter.MinimumAnchorCount} are required.");
        }
        for (var index = 0; index < selectedAnchors.Count; index++) {
            selectedAnchors[index].AnchorId = index + 1;
        }

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
            };

            if (file.AnalysisFileId == reference.File.AnalysisFileId) {
                foreach (var match in relevantMatches) {
                    match.Peak = match.Reference;
                    match.Status = "Reference";
                    match.Used = true;
                }
                var points = relevantMatches.Select(ToControlPoint).ToList();
                models[file.AnalysisFileId] = new PiecewiseLinearAlignmentRetentionTimeCorrectionModel(points);
                SetAuditFromPoints(fileAudit, points, AutomaticRtCorrectionModelSource.Reference);
            }
            else if (file.AnalysisFileType != AnalysisFileType.Blank) {
                var accepted = RejectOutliersAndNonMonotonic(relevantMatches, parameter.OutlierMadThreshold);
                if (accepted.Count >= parameter.MinimumAnchorCount) {
                    foreach (var match in accepted) {
                        match.Status = "Used";
                        match.Used = true;
                    }
                    var points = accepted.Select(ToControlPoint).ToList();
                    models[file.AnalysisFileId] = new PiecewiseLinearAlignmentRetentionTimeCorrectionModel(points);
                    SetAuditFromPoints(fileAudit, points, AutomaticRtCorrectionModelSource.DetectedAnchors);
                }
                else {
                    foreach (var match in accepted) {
                        match.Status = "InsufficientAnchors";
                        match.Used = false;
                    }
                    fileAudit.Note = $"Only {accepted.Count} usable anchor(s); original RT is retained.";
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

            fileAudits.Add(fileAudit);
            anchorAudits.AddRange(relevantMatches.Select(match => ToAudit(file, match)));
        }

        if (parameter.InterpolateBlankByAnalyticalOrder) {
            InterpolateBlankModels(files, models, fileAudits);
        }

        return new AutomaticAlignmentRetentionTimeCorrectionResult(
            new AlignmentRetentionTimeCorrectionCollection(reference.File.AnalysisFileId, models),
            fileAudits,
            anchorAudits);
    }

    private static void Validate(
        IReadOnlyList<AnalysisFileBean> files,
        AutomaticAlignmentRetentionTimeCorrectionParameter parameter,
        double mzTolerance,
        Delegate peakLoader) {
        if (files is null || files.Count == 0) throw new ArgumentException("At least one analysis file is required.", nameof(files));
        if (parameter is null) throw new ArgumentNullException(nameof(parameter));
        if (peakLoader is null) throw new ArgumentNullException(nameof(peakLoader));
        if (mzTolerance <= 0d) throw new ArgumentOutOfRangeException(nameof(mzTolerance));
        if (parameter.RtBinWidth <= 0d) throw new ArgumentOutOfRangeException(nameof(parameter.RtBinWidth));
        if (parameter.MatchRtTolerance <= 0d) throw new ArgumentOutOfRangeException(nameof(parameter.MatchRtTolerance));
        if (parameter.MinimumAnchorCount < 2) throw new ArgumentOutOfRangeException(nameof(parameter.MinimumAnchorCount));
        if (parameter.MaximumAnchorCount < parameter.MinimumAnchorCount) throw new ArgumentOutOfRangeException(nameof(parameter.MaximumAnchorCount));
        if (parameter.IntensityQuantile < 0d || parameter.IntensityQuantile > 1d) throw new ArgumentOutOfRangeException(nameof(parameter.IntensityQuantile));
        if (parameter.MaximumPeakWidthQuantile < 0d || parameter.MaximumPeakWidthQuantile > 1d) throw new ArgumentOutOfRangeException(nameof(parameter.MaximumPeakWidthQuantile));
        if (parameter.MinimumSampleCoverage < 0d || parameter.MinimumSampleCoverage > 1d) throw new ArgumentOutOfRangeException(nameof(parameter.MinimumSampleCoverage));
        if (parameter.ReferenceCentralityWeight < 0d || parameter.ReferenceCentralityWeight > 1d) throw new ArgumentOutOfRangeException(nameof(parameter.ReferenceCentralityWeight));
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

    private static List<AnchorMatch> RejectOutliersAndNonMonotonic(List<AnchorMatch> matches, double madThreshold) {
        var accepted = matches.Where(match => match.Peak is not null).ToList();
        var offsets = accepted.Select(match => match.Reference.Rt - match.Peak!.Rt).ToList();
        if (offsets.Count > 0 && madThreshold > 0d) {
            var median = Median(offsets);
            var mad = Median(offsets.Select(offset => Math.Abs(offset - median)));
            if (mad > 1e-12) {
                var scale = 1.4826d * mad;
                foreach (var match in accepted.Where(match => Math.Abs((match.Reference.Rt - match.Peak!.Rt) - median) / scale > madThreshold).ToList()) {
                    match.Status = "MadOutlier";
                    accepted.Remove(match);
                }
            }
        }

        accepted = accepted.OrderBy(match => match.Peak!.Rt).ToList();
        var changed = true;
        while (changed) {
            changed = false;
            for (var index = 1; index < accepted.Count; index++) {
                if (accepted[index].Reference.Rt > accepted[index - 1].Reference.Rt) continue;
                var remove = accepted[index].Peak!.QualityScore < accepted[index - 1].Peak!.QualityScore
                    ? accepted[index]
                    : accepted[index - 1];
                remove.Status = "NonMonotonic";
                accepted.Remove(remove);
                changed = true;
                break;
            }
        }
        return accepted;
    }

    private static AlignmentRetentionTimeCorrectionControlPoint ToControlPoint(AnchorMatch match) {
        return new AlignmentRetentionTimeCorrectionControlPoint(
            match.Reference.AnchorId,
            match.Reference.Mz,
            match.Peak!.Rt,
            match.Reference.Rt,
            match.Peak.QualityScore);
    }

    private static AutomaticRtCorrectionAnchorAudit ToAudit(AnalysisFileBean file, AnchorMatch match) {
        return new AutomaticRtCorrectionAnchorAudit {
            FileId = file.AnalysisFileId,
            FileName = file.AnalysisFileName,
            AnchorId = match.Reference.AnchorId,
            Mass = match.Reference.Mz,
            ReferenceRt = match.Reference.Rt,
            OriginalRt = match.Peak?.Rt,
            QualityScore = match.Peak?.QualityScore ?? 0d,
            SampleCoverage = match.Reference.SampleCoverage,
            Used = match.Used,
            Status = match.Status,
        };
    }

    private static void SetAuditFromPoints(
        AutomaticRtCorrectionFileAudit audit,
        IReadOnlyList<AlignmentRetentionTimeCorrectionControlPoint> points,
        AutomaticRtCorrectionModelSource source) {
        var absoluteOffsets = points.Select(point => Math.Abs(point.Offset)).ToList();
        audit.UsedAnchorCount = points.Count;
        audit.ModelSource = source;
        audit.MedianAbsoluteOffset = Median(absoluteOffsets);
        audit.MaximumAbsoluteOffset = absoluteOffsets.Count == 0 ? 0d : absoluteOffsets.Max();
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
    }
}

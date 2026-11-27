using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CompMs.MsdialCore.Algorithm.MassCalibration;

/// <summary>
/// What the mass recalibration found for one file: the calibrants of the first, permissive annotation, the decision on
/// the MS1 and on the MS2 model, and the recalibration recorded with the file, if any.
/// </summary>
public sealed class MassRecalibrationOutcome
{
    public MassRecalibrationOutcome(AnalysisFileBean file, IReadOnlyList<AnnotationCalibrant> calibrants, MassCalibrationDecision ms1, MassCalibrationDecision ms2, MassRecalibrationBean? recalibration) {
        FileName = file.AnalysisFileName;
        FileId = file.AnalysisFileId;
        Calibrants = calibrants;
        Ms1 = ms1;
        Ms2 = ms2;
        Recalibration = recalibration;
    }

    public string FileName { get; }

    public int FileId { get; }

    public IReadOnlyList<AnnotationCalibrant> Calibrants { get; }

    public MassCalibrationDecision Ms1 { get; }

    public MassCalibrationDecision Ms2 { get; }

    /// <summary>The recalibration recorded with the file; null when neither model is applied.</summary>
    public MassRecalibrationBean? Recalibration { get; }

    /// <summary>"MS1 …; MS2 …." as <see cref="MassCalibrationReport.Describe"/> gives each.</summary>
    public string Describe() {
        return $"MS1 {MassCalibrationReport.Describe(Ms1)}; MS2 {MassCalibrationReport.Describe(Ms2)}.";
    }
}

/// <summary>
/// The first pass of the two-pass annotation: annotates one file's features once more with wide m/z windows and without
/// the precursor m/z term (<see cref="AnnotationCalibrantCollector"/>), measures its MS1 and MS2 m/z against the
/// theoretical m/z of what they matched, and chooses and judges a model of each (<see cref="MassCalibrationDecision"/>).
/// The models that are to be applied make the file's <see cref="MassRecalibrationBean"/>; the caller records it with the
/// file and reads the file again, so that everything after reads recalibrated m/z.
/// </summary>
/// <remarks>
/// Thread-safe: it holds no state per file but the outcomes, so the per-file parallel loop can share one.
/// </remarks>
public sealed class MassRecalibrationProcess
{
    private readonly AnnotationCalibrantCollector _collector;
    private readonly ConcurrentDictionary<int, MassRecalibrationOutcome> _outcomes = new ConcurrentDictionary<int, MassRecalibrationOutcome>();

    public MassRecalibrationProcess(AnnotationCalibrantCollector collector, double minimumCorrectionPpm = MassCalibrationDecision.DefaultMinimumCorrectionPpm) {
        _collector = collector ?? throw new ArgumentNullException(nameof(collector));
        if (!(minimumCorrectionPpm >= 0d) || double.IsInfinity(minimumCorrectionPpm)) {
            throw new ArgumentOutOfRangeException(nameof(minimumCorrectionPpm), minimumCorrectionPpm, "The minimum correction must be a number of ppm, not negative.");
        }
        MinimumCorrectionPpm = minimumCorrectionPpm;
    }

    public double SearchPpm => _collector.SearchPpm;

    public double MinimumCorrectionPpm { get; }

    /// <summary>The outcome of each file this process has decided on, in the order of the files' IDs.</summary>
    public IReadOnlyList<MassRecalibrationOutcome> Outcomes => _outcomes.Values.OrderBy(outcome => outcome.FileId).ToList();

    /// <summary>
    /// Decides one file's recalibration from its features, picked and deconvoluted from the m/z as recorded, and its raw
    /// spectra, which the deconvoluted spectra index. For AIF, the spectra of the lowest collision energy are used.
    /// </summary>
    public MassRecalibrationOutcome Decide(AnalysisFileBean file, IReadOnlyList<ChromatogramPeakFeature> features, IReadOnlyList<MSDecResultCollection> deconvolutions, IReadOnlyList<RawSpectrum> rawSpectra, CancellationToken token = default) {
        if (file is null) {
            throw new ArgumentNullException(nameof(file));
        }
        if (features is null) {
            throw new ArgumentNullException(nameof(features));
        }
        if (deconvolutions is null) {
            throw new ArgumentNullException(nameof(deconvolutions));
        }

        IReadOnlyList<MSDecResult> spectra = deconvolutions.Count > 0 ? deconvolutions.Argmin(collection => collection.CollisionEnergy).MSDecResults : Array.Empty<MSDecResult>();
        var calibrants = _collector.Collect(features, spectra, rawSpectra, token);
        var ms1Points = MassCalibrationPoint.FromAnnotation(calibrants, 1, out var ms1Windows);
        var ms2Points = MassCalibrationPoint.FromAnnotation(calibrants, 2, out var ms2Windows);
        var ms1 = MassCalibrationDecision.Decide(ms1Points, SearchPpm, MinimumCorrectionPpm);
        var ms2 = MassCalibrationDecision.Decide(ms2Points, SearchPpm, MinimumCorrectionPpm);

        MassRecalibrationBean? recalibration = null;
        if (ms1.IsApplied || ms2.IsApplied) {
            recalibration = new MassRecalibrationBean
            {
                Ms1 = ms1.IsApplied ? MassErrorModelBean.FromModel(ms1.Model, ms1Windows) : null,
                Ms2 = ms2.IsApplied ? MassErrorModelBean.FromModel(ms2.Model, ms2Windows) : null,
                AlgorithmVersion = MassCalibrationDecision.AlgorithmVersion,
            };
        }
        var outcome = new MassRecalibrationOutcome(file, calibrants, ms1, ms2, recalibration);
        _outcomes[file.AnalysisFileId] = outcome;
        return outcome;
    }
}

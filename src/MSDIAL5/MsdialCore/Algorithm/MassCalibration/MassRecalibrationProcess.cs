using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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

    /// <summary>
    /// The recalibration a project's settings ask for, searching the project's own MSP and LBM annotators
    /// (<see cref="CalibrationLibrary.FromDataBases"/>), as the Console and the GUI both build it.
    /// </summary>
    /// <param name="parameter">The project's mass recalibration settings.</param>
    /// <param name="dataBases">The project's databases, with their annotators built.</param>
    /// <param name="targetOmics">The project's target omics.</param>
    /// <param name="refusal">Why a recalibration that is asked for cannot run, or null.</param>
    /// <returns>The process; null when none is asked for, or when it cannot run, which <paramref name="refusal"/> then says.</returns>
    public static MassRecalibrationProcess? FromParameter(MassRecalibrationParameter parameter, DataBaseStorage dataBases, TargetOmics targetOmics, out string? refusal) {
        if (parameter is null) {
            throw new ArgumentNullException(nameof(parameter));
        }
        refusal = null;
        if (!parameter.IsEnabled) {
            return null;
        }
        refusal = parameter.Problem();
        if (refusal is not null) {
            return null;
        }
        var libraries = CalibrationLibrary.FromDataBases(dataBases, targetOmics);
        if (libraries.Count == 0) {
            refusal = "Mass recalibration needs an MSP or LBM library with MS/MS spectra to annotate against, and none is set.";
            return null;
        }
        return new MassRecalibrationProcess(new AnnotationCalibrantCollector(libraries, parameter.SearchPpm), parameter.MinimumCorrectionPpm);
    }

    public double SearchPpm => _collector.SearchPpm;

    public double MinimumCorrectionPpm { get; }

    /// <summary>What the recalibration does, for a log: the libraries, the window and the rule.</summary>
    public string Describe() {
        var invariant = CultureInfo.InvariantCulture;
        return $"each file's features are first annotated against {string.Join(", ", _collector.Libraries.Select(library => library.Id))} with ±{SearchPpm.ToString("0.#", invariant)} ppm m/z windows and without the precursor m/z in the score; an MS1 or MS2 error model of at least {MinimumCorrectionPpm.ToString("0.0#", invariant)} ppm and {MassCalibrationDecision.SignificanceFactor.ToString("0", invariant)} standard errors is recorded with the file and taken out of its spectra, and its peaks are picked, deconvoluted and annotated from the recalibrated m/z.";
    }

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

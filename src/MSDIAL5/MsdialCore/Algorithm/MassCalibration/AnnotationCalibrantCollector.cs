using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Lipidomics;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CompMs.MsdialCore.Algorithm.MassCalibration;

/// <summary>
/// A library searched for calibrants: its annotator, the search settings it is used with, and what its m/z are.
/// </summary>
public sealed class CalibrationLibrary
{
    public CalibrationLibrary(
        IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> annotator,
        MsRefSearchParameterBase parameter,
        TargetOmics omics,
        DataBaseSource dataBaseSource) {
        Annotator = annotator ?? throw new ArgumentNullException(nameof(annotator));
        Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        Omics = omics;
        DataBaseSource = dataBaseSource;
    }

    public IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> Annotator { get; }

    /// <summary>The library's own search settings; the collector widens its m/z windows and keeps the rest.</summary>
    public MsRefSearchParameterBase Parameter { get; }

    public TargetOmics Omics { get; }

    public DataBaseSource DataBaseSource { get; }

    public string Id => Annotator.Id;

    /// <summary>
    /// Whether the library's fragment m/z are computed, so that a product ion can be measured against them: LBM and
    /// the in silico libraries. An acquired spectrum's fragment m/z carry the error of the instrument that recorded it.
    /// </summary>
    public bool FragmentMzAreTheoretical => DataBaseSource == DataBaseSource.Lbm || AnnotationEvidence.SpectraArePredicted(DataBaseSource);

    /// <summary>
    /// Whether this record's fragment m/z are computed. An LBM can carry acquired spectra beside its generated lipids
    /// (a converted MSP), so of an LBM only the records that read as a lipid of a known class are taken.
    /// </summary>
    public bool FragmentMzAreTheoreticalFor(MoleculeMsReference reference) {
        if (!FragmentMzAreTheoretical) {
            return false;
        }
        return DataBaseSource != DataBaseSource.Lbm || IsGeneratedLipid(reference);
    }

    internal static bool IsGeneratedLipid(MoleculeMsReference reference) {
        try {
            var lipid = LipidomicsConverter.ConvertMsdialLipidnameToLipidMoleculeObjectVS2(reference);
            return lipid is { IsValidatedFormat: true } && lipid.LipidClass != LbmClass.Undefined;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or IndexOutOfRangeException or NullReferenceException) {
            return false;
        }
    }
}

/// <summary>
/// Annotates each feature once more with m/z windows wide enough to hold a calibration error, and turns the matches
/// into calibrants: the feature's MS1 m/z against the matched compound's theoretical precursor m/z, and the product
/// ions of its deconvoluted spectrum against the fragments of a library whose fragment m/z are computed.
/// </summary>
/// <remarks>
/// <para>
/// The reference is chosen without the precursor m/z. MS-DIAL's total score rewards the candidate nearest the
/// observed m/z, so among candidates of different formulas within a wide window it would pick, more often than chance,
/// the one with the smallest error, and the error measured from those picks would be pulled toward zero. Here the
/// candidates are ranked by the spectrum terms of the total score (and the retention time term when the library scores
/// it), and only candidates whose spectrum passes the library's own cut-offs at the widened MS2 tolerance are taken.
/// When the best candidates tie, the feature gives no MS1 calibrant unless they share a precursor m/z, and no MS2
/// calibrant at all.
/// </para>
/// <para>
/// A wrong match costs little: an isomer has the same formula and therefore the right m/z, and a match to another
/// formula falls anywhere in the window, among the background that <see cref="MassErrorEstimate"/> measures and sets
/// aside.
/// </para>
/// <para>
/// A product ion is paired with the most intense peak within the window of a reference fragment. A fragment with
/// another fragment of the same reference within two windows of it is left out, since either could take the peak. So
/// is a peak that may be, or may hold, the M+2 isotope of an ion with one more double bond: two <sup>13</sup>C lie
/// 0.0089 Da below two hydrogens, 19 ppm at m/z 476, which a TOF does not resolve. Lipid fragments that differ by one
/// double bond are common, and their isotopes would otherwise make a second peak of errors 0.0089 Da below the true one.
/// The lighter ion is looked for in the raw product-ion scan the spectrum was deconvoluted at, when it is given: in a
/// SWATH window it often comes from a neighbouring lipid, and deconvolution leaves it out of this feature's spectrum
/// while its isotope, unresolved from the fragment, stays in.
/// </para>
/// </remarks>
public sealed class AnnotationCalibrantCollector
{
    /// <summary>The half width, in ppm, of the m/z windows of the search and of the fragment pairing.</summary>
    public const double DefaultSearchPpm = 100d;

    /// <summary>
    /// How far the m/z computed from a record's formula and adduct may lie from the record's own precursor m/z before
    /// the record is taken to be inconsistent and gives no MS1 calibrant.
    /// </summary>
    public const double FormulaConsistencyPpm = 10d;

    private const double TieTolerance = 1e-6;
    private const double TwoC13 = 2d * 1.0033548378d;
    private const double TwoH = 2d * 1.00782503207d;
    private const double IsotopeParentToleranceDa = 0.005d;
    private const double SamePrecursorPpm = 1d;

    private readonly IReadOnlyList<CalibrationLibrary> _libraries;

    public AnnotationCalibrantCollector(IReadOnlyList<CalibrationLibrary> libraries, double searchPpm = DefaultSearchPpm) {
        _libraries = libraries ?? throw new ArgumentNullException(nameof(libraries));
        if (!(searchPpm > 0d)) {
            throw new ArgumentOutOfRangeException(nameof(searchPpm), searchPpm, "The search window must be positive.");
        }
        SearchPpm = searchPpm;
    }

    public double SearchPpm { get; }

    /// <summary>
    /// The calibrants of one file: its features and their deconvoluted spectra, one for one in the same order, and the
    /// file's raw spectra, in which each deconvoluted spectrum's <see cref="MSDecResult.RawSpectrumID"/> is a position.
    /// Isotope peaks and features without a spectrum give none.
    /// </summary>
    public List<AnnotationCalibrant> Collect(IReadOnlyList<ChromatogramPeakFeature> features, IReadOnlyList<MSDecResult> spectra, IReadOnlyList<RawSpectrum>? rawSpectra = null, CancellationToken token = default) {
        if (features is null) {
            throw new ArgumentNullException(nameof(features));
        }
        if (spectra is null) {
            throw new ArgumentNullException(nameof(spectra));
        }
        var calibrants = new List<AnnotationCalibrant>();
        for (var i = 0; i < features.Count && i < spectra.Count; i++) {
            token.ThrowIfCancellationRequested();
            Collect(calibrants, features[i], spectra[i], RawScan(rawSpectra, spectra[i]));
        }
        return calibrants;
    }

    private static RawSpectrum? RawScan(IReadOnlyList<RawSpectrum>? rawSpectra, MSDecResult spectrum) {
        var id = spectrum.RawSpectrumID;
        if (rawSpectra is null || id < 0 || id >= rawSpectra.Count) {
            return null;
        }
        return rawSpectra[id] is { MsLevel: >= 2, Spectrum.Length: > 0 } scan ? scan : null;
    }

    private void Collect(List<AnnotationCalibrant> calibrants, ChromatogramPeakFeature feature, MSDecResult spectrum, RawSpectrum? rawScan) {
        if (feature.PeakCharacter.IsotopeWeightNumber > 0 || spectrum.Spectrum.IsEmptyOrNull()) {
            return;
        }

        var candidates = new List<Candidate>();
        foreach (var library in _libraries) {
            var parameter = Widen(library.Parameter, feature.PrecursorMz, SearchPpm);
            var query = new AnnotationQuery(feature, spectrum, Array.Empty<IsotopicPeak>(), feature.PeakCharacter, parameter, finder: null);
            foreach (var result in library.Annotator.FindCandidates(query)) {
                if (!result.IsSpectrumMatch) {
                    continue;
                }
                var reference = library.Annotator.Refer(result);
                if (reference is null) {
                    continue;
                }
                candidates.Add(new Candidate(library, reference, ScoreWithoutPrecursorMz(result, library.Omics, parameter.IsUseTimeForAnnotationScoring)));
            }
        }

        var precursor = Best(candidates, out var precursorTies);
        if (precursor is not null && precursorTies.All(tie => SamePrecursor(tie.Reference, precursor.Reference))) {
            var referenceMz = TheoreticalPrecursorMz(precursor.Reference, out var source);
            var observedMz = feature.PrecursorMz;
            if (!double.IsNaN(referenceMz) && Math.Abs(observedMz - referenceMz) / referenceMz * 1e6 <= SearchPpm) {
                calibrants.Add(new AnnotationCalibrant(
                    feature.MasterPeakID, feature.ChromXs.RT.Value, 1, observedMz, referenceMz, source, feature.PeakFeature.PeakHeightTop,
                    observedMz, isPrecursorIon: true, precursor.Reference.Name, precursor.Library.Id, precursor.Library.DataBaseSource, precursor.Score, feature.MS1RawSpectrumIdTop));
            }
        }

        var fragments = Best(candidates.Where(candidate => candidate.Library.FragmentMzAreTheoreticalFor(candidate.Reference)).ToList(), out var fragmentTies);
        if (fragments is not null && fragmentTies.Count == 0) {
            var observed = spectrum.Spectrum.Where(peak => peak.Intensity > 0d).OrderBy(peak => peak.Mass).ToArray();
            var reference = fragments.Reference;
            var (isolationLower, isolationUpper) = PrecursorCalibrantCollector.IsolationWindow(rawScan?.Precursor);
            foreach (var (referencePeak, observedPeak) in PairFragments(observed, reference.Spectrum, SearchPpm, rawScan?.Spectrum)) {
                var window = referencePeak.Mass * SearchPpm * 1e-6;
                calibrants.Add(new AnnotationCalibrant(
                    feature.MasterPeakID, feature.ChromXs.RT.Value, 2, observedPeak.Mass, referencePeak.Mass, AnnotationCalibrant.FromLibraryFragment, observedPeak.Intensity,
                    feature.PrecursorMz, Math.Abs(referencePeak.Mass - reference.PrecursorMz) <= window, reference.Name, fragments.Library.Id, fragments.Library.DataBaseSource, fragments.Score, spectrum.RawSpectrumID,
                    isolationLower, isolationUpper));
            }
        }
    }

    // The best candidate, and the others that tie with it.
    private static Candidate? Best(List<Candidate> candidates, out List<Candidate> ties) {
        ties = new List<Candidate>();
        if (candidates.Count == 0) {
            return null;
        }
        var best = candidates[0];
        foreach (var candidate in candidates.Skip(1)) {
            if (candidate.Score > best.Score) {
                best = candidate;
            }
        }
        foreach (var candidate in candidates) {
            if (!ReferenceEquals(candidate, best) && candidate.Score >= best.Score - TieTolerance && !ReferenceEquals(candidate.Reference, best.Reference)) {
                ties.Add(candidate);
            }
        }
        return best;
    }

    private static bool SamePrecursor(MoleculeMsReference a, MoleculeMsReference b) {
        var mzA = TheoreticalPrecursorMz(a, out _);
        var mzB = TheoreticalPrecursorMz(b, out _);
        return !double.IsNaN(mzA) && !double.IsNaN(mzB) && Math.Abs(mzA - mzB) / mzA * 1e6 <= SamePrecursorPpm;
    }

    /// <summary>
    /// The library's search settings with the MS1 window set to ±<paramref name="searchPpm"/> at the feature's m/z, and
    /// the MS2 tolerance widened to the same window at that m/z, which holds every fragment below it.
    /// </summary>
    /// <remarks>
    /// The annotators read the MS1 tolerance through <see cref="CompMs.Common.FormulaGenerator.Function.MolecularFormulaUtility.FixMassTolerance"/>,
    /// which keeps a tolerance in Da up to m/z 500 and scales it as ppm above, so the tolerance is the window in Da at
    /// the feature's m/z, or at m/z 500 above it.
    /// </remarks>
    internal static MsRefSearchParameterBase Widen(MsRefSearchParameterBase parameter, double precursorMz, double searchPpm) {
        return new MsRefSearchParameterBase(parameter)
        {
            Ms1Tolerance = (float)(searchPpm * 1e-6 * Math.Min(precursorMz, 500d)),
            Ms2Tolerance = (float)Math.Max(parameter.Ms2Tolerance, searchPpm * 1e-6 * precursorMz),
        };
    }

    /// <summary>
    /// MS-DIAL's total score (<see cref="MsReferenceScorer"/>, collision-induced dissociation) without its precursor m/z
    /// term: the spectrum terms, and the retention time term when the library scores it.
    /// </summary>
    internal static double ScoreWithoutPrecursorMz(MsScanMatchResult result, TargetOmics omics, bool useRetentionTime) {
        double dotProduct = 3d, reverseDotProduct = 2d, presence = 1d, rtFactor = 1d;
        if (omics == TargetOmics.Lipidomics) {
            dotProduct = 1d; reverseDotProduct = 2d; presence = 3d; rtFactor = 0.5d;
        }
        // The metabolomics weights for a one-peak reference are these halved, which leaves their ratio as it is.
        const double msmsFactor = 3d;
        var msms = (result.WeightedDotProduct * dotProduct + result.SimpleDotProduct * dotProduct + result.ReverseDotProduct * reverseDotProduct + result.MatchedPeaksPercentage * presence)
            / (2d * dotProduct + reverseDotProduct + presence);
        var scores = new List<double> { msms * msmsFactor };
        if (useRetentionTime && result.RtSimilarity >= 0) {
            scores.Add(result.RtSimilarity * rtFactor);
        }
        var score = scores.Average();
        return result.InChIKey.IsEmptyOrNull() ? score * 0.9d : score;
    }

    /// <summary>
    /// The record's precursor m/z as its formula and adduct give it, or the record's own precursor m/z when it has no
    /// usable formula or adduct; NaN when the two disagree by more than <see cref="FormulaConsistencyPpm"/>, or when the
    /// record has neither.
    /// </summary>
    internal static double TheoreticalPrecursorMz(MoleculeMsReference reference, out string source) {
        var library = reference.PrecursorMz;
        if (reference.Formula is { Mass: > 0d } formula && reference.AdductType is { FormatCheck: true, ChargeNumber: > 0, AdductIonXmer: > 0 } adduct) {
            var computed = adduct.ConvertToMz(formula.Mass);
            if (library > 0d && Math.Abs(computed - library) / computed * 1e6 > FormulaConsistencyPpm) {
                source = string.Empty;
                return double.NaN;
            }
            source = AnnotationCalibrant.FromFormula;
            return computed;
        }
        source = AnnotationCalibrant.FromLibraryPrecursor;
        return library > 0d ? library : double.NaN;
    }

    /// <summary>
    /// Each reference fragment with the most intense observed peak within ±<paramref name="searchPpm"/> of it; none for a
    /// fragment with no peak there, with another reference fragment within two windows of it, or whose peak has an ion
    /// at least as intense 2H to 2<sup>13</sup>C below it, whose M+2 isotope it may be or hold.
    /// </summary>
    /// <param name="observed">The observed peaks, in ascending m/z.</param>
    /// <param name="rawScan">
    /// The raw product-ion scan, in ascending m/z, in which the lighter ion and the peak are compared; the observed
    /// peaks when it is not given.
    /// </param>
    internal static List<(SpectrumPeak Reference, SpectrumPeak Observed)> PairFragments(IReadOnlyList<SpectrumPeak> observed, IReadOnlyList<SpectrumPeak> reference, double searchPpm, RawPeakElement[]? rawScan = null) {
        var pairs = new List<(SpectrumPeak, SpectrumPeak)>();
        if (reference is null || observed.Count == 0) {
            return pairs;
        }
        var fragments = reference.Where(peak => peak.Mass > 0d && peak.Intensity > 0d).OrderBy(peak => peak.Mass).ToArray();
        for (var j = 0; j < fragments.Length; j++) {
            var mz = fragments[j].Mass;
            var window = mz * searchPpm * 1e-6;
            if (j > 0 && mz - fragments[j - 1].Mass <= 2d * window || j + 1 < fragments.Length && fragments[j + 1].Mass - mz <= 2d * window) {
                continue;
            }
            var best = MostIntense(observed, mz - window, mz + window);
            if (best < 0) {
                continue;
            }
            var peak = observed[best];
            if (MayHoldTheM2IsotopeOfALighterIon(peak, observed, rawScan)) {
                continue;
            }
            pairs.Add((fragments[j], peak));
        }
        return pairs;
    }

    // Whether an ion 2H to 2 13C below the peak is at least as intense as the peak, in the raw scan when there is one.
    private static bool MayHoldTheM2IsotopeOfALighterIon(SpectrumPeak peak, IReadOnlyList<SpectrumPeak> observed, RawPeakElement[]? rawScan) {
        var lower = peak.Mass - TwoH - IsotopeParentToleranceDa;
        var upper = peak.Mass - TwoC13 + IsotopeParentToleranceDa;
        if (rawScan is { Length: > 0 }) {
            var parent = PrecursorCalibrantCollector.MostIntense(rawScan, lower, upper);
            if (parent < 0) {
                return false;
            }
            var self = PrecursorCalibrantCollector.MostIntense(rawScan, peak.Mass - IsotopeParentToleranceDa, peak.Mass + IsotopeParentToleranceDa);
            return self < 0 || rawScan[parent].Intensity >= rawScan[self].Intensity;
        }
        var lighter = MostIntense(observed, lower, upper);
        return lighter >= 0 && observed[lighter].Intensity >= peak.Intensity;
    }

    // Index of the most intense peak with lower <= m/z <= upper in peaks sorted by m/z, or -1.
    private static int MostIntense(IReadOnlyList<SpectrumPeak> peaks, double lower, double upper) {
        int lo = 0, hi = peaks.Count;
        while (lo < hi) {
            var mid = (lo + hi) / 2;
            if (peaks[mid].Mass < lower) {
                lo = mid + 1;
            }
            else {
                hi = mid;
            }
        }
        var best = -1;
        for (var i = lo; i < peaks.Count && peaks[i].Mass <= upper; i++) {
            if (best < 0 || peaks[i].Intensity > peaks[best].Intensity) {
                best = i;
            }
        }
        return best;
    }

    private sealed class Candidate
    {
        public Candidate(CalibrationLibrary library, MoleculeMsReference reference, double score) {
            Library = library;
            Reference = reference;
            Score = score;
        }

        public CalibrationLibrary Library { get; }
        public MoleculeMsReference Reference { get; }
        public double Score { get; }
    }
}

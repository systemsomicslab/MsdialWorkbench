using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Export;

/// <summary>
/// Exports every annotation candidate MS-DIAL kept for an alignment spot, one row per candidate, rather
/// than only the representative one that the .mdalign columns carry.
/// </summary>
/// <remarks>
/// A product-ion spectrum reports structure only indirectly, so a search often cannot choose between
/// references whose spectra are indistinguishable. MS-DIAL already keeps the alternatives, up to
/// NUMBER_OF_ANNOTATION_RESULTS per annotator, and alignment carries them from the representative peak
/// into the spot. Publishing only the winner turns "the search could not decide" into "the search
/// decided", which is the kind of unearned certainty this sidecar exists to remove.
///
/// Conventions follow the sibling <see cref="AlignmentProvenanceExporter"/> rather than the .mdalign
/// text export, because the consumer is the same audit pipeline: tab separated, UTF-8 without a BOM, an
/// empty cell wherever the run established nothing, and full round-trip precision instead of the display
/// rounding of a spreadsheet column. A score that reads 0.977 in .mdalign therefore reads 0.977049112
/// here; they are the same measurement.
/// </remarks>
public sealed class AlignmentCandidateExporter
{
    /// <summary>The value written wherever the run established nothing.</summary>
    private const string NotApplicable = "";

    private static readonly string[] Headers = [
        "alignment_master_id",
        "alignment_local_id",
        "parent_alignment_id",
        "candidate_rank",
        "candidate_count",
        "is_representative",
        "annotator_id",
        "database_id",
        "source",
        "priority",
        "library_id",
        "name",
        "formula",
        "ontology",
        "inchikey",
        "smiles",
        "reference_mz",
        "reference_rt_min",
        "reference_adduct",
        "annotation_tag_vs1",
        "is_reference_matched",
        "is_annotation_suggested",
        "is_precursor_mz_match",
        "is_spectrum_match",
        "is_spectrum_comparison_performed",
        "total_score",
        "mz_similarity",
        "rt_similarity",
        "ri_similarity",
        "ccs_similarity",
        "isotope_similarity",
        "simple_dot_product",
        "weighted_dot_product",
        "reverse_dot_product",
        "matched_peaks_count",
        "matched_peaks_percentage",
    ];

    private readonly IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>? _refer;
    private readonly IReadOnlyDictionary<string, string> _databaseIdByAnnotator;
    private readonly MachineCategory _machineCategory;

    public AlignmentCandidateExporter(
        IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>? refer,
        DataBaseStorage? databases,
        MachineCategory machineCategory)
    {
        _refer = refer;
        _databaseIdByAnnotator = databases is null
            ? new Dictionary<string, string>()
            : AnnotationCandidates.DatabaseIdByAnnotator(databases);
        _machineCategory = machineCategory;
    }

    public void Export(Stream stream, IEnumerable<AlignmentSpotProperty> spots)
    {
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true);
        WriteRow(writer, Headers);
        foreach (var spot in Flatten(spots)) {
            var candidates = AnnotationCandidates.Of(spot.MatchResults);
            var representative = spot.MatchResults?.Representative;
            for (int rank = 0; rank < candidates.Count; rank++) {
                var candidate = candidates[rank];
                WriteCandidate(writer, spot, candidate, rank + 1, candidates.Count, ReferenceEquals(candidate, representative));
            }
        }
    }

    private static IEnumerable<AlignmentSpotProperty> Flatten(IEnumerable<AlignmentSpotProperty> spots)
    {
        foreach (var spot in spots) {
            yield return spot;
            foreach (var driftSpot in spot.AlignmentDriftSpotFeatures ?? []) {
                yield return driftSpot;
            }
        }
    }

    private void WriteCandidate(
        StreamWriter writer,
        AlignmentSpotProperty spot,
        MsScanMatchResult candidate,
        int rank,
        int candidateCount,
        bool isRepresentative)
    {
        var reference = _refer?.Refer(candidate);
        var hadProductIonSpectrum = spot.IsMsmsAssigned;
        // One decision, shared with the .mdalign and .mdpeak score columns: a spectral score exists only
        // when a product-ion spectrum was actually compared against a reference spectrum. The rendering
        // differs, an empty cell here against the text "null" there, but the condition must not.
        var spectrumScored = AnnotationScoreFormat.IsComputed(candidate, hadProductIonSpectrum);
        WriteRow(writer, [
            spot.MasterAlignmentID.ToString(CultureInfo.InvariantCulture),
            spot.AlignmentID.ToString(CultureInfo.InvariantCulture),
            spot.ParentAlignmentID.ToString(CultureInfo.InvariantCulture),
            rank.ToString(CultureInfo.InvariantCulture),
            candidateCount.ToString(CultureInfo.InvariantCulture),
            BooleanText(isRepresentative),
            Sanitize(candidate.AnnotatorID),
            DatabaseId(candidate),
            SourceText(candidate.Source),
            candidate.Priority.ToString(CultureInfo.InvariantCulture),
            candidate.LibraryID.ToString(CultureInfo.InvariantCulture),
            Sanitize(string.IsNullOrEmpty(candidate.Name) ? reference?.Name : candidate.Name),
            Sanitize(reference?.Formula?.FormulaString),
            Sanitize(string.IsNullOrEmpty(reference?.CompoundClass) ? reference?.Ontology : reference?.CompoundClass),
            Sanitize(string.IsNullOrEmpty(candidate.InChIKey) ? reference?.InChIKey : candidate.InChIKey),
            Sanitize(reference?.SMILES),
            reference is null ? NotApplicable : Format(reference.PrecursorMz),
            reference?.ChromXs is null ? NotApplicable : Format(reference.ChromXs.RT.Value),
            Sanitize(reference?.AdductType?.AdductIonName),
            DataAccess.GetAnnotationCode(candidate, _machineCategory).ToString(CultureInfo.InvariantCulture),
            BooleanText(candidate.IsReferenceMatched),
            BooleanText(candidate.IsAnnotationSuggested),
            BooleanText(candidate.IsPrecursorMzMatch),
            BooleanText(candidate.IsSpectrumMatch),
            BooleanText(spectrumScored),
            Format(candidate.TotalScore),
            // These five do carry sentinels, and two different ones, because two different functions
            // produce them. The four Gaussian terms are positive when measured, -1 when there was nothing
            // to compare, and left at the field's default 0 when the run never enabled the term, so only
            // a positive value is a measurement. The isotope term is 1 minus an accumulated ratio
            // difference and is genuinely signed, so a negative value there IS a measurement -- it says
            // the patterns disagree -- and only exactly -1 is the sentinel. Both rules live on
            // MsScanMatchResult so the scoring side and the export side cannot drift apart.
            GaussianTerm(candidate.AcurateMassSimilarity),
            GaussianTerm(candidate.RtSimilarity),
            GaussianTerm(candidate.RiSimilarity),
            GaussianTerm(candidate.CcsSimilarity),
            candidate.IsIsotopeComparisonPerformed ? Format(candidate.IsotopeSimilarity) : NotApplicable,
            Score(spectrumScored, candidate.SimpleDotProduct),
            Score(spectrumScored, candidate.WeightedDotProduct),
            Score(spectrumScored, candidate.ReverseDotProduct),
            Score(spectrumScored, candidate.MatchedPeaksCount),
            Score(spectrumScored, candidate.MatchedPeaksPercentage),
        ]);
    }

    private string DatabaseId(MsScanMatchResult candidate)
        => candidate.AnnotatorID is string id && _databaseIdByAnnotator.TryGetValue(id, out var databaseId)
            ? Sanitize(databaseId)
            : NotApplicable;

    /// <summary>Writes one row, refusing a field count that does not match the header.</summary>
    private static void WriteRow(StreamWriter writer, string[] values)
    {
        if (values.Length != Headers.Length) {
            throw new InvalidOperationException(
                $"Alignment candidate row has {values.Length} fields but the header declares {Headers.Length}.");
        }
        writer.WriteLine(string.Join("\t", values));
    }

    private static string Score(bool computed, float value)
        => computed ? Format(value) : NotApplicable;

    private static string GaussianTerm(float similarity)
        => MsScanMatchResult.IsGaussianTermMeasured(similarity) ? Format(similarity) : NotApplicable;

    /// <summary>
    /// The stored score fields are Single, and G9 round-trips a Single exactly. The widening to double is
    /// deliberate and matches <see cref="AnnotationScoreFormat"/>: .NET Framework formats a Single through
    /// a 7-significant-digit intermediate, which shifts values that sit just below a rounding boundary.
    /// </summary>
    private static string Format(float value)
        => ((double)value).ToString("G9", CultureInfo.InvariantCulture);

    private static string Format(double value)
        => value.ToString("G17", CultureInfo.InvariantCulture);

    private static string SourceText(SourceType source)
        => source.ToString().Replace(" ", string.Empty);

    private static string BooleanText(bool value) => value ? "true" : "false";

    private static string Sanitize(string? value)
        => (value ?? string.Empty).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}

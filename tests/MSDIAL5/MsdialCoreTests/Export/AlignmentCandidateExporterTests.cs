using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCoreTests.Export;

[TestClass]
public class AlignmentCandidateExporterTests
{
    // Whole-line assertions, for the same reason the provenance exporter tests use them: a substring
    // assertion cannot see a ragged field count, and this file has three hand-maintained column lists.
    private const string ExpectedHeader =
        "alignment_master_id\talignment_local_id\tparent_alignment_id\tcandidate_rank\tcandidate_count\t" +
        "is_representative\tannotator_id\tdatabase_id\tsource\tpriority\tlibrary_id\tname\tformula\t" +
        "ontology\tinchikey\tsmiles\treference_mz\treference_rt_min\treference_adduct\t" +
        "annotation_tag_vs1\tis_reference_matched\tis_annotation_suggested\tis_precursor_mz_match\t" +
        "is_spectrum_match\tis_spectrum_comparison_performed\ttotal_score\tmz_similarity\t" +
        "rt_similarity\tri_similarity\tccs_similarity\tisotope_similarity\tsimple_dot_product\t" +
        "weighted_dot_product\treverse_dot_product\tmatched_peaks_count\tmatched_peaks_percentage";

    [TestMethod]
    public void EveryCandidateIsExportedOnceInRankOrder()
    {
        var best = ReferenceMatch("Quercetin", libraryId: 7, totalScore: 0.75f);
        var alternative = ReferenceMatch("Morin", libraryId: 8, totalScore: 0.5f);
        var spot = SpotWith(best, alternative);

        var lines = ExportLines(spot, Refer((7, ReferenceFor("Quercetin")), (8, ReferenceFor("Morin"))));

        Assert.AreEqual(ExpectedHeader, lines[0]);
        Assert.AreEqual(3, lines.Length);
        Assert.AreEqual(
            "12\t10\t-1\t1\t2\ttrue\tmsp\t\tMspDB\t1\t7\tQuercetin\tC15H10O7\tflavonoid\tQUERCETIN-KEY\tc1cc(O)ccc1\t" +
            // rt, ri and ccs are empty: this run never enabled those terms, so the 0 sitting in the field
            // is the unset default rather than a measurement. The isotope 0 is written, because only
            // exactly -1 is that term's sentinel and 0 there is a real ratio agreement.
            "300.125\t2.5\t[M+H]+\t430\ttrue\tfalse\ttrue\ttrue\ttrue\t0.75\t0.5\t\t\t\t0\t0.5\t0.25\t0.75\t12\t0.5",
            lines[1]);
        Assert.AreEqual(
            "12\t10\t-1\t2\t2\tfalse\tmsp\t\tMspDB\t1\t8\tMorin\tC15H10O7\tflavonoid\tMORIN-KEY\tc1cc(O)ccc1\t" +
            "300.125\t2.5\t[M+H]+\t430\ttrue\tfalse\ttrue\ttrue\ttrue\t0.5\t0.5\t\t\t\t0\t0.5\t0.25\t0.75\t12\t0.5",
            lines[2]);
    }

    /// <summary>
    /// The precedence that picks the representative is the precedence the file publishes: a reference
    /// match outranks a precursor-only suggestion even when the suggestion's annotator has the higher
    /// priority.
    /// </summary>
    [TestMethod]
    public void AReferenceMatchOutranksAHigherPrioritySuggestion()
    {
        var suggestion = ReferenceMatch("Suggested", libraryId: 8, totalScore: 0.5f);
        suggestion.IsReferenceMatched = false;
        suggestion.IsAnnotationSuggested = true;
        suggestion.IsSpectrumMatch = false;
        suggestion.Priority = 9;
        var match = ReferenceMatch("Matched", libraryId: 7, totalScore: 0.5f);

        var lines = ExportLines(SpotWith(suggestion, match), Refer());

        CollectionAssert.AreEqual(
            new[] { "Matched", "Suggested" },
            lines.Skip(1).Select(line => line.Split('\t')[11]).ToArray());
        CollectionAssert.AreEqual(
            new[] { "true", "false" },
            lines.Skip(1).Select(line => line.Split('\t')[5]).ToArray());
    }

    /// <summary>
    /// A spectral score that was never computed is an empty cell, never a 0. The distinction is the same
    /// one <see cref="AnnotationScoreFormat"/> makes for the .mdalign columns.
    /// </summary>
    [TestMethod]
    public void AnUncomparedSpectrumPublishesNoSpectralScore()
    {
        var precursorOnly = ReferenceMatch("Suggested", libraryId: 7, totalScore: 0.5f);
        precursorOnly.IsSpectrumMatch = false;
        // The scoring functions return -1 when there was no product-ion spectrum to compare.
        precursorOnly.SquaredSimpleDotProduct = -1f;
        precursorOnly.SquaredWeightedDotProduct = -1f;
        precursorOnly.SquaredReverseDotProduct = -1f;
        precursorOnly.MatchedPeaksCount = -1f;
        precursorOnly.MatchedPeaksPercentage = -1f;

        var fields = ExportLines(SpotWith(precursorOnly), Refer())[1].Split('\t');

        Assert.AreEqual("false", fields[24], "is_spectrum_comparison_performed");
        CollectionAssert.AreEqual(
            new[] { "", "", "", "", "" },
            new[] { fields[31], fields[32], fields[33], fields[34], fields[35] });
        Assert.AreEqual("0.5", fields[25], "the aggregate total score is still a measurement");
    }

    /// <summary>
    /// The isotope term has its own sentinel and its own rule. -1 means the comparison was never
    /// attempted; every other negative value is a measurement saying the patterns disagree.
    /// </summary>
    [TestMethod]
    public void AnUnattemptedIsotopeComparisonPublishesNoIsotopeSimilarity()
    {
        // GetIsotopeRatioSimilarity returns -1 when either side carries no isotopic peaks. Unlike the
        // dot products there is no clamping getter, so the -1 reaches the export unchanged. On one real
        // reference library 495 of 4972 candidate rows carried it, all of them in-house records with no
        // isotopic pattern deposited.
        var candidate = ReferenceMatch("Quercetin", libraryId: 7, totalScore: 0.5f);
        candidate.IsotopeSimilarity = -1f;

        var fields = ExportLines(SpotWith(candidate), Refer())[1].Split('\t');

        Assert.AreEqual("", fields[30], "isotope_similarity");
    }

    [TestMethod]
    public void AnIsotopePatternThatDisagreesIsAMeasurement()
    {
        // 1 minus an accumulated ratio difference, so this is genuinely signed. Discarding it would
        // throw away the strongest negative evidence the term produces.
        var candidate = ReferenceMatch("Quercetin", libraryId: 7, totalScore: 0.5f);
        candidate.IsotopeSimilarity = -0.75f;

        var fields = ExportLines(SpotWith(candidate), Refer())[1].Split('\t');

        Assert.AreEqual("-0.75", fields[30], "isotope_similarity");
    }

    /// <summary>
    /// The four Gaussian terms follow the opposite rule, because a different function produces them.
    /// </summary>
    [TestMethod]
    public void AGaussianTermPublishesNothingUnlessItWasMeasured()
    {
        var candidate = ReferenceMatch("Quercetin", libraryId: 7, totalScore: 0.5f);
        candidate.AcurateMassSimilarity = 0.5f;   // measured
        candidate.RtSimilarity = 0f;              // the run never enabled retention-time scoring
        candidate.RiSimilarity = -1f;             // attempted, nothing to compare
        candidate.CcsSimilarity = 0.25f;          // measured

        var fields = ExportLines(SpotWith(candidate), Refer())[1].Split('\t');

        CollectionAssert.AreEqual(
            new[] { "0.5", "", "", "0.25" },
            new[] { fields[26], fields[27], fields[28], fields[29] });
    }

    [TestMethod]
    public void AnUnannotatedSpotContributesNoRow()
    {
        var lines = ExportLines(SpotWith(), Refer());

        Assert.AreEqual(ExpectedHeader, lines[0]);
        Assert.AreEqual(1, lines.Length);
    }

    /// <summary>
    /// "Set unknown" stores a real match result carrying the unknown flag. It names no molecule, so it is
    /// not a candidate.
    /// </summary>
    [TestMethod]
    public void ASetUnknownAssignmentIsNotACandidate()
    {
        var unknown = new MsScanMatchResult { Source = SourceType.Manual | SourceType.Unknown, };

        var lines = ExportLines(SpotWith(unknown), Refer());

        Assert.AreEqual(1, lines.Length);
    }

    /// <summary>
    /// A candidate whose reference cannot be resolved still publishes its own evidence; the columns that
    /// come from the reference are empty rather than invented.
    /// </summary>
    [TestMethod]
    public void AnUnresolvableReferenceLeavesTheReferenceColumnsEmpty()
    {
        var fields = ExportLines(SpotWith(ReferenceMatch("Quercetin", libraryId: 7, totalScore: 0.5f)), Refer())[1].Split('\t');

        Assert.AreEqual("Quercetin", fields[11], "the name is on the match result itself");
        CollectionAssert.AreEqual(
            new[] { "", "", "", "", "" },
            new[] { fields[12], fields[13], fields[15], fields[16], fields[17] });
    }

    private static MsScanMatchResult ReferenceMatch(string name, int libraryId, float totalScore)
        => new() {
            Name = name,
            Source = SourceType.MspDB,
            AnnotatorID = "msp",
            Priority = 1,
            LibraryID = libraryId,
            TotalScore = totalScore,
            IsReferenceMatched = true,
            IsPrecursorMzMatch = true,
            IsSpectrumMatch = true,
            AcurateMassSimilarity = 0.5f,
            SquaredSimpleDotProduct = 0.25f,
            SquaredWeightedDotProduct = 0.0625f,
            SquaredReverseDotProduct = 0.5625f,
            MatchedPeaksCount = 12f,
            MatchedPeaksPercentage = 0.5f,
        };

    private static MoleculeMsReference ReferenceFor(string name)
        => new() {
            Name = name,
            Formula = new Formula { FormulaString = "C15H10O7", },
            Ontology = "flavonoid",
            InChIKey = name.ToUpperInvariant() + "-KEY",
            SMILES = "c1cc(O)ccc1",
            PrecursorMz = 300.125,
            ChromXs = new ChromXs(2.5),
            AdductType = AdductIon.GetAdductIon("[M+H]+"),
        };

    private static AlignmentSpotProperty SpotWith(params MsScanMatchResult[] results)
    {
        var spot = new AlignmentSpotProperty {
            MasterAlignmentID = 12,
            AlignmentID = 10,
            ParentAlignmentID = -1,
            AlignedPeakProperties = [],
        };
        spot.MatchResults.AddResults(results);
        return spot;
    }

    private static string[] ExportLines(AlignmentSpotProperty spot, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer)
    {
        using var stream = new MemoryStream();
        new AlignmentCandidateExporter(refer, null, MachineCategory.LCMS).Export(stream, [spot]);
        return Encoding.UTF8.GetString(stream.ToArray())
            .Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .Where(line => line.Length > 0)
            .ToArray();
    }

    private static IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> Refer(params (int LibraryId, MoleculeMsReference Reference)[] references)
        => new StubRefer(references.ToDictionary(pair => pair.LibraryId, pair => pair.Reference));

    private sealed class StubRefer(Dictionary<int, MoleculeMsReference> byLibraryId)
        : IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>
    {
        public string Key => "stub";

        public MoleculeMsReference? Refer(MsScanMatchResult? result)
            => result is not null && byLibraryId.TryGetValue(result.LibraryID, out var reference) ? reference : null;
    }
}

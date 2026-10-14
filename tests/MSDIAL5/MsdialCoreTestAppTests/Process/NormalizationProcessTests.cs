using CompMs.App.MsdialConsole.Process;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MsdialCoreTestAppTests.Process;

/// <summary>
/// MSDIALCUI normalize on an alignment built in memory: the standards table it reads, how it
/// ties each standard to a peak, and the two matrices it writes.
/// </summary>
/// <remarks>
/// The alignment has three files (two SPF samples and a blank) and eleven peaks: standards for
/// PC, Cer-NS and "Any others" (an LPC), a PC, a ceramide whose reference spells its class
/// "Cer_NS", a cardiolipin with no standard of its own, a peak with no annotation, a second
/// unannotated peak that a caller can assign as a standard, a ceramide suggestion that is not
/// reference matched, a ceramide whose reference gives only an Ontology, and a lipid A, a class
/// with no MS-DIAL lipid class name.
/// </remarks>
[TestClass]
public sealed class NormalizationProcessTests
{
    private const int PcStandard = 0, Pc = 1, CerStandard = 2, Cer = 3, LpcStandard = 4, Cardiolipin = 5, Unknown = 6, Unnamed = 7;
    private const int LowScoreCer = 8, OntologyOnlyCer = 9, LipidA = 10;

    private const string Header = "StandardName\tTargetClass\tConcentration\tPeakID";
    private const string PcRow = "PC 15:0_18:1(d7)\tPC\t66.4\t0";
    private const string CerRow = "Cer 18:1;O2/15:0(d7)\tCer-NS\t20\t2";
    private const string LpcRow = "LPC 18:1(d7)\tAny others\t100\t4";

    private string _scratch = null!;

    [TestInitialize]
    public void Initialize() {
        _scratch = Path.Combine(Path.GetTempPath(), $"msdial-normalize-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_scratch);
    }

    [TestCleanup]
    public void Cleanup() {
        if (Directory.Exists(_scratch)) {
            Directory.Delete(_scratch, recursive: true);
        }
    }

    [TestMethod]
    public void PeakIdsOnly_AStandardWithoutAnIdIsUnresolvedEvenWhenAPeakCarriesItsName() {
        var fixture = new Fixture();
        var records = Records(Header, PcRow, "Cer 18:1;O2/15:0(d7)\tCer-NS\t20\t", LpcRow);

        var byName = NormalizationProcess.ResolveStandards(records, fixture.Spots, peakIdsOnly: false);
        Assert.AreEqual(CerStandard, byName.Compounds.Single(compound => compound.TargetClass == "Cer-NS").PeakID,
            "without the flag a standard without an ID is still found by its name");

        var idsOnly = NormalizationProcess.ResolveStandards(records, fixture.Spots, peakIdsOnly: true);
        Assert.IsFalse(idsOnly.Compounds.Any(compound => compound.StandardName.StartsWith("Cer")));
        CollectionAssert.AreEqual(new[] { "Cer 18:1;O2/15:0(d7) (for Cer-NS)" }, idsOnly.Unresolved);
        StringAssert.Contains(string.Join("\n", idsOnly.Report), "does not look a standard up by name");
    }

    [TestMethod]
    public void PeakIdsOnly_AnIdThatIsNotInTheAlignmentIsUnresolvedRatherThanLookedUpByName() {
        var fixture = new Fixture();
        var records = Records(Header, PcRow, "Cer 18:1;O2/15:0(d7)\tCer-NS\t20\t99", LpcRow);

        var byName = NormalizationProcess.ResolveStandards(records, fixture.Spots, peakIdsOnly: false);
        Assert.AreEqual(CerStandard, byName.Compounds.Single(compound => compound.TargetClass == "Cer-NS").PeakID);

        var idsOnly = NormalizationProcess.ResolveStandards(records, fixture.Spots, peakIdsOnly: true);
        Assert.AreEqual(1, idsOnly.Unresolved.Count);
        StringAssert.Contains(string.Join("\n", idsOnly.Report), "alignment ID 99 is not in this alignment");
    }

    [TestMethod]
    public void PeakIdsOnly_RefusesATableWithoutAPeakIdColumn() {
        var path = Table("StandardName\tTargetClass\tConcentration", "PC 15:0_18:1(d7)\tPC\t66.4");

        Assert.AreEqual(1, NormalizationProcess.ReadStandards(path, peakIdsOnly: false).Count);
        var error = Assert.ThrowsException<FormatException>(() => NormalizationProcess.ReadStandards(path, peakIdsOnly: true));
        StringAssert.Contains(error.Message, "PeakID");
    }

    [TestMethod]
    public void AnUnresolvedHyphenClassEmptiesTheRowsWhoseOntologyIsSpelledWithAnUnderscore() {
        // The table names "Cer-NS"; the export's Ontology column says "Cer_NS". Compared as
        // written, the ceramide stayed in the matrix quantified against the LPC.
        var fixture = new Fixture();
        var run = Run(fixture, Options(peakIdsOnly: true, allowUnresolved: true),
            Header, PcRow, "Cer 18:1;O2/15:0(d7)\tCer-NS\t20\t", LpcRow);

        Assert.AreEqual(0, run.ExitCode, run.Error);
        var cer = run.Normalized.Row(Cer);
        Assert.IsTrue(cer.Samples.All(value => value.Length == 0));
        foreach (var (stat, fileClass) in new[] { ("Average", "SPF"), ("Stdev", "SPF"), ("Average", "Blank"), ("Stdev", "Blank") }) {
            Assert.AreEqual(string.Empty, cer.Stat(stat, fileClass), $"{stat} {fileClass} of the Any-others-divided values");
        }
        Assert.AreEqual(
            "NOT QUANTIFIED: the Cer-NS standard Cer 18:1;O2/15:0(d7) did not resolve in this alignment",
            cer.Comment);
        StringAssert.Contains(run.Output,
            "WARNING: 1 internal standard(s) did not resolve: Cer 18:1;O2/15:0(d7) (for Cer-NS). "
            + "Rows in the classes only they would quantify carry no concentration and say so: Cer-NS.");
        Assert.AreEqual(66.4 * 50 / 100, run.Normalized.Row(Pc).Value("s1"), 1e-9);
        Assert.AreEqual(100d * 30 / 1000, run.Normalized.Row(Cardiolipin).Value("s1"), 1e-9,
            "a class with no standard of its own still falls through to Any others");
        Assert.AreEqual(10d, run.Raw.Row(Cer).Value("s1"), "the raw matrix keeps every height");
    }

    [TestMethod]
    public void ATableThatSpellsTheClassWithAnUnderscoreQuantifiesItAgainstItsOwnStandard() {
        var fixture = new Fixture();
        var run = Run(fixture, Options(peakIdsOnly: true),
            Header, PcRow, "Cer 18:1;O2/15:0(d7)\tCer_NS\t20\t2", LpcRow);

        Assert.AreEqual(0, run.ExitCode, run.Error);
        Assert.AreEqual(20d * 10 / 20, run.Normalized.Row(Cer).Value("s1"), 1e-9,
            "Cer_NS and Cer-NS are one class; it used to be divided by the LPC instead");
        Assert.AreEqual(fixture.Spots[CerStandard].MasterAlignmentID, fixture.Spots[Cer].InternalStandardAlignmentID);
    }

    [TestMethod]
    public void AnUnresolvedAnyOthersStandardLeavesNoRawIntensityRows() {
        var fixture = new Fixture();
        var run = Run(fixture, Options(peakIdsOnly: true, allowUnresolved: true),
            Header, PcRow, CerRow, "LPC 18:1(d7)\tAny others\t100\t");

        Assert.AreEqual(0, run.ExitCode, run.Error);
        foreach (var id in new[] { Cardiolipin, Unknown, LpcStandard }) {
            var row = run.Normalized.Row(id);
            Assert.IsTrue(row.Samples.All(value => value.Length == 0), $"row {id}");
            StringAssert.Contains(row.Comment, "the Any others standard LPC 18:1(d7) did not resolve");
        }
        Assert.IsFalse(run.Normalized.Text.Contains("Normalized unit: Intensity"));
        Assert.AreEqual(66.4 * 50 / 100, run.Normalized.Row(Pc).Value("s1"), 1e-9);
        Assert.AreEqual(20d * 10 / 20, run.Normalized.Row(Cer).Value("s1"), 1e-9);
        StringAssert.Contains(run.Output,
            "WARNING: 1 internal standard(s) did not resolve: LPC 18:1(d7) (for Any others). Rows in the classes only "
            + "they would quantify carry no concentration and say so: any class without a standard of its own.");

        var refused = Run(new Fixture(), Options(peakIdsOnly: true), Header, PcRow, CerRow, "LPC 18:1(d7)\tAny others\t100\t");
        Assert.AreEqual(2, refused.ExitCode);
        StringAssert.Contains(refused.Error,
            "1 internal standard(s) were not found in the alignment: LPC 18:1(d7) (for Any others). Rows in the classes "
            + "only they would quantify would be left without a concentration: any class without a standard of its own.");
    }

    [TestMethod]
    public void AStandardWithNoHeightInAFileLeavesThatFileEmptyInsteadOfDividingByOne() {
        var fixture = new Fixture(lpcHeights: new[] { 1000d, 500d, 0d });
        var run = Run(fixture, Options(peakIdsOnly: true), Header, PcRow, CerRow, LpcRow);

        Assert.AreEqual(0, run.ExitCode, run.Error);
        var cardiolipin = run.Normalized.Row(Cardiolipin);
        Assert.AreEqual(100d * 30 / 1000, cardiolipin.Value("s1"), 1e-9);
        Assert.AreEqual(100d * 60 / 500, cardiolipin.Value("s2"), 1e-9);
        Assert.AreEqual(string.Empty, cardiolipin.Cell("blank"), "90 / 1 is not a concentration");
        Assert.AreEqual(string.Empty, cardiolipin.Stat("Average", "Blank"));
        Assert.AreEqual(string.Empty, cardiolipin.Stat("Stdev", "Blank"));
        Assert.AreEqual((3d + 12d) / 2, double.Parse(cardiolipin.Stat("Average", "SPF"), CultureInfo.CurrentCulture), 1e-9);
        StringAssert.Contains(cardiolipin.Comment,
            "NOT QUANTIFIED in 1 file(s) (blank): the internal standard LPC 18:1(d7) has no peak height there");
        StringAssert.Contains(cardiolipin.Comment, "Normalized unit: pmol_per_microL_plasma");
        Assert.AreEqual(string.Empty, run.Normalized.Row(Unknown).Cell("blank"));
        Assert.AreEqual(100d, run.Normalized.Row(LpcStandard).Value("blank"), "the standard's own row keeps its amount");
        Assert.AreEqual(66.4 * 10 / 400, run.Normalized.Row(Pc).Value("blank"), 1e-9, "another standard is unaffected");
        StringAssert.Contains(run.Output,
            "WARNING: internal standard LPC 18:1(d7) (alignment ID 4) has no peak height in 1 file(s): blank.");
    }

    [TestMethod]
    public void AssignNameExportsThePeakUnderTheStandardsNameInBothMatrices() {
        var fixture = new Fixture();
        var lines = new[] {
            Header + "\tAssignName",
            PcRow + "\ttrue",
            CerRow + "\tfalse",
            LpcRow + "\t",
            "PE 15:0_18:1(d7)\tPE\t50\t7\tTRUE",
        };

        var guarded = Run(fixture, Options(peakIdsOnly: true), lines);
        Assert.AreEqual(2, guarded.ExitCode, "an unannotated peak is still a mismatch until the caller says so");
        StringAssert.Contains(guarded.Error,
            "1 standard(s) are assigned by AssignName to a peak that carries another name or none: "
            + "PE 15:0_18:1(d7) (alignment ID 7, unannotated).");
        StringAssert.Contains(guarded.Error, "AssignName renames such a peak only with --allow-mismatched-peak-ids");
        Assert.IsFalse(guarded.Error.Contains("Correct the PeakID"), guarded.Error);
        Assert.IsFalse(guarded.Error.Contains("different compound"), guarded.Error);

        // The same peak only taken, not assigned, is the stale-table case the guard is for.
        var taken = Run(new Fixture(), Options(peakIdsOnly: true), Header, PcRow, CerRow, LpcRow, "PE 15:0_18:1(d7)\tPE\t50\t7");
        Assert.AreEqual(2, taken.ExitCode);
        StringAssert.Contains(taken.Error,
            "1 standard(s) name an alignment ID that holds a different compound or none: "
            + "PE 15:0_18:1(d7) (alignment ID 7, unannotated). ");
        StringAssert.Contains(taken.Error, "Correct the PeakID");
        Assert.IsFalse(taken.Error.Contains("AssignName"), taken.Error);

        var run = Run(new Fixture(), Options(peakIdsOnly: true, allowMismatched: true), lines);
        Assert.AreEqual(0, run.ExitCode, run.Error);
        StringAssert.Contains(run.Output, "ASSIGNED PE 15:0_18:1(d7) to alignment ID 7 (unannotated)");
        foreach (var matrix in new[] { run.Raw, run.Normalized }) {
            var assigned = matrix.Row(Unnamed);
            Assert.AreEqual("PE 15:0_18:1(d7)", assigned.Name);
            StringAssert.Contains(assigned.Comment, "Annotation method: internal standard assignment");
            var pc = matrix.Row(PcStandard);
            Assert.AreEqual("PC 15:0_18:1(d7)", pc.Name);
            StringAssert.Contains(pc.Comment, "Annotation method: internal standard assignment");
            Assert.IsFalse(pc.Comment.Contains("LbmDB"), "the name is no longer the annotator's");
            Assert.AreEqual("Cer 18:1;O2/15:0(d7)", matrix.Row(CerStandard).Name);
            StringAssert.Contains(matrix.Row(CerStandard).Comment, "Annotation method: LbmDB: test");
        }
        Assert.AreEqual(50d, run.Normalized.Row(Unnamed).Value("s1"));
        Assert.AreEqual("300", run.Raw.Row(Unnamed).Cell("s1"));
    }

    [TestMethod]
    public void AnAssignedPeakAnnotatedAsAnotherCompoundKeepsNoneOfThatAnnotationsReferenceOrScores() {
        // The PC 34:1 peak is, by the caller's search, the PE standard. Renamed but otherwise left
        // alone, its row said PE by name and PC by Ontology, InChIKey and scores.
        var fixture = new Fixture();
        var match = fixture.Spots[Pc].MatchResults.Representative;
        match.TotalScore = 0.9f;
        match.AcurateMassSimilarity = 0.95f;
        match.IsPrecursorMzMatch = true;
        match.IsSpectrumMatch = true;
        var reference = fixture.Refer.Refer(match)!;
        reference.InChIKey = "PCPCPCPCPCPCPC-UHFFFAOYSA-N";
        reference.SMILES = "CCCCCC";

        var before = Run(fixture, Options(peakIdsOnly: true), Header, PcRow, CerRow, LpcRow);
        Assert.AreEqual(0, before.ExitCode, before.Error);
        var annotated = before.Raw.Row(Pc);
        Assert.AreEqual("PC", annotated.Cell("Ontology"));
        Assert.AreEqual("PCPCPCPCPCPCPC-UHFFFAOYSA-N", annotated.Cell("INCHIKEY"));
        Assert.AreEqual("0.900", annotated.Cell("Total score"));
        Assert.AreNotEqual(before.Raw.Row(Unknown).Cell("Annotation tag (VS1.0)"), annotated.Cell("Annotation tag (VS1.0)"));

        var run = Run(fixture, Options(peakIdsOnly: true, allowMismatched: true),
            Header + "\tAssignName", PcRow + "\t", CerRow + "\t", LpcRow + "\t", "PE 15:0_18:1(d7)\tPE\t50\t1\ttrue");
        Assert.AreEqual(0, run.ExitCode, run.Error);
        StringAssert.Contains(run.Output, "ASSIGNED PE 15:0_18:1(d7) to alignment ID 1 (annotated 'PC 34:1')");
        var columns = new[] {
            "Reference RT", "Reference m/z", "Formula", "Ontology", "INCHIKEY", "SMILES", "Annotation tag (VS1.0)",
            "RT matched", "m/z matched", "MS/MS matched", "RT similarity", "m/z similarity", "Simple dot product",
            "Weighted dot product", "Reverse dot product", "Matched peaks count", "Matched peaks percentage",
            "Total score", "Measured terms", "Evidence source", "Candidates found", "Candidates above threshold",
            "Candidates reference matched",
        };
        foreach (var matrix in new[] { run.Raw, run.Normalized }) {
            var assigned = matrix.Row(Pc);
            var unannotated = matrix.Row(Unknown);
            Assert.AreEqual("PE 15:0_18:1(d7)", assigned.Name);
            foreach (var column in columns) {
                Assert.AreEqual(unannotated.Cell(column), assigned.Cell(column), column);
            }
            Assert.AreEqual(
                "Normalized unit: pmol_per_microL_plasma; Annotation method: internal standard assignment",
                assigned.Comment);
            Assert.AreEqual(annotated.Cell("Average Mz"), assigned.Cell("Average Mz"), "what was measured stays");
        }
        Assert.AreEqual(50d, run.Normalized.Row(Pc).Value("s1"));
    }

    [TestMethod]
    public void UnderTheDefaultUnitAStandardDividedByAnotherStandardIsCheckedLikeAnyOtherRow() {
        // The default unit divides every peak by the standard of its own class, the standards'
        // peaks included. An assigned peak MS-DIAL left unnamed is "Unknown" to it, so it is
        // divided by the Any others standard: 500 x 300 / 1 in the blank, where the LPC has no
        // height, and its raw 300 when that standard did not resolve.
        var unit = IonAbundanceUnit.NormalizedByInternalStandardPeakHeight;
        var lines = new[] {
            Header + "\tAssignName",
            PcRow + "\t",
            CerRow + "\t",
            LpcRow + "\t",
            "PE 15:0_18:1(d7)\tPE\t50\t7\ttrue",
        };
        var run = Run(new Fixture(lpcHeights: new[] { 1000d, 500d, 0d }),
            Options(peakIdsOnly: true, allowMismatched: true, unit: unit), lines);

        Assert.AreEqual(0, run.ExitCode, run.Error);
        var assigned = run.Normalized.Row(Unnamed);
        Assert.AreEqual("PE 15:0_18:1(d7)", assigned.Name);
        Assert.AreEqual(500d * 300 / 1000, assigned.Value("s1"), 1e-9);
        Assert.AreEqual(string.Empty, assigned.Cell("blank"), "500 x 300 / 1 is not a normalized height");
        StringAssert.Contains(assigned.Comment,
            "NOT QUANTIFIED in 1 file(s) (blank): the internal standard LPC 18:1(d7) has no peak height there");
        StringAssert.Contains(assigned.Comment, "Annotation method: internal standard assignment");
        var lpc = run.Normalized.Row(LpcStandard);
        Assert.AreEqual(500d, lpc.Value("s1"), 1e-9, "a standard divided by itself is left as the normalizer wrote it");
        Assert.AreEqual(0d, lpc.Value("blank"));
        Assert.IsFalse(lpc.Comment.Contains("NOT QUANTIFIED"), lpc.Comment);
        Assert.AreEqual((100d + 200d + 400d) / 3, run.Normalized.Row(PcStandard).Value("blank"), 1e-9);

        var unresolved = Run(new Fixture(), Options(peakIdsOnly: true, allowMismatched: true, allowUnresolved: true, unit: unit),
            Header + "\tAssignName",
            PcRow + "\t",
            CerRow + "\t",
            "LPC 18:1(d7)\tAny others\t100\t\t",
            "PE 15:0_18:1(d7)\tPE\t50\t7\ttrue");
        Assert.AreEqual(0, unresolved.ExitCode, unresolved.Error);
        var raw = unresolved.Normalized.Row(Unnamed);
        Assert.IsTrue(raw.Samples.All(value => value.Length == 0), string.Join(",", raw.Samples));
        StringAssert.StartsWith(raw.Comment,
            "NOT QUANTIFIED: no standard of its own class, and the Any others standard LPC 18:1(d7) did not resolve");
        Assert.IsFalse(unresolved.Normalized.Text.Contains("Normalized unit: Intensity"));
    }

    [TestMethod]
    public void TwoStandardsAssignedToOnePeakStopTheRun() {
        var run = Run(new Fixture(), Options(peakIdsOnly: true, allowMismatched: true),
            Header + "\tAssignName",
            PcRow + "\t",
            LpcRow + "\t",
            "PE 15:0_18:1(d7)\tPE\t50\t7\ttrue",
            "PG 15:0_18:1(d7)\tPG\t50\t7\ttrue");

        Assert.AreEqual(2, run.ExitCode);
        StringAssert.Contains(run.Error, "alignment ID 7 (PE 15:0_18:1(d7) and PG 15:0_18:1(d7))");
        Assert.IsFalse(File.Exists(run.NormalizedPath));
    }

    [TestMethod]
    public void AnAssignNameThatIsNeitherTrueNorFalseIsRefused() {
        var path = Table(Header + "\tAssignName", PcRow + "\tmaybe");

        var error = Assert.ThrowsException<FormatException>(() => NormalizationProcess.ReadStandards(path));
        StringAssert.Contains(error.Message, "maybe");
    }

    [TestMethod]
    public void TheDilutionFactorMultipliesEachFileByItsOwnFactorWhenAFileBetweenIsExcluded() {
        var files = new[] {
            AnalysisFile(0, "s1", "SPF", AnalysisFileType.Sample, dilution: 2d),
            AnalysisFile(1, "excluded", "SPF", AnalysisFileType.Sample, dilution: 100d, included: false),
            AnalysisFile(2, "s2", "SPF", AnalysisFileType.Sample, dilution: 4d),
        };
        // The LPC has no height in the excluded file, a failed injection left out of the run.
        var fixture = new Fixture(lpcHeights: new[] { 1000d, 0d, 500d }, files: files);
        var run = Run(fixture, Options(peakIdsOnly: true, dilution: true), Header, PcRow, CerRow, LpcRow);

        Assert.AreEqual(0, run.ExitCode, run.Error);
        var pc = run.Normalized.Row(Pc);
        Assert.AreEqual(66.4 * 50 / 100 * 2, pc.Value("s1"), 1e-9);
        Assert.AreEqual(66.4 * 10 / 400 * 4, pc.Value("s2"), 1e-9,
            "paired by position, the third file's values took the second included file's factor");
        Assert.AreEqual(66.4 * 4, run.Normalized.Row(PcStandard).Value("s2"), 1e-9);
        StringAssert.Contains(run.Output, "Dilution factors (multiplied): s1 2, s2 4");

        // The class statistics are those of the files the matrix shows: the excluded file's value
        // was neither diluted nor shown, and with no standard there it was 100 x 60 / 1.
        Assert.AreEqual((66.4 * 50 / 100 * 2 + 66.4 * 10 / 400 * 4) / 2, Number(pc.Stat("Average", "SPF")), 1e-9);
        var cardiolipin = run.Normalized.Row(Cardiolipin);
        Assert.AreEqual(100d * 30 / 1000 * 2, cardiolipin.Value("s1"), 1e-9);
        Assert.AreEqual(100d * 90 / 500 * 4, cardiolipin.Value("s2"), 1e-9);
        Assert.AreEqual((6d + 72d) / 2, Number(cardiolipin.Stat("Average", "SPF")), 1e-9);
        Assert.AreEqual(Math.Sqrt((33d * 33d) * 2), Number(cardiolipin.Stat("Stdev", "SPF")), 1e-9);
        Assert.IsFalse(cardiolipin.Comment.Contains("NOT QUANTIFIED"), cardiolipin.Comment);
        Assert.IsFalse(run.Output.Contains("has no peak height"), run.Output);
        Assert.AreEqual((50d + 10d) / 2, Number(run.Raw.Row(Pc).Stat("Average", "SPF")), 1e-9);
    }

    [TestMethod]
    public void ADilutionFactorThatIsNotPositiveIsRefused() {
        var files = new[] {
            AnalysisFile(0, "s1", "SPF", AnalysisFileType.Sample, dilution: 0d),
            AnalysisFile(1, "s2", "SPF", AnalysisFileType.Sample),
            AnalysisFile(2, "blank", "Blank", AnalysisFileType.Blank),
        };
        var run = Run(new Fixture(files: files), Options(peakIdsOnly: true, dilution: true), Header, PcRow, CerRow, LpcRow);

        Assert.AreEqual(-1, run.ExitCode);
        StringAssert.Contains(run.Error, "s1 (0)");
        Assert.IsFalse(File.Exists(run.NormalizedPath));
    }

    [TestMethod]
    public void WithoutTheNewOptionsATableResolvesAsBefore() {
        // No PeakID column at all: every standard by name, the default since #795.
        var run = Run(new Fixture(), Options(),
            "StandardName\tTargetClass\tConcentration",
            "PC 15:0_18:1(d7)\tPC\t66.4",
            "Cer 18:1;O2/15:0(d7)\tCer-NS\t20",
            "LPC 18:1(d7)\tAny others\t100");

        Assert.AreEqual(0, run.ExitCode, run.Error);
        StringAssert.Contains(run.Output, "PC 15:0_18:1(d7) via annotation, alignment ID 0, concentration 66.4");
        Assert.AreEqual(66.4 * 50 / 100, run.Normalized.Row(Pc).Value("s1"), 1e-9);
        Assert.AreEqual(20d * 10 / 20, run.Normalized.Row(Cer).Value("s1"), 1e-9);
        Assert.AreEqual(100d * 5 / 1000, run.Normalized.Row(Unknown).Value("s1"), 1e-9);
    }

    [TestMethod]
    public void CanonicalClassIsTheSpellingTheNormalizerLooksUp() {
        Assert.AreEqual("Cer-NS", NormalizationProcess.CanonicalClass("Cer_NS"));
        Assert.AreEqual("Cer-NS", NormalizationProcess.CanonicalClass("Cer-NS"));
        Assert.AreEqual("HexCer-NS", NormalizationProcess.CanonicalClass("HexCer_NS"));
        Assert.AreEqual("Cer-AH_d9", NormalizationProcess.CanonicalClass("Cer_AH_d9"));
        Assert.AreEqual("PE-Cer", NormalizationProcess.CanonicalClass("PE_Cer"));
        Assert.AreEqual("PC", NormalizationProcess.CanonicalClass(" PC "));
        Assert.AreEqual("Any others", NormalizationProcess.CanonicalClass("any others"));
        Assert.AreEqual("LipidA", NormalizationProcess.CanonicalClass("LipidA"),
            "a class with no VS2 name is kept as written, not made the Undefined every such peak is looked up as");
        Assert.IsFalse(NormalizationProcess.IsLookedUp("LipidA"));
        Assert.IsTrue(NormalizationProcess.IsLookedUp("Cer-NS"));
        Assert.IsTrue(NormalizationProcess.IsLookedUp("Any others"));
    }

    [TestMethod]
    public void NormalizeHelpListsPeakIdsOnlyAndSaysTheDilutionFactorMultiplies() {
        var root = new RootCommand("test");
        MainProcess.SetNormalizeCommand(root);
        var output = new StringWriter();

        var exitCode = root.Parse(["normalize", "--help"]).Invoke(new InvocationConfiguration { Output = output });

        Assert.AreEqual(0, exitCode);
        var help = output.ToString();
        StringAssert.Contains(help, "--peak-ids-only");
        StringAssert.Contains(help, "Multiply each file's normalized values by its dilution factor");
        StringAssert.Contains(help, "AssignName");
        Assert.IsFalse(help.Contains("Divide"));
        // AssignName renames a peak only with the mismatch option, and both say so.
        var flat = string.Join(" ", help.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
        StringAssert.Contains(flat, "a peak not already carrying that name is renamed only with --allow-mismatched-peak-ids");
        StringAssert.Contains(flat, "needed for AssignName to rename a peak");
        StringAssert.Contains(flat, "leaving the classes no other standard quantifies without a concentration");
    }

    [TestMethod]
    public void ARowTheNormalizerTreatsAsUnknownKeepsItsAnyOthersValueWhenItsLabelsStandardIsUnresolved() {
        // The normalizer quantifies a peak that is not reference matched, and one whose reference
        // gives only an Ontology that is not an MS-DIAL lipid class name, as "Unknown", against
        // Any others, whatever its label. Emptying it by the label threw away that value.
        var fixture = new Fixture();
        var run = Run(fixture, Options(peakIdsOnly: true, allowUnresolved: true),
            Header, PcRow, "Cer 18:1;O2/15:0(d7)\tCer-NS\t20\t", LpcRow);

        Assert.AreEqual(0, run.ExitCode, run.Error);
        StringAssert.StartsWith(run.Normalized.Row(Cer).Comment, "NOT QUANTIFIED: the Cer-NS standard");
        foreach (var (id, height) in new[] { (LowScoreCer, 40d), (OntologyOnlyCer, 60d) }) {
            var row = run.Normalized.Row(id);
            Assert.AreEqual(fixture.Spots[LpcStandard].MasterAlignmentID, fixture.Spots[id].InternalStandardAlignmentID);
            Assert.IsFalse(row.Comment.Contains("NOT QUANTIFIED"), $"row {id}: {row.Comment}");
            Assert.AreEqual(100d * height / 1000, row.Value("s1"), 1e-9, $"row {id}");
        }
    }

    [TestMethod]
    public void WithIntensityAsTheUnitAnUnresolvedAnyOthersEmptiesOnlyTheRowsNoStandardDivided() {
        var fixture = new Fixture();
        var run = Run(fixture, Options(peakIdsOnly: true, allowUnresolved: true, unit: IonAbundanceUnit.Intensity),
            Header, PcRow, CerRow, "LPC 18:1(d7)\tAny others\t100\t");

        Assert.AreEqual(0, run.ExitCode, run.Error);
        Assert.AreEqual(66.4 * 50 / 100, run.Normalized.Row(Pc).Value("s1"), 1e-9,
            "divided by its own standard, whatever the unit is called");
        Assert.AreEqual(20d * 10 / 20, run.Normalized.Row(Cer).Value("s1"), 1e-9);
        var cardiolipin = run.Normalized.Row(Cardiolipin);
        Assert.IsTrue(cardiolipin.Samples.All(value => value.Length == 0));
        StringAssert.Contains(cardiolipin.Comment, "the Any others standard LPC 18:1(d7) did not resolve");
    }

    [TestMethod]
    public void AClassWithNoMsdialNameIsReportedAsQuantifiedAgainstAnyOthersAndIsNotEmptiedForItsStandard() {
        // "LipidA" has no MS-DIAL lipid class name: the normalizer looks a lipid A peak up as
        // "Undefined" and divides it by Any others, whatever standard the table names for it.
        var fixture = new Fixture();
        var resolved = Run(fixture, Options(peakIdsOnly: true),
            Header, PcRow, CerRow, LpcRow, "PC 15:0_18:1(d7)\tLipidA\t66.4\t0");

        Assert.AreEqual(0, resolved.ExitCode, resolved.Error);
        StringAssert.Contains(resolved.Output, "PC 15:0_18:1(d7) via alignment ID 0, concentration 66.4");
        StringAssert.Contains(resolved.Output, $"      covers 1 class(es): PC{Environment.NewLine}");
        StringAssert.Contains(resolved.Output, "WARNING: 1 class(es) in the internal standard table have no MS-DIAL lipid class name");
        StringAssert.Contains(resolved.Output, "LipidA (PC 15:0_18:1(d7))");
        Assert.AreEqual(100d * 70 / 1000, resolved.Normalized.Row(LipidA).Value("s1"), 1e-9);

        var unresolved = Run(new Fixture(), Options(peakIdsOnly: true, allowUnresolved: true),
            Header, PcRow, CerRow, LpcRow, "Lipid A std\tLipidA\t10\t");
        Assert.AreEqual(0, unresolved.ExitCode, unresolved.Error);
        var lipidA = unresolved.Normalized.Row(LipidA);
        Assert.AreEqual(100d * 70 / 1000, lipidA.Value("s1"), 1e-9,
            "its standard was never going to be used, so its absence changes nothing");
        Assert.IsFalse(lipidA.Comment.Contains("NOT QUANTIFIED"), lipidA.Comment);
        StringAssert.Contains(unresolved.Output,
            "WARNING: 1 internal standard(s) did not resolve and are not needed, so no row is emptied for them: "
            + "Lipid A std (for LipidA, under which the normalizer looks no peak up).");
        StringAssert.Contains(unresolved.Output, "Their peaks go to the Any others standard LPC 18:1(d7), not to the one named for them.");
        Assert.IsFalse(unresolved.Output.Contains("carry no concentration"), unresolved.Output);
    }

    [TestMethod]
    public void AClassWithNoMsdialNameSaysWhereItsPeaksGoWhenThereIsNoAnyOthersStandardToTakeThem() {
        var none = Run(new Fixture(), Options(peakIdsOnly: true),
            Header, PcRow, CerRow, "PC 15:0_18:1(d7)\tLipidA\t66.4\t0");
        Assert.AreEqual(0, none.ExitCode, none.Error);
        StringAssert.Contains(none.Output,
            "With no Any others standard in the table, their peaks are exported as raw intensities.");
        StringAssert.Contains(none.Normalized.Row(LipidA).Comment, "Normalized unit: Intensity");

        var unresolved = Run(new Fixture(), Options(peakIdsOnly: true, allowUnresolved: true),
            Header, PcRow, CerRow, "LPC 18:1(d7)\tAny others\t100\t", "PC 15:0_18:1(d7)\tLipidA\t66.4\t0");
        Assert.AreEqual(0, unresolved.ExitCode, unresolved.Error);
        StringAssert.Contains(unresolved.Output,
            "Their peaks would go to the Any others standard LPC 18:1(d7), which did not resolve, so they carry no concentration.");
        Assert.IsTrue(unresolved.Normalized.Row(LipidA).Samples.All(value => value.Length == 0));
    }

    [TestMethod]
    public void APeakAssignedToOneStandardCannotQuantifyAnotherWhateverTheOtherRowsAssignName() {
        var assigned = "PE 15:0_18:1(d7)\tPE\t50\t7\ttrue";
        var other = "PG 15:0_18:1(d7)\tPG\t50\t7\tfalse";
        foreach (var order in new[] { new[] { assigned, other }, new[] { other, assigned } }) {
            var run = Run(new Fixture(), Options(peakIdsOnly: true, allowMismatched: true),
                new[] { Header + "\tAssignName", PcRow + "\t", LpcRow + "\t" }.Concat(order).ToArray());

            Assert.AreEqual(2, run.ExitCode, run.Output);
            StringAssert.Contains(run.Error, "alignment ID 7 (PE 15:0_18:1(d7) and PG 15:0_18:1(d7))");
            Assert.IsFalse(File.Exists(run.NormalizedPath));
        }
    }

    [TestMethod]
    public void NonAsciiFileNamesAreEmptiedInTheColumnsTheExporterWroteForThem() {
        // The exporter writes ASCII, so these names reach the header as "??_01", "??_02" and
        // "????". The rows are still emptied, rather than the matrix deleted.
        var files = new[] {
            AnalysisFile(0, "血漿_01", "SPF", AnalysisFileType.Sample),
            AnalysisFile(1, "血漿_02", "SPF", AnalysisFileType.Sample),
            AnalysisFile(2, "ブランク", "Blank", AnalysisFileType.Blank),
        };
        var fixture = new Fixture(lpcHeights: new[] { 1000d, 500d, 0d }, files: files);
        var run = Run(fixture, Options(peakIdsOnly: true, allowUnresolved: true),
            Header, PcRow, "Cer 18:1;O2/15:0(d7)\tCer-NS\t20\t", LpcRow);

        Assert.AreEqual(0, run.ExitCode, run.Error);
        Assert.IsTrue(File.Exists(run.NormalizedPath));
        Assert.IsTrue(run.Normalized.Row(Cer).Samples.All(value => value.Length == 0));
        var cardiolipin = run.Normalized.Row(Cardiolipin);
        Assert.AreEqual(100d * 30 / 1000, cardiolipin.Value("??_01"), 1e-9);
        Assert.AreEqual(100d * 60 / 500, cardiolipin.Value("??_02"), 1e-9);
        Assert.AreEqual(string.Empty, cardiolipin.Cell("????"));
        Assert.AreEqual(string.Empty, cardiolipin.Stat("Average", "Blank"));
        StringAssert.Contains(cardiolipin.Comment, "NOT QUANTIFIED in 1 file(s) (????)");
        Assert.AreEqual(66.4 * 50 / 100, run.Normalized.Row(Pc).Value("??_01"), 1e-9);
    }

    [TestMethod]
    public void UnderPeakIdsOnlyAPeakIdThatIsNotAnIntegerIsRefusedRatherThanTakenForAMissingOne() {
        var path = Table(Header, PcRow, "Cer 18:1;O2/15:0(d7)\tCer-NS\t20\t2.0", LpcRow);

        var error = Assert.ThrowsException<FormatException>(() => NormalizationProcess.ReadStandards(path, peakIdsOnly: true));
        StringAssert.Contains(error.Message, "Line 3");
        StringAssert.Contains(error.Message, "'2.0'");
        Assert.AreEqual(-1, NormalizationProcess.ReadStandards(path, peakIdsOnly: false)[1].PeakID,
            "without the option such an ID is looked up by name, as before");
    }

    [TestMethod]
    public void AClassThatAnotherRowResolvedIsQuantifiedRatherThanEmptied() {
        var cer = Run(new Fixture(), Options(peakIdsOnly: true, allowUnresolved: true),
            Header, PcRow, "Missing Cer std\tCer_NS\t20\t", CerRow, LpcRow);

        Assert.AreEqual(0, cer.ExitCode, cer.Error);
        StringAssert.Contains(cer.Output, "UNRESOLVED Missing Cer std");
        Assert.AreEqual(20d * 10 / 20, cer.Normalized.Row(Cer).Value("s1"), 1e-9);
        Assert.IsFalse(cer.Normalized.Row(Cer).Comment.Contains("NOT QUANTIFIED"));
        StringAssert.Contains(cer.Output,
            "WARNING: 1 internal standard(s) did not resolve and are not needed, so no row is emptied for them: "
            + "Missing Cer std (for Cer_NS, which Cer 18:1;O2/15:0(d7) quantifies).");
        Assert.IsFalse(cer.Output.Contains("carry no concentration"), cer.Output);

        var anyOthers = Run(new Fixture(), Options(peakIdsOnly: true, allowUnresolved: true),
            Header, PcRow, CerRow, "Missing LPC std\tAny others\t100\t", LpcRow);

        Assert.AreEqual(0, anyOthers.ExitCode, anyOthers.Error);
        Assert.AreEqual(100d * 30 / 1000, anyOthers.Normalized.Row(Cardiolipin).Value("s1"), 1e-9);
        Assert.IsFalse(anyOthers.Normalized.Text.Contains("NOT QUANTIFIED"));
        StringAssert.Contains(anyOthers.Output,
            "Missing LPC std (for Any others, which LPC 18:1(d7) quantifies).");
        Assert.IsFalse(anyOthers.Output.Contains("carry no concentration"), anyOthers.Output);

        // Nothing is lost, so nothing needs the caller's leave.
        var unasked = Run(new Fixture(), Options(peakIdsOnly: true),
            Header, PcRow, "Missing Cer std\tCer_NS\t20\t", CerRow, "Missing LPC std\tAny others\t100\t", LpcRow);
        Assert.AreEqual(0, unasked.ExitCode, unasked.Error);
        StringAssert.Contains(unasked.Output, "WARNING: 2 internal standard(s) did not resolve and are not needed");
    }

    [TestMethod]
    public void ThePeakIdsOnlyOptionReachesTheStandardsTableFromTheCommandLine() {
        var project = Path.Combine(_scratch, "project.mddata");
        File.WriteAllText(project, "not a project");
        var table = Table("StandardName\tTargetClass\tConcentration", "PC 15:0_18:1(d7)\tPC\t66.4");
        var output = Path.Combine(_scratch, "out");

        var flagged = Invoke("normalize", "-i", project, "-s", table, "-o", output, "--peak-ids-only");
        Assert.AreEqual(-1, flagged.ExitCode);
        StringAssert.Contains(flagged.Error, "--peak-ids-only takes each standard from its PeakID");

        // Any other switch in its place leaves the table alone, and the run goes on to the project.
        var other = Invoke("normalize", "-i", project, "-s", table, "-o", output, "--allow-mismatched-peak-ids");
        Assert.AreEqual(-1, other.ExitCode);
        Assert.IsFalse(other.Error.Contains("--peak-ids-only"), other.Error);
        StringAssert.Contains(other.Error, "could not be read as an MS-DIAL data file");
    }

    private static (int ExitCode, string Error) Invoke(params string[] args) {
        var root = new RootCommand("test");
        MainProcess.SetNormalizeCommand(root);
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var stderr = new StringWriter();
        try {
            Console.SetOut(new StringWriter());
            Console.SetError(stderr);
            var exitCode = root.Parse(args).Invoke(new InvocationConfiguration { Error = stderr });
            return (exitCode, stderr.ToString());
        }
        finally {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private NormalizationProcess.NormalizeOptions Options(
        bool peakIdsOnly = false, bool allowUnresolved = false, bool allowMismatched = false, bool dilution = false,
        IonAbundanceUnit unit = IonAbundanceUnit.pmol_per_microL_plasma) {
        return new NormalizationProcess.NormalizeOptions {
            Unit = unit,
            PeakIdsOnly = peakIdsOnly,
            AllowUnresolvedStandards = allowUnresolved,
            AllowMismatchedPeakIds = allowMismatched,
            ApplyDilutionFactor = dilution,
        };
    }

    private static double Number(string cell) => double.Parse(cell, CultureInfo.CurrentCulture);

    private List<NormalizationProcess.StandardRecord> Records(params string[] lines) {
        return NormalizationProcess.ReadStandards(Table(lines));
    }

    private string Table(params string[] lines) {
        var path = Path.Combine(_scratch, $"standards-{Guid.NewGuid():N}.tsv");
        File.WriteAllLines(path, lines);
        return path;
    }

    private RunResult Run(Fixture fixture, NormalizationProcess.NormalizeOptions options, params string[] table) {
        var records = NormalizationProcess.ReadStandards(Table(table), options.PeakIdsOnly);
        var output = Path.Combine(_scratch, $"out-{Guid.NewGuid():N}");
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        int exitCode;
        try {
            Console.SetOut(stdout);
            Console.SetError(stderr);
            exitCode = NormalizationProcess.NormalizeAndExport(
                fixture.IncludedFiles,
                fixture.Spots,
                records,
                fixture.Refer,
                new FacadeMatchResultEvaluator(),
                fixture.Parameter,
                () => fixture.Spots.Select(_ => new MSDecResult()).ToList(),
                output,
                "alignment",
                options);
        }
        finally {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
        return new RunResult(exitCode, stdout.ToString(), stderr.ToString(), output);
    }

    private static AnalysisFileBean AnalysisFile(
        int id, string name, string fileClass, AnalysisFileType type, double dilution = 1d, bool included = true) {
        return new AnalysisFileBean {
            AnalysisFileId = id,
            AnalysisFileName = name,
            AnalysisFileClass = fileClass,
            AnalysisFileType = type,
            AnalysisFileAnalyticalOrder = id + 1,
            AnalysisFileIncluded = included,
            DilutionFactor = dilution,
        };
    }

    private sealed class Fixture
    {
        public Fixture(double[]? lpcHeights = null, AnalysisFileBean[]? files = null) {
            AllFiles = files ?? new[] {
                AnalysisFile(0, "s1", "SPF", AnalysisFileType.Sample),
                AnalysisFile(1, "s2", "SPF", AnalysisFileType.Sample),
                AnalysisFile(2, "blank", "Blank", AnalysisFileType.Blank),
            };
            IncludedFiles = AllFiles.Where(file => file.AnalysisFileIncluded).ToList();
            Parameter = new MsdialLcmsParameter();
            Parameter.FileID_ClassName = AllFiles.ToDictionary(file => file.AnalysisFileId, file => file.AnalysisFileClass);
            Parameter.ClassnameToOrder = AllFiles
                .Select(file => file.AnalysisFileClass).Distinct()
                .Select((name, order) => (name, order))
                .ToDictionary(item => item.name, item => item.order);

            // Name, the CompoundClass and Ontology its reference gives, whether it is reference
            // matched, and its height in each of the three files.
            var peaks = new (string? Name, string? Class, string? Ontology, bool Matched, double[] Heights)[] {
                ("PC 15:0_18:1(d7)", "PC", null, true, new[] { 100d, 200d, 400d }),
                ("PC 34:1", "PC", null, true, new[] { 50d, 50d, 10d }),
                ("Cer 18:1;O2/15:0(d7)", "Cer_NS", null, true, new[] { 20d, 40d, 80d }),
                ("Cer 18:1;O2/16:0", "Cer_NS", null, true, new[] { 10d, 10d, 10d }),
                ("LPC 18:1(d7)", "LPC", null, true, lpcHeights ?? new[] { 1000d, 500d, 250d }),
                ("CL 72:8", "CL", null, true, new[] { 30d, 60d, 90d }),
                (null, null, null, false, new[] { 5d, 5d, 5d }),
                (null, null, null, false, new[] { 300d, 300d, 300d }),
                ("Cer 18:1;O2/18:0", "Cer_NS", null, false, new[] { 40d, 40d, 40d }),
                ("Cer 18:1;O2/20:0", null, "Cer_NS", true, new[] { 60d, 60d, 60d }),
                ("Lipid A test", "LipidA", null, true, new[] { 70d, 70d, 70d }),
            };
            var references = new List<MoleculeMsReference?>();
            Spots = new List<AlignmentSpotProperty>();
            foreach (var (peak, index) in peaks.Select((peak, index) => (peak, index))) {
                var spot = new AlignmentSpotProperty {
                    MasterAlignmentID = index,
                    AlignmentID = index,
                    MassCenter = 500d + index,
                    TimesCenter = new ChromXs(1d + index, ChromXType.RT, ChromXUnit.Min),
                    AlignedPeakProperties = AllFiles.Select((file, position) => new AlignmentChromPeakFeature {
                        FileID = file.AnalysisFileId,
                        FileName = file.AnalysisFileName,
                        PeakHeightTop = peak.Heights[position],
                    }).ToList(),
                };
                if (peak.Name is not null) {
                    spot.Name = peak.Name;
                    // A manual result is reference matched by itself; anything else needs an
                    // evaluator, and the empty one the tests pass matches nothing.
                    spot.MatchResults.AddResult(new MsScanMatchResult {
                        LibraryID = references.Count,
                        Name = peak.Name,
                        AnnotatorID = "LbmDB: test",
                        Source = peak.Matched ? SourceType.Manual | SourceType.GeneratedLipid : SourceType.GeneratedLipid,
                    });
                    references.Add(new MoleculeMsReference { Name = peak.Name, CompoundClass = peak.Class, Ontology = peak.Ontology });
                }
                Spots.Add(spot);
            }
            Refer = new ListRefer(references);
        }

        public IReadOnlyList<AnalysisFileBean> AllFiles { get; }
        public List<AnalysisFileBean> IncludedFiles { get; }
        public List<AlignmentSpotProperty> Spots { get; }
        public ListRefer Refer { get; }
        public MsdialLcmsParameter Parameter { get; }
    }

    private sealed class ListRefer : IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>
    {
        private readonly IReadOnlyList<MoleculeMsReference?> _references;

        public ListRefer(IReadOnlyList<MoleculeMsReference?> references) {
            _references = references;
        }

        public string Key => string.Empty;

        public MoleculeMsReference? Refer(MsScanMatchResult? result) {
            return result is not null && result.LibraryID >= 0 && result.LibraryID < _references.Count
                ? _references[result.LibraryID]
                : null;
        }
    }

    private sealed class RunResult
    {
        public RunResult(int exitCode, string output, string error, string directory) {
            ExitCode = exitCode;
            Output = output;
            Error = error;
            NormalizedPath = Path.Combine(directory, "alignment_NormalizedHeight.txt");
            RawPath = Path.Combine(directory, "alignment_Height.txt");
        }

        public int ExitCode { get; }
        public string Output { get; }
        public string Error { get; }
        public string NormalizedPath { get; }
        public string RawPath { get; }
        public Matrix Normalized => new Matrix(NormalizedPath);
        public Matrix Raw => new Matrix(RawPath);
    }

    /// <summary>An exported matrix: four label rows, the column names, one row per peak.</summary>
    private sealed class Matrix
    {
        private readonly string[] _lines;
        private readonly string[] _labels;
        private readonly string[] _columns;

        public Matrix(string path) {
            Text = File.ReadAllText(path);
            _lines = File.ReadAllLines(path);
            _labels = _lines[3].Split('\t');
            _columns = _lines[4].Split('\t');
        }

        public string Text { get; }

        public MatrixRow Row(int alignmentId) {
            var id = Array.IndexOf(_columns, "Alignment ID");
            var cells = _lines.Skip(5).Select(line => line.Split('\t')).Single(row => row[id] == alignmentId.ToString(CultureInfo.InvariantCulture));
            return new MatrixRow(cells, _columns, _labels);
        }
    }

    private sealed class MatrixRow
    {
        private readonly string[] _cells;
        private readonly string[] _columns;
        private readonly string[] _labels;

        public MatrixRow(string[] cells, string[] columns, string[] labels) {
            _cells = cells;
            _columns = columns;
            _labels = labels;
        }

        public string Name => Cell("Metabolite name");
        public string Comment => Cell("Comment");

        /// <summary>The file columns, which sit between the Comment-side metadata and the class statistics.</summary>
        public IEnumerable<string> Samples {
            get {
                var first = Array.IndexOf(_columns, "Candidates reference matched") + 1;
                var firstStat = Array.FindIndex(_labels, label => label == "Average");
                return _cells.Skip(first).Take(firstStat - first);
            }
        }

        public string Cell(string column) {
            var index = Array.IndexOf(_columns, column);
            Assert.IsTrue(index >= 0, column);
            return index < _cells.Length ? _cells[index] : string.Empty;
        }

        public double Value(string file) => double.Parse(Cell(file), CultureInfo.CurrentCulture);

        public string Stat(string stat, string fileClass) {
            for (var index = 0; index < _columns.Length; index++) {
                if (_labels[index] == stat && _columns[index] == fileClass) {
                    return index < _cells.Length ? _cells[index] : string.Empty;
                }
            }
            Assert.Fail($"{stat} {fileClass}");
            return string.Empty;
        }
    }
}

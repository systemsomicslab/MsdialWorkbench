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
/// The alignment has three files (two SPF samples and a blank) and eight peaks: standards for
/// PC, Cer-NS and "Any others" (an LPC), a PC, a ceramide whose reference spells its class
/// "Cer_NS", a cardiolipin with no standard of its own, a peak with no annotation, and a second
/// unannotated peak that a caller can assign as a standard.
/// </remarks>
[TestClass]
public sealed class NormalizationProcessTests
{
    private const int PcStandard = 0, Pc = 1, CerStandard = 2, Cer = 3, LpcStandard = 4, Cardiolipin = 5, Unknown = 6, Unnamed = 7;

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
        Assert.AreEqual(
            "NOT QUANTIFIED: the Cer-NS standard Cer 18:1;O2/15:0(d7) did not resolve in this alignment",
            cer.Comment);
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
        var fixture = new Fixture(files: files);
        var run = Run(fixture, Options(peakIdsOnly: true, dilution: true), Header, PcRow, CerRow, LpcRow);

        Assert.AreEqual(0, run.ExitCode, run.Error);
        Assert.AreEqual(66.4 * 50 / 100 * 2, run.Normalized.Row(Pc).Value("s1"), 1e-9);
        Assert.AreEqual(66.4 * 10 / 400 * 4, run.Normalized.Row(Pc).Value("s2"), 1e-9,
            "paired by position, the third file's values took the second included file's factor");
        Assert.AreEqual(66.4 * 4, run.Normalized.Row(PcStandard).Value("s2"), 1e-9);
        StringAssert.Contains(run.Output, "Dilution factors (multiplied): s1 2, s2 4");
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
        Assert.AreEqual("LipidA", NormalizationProcess.CanonicalClass("LipidA"), "a class with no VS2 name is kept as written");
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
    }

    private NormalizationProcess.NormalizeOptions Options(
        bool peakIdsOnly = false, bool allowUnresolved = false, bool allowMismatched = false, bool dilution = false) {
        return new NormalizationProcess.NormalizeOptions {
            Unit = IonAbundanceUnit.pmol_per_microL_plasma,
            PeakIdsOnly = peakIdsOnly,
            AllowUnresolvedStandards = allowUnresolved,
            AllowMismatchedPeakIds = allowMismatched,
            ApplyDilutionFactor = dilution,
        };
    }

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

            // Name, the class its reference gives, and its height in each of the three files.
            var peaks = new (string? Name, string? Class, double[] Heights)[] {
                ("PC 15:0_18:1(d7)", "PC", new[] { 100d, 200d, 400d }),
                ("PC 34:1", "PC", new[] { 50d, 50d, 10d }),
                ("Cer 18:1;O2/15:0(d7)", "Cer_NS", new[] { 20d, 40d, 80d }),
                ("Cer 18:1;O2/16:0", "Cer_NS", new[] { 10d, 10d, 10d }),
                ("LPC 18:1(d7)", "LPC", lpcHeights ?? new[] { 1000d, 500d, 250d }),
                ("CL 72:8", "CL", new[] { 30d, 60d, 90d }),
                (null, null, new[] { 5d, 5d, 5d }),
                (null, null, new[] { 300d, 300d, 300d }),
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
                    spot.MatchResults.AddResult(new MsScanMatchResult {
                        LibraryID = references.Count,
                        Name = peak.Name,
                        AnnotatorID = "LbmDB: test",
                        Source = SourceType.Manual | SourceType.GeneratedLipid,
                    });
                    references.Add(new MoleculeMsReference { Name = peak.Name, CompoundClass = peak.Class });
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

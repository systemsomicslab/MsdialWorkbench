#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.DiagnosticRules;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.MsfinderCommon.Tests;

/// <summary>One per-rule fixture spectrum (CommonStandardTests' committed rule fixtures, linked).</summary>
internal sealed class FixtureSpectrum
{
    public FixtureSpectrum(string ruleId, string caseName, string precursorMz, bool positive, string? instrumentType, IReadOnlyList<(string Mz, string Intensity)> peaks) {
        RuleId = ruleId;
        CaseName = caseName;
        PrecursorMz = precursorMz;
        Positive = positive;
        InstrumentType = instrumentType;
        Peaks = peaks;
    }

    public string RuleId { get; }
    public string CaseName { get; }
    public string PrecursorMz { get; }
    public bool Positive { get; }
    public string? InstrumentType { get; }
    public IReadOnlyList<(string Mz, string Intensity)> Peaks { get; }
    public string Label => RuleId + "/" + CaseName;
}

/// <summary>
/// Query files written at test time from the per-rule fixture spectra (OQ03): MSP with realistic fields, quoted peak
/// comments and an MS1 block, and MassBank records with PK$ANNOTATION. SMILES come from the rules' own public
/// examples; FORMULA, INCHIKEY and INCHI are synthetic, well-formed strings made here; nothing is committed.
/// </summary>
internal static class DiagnosticQueryFiles
{
    public const string FixtureDirectory = @"Resources\DiagnosticRuleFixtures\rules";

    /// <summary>Printable ASCII without ':' and '"' (design 6.1 item 5): the reader matches field names by
    /// substring, a quote would end a quoted comment, and a line break would start a new line.</summary>
    public static readonly string GarbageAlphabet = new string(Enumerable.Range(0x20, 0x7F - 0x20).Select(c => (char)c).Where(c => c != ':' && c != '"').ToArray());

    private static readonly Lazy<IReadOnlyList<FixtureSpectrum>> _fixtures = new Lazy<IReadOnlyList<FixtureSpectrum>>(Load);

    private static readonly Lazy<DiagnosticRuleRuntime> _runtime = new Lazy<DiagnosticRuleRuntime>(() => CreateRuntime(new MsfinderDiagnosticRuleSettings { Evaluation = true }));

    /// <summary>Every distinct fixture spectrum (rule id, peaks, precursor, mode, instrument type), in file and case order.</summary>
    public static IReadOnlyList<FixtureSpectrum> Fixtures => _fixtures.Value;

    /// <summary>The runtime MS-FINDER creates with evaluation on and every other setting at its default.</summary>
    public static DiagnosticRuleRuntime Runtime => _runtime.Value;

    public static DiagnosticRuleRuntime CreateRuntime(MsfinderDiagnosticRuleSettings settings) {
        var runtime = DiagnosticRuleRuntime.Create(settings, ProductTolerance.Da(0.01), out var error);
        if (runtime is null) {
            throw new InvalidOperationException(error);
        }
        return runtime;
    }

    private static IReadOnlyList<FixtureSpectrum> Load() {
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FixtureDirectory);
        var list = new List<FixtureSpectrum>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(dir, "*.json").OrderBy(f => f, StringComparer.Ordinal)) {
            var ruleId = Path.GetFileNameWithoutExtension(file);
            var doc = StrictJsonReader.Read(File.ReadAllBytes(file));
            foreach (var c in ((JsonArray)doc["cases"]).Cast<JsonObject>()) {
                var input = (JsonObject)c["input"];
                var peaks = ((JsonArray)input["peaks"]).Cast<JsonArray>().Select(p => (Str(p[0]), Str(p[1]))).ToList();
                var it = input["instrument_type"] is JsonString s ? s.Value : null;
                var f = new FixtureSpectrum(ruleId, Str(c["name"]), Str(input["precursor_mz"]), Str(input["ion_mode"]) == "pos", it, peaks);
                var key = ruleId + "|" + f.PrecursorMz + "|" + f.Positive + "|" + it + "|" + string.Join(";", peaks.Select(p => p.Item1 + " " + p.Item2));
                if (seen.Add(key)) {
                    list.Add(f);
                }
            }
        }
        return list;
    }

    private static string Str(JsonValue v) => ((JsonString)v).Value;

    /// <summary>A public example SMILES of the fixture's rule (alternating positive and negative by index).</summary>
    public static string Smiles(FixtureSpectrum f, int index) {
        var rule = Runtime.RuleSet.Rules.First(r => r.RuleId == f.RuleId);
        var examples = rule.Claim.PositiveExamples.Concat(rule.Claim.NegativeExamples).ToList();
        return examples[index % examples.Count].Smiles;
    }

    public static string SyntheticInchiKey(Random rng) {
        var sb = new StringBuilder();
        for (var i = 0; i < 25; i++) {
            if (i == 14 || i == 24) {
                sb.Append('-');
            }
            sb.Append((char)('A' + rng.Next(26)));
        }
        return sb.ToString();
    }

    public static string Garbage(Random rng, int max = 40) {
        var n = rng.Next(1, max);
        var sb = new StringBuilder();
        for (var i = 0; i < n; i++) {
            sb.Append(GarbageAlphabet[rng.Next(GarbageAlphabet.Length)]);
        }
        return sb.ToString();
    }

    /// <summary>Garbage that starts with a letter (MassBank rows need a second token).</summary>
    public static string GarbageWord(Random rng) => (char)('a' + rng.Next(26)) + Garbage(rng, 20);

    public static string RandomFormula(Random rng) => "C" + rng.Next(1, 60) + "H" + rng.Next(1, 90) + "O" + rng.Next(0, 20) + (rng.Next(2) == 0 ? "N" + rng.Next(1, 5) : "");

    private static string Inv(double x) => x.ToString("0.######", CultureInfo.InvariantCulture);

    /// <summary>
    /// An MSP query. With <paramref name="garbage"/>, every field that is not a declared adapter input, every
    /// quoted peak comment and the MS1 peaks are random; otherwise they are realistic.
    /// <paramref name="peakLines"/> replaces the MS2 peak lines when given.
    /// </summary>
    public static string Msp(FixtureSpectrum f, Random? garbage, IReadOnlyList<string>? peakLines = null, bool quotedComments = true) {
        var rng = garbage ?? new Random(1);
        string G(string clean) => garbage is null ? clean : Garbage(garbage);
        var precursor = double.Parse(f.PrecursorMz, CultureInfo.InvariantCulture);
        var sb = new StringBuilder();
        void L(string s) => sb.Append(s).Append("\r\n");
        L("NAME: " + G("Query " + f.CaseName));
        L("SCANNUMBER: " + G("1"));
        L("RETENTIONTIME: " + G("5.25"));
        L("RETENTIONINDEX: " + G("-1"));
        L("CCS: " + G("-1"));
        L("PRECURSORMZ: " + f.PrecursorMz);
        L("PRECURSORTYPE: " + G(f.Positive ? "[M+H]+" : "[M-H]-"));
        if (f.InstrumentType != null) {
            L("INSTRUMENTTYPE: " + f.InstrumentType);
        }
        L("INSTRUMENT: " + G("Synthetic QTOF"));
        L("Authors: " + G("synthetic"));
        L("License: " + G("CC0"));
        L("SMILES: " + G(Smiles(f, 0)));
        L("INCHI: " + G("InChI=1S/" + RandomFormula(rng) + "/c1-2-3"));
        L("INCHIKEY: " + G(SyntheticInchiKey(rng)));
        L("ONTOLOGY: " + G("Flavonoid glycosides"));
        L("IONMODE: " + (f.Positive ? "Positive" : "Negative"));
        L("COLLISIONENERGY: " + G("35"));
        L("SPECTRUMTYPE: Centroid");
        L("INTENSITY: " + G("1000"));
        L("METABOLITENAME: " + G("Synthetic metabolite"));
        L("FORMULA: " + G(RandomFormula(rng)));
        foreach (var key in new[] { "CarbonCount", "NitrogenCount", "SulfurCount", "OxygenCount", "CarbonNitrogenCount", "CarbonSulfurCount",
                                     "NitrogenSulfurCount", "CarbonNitrogenSulfurCount" }) {
            L(key + ": " + G("0"));
        }
        L("IsMarked: " + G("False"));
        L("COMMENT: " + G("synthetic query from a rule fixture"));
        L("MSTYPE: MS1");
        L("Num Peaks: 3");
        for (var i = 0; i < 3; i++) {
            var mz = garbage is null ? precursor + i * 1.00335 : garbage.NextDouble() * 2000.0 + 1.0;
            var intensity = garbage is null ? 1000.0 / (1 + 4 * i) : Math.Round(garbage.NextDouble() * 10000.0, 1);
            L(Inv(mz) + "\t" + Inv(intensity));
        }
        L("MSTYPE: MS2");
        var lines = peakLines ?? f.Peaks.Select(p => p.Mz + "\t" + p.Intensity + (quotedComments ? " \"" + G(RandomFormula(rng)) + "\"" : "")).ToList();
        L("Num Peaks: " + lines.Count.ToString(CultureInfo.InvariantCulture));
        foreach (var line in lines) {
            L(line);
        }
        L("");
        return sb.ToString();
    }

    /// <summary>
    /// A MassBank record. <paramref name="annotations"/> are PK$ANNOTATION rows (m/z text, comment);
    /// <paramref name="numPeak"/> overrides PK$NUM_PEAK.
    /// </summary>
    public static string MassBank(FixtureSpectrum f, Random? garbage, IReadOnlyList<(string Mz, string Comment)>? annotations, int? numPeak = null) {
        var rng = garbage ?? new Random(2);
        string G(string clean) => garbage is null ? clean : GarbageWord(garbage);
        var sb = new StringBuilder();
        void L(string s) => sb.Append(s).Append('\n');
        L("ACCESSION: MSBNK-Synthetic-000001");
        L("RECORD_TITLE: synthetic query");
        L("CH$NAME: " + G("Synthetic metabolite"));
        L("CH$COMPOUND_CLASS: " + G("Natural Product; Flavonoid"));
        L("CH$FORMULA: " + G(RandomFormula(rng)));
        L("CH$SMILES: " + G(Smiles(f, 0)));
        L("CH$IUPAC: " + G("InChI=1S/" + RandomFormula(rng) + "/c1-2-3"));
        L("CH$LINK: INCHIKEY " + G(SyntheticInchiKey(rng)));
        if (f.InstrumentType != null) {
            L("AC$INSTRUMENT_TYPE: " + f.InstrumentType);
        }
        L("AC$MASS_SPECTROMETRY: ION_MODE " + (f.Positive ? "POSITIVE" : "NEGATIVE"));
        L("MS$FOCUSED_ION: PRECURSOR_M/Z " + f.PrecursorMz);
        L("COMMENT: " + G("synthetic"));
        if (annotations != null && annotations.Count > 0) {
            L("PK$ANNOTATION: m/z tentative_formula");
            foreach (var (mz, comment) in annotations) {
                L("  " + mz + " " + comment);
            }
        }
        L("PK$NUM_PEAK: " + (numPeak ?? f.Peaks.Count).ToString(CultureInfo.InvariantCulture));
        L("PK$PEAK: m/z int. rel.int.");
        foreach (var (mz, intensity) in f.Peaks) {
            L("  " + mz + " " + intensity + " 999");
        }
        L("//");
        return sb.ToString();
    }

    /// <summary>Writes <paramref name="text"/> as ASCII to a new file in <paramref name="dir"/> and reads it with
    /// MS-FINDER's reader.</summary>
    public static RawData WriteAndRead(string dir, string fileName, string text) {
        var path = Path.Combine(dir, fileName);
        File.WriteAllBytes(path, Encoding.ASCII.GetBytes(text));
        return RawDataParcer.RawDataFileReader(path, new AnalysisParamOfMsfinder());
    }

    public static string NewTempDirectory() {
        var dir = Path.Combine(Path.GetTempPath(), "mfdq" + Guid.NewGuid().ToString("N").Substring(0, 12));
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static void DeleteDirectory(string dir) {
        try {
            Directory.Delete(dir, recursive: true);
        }
        catch (IOException) {
        }
        catch (UnauthorizedAccessException) {
        }
    }

    /// <summary>What the invariance assertions compare: the status, the canonical result bytes and input_sha256.</summary>
    public static (string Status, string? Canonical, string? InputSha256) Signature(DiagnosticQueryEvaluation e) =>
        (e.Status, e.Result is null ? null : DiagnosticResultJson.Write(e.Result), e.Result?.InputSha256);

    public static bool AnyFired(DiagnosticRuleResult? r) => r != null && r.Outcomes.Any(o => o.Fired);
}

using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CompMs.Common.Algorithm.Scoring.Tests;

/// <summary>
/// Parity of the C# kernels with the Python kernels of the MS-DIAL spectrum catalog, on its shared
/// <c>similarity-fixtures/</c> set (contract C3), copied unchanged into Resources\SimilarityFixtures.
/// </summary>
/// <remarks>
/// <para>The fixtures are owned by the catalog repository and copied from a commit of it; SOURCE.md
/// names the commit and the sha256 of every file. Until they are copied this test is Inconclusive,
/// never passed.</para>
/// <para>Only files of kind <c>kernel_pairs</c> and <c>prep</c> are scored, and only cases whose
/// <c>implementations</c> include "csharp". <c>kernel_ids</c> is checked by the identity test. A
/// <c>null</c> score means "not comparable" and is skipped (no MS-DIAL kernel has that result);
/// <c>"NaN"</c> expects NaN; <c>matched_peak_count</c> is compared only where it is given.</para>
/// </remarks>
[TestClass]
[DeploymentItem(@"Resources\SimilarityFixtures", @"Resources\SimilarityFixtures")]
public class SimilarityFixtureParityTests
{
    private const string FixtureDirectory = @"Resources\SimilarityFixtures";

    public TestContext TestContext { get; set; } = null!;

    private static string FixturePath() => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FixtureDirectory);

    private static SimilarityFixtureRun RunOrInconclusive() {
        var directory = FixturePath();
        if (!File.Exists(Path.Combine(directory, "manifest.json"))) {
            Assert.Inconclusive("WS2 fixtures not copied");
        }
        return SimilarityFixtureRun.Run(directory);
    }

    [TestMethod]
    public void TheCsharpKernelsReproduceTheSharedKernelFixtures() {
        var run = RunOrInconclusive();
        TestContext.WriteLine(run.Report());
        Assert.AreEqual(0, run.Failures.Count, string.Join(Environment.NewLine, run.Failures.Take(30)));
        Assert.IsTrue(run.Files.Where(f => f.Kind == "kernel_pairs").Sum(f => f.Passed) > 0, "no kernel case was run");
    }

    [TestMethod]
    public void TheIdentitiesRoundTripThroughTheSharedIdentityFixtures() {
        var run = RunOrInconclusive();
        TestContext.WriteLine(run.Report());
        Assert.AreEqual(0, run.IdentityFailures.Count, string.Join(Environment.NewLine, run.IdentityFailures.Take(30)));
    }

    // ---- Self-tests of the fixture reader, on a small fixture set written here in WS2's format. ----

    private static string WriteSyntheticFixtures(string directory, Action<JObject>? tamper = null, bool corruptSource = false) {
        Directory.CreateDirectory(directory);
        var kernels = new JObject {
            ["format"] = "msdial-similarity-fixtures", ["format_version"] = 1, ["kind"] = "kernel_pairs",
            ["cases"] = new JArray(
                new JObject {
                    ["id"] = "moddot-single-shared-fragment",
                    ["implementations"] = new JArray("python", "csharp"),
                    ["a"] = new JObject { ["precursor_mz"] = 250.0, ["peaks"] = new JArray(new JArray(100.0, 3.0), new JArray(150.0, 4.0)) },
                    ["b"] = new JObject { ["precursor_mz"] = 264.0, ["peaks"] = new JArray(new JArray(100.0, 3.0), new JArray(180.0, 12.0)) },
                    ["expected"] = new JArray(
                        new JObject {
                            ["kernel_id"] = "msdial.moddot.legacy?prep=none&tol=0.025", ["score"] = 1.0, ["matched_peak_count"] = 1,
                            ["abs_tol"] = 1e-9, ["swap_invariant"] = true,
                            ["msdial_pairs"] = new JObject { ["e"] = "a", ["pairs"] = new JArray { new JArray(0, 0, "product") } },
                        },
                        new JObject {
                            ["kernel_id"] = "gnps.modified_cosine?assign=greedy&ipow=1&mzpow=0&prep=none&tol=0.025",
                            ["score"] = 9.0 / (5.0 * Math.Sqrt(153.0)), ["matched_peak_count"] = 1, ["abs_tol"] = 1e-9, ["swap_invariant"] = true,
                        },
                        new JObject {
                            ["kernel_id"] = "gnps.modified_cosine?assign=optimal&ipow=1&mzpow=0&prep=none&tol=0.025",
                            ["score"] = 9.0 / (5.0 * Math.Sqrt(153.0)), ["matched_peak_count"] = null, ["abs_tol"] = 1e-9,
                        }),
                },
                new JObject {
                    ["id"] = "precwin-python-only",
                    ["implementations"] = new JArray("python"),
                    ["a"] = new JObject { ["precursor_mz"] = 250.0, ["peaks"] = new JArray() },
                    ["b"] = new JObject { ["precursor_mz"] = 250.0, ["peaks"] = new JArray() },
                    ["expected"] = new JArray(),
                },
                new JObject {
                    ["id"] = "moddot-nan",
                    ["implementations"] = new JArray("python", "csharp"),
                    ["a"] = new JObject { ["precursor_mz"] = 250.0, ["peaks"] = new JArray { new JArray(100.0, 0.0) } },
                    ["b"] = new JObject { ["precursor_mz"] = 250.0, ["peaks"] = new JArray { new JArray(100.0, 0.0) } },
                    ["expected"] = new JArray(
                        new JObject { ["kernel_id"] = "msdial.moddot.legacy?prep=none&tol=0.025", ["score"] = "NaN", ["matched_peak_count"] = 1, ["abs_tol"] = 1e-9 },
                        new JObject { ["kernel_id"] = "entropy.li2021?bin=0.05&prep=none&weighting=none", ["score"] = null, ["abs_tol"] = 1e-9 }),
                }),
        };
        tamper?.Invoke(kernels);
        var prep = new JObject {
            ["format"] = "msdial-similarity-fixtures", ["format_version"] = 1, ["kind"] = "prep",
            ["cases"] = new JArray(new JObject {
                ["id"] = "prep-frame-sum", ["implementations"] = new JArray("python", "csharp"),
                ["input"] = new JObject { ["precursor_mz"] = 300.0, ["peaks"] = new JArray(new JArray(100.2, 10.0), new JArray(100.6, 30.0), new JArray(150.0, 160.0)) },
                ["prep_id"] = "prep=mn&prep.above=0.05&prep.abs=0&prep.frame=1&prep.maxmz=10000&prep.minmz=0&prep.pow=0.5&prep.precwin=0&prep.rel=1&prep.scale=100&prep.topk=0&prep.topkwin=50",
                ["expected_peaks"] = new JArray(new JArray(100.6, 50.0), new JArray(150.0, 100.0)),
                ["abs_tol"] = 1e-9,
            }),
        };
        var ids = new JObject {
            ["format"] = "msdial-similarity-fixtures", ["format_version"] = 1, ["kind"] = "kernel_ids",
            ["cases"] = new JArray(
                new JObject { ["id"] = "alias", ["input"] = "msdial.bonanza.v1", ["expected"] = "msdial.bonanza.v1?prep=mn&prep.above=0.05&prep.abs=0&prep.frame=1&prep.maxmz=10000&prep.minmz=0&prep.pow=0.5&prep.precwin=0&prep.rel=1&prep.scale=100&prep.topk=0&prep.topkwin=50&tol=0.025" },
                new JObject { ["id"] = "zeros", ["input"] = "msdial.moddot.legacy?prep=none&tol=0.0250", ["expected"] = "msdial.moddot.legacy?prep=none&tol=0.025" },
                new JObject { ["id"] = "missing", ["input"] = "msdial.moddot.legacy?prep=none", ["expected"] = "error" },
                new JObject { ["id"] = "entropy", ["input"] = "entropy.li2021", ["expected"] = "entropy.li2021?bin=0.05&prep=none&weighting=none" }),
        };
        var files = new (string Name, JObject Content)[] { ("kernels.json", kernels), ("prep.json", prep), ("kernel-ids.json", ids) };
        var manifest = new JArray();
        var source = new StringBuilder("# SOURCE\n\nCommit: synthetic\n\n| file | sha256 |\n|---|---|\n");
        foreach (var (name, content) in files) {
            var bytes = Encoding.UTF8.GetBytes(content.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            File.WriteAllBytes(Path.Combine(directory, name), bytes);
            var sha = SimilarityFixtureRun.Sha256(bytes);
            manifest.Add(new JObject { ["path"] = name, ["sha256"] = sha, ["cases"] = ((JArray)content["cases"]!).Count });
            source.Append($"| {name} | {(corruptSource && name == "prep.json" ? new string('0', 64) : sha)} |\n");
        }
        File.WriteAllText(Path.Combine(directory, "manifest.json"), new JObject { ["format"] = "msdial-similarity-fixtures", ["format_version"] = 1, ["files"] = manifest }.ToString());
        File.WriteAllText(Path.Combine(directory, "SOURCE.md"), source.ToString());
        return directory;
    }

    private static string Scratch() => Path.Combine(Path.GetTempPath(), $"msdial-fixtures-{Guid.NewGuid():N}");

    [TestMethod]
    public void TheFixtureReaderRunsOnlyTheCsharpCasesAndCountsTheRest() {
        var directory = WriteSyntheticFixtures(Scratch());
        try {
            var run = SimilarityFixtureRun.Run(directory);

            Assert.AreEqual(0, run.Failures.Count, string.Join(Environment.NewLine, run.Failures));
            Assert.AreEqual(0, run.IdentityFailures.Count, string.Join(Environment.NewLine, run.IdentityFailures));
            var kernels = run.Files.Single(f => f.Path == "kernels.json");
            Assert.AreEqual(3, kernels.Passed, run.Report());
            Assert.AreEqual(1, kernels.SkippedNotCsharp);
            Assert.AreEqual(2, kernels.SkippedUnsupported, "assign=optimal and entropy");
            Assert.AreEqual(1, run.Files.Single(f => f.Path == "prep.json").Passed);
            var ids = run.Files.Single(f => f.Path == "kernel-ids.json");
            Assert.AreEqual(3, ids.Passed, run.Report());
            Assert.AreEqual(1, ids.SkippedUnsupported, "entropy");
        }
        finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void AWrongExpectedScoreIsAFailure() {
        var directory = WriteSyntheticFixtures(Scratch(), kernels => kernels["cases"]![0]!["expected"]![0]!["score"] = 0.9);
        try {
            var run = SimilarityFixtureRun.Run(directory);

            Assert.AreEqual(1, run.Failures.Count(f => f.Contains("moddot-single-shared-fragment")), string.Join(Environment.NewLine, run.Failures));
        }
        finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void AFileWhoseShaDiffersFromSourceMdIsAFailure() {
        var directory = WriteSyntheticFixtures(Scratch(), corruptSource: true);
        try {
            var run = SimilarityFixtureRun.Run(directory);

            Assert.IsTrue(run.Failures.Any(f => f.Contains("prep.json") && f.Contains("SOURCE.md")), string.Join(Environment.NewLine, run.Failures));
        }
        finally {
            Directory.Delete(directory, recursive: true);
        }
    }
}

/// <summary>One run over a fixture directory: per-file counts and every failure.</summary>
internal sealed class SimilarityFixtureRun
{
    internal sealed class FileCounts
    {
        public string Path { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public int Passed { get; set; }
        public int SkippedNotCsharp { get; set; }
        public int SkippedUnsupported { get; set; }
        public int SkippedNullScore { get; set; }
        public int Failed { get; set; }
    }

    public List<FileCounts> Files { get; } = new List<FileCounts>();
    public List<string> Failures { get; } = new List<string>();
    public List<string> IdentityFailures { get; } = new List<string>();

    public static string Sha256(byte[] bytes) {
        using (var sha = SHA256.Create()) {
            return string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }

    public string Report() {
        return string.Join(Environment.NewLine, Files.Select(f =>
            $"{f.Path} ({f.Kind}): run {f.Passed + f.Failed}, passed {f.Passed}, failed {f.Failed}, skipped: not csharp {f.SkippedNotCsharp}, unsupported id {f.SkippedUnsupported}, null score {f.SkippedNullScore}"));
    }

    public static SimilarityFixtureRun Run(string directory) {
        var run = new SimilarityFixtureRun();
        var manifest = JObject.Parse(File.ReadAllText(Path.Combine(directory, "manifest.json")));
        var sourcePath = Path.Combine(directory, "SOURCE.md");
        var sourceLines = File.Exists(sourcePath) ? File.ReadAllLines(sourcePath) : Array.Empty<string>();
        if (sourceLines.Length == 0) {
            run.Failures.Add("SOURCE.md is missing: it must name the catalog commit and every file's sha256.");
        }

        foreach (var entry in manifest["files"]!) {
            var path = (string)entry["path"]!;
            var bytes = File.ReadAllBytes(Path.Combine(directory, path));
            var sha = Sha256(bytes);
            if (!string.Equals(sha, (string)entry["sha256"], StringComparison.OrdinalIgnoreCase)) {
                run.Failures.Add($"{path}: sha256 {sha} differs from manifest.json");
            }
            var recorded = sourceLines.Where(line => line.Contains(path))
                .SelectMany(line => Regex.Matches(line, "[0-9a-fA-F]{64}").Cast<Match>().Select(m => m.Value.ToLowerInvariant()))
                .ToList();
            if (!recorded.Contains(sha)) {
                run.Failures.Add($"{path}: sha256 {sha} is not the one SOURCE.md records");
            }

            var document = JObject.Parse(Encoding.UTF8.GetString(bytes));
            var counts = new FileCounts { Path = path, Kind = (string?)document["kind"] ?? string.Empty };
            run.Files.Add(counts);
            switch (counts.Kind) {
                case "kernel_pairs":
                    foreach (var testCase in document["cases"]!) run.KernelCase(counts, testCase);
                    break;
                case "prep":
                    foreach (var testCase in document["cases"]!) run.PrepCase(counts, testCase);
                    break;
                case "kernel_ids":
                    foreach (var testCase in document["cases"]!) run.IdentityCase(counts, testCase);
                    break;
            }
        }
        return run;
    }

    private static bool IsForCsharp(JToken testCase) {
        return testCase["implementations"] is JArray implementations && implementations.Any(i => (string?)i == "csharp");
    }

    private static MSScanProperty Spectrum(JToken spectrum) {
        var scan = new MSScanProperty(0, (double)spectrum["precursor_mz"]!, new RetentionTime(0d), IonMode.Positive);
        foreach (var peak in spectrum["peaks"]!) {
            scan.AddPeak((double)peak[0]!, (double)peak[1]!);
        }
        return scan;
    }

    private static MSScanProperty Preprocessed(IMsmsSimilarityKernel kernel, MSScanProperty raw) {
        var scan = new MSScanProperty(0, raw.PrecursorMz, new RetentionTime(0d), IonMode.Positive) {
            Spectrum = kernel.Preprocess(raw.Spectrum, raw.PrecursorMz),
        };
        return scan;
    }

    private void KernelCase(FileCounts counts, JToken testCase) {
        var id = (string?)testCase["id"];
        if (!IsForCsharp(testCase)) {
            counts.SkippedNotCsharp++;
            return;
        }
        var a = Spectrum(testCase["a"]!);
        var b = Spectrum(testCase["b"]!);
        foreach (var expected in testCase["expected"]!) {
            var kernelId = (string)expected["kernel_id"]!;
            IMsmsSimilarityKernel kernel;
            try {
                kernel = MsmsSimilarityKernelFactory.FromIdentity(kernelId);
            }
            catch (NotSupportedException) {
                counts.SkippedUnsupported++;
                continue;
            }
            catch (FormatException ex) {
                Fail(counts, $"{id} {kernelId}: the identity does not parse: {ex.Message}");
                continue;
            }
            if (expected["score"] is null || expected["score"]!.Type == JTokenType.Null) {
                counts.SkippedNullScore++;
                continue;
            }
            var tolerance = expected["abs_tol"] is JToken t && t.Type != JTokenType.Null ? (double)t : 1e-9;
            var pa = Preprocessed(kernel, a);
            var pb = Preprocessed(kernel, b);
            var errors = new List<string>();
            Check(errors, "", kernel.Score(pa, pb), expected, tolerance);
            if ((bool?)expected["swap_invariant"] == true) {
                Check(errors, " swapped", kernel.Score(pb, pa), expected, tolerance);
            }
            if (expected["msdial_pairs"] is JObject pairs) {
                CheckPairs(errors, kernel, pa, pb, pairs);
            }
            if (errors.Count == 0) {
                counts.Passed++;
            }
            else {
                Fail(counts, $"{id} {kernelId}: {string.Join("; ", errors)}");
            }
        }
    }

    private static void Check(List<string> errors, string label, SimilarityScore actual, JToken expected, double tolerance) {
        var score = expected["score"]!;
        if (score.Type == JTokenType.String) {
            if ((string)score! != "NaN") {
                errors.Add($"score{label}: unknown expected value '{score}'");
            }
            else if (!double.IsNaN(actual.Score)) {
                errors.Add($"score{label}: expected NaN, actual {actual.Score:R}");
            }
        }
        else if (!(Math.Abs(actual.Score - (double)score) <= tolerance)) {
            errors.Add($"score{label}: expected {(double)score:R}, actual {actual.Score:R}");
        }
        var matched = expected["matched_peak_count"];
        if (matched is not null && matched.Type != JTokenType.Null && (int)matched != actual.Matched) {
            errors.Add($"matched{label}: expected {(int)matched}, actual {actual.Matched}");
        }
    }

    private static void CheckPairs(List<string> errors, IMsmsSimilarityKernel kernel, MSScanProperty a, MSScanProperty b, JObject pairs) {
        var tolerance = ((LegacyKernelBase)kernel).Tolerance;
        // The smaller precursor takes the e role; on equal precursors the second spectrum does.
        var eIsA = a.PrecursorMz < b.PrecursorMz;
        if ((string?)pairs["e"] != (eIsA ? "a" : "b")) {
            errors.Add($"msdial_pairs: expected e = {pairs["e"]}, MS-DIAL takes {(eIsA ? "a" : "b")}");
            return;
        }
        var (e, r) = eIsA ? (a, b) : (b, a);
        var match = MsScanMatching.MatchLegacy(e.Spectrum, e.PrecursorMz, r.Spectrum, r.PrecursorMz, tolerance, MassToleranceType.Da);
        var actual = match.Pairs.Select(p => $"{p.RIndex},{p.EIndex},{(p.IsProductIonMatched ? "product" : "neutral_loss")}").ToList();
        var expected = pairs["pairs"]!.Select(p => $"{(int)p[0]!},{(int)p[1]!},{(string)p[2]!}").ToList();
        if (!actual.SequenceEqual(expected)) {
            errors.Add($"msdial_pairs: expected [{string.Join(" ", expected)}], actual [{string.Join(" ", actual)}]");
        }
    }

    private void PrepCase(FileCounts counts, JToken testCase) {
        var id = (string?)testCase["id"];
        if (!IsForCsharp(testCase)) {
            counts.SkippedNotCsharp++;
            return;
        }
        var prepId = (string)testCase["prep_id"]!;
        MnPreprocessing preprocessing;
        try {
            // The kernel part is irrelevant; it lets the shared parser read and check the prep keys.
            var parsed = KernelIdentity.Parse("msdial.moddot.legacy?" + prepId + "&tol=0.025");
            if (parsed.Text("prep") == "mn" && parsed.Number("prep.topk") != 0d) {
                counts.SkippedUnsupported++;
                return;
            }
            preprocessing = parsed.Preprocessing() ?? MnPreprocessing.None;
        }
        catch (FormatException ex) {
            Fail(counts, $"{id} {prepId}: does not parse: {ex.Message}");
            return;
        }
        var input = Spectrum(testCase["input"]!);
        var actual = preprocessing.Apply(input.Spectrum, input.PrecursorMz);
        var expected = testCase["expected_peaks"]!.Select(p => ((double)p[0]!, (double)p[1]!)).ToList();
        var tolerance = testCase["abs_tol"] is JToken t && t.Type != JTokenType.Null ? (double)t : 1e-9;
        var ok = actual.Count == expected.Count
            && actual.Zip(expected, (x, y) => Math.Abs(x.Mass - y.Item1) <= tolerance && Math.Abs(x.Intensity - y.Item2) <= tolerance).All(v => v);
        if (ok) {
            counts.Passed++;
        }
        else {
            Fail(counts, $"{id}: expected [{string.Join(" ", expected.Select(p => $"{p.Item1:R}:{p.Item2:R}"))}], actual [{string.Join(" ", actual.Select(p => $"{p.Mass:R}:{p.Intensity:R}"))}]");
        }
    }

    private void IdentityCase(FileCounts counts, JToken testCase) {
        var input = (string?)testCase["input"] ?? string.Empty;
        var expected = (string?)(testCase["expected"] ?? testCase["canonical"] ?? testCase["output"]) ?? string.Empty;
        if (expected == "error") {
            try {
                KernelIdentity.Parse(input);
                IdentityFail(counts, $"{testCase["id"]}: '{input}' parsed, but it is an error");
            }
            catch (FormatException) {
                counts.Passed++;
            }
            return;
        }
        var question = expected.IndexOf('?');
        var baseName = question < 0 ? expected : expected.Substring(0, question);
        if (!KernelIdentity.IsKnownBase(baseName)) {
            counts.SkippedUnsupported++;
            return;
        }
        try {
            var canonical = KernelIdentity.Parse(input).ToString();
            if (canonical == expected) {
                counts.Passed++;
            }
            else {
                IdentityFail(counts, $"{testCase["id"]}: '{input}' → '{canonical}', expected '{expected}'");
            }
        }
        catch (FormatException ex) {
            IdentityFail(counts, $"{testCase["id"]}: '{input}' does not parse: {ex.Message}");
        }
    }

    private void Fail(FileCounts counts, string message) {
        counts.Failed++;
        Failures.Add($"{counts.Path}: {message}");
    }

    private void IdentityFail(FileCounts counts, string message) {
        counts.Failed++;
        IdentityFailures.Add($"{counts.Path}: {message}");
    }
}

#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.EngineHarness;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.EvaluatorHarness;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.InlineRuleSet;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// A mixed inline set and a seeded spectrum generator for the determinism, culture and never-throws tests: the basic
/// rule with a veto, the three-scaffold series, an msdial rule with any and k_of nodes and an upgrade, a positive rule,
/// the caffeoyl hexose pair with its declaration, and a rule vetoed by the precursor ion itself.
/// </summary>
internal static class MixedCases
{
    public const string Msdial = "flavonoid.msdial.neg", Pos = "flavonoid.o_glycoside.pos", Caffeoyl = "hydroxycinnamoyl.caffeoyl_hexose.neg",
                        PrecursorVeto = "flavonoid.precursor_veto.neg";

    public static InlineRuleSet Set() {
        var s = SeriesTests.SeriesSet().AddBasic(RuleId, withVeto: true).AddBasic(Pos);
        var msdialTerms = Terms(Y0, X, Term("Z", "product_ion", "required", Formula("C8H5O4"), "5.0"),
                                Term("c0", "product_ion", "counted", Formula("C9H5O4"), "2.0"), Term("c1", "product_ion", "counted", Formula("C10H9O4"), "2.0"),
                                Term("U", "product_ion", "upgrade", Formula("C5H3O3")));
        s.AddRule(Msdial, msdialTerms, AllNode(TermRef("Y0"), AnyNode(TermRef("X"), TermRef("Z")), KOf("1", TermRef("c0"), TermRef("c1"))),
                  CoreHex, "[\"[M-H]-\"]", "Y0").SetRule(Msdial, "convention", "\"msdial\"").SetRule(Msdial, "product_tolerance", "\"msfinder\"");
        s.AddRule(Caffeoyl, Terms(Term("loss_hex", "loss_from_precursor", "counted", Formula("C6H10O5")),
                                  Term("caffeate", "product_ion", "counted", Formula("C9H7O4")), Term("decarb", "product_ion", "counted", Formula("C8H7O2"))),
                  KOf("2", TermRef("loss_hex"), TermRef("caffeate"), TermRef("decarb")),
                  InlineRuleSet.Composition("C9H8O4", null, "{\"Hex\":[1,1]}", "{}"), "[\"[M-H]-\"]", "caffeate")
         .SetRule(Caffeoyl, "complementary", "[[\"loss_hex\",\"caffeate\"]]");
        // A veto window on the [M-H]- precursor ion itself: I_raw sees the precursor region, which the p2 base excludes.
        var precursorVeto = Term("p", "window", "veto_present", Window("C21H19O10", "ion", "0.02"));
        s.AddRule(PrecursorVeto, Terms(Y0, X, precursorVeto), AllOf(new[] { "Y0", "X" }, new[] { "p" }), CoreHex, "[\"[M-H]-\"]", "Y0")
         .SetRule(PrecursorVeto, "claim/claim_term_ids", "[\"Y0\",\"X\"]");
        return s;
    }

    /// <summary>The negative precursors the set knows, and the ions it looks for.</summary>
    public static readonly double[] Precursors =
    {
        PrecursorNeg, TestMass.Ion(TestMass.Of("C15H18O9") - TestMass.Of("H"), -1), TestMass.Ion(TestMass.Of("C27H30O15") - TestMass.Of("H"), -1),
        TestMass.Ion(TestMass.Of("C21H20O11") - TestMass.Of("H"), -1), TestMass.Ion(TestMass.Of("C21H20O10") + TestMass.Of("H"), 1),
    };

    public static readonly double[] Ions = new[] { "C15H9O5", "C7H3O4", "C9H7O4", "C8H5O4", "C9H5O4", "C10H9O4", "C5H3O3", "C8H7O2", "C15H9O7", "C7H5O4", "C8H5O4" }
        .Select(IonNeg).Concat(new[] { TestMass.Ion(TestMass.Of("C15H11O5"), 1) }).ToArray();

    private static readonly string?[] Instruments = { "QTOF", "Q Exactive", "LTQ-Orbitrap", "ion trap", null, "", " \t", "tof" };

    /// <summary>
    /// A tie-free spectrum near a known precursor: peaks at known ions (within +-0.004 Da) and random m/z, all m/z and
    /// intensities distinct, intensities from 1 to 10^4.
    /// </summary>
    public static DiagnosticSpectrum TieFree(Random random) {
        var precursor = Precursors[random.Next(Precursors.Length)] + (random.NextDouble() - 0.5) * 0.004;
        var mode = precursor > 432.0 && precursor < 434.0 ? DiagnosticIonMode.Positive : DiagnosticIonMode.Negative;
        var peaks = new List<DiagnosticPeak>();
        var n = random.Next(1, 25);
        for (var i = 0; i < n; i++) {
            var mz = random.Next(3) == 0 ? 50.0 + random.NextDouble() * 600.0 : Ions[random.Next(Ions.Length)] + (random.NextDouble() - 0.5) * 0.008;
            peaks.Add(new DiagnosticPeak(mz, 1.0 + random.NextDouble() * 9999.0));
        }
        // Printable instrument types only, so the strict reader can read the results back.
        return new DiagnosticSpectrum(peaks, precursor, mode, Instruments[random.Next(6)]!);
    }

    private static double Special(Random random, double usual) {
        switch (random.Next(14)) {
            case 0: return double.NaN;
            case 1: return double.PositiveInfinity;
            case 2: return double.NegativeInfinity;
            case 3: return 0.0;
            case 4: return -0.0;
            case 5: return -random.NextDouble() * 100.0;
            default: return usual;
        }
    }

    /// <summary>
    /// Any spectrum: NaN, infinite, zero, negative, huge and tiny precursors; no or the wrong ion mode; empty peak
    /// lists; non-finite, zero and negative m/z and intensities; duplicate peaks. Usable intensities are between 10^-3
    /// and 10^9, except in one spectrum in 20, which spans the whole double range: double.Epsilon away from the
    /// precursor and 10^300 near it, plus a 10^300 peak on the precursor, so a precursor-region rel I / base would
    /// overflow without saturation.
    /// </summary>
    public static DiagnosticSpectrum Any(Random random) {
        double precursor;
        switch (random.Next(12)) {
            case 0: precursor = 1e300; break;
            case 1: precursor = 1e-300; break;
            case 2: precursor = double.MaxValue; break;
            case 3: precursor = double.Epsilon; break;
            default: precursor = Special(random, Precursors[random.Next(Precursors.Length)] + (random.NextDouble() - 0.5) * 0.03); break;
        }
        var modeRoll = random.Next(6);
        DiagnosticIonMode? mode = modeRoll == 0 ? (DiagnosticIonMode?)null : modeRoll == 1 ? DiagnosticIonMode.Positive : DiagnosticIonMode.Negative;
        var n = random.Next(8) == 0 ? 0 : random.Next(1, 40);
        var extreme = random.Next(20) == 0;
        var peaks = new List<DiagnosticPeak>();
        for (var i = 0; i < n; i++) {
            var kind = random.Next(4) == 0 ? 0 : random.Next(10) == 0 && !double.IsNaN(precursor) ? 1 : 2;
            var mz = kind == 0 ? random.NextDouble() * 1500.0 : kind == 1 ? precursor + (random.NextDouble() - 0.5) * 2.2
                   : Ions[random.Next(Ions.Length)] + (random.NextDouble() - 0.5) * 0.02;
            var intensity = random.Next(10) == 0 ? 0.0 : extreme ? (kind == 1 ? 1e300 : double.Epsilon) : Math.Pow(10.0, -3.0 + random.NextDouble() * 12.0);
            peaks.Add(new DiagnosticPeak(Special(random, mz), Special(random, intensity)));
            if (random.Next(15) == 0) {
                peaks.Add(peaks[peaks.Count - 1]);
            }
        }
        if (extreme) {
            peaks.Add(new DiagnosticPeak(precursor, 1e300));
        }
        var instrument = random.Next(10) == 0 ? "Q\nTOF \u00e9\U0001F600" : Instruments[random.Next(Instruments.Length)];
        return new DiagnosticSpectrum(peaks, precursor, mode, instrument!);
    }
}

/// <summary>
/// Determinism (design 6.3): three repeats give identical bytes; seeded peak permutations give identical results once
/// peak_index is mapped (OQ19: tie-free spectra; input_sha256 hashes the input order and is left out); Parallel.ForEach
/// with 8 workers equals the sequential run; files enumerated in reverse order give the same sha256 and the same result
/// bytes (on the mixed inline set: the embedded set has no rules yet, and EmbeddedRuleSetTests checks its sha256);
/// EvaluatorNeverThrows over 10^4 seeded spectra, where every double but precursor_mz is finite.
/// </summary>
[TestClass]
public class DeterminismTests
{
    private static DiagnosticRuleEvaluator Mixed(bool verbose = true) =>
        new DiagnosticRuleEvaluator(MixedCases.Set().Load(), new DiagnosticEvaluationOptions { Filter = RuleFilter.All, Verbose = verbose });

    [TestMethod]
    public void ThreeRepeatsGiveIdenticalBytes() {
        var random = new Random(20261118);
        var spectra = Enumerable.Range(0, 300).Select(_ => MixedCases.TieFree(random)).ToList();
        var runs = Enumerable.Range(0, 3).Select(_ => {
            var evaluator = Mixed();
            return string.Join("\n", spectra.Select(s => ResultText(evaluator.Evaluate(s))));
        }).ToList();
        Assert.AreEqual(runs[0], runs[1]);
        Assert.AreEqual(runs[0], runs[2]);
        Assert.IsTrue(runs[0].Contains("\"fired\":true"), "some rule fires");
        Assert.IsTrue(runs[0].Contains("\"veto_blocked\":true"), "some rule is veto-blocked");
    }

    /// <summary>The result JSON with every peak_index mapped through <paramref name="map"/> and input_sha256 removed.</summary>
    private static string Normalise(DiagnosticRuleResult result, int[] map) {
        var text = Regex.Replace(ResultText(result), "\"input_sha256\":\"[0-9a-f]{64}\",", "");
        return Regex.Replace(text, "\"peak_index\":([0-9]+)",
                             m => "\"peak_index\":" + map[int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)].ToString(CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void PeakPermutationsGiveTheSameResultAfterMappingPeakIndex() {
        var evaluator = Mixed();
        var random = new Random(20261119);
        var fired = 0;
        for (var n = 0; n < 300; n++) {
            var spectrum = MixedCases.TieFree(random);
            var identity = Enumerable.Range(0, spectrum.Peaks.Count).ToArray();
            var expected = Normalise(evaluator.Evaluate(spectrum), identity);
            fired += expected.Contains("\"fired\":true") ? 1 : 0;
            for (var k = 0; k < 3; k++) {
                // perm[j] = the original index of the peak at position j.
                var perm = identity.OrderBy(_ => random.Next()).ToArray();
                var permuted = new DiagnosticSpectrum(perm.Select(j => spectrum.Peaks[j]).ToList(), spectrum.PrecursorMz, spectrum.IonMode, spectrum.InstrumentType);
                var actual = Normalise(evaluator.Evaluate(permuted), perm);
                Assert.AreEqual(expected, actual, "spectrum " + n.ToString(CultureInfo.InvariantCulture));
            }
        }
        Assert.IsTrue(fired > 30, fired.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>A file map that enumerates its keys in the order given, whatever a dictionary would do.</summary>
    private sealed class OrderedFiles : IReadOnlyDictionary<string, byte[]>
    {
        private readonly Dictionary<string, byte[]> _files;
        private readonly IReadOnlyList<string> _order;

        public OrderedFiles(Dictionary<string, byte[]> files, IEnumerable<string> order) {
            _files = files;
            _order = order.ToList();
            Assert.AreEqual(_files.Count, _order.Count);
        }

        public byte[] this[string key] => _files[key];
        public IEnumerable<string> Keys => _order;
        public IEnumerable<byte[]> Values => _order.Select(k => _files[k]);
        public int Count => _order.Count;
        public bool ContainsKey(string key) => _files.ContainsKey(key);
        public bool TryGetValue(string key, out byte[] value) => _files.TryGetValue(key, out value);
        public IEnumerator<KeyValuePair<string, byte[]>> GetEnumerator() =>
            _order.Select(k => new KeyValuePair<string, byte[]>(k, _files[k])).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [TestMethod]
    public void FilesInReverseOrderGiveTheSameSha256AndBytes() {
        var files = MixedCases.Set().Files();
        var ordinal = files.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        var forward = DiagnosticRuleSet.LoadFiles(new OrderedFiles(files, ordinal), RuleSetSource.Inline);
        var reverse = DiagnosticRuleSet.LoadFiles(new OrderedFiles(files, Enumerable.Reverse(ordinal)), RuleSetSource.Inline);
        Assert.AreEqual(forward.Identity.Sha256, reverse.Identity.Sha256);
        CollectionAssert.AreEqual(forward.Rules.Select(r => r.RuleId).ToList(), reverse.Rules.Select(r => r.RuleId).ToList());
        var options = new DiagnosticEvaluationOptions { Filter = RuleFilter.All, Verbose = true };
        var a = new DiagnosticRuleEvaluator(forward, options);
        var b = new DiagnosticRuleEvaluator(reverse, options);
        Assert.AreEqual(a.IdentityString, b.IdentityString);
        var random = new Random(20261121);
        var spectra = Enumerable.Range(0, 300).Select(i => i % 3 == 2 ? MixedCases.Any(random) : MixedCases.TieFree(random)).ToList();
        var bytesA = string.Join("\n", spectra.Select(s => ResultText(a.Evaluate(s))));
        var bytesB = string.Join("\n", spectra.Select(s => ResultText(b.Evaluate(s))));
        Assert.AreEqual(bytesA, bytesB);
        Assert.IsTrue(bytesA.Contains("\"fired\":true"), "some rule fires");
    }

    [TestMethod]
    public void ParallelEvaluationEqualsSequential() {
        var evaluator = Mixed();
        var random = new Random(20261120);
        var spectra = Enumerable.Range(0, 400).Select(i => i % 2 == 0 ? MixedCases.TieFree(random) : MixedCases.Any(random)).ToList();
        var sequential = spectra.Select(s => ResultText(evaluator.Evaluate(s))).ToArray();
        var parallel = new string[spectra.Count];
        Parallel.ForEach(Enumerable.Range(0, spectra.Count), new ParallelOptions { MaxDegreeOfParallelism = 8 },
                         i => parallel[i] = ResultText(evaluator.Evaluate(spectra[i])));
        CollectionAssert.AreEqual(sequential, parallel);
    }

    /// <summary>Every double reachable from a result, by property path; precursor_mz is the one allowed to be non-finite.</summary>
    private static IEnumerable<(string Path, double Value)> Doubles(object value, string path) {
        foreach (var p in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            var v = p.GetValue(value);
            var here = path + "." + p.Name;
            if (v is double d) {
                yield return (here, d);
            }
            else if (v is null || v is string || v is bool || v is int || v is long || v is RuleSetIdentity) {
                continue;
            }
            else if (v is IEnumerable list) {
                foreach (var item in list) {
                    if (item is null || item is string || item is bool || item.GetType().IsGenericType) {
                        continue;
                    }
                    foreach (var x in Doubles(item, here + "[]")) {
                        yield return x;
                    }
                }
            }
            else if (p.PropertyType.Namespace == typeof(RuleOutcome).Namespace) {
                foreach (var x in Doubles(v, here)) {
                    yield return x;
                }
            }
        }
    }

    [TestMethod]
    public void EvaluatorNeverThrows() {
        var verbose = Mixed();
        var compact = Mixed(verbose: false);
        var random = new Random(4242);
        var statuses = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var doubles = 0;
        var saturated = 0;
        for (var i = 0; i < 10000; i++) {
            var spectrum = MixedCases.Any(random);
            var evidence = random.Next(5) == 0 ? new AdductEvidence(random.Next(2) == 0 ? "[M-H]-" : "[M+H]+", AdductTier.Curated) : null;
            var evaluator = i % 2 == 0 ? verbose : compact;
            var result = evidence is null ? evaluator.Evaluate(spectrum) : evaluator.Evaluate(spectrum, evidence);
            Assert.IsTrue(DiagnosticResultJson.ToBytes(result).Length > 0);
            statuses[result.Status] = statuses.TryGetValue(result.Status, out var c) ? c + 1 : 1;
            foreach (var (path, value) in Doubles(result, "result")) {
                doubles++;
                saturated += value == double.MaxValue ? 1 : 0;
                if (path != "result.PrecursorMz" && (double.IsNaN(value) || double.IsInfinity(value))) {
                    Assert.Fail(path + " is " + value.ToString("R", CultureInfo.InvariantCulture) + " in spectrum " + i.ToString(CultureInfo.InvariantCulture));
                }
            }
        }
        Assert.AreEqual("evaluated no_peaks no_precursor unsupported_ion_mode", string.Join(" ", statuses.Keys));
        Assert.IsTrue(statuses["evaluated"] > 3000, statuses["evaluated"].ToString(CultureInfo.InvariantCulture));
        Assert.IsTrue(doubles > 50000, doubles.ToString(CultureInfo.InvariantCulture));
        // The generator reaches the overflow: a precursor-region I_raw that saturates instead of becoming infinite.
        Assert.IsTrue(saturated > 0, "no saturated double");

        // 10^5 peaks: the known ions among random ones.
        for (var k = 0; k < 2; k++) {
            var peaks = Enumerable.Range(0, 100000).Select(j => j % 1000 == 0
                ? new DiagnosticPeak(MixedCases.Ions[j / 1000 % MixedCases.Ions.Length], 500.0 + j)
                : new DiagnosticPeak(50.0 + random.NextDouble() * 1000.0, random.Next(5) == 0 ? 0.0 : random.NextDouble() * 100.0)).ToList();
            var result = verbose.Evaluate(new DiagnosticSpectrum(peaks, MixedCases.Precursors[k], DiagnosticIonMode.Negative, "QTOF"));
            Assert.AreEqual("evaluated", result.Status);
            Assert.AreEqual(100000, result.NPeaksIn);
            Assert.IsTrue(result.NPeaksDropped > 10000);
            Assert.IsTrue(DiagnosticResultJson.ToBytes(result).Length > 0);
        }
    }
}

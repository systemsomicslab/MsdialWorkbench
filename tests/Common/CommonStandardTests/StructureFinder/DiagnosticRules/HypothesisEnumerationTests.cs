#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.EngineHarness;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.InlineRuleSet;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// A naive reference enumerator, written here without the engine (design 4.2, 4.3, 7.1): every scaffold or core,
/// every unit-count vector inside the ranges and the class totals, every tried adduct; masses from the test-side pDa
/// oracle, formulas in Hill order, ids as the design spells them. The engine's lookup must give the same hypotheses
/// in the same order, and the same set as brute force on 10^5 seeded precursors.
/// </summary>
internal sealed class NaiveEnumerator
{
    public sealed class Unit
    {
        public Unit(string name, string cls, string residue, int lo, int hi) {
            Name = name;
            Class = cls;
            Residue = residue;
            Lo = lo;
            Hi = hi;
        }

        public string Name { get; }
        public string Class { get; }
        public string Residue { get; }
        public int Lo { get; }
        public int Hi { get; }
    }

    public sealed class Adduct
    {
        public Adduct(string name, int charge, string? add, string? remove) {
            Name = name;
            Charge = charge;
            Add = add;
            Remove = remove;
        }

        public string Name { get; }
        public int Charge { get; }
        public string? Add { get; }
        public string? Remove { get; }
    }

    public sealed class Hypothesis
    {
        public Hypothesis(string id, string formula, long pda) {
            Id = id;
            Formula = formula;
            Pda = pda;
        }

        public string Id { get; }
        public string Formula { get; }
        public long Pda { get; }
        public double Mz => Pda / 1e12;
    }

    /// <summary>Every hypothesis, ordered by id ordinally.</summary>
    public List<Hypothesis> All { get; } = new List<Hypothesis>();

    /// <param name="entries">(scaffold id or "core", neutral formula of the aglycone or core).</param>
    /// <param name="units">Every units-table row in table order, with the rule's range (0, 0 when not named).</param>
    /// <param name="totals">Class name to [lo, hi].</param>
    public NaiveEnumerator((string Id, string Formula)[] entries, Unit[] units, Dictionary<string, (int Lo, int Hi)> totals, Adduct[] adducts) {
        var vectors = new List<int[]> { new int[0] };
        foreach (var u in units) {
            vectors = vectors.SelectMany(v => Enumerable.Range(u.Lo, u.Hi - u.Lo + 1).Select(c => v.Concat(new[] { c }).ToArray())).ToList();
        }
        vectors = vectors.Where(v => totals.All(t => {
            var sum = units.Select((u, i) => u.Class == t.Key ? v[i] : 0).Sum();
            return sum >= t.Value.Lo && sum <= t.Value.Hi;
        })).ToList();
        foreach (var e in entries) {
            foreach (var v in vectors) {
                var counts = Parse(e.Formula);
                for (var i = 0; i < units.Length; i++) {
                    foreach (var kv in Parse(units[i].Residue)) {
                        counts[kv.Key] = (counts.TryGetValue(kv.Key, out var n) ? n : 0) + kv.Value * v[i];
                    }
                }
                var neutral = Hill(counts);
                var unitKey = string.Join(",", units.Select((u, i) => v[i] == 0 ? null : u.Name + v[i].ToString(CultureInfo.InvariantCulture)).Where(s => s != null));
                foreach (var a in adducts) {
                    var pda = TestMass.Of(neutral) + (a.Add is null ? 0 : TestMass.Of(a.Add)) - (a.Remove is null ? 0 : TestMass.Of(a.Remove)) - a.Charge * TestMass.Electron;
                    All.Add(new Hypothesis(e.Id + "|" + unitKey + "|" + a.Name, neutral, pda));
                }
            }
        }
        All.Sort((x, y) => string.CompareOrdinal(x.Id, y.Id));
    }

    /// <summary>The hypotheses whose precursor passes |P - mz| &lt;= tol + 1e-10, by id.</summary>
    public IEnumerable<Hypothesis> Matching(double precursor, double tolerance) => All.Where(h => Math.Abs(precursor - h.Mz) <= tolerance + 1e-10);

    public static Dictionary<string, long> Parse(string formula) {
        var counts = new Dictionary<string, long>(StringComparer.Ordinal);
        var i = 0;
        while (i < formula.Length) {
            var j = i + 1;
            while (j < formula.Length && char.IsLower(formula[j])) {
                j++;
            }
            var k = j;
            while (k < formula.Length && char.IsDigit(formula[k])) {
                k++;
            }
            var symbol = formula.Substring(i, j - i);
            counts[symbol] = (counts.TryGetValue(symbol, out var n) ? n : 0) + (k == j ? 1 : long.Parse(formula.Substring(j, k - j), CultureInfo.InvariantCulture));
            i = k;
        }
        return counts;
    }

    /// <summary>Hill order: C, H, then the others alphabetically (all alphabetically without carbon).</summary>
    public static string Hill(Dictionary<string, long> counts) {
        var keys = counts.Where(kv => kv.Value != 0).Select(kv => kv.Key).OrderBy(k => k, StringComparer.Ordinal).ToList();
        if (keys.Contains("C")) {
            keys = new[] { "C", "H" }.Where(keys.Contains).Concat(keys.Where(k => k != "C" && k != "H")).ToList();
        }
        return string.Concat(keys.Select(k => k + (counts[k] == 1 ? "" : counts[k].ToString(CultureInfo.InvariantCulture))));
    }
}

[TestClass]
public class HypothesisEnumerationTests
{
    private static NaiveEnumerator.Unit[] Units(int hexHi, int dHexHi, int penHi = 0, int caffeoylHi = 0) => new[]
    {
        new NaiveEnumerator.Unit("Hex", "sugar", "C6H10O5", 0, hexHi), new NaiveEnumerator.Unit("dHex", "sugar", "C6H10O4", 0, dHexHi),
        new NaiveEnumerator.Unit("Pen", "sugar", "C5H8O4", 0, penHi), new NaiveEnumerator.Unit("HexA", "sugar", "C6H8O6", 0, 0),
        new NaiveEnumerator.Unit("Gal", "sugar", "C6H10O5", 0, 0), new NaiveEnumerator.Unit("caffeoyl", "acyl", "C9H6O3", 0, caffeoylHi),
    };

    private static readonly (string, string)[] Aglycones = { ("genistein", "C15H10O5"), ("apigenin", "C15H10O5"), ("quercetin", "C15H10O7") };

    /// <summary>The series rule of <see cref="SeriesTests"/>: three scaffolds, Hex 0-2, dHex 0-1, 1-2 sugars, [M-H]- and
    /// the common [M+HCOO]-.</summary>
    private static (HypothesisIndex Index, NaiveEnumerator Naive) Series() =>
        (Index(SeriesTests.SeriesSet().Load(), SeriesTests.SeriesRule),
         new NaiveEnumerator(Aglycones, Units(2, 1), new Dictionary<string, (int, int)> { ["sugar"] = (1, 2) },
                             new[] { new NaiveEnumerator.Adduct("[M-H]-", -1, null, "H"), new NaiveEnumerator.Adduct("[M+HCOO]-", -1, "CHO2", null) }));

    private const string AcylRule = "flavonoid.acyl.pos";

    /// <summary>A positive core rule with Pen 0-1, Hex 0-3, caffeoyl 0-2, 0-3 sugars and 1-2 acyls, under [M+Na]+ and
    /// the common [M+H]+.</summary>
    private static (HypothesisIndex Index, NaiveEnumerator Naive) Acyl() {
        var set = new InlineRuleSet().AddRule(AcylRule, Terms(Term("Y0", "aglycone_ion", "required", Aglycone("even", "proton"))), AllOf(new[] { "Y0" }),
                                              InlineRuleSet.Composition("C15H10O6", null, "{\"Pen\":[0,1],\"Hex\":[0,3],\"caffeoyl\":[0,2]}",
                                                                        "{\"sugar\":[0,3],\"acyl\":[1,2]}"), "[\"[M+Na]+\"]", "Y0");
        return (Index(set.Load(), AcylRule),
                new NaiveEnumerator(new[] { ("core", "C15H10O6") }, Units(3, 0, 1, 2),
                                    new Dictionary<string, (int, int)> { ["sugar"] = (0, 3), ["acyl"] = (1, 2) },
                                    new[] { new NaiveEnumerator.Adduct("[M+Na]+", 1, "Na", null), new NaiveEnumerator.Adduct("[M+H]+", 1, "H", null) }));
    }

    private static string Describe(IEnumerable<EngineHypothesis> hypotheses) =>
        string.Join("\n", hypotheses.Select(h => h.HypothesisId + " " + h.NeutralFormula + " " + h.HypothesisMz.ToString("R", CultureInfo.InvariantCulture)));

    private static string Describe(IEnumerable<NaiveEnumerator.Hypothesis> hypotheses) =>
        string.Join("\n", hypotheses.Select(h => h.Id + " " + h.Formula + " " + h.Mz.ToString("R", CultureInfo.InvariantCulture)));

    [TestMethod]
    public void OrderAndSetEqualTheNaiveEnumerator() {
        foreach (var (index, naive, mode, p) in new[]
        {
            (Series().Index, Series().Naive, DiagnosticIonMode.Negative, 500.0),
            (Acyl().Index, Acyl().Naive, DiagnosticIonMode.Positive, 700.0),
        }) {
            // A tolerance that admits every hypothesis: the whole enumeration, in id order.
            var all = index.Select(p, mode, null, 1e4).Hypotheses;
            Assert.AreEqual(naive.All.Count, all.Count);
            Assert.AreEqual(Describe(naive.All), Describe(all));
            Assert.AreEqual(naive.All.Count, index.Compositions.Count * index.Tried.Count);
        }
        Assert.AreEqual(24, Series().Naive.All.Count);
        // (Pen 0-1 x Hex 0-3 without Pen1 Hex3, over 3 sugars) x caffeoyl 1-2 x two adducts.
        Assert.AreEqual(7 * 2 * 2, Acyl().Naive.All.Count);
    }

    [TestMethod]
    public void FastLookupEqualsBruteForceOn100000SeededPrecursors() {
        var random = new Random(20261118);
        var tolerances = new[] { 0.0, 0.001, 0.005, 0.01, 0.02, 0.5, 30.0 };
        var cases = new[] { (Series(), DiagnosticIonMode.Negative), (Acyl(), DiagnosticIonMode.Positive) };
        var nonEmpty = 0;
        for (var i = 0; i < 100000; i++) {
            var ((index, naive), mode) = cases[i % 2];
            var tol = tolerances[random.Next(tolerances.Length)];
            var near = naive.All[random.Next(naive.All.Count)].Mz;
            var p = (i / 2 % 5) switch
            {
                0 => near + (tol + 1e-10) * (random.Next(2) == 0 ? 1 : -1), // on the boundary, as the double falls
                1 => near + (random.NextDouble() - 0.5) * 4 * (tol + 1e-6),
                2 => 100.0 + random.NextDouble() * 1400.0,
                3 => near + (random.Next(2) == 0 ? 1 : -1) * (tol + random.Next(1, 4) * 1e-12),
                _ => near,
            };
            var fast = index.Select(p, mode, null, tol).Hypotheses;
            var expected = naive.Matching(p, tol).ToList();
            if (expected.Count > 0) {
                nonEmpty++;
            }
            if (fast.Count != expected.Count || !fast.Select(h => h.HypothesisId).SequenceEqual(expected.Select(h => h.Id), StringComparer.Ordinal)) {
                Assert.Fail("P " + p.ToString("R", CultureInfo.InvariantCulture) + " tol " + tol.ToString("R", CultureInfo.InvariantCulture)
                            + "\nfast:\n" + Describe(fast) + "\nnaive:\n" + Describe(expected));
            }
        }
        Assert.IsTrue(nonEmpty > 50000, nonEmpty.ToString(CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void TheEvaluatorCountsHypothesesAndDistinctFormulas() {
        var (_, naive) = Series();
        var evaluator = new DiagnosticRuleEvaluator(SeriesTests.SeriesSet().Load(), new DiagnosticEvaluationOptions { Filter = RuleFilter.All, Verbose = true });
        foreach (var target in new[] { "apigenin|Hex1|[M-H]-", "quercetin|Hex1,dHex1|[M+HCOO]-", "genistein|Hex2|[M-H]-" }) {
            var p = naive.All.Single(h => h.Id == target).Mz;
            var outcome = evaluator.Evaluate(MakeSpectrum(p, DiagnosticIonMode.Negative)).Outcomes.Single();
            var expected = naive.Matching(p, 0.01).ToList();
            CollectionAssert.AreEqual(expected.Select(h => h.Id).ToArray(), outcome.All.Select(h => h.HypothesisId).ToArray());
            Assert.AreEqual(expected.Count, outcome.NHypotheses);
            Assert.AreEqual(expected.Select(h => h.Formula).Distinct().Count(), outcome.NDistinctFormulas);
            CollectionAssert.AreEqual(expected.Select(h => h.Formula).ToArray(), outcome.All.Select(h => h.NeutralFormula).ToArray());
        }
        // genistein and apigenin share their formula: two hypotheses, one formula.
        var shared = evaluator.Evaluate(MakeSpectrum(naive.All.Single(h => h.Id == "apigenin|Hex1|[M-H]-").Mz, DiagnosticIonMode.Negative)).Outcomes.Single();
        Assert.AreEqual(2, shared.NHypotheses);
        Assert.AreEqual(1, shared.NDistinctFormulas);
    }
}

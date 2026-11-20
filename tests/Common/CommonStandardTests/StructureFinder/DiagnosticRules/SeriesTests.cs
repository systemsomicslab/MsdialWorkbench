#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Engine;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.InlineRuleSet;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// Composition series, hypotheses and their ids (design 4.2, 4.3): odometer order, unit_totals, the 100 000 cap,
/// composition and hypothesis ids, adduct modes and tiers, the precursor predicate and the fast lookup.
/// </summary>
[TestClass]
public class SeriesTests
{
    public const string SeriesRule = "flavonoid.series.neg";

    private static readonly string Y0 = Term("Y0", "aglycone_ion", "required", Aglycone("even", "proton"));
    private static readonly string Rda = Term("rda", "product_ion", "required", "\"scaffold_ions\":\"rda\"");
    private static readonly string Veto = Term("w", "window", "veto_present", Window("C9H7O4", "ion", "0.02"));

    /// <summary>Three scaffolds (genistein and apigenin share a formula), Hex 0-2, dHex 0-1, 1-2 sugars, [M-H]-; terms
    /// Y0, rda and a window veto w.</summary>
    internal static InlineRuleSet SeriesSet() =>
        new InlineRuleSet().AddRule(SeriesRule, "[" + Y0 + "," + Rda + "," + Veto + "]", AllOf(new[] { "Y0", "rda" }, new[] { "w" }),
                                    InlineRuleSet.Composition(null, "aglycones", "{\"Hex\":[0,2],\"dHex\":[0,1]}", "{\"sugar\":[1,2]}"),
                                    "[\"[M-H]-\"]", "Y0");

    private static HypothesisIndex SeriesIndex() => Index(SeriesSet().Load(), SeriesRule);

    /// <summary>The [M-H]- m/z of a neutral formula: M - H + e.</summary>
    private static double Deprotonated(string formula) => TestMass.Da(TestMass.Of(formula) - TestMass.Of("H") + TestMass.Electron);

    private static string Ids(HypothesisSet set) => string.Join(" ", set.Hypotheses.Select(h => h.HypothesisId));

    [TestMethod]
    public void CompositionsFollowTheOdometerAndUnitTotals() {
        var index = SeriesIndex();
        // Scaffold entries in list order; Hex (the first table unit) outermost, counts ascending; vectors outside
        // 1-2 sugars skipped (no sugar, and Hex2 dHex1).
        var expected = new[] { "genistein", "apigenin", "quercetin" }
            .SelectMany(e => new[] { "dHex1", "Hex1", "Hex1,dHex1", "Hex2" }.Select(u => e + "|" + u)).ToArray();
        CollectionAssert.AreEqual(expected, Enumerable.Range(0, index.Compositions.Count).Select(index.CompositionId).ToArray());
        Assert.AreEqual(TestMass.Of("C21H20O9"), index.Compositions[0].NeutralPda);
        Assert.AreEqual(TestMass.Of("C27H30O16"), index.Compositions[10].NeutralPda);
        CollectionAssert.AreEqual(new long[] { 1, 1, 0, 0, 0, 0 }, index.Compositions[10].Counts);
        // The adducts tried without evidence: the rule's list, then the common adducts of the polarity ([M+Cl]- is not common).
        CollectionAssert.AreEqual(new[] { "[M-H]-:rule", "[M+HCOO]-:common" }, index.Tried.Select(a => a.Adduct.Adduct + ":" + a.SourceName).ToArray());
    }

    [TestMethod]
    public void HypothesisIdsAndFieldsComeFromTheComposition() {
        var index = SeriesIndex();
        var p = Deprotonated("C21H20O10"); // apigenin or genistein + Hex
        var set = index.Select(p, DiagnosticIonMode.Negative, null, 0.01);
        Assert.AreEqual(PreconditionFailure.None, set.Failure);
        Assert.AreEqual(PreconditionMode.AdductAmbiguous, set.Mode);
        Assert.AreEqual(AdductTier.Assumed, set.Tier);
        // Equal formulas stay separate hypotheses, ordered by hypothesis_id (apigenin before genistein) although
        // genistein is enumerated first.
        Assert.AreEqual("apigenin|Hex1|[M-H]- genistein|Hex1|[M-H]-", Ids(set));
        var h = set.Hypotheses[0];
        Assert.AreEqual("apigenin|Hex1", h.CompositionId);
        Assert.AreEqual("apigenin", h.ScaffoldId);
        Assert.AreEqual("Hex:1", string.Join(",", h.Units.Select(u => u.Key + ":" + u.Value)));
        Assert.AreEqual("[M-H]-", h.Adduct);
        Assert.AreEqual("rule", h.AdductSource);
        Assert.AreEqual("C21H20O10", h.NeutralFormula);
        Assert.AreEqual(p, h.HypothesisMz);
        Assert.AreEqual(0.0, h.PrecursorErrorDa);
        Assert.AreEqual(TestMass.Of("C21H20O10") - TestMass.Of("H") + TestMass.Electron, h.Series.PrecursorPda);
        // P_obs - hypothesis m/z.
        Assert.AreEqual(p + 0.004 - p, index.Select(p + 0.004, DiagnosticIonMode.Negative, null, 0.01).Hypotheses[0].PrecursorErrorDa);
    }

    [TestMethod]
    public void ARuleWithoutAScaffoldListIsCore() {
        var s = new InlineRuleSet().AddRule("flavonoid.core.neg", "[" + Y0 + "]", AllOf(new[] { "Y0" }),
                                            InlineRuleSet.Composition("C15H10O5", null, "{\"Hex\":[1,1],\"dHex\":[0,1]}", "{}"),
                                            "[\"[M-H]-\"]", "Y0");
        var index = Index(s.Load(), "flavonoid.core.neg");
        CollectionAssert.AreEqual(new[] { "core|Hex1", "core|Hex1,dHex1" }, Enumerable.Range(0, index.Compositions.Count).Select(index.CompositionId).ToArray());
        var h = index.Select(Deprotonated("C21H20O10"), DiagnosticIonMode.Negative, null, 0.01).Hypotheses.Single();
        Assert.AreEqual("core|Hex1|[M-H]-", h.HypothesisId);
        Assert.IsNull(h.ScaffoldId);
    }

    [TestMethod]
    public void AFlavyliumCationIsItsOwnPrecursor() {
        // [M]+ carries the intrinsic charge: the composition is the cation's formula and the precursor is M - e.
        var s = new InlineRuleSet().AddRule("anthocyanin.flavylium.pos",
                                            "[" + Term("agl", "product_ion", "required", Formula("C15H11O6")) + "]", AllOf(new[] { "agl" }),
                                            InlineRuleSet.Composition("C21H21O11", null, "{}", "{}"), "[\"[M]+\"]", "agl");
        var index = Index(s.Load(), "anthocyanin.flavylium.pos");
        var set = index.Select(449.107837, DiagnosticIonMode.Positive, null, 0.01);
        var h = set.Hypotheses.Single();
        Assert.AreEqual("core||[M]+", h.HypothesisId);
        Assert.AreEqual("449.107837", ExactDecimal.Format(h.HypothesisMz, 6));
        Assert.AreEqual("C21H21O11", h.NeutralFormula);
        CollectionAssert.AreEqual(new[] { "[M]+:rule", "[M+H]+:common", "[M+Na]+:common" }, index.Tried.Select(a => a.Adduct.Adduct + ":" + a.SourceName).ToArray());
    }

    [TestMethod]
    public void WithoutEvidenceEveryTriedAdductIsAssumed() {
        var index = SeriesIndex();
        var formate = TestMass.Da(TestMass.Of("C21H20O10") + TestMass.Of("CHO2") + TestMass.Electron);
        var set = index.Select(formate, DiagnosticIonMode.Negative, null, 0.01);
        Assert.AreEqual("apigenin|Hex1|[M+HCOO]- genistein|Hex1|[M+HCOO]-", Ids(set));
        Assert.IsTrue(set.Hypotheses.All(h => h.AdductSource == "common"));
        Assert.AreEqual(AdductTier.Assumed, set.Tier);
    }

    [TestMethod]
    public void EvidenceTriesOnlyItsAdductAndOnlyWhenTheRuleListsIt() {
        var index = SeriesIndex();
        var p = Deprotonated("C21H20O10");
        var formate = TestMass.Da(TestMass.Of("C21H20O10") + TestMass.Of("CHO2") + TestMass.Electron);
        var listed = index.Select(p, DiagnosticIonMode.Negative, new AdductEvidence("[M-H]-", AdductTier.CrossPolarity), 0.01);
        Assert.AreEqual(PreconditionMode.MzOnly, listed.Mode);
        Assert.AreEqual(AdductTier.CrossPolarity, listed.Tier);
        Assert.AreEqual(PreconditionFailure.None, listed.Failure);
        Assert.AreEqual("apigenin|Hex1|[M-H]- genistein|Hex1|[M-H]-", Ids(listed));
        Assert.IsTrue(listed.Hypotheses.All(h => h.AdductSource == "rule"));
        // The evidence adduct alone: the formate precursor finds nothing under [M-H]-.
        Assert.AreEqual(0, index.Select(formate, DiagnosticIonMode.Negative, new AdductEvidence("[M-H]-", AdductTier.Curated), 0.01).Hypotheses.Count);
        // A common adduct the rule does not list: no hypotheses, adduct_not_in_rule, no fallback.
        var common = index.Select(formate, DiagnosticIonMode.Negative, new AdductEvidence("[M+HCOO]-", AdductTier.WithinPolarityPair), 0.01);
        Assert.AreEqual(PreconditionFailure.AdductNotInRule, common.Failure);
        Assert.AreEqual(0, common.Hypotheses.Count);
        Assert.AreEqual(AdductTier.WithinPolarityPair, common.Tier);
        Assert.AreEqual(PreconditionFailure.AdductNotInRule,
                        index.Select(formate, DiagnosticIonMode.Negative, new AdductEvidence("[M+X]-", AdductTier.Curated), 0.01).Failure);
    }

    [TestMethod]
    public void ARuleOfTheOtherPolarityIsNotTried() {
        var index = SeriesIndex();
        var set = index.Select(Deprotonated("C21H20O10"), DiagnosticIonMode.Positive, null, 0.01);
        Assert.AreEqual(PreconditionFailure.IonMode, set.Failure);
        Assert.AreEqual(0, set.Hypotheses.Count);
        Assert.AreEqual(PreconditionFailure.IonMode,
                        index.Select(Deprotonated("C21H20O10"), DiagnosticIonMode.Positive, new AdductEvidence("[M+H]+", AdductTier.Curated), 0.01).Failure);
    }

    [TestMethod]
    public void HypothesesAreOrderedByIdAcrossAdducts() {
        var index = SeriesIndex();
        // A 400 Da tolerance admits every composition under both adducts.
        var set = index.Select(Deprotonated("C21H20O10"), DiagnosticIonMode.Negative, null, 400.0);
        var ids = set.Hypotheses.Select(h => h.HypothesisId).ToList();
        Assert.AreEqual(index.Compositions.Count * 2, ids.Count);
        CollectionAssert.AreEqual(ids.OrderBy(x => x, StringComparer.Ordinal).ToList(), ids);
        Assert.AreEqual(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.AreEqual("apigenin|Hex1,dHex1|[M+HCOO]-", ids[0]); // '+' sorts before '-'
    }

    [TestMethod]
    public void FastLookupEqualsBruteForce() {
        var index = SeriesIndex();
        var random = new Random(20261120);
        var exact = new List<double>();
        for (var c = 0; c < index.Compositions.Count; c++) {
            for (var a = 0; a < index.Tried.Count; a++) {
                exact.Add(index.SeriesHypothesis(c, a).PrecursorPda / 1e12);
            }
        }
        var tolerances = new[] { 0.0, 0.002, 0.005, 0.01, 0.05, 15.0 };
        for (var i = 0; i < 5000; i++) {
            var tol = tolerances[random.Next(tolerances.Length)];
            var near = exact[random.Next(exact.Count)];
            var p = (i % 4) switch
            {
                0 => near + (tol + 1e-10) * (random.Next(2) == 0 ? 1 : -1), // on the boundary, as the double falls
                1 => near + (random.NextDouble() - 0.5) * 4 * (tol + 1e-6),
                2 => 200.0 + random.NextDouble() * 700.0,
                _ => near,
            };
            var fast = Ids(index.Select(p, DiagnosticIonMode.Negative, null, tol));
            var naive = new List<string>();
            for (var c = 0; c < index.Compositions.Count; c++) {
                for (var a = 0; a < index.Tried.Count; a++) {
                    var h = index.SeriesHypothesis(c, a);
                    if (Math.Abs(p - h.PrecursorPda / 1e12) <= tol + 1e-10) {
                        naive.Add(index.CompositionId(c) + "|" + h.Adduct.Adduct.Adduct);
                    }
                }
            }
            naive.Sort(StringComparer.Ordinal);
            Assert.AreEqual(string.Join(" ", naive), fast, "P " + p.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + " tol " + tol.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    [TestMethod]
    public void TheLookupNeverThrowsOnExtremePrecursors() {
        var index = SeriesIndex();
        // The evaluator checks the status first, so these never reach the index; the index still must not throw.
        foreach (var p in new[] { double.MaxValue, -double.MaxValue, double.Epsilon, 1e300, -1e300, 0.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity }) {
            foreach (var tol in new[] { 0.0, 0.01, 1e300, double.MaxValue }) {
                Assert.IsNotNull(index.Select(p, DiagnosticIonMode.Negative, null, tol));
            }
        }
        Assert.AreEqual(0, index.Select(1e300, DiagnosticIonMode.Negative, null, 0.01).Hypotheses.Count);
        // A huge tolerance clamps the window to the whole series.
        Assert.AreEqual(index.Compositions.Count * 2, index.Select(500.0, DiagnosticIonMode.Negative, null, 1e300).Hypotheses.Count);
    }

    // ------------------------------------------------------------------------------------------- the 100 000 cap

    private static InlineRuleSet CapSet(string hexRange) =>
        new InlineRuleSet().AddRule("flavonoid.cap.neg", "[" + Term("p", "product_ion", "required", Formula("C7H3O4")) + "]", AllOf(new[] { "p" }),
                                    InlineRuleSet.Composition("C15H10O5", null,
                                                              "{\"Hex\":" + hexRange + ",\"dHex\":[0,9],\"Pen\":[0,9],\"HexA\":[0,9],\"caffeoyl\":[0,9]}", "{}"),
                                    "[\"[M-H]-\"]", "p");

    [TestMethod]
    public void TheSeriesCapIs100000Compositions() {
        var index = Index(CapSet("[0,9]").Load(), "flavonoid.cap.neg");
        Assert.AreEqual(100000, index.Compositions.Count);
        Assert.AreEqual("core|", index.CompositionId(0));
        Assert.AreEqual("core|Hex9,dHex9,Pen9,HexA9,caffeoyl9", index.CompositionId(99999));
        var report = DiagnosticRuleSetLoader.Validate(CapSet("[0,10]").Files(), checkPin: false);
        Assert.AreEqual("series_too_large\trules/flavonoid.cap.neg.json\t/preconditions/composition", DiagnosticRuleFixtures.ErrorList(report.Errors));
    }
}

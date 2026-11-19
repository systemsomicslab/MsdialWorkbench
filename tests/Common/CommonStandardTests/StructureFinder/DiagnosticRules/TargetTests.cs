#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Engine;
using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.InlineRuleSet;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// Term members and targets (design 4.5) against an independent test-side mass oracle: every kind in both modes,
/// unit and scaffold members, Y0 even and radical, the adduct carrier, ion-series proper subsets with mass collapse,
/// windows, the decoy shift, the hypothesis and observed loss references, and the precursor region.
/// </summary>
[TestClass]
public class TargetTests
{
    private const string Neg = "flavonoid.targets.neg";
    private const string Pos = "flavonoid.targets.pos";

    private static readonly string[] Support = { "p_fix", "p_unit", "p_name", "p_scaf", "l_fix", "l_unit", "y_even", "y_rad", "y_add", "y_plus", "ys" };

    private static string Terms(string fixedIon) => "[" + string.Join(",", new[]
    {
        Term("p_fix", "product_ion", "required", Formula(fixedIon)),
        Term("p_unit", "product_ion", "required", UnitClass("sugar", minus: "H")),
        Term("p_name", "product_ion", "required", UnitName("dHex")),
        Term("p_scaf", "product_ion", "required", "\"scaffold_ions\":\"rda\""),
        Term("l_fix", "loss_from_precursor", "required", Formula("H2O")),
        Term("l_unit", "loss_from_precursor", "required", UnitClass("sugar")),
        Term("y_even", "aglycone_ion", "required", Aglycone("even", "proton")),
        Term("y_rad", "aglycone_ion", "required", Aglycone("radical", "proton")),
        Term("y_add", "aglycone_ion", "required", Aglycone("even", "adduct")),
        Term("y_plus", "aglycone_ion", "required", Aglycone("even", "proton", "H2O", "CO")),
        Term("ys", "ion_series", "required", Series("\"sugar\"")),
        Term("w_ion", "window", "veto_present", Window(fixedIon, "ion", "0.02")),
        Term("w_loss", "window", "veto_present", Window("CO2", "loss", "0.02")),
    }) + "]";

    private static InlineRuleSet TargetSet() =>
        new InlineRuleSet()
            .AddRule(Neg, Terms("C9H7O4"), AllOf(Support, new[] { "w_ion", "w_loss" }),
                     InlineRuleSet.Composition(null, "aglycones", "{\"Hex\":[1,2],\"dHex\":[0,1]}", "{}"), "[\"[M-H]-\"]", "p_fix")
            .AddRule(Pos, Terms("C9H9O4"), AllOf(Support, new[] { "w_ion", "w_loss" }),
                     InlineRuleSet.Composition(null, "aglycones", "{\"Hex\":[1,2],\"dHex\":[0,1]}", "{}"), "[\"[M+H]+\"]", "p_fix");

    private sealed class Case
    {
        public Case(DiagnosticRuleSet set, string ruleId, string compositionId, string adduct) {
            Index = InlineRuleSet.Index(set, ruleId);
            Builder = new TargetBuilder(Index);
            var c = Enumerable.Range(0, Index.Compositions.Count).Single(i => Index.CompositionId(i) == compositionId);
            var a = Enumerable.Range(0, Index.Tried.Count).Single(i => Index.Tried[i].Adduct.Adduct == adduct);
            Hypothesis = Index.Hypothesis(c, a, double.NaN);
            Z = Index.Tried[a].Adduct.Charge;
        }

        public HypothesisIndex Index { get; }
        public TargetBuilder Builder { get; }
        public EngineHypothesis Hypothesis { get; }
        public int Z { get; }
        public long Precursor => Hypothesis.Series.PrecursorPda;

        public IReadOnlyList<TermTargets> Build(long decoy = 0, LossReference reference = LossReference.Hypothesis, double precursorMz = double.NaN) =>
            Builder.Build(Hypothesis.Series, decoy, reference, precursorMz);
    }

    private static TermTargets TermOf(IReadOnlyList<TermTargets> targets, string id) => targets.Single(t => t.Term.Tid == id);

    /// <summary>"key=target" for every member, the target as an exact decimal.</summary>
    private static string Members(TermTargets t) =>
        string.Join(" ", t.Members.Select(m => m.Key + "=" + Json.ExactDecimal.Format(m.TargetMz, 9)));

    private static string Ion(string key, long massPda, int z) => key + "=" + Json.ExactDecimal.Format(TestMass.Ion(massPda, z), 9);

    private static string Loss(string key, long precursorPda, long lossPda) => key + "=" + Json.ExactDecimal.Format(TestMass.Da(precursorPda - lossPda), 9);

    private static long M(string formula) => TestMass.Of(formula);

    [TestMethod]
    public void EveryKindInNegativeMode() {
        var set = TargetSet().Load();
        var c = new Case(set, Neg, "quercetin|Hex1,dHex1", "[M-H]-");
        var t = c.Build();
        var a = M("C15H10O7"); // quercetin, the aglycone A
        Assert.AreEqual(M("C27H30O16") - M("H") + TestMass.Electron, c.Precursor);
        Assert.AreEqual(Ion("C9H7O4", M("C9H7O4"), -1), Members(TermOf(t, "p_fix")));
        // One member per unit present, ion = residue + plus - minus, by target.
        Assert.AreEqual(Ion("dHex", M("C6H9O4"), -1) + " " + Ion("Hex", M("C6H9O5"), -1), Members(TermOf(t, "p_unit")));
        Assert.AreEqual(Ion("dHex", M("C6H10O4"), -1), Members(TermOf(t, "p_name")));
        // The entry's ions for the mode, keyed by their Hill formulas.
        Assert.AreEqual(Ion("C7H3O4", M("C7H3O4"), -1) + " " + Ion("C8H5O4", M("C8H5O4"), -1), Members(TermOf(t, "p_scaf")));
        Assert.AreEqual(Loss("H2O", c.Precursor, M("H2O")), Members(TermOf(t, "l_fix")));
        Assert.AreEqual(Loss("Hex", c.Precursor, M("C6H10O5")) + " " + Loss("dHex", c.Precursor, M("C6H10O4")), Members(TermOf(t, "l_unit")));
        // Y0: even A - H, radical A - 2H, adduct carrier A + add - remove ([M-H]-: A - H), then plus and minus.
        Assert.AreEqual(Ion("A", a - M("H"), -1), Members(TermOf(t, "y_even")));
        Assert.AreEqual(Ion("A", a - M("H2"), -1), Members(TermOf(t, "y_rad")));
        Assert.AreEqual(Ion("A", a - M("H"), -1), Members(TermOf(t, "y_add")));
        Assert.AreEqual(Ion("A", a - M("H") + M("H2O") - M("CO"), -1), Members(TermOf(t, "y_plus")));
        // Every non-empty proper subset of the sugars present: {dHex1} and {Hex1}, never {Hex1, dHex1}.
        Assert.AreEqual(Ion("dHex1", a + M("C6H10O4") - M("H"), -1) + " " + Ion("Hex1", a + M("C6H10O5") - M("H"), -1), Members(TermOf(t, "ys")));
        // Windows: one centre each, the ion m/z or reference - loss.
        Assert.AreEqual(Ion("C9H7O4", M("C9H7O4"), -1), Members(TermOf(t, "w_ion")));
        Assert.AreEqual(Loss("CO2", c.Precursor, M("CO2")), Members(TermOf(t, "w_loss")));
        Assert.IsTrue(TermOf(t, "w_loss").IsWindow);
        Assert.AreEqual(0.02, TermOf(t, "w_loss").HalfWidthDa);
        Assert.IsTrue(double.IsNaN(TermOf(t, "p_fix").HalfWidthDa));
        Assert.AreEqual(0.05, TermOf(t, "p_fix").Floor);
        // Loss members carry their reference; ions do not.
        Assert.AreEqual(TestMass.Da(c.Precursor), TermOf(t, "l_fix").Members[0].LossReferenceMz);
        Assert.IsTrue(TermOf(t, "w_loss").Members[0].IsLoss);
        Assert.IsTrue(double.IsNaN(TermOf(t, "p_fix").Members[0].LossReferenceMz));
        Assert.AreEqual("C6H9O4", c.Builder.IonFormula(TermOf(t, "p_unit").Members[0]));
        Assert.AreEqual("H2O", c.Builder.IonFormula(TermOf(t, "l_fix").Members[0]));
    }

    [TestMethod]
    public void EveryKindInPositiveMode() {
        var set = TargetSet().Load();
        var c = new Case(set, Pos, "apigenin|Hex2", "[M+H]+");
        var t = c.Build();
        var a = M("C15H10O5");
        Assert.AreEqual(M("C27H30O15") + M("H") - TestMass.Electron, c.Precursor);
        Assert.AreEqual(Ion("C9H9O4", M("C9H9O4"), 1), Members(TermOf(t, "p_fix")));
        Assert.AreEqual(Ion("Hex", M("C6H9O5"), 1), Members(TermOf(t, "p_unit")));
        Assert.AreEqual("", Members(TermOf(t, "p_name"))); // no dHex in this composition
        Assert.AreEqual(Ion("C7H5O4", M("C7H5O4"), 1), Members(TermOf(t, "p_scaf")));
        Assert.AreEqual(Loss("Hex", c.Precursor, M("C6H10O5")), Members(TermOf(t, "l_unit")));
        // Y0: even A + H, radical A, adduct carrier A + H.
        Assert.AreEqual(Ion("A", a + M("H"), 1), Members(TermOf(t, "y_even")));
        Assert.AreEqual(Ion("A", a, 1), Members(TermOf(t, "y_rad")));
        Assert.AreEqual(Ion("A", a + M("H"), 1), Members(TermOf(t, "y_add")));
        Assert.AreEqual(Ion("A", a + M("H") + M("H2O") - M("CO"), 1), Members(TermOf(t, "y_plus")));
        // Hex2: the only proper non-empty subset is {Hex1}.
        Assert.AreEqual(Ion("Hex1", a + M("C6H10O5") + M("H"), 1), Members(TermOf(t, "ys")));
    }

    [TestMethod]
    public void TheAdductCarrierUsesTheHypothesisAdduct() {
        var set = TargetSet().Load();
        var neg = new Case(set, Neg, "apigenin|Hex1", "[M+HCOO]-").Build();
        Assert.AreEqual(Ion("A", M("C15H10O5") + M("CHO2"), -1), Members(TermOf(neg, "y_add")));
        Assert.AreEqual(Ion("A", M("C15H10O5") - M("H"), -1), Members(TermOf(neg, "y_even")));
        var pos = new Case(set, Pos, "apigenin|Hex1", "[M+Na]+").Build();
        Assert.AreEqual(Ion("A", M("C15H10O5") + M("Na"), 1), Members(TermOf(pos, "y_add")));
        Assert.AreEqual(Ion("A", M("C15H10O5") + M("H"), 1), Members(TermOf(pos, "y_even")));
    }

    [TestMethod]
    public void EqualMassMembersCollapse() {
        // Gal has Hex's residue: the members of equal mass collapse into one, keys joined by '+' in key order.
        var s = new InlineRuleSet().AddRule("flavonoid.collapse.neg",
            "[" + Term("ys", "ion_series", "required", Series("\"sugar\"")) + "," + Term("pu", "product_ion", "required", UnitClass("sugar")) + ","
            + Term("lu", "loss_from_precursor", "required", UnitClass("sugar")) + "]",
            AllOf(new[] { "ys", "pu", "lu" }), InlineRuleSet.Composition("C15H10O5", null, "{\"Hex\":[1,1],\"Gal\":[1,1],\"dHex\":[0,1]}", "{}"),
            "[\"[M-H]-\"]", "ys");
        var set = s.Load();
        var c = new Case(set, "flavonoid.collapse.neg", "core|Hex1,Gal1", "[M-H]-");
        var t = c.Build();
        var a = M("C15H10O5");
        Assert.AreEqual(Ion("Gal1+Hex1", a + M("C6H10O5") - M("H"), -1), Members(TermOf(t, "ys")));
        Assert.AreEqual(Ion("Gal+Hex", M("C6H10O5"), -1), Members(TermOf(t, "pu")));
        Assert.AreEqual(Loss("Gal+Hex", c.Precursor, M("C6H10O5")), Members(TermOf(t, "lu")));
        var d = new Case(set, "flavonoid.collapse.neg", "core|Hex1,dHex1,Gal1", "[M-H]-").Build();
        // Six non-empty proper subsets of {Hex1, dHex1, Gal1}; the four that differ only in Hex/Gal collapse into two,
        // keys in ordinal order ('H' before 'd').
        Assert.AreEqual(string.Join(" ", new[]
        {
            Ion("dHex1", a + M("C6H10O4") - M("H"), -1),
            Ion("Gal1+Hex1", a + M("C6H10O5") - M("H"), -1),
            Ion("Hex1,dHex1+dHex1,Gal1", a + M("C6H10O5") + M("C6H10O4") - M("H"), -1),
            Ion("Hex1,Gal1", a + 2 * M("C6H10O5") - M("H"), -1),
        }), Members(TermOf(d, "ys")));
    }

    [TestMethod]
    public void TheDecoyShiftMovesEveryTermMass() {
        var set = TargetSet().Load();
        var c = new Case(set, Neg, "quercetin|Hex1,dHex1", "[M-H]-");
        foreach (var delta in new[] { 5, 9, -7 }) {
            var d = delta * 1000000000000L;
            var plain = c.Build();
            var shifted = c.Build(d);
            foreach (var term in plain) {
                var other = TermOf(shifted, term.Term.Tid);
                Assert.AreEqual(term.Members.Count, other.Members.Count);
                for (var i = 0; i < term.Members.Count; i++) {
                    // Product targets move +delta, loss targets (precursor - (L + delta)) -delta, windows as their kind.
                    var expected = term.Members[i].TargetPda + (term.Members[i].IsLoss ? -d : d);
                    Assert.AreEqual(expected, other.Members[i].TargetPda, term.Term.Tid);
                    Assert.AreEqual(TestMass.Da(expected), other.Members[i].TargetMz, term.Term.Tid);
                    Assert.AreEqual(term.Members[i].Key, other.Members[i].Key);
                    Assert.AreEqual(term.Members[i].MassPda + d, other.Members[i].MassPda);
                }
            }
            // The hypotheses are untouched.
            Assert.AreEqual(M("C27H30O16") - M("H") + TestMass.Electron, c.Precursor);
        }
        Assert.AreEqual(TestMass.Ion(M("C9H7O4") + 5000000000000L, -1), TermOf(c.Build(5000000000000L), "p_fix").Members[0].TargetMz);
    }

    [TestMethod]
    public void TheObservedReferenceSubtractsTheLossFromPObs() {
        var set = TargetSet().Load();
        var c = new Case(set, Neg, "quercetin|Hex1,dHex1", "[M-H]-");
        var pObs = TestMass.Da(c.Precursor) + 0.006;
        var t = c.Build(0, LossReference.Observed, pObs);
        var water = TermOf(t, "l_fix").Members[0];
        Assert.AreEqual(pObs - M("H2O") / 1e12, water.TargetMz);
        Assert.AreEqual(pObs, water.LossReferenceMz);
        Assert.AreEqual(c.Precursor - M("H2O"), water.TargetPda); // order and collapse stay on the hypothesis reference
        Assert.AreEqual(pObs - M("CO2") / 1e12, TermOf(t, "w_loss").Members[0].TargetMz);
        Assert.AreEqual(pObs - (M("H2O") + 5000000000000L) / 1e12, TermOf(c.Build(5000000000000L, LossReference.Observed, pObs), "l_fix").Members[0].TargetMz);
        // Ions do not depend on the reference.
        Assert.AreEqual(TermOf(c.Build(), "p_fix").Members[0].TargetMz, TermOf(t, "p_fix").Members[0].TargetMz);
    }

    private static List<PeakHit> WaterHits(InlineRuleSet s, DiagnosticEvaluationOptions options) {
        var set = s.Load();
        var c = new Case(set, Neg, "quercetin|Hex1,dHex1", "[M-H]-");
        var rule = set.Rules.Single(r => r.RuleId == Neg);
        var settings = EvaluationSettings.From(options);
        var pObs = TestMass.Da(c.Precursor) + 0.006; // within the 0.01 Da precursor tolerance
        Assert.AreEqual(1, c.Index.Select(pObs, DiagnosticIonMode.Negative, null, settings.PrecursorToleranceFor(rule)).Hypotheses.Count);
        var hypothesisTarget = TestMass.Da(c.Precursor - M("H2O"));
        var view = SpectrumView.Create(new DiagnosticSpectrum(new[] { new DiagnosticPeak(150.0, 100.0), new DiagnosticPeak(hypothesisTarget, 50.0) },
                                                              pObs, DiagnosticIonMode.Negative, "QTOF"));
        var targets = c.Build(settings.DecoyShiftPda, settings.LossReferenceFor(rule), pObs);
        var water = TermOf(targets, "l_fix");
        var hits = new List<PeakHit>();
        view.Hits(water, water.Members[0], new MatchSettings(rule.Convention, settings.ProductToleranceFor(rule), settings.FloorScale), hits);
        return hits;
    }

    [TestMethod]
    public void ThePeakAtTheHypothesisLossTargetHitsOnlyUnderTheHypothesisReference() {
        // P_obs is 0.006 Da above the hypothesis m/z; the loss target near m/z 591 has a p2 tolerance of 0.00591 Da.
        Assert.AreEqual(1, WaterHits(TargetSet(), new DiagnosticEvaluationOptions()).Count);
        Assert.AreEqual(0, WaterHits(TargetSet(), new DiagnosticEvaluationOptions { LossReferenceOverride = LossReference.Observed }).Count);
        var observed = TargetSet().SetRule(Neg, "loss_reference", "\"observed\"");
        Assert.AreEqual(0, WaterHits(observed, new DiagnosticEvaluationOptions()).Count);
        Assert.AreEqual(1, WaterHits(observed, new DiagnosticEvaluationOptions { LossReferenceOverride = LossReference.Hypothesis }).Count);
    }

    [TestMethod]
    public void APeakInThePrecursorRegionIsNeverACandidate() {
        var set = TargetSet().Load();
        var c = new Case(set, Neg, "quercetin|Hex1,dHex1", "[M-H]-");
        var rule = set.Rules.Single(r => r.RuleId == Neg);
        var term = TermOf(c.Build(), "p_fix");
        var t = term.Members[0].TargetMz;
        var match = new MatchSettings(rule.Convention, EvaluationSettings.From(new DiagnosticEvaluationOptions()).ProductToleranceFor(rule), 1.0);
        PeakHit Hit(double pObs) {
            var view = SpectrumView.Create(new DiagnosticSpectrum(new[] { new DiagnosticPeak(100.0, 100.0), new DiagnosticPeak(t, 50.0) },
                                                                  pObs, DiagnosticIonMode.Negative, null!));
            return view.TermHits(term, match).Single();
        }
        // 0.9 Da below P_obs: a hit that I_raw and raw_present see, never a candidate.
        var inside = Hit(t + 0.9);
        Assert.IsTrue(inside.InPrecursorRegion);
        Assert.IsTrue(inside.Present);
        Assert.IsFalse(inside.IsCandidate);
        Assert.AreEqual(0, SpectrumView.Candidates(new[] { inside }).Count);
        Assert.AreEqual(0.5, SpectrumView.IRaw(new[] { inside }));
        Assert.IsTrue(SpectrumView.RawPresent(new[] { inside }));
        // 1.1 Da below: a candidate.
        var outside = Hit(t + 1.1);
        Assert.IsFalse(outside.InPrecursorRegion);
        Assert.IsTrue(outside.IsCandidate);
        Assert.AreEqual(1, SpectrumView.Candidates(new[] { outside }).Count);
        Assert.AreEqual(1, outside.InputIndex);
    }
}

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
/// Tolerance edges (design 4.3, 4.4): the p2 switch from 0.005 Da to 10 ppm at m/z 500, the inclusive p2 and strict
/// msdial hit on binary-exact inputs, the precursor predicate at 0.01 +- 2e-6 Da, and the loss constraint.
/// </summary>
[TestClass]
public class ToleranceTests
{
    private static readonly AppliedProductTolerance P2 = AppliedProductTolerance.For(ProductToleranceKind.P2, ProductTolerance.Da(0.01), null);

    private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

    [TestMethod]
    public void P2SwitchesFromFiveMillidaltonsToTenPpmAtMz500() {
        Assert.AreEqual("p2", P2.SourceName);
        Assert.AreEqual(0.005, P2.At(500.0));
        Assert.AreEqual(500.0 * 10.0 / 1000000.0, 0.005); // the two branches meet exactly at 500
        Assert.AreEqual(0.005, P2.At(499.999999));
        Assert.AreEqual(0.005, P2.At(100.0));
        Assert.AreEqual(500.000001 * 10.0 / 1000000.0, P2.At(500.000001));
        Assert.IsTrue(P2.At(500.000001) > 0.005);
        Assert.AreEqual(0.006, P2.At(600.0));
    }

    [TestMethod]
    public void OneExpressionIsBitIdenticalToEveryPerCaseFormula() {
        var random = new Random(20261120);
        var msfinderDa = AppliedProductTolerance.For(ProductToleranceKind.Msfinder, ProductTolerance.Da(0.01), null);
        var msfinderPpm = AppliedProductTolerance.For(ProductToleranceKind.Msfinder, ProductTolerance.FromPpm(15.0), null);
        var over = AppliedProductTolerance.For(ProductToleranceKind.P2, ProductTolerance.Da(0.01), ProductTolerance.FromPpm(7.5));
        Assert.AreEqual("msfinder", msfinderDa.SourceName);
        Assert.AreEqual("override", over.SourceName);
        for (var i = 0; i < 100000; i++) {
            var t = random.NextDouble() * 3000.0;
            Assert.AreEqual(Bits(Math.Max(0.005, t * 10.0 / 1000000.0)), Bits(P2.At(t)));
            Assert.AreEqual(Bits(0.01), Bits(msfinderDa.At(t)));
            Assert.AreEqual(Bits(t * 15.0 / 1000000.0), Bits(msfinderPpm.At(t)));
            Assert.AreEqual(Bits(t * 7.5 / 1000000.0), Bits(over.At(t)));
        }
    }

    private static SpectrumView View(double precursor, params double[] mz) =>
        SpectrumView.Create(new DiagnosticSpectrum(mz.Select(m => new DiagnosticPeak(m, 100.0)).ToList(), precursor, DiagnosticIonMode.Negative, null!));

    private static int[] Range(SpectrumView v, double target, double limit, bool strict) {
        v.HitRange(target, limit, strict, out var start, out var end);
        return Enumerable.Range(start, end - start).Select(v.InputIndex).ToArray();
    }

    [TestMethod]
    public void P2IsInclusiveAndMsdialStrictOnBinaryExactInputs() {
        // 2^-7 = 0.0078125: every value below is exact, and m - t is exact.
        const double t = 256.0, tol = 0.0078125;
        var v = View(900.0, t - tol - 0.0078125, t - tol, t - 0.00390625, t, t + tol, t + tol + 0.0078125);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, Range(v, t, tol + SpectrumView.Slack, strict: false));
        CollectionAssert.AreEqual(new[] { 2, 3 }, Range(v, t, tol, strict: true));
        CollectionAssert.AreEqual(new int[0], Range(v, 1000.0, tol, strict: false));
        CollectionAssert.AreEqual(new int[0], Range(v, 1.0, tol, strict: true));

        // The same through a term: the convention decides, at an override of exactly 0.0078125 Da.
        var set = SeriesTests.SeriesSet().Load();
        var index = Index(set, SeriesTests.SeriesRule);
        var term = new TermTargets(index.Rule.ById["rda"], new[] { new TargetMember("x", 256000000000000L, t, false, 0, new long[0], double.NaN) }, 0.05, double.NaN);
        var over = AppliedProductTolerance.For(ProductToleranceKind.P2, ProductTolerance.Da(0.01), ProductTolerance.Da(tol));
        var p2 = new List<PeakHit>();
        v.Hits(term, term.Members[0], new MatchSettings(RuleConvention.P2, over, 1.0), p2);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, p2.Select(h => h.InputIndex).ToArray());
        var msdial = new List<PeakHit>();
        v.Hits(term, term.Members[0], new MatchSettings(RuleConvention.Msdial, over, 1.0), msdial);
        CollectionAssert.AreEqual(new[] { 2, 3 }, msdial.Select(h => h.InputIndex).ToArray());
        CollectionAssert.AreEqual(new[] { -tol, -0.00390625, 0.0, tol }, p2.Select(h => h.ErrorDa).ToArray());
        // A window ignores the product tolerance: +-(half_width + 1e-10) in both conventions.
        var window = new TermTargets(index.Rule.ById["w"], term.Members, 0.05, 0.00390625);
        Assert.IsTrue(window.IsWindow);
        var w = new List<PeakHit>();
        v.Hits(window, term.Members[0], new MatchSettings(RuleConvention.Msdial, over, 1.0), w);
        CollectionAssert.AreEqual(new[] { 2, 3 }, w.Select(h => h.InputIndex).ToArray());
    }

    [TestMethod]
    public void ThePrecursorPredicateIsInclusiveWithItsSlack() {
        var set = SeriesTests.SeriesSet().Load();
        var index = Index(set, SeriesTests.SeriesRule);
        var mz = TestMass.Da(TestMass.Of("C21H20O10") - TestMass.Of("H") + TestMass.Electron);
        foreach (var sign in new[] { 1.0, -1.0 }) {
            Assert.AreEqual(2, index.Select(mz + sign * (0.01 - 2e-6), DiagnosticIonMode.Negative, null, 0.01).Hypotheses.Count, "inside " + sign);
            Assert.AreEqual(0, index.Select(mz + sign * (0.01 + 2e-6), DiagnosticIonMode.Negative, null, 0.01).Hypotheses.Count, "outside " + sign);
        }
        // The rule's tolerance unless overridden.
        var rule = set.Rules.Single();
        Assert.AreEqual(0.01, EvaluationSettings.From(new DiagnosticEvaluationOptions()).PrecursorToleranceFor(rule));
        var wider = EvaluationSettings.From(new DiagnosticEvaluationOptions { PrecursorToleranceDa = 0.02 }).PrecursorToleranceFor(rule);
        Assert.AreEqual(2, index.Select(mz + 0.01 + 2e-6, DiagnosticIonMode.Negative, null, wider).Hypotheses.Count);
        Assert.IsTrue(index.PrecursorMatches(index.Hypothesis(1, 0, mz).CompositionIndex, 0, mz + 0.01, 0.01));
    }

    [TestMethod]
    public void ALossHitNeedsOneDaltonBelowItsReference() {
        foreach (var convention in new[] { "p2", "msdial" }) {
            var s = new InlineRuleSet().AddRule("flavonoid.loss.neg", "[" + Term("lh", "loss_from_precursor", "required", Formula("H"), "0.01") + "]",
                                                AllOf(new[] { "lh" }), InlineRuleSet.Composition("C15H10O5", null, "{\"Hex\":[1,1]}", "{}"),
                                                "[\"[M-H]-\"]", "lh")
                .SetRule("flavonoid.loss.neg", "convention", "\"" + convention + "\"");
            var set = s.Load();
            var index = Index(set, "flavonoid.loss.neg");
            var rule = set.Rules.Single();
            var settings = EvaluationSettings.From(new DiagnosticEvaluationOptions { ProductToleranceOverride = ProductTolerance.Da(0.01) });
            var h = index.Hypothesis(0, 0, double.NaN);
            var reference = h.HypothesisMz;
            var term = new TargetBuilder(index).Build(h.Series, 0, LossReference.Hypothesis, reference).Single();
            Assert.AreEqual(reference, term.Members[0].LossReferenceMz);
            // Both peaks lie within 0.01 Da of the target (reference - 1.007825); only one is at least 1.0 Da below the reference.
            var view = SpectrumView.Create(new DiagnosticSpectrum(new[]
            {
                new DiagnosticPeak(150.0, 100.0), new DiagnosticPeak(reference - 0.999, 50.0), new DiagnosticPeak(reference - 1.0005, 50.0),
            }, reference, DiagnosticIonMode.Negative, null!));
            var hits = new List<PeakHit>();
            view.Hits(term, term.Members[0], new MatchSettings(rule.Convention, settings.ProductToleranceFor(rule), 1.0), hits);
            CollectionAssert.AreEqual(new[] { 2 }, hits.Select(x => x.InputIndex).ToArray(), convention);
        }
    }
}

/// <summary>The spectrum view (design 4.4): status order, peak drop and sort, both conventions' references and
/// presence.</summary>
[TestClass]
public class SpectrumViewTests
{
    private static SpectrumView View(double precursor, DiagnosticIonMode? mode, params (double Mz, double I)[] peaks) =>
        SpectrumView.Create(new DiagnosticSpectrum(peaks.Select(p => new DiagnosticPeak(p.Mz, p.I)).ToList(), precursor, mode, null!));

    [TestMethod]
    public void StatusesAreCheckedInOrder() {
        foreach (var p in new[] { double.NaN, 0.0, -1.0, -0.0, double.PositiveInfinity, double.NegativeInfinity }) {
            Assert.AreEqual("no_precursor", View(p, null).StatusName);
        }
        Assert.AreEqual("unsupported_ion_mode", View(300.0, null).StatusName);
        Assert.AreEqual("no_peaks", View(300.0, DiagnosticIonMode.Positive).StatusName);
        Assert.AreEqual("no_peaks", View(300.0, DiagnosticIonMode.Positive, (double.NaN, 1.0), (100.0, 0.0)).StatusName);
        Assert.AreEqual("evaluated", View(300.0, DiagnosticIonMode.Positive, (100.0, 1.0)).StatusName);
        Assert.AreEqual(SpectrumStatus.Evaluated, View(double.Epsilon, DiagnosticIonMode.Negative, (100.0, 1.0)).Status);
    }

    [TestMethod]
    public void UnusablePeaksAreDroppedAndTheRestSortedByMzThenInputIndex() {
        var v = View(300.0, DiagnosticIonMode.Negative,
                     (150.0, 10.0), (double.NaN, 5.0), (100.0, 20.0), (120.0, double.PositiveInfinity), (0.0, 5.0), (-3.0, 5.0),
                     (150.0, 30.0), (110.0, 0.0), (110.0, -1.0), (double.NegativeInfinity, 1.0), (100.0, 5.0), (90.0, double.NaN));
        Assert.AreEqual(12, v.NPeaksIn);
        Assert.AreEqual(8, v.NPeaksDropped);
        Assert.AreEqual(4, v.Count);
        CollectionAssert.AreEqual(new[] { 2, 10, 0, 6 }, Enumerable.Range(0, v.Count).Select(v.InputIndex).ToArray());
        CollectionAssert.AreEqual(new[] { 100.0, 100.0, 150.0, 150.0 }, Enumerable.Range(0, v.Count).Select(v.Mz).ToArray());
        // Dropped and counted whatever the status.
        Assert.AreEqual(1, View(double.NaN, null, (1.0, 0.0), (2.0, 1.0)).NPeaksDropped);
    }

    [TestMethod]
    public void TheP2BaseExcludesThePrecursorAndMsdialUsesTheMaximum() {
        // The precursor peak (300.0) and one exactly 1.0 Da away are not the base; one 1.0009765625 Da away is.
        var v = View(300.0, DiagnosticIonMode.Negative, (300.0, 1000.0), (299.0, 900.0), (298.9990234375, 400.0), (150.0, 200.0));
        Assert.IsTrue(v.HasBase);
        Assert.AreEqual(400.0, v.BaseIntensity);
        Assert.AreEqual(1000.0, v.MaxIntensity);
        var ofInput = Enumerable.Range(0, v.Count).ToDictionary(v.InputIndex);
        Assert.AreEqual(200.0 / 400.0, v.Rel(ofInput[3], RuleConvention.P2));
        Assert.AreEqual(900.0 / 400.0, v.Rel(ofInput[1], RuleConvention.P2));
        Assert.AreEqual((200.0 / 1000.0) * 100.0, v.Rel(ofInput[3], RuleConvention.Msdial));
        Assert.IsTrue(v.InPrecursorRegion(ofInput[1]));
        Assert.IsFalse(v.InPrecursorRegion(ofInput[2]));
    }

    [TestMethod]
    public void WithoutABasePeakP2SeesNothingAndMsdialStillWorks() {
        var v = View(300.0, DiagnosticIonMode.Negative, (300.0, 1000.0), (299.5, 900.0));
        Assert.IsFalse(v.HasBase);
        Assert.IsTrue(double.IsNaN(v.BaseIntensity));
        Assert.IsFalse(v.HasReference(RuleConvention.P2));
        Assert.IsTrue(v.HasReference(RuleConvention.Msdial));
        Assert.AreEqual(0.0, v.Rel(0, RuleConvention.P2));
        Assert.AreEqual((900.0 / 1000.0) * 100.0, v.Rel(0, RuleConvention.Msdial));
    }

    [TestMethod]
    public void PresenceIsInclusiveForP2AndStrictForMsdial() {
        Assert.IsTrue(SpectrumView.Present(0.05, 0.05, 1.0, RuleConvention.P2));
        Assert.IsFalse(SpectrumView.Present(0.0499, 0.05, 1.0, RuleConvention.P2));
        Assert.IsFalse(SpectrumView.Present(0.0, 0.0, 1.0, RuleConvention.P2)); // rel > 0 as well
        Assert.IsTrue(SpectrumView.Present(1e-300, 0.0, 1.0, RuleConvention.P2));
        Assert.IsFalse(SpectrumView.Present(5.0, 5.0, 1.0, RuleConvention.Msdial));
        Assert.IsTrue(SpectrumView.Present(5.0001, 5.0, 1.0, RuleConvention.Msdial));
        // The floor scale multiplies the floor.
        Assert.IsTrue(SpectrumView.Present(0.025, 0.05, 0.5, RuleConvention.P2));
        Assert.IsFalse(SpectrumView.Present(5.0, 2.5, 2.0, RuleConvention.Msdial));
    }

    [TestMethod]
    public void IRawIsTheMaximumHitRelIgnoringFloors() {
        var hits = new[]
        {
            new PeakHit(0, 4, 100.0, 0.02, 0.001, false, false),
            new PeakHit(1, 2, 100.001, 0.04, 0.002, false, true),
        };
        Assert.AreEqual(0.04, SpectrumView.IRaw(hits));
        Assert.IsFalse(SpectrumView.RawPresent(hits));
        Assert.AreEqual(0.0, SpectrumView.IRaw(new PeakHit[0]));
        Assert.IsFalse(SpectrumView.RawPresent(new PeakHit[0]));
    }

    [TestMethod]
    public void CandidatesAreOrderedByRelThenErrorThenInputIndex() {
        var hits = new[]
        {
            new PeakHit(0, 7, 100.0, 0.5, -0.002, true, false),
            new PeakHit(1, 3, 100.001, 0.5, 0.001, true, false),
            new PeakHit(2, 5, 100.002, 0.5, -0.001, true, false),
            new PeakHit(3, 1, 100.003, 0.9, 0.004, true, false),
            new PeakHit(4, 0, 100.004, 0.95, 0.004, false, false),
            new PeakHit(5, 2, 100.005, 0.99, 0.004, true, true),
        };
        CollectionAssert.AreEqual(new[] { 1, 3, 5, 7 }, SpectrumView.Candidates(hits).Select(h => h.InputIndex).ToArray());
    }
}

#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// Runs the engine components below the public evaluator on inline rule sets: one rule's <see cref="RuleEngine"/> under
/// an option snapshot, a spectrum built from (m/z, intensity) pairs, and the evaluation of every hypothesis that passes
/// the precursor predicate.
/// </summary>
internal static class EngineHarness
{
    /// <summary>The base peak every spectrum below carries: m/z 300, far from every target, intensity 1000, so a peak
    /// of intensity I has p2 rel I / 1000.</summary>
    public const double BaseMz = 300.0;
    public const double BaseIntensity = 1000.0;

    /// <summary>Apigenin hexoside: core C15H10O5 with one Hex, neutral C21H20O10.</summary>
    public static string CoreHex => InlineRuleSet.Composition("C15H10O5", null, "{\"Hex\":[1,1]}", "{}");

    /// <summary>[M-H]- of C21H20O10.</summary>
    public static double PrecursorNeg => TestMass.Ion(TestMass.Of("C21H20O10") - TestMass.Of("H"), -1);

    /// <summary>The m/z of a negative ion of the given formula.</summary>
    public static double IonNeg(string formula) => TestMass.Ion(TestMass.Of(formula), -1);

    /// <summary>A negative ion peak.</summary>
    public static (double Mz, double Intensity) Neg(string formula, double intensity) => (IonNeg(formula), intensity);

    public static RuleEngine MakeEngine(DiagnosticRuleSet set, string ruleId, DiagnosticEvaluationOptions? options = null) {
        for (var i = 0; i < set.Rules.Count; i++) {
            if (set.Rules[i].RuleId == ruleId) {
                return new RuleEngine(set.Rules[i], set.CompiledRules[i], set.Tables, EvaluationSettings.From(options ?? new DiagnosticEvaluationOptions()));
            }
        }
        throw new ArgumentException("no rule " + ruleId);
    }

    /// <summary>A spectrum with the base peak and <paramref name="peaks"/>, in that input order.</summary>
    public static DiagnosticSpectrum MakeSpectrum(double precursor, DiagnosticIonMode mode, params (double Mz, double Intensity)[] peaks) {
        var list = new List<DiagnosticPeak> { new DiagnosticPeak(BaseMz, BaseIntensity) };
        list.AddRange(peaks.Select(p => new DiagnosticPeak(p.Mz, p.Intensity)));
        return new DiagnosticSpectrum(list, precursor, mode, "QTOF");
    }

    /// <summary>A negative-mode spectrum at the [M-H]- of C21H20O10.</summary>
    public static DiagnosticSpectrum SpectrumNeg(params (double Mz, double Intensity)[] peaks) =>
        MakeSpectrum(PrecursorNeg, DiagnosticIonMode.Negative, peaks);

    /// <summary>The hypotheses of the rule for the spectrum, adduct_ambiguous.</summary>
    public static IReadOnlyList<EngineHypothesis> Hypotheses(RuleEngine engine, SpectrumView view) {
        Assert.AreEqual(SpectrumStatus.Evaluated, view.Status);
        return engine.Index.Select(view.PrecursorMz, view.IonMode!.Value, null, engine.PrecursorTolerance).Hypotheses;
    }

    /// <summary>Every hypothesis evaluated under the full logic, in hypothesis_id order.</summary>
    public static IReadOnlyList<HypothesisEvaluation> EvaluateAll(RuleEngine engine, DiagnosticSpectrum spectrum) {
        var view = SpectrumView.Create(spectrum);
        return Hypotheses(engine, view).Select(h => engine.Evaluate(view, h)).ToList();
    }

    /// <summary>The evaluation of the only hypothesis the spectrum's precursor matches.</summary>
    public static HypothesisEvaluation EvaluateOnly(RuleEngine engine, DiagnosticSpectrum spectrum) {
        var all = EvaluateAll(engine, spectrum);
        Assert.AreEqual(1, all.Count, "hypotheses: " + string.Join(" ", all.Select(e => e.Hypothesis.HypothesisId)));
        return all[0];
    }

    /// <summary>branch_fired of the rule for the spectrum.</summary>
    public static bool[][] BranchFired(RuleEngine engine, DiagnosticSpectrum spectrum) {
        var view = SpectrumView.Create(spectrum);
        return engine.BranchFired(view, Hypotheses(engine, view).Select(h => engine.Evaluate(view, h)).ToList());
    }

    /// <summary>"term_id:member_key@input_index" for every matched item, in canonical order.</summary>
    public static string Matched(HypothesisEvaluation e) =>
        string.Join(" ", e.Matched.Select(m => m.Term.Term.Tid + ":" + m.Member.Key + "@" + m.Peak.InputIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    /// <summary>A term with a than list and an optional ratio.</summary>
    public static string TermThan(string id, string kind, string role, string fields, string[] than, string? ratio, string floor = "0.05") =>
        InlineRuleSet.Term(id, kind, role, fields, floor).Replace(
            "\"than\":null,\"ratio\":null,\"ratio_source\":null",
            "\"than\":[" + string.Join(",", than.Select(t => "\"" + t + "\"")) + "],\"ratio\":" + (ratio ?? "null")
            + ",\"ratio_source\":" + (ratio is null ? "null" : "\"src:0\""));

    public static string TermRef(string id) => "{\"term\":\"" + id + "\"}";

    public static string AllNode(params string[] children) => "{\"all\":[" + string.Join(",", children) + "]}";

    public static string AnyNode(params string[] children) => "{\"any\":[" + string.Join(",", children) + "]}";

    public static string NotNode(string child) => "{\"not\":" + child + "}";

    /// <summary>k_of with k a JSON integer or a {var, offset, min} object.</summary>
    public static string KOf(string k, params string[] children) =>
        "{\"k_of\":{\"k\":" + k + ",\"k_source\":\"src:0\",\"of\":[" + string.Join(",", children) + "]}}";

    public static string KVar(string variable, int offset, int min) =>
        "{\"var\":\"" + variable + "\",\"offset\":" + offset.ToString(System.Globalization.CultureInfo.InvariantCulture)
        + ",\"min\":" + min.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}";

    public static string Terms(params string[] terms) => "[" + string.Join(",", terms) + "]";
}

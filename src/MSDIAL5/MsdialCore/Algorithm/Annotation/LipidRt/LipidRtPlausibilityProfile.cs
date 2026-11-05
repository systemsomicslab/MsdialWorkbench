using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt
{
    /// <summary>The verdict of a plausibility profile on one candidate.</summary>
    public sealed class LipidRtPlausibilityVerdict
    {
        public static LipidRtPlausibilityVerdict NotEvaluated { get; } = new LipidRtPlausibilityVerdict(LipidRtPlausibility.None, LipidRtPlausibilitySeverity.None, string.Empty, string.Empty);

        public LipidRtPlausibilityVerdict(LipidRtPlausibility verdict, LipidRtPlausibilitySeverity severity, string rule, string detail) {
            Verdict = verdict;
            Severity = severity;
            Rule = rule ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public LipidRtPlausibility Verdict { get; }
        public LipidRtPlausibilitySeverity Severity { get; }

        /// <summary>The failed rules, e.g. "B1" or "B2,B3"; empty unless Implausible.</summary>
        public string Rule { get; }

        public string Detail { get; }

        public bool IsImplausible => Verdict == LipidRtPlausibility.Implausible;
        public bool IsHard => IsImplausible && Severity == LipidRtPlausibilitySeverity.Hard;
    }

    /// <summary>
    /// A biological plausibility profile: chain-level rules for an organism, applied as a layer separate from the
    /// retention-time evidence (SPEC 2.4).
    /// </summary>
    /// <remarks>
    /// A profile states chain-level constraints only. It must never exclude or penalise a lipid class: the author's
    /// decision of 2026-10-15 (animals synthesise MGDG and DGDG, and diet and gut bacteria bring other classes).
    /// </remarks>
    public interface ILipidRtPlausibilityProfile
    {
        string Name { get; }

        LipidRtPlausibilityVerdict Evaluate(string ontology, IReadOnlyList<LipidRtChain> chains);
    }

    /// <summary>
    /// The chain-level plausibility rules B1-B3 as parameters: maximum double bonds per fatty chain (B1, hard),
    /// sphingolipid N-acyl double bonds (B2, soft), sphingoid base carbons and double bonds (B3, soft).
    /// </summary>
    /// <remarks>
    /// Rule semantics: a rule fails when a resolved chain breaks it; passes when every chain it concerns is resolved
    /// and passes; is untestable when it applies but the chain is not resolved (a species-level name); and is not
    /// applicable when it does not concern the class. The verdict is Implausible if any enabled rule fails (Hard when
    /// B1 is among them), Plausible if every applicable rule passes, and Untestable otherwise.
    /// </remarks>
    public sealed class LipidRtChainRuleProfile : ILipidRtPlausibilityProfile
    {
        /// <summary>The author's mammalian rules (2026-10-15): B1 DB &lt;= 6 per chain; B2 N-acyl DB &lt;= 2; B3 base C16-20, DB &lt;= 2.</summary>
        public static LipidRtChainRuleProfile Mammalian { get; } = new LipidRtChainRuleProfile("Mammalian", maxChainDoubleBond: 6, nAcylMaxDoubleBond: 2, baseMinCarbon: 16, baseMaxCarbon: 20, baseMaxDoubleBond: 2);

        public LipidRtChainRuleProfile(string name, int maxChainDoubleBond, int nAcylMaxDoubleBond, int baseMinCarbon, int baseMaxCarbon, int baseMaxDoubleBond,
            bool b1 = true, bool b2 = true, bool b3 = true) {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MaxChainDoubleBond = maxChainDoubleBond;
            NAcylMaxDoubleBond = nAcylMaxDoubleBond;
            BaseMinCarbon = baseMinCarbon;
            BaseMaxCarbon = baseMaxCarbon;
            BaseMaxDoubleBond = baseMaxDoubleBond;
            IsB1Enabled = b1;
            IsB2Enabled = b2;
            IsB3Enabled = b3;
        }

        public string Name { get; }
        public int MaxChainDoubleBond { get; }
        public int NAcylMaxDoubleBond { get; }
        public int BaseMinCarbon { get; }
        public int BaseMaxCarbon { get; }
        public int BaseMaxDoubleBond { get; }
        public bool IsB1Enabled { get; }
        public bool IsB2Enabled { get; }
        public bool IsB3Enabled { get; }

        /// <summary>The profile a parameter selects, or null for none.</summary>
        public static ILipidRtPlausibilityProfile FromParameter(LipidRtPlausibilityProfileKind kind) {
            switch (kind) {
                case LipidRtPlausibilityProfileKind.Mammalian:
                    return Mammalian;
                default:
                    return null;
            }
        }

        private enum RuleResult { NotApplicable, Pass, Fail, Untestable }

        public LipidRtPlausibilityVerdict Evaluate(string ontology, IReadOnlyList<LipidRtChain> chains) {
            ontology = ontology ?? string.Empty;
            chains = chains ?? Array.Empty<LipidRtChain>();
            var results = new List<(string Rule, RuleResult Result, string Detail)>();
            if (IsB1Enabled) {
                results.Add(("B1", B1(chains, out var detail), detail));
            }
            if (IsB2Enabled) {
                results.Add(("B2", B2(chains, ontology, out var detail), detail));
            }
            if (IsB3Enabled) {
                results.Add(("B3", B3(chains, ontology, out var detail), detail));
            }
            var fails = results.Where(r => r.Result == RuleResult.Fail).ToList();
            if (fails.Count > 0) {
                var severity = fails.Any(r => r.Rule == "B1") ? LipidRtPlausibilitySeverity.Hard : LipidRtPlausibilitySeverity.Soft;
                return new LipidRtPlausibilityVerdict(LipidRtPlausibility.Implausible, severity,
                    string.Join(",", fails.Select(r => r.Rule)),
                    string.Join("; ", fails.Select(r => r.Rule + ": " + r.Detail)));
            }
            var applicable = results.Where(r => r.Result != RuleResult.NotApplicable).ToList();
            if (applicable.Count > 0 && applicable.All(r => r.Result == RuleResult.Pass)) {
                return new LipidRtPlausibilityVerdict(LipidRtPlausibility.Plausible, LipidRtPlausibilitySeverity.None, string.Empty, string.Empty);
            }
            return new LipidRtPlausibilityVerdict(LipidRtPlausibility.Untestable, LipidRtPlausibilitySeverity.None, string.Empty, string.Empty);
        }

        private RuleResult B1(IReadOnlyList<LipidRtChain> chains, out string detail) {
            detail = string.Empty;
            var bad = chains.FirstOrDefault(c => c.IsFatty && c.DoubleBond > MaxChainDoubleBond);
            if (bad != null) {
                detail = $"chain {bad.Carbon}:{bad.DoubleBond} has DB>={MaxChainDoubleBond + 1}";
                return RuleResult.Fail;
            }
            var fatty = chains.Any(c => c.IsFatty);
            var unresolved = chains.Any(c => c.IsUnresolved);
            if (!fatty && !unresolved) {
                return RuleResult.NotApplicable;
            }
            return unresolved ? RuleResult.Untestable : RuleResult.Pass;
        }

        private RuleResult B2(IReadOnlyList<LipidRtChain> chains, string ontology, out string detail) {
            detail = string.Empty;
            if (!LipidRtNameParser.Sphingo.Contains(ontology) || LipidRtNameParser.SphingoidBaseOnly.Contains(ontology)) {
                return RuleResult.NotApplicable;
            }
            var nAcyl = chains.Where(c => c.Role == LipidRtChainRole.NAcyl).ToList();
            if (nAcyl.Count > 0) {
                var bad = nAcyl.FirstOrDefault(c => c.DoubleBond > NAcylMaxDoubleBond);
                if (bad != null) {
                    detail = $"N-acyl {bad.Carbon}:{bad.DoubleBond} has DB>={NAcylMaxDoubleBond + 1}";
                    return RuleResult.Fail;
                }
                return RuleResult.Pass;
            }
            return RuleResult.Untestable;
        }

        private RuleResult B3(IReadOnlyList<LipidRtChain> chains, string ontology, out string detail) {
            detail = string.Empty;
            if (!LipidRtNameParser.Sphingo.Contains(ontology)) {
                return RuleResult.NotApplicable;
            }
            var b = chains.FirstOrDefault(c => c.Role == LipidRtChainRole.SphingoidBase);
            if (b is null) {
                return RuleResult.Untestable;
            }
            if (b.Carbon < BaseMinCarbon || b.Carbon > BaseMaxCarbon) {
                detail = $"base {b.Carbon}:{b.DoubleBond} carbons outside {BaseMinCarbon}-{BaseMaxCarbon}";
                return RuleResult.Fail;
            }
            if (b.DoubleBond > BaseMaxDoubleBond) {
                detail = $"base {b.Carbon}:{b.DoubleBond} has DB>{BaseMaxDoubleBond}";
                return RuleResult.Fail;
            }
            return RuleResult.Pass;
        }
    }
}

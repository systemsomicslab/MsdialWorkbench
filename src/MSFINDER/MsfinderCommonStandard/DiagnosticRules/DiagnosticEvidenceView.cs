using CompMs.Common.StructureFinder.DiagnosticRules;
using Rfx.Riken.OsakaUniv;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Riken.Metabolomics.MsfinderCommon.DiagnosticRules
{
    /// <summary>
    /// What the GUI "Diagnostic rules" tab shows for one query (design 5.5 item 8), as display text: the header, the
    /// outcomes, the matched and vetoed terms, the ChemOnt comparison and the banners. Numbers use the export's fixed
    /// decimals, so the tab and the files agree. Report only.
    /// </summary>
    public sealed class DiagnosticEvidenceView
    {
        /// <summary>The engine's instrument class for an unknown instrument type.</summary>
        public const string UnknownInstrumentClass = "unknown";

        private readonly DiagnosticRuleResult _result;
        private readonly DiagnosticRuleRuntime _runtime;

        private DiagnosticEvidenceView(DiagnosticRuleRuntime runtime, DiagnosticRuleResult result) {
            _runtime = runtime;
            _result = result;
        }

        /// <summary>The runtime error (<c>rule_set_error:...</c>) when no runtime exists, else null.</summary>
        public string Error { get; private set; }
        public string Version { get; private set; } = string.Empty;
        public string Sha256 { get; private set; } = string.Empty;
        public string Source { get; private set; } = string.Empty;
        public string InstrumentType { get; private set; } = string.Empty;
        public string InstrumentClass { get; private set; } = string.Empty;
        public string InstrumentTypeSource { get; private set; } = string.Empty;
        public string PeaksCheck { get; private set; } = string.Empty;
        public string Status { get; private set; } = string.Empty;
        public string Scope { get; private set; } = string.Empty;
        public string Weight { get; private set; } = string.Empty;
        public IReadOnlyList<DiagnosticEvidenceOutcomeRow> Outcomes { get; private set; } = new DiagnosticEvidenceOutcomeRow[0];
        public IReadOnlyList<DiagnosticEvidenceTermRow> Terms { get; private set; } = new DiagnosticEvidenceTermRow[0];
        public IReadOnlyList<string> Banners { get; private set; } = new string[0];

        /// <summary>The tab when the runtime could not be created: the error only, no evidence and no ranking.</summary>
        public static DiagnosticEvidenceView Failed(string error) =>
            new DiagnosticEvidenceView(null, null) { Error = error ?? DiagnosticRuleRuntime.ErrorPrefix, Status = error ?? DiagnosticRuleRuntime.ErrorPrefix };

        public static DiagnosticEvidenceView Of(DiagnosticQueryEvaluation evaluation, DiagnosticRuleRuntime runtime) {
            if (evaluation == null) {
                throw new ArgumentNullException(nameof(evaluation));
            }
            if (runtime == null) {
                throw new ArgumentNullException(nameof(runtime));
            }
            var r = evaluation.Result;
            var info = evaluation.Info;
            var id = runtime.RuleSet.Identity;
            var view = new DiagnosticEvidenceView(runtime, r)
            {
                Version = id.Version ?? string.Empty,
                Sha256 = id.Sha256 ?? string.Empty,
                Source = DiagnosticRuleExport.SourceName(id.Source),
                InstrumentType = r?.InstrumentType ?? string.Empty,
                InstrumentClass = r?.InstrumentClass ?? string.Empty,
                InstrumentTypeSource = info?.InstrumentTypeSource ?? string.Empty,
                PeaksCheck = info == null ? string.Empty : info.PeaksSuspect ?? "ok",
                Status = evaluation.Status ?? string.Empty,
                Scope = DiagnosticRuleExport.ScopeName(runtime.RankingScope),
                Weight = DiagnosticRuleExport.Num(runtime.RankingWeight, DiagnosticRuleExport.MzDecimals),
            };
            var outcomes = new List<DiagnosticEvidenceOutcomeRow>();
            var terms = new List<DiagnosticEvidenceTermRow>();
            if (r != null) {
                foreach (var o in r.Outcomes) {
                    var h = DiagnosticRuleExport.Reported(o);
                    outcomes.Add(new DiagnosticEvidenceOutcomeRow(DiagnosticRuleExport.OutcomeName(o), o.RuleId, o.MotifId, DiagnosticRuleExport.Bool(o.ClaimEligible),
                        h?.HypothesisId ?? string.Empty, h?.Adduct ?? string.Empty, h?.NeutralFormula ?? string.Empty,
                        h == null ? string.Empty : DiagnosticRuleExport.Num(h.HypothesisMz, DiagnosticRuleExport.MzDecimals),
                        h == null ? string.Empty : DiagnosticRuleExport.Num(h.Score, DiagnosticRuleExport.MzDecimals),
                        DiagnosticRuleExport.Int(o.NHypothesesFired) + "/" + DiagnosticRuleExport.Int(o.NHypotheses)));
                    if (h == null) {
                        continue;
                    }
                    foreach (var m in h.Matched) {
                        terms.Add(new DiagnosticEvidenceTermRow(o.RuleId, m.TermId + ":" + m.MemberKey, m.Role ?? string.Empty, DiagnosticRuleExport.Int(m.PeakIndex),
                            DiagnosticRuleExport.Num(m.PeakMz, DiagnosticRuleExport.MzDecimals), DiagnosticRuleExport.Num(m.TargetMz, DiagnosticRuleExport.MzDecimals),
                            DiagnosticRuleExport.Num(m.ErrorDa * 1000.0, DiagnosticRuleExport.MdaDecimals), DiagnosticRuleExport.Num(m.ErrorPpm, DiagnosticRuleExport.PpmDecimals),
                            DiagnosticRuleExport.Num(m.RelIntensity, DiagnosticRuleExport.RelDecimals)));
                    }
                    foreach (var v in h.Vetoes) {
                        if (!v.Fired) {
                            continue;
                        }
                        terms.Add(new DiagnosticEvidenceTermRow(o.RuleId, v.TermId, "veto", v.PeakIndex.HasValue ? DiagnosticRuleExport.Int(v.PeakIndex.Value) : string.Empty,
                            v.PeakMz.HasValue ? DiagnosticRuleExport.Num(v.PeakMz.Value, DiagnosticRuleExport.MzDecimals) : string.Empty,
                            string.Empty, string.Empty, string.Empty, string.Empty));
                    }
                }
                foreach (var x in r.Refusals) {
                    var rule = runtime.RuleSet.Rules.FirstOrDefault(u => string.Equals(u.RuleId, x.RuleId, StringComparison.Ordinal));
                    outcomes.Add(new DiagnosticEvidenceOutcomeRow("refused", x.RuleId, rule?.Claim.MotifId ?? string.Empty, string.Empty, string.Empty,
                        string.Empty, string.Empty, string.Empty, string.Empty, string.Empty));
                }
            }
            view.Outcomes = outcomes;
            view.Terms = terms;
            view.Banners = BannersOf(runtime, r);
            return view;
        }

        private static IReadOnlyList<string> BannersOf(DiagnosticRuleRuntime runtime, DiagnosticRuleResult r) {
            var banners = new List<string>();
            if (runtime.RankingScope == DiagnosticRuleRankingScope.Measured) {
                banners.Add("Measured scope: rules that are not author-reviewed take part in the ranking.");
            }
            if (r != null && string.Equals(r.InstrumentClass, UnknownInstrumentClass, StringComparison.Ordinal)) {
                banners.Add("Instrument class unknown: set INSTRUMENTTYPE in the query or the instrument-type option.");
            }
            if (r != null && DiagnosticRankingInput.Of(r, runtime.RuleSet).ConflictUnder(runtime.RankingScope)) {
                banners.Add("Rule conflict: fired rules disagree, so the ranking term is suppressed for this query.");
            }
            var refused = runtime.Evaluator.Refusals.Select(x => x.RuleId).Distinct(StringComparer.Ordinal).Count();
            if (refused > 0) {
                banners.Add(DiagnosticRuleExport.Int(refused) + " rule(s) refused by the evaluator.");
            }
            return banners;
        }

        /// <summary>
        /// The ChemOnt comparison of each reported outcome (design 5.7): the FSEA top term of the top formula and
        /// the ontologies of the base and ranked rank-1 candidates, from <paramref name="context"/> (may be null).
        /// Empty when no runtime exists or the query was not evaluated.
        /// </summary>
        public IReadOnlyList<ChemOntComparison> ChemOnt(DiagnosticQueryContext context) {
            if (_runtime == null || _result == null) {
                return new ChemOntComparison[0];
            }
            context = context ?? DiagnosticQueryContext.Empty;
            return ChemOntAgreement.Compare(_result, _runtime.RuleSet, _runtime.RankingScope, context.TopFormulaTerm, context.BaseTopOntology, context.RankedTopOntology);
        }
    }

    /// <summary>One outcome row of the GUI tab.</summary>
    public sealed class DiagnosticEvidenceOutcomeRow
    {
        public DiagnosticEvidenceOutcomeRow(string outcome, string ruleId, string motifId, string claimEligible, string hypothesisId, string adduct,
                                            string neutralFormula, string hypothesisMz, string score, string hypothesesFired) {
            Outcome = outcome;
            RuleId = ruleId ?? string.Empty;
            MotifId = motifId ?? string.Empty;
            ClaimEligible = claimEligible;
            HypothesisId = hypothesisId;
            Adduct = adduct;
            NeutralFormula = neutralFormula;
            HypothesisMz = hypothesisMz;
            Score = score;
            HypothesesFired = hypothesesFired;
        }

        public string Outcome { get; }
        public string RuleId { get; }
        public string MotifId { get; }
        public string ClaimEligible { get; }
        public string HypothesisId { get; }
        public string Adduct { get; }
        public string NeutralFormula { get; }
        public string HypothesisMz { get; }
        public string Score { get; }
        /// <summary><c>fired/tested</c> hypotheses.</summary>
        public string HypothesesFired { get; }
    }

    /// <summary>One matched or vetoed term of the reported hypothesis of an outcome.</summary>
    public sealed class DiagnosticEvidenceTermRow
    {
        public DiagnosticEvidenceTermRow(string ruleId, string term, string role, string peakIndex, string observedMz, string theoreticalMz,
                                         string errorMDa, string errorPpm, string rel) {
            RuleId = ruleId ?? string.Empty;
            Term = term ?? string.Empty;
            Role = role;
            PeakIndex = peakIndex;
            ObservedMz = observedMz;
            TheoreticalMz = theoreticalMz;
            ErrorMDa = errorMDa;
            ErrorPpm = errorPpm;
            Rel = rel;
        }

        public string RuleId { get; }
        /// <summary><c>term:member</c> for a matched term, the term id for a veto.</summary>
        public string Term { get; }
        /// <summary>The term's role, or <c>veto</c>.</summary>
        public string Role { get; }
        public string PeakIndex { get; }
        public string ObservedMz { get; }
        public string TheoreticalMz { get; }
        public string ErrorMDa { get; }
        public string ErrorPpm { get; }
        public string Rel { get; }
    }
}

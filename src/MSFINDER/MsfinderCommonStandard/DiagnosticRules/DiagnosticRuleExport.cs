using CompMs.Common;
using CompMs.Common.StructureFinder.DiagnosticRules;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.Query;
using Riken.Metabolomics.StructureFinder.Result;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Riken.Metabolomics.MsfinderCommon.DiagnosticRules
{
    /// <summary>The file names of one run or export, all with the same stamp (design 5.5).</summary>
    public sealed class DiagnosticOutputNames
    {
        public DiagnosticOutputNames(string stamp) {
            Result = DiagnosticRuleExport.ResultPrefix + stamp + ".txt";
            Evidence = DiagnosticRuleExport.EvidencePrefix + stamp + ".jsonl";
            Context = DiagnosticRuleExport.ContextPrefix + stamp + ".txt";
            Run = DiagnosticRuleExport.RunPrefix + stamp + ".json";
            Timing = DiagnosticRuleExport.TimingPrefix + stamp + ".tsv";
        }

        public string Result { get; }
        public string Evidence { get; }
        public string Context { get; }
        public string Run { get; }
        public string Timing { get; }

        /// <summary>Every name, in a fixed order.</summary>
        public IReadOnlyList<string> All => new[] { Result, Evidence, Context, Run, Timing };
    }

    /// <summary>The per-run counts of the run manifest and the console summary line.</summary>
    public sealed class DiagnosticRunCounts
    {
        public int Queries { get; private set; }
        /// <summary>Queries whose result status is <c>evaluated</c> (= Fired + VetoBlocked + None).</summary>
        public int Evaluated { get; private set; }
        /// <summary>Evaluated queries with at least one fired outcome.</summary>
        public int Fired { get; private set; }
        /// <summary>Evaluated queries with no fired outcome and at least one veto_blocked outcome.</summary>
        public int VetoBlocked { get; private set; }
        /// <summary>Evaluated queries with neither.</summary>
        public int None { get; private set; }
        /// <summary>Queries neither evaluated nor failed (glue skips and engine statuses such as no_precursor).</summary>
        public int Skipped { get; private set; }
        /// <summary>Queries whose evaluation threw (not Strict).</summary>
        public int Errors { get; private set; }
        /// <summary>Evaluated queries with rule_conflict.</summary>
        public int Conflicts { get; private set; }
        /// <summary>Evaluated queries with eligible_rule_conflict.</summary>
        public int EligibleConflicts { get; private set; }
        /// <summary>The rules refused when the evaluator was built (constant over the run).</summary>
        public int Refused { get; internal set; }
        /// <summary>Skipped queries by their result-TSV Outcome (<c>skipped:&lt;reason&gt;</c>), in ordinal order.</summary>
        public SortedDictionary<string, int> SkippedByReason { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);

        public void Add(DiagnosticQueryEvaluation evaluation) {
            Queries++;
            var status = evaluation.Status ?? string.Empty;
            var r = evaluation.Result;
            if (r != null && status == DiagnosticRuleResult.StatusEvaluated) {
                Evaluated++;
                if (r.Outcomes.Any(o => o.Fired)) {
                    Fired++;
                }
                else if (r.Outcomes.Any(o => o.VetoBlocked)) {
                    VetoBlocked++;
                }
                else {
                    None++;
                }
                if (r.RuleConflict) {
                    Conflicts++;
                }
                if (r.EligibleRuleConflict) {
                    EligibleConflicts++;
                }
            }
            else if (status.StartsWith(DiagnosticRuleRuntime.ErrorStatusPrefix, StringComparison.Ordinal)) {
                Errors++;
            }
            else {
                Skipped++;
                // Keyed by the result TSV's Outcome text, so a consumer can join the counts to that column.
                var key = DiagnosticRuleExport.NoOutcomeName(status);
                SkippedByReason.TryGetValue(key, out var n);
                SkippedByReason[key] = n + 1;
            }
        }
    }

    /// <summary>
    /// The writers of the diagnostic-rule outputs (design 5.5): the result TSV, the evidence JSONL, the context TSV, the
    /// canonical run manifest, the appended structure-result columns and the console summary line. ASCII, file names
    /// only, every double through <see cref="ExactDecimal"/>, integers invariant; a non-finite value is an empty cell
    /// or JSON null. Banned in this type by the glue IL scan: every Double.ToString and string.Concat/Format of a
    /// boxed double (design 6.2).
    /// </summary>
    public static class DiagnosticRuleExport
    {
        public const string ResultPrefix = "Diagnostic rule result-";
        public const string EvidencePrefix = "Diagnostic rule evidence-";
        public const string ContextPrefix = "Diagnostic rule context-";
        public const string RunPrefix = "Diagnostic rule run-";
        public const string TimingPrefix = "Diagnostic rule timing-";

        /// <summary>The prefix of the error a run reports when one of its new files exists (OQ04).</summary>
        public const string OutputExists = "error:output_exists";

        /// <summary>The line break of every new file, whatever the platform.</summary>
        public const string NewLine = "\n";

        /// <summary>m/z, Da, Score, Ranking score and Rule weight (design 5.5).</summary>
        public const int MzDecimals = 6;
        public const int PpmDecimals = 3;
        public const int MdaDecimals = 3;
        public const int RelDecimals = 6;

        public static readonly string[] ResultColumns =
        {
            // query
            "File name", "Title", "Input sha256", "Precursor mz", "Ion mode", "Instrument type", "Instrument class",
            "Instrument type source", "Peaks source", "Peaks check", "Peaks dropped by adapter",
            // run
            "Rule set version", "Rule set sha256", "Rule set source", "Engine semantics", "Status",
            // outcome
            "Outcome", "Rule id", "Rule version", "Motif id", "Author reviewed", "Claim eligible", "Claim ineligible",
            "Precondition mode", "Adduct", "Adduct source", "Adduct tier", "Hypothesis id", "Hypothesis formula",
            "Hypothesis mz", "Precursor error (Da)", "N hypotheses", "N fired hypotheses", "Tied hypotheses", "Score",
            // terms
            "Matched terms", "Veto terms", "Branches fired", "Product tolerance", "Precursor tolerance", "Rule conflict",
        };

        public static readonly string[] ContextColumns =
        {
            "File name", "Input sha256", "Rule id", "Motif id", "Query precursor type", "FSEA top term", "FSEA agreement",
            "Base top ClassyFire", "Base top agreement", "Ranked top ClassyFire", "Ranked top agreement",
            "Hypothesis formula equals top formula", "Candidates matching motif", "Rule weight", "Rule scope",
        };

        /// <summary>The legacy console structure-result header (PredictProcess), unchanged.</summary>
        public const string LegacyStructureHeader = "File path\tFile name\tTitle\tMS1 count\tMSMS count\t" +
            "Precursor mz\tPrecursor type\tStructure rank\tTotal score\tDatabases\tFormula\tOntology\tInChIKey\tSMILES";

        /// <summary>The columns appended after SMILES when the ranking weight is above zero.</summary>
        public static readonly string[] StructureAppendedColumns =
        {
            "Ranking score", "Base rank", "Diagnostic rule term", "Matched fired motifs", "Matched vetoed motifs",
            "Diagnostic rule status", "Rule set version", "Rule set sha256",
        };

        public static string ResultHeader => string.Join("\t", ResultColumns);

        public static string ContextHeader => string.Join("\t", ContextColumns);

        public static string StructureHeader => LegacyStructureHeader + "\t" + string.Join("\t", StructureAppendedColumns);

        /// <summary>The legacy GUI batch-export structure header (ExportUtility.writeStructureResultHeader), unchanged.</summary>
        public const string GuiLegacyStructureHeader = "File path\tFile name\tTitle\tMS1 count\tMSMS count\t" +
            "Precursor mz\tPrecursor type\tStructure\tTotal score\tDatabases\tFormula\tOntology\tInChIKey\tSMILES";

        public static string GuiStructureHeader => GuiLegacyStructureHeader + "\t" + string.Join("\t", StructureAppendedColumns);

        /// <summary>The legacy GUI per-file structure header (ExportUtility.writeStructureResult), unchanged.</summary>
        public const string EachFileLegacyStructureHeader = "Rank\tTitle\tScore\tRetention time\tRetention index\tResources\tFormula\tOntology\tInChIKey\tSMILES";

        public static string EachFileStructureHeader => EachFileLegacyStructureHeader + "\t" + string.Join("\t", StructureAppendedColumns);

        /// <summary><c>yyyyMMddHHmmss</c> in the invariant culture.</summary>
        public static string Stamp(DateTime time) => time.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        /// <summary>A TSV cell: null is empty, tab and line breaks become a space, anything outside printable ASCII
        /// becomes '?'.</summary>
        public static string Cell(string value) {
            if (string.IsNullOrEmpty(value)) {
                return string.Empty;
            }
            var sb = new StringBuilder(value.Length);
            foreach (var c in value) {
                if (c == '\t' || c == '\r' || c == '\n') {
                    sb.Append(' ');
                }
                else if (c < ' ' || c > '~') {
                    sb.Append('?');
                }
                else {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>A double as exact fixed-point text, or an empty cell when it is not finite.</summary>
        public static string Num(double value, int decimals) => ExactDecimal.FormatOrNull(value, decimals) ?? string.Empty;

        public static string Bool(bool value) => value ? "true" : "false";

        public static string Int(long value) => value.ToString(CultureInfo.InvariantCulture);

        public static string SourceName(RuleSetSource source) {
            switch (source) {
                case RuleSetSource.Embedded: return "embedded";
                case RuleSetSource.Directory: return "directory";
                default: return "inline";
            }
        }

        public static string ScopeName(DiagnosticRuleRankingScope scope) => scope == DiagnosticRuleRankingScope.Measured ? "measured" : "reviewed";

        private static string IonModeName(IonMode mode) {
            switch (mode) {
                case IonMode.Positive: return "Positive";
                case IonMode.Negative: return "Negative";
                default: return "Both";
            }
        }

        private static string PeaksCheck(DiagnosticInputInfo info) => info == null ? null : info.PeaksSuspect ?? "ok";

        /// <summary>The Outcome cell of a query without an outcome row: <c>none</c> when evaluated, the
        /// <c>error:</c> status, or a <c>skipped:</c> status (engine statuses get the prefix).</summary>
        public static string NoOutcomeName(string status) {
            status = status ?? string.Empty;
            if (status == DiagnosticRuleResult.StatusEvaluated) {
                return "none";
            }
            if (status.StartsWith(DiagnosticRuleRuntime.ErrorStatusPrefix, StringComparison.Ordinal) || status.StartsWith("skipped:", StringComparison.Ordinal)) {
                return status;
            }
            return "skipped:" + status;
        }

        public static string OutcomeName(RuleOutcome o) => o.Fired ? "fired" : o.VetoBlocked ? "veto_blocked" : "not_fired";

        /// <summary>The hypothesis an outcome row reports: the chosen one, else (veto_blocked) the first that fired
        /// ignoring vetoes, else none.</summary>
        public static HypothesisOutcome Reported(RuleOutcome o) {
            if (o.Chosen != null) {
                return o.Chosen;
            }
            foreach (var h in o.All) {
                if (h.FiredIgnoringVetoes) {
                    return h;
                }
            }
            return null;
        }

        private static CompMs.Common.StructureFinder.DiagnosticRules.Model.Rule RuleOf(DiagnosticRuleSet set, string ruleId) {
            foreach (var rule in set.Rules) {
                if (string.Equals(rule.RuleId, ruleId, StringComparison.Ordinal)) {
                    return rule;
                }
            }
            return null;
        }

        /// <summary>
        /// The result-TSV lines of one query (no line breaks): one per reported outcome, in result order, then one per
        /// refused rule; a query without an outcome row starts with one row whose Outcome is
        /// <see cref="NoOutcomeName"/>. Nothing formula-derived is written.
        /// </summary>
        public static IReadOnlyList<string> ResultRows(string fileName, RawData raw, DiagnosticQueryEvaluation evaluation, DiagnosticRuleSet set) {
            var r = evaluation.Result;
            var info = evaluation.Info;
            var id = set.Identity;
            var query = new[]
            {
                Cell(fileName),
                Cell(raw?.Name),
                r?.InputSha256 ?? string.Empty,
                raw == null ? string.Empty : Num(raw.PrecursorMz, MzDecimals),
                raw == null ? string.Empty : IonModeName(raw.IonMode),
                Cell(r?.InstrumentType),
                Cell(r?.InstrumentClass),
                Cell(info?.InstrumentTypeSource),
                Cell(info?.PeaksSource),
                Cell(PeaksCheck(info)),
                info == null ? string.Empty : Int(info.NonPositiveDropped),
                Cell(id.Version),
                Cell(id.Sha256),
                SourceName(id.Source),
                DiagnosticRuleEvaluator.EngineSemantics,
                Cell(evaluation.Status),
            };
            var prefix = string.Join("\t", query);
            var conflict = r == null ? string.Empty : Bool(r.RuleConflict);
            var rows = new List<string>();
            if (r == null || r.Outcomes.Count == 0) {
                rows.Add(prefix + "\t" + NoOutcomeName(evaluation.Status) + new string('\t', ResultColumns.Length - query.Length - 1) + conflict);
            }
            if (r == null) {
                return rows;
            }
            foreach (var o in r.Outcomes) {
                var h = Reported(o);
                var cells = new[]
                {
                    OutcomeName(o),
                    Cell(o.RuleId),
                    Int(o.Version),
                    Cell(o.MotifId),
                    Bool(o.AuthorReviewed),
                    Bool(o.ClaimEligible),
                    Cell(string.Join(";", o.ClaimIneligible)),
                    Cell(r.PreconditionMode),
                    Cell(h?.Adduct),
                    Cell(h?.AdductSource),
                    Cell(h?.AdductTier),
                    Cell(h?.HypothesisId),
                    Cell(h?.NeutralFormula),
                    h == null ? string.Empty : Num(h.HypothesisMz, MzDecimals),
                    h == null ? string.Empty : Num(h.PrecursorErrorDa, MzDecimals),
                    Int(o.NHypotheses),
                    Int(o.NHypothesesFired),
                    Cell(string.Join(";", o.TiedHypothesisIds)),
                    h == null ? string.Empty : Num(h.Score, MzDecimals),
                    Cell(MatchedTerms(h)),
                    Cell(VetoTerms(h)),
                    Cell(Branches(o)),
                    Cell(ProductTolerance(o)),
                    Num(o.PrecursorToleranceDa, MzDecimals),
                    conflict,
                };
                rows.Add(prefix + "\t" + string.Join("\t", cells));
            }
            foreach (var x in r.Refusals) {
                var rule = RuleOf(set, x.RuleId);
                var cells = new string[ResultColumns.Length - query.Length];
                for (var i = 0; i < cells.Length; i++) {
                    cells[i] = string.Empty;
                }
                cells[0] = "refused";
                cells[1] = Cell(x.RuleId);
                cells[2] = rule == null ? string.Empty : Int(rule.Version);
                cells[3] = Cell(rule?.Claim.MotifId);
                cells[cells.Length - 1] = conflict;
                rows.Add(prefix + "\t" + string.Join("\t", cells));
            }
            return rows;
        }

        /// <summary><c>term:member@peak_index:mz:err_mDa:rel</c> joined by ';', in canonical order.</summary>
        private static string MatchedTerms(HypothesisOutcome h) {
            if (h == null) {
                return string.Empty;
            }
            var items = new List<string>();
            foreach (var m in h.Matched) {
                items.Add(m.TermId + ":" + m.MemberKey + "@" + Int(m.PeakIndex) + ":" + Num(m.PeakMz, MzDecimals) + ":"
                          + Num(m.ErrorDa * 1000.0, MdaDecimals) + ":" + Num(m.RelIntensity, RelDecimals));
            }
            return string.Join(";", items);
        }

        /// <summary>The vetoes that fired: <c>term@peak_index:mz</c> joined by ';'.</summary>
        private static string VetoTerms(HypothesisOutcome h) {
            if (h == null) {
                return string.Empty;
            }
            var items = new List<string>();
            foreach (var v in h.Vetoes) {
                if (!v.Fired) {
                    continue;
                }
                var text = v.TermId;
                if (v.PeakIndex.HasValue) {
                    text += "@" + Int(v.PeakIndex.Value) + ":" + (v.PeakMz.HasValue ? Num(v.PeakMz.Value, MzDecimals) : string.Empty);
                }
                items.Add(text);
            }
            return string.Join(";", items);
        }

        /// <summary><c>pointer=10</c> (one digit per child) joined by ';'.</summary>
        private static string Branches(RuleOutcome o) {
            var items = new List<string>();
            foreach (var b in o.BranchFired) {
                var sb = new StringBuilder();
                foreach (var child in b.Children) {
                    sb.Append(child ? '1' : '0');
                }
                items.Add(b.Pointer + "=" + sb.ToString());
            }
            return string.Join(";", items);
        }

        private static string ProductTolerance(RuleOutcome o) =>
            "abs_da=" + Num(o.ProductToleranceAbsDa, MzDecimals) + ";ppm=" + Num(o.ProductTolerancePpm, PpmDecimals) + ";source=" + o.ProductToleranceSource;

        /// <summary>
        /// The evidence line of one query: <c>{"file_name":...,"input":{...},"result":&lt;canonical result&gt;|null}</c>.
        /// </summary>
        public static string EvidenceLine(string fileName, DiagnosticQueryEvaluation evaluation) {
            var info = evaluation.Info;
            var sb = new StringBuilder();
            sb.Append("{\"file_name\":");
            CanonicalJson.WriteString(sb, fileName ?? string.Empty);
            sb.Append(",\"input\":{\"instrument_type_source\":");
            JsonOrNull(sb, info?.InstrumentTypeSource);
            sb.Append(",\"peaks_check\":");
            JsonOrNull(sb, PeaksCheck(info));
            sb.Append(",\"peaks_source\":");
            JsonOrNull(sb, info?.PeaksSource);
            sb.Append("},\"result\":");
            sb.Append(evaluation.Result == null ? "null" : DiagnosticResultJson.Write(evaluation.Result));
            sb.Append('}');
            return sb.ToString();
        }

        private static void JsonOrNull(StringBuilder sb, string value) {
            if (value == null) {
                sb.Append("null");
            }
            else {
                CanonicalJson.WriteString(sb, value);
            }
        }

        /// <summary>
        /// The context-TSV lines of one query (report only, design 5.7): one per result-TSV row that has a rule,
        /// joined on (File name, Input sha256, Rule id).
        /// </summary>
        public static IReadOnlyList<string> ContextRows(string fileName, RawData raw, DiagnosticQueryEvaluation evaluation, DiagnosticRuleRuntime runtime,
                                                        DiagnosticQueryContext context) {
            var rows = new List<string>();
            var r = evaluation.Result;
            if (r == null) {
                return rows;
            }
            context = context ?? DiagnosticQueryContext.Empty;
            var set = runtime.RuleSet;
            var topTerm = context.TopFormulaTerm;
            var comparisons = ChemOntAgreement.Compare(r, set, runtime.RankingScope, topTerm, context.BaseTopOntology, context.RankedTopOntology);
            var weight = Num(runtime.RankingWeight, MzDecimals);
            var scope = ScopeName(runtime.RankingScope);
            for (var i = 0; i < r.Outcomes.Count; i++) {
                var o = r.Outcomes[i];
                var c = comparisons[i];
                var h = Reported(o);
                rows.Add(ContextLine(fileName, raw, r, o.RuleId, o.MotifId, context, c.TopFormula, c.BaseRank1, c.RankedRank1,
                                     FormulaEquals(h?.NeutralFormula, context.TopFormula), weight, scope));
            }
            foreach (var x in r.Refusals) {
                var rule = RuleOf(set, x.RuleId);
                rows.Add(ContextLine(fileName, raw, r, x.RuleId, rule?.Claim.MotifId, context, ChemOntAgreement.NotComparable,
                                     ChemOntAgreement.NotComparable, ChemOntAgreement.NotComparable, string.Empty, weight, scope));
            }
            return rows;
        }

        private static string ContextLine(string fileName, RawData raw, DiagnosticRuleResult r, string ruleId, string motifId, DiagnosticQueryContext context,
                                          string fsea, string baseTop, string rankedTop, string formulaEquals, string weight, string scope) {
            var cells = new[]
            {
                Cell(fileName),
                r.InputSha256,
                Cell(ruleId),
                Cell(motifId),
                Cell(raw?.PrecursorType),
                Cell(context.TopFormulaTerm),
                fsea,
                Cell(context.BaseTopOntology),
                baseTop,
                Cell(context.RankedTopOntology),
                rankedTop,
                formulaEquals,
                context.CandidatesMatching(motifId),
                weight,
                scope,
            };
            return string.Join("\t", cells);
        }

        private static string FormulaEquals(string hypothesisFormula, string topFormula) =>
            string.IsNullOrEmpty(hypothesisFormula) || string.IsNullOrEmpty(topFormula) ? string.Empty : Bool(string.Equals(hypothesisFormula, topFormula, StringComparison.Ordinal));

        /// <summary>
        /// The structure-result rows in rule-adjusted order (w &gt; 0): the legacy columns with their legacy
        /// formatting, then the appended columns. The legacy filter <c>TotalScore &lt;= 0</c> is applied to the base
        /// score.
        /// </summary>
        public static void WriteStructureRows(TextWriter sw, RawData rawData, DiagnosticRanking ranking, RuleSetIdentity identity) {
            var fired = new HashSet<string>(ranking.FiredMotifs, StringComparer.Ordinal);
            var vetoes = new HashSet<string>(ranking.VetoMotifs, StringComparer.Ordinal);
            foreach (var row in ranking.Ranked) {
                var result = row.Candidate;
                if (result.TotalScore <= 0) continue;
                LegacyStructureColumns.Write(sw, rawData, result);
                sw.WriteLine(AppendedCells(row, ranking.Status, fired, vetoes, identity));
            }
        }

        /// <summary>
        /// The GUI batch-export structure rows in rule-adjusted order (w &gt; 0): the first <paramref name="count"/>
        /// candidates, unfiltered like the legacy GUI writer, with the legacy columns and then the appended ones.
        /// </summary>
        public static void WriteGuiStructureRows(TextWriter sw, RawData rawData, DiagnosticRanking ranking, RuleSetIdentity identity, int count) {
            var fired = new HashSet<string>(ranking.FiredMotifs, StringComparer.Ordinal);
            var vetoes = new HashSet<string>(ranking.VetoMotifs, StringComparer.Ordinal);
            var n = Math.Min(Math.Max(count, 0), ranking.Ranked.Count);
            for (var i = 0; i < n; i++) {
                var row = ranking.Ranked[i];
                LegacyStructureColumns.Write(sw, rawData, row.Candidate);
                sw.WriteLine(AppendedCells(row, ranking.Status, fired, vetoes, identity));
            }
        }

        /// <summary>
        /// The GUI per-file structure rows in rule-adjusted order (w &gt; 0): the legacy columns (Rank is the new
        /// rank) and then the appended ones; every candidate, like the legacy writer.
        /// </summary>
        public static void WriteEachFileStructureRows(TextWriter sw, DiagnosticRanking ranking, RuleSetIdentity identity) {
            var fired = new HashSet<string>(ranking.FiredMotifs, StringComparer.Ordinal);
            var vetoes = new HashSet<string>(ranking.VetoMotifs, StringComparer.Ordinal);
            for (var i = 0; i < ranking.Ranked.Count; i++) {
                var row = ranking.Ranked[i];
                LegacyStructureColumns.WriteEachFile(sw, i + 1, row.Candidate);
                sw.WriteLine(AppendedCells(row, ranking.Status, fired, vetoes, identity));
            }
        }

        private static string AppendedCells(RankedCandidate row, string status, HashSet<string> fired, HashSet<string> vetoes, RuleSetIdentity identity) {
            var cells = new[]
            {
                string.Empty,
                Num(row.RankingScore, MzDecimals),
                Int(row.BaseRank),
                Int(row.Term),
                string.Join(";", row.MatchedMotifs.Where(fired.Contains)),
                string.Join(";", row.MatchedMotifs.Where(vetoes.Contains)),
                status,
                Cell(identity.Version),
                Cell(identity.Sha256),
            };
            return string.Join("\t", cells);
        }

        /// <summary><c>Diagnostic rules: &lt;version&gt; (&lt;sha12&gt;, embedded|directory) evaluated N, fired F,
        /// veto_blocked V, none X, skipped S, refused R, errors E</c>.</summary>
        public static string SummaryLine(RuleSetIdentity identity, DiagnosticRunCounts counts) {
            var sha = identity.Sha256 ?? string.Empty;
            return "Diagnostic rules: " + identity.Version + " (" + (sha.Length > 12 ? sha.Substring(0, 12) : sha) + ", " + SourceName(identity.Source)
                   + ") evaluated " + Int(counts.Evaluated) + ", fired " + Int(counts.Fired) + ", veto_blocked " + Int(counts.VetoBlocked)
                   + ", none " + Int(counts.None) + ", skipped " + Int(counts.Skipped) + ", refused " + Int(counts.Refused)
                   + ", errors " + Int(counts.Errors);
        }

        /// <summary>
        /// The canonical options of the evaluator, as the result <c>options</c> object writes them. They are read from
        /// the result of an empty spectrum, so a run with no evaluated query still records them.
        /// </summary>
        public static JsonObject CanonicalOptions(DiagnosticRuleEvaluator evaluator) {
            var probe = evaluator.Evaluate(new DiagnosticSpectrum(new DiagnosticPeak[0], 0.0, null, null));
            var members = new List<KeyValuePair<string, JsonValue>>();
            foreach (var o in probe.Options) {
                members.Add(M(o.Key, OptionValue(o.Key, o.Value)));
            }
            return new JsonObject(members);
        }

        /// <summary>Typed as in the canonical result (DiagnosticResultJson): enum options as strings, strict and
        /// verbose as booleans, numbers as their invariant or ExactDecimal text.</summary>
        private static JsonValue OptionValue(string key, string text) {
            switch (key) {
                case "filter":
                case "overlap":
                case "loss_reference_override":
                    return JsonValue.Of(text);
                case "strict":
                case "verbose":
                    return JsonValue.Of(string.Equals(text, "true", StringComparison.Ordinal));
                default:
                    return RawNumber(text);
            }
        }

        /// <summary>Lowercase hex sha256 of the ASCII bytes of <paramref name="text"/>.</summary>
        public static string Sha256Hex(string text) {
            byte[] hash;
            using (var sha = SHA256.Create()) {
                hash = sha.ComputeHash(Encoding.ASCII.GetBytes(text));
            }
            const string digits = "0123456789abcdef";
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) {
                sb.Append(digits[b >> 4]);
                sb.Append(digits[b & 15]);
            }
            return sb.ToString();
        }

        /// <summary>The canonical run manifest (design 5.5 item 4).</summary>
        public static string RunManifest(DiagnosticRuleRun run) {
            var runtime = run.Runtime;
            var id = runtime.RuleSet.Identity;
            var counts = run.Counts;
            var options = CanonicalOptions(runtime.Evaluator);
            var files = new List<JsonValue>();
            foreach (var f in id.Files) {
                files.Add(Obj(M("path", JsonValue.Of(f.Path)), M("sha256", JsonValue.Of(f.Sha256))));
            }
            var skipped = new List<KeyValuePair<string, JsonValue>>();
            foreach (var s in counts.SkippedByReason) {
                skipped.Add(M(s.Key, JsonValue.Of(s.Value)));
            }
            var refusals = new List<JsonValue>();
            foreach (var x in runtime.Evaluator.Refusals) {
                refusals.Add(Obj(M("code", JsonValue.Of(x.Code)), M("rule_id", JsonValue.Of(x.RuleId))));
            }
            var settings = runtime.Settings;
            var names = run.Names;
            var timing = run.TimingTotals();
            var manifest = Obj(
                M("build_identity", JsonValue.Of(run.BuildIdentity ?? string.Empty)),
                M("counts", Obj(
                    M("conflicts", JsonValue.Of(counts.Conflicts)),
                    M("eligible_conflicts", JsonValue.Of(counts.EligibleConflicts)),
                    M("errors", JsonValue.Of(counts.Errors)),
                    M("evaluated", JsonValue.Of(counts.Evaluated)),
                    M("fired", JsonValue.Of(counts.Fired)),
                    M("none", JsonValue.Of(counts.None)),
                    M("queries", JsonValue.Of(counts.Queries)),
                    M("refused", JsonValue.Of(counts.Refused)),
                    M("skipped", JsonValue.Of(counts.Skipped)),
                    M("skipped_by_reason", new JsonObject(skipped)),
                    M("veto_blocked", JsonValue.Of(counts.VetoBlocked)))),
                M("engine_semantics", JsonValue.Of(DiagnosticRuleEvaluator.EngineSemantics)),
                M("identity", JsonValue.Of(runtime.Evaluator.IdentityString)),
                M("legacy_date_string", JsonValue.Of(run.LegacyDateString)),
                M("options", options),
                M("options_sha256", JsonValue.Of(Sha256Hex(ToCanonicalText(options)))),
                M("outputs", Obj(
                    M("context", JsonValue.Of(names.Context)),
                    M("evidence", JsonValue.Of(names.Evidence)),
                    M("result", JsonValue.Of(names.Result)),
                    M("run", JsonValue.Of(names.Run)),
                    M("timing", run.WritesTiming ? JsonValue.Of(names.Timing) : JsonValue.Null))),
                M("refusals", new JsonArray(refusals)),
                M("rule_set", Obj(
                    M("files", new JsonArray(files)),
                    M("pinned", JsonValue.Of(id.Pinned)),
                    M("sha256", JsonValue.Of(id.Sha256)),
                    M("source", JsonValue.Of(SourceName(id.Source))),
                    M("version", JsonValue.Of(id.Version)))),
                M("settings", Obj(
                    M("evaluation", JsonValue.Of(true)),
                    M("instrument_type", JsonValue.Of(settings.InstrumentType ?? string.Empty)),
                    M("ranking_scope", JsonValue.Of(ScopeName(settings.RankingScope))),
                    M("ranking_weight", JsonNum(settings.RankingWeight, MzDecimals)),
                    M("strict", JsonValue.Of(settings.Strict)))),
                M("stamp", JsonValue.Of(run.Stamp)),
                M("timing", Obj(
                    M("eval_ms", JsonNum(timing[0], DiagnosticTimingLog.MsDecimals)),
                    M("formula_step_ms", JsonNum(timing[2], DiagnosticTimingLog.MsDecimals)),
                    M("match_ms", JsonNum(timing[1], DiagnosticTimingLog.MsDecimals)),
                    M("queries", JsonValue.Of(counts.Queries)),
                    M("structure_step_ms", JsonNum(timing[3], DiagnosticTimingLog.MsDecimals)))));
            return ToCanonicalText(manifest);
        }

        private static JsonValue JsonNum(double value, int decimals) {
            var text = ExactDecimal.FormatOrNull(value, decimals);
            return text == null ? JsonValue.Null : RawNumber(text);
        }

        private const string RawMarker = "\u0001";

        private static readonly Regex RawPattern = new Regex("\"\\\\u0001([^\"]*)\"", RegexOptions.CultureInvariant);

        /// <summary>A number written as given (fixed decimals): a marked string that <see cref="ToCanonicalText"/>
        /// unquotes, because the engine's verbatim number value is internal to it.</summary>
        private static JsonValue RawNumber(string text) => JsonValue.Of(RawMarker + text);

        /// <summary>The canonical JSON text with the <see cref="RawNumber"/> values unquoted.</summary>
        public static string ToCanonicalText(JsonValue value) => RawPattern.Replace(CanonicalJson.ToText(value), "$1");

        private static KeyValuePair<string, JsonValue> M(string key, JsonValue value) => new KeyValuePair<string, JsonValue>(key, value);

        private static JsonObject Obj(params KeyValuePair<string, JsonValue>[] members) => new JsonObject(members);
    }

    /// <summary>
    /// The legacy console structure-result columns, written exactly as PredictProcess.writeStructureRow writes them
    /// (current-culture default formatting; both MS-FINDER processes fix the invariant culture). Kept out of
    /// <see cref="DiagnosticRuleExport"/> so the glue IL scan can ban double formatting there.
    /// </summary>
    internal static class LegacyStructureColumns
    {
        /// <summary>Writes the 14 legacy columns of one row, without the line break.</summary>
        public static void Write(TextWriter sw, RawData rawData, FragmenterResult result) {
            var filepath = rawData.RawdataFilePath;
            var filename = System.IO.Path.GetFileNameWithoutExtension(rawData.RawdataFilePath);
            sw.Write(filepath + "\t");
            sw.Write(filename + "\t");
            sw.Write(rawData.Name + "\t");
            sw.Write(rawData.Ms1PeakNumber + "\t");
            sw.Write(rawData.Ms2PeakNumber + "\t");
            sw.Write(rawData.PrecursorMz + "\t");
            sw.Write(rawData.PrecursorType + "\t");
            sw.Write(
                result.Title + "\t" +
                result.TotalScore + "\t" +
                result.Resources + "\t" +
                result.Formula + "\t" +
                result.Ontology + "\t" +
                result.Inchikey + "\t" +
                result.Smiles);
        }

        /// <summary>Writes the 10 legacy columns of one GUI per-file row (ExportUtility.writeStructureResult), without
        /// the line break.</summary>
        public static void WriteEachFile(TextWriter sw, int rank, FragmenterResult result) {
            sw.Write(rank.ToString() + "\t" + result.Title + "\t" + result.TotalScore + "\t" + result.RetentionTime + "\t" +
                result.RetentionIndex + "\t" + result.Resources + "\t" + result.Formula + "\t" + result.Ontology + "\t" + result.Inchikey + "\t" + result.Smiles);
        }
    }

    /// <summary>What the context file needs from the formula and structure step of a query (report only).</summary>
    public sealed class DiagnosticQueryContext
    {
        public static readonly DiagnosticQueryContext Empty = new DiagnosticQueryContext();

        /// <summary><c>ChemicalOntologyDescriptions[0]</c> of the top formula (FSEA), or null.</summary>
        public string TopFormulaTerm { get; set; }
        /// <summary>The top formula string, or null.</summary>
        public string TopFormula { get; set; }
        /// <summary>The Ontology of the base rank-1 candidate with TotalScore &gt; 0, or null.</summary>
        public string BaseTopOntology { get; set; }
        /// <summary>The Ontology of the ranked rank-1 candidate with TotalScore &gt; 0 (the base one when w = 0).</summary>
        public string RankedTopOntology { get; set; }
        /// <summary>Candidates (TotalScore &gt; 0) whose structure has the motif, by motif id; null when w = 0 (OQ07)
        /// or when the query reached no structure step.</summary>
        public IDictionary<string, int> MotifCounts { get; set; }

        public string CandidatesMatching(string motifId) {
            if (MotifCounts == null || motifId == null || !MotifCounts.TryGetValue(motifId, out var n)) {
                return string.Empty;
            }
            return n.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>The context of a query whose formula results (legacy order) and structure candidates (base order)
        /// are known.</summary>
        public static DiagnosticQueryContext Of(IReadOnlyList<FormulaResult> formulaResults, IReadOnlyList<FragmenterResult> baseOrder, DiagnosticRanking ranking) {
            var context = new DiagnosticQueryContext();
            var top = formulaResults != null && formulaResults.Count > 0 ? formulaResults[0] : null;
            if (top != null) {
                var terms = top.ChemicalOntologyDescriptions;
                context.TopFormulaTerm = terms != null && terms.Count > 0 ? terms[0] : null;
                context.TopFormula = top.Formula?.FormulaString;
            }
            if (baseOrder != null) {
                context.BaseTopOntology = baseOrder.FirstOrDefault(c => c.TotalScore > 0)?.Ontology;
                context.RankedTopOntology = ranking == null
                    ? context.BaseTopOntology
                    : ranking.Ranked.Select(c => c.Candidate).FirstOrDefault(c => c.TotalScore > 0)?.Ontology;
            }
            return context;
        }
    }

    /// <summary>One query of a run: its evaluation and what the export loop learns about it later.</summary>
    public sealed class DiagnosticRuleQuery
    {
        internal DiagnosticRuleQuery(object key, string fileName, RawData raw, DiagnosticQueryEvaluation evaluation, double evalMs) {
            Key = key;
            FileName = fileName;
            Raw = raw;
            Evaluation = evaluation;
            EvalMs = evalMs;
        }

        internal object Key { get; }
        public string FileName { get; }
        public RawData Raw { get; }
        public DiagnosticQueryEvaluation Evaluation { get; }
        /// <summary>The evaluation threw under Strict (OQ08): the run stops.</summary>
        public bool Failed { get; internal set; }
        internal double EvalMs { get; }
        internal double? MatchMs { get; set; }
        internal int? Candidates { get; set; }
        internal int? UniqueSmiles { get; set; }
        internal long? CacheHits { get; set; }
        internal DiagnosticQueryContext Context { get; set; }
    }

    /// <summary>What a run takes from its environment; tests fix it so outputs are byte-comparable.</summary>
    public sealed class DiagnosticRunEnvironment
    {
        public string BuildIdentity { get; set; } = MsdialBuildIdentity.FullIdentity;
        /// <summary>A monotonic timestamp in <see cref="Frequency"/> ticks per second.</summary>
        public Func<long> Clock { get; set; } = Stopwatch.GetTimestamp;
        public long Frequency { get; set; } = Stopwatch.Frequency;
        /// <summary>MS-FINDER's GC-EI derivative flag (queries are then skipped).</summary>
        public bool IsTmsMeoxDerivative { get; set; }
    }

    /// <summary>
    /// The diagnostic-rule part of one console <c>predict</c> run with evaluation on (design 5.3, 5.5, 5.8): the
    /// runtime, the stamp, the new files and the counts. The export loop evaluates every query sequentially in query
    /// order, ranks after the legacy sort, and completes the query; <see cref="Finish"/> writes the timing file and
    /// the run manifest and returns the summary line.
    /// </summary>
    public sealed class DiagnosticRuleRun : IDisposable
    {
        private readonly string _folder;
        private readonly DiagnosticRunEnvironment _environment;
        private readonly List<DiagnosticTimingRow> _timingRows = new List<DiagnosticTimingRow>();
        private StreamWriter _result;
        private StreamWriter _evidence;
        private StreamWriter _context;

        private DiagnosticRuleRun(DiagnosticRuleRuntime runtime, string folder, string stamp, long legacyDateString, DiagnosticRunEnvironment environment) {
            Runtime = runtime;
            _folder = folder;
            Stamp = stamp;
            LegacyDateString = legacyDateString;
            _environment = environment;
            Names = new DiagnosticOutputNames(stamp);
            Counts = new DiagnosticRunCounts { Refused = runtime.Evaluator.Refusals.Select(r => r.RuleId).Distinct(StringComparer.Ordinal).Count() };
        }

        public DiagnosticRuleRuntime Runtime { get; }
        public string Stamp { get; }
        public long LegacyDateString { get; }
        public DiagnosticOutputNames Names { get; }
        public DiagnosticRunCounts Counts { get; }
        public DiagnosticTimingLog Timing { get; } = new DiagnosticTimingLog();
        public string BuildIdentity => _environment.BuildIdentity;
        /// <summary>The ranking weight is above zero: the structure result gets the appended columns.</summary>
        public bool RankingOn => Runtime.RankingOn;
        /// <summary>Whether <see cref="Finish"/> writes the timing file; the GUI exports do not (OQ09), and their
        /// run manifest then has timing: null among the outputs.</summary>
        public bool WritesTiming { get; set; } = true;

        /// <summary>
        /// Creates the runtime from the parameter's settings and checks that none of the new files exists. Returns
        /// null with the error (<c>rule_set_error:...</c> or <c>error:output_exists:&lt;file name&gt;</c>) otherwise.
        /// Call it only when evaluation is on.
        /// </summary>
        public static DiagnosticRuleRun Start(AnalysisParamOfMsfinder param, string outputFolder, DateTime started, long legacyDateString, out string error) {
            var runtime = DiagnosticRuleRuntime.Create(param.DiagnosticRules, DiagnosticRuleRuntime.ToleranceOf(param), out error);
            if (runtime == null) {
                return null;
            }
            var environment = new DiagnosticRunEnvironment { IsTmsMeoxDerivative = param.IsTmsMeoxDerivative };
            return Start(runtime, outputFolder, DiagnosticRuleExport.Stamp(started), legacyDateString, environment, out error);
        }

        /// <summary>Starts a run on an existing runtime; returns null with <c>error:output_exists:&lt;file name&gt;</c>
        /// when a new file exists.</summary>
        public static DiagnosticRuleRun Start(DiagnosticRuleRuntime runtime, string outputFolder, string stamp, long legacyDateString,
                                             DiagnosticRunEnvironment environment, out string error) {
            if (runtime == null) {
                throw new ArgumentNullException(nameof(runtime));
            }
            error = null;
            foreach (var name in new DiagnosticOutputNames(stamp).All) {
                if (File.Exists(Path.Combine(outputFolder, name))) {
                    error = DiagnosticRuleExport.OutputExists + ":" + name;
                    return null;
                }
            }
            return new DiagnosticRuleRun(runtime, outputFolder, stamp, legacyDateString, environment ?? new DiagnosticRunEnvironment());
        }

        private static StreamWriter Create(string path) {
            var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            return new StreamWriter(stream, Encoding.ASCII) { NewLine = DiagnosticRuleExport.NewLine };
        }

        private bool TryCreate(string name, out StreamWriter writer, out string error) {
            writer = null;
            error = null;
            var path = Path.Combine(_folder, name);
            try {
                writer = Create(path);
                return true;
            }
            catch (Exception e) when (IsOutputFailure(e)) {
                error = OutputError(e, path, name);
                return false;
            }
        }

        /// <summary>A file-system failure to create an output file (an existing file, a read-only or missing folder),
        /// reported as an error line instead of escaping into MS-FINDER.</summary>
        private static bool IsOutputFailure(Exception e) => e is IOException || e is UnauthorizedAccessException;

        private static string OutputError(Exception e, string path, string name) =>
            File.Exists(path) ? DiagnosticRuleExport.OutputExists + ":" + name : "error:output_io:" + e.GetType().Name + ":" + name;

        /// <summary>Opens the result, evidence and context files (CreateNew) and writes the headers. On failure
        /// nothing stays open, the files this call created are removed, and the error names the file.</summary>
        public bool OpenWriters(out string error) {
            if (!TryCreate(Names.Result, out _result, out error)
                || !TryCreate(Names.Evidence, out _evidence, out error)
                || !TryCreate(Names.Context, out _context, out error)) {
                var created = new[] { _result != null ? Names.Result : null, _evidence != null ? Names.Evidence : null };
                Dispose();
                foreach (var name in created) {
                    if (name != null) {
                        File.Delete(Path.Combine(_folder, name));
                    }
                }
                return false;
            }
            _result.WriteLine(DiagnosticRuleExport.ResultHeader);
            _context.WriteLine(DiagnosticRuleExport.ContextHeader);
            return true;
        }

        private double Ms(long start, long end) => (end - start) * 1000.0 / _environment.Frequency;

        /// <summary>Evaluates one query (sequentially, in query order); every query, with or without a formula
        /// result.</summary>
        public DiagnosticRuleQuery Evaluate(MsfinderQueryFile file, RawData raw) => Evaluate(file, Path.GetFileName(file.RawDataFilePath ?? string.Empty), raw);

        public DiagnosticRuleQuery Evaluate(object key, string fileName, RawData raw) {
            var start = _environment.Clock();
            DiagnosticQueryEvaluation evaluation;
            var failed = false;
            try {
                evaluation = Runtime.Evaluate(raw, _environment.IsTmsMeoxDerivative);
            }
            catch (Exception e) {
                // Only under Strict does the runtime rethrow (OQ08).
                evaluation = new DiagnosticQueryEvaluation(DiagnosticRuleRuntime.ErrorStatusPrefix + e.GetType().Name, null, null);
                failed = true;
            }
            var query = new DiagnosticRuleQuery(key, fileName, raw, evaluation, Ms(start, _environment.Clock())) { Failed = failed };
            return query;
        }

        /// <summary>
        /// Records the formula results (legacy order) and the structure candidates (base order) of a query and, when
        /// the weight is above zero, ranks them. Returns the ranking, or null when w = 0 (legacy rows).
        /// </summary>
        public DiagnosticRanking Rank(DiagnosticRuleQuery query, IReadOnlyList<FormulaResult> formulaResults, IReadOnlyList<FragmenterResult> baseOrder) {
            DiagnosticRanking ranking = null;
            IDictionary<string, int> motifCounts = null;
            query.Candidates = baseOrder?.Count;
            if (baseOrder != null) {
                query.UniqueSmiles = new HashSet<string>(baseOrder.Select(c => c.Smiles ?? string.Empty), StringComparer.Ordinal).Count;
            }
            var matcher = Runtime.Matcher;
            if (matcher != null && baseOrder != null) {
                var start = _environment.Clock();
                var hits = matcher.CacheHits;
                ranking = MsfinderDiagnosticRanking.Rank(baseOrder, query.Evaluation.Result, Runtime);
                motifCounts = MotifCounts(query.Evaluation.Result, baseOrder);
                query.CacheHits = matcher.CacheHits - hits;
                query.MatchMs = Ms(start, _environment.Clock());
            }
            var context = DiagnosticQueryContext.Of(formulaResults, baseOrder, ranking);
            context.MotifCounts = motifCounts;
            query.Context = context;
            return ranking;
        }

        /// <summary>Candidates with TotalScore &gt; 0 whose structure has each reported rule's motif.</summary>
        private IDictionary<string, int> MotifCounts(DiagnosticRuleResult result, IReadOnlyList<FragmenterResult> baseOrder) {
            var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
            if (result == null) {
                return counts;
            }
            var motifs = result.Outcomes.Select(o => o.MotifId).ToList();
            foreach (var x in result.Refusals) {
                var rule = Runtime.RuleSet.Rules.FirstOrDefault(r => string.Equals(r.RuleId, x.RuleId, StringComparison.Ordinal));
                if (rule != null) {
                    motifs.Add(rule.Claim.MotifId);
                }
            }
            var matcher = Runtime.Matcher;
            foreach (var motif in motifs) {
                if (motif == null || counts.ContainsKey(motif) || !matcher.HasMotif(motif)) {
                    continue;
                }
                counts[motif] = baseOrder.Count(c => c.TotalScore > 0 && matcher.Matches(c.Smiles, motif));
            }
            return counts;
        }

        /// <summary>Writes the query's result, evidence and context rows and records its counts and timing row.</summary>
        public void Complete(DiagnosticRuleQuery query) {
            var e = query.Evaluation;
            foreach (var line in DiagnosticRuleExport.ResultRows(query.FileName, query.Raw, e, Runtime.RuleSet)) {
                _result.WriteLine(line);
            }
            _evidence.WriteLine(DiagnosticRuleExport.EvidenceLine(query.FileName, e));
            foreach (var line in DiagnosticRuleExport.ContextRows(query.FileName, query.Raw, e, Runtime, query.Context)) {
                _context.WriteLine(line);
            }
            Counts.Add(e);
            var r = e.Result;
            _timingRows.Add(new DiagnosticTimingRow
            {
                FileName = query.FileName,
                Ms2Peaks = query.Raw?.Ms2PeakNumber ?? 0,
                RulesEvaluated = r?.RulesEvaluated,
                HypothesesTested = r?.Outcomes.Sum(o => o.NHypotheses),
                Outcomes = r?.Outcomes.Count,
                Candidates = query.Candidates,
                UniqueSmiles = query.UniqueSmiles,
                CacheHits = query.CacheHits,
                EvalMs = query.EvalMs,
                MatchMs = query.MatchMs,
                FormulaStepMs = Timing.FormulaStepMs(query.Key),
                StructureStepMs = Timing.StructureStepMs(query.Key),
            });
        }

        /// <summary>The totals of eval, match, formula-step and structure-step milliseconds.</summary>
        internal double[] TimingTotals() {
            var totals = new double[4];
            foreach (var row in _timingRows) {
                totals[0] += row.EvalMs ?? 0.0;
                totals[1] += row.MatchMs ?? 0.0;
                totals[2] += row.FormulaStepMs ?? 0.0;
                totals[3] += row.StructureStepMs ?? 0.0;
            }
            return totals;
        }

        /// <summary>Closes the row files, writes the timing file and the run manifest (CreateNew), and returns the
        /// console summary line. A file that cannot be created (one that appeared after <c>Start</c>
        /// checked, or a read-only folder) is never overwritten: the line is then <c>Diagnostic rules: error:...</c>
        /// naming the file, and the manifest is not written.</summary>
        public string Finish() {
            Dispose();
            string name = null;
            try {
                if (WritesTiming) {
                    name = Names.Timing;
                    using (var timing = Create(Path.Combine(_folder, name))) {
                        DiagnosticTimingLog.Write(timing, _timingRows);
                    }
                }
                name = Names.Run;
                using (var run = Create(Path.Combine(_folder, name))) {
                    run.Write(DiagnosticRuleExport.RunManifest(this));
                    run.Write(DiagnosticRuleExport.NewLine);
                }
            }
            catch (Exception e) when (IsOutputFailure(e)) {
                return "Diagnostic rules: " + OutputError(e, Path.Combine(_folder, name), name);
            }
            return DiagnosticRuleExport.SummaryLine(Runtime.RuleSet.Identity, Counts);
        }

        /// <summary>Stops the run after a Strict failure: closes the files and returns the error line.</summary>
        public string Abort(DiagnosticRuleQuery query) {
            Dispose();
            return "Diagnostic rules (Strict): " + query.Evaluation.Status + " in " + DiagnosticRuleExport.Cell(query.FileName);
        }

        public void Dispose() {
            _result?.Dispose();
            _evidence?.Dispose();
            _context?.Dispose();
            _result = null;
            _evidence = null;
            _context = null;
        }
    }
}

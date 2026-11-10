using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt.Tests
{
    /// <summary>
    /// SPEC 13.2 and 13.3: the C# check reproduces the frozen Python reference on the 2026-10-15 demo, object by object,
    /// and reads every eligible name of the evaluation tables as the reference parser did.
    /// </summary>
    /// <remarks>
    /// The inputs are the harness tables the Python saw (RT, m/z and height as exported), so the comparison isolates the
    /// algorithm from the small differences between in-process and exported values. The reference outcomes are
    /// demo_outcomes_{pos,neg}[_samples].tsv (ClassLevelPolicy Consistent, the frozen default) and the full-precision
    /// files parity/demo_parity_{consistent,corroborated}_*.tsv written by running the frozen reference with each policy.
    ///
    /// The files are not in the repository; set MSDIAL_LIPIDRT_REFERENCE_DIR to a folder holding them (directly or in
    /// "tables" and "parity"). Without it these tests are skipped as inconclusive.
    /// </remarks>
    [TestClass]
    public class LipidRtDemoParityTests
    {
        public TestContext TestContext { get; set; }

        private static readonly string[] Datasets = { "demo_pos", "demo_neg", "demo_pos_samples", "demo_neg_samples" };

        private static void RequireReference() {
            if (!LipidRtReferenceTables.IsAvailable) {
                Assert.Inconclusive($"Set {LipidRtReferenceTables.EnvironmentVariable} to the folder of the lipid RT reference tables to run this test.");
            }
        }

        private static LipidRtCheckParameter ParameterOf(LipidRtClassLevelPolicy policy) {
            return new LipidRtCheckParameter { ClassLevelPolicy = policy }; // preset hp = HighPrecision, the other values are the frozen defaults
        }

        private static Dictionary<string, LipidRtObjectResult> RunSingle(string dataset, LipidRtClassLevelPolicy policy, ILipidRtPlausibilityProfile profile) {
            var check = new LipidRtCheck(ParameterOf(policy), profile);
            var results = new Dictionary<string, LipidRtObjectResult>(StringComparer.Ordinal);
            foreach (var unit in LipidRtReferenceTables.LoadUnits(dataset)) {
                var result = check.Evaluate(unit.Input);
                foreach (var r in result.Results) {
                    results[unit.SpotIdById[r.Id]] = r;
                }
            }
            return results;
        }

        private static Dictionary<string, LipidRtObjectResult> RunPaired(string dataset, LipidRtClassLevelPolicy policy) {
            var parameter = ParameterOf(policy);
            var check = new LipidRtCheck(parameter, LipidRtChainRuleProfile.Mammalian);
            var pos = LipidRtReferenceTables.LoadUnits("demo_pos").Single();
            var neg = LipidRtReferenceTables.LoadUnits("demo_neg").Single();
            var paired = LipidRtCorroboration.CrossPolarity(check.EvaluateSingleRun(pos.Input), check.EvaluateSingleRun(neg.Input), parameter);
            var (unit, result) = dataset == "demo_pos" ? (pos, paired.Positive) : (neg, paired.Negative);
            return result.Results.ToDictionary(r => unit.SpotIdById[r.Id], r => r, StringComparer.Ordinal);
        }

        private sealed class Tally
        {
            public int Compared;
            public int Agreed;
            public readonly List<string> Differences = new List<string>();

            public void Check(bool agrees, string what) {
                Compared++;
                if (agrees) {
                    Agreed++;
                }
                else if (Differences.Count < 40) {
                    Differences.Add(what);
                }
            }
        }

        private void Report(string label, Tally tally) {
            TestContext.WriteLine($"{label}: {tally.Agreed}/{tally.Compared} agree");
            foreach (var d in tally.Differences) {
                TestContext.WriteLine("  " + d);
            }
        }

        private static bool ScoreAgrees(string expected, double? actual, double tolerance) {
            if (string.IsNullOrEmpty(expected)) {
                return !actual.HasValue;
            }
            return actual.HasValue && Math.Abs(LipidRtReferenceTables.ParseDouble(expected) - actual.Value) <= tolerance;
        }

        /// <summary>The tolerance of a value the reference wrote with "%.4g".</summary>
        private static double FourSignificant(string expected) {
            var value = Math.Abs(LipidRtReferenceTables.ParseDouble(expected));
            if (value == 0 || double.IsNaN(value)) {
                return 1e-9;
            }
            return 0.5 * Math.Pow(10, Math.Floor(Math.Log10(value)) - 3) + 1e-9;
        }

        private static bool FeatureAgrees(string expected, LipidRtEvidence evidence) {
            if (string.IsNullOrEmpty(expected)) {
                return string.IsNullOrEmpty(evidence.Feature);
            }
            // "TG 52:1;O2@12.34": the composition exactly, the RT to the printed precision (the two runtimes round
            // exact binary ties differently)
            var at = expected.LastIndexOf('@');
            var actualAt = evidence.Feature.LastIndexOf('@');
            return actualAt > 0 && expected.Substring(0, at) == evidence.Feature.Substring(0, actualAt)
                && Math.Abs(LipidRtReferenceTables.ParseDouble(expected.Substring(at + 1)) - evidence.FeatureRt) <= 0.0051;
        }

        [TestMethod]
        public void SingleRunMatchesTheFrozenReferenceOutcomes() {
            RequireReference();
            var total = new Tally();
            foreach (var dataset in Datasets) {
                var reference = LipidRtReferenceTables.ReadTsv(LipidRtReferenceTables.Locate("demo_outcomes_" + dataset.Substring(5) + ".tsv"));
                var off = RunSingle(dataset, LipidRtClassLevelPolicy.Consistent, null);
                var mammalian = RunSingle(dataset, LipidRtClassLevelPolicy.Consistent, LipidRtChainRuleProfile.Mammalian);
                var spotIds = LipidRtReferenceTables.ReadTsv(LipidRtReferenceTables.Locate(dataset + ".tsv"))
                    .ToDictionary(r => (r["unit"], r["row_id"]), r => r["spot_id"]);
                var tally = new Tally();
                var counts = new Dictionary<LipidRtOutcome, int>();
                foreach (var row in reference) {
                    var key = row["alignment_id"].Length > 0 ? ("alignment", row["alignment_id"]) : (row["file"], row["peak_id"]);
                    var spotId = spotIds[key];
                    var o = off[spotId];
                    var m = mammalian[spotId];
                    var label = $"{dataset} {spotId} {row["name"]}";
                    tally.Check(LipidRtReferenceTables.OutcomeOf(row["outcome_off"]) == o.Outcome && LipidRtReferenceTables.ReasonOf(row["code_off"]) == o.Reason,
                        $"{label}: off {row["outcome_off"]}/{row["code_off"]} vs {o.Outcome}/{o.Reason}");
                    tally.Check(LipidRtReferenceTables.OutcomeOf(row["outcome_mammalian"]) == m.Outcome && LipidRtReferenceTables.ReasonOf(row["code_mammalian"]) == m.Reason,
                        $"{label}: mammalian {row["outcome_mammalian"]}/{row["code_mammalian"]} vs {m.Outcome}/{m.Reason}");
                    tally.Check(ScoreAgrees(row["score_mammalian"], m.Score, FourSignificant(row["score_mammalian"])),
                        $"{label}: score {row["score_mammalian"]} vs {m.Score}");
                    if (row["eligible"] == "1" && row["code_mammalian"] != "ne_gate") {
                        tally.Check(FeatureAgrees(row["feature"], m.Evidence), $"{label}: feature {row["feature"]} vs {m.Evidence.Feature}");
                        tally.Check(LipidRtReferenceTables.StatusOf(row["status"]) == m.Evidence.Status, $"{label}: status {row["status"]} vs {m.Evidence.Status}");
                        tally.Check(Math.Abs(LipidRtReferenceTables.ParseDouble(row["deficit"]) - m.Evidence.Deficit) < 1e-9, $"{label}: deficit {row["deficit"]} vs {m.Evidence.Deficit}");
                        tally.Check(row["resid_db"].Length == 0 ? !m.Evidence.ResidualDoubleBondSteps.HasValue
                            : m.Evidence.ResidualDoubleBondSteps.HasValue && Math.Abs(LipidRtReferenceTables.ParseDouble(row["resid_db"]) - m.Evidence.ResidualDoubleBondSteps.Value) <= 0.0051,
                            $"{label}: resid_db {row["resid_db"]} vs {m.Evidence.ResidualDoubleBondSteps}");
                        var expectedArtefacts = new SortedSet<string>(row["artefact"].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
                        var actualArtefacts = new SortedSet<string>(m.Evidence.Artefacts.Select(a => a.Kind), StringComparer.Ordinal);
                        tally.Check(expectedArtefacts.SetEquals(actualArtefacts), $"{label}: artefact {row["artefact"]} vs {string.Join(",", actualArtefacts)}");
                    }
                    if (row["eligible"] == "1") {
                        counts[m.Outcome] = counts.TryGetValue(m.Outcome, out var n) ? n + 1 : 1;
                    }
                }
                Report($"{dataset} vs demo_outcomes (Consistent; outcome+reason off and Mammalian, score, feature, status, deficit, residual, artefacts)", tally);
                TestContext.WriteLine($"{dataset} eligible single-run Mammalian: Confirmed {Get(counts, LipidRtOutcome.Confirmed)} / NotEvaluable {Get(counts, LipidRtOutcome.NotEvaluable)} / Inconsistent {Get(counts, LipidRtOutcome.Inconsistent)}");
                Merge(total, tally);
            }
            Assert.AreEqual(total.Compared, total.Agreed, string.Join(Environment.NewLine, total.Differences));
        }

        [DataTestMethod]
        [DataRow(LipidRtClassLevelPolicy.Consistent, "consistent")]
        [DataRow(LipidRtClassLevelPolicy.Corroborated, "corroborated")]
        public void SingleRunMatchesTheFullPrecisionReference(LipidRtClassLevelPolicy policy, string token) {
            RequireReference();
            var total = new Tally();
            foreach (var dataset in Datasets) {
                var path = LipidRtReferenceTables.Locate($"demo_parity_{token}_{dataset.Substring(5)}.tsv");
                if (path is null) {
                    Assert.Inconclusive($"demo_parity_{token}_{dataset.Substring(5)}.tsv not found");
                }
                var reference = LipidRtReferenceTables.ReadTsv(path);
                var off = RunSingle(dataset, policy, null);
                var mammalian = RunSingle(dataset, policy, LipidRtChainRuleProfile.Mammalian);
                var tally = new Tally();
                var outcomeTally = new Tally();
                foreach (var row in reference) {
                    var spotId = row["spot_id"];
                    var o = off[spotId];
                    var m = mammalian[spotId];
                    var label = $"{dataset} {spotId}";
                    var offAgrees = LipidRtReferenceTables.OutcomeOf(row["outcome_off"]) == o.Outcome && LipidRtReferenceTables.ReasonOf(row["code_off"]) == o.Reason;
                    var mammalianAgrees = LipidRtReferenceTables.OutcomeOf(row["outcome_mammalian"]) == m.Outcome && LipidRtReferenceTables.ReasonOf(row["code_mammalian"]) == m.Reason;
                    outcomeTally.Check(offAgrees, $"{label}: off {row["code_off"]} vs {o.Reason}");
                    outcomeTally.Check(mammalianAgrees, $"{label}: mammalian {row["code_mammalian"]} vs {m.Reason}");
                    tally.Check(offAgrees, $"{label}: off {row["code_off"]} vs {o.Reason}");
                    tally.Check(mammalianAgrees, $"{label}: mammalian {row["code_mammalian"]} vs {m.Reason}");
                    tally.Check(ScoreAgrees(row["score_off"], o.Score, 1e-6), $"{label}: score off {row["score_off"]} vs {o.Score}");
                    tally.Check(ScoreAgrees(row["score_mammalian"], m.Score, 1e-6), $"{label}: score {row["score_mammalian"]} vs {m.Score}");
                    if (row["status"].Length > 0) {
                        tally.Check(LipidRtReferenceTables.StatusOf(row["status"]) == m.Evidence.Status, $"{label}: status {row["status"]} vs {m.Evidence.Status}");
                        tally.Check(Math.Abs(LipidRtReferenceTables.ParseDouble(row["e"]) - Math.Round(m.Evidence.Margin, 2)) < 1e-9, $"{label}: e {row["e"]} vs {m.Evidence.Margin}");
                        tally.Check(Math.Abs(LipidRtReferenceTables.ParseDouble(row["d"]) - Math.Round(m.Evidence.Deficit, 2)) < 1e-9, $"{label}: d {row["d"]} vs {m.Evidence.Deficit}");
                        tally.Check(row["dirs"] == m.Evidence.Directions, $"{label}: dirs {row["dirs"]} vs {m.Evidence.Directions}");
                        tally.Check(LipidRtReferenceTables.ParseInt(row["axes"]) == m.Evidence.Axes, $"{label}: axes");
                        tally.Check(LipidRtReferenceTables.ParseInt(row["ties"]) == m.Evidence.Ties, $"{label}: ties {row["ties"]} vs {m.Evidence.Ties}");
                        tally.Check(LipidRtReferenceTables.ParseInt(row["nadd"]) == m.Evidence.AdductCount, $"{label}: nadd");
                        tally.Check(LipidRtReferenceTables.ParseInt(row["n_members"]) == m.Evidence.Members.Count, $"{label}: members");
                        tally.Check(row["resid_db"].Length == 0 ? !m.Evidence.ResidualDoubleBondSteps.HasValue
                            : m.Evidence.ResidualDoubleBondSteps.HasValue && Math.Abs(LipidRtReferenceTables.ParseDouble(row["resid_db"]) - m.Evidence.ResidualDoubleBondSteps.Value) <= 0.0051,
                            $"{label}: resid_db {row["resid_db"]} vs {m.Evidence.ResidualDoubleBondSteps}");
                        var actualArtefacts = string.Join(",", m.Evidence.Artefacts.Select(a => a.Kind).OrderBy(k => k, StringComparer.Ordinal));
                        tally.Check(row["artefact"] == actualArtefacts, $"{label}: artefact {row["artefact"]} vs {actualArtefacts}");
                        tally.Check((row["corr_adduct"] == "1") == m.CorroboratedByAdduct, $"{label}: corr_adduct {row["corr_adduct"]} vs {m.CorroboratedByAdduct}");
                    }
                }
                Report($"{dataset} {token}: outcome+reason (off and Mammalian)", outcomeTally);
                Report($"{dataset} {token}: every compared field (outcome, reason, score 1e-6, status, e, d, dirs, axes, ties, adducts, members, residual, artefacts, adduct corroboration)", tally);
                Merge(total, tally);
            }
            Assert.AreEqual(total.Compared, total.Agreed, string.Join(Environment.NewLine, total.Differences));
        }

        [DataTestMethod]
        [DataRow(LipidRtClassLevelPolicy.Consistent, "consistent")]
        [DataRow(LipidRtClassLevelPolicy.Corroborated, "corroborated")]
        public void CrossPolarityMatchesTheReference(LipidRtClassLevelPolicy policy, string token) {
            RequireReference();
            var total = new Tally();
            foreach (var dataset in new[] { "demo_pos", "demo_neg" }) {
                var reference = LipidRtReferenceTables.ReadTsv(LipidRtReferenceTables.Locate($"demo_parity_{token}_{dataset.Substring(5)}.tsv"));
                var paired = RunPaired(dataset, policy);
                var tally = new Tally();
                var outcomeTally = new Tally();
                var confirmed = 0;
                foreach (var row in reference) {
                    var r = paired[row["spot_id"]];
                    var label = $"{dataset} {row["spot_id"]}";
                    var agrees = LipidRtReferenceTables.OutcomeOf(row["outcome_after_xpol"]) == r.Outcome && LipidRtReferenceTables.ReasonOf(row["code_after_xpol"]) == r.Reason;
                    outcomeTally.Check(agrees, $"{label}: {row["code_after_xpol"]} vs {r.Reason}");
                    tally.Check(agrees, $"{label}: {row["code_after_xpol"]} vs {r.Reason}");
                    tally.Check(ScoreAgrees(row["score_after_xpol"], r.Score, 1e-6), $"{label}: score {row["score_after_xpol"]} vs {r.Score}");
                    var expectedTwins = new SortedSet<int>(row["corr_xpol"].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(LipidRtReferenceTables.IdOf));
                    tally.Check(expectedTwins.SetEquals(r.CorroboratedByPolarity), $"{label}: corr_xpol {row["corr_xpol"]} vs {string.Join(";", r.CorroboratedByPolarity)}");
                    tally.Check((row["xpol_conflict"] == "1") == r.PolarityConflict, $"{label}: conflict");
                    if (row["eligible"] == "1" && r.Outcome == LipidRtOutcome.Confirmed) {
                        confirmed++;
                    }
                }
                Report($"{dataset} {token} after cross-polarity: outcome+reason", outcomeTally);
                Report($"{dataset} {token} after cross-polarity: outcome, reason, score, corroborating twins, conflict", tally);
                TestContext.WriteLine($"{dataset} {token} Confirmed after cross-polarity: {confirmed}");
                Merge(total, tally);
            }
            Assert.AreEqual(total.Compared, total.Agreed, string.Join(Environment.NewLine, total.Differences));
        }

        [TestMethod]
        public void TheDemoOutcomesDoNotDependOnInputOrder() {
            RequireReference();
            var check = new LipidRtCheck(new LipidRtCheckParameter(), LipidRtChainRuleProfile.Mammalian);
            var random = new Random(17);
            foreach (var dataset in new[] { "demo_pos", "demo_neg" }) {
                var unit = LipidRtReferenceTables.LoadUnits(dataset).Single();
                var reference = check.Evaluate(unit.Input);
                var shuffled = new LipidRtRunInput(
                    unit.Input.Candidates.OrderBy(_ => random.Next()).ToList(),
                    unit.Input.Context.OrderBy(_ => random.Next()).ToList(),
                    unit.Input.RtMin, unit.Input.RtMax, unit.Input.Polarity);
                var result = check.Evaluate(shuffled);
                foreach (var r in reference.Results) {
                    var s = result[r.Id];
                    Assert.AreEqual(r.Outcome, s.Outcome, $"{dataset} {r.Id}");
                    Assert.AreEqual(r.Reason, s.Reason, $"{dataset} {r.Id}");
                    Assert.AreEqual(r.Score, s.Score, $"{dataset} {r.Id}");
                    Assert.AreEqual(r.Sentence, s.Sentence, $"{dataset} {r.Id}");
                }
            }
        }

        /// <summary>SPEC 13.2: the species composition of every eligible row, read by the C# parser, equals the table's.</summary>
        [TestMethod]
        public void TheSpeciesParseMatchesTheReferenceTables() {
            RequireReference();
            var datasets = LipidRtReferenceTables.TableDatasets(d => d.StartsWith("zt_") || d.StartsWith("nist_") || d.StartsWith("demo_") || d.StartsWith("osaka_")).ToList();
            Assert.IsTrue(datasets.Count > 0);
            var total = new Tally();
            var chains = new Tally();
            foreach (var dataset in datasets) {
                var tally = new Tally();
                foreach (var row in LipidRtReferenceTables.ReadTsv(LipidRtReferenceTables.Locate(dataset + ".tsv")).Where(r => r["eligible"] == "1")) {
                    var parsed = LipidRtNameParser.Parse(row["name_raw"], row["ontology"]);
                    var agrees = parsed.IsParsed
                        && parsed.Carbon == LipidRtReferenceTables.ParseInt(row["C"])
                        && parsed.DoubleBondEquivalent == LipidRtReferenceTables.ParseInt(row["DBe"])
                        && parsed.Oxygen == LipidRtReferenceTables.ParseInt(row["O"])
                        && parsed.ClassKey == row["class_key"];
                    tally.Check(agrees, $"{dataset} {row["spot_id"]} '{row["name_raw"]}' {row["ontology"]}: table {row["class_key"]} {row["C"]}:{row["DBe"]};O{row["O"]} vs {parsed.ClassKey} {parsed.Carbon}:{parsed.DoubleBondEquivalent};O{parsed.Oxygen}");
                    var expectedChains = row["chains"];
                    var actualChains = string.Join("|", parsed.Chains.Select(c => c.ToString()));
                    chains.Check(expectedChains == actualChains, $"{dataset} {row["spot_id"]} '{row["name_raw"]}': chains {expectedChains} vs {actualChains}");
                }
                Report($"{dataset}: species composition of eligible rows", tally);
                Merge(total, tally);
            }
            Report("all tables: chain roles of eligible rows", chains);
            Assert.AreEqual(total.Compared, total.Agreed, string.Join(Environment.NewLine, total.Differences));
            Assert.AreEqual(chains.Compared, chains.Agreed, string.Join(Environment.NewLine, chains.Differences));
        }

        /// <summary>
        /// The eligibility rule of the in-process builder (SPEC 2.1) against the tables' flag, with the match result
        /// reconstructed from the exported annotation tag and reference-matched flag.
        /// </summary>
        [TestMethod]
        public void TheBuilderEligibilityMatchesTheReferenceTables() {
            RequireReference();
            var total = new Tally();
            foreach (var dataset in Datasets) {
                var tally = new Tally();
                foreach (var row in LipidRtReferenceTables.ReadTsv(LipidRtReferenceTables.Locate(dataset + ".tsv"))) {
                    var tag = row["tag"];
                    var spectrum = LipidRtReferenceTables.IsMsmsTag(tag);
                    var result = new MsScanMatchResult
                    {
                        Name = row["name_raw"],
                        IsPrecursorMzMatch = true,
                        IsSpectrumMatch = spectrum,
                        IsRtMatch = tag.StartsWith("3") || tag.StartsWith("5"),
                        IsLipidChainsMatch = tag == "310" || tag == "410",
                        IsLipidClassMatch = tag == "310" || tag == "410" || tag == "320" || tag == "420",
                        IsReferenceMatched = row["ref_matched"] == "1",
                        Source = SourceType.MspDB,
                        WeightedDotProduct = (float)LipidRtReferenceTables.ParseDouble(row["weighted_dp"]),
                    };
                    var candidate = LipidRtInputBuilder.CreateCandidate(LipidRtReferenceTables.ParseInt(row["row_id"]), LipidRtReferenceTables.ParseDouble(row["rt"]),
                        LipidRtReferenceTables.ParseDouble(row["mz"]), LipidRtReferenceTables.ParseDouble(row["height"]), row["adduct"], result, row["ontology"], row["name_raw"]);
                    var expected = row["eligible"] == "1";
                    var agrees = candidate != null && candidate.IsEligible == expected
                        && candidate.TagLevel == (spectrum ? LipidRtReferenceTables.TagLevelOf(row["tag_level"]) : LipidRtTagLevel.None)
                        && (!expected || (candidate.Carbon == LipidRtReferenceTables.ParseInt(row["C"]) && candidate.DoubleBondEquivalent == LipidRtReferenceTables.ParseInt(row["DBe"])
                            && candidate.Oxygen == LipidRtReferenceTables.ParseInt(row["O"]) && candidate.ClassKey == row["class_key"] && candidate.Name == row["name"]
                            && candidate.Molecular == row["molecular"]));
                    tally.Check(agrees, $"{dataset} {row["spot_id"]} '{row["name_raw"]}' tag {tag}: table eligible {row["eligible"]} ({row["elig_reason"]}) vs {candidate?.IsEligible} ({candidate?.IneligibleReason})");
                }
                Report($"{dataset}: builder eligibility, composition, names", tally);
                Merge(total, tally);
            }
            Assert.AreEqual(total.Compared, total.Agreed, string.Join(Environment.NewLine, total.Differences));
        }

        /// <summary>
        /// Beyond the demo: every evaluation run the frozen reference wrote (out/&lt;run&gt;/outcomes/{off,on}/&lt;dataset&gt;.tsv),
        /// with its operating point and policy, the positive/negative pairs corroborated as the evaluation driver did.
        /// </summary>
        [DataTestMethod]
        [DataRow("dev_hp_final", LipidRtOperatingPoint.HighPrecision, LipidRtClassLevelPolicy.Consistent)]
        [DataRow("dev_hp_classcorr", LipidRtOperatingPoint.HighPrecision, LipidRtClassLevelPolicy.Corroborated)]
        [DataRow("test_hp", LipidRtOperatingPoint.HighPrecision, LipidRtClassLevelPolicy.Consistent)]
        [DataRow("test_hp_classcorr", LipidRtOperatingPoint.HighPrecision, LipidRtClassLevelPolicy.Corroborated)]
        [DataRow("test_strict", LipidRtOperatingPoint.Strict, LipidRtClassLevelPolicy.Consistent)]
        [DataRow("test_mid", LipidRtOperatingPoint.Mid, LipidRtClassLevelPolicy.Consistent)]
        [DataRow("test_bal", LipidRtOperatingPoint.Balanced, LipidRtClassLevelPolicy.Consistent)]
        public void EvaluationRunsMatchTheReference(string run, LipidRtOperatingPoint operatingPoint, LipidRtClassLevelPolicy policy) {
            RequireReference();
            var outFolder = new[] { System.IO.Path.Combine(LipidRtReferenceTables.Root, "out"), System.IO.Path.Combine(LipidRtReferenceTables.Root, "final", "out") }
                .FirstOrDefault(p => System.IO.Directory.Exists(System.IO.Path.Combine(p, run)));
            if (outFolder is null) {
                Assert.Inconclusive($"out/{run} not found");
            }
            var dev = run.StartsWith("dev_", StringComparison.Ordinal);
            var pairs = dev
                ? new[] { ("zt_plasma_pos", "zt_plasma_neg"), ("demo_pos", "demo_neg") }
                : new[] { ("zt_liver_pos", "zt_liver_neg"), ("nist_pos", "nist_neg"), ("osaka_pos", "osaka_neg"), ("liver_old_pos", "liver_old_neg"), ("demo_pos_r2024", "demo_neg_r2024") };
            var singles = dev
                ? new[] { "nist_pos", "demo_pos_samples", "demo_neg_samples" }
                : new[] { "hilic_mpst000192_neg", "hilic_mtbls2207_dda_neg", "hilic_mtbls2207_dia_neg" };
            var parameter = new LipidRtCheckParameter { OperatingPoint = operatingPoint, ClassLevelPolicy = policy };
            var total = new Tally();
            foreach (var (variant, profile) in new[] { ("off", (ILipidRtPlausibilityProfile)null), ("on", LipidRtChainRuleProfile.Mammalian) }) {
                var check = new LipidRtCheck(parameter, profile);
                var outcomes = new Dictionary<string, Dictionary<string, LipidRtObjectResult>>(StringComparer.Ordinal);
                foreach (var (a, b) in pairs) {
                    var ua = LipidRtReferenceTables.LoadUnits(a);
                    var ub = LipidRtReferenceTables.LoadUnits(b);
                    if (ua.Count == 1 && ub.Count == 1) {
                        var watch = System.Diagnostics.Stopwatch.StartNew();
                        var paired = LipidRtCorroboration.CrossPolarity(check.EvaluateSingleRun(ua[0].Input), check.EvaluateSingleRun(ub[0].Input), parameter);
                        TestContext.WriteLine($"{run} {variant} {a}+{b}: {ua[0].Input.Candidates.Count}+{ub[0].Input.Candidates.Count} candidates checked in {watch.ElapsedMilliseconds} ms");
                        outcomes[a] =paired.Positive.Results.ToDictionary(r => ua[0].SpotIdById[r.Id], r => r, StringComparer.Ordinal);
                        outcomes[b] = paired.Negative.Results.ToDictionary(r => ub[0].SpotIdById[r.Id], r => r, StringComparer.Ordinal);
                    }
                    else {
                        outcomes[a] = EvaluateEach(check, ua);
                        outcomes[b] = EvaluateEach(check, ub);
                    }
                }
                foreach (var dataset in singles) {
                    outcomes[dataset] = EvaluateEach(check, LipidRtReferenceTables.LoadUnits(dataset));
                }
                foreach (var pair in outcomes.OrderBy(p => p.Key, StringComparer.Ordinal)) {
                    var path = System.IO.Path.Combine(outFolder, run, "outcomes", variant, pair.Key + ".tsv");
                    if (!System.IO.File.Exists(path)) {
                        continue; // a dataset the run did not score
                    }
                    var tally = new Tally();
                    var scoreTally = new Tally();
                    foreach (var row in LipidRtReferenceTables.ReadTsv(path)) {
                        var r = pair.Value[row["spot_id"]];
                        tally.Check(LipidRtReferenceTables.OutcomeOf(row["outcome"]) == r.Outcome && LipidRtReferenceTables.ReasonOf(row["code"]) == r.Reason,
                            $"{pair.Key} {row["spot_id"]} {row["name_raw"]}: {row["outcome"]}/{row["code"]} vs {r.Outcome}/{r.Reason}");
                        scoreTally.Check(ScoreAgrees(row["score"], r.Score, FourSignificant(row["score"])), $"{pair.Key} {row["spot_id"]}: score {row["score"]} vs {r.Score}");
                    }
                    Report($"{run} {variant} {pair.Key}: outcome+reason", tally);
                    Report($"{run} {variant} {pair.Key}: score (%.4g)", scoreTally);
                    Merge(total, tally);
                    Merge(total, scoreTally);
                }
            }
            Assert.IsTrue(total.Compared > 0);
            Assert.AreEqual(total.Compared, total.Agreed, string.Join(Environment.NewLine, total.Differences));
        }

        private static Dictionary<string, LipidRtObjectResult> EvaluateEach(LipidRtCheck check, IEnumerable<LipidRtReferenceTables.Unit> units) {
            var results = new Dictionary<string, LipidRtObjectResult>(StringComparer.Ordinal);
            foreach (var unit in units) {
                foreach (var r in check.Evaluate(unit.Input).Results) {
                    results[unit.SpotIdById[r.Id]] = r;
                }
            }
            return results;
        }

        private static void Merge(Tally total, Tally tally) {
            total.Compared += tally.Compared;
            total.Agreed += tally.Agreed;
            foreach (var d in tally.Differences) {
                if (total.Differences.Count < 40) {
                    total.Differences.Add(d);
                }
            }
        }

        private static int Get(Dictionary<LipidRtOutcome, int> counts, LipidRtOutcome outcome) => counts.TryGetValue(outcome, out var n) ? n : 0;
    }
}

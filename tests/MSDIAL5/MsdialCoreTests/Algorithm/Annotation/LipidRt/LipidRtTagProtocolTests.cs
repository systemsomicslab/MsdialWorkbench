using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt.Tests
{
    /// <summary>
    /// The tag protocol (SPEC section 9 as re-expressed in addendum B1) and hooks A and B, on real peaks and spots: the
    /// check selects Confirmed with its own source, undoes only its own tags, never touches a person's or another tool's
    /// tag, never contradicts a Misannotation, never repeats what a person removed, and records a verdict per object.
    /// </summary>
    /// <remarks>
    /// The unit is the TG grid of the reference's synthetic tests (C 48-56 even, DB 0-3, RT = 2 + 0.15 C - 0.25 DB,
    /// [M+NH4]+, chain-level LBM matches, dot 0.8), plus two unannotated peaks at 0.5 and 19.5 min so that the run spans
    /// about 20 minutes, as in the reference.
    /// </remarks>
    [TestClass]
    public class LipidRtTagProtocolTests
    {
        private const string Agent = "Agent/curator/2";

        private static ParameterBase Lipidomics(Action<LipidRtCheckParameter> configure = null) {
            var parameter = new ParameterBase { TargetOmics = TargetOmics.Lipidomics, IonMode = IonMode.Positive, MachineCategory = MachineCategory.LCMS };
            configure?.Invoke(parameter.LipidRtCheckParam);
            return parameter;
        }

        private static MsScanMatchResult Lbm(string name) {
            return new MsScanMatchResult
            {
                Name = name,
                IsPrecursorMzMatch = true,
                IsSpectrumMatch = true,
                IsLipidClassMatch = true,
                IsLipidChainsMatch = true,
                IsReferenceMatched = true,
                Source = SourceType.MspDB,
                AnnotatorID = "lbm",
                WeightedDotProduct = 0.8f,
                TotalScore = 0.8f,
            };
        }

        private static IEnumerable<(int Id, double Rt, double Mz, string Name)> Grid() {
            yield return (0, 0.5, 300.1, null);
            yield return (1, 19.5, 301.1, null);
            var id = 2;
            foreach (var c in new[] { 48, 50, 52, 54, 56 }) {
                foreach (var db in new[] { 0, 1, 2, 3 }) {
                    yield return (id++, 2 + 0.15 * c - 0.25 * db, LipidRtSyntheticRun.Mass("TG", c, db) + LipidRtSyntheticRun.AdductOf("[M+NH4]+"), $"TG {c}:{db}|TG 16:0_16:0_{c - 32}:{db}");
                }
            }
        }

        private static List<AlignmentSpotProperty> Spots() {
            var spots = new List<AlignmentSpotProperty>();
            foreach (var (id, rt, mz, name) in Grid()) {
                var spot = new AlignmentSpotProperty
                {
                    MasterAlignmentID = id,
                    AlignmentID = id,
                    TimesCenter = new ChromXs(rt),
                    MassCenter = mz,
                    HeightAverage = 1e5f,
                    Ontology = name is null ? string.Empty : "TG",
                    Name = name ?? string.Empty,
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>(),
                    AlignmentDriftSpotFeatures = new List<AlignmentSpotProperty>(),
                };
                spot.SetAdductType(AdductIon.GetAdductIon("[M+NH4]+"));
                if (name != null) {
                    spot.MatchResults.AddResult(Lbm(name));
                }
                spots.Add(spot);
            }
            return spots;
        }

        private static List<ChromatogramPeakFeature> Peaks() {
            var peaks = new List<ChromatogramPeakFeature>();
            foreach (var (id, rt, mz, name) in Grid()) {
                var peak = new ChromatogramPeakFeature(new BaseChromatogramPeakFeature
                {
                    Mass = mz, PeakHeightTop = 1e5, ChromXsLeft = new ChromXs(rt - 0.02), ChromXsTop = new ChromXs(rt), ChromXsRight = new ChromXs(rt + 0.02),
                })
                {
                    MasterPeakID = id,
                    Ontology = name is null ? string.Empty : "TG",
                    Name = name ?? string.Empty,
                };
                peak.SetAdductType(AdductIon.GetAdductIon("[M+NH4]+"));
                if (name != null) {
                    peak.MatchResults.AddResult(Lbm(name));
                }
                peaks.Add(peak);
            }
            return peaks;
        }

        private static AlignmentSpotProperty Find(IEnumerable<AlignmentSpotProperty> spots, int c, int db) {
            return spots.Single(s => s.Name.StartsWith($"TG {c}:{db}|", StringComparison.Ordinal));
        }

        [TestMethod]
        public void HookBConfirmsWithItsOwnSourceAndRecordsAVerdictPerObject() {
            var spots = Spots();
            var parameter = Lipidomics();

            var run = LipidRtCheckHooks.RunOnSpots(spots, parameter, null);

            Assert.IsTrue(run.Ran, run.Describe());
            var confirmed = run.Result.Results.Where(r => r.Outcome == LipidRtOutcome.Confirmed).Select(r => r.Id).ToList();
            Assert.IsTrue(confirmed.Count > 0);
            foreach (var spot in spots) {
                var isConfirmed = confirmed.Contains(spot.MasterAlignmentID);
                Assert.AreEqual(isConfirmed, spot.TagCollection.IsSelected(PeakSpotTag.CONFIRMED), spot.Name);
                if (isConfirmed) {
                    Assert.AreEqual("MsdialLipidRtCheck/1.1", spot.TagCollection.GetSource(PeakSpotTag.CONFIRMED));
                }
            }
            var tg521 = Find(spots, 52, 1);
            Assert.AreEqual(LipidRtOutcome.Confirmed, tg521.LipidRtVerdict.Outcome);
            Assert.AreEqual(LipidRtReason.ConfirmedRt, tg521.LipidRtVerdict.Reason);
            Assert.AreEqual(parameter.LipidRtCheckParam.ComputeHash(), tg521.LipidRtVerdict.ParameterHash);
            Assert.AreEqual("1.1", tg521.LipidRtVerdict.RuleVersion);
            StringAssert.StartsWith(tg521.LipidRtVerdict.Evidence, "Confirmed: TG 52:1 elutes after ");
            var corner = Find(spots, 48, 3);
            Assert.AreEqual(LipidRtReason.Support, corner.LipidRtVerdict.Reason);
            Assert.IsFalse(corner.TagCollection.Any(), "the check never writes anything but Confirmed");
            Assert.IsNull(spots[0].LipidRtVerdict, "an unannotated spot gets no verdict");
            Assert.AreEqual(confirmed.Count, run.Tags.Set);
            Assert.AreEqual(20, run.Tags.Verdicts);
            Assert.AreEqual(0, run.Tags.Withdrawn);
            StringAssert.Contains(run.Describe(), $"{confirmed.Count} Confirmed");
        }

        /// <summary>
        /// A lipid named from a computed spectrum is never tagged Confirmed, whether its evidence record says in silico or
        /// only its library does; the verdict says why. Its neighbours, named by the LBM, are confirmed as before.
        /// </summary>
        [TestMethod]
        public void AnInSilicoLipidMatchIsNeverTaggedConfirmed() {
            var spots = Spots();
            var spectrumPredicted = Find(spots, 52, 1).MatchResults.Representative;
            spectrumPredicted.AnnotatorID = "glacier";
            spectrumPredicted.EvidenceSource = AnnotationEvidenceSource.BySpectrumPredictionTool;
            var fromPredictedLibrary = Find(spots, 54, 1).MatchResults.Representative;
            fromPredictedLibrary.AnnotatorID = "glacier_lipid";
            fromPredictedLibrary.EvidenceSource = AnnotationEvidenceSource.RuleBased;

            var run = LipidRtCheckHooks.RunOnSpots(spots, Lipidomics(), new HashSet<string> { "glacier_lipid" });

            Assert.IsTrue(run.Ran, run.Describe());
            foreach (var spot in new[] { Find(spots, 52, 1), Find(spots, 54, 1) }) {
                Assert.IsFalse(spot.TagCollection.IsSelected(PeakSpotTag.CONFIRMED), spot.Name);
                Assert.AreEqual(LipidRtOutcome.NotEvaluable, spot.LipidRtVerdict.Outcome, spot.Name);
                Assert.AreEqual(LipidRtReason.Ineligible, spot.LipidRtVerdict.Reason, spot.Name);
                StringAssert.Contains(spot.LipidRtVerdict.Evidence, "in silico", spot.Name);
            }
            Assert.IsTrue(Find(spots, 52, 2).TagCollection.IsSelected(PeakSpotTag.CONFIRMED), "the LBM-named neighbour is still confirmed");
        }

        /// <summary>
        /// Rules 1.0 evaluated an in-silico lipid match and counted it as a neighbour. The parameter hash covers the values
        /// and the version, not the rules, so only the version tells a reader which rules produced a verdict: every record
        /// this run writes names 1.1, on the excluded match and on its neighbours alike.
        /// </summary>
        [TestMethod]
        public void TheRulesThatLeaveInSilicoMatchesOutAreNamedOnEveryRecord() {
            var spots = Spots();
            var parameter = Lipidomics();
            Find(spots, 52, 1).MatchResults.Representative.AnnotatorID = "glacier_lipid";

            var run = LipidRtCheckHooks.RunOnSpots(spots, parameter, new HashSet<string> { "glacier_lipid" });

            Assert.AreEqual("1.1", LipidRtCheckParameter.RuleVersion);
            Assert.AreEqual("MsdialLipidRtCheck/1.1", LipidRtCheckParameter.TagSource);
            CollectionAssert.Contains(parameter.LipidRtCheckParam.ParametersAsText(TargetOmics.Lipidomics), "Lipid RT check rule version: 1.1");
            StringAssert.Contains(run.Describe(), "rules 1.1, parameter ");
            var excluded = Find(spots, 52, 1);
            Assert.AreEqual(LipidRtReason.Ineligible, excluded.LipidRtVerdict.Reason);
            var neighbour = Find(spots, 52, 2);
            Assert.AreEqual("MsdialLipidRtCheck/1.1", neighbour.TagCollection.GetSource(PeakSpotTag.CONFIRMED));
            foreach (var spot in spots.Where(s => s.LipidRtVerdict != null)) {
                Assert.AreEqual("1.1", spot.LipidRtVerdict.RuleVersion, spot.Name);
            }
        }

        /// <summary>
        /// A call written before the set existed, RunOnPeaks(peaks, parameter), still compiled while the set was optional,
        /// so a merge that restored one at a processing step went unnoticed. Required, it does not compile.
        /// </summary>
        [TestMethod]
        public void BothHooksRequireThePredictedLibraryAnnotators() {
            foreach (var name in new[] { nameof(LipidRtCheckHooks.RunOnPeaks), nameof(LipidRtCheckHooks.RunOnSpots) }) {
                var parameter = typeof(LipidRtCheckHooks).GetMethod(name).GetParameters().Single(p => p.Name == "predictedLibraryAnnotators");
                Assert.IsFalse(parameter.IsOptional, name);
                Assert.IsFalse(parameter.HasDefaultValue, name);
            }
        }

        [TestMethod]
        public void APersonsTagsAndAnotherToolsTagAreNeverTouched() {
            var spots = Spots();
            var personConfirmed = Find(spots, 52, 1);
            personConfirmed.TagCollection.Select(PeakSpotTag.CONFIRMED);
            var misannotated = Find(spots, 52, 2);
            misannotated.TagCollection.Select(PeakSpotTag.MISANNOTATION);
            var agentConfirmed = Find(spots, 50, 1);
            agentConfirmed.TagCollection.Select(PeakSpotTag.CONFIRMED, Agent);
            var suppressed = Find(spots, 54, 1);
            suppressed.TagCollection.Suppress(PeakSpotTag.CONFIRMED);
            var personOnInconsistent = Find(spots, 48, 3);
            personOnInconsistent.TagCollection.Select(PeakSpotTag.CONFIRMED);

            var run = LipidRtCheckHooks.RunOnSpots(spots, Lipidomics(), null);

            foreach (var s in new[] { personConfirmed, misannotated, agentConfirmed, suppressed }) {
                Assert.AreEqual(LipidRtOutcome.Confirmed, s.LipidRtVerdict.Outcome, s.Name + " is Confirmed by its RT");
            }
            Assert.IsNull(personConfirmed.TagCollection.GetSource(PeakSpotTag.CONFIRMED), "a person's Confirmed stays the person's");
            Assert.IsFalse(misannotated.TagCollection.IsSelected(PeakSpotTag.CONFIRMED), "never contradicts a person's Misannotation");
            Assert.IsTrue(misannotated.TagCollection.IsSelected(PeakSpotTag.MISANNOTATION));
            Assert.AreEqual(Agent, agentConfirmed.TagCollection.GetSource(PeakSpotTag.CONFIRMED));
            Assert.IsFalse(suppressed.TagCollection.IsSelected(PeakSpotTag.CONFIRMED), "a person removed it before");
            Assert.IsTrue(personOnInconsistent.TagCollection.IsSelected(PeakSpotTag.CONFIRMED), "a NotEvaluable never removes a person's tag");
            Assert.AreEqual(2, run.Tags.AlreadySelected);
            Assert.AreEqual(1, run.Tags.Misannotation);
            Assert.AreEqual(1, run.Tags.Suppressed);
        }

        [TestMethod]
        public void ARerunUndoesOnlyItsOwnTagsAndIsIdempotent() {
            var spots = Spots();
            var first = LipidRtCheckHooks.RunOnSpots(spots, Lipidomics(), null);
            var before = spots.Select(s => (s.TagCollection.ToString(), s.TagCollection.GetSource(PeakSpotTag.CONFIRMED), s.LipidRtVerdict?.Reason)).ToList();

            var second = LipidRtCheckHooks.RunOnSpots(spots, Lipidomics(), null);

            CollectionAssert.AreEqual(before, spots.Select(s => (s.TagCollection.ToString(), s.TagCollection.GetSource(PeakSpotTag.CONFIRMED), s.LipidRtVerdict?.Reason)).ToList());
            Assert.AreEqual(first.Tags.Set, second.Tags.Withdrawn, "the second run withdrew exactly what the first set");
            Assert.AreEqual(first.Tags.Set, second.Tags.Set);
        }

        [TestMethod]
        public void AnObjectThatIsNoLongerConfirmedLosesOnlyTheChecksTag() {
            var spots = Spots();
            LipidRtCheckHooks.RunOnSpots(spots, Lipidomics(), null);
            var moved = Find(spots, 52, 1);
            Assert.IsTrue(moved.TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
            moved.TagCollection.Select(PeakSpotTag.COELUTION);
            moved.TimesCenter = new ChromXs(moved.TimesCenter.RT.Value + 1.5); // t03: now out of order

            LipidRtCheckHooks.RunOnSpots(spots, Lipidomics(), null);

            Assert.AreEqual(LipidRtOutcome.Inconsistent, moved.LipidRtVerdict.Outcome);
            Assert.IsFalse(moved.TagCollection.IsSelected(PeakSpotTag.CONFIRMED), "its own Confirmed is withdrawn");
            Assert.IsTrue(moved.TagCollection.IsSelected(PeakSpotTag.COELUTION), "a person's other tag stays");
            Assert.IsFalse(moved.TagCollection.IsSelected(PeakSpotTag.MISANNOTATION), "Inconsistent is never written as a tag");
        }

        [TestMethod]
        public void APersonRemovingTheChecksTagIsHonouredOnTheNextRun() {
            var spots = Spots();
            LipidRtCheckHooks.RunOnSpots(spots, Lipidomics(), null);
            var spot = Find(spots, 52, 1);

            spot.TagCollection.DeselectByPerson(PeakSpotTag.CONFIRMED); // what the GUI toggle does
            var run = LipidRtCheckHooks.RunOnSpots(spots, Lipidomics(), null);

            Assert.AreEqual(LipidRtOutcome.Confirmed, spot.LipidRtVerdict.Outcome, "the verdict still says what the RT says");
            Assert.IsFalse(spot.TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.AreEqual(1, run.Tags.Suppressed);

            spot.TagCollection.SelectByPerson(PeakSpotTag.CONFIRMED); // and turned on again: now the person's
            LipidRtCheckHooks.RunOnSpots(spots, Lipidomics(c => c.IsEnabled = false), null);
            Assert.IsTrue(spot.TagCollection.IsSelected(PeakSpotTag.CONFIRMED), "a person's tag survives even the check being switched off");
        }

        [TestMethod]
        public void SwitchedOffOrNotLipidomicsWithdrawsItsTagsAndVerdictsOnly() {
            foreach (var parameter in new[] { Lipidomics(c => c.IsEnabled = false), new ParameterBase { TargetOmics = TargetOmics.Metabolomics, IonMode = IonMode.Positive } }) {
                var spots = Spots();
                var first = LipidRtCheckHooks.RunOnSpots(spots, Lipidomics(), null);
                var person = Find(spots, 48, 3);
                person.TagCollection.Select(PeakSpotTag.CONFIRMED);

                var run = LipidRtCheckHooks.RunOnSpots(spots, parameter, null);

                Assert.IsFalse(run.Ran);
                Assert.AreEqual(first.Tags.Set, run.Tags.Withdrawn);
                Assert.IsTrue(spots.All(s => s.LipidRtVerdict is null), "no verdict outlives the run that made it");
                Assert.IsTrue(spots.Where(s => s != person).All(s => !s.TagCollection.Any()));
                Assert.IsTrue(person.TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
                StringAssert.Contains(run.Describe(), "withdrawn");
            }
        }

        [TestMethod]
        public void AFailureLeavesNoHalfResultBehind() {
            var spots = Spots();
            LipidRtCheckHooks.RunOnSpots(spots, Lipidomics(), null);
            Find(spots, 56, 0).MasterAlignmentID = Find(spots, 48, 0).MasterAlignmentID; // the check refuses repeated Ids

            var run = LipidRtCheckHooks.RunOnSpots(spots, Lipidomics(), null);

            Assert.IsFalse(run.Ran);
            Assert.IsInstanceOfType(run.Error, typeof(ArgumentException));
            Assert.IsTrue(spots.All(s => !s.TagCollection.IsSelected(PeakSpotTag.CONFIRMED) && s.LipidRtVerdict is null));
            StringAssert.StartsWith(run.Describe(), "failed");
        }

        [TestMethod]
        public void HookAOnTheFilesPeaksAgreesWithTheCheckAndSurvivesTheSave() {
            var peaks = Peaks();
            var parameter = Lipidomics();

            var run = LipidRtCheckHooks.RunOnPeaks(peaks, parameter, null);

            var expected = new LipidRtCheck(parameter.LipidRtCheckParam).Evaluate(LipidRtInputBuilder.FromPeaks(peaks, IonMode.Positive));
            foreach (var peak in peaks) {
                var isConfirmed = expected.TryGet(peak.MasterPeakID, out var r) && r.Outcome == LipidRtOutcome.Confirmed;
                Assert.AreEqual(isConfirmed, peak.TagCollection.IsSelected(PeakSpotTag.CONFIRMED), peak.Name);
            }
            Assert.IsTrue(run.Tags.Set > 0);

            var scratch = Path.Combine(Path.GetTempPath(), $"msdial-lipidrt-{Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try {
                var file = Path.Combine(scratch, "sample.pai");
                MsdialPeakSerializer.SaveChromatogramPeakFeatures(file, peaks);
                var loaded = MsdialPeakSerializer.LoadChromatogramPeakFeatures(file);
                for (var i = 0; i < peaks.Count; i++) {
                    Assert.AreEqual(peaks[i].TagCollection.ToString(), loaded[i].TagCollection.ToString());
                    Assert.AreEqual(peaks[i].TagCollection.GetSource(PeakSpotTag.CONFIRMED), loaded[i].TagCollection.GetSource(PeakSpotTag.CONFIRMED));
                    Assert.AreEqual(peaks[i].LipidRtVerdict?.Reason, loaded[i].LipidRtVerdict?.Reason);
                    Assert.AreEqual(peaks[i].LipidRtVerdict?.Score, loaded[i].LipidRtVerdict?.Score);
                }

                // Re-annotation re-runs hook A on the loaded peaks: the check's own tags are replaced, not duplicated.
                var again = LipidRtCheckHooks.RunOnPeaks(loaded, parameter, null);
                Assert.AreEqual(run.Tags.Set, again.Tags.Withdrawn);
                Assert.AreEqual(run.Tags.Set, again.Tags.Set);
            }
            finally {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [TestMethod]
        public void TheAlignmentKeepsTheChecksTagsAndVerdictsThroughSaveAndLoad() {
            var spots = Spots();
            LipidRtCheckHooks.RunOnSpots(spots, Lipidomics(), null);
            var scratch = Path.Combine(Path.GetTempPath(), $"msdial-lipidrt-{Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try {
                var file = new AlignmentFileBean { FilePath = Path.Combine(scratch, "AlignmentResult.arf"), FileName = "AlignmentResult" };
                new AlignmentResultContainer { AlignmentSpotProperties = new ObservableCollection<AlignmentSpotProperty>(spots) }.Save(file);

                var loaded = AlignmentResultContainer.Load(file).AlignmentSpotProperties;

                for (var i = 0; i < spots.Count; i++) {
                    Assert.AreEqual(spots[i].TagCollection.ToString(), loaded[i].TagCollection.ToString());
                    Assert.AreEqual(spots[i].TagCollection.GetSource(PeakSpotTag.CONFIRMED), loaded[i].TagCollection.GetSource(PeakSpotTag.CONFIRMED));
                    Assert.AreEqual(spots[i].LipidRtVerdict?.Outcome, loaded[i].LipidRtVerdict?.Outcome);
                    Assert.AreEqual(spots[i].LipidRtVerdict?.Evidence, loaded[i].LipidRtVerdict?.Evidence);
                }
                StringAssert.Contains(File.ReadAllText(Path.Combine(scratch, "AlignmentResult_tags.xml")), "Source=\"MsdialLipidRtCheck/1.1\"");
            }
            finally {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [TestMethod]
        public void WithdrawTouchesOnlyTheChecksOwnSourceOfAnyRuleVersion() {
            var older = new PeakSpotTagCollection();
            older.Select(PeakSpotTag.CONFIRMED, "MsdialLipidRtCheck/0.9");
            Assert.IsTrue(LipidRtTagProtocol.Withdraw(older));
            Assert.IsFalse(older.Any());

            var agent = new PeakSpotTagCollection();
            agent.Select(PeakSpotTag.CONFIRMED, Agent);
            Assert.IsFalse(LipidRtTagProtocol.Withdraw(agent));
            Assert.IsTrue(agent.IsSelected(PeakSpotTag.CONFIRMED));

            var person = new PeakSpotTagCollection();
            person.Select(PeakSpotTag.CONFIRMED);
            Assert.IsFalse(LipidRtTagProtocol.Withdraw(person));
            Assert.IsTrue(person.IsSelected(PeakSpotTag.CONFIRMED));
        }
    }
}

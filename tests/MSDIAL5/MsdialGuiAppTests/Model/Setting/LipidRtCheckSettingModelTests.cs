using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Model.Table;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation.LipidRt;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Setting.Tests
{
    /// <summary>
    /// "Re-run lipid RT check" on a loaded alignment (addendum B3): the settings go into the project's parameter, the check
    /// runs on the spots through the shared tag protocol, the spots' views are told, and the result is saved.
    /// </summary>
    [TestClass]
    public class LipidRtCheckSettingModelTests
    {
        private const string CheckSource = "MsdialLipidRtCheck/1.1";

        private sealed class Fixture
        {
            public Fixture(ParameterBase? parameter = null, Func<Task>? save = null, DataBaseStorage? dataBases = null) {
                Parameter = parameter ?? LipidRtTestSpots.Lipidomics();
                DataBases = dataBases ?? DataBaseStorage.CreateEmpty();
                Spots = LipidRtTestSpots.Spots();
                Models = Spots.Select(s => new AlignmentSpotPropertyModel(s)).ToList();
                UndoManager = new UndoManager();
                Model = new LipidRtCheckSettingModel(Parameter, Models, DataBases, save ?? (() => { Saves++; return Task.CompletedTask; }), UndoManager);
            }

            public ParameterBase Parameter { get; }
            public DataBaseStorage DataBases { get; }
            public List<AlignmentSpotProperty> Spots { get; }
            public List<AlignmentSpotPropertyModel> Models { get; }
            public LipidRtCheckSettingModel Model { get; }
            public UndoManager UndoManager { get; }
            public int Saves { get; private set; }

            public AlignmentSpotProperty Find(int c, int db) => LipidRtTestSpots.Find(Spots, c, db);
            public AlignmentSpotPropertyModel ModelOf(AlignmentSpotProperty spot) => Models.Single(m => m.innerModel == spot);
        }

        [TestMethod]
        public void TheSettingsStartFromTheProjectsParameter() {
            var parameter = LipidRtTestSpots.Lipidomics();
            parameter.LipidRtCheckParam.OperatingPoint = LipidRtOperatingPoint.Mid;
            parameter.LipidRtCheckParam.PlausibilityProfile = LipidRtPlausibilityProfileKind.Mammalian;
            var fixture = new Fixture(parameter);

            Assert.AreEqual(LipidRtOperatingPoint.Mid, fixture.Model.OperatingPoint);
            Assert.AreEqual(LipidRtChromatography.Auto, fixture.Model.Chromatography);
            Assert.AreEqual(LipidRtPlausibilityProfileKind.Mammalian, fixture.Model.PlausibilityProfile);
            Assert.AreEqual(LipidRtClassLevelPolicy.Corroborated, fixture.Model.ClassLevelPolicy, "the addendum's default");
            Assert.AreEqual(LipidRtMinorDuplicatePolicy.Allow, fixture.Model.MinorDuplicatePolicy);
            Assert.IsNull(fixture.Model.CannotRunReason);
            Assert.IsFalse(fixture.Model.IsSwitchedOff);
        }

        [TestMethod]
        public async Task RunConfirmsWithTheChecksSourceTellsTheViewsAndSaves() {
            var fixture = new Fixture();
            var tg521 = fixture.Find(52, 1);
            var changed = new List<string>();
            fixture.ModelOf(tg521).PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            var run = await fixture.Model.RunAsync();

            Assert.IsNotNull(run);
            Assert.IsTrue(run.Ran, run.Describe());
            Assert.AreEqual(1, fixture.Saves, "the alignment result is saved once");
            Assert.IsTrue(tg521.TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.AreEqual(CheckSource, tg521.TagCollection.GetSource(PeakSpotTag.CONFIRMED));
            Assert.AreEqual(LipidRtOutcome.Confirmed, tg521.LipidRtVerdict.Outcome);
            Assert.AreEqual(fixture.Parameter.LipidRtCheckParam.ComputeHash(), tg521.LipidRtVerdict.ParameterHash);
            CollectionAssert.IsSubsetOf(new[] { "Confirmed", "LipidRtVerdict", "ConfirmedDetail" }, changed);

            var model = fixture.ModelOf(tg521);
            Assert.IsTrue(model.Confirmed);
            Assert.AreSame(tg521.LipidRtVerdict, model.LipidRtVerdict);
            StringAssert.Contains(model.ConfirmedDetail, "set by the lipid RT check (" + CheckSource + ")");
            StringAssert.Contains(model.ConfirmedDetail, "Confirmed: TG 52:1 elutes after ");

            // The same as hook B on the same spots: one routine for processing and the GUI.
            var reference = LipidRtTestSpots.Spots();
            LipidRtCheckHooks.RunOnSpots(reference, LipidRtTestSpots.Lipidomics(), null);
            CollectionAssert.AreEqual(
                reference.Select(s => s.TagCollection.IsSelected(PeakSpotTag.CONFIRMED)).ToList(),
                fixture.Spots.Select(s => s.TagCollection.IsSelected(PeakSpotTag.CONFIRMED)).ToList());
            CollectionAssert.AreEqual(
                reference.Select(s => s.LipidRtVerdict?.Reason).ToList(),
                fixture.Spots.Select(s => s.LipidRtVerdict?.Reason).ToList());

            StringAssert.Contains(fixture.Model.Summary, "Confirmed tags set");
            StringAssert.Contains(fixture.Model.Summary, "The alignment result was saved.");
            Assert.AreSame(run, fixture.Model.LastRun);
        }

        /// <summary>
        /// A lipid named from a library of predicted spectra is not evaluated by a run from here, as it is not in processing
        /// (Q5: an in-silico match is never Confirmed). Searched under lipidomics, its record says RuleBased, as the LBM's
        /// does, so only the project's databases tell it apart; without them the same spot would be confirmed.
        /// </summary>
        [TestMethod]
        public async Task RunDoesNotEvaluateAMatchFromALibraryOfPredictedSpectra() {
            var fixture = new Fixture(dataBases: LipidRtTestSpots.WithPredictedLibrary("glacier_lipid"));
            var tg521 = fixture.Find(52, 1);
            LipidRtTestSpots.MatchFromLibrary(tg521, "glacier_lipid");

            var run = await fixture.Model.RunAsync();

            Assert.IsNotNull(run);
            Assert.IsTrue(run.Ran, run.Describe());
            Assert.IsFalse(tg521.TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.AreEqual(LipidRtOutcome.NotEvaluable, tg521.LipidRtVerdict.Outcome);
            Assert.AreEqual(LipidRtReason.Ineligible, tg521.LipidRtVerdict.Reason);
            StringAssert.Contains(tg521.LipidRtVerdict.Evidence, "in silico");
            Assert.IsTrue(fixture.Find(52, 2).TagCollection.IsSelected(PeakSpotTag.CONFIRMED), "the LBM-named neighbour is still confirmed");

            var withoutTheLibrary = new Fixture();
            LipidRtTestSpots.MatchFromLibrary(withoutTheLibrary.Find(52, 1), "glacier_lipid");
            await withoutTheLibrary.Model.RunAsync();
            Assert.IsTrue(withoutTheLibrary.Find(52, 1).TagCollection.IsSelected(PeakSpotTag.CONFIRMED),
                "the control: the record alone does not keep the match out");
        }

        [TestMethod]
        public async Task RunWritesTheSettingsIntoTheProjectsParameter() {
            var fixture = new Fixture();
            fixture.Model.OperatingPoint = LipidRtOperatingPoint.Balanced;
            fixture.Model.Chromatography = LipidRtChromatography.ReversedPhase;
            fixture.Model.PlausibilityProfile = LipidRtPlausibilityProfileKind.Mammalian;
            fixture.Model.ClassLevelPolicy = LipidRtClassLevelPolicy.Never;
            fixture.Model.MinorDuplicatePolicy = LipidRtMinorDuplicatePolicy.Block;
            Assert.AreEqual(LipidRtOperatingPoint.HighPrecision, fixture.Parameter.LipidRtCheckParam.OperatingPoint, "nothing is written before a run");

            await fixture.Model.RunAsync();

            var written = fixture.Parameter.LipidRtCheckParam;
            Assert.AreEqual(LipidRtOperatingPoint.Balanced, written.OperatingPoint);
            Assert.AreEqual(LipidRtChromatography.ReversedPhase, written.Chromatography);
            Assert.AreEqual(LipidRtPlausibilityProfileKind.Mammalian, written.PlausibilityProfile);
            Assert.AreEqual(LipidRtClassLevelPolicy.Never, written.ClassLevelPolicy);
            Assert.AreEqual(LipidRtMinorDuplicatePolicy.Block, written.MinorDuplicatePolicy);
            Assert.AreEqual(written.ComputeHash(), fixture.Find(52, 1).LipidRtVerdict.ParameterHash);
        }

        [TestMethod]
        public async Task RunLeavesAPersonsTagsAndDecisionsAlone() {
            var fixture = new Fixture();
            var removedByPerson = fixture.Find(52, 1);
            removedByPerson.TagCollection.Select(PeakSpotTag.CONFIRMED, CheckSource);
            fixture.ModelOf(removedByPerson).Confirmed = false; // a person's toggle: suppressed
            var misannotated = fixture.Find(52, 2);
            fixture.ModelOf(misannotated).Misannotation = true;
            var corner = fixture.Find(48, 3);
            fixture.ModelOf(corner).Confirmed = true; // a person's Confirmed where the check has no support
            var agent = fixture.Find(54, 1);
            agent.TagCollection.Select(PeakSpotTag.CONFIRMED, "Agent/curator/2");

            await fixture.Model.RunAsync();

            Assert.IsFalse(removedByPerson.TagCollection.IsSelected(PeakSpotTag.CONFIRMED), "a Confirmed a person removed is not set again");
            Assert.IsTrue(removedByPerson.TagCollection.IsSuppressed(PeakSpotTag.CONFIRMED));
            Assert.AreEqual(LipidRtOutcome.Confirmed, removedByPerson.LipidRtVerdict.Outcome, "the verdict still says what the check found");
            Assert.IsFalse(misannotated.TagCollection.IsSelected(PeakSpotTag.CONFIRMED), "the check never contradicts a Misannotation");
            Assert.IsTrue(misannotated.TagCollection.IsSelected(PeakSpotTag.MISANNOTATION));
            Assert.IsTrue(corner.TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.IsNull(corner.TagCollection.GetSource(PeakSpotTag.CONFIRMED), "a person's tag stays the person's");
            Assert.AreEqual("Agent/curator/2", agent.TagCollection.GetSource(PeakSpotTag.CONFIRMED), "another tool's tag stays that tool's");
            StringAssert.Contains(fixture.ModelOf(removedByPerson).ConfirmedDetail, "removed by a person");
        }

        [TestMethod]
        public async Task ARerunWithOtherSettingsWithdrawsOnlyTheChecksOwnTags() {
            var fixture = new Fixture();
            await fixture.Model.RunAsync();
            var tg521 = fixture.Find(52, 1);
            Assert.AreEqual(CheckSource, tg521.TagCollection.GetSource(PeakSpotTag.CONFIRMED));
            var person = fixture.Find(48, 3);
            fixture.ModelOf(person).Confirmed = true;
            var changed = new List<string>();
            fixture.ModelOf(tg521).PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            fixture.Model.Chromatography = LipidRtChromatography.Other;
            await fixture.Model.RunAsync();

            Assert.IsFalse(tg521.TagCollection.IsSelected(PeakSpotTag.CONFIRMED), "the check's own Confirmed is withdrawn");
            Assert.IsFalse(tg521.TagCollection.IsSuppressed(PeakSpotTag.CONFIRMED), "a withdrawal is not a person's removal");
            Assert.AreEqual(LipidRtReason.Gate, tg521.LipidRtVerdict.Reason);
            Assert.IsFalse(fixture.ModelOf(tg521).Confirmed);
            CollectionAssert.Contains(changed, "Confirmed");
            Assert.IsTrue(person.TagCollection.IsSelected(PeakSpotTag.CONFIRMED), "a person's tag is never withdrawn");
            Assert.AreEqual(2, fixture.Saves);
        }

        [TestMethod]
        public async Task OnAProjectThatIsNotLipidomicsNothingRunsOrChanges() {
            var parameter = LipidRtTestSpots.Lipidomics();
            parameter.TargetOmics = TargetOmics.Metabolomics;
            var fixture = new Fixture(parameter);
            fixture.Model.OperatingPoint = LipidRtOperatingPoint.Balanced;

            var run = await fixture.Model.RunAsync();

            Assert.IsNull(run);
            Assert.AreEqual(0, fixture.Saves);
            Assert.IsTrue(fixture.Spots.All(s => !s.TagCollection.Any() && s.LipidRtVerdict is null));
            Assert.AreEqual(LipidRtOperatingPoint.HighPrecision, parameter.LipidRtCheckParam.OperatingPoint, "the project's settings are untouched");
            Assert.IsNotNull(fixture.Model.CannotRunReason);
            StringAssert.Contains(fixture.Model.CannotRunReason, "Metabolomics");
            Assert.AreEqual(fixture.Model.CannotRunReason, fixture.Model.Summary);
        }

        [TestMethod]
        public async Task RunningACheckThatWasSwitchedOffSwitchesItBackToAuto() {
            var parameter = LipidRtTestSpots.Lipidomics();
            parameter.LipidRtCheckParam.IsEnabled = false;
            var fixture = new Fixture(parameter);
            Assert.IsTrue(fixture.Model.IsSwitchedOff);

            var run = await fixture.Model.RunAsync();

            Assert.IsTrue(run!.Ran, run.Describe());
            Assert.IsNull(parameter.LipidRtCheckParam.IsEnabled, "Auto: on for lipidomics, so processing agrees with the result");
            Assert.IsFalse(fixture.Model.IsSwitchedOff);
            Assert.IsTrue(fixture.Find(52, 1).TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
        }

        [TestMethod]
        public async Task AFailedSaveIsReportedNotThrown() {
            var fixture = new Fixture(save: () => Task.FromException(new System.IO.IOException("disk full")));

            var run = await fixture.Model.RunAsync();

            Assert.IsTrue(run!.Ran);
            StringAssert.Contains(fixture.Model.Summary, "could not be saved");
            StringAssert.Contains(fixture.Model.Summary, "disk full");
            Assert.IsTrue(fixture.Find(52, 1).TagCollection.IsSelected(PeakSpotTag.CONFIRMED), "the session keeps the result");
        }

        [TestMethod]
        public async Task ARunClearsTheUndoHistorySoNoEarlierEditPutsBackTheChecksOldTags() {
            var fixture = new Fixture();
            await fixture.Model.RunAsync();
            var tg521 = fixture.Find(52, 1);
            Assert.AreEqual(CheckSource, tg521.TagCollection.GetSource(PeakSpotTag.CONFIRMED));
            IDoCommand markAll = new MarkAllAsConfirmedCommand<AlignmentSpotPropertyModel>(fixture.Models, false);
            markAll.Do();
            fixture.UndoManager.Add(markAll);
            Assert.IsTrue(tg521.TagCollection.IsSuppressed(PeakSpotTag.CONFIRMED), "the person removed the check's tag");

            fixture.Model.Chromatography = LipidRtChromatography.Other;
            await fixture.Model.RunAsync();
            Assert.AreEqual(LipidRtReason.Gate, tg521.LipidRtVerdict.Reason);

            fixture.UndoManager.Undo();

            Assert.IsFalse(tg521.TagCollection.IsSelected(PeakSpotTag.CONFIRMED),
                "no Confirmed credited to the check comes back against its new NotEvaluable verdict");
            Assert.IsTrue(tg521.TagCollection.IsSuppressed(PeakSpotTag.CONFIRMED), "the person's removal holds");
        }

        [TestMethod]
        public async Task TheSaveRunsOffTheCallersThreadAndTheSummarySaysItIsSaving() {
            var ui = new InlineSynchronizationContext();
            bool? savedOnTheCallersContext = null;
            string? summaryWhileSaving = null;
            Fixture? fixture = null;
            fixture = new Fixture(save: () => {
                savedOnTheCallersContext = SynchronizationContext.Current == ui;
                summaryWhileSaving = fixture!.Model.Summary;
                return Task.CompletedTask;
            });

            // RunAsync starts on a UI-like context, so its continuations come back to that context, as on the dispatcher.
            Task<LipidRtUnitRun?> running;
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(ui);
            try {
                running = fixture.Model.RunAsync();
            }
            finally {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
            await running;

            Assert.AreEqual(false, savedOnTheCallersContext, "the save of the whole result does not hold the UI thread");
            StringAssert.Contains(summaryWhileSaving, "Saving the alignment result");
            StringAssert.Contains(fixture.Model.Summary, "The alignment result was saved.");
        }

        /// <summary>
        /// A synchronization context that runs what is posted to it at once, on the posting thread, as itself: code that
        /// continues on it sees it as <see cref="SynchronizationContext.Current"/>, as code on the WPF dispatcher sees the
        /// dispatcher's.
        /// </summary>
        private sealed class InlineSynchronizationContext : SynchronizationContext
        {
            public override void Post(SendOrPostCallback d, object? state) => Run(d, state);

            public override void Send(SendOrPostCallback d, object? state) => Run(d, state);

            private void Run(SendOrPostCallback d, object? state) {
                var previous = Current;
                SetSynchronizationContext(this);
                try {
                    d(state);
                }
                finally {
                    SetSynchronizationContext(previous);
                }
            }
        }
    }
}

using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Model.Setting.Tests;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation.LipidRt;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Information.Tests
{
    /// <summary>
    /// A person changing an annotation takes away what the lipid RT check left on it, its own Confirmed tag and its verdict,
    /// which were about the annotation that is replaced (addendum A6: a manual annotation is neither tagged nor used as
    /// evidence). A person's Confirmed stays, and undoing "Set unknown" puts the check's tag and verdict back.
    /// </summary>
    [TestClass]
    public class SetAnnotationUsecaseLipidRtTests
    {
        private const string CheckSource = "MsdialLipidRtCheck/1.0";

        /// <summary>The TG grid after hook B, and TG 52:1, which the check confirmed, as a model.</summary>
        private static (AlignmentSpotProperty Spot, AlignmentSpotPropertyModel Model) Confirmed() {
            var spots = LipidRtTestSpots.Spots();
            LipidRtCheckHooks.RunOnSpots(spots, LipidRtTestSpots.Lipidomics());
            var spot = LipidRtTestSpots.Find(spots, 52, 1);
            Assert.AreEqual(CheckSource, spot.TagCollection.GetSource(PeakSpotTag.CONFIRMED));
            Assert.AreEqual(LipidRtOutcome.Confirmed, spot.LipidRtVerdict.Outcome);
            return (spot, new AlignmentSpotPropertyModel(spot));
        }

        [TestMethod]
        public void SetUnknownWithdrawsTheChecksConfirmedAndVerdictAndUndoPutsThemBack() {
            var (spot, model) = Confirmed();
            var verdict = spot.LipidRtVerdict;
            var undoManager = new UndoManager();
            var changed = new List<string>();
            model.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            new SetAnnotationUsecase(model, model.MatchResultsModel, undoManager).SetUnknown();

            Assert.AreEqual(string.Empty, spot.Name);
            Assert.IsFalse(spot.TagCollection.IsSelected(PeakSpotTag.CONFIRMED), "no Confirmed credited to the check on an unknown");
            Assert.IsFalse(spot.TagCollection.IsSuppressed(PeakSpotTag.CONFIRMED), "a withdrawal is not a person's removal");
            Assert.IsNull(spot.LipidRtVerdict, "no verdict about an annotation the spot no longer has");
            Assert.IsFalse(model.Confirmed);
            Assert.IsNull(model.ConfirmedDetail);
            CollectionAssert.IsSubsetOf(new[] { "Confirmed", "LipidRtVerdict", "ConfirmedDetail" }, changed);

            undoManager.Undo();

            StringAssert.StartsWith(spot.Name, "TG 52:1");
            Assert.IsTrue(spot.TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.AreEqual(CheckSource, spot.TagCollection.GetSource(PeakSpotTag.CONFIRMED), "the check's again");
            Assert.AreSame(verdict, spot.LipidRtVerdict);

            undoManager.Redo();

            Assert.IsFalse(spot.TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.IsNull(spot.LipidRtVerdict);
        }

        [TestMethod]
        public void SettingACompoundWithdrawsTheChecksConfirmedAndVerdict() {
            var (spot, model) = Confirmed();
            var reference = new MoleculeMsReference { Name = "TG 52:2", Ontology = "TG", Formula = new Formula(), AdductType = AdductIon.GetAdductIon("[M+NH4]+"), };
            var result = new MsScanMatchResult { Name = "TG 52:2", Source = SourceType.Manual, };

            new SetAnnotationUsecase(model, model.MatchResultsModel, new UndoManager()).SetConfidence(reference, result);

            Assert.AreEqual("TG 52:2", spot.Name);
            Assert.IsFalse(spot.TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.IsNull(spot.LipidRtVerdict);
        }

        [TestMethod]
        public void APersonsConfirmedStaysThroughTheirAnnotationChange() {
            var spots = LipidRtTestSpots.Spots();
            var spot = LipidRtTestSpots.Find(spots, 52, 1);
            spot.TagCollection.Select(PeakSpotTag.CONFIRMED);
            LipidRtCheckHooks.RunOnSpots(spots, LipidRtTestSpots.Lipidomics());
            Assert.IsNotNull(spot.LipidRtVerdict);
            var model = new AlignmentSpotPropertyModel(spot);

            new SetAnnotationUsecase(model, model.MatchResultsModel, new UndoManager()).SetUnknown();

            Assert.IsTrue(spot.TagCollection.IsSelected(PeakSpotTag.CONFIRMED), "the person's decision, not the check's");
            Assert.IsNull(spot.TagCollection.GetSource(PeakSpotTag.CONFIRMED));
            Assert.IsNull(spot.LipidRtVerdict);
        }

        [TestMethod]
        public void SetUnknownOnAPeakWithdrawsHookAsConfirmedAndVerdict() {
            var peaks = LipidRtTestSpots.Spots().Select(s => {
                var peak = new ChromatogramPeakFeature(new BaseChromatogramPeakFeature
                {
                    Mass = s.MassCenter, PeakHeightTop = 1e5,
                    ChromXsLeft = new ChromXs(s.TimesCenter.Value - 0.02), ChromXsTop = new ChromXs(s.TimesCenter.Value), ChromXsRight = new ChromXs(s.TimesCenter.Value + 0.02),
                })
                {
                    MasterPeakID = s.MasterAlignmentID,
                    Ontology = s.Ontology,
                    Name = s.Name,
                };
                peak.SetAdductType(s.AdductType);
                if (!string.IsNullOrEmpty(s.Name)) {
                    peak.MatchResults.AddResult(s.MatchResults.Representative);
                }
                return peak;
            }).ToList();
            LipidRtCheckHooks.RunOnPeaks(peaks, LipidRtTestSpots.Lipidomics());
            var peak521 = peaks.Single(p => p.Name.StartsWith("TG 52:1|"));
            Assert.AreEqual(CheckSource, peak521.TagCollection.GetSource(PeakSpotTag.CONFIRMED));
            var model = new ChromatogramPeakFeatureModel(peak521);
            var undoManager = new UndoManager();

            new SetAnnotationUsecase(model, model.MatchResultsModel, undoManager).SetUnknown();

            Assert.IsFalse(peak521.TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.IsNull(peak521.LipidRtVerdict);

            undoManager.Undo();

            Assert.AreEqual(CheckSource, peak521.TagCollection.GetSource(PeakSpotTag.CONFIRMED));
            Assert.AreEqual(LipidRtOutcome.Confirmed, peak521.LipidRtVerdict.Outcome);
        }
    }
}

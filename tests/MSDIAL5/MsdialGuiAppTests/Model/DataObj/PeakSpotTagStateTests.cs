using CompMs.MsdialCore.Algorithm.Annotation.LipidRt;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.App.Msdial.Model.DataObj.Tests
{
    [TestClass]
    public class PeakSpotTagStateTests
    {
        private const string CheckSource = "MsdialLipidRtCheck/1.0";

        [TestMethod]
        public void RestorePutsBackSelectionSourceAndSuppression() {
            var tags = new PeakSpotTagCollection();
            tags.Select(PeakSpotTag.CONFIRMED, CheckSource);
            var before = PeakSpotTagState.Capture(tags, PeakSpotTag.CONFIRMED);

            tags.DeselectByPerson(PeakSpotTag.CONFIRMED);
            tags.SelectByPerson(PeakSpotTag.CONFIRMED);
            Assert.IsNull(tags.GetSource(PeakSpotTag.CONFIRMED));

            before.RestoreTo(tags);

            Assert.IsTrue(tags.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.AreEqual(CheckSource, tags.GetSource(PeakSpotTag.CONFIRMED));
            Assert.IsFalse(tags.IsSuppressed(PeakSpotTag.CONFIRMED));
        }

        [TestMethod]
        public void RestorePutsBackASuppressionWithItsSource() {
            var tags = new PeakSpotTagCollection();
            tags.Suppress(PeakSpotTag.CONFIRMED, "Agent/curator/2");
            var before = PeakSpotTagState.Capture(tags, PeakSpotTag.CONFIRMED);
            tags.SelectByPerson(PeakSpotTag.CONFIRMED);

            before.RestoreTo(tags);

            Assert.IsFalse(tags.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.IsTrue(tags.IsSuppressed(PeakSpotTag.CONFIRMED));
            Assert.AreEqual("Agent/curator/2", tags.GetSuppressionSource(PeakSpotTag.CONFIRMED));
        }

        [TestMethod]
        public void RestoreOfAnUnchangedTagKeepsTheOrderOfTheTags() {
            var tags = new PeakSpotTagCollection();
            tags.Select(PeakSpotTag.CONFIRMED);
            tags.Select(PeakSpotTag.MISANNOTATION);
            var before = PeakSpotTagState.Capture(tags, PeakSpotTag.CONFIRMED);

            before.RestoreTo(tags);

            Assert.AreEqual("Confirmed|Misannotation", tags.ToString());
        }

        [TestMethod]
        public void AToolsTagAndAVerdictAreDescribedForTheTagsTooltip() {
            var spot = new AlignmentSpotProperty();
            var model = new AlignmentSpotPropertyModel(spot);
            Assert.IsNull(model.ConfirmedDetail, "nothing to add: the shortcut legend stays");

            spot.TagCollection.Select(PeakSpotTag.CONFIRMED);
            Assert.IsNull(model.ConfirmedDetail, "a person's tag without a verdict needs no explanation");

            spot.LipidRtVerdict = new LipidRtVerdict
            {
                Outcome = LipidRtOutcome.Confirmed, Reason = LipidRtReason.ConfirmedRt, Score = 0.875, RuleVersion = "1.0", ParameterHash = "0123456789abcdef",
                Evidence = "Confirmed: TG 52:1 elutes after TG 50:1 (+0.30 min).",
            };
            StringAssert.StartsWith(model.ConfirmedDetail, "Confirmed: set by a person");
            StringAssert.Contains(model.ConfirmedDetail, "Lipid RT check: Confirmed (ConfirmedRt), score 0.875.");
            StringAssert.Contains(model.ConfirmedDetail, "Confirmed: TG 52:1 elutes after TG 50:1 (+0.30 min).");
            StringAssert.Contains(model.ConfirmedDetail, "Rules 1.0, parameter 0123456789abcdef.");

            spot.TagCollection.Deselect(PeakSpotTag.CONFIRMED);
            spot.TagCollection.Select(PeakSpotTag.CONFIRMED, CheckSource);
            StringAssert.StartsWith(model.ConfirmedDetail, "Confirmed: set by the lipid RT check (MsdialLipidRtCheck/1.0).");

            var changed = false;
            model.PropertyChanged += (s, e) => changed |= e.PropertyName == nameof(AlignmentSpotPropertyModel.ConfirmedDetail);
            model.Confirmed = false;
            Assert.IsTrue(changed);
            StringAssert.StartsWith(model.ConfirmedDetail, "Confirmed: removed by a person");
        }

        [TestMethod]
        public void AnotherToolsTagIsNamedBySource() {
            var feature = new ChromatogramPeakFeature();
            feature.TagCollection.Select(PeakSpotTag.CONFIRMED, "Agent/curator/2");
            var model = new ChromatogramPeakFeatureModel(feature);

            Assert.AreEqual("Confirmed: set by Agent/curator/2.", model.ConfirmedDetail);
        }

        [TestMethod]
        public void TheVerdictLineIsTheEvidenceSentenceOrTheOutcome() {
            Assert.IsNull(LipidRtCheckDescription.Line(null));
            Assert.AreEqual("Not evaluable: x.", LipidRtCheckDescription.Line(new LipidRtVerdict { Outcome = LipidRtOutcome.NotEvaluable, Reason = LipidRtReason.Support, Evidence = "Not evaluable: x." }));
            Assert.AreEqual("Inconsistent (InconsistentOrder)", LipidRtCheckDescription.Line(new LipidRtVerdict { Outcome = LipidRtOutcome.Inconsistent, Reason = LipidRtReason.InconsistentOrder }));
        }
    }
}

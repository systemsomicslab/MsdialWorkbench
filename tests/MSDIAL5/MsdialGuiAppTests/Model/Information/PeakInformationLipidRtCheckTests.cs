using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting.Tests;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.MsdialCore.Algorithm.Annotation.LipidRt;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings;
using System.Linq;

namespace CompMs.App.Msdial.Model.Information.Tests
{
    /// <summary>The peak information panel shows the lipid RT check's verdict where there is one, and nothing otherwise.</summary>
    [TestClass]
    public class PeakInformationLipidRtCheckTests
    {
        [TestMethod]
        public void TheAlignmentPanelShowsTheVerdictOfTheSelectedSpot() {
            var spots = LipidRtTestSpots.Spots();
            LipidRtCheckHooks.RunOnSpots(spots, LipidRtTestSpots.Lipidomics(), null);
            var tg521 = new AlignmentSpotPropertyModel(LipidRtTestSpots.Find(spots, 52, 1));
            var unannotated = new AlignmentSpotPropertyModel(spots[0]);
            var target = new ReactivePropertySlim<AlignmentSpotPropertyModel?>(tg521);
            var model = new PeakInformationAlignmentModel(target);
            var viewModel = new PeakInformationViewModel(model);

            StringAssert.StartsWith(model.LipidRtCheck, "Confirmed: TG 52:1 elutes after ");
            StringAssert.StartsWith(model.LipidRtCheckDetail, "Confirmed: set by the lipid RT check (MsdialLipidRtCheck/1.1).");
            Assert.IsTrue(viewModel.HasLipidRtCheck.Value);
            Assert.AreEqual(model.LipidRtCheck, viewModel.LipidRtCheck.Value);

            target.Value = unannotated;
            Assert.IsNull(model.LipidRtCheck, "an unannotated spot has no verdict");
            Assert.IsFalse(viewModel.HasLipidRtCheck.Value, "the row is hidden");

            target.Value = tg521;
            target.Value = null;
            Assert.IsNull(model.LipidRtCheck, "no spot, no verdict");
            Assert.IsNull(model.LipidRtCheckDetail);
        }

        [TestMethod]
        public void TheAnalysisPanelFollowsAVerdictThatChangesOnTheSelectedPeak() {
            var feature = new ChromatogramPeakFeature();
            var peak = new ChromatogramPeakFeatureModel(feature);
            var model = new PeakInformationAnalysisModel(new ReactivePropertySlim<ChromatogramPeakFeatureModel?>(peak));
            Assert.IsNull(model.LipidRtCheck);

            feature.LipidRtVerdict = new LipidRtVerdict { Outcome = LipidRtOutcome.Inconsistent, Reason = LipidRtReason.InconsistentOrder, Evidence = "Inconsistent: TG 52:1 is out of order in its class." };
            feature.TagCollection.Select(PeakSpotTag.CONFIRMED, "Agent/curator/2");
            peak.Confirmed = false; // a person's toggle raises the change

            Assert.AreEqual("Confirmed: removed by a person after a tool had set it; no tool sets it again." + System.Environment.NewLine
                + "Lipid RT check: Inconsistent (InconsistentOrder)." + System.Environment.NewLine
                + "Inconsistent: TG 52:1 is out of order in its class.", model.LipidRtCheckDetail);
        }
    }
}

using CompMs.App.Msdial.Model.DataObj;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Table.Tests
{
    /// <summary>
    /// "Mark all as confirmed/unconfirmed" is a person's toggle on every row (addendum B1), and its Undo puts each tag back
    /// exactly as it was, so the lipid RT check's own tags stay the check's and a person's suppression is not forgotten.
    /// </summary>
    [TestClass]
    public class MarkAllAsConfirmedCommandTests
    {
        private const string CheckSource = "MsdialLipidRtCheck/1.0";

        [TestMethod]
        public void MarkAllAsUnconfirmedSuppressesTheChecksTagsAndUndoGivesThemBackToTheCheck() {
            var byCheck = new AlignmentSpotProperty();
            byCheck.TagCollection.Select(PeakSpotTag.CONFIRMED, CheckSource);
            var byPerson = new AlignmentSpotProperty();
            byPerson.TagCollection.Select(PeakSpotTag.CONFIRMED);
            var untagged = new AlignmentSpotProperty();
            var models = new List<AlignmentSpotPropertyModel>
            {
                new AlignmentSpotPropertyModel(byCheck), new AlignmentSpotPropertyModel(byPerson), new AlignmentSpotPropertyModel(untagged),
            };
            var command = new MarkAllAsConfirmedCommand<AlignmentSpotPropertyModel>(models, false);

            command.Do();

            Assert.IsFalse(byCheck.TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.IsTrue(byCheck.TagCollection.IsSuppressed(PeakSpotTag.CONFIRMED), "a person removed a tool's tag");
            Assert.IsFalse(byPerson.TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.IsFalse(byPerson.TagCollection.IsSuppressed(PeakSpotTag.CONFIRMED), "a person's own tag is just removed");
            Assert.IsFalse(untagged.TagCollection.HasEntries());

            command.Undo();

            Assert.IsTrue(byCheck.TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.AreEqual(CheckSource, byCheck.TagCollection.GetSource(PeakSpotTag.CONFIRMED), "the check's again, so a re-run can withdraw it");
            Assert.IsFalse(byCheck.TagCollection.IsSuppressed(PeakSpotTag.CONFIRMED));
            Assert.IsTrue(byPerson.TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.IsNull(byPerson.TagCollection.GetSource(PeakSpotTag.CONFIRMED));
            Assert.IsFalse(untagged.TagCollection.HasEntries());
            Assert.IsTrue(models[0].Confirmed);

            command.Do(); // Redo

            Assert.IsTrue(byCheck.TagCollection.IsSuppressed(PeakSpotTag.CONFIRMED));
            Assert.IsFalse(byPerson.TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
        }

        [TestMethod]
        public void UndoOfMarkAllAsConfirmedKeepsWhatAPersonHadRemoved() {
            var suppressed = new ChromatogramPeakFeature();
            suppressed.TagCollection.Select(PeakSpotTag.CONFIRMED, CheckSource);
            suppressed.TagCollection.DeselectByPerson(PeakSpotTag.CONFIRMED);
            var byCheck = new ChromatogramPeakFeature();
            byCheck.TagCollection.Select(PeakSpotTag.CONFIRMED, CheckSource);
            var models = new List<ChromatogramPeakFeatureModel> { new ChromatogramPeakFeatureModel(suppressed), new ChromatogramPeakFeatureModel(byCheck), };
            var command = new MarkAllAsConfirmedCommand<ChromatogramPeakFeatureModel>(models, true);

            command.Do();

            Assert.IsTrue(suppressed.TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.IsNull(suppressed.TagCollection.GetSource(PeakSpotTag.CONFIRMED), "turned on by a person");
            Assert.IsFalse(suppressed.TagCollection.IsSuppressed(PeakSpotTag.CONFIRMED));
            Assert.AreEqual(CheckSource, byCheck.TagCollection.GetSource(PeakSpotTag.CONFIRMED), "a tag that is already on keeps its source");

            command.Undo();

            Assert.IsFalse(suppressed.TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.IsTrue(suppressed.TagCollection.IsSuppressed(PeakSpotTag.CONFIRMED), "the person's earlier removal holds again");
            Assert.AreEqual(PeakSpotTagCollection.PersonSource, suppressed.TagCollection.GetSuppressionSource(PeakSpotTag.CONFIRMED));
            Assert.AreEqual(CheckSource, byCheck.TagCollection.GetSource(PeakSpotTag.CONFIRMED));
            Assert.IsFalse(models[0].Confirmed);
        }

        [TestMethod]
        public void UndoTellsTheViews() {
            var spot = new AlignmentSpotProperty();
            spot.TagCollection.Select(PeakSpotTag.CONFIRMED, CheckSource);
            var model = new AlignmentSpotPropertyModel(spot);
            var command = new MarkAllAsConfirmedCommand<AlignmentSpotPropertyModel>(new[] { model }, false);
            command.Do();
            var changed = new List<string>();
            model.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            command.Undo();

            CollectionAssert.Contains(changed, nameof(AlignmentSpotPropertyModel.Confirmed));
            CollectionAssert.Contains(changed, nameof(AlignmentSpotPropertyModel.ConfirmedDetail));
        }
    }
}

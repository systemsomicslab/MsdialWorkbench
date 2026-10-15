using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.MsdialCore.DataObj.Tests
{
    /// <summary>
    /// Tag provenance (addendum B1): who set a tag, and which tags a person removed after a tool had set them. The
    /// existing Select/Deselect keep their meaning; the tool path and the person's toggles are new.
    /// </summary>
    [TestClass]
    public class PeakSpotTagCollectionTests
    {
        private const string Tool = "MsdialLipidRtCheck/1.0";

        [TestMethod]
        public void TheExistingCallsKeepTheirMeaning() {
            var tags = new PeakSpotTagCollection();
            tags.Select(PeakSpotTag.MISANNOTATION);
            tags.Select(PeakSpotTag.CONFIRMED);
            tags.Select(PeakSpotTag.CONFIRMED);

            CollectionAssert.AreEqual(new[] { PeakSpotTag.MISANNOTATION, PeakSpotTag.CONFIRMED }, tags.Selected.ToArray());
            Assert.AreEqual("Misannotation|Confirmed", tags.ToString());
            Assert.IsTrue(tags.Any());
            Assert.IsNull(tags.GetSource(PeakSpotTag.CONFIRMED), "a tag selected the old way has no recorded source");

            tags.Deselect(PeakSpotTag.MISANNOTATION);
            CollectionAssert.AreEqual(new[] { PeakSpotTag.CONFIRMED }, tags.Selected.ToArray());
            Assert.IsFalse(tags.IsSuppressed(PeakSpotTag.MISANNOTATION), "Deselect alone never suppresses");
            Assert.AreEqual(0, tags.Suppressed.Count);
        }

        [TestMethod]
        public void AToolRecordsItsSourceAndNeverTakesOverAPersonsTag() {
            var tags = new PeakSpotTagCollection();
            tags.Select(PeakSpotTag.CONFIRMED, Tool);
            Assert.AreEqual(Tool, tags.GetSource(PeakSpotTag.CONFIRMED));
            Assert.AreEqual("Confirmed", tags.ToString(), "the text form does not change");

            var persons = new PeakSpotTagCollection();
            persons.Select(PeakSpotTag.CONFIRMED);
            persons.Select(PeakSpotTag.CONFIRMED, Tool);
            Assert.IsNull(persons.GetSource(PeakSpotTag.CONFIRMED), "a person's tag stays a person's");

            tags.Deselect(PeakSpotTag.CONFIRMED);
            Assert.IsNull(tags.GetSource(PeakSpotTag.CONFIRMED), "a deselected tag has no source");
            tags.Select(PeakSpotTag.CONFIRMED);
            Assert.IsNull(tags.GetSource(PeakSpotTag.CONFIRMED), "the old source does not come back");
        }

        [TestMethod]
        public void APersonTurningOffAToolsTagSuppressesIt() {
            var tags = new PeakSpotTagCollection();
            tags.Select(PeakSpotTag.CONFIRMED, Tool);

            tags.DeselectByPerson(PeakSpotTag.CONFIRMED);

            Assert.IsFalse(tags.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.IsTrue(tags.IsSuppressed(PeakSpotTag.CONFIRMED));
            Assert.AreEqual(PeakSpotTagCollection.PersonSource, tags.GetSuppressionSource(PeakSpotTag.CONFIRMED));
            Assert.IsFalse(tags.Any(), "a suppression is not a selected tag");
            Assert.IsTrue(tags.HasEntries(), "but it is written to the tag file");
        }

        [TestMethod]
        public void APersonTurningOffTheirOwnTagDoesNotSuppressIt() {
            var tags = new PeakSpotTagCollection();
            tags.SelectByPerson(PeakSpotTag.CONFIRMED);

            tags.DeselectByPerson(PeakSpotTag.CONFIRMED);

            Assert.IsFalse(tags.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.IsFalse(tags.IsSuppressed(PeakSpotTag.CONFIRMED));
            Assert.IsFalse(tags.HasEntries());
        }

        [TestMethod]
        public void APersonTurningATagOnAgainOwnsItAndLiftsTheSuppression() {
            var tags = new PeakSpotTagCollection();
            tags.Select(PeakSpotTag.CONFIRMED, Tool);
            tags.DeselectByPerson(PeakSpotTag.CONFIRMED);

            tags.SelectByPerson(PeakSpotTag.CONFIRMED);

            Assert.IsTrue(tags.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.IsNull(tags.GetSource(PeakSpotTag.CONFIRMED), "now the person's");
            Assert.IsFalse(tags.IsSuppressed(PeakSpotTag.CONFIRMED));
        }

        [TestMethod]
        public void APersonTurningOnAToolsTagTakesItOverInPlace() {
            var tags = new PeakSpotTagCollection();
            tags.Select(PeakSpotTag.CONFIRMED, Tool);
            tags.Select(PeakSpotTag.COELUTION);

            tags.SelectByPerson(PeakSpotTag.CONFIRMED);

            Assert.IsNull(tags.GetSource(PeakSpotTag.CONFIRMED), "now the person's, out of the tool's reach");
            Assert.AreEqual("Confirmed|Coelution (mixed spectra)", tags.ToString(), "the tag keeps its place, so the exported text does not change");
            Assert.IsFalse(tags.IsSuppressed(PeakSpotTag.CONFIRMED));
        }

        [TestMethod]
        public void OnlyTheRemovalOfAToolsTagIsSuppressed() {
            // Addendum B1: a suppression records that a person removed a tool's tag. Once the person turned it on again it
            // is the person's, and removing it again is the removal of the person's own tag.
            var tags = new PeakSpotTagCollection();
            tags.Select(PeakSpotTag.CONFIRMED, Tool);
            tags.DeselectByPerson(PeakSpotTag.CONFIRMED);
            tags.SelectByPerson(PeakSpotTag.CONFIRMED);

            tags.DeselectByPerson(PeakSpotTag.CONFIRMED);

            Assert.IsFalse(tags.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.IsFalse(tags.IsSuppressed(PeakSpotTag.CONFIRMED));
        }

        [TestMethod]
        public void SuppressionsKeepTheirSource() {
            var tags = new PeakSpotTagCollection();
            tags.Suppress(PeakSpotTag.CONFIRMED, "Agent/curator/2");
            Assert.AreEqual("Agent/curator/2", tags.GetSuppressionSource(PeakSpotTag.CONFIRMED));
            tags.Suppress(PeakSpotTag.CONFIRMED);
            Assert.AreEqual(PeakSpotTagCollection.PersonSource, tags.GetSuppressionSource(PeakSpotTag.CONFIRMED));
            Assert.AreEqual(1, tags.Suppressed.Count);
            tags.Unsuppress(PeakSpotTag.CONFIRMED);
            Assert.IsFalse(tags.IsSuppressed(PeakSpotTag.CONFIRMED));
            Assert.IsNull(tags.GetSuppressionSource(PeakSpotTag.CONFIRMED));
        }
    }
}

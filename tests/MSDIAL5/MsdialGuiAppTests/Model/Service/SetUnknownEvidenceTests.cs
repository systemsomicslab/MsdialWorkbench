using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.App.Msdial.Model.Service.Tests
{
    /// <summary>
    /// The one site that records <see cref="AnnotationEvidenceSource.Manual"/>: a person declaring
    /// a peak unknown.
    /// </summary>
    /// <remarks>
    /// Manual is deliberately narrow. A person ACCEPTING a candidate does not get relabelled,
    /// because the object they accept is the annotator's own instance, stored by reference with no
    /// clone anywhere in the write path -- so stamping Manual there would overwrite the only record
    /// of what the annotator compared, in exchange for a fact SourceType.Manual and
    /// IsManuallyModified already carry. Marking a peak unknown is different: the result is built
    /// fresh with no name, no key and no score, so there is nothing to overwrite and no comparison
    /// to describe. Without the record a deliberate human "unknown" is byte-identical to an
    /// unannotated peak from a project written before the evidence record existed.
    /// </remarks>
    [TestClass()]
    public class SetUnknownEvidenceTests
    {
        [TestMethod()]
        public void MarkingAPeakUnknownRecordsThatAPersonDidIt() {
            var container = new MsScanMatchResultContainerModel(new MsScanMatchResultContainer());
            var molecule = new MoleculeProperty { Name = "some annotation", };
            IDoCommand command = new SetUnknownDoCommand(molecule, container);

            command.Do();

            var unknown = container.MatchResults.Single(r => r.IsUnknown);
            Assert.AreEqual(AnnotationEvidenceSource.Manual, unknown.EvidenceSource);
            Assert.IsTrue(unknown.IsManuallyModified,
                "and SourceType.Manual is kept as well, so neither record replaces the other");
        }

        [TestMethod()]
        public void UndoRemovesTheRecordRatherThanRelabellingIt() {
            // An undo must restore, not relabel: the results it re-adds are the same instances that
            // were removed and already carry whatever their producers recorded.
            var container = new MsScanMatchResultContainerModel(new MsScanMatchResultContainer());
            var accepted = new MsScanMatchResult
            {
                Name = "an accepted candidate",
                Source = SourceType.MspDB | SourceType.Manual,
                EvidenceSource = AnnotationEvidenceSource.ReferenceSpectrum,
            };
            container.AddResult(accepted);
            var molecule = new MoleculeProperty { Name = "an accepted candidate", };
            IDoCommand command = new SetUnknownDoCommand(molecule, container);

            command.Do();
            command.Undo();

            Assert.AreEqual(AnnotationEvidenceSource.ReferenceSpectrum, accepted.EvidenceSource,
                "the restored result keeps its producer's evidence source");
        }
    }
}

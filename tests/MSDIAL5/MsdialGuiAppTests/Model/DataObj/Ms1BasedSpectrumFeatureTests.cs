using CompMs.MsdialCore.DataObj.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.IO;

namespace CompMs.App.Msdial.Model.DataObj.Tests
{
    [TestClass()]
    public class Ms1BasedSpectrumFeatureTests
    {
        [TestMethod()]
        public void GetCurrentSpectrumFeatureTest() {
            var spectrumFeature = SpectrumFeatureTestHelper.CreateSample();
            spectrumFeature.Comment = "";
            var model = new Ms1BasedSpectrumFeature(spectrumFeature)
            {
                Comment = "ABCD",
                QuantifiedChromatogramPeak = QuantifiedChromatogramPeakTestHelper.CreateSample().WithMs1RawSpectrumIdTop(80),
            };

            var actual = model.GetCurrentSpectrumFeature();
            Assert.AreEqual(80, actual.QuantifiedChromatogramPeak.MS1RawSpectrumIdTop);
            Assert.AreEqual("ABCD", actual.Comment);
        }

        [TestMethod]
        public void SetConfidenceWritesMsfinderResultToAnUnannotatedSpectrumAndPersistsIt() {
            var scan = new MSDecResult { ModelPeakMz = 73 };
            var annotated = new AnnotatedMSDecResult(scan, new MsScanMatchResultContainer());
            var feature = new SpectrumFeature(annotated, QuantifiedChromatogramPeakTestHelper.CreateSample());
            using var model = new Ms1BasedSpectrumFeature(feature);
            var writeBack = new SetAnnotationUsecase(model.Molecule, model.MatchResults, new UndoManager());
            var reference = new MoleculeMsReference {
                Name = "Ethanol",
                Formula = new Formula { FormulaString = "C2H6O" },
                SMILES = "CCO",
                InChIKey = "LFQSCWFLJHTTHZ-UHFFFAOYSA-N",
                Ontology = "Alcohol",
            };
            var match = new MsScanMatchResult {
                Source = SourceType.Manual,
                Name = reference.Name,
                EvidenceSource = AnnotationEvidenceSource.ByStructurePredictionTool,
            };

            Assert.AreEqual(string.Empty, model.Molecule.Name);
            Assert.IsNull(annotated.Molecule);

            writeBack.SetConfidence(reference, match);

            Assert.AreEqual(reference.Name, model.Molecule.Name);
            Assert.AreEqual(reference.SMILES, model.Molecule.SMILES);
            Assert.AreSame(match, annotated.MatchResults.Representative);
            Assert.IsTrue(match.IsReferenceMatched);

            using var stream = new MemoryStream();
            feature.Save(stream);
            stream.Position = 0;
            var restored = SpectrumFeature.Load(stream).AnnotatedMSDecResult;
            Assert.AreEqual(reference.Name, restored.Molecule?.Name);
            Assert.AreEqual(reference.Formula.FormulaString, restored.Molecule?.Formula.FormulaString);
            Assert.AreEqual(reference.SMILES, restored.Molecule?.SMILES);
            Assert.AreEqual(reference.InChIKey, restored.Molecule?.InChIKey);
            Assert.AreEqual(reference.Ontology, restored.Molecule?.Ontology);
            Assert.AreEqual(match.EvidenceSource, restored.MatchResults.Representative.EvidenceSource);
            Assert.AreEqual(73d, restored.QuantMass);
        }
    }
}

using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace MsdialGuiAppTests.Model.Search;

[TestClass]
public sealed class RealLcmsLibraryTests
{
    public TestContext TestContext { get; set; } = null!;

    [DataTestMethod]
    [TestCategory("LocalIntegration")]
    [DataRow("MSDIAL_TEST_LC_POSITIVE_MSP_FILE", TargetOmics.Metabolomics)]
    [DataRow("MSDIAL_TEST_LC_POSITIVE_MSP_FILE", TargetOmics.Lipidomics)]
    [DataRow("MSDIAL_TEST_LC_NEGATIVE_MSP_FILE", TargetOmics.Metabolomics)]
    [DataRow("MSDIAL_TEST_LC_NEGATIVE_MSP_FILE", TargetOmics.Lipidomics)]
    public void SharedImportAndReloadPreserveRealLibraryRecordsAndAnnotationResults(string variable, TargetOmics omics) {
        var path = Environment.GetEnvironmentVariable(variable);
        if (!File.Exists(path)) { Assert.Inconclusive($"Set {variable} to an approved local MSP library."); }
        var original = MspFileParser.MspFileReader(path);
        var imported = LibraryHandler.ReadMsLibrary(path, null!, out var error);
        Assert.AreEqual(string.Empty, error);
        Assert.IsTrue(original.Count > 0);
        Assert.AreEqual(original.Count, imported.Count);
        var database = new MoleculeDataBase(imported, "Test", DataBaseSource.Msp, SourceType.MspDB, "");
        using var saved = new MemoryStream();
        database.Save(saved);
        saved.Position = 0;
        var reloaded = new MoleculeDataBase("Test", SourceType.MspDB, DataBaseSource.Msp, "");
        reloaded.Load(saved, "");
        Assert.AreEqual(original.Count, reloaded.Database.Count);
        for (var index = 0; index < original.Count; index++) {
            var expected = MessagePackSerializer.Serialize(original[index]);
            CollectionAssert.AreEqual(expected, MessagePackSerializer.Serialize(imported[index].CreateSerializationCopy()));
            CollectionAssert.AreEqual(expected, MessagePackSerializer.Serialize(reloaded.Database[index].CreateSerializationCopy()));
        }
        var parameter = new MsRefSearchParameterBase { Ms1Tolerance = .01f, Ms2Tolerance = .025f };
        var legacyDatabase = new MoleculeDataBase(original, "Test", DataBaseSource.Msp, SourceType.MspDB, "");
        var legacyAnnotator = new LcmsMspAnnotator(legacyDatabase, parameter, omics, "Test", 0);
        var importedAnnotator = new LcmsMspAnnotator(database, parameter, omics, "Test", 0);
        var loadedAnnotator = new LcmsMspAnnotator(reloaded, parameter, omics, "Test", 0);
        var spectra = original.Where(record => record.Spectrum?.Count > 0).ToArray();
        Assert.IsTrue(spectra.Length > 0);
        var importedCompactBefore = imported.Count(record => record.IsSpectrumCompact);
        var reloadedCompactBefore = reloaded.Database.Count(record => record.IsSpectrumCompact);
        var samples = Math.Min(25, spectra.Length);
        var candidates = 0;
        for (var sample = 0; sample < samples; sample++) {
            var reference = spectra[sample * (spectra.Length - 1) / Math.Max(1, samples - 1)];
            var query = new ChromatogramPeakFeature {
                PrecursorMz = reference.PrecursorMz, IonMode = reference.IonMode, ChromXs = reference.ChromXs,
                Spectrum = reference.Spectrum.Select(peak => new SpectrumPeak(peak.Mass, peak.Intensity)).ToList(),
            };
            var expected = Search(legacyAnnotator);
            Assert.IsTrue(expected.Length > 0);
            foreach (var annotator in new[] { importedAnnotator, loadedAnnotator }) {
                var actual = Search(annotator);
                Assert.AreEqual(expected.Length, actual.Length);
                for (var rank = 0; rank < expected.Length; rank++) {
                    CollectionAssert.AreEqual(MessagePackSerializer.Serialize(expected[rank]), MessagePackSerializer.Serialize(actual[rank]));
                }
            }
            candidates += expected.Length;
            MsScanMatchResult[] Search(LcmsMspAnnotator annotator) => annotator.FindCandidates(
                new AnnotationQuery(query, query, null, null, parameter, annotator, ignoreIsotopicPeak: false)).ToArray();
        }
        Assert.AreEqual(importedCompactBefore, imported.Count(record => record.IsSpectrumCompact));
        Assert.AreEqual(reloadedCompactBefore, reloaded.Database.Count(record => record.IsSpectrumCompact));
        TestContext.WriteLine($"{omics}: {original.Count} complete records; {samples} library-derived queries; {candidates} ranked candidates identical after import and reload. Compact records before/after scoring: {importedCompactBefore}/{imported.Count(record => record.IsSpectrumCompact)} imported, {reloadedCompactBefore}/{reloaded.Database.Count(record => record.IsSpectrumCompact)} reloaded.");
    }
}

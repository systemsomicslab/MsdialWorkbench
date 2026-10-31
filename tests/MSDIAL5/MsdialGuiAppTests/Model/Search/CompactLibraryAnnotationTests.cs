using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.Common.Parameter;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Algorithm.Annotation;
using CompMs.MsdialImmsCore.Algorithm.Annotation;
using CompMs.MsdialLcImMsApi.Algorithm.Annotation;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MsdialGuiAppTests.Model.Search;

[TestClass]
public sealed class CompactLibraryAnnotationTests
{
    public static IEnumerable<object[]> Cases {
        get {
            foreach (var mode in new[] { "LCMS", "DIMS", "IMMS", "LCIMMS" })
                foreach (var omics in new[] { TargetOmics.Metabolomics, TargetOmics.Lipidomics })
                    foreach (var annotated in new[] { false, true })
                        foreach (var query in new[] { 0, 1, 2, 3 })
                            yield return new object[] { mode, omics, annotated, query };
        }
    }

    [DataTestMethod]
    [DynamicData(nameof(Cases))]
    public void LegacyLibraryReloadPreservesCandidatesScoresStructureLevelsAndExport(string mode, TargetOmics omics, bool annotated, int queryKind) {
        var parameter = new MsRefSearchParameterBase {
            Ms1Tolerance = .01f, Ms2Tolerance = .025f, RtTolerance = .5f, CcsTolerance = 5,
            IsUseTimeForAnnotationScoring = true, IsUseTimeForAnnotationFiltering = true,
            IsUseCcsForAnnotationScoring = true, IsUseCcsForAnnotationFiltering = true,
        };
        var records = Enumerable.Range(0, 6).Select(index => new MoleculeMsReference {
            ScanID = index, Name = "PC 16:0_18:1", CompoundClass = "PC", InChIKey = "Reference" + index,
            PrecursorMz = 760.585 + index * .001, AdductType = AdductIon.GetAdductIon("[M+H]+"), IonMode = IonMode.Positive,
            ChromXs = new ChromXs(2 + index * .02, ChromXType.RT, ChromXUnit.Min), CollisionCrossSection = 200 + index,
            Spectrum = Enumerable.Range(0, 20).Select(peak => new SpectrumPeak(60.1234 + peak * 17.002,
                100 - (peak * (index + 3) % 97), annotated ? "fragment" : null,
                annotated ? SpectrumComment.acylchain : SpectrumComment.none))
                .Append(new SpectrumPeak(184.0733, 1000)).OrderBy(peak => peak.Mass).ToList(),
        }).ToList();
        var target = new ChromatogramPeakFeature {
            PrecursorMz = 760.585, IonMode = IonMode.Positive,
            ChromXs = new ChromXs(queryKind == 3 ? 10 : 2, ChromXType.RT, ChromXUnit.Min),
            CollisionCrossSection = queryKind == 3 ? 240 : 200,
            Spectrum = queryKind == 2 ? [] : records[0].Spectrum
                .Select(peak => new SpectrumPeak(peak.Mass + (queryKind == 1 ? .5 : 0), peak.Intensity)).ToList(),
        };
        var original = new MoleculeDataBase(records, "Test", DataBaseSource.Msp, SourceType.MspDB, "");
        var expected = Search(original);
        if (queryKind == 0 && omics == TargetOmics.Lipidomics) {
            Assert.IsTrue(expected.Any(result => result.IsLipidClassMatch), "The fixture must exercise a positive structural verdict.");
        }
        using var legacy = new MemoryStream();
        LargeListMessagePack.Serialize(legacy, records);
        legacy.Position = 0;
        var reloaded = new MoleculeDataBase("Test", SourceType.MspDB, DataBaseSource.Msp, "");
        reloaded.Load(legacy, "");
        Assert.AreEqual(annotated ? 0 : records.Count, reloaded.Database.Count(record => record.IsSpectrumCompact));
        var actual = Search(reloaded);
        Assert.AreEqual(annotated ? 0 : records.Count, reloaded.Database.Count(record => record.IsSpectrumCompact),
            "Standard molecular scoring must retain compact reference peaks in every mode.");
        Assert.AreEqual(expected.Count, actual.Count);
        if (queryKind != 3) { Assert.IsTrue(actual.Count > 0); }
        for (var index = 0; index < expected.Count; index++) {
            CollectionAssert.AreEqual(MessagePackSerializer.Serialize(expected[index]), MessagePackSerializer.Serialize(actual[index]),
                "All result fields, including refined names, structure levels, evidence and verdicts must be identical at the same candidate rank.");
        }
        using var expectedExport = new MemoryStream();
        using var actualExport = new MemoryStream();
        MspFileParser.WriteAsMsp(expectedExport, records);
        MspFileParser.WriteAsMsp(actualExport, reloaded.Database);
        CollectionAssert.AreEqual(expectedExport.ToArray(), actualExport.ToArray());
        using var saved = new MemoryStream();
        reloaded.Save(saved);
        saved.Position = 0;
        var oldReader = LargeListMessagePack.Deserialize<MoleculeMsReference>(saved);
        for (var index = 0; index < records.Count; index++) {
            CollectionAssert.AreEqual(MessagePackSerializer.Serialize(records[index]), MessagePackSerializer.Serialize(oldReader[index]));
        }

        List<MsScanMatchResult> Search(MoleculeDataBase database) {
            switch (mode) {
                case "LCMS": {
                    var annotator = new LcmsMspAnnotator(database, parameter, omics, "Test", 0);
                    return annotator.FindCandidates(new AnnotationQuery(target, target, null, null, parameter, annotator, ignoreIsotopicPeak: false));
                }
                case "DIMS": {
                    var annotator = new DimsMspAnnotator(database, parameter, omics, "Test", 0);
                    return annotator.FindCandidates(new AnnotationQuery(target, target, null, null, parameter, annotator, ignoreIsotopicPeak: false));
                }
                case "IMMS": {
                    var annotator = new ImmsMspAnnotator(database, parameter, omics, "Test", 0);
                    return annotator.FindCandidates(new AnnotationQuery(target, target, null, null, parameter, annotator, ignoreIsotopicPeak: false));
                }
                default: {
                    var annotator = new LcimmsMspAnnotator(database, parameter, omics, "Test", 0);
                    return annotator.FindCandidates(new AnnotationQuery(target, target, null, null, parameter, annotator, ignoreIsotopicPeak: false));
                }
            }
        }
    }
}

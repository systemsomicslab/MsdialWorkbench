using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

namespace CompMs.MsdialCoreTests.DataObj;

[TestClass]
public sealed class LegacyProjectLibraryTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void UpstreamLibraryReopensSearchesAndPreservesEditsAfterResaving(bool nestedArchive) {
        // Generated once with unmodified upstream 6b73c623; expected bytes never use the current writer.
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "Resources", "Serialization", "legacy-library-6b73c623.json")));
        byte[] Bytes(string name) => Convert.FromBase64String(fixture.RootElement.GetProperty(name).GetString());
        var records = MessagePackSerializer.Deserialize<MoleculeMsReference[]>(Bytes("Records"));
        using var archive = Repackage(Bytes("Archive"), nestedArchive);
        var loaded = Load(archive);
        var database = loaded.MetabolomicsDataBases.Single().DataBase;
        Assert.AreEqual(4, database.Database.Count);
        Assert.IsTrue(database.Database[0].IsSpectrumCompact);
        Assert.IsFalse(database.Database[1].IsSpectrumCompact, "Diagnostic fragments must retain their metadata.");
        Assert.IsNull(database.Database[2].Spectrum);
        Assert.AreEqual(0, database.Database[3].Spectrum.Count);
        for (var index = 0; index < records.Length; index++) {
            CollectionAssert.AreEqual(MessagePackSerializer.Serialize(records[index]),
                MessagePackSerializer.Serialize(database.Database[index].CreateSerializationCopy()));
        }
        var results = Search(loaded, records[0]);
        CollectionAssert.AreEqual(Bytes("Results"), MessagePackSerializer.Serialize(results),
            "Restored annotators must preserve every upstream score, verdict and candidate rank.");

        var feature = new ChromatogramPeakFeature { Name = records[0].Name, PrecursorMz = 300 };
        var annotation = results[0].Clone();
        annotation.Source |= SourceType.Manual;
        feature.MatchResults.AddResult(annotation);
        var featureCopy = MessagePackSerializer.Deserialize<ChromatogramPeakFeature>(MessagePackSerializer.Serialize(feature));
        Assert.AreEqual(feature.Name, featureCopy.Name);
        CollectionAssert.AreEqual(MessagePackSerializer.Serialize(annotation),
            MessagePackSerializer.Serialize(featureCopy.MatchResults.Representative));

        var editable = database.Database[0].Spectrum;
        editable[0].Intensity += 3;
        database.Database[0].Name = "Edited reference";
        Assert.AreSame(editable, database.Database[0].Spectrum);
        var beforeSave = database.Database.Select(record =>
            MessagePackSerializer.Serialize(record.CreateSerializationCopy())).ToArray();
        var scoresAfterEdit = Search(loaded, records[0]);
        using var saved = new MemoryStream();
        loaded.Save(saved);
        saved.Position = 0;
        var reopened = Load(saved);
        for (var index = 0; index < records.Length; index++) {
            CollectionAssert.AreEqual(beforeSave[index],
                MessagePackSerializer.Serialize(reopened.MetabolomicsDataBases.Single().DataBase.Database[index].CreateSerializationCopy()));
        }
        CollectionAssert.AreEqual(MessagePackSerializer.Serialize(scoresAfterEdit),
            MessagePackSerializer.Serialize(Search(reopened, records[0])));
    }

    private static DataBaseStorage Load(Stream stream) {
        var parameter = new ParameterBase { TargetOmics = TargetOmics.Metabolomics };
        return DataBaseStorage.Load(stream, new StandardLoadAnnotatorVisitor(parameter),
            new StandardAnnotationQueryFactoryGenerationVisitor(parameter.PeakPickBaseParam, parameter.RefSpecMatchBaseParam), "");
    }

    private static MsScanMatchResult[] Search(DataBaseStorage storage, MoleculeMsReference reference) {
        var factory = storage.MetabolomicsDataBases.Single().Pairs.Single().AnnotationQueryFactory;
        var property = new ChromatogramPeakFeature { PrecursorMz = 300, IonMode = IonMode.Positive, ChromXs = new ChromXs(2) };
        var scan = new MSScanProperty {
            PrecursorMz = 300, IonMode = IonMode.Positive, ChromXs = new ChromXs(2),
            Spectrum = reference.Spectrum.Select(peak => peak.Clone()).ToList(),
        };
        return factory.Create(property, scan, Array.Empty<CompMs.Common.DataObj.RawPeakElement>(),
            new IonFeatureCharacter { IsotopeWeightNumber = 0 }, factory.PrepareParameter())
            .FindCandidates(forceFind: true).ToArray();
    }

    private static MemoryStream Repackage(byte[] bytes, bool nestedArchive) {
        using var input = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        var result = new MemoryStream();
        using (var output = new ZipArchive(result, ZipArchiveMode.Create, leaveOpen: true)) {
            foreach (var entry in input.Entries.Where(entry => !nestedArchive || entry.FullName == "Storage")) {
                Copy(entry, output, Path.Combine(entry.FullName.Split('\\', '/')));
            }
            if (nestedArchive) {
                using var entry = output.CreateEntry(Path.Combine("MetabolomicsDB", "Legacy")).Open();
                using var nested = new ZipArchive(entry, ZipArchiveMode.Create);
                foreach (var source in input.Entries.Where(entry => entry.FullName != "Storage")) {
                    Copy(source, nested, Path.Combine(source.FullName.Split('\\', '/').Skip(2).ToArray()));
                }
            }
        }
        result.Position = 0;
        return result;
    }

    private static void Copy(ZipArchiveEntry source, ZipArchive output, string name) {
        using var input = source.Open();
        using var destination = output.CreateEntry(name).Open();
        input.CopyTo(destination);
    }
}

using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore.DataObj;
using CompMs.MsdialGcMsApi.DataObj;
using CompMs.MsdialImmsCore.DataObj;
using CompMs.MsdialLcImMsApi.DataObj;
using CompMs.MsdialLcMsApi.DataObj;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MsdialGuiAppTests.Model.Search;

[TestClass]
public sealed class LegacyMspDatasetRoundTripTests
{
    [DataTestMethod]
    [DataRow("GC")] [DataRow("LC")] [DataRow("DI")] [DataRow("IM")] [DataRow("LCIM")]
    public async Task DatasetSerializerPreservesCompactLibrariesAndLegacySidecarContents(string mode) {
        (IMsdialDataStorage<ParameterBase>, IMsdialSerializer) pair = mode switch {
            "GC" => (new MsdialGcmsDataStorage { MsdialGcmsParameter = new() }, MsdialGcmsDataStorage.Serializer),
            "LC" => (new MsdialLcmsDataStorage { MsdialLcmsParameter = new() }, MsdialLcmsDataStorage.Serializer),
            "DI" => (new MsdialDimsDataStorage { MsdialDimsParameter = new() }, MsdialDimsDataStorage.Serializer),
            "IM" => (new MsdialImmsDataStorage { MsdialImmsParameter = new() }, MsdialImmsDataStorage.Serializer),
            _ => (new MsdialLcImMsDataStorage { MsdialLcImMsParameter = new() }, MsdialLcImMsDataStorage.Serializer),
        };
        var (storage, serializer) = pair;
        var references = new List<MoleculeMsReference> {
            new() { ScanID = 0, Name = "Plain", Spectrum = new List<SpectrumPeak> { new(73, 100), new(147, 50) } },
            new() { ScanID = 1, Name = "Rich", Spectrum = new List<SpectrumPeak> { new(100, 200, "diagnostic") } },
        };
        var expected = MessagePackSerializer.Serialize(references);
        Assert.IsTrue(references[0].TryCompactSpectrum());
        storage.MspDB = references;
        storage.DataBaseMapper = new DataBaseMapper();
        storage.DataBases = DataBaseStorage.CreateEmpty();
        storage.DataBases.AddMoleculeDataBase(new MoleculeDataBase(references, "Test", DataBaseSource.Msp, SourceType.MspDB, ""),
            new List<IAnnotatorParameterPair<MoleculeDataBase>>());
        var folder = Path.Combine(Path.GetTempPath(), "msdial-dataset-msp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try {
            using (IStreamManager manager = new DirectoryTreeStreamManager(folder)) {
                await storage.SaveAsync(manager, "Dataset.mddata", "");
                manager.Complete();
            }
            Assert.IsTrue(references[0].IsSpectrumCompact, "Saving either library representation must not expand the retained reference.");
            CollectionAssert.AreEqual(expected, MessagePackSerializer.Serialize(MoleculeMsRefMethods.LoadMspFromFile(Path.Combine(folder, "Dataset_Loaded.msp2"))));
            using var input = new DirectoryTreeStreamManager(folder);
            var loaded = await serializer.LoadAsync(input, "Dataset.mddata", folder, "");
            Assert.IsTrue(loaded.MspDB[0].IsSpectrumCompact);
            Assert.IsFalse(loaded.MspDB[1].IsSpectrumCompact);
            CollectionAssert.AreEqual(expected, MessagePackSerializer.Serialize(loaded.MspDB.Select(record => record.CreateSerializationCopy()).ToList()));
            var library = loaded.DataBases.MetabolomicsDataBases.Single().DataBase;
            Assert.IsTrue(library.Database[0].IsSpectrumCompact);
            CollectionAssert.AreEqual(expected, MessagePackSerializer.Serialize(library.Database.Select(record => record.CreateSerializationCopy()).ToList()));
        }
        finally { Directory.Delete(folder, recursive: true); }
    }
}

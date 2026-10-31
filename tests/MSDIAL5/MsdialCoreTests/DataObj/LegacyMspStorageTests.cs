using CompMs.Common.Components;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CompMs.MsdialCoreTests.DataObj;

[TestClass]
public sealed class LegacyMspStorageTests
{
    [TestMethod]
    public void SavingLegacyMspSidecarPreservesCompactStorageAndOldReaderContent() {
        var plain = new MoleculeMsReference { Name = "Plain", Spectrum = new List<SpectrumPeak> { new(73, 100), new(147, 50) } };
        var rich = new MoleculeMsReference { Name = "Rich", Spectrum = new List<SpectrumPeak> { new(100, 200, "diagnostic") } };
        var references = new List<MoleculeMsReference> { plain, rich, new() { Spectrum = null }, new() };
        var expected = MessagePackSerializer.Serialize(references);
        Assert.IsTrue(plain.TryCompactSpectrum());
        var storage = new LegacyStorage { MspDB = references };
        using var saved = new MemoryStream();
        storage.Write(saved);
        Assert.IsTrue(plain.IsSpectrumCompact, "Saving the legacy sidecar must not retain expanded library peaks.");
        saved.Position = 0;
        CollectionAssert.AreEqual(expected, MessagePackSerializer.Serialize(MoleculeMsRefMethods.LoadMspFromStream(saved)));
    }

    [TestMethod]
    public async Task LoadingLegacyMspSidecarCompactsPlainPeaksAndPreservesRichMetadata() {
        var references = new List<MoleculeMsReference> {
            new() { Name = "Plain", Spectrum = new List<SpectrumPeak> { new(73, 100), new(147, 50) } },
            new() { Name = "Rich", Spectrum = new List<SpectrumPeak> { new(100, 200, "diagnostic") } },
        };
        var expected = MessagePackSerializer.Serialize(references);
        var folder = Path.Combine(Path.GetTempPath(), "msdial-legacy-msp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try {
            MoleculeMsRefMethods.SaveMspToFile(references, Path.Combine(folder, "Legacy.msp2"));
            using var manager = new DirectoryTreeStreamManager(folder);
            var loaded = await LegacyStorage.Read(manager, "Legacy.msp2");
            Assert.IsTrue(loaded[0].IsSpectrumCompact);
            Assert.IsFalse(loaded[1].IsSpectrumCompact);
            CollectionAssert.AreEqual(expected, MessagePackSerializer.Serialize(loaded.ConvertAll(record => record.CreateSerializationCopy())));
        }
        finally { Directory.Delete(folder, recursive: true); }
    }

    private sealed class LegacyStorage : MsdialDataStorageBase
    {
        public void Write(Stream stream) => SaveMspDB(stream);
        public static Task<List<MoleculeMsReference>> Read(IStreamManager manager, string path) => LegacySerializer.Read(manager, path);
        private sealed class LegacySerializer : MsdialSerializer
        {
            public static Task<List<MoleculeMsReference>> Read(IStreamManager manager, string path) => LoadMspDBAsync(manager, path);
        }
    }
}

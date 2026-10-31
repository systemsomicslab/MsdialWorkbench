using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.MsdialCoreTests.DataObj;

[TestClass]
public sealed class CompactLibraryWriteTests
{
    [TestMethod]
    public void RepeatedDatabaseSavesPreserveOldReaderContentAndPackingChangesAfterEdits() {
        var database = new MoleculeDataBase(new[] {
            new MoleculeMsReference {
                Name = "Editable structure", SMILES = "C[C@H](O)C(=O)O", InChIKey = "TEST-STRUCTURE-UNCHANGED",
                Formula = new Formula(3, 6, 0, 3, 0, 0, 0, 0, 0, 0, 0), Ontology = "Hydroxy acid",
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
                Spectrum = new List<SpectrumPeak> { new(73, 65535), new(147, 100) },
            },
            new MoleculeMsReference { Name = "Fractional", Spectrum = new List<SpectrumPeak> { new(73.125, .25) } },
            new MoleculeMsReference { Name = "Annotated", Spectrum = new List<SpectrumPeak> {
                new(73, 100, "required") { SpectrumComment = SpectrumComment.acylchain, IsAbsolutelyRequiredFragmentForAnnotation = true },
            } },
            new MoleculeMsReference { Spectrum = null }, new MoleculeMsReference(),
        }.ToList(), "Cycles", DataBaseSource.Msp, SourceType.MspDB, "");
        foreach (var reference in database.Database) { reference.TryCompactSpectrum(); }
        for (var cycle = 0; cycle < 6; cycle++) {
            if (cycle == 2) { database.Database[0].Spectrum[0].Intensity = .125; }
            if (cycle == 4) { database.Database[0].Spectrum[0].Intensity = 65535; }
            var expected = database.Database.Select(reference => MessagePackSerializer.Serialize(reference.CreateSerializationCopy())).ToArray();
            using var stream = new MemoryStream();
            database.Save(stream, forceSerialize: true);
            stream.Position = 0;
            var oldReader = LargeListMessagePack.Deserialize<MoleculeMsReference>(stream, StandardResolver.Instance);
            stream.Position = 0;
            database.Load(stream, "");
            Assert.AreEqual(expected.Length, database.Database.Count);
            Assert.IsTrue(database.Database[0].IsSpectrumCompact);
            Assert.IsFalse(database.Database[2].IsSpectrumCompact);
            Assert.AreEqual("C[C@H](O)C(=O)O", database.Database[0].SMILES);
            Assert.AreEqual("TEST-STRUCTURE-UNCHANGED", database.Database[0].InChIKey);
            Assert.AreEqual("C3H6O3", database.Database[0].Formula.FormulaString);
            Assert.IsTrue(database.Database[2].Spectrum[0].IsAbsolutelyRequiredFragmentForAnnotation);
            for (var i = 0; i < expected.Length; i++) {
                CollectionAssert.AreEqual(expected[i], MessagePackSerializer.Serialize(oldReader[i]), $"Old reader, cycle {cycle}, record {i}.");
                CollectionAssert.AreEqual(expected[i], MessagePackSerializer.Serialize(database.Database[i].CreateSerializationCopy()), $"Current reader, cycle {cycle}, record {i}.");
            }
        }
    }

    [TestMethod]
    public void IntegralCompactionAllocatesFourBytesPerPeakAndKeepsIndependentEditableLists() {
        var reference = new MoleculeMsReference {
            Spectrum = Enumerable.Range(0, 20000).Select(i => new SpectrumPeak(i, ushort.MaxValue - i)).ToList(),
        };
        reference.Spectrum[0].Mass = ushort.MaxValue;
        var expected = MessagePackSerializer.Serialize(reference);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var compacted = reference.TryCompactSpectrum();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.IsTrue(compacted);
        Assert.IsTrue(allocated < 20000L * 4 + 1024, $"Compaction allocated {allocated} bytes.");
        var snapshot = reference.GetSpectrumPeakData();
        Assert.IsTrue(snapshot.SharesStorageWith(reference.GetSpectrumPeakData()));
        CollectionAssert.AreEqual(expected, MessagePackSerializer.Serialize(reference.CreateSerializationCopy()));
        Assert.AreEqual((double)ushort.MaxValue, snapshot.MassAt(0));
        Assert.AreEqual((double)ushort.MaxValue, snapshot.IntensityAt(0));
        reference.Spectrum[0].Mass = .125;
        Assert.AreEqual((double)ushort.MaxValue, snapshot.MassAt(0));
        Assert.AreEqual(.125, reference.GetSpectrumPeakData().MassAt(0));
        Assert.IsFalse(snapshot.SharesStorageWith(reference.GetSpectrumPeakData()));
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)]
    public void OneUnpackableValueAtTheEndPreservesEveryDoubleBit(int kind) {
        var reference = new MoleculeMsReference {
            Spectrum = Enumerable.Range(0, 2000).Select(i => new SpectrumPeak(i, i)).ToList(),
        };
        reference.Spectrum[1999].Intensity = kind switch {
            0 => BitConverter.Int64BitsToDouble(long.MinValue),
            1 => 65536d,
            2 => .125,
            3 => BitConverter.Int64BitsToDouble(0x7ff8000000001234),
            4 => double.PositiveInfinity,
            _ => -1d,
        };
        var expected = MessagePackSerializer.Serialize(reference);
        Assert.IsTrue(reference.TryCompactSpectrum());
        CollectionAssert.AreEqual(expected, MessagePackSerializer.Serialize(reference.CreateSerializationCopy()));
    }

    [TestMethod]
    public void CompactWriterPreservesOriginalReaderContentIncludingNonfiniteAndSignedZeroValues() {
        var reference = new MoleculeMsReference {
            Name = "Wire compatibility", Comment = "Retained metadata",
            Spectrum = new List<SpectrumPeak> {
                new(BitConverter.Int64BitsToDouble(unchecked((long)0x8000000000000000)), double.PositiveInfinity),
                new(BitConverter.Int64BitsToDouble(0x7ff8000000001234), double.NegativeInfinity),
            },
        };
        var records = new List<MoleculeMsReference> { reference, new() { Spectrum = null }, new() };
        var expected = Write(records, StandardResolver.Instance);
        var originalReferenceBytes = MessagePackSerializer.Serialize(reference);
        Assert.IsTrue(reference.TryCompactSpectrum());
        var compactReferenceBytes = MessagePackSerializer.Serialize(reference, CompactLibraryResolver.Instance);
        Assert.AreEqual(originalReferenceBytes.Length - 20 * 2, compactReferenceBytes.Length,
            "Only the 20 default trailing bytes per plain peak should be omitted.");
        CollectionAssert.AreEqual(originalReferenceBytes,
            MessagePackSerializer.Serialize(MessagePackSerializer.Deserialize<MoleculeMsReference>(compactReferenceBytes)));
        var actual = Write(records, CompactLibraryResolver.Instance);
        AssertOriginalReaderContent(expected, actual);
        Assert.IsTrue(reference.IsSpectrumCompact);
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)] [DataRow(6)]
    public void AnnotatedPeaksKeepTheOriginalSerializedBytes(int field) {
        var peak = new SpectrumPeak(73.123456789, 456.987654321);
        switch (field) {
            case 0: peak.Comment = ""; break;
            case 1: peak.Comment = "Required fragment"; break;
            case 2: peak.PeakQuality = (CompMs.Common.Enum.PeakQuality)1; break;
            case 3: peak.PeakID = 42; break;
            case 4: peak.SpectrumComment = SpectrumComment.doublebond; break;
            case 5: peak.IsAbsolutelyRequiredFragmentForAnnotation = true; break;
            case 6: peak.FragmentationScore = BitConverter.Int64BitsToDouble(long.MinValue); break;
        }
        var reference = new MoleculeMsReference { Spectrum = new List<SpectrumPeak> { new(10, 20), peak } };
        var records = new List<MoleculeMsReference> { reference };
        var expected = Write(records, StandardResolver.Instance);
        Assert.IsFalse(reference.TryCompactSpectrum());
        CollectionAssert.AreEqual(expected, Write(records, CompactLibraryResolver.Instance));
    }

    [TestMethod]
    public void ShortNumericEncodingsPreserveEveryOriginalDoubleBit() {
        var values = new List<double> { 0, -0d, -1, 127, 128, 255, 256, int.MinValue, uint.MaxValue,
            (double)uint.MaxValue + 1, 73.25, double.Epsilon, float.Epsilon, float.MaxValue,
            double.MaxValue, double.MinValue, double.PositiveInfinity, double.NegativeInfinity,
            BitConverter.Int64BitsToDouble(long.MinValue), BitConverter.Int64BitsToDouble(0x7ff8000000001234) };
        var random = new Random(924);
        var bits = new byte[8];
        for (var i = 0; i < 10000; i++) {
            random.NextBytes(bits);
            values.Add(BitConverter.Int64BitsToDouble(BitConverter.ToInt64(bits, 0)));
        }
        var reference = new MoleculeMsReference {
            Spectrum = values.Select((value, index) => new SpectrumPeak(value, values[values.Count - index - 1])).ToList(),
        };
        var expected = MessagePackSerializer.Serialize(reference);
        Assert.IsTrue(reference.TryCompactSpectrum());
        var encoded = MessagePackSerializer.Serialize(reference, CompactLibraryResolver.Instance);
        var originalReader = MessagePackSerializer.Deserialize<MoleculeMsReference>(encoded, StandardResolver.Instance);
        var currentReader = MessagePackSerializer.Deserialize<MoleculeMsReference>(encoded, CompactLibraryResolver.Instance);
        Assert.IsTrue(currentReader.IsSpectrumCompact);
        CollectionAssert.AreEqual(expected, MessagePackSerializer.Serialize(originalReader));
        CollectionAssert.AreEqual(expected, MessagePackSerializer.Serialize(currentReader));
    }

    [TestMethod]
    public void CompactSavingAvoidsAllocatingTemporaryPeakObjects() {
        var records = Enumerable.Range(0, 256).Select(index => new MoleculeMsReference {
            Name = "Reference " + index,
            Spectrum = Enumerable.Range(0, 256).Select(peak => new SpectrumPeak(50 + peak, 1000 - peak)).ToList(),
        }).ToList();
        foreach (var record in records) { Assert.IsTrue(record.TryCompactSpectrum()); }
        Write(records.Take(1).ToList(), CopyResolver.Instance);
        Write(records.Take(1).ToList(), CompactLibraryResolver.Instance);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var original = Write(records, CopyResolver.Instance);
        var originalAllocations = GC.GetAllocatedBytesForCurrentThread() - before;
        before = GC.GetAllocatedBytesForCurrentThread();
        var current = Write(records, CompactLibraryResolver.Instance);
        var currentAllocations = GC.GetAllocatedBytesForCurrentThread() - before;
        AssertOriginalReaderContent(original, current);
        Assert.IsTrue(originalAllocations - currentAllocations > 256L * 256 * 32,
            $"Temporary peak allocations remain: original {originalAllocations}, current {currentAllocations} bytes.");
        Assert.IsTrue(records.All(record => record.IsSpectrumCompact));
    }

    private static byte[] Write(List<MoleculeMsReference> records, IFormatterResolver resolver) {
        using var stream = new MemoryStream();
        LargeListMessagePack.Serialize(stream, records, resolver, 16 * 1024 * 1024);
        return stream.ToArray();
    }

    [TestMethod]
    public async Task ConcurrentWritersKeepEachLibrariesSpectrumValuesSeparate() {
        var libraries = Enumerable.Range(0, 8).Select(seed => Enumerable.Range(0, 32).Select(index => new MoleculeMsReference {
            Name = "Library " + seed,
            Spectrum = Enumerable.Range(0, 32).Select(peak => new SpectrumPeak(seed * 1000 + index * 10 + peak, seed + peak + 1)).ToList(),
        }).ToList()).ToArray();
        var expected = libraries.Select(records => Write(records, StandardResolver.Instance)).ToArray();
        foreach (var records in libraries) { foreach (var record in records) { Assert.IsTrue(record.TryCompactSpectrum()); } }
        await Task.WhenAll(libraries.Select((records, index) => Task.Run(() => {
            for (var repeat = 0; repeat < 5; repeat++) {
                AssertOriginalReaderContent(expected[index], Write(records, CompactLibraryResolver.Instance));
            }
        })));
    }

    private static void AssertOriginalReaderContent(byte[] expected, byte[] actual) {
        using var expectedStream = new MemoryStream(expected);
        using var actualStream = new MemoryStream(actual);
        var expectedRecords = LargeListMessagePack.Deserialize<MoleculeMsReference>(expectedStream, StandardResolver.Instance);
        var actualRecords = LargeListMessagePack.Deserialize<MoleculeMsReference>(actualStream, StandardResolver.Instance);
        Assert.AreEqual(expectedRecords.Count, actualRecords.Count);
        for (var i = 0; i < actualRecords.Count; i++) {
            CollectionAssert.AreEqual(MessagePackSerializer.Serialize(expectedRecords[i]), MessagePackSerializer.Serialize(actualRecords[i]));
        }
    }

    private sealed class CopyResolver : IFormatterResolver, IMessagePackFormatter<MoleculeMsReference>
    {
        public static readonly CopyResolver Instance = new();
        private readonly IMessagePackFormatter<MoleculeMsReference> _standard = StandardResolver.Instance.GetFormatter<MoleculeMsReference>();
        public IMessagePackFormatter<T> GetFormatter<T>() => typeof(T) == typeof(MoleculeMsReference)
            ? (IMessagePackFormatter<T>)(object)this : StandardResolver.Instance.GetFormatter<T>();
        public int Serialize(ref byte[] bytes, int offset, MoleculeMsReference value, IFormatterResolver resolver)
            => _standard.Serialize(ref bytes, offset, value?.CreateSerializationCopy(), resolver);
        public MoleculeMsReference Deserialize(byte[] bytes, int offset, IFormatterResolver resolver, out int readSize)
            => _standard.Deserialize(bytes, offset, resolver, out readSize);
    }
}

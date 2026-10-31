using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj.Tests;

[TestClass]
public sealed class CompactLibraryReadTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void ImpossibleSpectrumPeakCountFailsBeforeAllocationAndKeepsTheExistingLibrary() {
        var reference = Reference(1);
        var bytes = MessagePackSerializer.Serialize(reference);
        MessagePackBinary.ReadArrayHeader(bytes, 0, out var offset);
        for (var field = 0; field < 4; field++) { offset += MessagePackBinary.ReadNextBlock(bytes, offset); }
        var oldSize = MessagePackBinary.ReadNextBlock(bytes, offset);
        // Never allocate a List for the advertised two billion peaks in this corrupt field.
        var changed = bytes.Take(offset).Concat(new byte[] { 0xdd, 0x7f, 0xff, 0xff, 0xff })
            .Concat(bytes.Skip(offset + oldSize)).ToArray();
        Assert.ThrowsException<EndOfStreamException>(() => MessagePackBinary.ReadArrayHeader(changed, offset, out _));
        var payload = new byte[] { 0xdd, 0, 0, 0, 1 }.Concat(changed).ToArray();
        using var stream = new MemoryStream(LargeListMessagePack.ToLZ4Binary(new ArraySegment<byte>(payload)));
        var database = new MoleculeDataBase(new[] { reference }, "Test", DataBaseSource.Msp, SourceType.MspDB, "");
        var collection = database.Database;
        Assert.ThrowsException<EndOfStreamException>(() => database.Load(stream, ""));
        Assert.AreSame(collection, database.Database);
        Assert.AreSame(reference, database.Database[0]);
    }

    [TestMethod]
    public void ReadingPlainSpectraPreservesLegacyCompactAllocationBudget() {
        var original = Reference(20000);
        var bytes = MessagePackSerializer.Serialize(original);
        MessagePackSerializer.Deserialize<MoleculeMsReference>(bytes);
        MessagePackSerializer.Deserialize<MoleculeMsReference>(bytes, CompactLibraryResolver.Instance);

        var start = GC.GetAllocatedBytesForCurrentThread();
        var expanded = MessagePackSerializer.Deserialize<MoleculeMsReference>(bytes);
        expanded.TryCompactSpectrum();
        var expandedAllocation = GC.GetAllocatedBytesForCurrentThread() - start;
        start = GC.GetAllocatedBytesForCurrentThread();
        var compact = MessagePackSerializer.Deserialize<MoleculeMsReference>(bytes, CompactLibraryResolver.Instance);
        var compactAllocation = GC.GetAllocatedBytesForCurrentThread() - start;
        TestContext.WriteLine($"Allocated read bytes: legacy compact {expandedAllocation:N0}; fast compact {compactAllocation:N0}.");

        Assert.IsTrue(compact.IsSpectrumCompact);
        Assert.AreEqual(expanded.GetSpectrumPeakData().Count, compact.GetSpectrumPeakData().Count);
        Assert.IsTrue(compactAllocation <= expandedAllocation * 1.05,
            $"Fast compact read allocated {compactAllocation:N0} bytes; legacy compact read allocated {expandedAllocation:N0} bytes.");
        CollectionAssert.AreEqual(MessagePackSerializer.Serialize(expanded), MessagePackSerializer.Serialize(compact));
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(10000)] [DataRow(19999)]
    public void ReadingAnnotatedSpectraDoesNotAllocateDiscardedNumericArrays(int annotatedIndex) {
        var original = Reference(20000);
        original.Spectrum[annotatedIndex].Comment = "Required fragment";
        var bytes = MessagePackSerializer.Serialize(original);
        MessagePackSerializer.Deserialize<MoleculeMsReference>(bytes);
        MessagePackSerializer.Deserialize<MoleculeMsReference>(bytes, CompactLibraryResolver.Instance);
        var start = GC.GetAllocatedBytesForCurrentThread();
        var expected = MessagePackSerializer.Deserialize<MoleculeMsReference>(bytes);
        var standardAllocation = GC.GetAllocatedBytesForCurrentThread() - start;
        start = GC.GetAllocatedBytesForCurrentThread();
        var actual = MessagePackSerializer.Deserialize<MoleculeMsReference>(bytes, CompactLibraryResolver.Instance);
        var allocation = GC.GetAllocatedBytesForCurrentThread() - start;
        TestContext.WriteLine($"Annotated peak {annotatedIndex}: standard {standardAllocation:N0}; library {allocation:N0} allocated bytes.");
        Assert.IsFalse(actual.IsSpectrumCompact);
        CollectionAssert.AreEqual(MessagePackSerializer.Serialize(expected), MessagePackSerializer.Serialize(actual));
        Assert.IsTrue(allocation < standardAllocation * 1.05, "Annotated reads must not allocate a temporary numeric array before the retained objects.");
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)] [DataRow(6)] [DataRow(7)]
    public void ReadingAnnotatedSpectraPreservesEverySerializedField(int field) {
        var original = Reference(4);
        var peak = original.Spectrum[3];
        switch (field) {
            case 0: peak.Comment = ""; break;
            case 1: peak.Comment = "Required fragment"; break;
            case 2: peak.PeakQuality = (PeakQuality)1; break;
            case 3: peak.PeakID = 7; break;
            case 4: peak.SpectrumComment = SpectrumComment.doublebond | SpectrumComment.snposition; break;
            case 5: peak.IsAbsolutelyRequiredFragmentForAnnotation = true; break;
            case 6: peak.FragmentationScore = BitConverter.Int64BitsToDouble(long.MinValue); break;
            case 7: peak.FragmentationScore = .7; break;
        }
        var bytes = MessagePackSerializer.Serialize(original);
        var actual = MessagePackSerializer.Deserialize<MoleculeMsReference>(bytes, CompactLibraryResolver.Instance);
        Assert.IsFalse(actual.IsSpectrumCompact);
        CollectionAssert.AreEqual(bytes, MessagePackSerializer.Serialize(actual));
    }

    [TestMethod]
    public void ReadingPlainSpectraPreservesReferenceFieldsAndNumericBits() {
        var original = Reference(8);
        original.Spectrum[0].Mass = BitConverter.Int64BitsToDouble(unchecked((long)0x7ff8000000000042));
        original.Spectrum[1].Intensity = BitConverter.Int64BitsToDouble(long.MinValue);
        original.CollisionCrossSection = 203.17;
        original.ChromXs.RT = new RetentionTime(2.5);
        original.ChromXs.RI = new RetentionIndex(1400);
        original.QuantMass = 73.04;
        original.CompoundClass = "PC";
        original.FragmentationCondition = "CID";
        var bytes = MessagePackSerializer.Serialize(original);
        var actual = MessagePackSerializer.Deserialize<MoleculeMsReference>(bytes, CompactLibraryResolver.Instance);
        Assert.IsTrue(actual.IsSpectrumCompact);
        CollectionAssert.AreEqual(bytes, MessagePackSerializer.Serialize(actual));
    }

    [TestMethod]
    public void ParallelLibraryReadsKeepEachRecordsOwnSpectrum() {
        var references = Enumerable.Range(0, 40).Select(index => {
            var reference = Reference(20);
            reference.ScanID = index;
            reference.Spectrum[0].Intensity = index;
            return MessagePackSerializer.Serialize(reference);
        }).ToArray();
        Parallel.For(0, 400, index => {
            var actual = MessagePackSerializer.Deserialize<MoleculeMsReference>(references[index % 40], CompactLibraryResolver.Instance);
            Assert.AreEqual((double)actual.ScanID, actual.GetSpectrumPeakData()[0].Intensity);
        });
    }

    [DataTestMethod]
    [DataRow(256)] [DataRow(65536)]
    public void MixedRecordsRemainIndependentAcrossBlocksAndConcurrentLoads(int blockSize) {
        var annotated = Reference(4);
        annotated.Spectrum[3].Comment = "Required fragment";
        var nullSpectrum = Reference(0);
        nullSpectrum.Spectrum = null;
        var expected = new[] { Reference(20), null, nullSpectrum, Reference(0), annotated, Reference(10) };
        using var saved = new MemoryStream();
        LargeListMessagePack.Serialize(saved, expected, null, blockSize);
        var bytes = saved.ToArray();
        Parallel.For(0, 20, _ => {
            using var stream = new MemoryStream(bytes);
            var actual = LargeListMessagePack.Deserialize<MoleculeMsReference>(stream, CompactLibraryResolver.Instance, reuseBuffers: true);
            Assert.AreEqual(expected.Length, actual.Count);
            for (var index = 0; index < expected.Length; index++) {
                CollectionAssert.AreEqual(MessagePackSerializer.Serialize(expected[index]), MessagePackSerializer.Serialize(actual[index]));
            }
        });
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)]
    public void NullEmptyAndNullElementSpectraKeepLegacyBehavior(int kind) {
        var original = Reference(0);
        if (kind == 0) { original.Spectrum = null; }
        if (kind == 2) { original.Spectrum.Add(null); }
        var bytes = MessagePackSerializer.Serialize(original);
        var actual = MessagePackSerializer.Deserialize<MoleculeMsReference>(bytes, CompactLibraryResolver.Instance);
        Assert.IsFalse(actual.IsSpectrumCompact);
        CollectionAssert.AreEqual(bytes, MessagePackSerializer.Serialize(actual));
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void OlderAndExtendedPeakLayoutsKeepLegacyBehavior(bool extended) {
        var original = Reference(1);
        var bytes = MessagePackSerializer.Serialize(original);
        var offset = 0;
        MessagePackBinary.ReadArrayHeader(bytes, offset, out var header);
        offset += header;
        for (var field = 0; field < 4; field++) { offset += MessagePackBinary.ReadNextBlock(bytes, offset); }
        var oldSize = MessagePackBinary.ReadNextBlock(bytes, offset);
        var peak = MessagePackSerializer.Serialize(original.Spectrum[0]);
        var end = 1 + MessagePackBinary.ReadNextBlock(peak, 1);
        end += MessagePackBinary.ReadNextBlock(peak, end);
        var changedPeak = extended ? new byte[] { 0x9f }.Concat(peak.Skip(1)).Append((byte)0).ToArray()
            : new byte[] { 0x92 }.Concat(peak.Skip(1).Take(end - 1)).ToArray();
        var changed = bytes.Take(offset).Append((byte)0x91).Concat(changedPeak).Concat(bytes.Skip(offset + oldSize)).ToArray();
        var expected = MessagePackSerializer.Deserialize<MoleculeMsReference>(changed);
        var actual = MessagePackSerializer.Deserialize<MoleculeMsReference>(changed, CompactLibraryResolver.Instance);
        CollectionAssert.AreEqual(MessagePackSerializer.Serialize(expected), MessagePackSerializer.Serialize(actual));
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void ReferencesInHistoricalOversizedBlockBuffersPreserveContent(bool annotated) {
        var original = Reference(4);
        if (annotated) { original.Spectrum[2].Comment = "Required fragment"; }
        var bytes = MessagePackSerializer.Serialize(original);
        var block = new byte[LargeListMessagePack.MaxReusableBufferSize + 1];
        Array.Copy(bytes, block, bytes.Length);
        var actual = CompactLibraryResolver.Instance.GetFormatter<MoleculeMsReference>()
            .Deserialize(block, 0, CompactLibraryResolver.Instance, out var readSize);
        Assert.AreEqual(bytes.Length, readSize);
        Assert.AreEqual(!annotated, actual.IsSpectrumCompact);
        CollectionAssert.AreEqual(bytes, MessagePackSerializer.Serialize(actual));
    }

    [TestMethod]
    public void AlternatePeakNumericEncodingsKeepStandardReaderSemantics() {
        var original = Reference(1);
        var bytes = MessagePackSerializer.Serialize(original);
        MessagePackBinary.ReadArrayHeader(bytes, 0, out var offset);
        for (var field = 0; field < 4; field++) { offset += MessagePackBinary.ReadNextBlock(bytes, offset); }
        var spectrumSize = MessagePackBinary.ReadNextBlock(bytes, offset);
        var peak = MessagePackSerializer.Serialize(original.Spectrum[0]);
        var changedPeak = new byte[] { 0x9e }.Concat(MessagePackSerializer.Serialize(50.25f))
            .Concat(MessagePackSerializer.Serialize(2)).Concat(peak.Skip(19));
        var changed = bytes.Take(offset).Append((byte)0x91).Concat(changedPeak).Concat(bytes.Skip(offset + spectrumSize)).ToArray();
        var expected = MessagePackSerializer.Deserialize<MoleculeMsReference>(changed);
        expected.TryCompactSpectrum();
        var actual = MessagePackSerializer.Deserialize<MoleculeMsReference>(changed, CompactLibraryResolver.Instance);
        CollectionAssert.AreEqual(MessagePackSerializer.Serialize(expected), MessagePackSerializer.Serialize(actual));
        Assert.AreEqual(expected.IsSpectrumCompact, actual.IsSpectrumCompact);
    }

    [TestMethod]
    public void BoundedDatabaseBlocksStayInsideOneArchiveEntry() {
        var references = new[] { Reference(900000), Reference(2) };
        foreach (var reference in references) { reference.TryCompactSpectrum(); }
        var database = new MoleculeDataBase(references, "Blocks", DataBaseSource.Msp, SourceType.MspDB, "");
        using var project = new MemoryStream();
        using (var output = new ZipArchive(project, ZipArchiveMode.Create, leaveOpen: true)) {
            new DataBaseItem<MoleculeDataBase>(database, new List<IAnnotatorParameterPair<MoleculeDataBase>>()).Save(output, "Libraries");
        }
        project.Position = 0;
        using var archive = new ZipArchive(project, ZipArchiveMode.Read);
        Assert.AreEqual(1, archive.Entries.Count, "Multiple library blocks must not become separate archive entries.");
        using var entry = archive.Entries[0].Open();
        using var encoded = new MemoryStream();
        entry.CopyTo(encoded);
        var bytes = encoded.ToArray();
        var offset = 0;
        var blocks = 0;
        while (offset < bytes.Length) {
            var header = MessagePackBinary.ReadExtensionFormatHeader(bytes, offset, out var headerSize);
            offset += headerSize + (int)header.Length;
            blocks++;
        }
        Assert.AreEqual(bytes.Length, offset);
        Assert.IsTrue(blocks >= 2, "The fixture must exceed the production block target.");
        encoded.Position = 0;
        var reloaded = new MoleculeDataBase("Blocks", SourceType.MspDB, DataBaseSource.Msp, "");
        reloaded.Load(encoded, "");
        Assert.AreEqual(2, reloaded.Database.Count);
        Assert.AreEqual(900000, reloaded.Database[0].GetSpectrumPeakData().Count);
        Assert.AreEqual(2, reloaded.Database[1].GetSpectrumPeakData().Count);
    }

    [TestMethod]
    public void ReadingManyBlocksReusesDecompressionMemory() {
        var random = new Random(24);
        var values = Enumerable.Range(0, 8).Select(_ => {
            var value = new byte[512 * 1024];
            random.NextBytes(value);
            return value;
        }).ToArray();
        using var stream = new MemoryStream();
        LargeListMessagePack.Serialize(stream, values, null, 512 * 1024);
        stream.Position = 0;
        var start = GC.GetAllocatedBytesForCurrentThread();
        var actual = LargeListMessagePack.Deserialize<byte[]>(stream, null, reuseBuffers: true);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - start;
        TestContext.WriteLine($"Allocated multi-block read bytes: {allocated:N0}.");
        Assert.AreEqual(values.Length, actual.Count);
        for (var index = 0; index < values.Length; index++) { CollectionAssert.AreEqual(values[index], actual[index]); }
        Assert.IsTrue(allocated < values.Sum(value => (long)value.Length) * 2.5,
            "Reading consecutive blocks must not allocate a fresh decompression buffer for every block.");
    }

    [TestMethod]
    public void OversizedBlocksDoNotEnlargeTheReusableBufferForFollowingBlocks() {
        var large = new byte[LargeListMessagePack.MaxReusableBufferSize + 1];
        large[0] = 7;
        large[large.Length - 1] = 11;
        using var stream = new MemoryStream();
        LargeListMessagePack.Serialize(stream, new[] { large, new byte[] { 1, 2 }, new byte[] { 3, 4 } }, null, 1024);
        stream.Position = 0;
        var reader = new RecordingByteArrayResolver();
        var actual = LargeListMessagePack.Deserialize<byte[]>(stream, reader, reuseBuffers: true);
        Assert.AreEqual(large.Length, actual[0].Length);
        Assert.AreEqual((byte)7, actual[0][0]);
        Assert.AreEqual((byte)11, actual[0][large.Length - 1]);
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, actual[1]);
        CollectionAssert.AreEqual(new byte[] { 3, 4 }, actual[2]);
        Assert.IsTrue(reader.BufferLengths[0] > LargeListMessagePack.MaxReusableBufferSize);
        Assert.IsTrue(reader.BufferLengths.Skip(1).All(length => length <= LargeListMessagePack.MaxReusableBufferSize),
            "A historical oversized block must not leave its allocation in the reusable buffer slot.");
    }

    private sealed class RecordingByteArrayResolver : IFormatterResolver, IMessagePackFormatter<byte[]> {
        private readonly IMessagePackFormatter<byte[]> _standard = StandardResolver.Instance.GetFormatter<byte[]>();
        public List<int> BufferLengths { get; } = new();
        public IMessagePackFormatter<T> GetFormatter<T>() => typeof(T) == typeof(byte[])
            ? (IMessagePackFormatter<T>)(object)this : StandardResolver.Instance.GetFormatter<T>();
        public int Serialize(ref byte[] bytes, int offset, byte[] value, IFormatterResolver resolver)
            => _standard.Serialize(ref bytes, offset, value, StandardResolver.Instance);
        public byte[] Deserialize(byte[] bytes, int offset, IFormatterResolver resolver, out int readSize) {
            BufferLengths.Add(bytes.Length);
            return _standard.Deserialize(bytes, offset, StandardResolver.Instance, out readSize);
        }
    }

    [TestMethod]
    public void ReusableBlockBuffersDoNotTurnMissingRecordsIntoPaddingValues() {
        // The format reserves five array-header bytes, but this block declares two records and contains only one.
        var payload = new byte[] { 0x92, 0, 0, 0, 0, 1 };
        using var stream = new MemoryStream(LargeListMessagePack.ToLZ4Binary(new ArraySegment<byte>(payload)));
        Assert.ThrowsException<InvalidOperationException>(() => LargeListMessagePack.Deserialize<int>(stream, null, reuseBuffers: true));
    }

    [TestMethod]
    public void TruncatedLibraryBlocksFailWithoutReplacingTheExistingLibrary() {
        var reference = Reference(4);
        var database = new MoleculeDataBase(new[] { reference }, "DB", DataBaseSource.Msp, SourceType.MspDB, "");
        var collection = database.Database;
        var digest = database.ContentDigest;
        using var saved = new MemoryStream();
        database.Save(saved);
        var bytes = saved.ToArray();
        foreach (var length in new[] { 1, 5, 10, 11, 12, bytes.Length - 1 }) {
            using var truncated = new MemoryStream(bytes, 0, length);
            Assert.ThrowsException<EndOfStreamException>(() => database.Load(truncated, ""), $"Truncation at byte {length} must fail.");
            Assert.AreSame(collection, database.Database);
            Assert.AreEqual(1, database.Database.Count);
            Assert.AreSame(reference, database.Database[0]);
            Assert.AreEqual(digest, database.ContentDigest);
        }
        using var incompleteLaterHeader = new MemoryStream(bytes.Append((byte)0xc9).ToArray());
        Assert.ThrowsException<EndOfStreamException>(() => database.Load(incompleteLaterHeader, ""));
        Assert.AreSame(collection, database.Database);
        Assert.AreSame(reference, database.Database[0]);
        Assert.AreEqual(digest, database.ContentDigest);
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)]
    public void InvalidLibraryBlockHeadersAreRejected(int kind) {
        using var saved = new MemoryStream();
        LargeListMessagePack.Serialize(saved, new[] { 1, 2, 3 });
        var bytes = saved.ToArray();
        switch (kind) {
            case 0: bytes[0] = 0; break;
            case 1: bytes[5] = 98; break;
            case 2: MessagePackBinary.WriteInt32ForceInt32Block(ref bytes, 6, -1); break;
            case 3: bytes[1] = 255; break;
        }
        using var stream = new MemoryStream(bytes);
        Assert.ThrowsException<InvalidOperationException>(() => LargeListMessagePack.Deserialize<int>(stream, null, reuseBuffers: true));
    }

    [TestMethod]
    public void DefaultBlockReaderPreservesBorrowedByteSegments() {
        var values = Enumerable.Range(0, 4).Select(index => new ArraySegment<byte>(Enumerable.Repeat((byte)index, 4096).ToArray())).ToArray();
        using var stream = new MemoryStream();
        LargeListMessagePack.Serialize(stream, values, null, 4096);
        stream.Position = 0;
        var actual = LargeListMessagePack.Deserialize<ArraySegment<byte>>(stream);
        Assert.AreEqual(values.Length, actual.Count);
        for (var index = 0; index < values.Length; index++) {
            CollectionAssert.AreEqual(values[index].ToArray(), actual[index].ToArray());
        }
    }

    [TestMethod]
    public void NullAndEmptyLibraryStreamsKeepLegacyBehavior() {
        foreach (var values in new int[][] { null, Array.Empty<int>() }) {
            using var stream = new MemoryStream();
            LargeListMessagePack.Serialize(stream, values);
            stream.Position = 0;
            Assert.AreEqual(0, LargeListMessagePack.Deserialize<int>(stream, null, reuseBuffers: true).Count);
        }
    }

    [TestMethod]
    public void TrailingIncompleteHeadersAndImpossibleRecordCountsAreRejected() {
        using var saved = new MemoryStream();
        LargeListMessagePack.Serialize(saved, new[] { 1, 2, 3 });
        using var trailing = new MemoryStream(saved.ToArray().Append((byte)0xc0).ToArray());
        Assert.ThrowsException<EndOfStreamException>(() => LargeListMessagePack.Deserialize<int>(trailing, null, reuseBuffers: true));

        // The count fits the reused buffer capacity, but not the five-byte decoded payload.
        var payload = new byte[] { 0xdd, 0, 0, 0, 100 };
        using var impossible = new MemoryStream(LargeListMessagePack.ToLZ4Binary(new ArraySegment<byte>(payload)));
        Assert.ThrowsException<InvalidOperationException>(() => LargeListMessagePack.Deserialize<int>(impossible, null, reuseBuffers: true));
    }

    [TestMethod]
    public void OwnedBlockReaderSupportsShortNonSeekableStreamReads() {
        var expected = Enumerable.Range(0, 1000).ToArray();
        using var saved = new MemoryStream();
        LargeListMessagePack.Serialize(saved, expected, null, 256);
        using var stream = new ShortReadStream(saved.ToArray());
        CollectionAssert.AreEqual(expected, LargeListMessagePack.Deserialize<int>(stream, null, reuseBuffers: true));
    }

    private sealed class ShortReadStream : MemoryStream {
        public ShortReadStream(byte[] bytes) : base(bytes) { }
        public override bool CanSeek => false;
        public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, 3));
    }

    private static MoleculeMsReference Reference(int count) => new() {
        ScanID = 7, Name = "Reference", Comment = "Record metadata", InChIKey = "TESTKEY",
        Spectrum = Enumerable.Range(0, count).Select(index => new SpectrumPeak(50.000123 + index, index + .123456789)).ToList(),
    };
}

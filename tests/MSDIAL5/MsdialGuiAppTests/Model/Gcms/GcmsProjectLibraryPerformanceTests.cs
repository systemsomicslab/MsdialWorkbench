using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Gcms;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialGcMsApi.Algorithm;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;

namespace MsdialGuiAppTests.Model.Gcms;

[TestClass]
public sealed class GcmsProjectLibraryPerformanceTests
{
    public TestContext TestContext { get; set; } = null!;

    [DataTestMethod]
    [TestCategory("LocalIntegration")]
    [DataRow(1)] [DataRow(100)]
    public void AnnotatedLibraryReadsPreserveContentAndReportCost(int annotatedPercent) {
        var references = Enumerable.Range(0, 500).Select(index => new MoleculeMsReference {
            ScanID = index, Name = "Annotated reference " + index,
            Spectrum = Enumerable.Range(0, 400).Select(peak => new SpectrumPeak(50.25 + peak, 1000 - peak,
                peak % 100 < annotatedPercent ? "Required fragment" : null,
                peak % 100 < annotatedPercent ? SpectrumComment.acylchain : SpectrumComment.none)).ToList(),
        }).ToList();
        using var saved = new MemoryStream();
        LargeListMessagePack.Serialize(saved, references, null, 1024 * 1024);
        var expected = DigestLibrary(new MoleculeDataBase(references, "Annotated", DataBaseSource.Msp, SourceType.MspDB, ""));
        // Warm the same formatter paths before timing either reader.
        var warmup = MessagePackSerializer.Serialize(references[0]);
        MessagePackSerializer.Deserialize<MoleculeMsReference>(warmup, LegacyCompactResolver.Instance);
        using (var warmStream = new MemoryStream()) {
            LargeListMessagePack.Serialize(warmStream, new[] { references[0] });
            warmStream.Position = 0;
            new MoleculeDataBase("Warmup", SourceType.MspDB, DataBaseSource.Msp, "").Load(warmStream, "");
        }
        foreach (var legacy in new[] { true, false, false, true }) {
            saved.Position = 0;
            var timer = Stopwatch.StartNew();
            MoleculeDataBase database;
            if (legacy) {
                database = new MoleculeDataBase(LargeListMessagePack.Deserialize<MoleculeMsReference>(saved, LegacyCompactResolver.Instance),
                    "Annotated", DataBaseSource.Msp, SourceType.MspDB, "");
            }
            else {
                database = new MoleculeDataBase("Annotated", SourceType.MspDB, DataBaseSource.Msp, "");
                database.Load(saved, "");
            }
            var milliseconds = timer.Elapsed.TotalMilliseconds;
            Assert.AreEqual(expected, DigestLibrary(database));
            Assert.IsTrue(database.Database.All(record => !record.IsSpectrumCompact), "Annotated records must retain their full peak metadata.");
            TestContext.WriteLine($"Annotated {annotatedPercent}%: {(legacy ? "legacy" : "fast")} {milliseconds:F1} ms; 200,000 peaks; complete content identical.");
        }
    }

    [TestMethod]
    [TestCategory("LocalIntegration")]
    public void ExistingProjectLibraryLoadAndMainSearch() {
        var path = Environment.GetEnvironmentVariable("MSDIAL_TEST_GC_DATABASE_FILE");
        var resultPath = Environment.GetEnvironmentVariable("MSDIAL_TEST_GC_MSDEC_FILE");
        if (!File.Exists(path) || !File.Exists(resultPath)) {
            Assert.Inconclusive("Set MSDIAL_TEST_GC_DATABASE_FILE and MSDIAL_TEST_GC_MSDEC_FILE to an approved local project library and GC result. Run without another large-library process.");
        }
        using var archive = ZipFile.OpenRead(path);
        var entry = archive.Entries.First(item => item.FullName.EndsWith("DataBase", StringComparison.Ordinal) && item.Length > 0);
        var itemPath = Path.GetDirectoryName(entry.FullName);
        var database = new MoleculeDataBase(Path.GetFileName(itemPath), SourceType.MspDB, DataBaseSource.Msp, "");
        var legacy = Environment.GetEnvironmentVariable("MSDIAL_TEST_GC_USE_LEGACY_READER") == "1";
        var timer = Stopwatch.StartNew();
        if (legacy) {
            using (var stream = entry.Open()) {
                database = new MoleculeDataBase(LargeListMessagePack.Deserialize<MoleculeMsReference>(stream, LegacyCompactResolver.Instance),
                    "Local library", DataBaseSource.Msp, SourceType.MspDB, "");
            }
        }
        else {
            new DataBaseItem<MoleculeDataBase>(database, []).Load(archive, Path.GetDirectoryName(itemPath), null!, null!, "");
        }
        var loadMilliseconds = timer.Elapsed.TotalMilliseconds;
        TestContext.WriteLine($"Project library load ({(legacy ? "legacy resolver/current transport" : "fast compact")}): {loadMilliseconds:F1} ms; {database.Database.Count} records; compact {database.Database.Count(record => record.IsSpectrumCompact)}.");
        RecordMemory("Load return, no test collection");

        using var loader = new MSDecLoader(File.OpenRead(resultPath));
        var query = loader.LoadMSDecResults().Where(scan => scan.Spectrum.Count > 0)
            .OrderByDescending(scan => scan.Spectrum.Sum(peak => peak.Intensity)).First();
        var calculator = new CalculateMatchScore(new DataBaseItem<MoleculeDataBase>(database, []),
            new MsRefSearchParameterBase { Ms1Tolerance = .5f, Ms2Tolerance = .5f, RiTolerance = 100 }, RetentionType.RI, RiCompoundType.Alkanes);
        using var feature = new Ms1BasedSpectrumFeature(new SpectrumFeature(new AnnotatedMSDecResult(query, new MsScanMatchResultContainer()), null!));
        timer.Restart();
        var results = new GcmsAnalysisCompoundSearchUsecase(calculator).Search(feature);
        var coldMilliseconds = timer.Elapsed.TotalMilliseconds;
        timer.Restart();
        var warm = new GcmsAnalysisCompoundSearchUsecase(calculator).Search(feature);
        TestContext.WriteLine($"Main Compound Search: cold {coldMilliseconds:F1} ms; repeated {timer.Elapsed.TotalMilliseconds:F1} ms; {results.Count} candidates.");
        RecordMemory("After repeated main search, no test collection");
        Assert.AreEqual(results.Count, warm.Count);
        var scoreDigest = DigestResults(results);
        Assert.AreEqual(scoreDigest, DigestResults(warm), "Every ordered cached result field must match the cold search.");
        TestContext.WriteLine($"Retained library/search/cache data after test-only collection: {GC.GetTotalMemory(true) / 1048576d:F1} MiB.");
        RecordMemory("After test-only collection");

        var libraryDigest = DigestLibrary(database);
        TestContext.WriteLine($"Complete library digest: {libraryDigest}; complete ordered main-result digest: {scoreDigest}.");
        var approvedLibrary = Environment.GetEnvironmentVariable("MSDIAL_TEST_GC_LIBRARY_DIGEST");
        var approvedScores = Environment.GetEnvironmentVariable("MSDIAL_TEST_GC_MAIN_DIGEST");
        if (!string.IsNullOrEmpty(approvedLibrary)) { Assert.AreEqual(approvedLibrary, libraryDigest); }
        if (!string.IsNullOrEmpty(approvedScores)) { Assert.AreEqual(approvedScores, scoreDigest); }
        if (Environment.GetEnvironmentVariable("MSDIAL_TEST_GC_LIBRARY_ROUNDTRIP") == "1") {
            var temporary = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".msdial-test-library");
            using var saved = new FileStream(temporary, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 65536, FileOptions.DeleteOnClose);
            timer.Restart();
            database.Save(saved);
            TestContext.WriteLine($"Saved complete library: {timer.Elapsed.TotalMilliseconds:F1} ms; {saved.Length / 1048576d:F1} MiB.");
            saved.Position = 0;
            var reloaded = new MoleculeDataBase("Reloaded", SourceType.MspDB, DataBaseSource.Msp, "");
            timer.Restart();
            reloaded.Load(saved, "");
            TestContext.WriteLine($"Reloaded saved library: {timer.Elapsed.TotalMilliseconds:F1} ms.");
            Assert.AreEqual(database.Database.Count, reloaded.Database.Count);
            Assert.AreEqual(libraryDigest, DigestLibrary(reloaded));
            Assert.AreEqual(reloaded.Database.Count, reloaded.Database.Count(record => record.IsSpectrumCompact));
            var boundedInput = Environment.GetEnvironmentVariable("MSDIAL_TEST_GC_BOUNDED_DATABASE_FILE");
            if (!string.IsNullOrEmpty(boundedInput)) {
                using var output = new FileStream(boundedInput, FileMode.CreateNew, FileAccess.Write);
                using var boundedArchive = new ZipArchive(output, ZipArchiveMode.Create);
                using var boundedEntry = boundedArchive.CreateEntry(Path.Combine("MetabolomicsDB", database.Id, "DataBase")).Open();
                saved.Position = 0;
                saved.CopyTo(boundedEntry);
            }
        }
        var budget = Environment.GetEnvironmentVariable("MSDIAL_TEST_GC_MAX_LOAD_MS");
        if (double.TryParse(budget, out var maximum)) { Assert.IsTrue(loadMilliseconds <= maximum, "Project library load exceeded its explicit local budget."); }
        // Keep the production cache owner alive through retained-memory measurements.
        GC.KeepAlive(calculator);
    }

    private void RecordMemory(string stage) {
        using var process = Process.GetCurrentProcess();
        TestContext.WriteLine($"{stage}: managed {GC.GetTotalMemory(false) / 1048576d:F1} MiB; working set {process.WorkingSet64 / 1048576d:F1} MiB; private {process.PrivateMemorySize64 / 1048576d:F1} MiB; peak {process.PeakWorkingSet64 / 1048576d:F1} MiB; collections {GC.CollectionCount(0)}/{GC.CollectionCount(1)}/{GC.CollectionCount(2)}.");
        if (Environment.GetEnvironmentVariable("MSDIAL_TEST_GC_HEAP_COUNTERS") != "1") { return; }
        var category = new PerformanceCounterCategory(".NET CLR Memory");
        var instance = category.GetInstanceNames().First(name => {
            using var counter = new PerformanceCounter(category.CategoryName, "Process ID", name, true);
            return counter.RawValue == process.Id;
        });
        foreach (var name in new[] { "# Bytes in all Heaps", "# Total committed Bytes", "Large Object Heap size", "Gen 2 heap size" }) {
            using var counter = new PerformanceCounter(category.CategoryName, name, instance, true);
            TestContext.WriteLine($"{stage}, CLR {name}: {counter.RawValue / 1048576d:F1} MiB.");
        }
    }

    private static string DigestResults(IEnumerable<GcmsCompoundResult> results) {
        using var hash = SHA256.Create();
        foreach (ICompoundResult result in results) {
            var bytes = MessagePackSerializer.Serialize(result.MatchResult);
            hash.TransformBlock(bytes, 0, bytes.Length, null, 0);
        }
        hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return BitConverter.ToString(hash.Hash).Replace("-", "");
    }

    private static string DigestLibrary(MoleculeDataBase database) {
        using var hash = SHA256.Create();
        foreach (var record in database.Database) {
            var bytes = MessagePackSerializer.Serialize(record.CreateSerializationCopy());
            hash.TransformBlock(bytes, 0, bytes.Length, null, 0);
        }
        hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return BitConverter.ToString(hash.Hash).Replace("-", "");
    }

    // Retain the previous reader as the local performance oracle, in the same process/build as the new reader.
    private sealed class LegacyCompactResolver : IFormatterResolver, IMessagePackFormatter<MoleculeMsReference>
    {
        public static readonly LegacyCompactResolver Instance = new();
        private readonly IMessagePackFormatter<MoleculeMsReference> _standard = StandardResolver.Instance.GetFormatter<MoleculeMsReference>();
        public IMessagePackFormatter<T> GetFormatter<T>() => typeof(T) == typeof(MoleculeMsReference)
            ? (IMessagePackFormatter<T>)(object)this : StandardResolver.Instance.GetFormatter<T>();
        public int Serialize(ref byte[] bytes, int offset, MoleculeMsReference value, IFormatterResolver resolver)
            => _standard.Serialize(ref bytes, offset, value, resolver);
        public MoleculeMsReference Deserialize(byte[] bytes, int offset, IFormatterResolver resolver, out int readSize) {
            var value = _standard.Deserialize(bytes, offset, resolver, out readSize);
            value?.TryCompactSpectrum();
            return value;
        }
    }
}

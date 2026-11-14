using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

internal static class Program {
    private static readonly System.Reflection.PropertyInfo CompactProperty = typeof(MoleculeMsReference).GetProperty("IsSpectrumCompact");

    private static void Main(string[] args) {
        if (args.Length != 4) { throw new ArgumentException("Expected: integer|fractional input.msp archive.bin result.json"); }
        var fractional = args[0] == "fractional";
        WarmUp(args[1], fractional);
        var result = ImportScoreSave(args[1], args[2], fractional);
        LoadAndVerify(args[2], result);
        result["inputSha256"] = HashFile(args[1]);
        result["archiveSha256"] = HashFile(args[2]);
        result["archiveBytes"] = new FileInfo(args[2]).Length;
        result["processPeakWorkingSetBytes"] = Process.GetCurrentProcess().PeakWorkingSet64;
        result["runtime"] = Environment.Version.ToString();
        result["is64Bit"] = Environment.Is64BitProcess;
        File.WriteAllText(args[3], JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WarmUp(string input, bool fractional) {
        var warm = input + ".warm.msp";
        // A separate tiny input warms the actual parser, serializers and score paths without
        // importing the measured library or expanding any of its spectra.
        var lines = File.ReadLines(input).Take(72).ToArray();
        File.WriteAllLines(warm, lines);
        var references = LibraryHandler.ReadMspLibrary(warm);
        var query = Query(fractional);
        foreach (var reference in references) { Ei(query, reference); Lc(query, reference); }
        var database = Database(references);
        using (var stream = new MemoryStream()) {
            database.Save(stream);
            stream.Position = 0;
            EmptyDatabase().Load(stream, "");
        }
        MessagePackSerializer.Serialize(Ei(query, references[0]));
        File.Delete(warm);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Dictionary<string, object> ImportScoreSave(string input, string archive, bool fractional) {
        var before = GC.GetTotalMemory(true);
        var timer = Stopwatch.StartNew();
        var references = LibraryHandler.ReadMspLibrary(input);
        timer.Stop();
        var result = new Dictionary<string, object> {
            ["importMs"] = timer.Elapsed.TotalMilliseconds,
            ["records"] = references.Count,
            ["retainedManagedBytes"] = GC.GetTotalMemory(true) - before,
            ["compactAfterImport"] = CompactCount(references),
        };
        var query = Query(fractional);
        foreach (var mode in new[] { "ei", "lc" }) {
            var times = new List<double>();
            MsScanMatchResult[] scores = null;
            for (var pass = 0; pass < 3; pass++) {
                scores = new MsScanMatchResult[references.Count];
                timer.Restart();
                for (var i = 0; i < references.Count; i++) {
                    scores[i] = mode == "ei" ? Ei(query, references[i]) : Lc(query, references[i]);
                }
                timer.Stop();
                times.Add(timer.Elapsed.TotalMilliseconds);
            }
            result[mode + "PassMs"] = times;
            result[mode + "ResultSha256"] = HashBytes(MessagePackSerializer.Serialize(scores));
        }
        result["compactAfterScoring"] = CompactCount(references);
        var database = Database(references);
        timer.Restart();
        using (var stream = File.Create(archive)) { database.Save(stream); }
        timer.Stop();
        result["saveMs"] = timer.Elapsed.TotalMilliseconds;
        result["compactAfterSave"] = CompactCount(references);
        GC.KeepAlive(references);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LoadAndVerify(string archive, Dictionary<string, object> result) {
        var before = GC.GetTotalMemory(true);
        var database = EmptyDatabase();
        var timer = Stopwatch.StartNew();
        using (var stream = File.OpenRead(archive)) { database.Load(stream, ""); }
        timer.Stop();
        result["loadMs"] = timer.Elapsed.TotalMilliseconds;
        result["loadedRetainedManagedBytes"] = GC.GetTotalMemory(true) - before;
        result["compactAfterLoad"] = CompactCount(database.Database);
        if (database.Database.Count != (int)result["records"]) { throw new Exception("Record count changed after load."); }
        var roundTrip = archive + ".roundtrip";
        using (var stream = File.Create(roundTrip)) { database.Save(stream); }
        if (HashFile(archive) != HashFile(roundTrip)) { throw new Exception("Serialized contents changed after load/save."); }
        File.Delete(roundTrip);
        GC.KeepAlive(database);
    }

    private static MoleculeMsReference Query(bool fractional) => new MoleculeMsReference {
        PrecursorMz = 500,
        IonMode = IonMode.Positive,
        Spectrum = Enumerable.Range(0, 64).Select(i => new SpectrumPeak(
            50 + i * 5 + (fractional ? .123456789 : 0),
            10 + (i * 37) % 990 + (fractional ? .375 : 0))).ToList(),
    };

    private static readonly MsRefSearchParameterBase Parameters = new MsRefSearchParameterBase {
        Ms1Tolerance = .01f, Ms2Tolerance = .05f, MassRangeBegin = 0, MassRangeEnd = 2000,
    };
    private static readonly MsReferenceScorer Scorer = new MsReferenceScorer("Benchmark", 0,
        TargetOmics.Metabolomics, SourceType.MspDB, CollisionType.CID, true, DataBaseSource.Msp);
    private static MsScanMatchResult Ei(MoleculeMsReference query, MoleculeMsReference reference) =>
        MsScanMatching.CompareEIMSScanProperties(query, reference, Parameters);
    private static MsScanMatchResult Lc(MoleculeMsReference query, MoleculeMsReference reference) =>
        Scorer.CalculateScore(query, query, null, reference, null, Parameters);
    private static MoleculeDataBase Database(IList<MoleculeMsReference> references) =>
        new MoleculeDataBase(references, "Benchmark", DataBaseSource.Msp, SourceType.MspDB, "synthetic.msp");
    private static MoleculeDataBase EmptyDatabase() => new MoleculeDataBase("Benchmark", SourceType.MspDB, DataBaseSource.Msp, "synthetic.msp");
    private static int CompactCount(IEnumerable<MoleculeMsReference> references) => CompactProperty == null ? 0
        : references.Count(reference => (bool)CompactProperty.GetValue(reference));
    private static string HashFile(string path) {
        using (var hash = SHA256.Create())
        using (var stream = File.OpenRead(path)) { return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
    }
    private static string HashBytes(byte[] bytes) {
        using (var hash = SHA256.Create()) { return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    }
}

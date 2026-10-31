using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace MsdialGuiAppTests.Model.Search;

[TestClass]
public sealed class LcmsCompoundSearchTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("LocalIntegration")]
    public void BoundedSyntheticBenchmarkReportsFirstAndRepeatedFullSearches() {
        if (Environment.GetEnvironmentVariable("MSDIAL_RUN_SYNTHETIC_LC_BENCHMARK") != "1") {
            Assert.Inconclusive("Set MSDIAL_RUN_SYNTHETIC_LC_BENCHMARK=1 to run the bounded LC benchmark.");
        }
        MessagePackSerializer.Serialize(new MsRefSearchParameterBase());
        MessagePackSerializer.Serialize(new MoleculeMsReference { Spectrum = Peaks() });
        foreach (var count in new[] { 1000, 10000, 50000 }) {
            var (searcher, annotator, query, _) = Create(count);
            var timer = Stopwatch.StartNew();
            var first = Search(searcher, query);
            var firstMs = timer.Elapsed.TotalMilliseconds;
            AssertOriginal(searcher, annotator, query);
            var repeats = new List<double>();
            for (var i = 0; i < 5; i++) {
                timer.Restart(); var actual = Search(searcher, query); repeats.Add(timer.Elapsed.TotalMilliseconds);
                CollectionAssert.AreEqual(MessagePackSerializer.Serialize(first.Select(r => r.MatchResult).ToArray()),
                    MessagePackSerializer.Serialize(actual.Select(r => r.MatchResult).ToArray()));
            }
            TestContext.WriteLine($"{count} candidates, full scoring every call: first {firstMs:F3} ms; repeated median {repeats.OrderBy(v => v).ElementAt(2):F3} ms.");
        }
    }

    [DataTestMethod]
    [DataRow(0, TargetOmics.Metabolomics)]
    [DataRow(512, TargetOmics.Metabolomics)]
    [DataRow(50001, TargetOmics.Metabolomics)]
    [DataRow(512, TargetOmics.Lipidomics)]
    public void MainSearchPreservesEveryOrderedRecordFromTheOriginalAnnotator(int count, TargetOmics omics) {
        var (searcher, annotator, query, _) = Create(count, omics);
        AssertOriginal(searcher, annotator, query);
        AssertOriginal(searcher, annotator, query);
    }

    [TestMethod]
    public void ParameterReferenceAndQueryEditsAreScoredAgain() {
        var (searcher, annotator, query, database) = Create(512);
        AssertOriginal(searcher, annotator, query);
        foreach (var property in typeof(MsRefSearchParameterBase).GetProperties().Where(p => p.CanRead && p.CanWrite)) {
            var value = property.GetValue(searcher.MsRefSearchParameter);
            if (value is bool flag) { property.SetValue(searcher.MsRefSearchParameter, !flag); }
            else if (value is float number) { property.SetValue(searcher.MsRefSearchParameter, number + .01f); }
            else if (value is int integer) { property.SetValue(searcher.MsRefSearchParameter, integer + 1); }
            else { continue; }
            AssertOriginal(searcher, annotator, query);
            property.SetValue(searcher.MsRefSearchParameter, value);
        }
        database.Database[0].Name = "Edited reference";
        database.Database[0].Spectrum[0].Intensity++;
        query.Spectrum[0].Intensity++;
        query.PrecursorMz += .001;
        AssertOriginal(searcher, annotator, query);
    }

    [TestMethod]
    public void ManualResultEditsDoNotChangeLaterScoresAndCancelledQueriesStop() {
        var (searcher, annotator, query, _) = Create(512);
        var first = Search(searcher, query);
        first[0].MatchResult.Name = "Manual annotation";
        first[0].MatchResult.TotalScore = -100;
        AssertOriginal(searcher, annotator, query);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() => searcher.Search(query, query,
            Array.Empty<CompMs.Common.DataObj.RawPeakElement>(), new IonFeatureCharacter(), cancellation.Token).ToArray());
    }

    private static void AssertOriginal(CompoundSearcher searcher, LcmsMspAnnotator annotator, ChromatogramPeakFeature query) {
        var expected = annotator.FindCandidates(new AnnotationQuery(query, query, null, null,
            searcher.MsRefSearchParameter, annotator));
        foreach (var record in expected) { record.Source |= SourceType.Manual; }
        var actual = Search(searcher, query);
        Assert.AreEqual(expected.Count, actual.Length);
        for (var i = 0; i < expected.Count; i++) {
            CollectionAssert.AreEqual(MessagePackSerializer.Serialize(expected[i]), MessagePackSerializer.Serialize(actual[i].MatchResult));
        }
    }

    private static ICompoundResult[] Search(CompoundSearcher searcher, ChromatogramPeakFeature query) => searcher.Search(
        query, query, Array.Empty<CompMs.Common.DataObj.RawPeakElement>(), new IonFeatureCharacter()).ToArray();

    private static (CompoundSearcher, LcmsMspAnnotator, ChromatogramPeakFeature, MoleculeDataBase) Create(int count, TargetOmics omics = TargetOmics.Metabolomics) {
        var records = Enumerable.Range(0, count).Select(index => new MoleculeMsReference {
            Name = "Reference " + index, ScanID = index, PrecursorMz = 300, IonMode = IonMode.Positive,
            ChromXs = new ChromXs(5), Spectrum = Peaks(),
        }).ToList();
        foreach (var record in records) { record.TryCompactSpectrum(); }
        var database = new MoleculeDataBase(records, "LC", DataBaseSource.Msp, SourceType.MspDB, "");
        var parameter = new MsRefSearchParameterBase { IsUseTimeForAnnotationScoring = true };
        var annotator = new LcmsMspAnnotator(database, parameter, omics, "LC", 0);
        var factory = new AnnotationQueryFactory(annotator, new PeakPickBaseParameter(), parameter);
        var query = new ChromatogramPeakFeature { PrecursorMz = 300, IonMode = IonMode.Positive, ChromXs = new ChromXs(5), Spectrum = Peaks() };
        return (new CompoundSearcher(factory, database), annotator, query, database);
    }

    private static List<SpectrumPeak> Peaks() => new() { new(73, 100), new(147, 50), new(205, 25) };
}

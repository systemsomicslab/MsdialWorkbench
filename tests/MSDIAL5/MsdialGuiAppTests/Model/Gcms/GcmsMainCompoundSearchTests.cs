using System.Threading;
using System;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Gcms;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialGcMsApi.Algorithm;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace MsdialGuiAppTests.Model.Gcms;

[TestClass]
public sealed class GcmsMainCompoundSearchTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("LocalIntegration")]
    public void BoundedSyntheticGcBenchmarkIncludesReferenceValidationAndViewerExposure() {
        if (Environment.GetEnvironmentVariable("MSDIAL_RUN_SYNTHETIC_GC_BENCHMARK") != "1") {
            Assert.Inconclusive("Set MSDIAL_RUN_SYNTHETIC_GC_BENCHMARK=1 to run the bounded synthetic GC benchmark.");
        }
        foreach (var count in new[] { 10000, 50000 }) {
            var references = Enumerable.Range(0, count).Select(index => {
                var reference = new MoleculeMsReference { ScanID = index, Name = "Synthetic " + index,
                    ChromXs = new ChromXs(1000, ChromXType.RI, ChromXUnit.None),
                    Spectrum = Enumerable.Range(0, 24).Select(peak => new SpectrumPeak(50 + 15 * peak, 30 + peak * 37)).ToList() };
                reference.TryCompactSpectrum();
                return reference;
            }).ToList();
            var calculator = new CalculateMatchScore(new DataBaseItem<MoleculeDataBase>(
                new MoleculeDataBase(references, "Test", DataBaseSource.Msp, SourceType.MspDB, ""), []),
                new MsRefSearchParameterBase(), RetentionType.RI, RiCompoundType.Alkanes);
            var scan = new MSDecResult { ChromXs = new ChromXs(1000, ChromXType.RI, ChromXUnit.None), Spectrum = Spectrum() };
            var search = new GcmsAlignmentCompoundSearchUsecase(calculator);
            var target = new PeakSpotModel(null!, scan);
            var expected = calculator.CalculateMatches(scan).OrderByDescending(result => result.TotalScore).ToArray();
            foreach (var result in expected) { result.Source |= SourceType.Manual; }
            var first = search.Search(target);
            _ = ((ICompoundResult)first[0]).MsReference.Spectrum.Count;
            var cold = new List<double>();
            var warm = new List<double>();
            for (var repeat = 0; repeat < 5; repeat++) {
                var watch = Stopwatch.StartNew();
                var control = calculator.CalculateMatches(scan).OrderByDescending(result => result.TotalScore).ToArray();
                cold.Add(watch.Elapsed.TotalMilliseconds);
                foreach (var result in control) { result.Source |= SourceType.Manual; }
                watch.Restart();
                var actual = search.Search(target);
                warm.Add(watch.Elapsed.TotalMilliseconds);
                Assert.AreEqual(count, actual.Count);
                for (var index = 0; index < count; index++) {
                    CollectionAssert.AreEqual(MessagePackSerializer.Serialize(expected[index]), MessagePackSerializer.Serialize(control[index]));
                    CollectionAssert.AreEqual(MessagePackSerializer.Serialize(expected[index]), MessagePackSerializer.Serialize(((ICompoundResult)actual[index]).MatchResult));
                }
            }
            TestContext.WriteLine($"{count} GC records/candidates, 24 reference peaks: uncached median {cold.OrderBy(value => value).ElementAt(2):F3} ms; validated viewer-exposed hit median {warm.OrderBy(value => value).ElementAt(2):F3} ms.");
        }
    }

    [DataTestMethod]
    [DataRow(false, 0)] [DataRow(true, 0)]
    [DataRow(false, 1)] [DataRow(true, 1)]
    [DataRow(true, 2)]
    public void MainSearchKeepsMoreThanOneHundredCandidatesOnColdAndCachedQueries(bool alignment, int spectrumKind) {
        var references = Enumerable.Range(0, 150).Select(index => new MoleculeMsReference {
            ScanID = index, Name = "Candidate " + index,
            ChromXs = new ChromXs(1000, ChromXType.RI, ChromXUnit.None), Spectrum = Spectrum(),
        }).ToList();
        var calculator = new CalculateMatchScore(new DataBaseItem<MoleculeDataBase>(
            new MoleculeDataBase(references, "Main", DataBaseSource.Msp, SourceType.MspDB, ""), []),
            new MsRefSearchParameterBase { Ms1Tolerance = .5f, RiTolerance = 100 }, RetentionType.RI, RiCompoundType.Alkanes);
        var scan = new MSDecResult {
            ChromXs = new ChromXs(1000, ChromXType.RI, ChromXUnit.None),
            Spectrum = spectrumKind == 0 ? Spectrum() : spectrumKind == 1 ? [] : null!,
        };
        var expected = calculator.CalculateMatches(scan).OrderByDescending(result => result.TotalScore).ToArray();
        foreach (var result in expected) { result.Source |= SourceType.Manual; }
        using var feature = alignment ? null : new Ms1BasedSpectrumFeature(new SpectrumFeature(new AnnotatedMSDecResult(scan, new MsScanMatchResultContainer()), null!));
        var cold = Search();
        var cached = Search();
        Assert.AreEqual(150, cold.Count);
        Assert.AreEqual(150, cached.Count);
        CollectionAssert.AreEqual(Enumerable.Range(0, 150).ToArray(), cold.Select(result => result.LibraryID).ToArray());
        for (var index = 0; index < cold.Count; index++) {
            CollectionAssert.AreEqual(MessagePackSerializer.Serialize(expected[index]),
                MessagePackSerializer.Serialize(((ICompoundResult)cold[index]).MatchResult));
            CollectionAssert.AreEqual(MessagePackSerializer.Serialize(((ICompoundResult)cold[index]).MatchResult),
                MessagePackSerializer.Serialize(((ICompoundResult)cached[index]).MatchResult));
        }

        IReadOnlyList<GcmsCompoundResult> Search() => alignment
            ? new GcmsAlignmentCompoundSearchUsecase(calculator).Search(new PeakSpotModel(null!, scan))
            : new GcmsAnalysisCompoundSearchUsecase(calculator).Search(feature!);
    }

    [TestMethod]
    public void LibraryEditsInvalidateTheProductionGcCache() {
        var reference = new MoleculeMsReference { Name = "Original", ChromXs = new ChromXs(1000, ChromXType.RI, ChromXUnit.None), Spectrum = Spectrum() };
        reference.TryCompactSpectrum();
        var parameter = new MsRefSearchParameterBase();
        var calculator = new CalculateMatchScore(new DataBaseItem<MoleculeDataBase>(
            new MoleculeDataBase(new[] { reference }, "Test", DataBaseSource.Msp, SourceType.MspDB, ""), []),
            parameter, RetentionType.RI, RiCompoundType.Alkanes);
        var cache = GcmsCompoundSearchCache.For(calculator);
        var target = new MSScanProperty { ChromXs = new ChromXs(1000, ChromXType.RI, ChromXUnit.None), Spectrum = Spectrum() };
        var searches = 0;
        IReadOnlyList<GcmsCompoundResult> Search(MSScanProperty scan, MsRefSearchParameterBase settings) {
            searches++;
            return new[] { new GcmsCompoundResult(reference, new MsScanMatchResult { Name = reference.Name }) };
        }
        cache.Search(target, parameter, Search, default);
        cache.Search(target, parameter, Search, default);
        Assert.AreEqual(1, searches);
        reference.Name = "Edited";
        var changed = cache.Search(target, parameter, Search, default);
        Assert.AreEqual("Edited", ((ICompoundResult)changed[0]).MatchResult.Name);
        Assert.AreEqual(2, searches);
        reference.Spectrum[0].Intensity++;
        cache.Search(target, parameter, Search, default);
        cache.Search(target, parameter, Search, default);
        Assert.AreEqual(3, searches, "Reference edits require fresh scores; an unchanged exposed list can then be reused.");
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)]
    public void EiCacheIgnoresUnusedReferenceIsotopeProfilesButStillValidatesSpectrumEdits(int isotopeKind) {
        var reference = new MoleculeMsReference { ChromXs = new ChromXs(1000, ChromXType.RI, ChromXUnit.None), Spectrum = Spectrum() };
        if (isotopeKind == 0) { reference.IsotopicPeaks = null!; }
        if (isotopeKind == 2) {
            reference.IsotopicPeaks.Add(new CompMs.Common.DataObj.Property.IsotopicPeak { Mass = 100, RelativeAbundance = 100 });
        }
        reference.TryCompactSpectrum();
        var parameter = new MsRefSearchParameterBase();
        var calculator = new CalculateMatchScore(new DataBaseItem<MoleculeDataBase>(
            new MoleculeDataBase(new[] { reference }, "Test", DataBaseSource.Msp, SourceType.MspDB, ""), []),
            parameter, RetentionType.RI, RiCompoundType.Alkanes);
        var cache = GcmsCompoundSearchCache.For(calculator);
        var target = new MSScanProperty { ChromXs = new ChromXs(1000, ChromXType.RI, ChromXUnit.None), Spectrum = Spectrum() };
        var searches = 0;
        IReadOnlyList<GcmsCompoundResult> Search(MSScanProperty scan, MsRefSearchParameterBase settings) {
            searches++;
            return calculator.CalculateMatches(scan).Select(result => new GcmsCompoundResult(reference, result)).ToArray();
        }
        cache.Search(target, parameter, Search, default);
        var repeated = cache.Search(target, parameter, Search, default);
        Assert.AreEqual(1, searches, "EI does not score reference isotope profiles; they must not disable reuse.");
        if (reference.IsotopicPeaks?.Count > 0) { reference.IsotopicPeaks[0].RelativeAbundance++; }
        repeated = cache.Search(target, parameter, Search, default);
        Assert.AreEqual(1, searches);
        CollectionAssert.AreEqual(MessagePackSerializer.Serialize(calculator.CalculateMatches(target).Single()),
            MessagePackSerializer.Serialize(((ICompoundResult)repeated[0]).MatchResult));
        reference.Spectrum[0].Intensity++;
        var changed = cache.Search(target, parameter, Search, default);
        Assert.AreEqual(2, searches);
        CollectionAssert.AreEqual(MessagePackSerializer.Serialize(calculator.CalculateMatches(target).Single()),
            MessagePackSerializer.Serialize(((ICompoundResult)changed[0]).MatchResult));
    }

    [DataTestMethod]
    [DataRow(RetentionType.RI)] [DataRow(RetentionType.RT)]
    public void CandidateCacheIgnoresUnscoredEditsAndDetectsReferencesEnteringTheWindow(RetentionType retention) {
        var type = retention == RetentionType.RI ? ChromXType.RI : ChromXType.RT;
        var unit = retention == RetentionType.RI ? ChromXUnit.None : ChromXUnit.Min;
        var selected = new MoleculeMsReference { ScanID = 0, Name = "Selected", ChromXs = new ChromXs(1000, type, unit), Spectrum = Spectrum() };
        var outside = new MoleculeMsReference { ScanID = 1, Name = "Outside", ChromXs = new ChromXs(1300, type, unit), Spectrum = Spectrum() };
        selected.TryCompactSpectrum(); outside.TryCompactSpectrum();
        var settings = new MsRefSearchParameterBase { RiTolerance = 100, RtTolerance = 100 };
        var calculator = new CalculateMatchScore(new DataBaseItem<MoleculeDataBase>(
            new MoleculeDataBase(new[] { selected, outside }, "Window", DataBaseSource.Msp, SourceType.MspDB, ""), []),
            settings, retention, RiCompoundType.Alkanes);
        var cache = GcmsCompoundSearchCache.For(calculator);
        var target = new MSScanProperty { ChromXs = new ChromXs(1000, type, unit), Spectrum = Spectrum() };
        var calls = 0;
        IReadOnlyList<GcmsCompoundResult> Full(MSScanProperty scan, MsRefSearchParameterBase parameter) {
            calls++;
            var scorer = calculator.With(parameter);
            return scorer.CalculateMatches(scan).OrderByDescending(result => result.TotalScore)
                .Select(result => new GcmsCompoundResult(scorer.Reference(result), result)).ToArray();
        }
        cache.Search(target, settings, Full, default);
        outside.Name = "Edited outside";
        outside.Spectrum[0].Intensity++;
        var unchanged = cache.Search(target, settings, Full, default);
        Assert.AreEqual(1, calls, "Unscored spectrum/name edits do not change this query's results.");
        Assert.AreEqual(1, unchanged.Count);
        outside.ChromXs = new ChromXs(1100, type, unit);
        var changed = cache.Search(target, settings, Full, default);
        Assert.AreEqual(2, calls, "Candidate membership must be checked again before reusing scores.");
        Assert.AreEqual(2, changed.Count);
        var expected = calculator.CalculateMatches(target).OrderByDescending(result => result.TotalScore).ToArray();
        for (var i = 0; i < expected.Length; i++) {
            CollectionAssert.AreEqual(MessagePackSerializer.Serialize(expected[i]), MessagePackSerializer.Serialize(((ICompoundResult)changed[i]).MatchResult));
        }
    }

    [TestMethod]
    public void CacheInvalidatesParametersAndSpectrumAndDoesNotShareMutableAnnotationScores() {
        var parameter = new MsRefSearchParameterBase();
        var cache = CacheFor(new MoleculeMsReference { Spectrum = Spectrum() }, parameter);
        var target = new MSScanProperty { ChromXs = new ChromXs(), Spectrum = Spectrum() };
        var searches = 0;
        IReadOnlyList<GcmsCompoundResult> Search(MSScanProperty query, MsRefSearchParameterBase settings) {
            searches++;
            return new[] { new GcmsCompoundResult(new MoleculeMsReference { Name = "Cached" }, new MsScanMatchResult { TotalScore = .8f }) };
        }
        var first = cache.Search(target, parameter, Search, default);
        ((ICompoundResult)first[0]).MatchResult.TotalScore = 0;
        var second = cache.Search(target, parameter, Search, default);
        Assert.AreEqual(1, searches);
        Assert.AreEqual(.8f, ((ICompoundResult)second[0]).MatchResult.TotalScore);
        parameter.IsUseTimeForAnnotationScoring = true;
        cache.Search(target, parameter, Search, default);
        Assert.AreEqual(2, searches);
        target.Spectrum[0].Intensity++;
        cache.Search(target, parameter, Search, default);
        Assert.AreEqual(3, searches);
        parameter.RiTolerance++;
        cache.Search(target, parameter, Search, default);
        Assert.AreEqual(4, searches);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() => cache.Search(target, parameter, Search, cancellation.Token));
        Assert.AreEqual(4, searches, "Cancellation must be checked on cache hits too.");
    }

    [TestMethod]
    public void CancelledComputationDoesNotReplaceThePreviousCachedResult() {
        var parameter = new MsRefSearchParameterBase();
        var cache = CacheFor(new MoleculeMsReference { Spectrum = Spectrum() }, parameter);
        var target = new MSScanProperty { ChromXs = new ChromXs(), Spectrum = Spectrum() };
        using var cancellation = new CancellationTokenSource();
        var searches = 0;
        IReadOnlyList<GcmsCompoundResult> Search(MSScanProperty query, MsRefSearchParameterBase settings) {
            if (++searches == 2) { cancellation.Cancel(); }
            return new[] { new GcmsCompoundResult(new MoleculeMsReference(), new MsScanMatchResult()) };
        }
        cache.Search(target, parameter, Search, default);
        target.Spectrum[0].Intensity++;
        Assert.ThrowsException<OperationCanceledException>(() => cache.Search(target, parameter, Search, cancellation.Token));
        target.Spectrum[0].Intensity--;
        cache.Search(target, parameter, Search, default);
        Assert.AreEqual(2, searches, "A cancelled computation must leave the last complete cached query intact.");
    }

    private static GcmsCompoundSearchCache CacheFor(MoleculeMsReference reference, MsRefSearchParameterBase parameter)
        => GcmsCompoundSearchCache.For(new CalculateMatchScore(new DataBaseItem<MoleculeDataBase>(
            new MoleculeDataBase(new[] { reference }, "Cache", DataBaseSource.Msp, SourceType.MspDB, ""), []),
            parameter, RetentionType.RI, RiCompoundType.Alkanes));

    [TestMethod]
    public void ConcurrentSearchesAndReferenceViewingPreserveEveryResultAndAnnotationIsolation() {
        var references = Enumerable.Range(0, 64).Select(index => new MoleculeMsReference {
            ScanID = index, Name = "Reference " + index,
            ChromXs = new ChromXs(1000, ChromXType.RI, ChromXUnit.None), Spectrum = Spectrum(),
        }).ToArray();
        foreach (var reference in references) { reference.TryCompactSpectrum(); }
        var parameter = new MsRefSearchParameterBase { Ms1Tolerance = .5f };
        var calculator = new CalculateMatchScore(new DataBaseItem<MoleculeDataBase>(
            new MoleculeDataBase(references, "Concurrent", DataBaseSource.Msp, SourceType.MspDB, ""), []),
            parameter, RetentionType.RI, RiCompoundType.Alkanes);
        var cache = GcmsCompoundSearchCache.For(calculator);
        var target = new MSScanProperty { ChromXs = new ChromXs(1000, ChromXType.RI, ChromXUnit.None), Spectrum = Spectrum() };
        IReadOnlyList<GcmsCompoundResult> Full(MSScanProperty query, MsRefSearchParameterBase settings) {
            var scorer = calculator.With(settings);
            return scorer.CalculateMatches(query).OrderByDescending(result => result.TotalScore)
                .Select(result => new GcmsCompoundResult(scorer.Reference(result), result)).ToArray();
        }
        var expected = MessagePackSerializer.Serialize(Full(target, parameter).Select(result => ((ICompoundResult)result).MatchResult).ToArray());
        System.Threading.Tasks.Parallel.For(0, 64, index => {
            _ = references[index].Spectrum;
            for (var repeat = 0; repeat < 4; repeat++) {
                var results = cache.Search(target, parameter, Full, default);
                Assert.AreEqual(references.Length, results.Count);
                CollectionAssert.AreEqual(expected, MessagePackSerializer.Serialize(results.Select(result => ((ICompoundResult)result).MatchResult).ToArray()));
                ((ICompoundResult)results[0]).MatchResult.TotalScore = -100;
            }
        });
    }

    private static List<SpectrumPeak> Spectrum() => [
        new SpectrumPeak(73, 100), new SpectrumPeak(147, 60), new SpectrumPeak(205, 40), new SpectrumPeak(291, 20),
    ];
}

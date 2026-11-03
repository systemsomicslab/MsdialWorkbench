using CompMs.Common.Components;
using CompMs.Common.Components.Tests;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Algorithm.Scoring.Tests;

[TestClass()]
public class MsScanMatchingTests
{
    private readonly double _mzTolerance = .05;
    private readonly double _mzMin = 0;
    private readonly double _mzMax = double.MaxValue;

    [TestMethod()]
    public void GetSimpleDotProduct_SelfComparison() {
        var scan = CreateScan(20, 1, 42);
        var actual = MsScanMatching.GetSimpleDotProduct(scan, scan, _mzTolerance, _mzMin, _mzMax);
        Assert.AreEqual(1d, actual, .00001, "The similarity between a scan and itself should be 1.");
    }

    [TestMethod()]
    public void GetSimpleDotProduct_IsCommutative() {
        var scans = CreateScanBatch([20, 15], 1, 42);
        var actual = MsScanMatching.GetSimpleDotProduct(scans[0], scans[1], _mzTolerance, _mzMin, _mzMax);
        var expected = MsScanMatching.GetSimpleDotProduct(scans[1], scans[0], _mzTolerance, _mzMin, _mzMax);
        Assert.AreEqual(expected, actual, .00001, "The similarity between scan[0] and scan[1] should be the same as scan[1] and scan[0].");
    }

    [TestMethod()]
    public void GetSimpleDotProduct_MergeIfClose() {
        var scan1 = new MSScanProperty { Spectrum = [ new() { Mass = 100.00, Intensity = 1d, }, new() { Mass = 100.06, Intensity = 1d, }, ], };
        var scan2 = new MSScanProperty { Spectrum = [ new() { Mass = 99.98, Intensity = 1d, }, new() { Mass = 100.02, Intensity = 1d, }, ], };
        var actual = MsScanMatching.GetSimpleDotProduct(scan1, scan2, .05, _mzMin, _mzMax);
        Console.WriteLine("scan1:");
        scan1.Spectrum.ShowForTest();
        Console.WriteLine("scan2:");
        scan2.Spectrum.ShowForTest();
        Assert.AreEqual(.5, actual, .00001);
    }

    [TestMethod()]
    public void GetSimpleDotProduct_MassBeginWorks() {
        var scan1 = CreateScan(20, 1, 42);
        var scan2 = CreateScan(20, .4, 1412);
        var actual = MsScanMatching.GetSimpleDotProduct(scan1, scan2, _mzTolerance, 0.5, _mzMax);
        Console.WriteLine("scan1:");
        scan1.Spectrum.ShowForTest();
        Console.WriteLine("scan2:");
        scan2.Spectrum.ShowForTest();
        Assert.AreEqual(0d, actual, .00001, "The similarity between scan1 and scan2 should be 0.");
    }

    [DataTestMethod()]
    [DynamicData(nameof(GetBatchSimpleDotProduct_Failed_Cases_Data), DynamicDataSourceType.Property)]
    public void GetBatchSimpleDotProduct_Failed_Cases(IMSScanProperty[] scans) {
        var actuals = MsScanMatching.GetBatchSimpleDotProduct(scans, _mzTolerance, _mzMin, _mzMax);
        for (int i = 0; i < actuals.Length; i++) {
            for (int j = 0; j < actuals[i].Length; j++) {
                var expected = i == j ? 1d : MsScanMatching.GetSimpleDotProduct(scans[i], scans[j], _mzTolerance, _mzMin, _mzMax);
                try {
                    Assert.AreEqual(expected, actuals[i][j], .00001, $"The similarity between scan[{i}] and scan[{j}] is inconsistent.");
                }
                catch (AssertFailedException) {
                    Console.WriteLine("scan[{0}]:", i);
                    scans[i].Spectrum.ShowForTest();
                    Console.WriteLine("scan[{0}]:", j);
                    scans[j].Spectrum.ShowForTest();
                    throw;
                }
            }
        }
    }

    public static IEnumerable<object[]> GetBatchSimpleDotProduct_Failed_Cases_Data {
        get {
            yield return [
                new[]
                {
                    new MSScanProperty { Spectrum = [ new() { Mass = 100.00, Intensity = 1d, }, new() { Mass = 100.06, Intensity = 1d, }, ], },
                    new MSScanProperty { Spectrum = [ new() { Mass = 99.98, Intensity = 1d, }, new() { Mass = 100.02, Intensity = 1d, }, ], },
                },
            ];
            yield return [
                new[]
                {
                    new MSScanProperty { Spectrum = [ new() { Mass = 99.97, Intensity = 1d, }, new() { Mass = 100.00, Intensity = 1d, }, ], },
                    new MSScanProperty { Spectrum = [ new() { Mass = 99.93, Intensity = 1d, }, new() { Mass = 100.03, Intensity = 1d, }, ], },
                },
            ];
        }
    }

    [DataTestMethod()]
    [DataRow(2, 6, 1, 170)]
    [DataRow(7, 10, 5, 385)]
    [DataRow(2, 1, 0, 82)]
    [DataRow(5, 20, 5, 42)]
    [DataRow(5, 20, 5, 1412)]
    //[DataRow(1000, 100, 50, 42)]
    public void GetBatchSimpleDotProduct_MatchesIndividual(int size, int nPeak, int vPeak, int seed) {
        var rng = new Random(seed);
        var scans = CreateScanBatch(Enumerable.Repeat(0, size).Select(_ => rng.Next(-vPeak, vPeak) + nPeak).ToArray(), 1, rng.Next());
        var actuals = MsScanMatching.GetBatchSimpleDotProduct(scans, _mzTolerance, _mzMin, _mzMax);
        for (int i = 0; i < scans.Length; i++) {
            for (int j = 0; j < scans.Length; j++) {
                var expected = i == j ? 1d : MsScanMatching.GetSimpleDotProduct(scans[i], scans[j], _mzTolerance, _mzMin, _mzMax);
                try {
                    Assert.AreEqual(expected, actuals[i][j], .00001, $"The similarity between scan[{i}] and scan[{j}] is inconsistent.");
                }
                catch (AssertFailedException) {
                    Console.WriteLine("scan[{0}]:", i);
                    scans[i].Spectrum.ShowForTest();
                    Console.WriteLine("scan[{0}]:", j);
                    scans[j].Spectrum.ShowForTest();
                    throw;
                }
            }
        }
    }

    [TestMethod()]
    public void GetBatchSimpleDotProduct_ContainsNoPeakScan() {
        var rng = new Random(42);
        var scans = CreateScanBatch(Enumerable.Repeat(0, 5).Select(_ => rng.Next(15, 25)).ToArray(), 1, rng.Next());
        scans[2].Spectrum = null;
        scans[3].Spectrum = null;
        var actuals = MsScanMatching.GetBatchSimpleDotProduct(scans, _mzTolerance, _mzMin, _mzMax);
        for (int i = 0; i < scans.Length; i++) {
            for (int j = 0; j < scans.Length; j++) {
                var expected = i == j ? 1d : MsScanMatching.GetSimpleDotProduct(scans[i], scans[j], _mzTolerance, _mzMin, _mzMax);
                Assert.AreEqual(expected, actuals[i][j], .00001, $"The similarity between scan[{i}] and scan[{j}] is inconsistent.");
            }
        }
    }

    [DataTestMethod()]
    [DataRow(15, 25, .0, 42)]
    [DataRow(15, 25, .1, 42)]
    [DataRow(15, 25, .5, 42)]
    [DataRow(15, 25, .9, 42)]
    public void GetBatchSimpleDotProduct_MassBeginWorks(int nPeakMin, int nPeakMax, double massBegin, int seed) {
        var rng = new Random(seed);
        var scans = CreateScanBatch(Enumerable.Repeat(0, 5).Select(_ => rng.Next(nPeakMin, nPeakMax)).ToArray(), 1, rng.Next());
        var actuals = MsScanMatching.GetBatchSimpleDotProduct(scans, _mzTolerance, massBegin, _mzMax);
        for (int i = 0; i < scans.Length; i++) {
            for (int j = 0; j < scans.Length; j++) {
                var expected = i == j ? 1d : MsScanMatching.GetSimpleDotProduct(scans[i], scans[j], _mzTolerance, massBegin, _mzMax);
                try {
                    Assert.AreEqual(expected, actuals[i][j], .00001, $"The similarity between scan[{i}] and scan[{j}] is inconsistent.");
                }
                catch (AssertFailedException) {
                    Console.WriteLine("scan[{0}]:", i);
                    scans[i].Spectrum.ShowForTest();
                    Console.WriteLine("scan[{0}]:", j);
                    scans[j].Spectrum.ShowForTest();
                    throw;
                }
            }
        }
    }

    [TestMethod()]
    public void GetLipidomicsMatchedPeaksScores_UnparsedLipidName_ReturnsMatchedPeaksScores() {
        var reference = CreateUnparsedLipidReference();
        var scan = CreateQueryScan(reference);
        var expected = MsScanMatching.GetMatchedPeaksScores(scan, reference, _mzTolerance, _mzMin, _mzMax);
        var actual = MsScanMatching.GetLipidomicsMatchedPeaksScores(scan, reference, _mzTolerance, _mzMin, _mzMax);
        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod()]
    public void GetLipidomicsMoleculerSpeciesLevelAnnotationPeaksScoresForEIEIO_UnparsedLipidName_ReturnsMatchedPeaksScores() {
        var reference = CreateUnparsedLipidReference();
        var scan = CreateQueryScan(reference);
        var expected = MsScanMatching.GetMatchedPeaksScores(scan, reference, _mzTolerance, _mzMin, _mzMax);
        var actual = MsScanMatching.GetLipidomicsMoleculerSpeciesLevelAnnotationPeaksScoresForEIEIO(scan, reference, _mzTolerance, _mzMin, _mzMax);
        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod()]
    public void GetLipidomicsMoleculerSpeciesLevelAnnotationPeaksScoresForOAD_UnparsedLipidName_ReturnsMatchedPeaksScores() {
        var reference = CreateUnparsedLipidReference();
        var scan = CreateQueryScan(reference);
        var expected = MsScanMatching.GetMatchedPeaksScores(scan, reference, _mzTolerance, _mzMin, _mzMax);
        var actual = MsScanMatching.GetLipidomicsMoleculerSpeciesLevelAnnotationPeaksScoresForOAD(scan, reference, _mzTolerance, _mzMin, _mzMax);
        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod()]
    public void GetRefinedLipidAnnotationLevel_UnparsedLipidName_ReturnsReferenceNameAsOthers() {
        var reference = CreateUnparsedLipidReference();
        var scan = CreateQueryScan(reference);
        var actual = MsScanMatching.GetRefinedLipidAnnotationLevel(scan, reference, _mzTolerance,
            out var isLipidClassMatched, out var isLipidChainMatched, out var isLipidPositionMatched, out var isOthers);
        AssertReturnedAsOthers(reference, actual, isLipidClassMatched, isLipidChainMatched, isLipidPositionMatched, isOthers);
    }

    [TestMethod()]
    public void GetRefinedLipidAnnotationLevelForEIEIO_UnparsedLipidName_ReturnsReferenceNameAsOthers() {
        var reference = CreateUnparsedLipidReference();
        var scan = CreateQueryScan(reference);
        var actual = MsScanMatching.GetRefinedLipidAnnotationLevelForEIEIO(scan, reference, _mzTolerance,
            out var isLipidClassMatched, out var isLipidChainMatched, out var isLipidPositionMatched, out var isOthers);
        AssertReturnedAsOthers(reference, actual, isLipidClassMatched, isLipidChainMatched, isLipidPositionMatched, isOthers);
    }

    [TestMethod()]
    public void GetRefinedLipidAnnotationLevelForOAD_UnparsedLipidName_ReturnsReferenceNameAsOthers() {
        var reference = CreateUnparsedLipidReference();
        var scan = CreateQueryScan(reference);
        var actual = MsScanMatching.GetRefinedLipidAnnotationLevelForOAD(scan, reference, _mzTolerance,
            out var isLipidClassMatched, out var isLipidChainMatched, out var isLipidPositionMatched, out var isOthers);
        AssertReturnedAsOthers(reference, actual, isLipidClassMatched, isLipidChainMatched, isLipidPositionMatched, isOthers);
    }

    [TestMethod()]
    public void GetLipidNameFromReference_UnparsedLipidName_ReturnsReferenceName() {
        var reference = CreateUnparsedLipidReference();
        Assert.AreEqual(reference.Name, MsScanMatching.GetLipidNameFromReference(reference));
    }

    [TestMethod()]
    public void GetLipidomicsMatchedPeaksScores_ParsedLipidName_KeepsLipidBonus() {
        var reference = CreateParsedLipidReference();
        var scan = CreateQueryScan(reference);
        var plain = MsScanMatching.GetMatchedPeaksScores(scan, reference, _mzTolerance, _mzMin, _mzMax);
        var actual = MsScanMatching.GetLipidomicsMatchedPeaksScores(scan, reference, _mzTolerance, _mzMin, _mzMax);
        Assert.AreEqual(1d, plain[0]);
        CollectionAssert.AreEqual(new[] { 2d, plain[1], }, actual);
    }

    [TestMethod()]
    public void GetRefinedLipidAnnotationLevel_ParsedLipidName_MatchesClassAndChain() {
        var reference = CreateParsedLipidReference();
        var scan = CreateQueryScan(reference);
        var actual = MsScanMatching.GetRefinedLipidAnnotationLevel(scan, reference, _mzTolerance,
            out var isLipidClassMatched, out var isLipidChainMatched, out var isLipidPositionMatched, out var isOthers);
        Assert.AreEqual("PC 34:1|PC 16:0_18:1", actual);
        Assert.IsTrue(isLipidClassMatched);
        Assert.IsTrue(isLipidChainMatched);
        Assert.IsFalse(isLipidPositionMatched);
        Assert.IsFalse(isOthers);
    }

    [TestMethod()]
    public void GetLipidomicsMatchedPeaksScores_ChainWithoutColon_ReturnsMatchedPeaksScores() {
        var reference = CreateUnparsedLipidReference();
        reference.Name = "Cholesterol d7";
        reference.CompoundClass = "ST";
        var scan = CreateQueryScan(reference);
        var expected = MsScanMatching.GetMatchedPeaksScores(scan, reference, _mzTolerance, _mzMin, _mzMax);
        var actual = MsScanMatching.GetLipidomicsMatchedPeaksScores(scan, reference, _mzTolerance, _mzMin, _mzMax);
        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod()]
    public void GetLipidomicsMatchedPeaksScores_NullFormula_KeepsLipidBonus() {
        var reference = CreateParsedLipidReference();
        reference.Formula = null;
        var scan = CreateQueryScan(reference);
        var plain = MsScanMatching.GetMatchedPeaksScores(scan, reference, _mzTolerance, _mzMin, _mzMax);
        var actual = MsScanMatching.GetLipidomicsMatchedPeaksScores(scan, reference, _mzTolerance, _mzMin, _mzMax);
        CollectionAssert.AreEqual(new[] { 2d, plain[1], }, actual);
    }

    private static void AssertReturnedAsOthers(MoleculeMsReference reference, string actual,
        bool isLipidClassMatched, bool isLipidChainMatched, bool isLipidPositionMatched, bool isOthers) {
        Assert.AreEqual(reference.Name, actual);
        Assert.IsFalse(isLipidClassMatched);
        Assert.IsFalse(isLipidChainMatched);
        Assert.IsFalse(isLipidPositionMatched);
        Assert.IsTrue(isOthers);
    }

    // The CE record "Cholesterol(d7)" [M+NH4]+ of MSDIAL-TandemMassSpectralAtlas-VS69-Pos.msp.
    // LipidomicsConverter does not parse its name, so the lipid molecule has no LipidName.
    private static MoleculeMsReference CreateUnparsedLipidReference() {
        return new MoleculeMsReference
        {
            Name = "Cholesterol(d7)",
            CompoundClass = "CE",
            AdductType = AdductIon.GetAdductIon("[M+NH4]+"),
            PrecursorMz = 411.43259,
            IonMode = IonMode.Positive,
            Spectrum = [
                new() { Mass = 142.1612, Intensity = 200, },
                new() { Mass = 154.1612, Intensity = 200, },
                new() { Mass = 168.1768, Intensity = 200, },
                new() { Mass = 182.1926, Intensity = 200, },
                new() { Mass = 376.3949, Intensity = 999, },
                new() { Mass = 393.422, Intensity = 50, },
                new() { Mass = 411.4326, Intensity = 100, },
            ],
        };
    }

    // A PC whose name parses, with the m/z 184.0733 head group fragment and both acyl fragments.
    private static MoleculeMsReference CreateParsedLipidReference() {
        return new MoleculeMsReference
        {
            Name = "PC 16:0_18:1",
            CompoundClass = "PC",
            AdductType = AdductIon.GetAdductIon("[M+H]+"),
            PrecursorMz = 760.58508,
            IonMode = IonMode.Positive,
            Spectrum = [
                new() { Mass = 184.0733, Intensity = 999, },
                new() { Mass = 496.3398, Intensity = 50, },
                new() { Mass = 522.3554, Intensity = 50, },
                new() { Mass = 760.5851, Intensity = 100, },
            ],
        };
    }

    private static IMSScanProperty CreateQueryScan(MoleculeMsReference reference) {
        return new MSScanProperty
        {
            PrecursorMz = reference.PrecursorMz,
            IonMode = reference.IonMode,
            Spectrum = reference.Spectrum.Select(p => new SpectrumPeak { Mass = p.Mass, Intensity = p.Intensity, }).ToList(),
        };
    }

    private IMSScanProperty CreateScan(int size, double mzScale, int seed) {
        var rng = new Random(seed);
        var peaks = new List<SpectrumPeak>(size);
        for (int j = 0; j < size; j++) {
            peaks.Add(new()
            {
                Mass = rng.NextDouble() * mzScale,
                Intensity = rng.NextDouble(),
            });
        }
        peaks.Sort((a, b) => a.Mass.CompareTo(b.Mass));

        return new MSScanProperty
        {
            Spectrum = peaks,
        };
    }

    private IMSScanProperty[] CreateScanBatch(int[] sizes, double mzScale, int seed) {
        var scans = new IMSScanProperty[sizes.Length];

        var rng = new Random(seed);
        for (int i = 0; i < sizes.Length; i++) {
            scans[i] = CreateScan(sizes[i], mzScale, rng.Next());
        }

        return scans;
    }
}
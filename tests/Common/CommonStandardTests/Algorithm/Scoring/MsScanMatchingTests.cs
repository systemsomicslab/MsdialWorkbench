using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Algorithm.Scoring.Tests;

[TestClass()]
public class MsScanMatchingTests
{
    [TestMethod()]
    public void GetSimpleDotProduct_SelfComparison() {
        var scan = CreateScan(20, 1, 42);
        var actual = MsScanMatching.GetSimpleDotProduct(scan, scan, .05, 0, double.MaxValue);
        Assert.AreEqual(1d, actual, .001, "The similarity between a scan and itself should be 1.");
    }

    [TestMethod()]
    public void GetSimpleDotProduct_IsCummutative() {
        var scans = CreateScanBatch([20, 15], 1, 42);
        var actual = MsScanMatching.GetSimpleDotProduct(scans[0], scans[1], .05, 0, double.MaxValue);
        var expected = MsScanMatching.GetSimpleDotProduct(scans[1], scans[0], .05, 0, double.MaxValue);
        Assert.AreEqual(expected, actual, .001, "The similarity between scan[0] and scan[1] should be the same as scan[1] and scan[0].");
    }

    [DataTestMethod()]
    [DataRow(5, 20, 5, 42)]
    [DataRow(5, 20, 5, 1412)]
    public void GetBatchSimpleDotProduct_MatchesIndividual(int size, int nPeak, int vPeak, int seed) {
        var rng = new Random(seed);
        var scans = CreateScanBatch(Enumerable.Repeat(0, size).Select(_ => rng.Next(-vPeak,vPeak) + nPeak).ToArray(), 1, rng.Next());
        var actuals = MsScanMatching.GetBatchSimpleDotProduct(scans, .05, 0, double.MaxValue);
        for (int i = 0; i < scans.Length; i++) {
            for (int j = 0; j < scans.Length; j++) {
                var expected = MsScanMatching.GetSimpleDotProduct(scans[i], scans[j], .05, 0, double.MaxValue);
                Assert.AreEqual(expected, actuals[i][j], .001, $"The similarity between scan[{i}] and scan[{j}] is inconsistent.");
            }
        }
    }

    private IMSScanProperty CreateScan(int size, double mzScale, int seed) {
        var scan = new MSScanProperty();

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
        scan.Spectrum = peaks;

        return scan;
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
using CompMs.Common.Components;
using CompMs.Common.Components.Tests;
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
    public void GetSimpleDotProduct_IsCummutative() {
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
    [DataRow(5, 20, 5, 42)]
    [DataRow(5, 20, 5, 1412)]
    public void GetBatchSimpleDotProduct_MatchesIndividual(int size, int nPeak, int vPeak, int seed) {
        var rng = new Random(seed);
        var scans = CreateScanBatch(Enumerable.Repeat(0, size).Select(_ => rng.Next(-vPeak, vPeak) + nPeak).ToArray(), 1, rng.Next());
        var actuals = MsScanMatching.GetBatchSimpleDotProduct(scans, _mzTolerance, _mzMin, _mzMax);
        for (int i = 0; i < scans.Length; i++) {
            for (int j = 0; j < scans.Length; j++) {
                var expected = MsScanMatching.GetSimpleDotProduct(scans[i], scans[j], _mzTolerance, _mzMin, _mzMax);
                Assert.AreEqual(expected, actuals[i][j], .00001, $"The similarity between scan[{i}] and scan[{j}] is inconsistent.");
            }
        }
    }

    [DataTestMethod()]
    [DataRow(.0, 42)]
    [DataRow(.1, 42)]
    [DataRow(.5, 42)]
    [DataRow(.9, 42)]
    public void GetBatchSimpleDotProduct_MassBeginWorks(double massBegin, int seed) {
        var rng = new Random(seed);
        var scans = CreateScanBatch(Enumerable.Repeat(0, 5).Select(_ => rng.Next(15, 25)).ToArray(), 1, rng.Next());
        var actuals = MsScanMatching.GetBatchSimpleDotProduct(scans, _mzTolerance, massBegin, _mzMax);
        for (int i = 0; i < scans.Length; i++) {
            for (int j = 0; j < scans.Length; j++) {
                var expected = i == j ? 1d : MsScanMatching.GetSimpleDotProduct(scans[i], scans[j], _mzTolerance, massBegin, _mzMax);
                try {
                    Assert.AreEqual(expected, actuals[i][j], .00001, $"The similarity between scan[{i}] and scan[{j}] is inconsistent.");
                }
                catch (AssertFailedException ex) {
                    Console.WriteLine("scan[{0}]:", i);
                    scans[i].Spectrum.ShowForTest();
                    Console.WriteLine("scan[{0}]:", j);
                    scans[j].Spectrum.ShowForTest();
                    throw;
                }
            }
        }
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

        var merged = new List<SpectrumPeak>();
        for (int i = 0; i < peaks.Count;) {
            var k = i++;
            while (i < peaks.Count && peaks[i].Mass - peaks[k].Mass < _mzTolerance) {
                peaks[k].Intensity += peaks[i].Intensity;
                ++i;
            }
            merged.Add(peaks[k]);
        }

        return new MSScanProperty
        {
            Spectrum = merged
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
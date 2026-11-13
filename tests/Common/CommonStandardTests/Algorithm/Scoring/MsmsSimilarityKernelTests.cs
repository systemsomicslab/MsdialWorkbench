using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.Common.Algorithm.Scoring.Tests;

/// <summary>
/// The molecular-networking kernels on hand-computed spectra, and the guarantee that they never
/// write to the spectra they score.
/// </summary>
[TestClass]
public class MsmsSimilarityKernelTests
{
    private const double Tolerance = 0.025;

    internal static MSScanProperty Scan(double precursor, params (double Mz, double Intensity)[] peaks) {
        var scan = new MSScanProperty(0, precursor, new RetentionTime(1d), IonMode.Positive);
        foreach (var (mz, intensity) in peaks) {
            scan.AddPeak(mz, intensity);
        }
        return scan;
    }

    private static readonly Func<IMSScanProperty, IMSScanProperty, double[]>[] LegacyKernels = {
        (a, b) => MsScanMatching.GetModifiedDotProductScore(a, b, Tolerance),
        (a, b) => MsScanMatching.GetBonanzaScore(a, b, Tolerance),
        (a, b) => MsScanMatching.GetBonanzaModifiedDotCosineScores(a, b, Tolerance),
        (a, b) => MsScanMatching.GetCosineScore(a, b, Tolerance),
    };

    /// <summary>
    /// One shared fragment out of A's two. ModDot sums over the matched pair only, so it is 1.0; this
    /// documents the defect that makes it "almost always close to 1". Bonanza counts A's unmatched
    /// 90: 300 / (300 + 90²) = 300/8400.
    /// </summary>
    [TestMethod]
    public void ModDotIsOneForASingleMatchedPairWhileBonanzaCountsUnmatchedIntensity() {
        var a = Scan(300d, (100d, 10d), (200d, 90d));
        var b = Scan(300d, (100d, 30d));

        var moddot = MsScanMatching.GetModifiedDotProductScore(a, b, Tolerance);
        var bonanza = MsScanMatching.GetBonanzaScore(a, b, Tolerance);

        Assert.AreEqual(1d, moddot[0], 1e-15);
        Assert.AreEqual(1d, moddot[1]);
        Assert.AreEqual(300d / 8400d, bonanza[0], 1e-15);
        Assert.AreEqual(1d, bonanza[1]);
    }

    [TestMethod]
    public void TheLegacyMatcherTakesTheSmallestIntensityDifferenceAndThenTheFirstCandidate() {
        // r peak 100 (I 50) sees e peaks 100.01 (I 10), 99.99 (I 60) and 100.02 (I 40): |ΔI| 40, 10, 10.
        var e = new List<SpectrumPeak> { new SpectrumPeak(100.01, 10d), new SpectrumPeak(99.99, 60d), new SpectrumPeak(100.02, 40d), };
        var r = new List<SpectrumPeak> { new SpectrumPeak(100d, 50d), };

        var match = MsScanMatching.MatchLegacy(e, 300d, r, 300d, Tolerance, MassToleranceType.Da);

        Assert.AreEqual(1, match.Pairs.Count);
        Assert.AreEqual(0, match.Pairs[0].RIndex);
        Assert.AreEqual(1, match.Pairs[0].EIndex, "the first of the two equal differences");
        Assert.IsTrue(match.Pairs[0].IsProductIonMatched);
        CollectionAssert.AreEqual(new[] { false, true, false }, match.EUsed);
    }

    [TestMethod]
    public void TheLegacyMatcherPairsANeutralLossByThePrecursorDifference() {
        // e precursor 300, r precursor 314: e 150 shifted by +14 is r 164.
        var e = new List<SpectrumPeak> { new SpectrumPeak(150d, 10d), };
        var r = new List<SpectrumPeak> { new SpectrumPeak(164d, 10d), };

        var match = MsScanMatching.MatchLegacy(e, 300d, r, 314d, Tolerance, MassToleranceType.Da);

        Assert.AreEqual(1, match.Pairs.Count);
        Assert.IsFalse(match.Pairs[0].IsProductIonMatched);
    }

    [TestMethod]
    public void TheLegacyToleranceIsStrict() {
        // 0.5 and 100.5 - 100.0 are exact doubles: a pair exactly at the tolerance does not match.
        var a = Scan(300d, (100d, 10d));
        var b = Scan(300d, (100.5, 10d));

        Assert.AreEqual(0d, MsScanMatching.GetModifiedDotProductScore(a, b, 0.5)[1]);
        Assert.AreEqual(1d, MsScanMatching.GetModifiedDotProductScore(a, b, 0.5000001)[1]);
    }

    /// <summary>
    /// The kernels used to reset and set <see cref="SpectrumPeak.IsMatched"/> on both spectra.
    /// </summary>
    [TestMethod]
    public void NoKernelWritesToTheSpectraItScores() {
        var a = Scan(300d, (100d, 10d), (150d, 40d), (200d, 90d), (286d, 5d));
        var b = Scan(314d, (100d, 30d), (164d, 35d), (250d, 20d));
        var aPeaks = a.Spectrum.ToList();
        var bPeaks = b.Spectrum.ToList();
        var aValues = a.Spectrum.Select(p => (p.Mass, p.Intensity)).ToList();
        var bValues = b.Spectrum.Select(p => (p.Mass, p.Intensity)).ToList();
        var aList = a.Spectrum;
        var bList = b.Spectrum;

        foreach (var kernel in LegacyKernels) {
            kernel(a, b);
            kernel(b, a);

            Assert.AreSame(aList, a.Spectrum);
            Assert.AreSame(bList, b.Spectrum);
            CollectionAssert.AreEqual(aPeaks, a.Spectrum);
            CollectionAssert.AreEqual(bPeaks, b.Spectrum);
            CollectionAssert.AreEqual(aValues, a.Spectrum.Select(p => (p.Mass, p.Intensity)).ToList());
            CollectionAssert.AreEqual(bValues, b.Spectrum.Select(p => (p.Mass, p.Intensity)).ToList());
            Assert.IsTrue(a.Spectrum.Concat(b.Spectrum).All(p => !p.IsMatched));
        }
    }

    [TestMethod]
    public void SearchMatchedPeaksStillSetsTheFlagsForItsCallers() {
        var e = new List<SpectrumPeak> { new SpectrumPeak(100d, 10d), new SpectrumPeak(120d, 10d) { IsMatched = true, }, };
        var r = new List<SpectrumPeak> { new SpectrumPeak(100.01, 10d), new SpectrumPeak(180d, 10d) { IsMatched = true, }, };

        MsScanMatching.SearchMatchedPeaks(e, 300d, r, 300d, Tolerance, MassToleranceType.Da, out var matched);

        Assert.AreEqual(1, matched.Count);
        CollectionAssert.AreEqual(new[] { true, false }, e.Select(p => p.IsMatched).ToArray());
        CollectionAssert.AreEqual(new[] { true, false }, r.Select(p => p.IsMatched).ToArray());
    }

    /// <summary>
    /// The console's folder mode scores one file's records against another's under Parallel.For, so
    /// one spectrum is scored by many threads at once. With the flags on the shared peaks, Bonanza
    /// read another thread's matching.
    /// </summary>
    [TestMethod]
    public void ASpectrumScoredFromManyThreadsAtOnceGivesTheSequentialResults() {
        var spectra = LegacyMnKernelGoldenTests.CreateProcessedSpectra().Where(s => s.Spectrum.Count > 0).ToList();
        var shared = spectra.OrderByDescending(s => s.Spectrum.Count).First();
        var others = Enumerable.Range(0, 5000).Select(i => spectra[i % spectra.Count]).ToList();

        foreach (var kernel in LegacyKernels) {
            var sequential = others.Select(other => kernel(shared, other)).ToList();
            for (int repeat = 0; repeat < 20; repeat++) {
                var parallel = new double[others.Count][];
                Parallel.For(0, others.Count, new ParallelOptions { MaxDegreeOfParallelism = 16, }, i => parallel[i] = kernel(shared, others[i]));
                for (int i = 0; i < others.Count; i++) {
                    CollectionAssert.AreEqual(sequential[i], parallel[i], $"pair {i}, repeat {repeat}");
                }
            }
        }
    }
}

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
        (a, b) => { var s = ModifiedCosine(a, b); return new[] { s.Score, s.Matched }; },
    };

    // The raw kernel, no preprocessing.
    private static SimilarityScore ModifiedCosine(IMSScanProperty a, IMSScanProperty b, double tolerance = Tolerance) {
        return ModifiedCosineKernel.Compute(a.Spectrum, a.PrecursorMz, b.Spectrum, b.PrecursorMz, tolerance, mzPower: 0d, intensityPower: 1d);
    }

    /// <summary>
    /// The same pair as above. The modified cosine divides by the full norms:
    /// 10·30 / (√(10² + 90²) · 30) = 10/√8200.
    /// </summary>
    [TestMethod]
    public void ModifiedCosineDividesByTheFullSpectrumNorms() {
        var a = Scan(300d, (100d, 10d), (200d, 90d));
        var b = Scan(300d, (100d, 30d));

        var score = ModifiedCosine(a, b);

        Assert.AreEqual(10d / Math.Sqrt(8200d), score.Score, 1e-15);
        Assert.AreEqual(0.110431526, score.Score, 1e-9);
        Assert.AreEqual(1, score.Matched);
    }

    /// <summary>
    /// 250 in A and 264 in B are one precursor shift (−14) apart: with the shifted pair the score is
    /// (100·100 + 50·50) / (√12500 · √12500) = 1 with two matches; without it (equal precursors, so no
    /// shift) it is 10000/12500 = 0.8.
    /// </summary>
    [TestMethod]
    public void ModifiedCosinePairsPeaksOnePrecursorShiftApart() {
        var a = Scan(300d, (100d, 100d), (250d, 50d));
        var b = Scan(314d, (100d, 100d), (264d, 50d));

        var shifted = ModifiedCosine(a, b);
        var direct = ModifiedCosineKernel.Compute(a.Spectrum, 300d, b.Spectrum, 300d, Tolerance, 0d, 1d);

        Assert.AreEqual(1d, shifted.Score, 1e-15);
        Assert.AreEqual(2, shifted.Matched);
        Assert.AreEqual(0.8, direct.Score, 1e-15);
        Assert.AreEqual(1, direct.Matched);
    }

    /// <summary>
    /// Peaks at 299 and 319 sit 1 Da under the precursors 300 and 320, so they are one precursor shift
    /// apart. The legacy matcher pairs them; the modified cosine's 17 Da precursor window removes them
    /// first, and a window of 0 keeps them.
    /// </summary>
    [TestMethod]
    public void ThePrecursorWindowRemovesPeaksNearThePrecursorBeforeScoring() {
        var a = Scan(300d, (100d, 50d), (299d, 100d));
        var b = Scan(320d, (100d, 50d), (319d, 100d));

        Assert.AreEqual(2d, MsScanMatching.GetModifiedDotProductScore(a, b, Tolerance)[1], "the legacy matcher pairs the near-precursor peaks");

        foreach (var (window, matched, remaining) in new[] { (17d, 1, 1), (0d, 2, 2) }) {
            var kernel = new ModifiedCosineKernel(Tolerance, ModifiedCosineKernel.CreatePreprocessing(
                relativeAbundanceCutOff: 0d, absoluteAbundanceCutOff: 0d, tolerance: Tolerance, precursorWindow: window, intensityPower: 1d));
            var pa = Scan(300d, kernel.Preprocess(a.Spectrum, 300d).Select(p => (p.Mass, p.Intensity)).ToArray());
            var pb = Scan(320d, kernel.Preprocess(b.Spectrum, 320d).Select(p => (p.Mass, p.Intensity)).ToArray());

            var score = kernel.Score(pa, pb);

            Assert.AreEqual(remaining, pa.Spectrum.Count, $"window {window}");
            Assert.AreEqual(matched, score.Matched, $"window {window}");
            Assert.AreEqual(1d, score.Score, 1e-15, $"window {window}");
        }
    }

    [TestMethod]
    public void ThePrecursorWindowIsInclusive() {
        // |283 − 300| = 17 exactly; 282.5 is outside.
        var kernel = new ModifiedCosineKernel(Tolerance, ModifiedCosineKernel.CreatePreprocessing(0d, 0d, Tolerance, 17d, 1d));

        var peaks = kernel.Preprocess(new List<SpectrumPeak> { new SpectrumPeak(282.5, 10d), new SpectrumPeak(283d, 10d), }, 300d);

        CollectionAssert.AreEqual(new[] { 282.5 }, peaks.Select(p => p.Mass).ToArray());
    }

    [TestMethod]
    public void TheModifiedCosineGreedyMatchTakesTheLargerProductFirst() {
        // A 100 lies within the tolerance of B 99.99 (w = 50) and B 100.01 (w = 80): 80 is accepted.
        var a = Scan(300d, (100d, 10d));
        var b = Scan(300d, (99.99, 5d), (100.01, 8d));

        var score = ModifiedCosine(a, b);

        Assert.AreEqual(1, score.Matched);
        Assert.AreEqual(80d / (10d * Math.Sqrt(89d)), score.Score, 1e-15);
    }

    /// <summary>
    /// A 100 (I 10) has a direct candidate B 100 and a shifted candidate B 114 − 14 = 100, both w = 40.
    /// The list is direct then shifted; a stable ascending sort followed by a reversal puts the shifted
    /// (later) one first, so A 100 takes B 114. That leaves B 100 for A 86 through the shift (w = 4):
    /// two matches, (40 + 4) / (√101 · √32) = 0.773958…  Taking the direct candidate first would give
    /// one match and 40 / (√101 · √32).
    /// </summary>
    [TestMethod]
    public void OnEqualWeightsTheLaterCandidateIsAccepted() {
        var a = Scan(300d, (86d, 1d), (100d, 10d));
        var b = Scan(314d, (100d, 4d), (114d, 4d));

        var score = ModifiedCosine(a, b);

        Assert.AreEqual(2, score.Matched);
        Assert.AreEqual(44d / (Math.Sqrt(101d) * Math.Sqrt(32d)), score.Score, 1e-15);
        Assert.AreEqual(0.773958, score.Score, 1e-6);
    }

    /// <summary>
    /// 0.5 and 100.5 are exact doubles: a pair exactly at the tolerance is not a legacy match (strict)
    /// and is a modified-cosine match (inclusive).
    /// </summary>
    [TestMethod]
    public void AtExactlyTheToleranceLegacyDoesNotMatchAndModifiedCosineDoes() {
        var a = Scan(300d, (100d, 10d));
        var b = Scan(300d, (100.5, 10d));

        Assert.AreEqual(0d, MsScanMatching.GetModifiedDotProductScore(a, b, 0.5)[1]);
        var score = ModifiedCosine(a, b, tolerance: 0.5);
        Assert.AreEqual(1, score.Matched);
        Assert.AreEqual(1d, score.Score, 1e-15);
    }

    [TestMethod]
    public void AShiftWithinTheToleranceGivesNoShiftedCandidates() {
        // Precursors 0.02 apart: only direct pairs, so A 200 does not pair with B 200.02 twice over.
        var a = Scan(300d, (100d, 10d), (200d, 10d));
        var b = Scan(300.02, (100d, 10d), (200.01, 10d));

        var score = ModifiedCosine(a, b);

        Assert.AreEqual(2, score.Matched);
        Assert.AreEqual(1d, score.Score, 1e-15);
    }

    [TestMethod]
    public void AnEmptySpectrumScoresZero() {
        var a = Scan(300d);
        var b = Scan(300d, (100d, 10d));

        Assert.AreEqual(0d, ModifiedCosine(a, b).Score);
        Assert.AreEqual(0, ModifiedCosine(a, b).Matched);
    }

    [TestMethod]
    public void ModifiedCosineIsSymmetricOnTieFreeSpectra() {
        var random = new Random(4);
        for (int k = 0; k < 500; k++) {
            var a = RandomScan(random);
            var b = RandomScan(random);
            var ab = ModifiedCosine(a, b);
            var ba = ModifiedCosine(b, a);
            Assert.AreEqual(ab.Score, ba.Score, $"pair {k}");
            Assert.AreEqual(ab.Matched, ba.Matched, $"pair {k}");
        }
    }

    private static MSScanProperty RandomScan(Random random) {
        var precursor = Math.Round(300d + random.NextDouble() * 50d, 4);
        var peaks = Enumerable.Range(0, 1 + random.Next(15))
            .Select(_ => (Math.Round(50d + random.Next(250) + random.NextDouble() * 0.04, 4), 1d + random.NextDouble() * 1000d))
            .ToArray();
        return Scan(precursor, peaks);
    }

    [TestMethod]
    public void TheModifiedCosinePreprocessingFollowsTheSharedOrder() {
        // Base 1000 over all peaks (also the one above the precursor), rel 1% → 10; abs 5; above = tol.
        var kernel = new ModifiedCosineKernel(Tolerance, ModifiedCosineKernel.CreatePreprocessing(
            relativeAbundanceCutOff: 1d, absoluteAbundanceCutOff: 5d, tolerance: Tolerance, precursorWindow: 17d, intensityPower: 0.5));
        var peaks = new List<SpectrumPeak> {
            new SpectrumPeak(150d, 400d), new SpectrumPeak(100d, 100d), new SpectrumPeak(120d, 9d),
            new SpectrumPeak(130d, 4d), new SpectrumPeak(290d, 50d), new SpectrumPeak(300.02, 50d), new SpectrumPeak(300.03, 1000d),
        };

        var processed = kernel.Preprocess(peaks, 300d);

        // Kept in input order, no framing; I^0.5 scaled so the base of the kept peaks (400) is 100.
        CollectionAssert.AreEqual(new[] { 150d, 100d }, processed.Select(p => p.Mass).ToArray());
        Assert.AreEqual(100d, processed[0].Intensity, 1e-12);
        Assert.AreEqual(50d, processed[1].Intensity, 1e-12);
        Assert.AreEqual(1000d, peaks[6].Intensity, "the input is untouched");
    }

    /// <summary>The legacy kernels' preprocessing is GetProcessedSpectrum as msn calls it, to the bit.</summary>
    [TestMethod]
    public void TheLegacyKernelPreprocessingIsGetProcessedSpectrumBitForBit() {
        var kernel = new BonanzaKernel(Tolerance, MnPreprocessing.Legacy(relativeAbundanceCutOff: 1d, absoluteAbundanceCutOff: 0d));
        foreach (var scan in LegacyMnKernelGoldenTests.CreateRawSpectra()) {
            var expected = MsScanMatching.GetProcessedSpectrum(scan.Spectrum, scan.PrecursorMz, absoluteAbundanceCutOff: 0d, relativeAbundanceCutOff: 1d);
            var actual = kernel.Preprocess(scan.Spectrum, scan.PrecursorMz);
            CollectionAssert.AreEqual(
                expected.Select(p => (BitConverter.DoubleToInt64Bits(p.Mass), BitConverter.DoubleToInt64Bits(p.Intensity))).ToList(),
                actual.Select(p => (BitConverter.DoubleToInt64Bits(p.Mass), BitConverter.DoubleToInt64Bits(p.Intensity))).ToList());
        }
    }

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
    public void NoKernelPreprocessingWritesToItsInput() {
        var query = new Function.MolecularNetworkingQuery { MassTolerance = Tolerance, RelativeAbundanceCutOff = 1d, };
        foreach (var calc in new[] { MsmsSimilarityCalc.ModDotLegacy, MsmsSimilarityCalc.Bonanza, MsmsSimilarityCalc.Cosine, MsmsSimilarityCalc.ModifiedCosine }) {
            var a = Scan(300d, (150d, 40d), (100d, 10d), (200d, 90d), (299d, 5d));
            var before = a.Spectrum.Select(p => (p.Mass, p.Intensity)).ToList();
            var peaks = a.Spectrum.ToList();

            var processed = MsmsSimilarityKernelFactory.Create(calc, query).Preprocess(a.Spectrum, a.PrecursorMz);

            CollectionAssert.AreEqual(peaks, a.Spectrum, calc.ToString());
            CollectionAssert.AreEqual(before, a.Spectrum.Select(p => (p.Mass, p.Intensity)).ToList(), calc.ToString());
            Assert.IsTrue(processed.All(p => !a.Spectrum.Contains(p)), calc.ToString());
        }
    }

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

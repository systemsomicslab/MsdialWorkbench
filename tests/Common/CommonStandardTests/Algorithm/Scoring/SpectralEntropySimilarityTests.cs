using CompMs.Common.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Algorithm.Scoring.Tests;

// Pins the behaviour of the spectral entropy similarity before it is optimised. The score is
// stored as a float on MsScanMatchResult, so agreement to 1e-9 is far tighter than anything a
// reordered summation could disturb, and far looser than any real change of meaning.
[TestClass()]
public class SpectralEntropySimilarityTests
{
    private const double Bin = .05;
    private const double Tolerance = 1e-9;

    [TestMethod()]
    public void GetSpectralEntropy_SinglePeak_IsZero() {
        Assert.AreEqual(0d, MsScanMatching.GetSpectralEntropy([new(100d, 42d)]), Tolerance);
    }

    [TestMethod()]
    [DataRow(2)]
    [DataRow(8)]
    [DataRow(100)]
    public void GetSpectralEntropy_UniformPeaks_IsLog2OfCount(int count) {
        var peaks = Enumerable.Range(0, count).Select(i => new SpectrumPeak(100d + i, 7d)).ToList();
        Assert.AreEqual(Math.Log(count, 2), MsScanMatching.GetSpectralEntropy(peaks), Tolerance);
    }

    [TestMethod()]
    public void GetSpectralEntropy_IsScaleInvariant() {
        var peaks = RandomSpectrum(new Random(1), 30);
        var scaled = peaks.Select(p => new SpectrumPeak(p.Mass, p.Intensity * 1234.5)).ToList();
        Assert.AreEqual(MsScanMatching.GetSpectralEntropy(peaks), MsScanMatching.GetSpectralEntropy(scaled), Tolerance);
    }

    [TestMethod()]
    public void Similarity_SelfComparison_IsOne() {
        var peaks = RandomSpectrum(new Random(2), 50);
        Assert.AreEqual(1d, MsScanMatching.GetSpectralEntropySimilarity(peaks, peaks, Bin), Tolerance);
    }

    [TestMethod()]
    public void Similarity_DisjointBins_IsZero() {
        List<SpectrumPeak> peaks1 = [new(100d, 3d), new(150d, 1d), new(200d, 5d)];
        List<SpectrumPeak> peaks2 = [new(120d, 2d), new(170d, 8d)];
        Assert.AreEqual(0d, MsScanMatching.GetSpectralEntropySimilarity(peaks1, peaks2, Bin), Tolerance);
    }

    [TestMethod()]
    public void Similarity_HandComputedExample() {
        // p1 = (1/2, 1/2): H1 = 1. p2 = (1): H2 = 0. Combined (3/4, 1/4): H12 = 0.8112781244591328.
        List<SpectrumPeak> peaks1 = [new(100d, 1d), new(200d, 1d)];
        List<SpectrumPeak> peaks2 = [new(100d, 1d)];
        var h12 = -(.75 * Math.Log(.75, 2) + .25 * Math.Log(.25, 2));
        var expected = 1 - (2 * h12 - 1 - 0) * .5;
        Assert.AreEqual(expected, MsScanMatching.GetSpectralEntropySimilarity(peaks1, peaks2, Bin), Tolerance);
    }

    [TestMethod()]
    public void Similarity_PeaksInSameBin_AreMergedEvenWhenMassesDiffer() {
        // 100.01, 100.02 and 100.03 all fall in frame (int)(m / 0.05) = 2000.
        List<SpectrumPeak> peaks1 = [new(100.01, 1d), new(100.02, 1d)];
        List<SpectrumPeak> peaks2 = [new(100.03, 2d)];
        Assert.AreEqual(1d, MsScanMatching.GetSpectralEntropySimilarity(peaks1, peaks2, Bin), Tolerance);
    }

    [TestMethod()]
    public void Similarity_UnsortedPeaksInSameBin_AreMerged() {
        // The two peaks of frame 2000 are not adjacent in the list.
        List<SpectrumPeak> peaks1 = [new(100.01, 1d), new(200d, 2d), new(100.02, 1d)];
        List<SpectrumPeak> peaks2 = [new(200d, 1d), new(100.03, 1d)];
        Assert.AreEqual(1d, MsScanMatching.GetSpectralEntropySimilarity(peaks1, peaks2, Bin), Tolerance);
    }

    [TestMethod()]
    public void Similarity_PeaksAcrossBinBoundary_AreNotMergedEvenWhenWithinTolerance() {
        // 100.04 -> frame 2000, 100.06 -> frame 2001. Only 0.02 apart, less than the 0.05 "tolerance",
        // but binning is by fixed frames, not by distance. This is the current behaviour.
        List<SpectrumPeak> peaks1 = [new(100.04, 1d)];
        List<SpectrumPeak> peaks2 = [new(100.06, 1d)];
        Assert.AreEqual(0d, MsScanMatching.GetSpectralEntropySimilarity(peaks1, peaks2, Bin), Tolerance);
    }

    [TestMethod()]
    public void Similarity_IsSymmetric() {
        var random = new Random(3);
        for (int i = 0; i < 50; i++) {
            var (peaks1, peaks2) = RandomPair(random);
            Assert.AreEqual(
                MsScanMatching.GetSpectralEntropySimilarity(peaks1, peaks2, Bin),
                MsScanMatching.GetSpectralEntropySimilarity(peaks2, peaks1, Bin),
                Tolerance);
        }
    }

    [TestMethod()]
    public void Similarity_IsIndependentOfPeakOrder() {
        var random = new Random(4);
        for (int i = 0; i < 50; i++) {
            var (peaks1, peaks2) = RandomPair(random);
            var shuffled1 = peaks1.OrderBy(_ => random.Next()).ToList();
            var shuffled2 = peaks2.OrderBy(_ => random.Next()).ToList();
            Assert.AreEqual(
                MsScanMatching.GetSpectralEntropySimilarity(peaks1, peaks2, Bin),
                MsScanMatching.GetSpectralEntropySimilarity(shuffled1, shuffled2, Bin),
                Tolerance);
        }
    }

    [TestMethod()]
    public void Similarity_IsIndependentOfIntensityScale() {
        var random = new Random(5);
        for (int i = 0; i < 50; i++) {
            var (peaks1, peaks2) = RandomPair(random);
            var scaled2 = peaks2.Select(p => new SpectrumPeak(p.Mass, p.Intensity * 999d)).ToList();
            Assert.AreEqual(
                MsScanMatching.GetSpectralEntropySimilarity(peaks1, peaks2, Bin),
                MsScanMatching.GetSpectralEntropySimilarity(peaks1, scaled2, Bin),
                Tolerance);
        }
    }

    [TestMethod()]
    public void Similarity_IsBetweenZeroAndOne() {
        var random = new Random(6);
        for (int i = 0; i < 200; i++) {
            var (peaks1, peaks2) = RandomPair(random);
            var actual = MsScanMatching.GetSpectralEntropySimilarity(peaks1, peaks2, Bin);
            Assert.IsTrue(actual >= -Tolerance && actual <= 1 + Tolerance, $"Similarity {actual} is out of [0, 1].");
        }
    }

    [TestMethod()]
    [DataRow(.01)]
    [DataRow(.05)]
    [DataRow(.5)]
    public void Similarity_MatchesReferenceImplementation(double bin) {
        var random = new Random(7);
        for (int i = 0; i < 500; i++) {
            var (peaks1, peaks2) = RandomPair(random);
            Assert.AreEqual(
                ReferenceSimilarity(peaks1, peaks2, bin),
                MsScanMatching.GetSpectralEntropySimilarity(peaks1, peaks2, bin),
                Tolerance,
                $"Pair #{i} with bin {bin}");
        }
    }

    [TestMethod()]
    public void Similarity_DoesNotModifyInputs() {
        var (peaks1, peaks2) = RandomPair(new Random(8));
        var before1 = peaks1.Select(p => (p.Mass, p.Intensity)).ToArray();
        var before2 = peaks2.Select(p => (p.Mass, p.Intensity)).ToArray();

        MsScanMatching.GetSpectralEntropySimilarity(peaks1, peaks2, Bin);

        CollectionAssert.AreEqual(before1, peaks1.Select(p => (p.Mass, p.Intensity)).ToArray());
        CollectionAssert.AreEqual(before2, peaks2.Select(p => (p.Mass, p.Intensity)).ToArray());
    }

    [TestMethod()]
    public void Similarity_NullOrEmpty_ReturnsNotComputed() {
        List<SpectrumPeak> peaks = [new(100d, 1d)];
        Assert.AreEqual(-1d, MsScanMatching.GetSpectralEntropySimilarity(null, peaks, Bin));
        Assert.AreEqual(-1d, MsScanMatching.GetSpectralEntropySimilarity(peaks, null, Bin));
        Assert.AreEqual(-1d, MsScanMatching.GetSpectralEntropySimilarity([], peaks, Bin));
        Assert.AreEqual(-1d, MsScanMatching.GetSpectralEntropySimilarity(peaks, [], Bin));
    }

    [TestMethod()]
    public void GetSpectralEntropy_ZeroIntensityPeak_ContributesNothing() {
        // 0 log 0 is taken as 0, its limit, rather than the NaN that 0 * log2(0) evaluates to.
        Assert.AreEqual(1d, MsScanMatching.GetSpectralEntropy([new(100d, 1d), new(200d, 1d), new(300d, 0d)]), Tolerance);
    }

    [TestMethod()]
    public void GetSpectralEntropy_NegativeIntensity_IsCountedAsZero() {
        Assert.AreEqual(1d, MsScanMatching.GetSpectralEntropy([new(100d, 1d), new(200d, 1d), new(300d, -5d)]), Tolerance);
    }

    [TestMethod()]
    public void Similarity_FrameWithNegativeSum_IsCountedAsZero() {
        // Frame 6000 holds only a negative peak.
        List<SpectrumPeak> withNegative = [new(100.01, 3d), new(200d, 1d), new(300d, -4d)];
        List<SpectrumPeak> withZero = [new(100.01, 3d), new(200d, 1d), new(300d, 0d)];
        List<SpectrumPeak> other = [new(100.03, 1d), new(300d, 2d)];
        Assert.AreEqual(
            MsScanMatching.GetSpectralEntropySimilarity(withZero, other, Bin),
            MsScanMatching.GetSpectralEntropySimilarity(withNegative, other, Bin),
            Tolerance);
    }

    [TestMethod()]
    public void Similarity_NegativePeak_IsSummedWithItsFrameBeforeTheFrameIsCounted() {
        // Negative intensities are invalid input. They are clamped per frame, after summing, because
        // that is the cheaper place: frame 2000 holds 3 + (-2) = 1.
        List<SpectrumPeak> withNegative = [new(100.01, 3d), new(100.02, -2d), new(200d, 1d)];
        List<SpectrumPeak> summed = [new(100.01, 1d), new(200d, 1d)];
        List<SpectrumPeak> other = [new(100.03, 1d), new(300d, 2d)];
        Assert.AreEqual(
            MsScanMatching.GetSpectralEntropySimilarity(summed, other, Bin),
            MsScanMatching.GetSpectralEntropySimilarity(withNegative, other, Bin),
            Tolerance);
    }

    [TestMethod()]
    public void Similarity_BinHoldingOnlyZeroIntensity_IsIgnored() {
        List<SpectrumPeak> peaks1 = [new(100d, 1d), new(200d, 0d)];
        List<SpectrumPeak> peaks2 = [new(100d, 1d)];
        Assert.AreEqual(1d, MsScanMatching.GetSpectralEntropySimilarity(peaks1, peaks2, Bin), Tolerance);
    }

    // An independent, deliberately plain restatement of the score: bin each spectrum by frame
    // (int)(mass / bin), normalise each to unit total, mix them half and half, and compare entropies.
    private static double ReferenceSimilarity(List<SpectrumPeak> peaks1, List<SpectrumPeak> peaks2, double bin) {
        var p1 = Normalize(BinSums(peaks1, bin));
        var p2 = Normalize(BinSums(peaks2, bin));
        var mixed = new Dictionary<int, double>();
        foreach (var kv in p1.Concat(p2)) {
            mixed.TryGetValue(kv.Key, out var v);
            mixed[kv.Key] = v + kv.Value * .5;
        }
        return 1 - (2 * Entropy(mixed.Values) - Entropy(p1.Values) - Entropy(p2.Values)) * .5;
    }

    private static Dictionary<int, double> BinSums(List<SpectrumPeak> peaks, double bin) {
        var sums = new Dictionary<int, double>();
        foreach (var peak in peaks) {
            var frame = (int)(peak.Mass / bin);
            sums.TryGetValue(frame, out var v);
            sums[frame] = v + peak.Intensity;
        }
        return sums;
    }

    private static Dictionary<int, double> Normalize(Dictionary<int, double> sums) {
        var total = sums.Values.Sum();
        return sums.ToDictionary(kv => kv.Key, kv => kv.Value / total);
    }

    private static double Entropy(IEnumerable<double> probabilities) {
        return -probabilities.Where(p => p > 0).Sum(p => p * Math.Log(p, 2));
    }

    // A query and a reference that share about half their fragments, with m/z jitter smaller than
    // the bin so that some shared fragments straddle a frame boundary and some do not.
    private static (List<SpectrumPeak>, List<SpectrumPeak>) RandomPair(Random random) {
        var peaks1 = RandomSpectrum(random, random.Next(1, 80));
        var peaks2 = new List<SpectrumPeak>();
        foreach (var peak in peaks1) {
            if (random.NextDouble() < .5) {
                peaks2.Add(new SpectrumPeak(peak.Mass + (random.NextDouble() - .5) * .02, RandomIntensity(random)));
            }
        }
        peaks2.AddRange(RandomSpectrum(random, random.Next(0, 40)));
        if (peaks2.Count == 0) {
            peaks2.Add(new SpectrumPeak(peaks1[0].Mass, 1d));
        }
        return (peaks1, peaks2.OrderBy(p => p.Mass).ToList());
    }

    private static List<SpectrumPeak> RandomSpectrum(Random random, int count) {
        return Enumerable.Range(0, count)
            .Select(_ => new SpectrumPeak(50d + random.NextDouble() * 950d, RandomIntensity(random)))
            .OrderBy(p => p.Mass)
            .ToList();
    }

    // Spans several orders of magnitude, as fragment intensities do.
    private static double RandomIntensity(Random random) => Math.Pow(10, 1 + random.NextDouble() * 5);
}

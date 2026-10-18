using CompMs.Common.Components;
using CompMs.Common.Extension;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Algorithm.Function.Tests;

// Pins SpectrumHandler.GetBinnedSpectrum(spectrum, bin) against the Dictionary-based implementation
// it had before it shared its binning with the spectral entropy similarity.
[TestClass()]
public class SpectrumHandlerBinnedSpectrumTests
{
    [TestMethod()]
    public void GetBinnedSpectrum_SumsEachFrameAtTheMassOfItsMostIntensePeak() {
        List<SpectrumPeak> spectrum = [new(100.01, 1d), new(100.02, 3d), new(100.03, 2d), new(200d, 5d)];

        var actual = SpectrumHandler.GetBinnedSpectrum(spectrum, .05);

        CollectionAssert.AreEqual(new[] { (100.02, 6d), (200d, 5d) }, actual.Select(p => (p.Mass, p.Intensity)).ToArray());
    }

    [TestMethod()]
    public void GetBinnedSpectrum_TiedIntensity_KeepsTheFirstPeak() {
        List<SpectrumPeak> spectrum = [new(100.01, 2d), new(100.02, 2d)];

        var actual = SpectrumHandler.GetBinnedSpectrum(spectrum, .05);

        Assert.AreEqual(100.01, actual.Single().Mass);
    }

    [TestMethod()]
    public void GetBinnedSpectrum_EmptySpectrum_IsEmpty() {
        Assert.AreEqual(0, SpectrumHandler.GetBinnedSpectrum([], .05).Count);
    }

    [TestMethod()]
    [DataRow(.01)]
    [DataRow(.05)]
    [DataRow(1d)]
    public void GetBinnedSpectrum_SortedSpectrum_MatchesLegacyImplementation(double bin) {
        var random = new Random(11);
        for (int i = 0; i < 300; i++) {
            var spectrum = RandomSpectrum(random).OrderBy(p => p.Mass).ToList();
            CollectionAssert.AreEqual(
                Snapshot(LegacyGetBinnedSpectrum(spectrum, bin)),
                Snapshot(SpectrumHandler.GetBinnedSpectrum(spectrum, bin)),
                $"Spectrum #{i}");
        }
    }

    [TestMethod()]
    public void GetBinnedSpectrum_UnsortedSpectrum_HasTheLegacyBinsInMassOrder() {
        // The legacy order followed the first appearance of each frame; bins now come in m/z order.
        var random = new Random(12);
        for (int i = 0; i < 300; i++) {
            var spectrum = RandomSpectrum(random);
            CollectionAssert.AreEqual(
                Snapshot(LegacyGetBinnedSpectrum(spectrum.OrderBy(p => p.Mass).ToList(), .05)),
                Snapshot(SpectrumHandler.GetBinnedSpectrum(spectrum, .05)),
                $"Spectrum #{i}");
        }
    }

    [TestMethod()]
    public void GetBinnedSpectrum_DoesNotModifyInput() {
        var spectrum = RandomSpectrum(new Random(13));
        var before = Snapshot(spectrum);

        SpectrumHandler.GetBinnedSpectrum(spectrum, .05);

        CollectionAssert.AreEqual(before, Snapshot(spectrum));
    }

    private static (double, double)[] Snapshot(List<SpectrumPeak> peaks) => peaks.Select(p => (p.Mass, p.Intensity)).ToArray();

    // Peaks crowded into a narrow m/z range so that frames hold several peaks.
    private static List<SpectrumPeak> RandomSpectrum(Random random) {
        return Enumerable.Range(0, random.Next(0, 60))
            .Select(_ => new SpectrumPeak(100d + random.NextDouble() * 5d, Math.Pow(10, 1 + random.NextDouble() * 5)))
            .ToList();
    }

    // SpectrumHandler.GetBinnedSpectrum(spectrum, bin) as it was, kept verbatim.
    private static List<SpectrumPeak> LegacyGetBinnedSpectrum(List<SpectrumPeak> spectrum, double bin) {
        var peaks = new List<SpectrumPeak>();
        var range2Peaks = new Dictionary<int, List<SpectrumPeak>>();

        foreach (var peak in spectrum) {
            var mass = peak.Mass;
            var massframe = (int)(mass / bin);
            if (range2Peaks.ContainsKey(massframe))
                range2Peaks[massframe].Add(peak);
            else
                range2Peaks[massframe] = new List<SpectrumPeak>() { peak };
        }

        foreach (var pair in range2Peaks) {
            var maxMass = pair.Value.Argmax(n => n.Intensity).Mass;
            var sumIntensity = pair.Value.Sum(n => n.Intensity);
            peaks.Add(new SpectrumPeak(maxMass, sumIntensity));
        }
        return peaks;
    }
}

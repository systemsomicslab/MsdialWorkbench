using CompMs.Common.Algorithm.Function;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Extension;
using CompMs.Common.Lipidomics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Algorithm.Scoring.Tests;

// Compares GetSpectralEntropySimilarity with the implementation it replaced, on the spectra that
// the EAD, OAD and EID lipid generators produce, which are the references it is scored against in
// lipid annotation. Isomers of one species are included, so that high similarities are exercised
// as well as unrelated pairs.
[TestClass()]
public class SpectralEntropySimilarityGeneratedSpectraTests
{
    private const double Tolerance = 1e-9;

    private static readonly string[] LipidNames = [
        "PC 16:0/18:1(9Z)", "PC 18:1(9Z)/16:0", "PC 16:1(9Z)/18:0", "PC 16:0_18:1", "PC 34:1",
        "PC 18:0/20:4(5Z,8Z,11Z,14Z)", "PC 18:0_20:4",
        "PE 16:0/18:1(9Z)", "PE 18:0/20:4(5Z,8Z,11Z,14Z)", "PE 18:0_22:6",
        "PG 16:0/18:1(9Z)", "PI 18:0/20:4(5Z,8Z,11Z,14Z)", "PS 18:0/18:1(9Z)", "PA 16:0/18:1(9Z)",
        "LPC 16:0", "LPC 18:1(9Z)", "LPE 18:0", "LPG 16:0", "LPI 18:0", "LPS 18:1(9Z)",
        "MG 18:1(9Z)", "DG 16:0/18:1(9Z)", "DG 16:0_18:1",
        "TG 16:0/18:1(9Z)/18:2(9Z,12Z)", "TG 16:0_18:1_18:2", "TG 52:3",
        "SM 18:1(4E);2O/16:0", "SM 18:1;2O/24:1", "Cer 18:1(4E);2O/24:0", "Cer 18:1;2O/16:0",
        "HexCer 18:1;2O/16:0", "CAR 16:0", "CE 18:1(9Z)",
        "PC O-16:0/18:1(9Z)", "PE P-18:0/20:4(5Z,8Z,11Z,14Z)",
    ];

    private static readonly string[] AdductNames = [
        "[M+H]+", "[M+Na]+", "[M+NH4]+", "[M]+", "[M+H-H2O]+", "[M-H]-", "[M+HCOO]-", "[M+CH3COO]-",
    ];

    [TestMethod()]
    [DataRow(.01)]
    [DataRow(.05)]
    [DataRow(.5)]
    public void Similarity_MatchesLegacyImplementationOnGeneratedLipidSpectra(double bin) {
        var spectra = GenerateSpectra();
        Assert.IsTrue(spectra.Count >= 50, $"Only {spectra.Count} spectra were generated; the comparison would prove little.");

        var compared = 0;
        var legacyNaN = 0;
        var maxDifference = 0d;
        for (int i = 0; i < spectra.Count; i++) {
            for (int j = i; j < spectra.Count; j++) {
                // The EAD and EID plasmalogen PE spectra carry a peak of intensity 0 ("P-18:0 C3+H"),
                // which made the legacy score NaN for any partner. 0 log 0 is now taken as 0, which
                // is the same as leaving such peaks out, so that is what the legacy code is given.
                var expected = LegacySimilarity(Positive(spectra[i].Peaks), Positive(spectra[j].Peaks), bin);
                var actual = MsScanMatching.GetSpectralEntropySimilarity(spectra[i].Peaks, spectra[j].Peaks, bin);
                Assert.AreEqual(expected, actual, Tolerance, $"{spectra[i].Label} vs {spectra[j].Label}");
                if (double.IsNaN(LegacySimilarity(spectra[i].Peaks, spectra[j].Peaks, bin))) {
                    legacyNaN++;
                }
                maxDifference = Math.Max(maxDifference, Math.Abs(expected - actual));
                compared++;
            }
        }
        Console.WriteLine($"{spectra.Count} spectra, {compared} pairs, bin {bin}: max |legacy - new| = {maxDifference:E2}, legacy NaN on {legacyNaN} pairs");
    }

    private static List<SpectrumPeak> Positive(List<SpectrumPeak> peaks) => peaks.Where(p => p.Intensity > 0).ToList();

    private static List<(string Label, List<SpectrumPeak> Peaks)> GenerateSpectra() {
        var generators = new (string Name, ILipidSpectrumGenerator Generator)[] {
            ("EAD", FacadeLipidSpectrumGenerator.Default),
            ("OAD", FacadeLipidSpectrumGenerator.OadLipidGenerator),
            ("EID", FacadeLipidSpectrumGenerator.EidLipidGenerator),
        };
        var spectra = new List<(string, List<SpectrumPeak>)>();
        foreach (var name in LipidNames) {
            var lipid = FacadeLipidParser.Default.Parse(name);
            if (lipid is null) {
                continue;
            }
            foreach (var adductName in AdductNames) {
                var adduct = AdductIon.GetAdductIon(adductName);
                foreach (var (generatorName, generator) in generators) {
                    if (!generator.CanGenerate(lipid, adduct)) {
                        continue;
                    }
                    if (lipid.GenerateSpectrum(generator, adduct)?.Spectrum is { Count: > 0 } peaks) {
                        spectra.Add(($"{generatorName} {name} {adductName}", peaks));
                    }
                }
            }
        }
        return spectra;
    }

    // GetSpectralEntropySimilarity and GetSpectralEntropy as they were before the shared-bin rewrite,
    // with the SpectrumHandler helpers they called, all kept verbatim so that the comparison is
    // against the code that ran, not against a restatement.
    private static double LegacySimilarity(List<SpectrumPeak> peaks1, List<SpectrumPeak> peaks2, double bin) {
        if (peaks1 is null || peaks2 is null || peaks1.Count == 0 || peaks2.Count == 0) return -1d;

        var combinedSpectrum = LegacyGetCombinedSpectrum(SpectrumHandler.GetNormalizedByTotalIntensityPeaks(peaks1), SpectrumHandler.GetNormalizedByTotalIntensityPeaks(peaks2), bin);
        var entropy12 = LegacyEntropy(combinedSpectrum);
        var entropy1 = LegacyEntropy(LegacyGetBinnedSpectrum(peaks1, bin));
        var entropy2 = LegacyEntropy(LegacyGetBinnedSpectrum(peaks2, bin));

        return 1 - (2 * entropy12 - entropy1 - entropy2) * 0.5;
    }

    private static double LegacyEntropy(List<SpectrumPeak> peaks) {
        var sumIntensity = peaks.Sum(n => n.Intensity);
        return -1 * peaks.Sum(n => n.Intensity / sumIntensity * Math.Log(n.Intensity / sumIntensity, 2));
    }

    private static List<SpectrumPeak> LegacyGetCombinedSpectrum(List<SpectrumPeak> peaks1, List<SpectrumPeak> peaks2, double bin) {
        var peaks = new List<SpectrumPeak>();
        var range2Peaks = new Dictionary<int, List<SpectrumPeak>>();

        foreach (var peak in peaks1) {
            var mass = peak.Mass;
            var massframe = (int)(mass / bin);
            if (range2Peaks.ContainsKey(massframe))
                range2Peaks[massframe].Add(peak);
            else
                range2Peaks[massframe] = new List<SpectrumPeak>() { peak };
        }

        foreach (var peak in peaks2) {
            var mass = peak.Mass;
            var massframe = (int)(mass / bin);
            if (range2Peaks.ContainsKey(massframe))
                range2Peaks[massframe].Add(peak);
            else
                range2Peaks[massframe] = new List<SpectrumPeak>() { peak };
        }

        foreach (var pair in range2Peaks) {
            var maxMass = pair.Value.Argmax(n => n.Intensity).Mass;
            var sumIntensity = pair.Value.Sum(n => n.Intensity) * 0.5;
            peaks.Add(new SpectrumPeak(maxMass, sumIntensity));
        }

        return peaks;
    }

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

using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.MsdialCore.Utility.Tests;

[TestClass()]
public class DataAccessTests
{
    [TestMethod()]
    public void GetFineIsotopicPeaksTest() {
        var spectrum = new RawSpectrum
        {
            Spectrum = [
                new RawPeakElement { Mz = 50.0, Intensity = 50.0 }, // less than m/z
                new RawPeakElement { Mz = 80.0, Intensity = 80.0 }, // M+0
                new RawPeakElement { Mz = 81.0, Intensity = 81.0 }, // M+1
                new RawPeakElement { Mz = 81.5, Intensity = 81.5 }, // other peak
                new RawPeakElement { Mz = 81.995, Intensity = 81.995 }, // M+2
                new RawPeakElement { Mz = 83.005, Intensity = 83.005 }, // M+3
                new RawPeakElement { Mz = 83.995, Intensity = 83.995 }, // M+3
                new RawPeakElement { Mz = 84.005, Intensity = 84.005 }, // M+4
                new RawPeakElement { Mz = 85.0, Intensity = 85.0 }, // after maxIsotopes

            ],
        };
        var ionFeature = new IonFeatureCharacter
        {
            Charge = 1,
        };
        var mz = 80d;
        var tolerance = .01f;
        var maxIsotopes = 4;
        var actual = DataAccess.GetFineIsotopicPeaks(spectrum, ionFeature, mz, tolerance, maxIsotopes);

        var expected = new[]
        {
            new IsotopicPeak { Mass = 80.0, AbsoluteAbundance = 80.0f, },
            new IsotopicPeak { Mass = 81.0, AbsoluteAbundance = 81.0 },
            new IsotopicPeak { Mass = 81.995, AbsoluteAbundance = 81.995 },
            new IsotopicPeak { Mass = 83.005, AbsoluteAbundance = 83.005 },
            new IsotopicPeak { Mass = 83.995, AbsoluteAbundance = 83.995 },
            new IsotopicPeak { Mass = 84.005, AbsoluteAbundance = 84.005 },
        };

        for (int i = 0; i < actual.Count; i++) {
            System.Diagnostics.Debug.WriteLine($"Actual Isotopic Peak at index {i}: Mass = {actual[i].Mass}, Absolute Abundance = {actual[i].AbsoluteAbundance}");
        }

        Assert.AreEqual(expected.Length, actual.Count, "The count of expected isotopic peaks does not match the count of actual isotopic peaks.");
        for (int i = 0; i < expected.Length; i++) {
            Assert.AreEqual(expected[i].Mass, actual[i].Mass, $"The mass of expected isotopic peak at index {i} does not match the mass of actual isotopic peak at index {i}.");
            Assert.AreEqual(expected[i].AbsoluteAbundance, actual[i].AbsoluteAbundance, $"The absolute abundance of expected isotopic peak at index {i} does not match the absolute abundance of actual isotopic peak at index {i}.");
        }
    }

    [TestMethod()]
    public void GetAverageSpectrumPassIndexTest() {
        var spectra = new[]
        {
            new RawSpectrum { Spectrum = [new() { Mz = 100d, Intensity = 100, }] },
            new RawSpectrum { Spectrum = [new() { Mz = 200d, Intensity = 80, }] },
            new RawSpectrum { Spectrum = [new() { Mz = 300d, Intensity = 100, }] },
            new RawSpectrum { Spectrum = [new() { Mz = 100d, Intensity = 20, }, new() { Mz = 200d, Intensity = 70d }] },
        };

        var actual = DataAccess.GetAverageSpectrum(spectra, [0, 1, 3,], .01);
        var expected = new[]
        {
            new SpectrumPeak { Mass = 100d, Intensity = 40, },
            new SpectrumPeak { Mass = 200d, Intensity = 50, },
        };

        Assert.AreEqual(expected.Length, actual.Count);
        for (int i = 0; i < expected.Length; i++) {
            Assert.AreEqual(expected[i].Mass, actual[i].Mass);
            Assert.AreEqual(expected[i].Intensity, actual[i].Intensity);
        }
    }

    [TestMethod()]
    public void GetAverageSpectrumTest() {
        var spectra = new[]
        {
            new RawSpectrum { Spectrum = [new() { Mz = 100d, Intensity = 100, }] },
            new RawSpectrum { Spectrum = [new() { Mz = 200d, Intensity = 80, }] },
            new RawSpectrum { Spectrum = [new() { Mz = 100d, Intensity = 20, }, new() { Mz = 200d, Intensity = 70d }] },
        };

        var actual = DataAccess.GetAverageSpectrum(spectra, .01);
        var expected = new[]
        {
            new SpectrumPeak { Mass = 100d, Intensity = 40, },
            new SpectrumPeak { Mass = 200d, Intensity = 50, },
        };

        Assert.AreEqual(expected.Length, actual.Count);
        for (int i = 0; i < expected.Length; i++) {
            Assert.AreEqual(expected[i].Mass, actual[i].Mass);
            Assert.AreEqual(expected[i].Intensity, actual[i].Intensity);
        }
    }
}
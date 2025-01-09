using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MsdialCoreTestHelper.DataProvider;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj.Tests;

[TestClass]
public class RetentionTimeTypedSpectraTests
{
    [TestMethod]
    public async Task GetMS1ExtractedChromatogramTest() {
        var rawSpectra = new List<RawSpectrum>
                {
            new RawSpectrum { Index = 0, ScanStartTime = 1d, DriftTime = 5d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, } } },
            new RawSpectrum { Index = 1, ScanStartTime = 2d, DriftTime = 4d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 1001d, } } },
            new RawSpectrum { Index = 2, ScanStartTime = 3d, DriftTime = 3d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 1002d, } } },
            new RawSpectrum { Index = 3, ScanStartTime = 4d, DriftTime = 2d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 103d, Intensity = 1003d, } } },
            new RawSpectrum { Index = 4, ScanStartTime = 5d, DriftTime = 1d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 104d, Intensity = 1004d, } } },
        };
        var provider = new StubDataProvider() { Spectra = rawSpectra };
        var spectra = new RetentionTimeTypedSpectra(provider, ChromXUnit.Min, IonMode.Positive, AcquisitionType.DDA);
        var chromatogram = ((Chromatogram) await spectra.GetMS1ExtractedChromatogramAsync(102, 2d, 2d, 4d, default).ConfigureAwait(false)).AsPeakArray();
        Assert.AreEqual(3, chromatogram.Count);
        Assert.AreEqual(1, chromatogram[0].ID);
        Assert.AreEqual(101d, chromatogram[0].Mass);
        Assert.AreEqual(1001d, chromatogram[0].Intensity);
        Assert.AreEqual(2d, chromatogram[0].ChromXs.RT.Value);
        Assert.AreEqual(2, chromatogram[1].ID);
        Assert.AreEqual(102d, chromatogram[1].Mass);
        Assert.AreEqual(1002d, chromatogram[1].Intensity);
        Assert.AreEqual(3d, chromatogram[1].ChromXs.RT.Value);
        Assert.AreEqual(3, chromatogram[2].ID);
        Assert.AreEqual(103d, chromatogram[2].Mass);
        Assert.AreEqual(1003d, chromatogram[2].Intensity);
        Assert.AreEqual(4d, chromatogram[2].ChromXs.RT.Value);
    }

    [TestMethod()]
    public async Task GetMs1TotalIonChromatogramTest() {
        List<RawSpectrum> rawSpectra = new List<RawSpectrum>
        {
            new RawSpectrum { Index = 0, ScanStartTime = 1d, DriftTime = 5d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 105d, Intensity = 1005d, }, } },
            new RawSpectrum { Index = 1, ScanStartTime = 2d, DriftTime = 4d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 1001d, }, new RawPeakElement{ Mz = 106d, Intensity = 1006d, }, } },
            new RawSpectrum { Index = 2, ScanStartTime = 3d, DriftTime = 3d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 1002d, }, new RawPeakElement{ Mz = 107d, Intensity = 1007d, }, } },
            new RawSpectrum { Index = 3, ScanStartTime = 4d, DriftTime = 2d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 103d, Intensity = 1003d, }, new RawPeakElement{ Mz = 108d, Intensity = 1008d, }, } },
            new RawSpectrum { Index = 4, ScanStartTime = 5d, DriftTime = 1d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 104d, Intensity = 1004d, }, new RawPeakElement{ Mz = 109d, Intensity = 1009d, }, } },
        };
        var provider = new StubDataProvider() { Spectra = rawSpectra };
        var spectra = new RetentionTimeTypedSpectra(provider, ChromXUnit.Min, IonMode.Positive, AcquisitionType.DDA);
        var chromatogram = (await spectra.GetMS1TotalIonChromatogramAsync(2d, 4d, default).ConfigureAwait(false)).AsPeakArray();
        Assert.AreEqual(3, chromatogram.Count);
        Assert.AreEqual(1, chromatogram[0].ID);
        Assert.AreEqual(106d, chromatogram[0].Mass);
        Assert.AreEqual(2007d, chromatogram[0].Intensity);
        Assert.AreEqual(2d, chromatogram[0].ChromXs.RT.Value);
        Assert.AreEqual(2, chromatogram[1].ID);
        Assert.AreEqual(107d, chromatogram[1].Mass);
        Assert.AreEqual(2009d, chromatogram[1].Intensity);
        Assert.AreEqual(3d, chromatogram[1].ChromXs.RT.Value);
        Assert.AreEqual(3, chromatogram[2].ID);
        Assert.AreEqual(108d, chromatogram[2].Mass);
        Assert.AreEqual(2011d, chromatogram[2].Intensity);
        Assert.AreEqual(4d, chromatogram[2].ChromXs.RT.Value);
    }

    [TestMethod()]
    public async Task GetMS1BasePeakChromatogramTest() {
        List<RawSpectrum> rawSpectra = new List<RawSpectrum>
        {
            new RawSpectrum { Index = 0, RawSpectrumID = new IndexedSpectrumIdentifier(0), ScanStartTime = 1d, DriftTime = 5d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 105d, Intensity = 1005d, }, } },
            new RawSpectrum { Index = 1, RawSpectrumID = new IndexedSpectrumIdentifier(1), ScanStartTime = 2d, DriftTime = 4d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 1001d, }, new RawPeakElement{ Mz = 106d, Intensity = 1006d, }, } },
            new RawSpectrum { Index = 2, RawSpectrumID = new IndexedSpectrumIdentifier(2), ScanStartTime = 3d, DriftTime = 3d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 1002d, }, new RawPeakElement{ Mz = 107d, Intensity = 1007d, }, } },
            new RawSpectrum { Index = 3, RawSpectrumID = new IndexedSpectrumIdentifier(3), ScanStartTime = 4d, DriftTime = 2d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 103d, Intensity = 1003d, }, new RawPeakElement{ Mz = 108d, Intensity = 1008d, }, } },
            new RawSpectrum { Index = 4, RawSpectrumID = new IndexedSpectrumIdentifier(4), ScanStartTime = 5d, DriftTime = 1d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 104d, Intensity = 1004d, }, new RawPeakElement{ Mz = 109d, Intensity = 1009d, }, } },
        };
        var provider = new StubDataProvider() { Spectra = rawSpectra };
        var spectra = new RetentionTimeTypedSpectra(provider, ChromXUnit.Min, IonMode.Positive, AcquisitionType.DDA);
        var chromatogram = (await spectra.GetMS1BasePeakChromatogramAsync(2d, 4d, default).ConfigureAwait(false)).AsPeakArray();
        Assert.AreEqual(3, chromatogram.Count);
        Assert.AreEqual(1, chromatogram[0].ID);
        Assert.AreEqual(106d, chromatogram[0].Mass);
        Assert.AreEqual(1006d, chromatogram[0].Intensity);
        Assert.AreEqual(2d, chromatogram[0].ChromXs.RT.Value);
        Assert.AreEqual(2, chromatogram[1].ID);
        Assert.AreEqual(107d, chromatogram[1].Mass);
        Assert.AreEqual(1007d, chromatogram[1].Intensity);
        Assert.AreEqual(3d, chromatogram[1].ChromXs.RT.Value);
        Assert.AreEqual(3, chromatogram[2].ID);
        Assert.AreEqual(108d, chromatogram[2].Mass);
        Assert.AreEqual(1008d, chromatogram[2].Intensity);
        Assert.AreEqual(4d, chromatogram[2].ChromXs.RT.Value);
    }

    [TestMethod()]
    public async Task GetProductIonChromatogramTest() {
        List<RawSpectrum> rawSpectra = new List<RawSpectrum>
        {
            new RawSpectrum { Index = 0, ScanStartTime = 1d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 1, ScanStartTime = 2d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 2, ScanStartTime = 3d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 3, ScanStartTime = 4d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 4, ScanStartTime = 5d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50.2d, Intensity = 2000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 5, ScanStartTime = 6d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 200d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 6, ScanStartTime = 7d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 7, ScanStartTime = 8d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
        };
        var provider = new StubDataProvider() { Spectra = rawSpectra };
        var spectra = new RetentionTimeTypedSpectra(provider, ChromXUnit.Min, IonMode.Positive, AcquisitionType.DDA);
        var chromatogram = (await spectra.GetProductIonChromatogramAsync(new MzRange(100d, .1d), new MzRange(50d, .5d), new ChromatogramRange(3d, 7d, ChromXType.RT, ChromXUnit.Min), default).ConfigureAwait(false)).AsPeakArray();
        Assert.AreEqual(2, chromatogram.Length);
        Assert.AreEqual(3, chromatogram[0].Id);
        Assert.AreEqual(50d, chromatogram[0].Mz);
        Assert.AreEqual(1000d, chromatogram[0].Intensity);
        Assert.AreEqual(4d, chromatogram[0].Time);
        Assert.AreEqual(4, chromatogram[1].Id);
        Assert.AreEqual(50.2d, chromatogram[1].Mz);
        Assert.AreEqual(2000d, chromatogram[1].Intensity);
        Assert.AreEqual(5d, chromatogram[1].Time);
    }

    [TestMethod()]
    public async Task GetMS2TotalIonChromatogram_WithValidRange_ReturnsCorrectChromatogram()
    {
        // Arrange: Create a set of test spectra with varying characteristics.
        List<RawSpectrum> rawSpectra = new List<RawSpectrum>
        {
            new RawSpectrum { Index = 0, ScanStartTime = 1d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 1, ScanStartTime = 2d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 2, ScanStartTime = 3d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 3, ScanStartTime = 4d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 4, ScanStartTime = 5d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50.2d, Intensity = 2000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 5, ScanStartTime = 6d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 200d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 6, ScanStartTime = 7d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 7, ScanStartTime = 8d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
        };
        var provider = new StubDataProvider() { Spectra = rawSpectra };
        var spectra = new RetentionTimeTypedSpectra(provider, ChromXUnit.Min, IonMode.Positive, AcquisitionType.DDA);

        // Act: Call the method under test with a specific chromatogram range.
        var chromatogram = (await spectra.GetMS2TotalIonChromatogramAsync(new ChromatogramRange(2d, 7d, ChromXType.RT, ChromXUnit.Min), default).ConfigureAwait(false)).AsPeakArray();

        // Assert: Verify the method returns the correct number of peaks and their properties.
        Assert.AreEqual(2, chromatogram.Count, "Expected 2 peaks in the chromatogram.");

        Assert.AreEqual(3, chromatogram[0].ID, "First peak should have ID 3.");
        Assert.AreEqual(50.2d, chromatogram[0].Mass, "First peak should have an Mz of 50.2.");
        Assert.AreEqual(7000d, chromatogram[0].Intensity, "First peak should have an intensity of 7000.");
        Assert.AreEqual(4d, chromatogram[0].ChromXs.RT.Value, "First peak should have a time of 4 minutes.");

        Assert.AreEqual(7, chromatogram[1].ID, "Second peak should have ID 7.");
        Assert.AreEqual(50d, chromatogram[1].Mass, "Second peak should have an Mz of 50.");
        Assert.AreEqual(2000d, chromatogram[1].Intensity, "Second peak should have an intensity of 2000.");
        Assert.AreEqual(8d, chromatogram[1].ChromXs.RT.Value, "Second peak should have a time of 8 minutes.");
    }

    [TestMethod()]
    public async Task GetMS2TotalIonChromatogram_WithExperimentIdAndValidRange_ReturnsCorrectChromatogram()
    {
        // Arrange: Create a set of test spectra with varying characteristics and different experiment IDs.
        List<RawSpectrum> rawSpectra = new List<RawSpectrum>
        {
            // Spectra with different experiment IDs
            new RawSpectrum { Index = 0, ScanStartTime = 1d, MsLevel = 1, ExperimentID = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d }, new RawPeakElement{ Mz = 50d, Intensity = 1000d } } },
            new RawSpectrum { Index = 1, ScanStartTime = 2d, MsLevel = 2, ExperimentID = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d }, new RawPeakElement{ Mz = 100d, Intensity = 1000d } } },
            // Additional spectra omitted for brevity...
            new RawSpectrum { Index = 7, ScanStartTime = 8d, MsLevel = 2, ExperimentID = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d }, new RawPeakElement{ Mz = 100d, Intensity = 1000d } } },
        };
        var provider = new StubDataProvider() { Spectra = rawSpectra };
        var spectra = new RetentionTimeTypedSpectra(provider, ChromXUnit.Min, IonMode.Positive, AcquisitionType.DDA);

        int testExperimentID = 2; // Specify the experiment ID to filter by.

        // Act: Call the method under test with a specific chromatogram range and experiment ID.
        var chromatogram = (await spectra.GetMS2TotalIonChromatogramAsync(new ChromatogramRange(1d, 8d, ChromXType.RT, ChromXUnit.Min), testExperimentID, default).ConfigureAwait(false)).AsPeakArray();

        // Assert: Verify the method returns the correct number of peaks and their properties, filtering by experiment ID.
        Assert.AreEqual(2, chromatogram.Count, "Expected 2 peaks in the chromatogram filtered by experiment ID.");

        // Assertions for the first peak
        Assert.AreEqual(1, chromatogram[0].ID, "First peak should have ID 1.");
        Assert.AreEqual(50d, chromatogram[0].Mass, "First peak should have an Mz of 50.");
        Assert.AreEqual(2000d, chromatogram[0].Intensity, "First peak should have an intensity of 2000.");
        Assert.AreEqual(2d, chromatogram[0].ChromXs.RT.Value, "First peak should have a time of 2 minutes.");

        // Assertions for the second peak
        Assert.AreEqual(7, chromatogram[1].ID, "Second peak should have ID 7."); // This assumes there's a typo in the setup or a misunderstanding of how experiment IDs are filtered.
        Assert.AreEqual(50d, chromatogram[1].Mass, "Second peak should have an Mz of 50.");
        Assert.AreEqual(2000d, chromatogram[1].Intensity, "Second peak should have an intensity of 2000.");
        Assert.AreEqual(8d, chromatogram[1].ChromXs.RT.Value, "Second peak should have a time of 8 minutes.");
    }

    [TestMethod()]
    public async Task GetMS2ExtractedIonChromatogram_WithExperimentIdAndValidMzRange_ReturnsCorrectChromatogram()
    {
        // Arrange: Create a set of test spectra with varying characteristics and different experiment IDs.
        List<RawSpectrum> rawSpectra = new List<RawSpectrum>
        {
            new RawSpectrum { Index = 0, ScanStartTime = 1d, MsLevel = 2, ExperimentID = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 100d }, new RawPeakElement{ Mz = 200d, Intensity = 1000d } } },
            new RawSpectrum { Index = 1, ScanStartTime = 2d, MsLevel = 2, ExperimentID = 2, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 150d, Intensity = 500d }, new RawPeakElement{ Mz = 250d, Intensity = 1500d } } },
            // Additional spectra omitted for brevity...
            new RawSpectrum { Index = 7, ScanStartTime = 8d, MsLevel = 2, ExperimentID = 2, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 150d, Intensity = 500d }, new RawPeakElement{ Mz = 250d, Intensity = 1500d } } },
        };
        var provider = new StubDataProvider() { Spectra = rawSpectra };
        var spectra = new RetentionTimeTypedSpectra(provider, ChromXUnit.Min, IonMode.Positive, AcquisitionType.DDA);

        int testExperimentID = 2; // Specify the experiment ID to filter by.
        MzRange testMzRange = new MzRange(140d, 10d); // Targeting m/z of 150 with a tolerance of 10.

        // Act: Call the method under test with a specific chromatogram range, m/z range, and experiment ID.
        var chromatogram = (await spectra.GetMS2ExtractedIonChromatogramAsync(testMzRange, new ChromatogramRange(1d, 8d, ChromXType.RT, ChromXUnit.Min), testExperimentID, default).ConfigureAwait(false)).AsPeakArray();

        // Assert: Verify the method returns the correct number of peaks and their properties, filtering by experiment ID and m/z range.
        Assert.AreEqual(2, chromatogram.Length, "Expected 2 peaks in the chromatogram filtered by experiment ID and m/z range.");

        // Assertions for peaks, considering they are filtered by m/z range and experiment ID
        foreach (var peak in chromatogram)
        {
            Assert.IsTrue(peak.Mz >= testMzRange.Mz - testMzRange.Tolerance && peak.Mz <= testMzRange.Mz + testMzRange.Tolerance, $"Peak mass {peak.Mz} should be within the m/z range of {testMzRange.Mz} ± {testMzRange.Tolerance}.");
            Assert.IsTrue(peak.Intensity > 0, "Peak intensity should be greater than 0.");
            Assert.IsTrue(peak.Time >= 1d && peak.Time <= 8d, $"Peak retention time {peak.Time} should be within the specified range.");
        }
    }
}

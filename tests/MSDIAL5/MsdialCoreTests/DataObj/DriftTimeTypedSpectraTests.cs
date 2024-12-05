using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CompMs.MsdialCore.DataObj.Tests;

[TestClass]
public class DriftTimeTypedSpectraTests
{
    [TestMethod]
    public void GetMs1ExtractedChromatogramTest() {
        var spectra = new DriftTimeTypedSpectra(new[]
        {
            new RawSpectrum { Index = 0, ScanStartTime = 1d, DriftTime = 5d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, } } },
            new RawSpectrum { Index = 1, ScanStartTime = 2d, DriftTime = 4d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 1001d, } } },
            new RawSpectrum { Index = 2, ScanStartTime = 3d, DriftTime = 3d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 1002d, } } },
            new RawSpectrum { Index = 3, ScanStartTime = 4d, DriftTime = 2d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 103d, Intensity = 1003d, } } },
            new RawSpectrum { Index = 4, ScanStartTime = 5d, DriftTime = 1d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 104d, Intensity = 1004d, } } },
        },
        ChromXUnit.Msec,
        IonMode.Positive, AcquisitionType.DDA);
        var chromatogram = spectra.GetMs1ExtractedChromatogram(102, 2d, 2d, 4d).AsPeakArray();
        Assert.AreEqual(3, chromatogram.Count);
        Assert.AreEqual(3, chromatogram[0].ID);
        Assert.AreEqual(103d, chromatogram[0].Mass);
        Assert.AreEqual(1003d, chromatogram[0].Intensity);
        Assert.AreEqual(2d, chromatogram[0].ChromXs.Drift.Value);
        Assert.AreEqual(2, chromatogram[1].ID);
        Assert.AreEqual(102d, chromatogram[1].Mass);
        Assert.AreEqual(1002d, chromatogram[1].Intensity);
        Assert.AreEqual(3d, chromatogram[1].ChromXs.Drift.Value);
        Assert.AreEqual(1, chromatogram[2].ID);
        Assert.AreEqual(101d, chromatogram[2].Mass);
        Assert.AreEqual(1001d, chromatogram[2].Intensity);
        Assert.AreEqual(4d, chromatogram[2].ChromXs.Drift.Value);
    }

    [TestMethod()]
    public void GetMs1TotalIonChromatogramTest() {
        var spectra = new DriftTimeTypedSpectra(new[]
        {
            new RawSpectrum { Index = 0, ScanStartTime = 1d, DriftTime = 5d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 105d, Intensity = 1005d, }, } },
            new RawSpectrum { Index = 1, ScanStartTime = 2d, DriftTime = 4d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 1001d, }, new RawPeakElement{ Mz = 106d, Intensity = 1006d, }, } },
            new RawSpectrum { Index = 2, ScanStartTime = 3d, DriftTime = 3d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 1002d, }, new RawPeakElement{ Mz = 107d, Intensity = 1007d, }, } },
            new RawSpectrum { Index = 3, ScanStartTime = 4d, DriftTime = 2d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 103d, Intensity = 1003d, }, new RawPeakElement{ Mz = 108d, Intensity = 1008d, }, } },
            new RawSpectrum { Index = 4, ScanStartTime = 5d, DriftTime = 1d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 104d, Intensity = 1004d, }, new RawPeakElement{ Mz = 109d, Intensity = 1009d, }, } },
        },
        ChromXUnit.Msec,
        IonMode.Positive, AcquisitionType.DDA);
        var chromatogram = spectra.GetMs1TotalIonChromatogram(2d, 4d).AsPeakArray();
        Assert.AreEqual(3, chromatogram.Count);
        Assert.AreEqual(3, chromatogram[0].ID);
        Assert.AreEqual(108d, chromatogram[0].Mass);
        Assert.AreEqual(2011d, chromatogram[0].Intensity);
        Assert.AreEqual(2d, chromatogram[0].ChromXs.Drift.Value);
        Assert.AreEqual(2, chromatogram[1].ID);
        Assert.AreEqual(107d, chromatogram[1].Mass);
        Assert.AreEqual(2009d, chromatogram[1].Intensity);
        Assert.AreEqual(3d, chromatogram[1].ChromXs.Drift.Value);
        Assert.AreEqual(1, chromatogram[2].ID);
        Assert.AreEqual(106d, chromatogram[2].Mass);
        Assert.AreEqual(2007d, chromatogram[2].Intensity);
        Assert.AreEqual(4d, chromatogram[2].ChromXs.Drift.Value);
    }

    [TestMethod()]
    public void GetMs1BasePeakChromatogramTest() {
        var spectra = new DriftTimeTypedSpectra(new[]
        {
            new RawSpectrum { Index = 0, ScanStartTime = 1d, DriftTime = 5d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 105d, Intensity = 1005d, }, } },
            new RawSpectrum { Index = 1, ScanStartTime = 2d, DriftTime = 4d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 1001d, }, new RawPeakElement{ Mz = 106d, Intensity = 1006d, }, } },
            new RawSpectrum { Index = 2, ScanStartTime = 3d, DriftTime = 3d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 1002d, }, new RawPeakElement{ Mz = 107d, Intensity = 1007d, }, } },
            new RawSpectrum { Index = 3, ScanStartTime = 4d, DriftTime = 2d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 103d, Intensity = 1003d, }, new RawPeakElement{ Mz = 108d, Intensity = 1008d, }, } },
            new RawSpectrum { Index = 4, ScanStartTime = 5d, DriftTime = 1d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 104d, Intensity = 1004d, }, new RawPeakElement{ Mz = 109d, Intensity = 1009d, }, } },
        },
        ChromXUnit.Msec,
        IonMode.Positive, AcquisitionType.DDA);
        var chromatogram = spectra.GetMs1BasePeakChromatogram(2d, 4d).AsPeakArray();
        Assert.AreEqual(3, chromatogram.Count);
        Assert.AreEqual(3, chromatogram[0].ID);
        Assert.AreEqual(108d, chromatogram[0].Mass);
        Assert.AreEqual(1008d, chromatogram[0].Intensity);
        Assert.AreEqual(2d, chromatogram[0].ChromXs.Drift.Value);
        Assert.AreEqual(2, chromatogram[1].ID);
        Assert.AreEqual(107d, chromatogram[1].Mass);
        Assert.AreEqual(1007d, chromatogram[1].Intensity);
        Assert.AreEqual(3d, chromatogram[1].ChromXs.Drift.Value);
        Assert.AreEqual(1, chromatogram[2].ID);
        Assert.AreEqual(106d, chromatogram[2].Mass);
        Assert.AreEqual(1006d, chromatogram[2].Intensity);
        Assert.AreEqual(4d, chromatogram[2].ChromXs.Drift.Value);
    }

    [TestMethod()]
    public void GetProductIonChromatogramTest() {
        var spectra = new DriftTimeTypedSpectra(new[]
        {
            new RawSpectrum { Index = 0, DriftTime = 1d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 1, DriftTime = 2d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 2, DriftTime = 3d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 3, DriftTime = 4d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 4, DriftTime = 5d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50.2d, Intensity = 2000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 5, DriftTime = 6d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 200d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 6, DriftTime = 7d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 7, DriftTime = 8d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
        },
        ChromXUnit.Msec,
        IonMode.Positive, AcquisitionType.DDA);
        var chromatogram = spectra.GetProductIonChromatogram(new MzRange(100d, .1d), new MzRange(50d, .5d), new ChromatogramRange(3d, 7d, ChromXType.Drift, ChromXUnit.Msec)).AsPeakArray();
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
    public void GetMS2TotalIonChromatogram_WithDriftTimeRange_ReturnsCorrectChromatogram()
    {
        // Arrange: Set up a collection of MS2 spectra with varying drift times.
        var spectra = new DriftTimeTypedSpectra(new[]
        {
            new RawSpectrum { Index = 0, ScanStartTime = 1d, DriftTime = 5d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 500d }, new RawPeakElement{ Mz = 105d, Intensity = 505d } } },
            new RawSpectrum { Index = 1, ScanStartTime = 2d, DriftTime = 4d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 501d }, new RawPeakElement{ Mz = 106d, Intensity = 506d } } },
            new RawSpectrum { Index = 2, ScanStartTime = 3d, DriftTime = 3d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 502d }, new RawPeakElement{ Mz = 107d, Intensity = 507d } } },
            new RawSpectrum { Index = 3, ScanStartTime = 4d, DriftTime = 2d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 103d, Intensity = 503d }, new RawPeakElement{ Mz = 108d, Intensity = 508d } } },
            new RawSpectrum { Index = 4, ScanStartTime = 5d, DriftTime = 1d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 104d, Intensity = 504d }, new RawPeakElement{ Mz = 109d, Intensity = 509d } } },
        },
        ChromXUnit.Msec,
        IonMode.Positive, AcquisitionType.DDA);

        var chromatogramRange = new ChromatogramRange(2d, 4d, ChromXType.Drift, ChromXUnit.Msec);

        // Act: Generate the chromatogram for the specified drift time range.
        var chromatogram = spectra.GetMs2TotalIonChromatogram(chromatogramRange).AsPeakArray();

        // Assert: Verify the chromatogram contains the correct peaks with expected properties.
        Assert.AreEqual(3, chromatogram.Count, "Expected 3 peaks in the chromatogram.");
        Assert.AreEqual(3, chromatogram[0].ID, "First peak's ID should be 3.");
        Assert.AreEqual(108d, chromatogram[0].Mass, "First peak's mass should be 108.");
        Assert.AreEqual(1011d, chromatogram[0].Intensity, "First peak's intensity should be 1011.");
        Assert.AreEqual(2d, chromatogram[0].ChromXs.Drift.Value, "First peak's drift time should be 2 msec.");

        Assert.AreEqual(2, chromatogram[1].ID, "Second peak's ID should be 2.");
        Assert.AreEqual(107d, chromatogram[1].Mass, "Second peak's mass should be 107.");
        Assert.AreEqual(1009d, chromatogram[1].Intensity, "Second peak's intensity should be 1009.");
        Assert.AreEqual(3d, chromatogram[1].ChromXs.Drift.Value, "Second peak's drift time should be 3 msec.");

        Assert.AreEqual(1, chromatogram[2].ID, "Third peak's ID should be 1.");
        Assert.AreEqual(106d, chromatogram[2].Mass, "Third peak's mass should be 106.");
        Assert.AreEqual(1007d, chromatogram[2].Intensity, "Third peak's intensity should be 1007.");
        Assert.AreEqual(4d, chromatogram[2].ChromXs.Drift.Value, "Third peak's drift time should be 4 msec.");
    }

    [TestMethod()]
    public void GetMS2TotalIonChromatogram_WithExperimentIdAndDriftTimeRange_ReturnsCorrectChromatogram()
    {
        // Arrange: Set up a collection of MS2 spectra with varying drift times and different experiment IDs.
        var spectra = new DriftTimeTypedSpectra(new[]
        {
            new RawSpectrum { Index = 0, ScanStartTime = 1d, DriftTime = 5d, MsLevel = 2, ExperimentID = 2, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 500d }, new RawPeakElement{ Mz = 105d, Intensity = 505d } } },
            new RawSpectrum { Index = 1, ScanStartTime = 2d, DriftTime = 4d, MsLevel = 2, ExperimentID = 2, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 501d }, new RawPeakElement{ Mz = 106d, Intensity = 506d } } },
            new RawSpectrum { Index = 2, ScanStartTime = 3d, DriftTime = 3d, MsLevel = 2, ExperimentID = 3, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 502d }, new RawPeakElement{ Mz = 107d, Intensity = 507d } } },
            // Other spectra omitted for brevity
        },
        ChromXUnit.Msec,
        IonMode.Positive, AcquisitionType.DDA);

        var chromatogramRange = new ChromatogramRange(2d, 5d, ChromXType.Drift, ChromXUnit.Msec);
        int experimentID = 2; // Specify the experiment ID to filter by

        // Act: Generate the chromatogram for the specified drift time range and experiment ID.
        var chromatogram = spectra.GetMS2TotalIonChromatogram(chromatogramRange, experimentID).AsPeakArray();

        // Assert: Verify the chromatogram contains the correct peaks with expected properties, filtered by experiment ID.
        Assert.AreEqual(2, chromatogram.Count, "Expected 2 peaks in the chromatogram filtered by experiment ID.");

        Assert.AreEqual(1, chromatogram[0].ID, "First peak's ID should be 1.");
        Assert.AreEqual(106d, chromatogram[0].Mass, "First peak's mass should be 106.");
        Assert.AreEqual(1007d, chromatogram[0].Intensity, "First peak's intensity should be 1007.");
        Assert.AreEqual(4d, chromatogram[0].ChromXs.Drift.Value, "First peak's drift time should be 4 msec.");

        Assert.AreEqual(0, chromatogram[1].ID, "Second peak's ID should be 0.");
        Assert.AreEqual(105d, chromatogram[1].Mass, "Second peak's mass should be 105.");
        Assert.AreEqual(1005d, chromatogram[1].Intensity, "Second peak's intensity should be 1005.");
        Assert.AreEqual(5d, chromatogram[1].ChromXs.Drift.Value, "Second peak's drift time should be 5 msec.");
    }

    [TestMethod()]
    public void GetMS2ExtractedIonChromatogram_WithExperimentIdAndValidMzAndDriftTimeRange_ReturnsCorrectChromatogram()
    {
        // Arrange: Create a set of test spectra with varying characteristics and different experiment IDs.
        var spectra = new DriftTimeTypedSpectra(new[]
        {
            new RawSpectrum { Index = 0, DriftTime = 1d, MsLevel = 2, ExperimentID = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 100d }, new RawPeakElement{ Mz = 150d, Intensity = 1000d } } },
            new RawSpectrum { Index = 1, DriftTime = 2d, MsLevel = 2, ExperimentID = 2, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 150d, Intensity = 500d }, new RawPeakElement{ Mz = 200d, Intensity = 1500d } } },
            new RawSpectrum { Index = 2, DriftTime = 3d, MsLevel = 2, ExperimentID = 2, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 150d, Intensity = 500d }, new RawPeakElement{ Mz = 200d, Intensity = 1500d } } },
            // Additional spectra omitted for brevity...
            new RawSpectrum { Index = 6, DriftTime = 7d, MsLevel = 2, ExperimentID = 2, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 150d, Intensity = 500d }, new RawPeakElement{ Mz = 200d, Intensity = 1500d } } },
        },
        ChromXUnit.Msec,
        IonMode.Positive, AcquisitionType.DDA);

        int testExperimentID = 2; // Specify the experiment ID to filter by.
        MzRange testMzRange = new MzRange(150d, 5d); // Targeting m/z of 150 with a tolerance of 5.

        // Act: Call the method under test with a specific chromatogram range, m/z range, and experiment ID.
        var chromatogram = spectra.GetMS2ExtractedIonChromatogram(testMzRange, new ChromatogramRange(2d, 7d, ChromXType.Drift, ChromXUnit.Msec), testExperimentID).AsPeakArray();

        // Assert: Verify the method returns the correct number of peaks and their properties, filtering by experiment ID, m/z range, and drift time range.
        Assert.AreEqual(3, chromatogram.Length, "Expected 3 peaks in the chromatogram filtered by experiment ID, m/z range, and drift time range.");

        // Assertions for peaks
        foreach (var peak in chromatogram)
        {
            Assert.IsTrue(peak.Mz >= testMzRange.Mz - testMzRange.Tolerance && peak.Mz <= testMzRange.Mz + testMzRange.Tolerance, $"Peak Mz {peak.Mz} should be within the m/z range of {testMzRange.Mz} ± {testMzRange.Tolerance}.");
            Assert.IsTrue(peak.Intensity > 0, "Peak intensity should be greater than 0.");
            Assert.IsTrue(peak.Time >= 2d && peak.Time <= 7d, $"Peak drift time {peak.Time} should be within the specified range.");
        }
    }
}

using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.DataObj.Tests.Helper;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Loader.Tests;

[TestClass()]
public class MS2TicLoaderTests
{
     [TestMethod()]
    public void LoadChromatogram_ReturnsProcessedChromatogram()
    {
        // Arrange
        var mockRawSpectra = new MockRawSpectra();
        var chromatogramRange = new ChromatogramRange(0, 100, ChromXType.RT, ChromXUnit.Min);
        var peakPickParameter = new PeakPickBaseParameter
        {
            SmoothingMethod = SmoothingMethod.LinearWeightedMovingAverage, // Assuming an enum or similar exists
            SmoothingLevel = 2 // Assuming this is an appropriate value
        };

        var expectedPeaks = new ValuePeak[] {
            new ValuePeak(0, 1, 450, 1000),
            new ValuePeak(1, 2, 460, 2000),
        };
        var expectedPeakItems = expectedPeaks.Select(p => new PeakItem(p.ConvertToChromatogramPeak(ChromXType.RT, ChromXUnit.Min))).ToList();

        mockRawSpectra.ExpectedChromatogram2 = new Chromatogram(expectedPeaks, ChromXType.RT, ChromXUnit.Min);
        var expectedChromatogram = mockRawSpectra.ExpectedChromatogram2.ChromatogramSmoothing(peakPickParameter.SmoothingMethod, peakPickParameter.SmoothingLevel);
        
        var loader = new MS2TicLoader(mockRawSpectra, chromatogramRange, peakPickParameter);

        // Act
        var result = loader.LoadChromatogram();

        // Assert
        Assert.AreEqual(expectedChromatogram.Length, result.ChromatogramPeaks.Count, "The number of peaks in the chromatogram does not match the expected count.");
        for (int i = 0; i < expectedPeakItems.Count; i++)
        {
            Assert.AreEqual(expectedChromatogram.Time(i), result.ChromatogramPeaks[i].Time, "The peak time does not match the expected value.");
            Assert.AreEqual(expectedChromatogram.Intensity(i), result.ChromatogramPeaks[i].Intensity, "The peak intensity does not match the expected value.");
        }
    }

    [TestMethod()]
    public void LoadChromatogram_CallsGetMs2TotalIonChromatogramWithCorrectExperimentId()
    {
        // Arrange
        var mockRawSpectra = new MockRawSpectra_GetMS2TotalIonChromatogram();
        var chromatogramRange = new ChromatogramRange(0, 100, ChromXType.RT, ChromXUnit.Min);
        var peakPickParameter = new PeakPickBaseParameter
        {
            SmoothingMethod = SmoothingMethod.LinearWeightedMovingAverage,
            SmoothingLevel = 2
        };
        int experimentID = 2; // Example experiment ID to filter by

        var loader = new MS2TicLoader(mockRawSpectra, chromatogramRange, peakPickParameter);

        // Act
        loader.LoadChromatogram(experimentID);

        // Assert
        Assert.AreEqual(experimentID, mockRawSpectra.ExperimentID); // Verifies that GetMS2TotalIonChromatogram was called with the specified parameters
    }

    class MockRawSpectra_GetMS2TotalIonChromatogram : IRawSpectra
    {
        public int ExperimentID = -1;

        public Chromatogram GetDriftChromatogramByScanRtMz(int scanID, float rt, float rtWidth, float mz, float mztol) {
            throw new System.NotImplementedException();
        }

        public Chromatogram GetMS1BasePeakChromatogram(ChromatogramRange chromatogramRange) {
            throw new System.NotImplementedException();
        }

        public Chromatogram GetMS1ExtractedChromatogramByHighestBasePeakMz(IEnumerable<ISpectrumPeak> peaks, double tolerance, ChromatogramRange chromatogramRange) {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ExtractedIonChromatogram> GetMS1ExtractedChromatograms(IEnumerable<double> mzs, double tolerance, ChromatogramRange chromatogramRange) {
            throw new System.NotImplementedException();
        }

        public ExtractedIonChromatogram GetMS1ExtractedChromatogram(MzRange mzRange, ChromatogramRange chromatogramRange) {
            throw new System.NotImplementedException();
        }

        public Chromatogram GetMS1TotalIonChromatogram(ChromatogramRange chromatogramRange) {
            throw new System.NotImplementedException();
        }

        public ExtractedIonChromatogram GetMS2ExtractedIonChromatogram(int experimentID, MzRange product, ChromatogramRange chromatogramRange) {
            throw new NotImplementedException();
        }

        public Chromatogram GetMS2TotalIonChromatogram(ChromatogramRange chromatogramRange) {
            throw new System.NotImplementedException();
        }

        public SpecificExperimentChromatogram GetMS2TotalIonChromatogram(int experimentID, ChromatogramRange chromatogramRange) {
            ExperimentID = experimentID;
            return new SpecificExperimentChromatogram(Array.Empty<ValuePeak>(), ChromXType.RT, ChromXUnit.Min, experimentID);
        }

        public PeakMs2Spectra GetPeakMs2Spectra(ChromatogramPeakFeature rtPeakFeature, double ms2Tolerance, AcquisitionType acquisitionType, DriftTime driftTime) {
            throw new System.NotImplementedException();
        }

        public ExtractedIonChromatogram GetMS2ExtractedIonChromatogram(MzRange precursor, MzRange product, ChromatogramRange chromatogramRange) {
            throw new System.NotImplementedException();
        }
    }
}
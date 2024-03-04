using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj.Tests.Helper;

public class MockRawSpectra : IRawSpectra
{
    public Chromatogram ExpectedChromatogram { get; set; }

    Chromatogram IRawSpectra.GetProductIonChromatogram(MzRange precursor, MzRange product, ChromatogramRange chromatogramRange)
    {
        return ExpectedChromatogram;
    }

    Chromatogram IRawSpectra.GetDriftChromatogramByScanRtMz(int scanID, float rt, float rtWidth, float mz, float mztol) {
        throw new NotImplementedException();
    }

    Chromatogram IRawSpectra.GetMs1BasePeakChromatogram(ChromatogramRange chromatogramRange) {
        throw new NotImplementedException();
    }

    Chromatogram IRawSpectra.GetMs1ExtractedChromatogram(double mz, double tolerance, ChromatogramRange chromatogramRange) {
        throw new NotImplementedException();
    }

    Chromatogram IRawSpectra.GetMs1ExtractedChromatogramByHighestBasePeakMz(IEnumerable<ISpectrumPeak> peaks, double tolerance, ChromatogramRange chromatogramRange) {
        throw new NotImplementedException();
    }

    IEnumerable<ExtractedIonChromatogram> IRawSpectra.GetMs1ExtractedChromatograms_temp2(IEnumerable<double> mzs, double tolerance, ChromatogramRange chromatogramRange) {
        throw new NotImplementedException();
    }

    ExtractedIonChromatogram IRawSpectra.GetMs1ExtractedChromatogram_temp2(double mz, double tolerance, ChromatogramRange chromatogramRange) {
        throw new NotImplementedException();
    }

    Chromatogram IRawSpectra.GetMs1TotalIonChromatogram(ChromatogramRange chromatogramRange) {
        throw new NotImplementedException();
    }

    PeakMs2Spectra IRawSpectra.GetPeakMs2Spectra(ChromatogramPeakFeature rtPeakFeature, double ms2Tolerance, AcquisitionType acquisitionType, DriftTime driftTime) {
        throw new NotImplementedException();
    }
}

using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj.Tests.Helper;

public class MockRawSpectra : IRawSpectra
{
    public ExtractedIonChromatogram ExpectedChromatogram { get; set; }
    public Chromatogram ExpectedChromatogram2 { get; set; }
    public SpecificExperimentChromatogram ExpectedChromatogram3 { get; set; }

    ExtractedIonChromatogram IRawSpectra.GetMS2ExtractedIonChromatogram(MzRange precursor, MzRange product, ChromatogramRange chromatogramRange)
    {
        return ExpectedChromatogram;
    }

    Chromatogram IRawSpectra.GetDriftChromatogramByScanRtMz(int scanID, float rt, float rtWidth, float mz, float mztol) {
        throw new NotImplementedException();
    }

    Chromatogram IRawSpectra.GetMS1BasePeakChromatogram(ChromatogramRange chromatogramRange) {
        throw new NotImplementedException();
    }

    Chromatogram IRawSpectra.GetMS1ExtractedChromatogramByHighestBasePeakMz(IEnumerable<ISpectrumPeak> peaks, double tolerance, ChromatogramRange chromatogramRange) {
        throw new NotImplementedException();
    }

    IEnumerable<ExtractedIonChromatogram> IRawSpectra.GetMS1ExtractedChromatograms(IEnumerable<double> mzs, double tolerance, ChromatogramRange chromatogramRange) {
        throw new NotImplementedException();
    }

    ExtractedIonChromatogram IRawSpectra.GetMS1ExtractedChromatogram(MzRange mzRange, ChromatogramRange chromatogramRange) {
        throw new NotImplementedException();
    }

    Chromatogram IRawSpectra.GetMS1TotalIonChromatogram(ChromatogramRange chromatogramRange) {
        throw new NotImplementedException();
    }

    PeakMs2Spectra IRawSpectra.GetPeakMs2Spectra(ChromatogramPeakFeature rtPeakFeature, double ms2Tolerance, AcquisitionType acquisitionType, DriftTime driftTime) {
        throw new NotImplementedException();
    }

    public Chromatogram GetMS2TotalIonChromatogram(ChromatogramRange chromatogramRange) {
        return ExpectedChromatogram2;
    }

    public SpecificExperimentChromatogram GetMS2TotalIonChromatogram(int experimentID, ChromatogramRange chromatogramRange) {
        return ExpectedChromatogram3;
    }

    public ExtractedIonChromatogram GetMS2ExtractedIonChromatogram(int experimentID, MzRange product, ChromatogramRange chromatogramRange) {
        return ExpectedChromatogram;
    }
}

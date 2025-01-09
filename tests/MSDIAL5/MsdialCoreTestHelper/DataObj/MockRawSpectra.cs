using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj.Tests.Helper;

public class MockRawSpectra : IRawSpectra
{
    public ExtractedIonChromatogram ExpectedChromatogram { get; set; }
    public Chromatogram ExpectedChromatogram2 { get; set; }
    public SpecificExperimentChromatogram ExpectedChromatogram3 { get; set; }

    Task<ExtractedIonChromatogram> IRawSpectra.GetMS2ExtractedIonChromatogramAsync(MzRange precursor, MzRange product, ChromatogramRange chromatogramRange, CancellationToken token)
    {
        return Task.FromResult(ExpectedChromatogram);
    }

    Task<Chromatogram> IRawSpectra.GetDriftChromatogramByScanRtMzAsync(int scanID, float rt, float rtWidth, float mz, float mztol, CancellationToken token) {
        throw new NotImplementedException();
    }

    Task<Chromatogram> IRawSpectra.GetMS1BasePeakChromatogramAsync(ChromatogramRange chromatogramRange, CancellationToken token) {
        throw new NotImplementedException();
    }

    Task<Chromatogram> IRawSpectra.GetMS1ExtractedChromatogramByHighestBasePeakMzAsync(IEnumerable<ISpectrumPeak> peaks, double tolerance, ChromatogramRange chromatogramRange, CancellationToken token) {
        throw new NotImplementedException();
    }

    IEnumerable<ExtractedIonChromatogram> IRawSpectra.GetMS1ExtractedChromatograms(IEnumerable<double> mzs, double tolerance, ChromatogramRange chromatogramRange) {
        throw new NotImplementedException();
    }

    Task<ExtractedIonChromatogram> IRawSpectra.GetMS1ExtractedChromatogramAsync(MzRange mzRange, ChromatogramRange chromatogramRange, CancellationToken token) {
        throw new NotImplementedException();
    }

    Task<Chromatogram> IRawSpectra.GetMS1TotalIonChromatogramAsync(ChromatogramRange chromatogramRange, CancellationToken token) {
        throw new NotImplementedException();
    }

    Task<PeakMs2Spectra> IRawSpectra.GetPeakMs2SpectraAsync(ChromatogramPeakFeature rtPeakFeature, double ms2Tolerance, AcquisitionType acquisitionType, DriftTime driftTime, CancellationToken token) {
        throw new NotImplementedException();
    }

    public Task<Chromatogram> GetMS2TotalIonChromatogramAsync(ChromatogramRange chromatogramRange, CancellationToken token) {
        return Task.FromResult(ExpectedChromatogram2);
    }

    public Task<SpecificExperimentChromatogram> GetMS2TotalIonChromatogramAsync(int experimentID, ChromatogramRange chromatogramRange, CancellationToken token) {
        return Task.FromResult(ExpectedChromatogram3);
    }

    public Task<ExtractedIonChromatogram> GetMS2ExtractedIonChromatogramAsync(int experimentID, MzRange product, ChromatogramRange chromatogramRange, CancellationToken token) {
        return Task.FromResult(ExpectedChromatogram);
    }
}

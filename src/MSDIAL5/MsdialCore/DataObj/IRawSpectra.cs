using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj
{
    public interface IRawSpectra
    {
        Chromatogram GetMS1TotalIonChromatogram(ChromatogramRange chromatogramRange);
        Chromatogram GetMS1BasePeakChromatogram(ChromatogramRange chromatogramRange);
        ExtractedIonChromatogram GetMS1ExtractedChromatogram(MzRange mzRange, ChromatogramRange chromatogramRange);
        IEnumerable<ExtractedIonChromatogram> GetMS1ExtractedChromatograms(IEnumerable<double> mzs, double tolerance, ChromatogramRange chromatogramRange);
        Chromatogram GetMS2TotalIonChromatogram(ChromatogramRange chromatogramRange);
        SpecificExperimentChromatogram GetMS2TotalIonChromatogram(int experimentID, ChromatogramRange chromatogramRange);
        ExtractedIonChromatogram GetMS2ExtractedIonChromatogram(int experimentID, MzRange product, ChromatogramRange chromatogramRange);
        ExtractedIonChromatogram GetMS2ExtractedIonChromatogram(MzRange precursor, MzRange product, ChromatogramRange chromatogramRange);
        Chromatogram GetMS1ExtractedChromatogramByHighestBasePeakMz(IEnumerable<ISpectrumPeak> peaks, double tolerance, ChromatogramRange chromatogramRange);
        Chromatogram GetDriftChromatogramByScanRtMz(int scanID, float rt, float rtWidth, float mz, float mztol);
        PeakMs2Spectra GetPeakMs2Spectra(ChromatogramPeakFeature rtPeakFeature, double ms2Tolerance, AcquisitionType acquisitionType, DriftTime driftTime);
    }
}

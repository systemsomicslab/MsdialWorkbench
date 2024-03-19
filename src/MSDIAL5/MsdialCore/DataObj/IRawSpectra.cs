using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj
{
    public interface IRawSpectra
    {
        Chromatogram GetMs1ExtractedChromatogram(double mz, double tolerance, ChromatogramRange chromatogramRange);
        Chromatogram GetMs1TotalIonChromatogram(ChromatogramRange chromatogramRange);
        Chromatogram GetMs1BasePeakChromatogram(ChromatogramRange chromatogramRange);
        Chromatogram GetMs1ExtractedChromatogramByHighestBasePeakMz(IEnumerable<ISpectrumPeak> peaks, double tolerance, ChromatogramRange chromatogramRange);
        Chromatogram GetDriftChromatogramByScanRtMz(int scanID, float rt, float rtWidth, float mz, float mztol);
        Chromatogram GetMS2TotalIonChromatogram(ChromatogramRange chromatogramRange);
        Chromatogram GetMS2TotalIonChromatogram(ChromatogramRange chromatogramRange, int experimentID);
        ExtractedIonChromatogram GetProductIonChromatogram(MzRange precursor, MzRange product, ChromatogramRange chromatogramRange);
        ExtractedIonChromatogram GetMs1ExtractedChromatogram_temp2(double mz, double tolerance, ChromatogramRange chromatogramRange);
        IEnumerable<ExtractedIonChromatogram> GetMs1ExtractedChromatograms_temp2(IEnumerable<double> mzs, double tolerance, ChromatogramRange chromatogramRange);
        PeakMs2Spectra GetPeakMs2Spectra(ChromatogramPeakFeature rtPeakFeature, double ms2Tolerance, AcquisitionType acquisitionType, DriftTime driftTime);
    }
}

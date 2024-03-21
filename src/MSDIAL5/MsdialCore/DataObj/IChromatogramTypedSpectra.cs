using CompMs.Common.Components;
using CompMs.Common.DataObj;
using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj
{
    internal interface IChromatogramTypedSpectra
    {
        Chromatogram GetMs1ExtractedChromatogram(double mz, double tolerance, double start, double end);
        ExtractedIonChromatogram GetMs1ExtractedChromatogram_temp2(double mz, double tolerance, double start, double end);
        IEnumerable<ExtractedIonChromatogram> GetMs1ExtractedChromatograms_temp2(IEnumerable<double> mzs, double tolerance, double start, double end);
        Chromatogram GetMs1TotalIonChromatogram(double start, double end);
        Chromatogram GetMs1BasePeakChromatogram(double start, double end);
        Chromatogram GetMs2TotalIonChromatogram(ChromatogramRange chromatogramRange);
        SpecificExperimentChromatogram GetMS2TotalIonChromatogram(ChromatogramRange chromatogramRange, int experimentID);
        ExtractedIonChromatogram GetMS2ExtractedIonChromatogram(MzRange product, ChromatogramRange chromatogramRange, int experimentID);
        ExtractedIonChromatogram GetProductIonChromatogram(MzRange precursor, MzRange product, ChromatogramRange chromatogramRange);
    }
}

using CompMs.Common.Components;

namespace CompMs.MsdialCore.DataObj
{
    internal interface IChromatogramTypedSpectra
    {
        Chromatogram GetMs1ExtractedChromatogram(double mz, double tolerance, double start, double end);
        Chromatogram_temp2 GetMs1ExtractedChromatogram_temp2(double mz, double tolerance, double start, double end);
        Chromatogram GetMs1TotalIonChromatogram(double start, double end);
        Chromatogram GetMs1BasePeakChromatogram(double start, double end);
    }
}

using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj
{
    public interface IRawSpectra
    {
        Task<Chromatogram> GetMS1TotalIonChromatogramAsync(ChromatogramRange chromatogramRange, CancellationToken token);
        Task<Chromatogram> GetMS1BasePeakChromatogramAsync(ChromatogramRange chromatogramRange, CancellationToken token);
        Task<ExtractedIonChromatogram> GetMS1ExtractedChromatogramAsync(MzRange mzRange, ChromatogramRange chromatogramRange, CancellationToken token);
        IEnumerable<ExtractedIonChromatogram> GetMS1ExtractedChromatograms(IEnumerable<double> mzs, double tolerance, ChromatogramRange chromatogramRange);
        Task<Chromatogram> GetMS2TotalIonChromatogramAsync(ChromatogramRange chromatogramRange, CancellationToken token);
        Task<SpecificExperimentChromatogram> GetMS2TotalIonChromatogramAsync(int experimentID, ChromatogramRange chromatogramRange, CancellationToken token);
        Task<ExtractedIonChromatogram> GetMS2ExtractedIonChromatogramAsync(int experimentID, MzRange product, ChromatogramRange chromatogramRange, CancellationToken token);
        Task<ExtractedIonChromatogram> GetMS2ExtractedIonChromatogramAsync(MzRange precursor, MzRange product, ChromatogramRange chromatogramRange, CancellationToken token);
        Task<Chromatogram> GetMS1ExtractedChromatogramByHighestBasePeakMzAsync(IEnumerable<ISpectrumPeak> peaks, double tolerance, ChromatogramRange chromatogramRange, CancellationToken token);
        Task<Chromatogram> GetDriftChromatogramByScanRtMzAsync(int scanID, float rt, float rtWidth, float mz, float mztol, CancellationToken token);
        Task<PeakMs2Spectra> GetPeakMs2SpectraAsync(ChromatogramPeakFeature rtPeakFeature, double ms2Tolerance, AcquisitionType acquisitionType, DriftTime driftTime, CancellationToken token);
    }
}

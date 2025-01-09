using CompMs.Common.Components;
using CompMs.Common.DataObj;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj
{
    internal interface IChromatogramTypedSpectra
    {
        Task<ExtractedIonChromatogram> GetMS1ExtractedChromatogramAsync(double mz, double tolerance, double start, double end, CancellationToken token);
        IEnumerable<ExtractedIonChromatogram> GetMS1ExtractedChromatograms(IEnumerable<double> mzs, double tolerance, double start, double end);
        Task<Chromatogram> GetMS1TotalIonChromatogramAsync(double start, double end, CancellationToken token);
        Task<Chromatogram> GetMS1BasePeakChromatogramAsync(double start, double end, CancellationToken token);
        Task<Chromatogram> GetMS2TotalIonChromatogramAsync(ChromatogramRange chromatogramRange, CancellationToken token);
        Task<SpecificExperimentChromatogram> GetMS2TotalIonChromatogramAsync(ChromatogramRange chromatogramRange, int experimentID, CancellationToken token);
        Task<ExtractedIonChromatogram> GetMS2ExtractedIonChromatogramAsync(MzRange product, ChromatogramRange chromatogramRange, int experimentID, CancellationToken token);
        Task<ExtractedIonChromatogram> GetProductIonChromatogramAsync(MzRange precursor, MzRange product, ChromatogramRange chromatogramRange, CancellationToken token);
    }
}

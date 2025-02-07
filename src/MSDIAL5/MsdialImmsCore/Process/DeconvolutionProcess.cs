using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialImmsCore.Algorithm;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.Raw.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialImmsCore.Process;

internal sealed class DeconvolutionProcess
{
    private readonly IMsdialDataStorage<MsdialImmsParameter> _storage;

    public DeconvolutionProcess(IMsdialDataStorage<MsdialImmsParameter> storage) {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public async Task<MSDecResultCollection[]> DeconvoluteAsync(AnalysisFileBean file, IDataProvider provider, IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, ChromatogramPeaksDataSummaryDto summary, IProgress<int>? progress, CancellationToken token = default) {
        var targetCE2MSDecResults = await SpectrumDeconvolutionAsync(file, provider, chromPeakFeatures, summary, _storage.Parameter, _storage.IupacDatabase, progress, token).ConfigureAwait(false);
        return targetCE2MSDecResults.Select(kvp => new MSDecResultCollection(kvp.Value, kvp.Key)).ToArray();
    }

    private static async Task<Dictionary<double, List<MSDecResult>>> SpectrumDeconvolutionAsync(
        AnalysisFileBean file,
        IDataProvider provider,
        IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
        ChromatogramPeaksDataSummaryDto summary,
        MsdialImmsParameter parameter,
        IupacDatabase iupac,
        IProgress<int>? progress,
        CancellationToken token = default) {

        var targetCE2MSDecResults = new Dictionary<double, List<MSDecResult>>();
        var initial_msdec = 30.0;
        var max_msdec = 30.0;
        Ms2Dec ms2Dec = new Ms2Dec();
        if (MsmsAcquisition.GetOrDefault(file.AcquisitionType).MultipleCollisionEnergy) {
            var ceList = provider.LoadCollisionEnergyTargets();
            var max_msdec_aif = max_msdec / ceList.Count;
            for (int i = 0; i < ceList.Count; i++) {
                var targetCE = Math.Round(ceList[i], 2); // must be rounded by 2 decimal points
                if (targetCE <= 0) {
                    Console.WriteLine("No correct CE information in AIF-MSDEC");
                    continue;
                }
                var initial_msdec_aif = initial_msdec + max_msdec_aif * i;
                MsdialCore.Utility.ReportProgress reporter = MsdialCore.Utility.ReportProgress.FromLength(progress, initial_msdec_aif, max_msdec_aif);
                targetCE2MSDecResults[targetCE] = await ms2Dec.GetMS2DecResultsAsync(file, provider, chromPeakFeatures, parameter, summary, iupac, targetCE, parameter.NumThreads, reporter, token).ConfigureAwait(false);
            }
        }
        else {
            var targetCE = Math.Round(provider.GetMinimumCollisionEnergy(), 2);
            MsdialCore.Utility.ReportProgress reporter = MsdialCore.Utility.ReportProgress.FromLength(progress, initial_msdec, max_msdec);
            targetCE2MSDecResults[targetCE] = await ms2Dec.GetMS2DecResultsAsync(file, provider, chromPeakFeatures, parameter, summary, iupac, -1, parameter.NumThreads, reporter, token).ConfigureAwait(false);
        }
        return targetCE2MSDecResults;
    }
}

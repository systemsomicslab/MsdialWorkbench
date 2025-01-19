using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm;
using CompMs.Raw.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialLcMsApi.Process;

internal sealed class PeakPickProcess
{
    private readonly IMsdialDataStorage<MsdialLcmsParameter> _storage;

    public PeakPickProcess(IMsdialDataStorage<MsdialLcmsParameter> storage) {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public async Task<ChromatogramPeakFeatureCollection> PickAsync(AnalysisFileBean file, IDataProvider provider, IProgress<int>? progress, CancellationToken token = default) {
        var chromPeakFeatures = await (new PeakSpotting(file, 0, 30)).RunAsync(provider, _storage.Parameter, progress, token).ConfigureAwait(false);
        IsotopeEstimator.Process(chromPeakFeatures, _storage.Parameter, _storage.IupacDatabase);
        return new ChromatogramPeakFeatureCollection(chromPeakFeatures);
    }
}

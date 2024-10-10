using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm;
using System;
using System.Threading;

namespace CompMs.MsdialLcMsApi.Process;

internal sealed class PeakPickProcess
{
    private readonly IMsdialDataStorage<MsdialLcmsParameter> _storage;

    public PeakPickProcess(IMsdialDataStorage<MsdialLcmsParameter> storage) {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public ChromatogramPeakFeatureCollection Pick(AnalysisFileBean file, IDataProvider provider, IProgress<int>? progress, CancellationToken token) {
        var chromPeakFeatures = new PeakSpotting(file, 0, 30).Run(provider, _storage.Parameter, progress, token);
        IsotopeEstimator.Process(chromPeakFeatures, _storage.Parameter, _storage.IupacDatabase);
        return new ChromatogramPeakFeatureCollection(chromPeakFeatures);
    }
}

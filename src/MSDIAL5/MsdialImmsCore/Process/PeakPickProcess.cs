using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialImmsCore.Algorithm;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.Raw.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialImmsCore.Process;

public sealed class PeakPickProcess
{
    private readonly IMsdialDataStorage<MsdialImmsParameter> _storage;

    public PeakPickProcess(IMsdialDataStorage<MsdialImmsParameter> storage) {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public async Task<ChromatogramPeakFeatureCollection> PickAsync(AnalysisFileBean file, IDataProvider provider, IProgress<int>? progress, CancellationToken token = default) {
        var parameter = _storage.Parameter;
        parameter.FileID2CcsCoefficients.TryGetValue(file.AnalysisFileId, out var coeff);
        var chromPeakFeatures = await (new PeakSpotting(parameter)).RunAsync(file, provider, ReportProgress.FromLength(progress, 0, 30)).ConfigureAwait(false);
        IsotopeEstimator.Process(chromPeakFeatures.Items, parameter, _storage.IupacDatabase, true);
        CcsEstimator.Process(chromPeakFeatures.Items, parameter, parameter.IonMobilityType, coeff, parameter.IsAllCalibrantDataImported);
        return chromPeakFeatures;
    }
}

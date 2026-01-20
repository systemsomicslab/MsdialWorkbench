using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.Raw.Abstractions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialImmsImagingCore.Process;

public sealed class FileProcess
{
    private readonly MsdialImmsCore.Process.FileProcess _processor;
    private readonly IImagingDataProviderFactory<AnalysisFileBean> _providerFactory;

    public FileProcess(
        IMsdialDataStorage<MsdialImmsParameter> storage,
        IImagingDataProviderFactory<AnalysisFileBean> dataProviderFactory,
        IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>? mspAnnotator,
        IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>? textDBAnnotator,
        IMatchResultEvaluator<MsScanMatchResult> evaluator) {
        _processor = new MsdialImmsCore.Process.FileProcess(storage, dataProviderFactory, mspAnnotator, textDBAnnotator, evaluator);
        _providerFactory = dataProviderFactory;
    }

    public async Task RunAsync(AnalysisFileBean file, Action<int>? reportAction = null, CancellationToken token = default) {
        var provider = _providerFactory.Create(file);
        await _processor.RunAsync(file, provider, reportAction is not null ? new Progress<int>(reportAction) : null, token).ConfigureAwait(false);

        var chromPeakFeatures = await file.LoadChromatogramPeakFeatureCollectionAsync(token).ConfigureAwait(false);
        var _elements = chromPeakFeatures.Items.Select(item => new Raw2DElement(item.PeakFeature.Mass, item.PeakFeature.ChromXsTop.Drift.Value)).ToList();
        var pixels = provider.GetRawPixelFeatures(_elements, provider.GetMaldiFrames(), isNewProcess: true);
    }

    public async Task RunAsyncTest(AnalysisFileBean file, Action<int>? reportAction = null, CancellationToken token = default) {
        var provider = _providerFactory.Create(file);
        await _processor.RunAsync(file, provider, reportAction is not null ? new Progress<int>(reportAction) : null, token).ConfigureAwait(false);

        var chromPeakFeatures = await file.LoadChromatogramPeakFeatureCollectionAsync(token).ConfigureAwait(false);
        var _elements = chromPeakFeatures.Items.Select(item => new Raw2DElement(item.PeakFeature.Mass, item.PeakFeature.ChromXsTop.Drift.Value)).ToList();
        var pixels = provider.GetRawPixelFeatures(_elements, provider.GetMaldiFrames(), isNewProcess: true);

    }
}

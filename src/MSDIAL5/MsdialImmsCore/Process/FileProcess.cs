using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialImmsCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialImmsCore.Process;

public sealed class FileProcess : IFileProcessor
{
    private readonly PeakPickProcess _peakPickProcess;
    private readonly DeconvolutionProcess _deconvolutionProcess;
    private readonly PeakAnnotationProcess _peakAnnotationProcess;
    private readonly IDataProviderFactory<AnalysisFileBean> _dataProviderFactory;

    public FileProcess(
        IMsdialDataStorage<MsdialImmsParameter> storage,
        IDataProviderFactory<AnalysisFileBean> dataProviderFactory,
        IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>? mspAnnotator,
        IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>? textDBAnnotator,
        IMatchResultEvaluator<MsScanMatchResult> evaluator) {
        if (storage is null) {
            throw new ArgumentNullException(nameof(storage));
        }
        if (evaluator is null) {
            throw new ArgumentNullException(nameof(evaluator));
        }

        _peakPickProcess = new PeakPickProcess(storage);
        _deconvolutionProcess = new DeconvolutionProcess(storage);
        _peakAnnotationProcess = new PeakAnnotationProcess(storage, evaluator, mspAnnotator, textDBAnnotator);
        _dataProviderFactory = dataProviderFactory;
    }

    public async Task RunAsync(AnalysisFileBean file, ProcessOption option, IProgress<int>? reporter, CancellationToken token = default) {
        if (!option.HasFlag(ProcessOption.PeakSpotting) && !option.HasFlag(ProcessOption.Identification)) {
            return;
        }
        
        var action = reporter is not null ? reporter.Report : (Action<int>)null;
        var provider = new Lazy<IDataProvider>(() => _dataProviderFactory.Create(file));
        var (chromPeakFeatures, mSDecResultCollections) = option.HasFlag(ProcessOption.PeakSpotting)
            ? GetPeakAndScans(file, provider, reporter, token)
            : await LoadPeakAndScans(file).ConfigureAwait(false);

        if (option.HasFlag(ProcessOption.Identification)) {
            // annotations
            Console.WriteLine("Annotation started");
            _peakAnnotationProcess.Annotate(file, provider.Value, chromPeakFeatures.Items, mSDecResultCollections, reporter, token);
        }

        // file save
        await SaveToFileAsync(file, chromPeakFeatures, mSDecResultCollections).ConfigureAwait(false);

        reporter?.Report(100);
    }

    private async Task<(ChromatogramPeakFeatureCollection, MSDecResultCollection[])> LoadPeakAndScans(AnalysisFileBean file) {
        var peakTask = file.LoadChromatogramPeakFeatureCollectionAsync();
        var resultsTask = Task.WhenAll(MSDecResultCollection.DeserializeAsync(file));

        var chromPeakFeatures = await peakTask.ConfigureAwait(false);
        chromPeakFeatures.ClearMatchResultProperties();
        var mSDecResultCollections = await resultsTask.ConfigureAwait(false);

        return (chromPeakFeatures, mSDecResultCollections);
    }

    private (ChromatogramPeakFeatureCollection, MSDecResultCollection[]) GetPeakAndScans(AnalysisFileBean file, Lazy<IDataProvider> provider, IProgress<int>? progress, CancellationToken token) {
        Console.WriteLine("Peak picking started");
        var chromPeakFeatures = _peakPickProcess.Pick(file, provider.Value, progress);

        var summary = ChromFeatureSummarizer.GetChromFeaturesSummary(provider.Value, chromPeakFeatures.Items);
        file.ChromPeakFeaturesSummary = summary;

        Console.WriteLine("Deconvolution started");
        var mSDecResultCollections = _deconvolutionProcess.Deconvolute(file, provider.Value, chromPeakFeatures.Items, summary, progress, token);

        return (chromPeakFeatures, mSDecResultCollections);
    }

    public async Task RunAsync(AnalysisFileBean file, IDataProvider provider, IProgress<int>? progress, CancellationToken token = default) {
        Console.WriteLine("Peak picking started");
        var chromPeakFeatures = _peakPickProcess.Pick(file, provider, progress);

        var summary = ChromFeatureSummarizer.GetChromFeaturesSummary(provider, chromPeakFeatures.Items);
        file.ChromPeakFeaturesSummary = summary;

        Console.WriteLine("Deconvolution started");
        var mSDecResultCollections = _deconvolutionProcess.Deconvolute(file, provider, chromPeakFeatures.Items, summary, progress, token);

        // annotations
        Console.WriteLine("Annotation started");
        _peakAnnotationProcess.Annotate(file, provider, chromPeakFeatures.Items, mSDecResultCollections, progress, token);

        // file save
        await SaveToFileAsync(file, chromPeakFeatures, mSDecResultCollections).ConfigureAwait(false);

        progress?.Report(100);
    }

    private static Task SaveToFileAsync(AnalysisFileBean file, ChromatogramPeakFeatureCollection chromPeakFeatures, IReadOnlyList<MSDecResultCollection> mSDecResultCollections) {
        Task t1, t2;

        t1 = chromPeakFeatures.SerializeAsync(file);

        if (mSDecResultCollections.Count == 1) {
            t2 = mSDecResultCollections[0].SerializeAsync(file);
        }
        else {
            file.DeconvolutionFilePathList.Clear();
            t2 = Task.WhenAll(mSDecResultCollections.Select(mSDecResultCollection => mSDecResultCollection.SerializeWithCEAsync(file)));
        }

        return Task.WhenAll(t1, t2);
    }
}

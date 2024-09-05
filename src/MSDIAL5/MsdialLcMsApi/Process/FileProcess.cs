using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialLcMsApi.Process;

public sealed class FileProcess : IFileProcessor {
    private readonly IDataProviderFactory<AnalysisFileBean> _factory;
    private readonly PeakPickProcess _peakPickProcess;
    private readonly SpectrumDeconvolutionProcess _spectrumDeconvolutionProcess;
    private readonly PeakAnnotationProcess _peakAnnotationProcess;

    public FileProcess(IDataProviderFactory<AnalysisFileBean> factory, IMsdialDataStorage<MsdialLcmsParameter> storage, IAnnotationProcess annotationProcess, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
        if (storage is null) {
            throw new ArgumentNullException(nameof(storage));
        }

        if (annotationProcess is null) {
            throw new ArgumentNullException(nameof(annotationProcess));
        }

        if (evaluator is null) {
            throw new ArgumentNullException(nameof(evaluator));
        }

        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _peakPickProcess = new PeakPickProcess(storage);
        _spectrumDeconvolutionProcess = new SpectrumDeconvolutionProcess(storage);
        _peakAnnotationProcess = new PeakAnnotationProcess(annotationProcess, storage, evaluator);
    }

    public async Task RunAsync(AnalysisFileBean analysisFile, ProcessOption option, IProgress<int>? progress, CancellationToken token = default) {
        if (!option.HasFlag(ProcessOption.PeakSpotting) && !option.HasFlag(ProcessOption.Identification)) {
            return;
        }

        var provider = _factory.Create(analysisFile);
        var (chromPeakFeatures, mSDecResultCollections) = option.HasFlag(ProcessOption.PeakSpotting)
            ? FindPeakAndScans(analysisFile, provider, progress, token)
            : await LoadPeakAndScans(analysisFile, token).ConfigureAwait(false);

        if (option.HasFlag(ProcessOption.Identification)) {
            // annotations
            token.ThrowIfCancellationRequested();
            Console.WriteLine("Annotation started");
            await _peakAnnotationProcess.AnnotateAsync(analysisFile, mSDecResultCollections, chromPeakFeatures.Items, provider, progress, token).ConfigureAwait(false);
        }

        // file save
        token.ThrowIfCancellationRequested();
        await SaveToFileAsync(analysisFile, chromPeakFeatures, mSDecResultCollections).ConfigureAwait(false);
        progress?.Report(100);
    }

    private async Task<(ChromatogramPeakFeatureCollection, MSDecResultCollection[])> LoadPeakAndScans(AnalysisFileBean analysisFile, CancellationToken token) {
        var peakTask = analysisFile.LoadChromatogramPeakFeatureCollectionAsync(token);
        var resultsTask = Task.WhenAll(MSDecResultCollection.DeserializeAsync(analysisFile, token));

        var chromPeakFeatures = await peakTask.ConfigureAwait(false);
        chromPeakFeatures.ClearMatchResultProperties();
        var mSDecResultCollections = await resultsTask.ConfigureAwait(false);
        return (chromPeakFeatures, mSDecResultCollections);
    }

    private (ChromatogramPeakFeatureCollection, MSDecResultCollection[]) FindPeakAndScans(AnalysisFileBean analysisFile, IDataProvider provider, IProgress<int>? progress, CancellationToken token) {
        // feature detections
        token.ThrowIfCancellationRequested();
        Console.WriteLine("Peak picking started");
        var chromPeakFeatures = _peakPickProcess.Pick(analysisFile, provider, progress, token);

        var summaryDto = ChromFeatureSummarizer.GetChromFeaturesSummary(provider, chromPeakFeatures.Items);
        analysisFile.ChromPeakFeaturesSummary = summaryDto;

        // chrom deconvolutions
        token.ThrowIfCancellationRequested();
        Console.WriteLine("Deconvolution started");
        var mSDecResultCollections = _spectrumDeconvolutionProcess.Deconvolute(provider, chromPeakFeatures.Items, analysisFile, summaryDto, progress, token);
        return (chromPeakFeatures, mSDecResultCollections.ToArray());
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

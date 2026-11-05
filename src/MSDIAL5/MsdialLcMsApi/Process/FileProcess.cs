using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Algorithm.Annotation.LipidRt;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Concurrent;
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
    private readonly MsdialLcmsParameter _parameter;
    private readonly ConcurrentQueue<(string File, LipidRtUnitRun Run)> _lipidRtFailures = new ConcurrentQueue<(string, LipidRtUnitRun)>();

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
        _parameter = storage.Parameter;
        _peakPickProcess = new PeakPickProcess(storage);
        _spectrumDeconvolutionProcess = new SpectrumDeconvolutionProcess(storage);
        _peakAnnotationProcess = new PeakAnnotationProcess(annotationProcess, storage, evaluator);
    }

    /// <summary>
    /// The analysis files on which the lipid RT check (hook A) failed in this processor's runs, with what it reported. A
    /// failure does not stop the processing: the file is saved without the check's verdicts and tags. The Console prints
    /// it; a caller without a console, such as the GUI, reads it here to tell the person.
    /// </summary>
    public IReadOnlyList<(string File, LipidRtUnitRun Run)> LipidRtFailures => _lipidRtFailures.ToArray();

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

            // Hook A of the lipid RT check: this file's peaks against each other, on this file's own RT axis, after the
            // annotation and before the save, so the Console and the GUI both get it. It reads only this file's peaks and
            // the shared parameter, and writes only this file's peaks, so it is safe inside the per-file parallel loop.
            token.ThrowIfCancellationRequested();
            var lipidRt = LipidRtCheckHooks.RunOnPeaks(chromPeakFeatures.Items, _parameter);
            if (lipidRt.Ran || lipidRt.Error != null || lipidRt.Tags.Withdrawn > 0) {
                Console.WriteLine($"Lipid RT check ({analysisFile.AnalysisFileName}): {lipidRt.Describe()}");
            }
            if (lipidRt.Error != null) {
                _lipidRtFailures.Enqueue((analysisFile.AnalysisFileName, lipidRt));
            }
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

    internal static async Task SaveToFileAsync(AnalysisFileBean file, ChromatogramPeakFeatureCollection chromPeakFeatures, IReadOnlyList<MSDecResultCollection> mSDecResultCollections) {
        Task t1, t2;

        t1 = chromPeakFeatures.SerializeAsync(file);

        if (mSDecResultCollections.Count == 1) {
            t2 = mSDecResultCollections[0].SerializeAsync(file);
        }
        else {
            file.DeconvolutionFilePathList.Clear();
            t2 = Task.WhenAll(mSDecResultCollections.Select(mSDecResultCollection => mSDecResultCollection.SerializeWithCEAsync(file)));
        }

        await Task.WhenAll(t1, t2).ConfigureAwait(false);
        if (mSDecResultCollections.Count > 1) {
            // Written in completion order; readers that fall back to "the first" CE file
            // (MSDecLoader, the alignment's representative spectra) need one fixed order.
            file.DeconvolutionFilePathList = mSDecResultCollections
                .OrderBy(collection => collection.CollisionEnergy)
                .Select(collection => collection.GetDeconvolutionFilePathWithCE(file))
                .ToList();
        }
    }
}

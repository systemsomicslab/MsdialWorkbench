using CompMs.Common.DataObj.Result;
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

namespace CompMs.MsdialLcMsApi.Process
{
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

        public Task RunAsync(AnalysisFileBean file, IProgress<int> reportAction, CancellationToken token) {
            return RunAsync(file, reportAction is null ? (Action<int>)null : reportAction.Report, token);
        }

        public Task RunAsync(AnalysisFileBean file, Action<int> reportAction, CancellationToken token = default) {
            var provider = _factory.Create(file);
            return RunAsync(file, provider, reportAction, token);
        }

        public async Task RunAsync(AnalysisFileBean file, IDataProvider provider, Action<int> reportAction, CancellationToken token = default) {
            // feature detections
            token.ThrowIfCancellationRequested();
            Console.WriteLine("Peak picking started");
            var chromPeakFeatures = _peakPickProcess.Pick(file, provider, token, reportAction);

            var summaryDto = ChromFeatureSummarizer.GetChromFeaturesSummary(provider, chromPeakFeatures.Items);
            file.ChromPeakFeaturesSummary = summaryDto;

            // chrom deconvolutions
            token.ThrowIfCancellationRequested();
            Console.WriteLine("Deconvolution started");
            var mSDecResultCollections = _spectrumDeconvolutionProcess.Deconvolute(provider, chromPeakFeatures.Items, file, summaryDto, reportAction, token);

            // annotations
            token.ThrowIfCancellationRequested();
            Console.WriteLine("Annotation started");
            _peakAnnotationProcess.Annotate(file, mSDecResultCollections, chromPeakFeatures.Items, provider, token, reportAction);

            // file save
            token.ThrowIfCancellationRequested();
            await SaveToFileAsync(file, chromPeakFeatures, mSDecResultCollections).ConfigureAwait(false);
            reportAction?.Invoke(100);
        }

        public Task AnnotateAsync(AnalysisFileBean file, IProgress<int> reportAction, CancellationToken token = default) {
            return AnnotateAsync(file, reportAction is null ? (Action<int>)null : reportAction.Report, token);
        }
        public async Task AnnotateAsync(AnalysisFileBean file, Action<int> reportAction, CancellationToken token = default) {
            var peakTask = file.LoadChromatogramPeakFeatureCollectionAsync(token);
            var resultsTask = Task.WhenAll(MSDecResultCollection.DeserializeAsync(file, token));
            var provider = _factory.Create(file);

            // annotations
            token.ThrowIfCancellationRequested();
            Console.WriteLine("Annotation started");
            var chromPeakFeatures = await peakTask.ConfigureAwait(false);
            chromPeakFeatures.ClearMatchResultProperties();
            var mSDecResultCollections = await resultsTask.ConfigureAwait(false);
            _peakAnnotationProcess.Annotate(file, mSDecResultCollections, chromPeakFeatures.Items, provider, token, reportAction);

            // file save
            token.ThrowIfCancellationRequested();
            await SaveToFileAsync(file, chromPeakFeatures, mSDecResultCollections).ConfigureAwait(false);
            reportAction?.Invoke(100);
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
}

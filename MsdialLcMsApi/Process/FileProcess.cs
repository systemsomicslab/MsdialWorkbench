using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialLcMsApi.Process
{
    public sealed class FileProcess {
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

        public Task RunAllAsync(IEnumerable<AnalysisFileBean> files, IEnumerable<Action<int>> reportActions, int numParallel, Action afterEachRun, CancellationToken token = default) {
            var consumer = new Consumer(files, reportActions, afterEachRun, token);
            return Task.WhenAll(consumer.ConsumeAllAsync(RunAsync, numParallel));
        }

        public Task AnnotateAllAsync(IEnumerable<AnalysisFileBean> files, IEnumerable<Action<int>> reportActions, int numParallel, Action afterEachRun, CancellationToken token = default) {
            var consumer = new Consumer(files, reportActions, afterEachRun, token);
            return Task.WhenAll(consumer.ConsumeAllAsync(AnnotateAsync, numParallel));
        }

        public async Task RunAsync(AnalysisFileBean file, Action<int> reportAction, CancellationToken token = default) {
            var provider = _factory.Create(file);

            // feature detections
            token.ThrowIfCancellationRequested();
            Console.WriteLine("Peak picking started");
            var chromPeakFeatures = _peakPickProcess.Pick(provider, token, reportAction);

            var summaryDto = ChromFeatureSummarizer.GetChromFeaturesSummary(provider, chromPeakFeatures.Items);
            file.ChromPeakFeaturesSummary = summaryDto;

            // chrom deconvolutions
            token.ThrowIfCancellationRequested();
            Console.WriteLine("Deconvolution started");
            var mSDecResultCollections = _spectrumDeconvolutionProcess.Deconvolute(provider, chromPeakFeatures.Items, summaryDto, reportAction, token);

            // annotations
            token.ThrowIfCancellationRequested();
            Console.WriteLine("Annotation started");
            _peakAnnotationProcess.Annotate(mSDecResultCollections, chromPeakFeatures.Items, provider, token, reportAction);

            // file save
            token.ThrowIfCancellationRequested();
            await SaveToFileAsync(file, chromPeakFeatures, mSDecResultCollections).ConfigureAwait(false);
            reportAction?.Invoke(100);
        }

        public async Task AnnotateAsync(AnalysisFileBean file, Action<int> reportAction, CancellationToken token = default) {
            var peakTask = ChromatogramPeakFeatureCollection.LoadAsync(file.PeakAreaBeanInformationFilePath);
            var resultsTask = Task.WhenAll(MSDecResultCollection.DeserializeAsync(file));
            var provider = _factory.Create(file);

            // annotations
            token.ThrowIfCancellationRequested();
            Console.WriteLine("Annotation started");
            var chromPeakFeatures = peakTask.Result;
            var mSDecResultCollections = resultsTask.Result;
            _peakAnnotationProcess.Annotate(mSDecResultCollections, chromPeakFeatures.Items, provider, token, reportAction);

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

        class Consumer {
            private readonly ConcurrentQueue<(AnalysisFileBean File, Action<int> Report)> _queue;
            private readonly Action _afterEachRun;
            private readonly CancellationToken _token;

            public Consumer(IEnumerable<AnalysisFileBean> files, IEnumerable<Action<int>> reportActions, Action afterEachRun, CancellationToken token) {
                _queue = new ConcurrentQueue<(AnalysisFileBean, Action<int>)>(files.Zip(reportActions, (file, report) => (file, report)));
                _afterEachRun = afterEachRun;
                _token = token;
            }

            public async Task ConsumeAsync(Func<AnalysisFileBean, Action<int>, CancellationToken, Task> process) {
                while (_queue.TryDequeue(out var pair)) {
                    await process(pair.File, pair.Report, _token).ConfigureAwait(false);
                    _afterEachRun?.Invoke();
                }
            }

            public Task[] ConsumeAllAsync(Func<AnalysisFileBean, Action<int>, CancellationToken, Task> process, int parallel) {
                var tasks = new Task[parallel];
                for (int i = 0; i < parallel; i++) {
                    tasks[i] = Task.Run(() => ConsumeAsync(process), _token);
                }
                return tasks;
            }
        }
    }
}

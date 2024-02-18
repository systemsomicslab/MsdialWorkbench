using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialImmsCore.Parameter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialImmsCore.Process
{
    public sealed class FileProcess
    {
        private readonly PeakPickProcess _peakPickProcess;
        private readonly DeconvolutionProcess _deconvolutionProcess;
        private readonly PeakAnnotationProcess _peakAnnotationProcess;

        public FileProcess(
            IMsdialDataStorage<MsdialImmsParameter> storage,
            IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> mspAnnotator,
            IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> textDBAnnotator,
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
        }

        public Task RunAllAsync(IEnumerable<AnalysisFileBean> files, IEnumerable<IDataProvider> providers, IEnumerable<Action<int>> reportActions, int numParallel, Action afterEachRun, CancellationToken token = default) {
            var consumer = new Consumer(files, providers, reportActions, afterEachRun, token);
            return Task.WhenAll(consumer.ConsumeAllAsync(RunAsync, numParallel));
        }

        public Task AnnotateAllAsync(IEnumerable<AnalysisFileBean> files, IEnumerable<IDataProvider> providers, IEnumerable<Action<int>> reportActions, int numParallel, Action afterEachRun, CancellationToken token = default) {
            var consumer = new Consumer(files, providers, reportActions, afterEachRun, token);
            return Task.WhenAll(consumer.ConsumeAllAsync(AnnotateAsync, numParallel));
        }

        public async Task RunAsync(AnalysisFileBean file, IDataProvider provider, Action<int> reportAction = null, CancellationToken token = default) {
            Console.WriteLine("Peak picking started");
            var chromPeakFeatures = _peakPickProcess.Pick(file, provider, reportAction);

            var summary = ChromFeatureSummarizer.GetChromFeaturesSummary(provider, chromPeakFeatures.Items);
            file.ChromPeakFeaturesSummary = summary;

            Console.WriteLine("Deconvolution started");
            var mSDecResultCollections = _deconvolutionProcess.Deconvolute(file, provider, chromPeakFeatures.Items, summary, reportAction, token);

            // annotations
            Console.WriteLine("Annotation started");
            _peakAnnotationProcess.Annotate(file, provider, chromPeakFeatures.Items, mSDecResultCollections, reportAction, token);

            // file save
            await SaveToFileAsync(file, chromPeakFeatures, mSDecResultCollections).ConfigureAwait(false);

            reportAction?.Invoke(100);
        }

        public async Task AnnotateAsync(AnalysisFileBean file, IDataProvider provider, Action<int> reportAction = null, CancellationToken token = default) {
            var peakTask = file.LoadChromatogramPeakFeatureCollectionAsync();
            var resultsTask = Task.WhenAll(MSDecResultCollection.DeserializeAsync(file));

            var chromPeakFeatures = await peakTask.ConfigureAwait(false);
            chromPeakFeatures.ClearMatchResultProperties();
            var mSDecResultCollections = await resultsTask.ConfigureAwait(false);
            // annotations
            Console.WriteLine("Annotation started");
            _peakAnnotationProcess.Annotate(file, provider, chromPeakFeatures.Items, mSDecResultCollections, reportAction, token);

            // file save
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
            private readonly ConcurrentQueue<(AnalysisFileBean File, IDataProvider Provider, Action<int> Report)> _queue;
            private readonly Action _afterEachRun;
            private readonly CancellationToken _token;

            public Consumer(IEnumerable<AnalysisFileBean> files, IEnumerable<IDataProvider> providers, IEnumerable<Action<int>> reportActions, Action afterEachRun, CancellationToken token) {
                _queue = new ConcurrentQueue<(AnalysisFileBean, IDataProvider, Action<int>)>(files.Zip(providers, reportActions, (file, provider, report) => (file, provider, report)));
                _afterEachRun = afterEachRun;
                _token = token;
            }

            public async Task ConsumeAsync(Func<AnalysisFileBean, IDataProvider, Action<int>, CancellationToken, Task> process) {
                while (_queue.TryDequeue(out var pair)) {
                    await process(pair.File, pair.Provider, pair.Report, _token).ConfigureAwait(false);
                    _afterEachRun?.Invoke();
                }
            }

            public Task[] ConsumeAllAsync(Func<AnalysisFileBean, IDataProvider, Action<int>, CancellationToken, Task> process, int parallel) {
                var tasks = new Task[parallel];
                for (int i = 0; i < parallel; i++) {
                    tasks[i] = Task.Run(() => ConsumeAsync(process), _token);
                }
                return tasks;
            }
        }
    }
}

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
            return ProcessAllAsync(files, reportActions, numParallel, afterEachRun, token, Run);
        }

        public Task AnnotateAllAsync(IEnumerable<AnalysisFileBean> files, IEnumerable<Action<int>> reportActions, int numParallel, Action afterEachRun, CancellationToken token = default) {
            return ProcessAllAsync(files, reportActions, numParallel, afterEachRun, token, Annotate);
        }

        private Task ProcessAllAsync(IEnumerable<AnalysisFileBean> files, IEnumerable<Action<int>> reportActions, int numParallel, Action afterEachRun, CancellationToken token, Action<AnalysisFileBean, Action<int>, CancellationToken> process) {
            var queue = new ConcurrentQueue<(AnalysisFileBean, Action<int>)>(files.Zip(reportActions, (file, report) => (file, report)));
            var tasks = new Task[numParallel];
            for (int i = 0; i < numParallel; i++) {
                tasks[i] = Task.Run(() => Consume(queue, afterEachRun, token, process));
            }
            return Task.WhenAll(tasks);
        }

        private void Consume(ConcurrentQueue<(AnalysisFileBean File, Action<int> Report)> queue, Action afterEachRun, CancellationToken token, Action<AnalysisFileBean, Action<int>, CancellationToken> process) {
            while (queue.TryDequeue(out var pair)) {
                process(pair.File, pair.Report, token);
                afterEachRun?.Invoke();
            }
        }

        public void Run(AnalysisFileBean file, Action<int> reportAction, CancellationToken token = default) {
            var provider = _factory.Create(file);

            // feature detections
            Console.WriteLine("Peak picking started");
            var chromPeakFeatures = _peakPickProcess.Pick(provider, token, reportAction);

            var summaryDto = ChromFeatureSummarizer.GetChromFeaturesSummary(provider, chromPeakFeatures.Items);
            file.ChromPeakFeaturesSummary = summaryDto;

            // chrom deconvolutions
            Console.WriteLine("Deconvolution started");
            var mSDecResultCollections = _spectrumDeconvolutionProcess.Deconvolute(provider, chromPeakFeatures.Items, summaryDto, reportAction, token);

            // annotations
            Console.WriteLine("Annotation started");
            _peakAnnotationProcess.Annotate(mSDecResultCollections, chromPeakFeatures.Items, provider, token, reportAction);

            // file save
            SaveToFile(file, chromPeakFeatures, mSDecResultCollections);
            reportAction?.Invoke(100);
        }

        public void Annotate(AnalysisFileBean file, Action<int> reportAction, CancellationToken token = default) {
            var chromPeakFeatures = ChromatogramPeakFeatureCollection.LoadAsync(file.PeakAreaBeanInformationFilePath).Result;
            var mSDecResultCollections = Task.WhenAll(MSDecResultCollection.DeserializeAsync(file)).Result;
            var provider = _factory.Create(file);

            // annotations
            Console.WriteLine("Annotation started");
            _peakAnnotationProcess.Annotate(mSDecResultCollections, chromPeakFeatures.Items, provider, token, reportAction);

            // file save
            SaveToFile(file, chromPeakFeatures, mSDecResultCollections);
            reportAction?.Invoke(100);
        }

        private static void SaveToFile(AnalysisFileBean file, ChromatogramPeakFeatureCollection chromPeakFeatures, IReadOnlyList<MSDecResultCollection> mSDecResultCollections) {
            Task t1, t2;

            t1 = chromPeakFeatures.SerializeAsync(file);

            if (mSDecResultCollections.Count == 1) {
                t2 = mSDecResultCollections[0].SerializeAsync(file);
            }
            else {
                file.DeconvolutionFilePathList.Clear();
                t2 = Task.WhenAll(mSDecResultCollections.Select(mSDecResultCollection => mSDecResultCollection.SerializeWithCEAsync(file)));
            }

            Task.WaitAll(t1, t2);
        }
    }
}

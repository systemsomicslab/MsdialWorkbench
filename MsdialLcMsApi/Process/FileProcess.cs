using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
            var queue = new ConcurrentQueue<(AnalysisFileBean, Action<int>)>(files.Zip(reportActions, (file, report) => (file, report)));
            var tasks = new Task[numParallel];
            for (int i = 0; i < numParallel; i++) {
                tasks[i] = Task.Run(() => Consume(queue, afterEachRun, token));
            }
            return Task.WhenAll(tasks);
        }

        private void Consume(ConcurrentQueue<(AnalysisFileBean File, Action<int> Report)> queue, Action afterEachRun, CancellationToken token) {
            while (queue.TryDequeue(out var pair)) {
                Run(pair.File, pair.Report, token);
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
            var targetCE2MSDecResults = _spectrumDeconvolutionProcess.Deconvolute(provider, chromPeakFeatures.Items, summaryDto, reportAction, token);

            // annotations
            Console.WriteLine("Annotation started");
            _peakAnnotationProcess.Annotate(targetCE2MSDecResults, chromPeakFeatures.Items, provider, token, reportAction);

            // file save
            chromPeakFeatures.SerializeAsync(file).Wait();

            var dclfile = file.DeconvolutionFilePath;
            var dclfiles = new List<string>();
            foreach (var ce2msdecs in targetCE2MSDecResults) {
                if (targetCE2MSDecResults.Count == 1) {
                    dclfiles.Add(dclfile);
                    MsdecResultsWriter.Write(dclfile, ce2msdecs.Value);
                }
                else {
                    var suffix = Math.Round(ce2msdecs.Key * 100, 0); // CE 34.50 -> 3450
                    var dclfile_suffix = Path.Combine(Path.GetDirectoryName(dclfile), Path.GetFileNameWithoutExtension(dclfile) + "_" + suffix + ".dcl");
                    dclfiles.Add(dclfile_suffix);
                    MsdecResultsWriter.Write(dclfile_suffix, ce2msdecs.Value);
                }
            }
            reportAction?.Invoke(100);
        }
    }
}

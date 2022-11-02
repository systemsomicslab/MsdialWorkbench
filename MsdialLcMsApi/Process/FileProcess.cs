using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
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
            return ProcessAllAsync(files, reportActions, numParallel, afterEachRun, token, Run);
        }

        public Task AnnotateAllAsync(IEnumerable<AnalysisFileBean> files, IEnumerable<Action<int>> reportActions, int numParallel, Action afterEachRun, CancellationToken token = default) {
            return ProcessAllAsync(files, reportActions, numParallel, afterEachRun, token, Annotate);
        }

        public Task ProcessAllAsync(IEnumerable<AnalysisFileBean> files, IEnumerable<Action<int>> reportActions, int numParallel, Action afterEachRun, CancellationToken token, Action<AnalysisFileBean, Action<int>, CancellationToken> process) {
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
            var targetCE2MSDecResults = _spectrumDeconvolutionProcess.Deconvolute(provider, chromPeakFeatures.Items, summaryDto, reportAction, token);

            // annotations
            Console.WriteLine("Annotation started");
            _peakAnnotationProcess.Annotate(targetCE2MSDecResults, chromPeakFeatures.Items, provider, token, reportAction);

            // file save
            SaveToFile(chromPeakFeatures, file, targetCE2MSDecResults);
            reportAction?.Invoke(100);
        }

        public void Annotate(AnalysisFileBean file, Action<int> reportAction, CancellationToken token = default) {
            var (chromPeakFeatures, targetCE2MSDecResults) = LoadFromFile(file);
            var provider = _factory.Create(file);

            // annotations
            Console.WriteLine("Annotation started");
            _peakAnnotationProcess.Annotate(targetCE2MSDecResults, chromPeakFeatures.Items, provider, token, reportAction);

            // file save
            SaveToFile(chromPeakFeatures, file, targetCE2MSDecResults);
            reportAction?.Invoke(100);
        }

        private static (ChromatogramPeakFeatureCollection, Dictionary<double, List<MSDecResult>>) LoadFromFile(AnalysisFileBean file) {
            var peaks = ChromatogramPeakFeatureCollection.LoadAsync(file.PeakAreaBeanInformationFilePath).Result;
            var decResults = new Dictionary<double, List<MSDecResult>>();
            if (file.DeconvolutionFilePathList.Count == 1) {
                var dclfile = file.DeconvolutionFilePath;
                decResults[-1] = MsdecResultsReader.ReadMSDecResults(dclfile, out var _, out var _);
            }
            else {
                foreach (var dclfile in file.DeconvolutionFilePathList) {
                    var fileName = Path.GetFileNameWithoutExtension(dclfile);
                    var suffix = fileName.Split('_').Last();
                    var ce = double.TryParse(suffix, out var ce_) ? ce_ / 100 : -1d; // 3450 -> CE 34.50
                    decResults[ce] = MsdecResultsReader.ReadMSDecResults(dclfile, out var _, out var _);
                }
            }
            return (peaks, decResults);
        }

        private static void SaveToFile(ChromatogramPeakFeatureCollection chromPeakFeatures, AnalysisFileBean file, Dictionary<double, List<MSDecResult>> targetCE2MSDecResults) {
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
        }
    }
}

using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm
{
    public interface IFileProcessor {
        Task RunAsync(AnalysisFileBean file, IProgress<int> reportAction, CancellationToken token);
        Task AnnotateAsync(AnalysisFileBean file, IProgress<int> reportAction, CancellationToken token);
    }

    public sealed class ProcessRunner
    {
        private readonly IFileProcessor _processor;
        private readonly IReadOnlyList<AnalysisFileBean> _analysisFiles;

        public ProcessRunner(IFileProcessor processor, IReadOnlyList<AnalysisFileBean> analysisFiles) {
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _analysisFiles = analysisFiles;
        }

        public Task RunAllAsync(IEnumerable<IProgress<int>> reportActions, int numParallel, Action afterEachRun, CancellationToken token) {
            var consumer = new Consumer(_analysisFiles, reportActions, afterEachRun, token);
            return Task.WhenAll(consumer.ConsumeAllAsync(_processor.RunAsync, numParallel));
        }

        public Task AnnotateAllAsync(IEnumerable<IProgress<int>> reportActions, int numParallel, Action afterEachRun, CancellationToken token) {
            var consumer = new Consumer(_analysisFiles, reportActions, afterEachRun, token);
            return Task.WhenAll(consumer.ConsumeAllAsync(_processor.AnnotateAsync, numParallel));
        }

        sealed class Consumer {
            private readonly ConcurrentQueue<(AnalysisFileBean File, IProgress<int> Progress)> _queue;
            private readonly Action _afterEachRun;
            private readonly CancellationToken _token;

            public Consumer(IEnumerable<AnalysisFileBean> files, IEnumerable<IProgress<int>> reportActions, Action afterEachRun, CancellationToken token) {
                _queue = new ConcurrentQueue<(AnalysisFileBean File, IProgress<int> Progress)>(files.Zip(reportActions, (file, report) => (file, report)));
                _afterEachRun = afterEachRun;
                _token = token;
            }

            private async Task ConsumeAsync(Func<AnalysisFileBean, IProgress<int>, CancellationToken, Task> process) {
                while (_queue.TryDequeue(out var pair)) {
                    await process(pair.File, pair.Progress, _token).ConfigureAwait(false);
                    _afterEachRun?.Invoke();
                }
            }

            public Task[] ConsumeAllAsync(Func<AnalysisFileBean, IProgress<int>, CancellationToken, Task> process, int parallel) {
                var tasks = new Task[parallel];
                for (int i = 0; i < parallel; i++) {
                    tasks[i] = Task.Run(() => ConsumeAsync(process), _token);
                }
                return tasks;
            }
        }
    }
}

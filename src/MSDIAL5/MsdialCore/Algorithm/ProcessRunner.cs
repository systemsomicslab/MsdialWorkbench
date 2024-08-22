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

    public sealed class ProcessRunner(IFileProcessor processor, IReadOnlyList<AnalysisFileBean> analysisFiles, int numParallel)
    {
        private readonly IFileProcessor _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        private readonly IReadOnlyList<AnalysisFileBean> _analysisFiles = analysisFiles;
        private readonly int _numParallel = numParallel;

        public Task RunAllAsync(IEnumerable<IProgress<int>> reportActions, Action afterEachRun, CancellationToken token) {
            var consumer = new Consumer(_analysisFiles, reportActions, afterEachRun);
            return Task.WhenAll(consumer.ConsumeAllAsync(_processor.RunAsync, _numParallel, token));
        }

        public Task AnnotateAllAsync(IEnumerable<IProgress<int>> reportActions, Action afterEachRun, CancellationToken token) {
            var consumer = new Consumer(_analysisFiles, reportActions, afterEachRun);
            return Task.WhenAll(consumer.ConsumeAllAsync(_processor.AnnotateAsync, _numParallel, token));
        }

        sealed class Consumer(IEnumerable<AnalysisFileBean> files, IEnumerable<IProgress<int>> reportActions, Action afterEachRun)
        {
            private readonly ConcurrentQueue<(AnalysisFileBean File, IProgress<int> Progress)> _queue = new(files.Zip(reportActions, (file, report) => (file, report)));
            private readonly Action _afterEachRun = afterEachRun;

            private async Task ConsumeAsync(Func<AnalysisFileBean, IProgress<int>, CancellationToken, Task> process, CancellationToken token) {
                while (_queue.TryDequeue(out var pair)) {
                    await process(pair.File, pair.Progress, token).ConfigureAwait(false);
                    _afterEachRun?.Invoke();
                }
            }

            public Task[] ConsumeAllAsync(Func<AnalysisFileBean, IProgress<int>, CancellationToken, Task> process, int parallel, CancellationToken token) {
                var tasks = new Task[parallel];
                for (int i = 0; i < parallel; i++) {
                    tasks[i] = Task.Run(() => ConsumeAsync(process, token), token);
                }
                return tasks;
            }
        }
    }
}

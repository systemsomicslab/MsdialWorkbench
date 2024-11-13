using CompMs.Common.Enum;
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
        Task RunAsync(AnalysisFileBean file, ProcessOption option, IProgress<int>? reportAction, CancellationToken token);
    }

    public sealed class ProcessRunner(IFileProcessor processor, int numParallel)
    {
        private readonly IFileProcessor _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        private readonly int _numParallel = numParallel;

        public Task RunAllAsync(IReadOnlyList<AnalysisFileBean> analysisFiles, ProcessOption option, IEnumerable<IProgress<int>?> reportActions, Action? afterEachRun, CancellationToken token) {
            var consumer = new Consumer(_processor, _numParallel, analysisFiles, reportActions, afterEachRun);
            return Task.WhenAll(consumer.ConsumeAllAsync(option, token));
        }

        sealed class Consumer(IFileProcessor processor, int numParallel, IEnumerable<AnalysisFileBean> files, IEnumerable<IProgress<int>?> reportActions, Action? afterEachRun)
        {
            private readonly IFileProcessor _processor = processor;
            private readonly ConcurrentQueue<(AnalysisFileBean File, IProgress<int>? Progress)> _queue = new(files.Zip(reportActions, (file, report) => (file, report)));
            private readonly Action? _afterEachRun = afterEachRun;
            private readonly int _numParallel = numParallel;

            private async Task ConsumeAsync(ProcessOption option, CancellationToken token) {
                while (_queue.TryDequeue(out var pair)) {
                    token.ThrowIfCancellationRequested();
                    await _processor.RunAsync(pair.File, option, pair.Progress, token).ConfigureAwait(false);
                    _afterEachRun?.Invoke();
                }
            }

            public Task[] ConsumeAllAsync(ProcessOption option, CancellationToken token) {
                var tasks = new Task[_numParallel];
                for (int i = 0; i < _numParallel; i++) {
                    tasks[i] = Task.Run(() => ConsumeAsync(option, token), token);
                }
                return tasks;
            }
        }
    }
}

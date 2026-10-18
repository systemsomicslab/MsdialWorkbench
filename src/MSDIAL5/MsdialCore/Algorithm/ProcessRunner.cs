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

    /// <summary>
    /// Runs one processor over a list of analysis files, <paramref name="numParallel"/> at a time.
    /// </summary>
    /// <remarks>
    /// numParallel must be at least 1, and is CHECKED rather than clamped. At zero this class used to
    /// build an empty Task array, so RunAllAsync returned an already-completed task having processed
    /// nothing, and the caller carried on to its export stage as though the run had succeeded. What
    /// the export stage then wrote depended on what happened to be on disk: a crash on a missing
    /// .pai, or -- when the raw data already carried .pai files from an earlier run, which is the
    /// ordinary case since MS-DIAL writes its intermediates beside the files it reads -- THE PREVIOUS
    /// RUN'S PEAKS AND PEAK IDS, exported under this run's parameters, library list and version
    /// string. Artifacts describing a run that never happened is the worst outcome available here, so
    /// a caller that asks for no workers is told, loudly, instead of being given silence.
    ///
    /// Clamping to 1 was the alternative and is worse: it would hide the caller's arithmetic error.
    /// Every caller already floors its own value (Math.Max(1, ...)); this is the backstop that says
    /// so when one stops.
    /// </remarks>
    public sealed class ProcessRunner(IFileProcessor processor, int numParallel)
    {
        private readonly IFileProcessor _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        private readonly int _numParallel = numParallel > 0
            ? numParallel
            : throw new ArgumentOutOfRangeException(nameof(numParallel), numParallel, "At least one worker is required; zero would process no file and report success.");

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

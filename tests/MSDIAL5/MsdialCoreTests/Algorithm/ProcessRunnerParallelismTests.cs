using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Tests
{
    /// <summary>
    /// A run that processes no file must not report success.
    /// </summary>
    /// <remarks>
    /// The Console sized its worker pool as <c>NumThreads / 2</c> with integer division and no floor,
    /// in all five modes, while every GUI path used <c>Math.Max(1, ... / 2)</c>. So
    /// "number of threads: 1" -- a value the method-file parser accepts -- gave zero workers.
    ///
    /// The damage was not the empty run. It was what the export stage did next. MS-DIAL writes its
    /// per-file intermediates beside the raw data it reads, so a re-analysis of data that had been
    /// processed before found the previous run's .pai files, loaded them, and exported THE PREVIOUS
    /// RUN'S PEAKS AND PEAK IDS -- under the new run's parameter file, library list and version
    /// string. Peak ID is the join key across .mdpeak, .mdmsp, .mdalign, .mzTab and .mdpeakid.tsv, so
    /// the whole artifact set would then describe a run that never happened, consistently and
    /// without any error.
    ///
    /// Found by an audit of Peak ID traceability on 2026-09-15, and confirmed against the source:
    /// the parameter reader that made "number of threads" honourable is what created the zero case.
    /// </remarks>
    [TestClass()]
    public class ProcessRunnerParallelismTests
    {
        [TestMethod()]
        public void AskingForNoWorkersIsRefusedRatherThanSilentlyDoingNothing() {
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => new ProcessRunner(new CountingProcessor(), 0),
                "zero workers processed every file zero times and reported success");
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => new ProcessRunner(new CountingProcessor(), -1));
        }

        /// <summary>
        /// One worker is the value the Console's arithmetic used to turn into zero.
        /// </summary>
        [TestMethod()]
        public async Task OneWorkerProcessesEveryFile() {
            var processor = new CountingProcessor();
            var files = Files(4);

            await new ProcessRunner(processor, 1).RunAllAsync(
                files, ProcessOption.All, Enumerable.Repeat(default(IProgress<int>?), files.Count), null, default);

            Assert.AreEqual(4, processor.Count);
        }

        /// <summary>
        /// And the file count is independent of how many workers share the queue.
        /// </summary>
        /// <remarks>
        /// The queue is drained, not partitioned, so this also says no file is processed twice when
        /// there are more workers than files.
        /// </remarks>
        [TestMethod()]
        public async Task EveryWorkerCountProcessesEveryFileExactlyOnce() {
            foreach (var workers in new[] { 1, 2, 3, 8 }) {
                var processor = new CountingProcessor();
                var files = Files(4);

                await new ProcessRunner(processor, workers).RunAllAsync(
                    files, ProcessOption.All, Enumerable.Repeat(default(IProgress<int>?), files.Count), null, default);

                Assert.AreEqual(4, processor.Count, $"with {workers} workers");
                CollectionAssert.AreEquivalent(
                    files.Select(f => f.AnalysisFileId).ToArray(),
                    processor.Seen.ToArray(),
                    $"with {workers} workers");
            }
        }

        private static List<AnalysisFileBean> Files(int count) {
            return Enumerable.Range(0, count)
                .Select(i => new AnalysisFileBean { AnalysisFileId = i, AnalysisFileName = $"file{i}", })
                .ToList();
        }

        private sealed class CountingProcessor : IFileProcessor
        {
            private int _count;
            private readonly List<int> _seen = new List<int>();

            public int Count => _count;
            public IReadOnlyList<int> Seen { get { lock (_seen) { return _seen.ToList(); } } }

            public Task RunAsync(AnalysisFileBean file, ProcessOption option, IProgress<int>? reportAction, CancellationToken token) {
                Interlocked.Increment(ref _count);
                lock (_seen) {
                    _seen.Add(file.AnalysisFileId);
                }
                return Task.CompletedTask;
            }
        }
    }
}

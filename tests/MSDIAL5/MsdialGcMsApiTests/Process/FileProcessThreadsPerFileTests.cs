using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CompMs.MsdialGcMsApi.Process.Tests
{
    /// <summary>
    /// The per-file thread budget never changes which peak-spotting path a run takes: 1 stays the
    /// single-thread path (a different peak list by design), and anything above stays the
    /// multi-thread path, whose peaks do not depend on the thread count.
    /// </summary>
    [TestClass()]
    public class FileProcessThreadsPerFileTests
    {
        private static ProcessBaseParameter Threads(int numThreads) => new ProcessBaseParameter { NumThreads = numThreads };

        [DataTestMethod()]
        [DataRow(1, 1)]
        [DataRow(1, 8)]
        [DataRow(4, 8)]
        public void OneThreadKeepsTheSingleThreadPath(int numParallel, int fileCount) {
            Assert.AreEqual(1, FileProcess.ThreadsPerFile(Threads(1), numParallel, fileCount));
        }

        [DataTestMethod()]
        [DataRow(2, 1, 8)]
        [DataRow(4, 2, 8)]
        [DataRow(64, 32, 8)]
        [DataRow(64, 32, 0)]
        public void MoreThanOneThreadNeverFallsToTheSingleThreadPath(int numThreads, int numParallel, int fileCount) {
            Assert.IsTrue(FileProcess.ThreadsPerFile(Threads(numThreads), numParallel, fileCount) >= 2);
        }

        [TestMethod()]
        public void ALoneFileGetsEveryUsableThread() {
            var parameter = Threads(Environment.ProcessorCount * 4);
            Assert.AreEqual(Math.Max(2, Environment.ProcessorCount), FileProcess.ThreadsPerFile(parameter, numParallel: 1, fileCount: 1));
            Assert.AreEqual(Math.Max(2, Environment.ProcessorCount), FileProcess.ThreadsPerFile(parameter, numParallel: parameter.UsableNumThreads / 2, fileCount: 1));
        }

        [TestMethod()]
        public void ConcurrentFilesShareTheUsableThreads() {
            var parameter = Threads(Environment.ProcessorCount);
            var numParallel = Math.Max(1, parameter.UsableNumThreads / 2);
            var perFile = FileProcess.ThreadsPerFile(parameter, numParallel, fileCount: 1000);
            Assert.AreEqual(Math.Max(2, parameter.UsableNumThreads / numParallel), perFile);
        }
    }
}

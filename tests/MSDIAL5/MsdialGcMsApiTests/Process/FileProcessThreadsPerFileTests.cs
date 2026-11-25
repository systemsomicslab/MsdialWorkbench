using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CompMs.MsdialGcMsApi.Process.Tests
{
    /// <summary>
    /// The per-file thread budget never changes which peak-spotting path a run takes: 1 stays the
    /// single-thread path (a different peak list by design), and anything above stays the
    /// multi-thread path, whose peaks do not depend on the thread count. Annotation's degree is the
    /// machine's cores shared among the files processed at once, so a lone file keeps every core.
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
            Assert.AreEqual(1, FileProcess.PeakSpottingThreadsPerFile(Threads(1), numParallel, fileCount));
        }

        [DataTestMethod()]
        [DataRow(2, 1, 8)]
        [DataRow(4, 2, 8)]
        [DataRow(64, 32, 8)]
        [DataRow(64, 32, 0)]
        public void MoreThanOneThreadNeverFallsToTheSingleThreadPath(int numThreads, int numParallel, int fileCount) {
            Assert.IsTrue(FileProcess.PeakSpottingThreadsPerFile(Threads(numThreads), numParallel, fileCount) >= 2);
        }

        [TestMethod()]
        public void ALoneFileGetsEveryUsableThread() {
            var parameter = Threads(Environment.ProcessorCount * 4);
            Assert.AreEqual(Math.Max(2, Environment.ProcessorCount), FileProcess.PeakSpottingThreadsPerFile(parameter, numParallel: 1, fileCount: 1));
            Assert.AreEqual(Math.Max(2, Environment.ProcessorCount), FileProcess.PeakSpottingThreadsPerFile(parameter, numParallel: parameter.UsableNumThreads / 2, fileCount: 1));
        }

        [TestMethod()]
        public void ConcurrentFilesShareTheUsableThreads() {
            var parameter = Threads(Environment.ProcessorCount);
            var numParallel = Math.Max(1, parameter.UsableNumThreads / 2);
            var perFile = FileProcess.PeakSpottingThreadsPerFile(parameter, numParallel, fileCount: 1000);
            Assert.AreEqual(Math.Max(2, parameter.UsableNumThreads / numParallel), perFile);
        }

        /// <summary>
        /// The GUI's default "Number of threads" (2) runs one file at a time, and annotation's
        /// Parallel.For had no bound before: that file must still annotate on every core.
        /// </summary>
        [TestMethod()]
        public void AtTheGuiDefaultAFileAnnotatesOnEveryCore() {
            var parameter = new ProcessBaseParameter();
            var numParallel = Math.Max(1, parameter.UsableNumThreads / 2);
            Assert.AreEqual(1, numParallel);
            Assert.AreEqual(Environment.ProcessorCount, FileProcess.AnnotationThreadsPerFile(numParallel, fileCount: 8));
            Assert.AreEqual(Environment.ProcessorCount, FileProcess.AnnotationThreadsPerFile(numParallel: 1, fileCount: 1));
        }

        [DataTestMethod()]
        [DataRow(1, 8)]
        [DataRow(2, 8)]
        [DataRow(4, 8)]
        [DataRow(32, 8)]
        [DataRow(32, 0)]
        public void ConcurrentFilesShareTheCoresForAnnotation(int numParallel, int fileCount) {
            var concurrentFiles = Math.Max(1, Math.Min(numParallel, fileCount));
            var perFile = FileProcess.AnnotationThreadsPerFile(numParallel, fileCount);
            Assert.AreEqual(Math.Max(1, Environment.ProcessorCount / concurrentFiles), perFile);
            Assert.IsTrue(perFile * concurrentFiles <= Math.Max(Environment.ProcessorCount, concurrentFiles));
        }
    }
}

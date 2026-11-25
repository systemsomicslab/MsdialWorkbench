using BenchmarkDotNet.Running;

namespace CommonStandardBenchmark
{
    internal class Program
    {
        static void Main() {
            // BenchmarkRunner.Run<IEnumerableExtensionSequenceBenchmark>();
            //BenchmarkRunner.Run<Algorithm.Scoring.MsScanMatchingBenchmark>();
            //BenchmarkRunner.Run<Algorithm.Scoring.SpectralEntropyBenchmark>();
            //BenchmarkRunner.Run<Algorithm.Scoring.SpectralEntropyAnnotationBenchmark>();
            BenchmarkRunner.Run<DataStructure.PriorityQueueBenchmark>();
        }
    }
}

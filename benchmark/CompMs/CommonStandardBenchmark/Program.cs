using BenchmarkDotNet.Running;

namespace CommonStandardBenchmark
{
    internal class Program
    {
        static void Main() {
            // BenchmarkRunner.Run<IEnumerableExtensionSequenceBenchmark>();
            //BenchmarkRunner.Run<Algorithm.Scoring.MsScanMatchingBenchmark>();
            BenchmarkRunner.Run<DataStructure.PriorityQueueBenchmark>();
        }
    }
}

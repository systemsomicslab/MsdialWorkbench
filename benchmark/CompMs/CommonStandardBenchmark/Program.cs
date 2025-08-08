using BenchmarkDotNet.Running;
using CommonStandardBenchmark.Algorithm.Scoring;

namespace CommonStandardBenchmark
{
    internal class Program
    {
        static void Main() {
            // BenchmarkRunner.Run<IEnumerableExtensionSequenceBenchmark>();
            BenchmarkRunner.Run<MsScanMatchingBenchmark>();
        }
    }
}

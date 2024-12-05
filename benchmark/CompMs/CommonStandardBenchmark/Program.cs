using BenchmarkDotNet.Running;

namespace CommonStandardBenchmark
{
    internal class Program
    {
        static void Main() {
            BenchmarkRunner.Run<IEnumerableExtensionSequenceBenchmark>();
        }
    }
}

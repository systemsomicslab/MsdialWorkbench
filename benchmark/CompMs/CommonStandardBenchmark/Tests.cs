using BenchmarkDotNet.Attributes;
using CompMs.Common.Extension;

namespace CommonStandardBenchmark
{
    [HtmlExporter]
    [MemoryDiagnoser]
    [ShortRunJob]
    public class IEnumerableExtensionSequenceBenchmark
    {
        [Params(1, 64, 1024)]
        public int Length { get; set; }

        [Params(1, 64, 1024)]
        public int Width { get; set; }

        private double[][] input;

        [GlobalSetup]
        public void Setup() {
            var r = new Random();
            input = new double[Length][];
            for (int i = 0; i < Length; i++) {
                input[i] = new double[Width];
                for (int j = 0; j < Width; j++) {
                    input[i][j] = r.NextDouble();
                }
            }
        }

        [Benchmark]
        public int Sequence() {
            return input.Sequence().Count();
        }
    }
}

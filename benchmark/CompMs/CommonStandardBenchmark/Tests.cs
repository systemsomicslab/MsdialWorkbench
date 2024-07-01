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
        public int Width { get; set; }

        [Params(1, 64, 1024)]
        public int Length { get; set; }

        private double[][] input;

        [GlobalSetup]
        public void Setup() {
            var r = new Random();
            input = new double[Width][];
            for (int i = 0; i < Width; i++) {
                input[i] = new double[Length];
                for (int j = 0; j < Length; j++) {
                    input[i][j] = r.NextDouble();
                }
            }
        }

        [Benchmark(Baseline = true)]
        public int Sequence() {
            return input.Sequence().Count();
        }

        [Benchmark]
        public int SequenceNew() {
            return input.SequenceNew().Count();
        }
    }
}

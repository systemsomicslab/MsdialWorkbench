using BenchmarkDotNet.Attributes;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.Interfaces;

namespace CommonStandardBenchmark.Algorithm.Scoring;

[HtmlExporter]
[MemoryDiagnoser]
[ShortRunJob]
public class MsScanMatchingBenchmark
{
    [Params(42, 1412)]
    public int Seed { get; set; }

    [Params(2, 10, 100, 1000)]
    public int Size { get; set; }

    [Params(10, 20, 50)]
    public int SpectrumLength { get; set; }

    private IMSScanProperty[] scans;

    [GlobalSetup]
    public void Setup() {
        scans = new IMSScanProperty[Size];
        var rng = new Random(Seed);
        for (int i = 0; i < Size; i++) {
            scans[i] = CreateScan(SpectrumLength, 1d, rng.Next());
        }
    }

    [Benchmark]
    public double[][] Batch() {
        return MsScanMatching.GetBatchSimpleDotProduct(scans, .5d, 0d, double.MaxValue);
    }

    [Benchmark(Baseline = true)]
    public double[][] Each() {
        var result = new double[scans.Length][];
        for (int i = 0; i < result.Length; i++) {
            result[i] = new double[scans.Length];
        }
        for (int i = 0; i < scans.Length; i++) {
            result[i][i] = 1d;
            for (int j = i + 1; j < scans.Length; j++) {
                result[i][j] = result[j][i] = MsScanMatching.GetSimpleDotProduct(scans[i], scans[j], .5d, 0d, double.MaxValue);
            }
        }
        return result;
    }

    private IMSScanProperty CreateScan(int size, double mzScale, int seed) {
        var rng = new Random(seed);
        var peaks = new List<SpectrumPeak>(size);
        for (int j = 0; j < size; j++) {
            peaks.Add(new()
            {
                Mass = rng.NextDouble() * mzScale,
                Intensity = rng.NextDouble(),
            });
        }
        peaks.Sort((a, b) => a.Mass.CompareTo(b.Mass));

        return new MSScanProperty
        {
            Spectrum = peaks,
        };
    }
}

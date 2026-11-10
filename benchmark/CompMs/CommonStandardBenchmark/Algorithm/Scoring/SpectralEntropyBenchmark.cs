using BenchmarkDotNet.Attributes;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;

namespace CommonStandardBenchmark.Algorithm.Scoring;

// One query against one reference, as MsReferenceScorer.CalculateScore does per candidate.
// The weighted dot product runs beside it on the same pair: it is computed for the same
// candidate, so it says how expensive the entropy is by comparison.
[HtmlExporter]
[MemoryDiagnoser]
[ShortRunJob]
public class SpectralEntropyBenchmark
{
    private const double Bin = .05;

    [Params(10, 50, 200)]
    public int PeakCount { get; set; }

    private List<SpectrumPeak> query = null!;
    private List<SpectrumPeak> reference = null!;
    private MSScanProperty queryScan = null!;
    private MSScanProperty referenceScan = null!;

    [GlobalSetup]
    public void Setup() {
        var rng = new Random(42);
        query = SpectrumGenerator.Spectrum(rng, PeakCount);
        reference = SpectrumGenerator.Similar(rng, query, PeakCount);
        queryScan = new MSScanProperty { Spectrum = query, };
        referenceScan = new MSScanProperty { Spectrum = reference, };
    }

    [Benchmark(Baseline = true)]
    public double EntropySimilarity() {
        return MsScanMatching.GetSpectralEntropySimilarity(query, reference, Bin);
    }

    [Benchmark]
    public double WeightedDotProduct() {
        return MsScanMatching.GetWeightedDotProduct(queryScan, referenceScan, Bin, 0d, 2000d);
    }
}

// The shape of annotation: one query scored against every reference inside the precursor window.
[HtmlExporter]
[MemoryDiagnoser]
[ShortRunJob]
public class SpectralEntropyAnnotationBenchmark
{
    private const double Bin = .05;

    [Params(1000)]
    public int ReferenceCount { get; set; }

    [Params(50)]
    public int PeakCount { get; set; }

    private List<SpectrumPeak> query = null!;
    private List<SpectrumPeak>[] references = null!;

    [GlobalSetup]
    public void Setup() {
        var rng = new Random(42);
        query = SpectrumGenerator.Spectrum(rng, PeakCount);
        references = new List<SpectrumPeak>[ReferenceCount];
        for (int i = 0; i < references.Length; i++) {
            references[i] = i % 2 == 0
                ? SpectrumGenerator.Similar(rng, query, rng.Next(5, 2 * PeakCount))
                : SpectrumGenerator.Spectrum(rng, rng.Next(5, 2 * PeakCount));
        }
    }

    [Benchmark]
    public double OneQueryAgainstAllReferences() {
        var sum = 0d;
        foreach (var reference in references) {
            sum += MsScanMatching.GetSpectralEntropySimilarity(query, reference, Bin);
        }
        return sum;
    }
}

// MS/MS-like spectra: fragments sorted by m/z below the precursor, intensities spanning several
// orders of magnitude, and references that share about half their fragments with the query.
internal static class SpectrumGenerator
{
    public static List<SpectrumPeak> Spectrum(Random rng, int count, double precursorMz = 800d) {
        return Enumerable.Range(0, count)
            .Select(_ => new SpectrumPeak(50d + rng.NextDouble() * (precursorMz - 50d), Intensity(rng)))
            .OrderBy(p => p.Mass)
            .ToList();
    }

    public static List<SpectrumPeak> Similar(Random rng, List<SpectrumPeak> query, int count) {
        var peaks = query
            .Where(_ => rng.NextDouble() < .5)
            .Take(count)
            .Select(p => new SpectrumPeak(p.Mass + (rng.NextDouble() - .5) * .01, Intensity(rng)))
            .ToList();
        peaks.AddRange(Spectrum(rng, count - peaks.Count));
        return peaks.OrderBy(p => p.Mass).ToList();
    }

    private static double Intensity(Random rng) => Math.Pow(10, 1 + rng.NextDouble() * 5);
}

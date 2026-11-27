using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Gcms;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialGcMsApi.Algorithm;
using System.Linq;

namespace GcmsCompoundSearchBenchmark;

internal static class Program
{
    private static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}

/// <summary>
/// The GC-MS compound search the main window runs, against a synthetic library of Count references
/// with 24 peaks each, all within the RI window of the query. Uncached scores every reference and
/// builds the results as the search did before it was cached; Cached is a repeated search of the
/// same spot, which the cache answers.
/// </summary>
/// <remarks>
/// That both return the same results is checked by MsdialGuiAppTests (GcmsMainCompoundSearchTests).
/// InProcess, because a generated out-of-process project would have to rebuild MsdialGuiApp.
/// </remarks>
[InProcess]
[MemoryDiagnoser]
public class GcmsCompoundSearch
{
    [Params(10000, 50000)]
    public int Count { get; set; }

    private CalculateMatchScore _calculator = null!;
    private MSDecResult _scan = null!;
    private GcmsAlignmentCompoundSearchUsecase _search = null!;
    private PeakSpotModel _target = null!;

    [GlobalSetup]
    public void Setup() {
        var references = Enumerable.Range(0, Count).Select(index => {
            var reference = new MoleculeMsReference {
                ScanID = index,
                Name = "Synthetic " + index,
                ChromXs = new ChromXs(1000, ChromXType.RI, ChromXUnit.None),
                Spectrum = Enumerable.Range(0, 24).Select(peak => new SpectrumPeak(50 + 15 * peak, 30 + peak * 37)).ToList(),
            };
            reference.TryCompactSpectrum();
            return reference;
        }).ToList();
        _calculator = new CalculateMatchScore(new DataBaseItem<MoleculeDataBase>(
            new MoleculeDataBase(references, "Synthetic", DataBaseSource.Msp, SourceType.MspDB, ""), []),
            new MsRefSearchParameterBase(), RetentionType.RI, RiCompoundType.Alkanes);
        _scan = new MSDecResult {
            ChromXs = new ChromXs(1000, ChromXType.RI, ChromXUnit.None),
            Spectrum = [new SpectrumPeak(73, 100), new SpectrumPeak(147, 60), new SpectrumPeak(205, 40), new SpectrumPeak(291, 20)],
        };
        _search = new GcmsAlignmentCompoundSearchUsecase(_calculator);
        _target = new PeakSpotModel(null!, _scan);
        _search.Search(_target);
    }

    [Benchmark(Baseline = true)]
    public object Uncached() {
        var results = _calculator.CalculateMatches(_scan).OrderByDescending(result => result.TotalScore)
            .Select(result => new GcmsCompoundResult(_calculator.Reference(result), result)).ToArray();
        foreach (var result in results) {
            ((ICompoundResult)result).MatchResult.Source |= SourceType.Manual;
        }
        return results;
    }

    [Benchmark]
    public object Cached() => _search.Search(_target);
}

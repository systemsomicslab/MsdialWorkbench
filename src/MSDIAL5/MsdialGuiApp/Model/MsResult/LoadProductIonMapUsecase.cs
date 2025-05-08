using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj;
using CompMs.Common.Interfaces;
using CompMs.Raw.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.MsResult;

internal sealed class LoadProductIonMapUsecase(IDataProvider provider)
{
    private readonly IDataProvider _provider = provider;
    private IChromatogramPeakFeature? _previousPeak = null;
    private RawSpectrum[]? _previousSpectra = null;

    public double MzTolerance { get; set; } = .01d;

    public async Task<List<MappedIon>> LoadProductIonSpectraAsync(IChromatogramPeakFeature peak, MzRange productIonRange, CancellationToken token = default) {
        if (peak != _previousPeak) {
            _previousPeak = peak;

        var query = new SpectraLoadingQuery
        {
            MSLevel = 2,
            PrecursorMzRange = new() { Mz = peak.Mass, Tolerance = 20d, },
            ScanTimeRange = new() { Start = peak.ChromXsLeft.RT.Value, End = peak.ChromXsRight.RT.Value, },
        };
            _previousSpectra = await _provider.LoadMSSpectraAsync(query, token).ConfigureAwait(false);
        }
        var spectra = _previousSpectra;

        token.ThrowIfCancellationRequested();
        return spectra.SelectMany(
            s => s.Spectrum.Where(p => productIonRange.Includes(p.Mz)),
            (s, p) => new MappedIon(s.Id, s.ExperimentID, p.Intensity))
            .GroupBy(ion => (ion.ID, ion.ExperimentID))
            .Select(g => new MappedIon(g.Key.ID, g.Key.ExperimentID, g.Sum(ion => ion.Intensity)))
            .ToList();
    }
}

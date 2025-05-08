using CompMs.Common.DataObj;
using CompMs.Common.Interfaces;
using CompMs.Raw.Abstractions;
using System;
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

    public async Task<(MappedIon?, List<MappedIon>)> LoadProductIonSpectraAsync(IChromatogramPeakFeature peak, MzRange productIonRange, CancellationToken token = default) {
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

        var ions = spectra.SelectMany(
            s => s.Spectrum.Where(p => productIonRange.Includes(p.Mz)),
            (s, p) => new MappedIon(s.Id, s.ExperimentID, p.Intensity, s.ScanStartTime, s.Precursor.SelectedIonMz))
            .GroupBy(ion => (ion.ID, ion.ExperimentID))
            .Select(g => new MappedIon(g.Key.ID, g.Key.ExperimentID, g.Sum(ion => ion.Intensity), g.Average(ion => ion.Time), g.Max(ion => (ion.Intensity, ion.Mz)).Mz))
            .ToList();

        RawSpectrum? nearest = null;
        if (spectra is { Length: > 0}) {
            nearest = spectra.Min(s => (Math.Abs(s.Precursor.SelectedIonMz - peak.Mass), Math.Abs(s.ScanStartTime - peak.ChromXsTop.RT.Value), s)).s;
            if (!nearest.Precursor.ContainsMz(peak.Mass, .01d, CompMs.Common.Enum.AcquisitionType.SWATH) || Math.Abs(nearest.ScanStartTime - peak.ChromXsTop.RT.Value) > .1d) {
                nearest = null;
            }
        }
        var nearestIon = nearest is null
            ? null
            : ions.FirstOrDefault(ion => ion.ExperimentID == nearest.ExperimentID && ion.ID == nearest.Id);
        return (nearestIon, ions);
    }
}

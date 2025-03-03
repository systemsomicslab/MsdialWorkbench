using CompMs.Common.DataObj;
using CompMs.Raw.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Internal;

internal sealed class PrecursorMzSelectedDataProvider(IDataProvider other, double mz, double tolerance) : IDataProvider
{
    private readonly IDataProvider _other = other;
    private readonly double _mz = mz;
    private readonly double _tolerance = tolerance;

    public List<double> LoadCollisionEnergyTargets() {
        return LoadMsSpectrums().Select(s => s.CollisionEnergy).Distinct().ToList();
    }

    public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() => _other.LoadMs1Spectrums();

    public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) => _other.LoadMs1SpectrumsAsync(token);

    public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
        var spectra = _other.LoadMsNSpectrums(level);
        if (level <= 1) {
            return spectra;
        }
        return new ReadOnlyCollection<RawSpectrum>(spectra.Where(s => IsNearBy(s.Precursor, _mz, _tolerance)).ToArray());
    }

    public async Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) {
        var spectra = await _other.LoadMsNSpectrumsAsync(level, token).ConfigureAwait(false);
        if (level <= 1) {
            return spectra;
        }
        return new ReadOnlyCollection<RawSpectrum>(spectra.Where(s => IsNearBy(s.Precursor, _mz, _tolerance)).ToArray());
    }

    public async Task<RawSpectrum[]> LoadMSSpectraWithRtRangeAsync(int msLevel, double rtStart, double rtEnd, CancellationToken token) {
        var spectra = await _other.LoadMSSpectraWithRtRangeAsync(msLevel, rtStart, rtEnd, token).ConfigureAwait(false);
        return spectra.Where(s => s.MsLevel <= 1 || Math.Abs(s.Precursor.SelectedIonMz - _mz) <= _tolerance).ToArray();
    }

    public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
        var spectra = _other.LoadMsSpectrums();
        return new ReadOnlyCollection<RawSpectrum>(spectra.Where(s => s.MsLevel <= 1 || IsNearBy(s.Precursor, _mz, _tolerance)).ToArray());
    }

    public async Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) {
        var spectra = await _other.LoadMsSpectrumsAsync(token).ConfigureAwait(false);
        return new ReadOnlyCollection<RawSpectrum>(spectra.Where(s => s.MsLevel <= 1 || IsNearBy(s.Precursor, _mz, _tolerance)).ToArray());
    }

    private static bool IsNearBy(RawPrecursorIon p, double mz, double tolerance) {
        return p is not null && Math.Abs(p.SelectedIonMz - mz) <= tolerance;
    }

    public Task<RawSpectrum?> LoadSpectrumAsync(ulong id, SpectrumIDType idType) {
        return _other.LoadSpectrumAsync(id, idType);
    }

    public Task<RawSpectrum[]> LoadMSSpectraAsync(SpectraLoadingQuery query, CancellationToken token) {
        if (query.PrecursorMzRange is null) {
            query.PrecursorMzRange = new PrecursorMzRange { Mz = _mz, Tolerance = _tolerance };
        }
        return _other.LoadMSSpectraAsync(query, token);
    }

    public async IAsyncEnumerable<RawSpectrum[]> LoadMSSpectraAsync(SpectraLoadingQuery[] queries, [EnumeratorCancellation]CancellationToken token) {
        foreach (var query in queries) {
            yield return await LoadMSSpectraAsync(query, token).ConfigureAwait(false);
        }
    }
}

using CompMs.Common.DataObj;
using CompMs.Common.Utility;
using CompMs.Raw.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Internal;

internal sealed class MS1CachedDataProvider(IDataProvider dataProvider) : IDataProvider
{
    private readonly IDataProvider _dataProvider = dataProvider;

    private RawSpectrum[]? _ms1SpectraCache;

    private async Task<ReadOnlyCollection<RawSpectrum>> GetReadonlyMs1SpectraCache(CancellationToken token) {
        return _readonlyMs1SpectraCache ??= new ReadOnlyCollection<RawSpectrum>(await LoadMs1CoreAsync(token).ConfigureAwait(false));
    }
    private ReadOnlyCollection<RawSpectrum>? _readonlyMs1SpectraCache;

    public List<double> LoadCollisionEnergyTargets() => _dataProvider.LoadCollisionEnergyTargets();

    public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() => GetReadonlyMs1SpectraCache(default).Result;

    public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) => GetReadonlyMs1SpectraCache(token);

    public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) => level switch
    {
        1 => LoadMs1Spectrums(),
        _ => _dataProvider.LoadMsNSpectrums(level)
    };

    public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) => level switch
    {
        1 => LoadMs1SpectrumsAsync(token),
        _ => _dataProvider.LoadMsNSpectrumsAsync(level, token)
    };

    public Task<RawSpectrum[]> LoadMSSpectraWithRtRangeAsync(int msLevel, double rtStart, double rtEnd, CancellationToken token) {
        if (msLevel != 1) {
            return _dataProvider.LoadMSSpectraWithRtRangeAsync(msLevel, rtStart, rtEnd, token);
        }
        return Task.Run(async () => {
            var spectra = await LoadMs1CoreAsync(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            var lo = spectra.LowerBound(rtStart, (s, t) => s.ScanStartTime.CompareTo(t));
            var hi = spectra.UpperBound(rtEnd, (s, t) => s.ScanStartTime.CompareTo(t));
            var result = new RawSpectrum[hi - lo];
            Array.Copy(_ms1SpectraCache, lo, result, 0, hi - lo);
            return result;
        }, token);
    }

    public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() => _dataProvider.LoadMsSpectrums();

    public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) => _dataProvider.LoadMsSpectrumsAsync(token);

    public Task<RawSpectrum?> LoadSpectrumAsync(ulong id, SpectrumIDType idType) => _dataProvider.LoadSpectrumAsync(id, idType);

    private async Task<RawSpectrum[]> LoadMs1CoreAsync(CancellationToken token) {
        if (_ms1SpectraCache is not null) {
            return _ms1SpectraCache;
        }
        var spectra = await _dataProvider.LoadMs1SpectrumsAsync(token).ConfigureAwait(false);
        if (IsSorted(spectra)) {
            return _ms1SpectraCache = spectra.ToArray();
        }
        else {
            return _ms1SpectraCache = spectra.OrderBy(s => s.ScanStartTime).ToArray();
        }
    }

    private static bool IsSorted(ReadOnlyCollection<RawSpectrum> ms) {
        for (int i = 1; i < ms.Count; i++) {
            if (ms[i - 1].ScanStartTime > ms[i].ScanStartTime) {
                return false;
            }
        }
        return true;
    }
}

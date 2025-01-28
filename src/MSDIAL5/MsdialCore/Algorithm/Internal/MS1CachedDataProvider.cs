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
    private readonly object _lock = new();
    private TaskCompletionSource<MS1CacheObject>? _taskCompletionSource;

    private async Task<ReadOnlyCollection<RawSpectrum>> GetReadonlyMs1SpectraCache(CancellationToken token) {
        var ms1Spectra = await LoadMs1CoreAsync(token).ConfigureAwait(false);
        return _readonlyMs1SpectraCache ??= new ReadOnlyCollection<RawSpectrum>(ms1Spectra.Spectra);
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
            var lo = spectra.Spectra.LowerBound(rtStart, (s, t) => s.ScanStartTime.CompareTo(t));
            var hi = spectra.Spectra.UpperBound(rtEnd, (s, t) => s.ScanStartTime.CompareTo(t));
            var result = new RawSpectrum[hi - lo];
            Array.Copy(spectra.Spectra, lo, result, 0, hi - lo);
            return result;
        }, token);
    }

    public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() => _dataProvider.LoadMsSpectrums();

    public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) => _dataProvider.LoadMsSpectrumsAsync(token);

    public async Task<RawSpectrum?> LoadSpectrumAsync(ulong id, SpectrumIDType idType) {
        var spectra = await LoadMs1CoreAsync(default).ConfigureAwait(false);
        if (idType == spectra.IDType && spectra.Map.TryGetValue(id, out var s)) {
            return s;
        }
        return await _dataProvider.LoadSpectrumAsync(id, idType).ConfigureAwait(false);
    }

    public async Task<RawSpectrum[]> LoadMSSpectraAsync(SpectraLoadingQuery query, CancellationToken token) {
        if (query.MSLevel != 1) {
            return await _dataProvider.LoadMSSpectraAsync(query, token).ConfigureAwait(false);
        }

        switch (query.Flags)
        {
            case SpectraLoadingFlag.MSLevel:
                return await _dataProvider.LoadMsNSpectrumsAsync(query.MSLevel!.Value, token).ContinueWith(t => t.Result.ToArray(), token).ConfigureAwait(false);
            case SpectraLoadingFlag.MSLevel | SpectraLoadingFlag.ScanTimeRange:
                return await LoadMSSpectraWithRtRangeAsync(query.MSLevel!.Value, query.ScanTimeRange!.Start, query.ScanTimeRange!.End, token).ConfigureAwait(false);
        }

        var ms1Spectra = await LoadMs1CoreAsync(token).ConfigureAwait(false);
        IEnumerable<RawSpectrum> results = ms1Spectra.Spectra;
        if (query.CollisionEnergy.HasValue) {
            results = results.Where(s => s.Precursor is not { } precursor || precursor.CollisionEnergy == query.CollisionEnergy);
        }
        if (query.PrecursorMzRange is { } mzRange) {
            results = results.Where(s => s.Precursor is not { } precursor || Math.Abs(mzRange.Mz - precursor.SelectedIonMz) < mzRange.Tolerance);
        }
        if (query.ScanTimeRange is not null) {
            results = results.Where(s => query.ScanTimeRange.Start - s.ScanStartTime <= 0d  && s.ScanStartTime - query.ScanTimeRange.End <= 0d);
        }
        if (query.DriftTimeRange is { } driftTimeRange) {
            results = results.Where(s => s.DriftTime == 0d || driftTimeRange.Start - s.DriftTime <= 0d && s.DriftTime - driftTimeRange.End <= 0d);
        }

        return results.OrderBy(s => s.ScanStartTime).ToArray();
    }

    public IAsyncEnumerable<RawSpectrum[]> LoadMSSpectraAsync(SpectraLoadingQuery[] queries, CancellationToken token) {
        return _dataProvider.LoadMSSpectraAsync(queries, token);
    }

    private Task<MS1CacheObject> LoadMs1CoreAsync(CancellationToken token) {
        lock(_lock) {
            if (_taskCompletionSource is null) {
                _taskCompletionSource = new TaskCompletionSource<MS1CacheObject>();
                _dataProvider.LoadMs1SpectrumsAsync(token).ContinueWith(task => {
                    var spectra = task.Result;
                    if (IsSorted(spectra)) {
                        _taskCompletionSource.SetResult(new(spectra.ToArray()));
                    }
                    else {
                        _taskCompletionSource.SetResult(new(spectra.OrderBy(s => s.ScanStartTime).ToArray()));
                    }
                });
            }
        }

        return _taskCompletionSource.Task;
    }

    private static bool IsSorted(ReadOnlyCollection<RawSpectrum> ms) {
        for (int i = 1; i < ms.Count; i++) {
            if (ms[i - 1].ScanStartTime > ms[i].ScanStartTime) {
                return false;
            }
        }
        return true;
    }

    class MS1CacheObject(RawSpectrum[] spectra)
    {
        public RawSpectrum[] Spectra { get; set; } = spectra;
        public Dictionary<ulong, RawSpectrum> Map { get; set; } = spectra.ToDictionary(s => s.RawSpectrumID.ID, s => s);
        public SpectrumIDType IDType { get; set; } = spectra.FirstOrDefault()?.RawSpectrumID?.IDType ?? SpectrumIDType.Index;
    }
}

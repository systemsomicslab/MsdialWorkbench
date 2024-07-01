using CompMs.Common.DataObj;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Internal;

/// <summary>
/// Provides a caching layer over an existing <see cref="IDataProvider"/> implementation,
/// to optimize the retrieval of spectra data by caching previously accessed MS1, MS2, and n-level MS spectra.
/// This class aims to reduce the number of redundant data fetch operations by storing spectra in memory
/// after the first retrieval, improving performance for subsequent accesses to the same data.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="CachedDataProvider"/> class internally maintains caches for MS1, MS2, and generic MS spectra,
/// as well as a dynamic cache for spectra of specific MS levels beyond MS2. This allows for efficient data retrieval
/// across various MS levels without repeatedly querying the underlying data provider.
/// </para>
/// <para>
/// It is important to note that this caching strategy is most effective when there is a high likelihood of
/// repeated accesses to the same spectra data. The cache is initialized and populated on-demand, meaning
/// that spectra data is cached the first time it is requested through this provider.
/// </para>
/// </remarks>
internal sealed class CachedDataProvider: IDataProvider
{
    private readonly IDataProvider _provider;

    private ReadOnlyCollection<RawSpectrum>? _ms1SpectraCache, _ms2SpectraCache, _msSpectraCache;
    private readonly Dictionary<int, ReadOnlyCollection<RawSpectrum>> _msnSpectraCaches = new Dictionary<int, ReadOnlyCollection<RawSpectrum>>();

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedDataProvider"/> class,
    /// wrapping around an existing <see cref="IDataProvider"/> to add caching functionality.
    /// </summary>
    /// <param name="provider">The underlying data provider to which this class will add a caching layer.</param>
    public CachedDataProvider(IDataProvider provider)
    {
        _provider = provider;
    }

    public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
        return _ms1SpectraCache ??= _provider.LoadMs1Spectrums();
    }

    public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) {
        if (_ms1SpectraCache is not null) {
            return Task.FromResult(_ms1SpectraCache);
        }
        return LoadMs1SpectrumsAsyncCore(token);
    }

    private async Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsyncCore(CancellationToken token) {
        return _ms1SpectraCache = await _provider.LoadMs1SpectrumsAsync(token).ConfigureAwait(false);
    }

    public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
        return level switch
        {
            1 => _ms1SpectraCache ??= _provider.LoadMsNSpectrums(1),
            2 => _ms2SpectraCache ??= _provider.LoadMsNSpectrums(2),
            _ => _msnSpectraCaches.TryGetValue(level, out var spectra)
                ? spectra
                : _msnSpectraCaches[level] = _provider.LoadMsNSpectrums(level)
        };
    }

    public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) {
        switch (level) {
            case 1:
                if (_ms1SpectraCache is not null) {
                    return Task.FromResult(_ms1SpectraCache);
                }
                break;
            case 2:
                if (_ms2SpectraCache is not null) {
                    return Task.FromResult(_ms2SpectraCache);
                }
                break;
            default:
                if (_msnSpectraCaches.TryGetValue(level, out var spectra)) {
                    return Task.FromResult(spectra);
                }
                break;
        }
        return LoadMsNSpectrumsAsyncCore(level, token);
    }

    private async Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsyncCore(int level, CancellationToken token) {
        return level switch
        {
            1 => _ms1SpectraCache = await _provider.LoadMsNSpectrumsAsync(1, token).ConfigureAwait(false),
            2 => _ms2SpectraCache = await _provider.LoadMsNSpectrumsAsync(2, token).ConfigureAwait(false),
            _ => _msnSpectraCaches[level] = await _provider.LoadMsNSpectrumsAsync(level, token).ConfigureAwait(false),
        };
    }

    public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
        return _msSpectraCache ??= _provider.LoadMsSpectrums();
    }

    public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) {
        if (_msSpectraCache is not null) {
            return Task.FromResult(_msSpectraCache);
        }
        return LoadMsSpectrumsAsyncCore(token);
    }

    private async Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsyncCore(CancellationToken token) {
        return _msSpectraCache = await _provider.LoadMsSpectrumsAsync(token).ConfigureAwait(false);
    }
}

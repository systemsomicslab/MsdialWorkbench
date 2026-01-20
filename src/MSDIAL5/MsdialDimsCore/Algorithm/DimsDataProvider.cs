using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.Raw.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialDimsCore.Algorithm
{
    class DimsBaseDataProvider
    {
        private readonly IDataProvider _provider;
        private readonly List<RawSpectrum> _spectra;
        private readonly ConcurrentDictionary<int, ReadOnlyCollection<RawSpectrum>> _cache = [];

        public DimsBaseDataProvider(IDataProvider provider, List<RawSpectrum> spectra) {
            _provider = provider;
            _spectra = spectra;
        }

        public virtual List<double> LoadCollisionEnergyTargets() {
            return _provider.LoadCollisionEnergyTargets();
        }

        public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
            if (_spectra.FirstOrDefault(s => s.MsLevel == 1) is not { } spectrum) {
                return new ReadOnlyCollection<RawSpectrum>([]);
            }
            return new ReadOnlyCollection<RawSpectrum>([spectrum]);
        }

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) {
            return Task.FromResult(LoadMs1Spectrums());
        }

        public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
            return _cache.GetOrAdd(level, l => _spectra.Where(s => s.MsLevel == l).ToList().AsReadOnly());
        }

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) {
            return Task.FromResult(LoadMsNSpectrums(level));
        }

        public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
            return _spectra.AsReadOnly();
        }

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) {
            return Task.FromResult(_spectra.AsReadOnly());
        }
    }

    public sealed class DimsBpiDataProvider : IDataProvider
    {
        private readonly IDataProvider _provider;
        private readonly List<RawSpectrum> _spectra;
        private readonly DimsBaseDataProvider _baseProvider;

        public DimsBpiDataProvider(IDataProvider provider) {
            _provider = provider;
            _spectra = BasePeakIntensitySelect(_provider.LoadMsSpectrums());
            _baseProvider = new DimsBaseDataProvider(provider, _spectra);
        }

        public DimsBpiDataProvider(IDataProvider provider, double timeBegin, double timeEnd) {
            _provider = provider;
            _spectra = BasePeakIntensitySelect(_provider.LoadMsSpectrums().Where(s => timeBegin <= s.ScanStartTime && s.ScanStartTime <= timeEnd).ToList());
            _baseProvider = new DimsBaseDataProvider(provider, _spectra);
        }

        public List<double> LoadCollisionEnergyTargets() => _baseProvider.LoadCollisionEnergyTargets();

        public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() => _baseProvider.LoadMs1Spectrums();

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) => _baseProvider.LoadMs1SpectrumsAsync(token);

        public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) => _baseProvider.LoadMsNSpectrums(level);

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) => _baseProvider.LoadMsNSpectrumsAsync(level, token);

        public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() => _baseProvider.LoadMsSpectrums();

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) => _baseProvider.LoadMsSpectrumsAsync(token);

        private static List<RawSpectrum> BasePeakIntensitySelect(IReadOnlyCollection<RawSpectrum> spectrums) {
            var ms1Spectrum = spectrums.Where(spectrum => spectrum.MsLevel == 1).Argmax(spectrum => spectrum.BasePeakIntensity);
            var msSpectrums = spectrums.Where(spectrum => spectrum.MsLevel != 1);
            var result = msSpectrums.Prepend(ms1Spectrum).ToList();
            for (int i = 0; i < result.Count; i++) {
                result[i].Index = i;
                result[i].RawSpectrumID = new IndexedSpectrumIdentifier(i);
            }
            return result;
        }

        public Task<RawSpectrum?> LoadSpectrumAsync(ulong id, SpectrumIDType idType) {
            if (id < (ulong)_spectra.Count) {
                return Task.FromResult(_spectra[(int)id]);
            }
            return Task.FromResult<RawSpectrum?>(null);
        }

        public async Task<RawSpectrum[]> LoadMSSpectraWithRtRangeAsync(int msLevel, double rtStart, double rtEnd, CancellationToken token) {
            if (msLevel == 1) {
                var spectrum = await LoadMs1SpectrumsAsync(token).ConfigureAwait(false);
                if (spectrum[0].IsInScanTimeRange(rtStart, rtEnd)) {
                    return [spectrum[0]];
                }
                var spectra = await _provider.LoadMSSpectraWithRtRangeAsync(msLevel, rtStart, rtEnd, token).ConfigureAwait(false);
                if (spectra.Length == 0) {
                    return [];
                }
                var baseSpectrum = spectra.Argmax(s => s.BasePeakIntensity).ShallowCopy();
                baseSpectrum.RawSpectrumID = new IndexedSpectrumIdentifier(int.MaxValue);
                return [baseSpectrum];
            }
            var msNSpectra = await LoadMsNSpectrumsAsync(msLevel, token).ConfigureAwait(false);
            return msNSpectra.Where(s => s.IsInScanTimeRange(rtStart, rtEnd)).ToArray();
        }

        public Task<RawSpectrum[]> LoadMSSpectraAsync(SpectraLoadingQuery query, CancellationToken token) {
            return _provider.LoadMSSpectraAsync(query, token);
        }
        public async IAsyncEnumerable<RawSpectrum[]> LoadMSSpectraAsync(SpectraLoadingQuery[] queries, [EnumeratorCancellation]CancellationToken token) {
            foreach (var query in queries) {
                yield return await LoadMSSpectraAsync(query, token).ConfigureAwait(false);
            }
        }
    }

    public sealed class DimsTicDataProvider : IDataProvider
    {
        private readonly IDataProvider _provider;
        private readonly List<RawSpectrum> _spectra;
        private readonly DimsBaseDataProvider _baseProvider;

        public DimsTicDataProvider(IDataProvider provider) {
            _provider = provider;
            _spectra = TotalIonCurrentSelect(provider.LoadMsSpectrums());
            _baseProvider = new DimsBaseDataProvider(provider, _spectra);
        }

        public DimsTicDataProvider(IDataProvider provider, double timeBegin, double timeEnd) {
            _provider = provider;
            _spectra = TotalIonCurrentSelect(provider.LoadMsSpectrums().Where(s => timeBegin <= s.ScanStartTime && s.ScanStartTime <= timeEnd).ToList());
            _baseProvider = new DimsBaseDataProvider(provider, _spectra);
        }

        public List<double> LoadCollisionEnergyTargets() => _baseProvider.LoadCollisionEnergyTargets();

        public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() => _baseProvider.LoadMs1Spectrums();

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) => _baseProvider.LoadMs1SpectrumsAsync(token);

        public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) => _baseProvider.LoadMsNSpectrums(level);

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) => _baseProvider.LoadMsNSpectrumsAsync(level, token);

        public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() => _baseProvider.LoadMsSpectrums();

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) => _baseProvider.LoadMsSpectrumsAsync(token);

        private static List<RawSpectrum> TotalIonCurrentSelect(IReadOnlyList<RawSpectrum> spectrums) {
            if (spectrums.IsEmptyOrNull()) return null;
            var ms1Spectrum = spectrums.Count > 1 
                ? spectrums.Where(spectrum => spectrum.MsLevel == 1).Argmax(spectrum => spectrum.TotalIonCurrent)
                : spectrums[0];
            var msSpectrums = spectrums.Where(spectrum => spectrum.MsLevel != 1);
            var result = new[] { ms1Spectrum }.Concat(msSpectrums).ToList();
            for (int i = 0; i < result.Count; i++) {
                result[i].Index = i;
                result[i].RawSpectrumID = new IndexedSpectrumIdentifier(i);
            }
            return result;
        }

        public Task<RawSpectrum?> LoadSpectrumAsync(ulong id, SpectrumIDType idType) {
            if (id < (ulong)_spectra.Count) {
                return Task.FromResult(_spectra[(int)id]);
            }
            return Task.FromResult<RawSpectrum?>(null);
        }

        public async Task<RawSpectrum[]> LoadMSSpectraWithRtRangeAsync(int msLevel, double rtStart, double rtEnd, CancellationToken token) {
            if (msLevel == 1) {
                var spectrum = await LoadMs1SpectrumsAsync(token).ConfigureAwait(false);
                if (spectrum[0].IsInScanTimeRange(rtStart, rtEnd)) {
                    return [spectrum[0]];
                }
                var spectra = await _provider.LoadMSSpectraWithRtRangeAsync(msLevel, rtStart, rtEnd, token).ConfigureAwait(false);
                if (spectra.Length == 0) {
                    return [];
                }
                var baseSpectrum = spectra.Argmax(s => s.TotalIonCurrent).ShallowCopy();
                baseSpectrum.RawSpectrumID = new IndexedSpectrumIdentifier(int.MaxValue);
                return [baseSpectrum];
            }
            var msNSpectra = await LoadMsNSpectrumsAsync(msLevel, token).ConfigureAwait(false);
            return msNSpectra.Where(s => s.IsInScanTimeRange(rtStart, rtEnd)).ToArray();
        }

        public Task<RawSpectrum[]> LoadMSSpectraAsync(SpectraLoadingQuery query, CancellationToken token) {
            return _provider.LoadMSSpectraAsync(query, token);
        }
        public async IAsyncEnumerable<RawSpectrum[]> LoadMSSpectraAsync(SpectraLoadingQuery[] queries, [EnumeratorCancellation]CancellationToken token) {
            foreach (var query in queries) {
                yield return await LoadMSSpectraAsync(query, token).ConfigureAwait(false);
            }
        }
    }

    public sealed class DimsAverageDataProvider : IDataProvider
    {
        private readonly IDataProvider _provider;
        private readonly double _mzTolerance;
        private readonly List<RawSpectrum> _spectra;
        private readonly DimsBaseDataProvider _baseProvider;

        public DimsAverageDataProvider(IDataProvider provider, double mzTolerance) {
            _provider = provider;
            _mzTolerance = mzTolerance;
            _spectra = AccumulateRawSpectrums(provider.LoadMsSpectrums().Select(spec => spec.ShallowCopy()).ToList(), mzTolerance);
            _baseProvider = new DimsBaseDataProvider(provider, _spectra);
        }

        public DimsAverageDataProvider(IDataProvider provider, double mzTolerance, double timeBegin, double timeEnd) {
            _provider = provider;
            _mzTolerance = mzTolerance;
            _spectra = AccumulateRawSpectrums(provider.LoadMsSpectrums().Where(spec => timeBegin <= spec.ScanStartTime && spec.ScanStartTime <= timeEnd).Select(spec => spec.ShallowCopy()).ToList(), mzTolerance);
            _baseProvider = new DimsBaseDataProvider(provider, _spectra);
        }

        public List<double> LoadCollisionEnergyTargets() => _baseProvider.LoadCollisionEnergyTargets();

        public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() => _baseProvider.LoadMs1Spectrums();

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) => _baseProvider.LoadMs1SpectrumsAsync(token);

        public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) => _baseProvider.LoadMsNSpectrums(level);

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) => _baseProvider.LoadMsNSpectrumsAsync(level, token);

        public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() => _baseProvider.LoadMsSpectrums();

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) => _baseProvider.LoadMsSpectrumsAsync(token);

        private static List<RawSpectrum> AccumulateRawSpectrums(IReadOnlyCollection<RawSpectrum> spectrums, double massTolerance) {
            var targetmz4ppmcalc = 500.0;
            var binning = new Binning(targetmz4ppmcalc, massTolerance);
            var ms1Spectrums = spectrums.Where(spectrum => spectrum.MsLevel == 1).ToList();
            var groups = ms1Spectrums.SelectMany(spectrum => spectrum.Spectrum)
                .GroupBy(peak => binning.BinningMz(peak.Mz));
            var massBins = new Dictionary<int, double[]>();
            foreach (var group in groups) {
                var peaks = group.ToList();
                var accIntensity = peaks.Sum(peak => peak.Intensity) / ms1Spectrums.Count;
                var basepeak = peaks.Argmax(peak => peak.Intensity);
                massBins[group.Key] = [basepeak.Mz, accIntensity, basepeak.Intensity];
            }
            var result = ms1Spectrums.First().ShallowCopy();
            result.SetSpectrumProperties(massBins);
            var results = new[] { result }.Concat(spectrums.Where(spectrum => spectrum.MsLevel != 1)).ToList();
            for (int i = 0; i < results.Count; i++) {
                results[i].Index = i;
                results[i].RawSpectrumID = new IndexedSpectrumIdentifier(i);
            }
            return results;
        }

        public Task<RawSpectrum?> LoadSpectrumAsync(ulong id, SpectrumIDType idType) {
            if (id < (ulong)_spectra.Count) {
                return Task.FromResult(_spectra[(int)id]);
            }
            return Task.FromResult<RawSpectrum?>(null);
        }

        public async Task<RawSpectrum[]> LoadMSSpectraWithRtRangeAsync(int msLevel, double rtStart, double rtEnd, CancellationToken token) {
            if (msLevel == 1) {
                var spectra = await _provider.LoadMSSpectraWithRtRangeAsync(msLevel, rtStart, rtEnd, token).ConfigureAwait(false);
                if (spectra.Length == 0) {
                    return [];
                }
                var baseSpectrum = AccumulateRawSpectrums(spectra.Select(s => s.ShallowCopy()).ToList(), _mzTolerance)[0];
                baseSpectrum.RawSpectrumID = new IndexedSpectrumIdentifier(int.MaxValue);
                return [baseSpectrum];
            }
            var msNSpectra = await LoadMsNSpectrumsAsync(msLevel, token).ConfigureAwait(false);
            return msNSpectra.Where(s => s.IsInScanTimeRange(rtStart, rtEnd)).ToArray();
        }

        public Task<RawSpectrum[]> LoadMSSpectraAsync(SpectraLoadingQuery query, CancellationToken token) {
            return _provider.LoadMSSpectraAsync(query, token);
        }

        public async IAsyncEnumerable<RawSpectrum[]> LoadMSSpectraAsync(SpectraLoadingQuery[] queries, [EnumeratorCancellation]CancellationToken token) {
            foreach (var query in queries) {
                yield return await LoadMSSpectraAsync(query, token).ConfigureAwait(false);
            }
        }

        class Binning
        {
            public double Pivot { get; }
            public double Tolerance { get; }
            public double Ppm { get; }
            public double LogTolerance { get; }

            public Binning(double pivot, double tolerance) {
                Pivot = pivot;
                Tolerance = tolerance;
                Ppm = tolerance / Pivot * 1_000_000d;
                LogTolerance = Math.Log(1 + Ppm / 1_000_000d);
            }

            /// <summary>
            /// Calculates the bin index for a given m/z value.
            /// </summary>
            /// <param name="mz">The mass-to-charge (m/z) value.</param>
            /// <returns>The bin index as an integer.</returns>
            public int BinningMz(double mz) {
                if (mz <= Pivot) {
                    return (int)(mz / Tolerance);
                }
                else {
                    int pivotbin = (int)(Pivot / Tolerance) + 1;
                    return pivotbin + (int)(Math.Log(mz / Pivot) / LogTolerance);
                }
            }
        }
    }

    public sealed class DimsBpiDataProviderFactory<T>(IDataProviderFactory<T> factory, double timeBegin, double timeEnd) : IDataProviderFactory<T>
    {
        public IDataProvider Create(T source) => new DimsBpiDataProvider(factory.Create(source), timeBegin, timeEnd);
    }

    public sealed class DimsTicDataProviderFactory<T>(IDataProviderFactory<T> factory, double timeBegin, double timeEnd) : IDataProviderFactory<T>
    {
        public IDataProvider Create(T source) => new DimsTicDataProvider(factory.Create(source), timeBegin, timeEnd);
    }

    public sealed class DimsAverageDataProviderFactory<T>(IDataProviderFactory<T> factory, double mzTolerance, double timeBegin, double timeEnd) : IDataProviderFactory<T> {
        public IDataProvider Create(T source) => new DimsAverageDataProvider(factory.Create(source), mzTolerance, timeBegin, timeEnd);
    }
}

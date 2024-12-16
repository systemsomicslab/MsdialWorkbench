using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.Raw.Abstractions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    }

    public class DimsAverageDataProvider : IDataProvider
    {
        private readonly IDataProvider _provider;
        private readonly List<RawSpectrum> _spectra;
        private readonly DimsBaseDataProvider _baseProvider;

        public DimsAverageDataProvider(IDataProvider provider, double mzTolerance) {
            _provider = provider;
            _spectra = AccumulateRawSpectrums(provider.LoadMsSpectrums().Select(spec => spec.ShallowCopy()).ToList(), mzTolerance);
            _baseProvider = new DimsBaseDataProvider(provider, _spectra);
        }

        public DimsAverageDataProvider(IDataProvider provider, double mzTolerance, double timeBegin, double timeEnd) {
            _provider = provider;
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
            var ms1Spectrums = spectrums.Where(spectrum => spectrum.MsLevel == 1).ToList();
            var groups = ms1Spectrums.SelectMany(spectrum => spectrum.Spectrum)
                .GroupBy(peak => (int)(peak.Mz / massTolerance));
            var massBins = new Dictionary<int, double[]>();
            foreach (var group in groups) {
                var peaks = group.ToList();
                var accIntensity = peaks.Sum(peak => peak.Intensity) / ms1Spectrums.Count;
                var basepeak = peaks.Argmax(peak => peak.Intensity);
                massBins[group.Key] = [basepeak.Mz, accIntensity, basepeak.Intensity];
            }
            var result = ms1Spectrums.First();
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

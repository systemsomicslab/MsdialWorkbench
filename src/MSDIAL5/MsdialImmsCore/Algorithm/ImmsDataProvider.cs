using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.Raw.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialImmsCore.Algorithm
{
    public class ImmsRepresentativeDataProvider : IDataProvider {
        private readonly IDataProvider _provider;
        private readonly List<RawSpectrum> _spectra;
        private readonly double _begin, _end;
        private readonly ConcurrentDictionary<int, Lazy<ReadOnlyCollection<RawSpectrum>>> _cache = [];

        public ImmsRepresentativeDataProvider(IDataProvider provider, double timeBegin, double timeEnd) {
            _provider = provider;
            _spectra = SelectRepresentative(FilterByScanTime(provider.LoadMsSpectrums(), timeBegin, timeEnd));
        }

        public ImmsRepresentativeDataProvider(IDataProvider provider) {
            _provider = provider;
            _spectra = SelectRepresentative(provider.LoadMsSpectrums());
        }

        public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
            return _spectra.AsReadOnly();
        }

        public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
            return LoadMsNSpectrums(1);
        }

        public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
            return _cache.GetOrAdd(level,
                i => new Lazy<ReadOnlyCollection<RawSpectrum>>(() => {
                    return _spectra.Where(spectrum => spectrum.MsLevel == level).ToList().AsReadOnly();
                })).Value;
        }

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) {
            return Task.FromResult(_spectra.AsReadOnly());
        }

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) {
            return Task.FromResult(LoadMsNSpectrums(1));
        }

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) {
            return Task.FromResult(LoadMsNSpectrums(level));
        }

        public List<double> LoadCollisionEnergyTargets() {
            return _provider.LoadCollisionEnergyTargets();
        }

        private static List<RawSpectrum> SelectRepresentative(IEnumerable<RawSpectrum> rawSpectrums) {
            var ms1Spectrums = rawSpectrums
                .Where(spectrum => spectrum.MsLevel == 1)
                .GroupBy(spectrum => spectrum.ScanNumber)
                .Argmax(spectrums => spectrums.Sum(spectrum => spectrum.TotalIonCurrent));
            var result = ms1Spectrums.Concat(rawSpectrums.Where(spec => spec.MsLevel != 1))
                .Select(spec => spec.ShallowCopy())
                .OrderBy(spectrum => spectrum.DriftTime).ToList();
            for (int i = 0; i < result.Count; i++) {
                result[i].Index = i;
                result[i].RawSpectrumID = new IndexedSpectrumIdentifier(i);
            }
            return result;
        }

        private static IEnumerable<RawSpectrum> FilterByScanTime(IEnumerable<RawSpectrum> spectrums, double timeBegin, double timeEnd) {
            return spectrums.Where(spec => timeBegin <= spec.ScanStartTime && spec.ScanStartTime <= timeEnd);
        }

        public Task<RawSpectrum?> LoadSpectrumAsync(ulong id, SpectrumIDType idType) {
            if (id < (ulong)_spectra.Count) {
                return Task.FromResult(_spectra[(int)id]);
            }
            return Task.FromResult<RawSpectrum?>(null);
        }
    }

    public sealed class ImmsAverageDataProvider : IDataProvider {
        private readonly IDataProvider _provider;
        private readonly double _begin, _end;
        private readonly double _mzTolerance;
        private readonly double _driftTolerance;
        private readonly List<RawSpectrum> _spectra;
        private readonly ConcurrentDictionary<int, Lazy<ReadOnlyCollection<RawSpectrum>>> _cache = [];

        public ImmsAverageDataProvider(IDataProvider provider, double mzTolerance, double driftTolerance) {
            _provider = provider;
            _begin = double.MinValue;
            _end = double.MaxValue;
            _spectra = AccumulateSpectrum(_provider.LoadMsSpectrums().ToList(), mzTolerance, driftTolerance);
            _mzTolerance = mzTolerance;
            _driftTolerance = driftTolerance;
        }

        public ImmsAverageDataProvider(IDataProvider provider, double mzTolerance, double driftTolerance, double timeBegin, double timeEnd) {
            _provider = provider;
            _begin = timeBegin;
            _end = timeEnd;
            _spectra = FilterByScanTime(AccumulateSpectrum(_provider.LoadMsSpectrums().ToList(), mzTolerance, driftTolerance), timeBegin, timeEnd).ToList();
            _mzTolerance = mzTolerance;
            _driftTolerance = driftTolerance;
        }

        public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
            return _spectra.AsReadOnly();
        }

        public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
            return LoadMsNSpectrums(1);
        }

        public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
            return _cache.GetOrAdd(level,
                i => new Lazy<ReadOnlyCollection<RawSpectrum>>(() => {
                    return _spectra.Where(spectrum => spectrum.MsLevel == level).ToList().AsReadOnly();
                })).Value;
        }

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) {
            return Task.FromResult(_spectra.AsReadOnly());
        }

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) {
            return Task.FromResult(LoadMsNSpectrums(1));
        }

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token = default) {
            return Task.FromResult(LoadMsNSpectrums(level));
        }

        public List<double> LoadCollisionEnergyTargets() {
            return _provider.LoadCollisionEnergyTargets();
        }

        private static List<RawSpectrum> AccumulateSpectrum(List<RawSpectrum> rawSpectrums, double massTolerance, double driftTolerance) {
            var ms1Spectrum = rawSpectrums.Where(spectrum => spectrum.MsLevel == 1).ToList();
            var numOfMeasurement = ms1Spectrum.Select(spectrum => spectrum.ScanStartTime).Distinct().Count();

            var groups = rawSpectrums.GroupBy(spectrum => Math.Ceiling(spectrum.DriftTime / driftTolerance));
            var result = groups
                .OrderBy(kvp => kvp.Key)
                .SelectMany(group => AccumulateRawSpectrums(group.ToList(), massTolerance, numOfMeasurement))
                .ToList();
            for (var i = 0; i < result.Count; i++) {
                result[i].Index = i;
                result[i].RawSpectrumID = new IndexedSpectrumIdentifier(i);
            }

            return result;
        }

        private static IEnumerable<RawSpectrum> AccumulateRawSpectrums(IReadOnlyCollection<RawSpectrum> spectrums, double massTolerance, int numOfMeasurement) {
            var ms1Spectrums = spectrums.Where(spectrum => spectrum.MsLevel == 1).ToList();
            var groups = ms1Spectrums.SelectMany(spectrum => spectrum.Spectrum)
                .GroupBy(peak => (int)(peak.Mz / massTolerance));
            var massBins = new Dictionary<int, double[]>();
            foreach (var group in groups) {
                var peaks = group.ToList();
                var accIntensity = peaks.Sum(peak => peak.Intensity) / numOfMeasurement;
                //var accIntensity = peaks.Sum(peak => peak.Intensity);
                var basepeak = peaks.Argmax(peak => peak.Intensity);
                massBins[group.Key] = [basepeak.Mz, accIntensity, basepeak.Intensity];
            }
            var result = ms1Spectrums.First().ShallowCopy();
            result.SetSpectrumProperties(massBins);
            return new[] { result }.Concat(
                spectrums.Where(spectrum => spectrum.MsLevel != 1)
                .Select(spec => spec.ShallowCopy())
                .OrderBy(spectrum => spectrum.Index));
        }

        private static IEnumerable<RawSpectrum> FilterByScanTime(IEnumerable<RawSpectrum> spectrums, double timeBegin, double timeEnd) {
            return spectrums.Where(spec => timeBegin <= spec.ScanStartTime && spec.ScanStartTime <= timeEnd);
        }

        public Task<RawSpectrum?> LoadSpectrumAsync(ulong id, SpectrumIDType idType) {
            if (id < (ulong)_spectra.Count) {
                return Task.FromResult(_spectra[(int)id]);
            }
            return Task.FromResult<RawSpectrum?>(null);
        }
    }

    public sealed class ImmsRepresentativeDataProviderFactory<T>(IDataProviderFactory<T> factory, double timeBegin, double timeEnd) : IDataProviderFactory<T>
    {
        public IDataProvider Create(T source) => new ImmsRepresentativeDataProvider(factory.Create(source), timeBegin, timeEnd);
    }

    public sealed class ImmsAverageDataProviderFactory<T>(IDataProviderFactory<T> factory, double mzTolerance, double driftTolerance, double timeBegin = 0d, double timeEnd = double.MaxValue) : IDataProviderFactory<T>
    {
        public IDataProvider Create(T source) => new ImmsAverageDataProvider(factory.Create(source), mzTolerance, driftTolerance, timeBegin, timeEnd);
    }
}

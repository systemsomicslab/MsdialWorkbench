using CompMs.Common.DataObj;
using CompMs.Raw.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialLcImMsApi.Algorithm;

public sealed class LcimmsAccumulateDataProvider(IDataProvider dataProvider) : IDataProvider
{
    private readonly IDataProvider _dataProvider = dataProvider;

    public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
        return LoadMs1Spectrums().Concat(LoadMsSpectrums().Where(s => s.MsLevel != 1)).ToList().AsReadOnly(); 
    }

    public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
        return GetAccumulatedMs1Spectrum(_dataProvider.LoadMs1Spectrums()).AsReadOnly();
    }

    public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
        if (level == 1) {
            return LoadMs1Spectrums();
        }
        return _dataProvider.LoadMsNSpectrums(level);
    }

    public async Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) {
        var spectras = await Task.WhenAll(LoadMs1SpectrumsAsync(token), _dataProvider.LoadMsSpectrumsAsync(token)).ConfigureAwait(false);
        return spectras[0].Concat(spectras[1].Where(s => s.MsLevel != 1)).ToList().AsReadOnly(); 
    }

    public async Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) {
        var spectra = await _dataProvider.LoadMs1SpectrumsAsync(token).ConfigureAwait(false);
        return GetAccumulatedMs1Spectrum(spectra).AsReadOnly();
    }

    public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) {
        if (level == 1) {
            return LoadMs1SpectrumsAsync(token);
        }
        return _dataProvider.LoadMsNSpectrumsAsync(level, token);
    }

    public List<double> LoadCollisionEnergyTargets() {
        return _dataProvider.LoadCollisionEnergyTargets();
    }

    // spectra should be sorted by scan number then by drift number
    private static List<RawSpectrum> GetAccumulatedMs1Spectrum(ReadOnlyCollection<RawSpectrum> spectra) {
        var aSpec = new List<RawSpectrum>();

        var ranges = GetFrameRanges(spectra);
        var pool = new MassBinPool();
        var accumulatedMassBin = new Dictionary<int, MassBin>();

        for (int i = 0; i < ranges.Count; i++) {
            accumulatedMassBin.Clear();
            var lowest = spectra[ranges[i].Start].LowestObservedMz;
            var highest = spectra[ranges[i].Start].HighestObservedMz;
            for (int j = ranges[i].Start; j <= ranges[i].End; j++) {
                foreach (var peak in spectra[j].Spectrum)
                    AddToMassBinDictionary(accumulatedMassBin, pool, peak.Mz, peak.Intensity);
                lowest = Math.Min(spectra[j].LowestObservedMz, lowest);
                highest = Math.Max(spectra[j].HighestObservedMz, highest);
            }
            var rSpec = spectra[ranges[i].Start];
            var spec = new RawSpectrum {
                OriginalIndex = rSpec.OriginalIndex,
                Index = i,
                ScanNumber = rSpec.ScanNumber,
                ScanStartTime = rSpec.ScanStartTime,
                ScanStartTimeUnit = rSpec.ScanStartTimeUnit,
                MsLevel = rSpec.MsLevel,
                ScanPolarity = rSpec.ScanPolarity,
                Precursor = rSpec.Precursor,
                DriftScanNumber = rSpec.DriftScanNumber,
                DriftTime = rSpec.DriftTime,
                DriftTimeUnit = rSpec.DriftTimeUnit,
                MaldiFrameInfo = rSpec.MaldiFrameInfo,
                LowestObservedMz = lowest,
                HighestObservedMz = highest,
            };
            SetSpectrumProperties(spec, accumulatedMassBin);

            aSpec.Add(spec);

            pool.BatchReturn(accumulatedMassBin.Values);
        }
        return aSpec;
    }

    private static List<(int Start, int End)> GetFrameRanges(ReadOnlyCollection<RawSpectrum> spectra) {
        var ranges = new List<(int, int)>();
        var start = 0;
        var end = 0;
        var initial = spectra[0].ScanNumber;
        for (int i = 0; i < spectra.Count; i++) {
            if (spectra[i].ScanNumber == initial) {
                end = i;
            }
            else {
                initial = spectra[i].ScanNumber;
                ranges.Add((start, end));
                start = i;
                end = i;
            }
        }
        ranges.Add((start, end));
        return ranges;
    }

    private static void AddToMassBinDictionary(Dictionary<int, MassBin> accumulatedMassBin, MassBinPool pool, double mass, double intensity) {
        var massBin = (int)(mass * 1000);
        if (accumulatedMassBin.TryGetValue(massBin, out var bin)) {
            bin.Add(mass, intensity);
        }
        else {
            accumulatedMassBin.Add(massBin, pool.Get(mass, intensity));
        }
    }
    
    private static void SetSpectrumProperties(RawSpectrum spectrum, Dictionary<int, MassBin> accumulatedMassBin) {
        var basepeakIntensity = 0.0;
        var basepeakMz = 0.0;
        var totalIonCurrnt = 0.0;
        var lowestMz = double.MaxValue;
        var highestMz = double.MinValue;
        var minIntensity = double.MaxValue;

        var spectra = new RawPeakElement[accumulatedMassBin.Count];
        var idc = 0;

        foreach (var pair in accumulatedMassBin) {
            var pMzKey = pair.Key * 0.001;
            var pBasepeakMz = pair.Value.Mz;
            var pSummedIntensity = pair.Value.SummedIntensity;
            var pBasepeakIntensity = pair.Value.BaseIntensity;

            totalIonCurrnt += pSummedIntensity;

            if (pSummedIntensity > basepeakIntensity) {
                basepeakIntensity = pSummedIntensity;
                basepeakMz = pBasepeakMz;
            }
            if (lowestMz > pBasepeakMz) lowestMz = pBasepeakMz;
            if (highestMz < pBasepeakMz) highestMz = pBasepeakMz;
            if (minIntensity > pSummedIntensity) minIntensity = pSummedIntensity;

            spectra[idc++] = new RawPeakElement
            {
                Mz = Math.Round(pBasepeakMz, 5),
                Intensity = Math.Round(pSummedIntensity, 0)
            };
        }

        Array.Sort(spectra, (x, y) => x.Mz.CompareTo(y.Mz));
        spectrum.Spectrum = spectra;
        spectrum.DefaultArrayLength = spectra.Length;
        spectrum.BasePeakIntensity = basepeakIntensity;
        spectrum.BasePeakMz = basepeakMz;
        spectrum.TotalIonCurrent = totalIonCurrnt;
        spectrum.LowestObservedMz = lowestMz;
        spectrum.HighestObservedMz = highestMz;
        spectrum.MinIntensity = minIntensity;
    }

    sealed class MassBin
    {
        public MassBin(double mz, double intensity) {
            mz_ = mz;
            baseIntensity_ = summedIntensity_ = intensity;
        }

        public void Add(double mz, double intensity) {
            summedIntensity_ += intensity;
            if (intensity > baseIntensity_) {
                mz_ = mz;
                baseIntensity_ = intensity;
            }
        }

        public void Initialize(double mz, double intensity) {
            mz_ = mz;
            baseIntensity_ = summedIntensity_ = intensity;
        }

        public double Mz => mz_;
        private double mz_;

        public double BaseIntensity => baseIntensity_;
        private double baseIntensity_;

        public double SummedIntensity => summedIntensity_;
        private double summedIntensity_;
    }

    sealed class MassBinPool
    {
        private readonly Stack<MassBin> pool_;

        public MassBinPool() {
            pool_ = new Stack<MassBin>();
        }

        public MassBin Get(double mz, double intensity) {
            if (pool_.Count > 0) {
                var bin = pool_.Pop();
                bin.Initialize(mz, intensity);
                return bin;         
            }
            return new MassBin(mz, intensity);
        }

        public void Return(MassBin bin) => pool_.Push(bin);

        public void BatchReturn(IEnumerable<MassBin> bins) {
            foreach (var bin in bins) {
                pool_.Push(bin);
            }
        }
    }
}

public sealed class LcimmsAccumulateDataProviderFactory<T>(IDataProviderFactory<T> providerFactory) : IDataProviderFactory<T>
{
    public IDataProvider Create(T source) => new LcimmsAccumulateDataProvider(providerFactory.Create(source));
}

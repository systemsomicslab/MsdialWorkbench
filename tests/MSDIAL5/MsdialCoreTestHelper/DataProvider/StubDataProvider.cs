using CompMs.Common.DataObj;
using CompMs.Raw.Abstractions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MsdialCoreTestHelper.DataProvider;

public sealed class StubDataProvider : IDataProvider
{
    public List<RawSpectrum> Spectra { get; set; } = [];
    public List<double> CollisionEnergyTargets { get; set; }

    public void SetSpectra(List<RawSpectrum> spectra) {
        Spectra = spectra;
        CollisionEnergyTargets = spectra.Where(s => s.MsLevel >= 2).Select(s => s.CollisionEnergy).Distinct().ToList();
    }

    public List<double> LoadCollisionEnergyTargets() {
        return CollisionEnergyTargets;
    }

    public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
        return LoadMsNSpectrums(1);
    }

    public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) {
        return Task.FromResult(LoadMsNSpectrums(1));
    }

    public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
        return Spectra.Where(s => s.MsLevel == level).ToList().AsReadOnly();
    }

    public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) {
        return Task.FromResult(LoadMsNSpectrums(level));
    }

    public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
        return Spectra.AsReadOnly();
    }

    public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) {
        return Task.FromResult(LoadMsSpectrums());
    }

    public Task<RawSpectrum?> LoadSpectrumAsync(ulong id, SpectrumIDType idType) {
        if (id < (ulong)Spectra.Count) {
            return Task.FromResult(LoadMsSpectrums()[(int)id]);
        }
        return Task.FromResult<RawSpectrum?>(null);
    }

    public Task<RawSpectrum[]> LoadMSSpectraWithRtRangeAsync(int msLevel, double rtStart, double rtEnd, CancellationToken token) {
        return Task.FromResult(Spectra.Where(s => s.MsLevel == msLevel && s.ScanStartTime >= rtStart && s.ScanStartTime <= rtEnd).ToArray());
    }

    public async Task<RawSpectrum[]> LoadMSSpectraAsync(SpectraLoadingQuery query, CancellationToken token) {
        switch (query.Flags)
        {
            case SpectraLoadingFlag.None:
                return await LoadMsSpectrumsAsync(token).ContinueWith(t => t.Result.ToArray(), token).ConfigureAwait(false);
            case SpectraLoadingFlag.MSLevel:
                return await LoadMsNSpectrumsAsync(query.MSLevel!.Value, token).ContinueWith(t => t.Result.ToArray(), token).ConfigureAwait(false);
            case SpectraLoadingFlag.MSLevel | SpectraLoadingFlag.ScanTimeRange:
                return await LoadMSSpectraWithRtRangeAsync(query.MSLevel!.Value, query.ScanTimeRange!.Start, query.ScanTimeRange!.End, token).ConfigureAwait(false);
        }

        IEnumerable<RawSpectrum> results = [];
        if (query.MSLevel.HasValue) {
            results = await LoadMsNSpectrumsAsync(query.MSLevel.Value, token).ConfigureAwait(false);
        }
        else {
            results = await LoadMsSpectrumsAsync(token).ConfigureAwait(false);
        }
        if (query.CollisionEnergy.HasValue) {
            results = results.Where(s => s.Precursor is not { } precursor || precursor.CollisionEnergy == query.CollisionEnergy);
        }
        if (query.PrecursorMzRange is { } mzRange) {
            results = results.Where(s => s.Precursor is not { } precursor || System.Math.Abs(mzRange.Mz - precursor.SelectedIonMz) < mzRange.Tolerance);
        }
        if (query.ScanTimeRange is not null) {
            results = results.Where(s => query.ScanTimeRange.Start - s.ScanStartTime <= 0d  && s.ScanStartTime - query.ScanTimeRange.End <= 0d);
        }
        if (query.DriftTimeRange is { } driftTimeRange) {
            results = results.Where(s => s.DriftTime == 0d || driftTimeRange.Start - s.DriftTime <= 0d && s.DriftTime - driftTimeRange.End <= 0d);
        }

        return results.OrderBy(s => s.ScanStartTime).ToArray();
    }
}

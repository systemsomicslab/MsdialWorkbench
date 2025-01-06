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
    public List<RawSpectrum> Spectra { get; set; }
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
        return Task.FromResult(LoadMsSpectrums()[(int)id]);
    }

    public Task<RawSpectrum[]> LoadMSSpectraWithRtRangeAsync(int msLevel, double rtStart, double rtEnd, CancellationToken token) {
        return Task.FromResult(Spectra.Where(s => s.MsLevel == msLevel && s.ScanStartTime >= rtStart && s.ScanStartTime <= rtEnd).ToArray());
    }
}

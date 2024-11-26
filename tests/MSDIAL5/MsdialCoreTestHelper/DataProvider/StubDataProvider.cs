using CompMs.Common.DataObj;
using CompMs.Raw.Contract;
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
}

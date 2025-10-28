using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Internal;

internal sealed class PrecursorMzSelectedDataProvider(IDataProvider other, double mz, double tolerance, AcquisitionType acquisitionType) : IDataProvider
{
    private readonly IDataProvider _other = other;
    private readonly double _mz = mz;
    private readonly double _tolerance = tolerance;
    private readonly AcquisitionType _acquisitionType = acquisitionType;
    public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() => _other.LoadMs1Spectrums();

    public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) => _other.LoadMs1SpectrumsAsync(token);

    public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
        var spectra = _other.LoadMsNSpectrums(level);
        if (level <= 1) {
            return spectra;
        }
        return new ReadOnlyCollection<RawSpectrum>(spectra.Where(s => IsNearBy(s.Precursor, _mz, _tolerance)).ToArray());
    }

    public async Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) {
        var spectra = await _other.LoadMsNSpectrumsAsync(level, token).ConfigureAwait(false);
        if (level <= 1) {
            return spectra;
        }
        return new ReadOnlyCollection<RawSpectrum>(spectra.Where(s => IsNearBy(s.Precursor, _mz, _tolerance)).ToArray());
    }

    public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
        var spectra = _other.LoadMsSpectrums();
        return new ReadOnlyCollection<RawSpectrum>(spectra.Where(s => s.MsLevel <= 1 || IsNearBy(s.Precursor, _mz, _tolerance)).ToArray());
    }

    public async Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) {
        var spectra = await _other.LoadMsSpectrumsAsync(token).ConfigureAwait(false);
        return new ReadOnlyCollection<RawSpectrum>(spectra.Where(s => s.MsLevel <= 1 || IsNearBy(s.Precursor, _mz, _tolerance)).ToArray());
    }

    private bool IsNearBy(RawPrecursorIon p, double mz, double tolerance) {
        return p is not null && p.ContainsMz(mz, tolerance, _acquisitionType);
    }
}

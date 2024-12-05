using CompMs.Common.DataObj;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Internal;

/// <summary>
/// Provides filtered access to MS (Mass Spectrometry) spectrums data for a specific experiment ID.
/// </summary>
/// <param name="other">The data provider from which to retrieve the MS spectrums data.</param>
/// <param name="experimentID">The experiment ID used to filter the MS spectrums data.</param>
/// <remarks>
/// This class acts as a filter over an existing <see cref="IDataProvider"/>, allowing clients
/// to retrieve MS spectrums data that is associated exclusively with a given experiment ID.
/// </remarks>
internal sealed class ExperimentIDSelectedDataProvider(IDataProvider other, int experimentID) : IDataProvider
{
    /// <summary>
    /// Synchronously loads and returns MS1 spectrums associated with the specified experiment ID.
    /// </summary>
    /// <returns>A read-only collection of <see cref="RawSpectrum"/> objects.</returns>
    public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
        ReadOnlyCollection<RawSpectrum> spectra = other.LoadMs1Spectrums();
        return new ReadOnlyCollection<RawSpectrum>(spectra.Where(s => s.ExperimentID == experimentID).ToArray());
    }

    /// <summary>
    /// Asynchronously loads and returns MS1 spectrums associated with the specified experiment ID.
    /// </summary>
    /// <param name="token">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, with a read-only collection of <see cref="RawSpectrum"/> as the result.</returns>
    public async Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) {
        ReadOnlyCollection<RawSpectrum> spectra = await other.LoadMs1SpectrumsAsync(token).ConfigureAwait(false);
        return new ReadOnlyCollection<RawSpectrum>(spectra.Where(s => s.ExperimentID == experimentID).ToArray());
    }

    /// <summary>
    /// Synchronously loads and returns MSn spectrums for a specific MS level associated with the specified experiment ID.
    /// </summary>
    /// <param name="level">The MS level of the spectrums to load.</param>
    /// <returns>A read-only collection of <see cref="RawSpectrum"/> objects.</returns>
    public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
        var spectra = other.LoadMsNSpectrums(level);
        return new ReadOnlyCollection<RawSpectrum>(spectra.Where(s => s.ExperimentID == experimentID).ToArray());
    }

    /// <summary>
    /// Asynchronously loads and returns MSn spectrums for a specific MS level associated with the specified experiment ID.
    /// </summary>
    /// <param name="level">The MS level of the spectrums to load.</param>
    /// <param name="token">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, with a read-only collection of <see cref="RawSpectrum"/> as the result.</returns>
    public async Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) {
        var spectra = await other.LoadMsNSpectrumsAsync(level, token).ConfigureAwait(false);
        return new ReadOnlyCollection<RawSpectrum>(spectra.Where(s => s.ExperimentID == experimentID).ToArray());
    }

    /// <summary>
    /// Synchronously loads and returns all MS spectrums associated with the specified experiment ID.
    /// </summary>
    /// <returns>A read-only collection of <see cref="RawSpectrum"/> objects.</returns>
    public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
        var spectra = other.LoadMsSpectrums();
        return new ReadOnlyCollection<RawSpectrum>(spectra.Where(s => s.ExperimentID == experimentID).ToArray());
    }

    /// <summary>
    /// Asynchronously loads and returns all MS spectrums associated with the specified experiment ID.
    /// </summary>
    /// <param name="token">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, with a read-only collection of <see cref="RawSpectrum"/> as the result.</returns>
    public async Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) {
        var spectra = await other.LoadMsSpectrumsAsync(token).ConfigureAwait(false);
        return new ReadOnlyCollection<RawSpectrum>(spectra.Where(s => s.ExperimentID == experimentID).ToArray());
    }
}

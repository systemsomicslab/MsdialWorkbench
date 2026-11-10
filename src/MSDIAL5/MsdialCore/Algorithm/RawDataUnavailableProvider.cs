using CompMs.Common.DataObj;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm
{
    /// <summary>
    /// The provider of a sample whose raw data file is not present.
    /// Every load returns an empty result that is already complete: nothing reads the disk, waits, retries or faults,
    /// so a viewer given this provider draws what the project stores and leaves the raw panels empty
    /// (compare <see cref="BaseDataProvider"/>, which retries a missing file and then throws).
    /// </summary>
    public sealed class RawDataUnavailableProvider : IDataProvider
    {
        private static readonly ReadOnlyCollection<RawSpectrum> Empty = new ReadOnlyCollection<RawSpectrum>(Array.Empty<RawSpectrum>());
        private static readonly Task<ReadOnlyCollection<RawSpectrum>> EmptyTask = Task.FromResult(Empty);

        public static RawDataUnavailableProvider Instance { get; } = new RawDataUnavailableProvider();

        private RawDataUnavailableProvider() {

        }

        public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() => Empty;

        public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() => Empty;

        public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) => Empty;

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) => EmptyTask;

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) => EmptyTask;

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) => EmptyTask;
    }
}

using CompMs.Common.DataObj;
using CompMs.Common.Utility;
using CompMs.RawDataHandler.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm
{
    public static class DataProviderExtensions {
        public static RawSpectrum LoadMsSpectrumFromIndex(this IDataProvider provider, int index) {
            if (index < 0) {
                return null;
            }
            return provider.LoadMsSpectrums()[index];
        }

        public static async Task<RawSpectrum> LoadMsSpectrumFromIndexAsync(this IDataProvider provider, int index, CancellationToken token) {
            if (index < 0) {
                return null;
            }
            var spectra = await provider.LoadMsSpectrumsAsync(token).ConfigureAwait(false);
            return spectra[index];
        }

        public static RawSpectrum LoadMs1SpectrumFromIndex(this IDataProvider provider, int index) {
            return provider.LoadMs1Spectrums()[index];
        }

        /// <summary>
        /// An extension method for IDataProvider to retrieve MS2 spectra that have a ScanStartTime within a specified range.
        /// </summary>
        /// <param name="provider">The data provider instance on which the extension method operates.</param>
        /// <param name="start">The lower bound of the retention time range.</param>
        /// <param name="end">The upper bound of the retention time range.</param>
        /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation and returns an array of <see cref="RawSpectrum"/> objects within the specified retention time range.</returns>
        public static async Task<RawSpectrum[]> LoadMs2SpectraWithRtRange(this IDataProvider provider, double start, double end, CancellationToken token) {
            var spectra = await provider.LoadMsNSpectrumsAsync(2, token).ConfigureAwait(false);
            var lower = spectra.LowerBound(start, (t, s) => t.ScanStartTime.CompareTo(s));
            var upper = lower;
            while (upper < spectra.Count && spectra[upper].ScanStartTime < end) {
                ++upper;
            }
            var result = new RawSpectrum[upper - lower];
            for (int i = 0; i < result.Length; i++) {
                result[i] = spectra[i + lower];
            }
            return result;
        }

        public static double GetMinimumCollisionEnergy(this IDataProvider provider) {
            return provider.LoadMsSpectrums().DefaultIfEmpty().Min(s => s?.CollisionEnergy) ?? -1d;
        }
        
        public static (int, int) GetScanNumberRange(this IDataProvider provider) {
            var spectra = provider.LoadMsSpectrums();
            return (spectra.FirstOrDefault()?.ScanNumber ?? 0, spectra.LastOrDefault()?.ScanNumber ?? 0);
        }

        public static (double, double) GetRetentionTimeRange(this IDataProvider provider) {
            var spectra = provider.LoadMsSpectrums();
            return ((float)(spectra.FirstOrDefault()?.ScanStartTime ?? 0d), (float)(spectra.LastOrDefault()?.ScanStartTime ?? 0d));
        }

        public static (double, double) GetMassRange(this IDataProvider provider) {
            var spectra = provider.LoadMsSpectrums();
            return ((float)(spectra.Min(spectrum => spectrum?.LowestObservedMz) ?? 0d), (float)(spectra.Max(spectrum => spectrum?.HighestObservedMz) ?? 0d));
        }

        public static (double, double) GetIntensityRange(this IDataProvider provider) {
            var spectra = provider.LoadMsSpectrums();
            return ((float)(spectra.Min(spectrum => spectrum?.MinIntensity) ?? 0d), (float)(spectra.Max(spectrum => spectrum?.BasePeakIntensity) ?? 0d));
        }

        public static (double, double) GetDriftTimeRange(this IDataProvider provider) {
            var spectra = provider.LoadMsSpectrums();
            return ((float)(spectra.Min(spectrum => spectrum?.DriftTime) ?? 0d), (float)(spectra.Max(spectrum => spectrum?.DriftTime) ?? 0d));
        }

        public static int Count(this IDataProvider provider) {
            return provider.LoadMsSpectrums().Count;
        }

        public static List<double> LoadCollisionEnergyTargets(this IDataProvider provider) {
            return SpectrumParser.LoadCollisionEnergyTargets(provider.LoadMsSpectrums());
        }

        public static IDataProviderFactory<object> AsFactory(this IDataProvider provider) {
            return new IdentityFactory<object>(provider);
        }

        class IdentityFactory<T>(IDataProvider provider) : IDataProviderFactory<T> {
            public IDataProvider Create(T source) => provider;
        }
    }
}

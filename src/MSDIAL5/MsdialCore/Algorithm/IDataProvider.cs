using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm
{
    public interface IDataProvider {
        ReadOnlyCollection<RawSpectrum> LoadMsSpectrums();
        ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums();
        ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level);

        Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token);
        Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token);
        Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token);
    }

    public static class DataProvider {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="ionMode"></param>
        /// <returns>tuple (min Mz, max Mz)</returns>
        public static (float Min, float Max) GetMs1Range(this IDataProvider provider, IonMode ionMode) {
            var spectrumList = provider.LoadMs1Spectrums();
            float minMz = float.MaxValue, maxMz = float.MinValue;
            var scanPolarity = ionMode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            for (int i = 0; i < spectrumList.Count; i++) {
                if (spectrumList[i].MsLevel > 1) continue;
                if (spectrumList[i].ScanPolarity != scanPolarity) continue;
                if (spectrumList[i].DefaultArrayLength == 0) continue;
                if (spectrumList[i].LowestObservedMz == double.MaxValue) continue;
                if (spectrumList[i].HighestObservedMz == double.MinValue) continue;
                //if (spectrumCollection[i].DriftScanNumber > 0) continue;

                if (spectrumList[i].LowestObservedMz < minMz)
                    minMz = (float)spectrumList[i].LowestObservedMz;
                if (spectrumList[i].HighestObservedMz > maxMz)
                    maxMz = (float)spectrumList[i].HighestObservedMz;
            }
            if (minMz > maxMz) {
                return (0f, 0f);
            }
            return (minMz, maxMz);
        }
    }

    public interface IDataProviderFactory<in T>
    {
        IDataProvider Create(T source);
    }

    public static class DataProviderFactory
    {
        public static IDataProviderFactory<U> ContraMap<T, U>(this IDataProviderFactory<T> factory, Func<U, T> map) {
            return new MappedFactory<U, T>(factory, map);
        }

        sealed class MappedFactory<T, U> : IDataProviderFactory<T> {
            private readonly IDataProviderFactory<U> _impl;
            private readonly Func<T, U> _map;

            public MappedFactory(IDataProviderFactory<U> impl, Func<T, U> map) {
                _impl = impl ?? throw new ArgumentNullException(nameof(impl));
                _map = map ?? throw new ArgumentNullException(nameof(map));
            }

            IDataProvider IDataProviderFactory<T>.Create(T source) {
                return _impl.Create(_map(source));
            }
        }
    }
}

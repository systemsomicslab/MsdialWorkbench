using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Utility;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm
{
    public interface IDataProvider
    {
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
                //if (spectrumCollection[i].DriftScanNumber > 0) continue;

                if (spectrumList[i].LowestObservedMz < minMz)
                    minMz = (float)spectrumList[i].LowestObservedMz;
                if (spectrumList[i].HighestObservedMz > maxMz)
                    maxMz = (float)spectrumList[i].HighestObservedMz;
            }
            return (minMz, maxMz);
        }
    }

    public interface IDataProviderFactory<in T>
    {
        IDataProvider Create(T source);
    }
}

using CompMs.Common.DataObj;
using CompMs.Common.Enum;

namespace CompMs.Raw.Contract;

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

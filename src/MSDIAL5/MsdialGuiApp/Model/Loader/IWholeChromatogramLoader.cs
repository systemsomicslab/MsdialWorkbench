using CompMs.App.Msdial.Model.DataObj;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Loader
{
    public interface IWholeChromatogramLoader {
        List<PeakItem> LoadChromatogram();
    }

    public interface IWholeChromatogramLoader<T> {
        List<PeakItem> LoadChromatogram(T state);
    }
}
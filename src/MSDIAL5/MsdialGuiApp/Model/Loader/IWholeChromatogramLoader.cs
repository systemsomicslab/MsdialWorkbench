using CompMs.App.Msdial.Model.DataObj;

namespace CompMs.App.Msdial.Model.Loader
{
    public interface IWholeChromatogramLoader {
        DisplayChromatogram LoadChromatogram();
    }

    public interface IWholeChromatogramLoader<T> {
        DisplayChromatogram LoadChromatogram(T state);
    }
}
using CompMs.App.Msdial.Model.DataObj;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Loader
{
    public interface IWholeChromatogramLoader {
        Task<DisplayChromatogram> LoadChromatogramAsync(CancellationToken token);
    }

    public interface IWholeChromatogramLoader<T> {
        Task<DisplayChromatogram> LoadChromatogramAsync(T state, CancellationToken token);
    }
}
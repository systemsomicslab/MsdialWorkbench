using CompMs.App.Msdial.Model.DataObj;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Loader
{
    public interface IChromatogramLoader<T>
    {
        Task<Chromatogram> LoadChromatogramAsync(T target, CancellationToken token);
    }
}
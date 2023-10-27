using CompMs.App.Msdial.Model.DataObj;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Core
{
    internal interface IAnalysisModel : IResultModel, INotifyPropertyChanged
    {
        Task SaveAsync(CancellationToken token);
    }
}

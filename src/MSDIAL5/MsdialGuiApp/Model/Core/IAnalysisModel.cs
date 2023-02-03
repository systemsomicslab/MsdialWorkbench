using CompMs.App.Msdial.Model.DataObj;
using Reactive.Bindings;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Core
{
    internal interface IAnalysisModel : IResultModel, INotifyPropertyChanged
    {
        ObservableCollection<ChromatogramPeakFeatureModel> Ms1Peaks { get; }

        IReactiveProperty<ChromatogramPeakFeatureModel> Target { get; }

        Task SaveAsync(CancellationToken token);
    }
}

using CompMs.App.Msdial.Model.DataObj;
using Reactive.Bindings;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CompMs.App.Msdial.Model.Core
{
    public interface IAnalysisModel : INotifyPropertyChanged
    {
        ObservableCollection<ChromatogramPeakFeatureModel> Ms1Peaks { get; }

        ReactivePropertySlim<ChromatogramPeakFeatureModel> Target { get; }

        string DisplayLabel { get; }
    }
}

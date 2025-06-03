using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel.SpectraGrouping; 

public class SpectraGroupingViewModel : ViewModelBase {
    public ObservableCollection<string> MzCompoundList { get; } = new();
    public ObservableCollection<AlignmentChromPeakFeatureModel> SampleList { get; } = new();
}

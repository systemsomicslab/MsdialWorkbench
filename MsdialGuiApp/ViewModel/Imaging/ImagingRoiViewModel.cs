using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.CommonMVVM;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel.Imaging
{
    internal sealed class ImagingRoiViewModel : ViewModelBase
    {
        public string Id { get; }
        public ReadOnlyObservableCollection<SampleImageViewModel> Images { get; }
    }
}

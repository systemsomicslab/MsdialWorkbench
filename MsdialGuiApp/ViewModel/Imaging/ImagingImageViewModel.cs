using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.CommonMVVM;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel.Imaging
{
    internal sealed class ImagingImageViewModel : ViewModelBase
    {
        public string ImageTitle { get; }
        public ReadOnlyObservableCollection<ImagingRoiViewModel> RoiViewModels { get; }
        public ImagingRoiViewModel SelectedRoiViewModel {
            get => _selectedRoiViewModel;
            set => SetProperty(ref _selectedRoiViewModel, value);
        }
        private ImagingRoiViewModel _selectedRoiViewModel;
        public AnalysisPeakPlotViewModel PeakPlotViewModel { get; }
        public ViewModelBase[] PeakDetailViewModels { get; }
        public ViewModelBase[] Ms2ViewModels { get; }
    }
}

using CompMs.CommonMVVM;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel.Imaging
{
    internal sealed class ImagingMainViewModel : ViewModelBase
    {
        public ReadOnlyObservableCollection<ImagingImageViewModel> ImageViewModels { get; }
        public ReadOnlyObservableCollection<ImagingRoiCompareViewModel> RoiCompareViewModels { get; }

        public ImagingImageViewModel SelectedImageViewModel {
            get => _selectedImageViewModel;
            set => SetProperty(ref _selectedImageViewModel, value);
        }
        private ImagingImageViewModel _selectedImageViewModel;

        public ImagingRoiCompareViewModel SelectedRoiCompareViewModel {
            get => _selectedRoiCompareViewModel;
            set => SetProperty(ref _selectedRoiCompareViewModel, value);
        }
        private ImagingRoiCompareViewModel _selectedRoiCompareViewModel;
    }
}

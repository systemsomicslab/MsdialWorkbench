using CompMs.App.Msdial.Model.Imaging;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel.Imaging
{
    internal sealed class ImagingImageViewModel : ViewModelBase
    {
        public ImagingImageViewModel(ImagingImageModel model) {
            _model = model ?? throw new System.ArgumentNullException(nameof(model));
            RoiViewModels = model.ImagingRoiModels.ToReadOnlyReactiveCollection(m => new ImagingRoiViewModel(m)).AddTo(Disposables);
            ImageResultViewModel = new WholeImageResultViewModel(model.ImageResult).AddTo(Disposables);
            var peakInfo = new PeakInformationViewModel(model.PeakInformationModel).AddTo(Disposables);
            var moleculeStructure = new MoleculeStructureViewModel(model.MoleculeStructureModel).AddTo(Disposables);
            PeakDetailViewModels = new ViewModelBase[] { peakInfo, moleculeStructure, };
            Ms2ViewModels = new ViewModelBase[0];
        }

        public string ImageTitle => _model.File.AnalysisFileName;
        public ReadOnlyObservableCollection<ImagingRoiViewModel> RoiViewModels { get; }
        public ImagingRoiViewModel SelectedRoiViewModel {
            get => _selectedRoiViewModel;
            set => SetProperty(ref _selectedRoiViewModel, value);
        }
        private ImagingRoiViewModel _selectedRoiViewModel;
        private readonly ImagingImageModel _model;

        public WholeImageResultViewModel ImageResultViewModel { get; }
        public ViewModelBase[] PeakDetailViewModels { get; }
        public ViewModelBase[] Ms2ViewModels { get; }
    }
}

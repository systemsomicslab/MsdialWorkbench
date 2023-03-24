using CompMs.App.Msdial.Model.ImagingImms;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Helpers;
using Reactive.Bindings.Notifiers;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel.Imaging
{
    internal sealed class ImagingImageViewModel : ViewModelBase
    {
        private readonly ImagingImmsImageModel _model;

        public ImagingImageViewModel(ImagingImmsImageModel model, IMessageBroker broker) {
            _model = model ?? throw new System.ArgumentNullException(nameof(model));
            RoiViewModels = model.ImagingRoiModels.ToReadOnlyReactiveCollection(m => new ImagingRoiViewModel(m)).AddTo(Disposables);
            SelectedRoiViewModels = RoiViewModels.ToFilteredReadOnlyObservableCollection(vm => vm.IsSelected.Value, vm => vm.IsSelected).AddTo(Disposables);
            ImageResultViewModel = new WholeImageResultViewModel(model.ImageResult).AddTo(Disposables);
            var peakInfo = new PeakInformationViewModel(model.PeakInformationModel).AddTo(Disposables);
            var moleculeStructure = new MoleculeStructureViewModel(model.MoleculeStructureModel).AddTo(Disposables);
            PeakDetailViewModels = new ViewModelBase[] { peakInfo, moleculeStructure, };
            Ms2ViewModels = new ViewModelBase[0];
            RoiEditViewModel = new RoiEditViewModel(model.RoiEditModel).AddTo(Disposables);
            SaveImagesViewModel = new SaveImagesViewModel(model.SaveImagesModel, broker).AddTo(Disposables);
            AddRoiCommand = new AsyncReactiveCommand().WithSubscribe(model.AddRoiAsync).AddTo(Disposables);
        }

        public string ImageTitle => _model.File.AnalysisFileName;
        public ReadOnlyObservableCollection<ImagingRoiViewModel> RoiViewModels { get; }
        public IFilteredReadOnlyObservableCollection<ImagingRoiViewModel> SelectedRoiViewModels { get; }
        public WholeImageResultViewModel ImageResultViewModel { get; }
        public RoiEditViewModel RoiEditViewModel { get; }
        public SaveImagesViewModel SaveImagesViewModel { get; }
        public ViewModelBase[] PeakDetailViewModels { get; }
        public ViewModelBase[] Ms2ViewModels { get; }

        public AsyncReactiveCommand AddRoiCommand { get; }
    }
}

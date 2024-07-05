using CompMs.App.Msdial.Model.Imaging;
using CompMs.App.Msdial.Model.ImagingImms;
using CompMs.App.Msdial.ViewModel.Imaging;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Helpers;
using Reactive.Bindings.Notifiers;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel.ImagingImms
{
    internal sealed class ImagingImmsImageViewModel : ViewModelBase
    {
        private readonly ImagingImmsImageModel _model;

        public ImagingImmsImageViewModel(ImagingImmsImageModel model, FocusControlManager focusManager, IMessageBroker broker, IWindowService<PeakSpotTableViewModelBase> peakSpotTableService) {
            _model = model ?? throw new System.ArgumentNullException(nameof(model));
            RoiViewModels = model.ImagingRoiModels.ToReadOnlyReactiveCollection(m => new ImagingRoiViewModel(m)).AddTo(Disposables);
            SelectedRoiViewModels = RoiViewModels.ToFilteredReadOnlyObservableCollection(vm => vm.IsSelected.Value, vm => vm.IsSelected).AddTo(Disposables);
            ImageResultViewModel = new WholeImageResultViewModel(model.ImageResult, focusManager, peakSpotTableService, broker).AddTo(Disposables);
            var peakInfo = new PeakInformationViewModel(model.PeakInformationModel).AddTo(Disposables);
            var moleculeStructure = new MoleculeStructureViewModel(model.MoleculeStructureModel).AddTo(Disposables);
            RoiEditViewModel = new RoiEditViewModel(model.RoiEditModel).AddTo(Disposables);
            SaveImagesViewModel = new SaveImagesViewModel(model.SaveImagesModel, broker).AddTo(Disposables);
            AddRoiCommand = new AsyncReactiveCommand().WithSubscribe(model.AddRoiAsync).AddTo(Disposables);
            RemoveRoiCommand = new ReactiveCommand<ImagingRoiModel>().WithSubscribe(model.RemoveRoi).AddTo(Disposables);
            SaveIntensitiesCommand = new AsyncReactiveCommand().WithSubscribe(() => model.SaveIntensitiesAsync()).AddTo(Disposables);
            LoadRoiCommand = new ReactiveCommand().WithSubscribe(model.LoadRoi).AddTo(Disposables);
        }

        public string ImageTitle => _model.File.AnalysisFileName;
        public ReadOnlyObservableCollection<ImagingRoiViewModel> RoiViewModels { get; }
        public IFilteredReadOnlyObservableCollection<ImagingRoiViewModel> SelectedRoiViewModels { get; }
        public WholeImageResultViewModel ImageResultViewModel { get; }
        public RoiEditViewModel RoiEditViewModel { get; }
        public SaveImagesViewModel SaveImagesViewModel { get; }
        public ViewModelBase[] PeakDetailViewModels => ImageResultViewModel.AnalysisViewModel.PeakDetailViewModels;
        public AsyncReactiveCommand AddRoiCommand { get; }
        public ReactiveCommand<ImagingRoiModel> RemoveRoiCommand { get; }
        public AsyncReactiveCommand SaveIntensitiesCommand { get; }
        public ReactiveCommand LoadRoiCommand { get; }
    }
}

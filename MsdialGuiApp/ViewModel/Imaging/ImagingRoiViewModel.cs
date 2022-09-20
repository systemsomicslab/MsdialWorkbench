using CompMs.App.Msdial.Model.Imaging;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Imaging
{
    internal sealed class ImagingRoiViewModel : ViewModelBase
    {
        public ImagingRoiViewModel(ImagingRoiModel model) {
            Intensities = model.Intensities.ToReadOnlyReactiveCollection(intensity => new RoiIntensitiesViewModel(intensity)).AddTo(Disposables);
            SelectedPeakIntensities = model.SelectedPeakIntensities.Select(m => Intensities.FirstOrDefault(vm => vm.Model == m)).ToReactiveProperty().AddTo(Disposables);
        }

        public string Id { get; }
        public ReadOnlyReactiveCollection<RoiIntensitiesViewModel> Intensities { get; }

        public ReactiveProperty<RoiIntensitiesViewModel> SelectedPeakIntensities { get; }
    }
}

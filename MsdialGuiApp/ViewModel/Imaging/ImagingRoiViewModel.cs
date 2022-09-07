using CompMs.App.Msdial.Model.Imaging;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel.Imaging
{
    internal sealed class ImagingRoiViewModel : ViewModelBase
    {
        public ImagingRoiViewModel(ImagingRoiModel model) {
            Intensities = model.Intensities.ToReadOnlyReactiveCollection(intensity => new RoiIntensitiesViewModel(intensity)).AddTo(Disposables);
        }

        public string Id { get; }
        public ReadOnlyReactiveCollection<RoiIntensitiesViewModel> Intensities { get; }
    }
}

using CompMs.App.Msdial.Model.Imaging;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.ViewModel.Imaging
{
    internal sealed class RoiIntensitiesViewModel : ViewModelBase
    {
        private readonly RoiIntensitiesModel _model;

        public RoiIntensitiesViewModel(RoiIntensitiesModel model) {
            _model = model ?? throw new System.ArgumentNullException(nameof(model));
            SampleImageViewModel = new SampleImageViewModel(model.SampleImageModel).AddTo(Disposables);
        }

        public string Title => $"{_model.Mz} {_model.Drift}";

        public SampleImageViewModel SampleImageViewModel { get; }
    }
}

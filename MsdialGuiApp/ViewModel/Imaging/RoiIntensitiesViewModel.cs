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
            BitmapImageViewModel = new BitmapImageViewModel(model.BitmapImageModel).AddTo(Disposables);
        }

        public string Title => $"{_model.Mz} {_model.Drift}";

        public BitmapImageViewModel BitmapImageViewModel { get; }
    }
}

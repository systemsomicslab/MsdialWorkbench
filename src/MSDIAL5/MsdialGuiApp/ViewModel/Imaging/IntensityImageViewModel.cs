using CompMs.App.Msdial.Model.Imaging;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.ViewModel.Imaging
{
    internal sealed class IntensityImageViewModel : ViewModelBase
    {
        private readonly IntensityImageModel _model;

        public IntensityImageViewModel(IntensityImageModel model) {
            _model = model ?? throw new System.ArgumentNullException(nameof(model));
            BitmapImageViewModel = new BitmapImageViewModel(model.BitmapImageModel).AddTo(Disposables);
        }

        public IntensityImageModel Model => _model;

        public string Title => $"{_model.Mz} {_model.Drift}";

        public BitmapImageViewModel BitmapImageViewModel { get; }
    }
}

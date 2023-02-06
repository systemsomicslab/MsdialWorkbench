using CompMs.App.Msdial.Model.Imaging;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Imaging
{
    internal sealed class RoiViewModel : ViewModelBase
    {
        private readonly RoiModel _model;

        public RoiViewModel(RoiModel model) {
            _model = model ?? throw new System.ArgumentNullException(nameof(model));
            RoiImage = new BitmapImageViewModel(model.RoiImage).AddTo(Disposables);
        }

        public BitmapImageViewModel RoiImage { get; }
        public Color Color => _model.Color;
    }
}

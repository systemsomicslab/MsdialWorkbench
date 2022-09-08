using CompMs.App.Msdial.Model.Imaging;
using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.ViewModel.Imaging
{
    internal sealed class RoiViewModel : ViewModelBase
    {
        private readonly RoiModel _model;

        public RoiViewModel(RoiModel model) {
            _model = model ?? throw new System.ArgumentNullException(nameof(model));
        }
    }
}

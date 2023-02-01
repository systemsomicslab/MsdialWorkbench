using CompMs.App.Msdial.Model.Chart;
using CompMs.CommonMVVM;
using System.Windows.Media.Imaging;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class BitmapImageViewModel : ViewModelBase
    {
        private readonly BitmapImageModel _model;

        public BitmapImageViewModel(BitmapImageModel model) {
            _model = model;
        }

        public string Title => _model.Title;
        public BitmapSource BitmapSource => _model.BitmapSource;
    }
}

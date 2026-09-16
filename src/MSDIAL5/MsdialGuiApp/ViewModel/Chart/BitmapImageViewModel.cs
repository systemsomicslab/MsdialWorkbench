using CompMs.App.Msdial.Model.Chart;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class BitmapImageViewModel : ViewModelBase
    {
        private readonly BitmapImageModel _model;

        public BitmapImageViewModel(BitmapImageModel model) {
            _model = model;
            BitmapSource = model.ObserveProperty(m => m.BitmapSource).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public string Title => _model.Title;
        public ReadOnlyReactivePropertySlim<BitmapSource?> BitmapSource { get; }

        public async Task EnsureBitmapSourceAsync() {
            await _model.EnsureBitmapSourceAsync().ConfigureAwait(false);
        }
    }
}

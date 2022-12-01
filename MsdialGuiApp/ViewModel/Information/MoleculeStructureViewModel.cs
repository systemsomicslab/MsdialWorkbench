using CompMs.App.Msdial.Model.Information;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CompMs.App.Msdial.ViewModel.Information
{
    internal sealed class MoleculeStructureViewModel : ViewModelBase
    {
        private readonly MoleculeStructureModel _model;

        public MoleculeStructureViewModel(MoleculeStructureModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            var current = model.ObserveProperty(m => m.Current).Where(c => c != null).ToReactiveProperty().AddTo(Disposables);
            Image = current.Select(c => Observable.FromAsync(() => c?.Image ?? Task.FromResult<BitmapSource>(null)).StartWith((BitmapSource)null)).Switch()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            IsLoading = current.Select(c => c?.ObserveProperty(m => m.IsLoading) ?? Observable.Never<bool>()).Switch()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            IsFailed = current.Select(c => c?.ObserveProperty(m => m.IsFailed) ?? Observable.Never<bool>()).Switch()
                .ToReactiveProperty()
                .AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<BitmapSource> Image { get; }
        public ReadOnlyReactivePropertySlim<bool> IsLoading { get; }
        public IReadOnlyReactiveProperty<bool> IsFailed { get; }
    }
}

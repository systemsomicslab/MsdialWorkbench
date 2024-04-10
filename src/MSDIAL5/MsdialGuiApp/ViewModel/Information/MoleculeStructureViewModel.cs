using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;
using System.Windows.Media.Imaging;

namespace CompMs.App.Msdial.ViewModel.Information
{
    internal sealed class MoleculeStructureViewModel : ViewModelBase
    {
        private readonly MoleculeStructureModel _model;

        public MoleculeStructureViewModel(MoleculeStructureModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            ReactiveProperty<MoleculeImage?> current = model.ObserveProperty(m => m.Current).SkipNull().ToReactiveProperty().AddTo(Disposables);
            Image = new[]
            {
                current.TakeNull().ToConstant((BitmapSource?)null),
                current.SkipNull().Select(c => c.Image).Switch(),
            }.Merge()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
            IsLoading = current.SkipNull().SelectSwitch(c => c!.ObserveProperty(m => m.IsLoading))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            IsFailed = current.SkipNull().SelectSwitch(c => c!.ObserveProperty(m => m.IsFailed))
                .ToReactiveProperty()
                .AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<BitmapSource?> Image { get; }
        public ReadOnlyReactivePropertySlim<bool> IsLoading { get; }
        public IReadOnlyReactiveProperty<bool> IsFailed { get; }
    }
}

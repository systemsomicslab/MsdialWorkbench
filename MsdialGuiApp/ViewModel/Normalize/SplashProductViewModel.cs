using CompMs.App.Msdial.Model.Normalize;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.ViewModel.Normalize
{
    class SplashProductViewModel : ViewModelBase {
        public SplashProductViewModel(SplashProduct product) {
            Model = product;
            Lipids = Model.Lipids
                .ToReadOnlyReactiveCollection(m => new StandardCompoundViewModel(m))
                .AddTo(Disposables);
        }

        public SplashProduct Model { get; }

        public string Label => Model.Label;

        public ReadOnlyReactiveCollection<StandardCompoundViewModel> Lipids { get; }
    }
}

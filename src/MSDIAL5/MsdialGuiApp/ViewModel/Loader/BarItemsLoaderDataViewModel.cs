using CompMs.App.Msdial.Model.Loader;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Loader
{
    internal sealed class BarItemsLoaderDataViewModel : ViewModelBase
    {
        private readonly IReactiveProperty<BarItemsLoaderData> _selectedData;

        public BarItemsLoaderDataViewModel(BarItemsLoaderData model, IReactiveProperty<BarItemsLoaderData> selectedData) {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            _selectedData = selectedData ?? throw new ArgumentNullException(nameof(selectedData));
            SwitchCommand = Model.IsEnabled.ToReactiveCommand().WithSubscribe(() => _selectedData.Value = Model).AddTo(Disposables);
            IsEnabled = Model.IsEnabled.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public BarItemsLoaderData Model { get; }
        public string Label => Model.Label;
        public IObservable<IBarItemsLoader> Loader => Observable.Return(Model.Loader);
        public ICommand SwitchCommand { get; }
        public ReadOnlyReactivePropertySlim<bool> IsEnabled { get; }
    }
}

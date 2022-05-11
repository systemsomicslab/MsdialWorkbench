using CompMs.App.Msdial.Model.Search;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.ViewModel.Search
{
    public class FocusNavigatorViewModel : ViewModelBase
    {
        public FocusNavigatorViewModel(FocusNavigatorModel focusNavigator) {
            SpotFocuses = focusNavigator.SpotFocuses
                .ToReadOnlyReactiveCollection(m => new SpotFocusViewModel(m))
                .AddTo(Disposables);
        }

        public ReadOnlyReactiveCollection<SpotFocusViewModel> SpotFocuses { get; }
    }
}

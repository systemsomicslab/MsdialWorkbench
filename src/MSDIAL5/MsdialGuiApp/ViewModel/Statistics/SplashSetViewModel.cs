using CompMs.App.Msdial.Model.Statistics;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.ViewModel.Statistics
{
    internal sealed class SplashSetViewModel : ViewModelBase
    {
        private readonly SplashSetModel _model;

        public SplashSetViewModel(SplashSetModel model) {
            _model = model;

            TargetMetabolites = _model.TargetMetabolites;
            SplashProducts = _model.SplashProducts
                .ToReadOnlyReactiveCollection(product => new SplashProductViewModel(product))
                .AddTo(Disposables);
            SplashProduct = model.ToReactivePropertyAsSynchronized(
                m => m.SplashProduct,
                m => SplashProducts.FirstOrDefault(vm => vm.Model == m),
                vm => vm.Model,
                ignoreValidationErrorValue: true)
                .AddTo(Disposables);

            OutputUnits = new ReadOnlyObservableCollection<IonAbundance>(_model.OutputUnits);
            OutputUnit = _model
                .ToReactivePropertyAsSynchronized(m => m.OutputUnit, ignoreValidationErrorValue: true)
                .AddTo(Disposables);
        }

        public ReadOnlyCollection<string> TargetMetabolites { get; }

        public ReadOnlyObservableCollection<SplashProductViewModel> SplashProducts { get; }

        public ReactiveProperty<SplashProductViewModel> SplashProduct { get; }

        public ReadOnlyObservableCollection<IonAbundance> OutputUnits { get; }

        public ReactiveProperty<IonAbundance> OutputUnit { get; }

        public DelegateCommand FindCommand => _findCommand ??= new DelegateCommand(Find);
        private DelegateCommand? _findCommand;

        private void Find() {
            _model.Find();
            SplashProduct.Value?.Refresh();
        }

        public DelegateCommand AddLastCommand => _addLastCommand ??= new DelegateCommand(_model.AddLast);
        private DelegateCommand? _addLastCommand;

        public DelegateCommand DeleteCommand => _deleteCommand ??= new DelegateCommand(_model.Delete);
        private DelegateCommand? _deleteCommand;
    }
}

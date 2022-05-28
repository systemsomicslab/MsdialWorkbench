using CompMs.App.Msdial.Model.Normalize;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.ViewModel.Normalize
{
    internal class SplashSetViewModel : ViewModelBase
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

        public ReadOnlyObservableCollection<StandardCompoundViewModel> StandardCompounds => SplashProduct.Value?.Lipids;

        public ReadOnlyObservableCollection<SplashProductViewModel> SplashProducts { get; }

        public ReactiveProperty<SplashProductViewModel> SplashProduct { get; }

        public ReadOnlyObservableCollection<IonAbundance> OutputUnits { get; }

        public ReactiveProperty<IonAbundance> OutputUnit { get; }

        public DelegateCommand FindCommand => findCommand ?? (findCommand = new DelegateCommand(Find));

        private DelegateCommand findCommand;

        private void Find() {
            _model.Find();
            foreach (var compound in StandardCompounds) {
                compound.Refresh();
            }
        }

        public DelegateCommand NormalizeCommand => normalizeCommand ?? (normalizeCommand = new DelegateCommand(_model.Normalize));//, Model.CanNormalize));
        private DelegateCommand normalizeCommand;
    }
}

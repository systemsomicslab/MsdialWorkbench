using CompMs.App.Msdial.Model.Normalize;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Common;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel.Normalize
{
    public class SplashSetViewModel : ViewModelBase
    {
        public SplashSetViewModel() {
            Model = new SplashSetModel();

            SplashProducts = new ReadOnlyObservableCollection<string>(Model.SplashProducts);
            SplashProduct = Model.SplashProduct;
            OutputUnits = new ReadOnlyObservableCollection<string>(Model.OutputUnits);
            outputUnit = Model.OutputUnit;

            var notifier = new PropertyChangedNotifier(Model);
            Disposables.Add(notifier);
            notifier
                .SubscribeTo(nameof(Model.StandardCompounds), () => OnPropertyChanged(nameof(StandardCompounds)))
                .SubscribeTo(nameof(Model.SplashProduct), () => SplashProduct = Model.SplashProduct)
                .SubscribeTo(nameof(Model.OutputUnit), () => OutputUnit = Model.OutputUnit);
        }

        public SplashSetModel Model { get; }

        public ObservableCollection<StandardCompound> StandardCompounds => Model.StandardCompounds;

        public ReadOnlyObservableCollection<string> SplashProducts { get; }

        public string SplashProduct {
            get {
                return splashProduct;
            }
            set {
                if (SetProperty(ref splashProduct, value)) {
                    if (!ContainsError(nameof(SplashProduct))) {
                        Model.SplashProduct = splashProduct;
                    }
                }
            }
        }

        private string splashProduct;

        public ReadOnlyObservableCollection<string> OutputUnits { get; }

        public string OutputUnit {
            get {
                return outputUnit;
            }
            set {
                if (SetProperty(ref outputUnit, value)) {
                    if (!ContainsError(nameof(OutputUnit))) {
                        Model.OutputUnit = outputUnit;
                    }
                }
            }
        }

        private string outputUnit;

        public DelegateCommand FindCommand => findCommand ?? (findCommand = new DelegateCommand(() => { }));

        private DelegateCommand findCommand;

        public DelegateCommand NormalizeCommand => normalizeCommand ?? (normalizeCommand = new DelegateCommand(() => { }));

        private DelegateCommand normalizeCommand;
    }
}

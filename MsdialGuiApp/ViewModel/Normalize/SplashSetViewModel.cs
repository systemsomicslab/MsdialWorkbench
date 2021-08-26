using CompMs.App.Msdial.Model.Normalize;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Common;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.ViewModel.Normalize
{
    class SplashSetViewModel : ViewModelBase
    {
        public SplashSetViewModel(AlignmentResultContainer container, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, ParameterBase parameter, DataBaseMapper mapper) {
            Model = new SplashSetModel(container, refer, parameter, mapper);

            TargetMetabolites = Model.TargetMetabolites;

            var splashProducts = Model.SplashProducts.ToMappedReadOnlyObservableCollection(product => new SplashProductViewModel(product));
            SplashProducts = splashProducts;
            Disposables.Add(splashProducts);
            SplashProduct = SplashProducts.FirstOrDefault();

            OutputUnits = new ReadOnlyObservableCollection<IonAbundance>(Model.OutputUnits);
            outputUnit = OutputUnits.FirstOrDefault();
        }

        public SplashSetModel Model { get; }

        public ReadOnlyCollection<string> TargetMetabolites { get; }

        public ObservableCollection<StandardCompoundViewModel> StandardCompounds => SplashProduct.Lipids;

        public ReadOnlyObservableCollection<SplashProductViewModel> SplashProducts { get; }

        public SplashProductViewModel SplashProduct {
            get {
                return splashProduct;
            }
            set {
                if (SetProperty(ref splashProduct, value)) {
                    if (!ContainsError(nameof(SplashProduct))) {
                        Model.SplashProduct = splashProduct.Model;
                    }
                }
            }
        }

        private SplashProductViewModel splashProduct;

        public ReadOnlyObservableCollection<IonAbundance> OutputUnits { get; }

        public IonAbundance OutputUnit {
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

        private IonAbundance outputUnit;

        public DelegateCommand FindCommand => findCommand ?? (findCommand = new DelegateCommand(Find));

        private DelegateCommand findCommand;

        private void Find() {
            Model.Find();
            foreach (var compound in StandardCompounds) {
                compound.Refresh();
            }
        }

        public DelegateCommand NormalizeCommand => normalizeCommand ?? (normalizeCommand = new DelegateCommand(Model.Normalize));//, Model.CanNormalize));

        private DelegateCommand normalizeCommand;
    }

    class SplashProductViewModel : ViewModelBase {
        public SplashProductViewModel(SplashProduct product) {
            Model = product;
            var lipids = Model.Lipids.ToMappedObservableCollection(
                lipid => new StandardCompoundViewModel(lipid),
                lipidvm => lipidvm.Compound);
            Disposables.Add(lipids);
            Lipids = lipids;
        }

        public SplashProduct Model { get; }

        public string Label => Model.Label;

        public ObservableCollection<StandardCompoundViewModel> Lipids { get; }
    }
}

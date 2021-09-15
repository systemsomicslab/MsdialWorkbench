using CompMs.App.Msdial.View.Setting;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.Proteomics.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel {
    public class ProteomicsParameterVM : ViewModelBase {

        [Required(ErrorMessage = "Andromeda delta required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, double.MaxValue, ErrorMessage = "Positive value required.")]
        public ReactiveProperty<string> AndromedaDelta { get; }

        [Required(ErrorMessage = "Andromeda max peaks required.")]
        [RegularExpression("[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, double.MaxValue, ErrorMessage = "Positive value required.")]
        public ReactiveProperty<string> AndromedaMaxPeaks { get; }

        [Required(ErrorMessage = "False discovery rate for peptide required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, double.MaxValue, ErrorMessage = "Positive value required.")]
        public ReactiveProperty<string> FalseDiscoveryRateForPeptide { get; }
      
        [Required(ErrorMessage = "False discovery rate for protein required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, double.MaxValue, ErrorMessage = "Positive value required.")]
        public ReactiveProperty<string> FalseDiscoveryRateForProtein { get; }

        private readonly ProteomicsParameter model;

        public List<Enzyme> Enzymes { get => model.EnzymesForDigestion; }
        public int MaxMissedCleavage { get => model.MaxMissedCleavage; }
        public List<Modification> VariableModifications { get => model.VariableModifications; }
        public List<Modification> FixedModifications { get => model.FixedModifications; }
        public int MaxNumberOfModificationsPerPeptide { get => model.MaxNumberOfModificationsPerPeptide; }

        public ProteomicsParameterVM(ProteomicsParameter model) {
            this.model = model;
          
            AndromedaDelta = new ReactiveProperty<string>(model.AndromedaDelta.ToString())
              .SetValidateAttribute(() => AndromedaDelta)
              .AddTo(Disposables);
            AndromedaDelta.ObserveHasErrors.Where(e => !e)
                .Select(_ => float.Parse(AndromedaDelta.Value))
                .Subscribe(x => this.model.AndromedaDelta = x)
                .AddTo(Disposables);

            AndromedaMaxPeaks = new ReactiveProperty<string>(model.AndromedaMaxPeaks.ToString())
            .SetValidateAttribute(() => AndromedaMaxPeaks)
            .AddTo(Disposables);
            AndromedaMaxPeaks.ObserveHasErrors.Where(e => !e)
                .Select(_ => int.Parse(AndromedaMaxPeaks.Value))
                .Subscribe(x => this.model.AndromedaMaxPeaks = x)
                .AddTo(Disposables);

            FalseDiscoveryRateForPeptide = new ReactiveProperty<string>(model.FalseDiscoveryRateForPeptide.ToString())
             .SetValidateAttribute(() => FalseDiscoveryRateForPeptide)
             .AddTo(Disposables);
            FalseDiscoveryRateForPeptide.ObserveHasErrors.Where(e => !e)
                .Select(_ => float.Parse(FalseDiscoveryRateForPeptide.Value))
                .Subscribe(x => this.model.FalseDiscoveryRateForPeptide = x)
                .AddTo(Disposables);

            FalseDiscoveryRateForProtein = new ReactiveProperty<string>(model.FalseDiscoveryRateForProtein.ToString())
             .SetValidateAttribute(() => FalseDiscoveryRateForProtein)
             .AddTo(Disposables);
            FalseDiscoveryRateForProtein.ObserveHasErrors.Where(e => !e)
                .Select(_ => float.Parse(FalseDiscoveryRateForProtein.Value))
                .Subscribe(x => this.model.FalseDiscoveryRateForProtein = x)
                .AddTo(Disposables);

        }

        public DelegateCommand EnzymeSetCommand => enzymeSetCommand ?? (enzymeSetCommand = new DelegateCommand(EnzymeSet));
        private DelegateCommand enzymeSetCommand;

        private void EnzymeSet() {
            using (var vm = new EnzymeSettingViewModel(model)) {
                var window = new EnzymeSettingWin {
                    DataContext = vm,
                };
                window.ShowDialog();
            }
        }

        public DelegateCommand ModificationSetCommand => modificationSetCommand ?? (modificationSetCommand = new DelegateCommand(ModificationSet));
        private DelegateCommand modificationSetCommand;

        private void ModificationSet() {
            using (var vm = new ModificationSettingViewModel(model)) {
                var window = new ModificationSettingWin {
                    DataContext = vm,
                };
                window.ShowDialog();
            }
        }



    }
}

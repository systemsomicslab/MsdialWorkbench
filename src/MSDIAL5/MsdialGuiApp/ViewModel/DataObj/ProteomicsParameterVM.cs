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

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    public sealed class ProteomicsParameterVM : ViewModelBase
    {

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

        [Required(ErrorMessage = "Minimum peptide mass required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, double.MaxValue, ErrorMessage = "Positive value required.")]
        public ReactiveProperty<string> MinPeptideMass { get; }

        [Required(ErrorMessage = "Max peptide mass required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, double.MaxValue, ErrorMessage = "Positive value required.")]
        public ReactiveProperty<string> MaxPeptideMass { get; }

        [Required(ErrorMessage = "Minimum MS2 m/z value required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, double.MaxValue, ErrorMessage = "Positive value required.")]
        public ReactiveProperty<string> MinMs2Mz { get; }

        [Required(ErrorMessage = "Max MS2 m/z value required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, double.MaxValue, ErrorMessage = "Positive value required.")]
        public ReactiveProperty<string> MaxMs2Mz { get; }

        [Required(ErrorMessage = "Minimum peptide length required.")]
        [RegularExpression("[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, double.MaxValue, ErrorMessage = "Positive value required.")]
        public ReactiveProperty<string> MinimumPeptideLength { get; }

        private readonly ProteomicsParameter model;

        public List<Enzyme> Enzymes { get => model.EnzymesForDigestion; }
        public int MaxMissedCleavage { get => model.MaxMissedCleavage; }
        public List<Modification> VariableModifications { get => model.VariableModifications; }
        public List<Modification> FixedModifications { get => model.FixedModifications; }
        public int MaxNumberOfModificationsPerPeptide { get => model.MaxNumberOfModificationsPerPeptide; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public ProteomicsParameterVM(ProteomicsParameter model) {
            this.model = model;

            AndromedaDelta = new ReactiveProperty<string>(model.AndromedaDelta.ToString())
              .SetValidateAttribute(() => AndromedaDelta)
              .AddTo(Disposables);
            AndromedaDelta.Where(_ => !AndromedaDelta.HasErrors)
                .Select(x => float.Parse(x))
                .Subscribe(x => this.model.AndromedaDelta = x)
                .AddTo(Disposables);

            AndromedaMaxPeaks = new ReactiveProperty<string>(model.AndromedaMaxPeaks.ToString())
            .SetValidateAttribute(() => AndromedaMaxPeaks)
            .AddTo(Disposables);
            AndromedaMaxPeaks.Where(_ => !AndromedaMaxPeaks.HasErrors)
                .Select(x => int.Parse(x))
                .Subscribe(x => this.model.AndromedaMaxPeaks = x)
                .AddTo(Disposables);

            FalseDiscoveryRateForPeptide = new ReactiveProperty<string>(model.FalseDiscoveryRateForPeptide.ToString())
             .SetValidateAttribute(() => FalseDiscoveryRateForPeptide)
             .AddTo(Disposables);
            FalseDiscoveryRateForPeptide.Where(e => !FalseDiscoveryRateForPeptide.HasErrors)
                .Select(x => float.Parse(x))
                .Subscribe(x => this.model.FalseDiscoveryRateForPeptide = x)
                .AddTo(Disposables);

            FalseDiscoveryRateForProtein = new ReactiveProperty<string>(model.FalseDiscoveryRateForProtein.ToString())
             .SetValidateAttribute(() => FalseDiscoveryRateForProtein)
             .AddTo(Disposables);
            FalseDiscoveryRateForProtein.Where(e => !FalseDiscoveryRateForProtein.HasErrors)
                .Select(x => float.Parse(x))
                .Subscribe(x => this.model.FalseDiscoveryRateForProtein = x)
                .AddTo(Disposables);

            MinPeptideMass = new ReactiveProperty<string>(model.MinPeptideMass.ToString())
                .SetValidateAttribute(() => MinPeptideMass)
                .AddTo(Disposables);
            MinPeptideMass.Where(_ => !MinPeptideMass.HasErrors)
                .Select(x => int.Parse(x))
                .Subscribe(x => this.model.MinPeptideMass = x)
                .AddTo(Disposables);

            MaxPeptideMass = new ReactiveProperty<string>(model.MaxPeptideMass.ToString())
                .SetValidateAttribute(() => MaxPeptideMass)
                .AddTo(Disposables);
            MaxPeptideMass.Where(_ => !MaxPeptideMass.HasErrors)
                .Select(x => int.Parse(x))
                .Subscribe(x => this.model.MaxPeptideMass = x)
                .AddTo(Disposables);

            MinMs2Mz = new ReactiveProperty<string>(model.MinMs2Mz.ToString())
               .SetValidateAttribute(() => MinMs2Mz)
               .AddTo(Disposables);
            MinMs2Mz.Where(_ => !MinMs2Mz.HasErrors)
                .Select(x => int.Parse(x))
                .Subscribe(x => this.model.MinMs2Mz = x)
                .AddTo(Disposables);

            MaxMs2Mz = new ReactiveProperty<string>(model.MaxMs2Mz.ToString())
               .SetValidateAttribute(() => MaxMs2Mz)
               .AddTo(Disposables);
            MaxMs2Mz.Where(_ => !MaxMs2Mz.HasErrors)
                .Select(x => int.Parse(x))
                .Subscribe(x => this.model.MaxMs2Mz = x)
                .AddTo(Disposables);

            MinimumPeptideLength = new ReactiveProperty<string>(model.MinimumPeptideLength.ToString())
                .SetValidateAttribute(() => MinimumPeptideLength)
                .AddTo(Disposables);
            MinimumPeptideLength.Where(_ => !MinimumPeptideLength.HasErrors)
                .Select(x => int.Parse(x))
                .Subscribe(x => this.model.MinimumPeptideLength = x)
                .AddTo(Disposables);


            ObserveHasErrors = new[]
            {
                AndromedaDelta.ObserveHasErrors,
                AndromedaMaxPeaks.ObserveHasErrors,
                FalseDiscoveryRateForPeptide.ObserveHasErrors,
                FalseDiscoveryRateForProtein.ObserveHasErrors,
                MinPeptideMass.ObserveHasErrors,
                MaxPeptideMass.ObserveHasErrors,
                MinMs2Mz.ObserveHasErrors,
                MaxMs2Mz.ObserveHasErrors,
                MinimumPeptideLength.ObserveHasErrors,

            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public DelegateCommand EnzymeSetCommand => enzymeSetCommand ??= new DelegateCommand(EnzymeSet);
        private DelegateCommand? enzymeSetCommand;

        private void EnzymeSet() {
            using var vm = new EnzymeSettingViewModel(model);
            var window = new EnzymeSettingWin
            {
                DataContext = vm,
            };
            window.ShowDialog();
        }

        public DelegateCommand ModificationSetCommand => modificationSetCommand ??= new DelegateCommand(ModificationSet);

        private DelegateCommand? modificationSetCommand;

        private void ModificationSet() {
            using var vm = new ModificationSettingViewModel(model);
            var window = new ModificationSettingWin
            {
                DataContext = vm,
            };
            window.ShowDialog();
        }
    }
}

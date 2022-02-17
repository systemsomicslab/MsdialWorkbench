using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Dims;
using CompMs.App.Msdial.ViewModel.Imms;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class DataCollectionSettingViewModel : ViewModelBase
    {
        public DataCollectionSettingViewModel(DataCollectionSettingModel model) {
            Model = model ?? throw new ArgumentNullException(nameof(model));

            Ms1Tolerance = Model.ToReactivePropertyAsSynchronized(
                m => m.Ms1Tolerance,
                m => m.ToString(),
                vm => float.Parse(vm)
            ).SetValidateAttribute(() => Ms1Tolerance).AddTo(Disposables);

            Ms2Tolerance = Model.ToReactivePropertyAsSynchronized(
                m => m.Ms2Tolerance,
                m => m.ToString(),
                vm => float.Parse(vm)
            ).SetValidateAttribute(() => Ms2Tolerance).AddTo(Disposables);

            MaxChargeNumber = Model.ToReactivePropertyAsSynchronized(
                m => m.MaxChargeNumber,
                m => m.ToString(),
                vm => int.Parse(vm)
            ).SetValidateAttribute(() => MaxChargeNumber).AddTo(Disposables);

            IsBrClConsideredForIsotopes = Model.ToReactivePropertySlimAsSynchronized(m => m.IsBrClConsideredForIsotopes).AddTo(Disposables);

            NumberOfThreads = Model.ToReactivePropertyAsSynchronized(
                m => m.NumberOfThreads,
                m => m.ToString(),
                vm => Math.Max(1, Math.Min(Environment.ProcessorCount, int.Parse(vm)))
            ).SetValidateAttribute(() => NumberOfThreads).AddTo(Disposables);

            DataCollectionRangeSettings = Model.DataCollectionRangeSettings.Select(DataCollectionRangeSettingViewModelFactory.Create).ToList().AsReadOnly();

            DimsDataCollectionSettingViewModel = new DimsDataCollectionSettingViewModel(Model.DimsProviderFactoryParameter).AddTo(Disposables);
            CanSetDimsDataCollectionSettingViewModel = DimsDataCollectionSettingViewModel != null;

            ImmsDataCollectionSettingViewModel = new ImmsDataCollectionSettingViewModel(Model.ImmsProviderFactoryParameter).AddTo(Disposables);
            CanSetImmsDataCollectionSettingViewModel = ImmsDataCollectionSettingViewModel != null;
        }

        public DataCollectionSettingModel Model { get; }

        [Required(ErrorMessage = "Ms1 tolerance is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Tolerance should be positive value.")]
        public ReactiveProperty<string> Ms1Tolerance { get; }

        [Required(ErrorMessage = "Ms2 tolerance is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Tolerance should be positive value.")]
        public ReactiveProperty<string> Ms2Tolerance { get; }

        [Required(ErrorMessage = "Maximum charge number is required.")]
        [RegularExpression(@"\d+", ErrorMessage = "Invalid character entered.")]
        [Range(1, int.MaxValue, ErrorMessage = "Charge should be positive value.")]
        public ReactiveProperty<string> MaxChargeNumber { get; }

        public ReactivePropertySlim<bool> IsBrClConsideredForIsotopes { get; }

        [Required(ErrorMessage = "Number of threads is required.")]
        [RegularExpression(@"\d+", ErrorMessage = "Invalid character entered.")]
        public ReactiveProperty<string> NumberOfThreads { get; }

        public ReadOnlyCollection<DataCollectionRangeSettingViewModel> DataCollectionRangeSettings { get; }

        public DimsDataCollectionSettingViewModel DimsDataCollectionSettingViewModel { get; }
        public bool CanSetDimsDataCollectionSettingViewModel { get; }

        public ImmsDataCollectionSettingViewModel ImmsDataCollectionSettingViewModel { get; }
        public bool CanSetImmsDataCollectionSettingViewModel { get; }
    }
}

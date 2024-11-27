using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.Dims;
using CompMs.App.Msdial.ViewModel.Imms;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public sealed class DataCollectionSettingViewModel : ViewModelBase, ISettingViewModel
    {
        public DataCollectionSettingViewModel(DataCollectionSettingModel model, IObservable<bool> isEnabled) {
            Model = model ?? throw new ArgumentNullException(nameof(model));

            IsReadOnly = model.IsReadOnly;

            Ms1Tolerance = model.PeakPickBaseParameterModel.ToReactivePropertyAsSynchronized(
                m => m.CentroidMs1Tolerance,
                m => m.ToString(),
                vm => float.Parse(vm),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => Ms1Tolerance).AddTo(Disposables);

            Ms2Tolerance = model.PeakPickBaseParameterModel.ToReactivePropertyAsSynchronized(
                m => m.CentroidMs2Tolerance,
                m => m.ToString(),
                vm => float.Parse(vm),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => Ms2Tolerance).AddTo(Disposables);

            MaxChargeNumber = model.PeakPickBaseParameterModel.ToReactivePropertyAsSynchronized(
                m => m.MaxChargeNumber,
                m => m.ToString(),
                vm => int.Parse(vm),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => MaxChargeNumber).AddTo(Disposables);

            IsBrClConsideredForIsotopes = model.PeakPickBaseParameterModel.ToReactivePropertySlimAsSynchronized(m => m.IsBrClConsideredForIsotopes).AddTo(Disposables);

            MaxIsotopesDetectedInMs1Spectrum = model.PeakPickBaseParameterModel.ToReactivePropertyAsSynchronized(
                m => m.MaxIsotopesDetectedInMs1Spectrum,
                m => m.ToString(),
                vm => int.Parse(vm),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => MaxIsotopesDetectedInMs1Spectrum).AddTo(Disposables);

            NumberOfThreads = model.ToReactivePropertyAsSynchronized(
                m => m.NumberOfThreads,
                m => m.ToString(),
                vm => Math.Max(1, Math.Min(Environment.ProcessorCount, int.Parse(vm))),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => NumberOfThreads).AddTo(Disposables);

            ExcuteRtCorrection = model.ToReactivePropertySlimAsSynchronized(m => m.ExcuteRtCorrection).AddTo(Disposables);

            DataCollectionRangeSettings = model.DataCollectionRangeSettings.ToReadOnlyReactiveCollection(DataCollectionRangeSettingViewModelFactory.Create).AddTo(Disposables);

            DimsDataCollectionSettingViewModel = model.DimsProviderFactoryParameter is null
                ? null
                : new DimsDataCollectionSettingViewModel(model.DimsProviderFactoryParameter).AddTo(Disposables);
            CanSetDimsDataCollectionSettingViewModel = DimsDataCollectionSettingViewModel != null;

            ImmsDataCollectionSettingViewModel = model.ImmsProviderFactoryParameter is null
                ? null
                : new ImmsDataCollectionSettingViewModel(model.ImmsProviderFactoryParameter).AddTo(Disposables);
            CanSetImmsDataCollectionSettingViewModel = ImmsDataCollectionSettingViewModel != null;

            IsEnabled = isEnabled.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                Ms1Tolerance.ObserveHasErrors,
                Ms2Tolerance.ObserveHasErrors,
                MaxChargeNumber.ObserveHasErrors,
                NumberOfThreads.ObserveHasErrors,
                DataCollectionRangeSettings.Select(vm => vm?.ObserveHasErrors ?? Observable.Return(false)).CombineLatestValuesAreAnyTrue(),
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChanges = new[]
            {
                Ms1Tolerance.ToUnit(),
                Ms2Tolerance.ToUnit(),
                MaxChargeNumber.ToUnit(),
                IsBrClConsideredForIsotopes.ToUnit(),
                NumberOfThreads.ToUnit(),
                DataCollectionRangeSettings.Select(vm => vm?.PropertyChangedAsObservable().ToUnit() ?? Observable.Never<Unit>()).Merge(),
                DimsDataCollectionSettingViewModel?.PropertyChangedAsObservable().ToUnit() ?? Observable.Never<Unit>(),
                ImmsDataCollectionSettingViewModel?.PropertyChangedAsObservable().ToUnit() ?? Observable.Never<Unit>(),
            }.Merge();

            decide = new Subject<Unit>().AddTo(Disposables);
            var change = ObserveChanges.TakeFirstAfterEach(decide);
            ObserveChangeAfterDecision = new[]
            {
                change.ToConstant(true),
                decide.ToConstant(false),
            }.Merge()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public DataCollectionSettingModel Model { get; }

        public bool IsReadOnly { get; }

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

        [Required(ErrorMessage = "Max isotopes number is required.")]
        [RegularExpression(@"\d+", ErrorMessage = "Invalid character entered.")]
        [Range(1, int.MaxValue, ErrorMessage = "Number of Isotopes should be positive value.")]
        public ReactiveProperty<string> MaxIsotopesDetectedInMs1Spectrum { get; }

        [Required(ErrorMessage = "Number of threads is required.")]
        [RegularExpression(@"\d+", ErrorMessage = "Invalid character entered.")]
        public ReactiveProperty<string> NumberOfThreads { get; }

        public ReactivePropertySlim<bool> ExcuteRtCorrection { get; }

        public ReadOnlyReactiveCollection<DataCollectionRangeSettingViewModel?> DataCollectionRangeSettings { get; }

        public DimsDataCollectionSettingViewModel? DimsDataCollectionSettingViewModel { get; }
        public bool CanSetDimsDataCollectionSettingViewModel { get; }

        public ImmsDataCollectionSettingViewModel? ImmsDataCollectionSettingViewModel { get; }
        public bool CanSetImmsDataCollectionSettingViewModel { get; }

        public ReadOnlyReactivePropertySlim<bool> IsEnabled { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
        IObservable<bool> ISettingViewModel.ObserveHasErrors => ObserveHasErrors;

        public IObservable<Unit> ObserveChanges { get; }

        private readonly Subject<Unit> decide;
        public ReadOnlyReactivePropertySlim<bool> ObserveChangeAfterDecision { get; }
        IObservable<bool> ISettingViewModel.ObserveChangeAfterDecision => ObserveChangeAfterDecision;

        public ISettingViewModel? Next(ISettingViewModel selected) {
            decide.OnNext(Unit.Default);
            return null;
        }
    }
}

using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class PeakDetectionSettingViewModel : ViewModelBase, ISettingViewModel
    {
        public PeakDetectionSettingViewModel(PeakDetectionSettingModel model, IObservable<bool> isEnabled) {
            Model = model;
            IsReadOnly = model.IsReadOnly;

            MinimumAmplitude = Model.PeakPickSettingModel.ToReactivePropertyAsSynchronized(
                m => m.MinimumAmplitude,
                m => m.ToString(CultureInfo.InvariantCulture),
                vm => float.Parse(vm, CultureInfo.InvariantCulture),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => MinimumAmplitude).AddTo(Disposables);

            MassSliceWidth = Model.PeakPickSettingModel.ToReactivePropertyAsSynchronized(
                m => m.MassSliceWidth,
                m => m.ToString(CultureInfo.InvariantCulture),
                vm => float.Parse(vm, CultureInfo.InvariantCulture),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => MassSliceWidth).AddTo(Disposables);

            SmoothingMethod = Model.PeakPickSettingModel
                .ToReactivePropertySlimAsSynchronized(m => m.SmoothingMethod).AddTo(Disposables);

            SmoothingLevel = Model.PeakPickSettingModel.ToReactivePropertyAsSynchronized(
                m => m.SmoothingLevel,
                m => m.ToString(CultureInfo.InvariantCulture),
                vm => int.Parse(vm, CultureInfo.InvariantCulture),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => SmoothingLevel).AddTo(Disposables);

            MinimumDatapoints = Model.PeakPickSettingModel.ToReactivePropertyAsSynchronized(
                m => m.MinimumDatapoints,
                m => m.ToString(CultureInfo.InvariantCulture),
                vm => double.Parse(vm, CultureInfo.InvariantCulture),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => MinimumDatapoints).AddTo(Disposables);

            ExcludedMassList = Model.PeakPickSettingModel.ExcludedMzQueries
                .ToReadOnlyReactiveCollection(m => new MzSearchQueryViewModel(m)).AddTo(Disposables);

            AddCommand = new[]
            {
                this.ObserveProperty(vm => vm.NewMass).ToUnit(),
                this.ObserveProperty(vm => vm.NewTolerance).ToUnit(),
            }.Merge()
            .Select(_ => CanAddQuery())
            .ToReactiveCommand()
            .WithSubscribe(() => Model.PeakPickSettingModel.AddQuery(NewMass, NewTolerance))
            .AddTo(Disposables);

            RemoveCommand = this.ObserveProperty(vm => vm.SelectedQuery)
                .Select(q => q is not null)
                .ToReactiveCommand()
                .WithSubscribe(() => Model.PeakPickSettingModel.RemoveQuery(SelectedQuery!.Model))
                .AddTo(Disposables);

            IsEnabled = isEnabled.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                MinimumAmplitude.ObserveHasErrors,
                MassSliceWidth.ObserveHasErrors,
                SmoothingLevel.ObserveHasErrors,
                MinimumDatapoints.ObserveHasErrors,
                new[]
                {
                    ExcludedMassList.ObserveElementPropertyChanged().ToUnit(),
                    ExcludedMassList.CollectionChangedAsObservable().ToUnit(),
                }.Merge()
                .Select(_ => ExcludedMassList.Any(vm => vm.HasValidationErrors)),
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChanges = new[]
            {
                MinimumAmplitude.ToUnit(),
                MassSliceWidth.ToUnit(),
                SmoothingMethod.ToUnit(),
                SmoothingLevel.ToUnit(),
                MinimumDatapoints.ToUnit(),
                ExcludedMassList.ObserveElementPropertyChanged().ToUnit(),
            }.Merge();

            decide = new Subject<Unit>().AddTo(Disposables);
            ObserveChangeAfterDecision = Observable.Merge(new[]
            {
                ObserveChanges.TakeFirstAfterEach(decide).ToConstant(true),
                decide.ToConstant(false)
            })
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public PeakDetectionSettingModel Model { get; }

        public bool IsReadOnly { get; }

        [Required(ErrorMessage = "Minimum amplitude is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0, int.MaxValue, ErrorMessage = "Amplitude should be positive value.")]
        public ReactiveProperty<string> MinimumAmplitude { get; }

        [Required(ErrorMessage = "Mass slice width is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0.0001, int.MaxValue, ErrorMessage = "Width should be positive value.")]
        public ReactiveProperty<string> MassSliceWidth { get; }

        public ReactivePropertySlim<SmoothingMethod> SmoothingMethod { get; }

        [Required(ErrorMessage = "Smoothing level is required.")]
        [RegularExpression(@"\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0, int.MaxValue, ErrorMessage = "Amplitude should be positive value.")]
        public ReactiveProperty<string> SmoothingLevel { get; }

        [Required(ErrorMessage = "Minimum datapoint is required.")]
        [RegularExpression(@"\d\.?\d*", ErrorMessage = "Invalid character entered.")]
        [Range(1, int.MaxValue, ErrorMessage = "Data points should be positive value.")]
        public ReactiveProperty<string> MinimumDatapoints { get; }

        public ReadOnlyReactiveCollection<MzSearchQueryViewModel> ExcludedMassList { get; }

        public MzSearchQueryViewModel? SelectedQuery {
            get => selectedQuery;
            set => SetProperty(ref selectedQuery, value);
        }
        private MzSearchQueryViewModel? selectedQuery;

        [Range(0d, double.MaxValue, ErrorMessage = "Mass should be zero or positive value.")]
        public double NewMass {
            get => newMass;
            set => SetProperty(ref newMass, value);
        }
        private double newMass;

        [Range(0.0001d, double.MaxValue, ErrorMessage = "Tolerance should be positive value.")]
        public double NewTolerance {
            get => newTolerance;
            set => SetProperty(ref newTolerance, value);
        }
        private double newTolerance;

        // The values are checked rather than their recorded errors: a value nobody has typed has none,
        // and the tolerance starts at 0, which would add an entry that removes nothing.
        private bool CanAddQuery() {
            return IsValidValue(nameof(NewMass), NewMass) && IsValidValue(nameof(NewTolerance), NewTolerance);
        }

        private bool IsValidValue(string propertyName, double value) {
            return Validator.TryValidateProperty(value, new ValidationContext(this) { MemberName = propertyName }, null);
        }

        public ReactiveCommand AddCommand { get; }

        public ReactiveCommand RemoveCommand { get; }

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

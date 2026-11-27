using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public sealed class MassRecalibrationSettingViewModel : ViewModelBase, ISettingViewModel
    {
        public MassRecalibrationSettingViewModel(MassRecalibrationSettingModel model, IObservable<bool> isEnabled) {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            IsReadOnly = model.IsReadOnly;

            Recalibrate = model.ToReactivePropertySlimAsSynchronized(m => m.Recalibrate).AddTo(Disposables);

            // The decimal point is always '.', as the method file and the parameter text write it.
            SearchPpm = model.ToReactivePropertyAsSynchronized(
                m => m.SearchPpm,
                m => m.ToString(CultureInfo.InvariantCulture),
                vm => double.Parse(vm, CultureInfo.InvariantCulture),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => SearchPpm).AddTo(Disposables);

            MinimumCorrectionPpm = model.ToReactivePropertyAsSynchronized(
                m => m.MinimumCorrectionPpm,
                m => m.ToString(CultureInfo.InvariantCulture),
                vm => double.Parse(vm, CultureInfo.InvariantCulture),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => MinimumCorrectionPpm).AddTo(Disposables);

            IsEnabled = isEnabled.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                SearchPpm.ObserveHasErrors,
                MinimumCorrectionPpm.ObserveHasErrors,
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChanges = new[]
            {
                Recalibrate.ToUnit(),
                SearchPpm.ToUnit(),
                MinimumCorrectionPpm.ToUnit(),
            }.Merge();

            decide = new Subject<Unit>().AddTo(Disposables);
            ObserveChangeAfterDecision = new[]
            {
                ObserveChanges.TakeFirstAfterEach(decide).ToConstant(true),
                decide.ToConstant(false),
            }.Merge()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public MassRecalibrationSettingModel Model { get; }

        public bool IsReadOnly { get; }

        public ReactivePropertySlim<bool> Recalibrate { get; }

        [Required(ErrorMessage = "The search window is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "The search window should be a positive number of ppm.")]
        public ReactiveProperty<string> SearchPpm { get; }

        [Required(ErrorMessage = "The minimum correction is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0d, double.MaxValue, ErrorMessage = "The minimum correction should be a number of ppm, zero or more.")]
        public ReactiveProperty<string> MinimumCorrectionPpm { get; }

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

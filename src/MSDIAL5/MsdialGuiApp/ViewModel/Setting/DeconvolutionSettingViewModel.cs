using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class DeconvolutionSettingViewModel : ViewModelBase, ISettingViewModel
    {
        public DeconvolutionSettingViewModel(DeconvolutionSettingModel model, IObservable<bool> isEnabled) {
            this.model = model;

            IsReadOnly = model.IsReadOnly;

            SigmaWindowValue = model.ToReactivePropertyAsSynchronized(
                m => m.SigmaWindowValue,
                m => m.ToString(),
                vm => float.Parse(vm),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => SigmaWindowValue).AddTo(Disposables);

            AmplitudeCutoff = model.ToReactivePropertyAsSynchronized(
                m => m.AmplitudeCutoff,
                m => m.ToString(),
                vm => float.Parse(vm),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => AmplitudeCutoff).AddTo(Disposables);

            RemoveAfterPrecursor = model.ToReactivePropertySlimAsSynchronized(m => m.RemoveAfterPrecursor).AddTo(Disposables);

            KeptIsotopeRange = model.ToReactivePropertyAsSynchronized(
                m => m.KeptIsotopeRange,
                m => m.ToString(),
                vm => float.Parse(vm),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => KeptIsotopeRange).AddTo(Disposables);

            KeepOriginalPrecurosrIsotopes = model.ToReactivePropertySlimAsSynchronized(m => m.KeepOriginalPrecurosrIsotopes).AddTo(Disposables);

            IsEnabled = isEnabled.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                SigmaWindowValue.ObserveHasErrors,
                AmplitudeCutoff.ObserveHasErrors,
                KeptIsotopeRange.ObserveHasErrors,
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChanges = new[]
            {
                SigmaWindowValue.ToUnit(),
                AmplitudeCutoff.ToUnit(),
                RemoveAfterPrecursor.ToUnit(),
                KeptIsotopeRange.ToUnit(),
                KeepOriginalPrecurosrIsotopes.ToUnit(),
            }.Merge();

            decide = new Subject<Unit>().AddTo(Disposables);
            var changes = ObserveChanges.TakeFirstAfterEach(decide);
            ObserveChangeAfterDecision = new[]
            {
                changes.ToConstant(true),
                decide.ToConstant(false)
            }.Merge()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        private readonly DeconvolutionSettingModel model;

        public bool IsReadOnly { get; }

        [Required(ErrorMessage = "Sigma window value is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0, float.MaxValue, ErrorMessage = "Window size should be positive value.")]
        public ReactiveProperty<string> SigmaWindowValue { get; }

        [Required(ErrorMessage = "Amplitude cut off value is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        public ReactiveProperty<string> AmplitudeCutoff { get; }

        public ReactivePropertySlim<bool> RemoveAfterPrecursor { get; }

        [Required(ErrorMessage = "Kept isotope range is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0, float.MaxValue, ErrorMessage = "Kept isotope range is required.")]
        public ReactiveProperty<string> KeptIsotopeRange { get; }

        public ReactivePropertySlim<bool> KeepOriginalPrecurosrIsotopes { get; }

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

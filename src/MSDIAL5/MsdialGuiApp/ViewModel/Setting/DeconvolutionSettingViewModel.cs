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

            RelativeAmplitudeCutoff = model.ToReactivePropertyAsSynchronized(
                m => m.RelativeAmplitudeCutoff,
                m => Math.Min(m * 100f, 100f).ToString(),
                vm => float.Parse(vm) / 100f,
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => RelativeAmplitudeCutoff).AddTo(Disposables);

            RemoveAfterPrecursor = model.ToReactivePropertySlimAsSynchronized(m => m.RemoveAfterPrecursor).AddTo(Disposables);

            KeptIsotopeRange = model.ToReactivePropertyAsSynchronized(
                m => m.KeptIsotopeRange,
                m => m.ToString(),
                vm => float.Parse(vm),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => KeptIsotopeRange).AddTo(Disposables);

            KeepOriginalPrecurosrIsotopes = model.ToReactivePropertySlimAsSynchronized(m => m.KeepOriginalPrecurosrIsotopes).AddTo(Disposables);

            ExecuteChromDeconvolution = model.ToReactivePropertySlimAsSynchronized(m => m.ExecuteChromDeconvolution).AddTo(Disposables);

            IsEnabled = isEnabled.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                SigmaWindowValue.ObserveHasErrors,
                AmplitudeCutoff.ObserveHasErrors,
                RelativeAmplitudeCutoff.ObserveHasErrors,
                KeptIsotopeRange.ObserveHasErrors,
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChanges = new[]
            {
                SigmaWindowValue.ToUnit(),
                AmplitudeCutoff.ToUnit(),
                RelativeAmplitudeCutoff.ToUnit(),
                RemoveAfterPrecursor.ToUnit(),
                KeptIsotopeRange.ToUnit(),
                KeepOriginalPrecurosrIsotopes.ToUnit(),
                ExecuteChromDeconvolution.ToUnit(),
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

        [Required(ErrorMessage = "Relative amplitude cut off value is required.")]
        [RegularExpression(@"100(\.0+)?|([1-9]?\d)?(\.\d+)?", ErrorMessage = "Invalid value entered.")]
        public ReactiveProperty<string> RelativeAmplitudeCutoff { get; }

        public ReactivePropertySlim<bool> RemoveAfterPrecursor { get; }

        [Required(ErrorMessage = "Kept isotope range is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0, float.MaxValue, ErrorMessage = "Kept isotope range is required.")]
        public ReactiveProperty<string> KeptIsotopeRange { get; }

        public ReactivePropertySlim<bool> KeepOriginalPrecurosrIsotopes { get; }

        public ReactivePropertySlim<bool> ExecuteChromDeconvolution { get; }

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

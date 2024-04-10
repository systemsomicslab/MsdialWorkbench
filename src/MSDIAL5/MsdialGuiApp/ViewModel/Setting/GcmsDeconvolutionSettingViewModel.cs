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
    public sealed class GcmsDeconvolutionSettingViewModel : ViewModelBase, ISettingViewModel
    {
        private readonly DeconvolutionSettingModel _model;
        private readonly Subject<Unit> _decide;

        public GcmsDeconvolutionSettingViewModel(DeconvolutionSettingModel model, IObservable<bool> isEnabled)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));

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

            IsEnabled = isEnabled.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                SigmaWindowValue.ObserveHasErrors,
                AmplitudeCutoff.ObserveHasErrors,
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChanges = new[]
            {
                SigmaWindowValue.ToUnit(),
                AmplitudeCutoff.ToUnit(),
            }.Merge();

            _decide = new Subject<Unit>().AddTo(Disposables);
            var changes = ObserveChanges.TakeFirstAfterEach(_decide);
            ObserveChangeAfterDecision = new[]
            {
                changes.ToConstant(true),
                _decide.ToConstant(false)
            }.Merge()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public bool IsReadOnly => _model.IsReadOnly;

        [Required(ErrorMessage = "Sigma window value is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0, float.MaxValue, ErrorMessage = "Window size should be positive value.")]
        public ReactiveProperty<string> SigmaWindowValue { get; }

        [Required(ErrorMessage = "Amplitude cut off value is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        public ReactiveProperty<string> AmplitudeCutoff { get; }

        public ReadOnlyReactivePropertySlim<bool> IsEnabled { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
        IObservable<bool> ISettingViewModel.ObserveHasErrors => ObserveHasErrors;

        public IObservable<Unit> ObserveChanges { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveChangeAfterDecision { get; }
        IObservable<bool> ISettingViewModel.ObserveChangeAfterDecision => ObserveChangeAfterDecision;

        public ISettingViewModel? Next(ISettingViewModel selected)
        {
            _decide.OnNext(Unit.Default);
            return null;
        }
    }
}

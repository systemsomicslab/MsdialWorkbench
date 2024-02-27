using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public abstract class PeakEqualityParameterSettingViewModel : ViewModelBase
    {
        public PeakEqualityParameterSettingViewModel(IPeakEqualityParameterSetting model) {
            Model = model;

            Tolerance = Model.ToReactivePropertyAsSynchronized(
                m => m.Tolerance,
                m => m.ToString(),
                vm => float.Parse(vm),
                ignoreValidationErrorValue: true)
            .SetValidateAttribute(() => Tolerance)
            .AddTo(Disposables);

            Factor = Model.ToReactivePropertyAsSynchronized(
                m => m.Factor,
                m => m.ToString(),
                vm => float.Parse(vm),
                ignoreValidationErrorValue: true)
            .SetValidateAttribute(() => Factor)
            .AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                Tolerance.ObserveHasErrors,
                Factor.ObserveHasErrors,
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChanges = new[]
            {
                Tolerance.ToUnit(),
                Factor.ToUnit(),
            }.Merge()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public IPeakEqualityParameterSetting Model { get; }

        [Required(ErrorMessage = "Tolerance value is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid format.")]
        [Range(0.0001, float.MaxValue, ErrorMessage = "Tolerance should be positive value.")]
        public ReactiveProperty<string> Tolerance { get; }

        [Required(ErrorMessage = "Factor value is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid format.")]
        [Range(0d, 1d, ErrorMessage = "Factor should be between 0 and 1.")]
        public ReactiveProperty<string> Factor { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
        public ReadOnlyReactivePropertySlim<Unit> ObserveChanges { get; }
    }

    public class RetentionTimeEqualityParameterSettingViewModel : PeakEqualityParameterSettingViewModel
    {
        public RetentionTimeEqualityParameterSettingViewModel(RetentionTimeEqualityParameterSetting model) : base(model) {

        }
    }

    public class RetentionIndexEqualityParameterSettingViewModel : PeakEqualityParameterSettingViewModel
    {
        public RetentionIndexEqualityParameterSettingViewModel(RetentionIndexEqualityParameterSetting model) : base(model) {

        }
    }

    public class DriftTimeEqualityParameterSettingViewModel : PeakEqualityParameterSettingViewModel
    {
        public DriftTimeEqualityParameterSettingViewModel(DriftTimeEqualityParameterSetting model) : base(model) {

        }
    }

    public class Ms1EqualityParameterSettingViewModel : PeakEqualityParameterSettingViewModel
    {
        public Ms1EqualityParameterSettingViewModel(Ms1EqualityParameterSetting model) : base(model) {

        }
    }

    public static class PeakEqualityParameterSettingViewModelFactory
    {
        public static PeakEqualityParameterSettingViewModel? Create(IPeakEqualityParameterSetting model) {
            switch (model) {
                case RetentionTimeEqualityParameterSetting rt:
                    return new RetentionTimeEqualityParameterSettingViewModel(rt);
                case RetentionIndexEqualityParameterSetting ri:
                    return new RetentionIndexEqualityParameterSettingViewModel(ri);
                case DriftTimeEqualityParameterSetting dt:
                    return new DriftTimeEqualityParameterSettingViewModel(dt);
                case Ms1EqualityParameterSetting ms1:
                    return new Ms1EqualityParameterSettingViewModel(ms1);
                default:
                    return null;
            }
        }
    }
}

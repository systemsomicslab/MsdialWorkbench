using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.ComponentModel.DataAnnotations;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class DeconvolutionSettingViewModel : ViewModelBase
    {
        public DeconvolutionSettingViewModel(DeconvolutionSettingModel model) {
            this.model = model;

            SigmaWindowValue = model.ToReactivePropertyAsSynchronized(
                m => m.SigmaWindowValue,
                m => m.ToString(),
                vm => float.Parse(vm)
            ).SetValidateAttribute(() => SigmaWindowValue).AddTo(Disposables);

            AmplitudeCutoff = model.ToReactivePropertyAsSynchronized(
                m => m.AmplitudeCutoff,
                m => m.ToString(),
                vm => float.Parse(vm)
            ).SetValidateAttribute(() => AmplitudeCutoff).AddTo(Disposables);

            RemoveAfterPrecursor = model.ToReactivePropertySlimAsSynchronized(m => m.RemoveAfterPrecursor).AddTo(Disposables);

            KeptIsotopeRange = model.ToReactivePropertyAsSynchronized(
                m => m.KeptIsotopeRange,
                m => m.ToString(),
                vm => float.Parse(vm)
            ).SetValidateAttribute(() => KeptIsotopeRange).AddTo(Disposables);

            KeepOriginalPrecurosrIsotopes = model.ToReactivePropertySlimAsSynchronized(m => m.KeepOriginalPrecurosrIsotopes).AddTo(Disposables);
        }

        private readonly DeconvolutionSettingModel model;

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
    }
}

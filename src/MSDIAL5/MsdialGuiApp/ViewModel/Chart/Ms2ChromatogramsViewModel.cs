using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.ViewModel.Loader;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class Ms2ChromatogramsViewModel : ViewModelBase {
        public Ms2ChromatogramsViewModel(Ms2ChromatogramsModel model, Action focusAction, IObservable<bool> isFocused) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            ChromatogramsViewModel = model.ChromatogramsModel
                .Where(chromatograms => chromatograms is not null)
                .Select(chromatograms => new ChromatogramsViewModel(chromatograms!))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            IsRawSelected = model.IsRawSelected;
            IsDeconvolutedSelected = model.IsDeconvolutedSelected;
            IsBothSelected = model.IsBothSelected;
            IsRawEnabled = model.IsRawEnabled;
            IsDeconvolutedEnabled = model.IsDeconvolutedEnabled;
            IsBothEnabled = model.IsBothEnabled;
            FocusAction = focusAction;
            IsFocused = isFocused.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            MultiMsRawSpectrumLoaderViewModel = new MultiMsmsRawSpectrumLoaderViewModel(model.Loader);
            NumberOfChromatograms = model.NumberOfChromatograms.SetValidateAttribute(() => NumberOfChromatograms);
            CopyAsTableCommand = new ReactiveCommand().WithSubscribe(model.CopyAsTable).AddTo(Disposables);
            SaveAsTableCommand = new AsyncReactiveCommand().WithSubscribe(model.SaveAsTableAsync).AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<ChromatogramsViewModel?> ChromatogramsViewModel { get; }

        public ReactivePropertySlim<bool> IsRawSelected { get; }
        public ReactivePropertySlim<bool> IsDeconvolutedSelected { get; }
        public ReactivePropertySlim<bool> IsBothSelected { get; }

        public ReadOnlyReactivePropertySlim<bool> IsRawEnabled { get; }
        public ReadOnlyReactivePropertySlim<bool> IsDeconvolutedEnabled { get; }
        public ReadOnlyReactivePropertySlim<bool> IsBothEnabled { get; }
        public Action FocusAction { get; }
        public ReadOnlyReactivePropertySlim<bool> IsFocused { get; }
        public MultiMsmsRawSpectrumLoaderViewModel MultiMsRawSpectrumLoaderViewModel { get; }

        [RegularExpression(@"\d+", ErrorMessage = "Invalid character is entered.")]
        [Range(0, int.MaxValue, ErrorMessage = "Invalid value is requested.")]
        public ReactiveProperty<int> NumberOfChromatograms { get; }

        public ReactiveCommand CopyAsTableCommand { get; }
        public AsyncReactiveCommand SaveAsTableCommand { get; }
    }
}

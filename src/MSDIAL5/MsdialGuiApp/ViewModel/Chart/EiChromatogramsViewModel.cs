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
    internal sealed class EiChromatogramsViewModel : ViewModelBase
    {
        public EiChromatogramsViewModel(EiChromatogramsModel model, ReactivePropertySlim<int> numberOfEIChromatograms, MultiMsmsRawSpectrumLoaderViewModel? loader, Action focusAction, IObservable<bool> isFocused) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            NumberOfEIChromatograms = numberOfEIChromatograms;
            MultiMsRawSpectrumLoaderViewModel = loader;

            RawChromatogramsViewModel = model.RawChromatogramsModel
                .Select(chromatograms => chromatograms is null ? null : new ChromatogramsViewModel(chromatograms))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            DeconvolutedChromatogramsViewModel = model.DeconvolutedChromatogramsModel
                .Select(chromatograms => chromatograms is null ? null : new ChromatogramsViewModel(chromatograms))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            HorizontalAxisItemModel = model.HorizontalAxisItemModel;
            VerticalAxisItemModel = model.VerticalAxisItemModel;
            IsRawSelected = model.IsRawSelected;
            IsDeconvolutedSelected = model.IsDeconvolutedSelected;
            IsBothSelected = model.IsBothSelected;
            IsRawEnabled = model.IsRawEnabled;
            IsDeconvolutedEnabled = model.IsDeconvolutedEnabled;
            IsBothEnabled = model.IsBothEnabled;
            FocusAction = focusAction;
            IsFocused = isFocused.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            CopyAsTableCommand = new ReactiveCommand().WithSubscribe(model.CopyAsTable).AddTo(Disposables);
            SaveAsTableCommand = new AsyncReactiveCommand().WithSubscribe(model.SaveAsTableAsync).AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<ChromatogramsViewModel?> RawChromatogramsViewModel { get; }
        public ReadOnlyReactivePropertySlim<ChromatogramsViewModel?> DeconvolutedChromatogramsViewModel { get; }
        public ReadOnlyReactivePropertySlim<AxisItemModel<double>?> HorizontalAxisItemModel { get; }
        public ReadOnlyReactivePropertySlim<AxisItemModel<double>?> VerticalAxisItemModel { get; }

        public ReactivePropertySlim<bool> IsRawSelected { get; }
        public ReactivePropertySlim<bool> IsDeconvolutedSelected { get; }
        public ReactivePropertySlim<bool> IsBothSelected { get; }

        public ReadOnlyReactivePropertySlim<bool> IsRawEnabled { get; }
        public ReadOnlyReactivePropertySlim<bool> IsDeconvolutedEnabled { get; }
        public ReadOnlyReactivePropertySlim<bool> IsBothEnabled { get; }
        public Action FocusAction { get; }
        public ReadOnlyReactivePropertySlim<bool> IsFocused { get; }

        public ReactivePropertySlim<int> NumberOfEIChromatograms { get; }
        public MultiMsmsRawSpectrumLoaderViewModel? MultiMsRawSpectrumLoaderViewModel { get; }

        [RegularExpression(@"\d+", ErrorMessage = "Invalid character is entered.")]
        [Range(0, int.MaxValue, ErrorMessage = "Invalid value is requested.")]

        public ReactiveCommand CopyAsTableCommand { get; }
        public AsyncReactiveCommand SaveAsTableCommand { get; }
    }
}

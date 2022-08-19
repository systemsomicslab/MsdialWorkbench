using CompMs.App.Msdial.Model.Loader;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class Ms2ChromatogramsModel : DisposableModelBase
    {
        public Ms2ChromatogramsModel(IObservable<ChromatogramsModel> rawChromatograms, IObservable<ChromatogramsModel> deconvolutedChromatograms, MultiMsRawSpectrumLoader loader, bool isSwath) {
            var bothChromatograms = deconvolutedChromatograms.CombineLatest(rawChromatograms, (dec, raw) => dec.Merge(raw));

            IsRawSelected = new ReactivePropertySlim<bool>(!isSwath).AddTo(Disposables);
            IsDeconvolutedSelected = new ReactivePropertySlim<bool>(isSwath).AddTo(Disposables);
            IsBothSelected = new ReactivePropertySlim<bool>(false).AddTo(Disposables);

            IsRawEnabled = Observable.Return(true).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsDeconvolutedEnabled = Observable.Return(isSwath).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsBothEnabled = Observable.Return(isSwath).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ChromatogramsModel = new[]
            {
                IsRawSelected.Where(x => x).Select(_ => rawChromatograms),
                IsDeconvolutedSelected.Where(x => x).Select(_ => deconvolutedChromatograms),
                IsBothSelected.Where(x => x).Select(_ => bothChromatograms),
            }.Merge()
            .Switch()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
            Loader = loader;
        }

        public ReadOnlyReactivePropertySlim<ChromatogramsModel> ChromatogramsModel { get; }

        public ReactivePropertySlim<bool> IsRawSelected { get; }
        public ReactivePropertySlim<bool> IsDeconvolutedSelected { get; }
        public ReactivePropertySlim<bool> IsBothSelected { get; }

        public ReadOnlyReactivePropertySlim<bool> IsRawEnabled { get; }
        public ReadOnlyReactivePropertySlim<bool> IsDeconvolutedEnabled { get; }
        public ReadOnlyReactivePropertySlim<bool> IsBothEnabled { get; }

        public MultiMsRawSpectrumLoader Loader { get; }
    }
}

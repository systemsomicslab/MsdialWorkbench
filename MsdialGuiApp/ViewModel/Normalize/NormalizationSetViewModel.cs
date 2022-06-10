using CompMs.App.Msdial.Model.Normalize;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.ViewModel.Normalize
{
    internal sealed class NormalizationSetViewModel : ViewModelBase
    {
        private readonly NormalizationSetModel _model;

        public NormalizationSetViewModel(NormalizationSetModel model) {

            _model = model;
            SplashViewModel = new SplashSetViewModel(_model.SplashSetModel).AddTo(Disposables);

            IsNormalizeNone = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeNone).AddTo(Disposables);
            IsNormalizeIS = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeIS).AddTo(Disposables);
            IsNormalizeLowess = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeLowess).AddTo(Disposables);
            IsNormalizeIsLowess = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeIsLowess).AddTo(Disposables);
            IsNormalizeSplash = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeSplash).AddTo(Disposables);
            IsNormalizeTic = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeTic).AddTo(Disposables);
            IsNormalizeMTic = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeMTic).AddTo(Disposables);

            NormalizeCommand = model.CanNormalizeProperty
                .ToReactiveCommand()
                .WithSubscribe(model.Normalize)
                .AddTo(Disposables);
        }

        public ReactivePropertySlim<bool> IsNormalizeNone { get; }
        public ReactivePropertySlim<bool> IsNormalizeIS { get; }
        public ReactivePropertySlim<bool> IsNormalizeLowess { get; }
        public ReactivePropertySlim<bool> IsNormalizeIsLowess { get; }
        public ReactivePropertySlim<bool> IsNormalizeSplash { get; }
        public ReactivePropertySlim<bool> IsNormalizeTic { get; }
        public ReactivePropertySlim<bool> IsNormalizeMTic { get; }

        public SplashSetViewModel SplashViewModel { get; }

        public ReactiveCommand NormalizeCommand { get; }
    }
}

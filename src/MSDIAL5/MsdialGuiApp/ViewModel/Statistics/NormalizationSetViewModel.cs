using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Statistics;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Statistics
{
    internal sealed class NormalizationSetViewModel : ViewModelBase
    {
        private readonly NormalizationSetModel _model;

        public NormalizationSetViewModel(NormalizationSetModel model, InternalStandardSetViewModel isSetViewModel) {
            _model = model;

            IsNormalizeNone = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeNone).AddTo(Disposables);
            IsNormalizeIS = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeIS).AddTo(Disposables);
            IsNormalizeLowess = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeLowess).AddTo(Disposables);
            IsNormalizeIsLowess = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeIsLowess).AddTo(Disposables);
            IsNormalizeSplash = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeSplash).AddTo(Disposables);
            IsNormalizeTic = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeTic).AddTo(Disposables);
            IsNormalizeMTic = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeMTic).AddTo(Disposables);
            ApplyDilutionFactor = model.ToReactivePropertySlimAsSynchronized(m => m.ApplyDilutionFactor).AddTo(Disposables);

            SplashViewModel = new SplashSetViewModel(_model.SplashSetModel).AddTo(Disposables);
            IsSetViewModel = isSetViewModel;
            IsSetViewModelVisible = IsNormalizeIS.CombineLatest(IsNormalizeIsLowess, (a, b) => a || b).ToReadOnlyReactivePropertySlim(false).AddTo(Disposables);

            NormalizeCommand = new[]{
                model.CanNormalizeProperty,
                IsSetViewModelVisible.SelectSwitch(v => v ? isSetViewModel.IsEditting.StartWith(isSetViewModel.IsEditting.Value).Inverse() : Observable.Return(true)),
            }.CombineLatestValuesAreAllTrue()
                .ToReactiveCommand()
                .WithSubscribe(Normalize)
                .AddTo(Disposables);
            CancelCommand = isSetViewModel.CancelCommand;
        }

        public ReactivePropertySlim<bool> IsNormalizeNone { get; }
        public ReactivePropertySlim<bool> IsNormalizeIS { get; }
        public ReactivePropertySlim<bool> IsNormalizeLowess { get; }
        public ReactivePropertySlim<bool> IsNormalizeIsLowess { get; }
        public ReactivePropertySlim<bool> IsNormalizeSplash { get; }
        public ReactivePropertySlim<bool> IsNormalizeTic { get; }
        public ReactivePropertySlim<bool> IsNormalizeMTic { get; }
        public ReactivePropertySlim<bool> ApplyDilutionFactor { get; }

        public SplashSetViewModel SplashViewModel { get; }
        public InternalStandardSetViewModel IsSetViewModel { get; }
        public ReadOnlyReactivePropertySlim<bool> IsSetViewModelVisible { get; }

        public AnalysisFileBeanModelCollection FileCollection => _model.FileCollection;

        public ReactiveCommand NormalizeCommand { get; }
        public ICommand CancelCommand { get; }

        private void Normalize() {
            if (IsSetViewModelVisible.Value) {
                IsSetViewModel.ApplyCommand.Execute(null);
            }
            _model.Normalize();
        }
    }
}

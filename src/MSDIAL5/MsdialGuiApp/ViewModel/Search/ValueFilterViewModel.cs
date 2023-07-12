using CompMs.App.Msdial.Model.Search;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Search
{
    internal sealed class ValueFilterViewModel : ViewModelBase {
        private readonly ValueFilterModel _model;

        public ValueFilterViewModel(ValueFilterModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            Minimum = model.ObserveProperty(m => m.Minimum).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Maximum = model.ObserveProperty(m => m.Maximum).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Lower = model.ToReactivePropertyAsSynchronized(m => m.Lower).AddTo(Disposables);
            Upper = model.ToReactivePropertyAsSynchronized(m => m.Upper).AddTo(Disposables);
            Lower.SetValidateNotifyError(v => Upper.Value >= v ? null : "Too large");
            Upper.SetValidateNotifyError(v => Lower.Value <= v ? null : "Too small");
            Lower.ForceValidate();
            Upper.ForceValidate();

            var observeChanged = new[] { Lower, Upper }.Merge().ToUnit().Publish();
            ObserveChanged = observeChanged;
            var observeHasErrors = observeChanged.Select(_ => HasValidationErrors).Publish();
            ObserveHasErrors = observeHasErrors;

            Disposables.Add(observeChanged.Connect());
            Disposables.Add(observeHasErrors.Connect());
        }

        public string Label => _model.Label;
        public ReadOnlyReactivePropertySlim<double> Minimum { get; }
        public ReadOnlyReactivePropertySlim<double> Maximum { get; }
        public ReactiveProperty<double> Lower { get; }
        public ReactiveProperty<double> Upper { get; }

        public IObservable<Unit> ObserveChanged { get; }
        public IObservable<bool> ObserveHasErrors { get; }
    }
}

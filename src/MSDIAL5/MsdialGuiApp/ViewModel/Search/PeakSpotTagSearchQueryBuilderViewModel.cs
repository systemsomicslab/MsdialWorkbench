using CompMs.App.Msdial.Model.Search;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Search
{
    internal sealed class PeakSpotTagSearchQueryBuilderViewModel : ViewModelBase
    {
        private readonly PeakSpotTagSearchQueryBuilderModel _model;

        public PeakSpotTagSearchQueryBuilderViewModel(PeakSpotTagSearchQueryBuilderModel model) {
            _model = model;
            Confirmed = model.ToReactivePropertySlimAsSynchronized(m => m.Confirmed).AddTo(Disposables);
            LowQualitySpectrum = model.ToReactivePropertySlimAsSynchronized(m => m.LowQualitySpectrum).AddTo(Disposables);
            Misannotation = model.ToReactivePropertySlimAsSynchronized(m => m.Misannotation).AddTo(Disposables);
            Coelution = model.ToReactivePropertySlimAsSynchronized(m => m.Coelution).AddTo(Disposables);
            Overannotation = model.ToReactivePropertySlimAsSynchronized(m => m.Overannotation).AddTo(Disposables);
            Excludes = model.ToReactivePropertySlimAsSynchronized(m => m.Excludes).AddTo(Disposables);
            ObserveChanged = new[]
            {
                Confirmed,
                LowQualitySpectrum,
                Misannotation,
                Coelution,
                Overannotation,
                Excludes,
            }.Merge().ToUnit().Publish().RefCount();
        }

        public ReactivePropertySlim<bool> Confirmed { get; }
        public ReactivePropertySlim<bool> LowQualitySpectrum { get; }
        public ReactivePropertySlim<bool> Misannotation { get; }
        public ReactivePropertySlim<bool> Coelution { get; }
        public ReactivePropertySlim<bool> Overannotation { get; }
        public ReactivePropertySlim<bool> Excludes { get; }

        public IObservable<Unit> ObserveChanged { get; }
    }
}

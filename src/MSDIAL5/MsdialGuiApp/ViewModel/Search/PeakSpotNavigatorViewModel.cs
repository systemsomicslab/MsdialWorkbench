using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Search
{
    internal sealed class PeakSpotNavigatorViewModel : ViewModelBase
    {
        private readonly PeakSpotNavigatorModel _model;

        public PeakSpotNavigatorViewModel(PeakSpotNavigatorModel model) {
            _model = model;
            SelectedAnnotationLabel = model
                .ToReactivePropertySlimAsSynchronized(m => m.SelectedAnnotationLabel)
                .AddTo(Disposables);
            PeakFilterViewModel = new PeakFilterViewModel(model.PeakFilters.ToArray()).AddTo(Disposables);
            TagSearchBuilderViewModel = new PeakSpotTagSearchQueryBuilderViewModel(model.TagSearchQueryBuilder).AddTo(Disposables);

            AmplitudeLowerValue = model.AmplitudeFilterModel
                .ToReactivePropertyAsSynchronized(m => m.Lower)
                .AddTo(Disposables);
            AmplitudeUpperValue = model.AmplitudeFilterModel
                .ToReactivePropertyAsSynchronized(m => m.Upper)
                .AddTo(Disposables);
            AmplitudeLowerValue.SetValidateNotifyError(v => AmplitudeUpperValue.Value >= v ? null : "Too large");
            AmplitudeUpperValue.SetValidateNotifyError(v => AmplitudeLowerValue.Value <= v ? null : "Too small");
            ValueFilterViewModels = model.ValueFilterModels.ToReadOnlyReactiveCollection(m => new ValueFilterViewModel(m)).AddTo(Disposables);
            KeywordFilterViewModels = model.KeywordFilterModels.ToReadOnlyReactiveCollection(m => new KeywordFilterViewModel(m)).AddTo(Disposables);

            IsEditting = new ReactivePropertySlim<bool>().AddTo(Disposables);

            PeakSpotsView = CollectionViewSource.GetDefaultView(model.PeakSpots);

            var needRefresh = new[]
            {
                PeakFilterViewModel.CheckedFilter.ToUnit(),
                TagSearchBuilderViewModel.ObserveChanged,
                AmplitudeLowerValue.ToUnit(),
                AmplitudeUpperValue.ToUnit(),
                ValueFilterViewModels.ObserveElementObservableProperty(vm => vm.ObserveChanged).ToUnit(),
                KeywordFilterViewModels.ObserveElementObservableProperty(vm => vm.ObserveChanged).ToUnit(),
            }.Merge().Publish();

            var editEnd = IsEditting.Where(e => !e);
            var isEdittingFalse = IsEditting.Select(e => e ? Observable.Never<Unit>() : needRefresh).Switch();
            var isEdittingTrue = IsEditting.Select(e => e ? Observable.Defer(() => needRefresh.Buffer(editEnd.Take(1)).Where(xs => xs.Count >= 1).ToUnit()) : Observable.Never<Unit>()).Switch();
            var needRefreshFire = new[] { isEdittingFalse, isEdittingTrue }.Merge();
            var refreshView = Observable.Defer(() => {
                model.RefreshCollectionViews();
                return Observable.Return(Unit.Default);
            }).OnErrorRetry<Unit, InvalidOperationException>(_ => System.Diagnostics.Debug.WriteLine("Failed to refresh. Retry after 0.1 seconds."), retryCount: 5, delay: TimeSpan.FromSeconds(.1d))
            .Catch<Unit, InvalidOperationException>(e => {
                System.Diagnostics.Debug.WriteLine("Failed to refresh. CollectionView couldn't be refreshed.");
                return Observable.Return(Unit.Default);
            });
            needRefreshFire
                .ObserveOnUIDispatcher()
                .SelectSwitch(_ => refreshView)
                .Subscribe()
                .AddTo(Disposables);
            Disposables.Add(needRefresh.Connect());
        }

        public ReactivePropertySlim<string?> SelectedAnnotationLabel { get; }

        public ICollectionView PeakSpotsView { get; }

        public ReactivePropertySlim<bool> IsEditting { get; }

        public ReactiveProperty<double> AmplitudeLowerValue { get; }
        public ReactiveProperty<double> AmplitudeUpperValue { get; }
        public ReadOnlyReactiveCollection<ValueFilterViewModel> ValueFilterViewModels { get; }
        public ReadOnlyReactiveCollection<KeywordFilterViewModel> KeywordFilterViewModels { get; }

        public PeakFilterViewModel PeakFilterViewModel { get; }
        public PeakSpotTagSearchQueryBuilderViewModel TagSearchBuilderViewModel { get; }
    }
}

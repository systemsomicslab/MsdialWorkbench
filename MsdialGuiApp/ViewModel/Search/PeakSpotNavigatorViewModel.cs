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
    public sealed class PeakSpotNavigatorViewModel : ViewModelBase
    {
        private readonly PeakSpotNavigatorModel model;

        public PeakSpotNavigatorViewModel(PeakSpotNavigatorModel model) {
            this.model = model;
            SelectedAnnotationLabel = model
                .ToReactivePropertySlimAsSynchronized(m => m.SelectedAnnotationLabel)
                .AddTo(Disposables);
            PeakFilterViewModel = new PeakFilterViewModel(model.PeakFilters.ToArray()).AddTo(Disposables);

            AmplitudeLowerValue = model
                .ToReactivePropertyAsSynchronized(m => m.AmplitudeLowerValue)
                .AddTo(Disposables);
            AmplitudeUpperValue = model
                .ToReactivePropertyAsSynchronized(m => m.AmplitudeUpperValue)
                .AddTo(Disposables);
            AmplitudeLowerValue.SetValidateNotifyError(v => AmplitudeUpperValue.Value >= v ? null : "Too large");
            AmplitudeUpperValue.SetValidateNotifyError(v => AmplitudeLowerValue.Value <= v ? null : "Too small");
            MzLowerValue = model
                .ToReactivePropertyAsSynchronized(m => m.MzLowerValue)
                .AddTo(Disposables);
            MzUpperValue = model
                .ToReactivePropertyAsSynchronized(m => m.MzUpperValue)
                .AddTo(Disposables);
            MzLowerValue.SetValidateNotifyError(v => MzUpperValue.Value >= v ? null : "Too large");
            MzUpperValue.SetValidateNotifyError(v => MzLowerValue.Value <= v ? null : "Too small");
            RtLowerValue = model
                .ToReactivePropertyAsSynchronized(m => m.RtLowerValue)
                .AddTo(Disposables);
            RtUpperValue = model
                .ToReactivePropertyAsSynchronized(m => m.RtUpperValue)
                .AddTo(Disposables);
            RtLowerValue.SetValidateNotifyError(v => RtUpperValue.Value >= v ? null : "Too large");
            RtUpperValue.SetValidateNotifyError(v => RtLowerValue.Value <= v ? null : "Too small");
            DtLowerValue = model
                .ToReactivePropertyAsSynchronized(m => m.DtLowerValue)
                .AddTo(Disposables);
            DtUpperValue = model
                .ToReactivePropertyAsSynchronized(m => m.DtUpperValue)
                .AddTo(Disposables);
            DtLowerValue.SetValidateNotifyError(v => DtUpperValue.Value >= v ? null : "Too large");
            DtUpperValue.SetValidateNotifyError(v => DtLowerValue.Value <= v ? null : "Too small");

            MetaboliteFilterKeyword = new ReactivePropertySlim<string>(string.Empty).AddTo(Disposables);
            MetaboliteFilterKeyword
                .Where(keywords => !(keywords is null))
                .Select(keywords => Observable.FromAsync(token => model.SetMetaboliteKeywordsAsync(keywords.Split(), token)))
                .Switch()
                .Subscribe()
                .AddTo(Disposables);
            ProteinFilterKeyword = new ReactivePropertySlim<string>(string.Empty).AddTo(Disposables);
            ProteinFilterKeyword
                .Where(keywords => !(keywords is null))
                .Select(keywords => Observable.FromAsync(token => model.SetProteinKeywordsAsync(keywords.Split(), token)))
                .Switch()
                .Subscribe()
                .AddTo(Disposables);
            CommentFilterKeyword = new ReactivePropertySlim<string>(string.Empty).AddTo(Disposables);
            CommentFilterKeyword
                .Where(keywords => !(keywords is null))
                .Select(keywords => Observable.FromAsync(token => model.SetCommentKeywordsAsync(keywords.Split(), token)))
                .Switch()
                .Subscribe()
                .AddTo(Disposables);

            IsEditting = new ReactivePropertySlim<bool>().AddTo(Disposables);

            PeakSpotsView = CollectionViewSource.GetDefaultView(model.PeakSpots);

            var needRefresh = new[]
            {
                PeakFilterViewModel.CheckedFilter.ToUnit(),
                AmplitudeLowerValue.ToUnit(),
                AmplitudeUpperValue.ToUnit(),
                MzLowerValue.ToUnit(),
                MzUpperValue.ToUnit(),
                RtLowerValue.ToUnit(),
                RtUpperValue.ToUnit(),
                DtLowerValue.ToUnit(),
                DtUpperValue.ToUnit(),
                MetaboliteFilterKeyword.ToUnit(),
                ProteinFilterKeyword.ToUnit(),
                CommentFilterKeyword.ToUnit(),
            }.Merge();

            var ifIsEditting = needRefresh.Take(1).Zip(IsEditting.Where(x => !x)).Select(x => x.First);
            var ifIsNotEditting = needRefresh;

            IsEditting
                .Select(isEditting => isEditting ? ifIsEditting : ifIsNotEditting)
                .Switch()
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOnUIDispatcher()
                .SelectMany(_ => Observable.Defer(() => {
                    foreach (var view in model.PeakSpotsCollection) {
                        view.Refresh();
                    }
                    return Observable.Return(Unit.Default);
                }))
                .OnErrorRetry<Unit, InvalidOperationException>(_ => System.Diagnostics.Debug.WriteLine("Failed to refresh. Retry after 0.1 seconds."), retryCount: 5, delay: TimeSpan.FromSeconds(.1d))
                .Catch<Unit, InvalidOperationException>(e => {
                    System.Diagnostics.Debug.WriteLine("Failed to refresh. CollectionView couldn't be refreshed.");
                    return Observable.Return(Unit.Default);
                })
                .Repeat()
                .Subscribe()
                .AddTo(Disposables);
        }

        public ReactivePropertySlim<string> SelectedAnnotationLabel { get; }

        public ICollectionView PeakSpotsView { get; }

        public ReactivePropertySlim<bool> IsEditting { get; }

        public ReactiveProperty<double> AmplitudeLowerValue { get; }
        public ReactiveProperty<double> AmplitudeUpperValue { get; }
        public ReactiveProperty<double> MzLowerValue { get; }
        public ReactiveProperty<double> MzUpperValue { get; }
        public ReactiveProperty<double> RtLowerValue { get; }
        public ReactiveProperty<double> RtUpperValue { get; }
        public ReactiveProperty<double> DtLowerValue { get; }
        public ReactiveProperty<double> DtUpperValue { get; }

        public ReactivePropertySlim<string> MetaboliteFilterKeyword { get; }
        public ReactivePropertySlim<string> ProteinFilterKeyword { get; }
        public ReactivePropertySlim<string> CommentFilterKeyword { get; }

        public PeakFilterViewModel PeakFilterViewModel { get; }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                // PeakSpotsView.Filter -= PeakFilter;
            }
            base.Dispose(disposing);
        }

        ~PeakSpotNavigatorViewModel() {
            Dispose(disposing: false);
        }
    }
}

using CompMs.App.Msdial.Model.Search;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Search
{
    public class PeakSpotNavigatorViewModel : ViewModelBase
    {
        private readonly PeakSpotNavigatorModel model;

        public PeakSpotNavigatorViewModel(PeakSpotNavigatorModel model) {
            this.model = model;
            SelectedAnnotationLabel = model
                .ToReactivePropertySlimAsSynchronized(m => m.SelectedAnnotationLabel)
                .AddTo(Disposables);
            PeakFilterViewModel = new PeakFilterViewModel(model.PeakFilterModel).AddTo(Disposables);

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

            PeakSpotsView = CollectionViewSource.GetDefaultView(model.PeakSpots);
            PeakSpotsView.Filter += PeakFilter;

            new[]
            {
                PeakFilterViewModel.CheckedFilter.ToUnit(),
                AmplitudeLowerValue.ToUnit(),
                AmplitudeUpperValue.ToUnit(),
                MzLowerValue.ToUnit(),
                MzUpperValue.ToUnit(),
                RtLowerValue.ToUnit(),
                RtUpperValue.ToUnit(),
                MetaboliteFilterKeyword.ToUnit(),
                ProteinFilterKeyword.ToUnit(),
                CommentFilterKeyword.ToUnit(),
            }.Merge()
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOnUIDispatcher()
            .Subscribe(_ => PeakSpotsView?.Refresh())
            .AddTo(Disposables);
        }

        public ReactivePropertySlim<string> SelectedAnnotationLabel { get; }

        public ICollectionView PeakSpotsView { get; }

        public ReactiveProperty<double> AmplitudeLowerValue { get; }
        public ReactiveProperty<double> AmplitudeUpperValue { get; }
        public ReactiveProperty<double> MzLowerValue { get; }
        public ReactiveProperty<double> MzUpperValue { get; }
        public ReactiveProperty<double> RtLowerValue { get; }
        public ReactiveProperty<double> RtUpperValue { get; }

        public ReactivePropertySlim<string> MetaboliteFilterKeyword { get; }
        public ReactivePropertySlim<string> ProteinFilterKeyword { get; }
        public ReactivePropertySlim<string> CommentFilterKeyword { get; }

        public PeakFilterViewModel PeakFilterViewModel { get; }

        private bool PeakFilter(object obj) {
            return obj is IFilterable filterable && model.PeakFilter(filterable);
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                PeakSpotsView.Filter -= PeakFilter;
            }
            base.Dispose(disposing);
        }

        ~PeakSpotNavigatorViewModel() {
            Dispose(disposing: false);
        }
    }
}

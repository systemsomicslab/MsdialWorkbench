using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace CompMs.App.Msdial.Model.Search
{
    [Flags]
    public enum FilterEnableStatus {
        None = 0x0,
        Rt = 0x1,
        Dt = 0x2,
        Mz = 0x4,
        Metabolite = 0x8,
        Protein = 0x10,
        Comment = 0x20,
        Amplitude = 0x40,
        Ontology = 0x80,
        Adduct = 0x100,
        All = ~None,
    }

    internal sealed class PeakSpotNavigatorModel : DisposableModelBase
    {
        public PeakSpotNavigatorModel(object peakSpots, IReadOnlyList<ValueFilterModel> valueFilterModels, IReadOnlyList<KeywordFilterModel> keywordFilterModels, ValueFilterModel amplitudeFilterModel, PeakSpotTagSearchQueryBuilderModel tagSearchQueryBuilderModel) {
            PeakSpots = peakSpots ?? throw new ArgumentNullException(nameof(peakSpots));
            AmplitudeFilterModel = amplitudeFilterModel;
            ValueFilterModels = new ObservableCollection<ValueFilterModel>(valueFilterModels);
            KeywordFilterModels = new ObservableCollection<KeywordFilterModel>(keywordFilterModels);
            TagSearchQueryBuilder = tagSearchQueryBuilderModel;
            PeakSpotsCollection.CollectionChanged += OnPeakSpotsCollectionChanged;
        }

        public string? SelectedAnnotationLabel {
            get => _selectedAnnotationLabel;
            set => SetProperty(ref _selectedAnnotationLabel, value);
        }
        private string? _selectedAnnotationLabel;

        public object PeakSpots { get; }

        public ObservableCollection<ICollectionView> PeakSpotsCollection { get; } = new ObservableCollection<ICollectionView>();
        private readonly Dictionary<ICollectionView, PendingRefresh> _pendingRefreshes = new Dictionary<ICollectionView, PendingRefresh>();
        public ValueFilterModel AmplitudeFilterModel { get; }
        public ObservableCollection<ValueFilterModel> ValueFilterModels { get; }
        public ObservableCollection<KeywordFilterModel> KeywordFilterModels { get; }
        public PeakSpotTagSearchQueryBuilderModel TagSearchQueryBuilder { get; }
        public ObservableCollection<PeakFilterModel> PeakFilters { get; } = new ObservableCollection<PeakFilterModel>();

        public void ResetFilters() {
            foreach (var peakFilterModel in PeakFilters) {
                peakFilterModel.CheckedFilter = DisplayFilter.Unset;
            }
            AmplitudeFilterModel.Reset();
            foreach (var valueFilterModel in ValueFilterModels) {
                valueFilterModel.Reset();
            }
            foreach (var keywordFilterModel in KeywordFilterModels) {
                keywordFilterModel.ClearKeywords();
            }
            TagSearchQueryBuilder.Reset();
        }

        /// <summary>
        /// Refreshes every view that is not in an edit transaction now; a view in one is refreshed once, after its edit ends.
        /// </summary>
        /// <remarks>
        /// A view that fails to refresh does not stop the others: every view is refreshed first, then the failure is rethrown,
        /// or, when several views failed, all of them together in an <see cref="AggregateException"/>.
        /// </remarks>
        public void RefreshCollectionViews() {
            var errors = new List<Exception>();
            foreach (var view in PeakSpotsCollection) {
                // A view added after the navigator was disposed has no pending refresh, and is no longer refreshed.
                if (!_pendingRefreshes.TryGetValue(view, out var pendingRefresh)) {
                    continue;
                }
                try {
                    pendingRefresh.Request();
                }
                catch (Exception ex) {
                    errors.Add(ex);
                }
            }
            if (errors.Count == 1) {
                ExceptionDispatchInfo.Capture(errors[0]).Throw();
            }
            if (errors.Count > 1) {
                throw new AggregateException(errors);
            }
        }

        // Keeps one PendingRefresh per view in the list; a view leaving the list drops its pending refresh.
        private void OnPeakSpotsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            foreach (var removed in _pendingRefreshes.Keys.Except(PeakSpotsCollection).ToArray()) {
                _pendingRefreshes[removed].Dispose();
                _pendingRefreshes.Remove(removed);
            }
            foreach (var added in PeakSpotsCollection.Except(_pendingRefreshes.Keys).ToArray()) {
                _pendingRefreshes.Add(added, new PendingRefresh(added));
            }
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                PeakFilters.Clear();
                PeakSpotsCollection.Clear();
                PeakSpotsCollection.CollectionChanged -= OnPeakSpotsCollectionChanged;
            }
            base.Dispose(disposing);
        }
    }
}

using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

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
        }

        public string? SelectedAnnotationLabel {
            get => _selectedAnnotationLabel;
            set => SetProperty(ref _selectedAnnotationLabel, value);
        }
        private string? _selectedAnnotationLabel;

        public object PeakSpots { get; }

        public ObservableCollection<ICollectionView> PeakSpotsCollection { get; } = new ObservableCollection<ICollectionView>();
        public ValueFilterModel AmplitudeFilterModel { get; }
        public ObservableCollection<ValueFilterModel> ValueFilterModels { get; }
        public ObservableCollection<KeywordFilterModel> KeywordFilterModels { get; }
        public PeakSpotTagSearchQueryBuilderModel TagSearchQueryBuilder { get; }
        public ObservableCollection<PeakFilterModel> PeakFilters { get; } = new ObservableCollection<PeakFilterModel>();

        public void RefreshCollectionViews() {
            foreach (var view in PeakSpotsCollection) {
                view.Refresh();
            }
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                PeakFilters.Clear();
                PeakSpotsCollection.Clear();
            }
            base.Dispose(disposing);
        }
    }
}

using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
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
        public PeakSpotNavigatorModel(IReadOnlyList<IFilterable> peakSpots) {
            AmplitudeFilterModel = new ValueFilterModel { Lower = 0d, Upper = 1d, };
            MzFilterModel = new ValueFilterModel();
            RtFilterModel = new ValueFilterModel();
            DtFilterModel = new ValueFilterModel();

            PeakSpots = peakSpots ?? throw new ArgumentNullException(nameof(peakSpots));
            TagSearchQueryBuilder = new PeakSpotTagSearchQueryBuilderModel();
            MetaboliteFilterModel = new KeywordFilterModel().AddTo(Disposables);
            ProteinFilterModel = new KeywordFilterModel(KeywordFilteringType.KeepIfWordIsNull).AddTo(Disposables);
            CommentFilterModel = new KeywordFilterModel().AddTo(Disposables);
            OntologyFilterModel = new KeywordFilterModel(KeywordFilteringType.ExactMatch).AddTo(Disposables);
            AdductFilterModel = new KeywordFilterModel(KeywordFilteringType.KeepIfWordIsNull).AddTo(Disposables);
        }

        public string SelectedAnnotationLabel {
            get => _selectedAnnotationLabel;
            set => SetProperty(ref _selectedAnnotationLabel, value);
        }
        private string _selectedAnnotationLabel;

        public IReadOnlyList<IFilterable> PeakSpots { get; }
        public ObservableCollection<ICollectionView> PeakSpotsCollection { get; } = new ObservableCollection<ICollectionView>();
        public ValueFilterModel AmplitudeFilterModel { get; }
        public ValueFilterModel MzFilterModel { get; }
        public ValueFilterModel RtFilterModel { get; }
        public ValueFilterModel DtFilterModel { get; }
        public KeywordFilterModel MetaboliteFilterModel { get; }
        public KeywordFilterModel ProteinFilterModel { get; }
        public KeywordFilterModel CommentFilterModel { get; }
        public KeywordFilterModel OntologyFilterModel { get; }
        public KeywordFilterModel AdductFilterModel { get; }
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

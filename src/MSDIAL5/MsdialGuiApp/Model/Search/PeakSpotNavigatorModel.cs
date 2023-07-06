using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Data;

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
            if (peakSpots is INotifyCollectionChanged notifyCollection) {
                notifyCollection.CollectionChangedAsObservable().ToUnit()
                    .StartWith(Unit.Default)
                    .Throttle(TimeSpan.FromSeconds(.1d))
                    .Subscribe(_ =>
                    {
                        MzFilterModel.Lower = peakSpots.DefaultIfEmpty().Min(p => p?.Mass) ?? 0d;
                        MzFilterModel.Upper = peakSpots.DefaultIfEmpty().Max(p => p?.Mass) ?? 1d;
                        RtFilterModel.Lower = peakSpots.DefaultIfEmpty().Min(p => p?.ChromXs.RT.Value) ?? 0d;
                        RtFilterModel.Upper = peakSpots.DefaultIfEmpty().Max(p => p?.ChromXs.RT.Value) ?? 1d;
                        DtFilterModel.Lower = peakSpots.DefaultIfEmpty().Min(p => p?.ChromXs.Drift.Value) ?? 0d;
                        DtFilterModel.Upper = peakSpots.DefaultIfEmpty().Max(p => p?.ChromXs.Drift.Value) ?? 1d;
                    }).AddTo(Disposables);
            }
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

        public void AttachFilter<T>(IEnumerable<T> peaks, PeakSpotFiltering<T> peakSpotFiltering, PeakFilterModel peakFilterModel, IMatchResultEvaluator<T> evaluator, FilterEnableStatus status) where T: IFilterable {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ICollectionView collection = CollectionViewSource.GetDefaultView(peaks);
                if (collection is ListCollectionView list) {
                    list.IsLiveFiltering = true;
                }
                peakSpotFiltering.AttachFilter(collection, peakFilterModel, TagSearchQueryBuilder, evaluator);
                if ((status & FilterEnableStatus.Rt) != FilterEnableStatus.None) {
                    peakSpotFiltering.AttachFilter(RtFilterModel, obj => ((IFilterable)obj).ChromXs.RT.Value, collection);
                }
                if ((status & FilterEnableStatus.Dt) != FilterEnableStatus.None) {
                    peakSpotFiltering.AttachFilter(DtFilterModel, obj => ((IFilterable)obj).ChromXs.Drift.Value, collection);
                }
                if ((status & FilterEnableStatus.Mz) != FilterEnableStatus.None) {
                    peakSpotFiltering.AttachFilter(MzFilterModel, obj => ((IFilterable)obj).Mass, collection);
                }
                if ((status & FilterEnableStatus.Amplitude) != FilterEnableStatus.None) {
                    peakSpotFiltering.AttachFilter(AmplitudeFilterModel, obj => ((IFilterable)obj).RelativeAmplitudeValue, collection);
                }
                if ((status & FilterEnableStatus.Metabolite) != FilterEnableStatus.None) {
                    peakSpotFiltering.AttachFilter(MetaboliteFilterModel, obj => ((IFilterable)obj).Name, collection);
                }
                if ((status & FilterEnableStatus.Protein) != FilterEnableStatus.None) {
                    peakSpotFiltering.AttachFilter(ProteinFilterModel, obj => ((IFilterable)obj).Protein, collection);
                }
                if ((status & FilterEnableStatus.Comment) != FilterEnableStatus.None) {
                    peakSpotFiltering.AttachFilter(CommentFilterModel, obj => ((IFilterable)obj).Comment, collection);
                }
                if ((status & FilterEnableStatus.Adduct) != FilterEnableStatus.None) {
                    peakSpotFiltering.AttachFilter(AdductFilterModel, obj => ((IFilterable)obj).AdductIonName, collection);
                }
                if ((status & FilterEnableStatus.Ontology) != FilterEnableStatus.None) {
                    peakSpotFiltering.AttachFilter(OntologyFilterModel, obj => ((IFilterable)obj).Ontology, collection);
                }
                PeakSpotsCollection.Add(collection);
                PeakFilters.Add(peakFilterModel);
            });
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

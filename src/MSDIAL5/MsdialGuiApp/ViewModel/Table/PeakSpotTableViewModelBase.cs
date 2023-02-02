using CompMs.App.Msdial.Model.Table;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using System;
using System.ComponentModel;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Table
{
    public abstract class PeakSpotTableViewModelBase : ViewModelBase
    {
        private readonly IPeakSpotTableModelBase model;

        public PeakSpotTableViewModelBase(
            IPeakSpotTableModelBase model,
            IReactiveProperty<string> metaboliteFilterKeyword,
            IReactiveProperty<string> commentFilterKeyword,
            IReactiveProperty<string> ontologyFilterKeyword,
            IReactiveProperty<string> adductFilterKeyword) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            if (metaboliteFilterKeyword is null) {
                throw new ArgumentNullException(nameof(metaboliteFilterKeyword));
            }

            if (commentFilterKeyword is null) {
                throw new ArgumentNullException(nameof(commentFilterKeyword));
            }

            if (ontologyFilterKeyword is null) {
                throw new ArgumentNullException(nameof(ontologyFilterKeyword));
            }

            if (adductFilterKeyword is null) {
                throw new ArgumentNullException(nameof(adductFilterKeyword));
            }

            this.model = model;
            PeakSpotsView = CollectionViewSource.GetDefaultView(this.model.PeakSpots);
            Target = this.model.Target;
            MetaboliteFilterKeyword = metaboliteFilterKeyword;
            CommentFilterKeyword = commentFilterKeyword;
            OntologyFilterKeyword = ontologyFilterKeyword;
            AdductFilterKeyword = adductFilterKeyword;
        }

        public ICollectionView PeakSpotsView {
            get => peakSpotsView;
            set => SetProperty(ref peakSpotsView, value);
        }
        private ICollectionView peakSpotsView;

        public IReactiveProperty Target { get; }

        public IReactiveProperty<string> MetaboliteFilterKeyword { get; }

        public IReactiveProperty<string> CommentFilterKeyword { get; }

        public IReactiveProperty<string> OntologyFilterKeyword { get; }

        public IReactiveProperty<string> AdductFilterKeyword { get; }
    }
}

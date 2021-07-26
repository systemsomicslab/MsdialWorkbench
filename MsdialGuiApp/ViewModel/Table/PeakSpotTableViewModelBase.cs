using CompMs.App.Msdial.Model.Table;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using System;
using System.ComponentModel;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Table
{
    abstract class PeakSpotTableViewModelBase : ViewModelBase
    {
        private readonly IPeakSpotTableModelBase model;

        public PeakSpotTableViewModelBase(
            IPeakSpotTableModelBase model,
            IReactiveProperty<string> metaboliteFilterKeyword,
            IReactiveProperty<string> commentFilterKeyword) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            if (metaboliteFilterKeyword is null) {
                throw new ArgumentNullException(nameof(metaboliteFilterKeyword));
            }

            if (commentFilterKeyword is null) {
                throw new ArgumentNullException(nameof(commentFilterKeyword));
            }

            this.model = model;
            PeakSpotsView = CollectionViewSource.GetDefaultView(this.model.PeakSpots);
            Target = this.model.Target;
            MetaboliteFilterKeyword = metaboliteFilterKeyword;
            CommentFilterKeyword = commentFilterKeyword;
        }

        public ICollectionView PeakSpotsView {
            get => peakSpotsView;
            set => SetProperty(ref peakSpotsView, value);
        }
        private ICollectionView peakSpotsView;

        public IReactiveProperty Target { get; }

        public IReactiveProperty<string> MetaboliteFilterKeyword { get; }

        public IReactiveProperty<string> CommentFilterKeyword { get; }
    }
}

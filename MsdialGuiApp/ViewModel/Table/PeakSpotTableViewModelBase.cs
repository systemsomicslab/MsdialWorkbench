using CompMs.App.Msdial.Model.Table;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Table
{
    abstract class PeakSpotTableViewModelBase<T> : ViewModelBase
    {
        private readonly PeakSpotTableModelBase<T> model;

        public PeakSpotTableViewModelBase(
            PeakSpotTableModelBase<T> model,
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
            PeakSpots = this.model.PeakSpots;
            PeakSpotsView = CollectionViewSource.GetDefaultView(PeakSpots);
            Target = this.model.Target;
            MetaboliteFilterKeyword = metaboliteFilterKeyword;
            CommentFilterKeyword = commentFilterKeyword;
        }

        public ObservableCollection<T> PeakSpots { get; }

        public ICollectionView PeakSpotsView {
            get => peakSpotsView;
            set => SetProperty(ref peakSpotsView, value);
        }
        private ICollectionView peakSpotsView;

        public IReactiveProperty<T> Target { get; }

        public IReactiveProperty<string> MetaboliteFilterKeyword { get; }

        public IReactiveProperty<string> CommentFilterKeyword { get; }
    }
}

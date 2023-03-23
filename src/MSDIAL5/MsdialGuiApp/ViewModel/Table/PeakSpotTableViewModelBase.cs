using CompMs.App.Msdial.Model.Table;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using System;
using System.ComponentModel;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Table
{
    public abstract class PeakSpotTableViewModelBase : ViewModelBase
    {
        private readonly IPeakSpotTableModelBase _model;
        private readonly PeakSpotNavigatorViewModel _peakSpotNavigatorViewModel;

        public PeakSpotTableViewModelBase(IPeakSpotTableModelBase model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _peakSpotNavigatorViewModel = peakSpotNavigatorViewModel ?? throw new ArgumentNullException(nameof(peakSpotNavigatorViewModel));

            PeakSpotsView = CollectionViewSource.GetDefaultView(model.PeakSpots);
        }

        public ICollectionView PeakSpotsView { get; }
        public IReactiveProperty Target => _model.Target;
        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel => _peakSpotNavigatorViewModel;
    }
}

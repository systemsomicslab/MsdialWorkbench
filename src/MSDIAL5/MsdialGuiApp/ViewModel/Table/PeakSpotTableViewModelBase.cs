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

        public IReactiveProperty<double> MassLower => _peakSpotNavigatorViewModel.MzLowerValue;
        public IReactiveProperty<double> MassUpper => _peakSpotNavigatorViewModel.MzUpperValue;
        public IReactiveProperty<double> RtLower => _peakSpotNavigatorViewModel.RtLowerValue;
        public IReactiveProperty<double> RtUpper => _peakSpotNavigatorViewModel.RtUpperValue;
        public IReactiveProperty<double> DtLower => _peakSpotNavigatorViewModel.DtLowerValue;
        public IReactiveProperty<double> DtUpper => _peakSpotNavigatorViewModel.DtUpperValue;
        public IReactiveProperty<double> DriftLower => _peakSpotNavigatorViewModel.DtLowerValue;
        public IReactiveProperty<double> DriftUpper => _peakSpotNavigatorViewModel.DtUpperValue;

        public IReactiveProperty<string> MetaboliteFilterKeyword => _peakSpotNavigatorViewModel.MetaboliteFilterKeyword;
        public IReactiveProperty<string> CommentFilterKeyword => _peakSpotNavigatorViewModel.CommentFilterKeyword;
        public IReactiveProperty<string> OntologyFilterKeyword => _peakSpotNavigatorViewModel.OntologyFilterKeyword;
        public IReactiveProperty<string> AdductFilterKeyword => _peakSpotNavigatorViewModel.AdductFilterKeyword;

        public IReactiveProperty<bool> IsEdittng => _peakSpotNavigatorViewModel.IsEditting;
    }
}

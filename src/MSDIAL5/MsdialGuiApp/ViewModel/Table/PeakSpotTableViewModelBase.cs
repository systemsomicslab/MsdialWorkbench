using CompMs.App.Msdial.Model.Table;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using System;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Table
{
    internal abstract class PeakSpotTableViewModelBase : ViewModelBase
    {
        private readonly IPeakSpotTableModelBase _model;

        public PeakSpotTableViewModelBase(IPeakSpotTableModelBase model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            PeakSpotNavigatorViewModel = peakSpotNavigatorViewModel ?? throw new ArgumentNullException(nameof(peakSpotNavigatorViewModel));
            SetUnknownCommand = setUnknownCommand ?? throw new ArgumentNullException(nameof(setUnknownCommand));
            UndoManagerViewModel = undoManagerViewModel ?? throw new ArgumentNullException(nameof(undoManagerViewModel));
            PeakSpotsView = CollectionViewSource.GetDefaultView(model.PeakSpots);
        }

        public ICollectionView PeakSpotsView { get; }
        public IReactiveProperty Target => _model.Target;
        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }

        public ICommand SetUnknownCommand { get; }
        public UndoManagerViewModel UndoManagerViewModel { get; }
    }
}

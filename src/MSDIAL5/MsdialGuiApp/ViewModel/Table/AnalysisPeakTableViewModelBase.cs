using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Table;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Table
{
    internal abstract class AnalysisPeakTableViewModelBase : PeakSpotTableViewModelBase {
        public AnalysisPeakTableViewModelBase(IPeakSpotTableModelBase model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknonwCommand, UndoManagerViewModel undoManagerViewModel, IObservable<EicLoader> eicLoader)
            : base(model, peakSpotNavigatorViewModel, setUnknonwCommand, undoManagerViewModel) {
            if (eicLoader is null) {
                throw new ArgumentNullException(nameof(eicLoader));
            }

            EicLoader = eicLoader.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<EicLoader?> EicLoader { get; }
    }
}

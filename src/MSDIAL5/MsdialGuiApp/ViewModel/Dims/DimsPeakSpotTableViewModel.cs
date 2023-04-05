using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Graphics.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    internal sealed class DimsAnalysisPeakTableViewModel : AnalysisPeakTableViewModelBase
    {
        public DimsAnalysisPeakTableViewModel(DimsAnalysisPeakTableModel model, IObservable<EicLoader> eicLoader, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel, eicLoader) {
            MassMin = model.MassMin;
            MassMax = model.MassMax;
        }

        public double MassMin { get; }
        public double MassMax { get; }
    }

    internal sealed class DimsAlignmentSpotTableViewModel : AlignmentSpotTableViewModelBase
    {
        public DimsAlignmentSpotTableViewModel(DimsAlignmentSpotTableModel model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel) {
            MassMin = model.MassMin;
            MassMax = model.MassMax;
        }

        public double MassMin { get; }
        public double MassMax { get; }
    }
}

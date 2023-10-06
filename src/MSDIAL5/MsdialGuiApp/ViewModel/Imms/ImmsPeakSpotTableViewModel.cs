using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Table;
using Reactive.Bindings.Notifiers;
using System;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    internal sealed class ImmsAnalysisPeakTableViewModel : AnalysisPeakTableViewModelBase
    {
        public ImmsAnalysisPeakTableViewModel(ImmsAnalysisPeakTableModel model, IObservable<EicLoader> eicLoader, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel, eicLoader) {

            MassMin = model.MassMin;
            MassMax = model.MassMax;
            DriftMin = model.DriftMin;
            DriftMax = model.DriftMax;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double DriftMin { get; }
        public double DriftMax { get; }
    }

    internal sealed class ImmsAlignmentSpotTableViewModel : AlignmentSpotTableViewModelBase
    {
        public ImmsAlignmentSpotTableViewModel(ImmsAlignmentSpotTableModel model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel, IMessageBroker broker)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel, broker) {
            MassMin = model.MassMin;
            MassMax = model.MassMax;
            DriftMin = model.DriftMin;
            DriftMax = model.DriftMax;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double DriftMin { get; }
        public double DriftMax { get; }
    }
}

using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Table;
using Reactive.Bindings.Notifiers;
using System;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    internal sealed class LcimmsAnalysisPeakTableViewModel : AnalysisPeakTableViewModelBase
    {
        public LcimmsAnalysisPeakTableViewModel(LcimmsAnalysisPeakTableModel model, IObservable<EicLoader> eicLoader, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel, eicLoader) {
            MassMin = model.MassMin;
            MassMax = model.MassMax;
            RtMin = model.RtMin;
            RtMax = model.RtMax;
            DtMin = model.DtMin;
            DtMax = model.DtMax;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }
        public double DtMin { get; }
        public double DtMax { get; }
    }

    internal sealed class LcimmsAlignmentSpotTableViewModel : AlignmentSpotTableViewModelBase
    {
        public LcimmsAlignmentSpotTableViewModel(LcimmsAlignmentSpotTableModel model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel, IMessageBroker broker)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel, broker) {
            MassMin = model.MassMin;
            MassMax = model.MassMax;
            RtMin = model.RtMin;
            RtMax = model.RtMax;
            DtMin = model.DtMin;
            DtMax = model.DtMax;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }
        public double DtMin { get; }
        public double DtMax { get; }
    }
}

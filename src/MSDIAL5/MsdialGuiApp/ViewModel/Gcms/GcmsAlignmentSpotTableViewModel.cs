using CompMs.App.Msdial.Model.Gcms;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Table;
using Reactive.Bindings.Notifiers;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Gcms
{
    internal sealed class GcmsAlignmentSpotTableViewModel : AlignmentSpotTableViewModelBase
    {
        public GcmsAlignmentSpotTableViewModel(GcmsAlignmentSpotTableModel model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknonwCommand, UndoManagerViewModel undoManagerViewModel, IMessageBroker broker)
            : base(model, peakSpotNavigatorViewModel, setUnknonwCommand, undoManagerViewModel, broker) {
            MassMin = model.MassMin;
            MassMax = model.MassMax;
            RtMin = model.RtMin;
            RtMax = model.RtMax;
            RiMin = model.RiMin;
            RiMax = model.RiMax;
            IsRiValid = model.IsRiValid;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }
        public double RiMin { get; }
        public double RiMax { get; }
        public bool IsRiValid { get; }
    }
}

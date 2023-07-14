using CompMs.App.Msdial.Model.Gcms;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Table;
using System;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Gcms
{
    internal sealed class GcmsAnalysisPeakTableViewModel : AnalysisPeakTableViewModelBase
    {
        public GcmsAnalysisPeakTableViewModel(GcmsAnalysisPeakTableModel model, IObservable<EicLoader> eicLoader, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel, eicLoader) {
            MassMin = model.MassMin;
            MassMax = model.MassMax;
            RtMin = model.RtMin;
            RtMax = model.RtMax;
            RiMin = model.RiMin;
            RiMax = model.RiMax;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }
        public double RiMin { get; }
        public double RiMax { get; }
    }
}

using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imms;
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
        public ImmsAlignmentSpotTableViewModel(ImmsAlignmentSpotTableModel model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel) {
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

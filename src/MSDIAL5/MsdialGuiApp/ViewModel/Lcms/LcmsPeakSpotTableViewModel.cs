using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Table;
using Reactive.Bindings.Notifiers;
using System;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    internal sealed class LcmsProteomicsPeakTableViewModel : AnalysisPeakTableViewModelBase {
        public LcmsProteomicsPeakTableViewModel(LcmsAnalysisPeakTableModel model, IObservable<EicLoader> eicLoader, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel, eicLoader) {
            MassMin = model.MassMin;
            MassMax = model.MassMax;
            RtMin = model.RtMin;
            RtMax = model.RtMax;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }
    }

    internal sealed class LcmsAnalysisPeakTableViewModel : AnalysisPeakTableViewModelBase
    {
        public LcmsAnalysisPeakTableViewModel(LcmsAnalysisPeakTableModel model, IObservable<EicLoader> eicLoader, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel, eicLoader) {
            MassMin = model.MassMin;
            MassMax = model.MassMax;
            RtMin = model.RtMin;
            RtMax = model.RtMax;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }
    }

    internal sealed class LcmsAlignmentSpotTableViewModel : AlignmentSpotTableViewModelBase
    {
        public LcmsAlignmentSpotTableViewModel(LcmsAlignmentSpotTableModel model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel, IMessageBroker broker)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel, broker) {
            MassMin = model.MassMin;
            MassMax = model.MassMax;
            RtMin = model.RtMin;
            RtMax = model.RtMax;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }
    }

    internal sealed class LcmsProteomicsAlignmentTableViewModel : AlignmentSpotTableViewModelBase {
        public LcmsProteomicsAlignmentTableViewModel(LcmsAlignmentSpotTableModel model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel, IMessageBroker broker)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel, broker) {
            MassMin = model.MassMin;
            MassMax = model.MassMax;
            RtMin = model.RtMin;
            RtMax = model.RtMax;

        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }
    }

    internal static class LcmsTableViewModelHelper {
        public static AnalysisPeakTableViewModelBase CreateViewModel(LcmsAnalysisPeakTableModel model, IObservable<EicLoader> eicLoader, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel) {
            switch (model.Omics) {
                case CompMs.Common.Enum.TargetOmics.Proteomics:
                    return new LcmsProteomicsPeakTableViewModel(model, eicLoader, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel);
                case CompMs.Common.Enum.TargetOmics.Metabolomics:
                case CompMs.Common.Enum.TargetOmics.Lipidomics:
                    return new LcmsAnalysisPeakTableViewModel(model, eicLoader, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel);
                default:
                    throw new NotSupportedException($"Unknown target({model.Omics})");
            }
        }

        public static AlignmentSpotTableViewModelBase CreateViewModel(LcmsAlignmentSpotTableModel model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel, IMessageBroker broker) {
            switch (model.Omics) {
                case CompMs.Common.Enum.TargetOmics.Proteomics:
                    return new LcmsProteomicsAlignmentTableViewModel(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel, broker);
                case CompMs.Common.Enum.TargetOmics.Metabolomics:
                case CompMs.Common.Enum.TargetOmics.Lipidomics:
                    return new LcmsAlignmentSpotTableViewModel(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel, broker);
                default:
                    throw new NotSupportedException($"Unknown target({model.Omics})");
            }
        }
    }
}

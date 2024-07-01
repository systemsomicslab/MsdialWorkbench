using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Model.Table;
using CompMs.Common.Enum;
using CompMs.Graphics.Base;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Lcms
{
    interface ILcmsPeakSpotTableModel : IPeakSpotTableModelBase
    {
        double MassMin { get; }
        double MassMax { get; }
        double RtMin { get; }
        double RtMax { get; }
    }

    internal sealed class LcmsAlignmentSpotTableModel : AlignmentSpotTableModelBase, ILcmsPeakSpotTableModel
    {
        public LcmsAlignmentSpotTableModel(
            IReadOnlyList<AlignmentSpotPropertyModel> peakSpots,
            IReactiveProperty<AlignmentSpotPropertyModel?> target,
            IObservable<IBrushMapper<BarItem>> classBrush,
            FileClassPropertiesModel classProperties,
            IObservable<IBarItemsLoader> barItemsLoader,
            TargetOmics omics,
            PeakSpotFiltering<AlignmentSpotPropertyModel>.PeakSpotFilter peakSpotFilter,
            AlignmentSpotSpectraLoader spectraLoader,
            UndoManager undoManager)
            : base(peakSpots, target, classBrush, classProperties, barItemsLoader, peakSpotFilter, spectraLoader, undoManager) {
            MassMin = peakSpots.Select(s => s.Mass).DefaultIfEmpty().Min();
            MassMax = peakSpots.Select(s => s.Mass).DefaultIfEmpty().Max();
            RtMin = peakSpots.Select(s => s.RT).DefaultIfEmpty().Min();
            RtMax = peakSpots.Select(s => s.RT).DefaultIfEmpty().Max();
            Omics = omics;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }
        public TargetOmics Omics { get; }
    }

    internal sealed class LcmsAnalysisPeakTableModel : AnalysisPeakSpotTableModelBase, ILcmsPeakSpotTableModel
    {
        public LcmsAnalysisPeakTableModel(IReadOnlyList<ChromatogramPeakFeatureModel> peakSpots, IReactiveProperty<ChromatogramPeakFeatureModel?> target, PeakSpotNavigatorModel peakSpotNavigatorModel, TargetOmics omics, UndoManager undoManager)
            : base(peakSpots, target, peakSpotNavigatorModel, undoManager) {
            MassMin = peakSpots.Select(s => s.Mass).DefaultIfEmpty().Min();
            MassMax = peakSpots.Select(s => s.Mass).DefaultIfEmpty().Max();
            RtMin = peakSpots.Select(s => s.RT.Value).DefaultIfEmpty().Min();
            RtMax = peakSpots.Select(s => s.RT.Value).DefaultIfEmpty().Max();
            Omics = omics;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }
        public TargetOmics Omics { get; }
    }
}

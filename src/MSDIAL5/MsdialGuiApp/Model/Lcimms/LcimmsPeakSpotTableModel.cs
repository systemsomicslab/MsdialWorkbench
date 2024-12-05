using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Model.Table;
using CompMs.Graphics.Base;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Lcimms
{
    internal interface ILcimmsPeakSpotTableModel : IPeakSpotTableModelBase {
        double MassMin { get; }
        double MassMax { get; }
        double RtMin { get; }
        double RtMax { get; }
        double DtMin { get; }
        double DtMax { get; }
    }

    internal sealed class LcimmsAlignmentSpotTableModel : AlignmentSpotTableModelBase, ILcimmsPeakSpotTableModel
    {
        public LcimmsAlignmentSpotTableModel(
            IReadOnlyList<AlignmentSpotPropertyModel> peakSpots,
            IReactiveProperty<AlignmentSpotPropertyModel?> target,
            IObservable<IBrushMapper<BarItem>> classBrush,
            FileClassPropertiesModel classProperties,
            IObservable<IBarItemsLoader> barItemsLoader,
            PeakSpotFiltering<AlignmentSpotPropertyModel>.PeakSpotFilter peakSpotFilter,
            AlignmentSpotSpectraLoader spectraLoader,
            UndoManager undoManager)
            : base(peakSpots, target, classBrush, classProperties, barItemsLoader, peakSpotFilter, spectraLoader, undoManager) {
            MassMin = peakSpots.Select(s => s.Mass).DefaultIfEmpty().Min();
            MassMax = peakSpots.Select(s => s.Mass).DefaultIfEmpty().Max();
            RtMin = peakSpots.Select(s => s.RT).DefaultIfEmpty().Min();
            RtMax = peakSpots.Select(s => s.RT).DefaultIfEmpty().Max();
            DtMin = peakSpots.Select(s => s.Drift).DefaultIfEmpty().Min();
            DtMax = peakSpots.Select(s => s.Drift).DefaultIfEmpty().Max();
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }
        public double DtMin { get; }
        public double DtMax { get; }
    }

    internal sealed class LcimmsAnalysisPeakTableModel : AnalysisPeakSpotTableModelBase, ILcimmsPeakSpotTableModel
    {
        public LcimmsAnalysisPeakTableModel(IReadOnlyList<ChromatogramPeakFeatureModel> peakSpots, IReactiveProperty<ChromatogramPeakFeatureModel?> target, PeakSpotNavigatorModel peakSpotNavigatorModel, UndoManager undoManager)
            : base(peakSpots, target, peakSpotNavigatorModel, undoManager) {
            MassMin = peakSpots.Select(s => s.Mass).DefaultIfEmpty().Min();
            MassMax = peakSpots.Select(s => s.Mass).DefaultIfEmpty().Max();
            RtMin = peakSpots.Select(s => s.RT.Value).DefaultIfEmpty().Min();
            RtMax = peakSpots.Select(s => s.RT.Value).DefaultIfEmpty().Max();
            DtMin = peakSpots.Select(s => s.Drift.Value).DefaultIfEmpty().Min();
            DtMax = peakSpots.Select(s => s.Drift.Value).DefaultIfEmpty().Max();
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }
        public double DtMin { get; }
        public double DtMax { get; }
    }
}

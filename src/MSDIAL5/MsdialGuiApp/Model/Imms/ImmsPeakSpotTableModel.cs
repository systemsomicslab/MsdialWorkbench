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

namespace CompMs.App.Msdial.Model.Imms
{
    internal interface IImmsPeakSpotTableModel : IPeakSpotTableModelBase
    {
        double MassMin { get; }
        double MassMax { get; }
        double DriftMin { get; }
        double DriftMax { get; }
    }

    internal sealed class ImmsAlignmentSpotTableModel : AlignmentSpotTableModelBase, IImmsPeakSpotTableModel
    {
        public ImmsAlignmentSpotTableModel(
            IReadOnlyList<AlignmentSpotPropertyModel> spots,
            IReactiveProperty<AlignmentSpotPropertyModel?> target,
            IObservable<IBrushMapper<BarItem>> classBrush,
            FileClassPropertiesModel classProperties,
            IObservable<IBarItemsLoader> barItemsLoader,
            PeakSpotFiltering<AlignmentSpotPropertyModel>.PeakSpotFilter peakSpotFilter,
            AlignmentSpotSpectraLoader spectraLoader,
            UndoManager undoManager)
            : base(spots, target, classBrush, classProperties, barItemsLoader, peakSpotFilter, spectraLoader, undoManager) {

            MassMin = spots.Select(s => s.Mass).DefaultIfEmpty().Min();
            MassMax = spots.Select(s => s.Mass).DefaultIfEmpty().Max();
            DriftMin = spots.Select(s => s.Drift).DefaultIfEmpty().Min();
            DriftMax = spots.Select(s => s.Drift).DefaultIfEmpty().Max();
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double DriftMin { get; }
        public double DriftMax { get; }
    }

    internal sealed class ImmsAnalysisPeakTableModel : AnalysisPeakSpotTableModelBase, IImmsPeakSpotTableModel
    {
        public ImmsAnalysisPeakTableModel(IReadOnlyList<ChromatogramPeakFeatureModel> peaks, IReactiveProperty<ChromatogramPeakFeatureModel?> target, PeakSpotNavigatorModel peakSpotNavigatorModel, UndoManager undoManager)
            : base(peaks, target, peakSpotNavigatorModel, undoManager) {
            MassMin = peaks.Select(s => s.Mass).DefaultIfEmpty().Min();
            MassMax = peaks.Select(s => s.Mass).DefaultIfEmpty().Max();
            DriftMin = peaks.Select(s => s.Drift.Value).DefaultIfEmpty().Min();
            DriftMax = peaks.Select(s => s.Drift.Value).DefaultIfEmpty().Max();
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double DriftMin { get; }
        public double DriftMax { get; }
    }
}

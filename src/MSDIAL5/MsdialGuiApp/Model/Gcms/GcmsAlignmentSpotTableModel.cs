using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Model.Table;
using CompMs.Graphics.Base;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Gcms
{
    internal sealed class GcmsAlignmentSpotTableModel : AlignmentSpotTableModelBase
    {
        public GcmsAlignmentSpotTableModel(IReadOnlyList<AlignmentSpotPropertyModel> spots, IReactiveProperty<AlignmentSpotPropertyModel?> target, IObservable<IBrushMapper<BarItem>> classBrush, FileClassPropertiesModel classProperties, IObservable<IBarItemsLoader> barItemsLoader, PeakSpotFiltering<AlignmentSpotPropertyModel>.PeakSpotFilter peakSpotFilter, AlignmentSpotSpectraLoader spectraLoader)
            : base(spots, target, classBrush, classProperties, barItemsLoader, peakSpotFilter, spectraLoader) {

            MassMin = spots.Select(s => s.Mass).DefaultIfEmpty().Min();
            MassMax = spots.Select(s => s.Mass).DefaultIfEmpty().Max();
            RtMin = spots.Select(s => s.RT).DefaultIfEmpty().Min();
            RtMax = spots.Select(s => s.RT).DefaultIfEmpty().Max();
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }
    }
}

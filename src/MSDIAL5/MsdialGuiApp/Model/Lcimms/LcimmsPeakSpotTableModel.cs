using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Model.Table;
using CompMs.Common.Interfaces;
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

    internal abstract class LcimmsPeakSpotTableModel<T> : PeakSpotTableModelBase<T>, ILcimmsPeakSpotTableModel where T : class, IChromatogramPeak
    {
        public LcimmsPeakSpotTableModel(IReadOnlyList<T> peakSpots, IReactiveProperty<T> target, PeakSpotNavigatorModel peakSpotNavigatorModel) : base(peakSpots, target, peakSpotNavigatorModel) {
            MassMin = peakSpots.Select(s => s.Mass).DefaultIfEmpty().Min();
            MassMax = peakSpots.Select(s => s.Mass).DefaultIfEmpty().Max();
            RtMin = peakSpots.Select(s => s.ChromXs.RT.Value).DefaultIfEmpty().Min();
            RtMax = peakSpots.Select(s => s.ChromXs.RT.Value).DefaultIfEmpty().Max();
            DtMin = peakSpots.Select(s => s.ChromXs.Drift.Value).DefaultIfEmpty().Min();
            DtMax = peakSpots.Select(s => s.ChromXs.Drift.Value).DefaultIfEmpty().Max();
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }
        public double DtMin { get; }
        public double DtMax { get; }
    }

    internal sealed class LcimmsAlignmentSpotTableModel : LcimmsPeakSpotTableModel<AlignmentSpotPropertyModel>
    {
        public LcimmsAlignmentSpotTableModel(
            IReadOnlyList<AlignmentSpotPropertyModel> peakSpots,
            IReactiveProperty<AlignmentSpotPropertyModel> target,
            IObservable<IBrushMapper<BarItem>> classBrush,
            FileClassPropertiesModel classProperties,
            IObservable<IBarItemsLoader> barItemsLoader,
            PeakSpotNavigatorModel peakSpotNavigatorModel)
            : base(peakSpots, target, peakSpotNavigatorModel) {
            ClassBrush = classBrush;
            BarItemsLoader = barItemsLoader;
            FileClassProperties = classProperties;
        }

        public IObservable<IBrushMapper<BarItem>> ClassBrush { get; }
        public IObservable<IBarItemsLoader> BarItemsLoader { get; }
        public FileClassPropertiesModel FileClassProperties { get; }
    }

    internal sealed class LcimmsAnalysisPeakTableModel : LcimmsPeakSpotTableModel<ChromatogramPeakFeatureModel>
    {
        public LcimmsAnalysisPeakTableModel(IReadOnlyList<ChromatogramPeakFeatureModel> peakSpots, IReactiveProperty<ChromatogramPeakFeatureModel> target, PeakSpotNavigatorModel peakSpotNavigatorModel)
            : base(peakSpots, target, peakSpotNavigatorModel) {
        }
    }
}

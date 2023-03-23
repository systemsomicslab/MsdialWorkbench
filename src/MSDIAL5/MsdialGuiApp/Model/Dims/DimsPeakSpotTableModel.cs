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

namespace CompMs.App.Msdial.Model.Dims
{
    internal interface IDimsPeakSpotTableModel : IPeakSpotTableModelBase
    {
        double MassMin { get; }
        double MassMax { get; }
    }

    internal abstract class DimsPeakSpotTableModel<T> : PeakSpotTableModelBase<T>, IDimsPeakSpotTableModel where T: class, ISpectrumPeak
    {
        protected DimsPeakSpotTableModel(IReadOnlyList<T> peakSpots, IReactiveProperty<T> target, PeakSpotNavigatorModel peakSpotNavigatorModel) : base(peakSpots, target, peakSpotNavigatorModel) {
            MassMin = peakSpots.Select(s => s.Mass).DefaultIfEmpty().Min();
            MassMax = peakSpots.Select(s => s.Mass).DefaultIfEmpty().Max();
        }

        public double MassMin { get; }
        public double MassMax { get; }
    }

    internal sealed class DimsAnalysisPeakTableModel : DimsPeakSpotTableModel<ChromatogramPeakFeatureModel>
    {
        public DimsAnalysisPeakTableModel(IReadOnlyList<ChromatogramPeakFeatureModel> peaks, IReactiveProperty<ChromatogramPeakFeatureModel> target, PeakSpotNavigatorModel peakSpotNavigatorModel)
            : base(peaks, target, peakSpotNavigatorModel) {

        }
    }

    internal sealed class DimsAlignmentSpotTableModel : DimsPeakSpotTableModel<AlignmentSpotPropertyModel>
    {
        public DimsAlignmentSpotTableModel(
            IReadOnlyList<AlignmentSpotPropertyModel> spots,
            IReactiveProperty<AlignmentSpotPropertyModel> target,
            IObservable<IBrushMapper<BarItem>> classBrush,
            FileClassPropertiesModel classProperties,
            IObservable<IBarItemsLoader> barItemsLoader,
            PeakSpotNavigatorModel peakSpotNavigatorModel)
            : base(spots, target, peakSpotNavigatorModel) {
            ClassBrush = classBrush;
            BarItemsLoader = barItemsLoader;
            FileClassProperties = classProperties;
        }

        public IObservable<IBrushMapper<BarItem>> ClassBrush { get; }
        public IObservable<IBarItemsLoader> BarItemsLoader { get; }
        public FileClassPropertiesModel FileClassProperties { get; }
    }
}

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
using System.Collections.ObjectModel;
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

    internal abstract class ImmsPeakSpotTableModel<T> : PeakSpotTableModelBase<T>, IImmsPeakSpotTableModel where T : class, IChromatogramPeak
    {
        public ImmsPeakSpotTableModel(IReadOnlyList<T> peakSpots, IReactiveProperty<T> target, PeakSpotNavigatorModel peakSpotNavigatorModel) : base(peakSpots, target, peakSpotNavigatorModel) {
            MassMin = peakSpots.Select(s => s.Mass).DefaultIfEmpty().Min();
            MassMax = peakSpots.Select(s => s.Mass).DefaultIfEmpty().Max();
            DriftMin = peakSpots.Select(s => s.ChromXs.Drift.Value).DefaultIfEmpty().Min();
            DriftMax = peakSpots.Select(s => s.ChromXs.Drift.Value).DefaultIfEmpty().Max();
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double DriftMin { get; }
        public double DriftMax { get; }
    }

    internal sealed class ImmsAlignmentSpotTableModel : ImmsPeakSpotTableModel<AlignmentSpotPropertyModel>
    {
        public ImmsAlignmentSpotTableModel(
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

    internal sealed class ImmsAnalysisPeakTableModel : ImmsPeakSpotTableModel<ChromatogramPeakFeatureModel>
    {
        public ImmsAnalysisPeakTableModel(IReadOnlyList<ChromatogramPeakFeatureModel> peaks, IReactiveProperty<ChromatogramPeakFeatureModel> target, PeakSpotNavigatorModel peakSpotNavigatorModel)
            : base(peaks, target, peakSpotNavigatorModel) {
        }
    }
}

using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Model.Table;
using CompMs.Graphics.Base;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Dims
{
    interface IDimsPeakSpotTableModel : IPeakSpotTableModelBase
    {
        double MassMin { get; }
        double MassMax { get; }
    }

    abstract class DimsPeakSpotTableModel<T> : PeakSpotTableModelBase<T>, IDimsPeakSpotTableModel where T: class
    {
        protected DimsPeakSpotTableModel(
            ObservableCollection<T> peakSpots,
            IReactiveProperty<T> target,
            double massMin, double massMax)
            : base(peakSpots, target) {

            MassMin = massMin;
            MassMax = massMax;
        }

        public double MassMin { get; }

        public double MassMax { get; }
    }

    internal sealed class DimsAnalysisPeakTableModel : DimsPeakSpotTableModel<ChromatogramPeakFeatureModel>
    {
        public DimsAnalysisPeakTableModel(ObservableCollection<ChromatogramPeakFeatureModel> peaks, IReactiveProperty<ChromatogramPeakFeatureModel> target)
            : base(peaks, target, peaks.Select(peak => peak.Mass).DefaultIfEmpty().Min(), peaks.Select(peak => peak.Mass).DefaultIfEmpty().Max()) {

        }
    }

    internal sealed class DimsAlignmentSpotTableModel : DimsPeakSpotTableModel<AlignmentSpotPropertyModel>
    {
        public DimsAlignmentSpotTableModel(
            ObservableCollection<AlignmentSpotPropertyModel> spots,
            IReactiveProperty<AlignmentSpotPropertyModel> target,
            IObservable<IBrushMapper<BarItem>> classBrush,
            FileClassPropertiesModel classProperties,
            IObservable<IBarItemsLoader> barItemsLoader)
            : base(spots, target, spots.Select(spot => spot.MassCenter).DefaultIfEmpty().Min(), spots.Select(spot => spot.MassCenter).DefaultIfEmpty().Max()) {
            ClassBrush = classBrush;
            BarItemsLoader = barItemsLoader;
            FileClassProperties = classProperties;
        }

        public IObservable<IBrushMapper<BarItem>> ClassBrush { get; }
        public IObservable<IBarItemsLoader> BarItemsLoader { get; }
        public FileClassPropertiesModel FileClassProperties { get; }
    }
}

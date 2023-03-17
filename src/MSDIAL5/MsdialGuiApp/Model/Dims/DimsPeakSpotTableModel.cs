using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Model.Table;
using CompMs.Graphics.Base;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Dims
{
    internal interface IDimsPeakSpotTableModel : IPeakSpotTableModelBase
    {
        double MassMin { get; }
        double MassMax { get; }
    }

    internal abstract class DimsPeakSpotTableModel<T> : PeakSpotTableModelBase<T>, IDimsPeakSpotTableModel where T: class
    {
        private readonly PeakSpotNavigatorModel _peakSpotNavigatorModel;

        protected DimsPeakSpotTableModel(ObservableCollection<T> peakSpots, IReactiveProperty<T> target, PeakSpotNavigatorModel peakSpotNavigatorModel) : base(peakSpots, target) {
            _peakSpotNavigatorModel = peakSpotNavigatorModel;
        }

        public double MassMin => _peakSpotNavigatorModel.MzLowerValue;
        public double MassMax => _peakSpotNavigatorModel.MzUpperValue;
    }

    internal sealed class DimsAnalysisPeakTableModel : DimsPeakSpotTableModel<ChromatogramPeakFeatureModel>
    {
        public DimsAnalysisPeakTableModel(ObservableCollection<ChromatogramPeakFeatureModel> peaks, IReactiveProperty<ChromatogramPeakFeatureModel> target, PeakSpotNavigatorModel peakSpotNavigatorModel)
            : base(peaks, target, peakSpotNavigatorModel) {

        }
    }

    internal sealed class DimsAlignmentSpotTableModel : DimsPeakSpotTableModel<AlignmentSpotPropertyModel>
    {
        public DimsAlignmentSpotTableModel(
            ObservableCollection<AlignmentSpotPropertyModel> spots,
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

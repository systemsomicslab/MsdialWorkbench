using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Model.Table;
using CompMs.Graphics.Base;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Lcms
{
    interface ILcmsPeakSpotTableModel : IPeakSpotTableModelBase
    {
        double MassMin { get; }
        double MassMax { get; }
        double RtMin { get; }
        double RtMax { get; }
    }

    internal abstract class LcmsPeakSpotTableModel<T> : PeakSpotTableModelBase<T>, ILcmsPeakSpotTableModel where T : class
    {
        private readonly PeakSpotNavigatorModel _peakSpotNavigatorModel;

        public LcmsPeakSpotTableModel(ObservableCollection<T> peakSpots, IReactiveProperty<T> target, PeakSpotNavigatorModel peakSpotNavigatorModel) : base(peakSpots, target) {
            _peakSpotNavigatorModel = peakSpotNavigatorModel ?? throw new ArgumentNullException(nameof(peakSpotNavigatorModel));
        }

        public double MassMin => _peakSpotNavigatorModel.MzLowerValue;
        public double MassMax => _peakSpotNavigatorModel.MzUpperValue;
        public double RtMin => _peakSpotNavigatorModel.RtLowerValue;
        public double RtMax => _peakSpotNavigatorModel.RtUpperValue;
    }

    internal sealed class LcmsAlignmentSpotTableModel : LcmsPeakSpotTableModel<AlignmentSpotPropertyModel>
    {
        public LcmsAlignmentSpotTableModel(
            ObservableCollection<AlignmentSpotPropertyModel> peakSpots,
            IReactiveProperty<AlignmentSpotPropertyModel> target,
            PeakSpotNavigatorModel peakSpotNavigatorModel,
            IObservable<IBrushMapper<BarItem>> classBrush,
            FileClassPropertiesModel classProperties,
            IObservable<IBarItemsLoader> barItemsLoader)
            : base(peakSpots, target, peakSpotNavigatorModel) {
            ClassBrush = classBrush;
            BarItemsLoader = barItemsLoader;
            FileClassProperties = classProperties;
        }

        public IObservable<IBrushMapper<BarItem>> ClassBrush { get; }
        public IObservable<IBarItemsLoader> BarItemsLoader { get; }
        public FileClassPropertiesModel FileClassProperties { get; }
    }

    internal sealed class LcmsAnalysisPeakTableModel : LcmsPeakSpotTableModel<ChromatogramPeakFeatureModel>
    {
        public LcmsAnalysisPeakTableModel(ObservableCollection<ChromatogramPeakFeatureModel> peakSpots, IReactiveProperty<ChromatogramPeakFeatureModel> target, PeakSpotNavigatorModel peakSpotNavigatorModel)
            : base(peakSpots, target, peakSpotNavigatorModel) {

        }
    }
}

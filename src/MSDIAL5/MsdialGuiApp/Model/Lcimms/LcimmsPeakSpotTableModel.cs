using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Model.Table;
using CompMs.Graphics.Base;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;

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

    internal abstract class LcimmsPeakSpotTableModel<T> : PeakSpotTableModelBase<T>, ILcimmsPeakSpotTableModel where T : class
    {
        private readonly PeakSpotNavigatorModel _peakSpotNavigatorModel;

        public LcimmsPeakSpotTableModel(ReadOnlyObservableCollection<T> peakSpots, IReactiveProperty<T> target, PeakSpotNavigatorModel peakSpotNavigatorModel) : base(peakSpots, target) {
            _peakSpotNavigatorModel = peakSpotNavigatorModel ?? throw new ArgumentNullException(nameof(peakSpotNavigatorModel));
        }

        public double MassMin => _peakSpotNavigatorModel.MzLowerValue;
        public double MassMax => _peakSpotNavigatorModel.MzUpperValue;
        public double RtMin => _peakSpotNavigatorModel.RtLowerValue;
        public double RtMax => _peakSpotNavigatorModel.RtUpperValue;
        public double DtMin => _peakSpotNavigatorModel.DtLowerValue;
        public double DtMax => _peakSpotNavigatorModel.DtUpperValue;
    }

    internal sealed class LcimmsAlignmentSpotTableModel : LcimmsPeakSpotTableModel<AlignmentSpotPropertyModel>
    {
        public LcimmsAlignmentSpotTableModel(
            ReadOnlyObservableCollection<AlignmentSpotPropertyModel> peakSpots,
            IReactiveProperty<AlignmentSpotPropertyModel> target,
            IObservable<IBrushMapper<BarItem>> classBrush,
            FileClassPropertiesModel classProperties,
            IObservable<IBarItemsLoader> barItemsLoader,
            PeakSpotNavigatorModel peakSpotNavigatorModel)
            : base(peakSpots, target,
                  peakSpotNavigatorModel) {
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
        public LcimmsAnalysisPeakTableModel(ReadOnlyObservableCollection<ChromatogramPeakFeatureModel> peakSpots, IReactiveProperty<ChromatogramPeakFeatureModel> target, PeakSpotNavigatorModel peakSpotNavigatorModel)
            : base(peakSpots, target,
                peakSpotNavigatorModel) {
        }
    }
}

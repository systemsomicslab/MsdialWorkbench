using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Model.Table;
using CompMs.Graphics.Base;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Imms
{
    internal interface IImmsPeakSpotTableModel : IPeakSpotTableModelBase
    {
        double MassMin { get; }
        double MassMax { get; }
        double DriftMin { get; }
        double DriftMax { get; }
    }

    internal abstract class ImmsPeakSpotTableModel<T> : PeakSpotTableModelBase<T>, IImmsPeakSpotTableModel where T : class
    {
        private readonly PeakSpotNavigatorModel _peakSpotNavigatorModel;

        public ImmsPeakSpotTableModel(ReadOnlyObservableCollection<T> peakSpots, IReactiveProperty<T> target, PeakSpotNavigatorModel peakSpotNavigatorModel) : base(peakSpots, target) {
            _peakSpotNavigatorModel = peakSpotNavigatorModel ?? throw new ArgumentNullException(nameof(peakSpotNavigatorModel));
        }

        public double MassMin => _peakSpotNavigatorModel.MzLowerValue;
        public double MassMax => _peakSpotNavigatorModel.MzUpperValue;
        public double DriftMin => _peakSpotNavigatorModel.DtLowerValue;
        public double DriftMax => _peakSpotNavigatorModel.DtUpperValue;
    }

    internal sealed class ImmsAlignmentSpotTableModel : ImmsPeakSpotTableModel<AlignmentSpotPropertyModel>
    {
        public ImmsAlignmentSpotTableModel(
            ReadOnlyObservableCollection<AlignmentSpotPropertyModel> spots,
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
        public ImmsAnalysisPeakTableModel(ReadOnlyObservableCollection<ChromatogramPeakFeatureModel> peaks, IReactiveProperty<ChromatogramPeakFeatureModel> target, PeakSpotNavigatorModel peakSpotNavigatorModel)
            : base(peaks, target, peakSpotNavigatorModel) {
        }
    }
}

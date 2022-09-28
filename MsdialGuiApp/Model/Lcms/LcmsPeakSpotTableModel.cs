using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Table;
using CompMs.Graphics.Base;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Lcms
{
    interface ILcmsPeakSpotTableModel : IPeakSpotTableModelBase
    {
        double MassMin { get; }
        double MassMax { get; }
        double RtMin { get; }
        double RtMax { get; }
    }

    abstract class LcmsPeakSpotTableModel<T> : PeakSpotTableModelBase<T>, ILcmsPeakSpotTableModel where T : class
    {
        public LcmsPeakSpotTableModel(
            ObservableCollection<T> peakSpots,
            IReactiveProperty<T> target,
            double massMin,
            double massMax,
            double rtMin,
            double rtMax)
            : base(peakSpots, target) {

            MassMin = massMin;
            MassMax = massMax;
            RtMin = rtMin;
            RtMax = rtMax;
        }

        public double MassMin { get; }

        public double MassMax { get; }

        public double RtMin { get; }

        public double RtMax { get; }
    }

    sealed class LcmsAlignmentSpotTableModel : LcmsPeakSpotTableModel<AlignmentSpotPropertyModel>
    {
        public LcmsAlignmentSpotTableModel(
            ObservableCollection<AlignmentSpotPropertyModel> peakSpots,
            IReactiveProperty<AlignmentSpotPropertyModel> target,
            IObservable<IBrushMapper<BarItem>> classBrush)
            : base(peakSpots, target,
                  peakSpots.DefaultIfEmpty().Min(peakSpot => peakSpot?.MassCenter) ?? 0d, peakSpots.DefaultIfEmpty().Max(peakSpot => peakSpot?.MassCenter) ?? 0d,
                  peakSpots.DefaultIfEmpty().Min(peakSpot => peakSpot?.TimesCenter) ?? 0d, peakSpots.DefaultIfEmpty().Max(peakSpot => peakSpot?.TimesCenter) ?? 0d) {
            ClassBrush = classBrush;
        }

        public IObservable<IBrushMapper<BarItem>> ClassBrush { get; }
    }

    sealed class LcmsAnalysisPeakTableModel : LcmsPeakSpotTableModel<ChromatogramPeakFeatureModel>
    {
        public LcmsAnalysisPeakTableModel(ObservableCollection<ChromatogramPeakFeatureModel> peakSpots, IReactiveProperty<ChromatogramPeakFeatureModel> target)
            : base(peakSpots, target,
                  peakSpots.Select(peakSpot => peakSpot.Mass).DefaultIfEmpty().Min(), peakSpots.Select(peakSpot => peakSpot.Mass).DefaultIfEmpty().Max(),
                  peakSpots.DefaultIfEmpty().Min(peakSpot => peakSpot?.ChromXValue) ?? 0d, peakSpots.DefaultIfEmpty().Max(peakSpot => peakSpot?.ChromXValue) ?? 0d) {
        }
    }
}

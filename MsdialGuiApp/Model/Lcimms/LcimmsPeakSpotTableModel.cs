using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Table;
using CompMs.Graphics.Base;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Lcimms
{
    interface ILcimmsPeakSpotTableModel : IPeakSpotTableModelBase {
        double MassMin { get; }
        double MassMax { get; }
        double RtMin { get; }
        double RtMax { get; }
        double DtMin { get; }
        double DtMax { get; }
    }

    abstract class LcimmsPeakSpotTableModel<T> : PeakSpotTableModelBase<T>, ILcimmsPeakSpotTableModel where T : class
    {
        public LcimmsPeakSpotTableModel(
            ObservableCollection<T> peakSpots,
            IReactiveProperty<T> target,
            double massMin,
            double massMax,
            double rtMin,
            double rtMax,
            double dtMin,
            double dtMax)
            : base(peakSpots, target) {

            MassMin = massMin;
            MassMax = massMax;
            RtMin = rtMin;
            RtMax = rtMax;
            DtMin = dtMin;
            DtMax = dtMax;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }
        public double DtMin { get; }
        public double DtMax { get; }
    }

    sealed class LcimmsAlignmentSpotTableModel : LcimmsPeakSpotTableModel<AlignmentSpotPropertyModel>
    {
        public LcimmsAlignmentSpotTableModel(
            ObservableCollection<AlignmentSpotPropertyModel> peakSpots,
            IReactiveProperty<AlignmentSpotPropertyModel> target,
            IObservable<IBrushMapper<BarItem>> classBrush)
            : base(peakSpots, target,
                  peakSpots.DefaultIfEmpty().Min(peak => peak?.MassCenter) ?? 0d, peakSpots.DefaultIfEmpty().Max(peak => peak?.MassCenter) ?? 0d,
                  peakSpots.DefaultIfEmpty().Min(peak => peak?.RT) ?? 0d, peakSpots.DefaultIfEmpty().Max(peak => peak?.RT) ?? 0d,
                  peakSpots.DefaultIfEmpty().Min(peak => peak?.Drift) ?? 0d, peakSpots.DefaultIfEmpty().Max(peak => peak?.Drift) ?? 0d) {
            ClassBrush = classBrush;
        }

        public IObservable<IBrushMapper<BarItem>> ClassBrush { get; }
    }

    sealed class LcimmsAnalysisPeakTableModel : LcimmsPeakSpotTableModel<ChromatogramPeakFeatureModel>
    {
        public LcimmsAnalysisPeakTableModel(ObservableCollection<ChromatogramPeakFeatureModel> peakSpots, IReactiveProperty<ChromatogramPeakFeatureModel> target)
            : base(peakSpots, target,
                peakSpots.DefaultIfEmpty().Min(peak => peak?.Mass) ?? 0d, peakSpots.DefaultIfEmpty().Max(peak => peak?.Mass) ?? 0d,
                peakSpots.DefaultIfEmpty().Min(peak => peak?.InnerModel.ChromXsTop.RT.Value) ?? 0d, peakSpots.DefaultIfEmpty().Max(peak => peak?.InnerModel.ChromXsTop.RT.Value) ?? 0d,
                peakSpots.DefaultIfEmpty().Min(peak => peak?.InnerModel.ChromXsTop.Drift.Value) ?? 0d, peakSpots.DefaultIfEmpty().Max(peak => peak?.InnerModel.ChromXsTop.Drift.Value) ?? 0d) {
        }
    }
}

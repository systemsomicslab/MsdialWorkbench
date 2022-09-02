using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Table;
using CompMs.Graphics.Base;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;

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
            double massMin,
            double massMax,
            double rtMin,
            double rtMax,
            double dtMin,
            double dtMax,
            IObservable<IBrushMapper<BarItem>> classBrush)
            : base(
                  peakSpots,
                  target,
                  massMin,
                  massMax,
                  rtMin,
                  rtMax,
                  dtMin,
                  dtMax) {
            ClassBrush = classBrush;
        }

        public IObservable<IBrushMapper<BarItem>> ClassBrush { get; }
    }

    sealed class LcimmsAnalysisPeakTableModel : LcimmsPeakSpotTableModel<ChromatogramPeakFeatureModel>
    {
        public LcimmsAnalysisPeakTableModel(
            ObservableCollection<ChromatogramPeakFeatureModel> peakSpots,
            IReactiveProperty<ChromatogramPeakFeatureModel> target,
            double massMin,
            double massMax,
            double rtMin,
            double rtMax,
            double dtMin,
            double dtMax)
            : base(
                  peakSpots,
                  target,
                  massMin,
                  massMax,
                  rtMin,
                  rtMax,
                  dtMin,
                  dtMax) {
        }
    }
}

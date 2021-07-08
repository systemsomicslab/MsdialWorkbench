using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Table;
using Reactive.Bindings;
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
            double massMin,
            double massMax,
            double rtMin,
            double rtMax)
            : base(
                  peakSpots,
                  target,
                  massMin,
                  massMax,
                  rtMin,
                  rtMax) {
        }
    }

    sealed class LcmsAnalysisPeakTableModel : LcmsPeakSpotTableModel<ChromatogramPeakFeatureModel>
    {
        public LcmsAnalysisPeakTableModel(
            ObservableCollection<ChromatogramPeakFeatureModel> peakSpots,
            IReactiveProperty<ChromatogramPeakFeatureModel> target,
            double massMin,
            double massMax,
            double rtMin,
            double rtMax)
            : base(
                  peakSpots,
                  target,
                  massMin,
                  massMax,
                  rtMin,
                  rtMax) {
        }
    }
}

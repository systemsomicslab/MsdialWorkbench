using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Table;
using Reactive.Bindings;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Imms
{
    abstract class ImmsPeakSpotTableModel<T> : PeakSpotTableModelBase<T>
    {
        public ImmsPeakSpotTableModel(
            ObservableCollection<T> peakSpots,
            IReactiveProperty<T> target,
            double massMin, double massMax,
            double driftMin, double driftMax)
            : base(peakSpots, target) {

            MassMin = massMin;
            MassMax = massMax;

            DriftMin = driftMin;
            DriftMax = driftMax;
        }

        public double MassMin { get; }

        public double MassMax { get; }

        public double DriftMin { get; }

        public double DriftMax { get; }
    }

    sealed class ImmsAlignmentSpotTableModel : ImmsPeakSpotTableModel<AlignmentSpotPropertyModel>
    {
        public ImmsAlignmentSpotTableModel(
            ObservableCollection<AlignmentSpotPropertyModel> spots,
            IReactiveProperty<AlignmentSpotPropertyModel> target,
            double massMin, double massMax,
            double driftMin, double driftMax)
            : base(
                  spots, target,
                  massMin, massMax,
                  driftMin, driftMax) {

        }
    }

    sealed class ImmsAnalysisPeakTableModel : ImmsPeakSpotTableModel<ChromatogramPeakFeatureModel>
    {
        public ImmsAnalysisPeakTableModel(
            ObservableCollection<ChromatogramPeakFeatureModel> peaks,
            IReactiveProperty<ChromatogramPeakFeatureModel> target,
            double massMin, double massMax,
            double driftMin, double driftMax)
            : base(
                  peaks, target,
                  massMin, massMax,
                  driftMin, driftMax) {
        }
    }
}

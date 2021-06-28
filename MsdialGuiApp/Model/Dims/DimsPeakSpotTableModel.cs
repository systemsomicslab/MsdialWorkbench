using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Table;
using Reactive.Bindings;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Dims
{
    abstract class DimsPeakSpotTableModel<T> : PeakSpotTableModelBase<T>
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

    sealed class DimsAnalysisPeakTableModel : DimsPeakSpotTableModel<ChromatogramPeakFeatureModel>
    {
        public DimsAnalysisPeakTableModel(
            ObservableCollection<ChromatogramPeakFeatureModel> peaks,
            IReactiveProperty<ChromatogramPeakFeatureModel> target,
            double massMin, double massMax)
            : base(peaks, target, massMin, massMax) {

        }
    }
}

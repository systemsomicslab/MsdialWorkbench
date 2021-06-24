using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Imms
{
    class ImmsAlignmentSpotTableModel : BindableBase
    {
        public ImmsAlignmentSpotTableModel(
            ObservableCollection<AlignmentSpotPropertyModel> spots,
            IReactiveProperty<AlignmentSpotPropertyModel> target,
            double massMin, double massMax,
            double driftMin, double driftMax) {
            Spots = spots ?? throw new ArgumentNullException(nameof(spots));
            Target = target ?? throw new ArgumentNullException(nameof(target));

            MassMin = massMin;
            MassMax = massMax;

            DriftMin = driftMin;
            DriftMax = driftMax;
        }

        public ObservableCollection<AlignmentSpotPropertyModel> Spots { get; }

        public IReactiveProperty<AlignmentSpotPropertyModel> Target { get; }

        public double MassMin { get; }

        public double MassMax { get; }

        public double DriftMin { get; }

        public double DriftMax { get; }
    }
}

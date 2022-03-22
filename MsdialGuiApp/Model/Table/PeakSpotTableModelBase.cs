using CompMs.CommonMVVM;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Table
{
    public interface IPeakSpotTableModelBase
    {
        IReadOnlyList<object> PeakSpots { get; }

        IReactiveProperty Target { get; }
    }

    abstract class PeakSpotTableModelBase<T> : BindableBase, IPeakSpotTableModelBase where T: class
    {
        public PeakSpotTableModelBase(ObservableCollection<T> peakSpots, IReactiveProperty<T> target) {
            if (peakSpots is null) {
                throw new ArgumentNullException(nameof(peakSpots));
            }

            if (target is null) {
                throw new ArgumentNullException(nameof(target));
            }
            PeakSpots = peakSpots;
            Target = target;
        }

        public ObservableCollection<T> PeakSpots { get; }

        public IReactiveProperty<T> Target { get; }

        IReadOnlyList<object> IPeakSpotTableModelBase.PeakSpots => PeakSpots;

        IReactiveProperty IPeakSpotTableModelBase.Target => Target;
    }
}

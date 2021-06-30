using CompMs.CommonMVVM;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Table
{
    abstract class PeakSpotTableModelBase<T> : BindableBase
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
    }
}

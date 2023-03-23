using CompMs.App.Msdial.Model.Search;
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

    abstract class PeakSpotTableModelBase<T> : DisposableModelBase, IPeakSpotTableModelBase where T: class
    {
        private readonly PeakSpotNavigatorModel _peakSpotNavigatorModel;

        public PeakSpotTableModelBase(ReadOnlyObservableCollection<T> peakSpots, IReactiveProperty<T> target, PeakSpotNavigatorModel peakSpotNavigatorModel) {
            if (peakSpots is null) {
                throw new ArgumentNullException(nameof(peakSpots));
            }

            if (target is null) {
                throw new ArgumentNullException(nameof(target));
            }
            PeakSpots = peakSpots;
            Target = target;
            _peakSpotNavigatorModel = peakSpotNavigatorModel;
        }

        public ReadOnlyObservableCollection<T> PeakSpots { get; }

        public IReactiveProperty<T> Target { get; }

        IReadOnlyList<object> IPeakSpotTableModelBase.PeakSpots => PeakSpots;

        IReactiveProperty IPeakSpotTableModelBase.Target => Target;
    }
}

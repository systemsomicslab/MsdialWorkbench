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
        private readonly IReadOnlyList<T> _peakSpots;
        private readonly IReactiveProperty<T> _target;

        public PeakSpotTableModelBase(IReadOnlyList<T> peakSpots, IReactiveProperty<T> target, PeakSpotNavigatorModel peakSpotNavigatorModel) {
            _peakSpots = peakSpots ?? throw new ArgumentNullException(nameof(peakSpots));
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _peakSpotNavigatorModel = peakSpotNavigatorModel ?? throw new ArgumentNullException(nameof(peakSpotNavigatorModel));
        }

        IReadOnlyList<object> IPeakSpotTableModelBase.PeakSpots => _peakSpots;
        IReactiveProperty IPeakSpotTableModelBase.Target => _target;
    }
}

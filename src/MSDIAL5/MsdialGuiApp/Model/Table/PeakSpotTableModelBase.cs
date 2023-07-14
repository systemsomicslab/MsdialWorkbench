using CompMs.App.Msdial.Model.Search;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Table
{
    public interface IPeakSpotTableModelBase
    {
        IReadOnlyList<object> PeakSpots { get; }

        IReactiveProperty Target { get; }
        void MarkAllAsConfirmed();
    }

    internal abstract class PeakSpotTableModelBase<T> : DisposableModelBase, IPeakSpotTableModelBase where T: class
    {
        private readonly PeakSpotNavigatorModel _peakSpotNavigatorModel;
        private readonly IReactiveProperty<T> _target;

        public PeakSpotTableModelBase(IReadOnlyList<T> peakSpots, IReactiveProperty<T> target, PeakSpotNavigatorModel peakSpotNavigatorModel) {
            PeakSpots = peakSpots ?? throw new ArgumentNullException(nameof(peakSpots));
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _peakSpotNavigatorModel = peakSpotNavigatorModel ?? throw new ArgumentNullException(nameof(peakSpotNavigatorModel));
        }

        protected IReadOnlyList<T> PeakSpots { get; }
        IReadOnlyList<object> IPeakSpotTableModelBase.PeakSpots => PeakSpots;
        IReactiveProperty IPeakSpotTableModelBase.Target => _target;
        public abstract void MarkAllAsConfirmed();
    }
}

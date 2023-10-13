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
    }

    internal abstract class PeakSpotTableModelBase<T> : DisposableModelBase, IPeakSpotTableModelBase where T: class
    {
        private readonly IReactiveProperty<T> _target;

        public PeakSpotTableModelBase(IReadOnlyList<T> peakSpots, IReactiveProperty<T> target) {
            PeakSpots = peakSpots ?? throw new ArgumentNullException(nameof(peakSpots));
            _target = target ?? throw new ArgumentNullException(nameof(target));
        }

        protected IReadOnlyList<T> PeakSpots { get; }

        IReadOnlyList<object> IPeakSpotTableModelBase.PeakSpots => PeakSpots;
        IReactiveProperty IPeakSpotTableModelBase.Target => _target;
    }
}

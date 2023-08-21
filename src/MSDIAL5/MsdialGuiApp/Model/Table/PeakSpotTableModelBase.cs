using CompMs.App.Msdial.Model.Search;
using CompMs.Common.DataObj.Property;
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
        IReadOnlyList<AdductIon> AdductIons { get; }
    }

    internal abstract class PeakSpotTableModelBase<T> : DisposableModelBase, IPeakSpotTableModelBase where T: class
    {
        private readonly PeakSpotNavigatorModel _peakSpotNavigatorModel;
        private readonly IReactiveProperty<T> _target;

        public PeakSpotTableModelBase(IReadOnlyList<T> peakSpots, IReactiveProperty<T> target, PeakSpotNavigatorModel peakSpotNavigatorModel, IReadOnlyList<AdductIon> adductIons) {
            PeakSpots = peakSpots ?? throw new ArgumentNullException(nameof(peakSpots));
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _peakSpotNavigatorModel = peakSpotNavigatorModel ?? throw new ArgumentNullException(nameof(peakSpotNavigatorModel));
            AdductIons = adductIons;
        }

        protected IReadOnlyList<T> PeakSpots { get; }
        public IReadOnlyList<AdductIon> AdductIons { get; }

        IReadOnlyList<object> IPeakSpotTableModelBase.PeakSpots => PeakSpots;
        IReactiveProperty IPeakSpotTableModelBase.Target => _target;
    }
}

using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.DataObj.Property;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Table
{
    internal abstract class AnalysisPeakSpotTableModelBase : PeakSpotTableModelBase<ChromatogramPeakFeatureModel>
    {
        private readonly IReactiveProperty<ChromatogramPeakFeatureModel> _target;

        protected AnalysisPeakSpotTableModelBase(IReadOnlyList<ChromatogramPeakFeatureModel> peakSpots, IReactiveProperty<ChromatogramPeakFeatureModel> target, PeakSpotNavigatorModel peakSpotNavigatorModel, IReadOnlyList<AdductIon> adductIons)
            : base(peakSpots, target, peakSpotNavigatorModel, adductIons) {
            _target = target;
        }

        public void SwitchTag(PeakSpotTag tag) {
            if (_target.Value is null) {
                return;
            }
            _target.Value.SwitchPeakSpotTag(tag);
        }

        public void MarkAllAsConfirmed() {
            foreach (var peak in PeakSpots) {
                peak.Confirmed = true;
            }
        }
    }
}

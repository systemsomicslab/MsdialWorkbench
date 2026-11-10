using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Table
{
    internal abstract class AnalysisPeakSpotTableModelBase : PeakSpotTableModelBase<ChromatogramPeakFeatureModel>
    {
        private readonly IReactiveProperty<ChromatogramPeakFeatureModel?> _target;
        private readonly UndoManager _undoManager;

        protected AnalysisPeakSpotTableModelBase(IReadOnlyList<ChromatogramPeakFeatureModel> peakSpots, IReactiveProperty<ChromatogramPeakFeatureModel?> target, PeakSpotNavigatorModel peakSpotNavigatorModel, UndoManager undoManager)
            : base(peakSpots, target) {
            _target = target;
            _undoManager = undoManager;
        }

        public override void SwitchTag(PeakSpotTag tag) {
            if (_target.Value is null) {
                return;
            }
            _target.Value.SwitchPeakSpotTag(tag);
        }

        public override void MarkAllAsConfirmed(IEnumerable<object> peakSpots) {
            IDoCommand command = new MarkAllAsConfirmedCommand<ChromatogramPeakFeatureModel>(peakSpots.OfType<ChromatogramPeakFeatureModel>().ToList(), true);
            command.Do();
            _undoManager.Add(command);
        }

        public override void MarkAllAsUnconfirmed(IEnumerable<object> peakSpots) {
            IDoCommand command = new MarkAllAsConfirmedCommand<ChromatogramPeakFeatureModel>(peakSpots.OfType<ChromatogramPeakFeatureModel>().ToList(), false);
            command.Do();
            _undoManager.Add(command);
        }
    }
}

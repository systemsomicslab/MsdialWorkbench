using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using System.Collections.Generic;

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

        public override void MarkAllAsConfirmed() {
            IDoCommand command = new MarkAllAsCommand(PeakSpots, true);
            command.Do();
            _undoManager.Add(command);
        }

        public override void MarkAllAsUnconfirmed() {
            IDoCommand command = new MarkAllAsCommand(PeakSpots, false);
            command.Do();
            _undoManager.Add(command);
        }

        sealed class MarkAllAsCommand : IDoCommand {
            private readonly IReadOnlyList<ChromatogramPeakFeatureModel> _peaks;
            private readonly List<ChromatogramPeakFeatureModel> _confirmeds;
            private readonly bool _status;

            public MarkAllAsCommand(IReadOnlyList<ChromatogramPeakFeatureModel> peaks, bool status)
            {
                _peaks = peaks;
                _status = status;
                _confirmeds = new List<ChromatogramPeakFeatureModel>();
            }

            void IDoCommand.Do() {
                _confirmeds.Clear();
                foreach (var peak in _peaks) {
                    if (peak.Confirmed != _status) {
                        peak.Confirmed = _status;
                        _confirmeds.Add(peak);
                    }
                }
            }

            void IDoCommand.Undo() {
                foreach (var peak in _confirmeds) {
                    peak.Confirmed = !_status;
                }
            }
        }
    }
}

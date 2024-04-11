using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Model.Table;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Gcms
{
    internal sealed class GcmsAnalysisPeakTableModel : PeakSpotTableModelBase<Ms1BasedSpectrumFeature>
    {
        private readonly IReactiveProperty<Ms1BasedSpectrumFeature?> _target;
        private readonly UndoManager _undoManager;

        public GcmsAnalysisPeakTableModel(IReadOnlyList<Ms1BasedSpectrumFeature> spectrumFeature, IReactiveProperty<Ms1BasedSpectrumFeature?> target, PeakSpotNavigatorModel peakSpotNavigatorModel, UndoManager undoManager)
            : base(spectrumFeature, target) {
            MassMin = spectrumFeature.Select(s => s.QuantifiedChromatogramPeak.PeakFeature.Mass).DefaultIfEmpty().Min();
            MassMax = spectrumFeature.Select(s => s.QuantifiedChromatogramPeak.PeakFeature.Mass).DefaultIfEmpty().Max();
            RtMin = spectrumFeature.Select(s => s.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RT.Value).DefaultIfEmpty().Min();
            RtMax = spectrumFeature.Select(s => s.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RT.Value).DefaultIfEmpty().Max();
            RiMin = spectrumFeature.Select(s => s.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RI.Value).DefaultIfEmpty().Min();
            RiMax = spectrumFeature.Select(s => s.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RI.Value).DefaultIfEmpty().Max();
            _target = target;
            _undoManager = undoManager;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }
        public double RiMin { get; }
        public double RiMax { get; }

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

        public override void SwitchTag(PeakSpotTag tag) {
            if (_target.Value is null) {
                return;
            }
            _target.Value.SwitchPeakSpotTag(tag);
        }

        sealed class MarkAllAsCommand : IDoCommand {
            private readonly IReadOnlyList<Ms1BasedSpectrumFeature> _peaks;
            private readonly List<Ms1BasedSpectrumFeature> _confirmeds;
            private readonly bool _status;

            public MarkAllAsCommand(IReadOnlyList<Ms1BasedSpectrumFeature> peaks, bool status)
            {
                _peaks = peaks;
                _status = status;
                _confirmeds = new List<Ms1BasedSpectrumFeature>();
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

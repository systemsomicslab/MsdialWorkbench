using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Graphics.Base;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Table
{
    internal abstract class AlignmentSpotTableModelBase : PeakSpotTableModelBase<AlignmentSpotPropertyModel>
    {
        private readonly IReactiveProperty<AlignmentSpotPropertyModel?> _target;
        private readonly PeakSpotFiltering<AlignmentSpotPropertyModel>.PeakSpotFilter _peakSpotFilter;
        private readonly AlignmentSpotSpectraLoader _spectraLoader;
        private readonly UndoManager _undoManager;

        public AlignmentSpotTableModelBase(
            IReadOnlyList<AlignmentSpotPropertyModel> spots,
            IReactiveProperty<AlignmentSpotPropertyModel?> target,
            IObservable<IBrushMapper<BarItem>> classBrush,
            FileClassPropertiesModel classProperties,
            IObservable<IBarItemsLoader> barItemsLoader,
            PeakSpotFiltering<AlignmentSpotPropertyModel>.PeakSpotFilter peakSpotFilter,
            AlignmentSpotSpectraLoader spectraLoader,
            UndoManager undoManager)
            : base(spots, target) {
            _target = target;
            ClassBrush = classBrush;
            BarItemsLoader = barItemsLoader;
            _peakSpotFilter = peakSpotFilter;
            _spectraLoader = spectraLoader;
            _undoManager = undoManager;
            FileClassProperties = classProperties;
        }

        public IObservable<IBrushMapper<BarItem>> ClassBrush { get; }
        public IObservable<IBarItemsLoader> BarItemsLoader { get; }
        public FileClassPropertiesModel FileClassProperties { get; }

        public async Task ExportMatchedSpectraAsync(string directory) {
            var peaks = _peakSpotFilter.Filter(PeakSpots);
            peaks = _peakSpotFilter.FilterAnnotatedPeaks(peaks);
            foreach (var peak in peaks) {
                var spectra = await _spectraLoader.GetMatchedSpectraMatrixsAsync(peak, peak.ScanMatchResult).ConfigureAwait(false);
                using var stream = File.Open(Path.Combine(directory, $"AlignmentID{peak.MasterAlignmentID:D6}.txt"), FileMode.Create, FileAccess.Write);
                spectra?.Export(stream);
            }
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

        public override void SwitchTag(PeakSpotTag tag) {
            if (_target.Value is null) {
                return;
            }
            _target.Value.SwitchPeakSpotTag(tag);
        }

        sealed class MarkAllAsCommand : IDoCommand {
            private readonly IReadOnlyList<AlignmentSpotPropertyModel> _peaks;
            private readonly List<AlignmentSpotPropertyModel> _confirmeds;
            private readonly bool _status;

            public MarkAllAsCommand(IReadOnlyList<AlignmentSpotPropertyModel> peaks, bool status)
            {
                _peaks = peaks;
                _status = status;
                _confirmeds = new List<AlignmentSpotPropertyModel>();
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

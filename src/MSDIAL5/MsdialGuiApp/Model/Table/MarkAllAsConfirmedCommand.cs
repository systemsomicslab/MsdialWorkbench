using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Service;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Table
{
    /// <summary>
    /// "Mark all as confirmed" and "Mark all as unconfirmed" of the peak tables: a person's toggle of Confirmed on every
    /// peak or spot of the table that is not already in that state.
    /// </summary>
    /// <remarks>
    /// The toggle is the person's path (addendum B1): turning Confirmed off where a tool such as the lipid RT check set it
    /// also suppresses it, and turning it on makes it the person's. Undo therefore does not toggle back; it puts each
    /// changed tag back exactly as it was, with its source and its suppression, so the check's own tags stay the check's.
    /// </remarks>
    internal sealed class MarkAllAsConfirmedCommand<T> : IDoCommand where T : IPeakSpotTagEditable
    {
        private readonly IReadOnlyList<T> _peaks;
        private readonly bool _status;
        private readonly List<(T Peak, PeakSpotTagState Before)> _changed;

        public MarkAllAsConfirmedCommand(IReadOnlyList<T> peaks, bool status) {
            _peaks = peaks ?? throw new ArgumentNullException(nameof(peaks));
            _status = status;
            _changed = new List<(T, PeakSpotTagState)>();
        }

        public void Do() {
            _changed.Clear();
            foreach (var peak in _peaks) {
                if (peak.Confirmed != _status) {
                    var before = peak.CapturePeakSpotTag(PeakSpotTag.CONFIRMED);
                    peak.Confirmed = _status;
                    _changed.Add((peak, before));
                }
            }
        }

        public void Undo() {
            foreach (var (peak, before) in _changed) {
                peak.RestorePeakSpotTag(before);
            }
        }
    }
}

using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Service;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Table
{
    /// <summary>
    /// "Mark all as confirmed" and "Mark all as unconfirmed" of the peak tables: a person's decision on Confirmed for every
    /// peak or spot the table shows. While the table is filtered, only the rows left by the filters are marked; the rows
    /// the filters hide keep their tags.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The decision is the person's (addendum B1). "Mark all as confirmed" leaves every row with the person's Confirmed:
    /// it turns Confirmed on where it is off, and takes it over where a tool such as the lipid RT check had set it, so a
    /// later run of that tool, which withdraws only its own tags, does not take back what the person confirmed. "Mark all
    /// as unconfirmed" turns Confirmed off wherever it is on; where a tool had set it, that also suppresses it.
    /// </para>
    /// <para>
    /// Undo does not toggle back; it puts each changed tag back exactly as it was, with its source and its suppression,
    /// so the check's own tags are the check's again.
    /// </para>
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
                var before = peak.CapturePeakSpotTag(PeakSpotTag.CONFIRMED);
                if (_status) {
                    if (before.IsSelected && before.Source is null && !before.IsSuppressed) {
                        continue; // already the person's
                    }
                    peak.SelectPeakSpotTagByPerson(PeakSpotTag.CONFIRMED);
                }
                else {
                    if (!before.IsSelected) {
                        continue;
                    }
                    peak.Confirmed = false;
                }
                _changed.Add((peak, before));
            }
        }

        public void Undo() {
            foreach (var (peak, before) in _changed) {
                peak.RestorePeakSpotTag(before);
            }
        }
    }
}

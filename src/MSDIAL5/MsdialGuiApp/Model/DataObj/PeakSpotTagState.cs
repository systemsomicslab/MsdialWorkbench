using CompMs.MsdialCore.DataObj;
using System;

namespace CompMs.App.Msdial.Model.DataObj
{
    /// <summary>
    /// One tag of one peak or spot as it was at a moment: whether it was selected, who set it, and whether a person had
    /// suppressed it. An undo puts the tag back from this exactly, its provenance included (addendum B1).
    /// </summary>
    /// <remarks>
    /// Undoing through a person's toggle would not: turning a tool's Confirmed back on that way makes it the person's, and
    /// turning a tag on lifts its suppression. "Mark all as unconfirmed" followed by Undo would then hand every Confirmed
    /// the lipid RT check had set to the person, out of the check's reach, and "Mark all as confirmed" followed by Undo
    /// would forget what a person had removed before.
    /// </remarks>
    public sealed class PeakSpotTagState
    {
        private PeakSpotTagState(PeakSpotTag tag, bool isSelected, string? source, bool isSuppressed, string? suppressionSource) {
            Tag = tag;
            IsSelected = isSelected;
            Source = source;
            IsSuppressed = isSuppressed;
            SuppressionSource = suppressionSource;
        }

        public PeakSpotTag Tag { get; }
        public bool IsSelected { get; }

        /// <summary>Who set the tag; null when a person did or nobody recorded who did.</summary>
        public string? Source { get; }
        public bool IsSuppressed { get; }
        public string? SuppressionSource { get; }

        public static PeakSpotTagState Capture(PeakSpotTagCollection tags, PeakSpotTag tag) {
            if (tags is null) {
                throw new ArgumentNullException(nameof(tags));
            }
            if (tag is null) {
                throw new ArgumentNullException(nameof(tag));
            }
            return new PeakSpotTagState(tag, tags.IsSelected(tag), tags.GetSource(tag), tags.IsSuppressed(tag), tags.GetSuppressionSource(tag));
        }

        /// <summary>
        /// Puts the tag back as it was captured. A part that is already as it was is left alone, so the order of the
        /// selected tags (and with it the "Tag:" text of the exports) changes only where the tag itself changes.
        /// </summary>
        public void RestoreTo(PeakSpotTagCollection tags) {
            if (tags is null) {
                throw new ArgumentNullException(nameof(tags));
            }
            if (tags.IsSelected(Tag) != IsSelected || tags.GetSource(Tag) != Source) {
                tags.Deselect(Tag);
                if (IsSelected) {
                    if (Source is null) {
                        tags.Select(Tag);
                    }
                    else {
                        tags.Select(Tag, Source);
                    }
                }
            }
            if (tags.IsSuppressed(Tag) != IsSuppressed || tags.GetSuppressionSource(Tag) != SuppressionSource) {
                tags.Unsuppress(Tag);
                if (IsSuppressed) {
                    // A null source records a suppression nobody signed, which is what it was.
                    tags.Suppress(Tag, SuppressionSource!);
                }
            }
        }
    }
}

using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.Model.DataObj
{
    /// <summary>
    /// A peak or spot whose tags a person edits in the GUI. Setting a tag property is a person's toggle (addendum B1);
    /// capturing and restoring a tag is how an undo puts it back exactly, provenance included.
    /// </summary>
    public interface IPeakSpotTagEditable
    {
        bool Confirmed { get; set; }

        /// <summary>
        /// A person turns the tag on, or takes it over where a tool set it: afterwards it is selected as the person's, so
        /// the tool will not withdraw it, and not suppressed. Setting a tag property does nothing for a tag that is on.
        /// </summary>
        void SelectPeakSpotTagByPerson(PeakSpotTag tag);

        PeakSpotTagState CapturePeakSpotTag(PeakSpotTag tag);

        void RestorePeakSpotTag(PeakSpotTagState state);
    }

    /// <summary>The names of the tag properties every tag-editable model exposes, for its change notifications.</summary>
    internal static class PeakSpotTagPropertyNames
    {
        public static string Of(PeakSpotTag tag) {
            if (tag == PeakSpotTag.CONFIRMED) {
                return nameof(IPeakSpotTagEditable.Confirmed);
            }
            if (tag == PeakSpotTag.LOW_QUALITY_SPECTRUM) {
                return nameof(AlignmentSpotPropertyModel.LowQualitySpectrum);
            }
            if (tag == PeakSpotTag.MISANNOTATION) {
                return nameof(AlignmentSpotPropertyModel.Misannotation);
            }
            if (tag == PeakSpotTag.COELUTION) {
                return nameof(AlignmentSpotPropertyModel.Coelution);
            }
            if (tag == PeakSpotTag.OVERANNOTATION) {
                return nameof(AlignmentSpotPropertyModel.Overannotation);
            }
            // An unknown tag: every property may have changed.
            return string.Empty;
        }
    }
}

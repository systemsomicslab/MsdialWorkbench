using CompMs.MsdialCore.Algorithm.Annotation.LipidRt;
using System;

namespace CompMs.App.Msdial.Model.DataObj
{
    /// <summary>
    /// A peak or spot the lipid RT check gives a verdict to (LC-MS peaks and alignment spots).
    /// </summary>
    /// <remarks>
    /// The verdict, and a Confirmed tag the check set, are about the annotation the check saw. When a person changes the
    /// annotation (compound search, MS-FINDER, "Set unknown"), they no longer describe it, and a manual annotation is
    /// neither tagged nor used as evidence by the check (addendum A6). The change therefore withdraws both: the check's
    /// own Confirmed (a person's or another tool's Confirmed stays) and the verdict. An undo of the change puts both back.
    /// </remarks>
    public interface ILipidRtCheckedObject
    {
        /// <summary>The Confirmed tag and the verdict as they are now.</summary>
        LipidRtCheckState CaptureLipidRtCheck();

        /// <summary>Removes the check's own Confirmed tag and the verdict, and tells the views.</summary>
        void WithdrawLipidRtCheck();

        /// <summary>Puts the Confirmed tag and the verdict back as captured, and tells the views.</summary>
        void RestoreLipidRtCheck(LipidRtCheckState state);
    }

    /// <summary>What the lipid RT check left on one peak or spot at a moment: its Confirmed tag, exactly, and its verdict.</summary>
    public sealed class LipidRtCheckState
    {
        public LipidRtCheckState(PeakSpotTagState confirmed, LipidRtVerdict? verdict) {
            Confirmed = confirmed ?? throw new ArgumentNullException(nameof(confirmed));
            Verdict = verdict;
        }

        /// <summary>The Confirmed tag with its source and suppression.</summary>
        public PeakSpotTagState Confirmed { get; }

        public LipidRtVerdict? Verdict { get; }
    }
}

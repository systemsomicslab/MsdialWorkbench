using MessagePack;
using System;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt
{
    /// <summary>
    /// The persisted record of the lipid retention-time order check for one peak or alignment spot, kept for the GUI's
    /// evidence sentence and the review list (SPEC section 9 with addendum B2).
    /// </summary>
    /// <remarks>
    /// The record does not own the tag. Whether the Confirmed tag was set by this check or by a person is recorded on
    /// the tag itself, in the tag files (addendum B1), so this record carries no tag provenance.
    ///
    /// Every member's "not recorded" state is its CLR default, because MessagePack assigns default(T) to a key absent
    /// from a shorter array: an older record must read as "not evaluated", never as a claim.
    /// </remarks>
    [MessagePackObject]
    public sealed class LipidRtVerdict
    {
        [Key(0)]
        public LipidRtOutcome Outcome { get; set; }

        [Key(1)]
        public LipidRtReason Reason { get; set; }

        /// <summary>Null when the object was not evaluated.</summary>
        [Key(2)]
        public double? Score { get; set; }

        [Key(3)]
        public string RuleVersion { get; set; }

        [Key(4)]
        public string ParameterHash { get; set; }

        [Key(5)]
        public bool CorroboratedByAdduct { get; set; }

        /// <summary>The Ids of the other-polarity twins that corroborate this object.</summary>
        [Key(6)]
        public int[] CorroboratedByPolarity { get; set; } = Array.Empty<int>();

        /// <summary>The evidence sentence.</summary>
        [Key(7)]
        public string Evidence { get; set; }

        [IgnoreMember]
        public bool IsConfirmed => Outcome == LipidRtOutcome.Confirmed;
    }
}

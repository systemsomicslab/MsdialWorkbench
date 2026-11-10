using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt
{
    /// <summary>What applying the check's verdicts to one object's Confirmed tag did.</summary>
    public enum LipidRtTagAction
    {
        /// <summary>The outcome is not Confirmed; the tag was not touched.</summary>
        None = 0,
        /// <summary>The check selected Confirmed with its own source.</summary>
        Set = 1,
        /// <summary>Confirmed was already selected by a person or another tool; it stays theirs.</summary>
        AlreadySelected = 2,
        /// <summary>A person's Misannotation is selected; the check does not contradict it.</summary>
        Misannotation = 3,
        /// <summary>A person removed the check's Confirmed earlier; the check does not set it again.</summary>
        Suppressed = 4,
    }

    /// <summary>What applying one unit's verdicts did to its tags.</summary>
    public sealed class LipidRtTagSummary
    {
        /// <summary>Confirmed tags this check had set before and removed before re-applying (its own only).</summary>
        public int Withdrawn { get; internal set; }

        /// <summary>Confirmed tags the check set.</summary>
        public int Set { get; internal set; }

        /// <summary>Confirmed outcomes where Confirmed was already selected by a person or another tool.</summary>
        public int AlreadySelected { get; internal set; }

        /// <summary>Confirmed outcomes left untagged because a person's Misannotation is selected.</summary>
        public int Misannotation { get; internal set; }

        /// <summary>Confirmed outcomes left untagged because a person removed the check's tag before.</summary>
        public int Suppressed { get; internal set; }

        /// <summary>Objects that received a verdict.</summary>
        public int Verdicts { get; internal set; }

        internal void Count(LipidRtTagAction action) {
            switch (action) {
                case LipidRtTagAction.Set: Set++; break;
                case LipidRtTagAction.AlreadySelected: AlreadySelected++; break;
                case LipidRtTagAction.Misannotation: Misannotation++; break;
                case LipidRtTagAction.Suppressed: Suppressed++; break;
            }
        }
    }

    /// <summary>
    /// The tag protocol of the lipid RT check (SPEC section 9 as re-expressed in addendum B1): the one routine every
    /// caller uses to put a unit's verdicts on its objects - hook A on the peaks of an analysis file, hook B on the spots
    /// of an alignment, and a re-run on a loaded result.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The check only ever SELECTS Confirmed (decision D1). It keeps its own record of what it set in the tag's source
    /// ("MsdialLipidRtCheck/&lt;rule version&gt;"), so before re-applying it removes only the Confirmed tags whose source
    /// is its own, of any rule version, and never a tag a person or another tool set. It then selects Confirmed only
    /// where the outcome is Confirmed, Confirmed is not already selected (a person's stays a person's), Misannotation is
    /// not selected (it never contradicts a person) and Confirmed is not suppressed (a person removed the check's tag
    /// before). It never writes Misannotation: Inconsistent stays in the verdict and the review list.
    /// </para>
    /// <para>
    /// The verdict record is replaced on every object of the unit, and cleared where the check had nothing to say, so
    /// that no verdict of an earlier run outlives the annotation it was about.
    /// </para>
    /// </remarks>
    public static class LipidRtTagProtocol
    {
        /// <summary>True when the source is this check's, of any rule version.</summary>
        public static bool IsOwnSource(string source) {
            return source != null && source.StartsWith(LipidRtCheckParameter.TagSourcePrefix, StringComparison.Ordinal);
        }

        /// <summary>Removes the Confirmed tag only if this check set it. Returns true when it did.</summary>
        public static bool Withdraw(PeakSpotTagCollection tags) {
            if (tags is null) {
                throw new ArgumentNullException(nameof(tags));
            }
            var confirmed = PeakSpotTag.CONFIRMED;
            if (tags.IsSelected(confirmed) && IsOwnSource(tags.GetSource(confirmed))) {
                tags.Deselect(confirmed);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Applies one Confirmed outcome to one object's tags with the given source, unless a person's tag or decision is
        /// in the way. Call <see cref="Withdraw"/> first.
        /// </summary>
        public static LipidRtTagAction Confirm(PeakSpotTagCollection tags, string source) {
            if (tags is null) {
                throw new ArgumentNullException(nameof(tags));
            }
            var confirmed = PeakSpotTag.CONFIRMED;
            if (tags.IsSelected(confirmed)) {
                return LipidRtTagAction.AlreadySelected;
            }
            if (tags.IsSelected(PeakSpotTag.MISANNOTATION)) {
                return LipidRtTagAction.Misannotation;
            }
            if (tags.IsSuppressed(confirmed)) {
                return LipidRtTagAction.Suppressed;
            }
            tags.Select(confirmed, source);
            return LipidRtTagAction.Set;
        }

        /// <summary>
        /// The whole protocol for one unit: withdraw the check's own Confirmed tags, record each object's verdict (null
        /// where the check has none), and select Confirmed where the rules above allow it.
        /// </summary>
        /// <param name="result">The unit's result; null withdraws the check's tags and clears every verdict, which is
        /// what a unit gets when the check is switched off or could not run.</param>
        /// <param name="parameterHash">The hash of the parameter that produced <paramref name="result"/>.</param>
        public static LipidRtTagSummary Apply<T>(IEnumerable<T> objects, Func<T, int> id, Func<T, PeakSpotTagCollection> tags,
            Action<T, LipidRtVerdict> setVerdict, LipidRtRunResult result, string parameterHash) {
            if (objects is null) throw new ArgumentNullException(nameof(objects));
            if (id is null) throw new ArgumentNullException(nameof(id));
            if (tags is null) throw new ArgumentNullException(nameof(tags));
            if (setVerdict is null) throw new ArgumentNullException(nameof(setVerdict));

            var summary = new LipidRtTagSummary();
            var source = LipidRtCheckParameter.TagSource;
            foreach (var o in objects) {
                var collection = tags(o);
                if (Withdraw(collection)) {
                    summary.Withdrawn++;
                }
                if (result != null && result.TryGet(id(o), out var r)) {
                    setVerdict(o, r.ToVerdict(parameterHash));
                    summary.Verdicts++;
                    if (r.Outcome == LipidRtOutcome.Confirmed) {
                        summary.Count(Confirm(collection, source));
                    }
                }
                else {
                    setVerdict(o, null);
                }
            }
            return summary;
        }

        /// <summary><see cref="Apply"/> on the peaks of one analysis file (hook A), keyed by MasterPeakID.</summary>
        public static LipidRtTagSummary ApplyToPeaks(IEnumerable<ChromatogramPeakFeature> peaks, LipidRtRunResult result, string parameterHash) {
            return Apply(peaks, p => p.MasterPeakID, p => p.TagCollection, (p, v) => p.LipidRtVerdict = v, result, parameterHash);
        }

        /// <summary><see cref="Apply"/> on the spots of one alignment (hook B), keyed by MasterAlignmentID.</summary>
        public static LipidRtTagSummary ApplyToSpots(IEnumerable<AlignmentSpotProperty> spots, LipidRtRunResult result, string parameterHash) {
            return Apply(spots, s => s.MasterAlignmentID, s => s.TagCollection, (s, v) => s.LipidRtVerdict = v, result, parameterHash);
        }
    }
}

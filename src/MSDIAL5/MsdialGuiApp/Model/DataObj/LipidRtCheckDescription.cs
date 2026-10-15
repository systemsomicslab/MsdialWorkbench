using CompMs.MsdialCore.Algorithm.Annotation.LipidRt;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace CompMs.App.Msdial.Model.DataObj
{
    /// <summary>
    /// What a person reads in the GUI about the lipid retention-time order check on one peak or spot: the verdict, and who
    /// set or removed its Confirmed tag (addendum B1 and B2).
    /// </summary>
    internal static class LipidRtCheckDescription
    {
        /// <summary>One line for the peak information panel: the evidence sentence, or the outcome and reason.</summary>
        /// <returns>Null when the check has no verdict on the object.</returns>
        public static string? Line(LipidRtVerdict? verdict) {
            if (verdict is null) {
                return null;
            }
            return string.IsNullOrEmpty(verdict.Evidence) ? $"{verdict.Outcome} ({verdict.Reason})" : verdict.Evidence;
        }

        /// <summary>
        /// The text behind a Confirmed tag: who set it (or that a person removed a tool's), and the check's verdict with the
        /// rule version and parameter hash that produced it.
        /// </summary>
        /// <returns>Null when there is nothing to add to the tag itself: no verdict, and Confirmed neither set by a tool
        /// nor suppressed.</returns>
        public static string? ConfirmedDetail(PeakSpotTagCollection tags, LipidRtVerdict? verdict) {
            if (tags is null) {
                throw new ArgumentNullException(nameof(tags));
            }
            var lines = new List<string>();
            var confirmed = PeakSpotTag.CONFIRMED;
            if (tags.IsSelected(confirmed)) {
                var source = tags.GetSource(confirmed);
                if (source is null) {
                    if (verdict != null) {
                        lines.Add("Confirmed: set by a person (no tool recorded as its source).");
                    }
                }
                else if (LipidRtTagProtocol.IsOwnSource(source)) {
                    lines.Add($"Confirmed: set by the lipid RT check ({source}).");
                }
                else {
                    lines.Add($"Confirmed: set by {source}.");
                }
            }
            else if (tags.IsSuppressed(confirmed)) {
                lines.Add("Confirmed: removed by a person after a tool had set it; no tool sets it again.");
            }
            if (verdict != null) {
                var score = verdict.Score.HasValue ? ", score " + verdict.Score.Value.ToString("0.###", CultureInfo.InvariantCulture) : string.Empty;
                lines.Add($"Lipid RT check: {verdict.Outcome} ({verdict.Reason}){score}.");
                if (!string.IsNullOrEmpty(verdict.Evidence)) {
                    lines.Add(verdict.Evidence);
                }
                if (!string.IsNullOrEmpty(verdict.RuleVersion) || !string.IsNullOrEmpty(verdict.ParameterHash)) {
                    lines.Add($"Rules {verdict.RuleVersion}, parameter {verdict.ParameterHash}.");
                }
            }
            return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);
        }
    }
}

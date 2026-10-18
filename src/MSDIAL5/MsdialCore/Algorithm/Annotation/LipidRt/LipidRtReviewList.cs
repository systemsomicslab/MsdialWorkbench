using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt
{
    /// <summary>The groups of the review list, in the order a person reads them.</summary>
    public enum LipidRtReviewGroup
    {
        InconsistentOrderStrong = 0,
        InconsistentOrderWeak = 1,
        InconsistentPlausibility = 2,
        Artefact = 3,
        PolarityConflict = 4,
    }

    /// <summary>One row of the review list.</summary>
    public sealed class LipidRtReviewItem
    {
        internal LipidRtReviewItem(LipidRtReviewGroup group, LipidRtObjectResult result) {
            Group = group;
            Result = result;
        }

        public LipidRtReviewGroup Group { get; }
        public LipidRtObjectResult Result { get; }
    }

    /// <summary>
    /// The review list (SPEC section 10): the objects a person should look at, which are not tags. Inconsistent by RT
    /// order (Strong when the deficit is at least 2 and the residual is at least one double-bond step or unknown,
    /// otherwise Weak), Inconsistent by the hard plausibility rule, NotEvaluable with an MS1 artefact explanation, and
    /// Confirmed with a conflicting other-polarity twin. The artefact group holds every NotEvaluable object with an
    /// artefact explanation of its own, whatever rule decided it.
    /// </summary>
    /// <remarks>Builds rows and formats them; where they are written is the caller's business.</remarks>
    public static class LipidRtReviewList
    {
        public static IReadOnlyList<LipidRtReviewItem> Build(LipidRtRunResult result) {
            if (result is null) {
                throw new ArgumentNullException(nameof(result));
            }
            var items = new List<LipidRtReviewItem>();
            foreach (var r in result.Results) {
                var group = GroupOf(r);
                if (group.HasValue) {
                    items.Add(new LipidRtReviewItem(group.Value, r));
                }
            }
            return items
                .OrderBy(i => i.Group)
                .ThenByDescending(i => i.Group <= LipidRtReviewGroup.InconsistentOrderWeak ? i.Result.Evidence.Deficit : 0d)
                .ThenBy(i => i.Result.Candidate.Rt)
                .ThenBy(i => i.Result.Id)
                .ToList();
        }

        private static LipidRtReviewGroup? GroupOf(LipidRtObjectResult r) {
            switch (r.Reason) {
                case LipidRtReason.InconsistentOrder:
                    var e = r.Evidence;
                    var strong = e.Deficit >= 2d && (!e.ResidualDoubleBondSteps.HasValue || Math.Abs(e.ResidualDoubleBondSteps.Value) >= 1d);
                    return strong ? LipidRtReviewGroup.InconsistentOrderStrong : LipidRtReviewGroup.InconsistentOrderWeak;
                case LipidRtReason.InconsistentPlausibility:
                    return LipidRtReviewGroup.InconsistentPlausibility;
                default:
                    if (r.Outcome == LipidRtOutcome.NotEvaluable && (r.Reason == LipidRtReason.Artefact || r.Evidence.Artefacts.Count > 0)) {
                        return LipidRtReviewGroup.Artefact;
                    }
                    if (r.PolarityConflict) {
                        return LipidRtReviewGroup.PolarityConflict;
                    }
                    return null;
            }
        }

        public static readonly string[] Columns = {
            "ID", "Name", "Adduct", "RT (min)", "Group", "Outcome", "Reason", "Score", "Evidence", "Neighbours",
            "Predicted RT (min)", "Residual (DB steps)", "Artefact partner", "Twins",
        };

        /// <summary>Writes the rows as tab-separated text with a header line.</summary>
        public static void Write(TextWriter writer, IEnumerable<LipidRtReviewItem> items) {
            writer.WriteLine(string.Join("\t", Columns));
            foreach (var item in items) {
                var r = item.Result;
                var c = r.Candidate;
                var e = r.Evidence;
                var artefact = e.Artefacts.FirstOrDefault() ?? e.FeatureArtefact;
                var fields = new[] {
                    r.Id.ToString(CultureInfo.InvariantCulture),
                    string.IsNullOrEmpty(c.Molecular) ? c.Name : c.Name + "|" + c.Molecular,
                    c.Adduct,
                    c.Rt.ToString("0.000", CultureInfo.InvariantCulture),
                    item.Group.ToString(),
                    r.Outcome.ToString(),
                    r.Reason.ToString(),
                    r.Score.HasValue ? r.Score.Value.ToString("0.000", CultureInfo.InvariantCulture) : string.Empty,
                    r.Sentence,
                    string.Join("; ", e.Support.Select(n => string.Format(CultureInfo.InvariantCulture, "{0} {1} {2:+0.000;-0.000;+0.000}", n.Direction, n.Feature, n.Gap))),
                    e.Predicted.HasValue ? e.Predicted.Value.ToString("0.000", CultureInfo.InvariantCulture) : string.Empty,
                    e.ResidualDoubleBondSteps.HasValue ? e.ResidualDoubleBondSteps.Value.ToString("0.00", CultureInfo.InvariantCulture) : string.Empty,
                    artefact is null ? string.Empty : artefact.PartnerId.ToString(CultureInfo.InvariantCulture) + " " + artefact.Text,
                    string.Join("; ", r.Twins.Select(t => string.Format(CultureInfo.InvariantCulture, "{0} {1}@{2:0.000} {3}", t.Id, t.Adduct, t.Rt, t.SingleRunReason))),
                };
                writer.WriteLine(string.Join("\t", fields.Select(Clean)));
            }
        }

        private static string Clean(string value) {
            return (value ?? string.Empty).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
        }
    }
}

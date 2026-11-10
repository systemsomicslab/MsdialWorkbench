using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt
{
    /// <summary>
    /// The sentence a person reads for one outcome, e.g. "Confirmed: TG 52:1 elutes after TG 50:1 (+0.30 min) and
    /// TG 52:2 (+0.25), and before TG 54:1 (-0.30) and TG 52:0 (-0.25); residual +0.00 min (0.00 DB steps)."
    /// </summary>
    /// <remarks>
    /// Free text for the GUI and the review list; nothing parses it. It says what a Confirmed does not certify only in
    /// the documentation, to keep the sentence short.
    /// </remarks>
    public static class LipidRtEvidenceSentence
    {
        public static string Format(LipidRtObjectResult result) {
            if (result is null) {
                return string.Empty;
            }
            var c = result.Candidate;
            var e = result.Evidence;
            var subject = e.Composition?.ToString() ?? c?.Composition?.ToString() ?? c?.Name ?? string.Empty;
            switch (result.Reason) {
                case LipidRtReason.ConfirmedRt:
                    return "Confirmed: " + OrderClause(subject, e) + Residual(e) + Corroboration(result) + ".";
                case LipidRtReason.ConfirmedPromotedByPolarity:
                    return "Confirmed in the other polarity (" + Twins(result) + "); in this run: " + Describe(result.SingleRunReason, e) + ".";
                case LipidRtReason.InconsistentOrder:
                    return string.Format(CultureInfo.InvariantCulture,
                        "Inconsistent: {0} is out of order in its class (deficit {1:0.00}); discordant with {2}{3}.",
                        subject, e.Deficit, Discordant(e), Residual(e));
                case LipidRtReason.InconsistentPlausibility:
                    return string.Format(CultureInfo.InvariantCulture, "Inconsistent: plausibility {0} ({1}).", e.Plausibility.Rule, e.Plausibility.Detail);
                case LipidRtReason.Ineligible:
                    return "Not evaluated: " + (string.IsNullOrEmpty(c?.IneligibleReason) ? "not eligible" : c.IneligibleReason.Replace('_', ' ')) + ".";
                case LipidRtReason.Gate:
                    return "Not evaluable: the run does not show reversed-phase order (" + (e.Gate?.ToString() ?? string.Empty) + ").";
                case LipidRtReason.PolicyClass:
                    return "Not evaluable: a class-level match is not confirmed without corroboration (policy); " + OrderClause(subject, e) + ".";
                case LipidRtReason.Uncorroborated:
                    return "Not evaluable: not corroborated by a second adduct or the other polarity (Strict); " + OrderClause(subject, e) + ".";
                case LipidRtReason.PolarityConflict:
                    return "Not evaluable: the other polarity contradicts (" + Twins(result) + "); " + OrderClause(subject, e) + ".";
                default:
                    return "Not evaluable: " + Describe(result.Reason, e) + ".";
            }
        }

        private static string Describe(LipidRtReason reason, LipidRtEvidence e) {
            switch (reason) {
                case LipidRtReason.Support:
                    return "insufficient adjacent support (" + Directions(e) + ")";
                case LipidRtReason.ClassGate:
                    return string.Format(CultureInfo.InvariantCulture, "the class order is not reversed-phase like (adjacent concordance {0})",
                        e.ClassAdjacentConcordance.HasValue ? e.ClassAdjacentConcordance.Value.ToString("0.00", CultureInfo.InvariantCulture) : "-");
                case LipidRtReason.Ambiguous:
                    return "an order conflict that the evidence does not resolve; discordant with " + Discordant(e);
                case LipidRtReason.Tie:
                    return "co-elutes with the adjacent composition " + e.TiePartner + " (isotope, adduct or in-source artefact?)";
                case LipidRtReason.Artefact:
                    var artefact = e.Artefacts.FirstOrDefault() ?? e.FeatureArtefact;
                    return "MS1 artefact: " + (artefact?.Text ?? "explained by a brighter co-eluting ion");
                case LipidRtReason.Magnitude:
                    return "the RT is closer to a neighbouring composition than the class steps predict" + Residual(e);
                case LipidRtReason.Margin:
                    return string.Format(CultureInfo.InvariantCulture, "wins its order conflicts only narrowly (margin {0:0.00}, {1} discordant)", e.Margin, e.DiscordantCount);
                case LipidRtReason.SmallDeficit:
                    return string.Format(CultureInfo.InvariantCulture, "out of order by a small deficit ({0:0.00}); discordant with {1}", e.Deficit, Discordant(e));
                case LipidRtReason.OxNoFlag:
                    return string.Format(CultureInfo.InvariantCulture, "an order conflict in an oxidised class, not flagged (deficit {0:0.00})", e.Deficit);
                case LipidRtReason.TailNoFlag:
                    return string.Format(CultureInfo.InvariantCulture, "an order conflict in the gradient-end tail, not flagged (deficit {0:0.00})", e.Deficit);
                case LipidRtReason.PolicySecondary:
                    return "secondary ions only (policy)";
                case LipidRtReason.MinorDuplicate:
                    return "a minor duplicate of " + e.MinorDuplicateOf + " (policy)";
                case LipidRtReason.PlausibilitySoft:
                    return string.Format(CultureInfo.InvariantCulture, "plausibility {0} soft ({1})", e.Plausibility.Rule, e.Plausibility.Detail);
                case LipidRtReason.ConfirmedRt:
                    return "Confirmed";
                default:
                    return reason.ToString();
            }
        }

        private static string OrderClause(string subject, LipidRtEvidence e) {
            var lower = e.Support.Where(n => n.IsLowerBound).ToList();
            var upper = e.Support.Where(n => !n.IsLowerBound).ToList();
            if (lower.Count == 0 && upper.Count == 0) {
                return subject + " has no supporting neighbour";
            }
            var builder = new StringBuilder(subject);
            if (lower.Count > 0) {
                builder.Append(" elutes after ").Append(Join(lower));
            }
            if (upper.Count > 0) {
                builder.Append(lower.Count > 0 ? ", and before " : " elutes before ").Append(Join(upper));
            }
            return builder.ToString();
        }

        private static string Join(IReadOnlyList<LipidRtNeighbour> neighbours) {
            var parts = neighbours.Select((n, i) => string.Format(CultureInfo.InvariantCulture, "{0} ({1:+0.00;-0.00;+0.00}{2})",
                Strip(n.Feature), n.Gap, i == 0 ? " min" : string.Empty)).ToList();
            if (parts.Count == 1) {
                return parts[0];
            }
            return string.Join(", ", parts.Take(parts.Count - 1)) + " and " + parts[parts.Count - 1];
        }

        /// <summary>"TG 50:1@12.04" -> "TG 50:1".</summary>
        private static string Strip(string feature) {
            var at = feature.LastIndexOf('@');
            return at > 0 ? feature.Substring(0, at) : feature;
        }

        private static string Residual(LipidRtEvidence e) {
            if (!e.Residual.HasValue || !e.ResidualDoubleBondSteps.HasValue) {
                return string.Empty;
            }
            return string.Format(CultureInfo.InvariantCulture, "; residual {0:+0.00;-0.00;+0.00} min ({1:+0.00;-0.00;+0.00} DB steps)", e.Residual.Value, e.ResidualDoubleBondSteps.Value);
        }

        private static string Discordant(LipidRtEvidence e) {
            if (e.Discordant.Count == 0) {
                return "-";
            }
            return string.Join(", ", e.Discordant.Select(d => d.Feature + " [" + d.Status + "]"));
        }

        private static string Directions(LipidRtEvidence e) {
            return string.Format(CultureInfo.InvariantCulture, "directions {0}, {1} axes, {2} ties", string.IsNullOrEmpty(e.Directions) ? "-" : e.Directions, e.Axes, e.Ties);
        }

        private static string Twins(LipidRtObjectResult result) {
            if (result.Twins.Count == 0) {
                return "no twin";
            }
            return string.Join(", ", result.Twins.Select(t => string.Format(CultureInfo.InvariantCulture, "{0} {1}@{2:0.00} {3}", t.Id, t.Adduct, t.Rt, t.SingleRunReason)));
        }

        private static string Corroboration(LipidRtObjectResult result) {
            var parts = new List<string>();
            if (result.CorroboratedByAdduct) {
                parts.Add("second adduct");
            }
            if (result.CorroboratedByPolarity.Count > 0) {
                parts.Add("other polarity");
            }
            if (result.PolarityConflict) {
                parts.Add("conflicting other-polarity twin");
            }
            if (result.Evidence.SoftPlausibilityOverridden) {
                parts.Add("plausibility " + result.Evidence.Plausibility.Rule + " soft overridden by RT and spectrum");
            }
            return parts.Count == 0 ? string.Empty : "; " + string.Join("; ", parts);
        }
    }
}

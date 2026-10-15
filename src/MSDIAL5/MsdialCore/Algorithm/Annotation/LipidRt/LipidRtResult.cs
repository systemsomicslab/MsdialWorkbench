using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt
{
    /// <summary>An adjacent neighbour that supports a feature's position.</summary>
    public sealed class LipidRtNeighbour
    {
        public LipidRtNeighbour(string direction, string feature, double rt, double gap) {
            Direction = direction;
            Feature = feature;
            Rt = rt;
            Gap = gap;
        }

        /// <summary>
        /// "C&lt;", "D&gt;" or "O&gt;": a lower bound (a smaller, more unsaturated or more oxidised neighbour elutes
        /// earlier); "C&gt;", "D&lt;" or "O&lt;": an upper bound.
        /// </summary>
        public string Direction { get; }

        /// <summary>The neighbour feature, e.g. "TG 50:1@12.04".</summary>
        public string Feature { get; }

        public double Rt { get; }

        /// <summary>The feature's RT minus the neighbour's (min).</summary>
        public double Gap { get; }

        public bool IsLowerBound => Direction == "C<" || Direction == "D>" || Direction == "O>";
    }

    /// <summary>A feature whose order contradicts this one.</summary>
    public sealed class LipidRtDiscordantPartner
    {
        public LipidRtDiscordantPartner(string feature, double rt, LipidRtFeatureStatus status) {
            Feature = feature;
            Rt = rt;
            Status = status;
        }

        public string Feature { get; }
        public double Rt { get; }
        public LipidRtFeatureStatus Status { get; }
    }

    /// <summary>An MS1 artefact explanation: a brighter co-eluting peak this ion can be a by-product of.</summary>
    public sealed class LipidRtArtefact
    {
        public LipidRtArtefact(string kind, string text, int partnerId) {
            Kind = kind;
            Text = text;
            PartnerId = partnerId;
        }

        /// <summary>"iso1", "iso2", "naH", "naNH4", "kH", "kNa", "nh4H", "acetate", "formate", "chloride", "demethyl", "isf_chain", "isf_head" or "ox".</summary>
        public string Kind { get; }

        public string Text { get; }

        /// <summary>The Id of the brighter co-eluting peak.</summary>
        public int PartnerId { get; }
    }

    /// <summary>An eligible candidate of the other polarity with the same composition at the same (mapped) RT.</summary>
    public sealed class LipidRtTwin
    {
        public LipidRtTwin(int id, string adduct, double rt, LipidRtReason singleRunReason) {
            Id = id;
            Adduct = adduct;
            Rt = rt;
            SingleRunReason = singleRunReason;
        }

        public int Id { get; }
        public string Adduct { get; }
        public double Rt { get; }

        /// <summary>The twin's own single-run reason; corroboration never chains.</summary>
        public LipidRtReason SingleRunReason { get; }
    }

    /// <summary>The run-level reversed-phase gate (SPEC section 7).</summary>
    public sealed class LipidRtGateStatistics
    {
        public bool Passed { get; internal set; }

        /// <summary>True when the parameter said the chromatography is not reversed phase; nothing was measured.</summary>
        public bool ChromatographyIsOther { get; internal set; }

        /// <summary>The median over classes of the median within-class carbon slope (min per carbon).</summary>
        public double MedianCarbonSlope { get; internal set; }

        public double SlopeThreshold { get; internal set; }

        /// <summary>Classes that contributed a slope (at least 5 features and 3 distinct carbon counts).</summary>
        public int ClassCount { get; internal set; }

        /// <summary>The adjacent-carbon concordance on the pairs used, or null when there were none.</summary>
        public double? Concordance { get; internal set; }

        /// <summary>The number of adjacent-carbon pairs used.</summary>
        public int PairCount { get; internal set; }

        /// <summary>"trusted" (chain-level features at or above the class median height) or "all".</summary>
        public string ConcordanceOn { get; internal set; } = string.Empty;

        /// <summary>The concordance over all adjacent-carbon pairs, or null when there were none.</summary>
        public double? ConcordanceAllPairs { get; internal set; }

        public override string ToString() {
            if (ChromatographyIsOther) {
                return "chromatography parameter says not RP";
            }
            return $"median carbon slope {MedianCarbonSlope:0.0000} vs threshold {SlopeThreshold:0.0000} over {ClassCount} classes; "
                + $"adjacent-carbon concordance {(Concordance.HasValue ? Concordance.Value.ToString("0.000") : "-")} over {PairCount} {ConcordanceOn} pairs"
                + $" (all pairs {(ConcordanceAllPairs.HasValue ? ConcordanceAllPairs.Value.ToString("0.000") : "-")})";
        }
    }

    /// <summary>The machine-readable evidence behind one object's outcome (SPEC section 8).</summary>
    public sealed class LipidRtEvidence
    {
        internal LipidRtEvidence() { }

        /// <summary>The feature, e.g. "TG 52:1@12.34"; empty when the object was not evaluated as part of a feature.</summary>
        public string Feature { get; internal set; } = string.Empty;

        public LipidRtComposition Composition { get; internal set; }

        public double FeatureRt { get; internal set; }

        public LipidRtFeatureStatus Status { get; internal set; }

        /// <summary>The essential margin e (evidence units) of a Core feature, the weight of a Free one, 0 otherwise.</summary>
        public double Margin { get; internal set; }

        /// <summary>The deficit d (evidence units) of an Excluded feature, 0 otherwise.</summary>
        public double Deficit { get; internal set; }

        /// <summary>The integer weight of the feature (x100 evidence units).</summary>
        public long Weight { get; internal set; }

        /// <summary>The supporting directions, sorted, e.g. "C&lt;C&gt;D&lt;D&gt;".</summary>
        public string Directions { get; internal set; } = string.Empty;

        public int Axes { get; internal set; }

        public int Ties { get; internal set; }

        /// <summary>Up to the configured number of supporting neighbours, by |gap| descending then direction.</summary>
        public IReadOnlyList<LipidRtNeighbour> Support { get; internal set; } = Array.Empty<LipidRtNeighbour>();

        /// <summary>Up to the configured number of discordant partners, by weight descending then RT.</summary>
        public IReadOnlyList<LipidRtDiscordantPartner> Discordant { get; internal set; } = Array.Empty<LipidRtDiscordantPartner>();

        public int DiscordantCount { get; internal set; }

        /// <summary>The adjacent composition this feature ties with, when the Tie rule decided.</summary>
        public string TiePartner { get; internal set; } = string.Empty;

        /// <summary>The less intense duplicate's dominant twin, when the MinorDuplicate rule decided.</summary>
        public string MinorDuplicateOf { get; internal set; } = string.Empty;

        public double? Predicted { get; internal set; }
        public double? Residual { get; internal set; }
        public double? ResidualDoubleBondSteps { get; internal set; }

        public bool ClassGatePassed { get; internal set; } = true;
        public double? ClassAdjacentConcordance { get; internal set; }

        /// <summary>The members of the feature (object Ids) and the number of distinct adducts.</summary>
        public IReadOnlyList<int> Members { get; internal set; } = Array.Empty<int>();
        public int AdductCount { get; internal set; }

        /// <summary>This object's own MS1 artefact explanations.</summary>
        public IReadOnlyList<LipidRtArtefact> Artefacts { get; internal set; } = Array.Empty<LipidRtArtefact>();

        /// <summary>The artefact explanation of the feature's representative member, when every member has one.</summary>
        public LipidRtArtefact FeatureArtefact { get; internal set; }

        public LipidRtPlausibilityVerdict Plausibility { get; internal set; } = LipidRtPlausibilityVerdict.NotEvaluated;

        /// <summary>True when a soft plausibility rule failed and "RT and spectrum both excellent" kept the Confirmed.</summary>
        public bool SoftPlausibilityOverridden { get; internal set; }

        /// <summary>Set for NotEvaluable(Gate).</summary>
        public LipidRtGateStatistics Gate { get; internal set; }
    }

    /// <summary>The outcome of the check for one object.</summary>
    public sealed class LipidRtObjectResult
    {
        internal LipidRtObjectResult() { }

        public int Id { get; internal set; }

        public LipidRtCandidate Candidate { get; internal set; }

        /// <summary>The outcome after corroboration and the corroboration-dependent policies.</summary>
        public LipidRtOutcome Outcome { get; internal set; }
        public LipidRtReason Reason { get; internal set; }

        /// <summary>The graded score; null when the object was not evaluated (ineligible, or the run gate failed).</summary>
        public double? Score { get; internal set; }

        /// <summary>The outcome of this unit's own evidence, steps 1-10, before any corroboration.</summary>
        public LipidRtOutcome SingleRunOutcome { get; internal set; }
        public LipidRtReason SingleRunReason { get; internal set; }
        public double? SingleRunScore { get; internal set; }

        /// <summary>A Confirmed(Rt) member whose feature holds at least two distinct adducts.</summary>
        public bool CorroboratedByAdduct { get; internal set; }

        /// <summary>The other-polarity twins Confirmed by their own evidence that corroborate or promote this object.</summary>
        public IReadOnlyList<int> CorroboratedByPolarity { get; internal set; } = Array.Empty<int>();

        /// <summary>The other-polarity twins, when cross-polarity corroboration ran.</summary>
        public IReadOnlyList<LipidRtTwin> Twins { get; internal set; } = Array.Empty<LipidRtTwin>();

        /// <summary>Confirmed here while every other-polarity twin is Inconsistent.</summary>
        public bool PolarityConflict { get; internal set; }

        public LipidRtEvidence Evidence { get; internal set; } = new LipidRtEvidence();

        /// <summary>The sentence a person reads, e.g. "Confirmed: TG 52:1 elutes after TG 50:1 (+0.30 min) ...".</summary>
        public string Sentence => LipidRtEvidenceSentence.Format(this);

        internal LipidRtObjectResult CopyForCorroboration() {
            return new LipidRtObjectResult
            {
                Id = Id,
                Candidate = Candidate,
                Outcome = SingleRunOutcome,
                Reason = SingleRunReason,
                Score = SingleRunScore,
                SingleRunOutcome = SingleRunOutcome,
                SingleRunReason = SingleRunReason,
                SingleRunScore = SingleRunScore,
                Evidence = Evidence,
            };
        }

        /// <summary>The persisted record of this result.</summary>
        public LipidRtVerdict ToVerdict(string parameterHash) {
            return new LipidRtVerdict
            {
                Outcome = Outcome,
                Reason = Reason,
                Score = Score,
                RuleVersion = Parameter.LipidRtCheckParameter.RuleVersion,
                ParameterHash = parameterHash ?? string.Empty,
                CorroboratedByAdduct = CorroboratedByAdduct,
                CorroboratedByPolarity = CorroboratedByPolarity.ToArray(),
                Evidence = Sentence,
            };
        }
    }

    /// <summary>The outcome of the check for one unit.</summary>
    public sealed class LipidRtRunResult
    {
        internal LipidRtRunResult(LipidRtRunInput input, IReadOnlyList<LipidRtObjectResult> results, LipidRtGateStatistics gate) {
            Input = input;
            Results = results;
            Gate = gate;
            _byId = new Dictionary<int, LipidRtObjectResult>();
            foreach (var result in results) {
                _byId[result.Id] = result;
            }
        }

        private readonly Dictionary<int, LipidRtObjectResult> _byId;

        public LipidRtRunInput Input { get; }

        /// <summary>One result per candidate, in the candidates' order.</summary>
        public IReadOnlyList<LipidRtObjectResult> Results { get; }

        /// <summary>The run-level gate; null when nothing was eligible.</summary>
        public LipidRtGateStatistics Gate { get; }

        public IonMode Polarity => Input.Polarity;

        public double Span => Input.Span;

        public bool TryGet(int id, out LipidRtObjectResult result) => _byId.TryGetValue(id, out result);

        public LipidRtObjectResult this[int id] => _byId[id];
    }

    /// <summary>The fitted RT line between the two polarities (step 11b).</summary>
    public sealed class LipidRtCrossPolarityLine
    {
        internal LipidRtCrossPolarityLine(double intercept, double slope, double? residualSd, int compositions, double tolerance) {
            Intercept = intercept;
            Slope = slope;
            ResidualSd = residualSd;
            Compositions = compositions;
            Tolerance = tolerance;
        }

        /// <summary>RtNeg = Intercept + Slope x RtPos.</summary>
        public double Intercept { get; }
        public double Slope { get; }

        /// <summary>1.4826 x MAD of the residuals, or null when fewer than 5 compositions were shared.</summary>
        public double? ResidualSd { get; }

        /// <summary>Compositions with a single RT cluster in both units.</summary>
        public int Compositions { get; }

        /// <summary>The twin tolerance (min).</summary>
        public double Tolerance { get; }
    }
}

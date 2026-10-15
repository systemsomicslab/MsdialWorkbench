using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt
{
    /// <summary>
    /// The lipid retention-time order check (specification v1.0 with the addendum of 2026-10-15): a pure, deterministic
    /// evaluation of one unit (an analysis file or an alignment).
    /// </summary>
    /// <remarks>
    /// A Confirmed means: in this run, the species-level composition of the annotation (class, total C, total DB with
    /// P- counted as O- plus one, total O) is consistent with the reversed-phase rules among the run's other MS/MS-backed
    /// annotations of the same class (more carbons elute later; more double bonds and more oxygens elute earlier), it is
    /// bracketed by such neighbours on two axes, and it is not explained as an MS1 artefact of a brighter co-eluting ion.
    /// It does not certify the chain assignment, the choice among isomers of one composition, or the absolute RT against
    /// a library; the library RT is not used.
    ///
    /// The steps mirror the frozen Python reference (lipidrt_final.decide_full, poset.analyze, xpol.corroborate):
    /// eligibility, plausibility, MS1 artefacts, features, the run-level RP gate, the exact maximum consistent subset per
    /// class, adjacent directional support with ties and the class gate, the magnitude prediction, the feature outcome,
    /// propagation to members, and the corroboration step.
    ///
    /// No randomness and no dependence on input order: every ordering is explicit, the max-flow capacities are
    /// integers, medians follow numpy (mean of the two middle values) and weights round half to even.
    /// </remarks>
    public sealed class LipidRtCheck
    {
        private const int Scale = 100;

        private static readonly string[] SecondaryTokens = { "Na", "K]", "K+", "Li", "2-", "2+", "3-", "3+" };

        private static readonly Dictionary<string, HashSet<string>> PrimaryByClass = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal) {
            ["CL"] = new HashSet<string>(StringComparer.Ordinal) { "[M-2H]2-", "[M-H]-" },
            ["DLCL"] = new HashSet<string>(StringComparer.Ordinal) { "[M-H]-", "[M-2H]2-" },
            ["MLCL"] = new HashSet<string>(StringComparer.Ordinal) { "[M-H]-", "[M-2H]2-" },
        };

        private static readonly HashSet<string> OxidisedClasses = new HashSet<string>(StringComparer.Ordinal) {
            "OxTG", "OxPC", "OxPE", "OxPG", "OxPI", "OxPS", "OxFA", "EtherOxPC", "EtherOxPE", "DMEDOxFA",
        };

        private sealed class Offset
        {
            public Offset(string kind, LipidRtArtefactKinds flag, double value, string partnerIon, string spotIon) {
                Kind = kind; Flag = flag; Value = value; PartnerIon = partnerIon; SpotIon = spotIon;
            }
            public string Kind { get; }
            public LipidRtArtefactKinds Flag { get; }
            public double Value { get; }
            /// <summary>The ion an annotated partner must carry for the relation to hold.</summary>
            public string PartnerIon { get; }
            /// <summary>A spot annotated as this ion IS that ion: a duplicate, not an artefact.</summary>
            public string SpotIon { get; }
        }

        // m/z(spot) - m/z(brighter co-eluting partner)
        private static readonly Offset[] PositiveOffsets = {
            new Offset("iso1", LipidRtArtefactKinds.Iso1, 1.00335, null, null),
            new Offset("iso2", LipidRtArtefactKinds.Iso2, 2.00671, null, null),
            new Offset("naH", LipidRtArtefactKinds.NaH, 21.98194, "[M+H]+", "[M+Na]+"),
            new Offset("naNH4", LipidRtArtefactKinds.NaNH4, 4.95539, "[M+NH4]+", "[M+Na]+"),
            new Offset("kH", LipidRtArtefactKinds.KH, 37.95588, "[M+H]+", "[M+K]+"),
            new Offset("kNa", LipidRtArtefactKinds.KNa, 15.97394, "[M+Na]+", "[M+K]+"),
            new Offset("nh4H", LipidRtArtefactKinds.NH4H, 17.02655, "[M+H]+", "[M+NH4]+"),
            new Offset("acn", LipidRtArtefactKinds.Acn, 41.02655, "[M+H]+", "[M+ACN+H]+"),
            new Offset("water", LipidRtArtefactKinds.Water, -18.01056, null, "[M+H-H2O]+"),
        };

        private static readonly Offset[] NegativeOffsets = {
            new Offset("iso1", LipidRtArtefactKinds.Iso1, 1.00335, null, null),
            new Offset("iso2", LipidRtArtefactKinds.Iso2, 2.00671, null, null),
            new Offset("acetate", LipidRtArtefactKinds.Acetate, 60.02113, "[M-H]-", "[M+CH3COO]-"),
            new Offset("formate", LipidRtArtefactKinds.Formate, 46.00548, "[M-H]-", "[M+HCOO]-"),
            new Offset("chloride", LipidRtArtefactKinds.Chloride, 35.97668, "[M-H]-", "[M+Cl]-"),
            new Offset("demethyl", LipidRtArtefactKinds.Demethyl, -74.03678, "[M+CH3COO]-", null),
            new Offset("demethyl", LipidRtArtefactKinds.Demethyl, -60.02113, "[M+HCOO]-", null),
            new Offset("water", LipidRtArtefactKinds.Water, -18.01056, null, "[M-H-H2O]-"),
        };

        /// <summary>Fragment class -> parent classes: the parent is the fragment plus one realistic chain, at the same RT.</summary>
        private static readonly Dictionary<string, string[]> InSourceChainLoss = new Dictionary<string, string[]>(StringComparer.Ordinal) {
            ["LPC"] = new[] { "PC" }, ["LPE"] = new[] { "PE" }, ["LPI"] = new[] { "PI" }, ["LPS"] = new[] { "PS" }, ["LPG"] = new[] { "PG" }, ["LPA"] = new[] { "PA" },
            ["EtherLPC"] = new[] { "EtherPC" }, ["EtherLPE"] = new[] { "EtherPE" }, ["MG"] = new[] { "DG" }, ["DG"] = new[] { "TG" }, ["EtherDG"] = new[] { "EtherTG" },
            ["FA"] = new[] { "PC", "PE", "PI", "PS", "PG", "PA", "TG", "DG", "LPC", "LPE", "EtherPC", "EtherPE" },
        };

        /// <summary>Fragment class -> parent classes with the identical composition, at the same RT.</summary>
        private static readonly Dictionary<string, string[]> InSourceHeadLoss = new Dictionary<string, string[]>(StringComparer.Ordinal) {
            ["Cer_NS"] = new[] { "SM", "HexCer_NS", "Hex2Cer", "CerP" }, ["Cer_NDS"] = new[] { "SM", "HexCer_NDS" }, ["Cer_AS"] = new[] { "HexCer_AS" },
            ["Cer_NP"] = new[] { "HexCer_AP" }, ["DG"] = new[] { "PC", "PE", "PI", "PS", "PG", "PA", "MGDG", "DGDG", "SQDG" },
            ["EtherDG"] = new[] { "EtherPC", "EtherPE" }, ["LPA"] = new[] { "LPC", "LPE", "LPI", "LPS", "LPG" }, ["PA"] = new[] { "PC", "PE", "PI", "PS", "PG" },
            ["MG"] = new[] { "LPC", "LPE", "LPI", "LPS", "LPG" }, ["Sph"] = new[] { "SM", "Cer_NS" }, ["DHSph"] = new[] { "SM", "Cer_NDS" },
            ["DMPE"] = new[] { "PC" }, // NEG: PC [M-CH3]- has the formula of DMPE [M-H]-
        };

        /// <summary>Oxidised class -> its base class.</summary>
        private static readonly Dictionary<string, string> OxidisedBase = new Dictionary<string, string>(StringComparer.Ordinal) {
            ["OxTG"] = "TG", ["OxPC"] = "PC", ["OxPE"] = "PE", ["OxPI"] = "PI", ["OxPG"] = "PG", ["OxPS"] = "PS", ["OxFA"] = "FA",
            ["EtherOxPC"] = "EtherPC", ["EtherOxPE"] = "EtherPE", ["DMEDOxFA"] = "DMEDFA",
        };

        private readonly LipidRtCheckParameter _parameter;
        private readonly ILipidRtPlausibilityProfile _profile;

        /// <summary>A check with the plausibility profile the parameter names (<see cref="LipidRtCheckParameter.PlausibilityProfile"/>).</summary>
        public LipidRtCheck(LipidRtCheckParameter parameter)
            : this(parameter, LipidRtChainRuleProfile.FromParameter((parameter ?? throw new ArgumentNullException(nameof(parameter))).PlausibilityProfile)) {
        }

        /// <summary>A check with an explicit plausibility profile; null means no profile, whatever the parameter says.</summary>
        public LipidRtCheck(LipidRtCheckParameter parameter, ILipidRtPlausibilityProfile profile) {
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _profile = profile;
        }

        public LipidRtCheckParameter Parameter => _parameter;

        public ILipidRtPlausibilityProfile Profile => _profile;

        /// <summary>
        /// Evaluates one unit and applies the corroboration available within it: the second-adduct flag, the class-level
        /// policy and the Strict operating point (step 11a and 11c). This is what a single-polarity Console or GUI run
        /// produces. <see cref="LipidRtCorroboration.CrossPolarity"/> adds the other polarity later.
        /// </summary>
        public LipidRtRunResult Evaluate(LipidRtRunInput input) {
            var single = EvaluateSingleRun(input);
            return LipidRtCorroboration.Apply(new[] { single }, _parameter)[0];
        }

        /// <summary>Steps 1-10 only: the unit's own evidence, before any corroboration.</summary>
        public LipidRtRunResult EvaluateSingleRun(LipidRtRunInput input) {
            if (input is null) {
                throw new ArgumentNullException(nameof(input));
            }
            var p = _parameter;
            var rtMin = Finite(input.RtMin);
            var rtMax = Finite(input.RtMax);
            var span = Math.Max(rtMax - rtMin, 1d);
            var eps = p.DiscordanceFraction * span;
            var tie = p.TieFraction * span;
            var win = p.CollapseFraction * span;
            var tailStart = rtMax - p.TailFraction * span;
            var negative = IsNegative(input);

            var results = new Dictionary<int, LipidRtObjectResult>();
            var candidates = input.Candidates;
            var ids = new HashSet<int>();
            foreach (var c in candidates) {
                if (!ids.Add(c.Id)) {
                    throw new ArgumentException($"Candidate Ids must be unique within a unit; {c.Id} is repeated.", nameof(input));
                }
            }
            foreach (var c in candidates) {
                if (!IsEvaluable(c)) {
                    results[c.Id] = Single(c, LipidRtOutcome.NotEvaluable, LipidRtReason.Ineligible, null, new LipidRtEvidence());
                }
            }
            var eligible = candidates.Where(IsEvaluable).ToList();
            if (eligible.Count == 0) {
                return new LipidRtRunResult(input, candidates.Select(c => results[c.Id]).ToList(), null);
            }

            // Step 2: plausibility
            var plausibility = new Dictionary<int, LipidRtPlausibilityVerdict>();
            if (_profile != null) {
                foreach (var c in eligible) {
                    plausibility[c.Id] = _profile.Evaluate(c.Ontology, c.Chains);
                }
            }

            // Step 3: MS1 artefact explanations
            var pool = BuildPool(candidates, input.Context);
            var artefacts = ExplainArtefacts(eligible, pool, negative, span);

            // Step 4: features
            var features = BuildFeatures(eligible, win, plausibility, artefacts);

            // Step 5: RP gate
            var gate = RunGate(features, span, tie);
            if (!gate.Passed) {
                foreach (var c in eligible) {
                    results[c.Id] = Single(c, LipidRtOutcome.NotEvaluable, LipidRtReason.Gate, null, new LipidRtEvidence { Gate = gate });
                }
                return new LipidRtRunResult(input, candidates.Select(c => results[c.Id]).ToList(), gate);
            }

            // Steps 6-7 per class, then the run-level DB/C step ratio, then step 8
            var byClass = features.GroupBy(f => f.ClassKey, StringComparer.Ordinal)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => g.ToList())
                .ToList();
            var classInfos = new List<ClassInfo>();
            foreach (var fs in byClass) {
                classInfos.Add(fs.Count >= 2 ? AnalyzeClass(fs, eps, tie) : ClassInfo.Single());
            }
            var ratios = new List<double>();
            foreach (var info in classInfos) {
                if (info.StepD is double d && info.StepC is double cs && cs > 0 && d > 0) {
                    ratios.Add(d / cs);
                }
            }
            double? kdRun = ratios.Count >= 2 ? Median(ratios) : (double?)null;
            for (int k = 0; k < byClass.Count; k++) {
                Magnitude(byClass[k], classInfos[k], kdRun);
            }

            // Steps 9-10
            for (int k = 0; k < byClass.Count; k++) {
                foreach (var f in byClass[k]) {
                    DecideFeature(f, byClass[k], classInfos[k], plausibility, artefacts, tailStart, results);
                }
            }
            return new LipidRtRunResult(input, candidates.Select(c => results[c.Id]).ToList(), gate);
        }

        private static double Finite(double value) => double.IsNaN(value) || double.IsInfinity(value) ? 0d : value;

        /// <summary>An eligible candidate with a composition; anything else is NotEvaluable(Ineligible).</summary>
        private static bool IsEvaluable(LipidRtCandidate c) => c.IsEligible && c.Composition != null && !double.IsNaN(c.Rt);

        private static bool IsNegative(LipidRtRunInput input) {
            switch (input.Polarity) {
                case IonMode.Negative:
                    return true;
                case IonMode.Positive:
                    return false;
                default:
                    return input.Candidates.Any(c => (c.Adduct ?? string.Empty).EndsWith("-", StringComparison.Ordinal));
            }
        }

        private static LipidRtObjectResult Single(LipidRtCandidate c, LipidRtOutcome outcome, LipidRtReason reason, double? score, LipidRtEvidence evidence) {
            return new LipidRtObjectResult
            {
                Id = c.Id,
                Candidate = c,
                Outcome = outcome,
                Reason = reason,
                Score = score,
                SingleRunOutcome = outcome,
                SingleRunReason = reason,
                SingleRunScore = score,
                Evidence = evidence,
            };
        }

        internal static bool IsSecondary(string adduct, string classKey) {
            if (adduct != null && PrimaryByClass.TryGetValue(classKey ?? string.Empty, out var primary) && primary.Contains(adduct)) {
                return false;
            }
            var a = adduct ?? string.Empty;
            return SecondaryTokens.Any(t => a.Contains(t));
        }

        internal static bool IsOxidisedClass(string classKey) => OxidisedClasses.Contains(classKey ?? string.Empty);

        internal static string FormatFeature(string classKey, int c, int d, int o, double rt) {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}:{2}{3}@{4:0.00}", classKey, c, d, o != 0 ? ";O" + o.ToString(CultureInfo.InvariantCulture) : string.Empty, rt);
        }

        private static double NonNegativeHeight(double height) => double.IsNaN(height) ? 0d : height;

        private static double Dot(double dot) => double.IsNaN(dot) ? 0d : dot;

        // ------------------------------------------------------------------------------------------ MS1 artefact layer

        private sealed class PoolRow
        {
            public int Id;
            public double Rt;
            public double Mz;
            public double Height;
            public string Adduct = string.Empty;
            public bool IsMsmsAnnotated;
            public LipidRtComposition Composition;
            public string ClassKey = string.Empty;
            public int C, D, O;
            public bool IsLabelled;
            public bool IsLipid;
            public string Name = string.Empty;
        }

        private static List<PoolRow> BuildPool(IReadOnlyList<LipidRtCandidate> candidates, IReadOnlyList<LipidRtContextPeak> context) {
            var rows = new Dictionary<int, PoolRow>();
            foreach (var c in context) {
                rows[c.Id] = new PoolRow
                {
                    Id = c.Id, Rt = c.Rt, Mz = c.Mz, Height = NonNegativeHeight(c.Height), Adduct = c.Adduct ?? string.Empty,
                    IsMsmsAnnotated = c.IsMsmsAnnotated,
                };
            }
            foreach (var s in candidates) {
                var composition = s.Composition;
                rows[s.Id] = new PoolRow
                {
                    Id = s.Id, Rt = s.Rt, Mz = s.Mz, Height = NonNegativeHeight(s.Height), Adduct = s.Adduct ?? string.Empty,
                    IsMsmsAnnotated = s.IsMsmsAnnotated,
                    Composition = composition,
                    ClassKey = string.IsNullOrEmpty(s.ClassKey) ? s.Ontology ?? string.Empty : s.ClassKey,
                    C = s.Carbon, D = s.DoubleBondEquivalent, O = s.Oxygen,
                    IsLabelled = s.IsLabelled,
                    IsLipid = composition != null,
                    Name = s.Name ?? string.Empty,
                };
            }
            return rows.Values.Where(r => !double.IsNaN(r.Rt))
                .OrderBy(r => r.Rt).ThenBy(r => r.Id)
                .ToList();
        }

        private static int LowerBound(List<PoolRow> pool, double value) {
            int lo = 0, hi = pool.Count;
            while (lo < hi) {
                var mid = (lo + hi) >> 1;
                if (pool[mid].Rt < value) lo = mid + 1; else hi = mid;
            }
            return lo;
        }

        private static int UpperBound(List<PoolRow> pool, double value) {
            int lo = 0, hi = pool.Count;
            while (lo < hi) {
                var mid = (lo + hi) >> 1;
                if (pool[mid].Rt <= value) lo = mid + 1; else hi = mid;
            }
            return lo;
        }

        private Dictionary<int, List<LipidRtArtefact>> ExplainArtefacts(List<LipidRtCandidate> eligible, List<PoolRow> pool, bool negative, double span) {
            var p = _parameter;
            var window = p.CoelutionFraction * span;
            var kinds = p.ArtefactKinds;
            var offsets = (negative ? NegativeOffsets : PositiveOffsets).Where(o => (kinds & o.Flag) != 0).ToArray();
            var result = new Dictionary<int, List<LipidRtArtefact>>();
            foreach (var s in eligible) {
                var mz = s.Mz;
                var hs = NonNegativeHeight(s.Height);
                var rt = s.Rt;
                var mine = s.Composition;
                var flags = new List<LipidRtArtefact>();
                var lo = LowerBound(pool, rt - window);
                var hi = UpperBound(pool, rt + window);
                if (!double.IsNaN(mz) && offsets.Length > 0) {
                    var tol = p.MzToleranceDa + p.MzTolerancePpm * 1e-6 * Math.Abs(mz);
                    for (int k = lo; k < hi; k++) {
                        var c = pool[k];
                        if (c.Id == s.Id || double.IsNaN(c.Mz) || Equals(c.Composition, mine)) {
                            continue;
                        }
                        var d = mz - c.Mz;
                        foreach (var offset in offsets) {
                            if (Math.Abs(d - offset.Value) > tol) {
                                continue;
                            }
                            var ratio = offset.Kind == "iso1" ? p.Iso1Ratio : offset.Kind == "iso2" ? p.Iso2Ratio : p.ParentRatio;
                            if (c.Height < ratio * hs) {
                                continue;
                            }
                            if (offset.SpotIon != null && s.Adduct == offset.SpotIon) {
                                continue; // the spot IS annotated as that ion: a duplicate, not an artefact
                            }
                            if (offset.PartnerIon != null && c.IsMsmsAnnotated && c.Adduct != offset.PartnerIon) {
                                continue; // an annotated partner must carry the ion the relation assumes
                            }
                            var partner = c.Name.Length > 0 ? c.Name : string.Format(CultureInfo.InvariantCulture, "m/z {0:0.0000}", c.Mz);
                            var fold = hs > 0 ? (c.Height / hs).ToString("0.0", CultureInfo.InvariantCulture) : "inf";
                            flags.Add(new LipidRtArtefact(offset.Kind, string.Format(CultureInfo.InvariantCulture,
                                "{0}: {1:0.0000} is {2} {3}{4:0.0000} of a {5}x brighter co-eluting {6}@{7:0.00}",
                                offset.Kind, mz, partner, offset.Value >= 0 ? "+" : "-", Math.Abs(offset.Value), fold, c.Adduct, c.Rt), c.Id));
                            break;
                        }
                    }
                }
                var cls = mine?.ClassKey ?? string.Empty;
                var chainParents = (kinds & LipidRtArtefactKinds.IsfChain) != 0 && InSourceChainLoss.TryGetValue(cls, out var pc) ? pc : Array.Empty<string>();
                var headParents = (kinds & LipidRtArtefactKinds.IsfHead) != 0 && InSourceHeadLoss.TryGetValue(cls, out var ph) ? ph : Array.Empty<string>();
                var oxBase = (kinds & LipidRtArtefactKinds.Ox) != 0 && OxidisedBase.TryGetValue(cls, out var ob) ? ob : null;
                if (chainParents.Length > 0 || headParents.Length > 0 || oxBase != null) {
                    for (int k = lo; k < hi; k++) {
                        var a = pool[k];
                        if (!a.IsLipid || a.IsLabelled || a.Id == s.Id || a.Height < p.ParentRatio * hs) {
                            continue;
                        }
                        int dC = a.C - s.Carbon, dD = a.D - s.DoubleBondEquivalent, dO = a.O - s.Oxygen;
                        if (chainParents.Contains(a.ClassKey) && 12 <= dC && dC <= 26 && 0 <= dD && dD <= 6 && dC - 2 * dD >= 8 && dO >= 0) {
                            flags.Add(new LipidRtArtefact("isf_chain", string.Format(CultureInfo.InvariantCulture,
                                "isf: chain-loss fragment of brighter {0} {1}@{2:0.00}", a.ClassKey, a.Name, a.Rt), a.Id));
                            break;
                        }
                        if (headParents.Contains(a.ClassKey) && dC == 0 && dD == 0 && dO == 0) {
                            flags.Add(new LipidRtArtefact("isf_head", string.Format(CultureInfo.InvariantCulture,
                                "isf: head-group loss of brighter {0} {1}@{2:0.00}", a.ClassKey, a.Name, a.Rt), a.Id));
                            break;
                        }
                        if (oxBase != null && a.ClassKey == oxBase && a.D == s.DoubleBondEquivalent && a.O == 0 && (a.C == s.Carbon || a.C == s.Carbon - 1)) {
                            flags.Add(new LipidRtArtefact("ox", string.Format(CultureInfo.InvariantCulture,
                                "ox: co-elutes with brighter base species {0} {1}@{2:0.00} (more O should elute earlier)", a.ClassKey, a.Name, a.Rt), a.Id));
                            break;
                        }
                    }
                }
                if (flags.Count > 0) {
                    result[s.Id] = flags;
                }
            }
            return result;
        }

        // -------------------------------------------------------------------------------------------------- features

        private sealed class Feature
        {
            public string ClassKey;
            public int C, D, O;
            public double Rt;
            public List<LipidRtCandidate> Members;
            public LipidRtCandidate Best;
            public double W0;
            public double Multiplier;
            public double MaxHeight;
            public long W = 1;
            public LipidRtTagLevel Level;
            public bool Hard;
            public bool Artefact;
            public bool SecondaryOnly;
            public int AdductCount;
            public string Name;
            public string Molecular;
            public double HeightMedian;
            public LipidRtFeatureStatus Status = LipidRtFeatureStatus.Free;
            public double E;
            public double Deficit;
            public readonly SortedSet<string> Directions = new SortedSet<string>(StringComparer.Ordinal);
            public readonly List<(string Label, int Index, double Gap)> Support = new List<(string, int, double)>();
            public List<int> Discordant = new List<int>();
            public int Ties;
            public readonly List<int> TiePartners = new List<int>();
            public double? Predicted, Residual, Delta;
            public Feature MinorDuplicateOf;

            public string Format() => FormatFeature(ClassKey, C, D, O, Rt);
        }

        private static (bool Primary, int Rank, double Dot, double Height, double NegRt) BestKey(LipidRtCandidate s, string classKey) {
            return (!IsSecondary(s.Adduct, classKey), LevelRank(s.TagLevel), Dot(s.WeightedDot), NonNegativeHeight(s.Height), -s.Rt);
        }

        private static int LevelRank(LipidRtTagLevel level) {
            switch (level) {
                case LipidRtTagLevel.Chain: return 3;
                case LipidRtTagLevel.Msms: return 2;
                case LipidRtTagLevel.Class: return 1;
                default: return 0;
            }
        }

        private double LevelWeight(LipidRtTagLevel level) {
            switch (level) {
                case LipidRtTagLevel.Chain: return _parameter.LevelWeightChain;
                case LipidRtTagLevel.Msms: return _parameter.LevelWeightMsms;
                case LipidRtTagLevel.Class: return _parameter.LevelWeightClass;
                default: return 0.5;
            }
        }

        private static int CompareBestKeys((bool Primary, int Rank, double Dot, double Height, double NegRt) a, (bool Primary, int Rank, double Dot, double Height, double NegRt) b) {
            var c = a.Primary.CompareTo(b.Primary);
            if (c != 0) return c;
            c = a.Rank.CompareTo(b.Rank);
            if (c != 0) return c;
            c = a.Dot.CompareTo(b.Dot);
            if (c != 0) return c;
            c = a.Height.CompareTo(b.Height);
            if (c != 0) return c;
            return a.NegRt.CompareTo(b.NegRt);
        }

        private List<Feature> BuildFeatures(List<LipidRtCandidate> eligible, double win, Dictionary<int, LipidRtPlausibilityVerdict> plausibility, Dictionary<int, List<LipidRtArtefact>> artefacts) {
            var p = _parameter;
            var groups = eligible.GroupBy(s => s.Composition).OrderBy(g => g.Key).ToList();
            var features = new List<Feature>();
            foreach (var group in groups) {
                var key = group.Key;
                var rs = group.OrderBy(s => s.Rt).ThenBy(s => s.Id).ToList();
                var chains = new List<List<LipidRtCandidate>>();
                var current = new List<LipidRtCandidate> { rs[0] };
                for (int i = 1; i < rs.Count; i++) {
                    if (rs[i].Rt - current[current.Count - 1].Rt <= win) {
                        current.Add(rs[i]);
                    }
                    else {
                        chains.Add(current);
                        current = new List<LipidRtCandidate> { rs[i] };
                    }
                }
                chains.Add(current);
                foreach (var members in chains) {
                    // the best member: first maximum in (Rt, Id) order
                    var best = members[0];
                    var bestKey = BestKey(best, key.ClassKey);
                    for (int i = 1; i < members.Count; i++) {
                        var k = BestKey(members[i], key.ClassKey);
                        if (CompareBestKeys(k, bestKey) > 0) {
                            best = members[i];
                            bestKey = k;
                        }
                    }
                    var adducts = members.Select(m => m.Adduct ?? string.Empty).Distinct(StringComparer.Ordinal).Count();
                    var w = LevelWeight(best.TagLevel) + p.DotWeight * Dot(best.WeightedDot) + p.DuplicateWeight * Math.Min(adducts - 1, p.DuplicateCap);
                    var hard = plausibility.Count > 0 && plausibility[best.Id].IsHard;
                    var artefact = members.All(m => artefacts.ContainsKey(m.Id));
                    var multiplier = (hard ? p.ImplausibleWeight : 1d) * (artefact ? p.ArtefactWeight : 1d);
                    features.Add(new Feature
                    {
                        ClassKey = key.ClassKey, C = key.Carbon, D = key.DoubleBondEquivalent, O = key.Oxygen,
                        Rt = best.Rt, Members = members, Best = best,
                        W0 = Math.Max(w, 0.1) * multiplier, Multiplier = multiplier,
                        MaxHeight = members.Max(m => NonNegativeHeight(m.Height)),
                        Level = best.TagLevel, Hard = hard, Artefact = artefact,
                        SecondaryOnly = members.All(m => IsSecondary(m.Adduct, key.ClassKey)),
                        AdductCount = adducts, Name = best.Name ?? string.Empty, Molecular = best.Molecular ?? string.Empty,
                    });
                }
            }
            foreach (var fs in features.GroupBy(f => f.ClassKey, StringComparer.Ordinal)) {
                var list = fs.ToList();
                var logs = list.Where(f => f.MaxHeight > 0).Select(f => Math.Log10(f.MaxHeight)).ToList();
                var median = logs.Count > 0 ? Median(logs) : 0d;
                foreach (var f in list) {
                    f.HeightMedian = median;
                    var hb = f.MaxHeight > 0 ? Math.Min(Math.Max(Math.Log10(f.MaxHeight) - median, 0d), 3d) : 0d;
                    f.W = Math.Max(1L, (long)Math.Round(Scale * (f.W0 + p.HeightWeight * hb * f.Multiplier), MidpointRounding.ToEven));
                }
                // minor duplicate: same species name, same (or unresolved) molecular name, at another RT, less intense
                foreach (var byName in list.GroupBy(f => f.Name, StringComparer.Ordinal)) {
                    var gs = byName.ToList();
                    if (gs.Count < 2) {
                        continue;
                    }
                    foreach (var f in gs) {
                        foreach (var g in gs) {
                            if (ReferenceEquals(g, f) || (f.Molecular.Length > 0 && g.Molecular.Length > 0 && f.Molecular != g.Molecular)) {
                                continue;
                            }
                            if (g.MaxHeight > f.MaxHeight || (g.MaxHeight == f.MaxHeight && -g.Rt > -f.Rt)) {
                                f.MinorDuplicateOf = g;
                                break;
                            }
                        }
                    }
                }
            }
            return features;
        }

        // ------------------------------------------------------------------------------------------------------ gate

        private static string AdjacentKind(int dC, int dD, int dO) {
            int a = Math.Abs(dC), b = Math.Abs(dD), c = Math.Abs(dO);
            if (b == 0 && c == 0 && 1 <= a && a <= 2) return "C";
            if (a == 0 && c == 0 && b == 1) return "D";
            if (a == 0 && b == 0 && c == 1) return "O";
            return null;
        }

        private LipidRtGateStatistics RunGate(List<Feature> features, double span, double tie) {
            var p = _parameter;
            if (p.Chromatography == LipidRtChromatography.Other) {
                return new LipidRtGateStatistics { Passed = false, ChromatographyIsOther = true };
            }
            var slopes = new List<double>();
            int allConcordant = 0, allTotal = 0, trustedConcordant = 0, trustedTotal = 0;
            foreach (var group in features.Where(f => !f.Artefact && !f.Hard).GroupBy(f => f.ClassKey, StringComparer.Ordinal)) {
                var fs = group.ToList();
                if (fs.Count >= 5 && fs.Select(f => f.C).Distinct().Count() >= 3) {
                    var sl = new List<double>();
                    foreach (var a in fs) {
                        foreach (var b in fs) {
                            if (a.D == b.D && a.O == b.O && b.C > a.C) {
                                sl.Add((b.Rt - a.Rt) / (b.C - a.C));
                            }
                        }
                    }
                    if (sl.Count > 0) {
                        slopes.Add(Median(sl));
                    }
                }
                for (int i = 0; i < fs.Count; i++) {
                    var a = fs[i];
                    for (int j = i + 1; j < fs.Count; j++) {
                        var b = fs[j];
                        if (AdjacentKind(a.C - b.C, a.D - b.D, a.O - b.O) != "C") {
                            continue;
                        }
                        var (hi, lo) = a.C > b.C ? (a, b) : (b, a);
                        var ok = (hi.Rt - lo.Rt) > tie;
                        allConcordant += ok ? 1 : 0;
                        allTotal++;
                        if (IsTrusted(a) && IsTrusted(b)) {
                            trustedConcordant += ok ? 1 : 0;
                            trustedTotal++;
                        }
                    }
                }
            }
            var median = slopes.Count > 0 ? Median(slopes) : 0d;
            var useTrusted = trustedTotal >= p.GateMinPairs;
            var concordant = useTrusted ? trustedConcordant : allConcordant;
            var total = useTrusted ? trustedTotal : allTotal;
            double? fraction = total > 0 ? (double)concordant / total : (double?)null;
            var slopeOk = slopes.Count > 0 && median > p.SlopeFraction * span;
            var concordanceOk = total >= p.GateMinPairs && fraction >= p.GateConcordance;
            return new LipidRtGateStatistics
            {
                Passed = slopeOk && concordanceOk,
                MedianCarbonSlope = median,
                SlopeThreshold = p.SlopeFraction * span,
                ClassCount = slopes.Count,
                Concordance = fraction,
                PairCount = total,
                ConcordanceOn = useTrusted ? "trusted" : "all",
                ConcordanceAllPairs = allTotal > 0 ? (double)allConcordant / allTotal : (double?)null,
            };
        }

        private static bool IsTrusted(Feature f) {
            return f.Level == LipidRtTagLevel.Chain && f.MaxHeight > 0 && Math.Log10(f.MaxHeight) >= f.HeightMedian;
        }

        // -------------------------------------------------------------------------------------- class order analysis

        private sealed class ClassInfo
        {
            public int AdjacentTotal;
            public double? AdjacentConcordance;
            public double? StepC, StepD, StepO;
            public bool GateOk = true;
            public Dictionary<int, List<int>> Neighbours = new Dictionary<int, List<int>>();

            public static ClassInfo Single() => new ClassInfo();

            public double? Step(string kind) => kind == "C" ? StepC : kind == "D" ? StepD : StepO;
        }

        private static bool Dominates(Feature i, Feature j) {
            return i.C >= j.C && i.D <= j.D && i.O <= j.O && !(i.C == j.C && i.D == j.D && i.O == j.O);
        }

        private ClassInfo AnalyzeClass(List<Feature> fs, double eps, double tie) {
            var p = _parameter;
            var n = fs.Count;
            var discordant = new List<(int, int)>();
            var adjacent = new List<(int I, int J)>();
            for (int i = 0; i < n; i++) {
                for (int j = 0; j < n; j++) {
                    if (i == j || !Dominates(fs[i], fs[j])) {
                        continue;
                    }
                    var gap = fs[i].Rt - fs[j].Rt;
                    if (gap < -eps) {
                        discordant.Add((i, j));
                    }
                    if (AdjacentKind(fs[i].C - fs[j].C, fs[i].D - fs[j].D, fs[i].O - fs[j].O) != null) {
                        adjacent.Add((i, j));
                    }
                }
            }
            var analysis = LipidRtAntichain.Analyze(n, discordant, fs.Select(f => f.W).ToList(), margins: true);
            for (int k = 0; k < n; k++) {
                var f = fs[k];
                f.Status = analysis.Status[k];
                f.E = (double)analysis.Essential[k] / Scale;
                f.Deficit = (double)analysis.Deficit[k] / Scale;
                f.Discordant = analysis.Partners[k].OrderByDescending(j => fs[j].W).ThenBy(j => fs[j].Rt).ThenBy(j => j).ToList();
            }

            var steps = new Dictionary<string, List<double>> { ["C"] = new List<double>(), ["D"] = new List<double>(), ["O"] = new List<double>() };
            int concordant = 0, total = 0;
            foreach (var (i, j) in adjacent) {
                var kind = AdjacentKind(fs[i].C - fs[j].C, fs[i].D - fs[j].D, fs[i].O - fs[j].O);
                var g = fs[i].Rt - fs[j].Rt;
                if (fs[i].Level == LipidRtTagLevel.Chain && fs[j].Level == LipidRtTagLevel.Chain) {
                    total++;
                    concordant += g > tie ? 1 : 0;
                }
                if (fs[i].Status != LipidRtFeatureStatus.Excluded && fs[j].Status != LipidRtFeatureStatus.Excluded) {
                    steps[kind].Add(g / (kind == "C" ? Math.Abs(fs[i].C - fs[j].C) : 1));
                }
            }
            var info = new ClassInfo
            {
                AdjacentTotal = total,
                AdjacentConcordance = total > 0 ? (double)concordant / total : (double?)null,
                StepC = steps["C"].Count >= 3 ? Median(steps["C"]) : (double?)null,
                StepD = steps["D"].Count >= 3 ? Median(steps["D"]) : (double?)null,
                StepO = steps["O"].Count >= 3 ? Median(steps["O"]) : (double?)null,
            };
            if (total >= p.ClassGateMinPairs && (double)concordant / total < p.ClassGateConcordance) {
                info.GateOk = false;
            }
            if (info.StepC is double stepC && stepC <= 0) {
                info.GateOk = false;
            }
            for (int k = 0; k < n; k++) {
                info.Neighbours[k] = new List<int>();
            }
            foreach (var (i, j) in adjacent) {
                var kind = AdjacentKind(fs[i].C - fs[j].C, fs[i].D - fs[j].D, fs[i].O - fs[j].O);
                var g = fs[i].Rt - fs[j].Rt; // expected > 0: i is predicted later
                var step = info.Step(kind) is double st && st > 0 ? st : 0d;
                var threshold = Math.Max(tie, p.StepFraction * step * (kind == "C" ? Math.Abs(fs[i].C - fs[j].C) : 1));
                var fi = fs[i];
                var fj = fs[j];
                string labelI, labelJ;
                switch (kind) {
                    case "C": labelI = "C<"; labelJ = "C>"; break;
                    case "D": labelI = "D>"; labelJ = "D<"; break;
                    default: labelI = "O>"; labelJ = "O<"; break;
                }
                var usableI = IsUsable(fi);
                var usableJ = IsUsable(fj);
                if (usableJ) {
                    info.Neighbours[i].Add(j);
                }
                if (usableI) {
                    info.Neighbours[j].Add(i);
                }
                if (g > threshold) {
                    if (usableJ) {
                        fi.Directions.Add(labelI);
                        fi.Support.Add((labelI, j, g));
                    }
                    if (usableI) {
                        fj.Directions.Add(labelJ);
                        fj.Support.Add((labelJ, i, -g));
                    }
                }
                else if (g >= -eps) {
                    fi.Ties++;
                    fj.Ties++;
                    fi.TiePartners.Add(j);
                    fj.TiePartners.Add(i);
                }
            }
            return info;
        }

        private static bool IsUsable(Feature f) {
            return (f.Status == LipidRtFeatureStatus.Core || f.Status == LipidRtFeatureStatus.Free) && !f.Hard && !f.Artefact;
        }

        private static void Magnitude(List<Feature> fs, ClassInfo info, double? kdRun) {
            var sC = info.StepC;
            var sD = info.StepD;
            var sO = info.StepO;
            if (sD is null && sC is double c && c > 0 && kdRun is double kd) {
                sD = kd * c;
            }
            if (sO is null && sD.HasValue) {
                sO = sD;
            }
            for (int k = 0; k < fs.Count; k++) {
                var f = fs[k];
                var predictions = new List<double>();
                if (info.Neighbours.TryGetValue(k, out var neighbours)) {
                    foreach (var j in neighbours) {
                        var g = fs[j];
                        int dc = f.C - g.C, dd = f.D - g.D, dO = f.O - g.O;
                        if ((dc != 0 && sC is null) || (dd != 0 && sD is null) || (dO != 0 && sO is null)) {
                            continue;
                        }
                        predictions.Add(g.Rt + dc * (sC ?? 0d) - dd * (sD ?? 0d) - dO * (sO ?? 0d));
                    }
                }
                if (predictions.Count > 0 && sD is double delta && delta > 0) {
                    f.Predicted = Median(predictions);
                    f.Residual = f.Rt - f.Predicted;
                    f.Delta = delta;
                }
            }
        }

        // ------------------------------------------------------------------------------------------------- decision

        private bool SupportOk(int axes, int directions, bool lower, bool upper) {
            int minAxes, minDirections;
            switch (_parameter.OperatingPoint) {
                case LipidRtOperatingPoint.Balanced:
                    minAxes = 1; minDirections = 1;
                    break;
                default:
                    minAxes = 2; minDirections = 2;
                    break;
            }
            var axesOk = axes >= minAxes && directions >= minDirections;
            var bracketOk = lower && upper && directions >= minDirections;
            switch (_parameter.OperatingPoint) {
                case LipidRtOperatingPoint.Balanced:
                    return axesOk;
                case LipidRtOperatingPoint.Mid:
                    return axesOk || bracketOk;
                default: // Strict and HighPrecision
                    return axesOk && bracketOk;
            }
        }

        private void DecideFeature(Feature f, List<Feature> fs, ClassInfo info, Dictionary<int, LipidRtPlausibilityVerdict> plausibility,
            Dictionary<int, List<LipidRtArtefact>> artefacts, double tailStart, Dictionary<int, LipidRtObjectResult> results) {
            var p = _parameter;
            var axes = f.Directions.Select(d => d[0]).Distinct().Count();
            var nd = f.Directions.Count;
            double? rel = null;
            if (f.Residual.HasValue && f.Delta is double delta && delta != 0) {
                rel = f.Residual.Value / delta;
            }
            var magnitudeOk = !(p.MagnitudeK > 0 && rel.HasValue && Math.Abs(rel.Value) > p.MagnitudeK);

            LipidRtOutcome outcome;
            LipidRtReason reason;
            double score;
            Feature strongTie = null;
            if (f.Status == LipidRtFeatureStatus.Excluded) {
                score = -1d - f.Deficit;
                if (f.Deficit >= p.InconsistentMinDeficit) {
                    if (IsOxidisedClass(f.ClassKey) && !p.FlagOxidisedClasses) {
                        (outcome, reason) = (LipidRtOutcome.NotEvaluable, LipidRtReason.OxNoFlag);
                    }
                    else if (f.Rt > tailStart && !p.FlagGradientTail) {
                        (outcome, reason) = (LipidRtOutcome.NotEvaluable, LipidRtReason.TailNoFlag);
                    }
                    else {
                        (outcome, reason) = (LipidRtOutcome.Inconsistent, LipidRtReason.InconsistentOrder);
                    }
                }
                else {
                    (outcome, reason) = (LipidRtOutcome.NotEvaluable, LipidRtReason.SmallDeficit);
                }
            }
            else if (f.Status == LipidRtFeatureStatus.Ambiguous) {
                score = 0d;
                (outcome, reason) = (LipidRtOutcome.NotEvaluable, LipidRtReason.Ambiguous);
            }
            else if (!info.GateOk) {
                score = -0.5;
                (outcome, reason) = (LipidRtOutcome.NotEvaluable, LipidRtReason.ClassGate);
            }
            else {
                score = nd + 0.5 * axes - 0.25 * f.Ties + (f.Status == LipidRtFeatureStatus.Core ? 0.1 * Math.Min(f.E, 5d) : 0.5);
                if (rel.HasValue) {
                    score -= 0.5 * Math.Min(Math.Abs(rel.Value), 2d);
                }
                var lower = f.Directions.Contains("C<") || f.Directions.Contains("D>") || f.Directions.Contains("O>");
                var upper = f.Directions.Contains("C>") || f.Directions.Contains("D<") || f.Directions.Contains("O<");
                foreach (var j in f.TiePartners) {
                    if (fs[j].Status != LipidRtFeatureStatus.Excluded && fs[j].W >= p.TieRatio * f.W) {
                        strongTie = fs[j];
                        break;
                    }
                }
                if (!SupportOk(axes, nd, lower, upper)) {
                    (outcome, reason) = (LipidRtOutcome.NotEvaluable, LipidRtReason.Support);
                }
                else if (p.TieBlock && strongTie != null) {
                    score -= p.BlockPenalty;
                    (outcome, reason) = (LipidRtOutcome.NotEvaluable, LipidRtReason.Tie);
                }
                else if (f.Artefact) {
                    score -= p.BlockPenalty;
                    (outcome, reason) = (LipidRtOutcome.NotEvaluable, LipidRtReason.Artefact);
                }
                else if (!magnitudeOk) {
                    score -= p.BlockPenalty;
                    (outcome, reason) = (LipidRtOutcome.NotEvaluable, LipidRtReason.Magnitude);
                }
                else if (f.Status == LipidRtFeatureStatus.Core && f.E < p.CoreMargin) {
                    score -= p.BlockPenalty;
                    (outcome, reason) = (LipidRtOutcome.NotEvaluable, LipidRtReason.Margin);
                }
                else if (f.SecondaryOnly && p.SecondaryOnlyPolicy == LipidRtSecondaryOnlyPolicy.Block) {
                    score -= p.PolicyPenalty;
                    (outcome, reason) = (LipidRtOutcome.NotEvaluable, LipidRtReason.PolicySecondary);
                }
                else if (f.MinorDuplicateOf != null && p.MinorDuplicatePolicy == LipidRtMinorDuplicatePolicy.Block) {
                    score -= p.PolicyPenalty;
                    (outcome, reason) = (LipidRtOutcome.NotEvaluable, LipidRtReason.MinorDuplicate);
                }
                else {
                    (outcome, reason) = (LipidRtOutcome.Confirmed, LipidRtReason.ConfirmedRt);
                }
            }

            var max = Math.Max(p.MaxEvidenceItems, 0);
            var support = f.Support
                .OrderByDescending(s => Math.Abs(s.Gap)).ThenBy(s => s.Label, StringComparer.Ordinal)
                .Take(max)
                .Select(s => new LipidRtNeighbour(s.Label, fs[s.Index].Format(), fs[s.Index].Rt, s.Gap))
                .ToList();
            var discordantPartners = f.Discordant.Take(max)
                .Select(j => new LipidRtDiscordantPartner(fs[j].Format(), fs[j].Rt, fs[j].Status))
                .ToList();
            var memberIds = f.Members.Select(m => m.Id).ToList();
            LipidRtArtefact featureArtefact = f.Artefact && artefacts.TryGetValue(f.Best.Id, out var bestArtefacts) ? bestArtefacts[0] : null;

            foreach (var s in f.Members) {
                var memberOutcome = outcome;
                var memberReason = reason;
                var penalty = 0d;
                var softOverridden = false;
                if (memberOutcome == LipidRtOutcome.Confirmed) {
                    if (p.ClassLevelPolicy == LipidRtClassLevelPolicy.Never && s.TagLevel == LipidRtTagLevel.Class) {
                        penalty += p.PolicyPenalty;
                        (memberOutcome, memberReason) = (LipidRtOutcome.NotEvaluable, LipidRtReason.PolicyClass);
                    }
                    else if (artefacts.ContainsKey(s.Id)) {
                        penalty += p.BlockPenalty;
                        (memberOutcome, memberReason) = (LipidRtOutcome.NotEvaluable, LipidRtReason.Artefact);
                    }
                }
                var verdict = LipidRtPlausibilityVerdict.NotEvaluated;
                if (plausibility.Count > 0) {
                    verdict = plausibility[s.Id];
                    if (verdict.IsImplausible) {
                        if (verdict.Severity == LipidRtPlausibilitySeverity.Hard) {
                            penalty += 2 * p.PolicyPenalty;
                            (memberOutcome, memberReason) = (LipidRtOutcome.Inconsistent, LipidRtReason.InconsistentPlausibility);
                        }
                        else if (memberOutcome == LipidRtOutcome.Confirmed) {
                            if (!(axes >= p.SoftOverrideAxes && Dot(s.WeightedDot) >= p.SoftOverrideDot)) {
                                penalty += p.PolicyPenalty;
                                (memberOutcome, memberReason) = (LipidRtOutcome.NotEvaluable, LipidRtReason.PlausibilitySoft);
                            }
                            else {
                                softOverridden = true;
                            }
                        }
                        else {
                            penalty += p.PolicyPenalty;
                        }
                    }
                }
                var evidence = new LipidRtEvidence
                {
                    Feature = f.Format(),
                    Composition = new LipidRtComposition(f.ClassKey, f.C, f.D, f.O),
                    FeatureRt = f.Rt,
                    Status = f.Status,
                    Margin = f.E,
                    Deficit = f.Deficit,
                    Weight = f.W,
                    Directions = string.Concat(f.Directions),
                    Axes = axes,
                    Ties = f.Ties,
                    Support = support,
                    Discordant = discordantPartners,
                    DiscordantCount = f.Discordant.Count,
                    TiePartner = reason == LipidRtReason.Tie && strongTie != null ? strongTie.Format() : string.Empty,
                    MinorDuplicateOf = f.MinorDuplicateOf is null ? string.Empty
                        : string.Format(CultureInfo.InvariantCulture, "{0} {1}@{2:0.00}", f.MinorDuplicateOf.Name, f.MinorDuplicateOf.Molecular, f.MinorDuplicateOf.Rt),
                    Predicted = f.Predicted,
                    Residual = f.Residual,
                    ResidualDoubleBondSteps = rel,
                    ClassGatePassed = info.GateOk,
                    ClassAdjacentConcordance = info.AdjacentConcordance,
                    Members = memberIds,
                    AdductCount = f.AdductCount,
                    Artefacts = artefacts.TryGetValue(s.Id, out var own) ? own : (IReadOnlyList<LipidRtArtefact>)Array.Empty<LipidRtArtefact>(),
                    FeatureArtefact = featureArtefact,
                    Plausibility = verdict,
                    SoftPlausibilityOverridden = softOverridden,
                };
                results[s.Id] = Single(s, memberOutcome, memberReason, score - penalty, evidence);
            }
        }

        // ------------------------------------------------------------------------------------------------- numerics

        /// <summary>The median as numpy computes it: the mean of the two middle values for an even count.</summary>
        internal static double Median(IReadOnlyCollection<double> values) {
            if (values.Count == 0) {
                throw new ArgumentException("No values.", nameof(values));
            }
            var sorted = values.ToArray();
            Array.Sort(sorted);
            var mid = sorted.Length / 2;
            return sorted.Length % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2d;
        }
    }
}

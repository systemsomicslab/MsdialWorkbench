using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt.Tests
{
    /// <summary>
    /// Builds tiny synthetic runs with the numbers of the Python reference's test_lipidrt.py (SPEC 13.1).
    /// </summary>
    internal sealed class LipidRtSyntheticRun
    {
        private const double H = 1.007825, C12 = 12.0, O16 = 15.994915, N14 = 14.003074, P31 = 30.973762;
        private const double Proton = 1.007276;

        private static readonly Dictionary<string, double> AdductMass = new Dictionary<string, double> {
            ["[M+H]+"] = Proton, ["[M+NH4]+"] = 18.033823, ["[M+Na]+"] = 22.989218, ["[M-H]-"] = -Proton, ["[M+CH3COO]-"] = 59.013853,
        };

        private int _next;

        /// <summary>Neutral monoisotopic mass of a few classes from total chain C:DB (enough for consistent synthetic m/z).</summary>
        public static double Mass(string cls, int c, int db, int o = 0) {
            switch (cls) {
                case "TG":
                    return C12 * (c + 3) + H * (2 * c - 2 * db + 2) + O16 * (6 + o);
                case "PC":
                case "OxPC":
                    return C12 * (c + 8) + H * (2 * c - 2 * db + 16) + N14 + O16 * (8 + o) + P31;
                case "PE":
                    return C12 * (c + 5) + H * (2 * c - 2 * db + 10) + N14 + O16 * (8 + o) + P31;
                default:
                    throw new ArgumentException(cls);
            }
        }

        public static double AdductOf(string adduct) => AdductMass[adduct];

        public LipidRtCandidate Mk(string cls, int c, int db, double rt, string adduct = "[M+NH4]+", int o = 0, LipidRtTagLevel level = LipidRtTagLevel.Chain,
            double h = 1e5, double? mz = null, string molecular = null, bool eligible = true, string ontology = null, double dp = 0.8) {
            _next++;
            var token = cls == "OxPC" ? "PC" : cls;
            var name = $"{token} {c}:{db}" + (o == 1 ? ";O" : o > 0 ? ";O" + o : string.Empty);
            if (molecular is null && level == LipidRtTagLevel.Chain) {
                molecular = $"{token} {c / 2}:{db / 2}_{c - c / 2}:{db - db / 2}";
            }
            var raw = name + (string.IsNullOrEmpty(molecular) ? string.Empty : "|" + molecular);
            var ont = ontology ?? cls;
            return new LipidRtCandidate
            {
                Id = _next,
                Rt = rt,
                Mz = mz ?? Mass(cls, c, db, o) + AdductMass[adduct],
                Height = h,
                Adduct = adduct,
                Name = name,
                Molecular = molecular ?? string.Empty,
                Ontology = ont,
                ClassKey = cls,
                Carbon = c,
                DoubleBondEquivalent = db,
                Oxygen = o,
                IsLipid = true,
                IsLabelled = false,
                IsMsmsAnnotated = true,
                TagLevel = level,
                WeightedDot = dp,
                Chains = LipidRtNameParser.Parse(raw, ont).Chains,
                IsEligible = eligible,
                IneligibleReason = eligible ? string.Empty : "test",
            };
        }

        /// <summary>The TG grid: C 48..56 (even), DB 0..3, Rt = rt0 + 0.15 C - 0.25 DB, [M+NH4]+, chain level, height 1e5, dot 0.8.</summary>
        public List<LipidRtCandidate> TgGrid(double rt0 = 2.0, double bC = 0.15, double cD = -0.25, double scale = 1.0,
            ICollection<(int, int)> skip = null, string adduct = "[M+NH4]+") {
            var result = new List<LipidRtCandidate>();
            foreach (var c in new[] { 48, 50, 52, 54, 56 }) {
                foreach (var db in new[] { 0, 1, 2, 3 }) {
                    if (skip != null && skip.Contains((c, db))) {
                        continue;
                    }
                    result.Add(Mk("TG", c, db, scale * (rt0 + bC * c + cD * db), adduct: adduct));
                }
            }
            return result;
        }

        public static LipidRtCheckParameter Parameter(Action<LipidRtCheckParameter> configure = null) {
            // The reference's synthetic tests ran with the frozen default, ClassLevelPolicy Consistent.
            var parameter = new LipidRtCheckParameter { ClassLevelPolicy = LipidRtClassLevelPolicy.Consistent };
            configure?.Invoke(parameter);
            return parameter;
        }

        public static LipidRtRunInput Input(IEnumerable<LipidRtCandidate> spots, double rtMin = 0, double rtMax = 20, IonMode polarity = IonMode.Positive) {
            return new LipidRtRunInput(spots.ToList(), Array.Empty<LipidRtContextPeak>(), rtMin, rtMax, polarity);
        }

        public static LipidRtRunResult Run(IEnumerable<LipidRtCandidate> spots, double rtMin = 0, double rtMax = 20, IonMode polarity = IonMode.Positive,
            ILipidRtPlausibilityProfile profile = null, Action<LipidRtCheckParameter> configure = null) {
            var check = new LipidRtCheck(Parameter(configure), profile);
            return check.Evaluate(Input(spots, rtMin, rtMax, polarity));
        }

        public static LipidRtRunResult RunSingle(IEnumerable<LipidRtCandidate> spots, double rtMin = 0, double rtMax = 20, IonMode polarity = IonMode.Positive,
            ILipidRtPlausibilityProfile profile = null, Action<LipidRtCheckParameter> configure = null) {
            var check = new LipidRtCheck(Parameter(configure), profile);
            return check.EvaluateSingleRun(Input(spots, rtMin, rtMax, polarity));
        }

        public static int Find(IEnumerable<LipidRtCandidate> spots, string cls, int c, int db, int o = 0, string adduct = null) {
            return spots.First(s => s.ClassKey == cls && s.Carbon == c && s.DoubleBondEquivalent == db && s.Oxygen == o && (adduct is null || s.Adduct == adduct)).Id;
        }

        public static LipidRtCandidate With(LipidRtCandidate s, double? rt = null, LipidRtTagLevel? level = null, string molecular = null) {
            return new LipidRtCandidate
            {
                Id = s.Id, Rt = rt ?? s.Rt, Mz = s.Mz, Height = s.Height, Adduct = s.Adduct, Name = s.Name,
                Molecular = molecular ?? s.Molecular, Ontology = s.Ontology, ClassKey = s.ClassKey,
                Carbon = s.Carbon, DoubleBondEquivalent = s.DoubleBondEquivalent, Oxygen = s.Oxygen,
                IsLipid = s.IsLipid, IsLabelled = s.IsLabelled, IsMsmsAnnotated = s.IsMsmsAnnotated,
                TagLevel = level ?? s.TagLevel, WeightedDot = s.WeightedDot, Chains = s.Chains,
                IsEligible = s.IsEligible, IsInSilico = s.IsInSilico, IneligibleReason = s.IneligibleReason,
            };
        }
    }
}

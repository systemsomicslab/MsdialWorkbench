using CompMs.Common.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    /// <summary>
    /// How far a staged evaluation got for one lipid candidate.
    /// </summary>
    public enum StagedLipidLevel
    {
        /// <summary>The class could not be confirmed.</summary>
        None,
        /// <summary>Class (and so the total composition) confirmed.</summary>
        Class,
        /// <summary>Individual chains confirmed (molecular species).</summary>
        MolecularSpecies,
        /// <summary>Chains confirmed and assigned to sn positions.</summary>
        Position,
    }

    /// <summary>
    /// Why the evaluation stopped where it did.
    /// </summary>
    public enum StagedLipidStopReason
    {
        /// <summary>The rule set has no LSILevel (None); nothing is evaluated.</summary>
        NotAnnotatable,
        /// <summary>A diagnostic class-level ion was not observed above its threshold.</summary>
        ClassDiagnosticIonMissing,
        /// <summary>A prohibited ion was observed above its threshold.</summary>
        ProhibitedIonObserved,
        /// <summary>The rule set resolves species level only (LSILevel SL).</summary>
        ResolutionLimitedToSpecies,
        /// <summary>The candidate has no individual chains to confirm.</summary>
        ChainsNotDetermined,
        /// <summary>A diagnostic chain ion was not observed above its threshold.</summary>
        ChainDiagnosticIonMissing,
        /// <summary>The rule set has no position-diagnostic ion.</summary>
        NoPositionDiagnosticIon,
        /// <summary>A position-diagnostic ion was not observed above its threshold.</summary>
        PositionDiagnosticIonMissing,
        /// <summary>Several molecular species explain the spectrum equally well; only the class is reported.</summary>
        MolecularSpeciesAmbiguous,
        /// <summary>Several sn orders explain the spectrum equally well; only the molecular species is reported.</summary>
        PositionAmbiguous,
        /// <summary>The seed expands to more molecular species than the annotator evaluates; only the class is checked.</summary>
        TooManyCandidates,
        /// <summary>Every stage passed.</summary>
        Resolved,
    }

    /// <summary>
    /// The result of evaluating one candidate's fragments against an observed spectrum.
    /// </summary>
    public sealed class StagedLipidEvaluation
    {
        public StagedLipidEvaluation(StagedLipidLevel level, StagedLipidStopReason stopReason, IReadOnlyList<LipidFragmentMatch> matches, double similarity) {
            Level = level;
            StopReason = stopReason;
            Matches = matches;
            Similarity = similarity;
        }

        public StagedLipidLevel Level { get; }
        public StagedLipidStopReason StopReason { get; }

        /// <summary>
        /// Cosine similarity (0-1) between the candidate's theoretical spectrum (visible fragments) and the
        /// whole observed spectrum; unexplained observed peaks lower it. Ranks candidates of the same level.
        /// </summary>
        public double Similarity { get; }

        /// <summary>Every evaluated fragment with the observed intensity at its m/z (0 when absent).</summary>
        public IReadOnlyList<LipidFragmentMatch> Matches { get; }
    }

    /// <summary>
    /// A fragment and what the observed spectrum has at its m/z.
    /// </summary>
    public sealed class LipidFragmentMatch
    {
        public LipidFragmentMatch(LipidFragment fragment, double observedIntensity, double threshold) {
            Fragment = fragment;
            ObservedIntensity = observedIntensity;
            Threshold = threshold;
        }

        public LipidFragment Fragment { get; }

        /// <summary>The most intense observed peak within tolerance, on the normalized (max 100) scale.</summary>
        public double ObservedIntensity { get; }

        /// <summary>The normalized intensity the peak has to exceed.</summary>
        public double Threshold { get; }

        public bool IsObserved => ObservedIntensity > Threshold;
    }

    /// <summary>
    /// Decides how far an observed MS/MS spectrum supports a lipid candidate, stage by stage, using
    /// the candidate's generated fragments (<see cref="ILipidSpectrumModel.GenerateFragments"/>):
    /// <list type="number">
    /// <item>Class: every diagnostic Precursor/ClassIon is observed and no prohibited ion is.</item>
    /// <item>Molecular species: the rule set is MSL and every diagnostic ChainIon is observed.</item>
    /// <item>Position: every position-diagnostic ion is observed.</item>
    /// </list>
    /// For now every diagnostic ion is required; DiagnosticIonGroup/Count and InvisibleIons are not used.
    /// </summary>
    public sealed class StagedLipidSpectrumEvaluator
    {
        // The rule intensities are on the XML scale (theoretical spectrum up to 999), thresholds included;
        // the observed spectrum is normalized to a maximum of 100.
        private const double RuleToNormalizedScale = 10d;

        private readonly double _ms2Tolerance;

        public StagedLipidSpectrumEvaluator(double ms2Tolerance) {
            if (ms2Tolerance <= 0) {
                throw new ArgumentOutOfRangeException(nameof(ms2Tolerance));
            }
            _ms2Tolerance = ms2Tolerance;
        }

        /// <param name="spectrum">The observed spectrum; it is normalized to a maximum of 100 here.</param>
        /// <param name="resolution">The candidate's rule set resolution (<see cref="LipidSpectrumRuleSet.Resolution"/>).</param>
        /// <param name="fragments">The candidate's fragments.</param>
        /// <param name="hasDeterminedChains">Whether the candidate has individual chains (molecular species or finer).</param>
        public StagedLipidEvaluation Evaluate(IReadOnlyList<SpectrumPeak> spectrum, LipidSpectrumResolution resolution, IReadOnlyList<LipidFragment> fragments, bool hasDeterminedChains) {
            var normalized = Normalize(spectrum);
            var matches = fragments.Select(f => Match(normalized, f)).ToList();
            var similarity = CalculateSimilarity(normalized, fragments.Where(f => f.Rule.IsVisible));

            if (resolution == LipidSpectrumResolution.None) {
                return new StagedLipidEvaluation(StagedLipidLevel.None, StagedLipidStopReason.NotAnnotatable, matches, similarity);
            }
            if (matches.Any(m => m.Fragment.Rule.Kind == LipidFragmentKind.ProhibitedIon && m.IsObserved)) {
                return new StagedLipidEvaluation(StagedLipidLevel.None, StagedLipidStopReason.ProhibitedIonObserved, matches, similarity);
            }
            if (!AllObserved(matches, IsClassDiagnostic)) {
                return new StagedLipidEvaluation(StagedLipidLevel.None, StagedLipidStopReason.ClassDiagnosticIonMissing, matches, similarity);
            }
            if (resolution != LipidSpectrumResolution.MolecularSpeciesLevel) {
                return new StagedLipidEvaluation(StagedLipidLevel.Class, StagedLipidStopReason.ResolutionLimitedToSpecies, matches, similarity);
            }
            if (!hasDeterminedChains) {
                return new StagedLipidEvaluation(StagedLipidLevel.Class, StagedLipidStopReason.ChainsNotDetermined, matches, similarity);
            }
            if (!AllObserved(matches, IsChainDiagnostic)) {
                return new StagedLipidEvaluation(StagedLipidLevel.Class, StagedLipidStopReason.ChainDiagnosticIonMissing, matches, similarity);
            }
            if (!matches.Any(m => IsPositionDiagnostic(m.Fragment.Rule))) {
                return new StagedLipidEvaluation(StagedLipidLevel.MolecularSpecies, StagedLipidStopReason.NoPositionDiagnosticIon, matches, similarity);
            }
            if (!AllObserved(matches, IsPositionDiagnostic)) {
                return new StagedLipidEvaluation(StagedLipidLevel.MolecularSpecies, StagedLipidStopReason.PositionDiagnosticIonMissing, matches, similarity);
            }
            return new StagedLipidEvaluation(StagedLipidLevel.Position, StagedLipidStopReason.Resolved, matches, similarity);
        }

        private static bool IsClassDiagnostic(LipidFragmentRule rule) {
            return rule.IsDiagnostic && (rule.Kind == LipidFragmentKind.Precursor || rule.Kind == LipidFragmentKind.ClassIon);
        }

        private static bool IsChainDiagnostic(LipidFragmentRule rule) {
            return rule.IsDiagnostic && rule.Kind == LipidFragmentKind.ChainIon;
        }

        private static bool IsPositionDiagnostic(LipidFragmentRule rule) {
            return rule.IsPositionDiagnostic;
        }

        private static bool AllObserved(IEnumerable<LipidFragmentMatch> matches, Func<LipidFragmentRule, bool> predicate) {
            return matches.Where(m => predicate(m.Fragment.Rule)).All(m => m.IsObserved);
        }

        private LipidFragmentMatch Match(IReadOnlyList<SpectrumPeak> normalized, LipidFragment fragment) {
            var observed = 0d;
            foreach (var peak in normalized) {
                if (Math.Abs(peak.Mass - fragment.Mz) < _ms2Tolerance && peak.Intensity > observed) {
                    observed = peak.Intensity;
                }
            }
            return new LipidFragmentMatch(fragment, observed, Threshold(fragment.Rule));
        }

        // Theoretical peaks that fall on the same observed peak are summed (e.g. SN1 and SN2 ions of
        // identical chains); theoretical peaks with no observed peak and observed peaks with no
        // theoretical one both count against the similarity.
        private double CalculateSimilarity(IReadOnlyList<SpectrumPeak> observed, IEnumerable<LipidFragment> theoretical) {
            var matched = new double[observed.Count];
            var unmatchedSquares = 0d;
            foreach (var fragment in theoretical) {
                var index = -1;
                for (var i = 0; i < observed.Count; i++) {
                    if (Math.Abs(observed[i].Mass - fragment.Mz) < _ms2Tolerance && (index < 0 || observed[i].Intensity > observed[index].Intensity)) {
                        index = i;
                    }
                }
                if (index >= 0) {
                    matched[index] += fragment.Intensity;
                }
                else {
                    unmatchedSquares += fragment.Intensity * fragment.Intensity;
                }
            }
            var dot = 0d;
            var theoreticalSquares = unmatchedSquares;
            var observedSquares = 0d;
            for (var i = 0; i < observed.Count; i++) {
                dot += matched[i] * observed[i].Intensity;
                theoreticalSquares += matched[i] * matched[i];
                observedSquares += observed[i].Intensity * observed[i].Intensity;
            }
            return theoreticalSquares > 0 && observedSquares > 0 ? dot / Math.Sqrt(theoreticalSquares * observedSquares) : 0d;
        }

        private static double Threshold(LipidFragmentRule rule) {
            if (rule.Kind == LipidFragmentKind.ProhibitedIon) {
                return rule.Intensity / RuleToNormalizedScale;
            }
            return (rule.DiagnosticIonIntensity ?? 0d) / RuleToNormalizedScale;
        }

        private static IReadOnlyList<SpectrumPeak> Normalize(IReadOnlyList<SpectrumPeak> spectrum) {
            var max = spectrum.Count == 0 ? 0d : spectrum.Max(p => p.Intensity);
            if (max <= 0) {
                return Array.Empty<SpectrumPeak>();
            }
            return spectrum.Select(p => new SpectrumPeak(p.Mass, p.Intensity / max * 100d, p.Comment)).ToArray();
        }
    }
}

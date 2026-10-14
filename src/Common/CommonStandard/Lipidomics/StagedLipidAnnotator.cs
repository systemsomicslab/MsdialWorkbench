using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    /// <summary>
    /// One candidate structure evaluated against the observed spectrum.
    /// </summary>
    public sealed class StagedLipidCandidate
    {
        public StagedLipidCandidate(ILipid lipid, ILipidSpectrumModel model, LipidSpectrumRuleSet ruleSet, StagedLipidEvaluation evaluation) {
            Lipid = lipid;
            Model = model;
            RuleSet = ruleSet;
            Evaluation = evaluation;
        }

        public ILipid Lipid { get; }
        public ILipidSpectrumModel Model { get; }
        public LipidSpectrumRuleSet RuleSet { get; }
        public StagedLipidEvaluation Evaluation { get; }
    }

    /// <summary>
    /// The outcome of annotating one spectrum from one seed.
    /// </summary>
    public sealed class StagedLipidAnnotationResult
    {
        public StagedLipidAnnotationResult(ILipid seed, ILipid? lipid, StagedLipidLevel level, StagedLipidStopReason stopReason, StagedLipidCandidate? best, IReadOnlyList<StagedLipidCandidate> candidates) {
            Seed = seed;
            Lipid = lipid;
            Level = level;
            StopReason = stopReason;
            Best = best;
            Candidates = candidates;
        }

        /// <summary>The library entry the candidates were expanded from (species level).</summary>
        public ILipid Seed { get; }

        /// <summary>
        /// The structure at the reached level: the seed for <see cref="StagedLipidLevel.Class"/>, the best
        /// molecular species or sn-positioned candidate above that; null when the class is not confirmed.
        /// </summary>
        public ILipid? Lipid { get; }

        public StagedLipidLevel Level { get; }

        /// <summary>Why the annotation stopped where it did.</summary>
        public StagedLipidStopReason StopReason { get; }

        /// <summary>The candidate the result is based on (for a class-level result, the best one evaluated).</summary>
        public StagedLipidCandidate? Best { get; }

        /// <summary>Every evaluated candidate, best first.</summary>
        public IReadOnlyList<StagedLipidCandidate> Candidates { get; }
    }

    /// <summary>
    /// Annotates an MS/MS spectrum starting from a species-level seed (e.g. "PC 34:1" from the library):
    /// expands it to molecular species, evaluates each with the generated spectrum model, and, when the
    /// rule set has position-diagnostic ions, tries the sn orders of the best molecular species.
    /// Candidates reaching the same level are ranked by spectral similarity; a tie between different
    /// structures falls back to the previous level.
    /// </summary>
    public sealed class StagedLipidAnnotator
    {
        // Similarities closer than this are a tie.
        private const double SimilarityTolerance = 1e-6;

        public const int DefaultMaxCandidates = 10000;

        private readonly IReadOnlyList<ILipidSpectrumModel> _models;
        private readonly ILipidGenerator _generator;
        private readonly StagedLipidSpectrumEvaluator _evaluator;
        private readonly int _maxCandidates;

        public StagedLipidAnnotator(IEnumerable<ILipidSpectrumModel> models, ILipidGenerator generator, StagedLipidSpectrumEvaluator evaluator, int maxCandidates = DefaultMaxCandidates) {
            _models = models?.ToArray() ?? throw new ArgumentNullException(nameof(models));
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            if (maxCandidates < 1) {
                throw new ArgumentOutOfRangeException(nameof(maxCandidates));
            }
            _maxCandidates = maxCandidates;
        }

        /// <summary>
        /// The chain expansion used unless another is given: chains of 8 carbons or more, as for EAD.
        /// </summary>
        public static ILipidGenerator CreateDefaultGenerator() {
            return new LipidGenerator(new TotalChainVariationGenerator(minLength: 8));
        }

        public StagedLipidAnnotationResult Annotate(ILipid seed, AdductIon adduct, IReadOnlyList<SpectrumPeak> spectrum) {
            var molecularSpecies = ExpandToMolecularSpecies(seed).Take(_maxCandidates + 1).ToList();
            if (molecularSpecies.Count > _maxCandidates) {
                // Too many to rank: check the class with one candidate only. (Class ions seldom involve chain
                // masses; where they do, this may miss a class another expansion would confirm.)
                var sample = Rank(EvaluateWithPlasmalogenVariant(molecularSpecies[0], adduct, spectrum));
                var classConfirmed = sample.Count > 0 && sample[0].Evaluation.Level >= StagedLipidLevel.Class;
                return classConfirmed
                    ? new StagedLipidAnnotationResult(seed, seed, StagedLipidLevel.Class, StagedLipidStopReason.TooManyCandidates, sample[0], sample)
                    : new StagedLipidAnnotationResult(seed, null, StagedLipidLevel.None, sample.Count > 0 ? sample[0].Evaluation.StopReason : StagedLipidStopReason.NotAnnotatable, sample.FirstOrDefault(), sample);
            }

            var candidates = Rank(molecularSpecies.SelectMany(lipid => EvaluateWithPlasmalogenVariant(lipid, adduct, spectrum)));
            if (candidates.Count == 0) {
                return new StagedLipidAnnotationResult(seed, null, StagedLipidLevel.None, StagedLipidStopReason.NotAnnotatable, null, candidates);
            }

            var best = candidates[0];
            switch (best.Evaluation.Level) {
                case StagedLipidLevel.None:
                    return new StagedLipidAnnotationResult(seed, null, StagedLipidLevel.None, best.Evaluation.StopReason, best, candidates);
                case StagedLipidLevel.Class:
                    return new StagedLipidAnnotationResult(seed, seed, StagedLipidLevel.Class, best.Evaluation.StopReason, best, candidates);
            }
            if (IsTied(candidates, best)) {
                return new StagedLipidAnnotationResult(seed, seed, StagedLipidLevel.Class, StagedLipidStopReason.MolecularSpeciesAmbiguous, best, candidates);
            }
            if (!best.RuleSet.Rules.Any(r => r.IsPositionDiagnostic)) {
                return new StagedLipidAnnotationResult(seed, best.Lipid, StagedLipidLevel.MolecularSpecies, StagedLipidStopReason.NoPositionDiagnosticIon, best, candidates);
            }

            var positioned = Rank(ExpandToPositions(best.Lipid).SelectMany(lipid => Evaluate(lipid, adduct, spectrum)))
                .Where(c => c.Evaluation.Level == StagedLipidLevel.Position)
                .ToList();
            var all = positioned.Concat(candidates).ToList();
            if (positioned.Count == 0) {
                return new StagedLipidAnnotationResult(seed, best.Lipid, StagedLipidLevel.MolecularSpecies, StagedLipidStopReason.PositionDiagnosticIonMissing, best, all);
            }
            if (IsTied(positioned, positioned[0])) {
                return new StagedLipidAnnotationResult(seed, best.Lipid, StagedLipidLevel.MolecularSpecies, StagedLipidStopReason.PositionAmbiguous, best, all);
            }
            return new StagedLipidAnnotationResult(seed, positioned[0].Lipid, StagedLipidLevel.Position, StagedLipidStopReason.Resolved, positioned[0], all);
        }

        private static List<StagedLipidCandidate> Rank(IEnumerable<StagedLipidCandidate> candidates) {
            return candidates
                .OrderByDescending(c => c.Evaluation.Level)
                .ThenByDescending(c => c.Evaluation.Similarity)
                .ToList();
        }

        // Another structure at the same level explains the spectrum as well as the best one.
        private static bool IsTied(IReadOnlyList<StagedLipidCandidate> ranked, StagedLipidCandidate best) {
            return ranked.Any(c => c != best
                && c.Evaluation.Level == best.Evaluation.Level
                && Math.Abs(c.Evaluation.Similarity - best.Evaluation.Similarity) < SimilarityTolerance
                && c.Lipid.Name != best.Lipid.Name);
        }

        // The plasmalogen form only matters where the XML has its own "_P" rule set; with a shared rule
        // set both forms generate the same spectrum and would only tie.
        private IEnumerable<StagedLipidCandidate> EvaluateWithPlasmalogenVariant(ILipid lipid, AdductIon adduct, IReadOnlyList<SpectrumPeak> spectrum) {
            var original = Evaluate(lipid, adduct, spectrum).ToList();
            var plasmalogen = PlasmalogenVariant(lipid);
            if (plasmalogen is null) {
                return original;
            }
            var variant = Evaluate(plasmalogen, adduct, spectrum).Where(v => original.All(o => o.Model != v.Model));
            return original.Concat(variant);
        }

        private IEnumerable<StagedLipidCandidate> Evaluate(ILipid lipid, AdductIon adduct, IReadOnlyList<SpectrumPeak> spectrum) {
            if (!(lipid is Lipid concrete)) {
                yield break;
            }
            var model = LipidSpectrumModelSelector.Select(_models, lipid, adduct);
            var ruleSet = model?.FindRuleSet(adduct.AdductIonName);
            if (model is null || ruleSet is null) {
                yield break;
            }
            IReadOnlyList<LipidFragment>? fragments;
            try {
                fragments = model.GenerateFragments(concrete, adduct);
            }
            catch (Exception) {
                // The generated code indexes chains directly; a chain shape it doesn't expect is not a candidate.
                yield break;
            }
            if (fragments is null) {
                yield break;
            }
            var hasChains = lipid.AnnotationLevel >= 2 && lipid.Chains.GetDeterminedChains().Length > 0;
            yield return new StagedLipidCandidate(lipid, model, ruleSet, _evaluator.Evaluate(spectrum, ruleSet.Resolution, fragments, hasChains));
        }

        private IEnumerable<ILipid> ExpandToMolecularSpecies(ILipid seed) {
            IEnumerable<ILipid> lipids;
            if (seed.AnnotationLevel >= 2 || !_generator.CanGenerate(seed)) {
                lipids = new[] { seed };
            }
            else {
                var expanded = _generator.Generate(seed).ToList();
                lipids = expanded.Count > 0 ? expanded : new List<ILipid> { seed };
            }
            return lipids;
        }

        private IEnumerable<ILipid> ExpandToPositions(ILipid molecularSpecies) {
            if (molecularSpecies.Chains is PositionLevelChains) {
                return new[] { molecularSpecies };
            }
            return _generator.CanGenerate(molecularSpecies) ? _generator.Generate(molecularSpecies) : Enumerable.Empty<ILipid>();
        }

        // A total-chain seed can't say O- (alkyl ether) from P- (plasmalogen, a Δ1 double bond): the
        // expanded alkyl chains have undecided double bonds. Returns the plasmalogen form, if there is one,
        // so the "_P" rule sets get evaluated too.
        private static ILipid? PlasmalogenVariant(ILipid lipid) {
            var chains = lipid.Chains.GetDeterminedChains();
            if (chains.Length == 0 || !chains.OfType<AlkylChain>().Any(CanBePlasmalogen)) {
                return null;
            }
            var converted = chains.Select(c => c is AlkylChain alkyl && CanBePlasmalogen(alkyl)
                ? new AlkylChain(alkyl.CarbonCount, alkyl.DoubleBond.Decide(DoubleBondInfo.Create(1)), alkyl.Oxidized)
                : c).ToArray();
            ITotalChain? total = lipid.Chains is PositionLevelChains ? new PositionLevelChains(converted)
                : lipid.Chains is MolecularSpeciesLevelChains ? new MolecularSpeciesLevelChains(converted)
                : null;
            return total is null ? null : new Lipid(lipid.LipidClass, lipid.Mass, total);
        }

        private static bool CanBePlasmalogen(AlkylChain chain) {
            return !chain.IsPlasmalogen && chain.DoubleBond.UnDecidedCount > 0;
        }
    }
}

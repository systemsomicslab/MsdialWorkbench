using CompMs.Common.Algorithm.Function;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Algorithm.Scoring
{
    /// <summary>A similarity and the number of peak pairs it was computed from.</summary>
    public readonly struct SimilarityScore
    {
        public SimilarityScore(double score, int matched) {
            Score = score;
            Matched = matched;
        }

        public double Score { get; }
        public int Matched { get; }
    }

    /// <summary>
    /// A molecular-networking similarity: its preprocessing and its score, named by a kernel identity
    /// (<see cref="KernelIdentity"/>). Implementations never write to the spectra they are given.
    /// </summary>
    public interface IMsmsSimilarityKernel
    {
        /// <summary>
        /// The canonical kernel identity, or <see cref="KernelIdentity.UnregisteredSimpleDot"/> for the
        /// one kernel that has none (<see cref="IsRegistered"/> is false).
        /// </summary>
        string Identity { get; }

        /// <summary>Whether <see cref="Identity"/> is a registered kernel identity.</summary>
        bool IsRegistered { get; }

        /// <summary>The spectrum this kernel scores, as a new list.</summary>
        List<SpectrumPeak> Preprocess(IReadOnlyList<SpectrumPeak> peaks, double precursorMz);

        /// <summary>Score two spectra that <see cref="Preprocess"/> produced.</summary>
        SimilarityScore Score(IMSScanProperty a, IMSScanProperty b);
    }

    /// <summary>
    /// The preprocessing of molecular networking, in the order of the shared kernel definition:
    /// refine, precursor window, (top-k), frame, normalise.
    /// </summary>
    /// <remarks>
    /// With <see cref="LegacyDefaults"/> (and the method file's abundance cutoffs) it is exactly
    /// <see cref="MsScanMatching.GetProcessedSpectrum"/> as the networking callers use it: the same calls
    /// in the same order. Top-k is not implemented here.
    /// </remarks>
    public sealed class MnPreprocessing
    {
        private MnPreprocessing() {
            IsNone = true;
        }

        /// <summary><c>prep=none</c>: the peaks stably sorted by m/z and nothing else.</summary>
        public static MnPreprocessing None { get; } = new MnPreprocessing();

        /// <summary>Whether this is <see cref="None"/>.</summary>
        public bool IsNone { get; }

        public MnPreprocessing(
            double relativeAbundanceCutOff, double absoluteAbundanceCutOff, double? above,
            double minMz, double maxMz, double frame, double power, double scale,
            double precursorWindow, int topK, double topKWindow) {
            if (topK != 0) {
                throw new NotSupportedException("Top-k peak filtering is not implemented in MS-DIAL.");
            }
            RelativeAbundanceCutOff = relativeAbundanceCutOff;
            AbsoluteAbundanceCutOff = absoluteAbundanceCutOff;
            Above = above;
            MinMz = minMz;
            MaxMz = maxMz;
            Frame = frame;
            Power = power;
            Scale = scale;
            PrecursorWindow = precursorWindow;
            TopK = topK;
            TopKWindow = topKWindow;
        }

        /// <summary>
        /// GetProcessedSpectrum's defaults as the MN callers use them: above-precursor margin 0.05 Da
        /// (not MnMassTolerance), 1-Da frames, intensity^0.5 scaled to 100, relative cutoff 1%,
        /// absolute cutoff 0.
        /// </summary>
        public static MnPreprocessing LegacyDefaults { get; } = Legacy(relativeAbundanceCutOff: 1d, absoluteAbundanceCutOff: 0d);

        public static MnPreprocessing Legacy(double relativeAbundanceCutOff, double absoluteAbundanceCutOff) {
            return new MnPreprocessing(relativeAbundanceCutOff, absoluteAbundanceCutOff, above: 0.05,
                minMz: 0d, maxMz: 10000d, frame: 1d, power: 0.5, scale: 100d, precursorWindow: 0d, topK: 0, topKWindow: 50d);
        }

        public double RelativeAbundanceCutOff { get; }
        public double AbsoluteAbundanceCutOff { get; }
        /// <summary>Peaks above precursor + this are dropped; null keeps them.</summary>
        public double? Above { get; }
        public double MinMz { get; }
        public double MaxMz { get; }
        /// <summary>The frame width of the one-peak-per-frame binning; 0 disables it.</summary>
        public double Frame { get; }
        public double Power { get; }
        public double Scale { get; }
        /// <summary>Peaks with |m/z − precursor| ≤ this are dropped; 0 disables it.</summary>
        public double PrecursorWindow { get; }
        public int TopK { get; }
        public double TopKWindow { get; }

        public List<SpectrumPeak> Apply(IReadOnlyList<SpectrumPeak> peaks, double precursorMz) {
            if (IsNone) {
                return SortOnly(peaks);
            }
            var list = peaks as List<SpectrumPeak> ?? peaks.ToList();
            // Refine: the base peak is taken over all input peaks; the survivors are new objects in input order.
            var refined = SpectrumHandler.GetRefinedPeaklist(list, RelativeAbundanceCutOff, AbsoluteAbundanceCutOff, MinMz, MaxMz,
                precursorMz, Above ?? 0d, MassToleranceType.Da, 1, removeAfterPrecursor: Above.HasValue);
            if (PrecursorWindow > 0d) {
                refined = refined.Where(peak => !(Math.Abs(peak.Mass - precursorMz) <= PrecursorWindow)).ToList();
            }
            if (Frame > 0d) {
                refined = SpectrumHandler.GetBinnedSpectrum(refined, Frame);
            }
            return SpectrumHandler.GetNormalizedPeaks(refined, Power, Scale);
        }

        public IEnumerable<KeyValuePair<string, string>> ToParameters() {
            if (IsNone) {
                yield return Pair("prep", "none");
                yield break;
            }
            yield return Pair("prep", "mn");
            yield return Pair("prep.abs", KernelIdentity.FormatNumber(AbsoluteAbundanceCutOff));
            yield return Pair("prep.above", Above.HasValue ? KernelIdentity.FormatNumber(Above.Value) : "none");
            yield return Pair("prep.frame", KernelIdentity.FormatNumber(Frame));
            yield return Pair("prep.maxmz", KernelIdentity.FormatNumber(MaxMz));
            yield return Pair("prep.minmz", KernelIdentity.FormatNumber(MinMz));
            yield return Pair("prep.pow", KernelIdentity.FormatNumber(Power));
            yield return Pair("prep.precwin", KernelIdentity.FormatNumber(PrecursorWindow));
            yield return Pair("prep.rel", KernelIdentity.FormatNumber(RelativeAbundanceCutOff));
            yield return Pair("prep.scale", KernelIdentity.FormatNumber(Scale));
            yield return Pair("prep.topk", KernelIdentity.FormatNumber(TopK));
            yield return Pair("prep.topkwin", KernelIdentity.FormatNumber(TopKWindow));
        }

        /// <summary><c>prep=none</c>: the peaks as new objects, stably sorted by m/z, nothing else.</summary>
        public static List<SpectrumPeak> SortOnly(IReadOnlyList<SpectrumPeak> peaks) {
            return peaks.OrderBy(peak => peak.Mass).Select(peak => new SpectrumPeak(peak.Mass, peak.Intensity)).ToList();
        }

        internal static KeyValuePair<string, string> Pair(string key, string value) => new KeyValuePair<string, string>(key, value);
    }

    public abstract class LegacyKernelBase : IMsmsSimilarityKernel
    {
        protected LegacyKernelBase(string baseName, double tolerance, MnPreprocessing preprocessing) {
            Tolerance = tolerance;
            Preprocessing = preprocessing;
            Identity = KernelIdentity.Format(baseName, preprocessing.ToParameters(), new[] { MnPreprocessing.Pair("tol", KernelIdentity.FormatNumber(tolerance)) });
        }

        public double Tolerance { get; }
        public MnPreprocessing Preprocessing { get; }
        public string Identity { get; protected set; }
        public virtual bool IsRegistered => true;

        public List<SpectrumPeak> Preprocess(IReadOnlyList<SpectrumPeak> peaks, double precursorMz) => Preprocessing.Apply(peaks, precursorMz);

        public SimilarityScore Score(IMSScanProperty a, IMSScanProperty b) {
            var items = ScoreItems(a, b);
            return new SimilarityScore(items[0], (int)items[1]);
        }

        /// <summary>The kernel's score items as MS-DIAL has always returned them: <c>[score, matched]</c>.</summary>
        public abstract double[] ScoreItems(IMSScanProperty a, IMSScanProperty b);
    }

    /// <summary><c>msdial.moddot.legacy</c>: <see cref="MsScanMatching.GetModifiedDotProductScore"/>, matched peaks only.</summary>
    public sealed class LegacyModDotKernel : LegacyKernelBase
    {
        public LegacyModDotKernel(double tolerance, MnPreprocessing preprocessing) : base(KernelIdentity.ModDotLegacy, tolerance, preprocessing) { }
        public override double[] ScoreItems(IMSScanProperty a, IMSScanProperty b) => MsScanMatching.GetModifiedDotProductScore(a, b, Tolerance);
    }

    /// <summary><c>msdial.bonanza.v1</c>: <see cref="MsScanMatching.GetBonanzaScore"/>.</summary>
    public sealed class BonanzaKernel : LegacyKernelBase
    {
        public BonanzaKernel(double tolerance, MnPreprocessing preprocessing) : base(KernelIdentity.Bonanza, tolerance, preprocessing) { }
        public override double[] ScoreItems(IMSScanProperty a, IMSScanProperty b) => MsScanMatching.GetBonanzaScore(a, b, Tolerance);
    }

    /// <summary>
    /// <c>msdial.cosine_matched.legacy</c>: the fourth value of
    /// <see cref="MsScanMatching.GetBonanzaModifiedDotCosineScores"/>, a cosine over the product-ion
    /// pairs with matched-only norms. Its matched count is the number of product-ion pairs.
    /// </summary>
    public sealed class CosineMatchedLegacyKernel : LegacyKernelBase
    {
        public CosineMatchedLegacyKernel(double tolerance, MnPreprocessing preprocessing) : base(KernelIdentity.CosineMatchedLegacy, tolerance, preprocessing) { }

        public override double[] ScoreItems(IMSScanProperty a, IMSScanProperty b) {
            var all = MsScanMatching.GetBonanzaModifiedDotCosineScores(a, b, Tolerance);
            var products = all[1] == 0d ? 0 : CountProductIonPairs(a, b);
            return new[] { all[3], products };
        }

        private int CountProductIonPairs(IMSScanProperty a, IMSScanProperty b) {
            var match = a.PrecursorMz < b.PrecursorMz
                ? MsScanMatching.MatchLegacy(a.Spectrum, a.PrecursorMz, b.Spectrum, b.PrecursorMz, Tolerance, MassToleranceType.Da)
                : MsScanMatching.MatchLegacy(b.Spectrum, b.PrecursorMz, a.Spectrum, a.PrecursorMz, Tolerance, MassToleranceType.Da);
            return match.Pairs.Count(pair => pair.IsProductIonMatched);
        }
    }

    /// <summary>
    /// MS-DIAL's "Cosine" mode, <see cref="MsScanMatching.GetCosineScore"/>: a squared simple dot product
    /// over 0..min(precursor). It has no registered identity.
    /// </summary>
    public sealed class SimpleDotCosineKernel : LegacyKernelBase
    {
        public SimpleDotCosineKernel(double tolerance, MnPreprocessing preprocessing) : base(KernelIdentity.ModDotLegacy, tolerance, preprocessing) {
            Identity = KernelIdentity.UnregisteredSimpleDot;
        }

        public override bool IsRegistered => false;
        public override double[] ScoreItems(IMSScanProperty a, IMSScanProperty b) => MsScanMatching.GetCosineScore(a, b, Tolerance);
    }

    /// <summary>
    /// <c>gnps.modified_cosine</c> with greedy assignment: the GNPS-normalised modified cosine, defined
    /// to reproduce matchms 0.34.0 <c>ModifiedCosineGreedy</c>.
    /// </summary>
    /// <remarks>
    /// Unlike <c>msdial.moddot.legacy</c>, the denominator is the norm of the whole spectra, so unmatched
    /// intensity lowers the score, and the tolerance is inclusive. The intensity power is applied in the
    /// preprocessing (<c>prep.pow</c>), so the kernel's own powers are <c>ipow=1</c>, <c>mzpow=0</c>.
    /// </remarks>
    public sealed class ModifiedCosineKernel : IMsmsSimilarityKernel
    {
        public ModifiedCosineKernel(double tolerance, MnPreprocessing preprocessing) : this(tolerance, preprocessing, mzPower: 0d, intensityPower: 1d) { }

        /// <param name="mzPower">mzpow; MS-DIAL uses 0.</param>
        /// <param name="intensityPower">ipow; MS-DIAL uses 1 and puts its intensity power in the preprocessing.</param>
        public ModifiedCosineKernel(double tolerance, MnPreprocessing preprocessing, double mzPower, double intensityPower) {
            Tolerance = tolerance;
            Preprocessing = preprocessing;
            MzPower = mzPower;
            IntensityPower = intensityPower;
            Identity = KernelIdentity.Format(KernelIdentity.ModifiedCosine, preprocessing.ToParameters(), new[] {
                MnPreprocessing.Pair("assign", "greedy"),
                MnPreprocessing.Pair("ipow", KernelIdentity.FormatNumber(intensityPower)),
                MnPreprocessing.Pair("mzpow", KernelIdentity.FormatNumber(mzPower)),
                MnPreprocessing.Pair("tol", KernelIdentity.FormatNumber(tolerance)),
            });
        }

        public double MzPower { get; }
        public double IntensityPower { get; }

        /// <summary>
        /// MS-DIAL's networking preprocessing for this kernel: the method file's abundance cutoffs, the
        /// above-precursor margin equal to the mass tolerance, the precursor window, no framing, and the
        /// intensity power, scaled to 100.
        /// </summary>
        public static MnPreprocessing CreatePreprocessing(double relativeAbundanceCutOff, double absoluteAbundanceCutOff, double tolerance, double precursorWindow, double intensityPower) {
            return new MnPreprocessing(relativeAbundanceCutOff, absoluteAbundanceCutOff, above: tolerance,
                minMz: 0d, maxMz: 10000d, frame: 0d, power: intensityPower, scale: 100d,
                precursorWindow: precursorWindow, topK: 0, topKWindow: 50d);
        }

        public double Tolerance { get; }
        public MnPreprocessing Preprocessing { get; }
        public string Identity { get; }
        public bool IsRegistered => true;

        public List<SpectrumPeak> Preprocess(IReadOnlyList<SpectrumPeak> peaks, double precursorMz) => Preprocessing.Apply(peaks, precursorMz);

        public SimilarityScore Score(IMSScanProperty a, IMSScanProperty b) {
            return Compute(a.Spectrum, a.PrecursorMz, b.Spectrum, b.PrecursorMz, Tolerance, MzPower, IntensityPower);
        }

        /// <summary>
        /// The greedy modified cosine of two spectra as they are (no preprocessing).
        /// </summary>
        /// <remarks>
        /// <list type="number">
        /// <item>Each spectrum is stably sorted by m/z; shift = precursorA − precursorB.</item>
        /// <item>Direct candidates: for each i ascending, each j ascending with
        /// mB_j &gt;= mA_i − tol and mB_j &lt;= mA_i + tol (inclusive).</item>
        /// <item>Shifted candidates: the same with mB_j + shift; none when |shift| ≤ tol.</item>
        /// <item>The list is direct then shifted, not de-duplicated. w = (mA^mzp·IA^ip)·(mB^mzp·IB^ip).</item>
        /// <item>Stable ascending sort by w, then reversed (so on equal w the later candidate comes first);
        /// a pair is accepted when both its peaks are still unused.</item>
        /// <item>score = Σ accepted w / (√Σ_all (mA^mzp·IA^ip)² · √Σ_all (mB^mzp·IB^ip)²); 0 when a norm is 0.</item>
        /// </list>
        /// </remarks>
        public static SimilarityScore Compute(IReadOnlyList<SpectrumPeak> a, double precursorA, IReadOnlyList<SpectrumPeak> b, double precursorB, double tolerance, double mzPower, double intensityPower) {
            var sortedA = a.OrderBy(peak => peak.Mass).ToArray();
            var sortedB = b.OrderBy(peak => peak.Mass).ToArray();
            var mzA = sortedA.Select(peak => peak.Mass).ToArray();
            var mzB = sortedB.Select(peak => peak.Mass).ToArray();
            var powerA = sortedA.Select(peak => Math.Pow(peak.Mass, mzPower) * Math.Pow(peak.Intensity, intensityPower)).ToArray();
            var powerB = sortedB.Select(peak => Math.Pow(peak.Mass, mzPower) * Math.Pow(peak.Intensity, intensityPower)).ToArray();

            var candidates = new List<(int I, int J, double W)>();
            AddCandidates(candidates, mzA, mzB, 0d, tolerance, powerA, powerB);
            var shift = precursorA - precursorB;
            if (Math.Abs(shift) > tolerance) {
                AddCandidates(candidates, mzA, mzB, shift, tolerance, powerA, powerB);
            }

            var usedA = new bool[sortedA.Length];
            var usedB = new bool[sortedB.Length];
            var score = 0d;
            var matched = 0;
            // OrderBy is a stable sort; reversing it puts later candidates first among equal weights.
            foreach (var (i, j, w) in candidates.OrderBy(candidate => candidate.W).Reverse()) {
                if (usedA[i] || usedB[j]) continue;
                usedA[i] = true;
                usedB[j] = true;
                score += w;
                matched++;
            }

            var normA = Math.Sqrt(SumOfSquares(powerA));
            var normB = Math.Sqrt(SumOfSquares(powerB));
            if (normA == 0d || normB == 0d) {
                return new SimilarityScore(0d, matched);
            }
            return new SimilarityScore(score / (normA * normB), matched);
        }

        private static void AddCandidates(List<(int I, int J, double W)> candidates, double[] mzA, double[] mzB, double shift, double tolerance, double[] powerA, double[] powerB) {
            for (int i = 0; i < mzA.Length; i++) {
                var low = mzA[i] - tolerance;
                var high = mzA[i] + tolerance;
                for (int j = 0; j < mzB.Length; j++) {
                    var shifted = mzB[j] + shift;
                    if (shifted >= low && shifted <= high) {
                        candidates.Add((i, j, powerA[i] * powerB[j]));
                    }
                }
            }
        }

        private static double SumOfSquares(double[] values) {
            var sum = 0d;
            foreach (var value in values) {
                sum += value * value;
            }
            return sum;
        }
    }

    /// <summary>
    /// Creates the molecular-networking kernels from MS-DIAL's settings.
    /// </summary>
    public static class MsmsSimilarityKernelFactory
    {
        /// <summary>
        /// The kernel of one setting. <see cref="MsmsSimilarityCalc.All"/> is three kernels; use
        /// <see cref="CreateMany"/>.
        /// </summary>
        public static IMsmsSimilarityKernel Create(MsmsSimilarityCalc calc, MolecularNetworkingQuery query) {
            var legacy = MnPreprocessing.Legacy(query.RelativeAbundanceCutOff, query.AbsoluteAbundanceCutOff);
            switch (calc) {
                case MsmsSimilarityCalc.ModDotLegacy:
                    return new LegacyModDotKernel(query.MassTolerance, legacy);
                case MsmsSimilarityCalc.Bonanza:
                    return new BonanzaKernel(query.MassTolerance, legacy);
                case MsmsSimilarityCalc.Cosine:
                    return new SimpleDotCosineKernel(query.MassTolerance, legacy);
                case MsmsSimilarityCalc.ModifiedCosine:
                    return new ModifiedCosineKernel(query.MassTolerance, ModifiedCosineKernel.CreatePreprocessing(
                        query.RelativeAbundanceCutOff, query.AbsoluteAbundanceCutOff, query.MassTolerance, query.PrecursorWindow, query.IntensityPower));
                case MsmsSimilarityCalc.All:
                    throw new ArgumentException("MsmsSimilarityCalc.All is three kernels; use CreateMany.", nameof(calc));
                default:
                    throw new ArgumentOutOfRangeException(nameof(calc), calc, "Unknown molecular-networking similarity.");
            }
        }

        /// <summary>
        /// The kernel an identity names, for the identities this assembly computes: the three msdial
        /// bases and <c>gnps.modified_cosine</c> with <c>assign=greedy</c>, each with <c>prep=none</c> or
        /// <c>prep=mn</c> without top-k.
        /// </summary>
        /// <exception cref="NotSupportedException">A base, assignment or preprocessing this assembly does not compute.</exception>
        /// <exception cref="FormatException">A malformed identity of a base it does compute.</exception>
        public static IMsmsSimilarityKernel FromIdentity(string identity) {
            var question = identity?.IndexOf('?') ?? -1;
            var baseName = question < 0 ? identity : identity.Substring(0, question);
            if (!KernelIdentity.IsKnownBase(baseName)) {
                throw new NotSupportedException($"MS-DIAL does not compute the kernel base '{baseName}'.");
            }
            var parsed = KernelIdentity.Parse(identity);
            if (parsed.Text("prep") == "mn" && parsed.Number("prep.topk") != 0d) {
                throw new NotSupportedException("Top-k peak filtering (prep.topk > 0) is not implemented in MS-DIAL.");
            }
            var preprocessing = parsed.Preprocessing() ?? MnPreprocessing.None;
            var tolerance = parsed.Number("tol");
            switch (parsed.Base) {
                case KernelIdentity.ModDotLegacy:
                    return new LegacyModDotKernel(tolerance, preprocessing);
                case KernelIdentity.Bonanza:
                    return new BonanzaKernel(tolerance, preprocessing);
                case KernelIdentity.CosineMatchedLegacy:
                    return new CosineMatchedLegacyKernel(tolerance, preprocessing);
                case KernelIdentity.ModifiedCosine:
                    if (parsed.Text("assign") != "greedy") {
                        throw new NotSupportedException("Only the greedy assignment of the modified cosine is implemented in MS-DIAL.");
                    }
                    return new ModifiedCosineKernel(tolerance, preprocessing, parsed.Number("mzpow"), parsed.Number("ipow"));
                default:
                    throw new NotSupportedException($"MS-DIAL does not compute the kernel base '{parsed.Base}'.");
            }
        }

        /// <summary>
        /// The kernels of several settings, in order, with All expanded to Bonanza, the matched-only
        /// modified dot product and the matched-only product-ion cosine. A kernel already listed is not
        /// repeated.
        /// </summary>
        public static List<IMsmsSimilarityKernel> CreateMany(IEnumerable<MsmsSimilarityCalc> calcs, MolecularNetworkingQuery query) {
            var kernels = new List<IMsmsSimilarityKernel>();
            foreach (var calc in calcs) {
                if (calc == MsmsSimilarityCalc.All) {
                    var legacy = MnPreprocessing.Legacy(query.RelativeAbundanceCutOff, query.AbsoluteAbundanceCutOff);
                    kernels.Add(new BonanzaKernel(query.MassTolerance, legacy));
                    kernels.Add(new LegacyModDotKernel(query.MassTolerance, legacy));
                    kernels.Add(new CosineMatchedLegacyKernel(query.MassTolerance, legacy));
                }
                else {
                    kernels.Add(Create(calc, query));
                }
            }
            return kernels.GroupBy(kernel => kernel.Identity).Select(group => group.First()).ToList();
        }

        /// <summary>
        /// The preprocessing of one setting; All uses the legacy preprocessing its three kernels share.
        /// </summary>
        public static Func<IReadOnlyList<SpectrumPeak>, double, List<SpectrumPeak>> CreatePreprocessor(MsmsSimilarityCalc calc, MolecularNetworkingQuery query) {
            var kernel = calc == MsmsSimilarityCalc.All ? Create(MsmsSimilarityCalc.Bonanza, query) : Create(calc, query);
            return kernel.Preprocess;
        }

        /// <summary>
        /// The score items networking has always used: <c>[score, matched]</c>, and for All
        /// <c>[bonanza, matched, moddot, cosine]</c>.
        /// </summary>
        public static double[] ScoreItems(MsmsSimilarityCalc calc, IMSScanProperty a, IMSScanProperty b, double massTolerance) {
            switch (calc) {
                case MsmsSimilarityCalc.ModDotLegacy:
                    return MsScanMatching.GetModifiedDotProductScore(a, b, massTolerance);
                case MsmsSimilarityCalc.Bonanza:
                    return MsScanMatching.GetBonanzaScore(a, b, massTolerance);
                case MsmsSimilarityCalc.Cosine:
                    return MsScanMatching.GetCosineScore(a, b, massTolerance);
                case MsmsSimilarityCalc.All:
                    return MsScanMatching.GetBonanzaModifiedDotCosineScores(a, b, massTolerance);
                case MsmsSimilarityCalc.ModifiedCosine: {
                        var score = ModifiedCosineKernel.Compute(a.Spectrum, a.PrecursorMz, b.Spectrum, b.PrecursorMz, massTolerance, mzPower: 0d, intensityPower: 1d);
                        return new[] { score.Score, score.Matched };
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(calc), calc, "Unknown molecular-networking similarity.");
            }
        }

        /// <summary>
        /// The identity written on a spectral edge: the kernel's, and for All the Bonanza identity,
        /// because All's edge score is its Bonanza value.
        /// </summary>
        public static string EdgeIdentity(MsmsSimilarityCalc calc, MolecularNetworkingQuery query) {
            return (calc == MsmsSimilarityCalc.All ? Create(MsmsSimilarityCalc.Bonanza, query) : Create(calc, query)).Identity;
        }
    }
}

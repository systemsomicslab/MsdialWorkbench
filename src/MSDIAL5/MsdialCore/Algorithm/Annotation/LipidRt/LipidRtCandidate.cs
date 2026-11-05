using CompMs.Common.Enum;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt
{
    /// <summary>
    /// One annotated peak or alignment spot of a unit, as the lipid retention-time order check reads it (SPEC 1.2).
    /// </summary>
    /// <remarks>
    /// Every annotated object of the unit is a candidate and receives an outcome; only the eligible ones
    /// (<see cref="IsEligible"/>, SPEC 2.1) are evaluated, the others are NotEvaluable(Ineligible). An annotated object
    /// that is not eligible still takes part in the MS1 artefact layer as a possible brighter partner, with its
    /// composition when its name is a lipid.
    ///
    /// <see cref="Id"/> is a key and the last tie-breaker, never evidence.
    /// </remarks>
    public sealed class LipidRtCandidate
    {
        /// <summary>MasterPeakID or MasterAlignmentID.</summary>
        public int Id { get; set; }

        /// <summary>Retention time (min) on the unit's own axis.</summary>
        public double Rt { get; set; }

        /// <summary>Precursor m/z.</summary>
        public double Mz { get; set; }

        /// <summary>Peak height (top, or the alignment average). Only ratios and the class median of log10 are used.</summary>
        public double Height { get; set; }

        /// <summary>The adduct, e.g. "[M+NH4]+".</summary>
        public string Adduct { get; set; } = string.Empty;

        /// <summary>The species-level name (left of "|"), e.g. "PC 34:1".</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>The chain-level name, or empty when the chains are not resolved.</summary>
        public string Molecular { get; set; } = string.Empty;

        /// <summary>The Ontology (LbmClass name).</summary>
        public string Ontology { get; set; } = string.Empty;

        /// <summary>The Ontology, except "BMP" for BMP-named rows exported with Ontology PG.</summary>
        public string ClassKey { get; set; } = string.Empty;

        /// <summary>Species total carbons.</summary>
        public int Carbon { get; set; }

        /// <summary>Species total double bonds, P- counted as O- plus one.</summary>
        public int DoubleBondEquivalent { get; set; }

        /// <summary>Species total extra oxygens.</summary>
        public int Oxygen { get; set; }

        /// <summary>True when the name is a lipid of a lipid Ontology and has a parsed composition with carbons.</summary>
        public bool IsLipid { get; set; }

        /// <summary>An isotope-labelled name: an internal standard.</summary>
        public bool IsLabelled { get; set; }

        /// <summary>The representative is an MS/MS match (annotation tag 3xx/4xx).</summary>
        public bool IsMsmsAnnotated { get; set; }

        public LipidRtTagLevel TagLevel { get; set; }

        /// <summary>The representative's weighted dot product, 0..1.</summary>
        public double WeightedDot { get; set; }

        /// <summary>The chains with roles, for the plausibility profile.</summary>
        public IReadOnlyList<LipidRtChain> Chains { get; set; } = Array.Empty<LipidRtChain>();

        public bool IsEligible { get; set; }

        /// <summary>Why the candidate is not eligible, e.g. "not_ref_matched,tag_520"; empty when eligible.</summary>
        public string IneligibleReason { get; set; } = string.Empty;

        /// <summary>The composition key (class key, C, DBe, O), or null when the candidate has none.</summary>
        public LipidRtComposition Composition => IsLipid && Carbon > 0
            ? new LipidRtComposition(string.IsNullOrEmpty(ClassKey) ? Ontology : ClassKey, Carbon, DoubleBondEquivalent, Oxygen)
            : null;
    }

    /// <summary>The species-level composition: class key, total carbons, double-bond equivalents and extra oxygens.</summary>
    public sealed class LipidRtComposition : IEquatable<LipidRtComposition>, IComparable<LipidRtComposition>
    {
        public LipidRtComposition(string classKey, int carbon, int doubleBondEquivalent, int oxygen) {
            ClassKey = classKey ?? string.Empty;
            Carbon = carbon;
            DoubleBondEquivalent = doubleBondEquivalent;
            Oxygen = oxygen;
        }

        public string ClassKey { get; }
        public int Carbon { get; }
        public int DoubleBondEquivalent { get; }
        public int Oxygen { get; }

        public int CompareTo(LipidRtComposition other) {
            var c = string.CompareOrdinal(ClassKey, other.ClassKey);
            if (c != 0) return c;
            c = Carbon.CompareTo(other.Carbon);
            if (c != 0) return c;
            c = DoubleBondEquivalent.CompareTo(other.DoubleBondEquivalent);
            if (c != 0) return c;
            return Oxygen.CompareTo(other.Oxygen);
        }

        public bool Equals(LipidRtComposition other) {
            return other is not null && ClassKey == other.ClassKey && Carbon == other.Carbon
                && DoubleBondEquivalent == other.DoubleBondEquivalent && Oxygen == other.Oxygen;
        }

        public override bool Equals(object obj) => Equals(obj as LipidRtComposition);

        public override int GetHashCode() {
            unchecked {
                var hash = ClassKey.GetHashCode();
                hash = hash * 31 + Carbon;
                hash = hash * 31 + DoubleBondEquivalent;
                return hash * 31 + Oxygen;
            }
        }

        /// <summary>"PC 34:1" or "OxPC 36:3;O2".</summary>
        public override string ToString() {
            return Oxygen != 0
                ? $"{ClassKey} {Carbon}:{DoubleBondEquivalent};O{Oxygen}"
                : $"{ClassKey} {Carbon}:{DoubleBondEquivalent}";
        }
    }

    /// <summary>
    /// One peak or spot of the unit for the MS1 artefact layer: every peak, annotated or not (SPEC 1.2, "context").
    /// </summary>
    /// <remarks>
    /// A candidate with the same <see cref="Id"/> overrides the context peak, so the context may hold every peak of the
    /// unit including the annotated ones.
    /// </remarks>
    public sealed class LipidRtContextPeak
    {
        public int Id { get; set; }
        public double Rt { get; set; }
        public double Mz { get; set; }
        public double Height { get; set; }
        public string Adduct { get; set; } = string.Empty;

        /// <summary>The representative is an MS/MS match (annotation tag 3xx/4xx).</summary>
        public bool IsMsmsAnnotated { get; set; }
    }

    /// <summary>One unit (an analysis file, or an alignment) to check.</summary>
    public sealed class LipidRtRunInput
    {
        public LipidRtRunInput(IReadOnlyList<LipidRtCandidate> candidates, IReadOnlyList<LipidRtContextPeak> context, double rtMin, double rtMax, IonMode polarity) {
            Candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
            Context = context ?? Array.Empty<LipidRtContextPeak>();
            RtMin = rtMin;
            RtMax = rtMax;
            Polarity = polarity;
        }

        /// <summary>Every annotated object of the unit; each receives an outcome.</summary>
        public IReadOnlyList<LipidRtCandidate> Candidates { get; }

        /// <summary>Every peak of the unit for the MS1 artefact layer, annotated or not.</summary>
        public IReadOnlyList<LipidRtContextPeak> Context { get; }

        /// <summary>The smallest RT over all peaks of the unit, not only lipids.</summary>
        public double RtMin { get; }

        /// <summary>The largest RT over all peaks of the unit.</summary>
        public double RtMax { get; }

        public IonMode Polarity { get; }

        /// <summary>The RT span the windows scale with: max(RtMax - RtMin, 1 min); a missing bound counts as 0.</summary>
        public double Span => Math.Max(Finite(RtMax) - Finite(RtMin), 1d);

        private static double Finite(double value) => double.IsNaN(value) || double.IsInfinity(value) ? 0d : value;
    }
}

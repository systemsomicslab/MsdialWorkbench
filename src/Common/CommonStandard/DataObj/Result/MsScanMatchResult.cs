using CompMs.Common.Enum;
using MessagePack;
using System;

namespace CompMs.Common.DataObj.Result {
    [Flags]
    public enum SourceType : byte
    {
        None = 0,
        Unknown = 1 << 0,
        FastaDB = 1 << 1,
        MspDB = 1 << 2,
        TextDB = 1 << 4,
        GeneratedLipid = 1 << 5,
        Manual = 1 << 6,
        DataBases = FastaDB | MspDB | TextDB | GeneratedLipid,
    }
    public enum DataBaseSource {
        None, Msp, Lbm, Text, Fasta, EieioLipid, OadLipid, EidLipid, MsFinder,
    }

    /// <summary>
    /// What kind of evidence an annotation rests on.
    /// </summary>
    /// <remarks>
    /// Deliberately not a new <see cref="SourceType"/> bit. <see cref="SourceType"/> is a [Flags]
    /// byte with two free bits for five evidence kinds, and it already carries several unrelated
    /// meanings -- which database, whether a person intervened, whether anything matched at all --
    /// consulted by the ranking key, the exporters and the deserialization strip. Adding evidence
    /// to it would make one value answer two different questions.
    ///
    /// Also not <see cref="MsScanMatchResult.IsReferenceMatched"/>, which reads as "a spectrum
    /// matched" and is not: the text-database annotators set it on precursor m/z agreement alone,
    /// and a manual acceptance sets it on anything a person accepted, an in-silico structure
    /// included. That boolean keeps its present meaning and its present value; this records what
    /// the evidence actually was, beside it.
    ///
    /// <see cref="Unspecified"/> must stay 0: every result deserialized from a project written
    /// before this member existed reads back as 0, and that has to mean "not recorded" rather than
    /// any particular kind of evidence.
    ///
    /// These values describe WHAT WAS COMPARED AND WHAT IT SUPPORTS. So an MSP candidate for a
    /// feature with no product-ion spectrum is <see cref="PrecursorOnly"/>, not
    /// <see cref="ReferenceSpectrum"/>: the repository already speaks that way --
    /// AnnotationName.NoMs2Prefix calls exactly this case a "precursor-only suggestion", and the
    /// programme's annotation policy is written the same way, "a lower-priority MS/MS reference
    /// match outranks a higher-priority precursor-only suggestion". Which database the name came
    /// from is a separate question, and <see cref="MsScanMatchResult.Source"/> already answers it.
    ///
    /// The "and what it supports" half is carried by <see cref="WeakSpectrumMatch"/> and
    /// <see cref="UnmatchedSpectrum"/>, and nothing is lost by it, because the narrower question
    /// "was a spectrum compared at all" is answered exactly by
    /// <see cref="MeasuredTerms.Spectrum"/> on the same record. Read as a pair, the two keys say
    /// what was measured and what the measurement was worth. That is the distinction the author of
    /// MS-DIAL asked for on 2026-09-10: a precursor-mass match with NO spectrum acquired can
    /// legitimately be reported at class level for a lipid, while a precursor-mass match WITH a
    /// spectrum acquired that failed is, by his criteria, unknown unless retention time supports
    /// it. Those two are opposite verdicts and the suggestion machinery had been treating them as
    /// one bucket.
    /// </remarks>
    public enum AnnotationEvidenceSource : byte {
        Unspecified = 0,
        /// <summary>
        /// Precursor mass, optionally with retention time or collision cross-section. No spectrum
        /// was opened -- either the database holds none, or the feature had no product-ion spectrum.
        /// </summary>
        PrecursorOnly,
        /// <summary>An experimentally acquired reference spectrum was compared.</summary>
        ReferenceSpectrum,
        /// <summary>
        /// A tool that predicts a STRUCTURE from a spectrum established this: the measured peaks
        /// went in, a candidate structure came out. MS-FINDER and SIRIUS work this way.
        /// </summary>
        /// <remarks>
        /// Keeps the serialized value that <c>PredictedSpectrum</c> held, because MS-FINDER
        /// acceptance was its only producer of consequence and the name was the wrong way round:
        /// it described the other direction of in-silico work.
        ///
        /// EXPORTED AS "InSilico", not under this name. The evidence inventory this programme
        /// publishes against records in-silico assignment as one category, so the two directions
        /// are one tag outside MS-DIAL and two members inside it. Which tool it was is already on
        /// the record beside this, in <see cref="MsScanMatchResult.AnnotatorID"/>.
        /// </remarks>
        ByStructurePredictionTool,
        /// <summary>
        /// Diagnostic fragment ions were evaluated against a rule set, and that evaluation is what
        /// established the annotation.
        /// </summary>
        /// <remarks>
        /// This is the lipid pipeline. A spectral comparison does run first, but as a permissive
        /// pre-filter rather than as the evidence: for <c>TargetOmics.Lipidomics</c>
        /// MsReferenceScorer.ValidateBase combines the three dot products with OR where
        /// metabolomics uses AND, the default cut-offs are low, and ValidateOnLipidomics then does
        /// <c>IsSpectrumMatch &amp;= isLipidChainsMatch | isLipidClassMatch | ...</c> -- so the
        /// characteristic-ion rules in MsmsCharacterization decide both whether the match stands
        /// and at what structural level it is reported. Recording ReferenceSpectrum here would
        /// credit the evidence to the pre-filter, and would additionally assert "experimentally
        /// acquired" about a library whose spectra are generated in silico.
        /// </remarks>
        RuleBased,
        /// <summary>
        /// A person made this record, and no comparison of any kind stands behind it.
        /// </summary>
        /// <remarks>
        /// Reserved for a human assertion with nothing to describe -- today only marking a peak
        /// unknown. A person ACCEPTING a candidate does not relabel it: that would overwrite the
        /// only record of what the annotator compared, in exchange for a fact
        /// <see cref="SourceType.Manual"/> and <see cref="MsScanMatchResult.IsManuallyModified"/>
        /// already carry. The producer of an annotation owns its evidence source; acceptance does
        /// not.
        /// </remarks>
        Manual,
        /// <summary>
        /// A reference spectrum was compared, some of the acceptance criteria were met, and the
        /// conjunction of them was not. The name is reportable but the spectrum did not settle it.
        /// </summary>
        /// <remarks>
        /// This is the tier that a single cut-off cannot express. With a 70% threshold, a candidate
        /// at 69% and a candidate that matched two fragments out of forty are both "below
        /// threshold", and treating them alike is what makes the cut-off feel arbitrary. The line
        /// between this and <see cref="UnmatchedSpectrum"/> uses no new number: it reuses the
        /// analyst's own <c>MinimumSpectrumMatch</c> for metabolomics, and for lipidomics it asks
        /// the diagnostic-fragment rules instead, because there a single characteristic ion can
        /// settle a class outright -- cholesteryl ester is the standard example -- so a peak count
        /// says nothing.
        /// </remarks>
        WeakSpectrumMatch,
        /// <summary>
        /// A reference spectrum was compared and explained essentially nothing. The precursor mass
        /// may still agree; on its own that is not an identification.
        /// </summary>
        /// <remarks>
        /// Distinct from <see cref="PrecursorOnly"/>, and the distinction is the point. There, no
        /// product-ion spectrum existed to compare, so the mass is all the evidence there could
        /// have been; here a spectrum was acquired, compared, and disagreed, which is a positive
        /// finding against the candidate rather than an absence of one.
        /// </remarks>
        UnmatchedSpectrum,
        /// <summary>
        /// A tool that predicts a SPECTRUM from a structure established this: a candidate structure
        /// or sequence went in, an expected fragment spectrum came out, and the measured peaks were
        /// scored against it. CFM-ID and FIORA work this way, as does the peptide b/y ladder that
        /// SequenceToSpec computes from a FASTA entry.
        /// </summary>
        /// <remarks>
        /// Distinguished from <see cref="ByStructurePredictionTool"/> inside MS-DIAL and merged
        /// with it on the way out; see that member. The distinction is kept because the two carry
        /// different failure modes -- a spectrum predictor can be wrong about the fragmentation of
        /// a structure that is nonetheless present, while a structure predictor can propose a
        /// structure that was never there -- and because the author of MS-DIAL asked for the
        /// separation to survive internally.
        /// </remarks>
        BySpectrumPredictionTool,
    }

    /// <summary>
    /// Decides an <see cref="AnnotationEvidenceSource"/> from the facts an annotation site has in
    /// hand at the moment it produces a result.
    /// </summary>
    /// <remarks>
    /// Here rather than at each site for the same reason as <see cref="MeasuredTermsExtension"/>:
    /// the mode-specific annotators are near-identical and an omission or a divergence in one of
    /// them silently mislabels a whole acquisition mode.
    ///
    /// Both methods take <see cref="MeasuredTerms"/> rather than re-deriving whether a spectrum was
    /// compared, so key 40 cannot contradict key 39 on the same record.
    /// </remarks>
    public static class AnnotationEvidence {
        /// <summary>
        /// The evidence for a search against a reference database, given what the search measured
        /// and which omics rule set governed it.
        /// </summary>
        public static AnnotationEvidenceSource ForDatabaseMatch(MeasuredTerms measured, TargetOmics omics) {
            if (!measured.HasFlag(MeasuredTerms.Spectrum)) {
                return AnnotationEvidenceSource.PrecursorOnly;
            }
            return omics == TargetOmics.Lipidomics
                ? AnnotationEvidenceSource.RuleBased
                : AnnotationEvidenceSource.ReferenceSpectrum;
        }

        /// <summary>
        /// <paramref name="evidence"/> when a spectrum was actually compared, and
        /// <see cref="AnnotationEvidenceSource.PrecursorOnly"/> when none was, for the sites that
        /// know their reference material by type rather than from a parameter.
        /// </summary>
        public static AnnotationEvidenceSource WhenSpectrumCompared(MeasuredTerms measured, AnnotationEvidenceSource evidence) {
            return measured.HasFlag(MeasuredTerms.Spectrum) ? evidence : AnnotationEvidenceSource.PrecursorOnly;
        }

        /// <summary>
        /// Downgrades the evidence of a candidate whose reference spectrum was compared and did not
        /// carry the match, from "a spectrum decided this" to how far it actually got.
        /// </summary>
        /// <remarks>
        /// A SEPARATE, LATER STEP ON PURPOSE. The verdict this reads -- IsSpectrumMatch and the
        /// lipid rule flags -- is not set until validation, which in several annotators runs in the
        /// caller of the method that produces the result, so grading at the production site would
        /// read flags that are not there yet. Rather than move six assignments and hope none was
        /// missed, this only ever DOWNGRADES an already-recorded value. A site that never calls it
        /// keeps the value it had before this change: wrong in the old way, never blank. Missing a
        /// site therefore costs precision, not the record itself, and the per-mode tests say which
        /// sites are covered.
        ///
        /// Callers that are not database searches -- a manual assertion, an in-silico structure,
        /// a candidate for which no spectrum was compared -- fall out of the first guard untouched.
        /// </remarks>
        public static void RecordSpectrumVerdict(MsScanMatchResult result, TargetOmics omics, float minimumSpectrumMatch) {
            if (result.EvidenceSource != AnnotationEvidenceSource.ReferenceSpectrum
                && result.EvidenceSource != AnnotationEvidenceSource.RuleBased) {
                return;
            }
            if (result.IsSpectrumMatch) {
                return;
            }
            result.EvidenceSource = ExplainedNothing(result, omics, minimumSpectrumMatch)
                ? AnnotationEvidenceSource.UnmatchedSpectrum
                : AnnotationEvidenceSource.WeakSpectrumMatch;
        }


        /// <summary>
        /// How strongly a kind of evidence supports an identification, as an order and nothing more.
        /// Larger is stronger; only the comparisons between two values mean anything.
        /// </summary>
        /// <remarks>
        /// A RANK RATHER THAN A WEIGHT, at the author's direction of 2026-09-15: a qualitative
        /// judgement -- a name that a spectrum decided outranks a name that only a mass suggested --
        /// belongs in the ordering of the candidates, not folded into their scores. A weight would
        /// make that judgement tradeable, so a large enough similarity could buy a precursor-only
        /// candidate past a spectral match; as a separate key it cannot be bought at any score.
        /// It is also the programme's written annotation policy, in as many words: "a lower-priority
        /// MS/MS reference match outranks a higher-priority precursor-only suggestion".
        ///
        /// NEVER <c>(int)source</c>. The serialized values are a compatibility record -- 3 is where
        /// <c>PredictedSpectrum</c> sat, 8 is the member that was appended after it -- and they run
        /// in the order the members happened to be written. Casting would make every future
        /// insertion silently reorder candidates in projects already on disk. The switch says the
        /// order once, out loud, where changing it is a decision rather than an accident.
        ///
        /// The numbers are ordinals with no unit. Equal ranks are deliberate and mean "this ordering
        /// declines to separate these two"; the next key down then decides, exactly as it does today.
        /// </remarks>
        public static int Rank(AnnotationEvidenceSource source) {
            switch (source) {
                // A spectrum was compared and carried the match. The lipid rule set and a reference
                // spectrum are equal here because for lipidomics the rules ARE the evidence -- see
                // RuleBased -- so ranking one under the other would penalise the pipeline that is
                // doing the more specific work.
                case AnnotationEvidenceSource.ReferenceSpectrum:
                case AnnotationEvidenceSource.RuleBased:
                    return 6;

                // A spectrum was compared and got part of the way. Still above everything that
                // compared no spectrum at all: the author's 69%-against-a-70%-threshold case, which
                // is a real partial agreement rather than an absence of evidence.
                case AnnotationEvidenceSource.WeakSpectrumMatch:
                    return 5;

                // NEUTRAL, and both for the same reason: neither says anything this key can weigh.
                //
                // Unspecified is "not recorded" -- every candidate in a project written before the
                // evidence record existed. Ranking it last would make opening such a project reorder
                // its candidates against whatever a fresh annotation added; ranking it first would
                // let an unrecorded candidate outrank a measured one. In a project where nothing is
                // recorded every candidate sits here, they tie, and the order is exactly today's.
                //
                // Manual is a person asserting something with no comparison behind it. A person
                // ACCEPTING a candidate is a different fact and is not this: it wins higher up, on
                // the Manual bit of SourceType -- read directly as the facade's first key, and read
                // through IsManuallyModified as ResultOrder's. It is the top bit of the flags byte,
                // so a human's decision has already settled both orderings before this is consulted.
                case AnnotationEvidenceSource.Unspecified:
                case AnnotationEvidenceSource.Manual:
                    return 4;

                // BELOW a compared spectrum, ABOVE a bare precursor mass.
                //
                // Below, confirmed by the author on 2026-09-14: a computed spectrum or a computed
                // structure is a hypothesis about a compound, and a real spectrum that partly agreed
                // is an observation of one.
                //
                // Above, on his reasoning of 2026-09-15, and the reasoning matters more than the
                // placement. These tools do take the product-ion spectrum into account -- MS-FINDER
                // and SIRIUS read the measured peaks to get where they get, and a spectrum predictor
                // is scored against them -- so a bare mass is the one term they have in common and
                // everything else is extra. And a precursor-only candidate carries a claim it cannot
                // support: it arrives as a STRUCTURE, a named compound, when m/z alone justifies at
                // most a formula. Retention time plus m/z would be a different matter. So the
                // overreaching claim ranks under the calculated one.
                case AnnotationEvidenceSource.ByStructurePredictionTool:
                case AnnotationEvidenceSource.BySpectrumPredictionTool:
                    return 3;

                case AnnotationEvidenceSource.PrecursorOnly:
                    return 2;

                // Last, and below PrecursorOnly on purpose. A spectrum was acquired, compared, and
                // explained nothing -- evidence AGAINST the candidate, where PrecursorOnly is merely
                // the absence of evidence either way.
                case AnnotationEvidenceSource.UnmatchedSpectrum:
                    return 1;

                // A member added without being ranked. Neutral rather than fatal, so an omission
                // costs precision in the ordering instead of failing an annotation run mid-way;
                // AnnotationEvidenceRankTests enumerates the enum so the omission fails a test.
                default:
                    return 4;
            }
        }

        /// <summary>
        /// <see cref="Rank(AnnotationEvidenceSource)"/> for a candidate, with a floor for the null
        /// that <c>DefaultIfEmpty</c> introduces in the ordering keys.
        /// </summary>
        /// <remarks>
        /// Not <c>Rank(result?.EvidenceSource ?? Unspecified)</c>: that would hand the placeholder
        /// the neutral rank and let it beat a real candidate whose spectrum was compared and failed.
        /// A placeholder is not a candidate and must lose every key it appears in.
        /// </remarks>
        public static int RankOf(MsScanMatchResult? result) {
            return result is null ? int.MinValue : Rank(result.EvidenceSource);
        }

        /// <summary>
        /// Whether the comparison explained essentially nothing, as opposed to falling short.
        /// </summary>
        /// <remarks>
        /// No new threshold is introduced. For metabolomics the line is the analyst's own
        /// <c>MinimumSpectrumMatch</c>, the same number their acceptance criteria already use, with
        /// a floor of one fragment for the case where they have switched it off -- a comparison
        /// that matched no fragment at all explained nothing by any reading.
        ///
        /// For lipidomics the peak count is deliberately NOT consulted. One diagnostic fragment can
        /// settle a lipid class on its own -- cholesteryl ester is the example the author gave --
        /// so a count near zero is compatible with a correct class assignment. The rules in
        /// MsmsCharacterization are the evidence there, so they are what is asked.
        /// </remarks>
        private static bool ExplainedNothing(MsScanMatchResult result, TargetOmics omics, float minimumSpectrumMatch) {
            if (omics == TargetOmics.Lipidomics) {
                return !(result.IsLipidChainsMatch || result.IsLipidClassMatch
                    || result.IsLipidPositionMatch || result.IsOtherLipidMatch);
            }
            var atLeastOneFragment = System.Math.Max(minimumSpectrumMatch, 1f);
            return result.MatchedPeaksCount < atLeastOneFragment;
        }
    }

    /// <summary>
    /// Which similarity terms were actually computed for a result.
    /// </summary>
    /// <remarks>
    /// Recorded where a term is computed, so that no consumer has to infer it from the value
    /// afterwards. The three existing predicates on <see cref="MsScanMatchResult"/> do infer it --
    /// <see cref="MsScanMatchResult.IsIsotopeComparisonPerformed"/> from a -1 sentinel,
    /// <see cref="MsScanMatchResult.IsGaussianTermMeasured"/> from the sign, and
    /// <see cref="MsScanMatchResult.IsSpectrumComparisonPerformed"/> from the whole block -- and
    /// they stay untouched, because a project written before this member exists reads
    /// <see cref="None"/> while those predicates still answer correctly for it.
    ///
    /// <see cref="MsScanMatchResult.IsSpectrumMatch"/> and its siblings are not this: they say a
    /// term passed its threshold, and a term that was never computed also reports false there.
    /// Conflating the two is what these flags exist to end.
    /// </remarks>
    [Flags]
    public enum MeasuredTerms : byte {
        None = 0,
        Spectrum = 1 << 0,
        AccurateMass = 1 << 1,
        RetentionTime = 1 << 2,
        RetentionIndex = 1 << 3,
        Ccs = 1 << 4,
        Isotope = 1 << 5,
    }

    /// <summary>
    /// Records <see cref="MeasuredTerms"/> from the values the scoring functions return, at the
    /// point they return them.
    /// </summary>
    /// <remarks>
    /// This exists so the rule is written once. Nine places build a <see cref="MsScanMatchResult"/>
    /// from freshly computed similarity terms, and they differ only in which terms the acquisition
    /// mode has. An omission in one of them leaves a whole mode's evidence blank, which is the
    /// failure this record was added to prevent, so the composition does not get copied nine times.
    ///
    /// The test is "did the scoring function return its not-compared sentinel", not "is the value
    /// positive". Those differ, and the difference is the reason for recording at the measurement
    /// site: <see cref="MsScanMatchResult.IsGaussianTermMeasured"/> has to approximate with
    /// <c>&gt; 0</c> because by the time it runs it cannot separate an unset field from a computed
    /// one, so it reports a term whose Gaussian underflowed -- a difference beyond roughly 38
    /// tolerance widths -- as not measured. Here the sentinel is still present and the question is
    /// answerable exactly. Where the flag and the sign test disagree, the flag is the true one.
    /// Nothing reads the flag yet, so recording it changes no score and no decision.
    /// </remarks>
    public static class MeasuredTermsExtension {
        /// <summary>What the scoring functions in MsScanMatching return when there was nothing to compare.</summary>
        private const double NotCompared = -1d;

        /// <summary>
        /// Adds <paramref name="term"/> when <paramref name="similarity"/> is a measurement rather
        /// than the not-compared sentinel.
        /// </summary>
        public static MeasuredTerms With(this MeasuredTerms terms, MeasuredTerms term, double similarity) {
            return similarity == NotCompared ? terms : terms | term;
        }

        /// <summary>
        /// Adds <paramref name="term"/> when both sides of a Gaussian similarity term were present,
        /// for the callers that have no sentinel to test.
        /// </summary>
        /// <remarks>
        /// MsScanMatching has two GetGaussianSimilarity overloads. The four-argument one guards --
        /// either side not positive and it returns the sentinel -- and callers of that one use
        /// <see cref="With(MeasuredTerms, MeasuredTerms, double)"/>. The three-argument one does
        /// not guard: it evaluates exp(-0.5 * ((actual - reference) / tolerance)^2) on whatever it
        /// is given, so a reference with no retention time contributes a similarity computed
        /// against 0, and the returned value carries no trace of the absence. Most of the
        /// annotators call that one, so for them the presence test has to be made on the inputs.
        ///
        /// This deliberately records "not measured" for a term whose value is nevertheless
        /// non-zero at those sites. The flag is the true statement and the value is the wrong one;
        /// correcting the value is a separate change, because it moves scores.
        /// </remarks>
        public static MeasuredTerms WithComparedValues(this MeasuredTerms terms, MeasuredTerms term, double actual, double reference) {
            return actual > 0d && reference > 0d ? terms | term : terms;
        }

        /// <summary>
        /// Adds <see cref="MeasuredTerms.Spectrum"/> when a product-ion spectrum was actually
        /// compared against a reference spectrum.
        /// </summary>
        /// <remarks>
        /// The three dot products, the matched-peaks ratio and the matched-peaks count share one
        /// availability gate -- both spectra non-null and non-empty -- so any one of them holding
        /// the sentinel means none of them was computed. All five are tested anyway, to match
        /// <see cref="MsScanMatchResult.IsSpectrumComparisonPerformed"/> term for term.
        /// </remarks>
        public static MeasuredTerms WithSpectrum(
            this MeasuredTerms terms,
            double sqWeightedDotProduct, double sqSimpleDotProduct, double sqReverseDotProduct,
            double matchedPeaksPercentage, double matchedPeaksCount) {

            if (matchedPeaksPercentage == NotCompared || matchedPeaksCount == NotCompared) {
                return terms;
            }
            return terms.WithSpectrum(sqWeightedDotProduct, sqSimpleDotProduct, sqReverseDotProduct);
        }

        /// <summary>
        /// Adds <see cref="MeasuredTerms.Spectrum"/> from the dot products alone, for the callers
        /// that record before the matched-peaks terms are in hand.
        /// </summary>
        /// <remarks>
        /// Reaches the same answer as the five-term overload, because the two matched-peaks values
        /// share the dot products' availability gate.
        /// </remarks>
        public static MeasuredTerms WithSpectrum(
            this MeasuredTerms terms,
            double sqWeightedDotProduct, double sqSimpleDotProduct, double sqReverseDotProduct) {

            if (sqWeightedDotProduct == NotCompared
                || sqSimpleDotProduct == NotCompared
                || sqReverseDotProduct == NotCompared) {
                return terms;
            }
            return terms | MeasuredTerms.Spectrum;
        }
    }

    [MessagePackObject]
    public class MsScanMatchResult {
        // basic annotated information
        [Key(0)]
        public string Name { get; set; }
        [Key(1)]
        public string InChIKey { get; set; }

        [Key(2)]
        public float TotalScore { get; set; }

        // spectral similarity
        [Key(3)]
        public float SquaredWeightedDotProduct { get; set; }
        [IgnoreMember]
        public float WeightedDotProduct {
            get => (float)Math.Sqrt(Math.Max(SquaredWeightedDotProduct, 0f));
            set => SquaredWeightedDotProduct = value * value;
        }

        [Key(4)]
        public float SquaredSimpleDotProduct { get; set; }
        [IgnoreMember]
        public float SimpleDotProduct {
            get => (float)Math.Sqrt(Math.Max(SquaredSimpleDotProduct, 0f));
            set => SquaredSimpleDotProduct = value * value;
        }
        [Key(5)]
        public float SquaredReverseDotProduct { get; set; }
        [IgnoreMember]
        public float ReverseDotProduct {
            get => (float)Math.Sqrt(Math.Max(SquaredReverseDotProduct, 0f));
            set => SquaredReverseDotProduct = value * value;
        }
        [Key(6)]
        public float MatchedPeaksCount { get; set; }
        [Key(7)]
        public float MatchedPeaksPercentage { get; set; }
        [Key(8)]
        public float EssentialFragmentMatchedScore { get; set; }
        [Key(29)]
        public float AndromedaScore { get; set; }
        [Key(32)]
        public float PEPScore { get; set; }

        // others
        [Key(9)]
        public float RtSimilarity { get; set; }
        [Key(10)]
        public float RiSimilarity { get; set; }
        [Key(11)]
        public float CcsSimilarity { get; set; }
        [Key(12)]
        public float IsotopeSimilarity { get; set; }
        [Key(13)]
        public float AcurateMassSimilarity { get; set; }

        /// <summary>The value the scoring functions return when there was nothing to compare.</summary>
        private const float NotCompared = -1f;

        /// <summary>
        /// True when the isotope-ratio comparison was attempted, so <see cref="IsotopeSimilarity"/> holds
        /// a measurement.
        /// </summary>
        /// <remarks>
        /// MsScanMatching.GetIsotopeRatioSimilarity returns -1 when there is nothing to compare: either
        /// side carries no isotopic peaks, or a monoisotopic abundance is not positive. Unlike the dot
        /// products there is no clamping getter, so that -1 reaches every consumer unchanged.
        ///
        /// The test is equality with the sentinel rather than a sign test, because this score is 1 minus
        /// an accumulated ratio difference and is genuinely signed: a negative value ordinarily means the
        /// isotope patterns disagree, which is a measurement and a strong one. Only exactly -1 is the
        /// sentinel, and a computed value of exactly -1 is indistinguishable from an unattempted
        /// comparison. That ambiguity is resolved in favour of "not computed", because publishing a
        /// sentinel as a measurement is the worse of the two errors, and it is narrow: reaching it needs
        /// the accumulated difference to land on exactly 2.
        /// </remarks>
        [IgnoreMember]
        public bool IsIsotopeComparisonPerformed => IsotopeSimilarity != NotCompared;

        /// <summary>
        /// True when <paramref name="similarity"/> holds a measurement from one of the Gaussian
        /// similarity terms: retention time, retention index, collision cross-section or accurate mass.
        /// </summary>
        /// <remarks>
        /// A different rule applies to these than to <see cref="IsIsotopeComparisonPerformed"/>, because a
        /// different function produces them. MsScanMatching.GetGaussianSimilarity returns
        /// exp(-0.5 * ((actual - reference) / tolerance)^2), which is strictly positive for any finite
        /// argument until it underflows past roughly 38 tolerance widths, and returns -1 when either
        /// value is missing or not positive.
        ///
        /// So a measured term is positive, a term with nothing to compare is -1, and a term the run never
        /// enabled is left at the field's default 0. Exactly 0 is therefore either that unset default or a
        /// difference so large the score underflowed, and neither is a measurement worth publishing. This
        /// is the same test GetTotalScore already applies before adding a term to the total, so the export
        /// convention and the scoring convention agree.
        /// </remarks>
        public static bool IsGaussianTermMeasured(float similarity) => similarity > 0f;

        // Link to database
        [Key(14)]
        public int LibraryID { get; set; } = -1;
        [Key(24)]
        public int LibraryIDWhenOrdered { get; set; } = -1;

        // Checker
        [Key(15)]
        public bool IsPrecursorMzMatch { get; set; }
        [Key(16)]
        public bool IsSpectrumMatch { get; set; }
        [Key(17)]
        public bool IsRtMatch { get; set; }
        [Key(23)]
        public bool IsRiMatch { get; set; }
        [Key(18)]
        public bool IsCcsMatch { get; set; }
        [Key(19)]
        public bool IsLipidClassMatch { get; set; }
        [Key(20)]
        public bool IsLipidChainsMatch { get; set; }
        [Key(21)]
        public bool IsLipidPositionMatch { get; set; }
        [Key(35)]
        public bool IsLipidDoubleBondPositionMatch { get; set; }
        [Key(22)]
        public bool IsOtherLipidMatch { get; set; }
        [IgnoreMember]
        public bool IsUnknown => Source.HasFlag(SourceType.Unknown);
        [IgnoreMember]
        public bool AnyMatched => (Source & SourceType.DataBases) != SourceType.None;

        /// <summary>
        /// True when a product-ion spectrum was actually compared against a reference spectrum, so the
        /// spectral score fields below hold measurements rather than unset defaults.
        /// </summary>
        /// <remarks>
        /// The scoring functions in MsScanMatching return -1 when there is nothing to compare, which is
        /// the same condition that gives a suggested annotation the "no MS2: " prefix. That -1 survives
        /// in <see cref="SquaredSimpleDotProduct"/> and its siblings, but the <see cref="SimpleDotProduct"/>,
        /// <see cref="WeightedDotProduct"/> and <see cref="ReverseDotProduct"/> getters clamp it to 0, so a
        /// score that was never computed is otherwise indistinguishable from one that was computed as 0.
        /// A text database holds no reference spectrum, so no comparison is attempted for a TextDB result
        /// either and its annotator leaves every spectral field at the default 0.
        /// </remarks>
        [IgnoreMember]
        public bool IsSpectrumComparisonPerformed {
            get {
                if (IsUnknown) {
                    return false;
                }
                if ((Source & SourceType.DataBases) == SourceType.TextDB) {
                    return false;
                }
                return SquaredSimpleDotProduct >= 0f
                    && SquaredWeightedDotProduct >= 0f
                    && SquaredReverseDotProduct >= 0f
                    && MatchedPeaksCount >= 0f
                    && MatchedPeaksPercentage >= 0f;
            }
        }

        // Support for multiple annotation method
        [IgnoreMember]
        public bool IsManuallyModified => (Source & SourceType.Manual) != 0;
        [Key(26)]
        public SourceType Source { get; set; }
        [Key(27)]
        public string AnnotatorID { get; set; }
        [Key(28)]
        public int SpectrumID { get; set; } = -1;
        [Key(30)]
        public bool IsDecoy { get; set; } = false;
        [Key(31)]
        public int Priority { get; set; } = -1;
        [Key(33)]
        public bool IsReferenceMatched { get; set; } = false;
        [Key(34)]
        public bool IsAnnotationSuggested { get; set; } = false;
        [Key(36)]
        public double CollisionEnergy { get; set; }
        [Key(37)]
        public float EnhancedDotProduct { get; set; }
        [Key(38)]
        public float SpectralEntropy { get; set; }

        // Evidence record. Keys 39 onward; key 25 is a hole that predates this repository and is
        // left alone. Nothing reads these yet.
        //
        // Every "not recorded" state below is the CLR default for its type, and that is a
        // requirement rather than a convenience. MessagePack's generated deserializer assigns
        // default(T) to every key absent from a short array -- it does not fall back to the C#
        // property initializer -- so a member whose "not recorded" state is anything other than
        // the default comes back from an older project asserting a fact nobody established. That
        // is why the two counts are int? and not int with a -1 initializer: -1 does not survive,
        // 0 does, and "zero candidates were scored" is a claim.
        // See MsScanMatchResultBackwardCompatibilityTests.

        /// <summary>Which similarity terms were computed. <see cref="MeasuredTerms.None"/> means not recorded.</summary>
        [Key(39)]
        public MeasuredTerms MeasuredTerms { get; set; } = MeasuredTerms.None;

        /// <summary>What kind of evidence this rests on. <see cref="AnnotationEvidenceSource.Unspecified"/> means not recorded.</summary>
        [Key(40)]
        public AnnotationEvidenceSource EvidenceSource { get; set; } = AnnotationEvidenceSource.Unspecified;

        /// <summary>
        /// How many candidates this annotator scored for this peak, before any were discarded.
        /// Null means not recorded.
        /// </summary>
        /// <remarks>
        /// This is the number that cannot be recovered afterwards: the annotation processes keep
        /// only the best few candidates, so without it "the run could not discriminate between
        /// candidates" is indistinguishable from "the run never reported more than a handful".
        /// Whether the list was truncated is left to the reader to derive from this and
        /// <see cref="CandidatesAboveThreshold"/>, because the cap differs between the annotation
        /// processes and a single flag would be wrong for most of them.
        ///
        /// SCOPE: one population is one (peak, annotator, product-ion spectrum) triple -- NOT one
        /// (peak, annotator) pair. Two configurations in scope break the pairwise reading. Under
        /// AIF / all-ion acquisition one MSDecResultCollection is built per collision energy and
        /// the same annotator runs once per collection, and in LC-IM-MS the same annotator runs
        /// once per drift peak with the survivors added to the parent LC peak as well. In both, one
        /// container legitimately holds several populations from one annotator. The discriminators
        /// are already on this object and are set immediately before these counts:
        /// <see cref="SpectrumID"/> and <see cref="CollisionEnergy"/>. A consumer must therefore
        /// group by (AnnotatorID, SpectrumID) and take a DISTINCT value per group -- never a sum,
        /// since every survivor of one selection carries the same figures.
        ///
        /// LIMIT, and it is the sharpest one on this record: the only carriers of these counts are
        /// the candidates that survived selection. An annotator that scored candidates and named
        /// none of them stores nothing at all, so its population size has no carrier and is not
        /// merely unrecorded but unrecordable here. A reader summing or averaging over visible rows
        /// is therefore looking at a lower bound with no way to know how far off it is. Closing
        /// that would need a carrier that is not a survivor -- a per-(peak, annotator) record that
        /// does not exist today -- which is a larger change than adding these fields was.
        /// </remarks>
        [Key(41)]
        public int? CandidatesFound { get; set; }

        /// <summary>
        /// How many of <see cref="CandidatesFound"/> the annotator judged to be either a reference
        /// match or a precursor-only suggestion. Null means not recorded.
        /// </summary>
        /// <remarks>
        /// Named for the evaluator method that produces it, but note what that method actually
        /// does: MsScanMatchResultEvaluator.FilterByThreshold is
        /// <c>IsAnnotationSuggested || IsReferenceMatched</c>, and its constructor ignores the
        /// search parameter entirely. No cut-off is applied here -- the thresholds were applied
        /// inside the annotator when it set those two booleans. So this is "candidates the
        /// annotator was willing to name", not "candidates above a score".
        /// </remarks>
        [Key(42)]
        public int? CandidatesAboveThreshold { get; set; }

        /// <summary>
        /// How many of <see cref="CandidatesAboveThreshold"/> were reference matches rather than
        /// precursor-only suggestions. Null means not recorded.
        /// </summary>
        /// <remarks>
        /// The three numbers together give the tiers the programme's annotation policy
        /// distinguishes, none of which survives truncation on its own:
        /// <see cref="CandidatesFound"/> minus <see cref="CandidatesAboveThreshold"/> is what the
        /// annotator rejected outright; <see cref="CandidatesAboveThreshold"/> minus this is the
        /// precursor-only suggestions; and this is the MS/MS reference matches. That last number
        /// is the one an ambiguity claim rests on -- "the run could not choose between six equally
        /// good reference matches" is a different statement from "the run found six candidates".
        ///
        /// Equal to <c>FilterByThreshold(candidates).Count(r =&gt; r.IsReferenceMatched)</c>, which
        /// is what SelectReferenceMatchResults computes.
        /// </remarks>
        [Key(43)]
        public int? CandidatesReferenceMatched { get; set; }

        public MsScanMatchResult Clone() {
            return (MsScanMatchResult)MemberwiseClone();
        }
    }
}

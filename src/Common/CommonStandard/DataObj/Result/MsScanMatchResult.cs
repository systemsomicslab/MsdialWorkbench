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
    /// These values describe WHAT WAS COMPARED, not what the database happens to hold. So an MSP
    /// candidate for a feature with no product-ion spectrum is <see cref="PrecursorOnly"/>, not
    /// <see cref="ReferenceSpectrum"/>. Two readings were possible and this one is chosen because
    /// it is the one the repository already speaks: AnnotationName.NoMs2Prefix calls exactly this
    /// case a "precursor-only suggestion" and applies it to MSP and LBM names, and the programme's
    /// annotation policy is written the same way -- "a lower-priority MS/MS reference match
    /// outranks a higher-priority precursor-only suggestion". Under the other reading a record
    /// could be labelled <see cref="ReferenceSpectrum"/> while its own name carried the "no MS2: "
    /// prefix. Which database the name came from is a separate question, and
    /// <see cref="MsScanMatchResult.Source"/> already answers it.
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
        /// <summary>A computationally generated spectrum was compared.</summary>
        PredictedSpectrum,
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
        /// Per (peak, annotator), never per peak. This is the number that cannot be recovered
        /// afterwards: the annotation processes keep only the best few candidates, so without it
        /// "the run could not discriminate between candidates" is indistinguishable from "the run
        /// never reported more than a handful". Whether the list was truncated is left to the
        /// reader to derive from this and <see cref="CandidatesAboveThreshold"/>, because the cap
        /// differs between the annotation processes and a single flag would be wrong for most of
        /// them.
        /// </remarks>
        [Key(41)]
        public int? CandidatesFound { get; set; }

        /// <summary>
        /// How many of <see cref="CandidatesFound"/> passed this annotator's thresholds. Null means
        /// not recorded.
        /// </summary>
        [Key(42)]
        public int? CandidatesAboveThreshold { get; set; }

        public MsScanMatchResult Clone() {
            return (MsScanMatchResult)MemberwiseClone();
        }
    }
}

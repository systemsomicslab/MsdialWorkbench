using CompMs.Common.Enum;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CompMs.MsdialCore.Parameter
{
    /// <summary>
    /// The precision/recall knob of the lipid retention-time order check (decision D2).
    /// </summary>
    public enum LipidRtOperatingPoint
    {
        /// <summary>HighPrecision, and a Confirmed must also be corroborated by a second adduct or the other polarity.</summary>
        Strict = 0,
        /// <summary>Bracket support: lower and upper neighbours on at least two axes. The default.</summary>
        HighPrecision = 1,
        /// <summary>Bracket support or two axes.</summary>
        Mid = 2,
        /// <summary>One supporting direction.</summary>
        Balanced = 3,
    }

    /// <summary>
    /// What the analyst says about the chromatography. ParameterBase has no reversed-phase/HILIC field, so the
    /// check also tests the data itself (the run-level gate) unless the analyst says the run is not reversed phase.
    /// </summary>
    public enum LipidRtChromatography
    {
        /// <summary>The data gate decides.</summary>
        Auto = 0,
        /// <summary>The analyst says reversed phase; the data gate is still applied as a safety net.</summary>
        ReversedPhase = 1,
        /// <summary>HILIC, SFC, direct infusion or anything else: nothing is evaluated.</summary>
        Other = 2,
    }

    /// <summary>
    /// The biological plausibility profile, a layer separate from the retention-time evidence.
    /// </summary>
    /// <remarks>
    /// A profile holds chain-level rules only. It never excludes or penalises a lipid class for an organism
    /// (the author's decision of 2026-10-15: animals synthesise MGDG and DGDG, and diet and gut bacteria bring
    /// other classes).
    /// </remarks>
    public enum LipidRtPlausibilityProfileKind
    {
        None = 0,
        /// <summary>B1 (no fatty chain with 7 or more double bonds, hard), B2 and B3 (sphingolipid N-acyl and base, soft).</summary>
        Mammalian = 1,
    }

    /// <summary>What happens to a Confirmed member whose own match is class level only.</summary>
    public enum LipidRtClassLevelPolicy
    {
        /// <summary>A class-level member is Confirmed with its feature (D3 literal).</summary>
        Consistent = 0,
        /// <summary>Confirmed only with a second adduct of the same feature or a Confirmed other-polarity twin. The default.</summary>
        Corroborated = 1,
        /// <summary>Never Confirmed, but still used as order evidence.</summary>
        Never = 2,
    }

    /// <summary>A less intense feature with the same species name (and the same or an unresolved chain name) at another RT.</summary>
    public enum LipidRtMinorDuplicatePolicy
    {
        Allow = 0,
        Block = 1,
    }

    /// <summary>A feature made only of secondary ions (Na, K, Li, multiply charged).</summary>
    public enum LipidRtSecondaryOnlyPolicy
    {
        Evaluate = 0,
        Block = 1,
    }

    /// <summary>A Confirmed whose other-polarity twins are all Inconsistent.</summary>
    public enum LipidRtPolarityConflictPolicy
    {
        /// <summary>Stays Confirmed; the conflict is recorded.</summary>
        Flag = 0,
        /// <summary>Becomes NotEvaluable(PolarityConflict).</summary>
        Demote = 1,
    }

    /// <summary>
    /// The MS1 artefact relations the check looks for between a candidate and a brighter co-eluting peak.
    /// </summary>
    [Flags]
    public enum LipidRtArtefactKinds
    {
        None = 0,
        Iso1 = 1 << 0,
        Iso2 = 1 << 1,
        NaH = 1 << 2,
        NaNH4 = 1 << 3,
        KH = 1 << 4,
        KNa = 1 << 5,
        NH4H = 1 << 6,
        Acn = 1 << 7,
        Water = 1 << 8,
        Acetate = 1 << 9,
        Formate = 1 << 10,
        Chloride = 1 << 11,
        Demethyl = 1 << 12,
        IsfChain = 1 << 13,
        IsfHead = 1 << 14,
        Ox = 1 << 15,
        /// <summary>Every kind except the water loss and the acetonitrile adduct, which were not specific on the development data.</summary>
        Default = Iso1 | Iso2 | NaH | NaNH4 | KH | KNa | NH4H | Acetate | Formate | Chloride | Demethyl | IsfChain | IsfHead | Ox,
    }

    /// <summary>
    /// Parameters of the lipid retention-time order check (specification v1.0, sections 5 and 6, with the addendum of
    /// 2026-10-15).
    /// </summary>
    /// <remarks>
    /// The first group is user facing; the rest are expert constants that are serialized but not shown. Every
    /// window is a fraction of the run's RT span, so the same values work on a 20-minute and a 6.5-minute gradient.
    ///
    /// Every member's "not set" state must be what an older project should mean, because MessagePack assigns
    /// default(T) to a key that is absent from a shorter array. That is why <see cref="IsEnabled"/> is nullable:
    /// null means "the default for the project's target omics", see <see cref="IsEnabledFor(TargetOmics)"/>. The
    /// other members are only read once the whole object exists, and an object written by this version carries
    /// every key.
    /// </remarks>
    [MessagePackObject]
    public sealed class LipidRtCheckParameter
    {
        /// <summary>The version of the rules. Part of every verdict and of the tag source string.</summary>
        /// <remarks>
        /// Raised whenever the check can conclude differently from the same objects and the same values, including a change
        /// in which objects it evaluates. The parameter hash covers the values and this version, not the rules themselves,
        /// so without the raise two records of the same settings would carry the same version and hash whichever rules
        /// produced them.
        /// <list type="bullet">
        /// <item>1.0: the specification as released.</item>
        /// <item>1.1: a match whose evidence is in silico, or whose annotator searches a library of predicted spectra, is
        /// not evaluated (reason "in_silico") and is no longer a neighbour of the matches that are, so their verdicts can
        /// differ from 1.0's too. Nor is it evidence in their MS1 artefact screen: there it is the unannotated peak it
        /// would be without that library, so its adduct no longer clears a relation and its composition no longer makes it
        /// an in-source parent.</item>
        /// </list>
        /// </remarks>
        public const string RuleVersion = "1.1";

        /// <summary>The source written on a tag this check sets, without the version.</summary>
        public const string TagSourcePrefix = "MsdialLipidRtCheck/";

        /// <summary>The source written on a tag this check sets.</summary>
        [IgnoreMember]
        public static string TagSource => TagSourcePrefix + RuleVersion;

        // ---- user facing ----

        /// <summary>Null means "on for lipidomics projects, off otherwise".</summary>
        [Key(0)]
        public bool? IsEnabled { get; set; }

        [Key(1)]
        public LipidRtOperatingPoint OperatingPoint { get; set; } = LipidRtOperatingPoint.HighPrecision;

        [Key(2)]
        public LipidRtChromatography Chromatography { get; set; } = LipidRtChromatography.Auto;

        [Key(3)]
        public LipidRtPlausibilityProfileKind PlausibilityProfile { get; set; } = LipidRtPlausibilityProfileKind.None;

        [Key(4)]
        public LipidRtClassLevelPolicy ClassLevelPolicy { get; set; } = LipidRtClassLevelPolicy.Corroborated;

        [Key(5)]
        public LipidRtMinorDuplicatePolicy MinorDuplicatePolicy { get; set; } = LipidRtMinorDuplicatePolicy.Allow;

        [Key(6)]
        public LipidRtSecondaryOnlyPolicy SecondaryOnlyPolicy { get; set; } = LipidRtSecondaryOnlyPolicy.Evaluate;

        /// <summary>Promotion and corroboration by the other polarity, when both polarities are available.</summary>
        [Key(7)]
        public bool CrossPolarity { get; set; } = true;

        // ---- run-scaled windows (fractions of the RT span) ----

        /// <summary>Discordance epsilon: 0.05 min on a 20-min run.</summary>
        [Key(8)]
        public double DiscordanceFraction { get; set; } = 0.0025;

        /// <summary>An adjacent gap below this is a tie, not support.</summary>
        [Key(9)]
        public double TieFraction { get; set; } = 0.001;

        /// <summary>A supporting gap must also exceed this fraction of the class median adjacent step.</summary>
        [Key(10)]
        public double StepFraction { get; set; } = 0.25;

        /// <summary>Adduct duplicates within this window are one feature.</summary>
        [Key(11)]
        public double CollapseFraction { get; set; } = 0.0025;

        /// <summary>Co-elution window of the MS1 artefact layer.</summary>
        [Key(12)]
        public double CoelutionFraction { get; set; } = 0.0015;

        /// <summary>No Inconsistent in the last fraction of the span (gradient-end compression).</summary>
        [Key(13)]
        public double TailFraction { get; set; } = 0.05;

        // ---- feature weights (evidence units) ----

        [Key(14)]
        public double LevelWeightChain { get; set; } = 3d;

        [Key(15)]
        public double LevelWeightMsms { get; set; } = 2d;

        [Key(16)]
        public double LevelWeightClass { get; set; } = 1d;

        [Key(17)]
        public double DotWeight { get; set; } = 1d;

        [Key(18)]
        public double DuplicateWeight { get; set; } = 0.5;

        [Key(19)]
        public int DuplicateCap { get; set; } = 2;

        [Key(20)]
        public double HeightWeight { get; set; } = 1d;

        /// <summary>Weight multiplier of a feature whose best member is hard-implausible.</summary>
        [Key(21)]
        public double ImplausibleWeight { get; set; } = 0.25;

        /// <summary>Weight multiplier of a feature all of whose members have an MS1 artefact explanation.</summary>
        [Key(22)]
        public double ArtefactWeight { get; set; } = 0.25;

        // ---- run-level reversed-phase gate ----

        /// <summary>The median within-class carbon slope must exceed this fraction of the span per carbon.</summary>
        [Key(23)]
        public double SlopeFraction { get; set; } = 0.002;

        [Key(24)]
        public double GateConcordance { get; set; } = 0.6;

        [Key(25)]
        public int GateMinPairs { get; set; } = 5;

        // ---- class gate ----

        [Key(26)]
        public double ClassGateConcordance { get; set; } = 0.7;

        [Key(27)]
        public int ClassGateMinPairs { get; set; } = 4;

        // ---- outcome rules ----

        /// <summary>Block when a co-eluting adjacent composition weighs at least this multiple of the feature.</summary>
        [Key(28)]
        public double TieRatio { get; set; } = 1d;

        /// <summary>A feature that wins order conflicts must win them by at least this many evidence units.</summary>
        [Key(29)]
        public double CoreMargin { get; set; } = 2d;

        /// <summary>Magnitude veto: |residual| above this many double-bond steps is not Confirmed (0 = off).</summary>
        [Key(30)]
        public double MagnitudeK { get; set; } = 0.75;

        // ---- MS1 artefact layer ----

        [Key(31)]
        public LipidRtArtefactKinds ArtefactKinds { get; set; } = LipidRtArtefactKinds.Default;

        [Key(32)]
        public double ParentRatio { get; set; } = 1d;

        [Key(33)]
        public double Iso1Ratio { get; set; } = 1.5;

        [Key(34)]
        public double Iso2Ratio { get; set; } = 3d;

        [Key(35)]
        public double MzToleranceDa { get; set; } = 0.002;

        [Key(36)]
        public double MzTolerancePpm { get; set; } = 6d;

        // ---- outcomes ----

        /// <summary>The minimum deficit for Inconsistent.</summary>
        [Key(37)]
        public double InconsistentMinDeficit { get; set; } = 1d;

        /// <summary>"RT and spectrum both excellent" override of a soft plausibility rule: this many axes ...</summary>
        [Key(38)]
        public int SoftOverrideAxes { get; set; } = 3;

        /// <summary>... and at least this weighted dot product.</summary>
        [Key(39)]
        public double SoftOverrideDot { get; set; } = 0.8;

        // ---- cross polarity ----

        [Key(40)]
        public double CrossPolarityToleranceFraction { get; set; } = 0.0025;

        [Key(41)]
        public double CrossPolarityToleranceSd { get; set; } = 3d;

        /// <summary>
        /// Single-run reasons that a Confirmed other-polarity twin may promote. Null or empty means the default set
        /// (Support, ClassGate, Ambiguous).
        /// </summary>
        [Key(42)]
        public List<int> PromotableReasons { get; set; }

        [Key(43)]
        public LipidRtPolarityConflictPolicy PolarityConflict { get; set; } = LipidRtPolarityConflictPolicy.Flag;

        // ---- review-list hygiene and penalties ----

        /// <summary>Allow Inconsistent for oxidised classes (off: their conflicts are NotEvaluable).</summary>
        [Key(44)]
        public bool FlagOxidisedClasses { get; set; } = false;

        /// <summary>Allow Inconsistent in the gradient-end tail (off: NotEvaluable).</summary>
        [Key(45)]
        public bool FlagGradientTail { get; set; } = false;

        [Key(46)]
        public bool TieBlock { get; set; } = true;

        [Key(47)]
        public double PolicyPenalty { get; set; } = 2d;

        [Key(48)]
        public double BlockPenalty { get; set; } = 1.5;

        /// <summary>Supporting and discordant neighbours kept in the evidence record.</summary>
        [Key(49)]
        public int MaxEvidenceItems { get; set; } = 4;

        /// <summary>
        /// Whether the check runs for a project. An explicit value wins; otherwise it runs for lipidomics projects.
        /// </summary>
        public bool IsEnabledFor(TargetOmics targetOmics) {
            return IsEnabled ?? targetOmics == TargetOmics.Lipidomics;
        }

        /// <summary>
        /// Whether processing runs the check: only on a lipidomics project, and only while it is enabled. An explicit
        /// True on another target omics does not make it run, because the rules are lipid retention rules.
        /// </summary>
        public bool RunsFor(TargetOmics targetOmics) {
            return targetOmics == TargetOmics.Lipidomics && IsEnabledFor(targetOmics);
        }

        /// <summary>
        /// The parameter-file lines of the check. The setting lines use the keys of the Console method file, so the
        /// values can be copied into one; the last three lines record what a run did with them.
        /// </summary>
        public List<string> ParametersAsText(TargetOmics targetOmics) {
            return new List<string>
            {
                "Lipid RT check: " + (IsEnabled is bool enabled ? enabled.ToString() : "Auto"),
                "Lipid RT check operating point: " + OperatingPoint,
                "Lipid RT check chromatography: " + Chromatography,
                "Lipid RT check plausibility profile: " + PlausibilityProfile,
                "Lipid RT check class-level matches: " + ClassLevelPolicy,
                "Lipid RT check minor duplicates: " + MinorDuplicatePolicy,
                "Lipid RT check secondary-only features: " + SecondaryOnlyPolicy,
                "Lipid RT check runs for this project: " + RunsFor(targetOmics),
                "Lipid RT check rule version: " + RuleVersion,
                "Lipid RT check parameter hash: " + ComputeHash(),
            };
        }

        /// <summary>The single-run reasons a Confirmed other-polarity twin may promote, as reason codes.</summary>
        /// <remarks>
        /// Stored as integers so that the parameter has no dependency on the algorithm's enum; the default is
        /// Support (14), ClassGate (12) and Ambiguous (13). On the development data Support promotions were 51/51
        /// in scope, while Margin promotions were 5/7 and were left out.
        /// </remarks>
        public IReadOnlyCollection<int> GetPromotableReasons() {
            return PromotableReasons is null || PromotableReasons.Count == 0
                ? DefaultPromotableReasons
                : PromotableReasons;
        }

        [IgnoreMember]
        public static IReadOnlyCollection<int> DefaultPromotableReasons { get; } = new[] { 12, 13, 14 };

        /// <summary>A copy, so that a caller can change one value for one run without touching the project's.</summary>
        public LipidRtCheckParameter Clone() {
            var clone = (LipidRtCheckParameter)MemberwiseClone();
            clone.PromotableReasons = PromotableReasons is null ? null : new List<int>(PromotableReasons);
            return clone;
        }

        /// <summary>
        /// A short, stable fingerprint of every value that affects an outcome, recorded with each verdict so a reader
        /// can tell which settings produced it.
        /// </summary>
        /// <remarks>
        /// Independent of <see cref="IsEnabled"/>, which decides whether the check runs, not what it concludes.
        /// </remarks>
        public string ComputeHash() {
            var text = string.Join(";", CanonicalValues());
            using (var sha = SHA256.Create()) {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                var builder = new StringBuilder(16);
                for (int i = 0; i < 8; i++) {
                    builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
                }
                return builder.ToString();
            }
        }

        private IEnumerable<string> CanonicalValues() {
            yield return "rule=" + RuleVersion;
            yield return "op=" + OperatingPoint;
            yield return "chrom=" + Chromatography;
            yield return "profile=" + PlausibilityProfile;
            yield return "class=" + ClassLevelPolicy;
            yield return "minordup=" + MinorDuplicatePolicy;
            yield return "secondary=" + SecondaryOnlyPolicy;
            yield return "xpol=" + CrossPolarity;
            foreach (var value in new[] {
                DiscordanceFraction, TieFraction, StepFraction, CollapseFraction, CoelutionFraction, TailFraction,
                LevelWeightChain, LevelWeightMsms, LevelWeightClass, DotWeight, DuplicateWeight, HeightWeight,
                ImplausibleWeight, ArtefactWeight, SlopeFraction, GateConcordance, ClassGateConcordance, TieRatio,
                CoreMargin, MagnitudeK, ParentRatio, Iso1Ratio, Iso2Ratio, MzToleranceDa, MzTolerancePpm,
                InconsistentMinDeficit, SoftOverrideDot, CrossPolarityToleranceFraction, CrossPolarityToleranceSd,
                PolicyPenalty, BlockPenalty, }) {
                yield return value.ToString("R", CultureInfo.InvariantCulture);
            }
            yield return "ints=" + string.Join(",", new[] { DuplicateCap, GateMinPairs, ClassGateMinPairs, SoftOverrideAxes, MaxEvidenceItems });
            yield return "art=" + (int)ArtefactKinds;
            yield return "promote=" + string.Join(",", GetPromotableReasons().OrderBy(x => x));
            yield return "conflict=" + PolarityConflict;
            yield return "flags=" + FlagOxidisedClasses + "," + FlagGradientTail + "," + TieBlock;
        }
    }
}

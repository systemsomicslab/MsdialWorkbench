namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt
{
    /// <summary>What the lipid retention-time order check concluded about one object.</summary>
    public enum LipidRtOutcome : byte
    {
        NotEvaluated = 0,
        Confirmed = 1,
        NotEvaluable = 2,
        Inconsistent = 3,
    }

    /// <summary>
    /// Why the check reached its outcome. The numbers are part of the persisted record and must not change.
    /// </summary>
    public enum LipidRtReason : byte
    {
        None = 0,
        /// <summary>Bracketed by the run's other MS/MS-backed annotations of the class, in RT order.</summary>
        ConfirmedRt = 1,
        /// <summary>Not decidable in this run, but a twin in the other polarity was Confirmed by its own evidence.</summary>
        ConfirmedPromotedByPolarity = 2,
        Ineligible = 10,
        /// <summary>The run does not show reversed-phase order, or the analyst said the chromatography is not RP.</summary>
        Gate = 11,
        /// <summary>The class's adjacent compositions are not ordered like reversed phase.</summary>
        ClassGate = 12,
        /// <summary>In some but not all maximum order-consistent subsets.</summary>
        Ambiguous = 13,
        /// <summary>Not bracketed by adjacent compositions.</summary>
        Support = 14,
        /// <summary>Co-elutes with an adjacent composition that weighs at least as much.</summary>
        Tie = 15,
        /// <summary>Explained as an MS1 artefact of a brighter co-eluting ion.</summary>
        Artefact = 16,
        /// <summary>The RT is closer to a neighbouring composition than the class steps predict.</summary>
        Magnitude = 17,
        /// <summary>Wins its order conflicts only narrowly.</summary>
        Margin = 18,
        /// <summary>In no maximum consistent subset, but by less than the Inconsistent threshold.</summary>
        SmallDeficit = 19,
        /// <summary>An order conflict in an oxidised class, which is not flagged.</summary>
        OxNoFlag = 20,
        /// <summary>An order conflict in the gradient-end tail, which is not flagged.</summary>
        TailNoFlag = 21,
        /// <summary>A class-level match withheld by ClassLevelPolicy.</summary>
        PolicyClass = 22,
        /// <summary>A feature of secondary ions only, withheld by SecondaryOnlyPolicy.</summary>
        PolicySecondary = 23,
        /// <summary>A minor duplicate, withheld by MinorDuplicatePolicy.</summary>
        MinorDuplicate = 24,
        /// <summary>A soft plausibility rule (B2/B3) failed and the RT/spectrum override did not apply.</summary>
        PlausibilitySoft = 25,
        /// <summary>Neither adduct- nor polarity-corroborated at the Strict operating point.</summary>
        Uncorroborated = 26,
        /// <summary>Demoted because every other-polarity twin is Inconsistent (PolarityConflict = Demote).</summary>
        PolarityConflict = 27,
        /// <summary>Excluded from every maximum order-consistent subset by a clear deficit. A review item, never a Misannotation tag.</summary>
        InconsistentOrder = 40,
        /// <summary>A hard plausibility rule (B1) failed, whatever the RT says.</summary>
        InconsistentPlausibility = 41,
    }

    /// <summary>The structural level of the representative MS/MS match.</summary>
    public enum LipidRtTagLevel : byte
    {
        None = 0,
        /// <summary>Lipid class resolved (annotation tag 320/420).</summary>
        Class = 1,
        /// <summary>An MS/MS spectrum match without lipid levels (330/430).</summary>
        Msms = 2,
        /// <summary>Lipid chains or positions resolved (310/410, 300/400).</summary>
        Chain = 3,
    }

    /// <summary>Membership of a feature in the maximum order-consistent subsets of its class.</summary>
    public enum LipidRtFeatureStatus : byte
    {
        /// <summary>No discordant partner.</summary>
        Free = 0,
        /// <summary>In every maximum consistent subset.</summary>
        Core = 1,
        /// <summary>In some but not all.</summary>
        Ambiguous = 2,
        /// <summary>In none.</summary>
        Excluded = 3,
    }

    /// <summary>The role of one chain in a lipid name.</summary>
    public enum LipidRtChainRole : byte
    {
        Acyl,
        Alkyl,
        SphingoidBase,
        NAcyl,
        /// <summary>The ester-linked acyl of EOS/ASM/TG_EST/NAGly, written "(FA x:y)".</summary>
        EsterAcyl,
        /// <summary>The O-acyl on a hexose, written "(O-x:y)".</summary>
        OAcyl,
        SterolCore,
        /// <summary>A species-level total of several fatty chains: unresolved.</summary>
        SpeciesCore,
        /// <summary>A species-level total of a sphingoid base and its N-acyl: unresolved.</summary>
        SphingoidCore,
    }

    /// <summary>The overall verdict of a plausibility profile.</summary>
    public enum LipidRtPlausibility : byte
    {
        /// <summary>No profile, or not evaluated.</summary>
        None = 0,
        Plausible = 1,
        Implausible = 2,
        /// <summary>A rule applies but the chain it concerns is not resolved.</summary>
        Untestable = 3,
    }

    public enum LipidRtPlausibilitySeverity : byte
    {
        None = 0,
        Soft = 1,
        Hard = 2,
    }
}

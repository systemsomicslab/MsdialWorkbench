using System.Collections.Generic;

namespace CompMs.Common.StructureFinder.DiagnosticRules
{
    /// <summary>
    /// Every error code of the rule-set format, verbatim from the Python reference (<c>rule_format.ERROR_CODES</c>
    /// in msdial_spectrum_catalog tools/ws8_p3). The first block is the strict reader (design 3.1).
    /// </summary>
    public static class RuleErrorCodes
    {
        public const string Bom = "bom";
        public const string InvalidUtf8 = "invalid_utf8";
        public const string Syntax = "syntax";
        public const string TopLevelNotObject = "top_level_not_object";
        public const string DuplicateKey = "duplicate_key";
        public const string Nonfinite = "nonfinite";
        public const string NestingDepth = "nesting_depth";
        public const string NumberRange = "number_range";
        public const string NegativeZero = "negative_zero";
        public const string NumberForm = "number_form";
        public const string FloatPrecision = "float_precision";
        public const string ControlChar = "control_char";
        public const string NonAscii = "non_ascii";
        public const string MissingFile = "missing_file";
        public const string UnexpectedFile = "unexpected_file";
        public const string UnknownKey = "unknown_key";
        public const string MissingKey = "missing_key";
        public const string BadType = "bad_type";
        public const string BadValue = "bad_value";
        public const string KindFieldChoice = "kind_field_choice";
        public const string FormulaDependentKey = "formula_dependent_key";
        public const string TypedMass = "typed_mass";
        public const string OutOfScopeKind = "out_of_scope_kind";
        public const string OutOfScopeVariable = "out_of_scope_variable";
        public const string BadFormula = "bad_formula";
        public const string FormulaNotHill = "formula_not_hill";
        public const string UnknownElement = "unknown_element";
        public const string MassPrecision = "mass_precision";
        public const string MissingSource = "missing_source";
        public const string DuplicateId = "duplicate_id";
        public const string AdductPolarity = "adduct_polarity";
        public const string AdductNameMismatch = "adduct_name_mismatch";
        public const string MissingRequiredUnit = "missing_required_unit";
        public const string NestingCycle = "nesting_cycle";
        public const string FileNameMismatch = "file_name_mismatch";
        public const string ModeMismatch = "mode_mismatch";
        public const string UnknownTerm = "unknown_term";
        public const string UnknownUnit = "unknown_unit";
        public const string UnknownScaffoldList = "unknown_scaffold_list";
        public const string UnknownIonKey = "unknown_ion_key";
        public const string UnknownAdduct = "unknown_adduct";
        public const string UnknownMotif = "unknown_motif";
        public const string UnknownSourceRef = "unknown_source_ref";
        public const string UnknownVariable = "unknown_variable";
        public const string DuplicateTerm = "duplicate_term";
        public const string UnusedTerm = "unused_term";
        public const string ClaimTermRole = "claim_term_role";
        public const string ClaimTermsTooFew = "claim_terms_too_few";
        public const string RolePlacement = "role_placement";
        public const string LogicNode = "logic_node";
        public const string LogicDepth = "logic_depth";
        public const string KRange = "k_range";
        public const string SeriesTooLarge = "series_too_large";
        public const string SeriesEmpty = "series_empty";
        public const string NegativeCount = "negative_count";
        public const string MassRange = "mass_range";
        public const string ComplementaryPair = "complementary_pair";
        public const string ComplementaryMultiMember = "complementary_multi_member";
        public const string SingleIonClaim = "single_ion_claim";
        public const string ClaimSingleIon = "claim_single_ion";
        public const string SupportTargetIsPrecursor = "support_target_is_precursor";
        public const string OriginInconsistent = "origin_inconsistent";
        public const string ReviewMissing = "review_missing";
        public const string ReviewNotAllowed = "review_not_allowed";
        public const string ReviewStale = "review_stale";
        public const string HistoryFormat = "history_format";
        public const string RulesetUnpinned = "ruleset_unpinned";
        public const string RulesetShaMismatch = "ruleset_sha_mismatch";

        /// <summary>Every code with its description, in the reference's order.</summary>
        public static IReadOnlyList<KeyValuePair<string, string>> All { get; } = new[]
        {
            Pair(Bom, "UTF-8 byte-order mark"),
            Pair(InvalidUtf8, "bytes are not UTF-8"),
            Pair(Syntax, "not RFC 8259 JSON (comments, trailing commas, raw control characters ...)"),
            Pair(TopLevelNotObject, "the top level is not an object"),
            Pair(DuplicateKey, "a key occurs twice in one object"),
            Pair(Nonfinite, "NaN or Infinity"),
            Pair(NestingDepth, "arrays and objects nested deeper than 128"),
            Pair(NumberRange, "an integer above 2^53-1 or a float outside 0 and [1e-4, 1e15)"),
            Pair(NegativeZero, "-0, -0.0"),
            Pair(NumberForm, "a float with an exponent"),
            Pair(FloatPrecision, "a float with more than 15 significant digits"),
            Pair(ControlChar, "a string with a code point below 0x20 or 0x7F"),
            Pair(NonAscii, "a string with a code point above 0x7F"),
            Pair(MissingFile, "a required file is absent"),
            Pair(UnexpectedFile, "a *.json outside the layout (or a staging file without --draft)"),
            Pair(UnknownKey, "a key outside the closed key set"),
            Pair(MissingKey, "a required key is absent"),
            Pair(BadType, "a value of the wrong JSON type (floats must be written with a decimal point)"),
            Pair(BadValue, "a value outside its enum, pattern, length or range"),
            Pair(KindFieldChoice, "not exactly one of the alternative fields (term kind source, unit class/name)"),
            Pair(FormulaDependentKey, "a key that reads a formula (directive: MS2 rules never consult a formula)"),
            Pair(TypedMass, "a typed mass; rule files hold formulas only"),
            Pair(OutOfScopeKind, "a lipid chain construct"),
            Pair(OutOfScopeVariable, "n_chains"),
            Pair(BadFormula, "a formula outside the grammar"),
            Pair(FormulaNotHill, "a formula not in strict Hill order, or an element repeated"),
            Pair(UnknownElement, "an element missing from elements.json"),
            Pair(MassPrecision, "an element or electron mass with more than 12 fraction digits"),
            Pair(MissingSource, "an as_published (or source_defined) row with no source"),
            Pair(DuplicateId, "a table id (adduct name or alias, unit, motif, scaffold, nesting pair) defined twice"),
            Pair(AdductPolarity, "an adduct whose charge contradicts its name or the rule's ion mode"),
            Pair(AdductNameMismatch, "an adduct name that disagrees with multiplier, add and remove"),
            Pair(MissingRequiredUnit, "units.json lacks Hex, dHex, Pen or HexA"),
            Pair(NestingCycle, "motif nesting with a cycle or a self-loop"),
            Pair(FileNameMismatch, "rule_id or list_id differs from the file stem"),
            Pair(ModeMismatch, "the rule_id mode differs from preconditions.ion_mode"),
            Pair(UnknownTerm, "a term id that no term defines"),
            Pair(UnknownUnit, "a unit name missing from units.json"),
            Pair(UnknownScaffoldList, "a scaffold list that does not exist, or scaffold_ions without a scaffold list"),
            Pair(UnknownIonKey, "a scaffold ion key missing (or empty for the mode) in some entry"),
            Pair(UnknownAdduct, "an adduct name missing from adducts.json"),
            Pair(UnknownMotif, "a motif id missing from motifs.json"),
            Pair(UnknownSourceRef, "a provenance token src:<i> past the end of source"),
            Pair(UnknownVariable, "a k variable outside the closed set"),
            Pair(DuplicateTerm, "a term id defined twice"),
            Pair(UnusedTerm, "a non-upgrade term that the logic never uses"),
            Pair(ClaimTermRole, "a claim term that is repeated or is not a support term"),
            Pair(ClaimTermsTooFew, "a measured rule claiming on fewer than two terms (design 3.5)"),
            Pair(RolePlacement, "a role outside its place in the logic (design 3.6 item 3)"),
            Pair(LogicNode, "a logic node that is not exactly one of all, any, not, k_of, term"),
            Pair(LogicDepth, "logic deeper than 16 or larger than 256 nodes"),
            Pair(KRange, "k or its min below 1"),
            Pair(SeriesTooLarge, "more than 100 000 compositions"),
            Pair(SeriesEmpty, "no composition satisfies the unit ranges and totals"),
            Pair(NegativeCount, "a negative count (unit range, ion or precursor composition)"),
            Pair(MassRange, "a mass that is not positive or not below 2^53 pDa"),
            Pair(ComplementaryPair, "a complementary pair of one term, of one kind, of non-support terms, or repeated"),
            Pair(ComplementaryMultiMember, "a complementary term with more than one member in some hypothesis"),
            Pair(SingleIonClaim, "the logic holds with no evidence unit or with one (two-ion check, pass 1)"),
            Pair(ClaimSingleIon, "with every non-claim unit present, no or one claim unit satisfies the logic (pass 2)"),
            Pair(SupportTargetIsPrecursor, "a support or upgrade member equals a hypothesis precursor m/z"),
            Pair(OriginInconsistent, "parameters_origin differs from the derived maximum"),
            Pair(ReviewMissing, "author_reviewed and review disagree"),
            Pair(ReviewNotAllowed, "author_reviewed without source_checked and measure"),
            Pair(ReviewStale, "review.rule_semantic_sha256 differs from the recomputed one"),
            Pair(HistoryFormat, "a malformed or repeated ruleset-history.tsv row"),
            Pair(RulesetUnpinned, "rule_set_version has no ruleset-history.tsv row"),
            Pair(RulesetShaMismatch, "the history row for rule_set_version names another sha256"),
        };

        private static KeyValuePair<string, string> Pair(string code, string description) =>
            new KeyValuePair<string, string>(code, description);
    }
}

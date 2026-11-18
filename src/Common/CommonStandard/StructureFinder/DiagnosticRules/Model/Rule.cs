using CompMs.Common.StructureFinder.DiagnosticRules.Chemistry;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using System.Collections.Generic;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Model
{
    /// <summary>A public PubChem example of a claim.</summary>
    public sealed class ClaimExample
    {
        internal ClaimExample(JsonObject o) {
            PubchemCid = Validated.Int(o, "pubchem_cid");
            Smiles = Validated.Str(o, "smiles");
        }

        public long PubchemCid { get; }
        public string Smiles { get; }
    }

    /// <summary>What a rule claims when it fires (design 3.5).</summary>
    public sealed class Claim
    {
        internal Claim(JsonObject o) {
            MotifId = Validated.Str(o, "motif_id");
            ClaimTermIds = Validated.Strings(Validated.Arr(o, "claim_term_ids"));
            var level = Validated.Str(o, "claim_level");
            ClaimLevel = level == "class" ? Model.ClaimLevel.Class : level == "scaffold" ? Model.ClaimLevel.Scaffold : Model.ClaimLevel.Substituent;
            NonClaims = Validated.Strings(Validated.Arr(o, "non_claims"));
            PositiveExamples = Examples(Validated.Arr(o, "positive_examples"));
            NegativeExamples = Examples(Validated.Arr(o, "negative_examples"));
        }

        public string MotifId { get; }
        public IReadOnlyList<string> ClaimTermIds { get; }
        public ClaimLevel ClaimLevel { get; }
        public IReadOnlyList<string> NonClaims { get; }
        public IReadOnlyList<ClaimExample> PositiveExamples { get; }
        public IReadOnlyList<ClaimExample> NegativeExamples { get; }

        private static IReadOnlyList<ClaimExample> Examples(JsonArray a) {
            var list = new List<ClaimExample>();
            foreach (var x in a) {
                list.Add(new ClaimExample((JsonObject)x));
            }
            return Validated.List(list);
        }
    }

    /// <summary>The product tolerance a review measured at: {abs_da, ppm, source}.</summary>
    public sealed class ReviewedProductTolerance
    {
        internal ReviewedProductTolerance(JsonObject o) {
            AbsDa = Validated.Float(o, "abs_da");
            Ppm = Validated.Float(o, "ppm");
            Source = Validated.Str(o, "source") == "p2" ? ProductToleranceKind.P2 : ProductToleranceKind.Msfinder;
        }

        public double AbsDa { get; }
        public double Ppm { get; }
        public ProductToleranceKind Source { get; }
    }

    /// <summary>The author's review of a rule (design 3.5), with the tolerances it was measured at.</summary>
    public sealed class Review
    {
        internal Review(JsonObject o) {
            By = Validated.Str(o, "by");
            At = Validated.Str(o, "at");
            Basis = Validated.Str(o, "basis");
            RunId = Validated.Str(o, "run_id");
            RulesPrecisionSha256 = Validated.Str(o, "rules_precision_sha256");
            ReviewProposalSha256 = Validated.Str(o, "review_proposal_sha256");
            RuleSemanticSha256 = Validated.Str(o, "rule_semantic_sha256");
            InstrumentClasses = Validated.Strings(Validated.Arr(o, "instrument_classes"));
            ProductTolerance = new ReviewedProductTolerance(Validated.Obj(o, "product_tolerance"));
            PrecursorToleranceDa = Validated.Float(o, "precursor_tolerance_da");
        }

        public string By { get; }
        /// <summary>yyyy-mm-dd.</summary>
        public string At { get; }
        public string Basis { get; }
        public string RunId { get; }
        public string RulesPrecisionSha256 { get; }
        public string ReviewProposalSha256 { get; }
        public string RuleSemanticSha256 { get; }
        public IReadOnlyList<string> InstrumentClasses { get; }
        public ReviewedProductTolerance ProductTolerance { get; }
        public double PrecursorToleranceDa { get; }
    }

    /// <summary>An inclusive [min, max] count range.</summary>
    public readonly struct CountRange
    {
        public CountRange(long min, long max) {
            Min = min;
            Max = max;
        }

        public long Min { get; }
        public long Max { get; }
    }

    /// <summary>The composition a rule's hypotheses range over (design 3.5, 4.2).</summary>
    public sealed class CompositionSpec
    {
        internal CompositionSpec(JsonObject o) {
            Core = Validated.StrOrNull(o, "core");
            ScaffoldList = Validated.StrOrNull(o, "scaffold_list");
            Units = Ranges(Validated.Obj(o, "units"));
            UnitTotals = Ranges(Validated.Obj(o, "unit_totals"));
        }

        public string? Core { get; }
        public string? ScaffoldList { get; }
        /// <summary>Unit name to count range, in file order.</summary>
        public IReadOnlyList<KeyValuePair<string, CountRange>> Units { get; }
        /// <summary>Unit class name (sugar, acyl, conjugate) to total range, in file order.</summary>
        public IReadOnlyList<KeyValuePair<string, CountRange>> UnitTotals { get; }

        private static IReadOnlyList<KeyValuePair<string, CountRange>> Ranges(JsonObject o) {
            var list = new List<KeyValuePair<string, CountRange>>();
            foreach (var m in o.Members) {
                var a = (JsonArray)m.Value;
                list.Add(new KeyValuePair<string, CountRange>(m.Key, new CountRange(((JsonInteger)a[0]).Value, ((JsonInteger)a[1]).Value)));
            }
            return Validated.List(list);
        }
    }

    /// <summary>A prior or a competitor note of a rule's preconditions.</summary>
    public sealed class PreconditionNote
    {
        internal PreconditionNote(string key, string note) {
            Key = key;
            Note = note;
        }

        /// <summary>The source token (priors) or the motif id (competitors).</summary>
        public string Key { get; }
        public string Note { get; }
    }

    public sealed class Preconditions
    {
        internal Preconditions(JsonObject o) {
            IonMode = Validated.Str(o, "ion_mode") == "pos" ? RuleIonMode.Positive : RuleIonMode.Negative;
            Adducts = Validated.Strings(Validated.Arr(o, "adducts"));
            PrecursorToleranceDa = Validated.Float(o, "precursor_tolerance_da");
            Composition = new CompositionSpec(Validated.Obj(o, "composition"));
            Priors = Notes(Validated.Arr(o, "priors"), "source");
            Competitors = Notes(Validated.Arr(o, "competitors"), "motif_id");
        }

        public RuleIonMode IonMode { get; }
        /// <summary>Canonical adduct names in the rule's try order.</summary>
        public IReadOnlyList<string> Adducts { get; }
        public double PrecursorToleranceDa { get; }
        public CompositionSpec Composition { get; }
        public IReadOnlyList<PreconditionNote> Priors { get; }
        public IReadOnlyList<PreconditionNote> Competitors { get; }

        private static IReadOnlyList<PreconditionNote> Notes(JsonArray a, string key) {
            var list = new List<PreconditionNote>();
            foreach (var x in a) {
                var n = (JsonObject)x;
                list.Add(new PreconditionNote(Validated.Str(n, key), Validated.Str(n, "note")));
            }
            return Validated.List(list);
        }
    }

    /// <summary>
    /// A rule record <c>rules/&lt;rule_id&gt;.json</c> (design 3.5) that passed every load-time check, with the
    /// adducts it tries in adduct_ambiguous order and its <c>rule_semantic_sha256</c>.
    /// </summary>
    public sealed class Rule
    {
        internal Rule(JsonObject doc, ElementTable elements, IReadOnlyList<AdductTry> adductsTried, string semanticSha256) {
            Document = doc;
            RuleId = Validated.Str(doc, "rule_id");
            Version = Validated.Int(doc, "version");
            Source = Validated.Citations(doc, "source");
            TranscribedBy = Validated.Str(doc, "transcribed_by");
            SourceChecked = Validated.Bool(doc, "source_checked");
            AuthorReviewed = Validated.Bool(doc, "author_reviewed");
            Measure = Validated.Bool(doc, "measure");
            Review = doc["review"] is JsonObject review ? new Review(review) : null;
            ParametersOrigin = RuleNames.ParseOrigin(Validated.Str(doc, "parameters_origin"));
            PrimaryPreconditionMode = Validated.Str(doc, "primary_precondition_mode") == "mz_only" ? PreconditionMode.MzOnly : PreconditionMode.AdductAmbiguous;
            CalibratedOn = Validated.Strings(Validated.Arr(doc, "calibrated_on"));
            Convention = Validated.Str(doc, "convention") == "p2" ? RuleConvention.P2 : RuleConvention.Msdial;
            ProductTolerance = Validated.Str(doc, "product_tolerance") == "p2" ? ProductToleranceKind.P2 : ProductToleranceKind.Msfinder;
            LossReference = Validated.Str(doc, "loss_reference") == "hypothesis" ? Model.LossReference.Hypothesis : Model.LossReference.Observed;
            VetoExcludesMotif = Validated.Bool(doc, "veto_excludes_motif");
            Claim = new Claim(Validated.Obj(doc, "claim"));
            Preconditions = new Preconditions(Validated.Obj(doc, "preconditions"));
            var terms = new List<Term>();
            var termArray = Validated.Arr(doc, "terms");
            for (var i = 0; i < termArray.Count; i++) {
                terms.Add(new Term(i, (JsonObject)termArray[i], elements));
            }
            Terms = Validated.List(terms);
            var pairs = new List<KeyValuePair<string, string>>();
            foreach (var x in Validated.Arr(doc, "complementary")) {
                var pair = (JsonArray)x;
                pairs.Add(new KeyValuePair<string, string>(((JsonString)pair[0]).Value, ((JsonString)pair[1]).Value));
            }
            Complementary = Validated.List(pairs);
            Logic = LogicNode.From(Validated.Obj(doc, "logic"), "/logic");
            Notes = Validated.Str(doc, "notes");
            AdductsTried = adductsTried;
            SemanticSha256 = semanticSha256;
        }

        public string RuleId { get; }
        public long Version { get; }
        public IReadOnlyList<Citation> Source { get; }
        public string TranscribedBy { get; }
        public bool SourceChecked { get; }
        public bool AuthorReviewed { get; }
        public bool Measure { get; }
        public Review? Review { get; }
        public ParametersOrigin ParametersOrigin { get; }
        public PreconditionMode PrimaryPreconditionMode { get; }
        public IReadOnlyList<string> CalibratedOn { get; }
        public RuleConvention Convention { get; }
        public ProductToleranceKind ProductTolerance { get; }
        public LossReference LossReference { get; }
        public bool VetoExcludesMotif { get; }
        public Claim Claim { get; }
        public Preconditions Preconditions { get; }
        public IReadOnlyList<Term> Terms { get; }
        public IReadOnlyList<KeyValuePair<string, string>> Complementary { get; }
        public LogicNode Logic { get; }
        public string Notes { get; }

        /// <summary>The adducts tried in adduct_ambiguous mode (design 4.3): the rule's list, then the common adducts
        /// of its polarity that it does not list, in table order.</summary>
        public IReadOnlyList<AdductTry> AdductsTried { get; }

        /// <summary>The rule_semantic_sha256 recomputed at load (design 3.2).</summary>
        public string SemanticSha256 { get; }

        internal JsonObject Document { get; }
    }

    /// <summary>An adduct a rule tries, and whether it came from the rule's list or the common adducts.</summary>
    public sealed class AdductTry
    {
        internal AdductTry(AdductRow adduct, bool fromRule) {
            Adduct = adduct;
            FromRule = fromRule;
        }

        public AdductRow Adduct { get; }
        /// <summary>True for the rule's own list ("rule"), false for a common adduct of the polarity ("common").</summary>
        public bool FromRule { get; }
        public string SourceName => FromRule ? "rule" : "common";
    }
}

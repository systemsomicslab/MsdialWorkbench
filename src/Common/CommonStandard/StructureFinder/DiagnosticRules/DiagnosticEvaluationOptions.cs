using CompMs.Common.StructureFinder.DiagnosticRules.Model;

namespace CompMs.Common.StructureFinder.DiagnosticRules
{
    /// <summary>Which rules an evaluator evaluates (design 4.10). The others are listed as <c>not_measured</c>.</summary>
    public enum RuleFilter
    {
        /// <summary>Rules with <c>measure</c> and <c>source_checked</c> (the default).</summary>
        MeasuredAndSourceChecked,
        /// <summary>Rules with <c>author_reviewed</c>.</summary>
        AuthorReviewed,
        /// <summary>Every rule.</summary>
        All,
    }

    /// <summary>What happens to a rule whose support terms can coincide without a complementary declaration (design 4.8).</summary>
    public enum OverlapMode
    {
        /// <summary>The rule is refused (<c>overlap_undeclared</c>) and not evaluated (the default).</summary>
        Refuse,
        /// <summary>Nothing is refused; the exclusive assignment resolves conflicts at run time (tolerance and decoy
        /// variants).</summary>
        Runtime,
    }

    /// <summary>
    /// A product-ion tolerance: at target m/z t it is <c>Math.Max(AbsDa, t * Ppm / 1000000.0)</c> (design 4.4). A pure
    /// Da tolerance has Ppm 0 and a pure ppm tolerance has AbsDa 0; <c>p2</c> is {0.005, 10}.
    /// </summary>
    public readonly struct ProductTolerance
    {
        public ProductTolerance(double absDa, double ppm) {
            AbsDa = absDa;
            Ppm = ppm;
        }

        public double AbsDa { get; }

        public double Ppm { get; }

        /// <summary>A tolerance in Da only.</summary>
        public static ProductTolerance Da(double absDa) => new ProductTolerance(absDa, 0.0);

        /// <summary>A tolerance in ppm only.</summary>
        public static ProductTolerance FromPpm(double ppm) => new ProductTolerance(0.0, ppm);
    }

    /// <summary>
    /// Options of a <c>DiagnosticRuleEvaluator</c> (design 4.10). The object is mutable; the evaluator copies and
    /// checks the values once, at construction, and is unaffected by later changes. Every option except
    /// <see cref="Filter"/>, <see cref="MsfinderProductTolerance"/>, <see cref="Strict"/> and <see cref="Verbose"/> is a
    /// variant option: at a non-default value it makes every outcome claim-ineligible (design 4.9).
    /// </summary>
    public sealed class DiagnosticEvaluationOptions
    {
        /// <summary>The default <see cref="AssignmentCap"/>.</summary>
        public const int DefaultAssignmentCap = 4096;

        public RuleFilter Filter { get; set; } = RuleFilter.MeasuredAndSourceChecked;

        /// <summary>The precursor tolerance in Da for every rule; null uses each rule's <c>precursor_tolerance_da</c>.</summary>
        public double? PrecursorToleranceDa { get; set; }

        /// <summary>A product tolerance for every rule, whatever it declares; null uses the declared one.</summary>
        public ProductTolerance? ProductToleranceOverride { get; set; }

        /// <summary>
        /// The tolerance of rules that declare <c>msfinder</c>: MS-FINDER's Ms2Tolerance setting. Like every double
        /// option it may have at most 6 decimals, because IdentityString writes it with 6; a float setting is passed
        /// through its decimal text (<c>double.Parse(f.ToString("R", CultureInfo.InvariantCulture),
        /// CultureInfo.InvariantCulture)</c>), which is also what makes it equal to a review's tolerance (design 4.9).
        /// </summary>
        public ProductTolerance MsfinderProductTolerance { get; set; } = ProductTolerance.Da(0.01);

        /// <summary>A loss reference for every rule; null uses each rule's <c>loss_reference</c>.</summary>
        public LossReference? LossReferenceOverride { get; set; }

        /// <summary>The decoy shift in whole Da, added to every term mass before ionisation (design 4.5).</summary>
        public int DecoyShiftDa { get; set; }

        /// <summary>Multiplies every intensity floor.</summary>
        public double FloorScale { get; set; } = 1.0;

        /// <summary>Added to every k of a k_of node; k_eff = max(1, k + KOffset) (design 4.7).</summary>
        public int KOffset { get; set; }

        public OverlapMode Overlap { get; set; } = OverlapMode.Refuse;

        /// <summary>The leaf cap of the exclusive assignment (design 4.6).</summary>
        public int AssignmentCap { get; set; } = DefaultAssignmentCap;

        /// <summary>A refused rule makes the evaluator constructor throw.</summary>
        public bool Strict { get; set; }

        /// <summary>Results list every hypothesis of every outcome, and every outcome.</summary>
        public bool Verbose { get; set; }
    }
}

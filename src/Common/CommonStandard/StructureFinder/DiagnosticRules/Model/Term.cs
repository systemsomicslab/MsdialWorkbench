using CompMs.Common.StructureFinder.DiagnosticRules.Chemistry;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using System.Collections.Generic;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Model
{
    /// <summary>The unit source of a product_ion or loss_from_precursor term: a unit class or name, plus and minus formulas.</summary>
    public sealed class TermUnit
    {
        internal TermUnit(JsonObject unit, ElementTable elements) {
            if (unit.ContainsKey("class")) {
                Class = RuleNames.ParseUnitClass(Validated.Str(unit, "class"));
            }
            else {
                Name = Validated.Str(unit, "name");
            }
            Plus = Validated.StrOrNull(unit, "plus");
            Minus = Validated.StrOrNull(unit, "minus");
            PlusComp = Plus is null ? null : Composition.Parse(Plus, elements);
            MinusComp = Minus is null ? null : Composition.Parse(Minus, elements);
        }

        /// <summary>The unit class, or null when the term names one unit.</summary>
        public UnitClass? Class { get; }
        /// <summary>The unit name, or null when the term selects a class.</summary>
        public string? Name { get; }
        public string? Plus { get; }
        public string? Minus { get; }

        internal long[]? PlusComp { get; }
        internal long[]? MinusComp { get; }
    }

    /// <summary>A term of a rule (design 4.5): one diagnostic ion, loss, aglycone ion, ion series or veto window.</summary>
    public sealed class Term
    {
        internal Term(int index, JsonObject t, ElementTable elements) {
            Index = index;
            Document = t;
            TermId = Validated.Str(t, "term_id");
            Kind = RuleNames.ParseKind(Validated.Str(t, "kind"));
            Role = RuleNames.ParseRole(Validated.Str(t, "role"));
            TermSource = Validated.Str(t, "term_source");
            MinRelIntensity = Validated.Float(t, "min_rel_intensity");
            FloorSource = Validated.Str(t, "floor_source");
            Than = t["than"] is JsonArray than ? Validated.Strings(than) : null;
            Ratio = Validated.FloatOrNull(t, "ratio");
            RatioSource = Validated.StrOrNull(t, "ratio_source");
            Note = Validated.StrOrNull(t, "note");
            if (t.ContainsKey("formula")) {
                Formula = Validated.Str(t, "formula");
                FormulaComp = Composition.Parse(Formula, elements);
            }
            if (t.ContainsKey("unit")) {
                Unit = new TermUnit(Validated.Obj(t, "unit"), elements);
            }
            if (t.ContainsKey("scaffold_ions")) {
                ScaffoldIons = Validated.Str(t, "scaffold_ions");
            }
            if (t.ContainsKey("form")) {
                Form = Validated.Str(t, "form") == "even" ? IonForm.Even : IonForm.Radical;
                ChargeCarrier = Validated.Str(t, "charge_carrier") == "proton" ? Model.ChargeCarrier.Proton : Model.ChargeCarrier.Adduct;
                Plus = Validated.StrOrNull(t, "plus");
                Minus = Validated.StrOrNull(t, "minus");
                PlusComp = Plus is null ? null : Composition.Parse(Plus, elements);
                MinusComp = Minus is null ? null : Composition.Parse(Minus, elements);
            }
            if (t.ContainsKey("unit_classes")) {
                var classes = new List<UnitClass>();
                foreach (var x in Validated.Arr(t, "unit_classes")) {
                    classes.Add(RuleNames.ParseUnitClass(((JsonString)x).Value));
                }
                UnitClasses = Validated.List(classes);
            }
            if (t.ContainsKey("as")) {
                WindowReference = Validated.Str(t, "as") == "ion" ? Model.WindowReference.Ion : Model.WindowReference.Loss;
                HalfWidthDa = Validated.Float(t, "half_width_da");
            }
        }

        /// <summary>The position of the term in the rule's terms array.</summary>
        public int Index { get; }
        public string TermId { get; }
        public TermKind Kind { get; }
        public TermRole Role { get; }
        /// <summary>Provenance token: src:&lt;i&gt;, tuned_vs20 or tuned_pooled.</summary>
        public string TermSource { get; }
        /// <summary>The intensity floor on the convention's scale.</summary>
        public double MinRelIntensity { get; }
        public string FloorSource { get; }
        public IReadOnlyList<string>? Than { get; }
        public double? Ratio { get; }
        public string? RatioSource { get; }
        public string? Note { get; }

        /// <summary>product_ion, loss_from_precursor or window: a fixed formula.</summary>
        public string? Formula { get; }
        /// <summary>product_ion or loss_from_precursor: a unit class or name.</summary>
        public TermUnit? Unit { get; }
        /// <summary>product_ion: a scaffold-ion key of the rule's scaffold list.</summary>
        public string? ScaffoldIons { get; }
        /// <summary>aglycone_ion and ion_series.</summary>
        public IonForm? Form { get; }
        public ChargeCarrier? ChargeCarrier { get; }
        public string? Plus { get; }
        public string? Minus { get; }
        /// <summary>ion_series.</summary>
        public IReadOnlyList<UnitClass>? UnitClasses { get; }
        /// <summary>window (veto only).</summary>
        public WindowReference? WindowReference { get; }
        public double? HalfWidthDa { get; }

        internal JsonObject Document { get; }
        internal long[]? FormulaComp { get; }
        internal long[]? PlusComp { get; }
        internal long[]? MinusComp { get; }
    }

    /// <summary>k of a k_of node: a constant, or max(min, value(var) + offset) (design 4.7).</summary>
    public sealed class KSpec
    {
        internal KSpec(JsonValue k) {
            if (k is JsonInteger i) {
                Constant = i.Value;
            }
            else {
                var o = (JsonObject)k;
                Variable = RuleNames.ParseVariable(Validated.Str(o, "var"));
                Offset = Validated.Int(o, "offset");
                Min = Validated.Int(o, "min");
            }
        }

        /// <summary>The constant k, or null for a variable k.</summary>
        public long? Constant { get; }
        public KVariable? Variable { get; }
        public long Offset { get; }
        public long Min { get; }
    }

    /// <summary>A node of a rule's logic: all, any, not, k_of or term (design 4.7).</summary>
    public sealed class LogicNode
    {
        private LogicNode(LogicNodeKind kind, string pointer, IReadOnlyList<LogicNode> children, string? termId,
                          KSpec? k, string? kSource) {
            Kind = kind;
            Pointer = pointer;
            Children = children;
            TermId = termId;
            K = k;
            KSource = kSource;
        }

        public LogicNodeKind Kind { get; }
        /// <summary>The RFC 6901 pointer of this node in the rule file.</summary>
        public string Pointer { get; }
        /// <summary>all and any: the children; not: one child; k_of: the of-children; term: none.</summary>
        public IReadOnlyList<LogicNode> Children { get; }
        public string? TermId { get; }
        public KSpec? K { get; }
        public string? KSource { get; }

        internal static LogicNode From(JsonObject node, string pointer) {
            var member = node.Members[0];
            var p = Pointers.Child(pointer, member.Key);
            switch (member.Key) {
                case "term":
                    return new LogicNode(LogicNodeKind.Term, pointer, new LogicNode[0], ((JsonString)member.Value).Value, null, null);
                case "all":
                case "any": {
                    var children = new List<LogicNode>();
                    var items = (JsonArray)member.Value;
                    for (var i = 0; i < items.Count; i++) {
                        children.Add(From((JsonObject)items[i], Pointers.Child(p, i)));
                    }
                    return new LogicNode(member.Key == "all" ? LogicNodeKind.All : LogicNodeKind.Any, pointer,
                                         Validated.List(children), null, null, null);
                }
                case "not":
                    return new LogicNode(LogicNodeKind.Not, pointer, new[] { From((JsonObject)member.Value, p) }, null, null, null);
                default: {
                    var kof = (JsonObject)member.Value;
                    var children = new List<LogicNode>();
                    var items = Validated.Arr(kof, "of");
                    var of = Pointers.Child(p, "of");
                    for (var i = 0; i < items.Count; i++) {
                        children.Add(From((JsonObject)items[i], Pointers.Child(of, i)));
                    }
                    return new LogicNode(LogicNodeKind.KOf, pointer, Validated.List(children), null, new KSpec(kof["k"]),
                                         Validated.Str(kof, "k_source"));
                }
            }
        }
    }
}

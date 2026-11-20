using CompMs.Common.StructureFinder.DiagnosticRules.Chemistry;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Model
{
    /// <summary>Reads fields of a document the loader has already validated.</summary>
    internal static class Validated
    {
        public static string Str(JsonObject o, string key) => ((JsonString)o[key]).Value;

        public static string? StrOrNull(JsonObject o, string key) => o[key] is JsonString s ? s.Value : null;

        public static bool Bool(JsonObject o, string key) => ((JsonBoolean)o[key]).Value;

        public static long Int(JsonObject o, string key) => ((JsonInteger)o[key]).Value;

        public static double Float(JsonObject o, string key) => ((JsonFloat)o[key]).Value;

        public static double? FloatOrNull(JsonObject o, string key) => o[key] is JsonFloat f ? f.Value : (double?)null;

        public static JsonArray Arr(JsonObject o, string key) => (JsonArray)o[key];

        public static JsonObject Obj(JsonObject o, string key) => (JsonObject)o[key];

        public static IReadOnlyList<string> Strings(JsonArray a) {
            var list = new List<string>();
            foreach (var x in a) {
                list.Add(((JsonString)x).Value);
            }
            return new ReadOnlyCollection<string>(list);
        }

        public static IReadOnlyList<T> List<T>(IEnumerable<T> items) => new ReadOnlyCollection<T>(new List<T>(items));

        public static IReadOnlyList<Citation> Citations(JsonObject o, string key) {
            var list = new List<Citation>();
            foreach (var x in Arr(o, key)) {
                list.Add(Citation.From((JsonObject)x));
            }
            return new ReadOnlyCollection<Citation>(list);
        }
    }

    /// <summary>A page-level citation: a DOI or a free reference, with its locator (page, table or figure).</summary>
    public sealed class Citation
    {
        private Citation(string? doi, string? reference, string locator) {
            Doi = doi;
            Reference = reference;
            Locator = locator;
        }

        public string? Doi { get; }
        public string? Reference { get; }
        public string Locator { get; }

        internal static Citation From(JsonObject o) =>
            new Citation(o.ContainsKey("doi") ? Validated.Str(o, "doi") : null,
                         o.ContainsKey("reference") ? Validated.Str(o, "reference") : null,
                         Validated.Str(o, "locator"));
    }

    /// <summary>A row of tables/adducts.json (design 3.4).</summary>
    public sealed class AdductRow
    {
        internal AdductRow(JsonObject row, ElementTable elements, long[] add, long[] remove) {
            Row = row;
            Adduct = Validated.Str(row, "adduct");
            Aliases = Validated.Strings(Validated.Arr(row, "aliases"));
            Charge = (int)Validated.Int(row, "charge");
            Multiplier = Validated.Int(row, "multiplier");
            AddFormula = Validated.StrOrNull(row, "add");
            RemoveFormula = Validated.StrOrNull(row, "remove");
            Add = add;
            Remove = remove;
            Common = Validated.Bool(row, "common");
            Origin = RuleNames.ParseOrigin(Validated.Str(row, "origin"));
            OriginText = Validated.Str(row, "origin");
            Source = Validated.Citations(row, "source");
        }

        /// <summary>The canonical name, for example [M+H]+.</summary>
        public string Adduct { get; }
        public IReadOnlyList<string> Aliases { get; }
        /// <summary>+1 or -1.</summary>
        public int Charge { get; }
        public long Multiplier { get; }
        public string? AddFormula { get; }
        public string? RemoveFormula { get; }
        public bool Common { get; }
        public ParametersOrigin Origin { get; }
        public IReadOnlyList<Citation> Source { get; }

        /// <summary>The added composition (empty for null), indexed by the element table.</summary>
        internal long[] Add { get; }
        internal long[] Remove { get; }
        internal string OriginText { get; }
        internal JsonObject Row { get; }
    }

    /// <summary>A row of tables/units.json (design 3.4): a condensation residue.</summary>
    public sealed class UnitRow
    {
        internal UnitRow(JsonObject row, long[] residue) {
            Row = row;
            Name = Validated.Str(row, "name");
            ClassText = Validated.Str(row, "class");
            Class = RuleNames.ParseUnitClass(ClassText);
            ResidueFormula = Validated.Str(row, "residue");
            Residue = residue;
            OriginText = Validated.Str(row, "origin");
            Origin = RuleNames.ParseOrigin(OriginText);
            Source = Validated.Citations(row, "source");
        }

        public string Name { get; }
        public UnitClass Class { get; }
        public string ResidueFormula { get; }
        public ParametersOrigin Origin { get; }
        public IReadOnlyList<Citation> Source { get; }

        internal string ClassText { get; }
        internal long[] Residue { get; }
        internal string OriginText { get; }
        internal JsonObject Row { get; }
    }

    /// <summary>A ChemOnt node a motif is compatible with or broader than (report only).</summary>
    public sealed class ChemOntNode
    {
        internal ChemOntNode(string id, string name) {
            Id = id;
            Name = name;
        }

        public string Id { get; }
        public string Name { get; }
    }

    /// <summary>A row of tables/motifs.json (design 3.4).</summary>
    public sealed class MotifRow
    {
        internal MotifRow(JsonObject row) {
            Row = row;
            MotifId = Validated.Str(row, "motif_id");
            Smarts = Validated.Str(row, "smarts");
            OriginText = Validated.Str(row, "origin");
            Origin = RuleNames.ParseMotifOrigin(OriginText);
            Source = Validated.Citations(row, "source");
            var chemont = Validated.Obj(row, "chemont");
            Compatible = Nodes(Validated.Arr(chemont, "compatible"));
            Broader = Nodes(Validated.Arr(chemont, "broader"));
        }

        public string MotifId { get; }
        public string Smarts { get; }
        public MotifOrigin Origin { get; }
        public IReadOnlyList<Citation> Source { get; }
        public IReadOnlyList<ChemOntNode> Compatible { get; }
        public IReadOnlyList<ChemOntNode> Broader { get; }

        internal string OriginText { get; }
        internal JsonObject Row { get; }

        private static IReadOnlyList<ChemOntNode> Nodes(JsonArray a) {
            var list = new List<ChemOntNode>();
            foreach (var x in a) {
                var o = (JsonObject)x;
                list.Add(new ChemOntNode(Validated.Str(o, "id"), Validated.Str(o, "name")));
            }
            return new ReadOnlyCollection<ChemOntNode>(list);
        }
    }

    /// <summary>A row of tables/motif_nesting.json: child is nested under parent. Never inferred from SMARTS.</summary>
    public sealed class MotifNesting
    {
        internal MotifNesting(JsonObject row) {
            Child = Validated.Str(row, "child");
            Parent = Validated.Str(row, "parent");
            Source = row.ContainsKey("source") ? Validated.Citations(row, "source") : new ReadOnlyCollection<Citation>(new Citation[0]);
        }

        public string Child { get; }
        public string Parent { get; }
        public IReadOnlyList<Citation> Source { get; }
    }

    /// <summary>The ions of one scaffold-ion key: elemental compositions of ions per mode.</summary>
    public sealed class ScaffoldIons
    {
        internal ScaffoldIons(IReadOnlyList<string> positive, IReadOnlyList<long[]> positiveComps,
                              IReadOnlyList<string> negative, IReadOnlyList<long[]> negativeComps) {
            Positive = positive;
            Negative = negative;
            PositiveComps = positiveComps;
            NegativeComps = negativeComps;
        }

        public IReadOnlyList<string> Positive { get; }
        public IReadOnlyList<string> Negative { get; }

        internal IReadOnlyList<long[]> PositiveComps { get; }
        internal IReadOnlyList<long[]> NegativeComps { get; }

        internal IReadOnlyList<long[]> Comps(RuleIonMode mode) => mode == RuleIonMode.Positive ? PositiveComps : NegativeComps;
    }

    /// <summary>An entry of scaffolds/&lt;list_id&gt;.json.</summary>
    public sealed class ScaffoldEntry
    {
        internal ScaffoldEntry(JsonObject entry, long[] formula, IReadOnlyList<string> ionKeys, IReadOnlyDictionary<string, ScaffoldIons> ions) {
            Entry = entry;
            ScaffoldId = Validated.Str(entry, "scaffold_id");
            Name = Validated.Str(entry, "name");
            FormulaText = Validated.Str(entry, "formula");
            Formula = formula;
            Class = Validated.Str(entry, "class");
            IonKeys = ionKeys;
            Ions = ions;
            OriginText = Validated.Str(entry, "origin");
            Origin = RuleNames.ParseOrigin(OriginText);
            Source = Validated.Citations(entry, "source");
        }

        public string ScaffoldId { get; }
        public string Name { get; }
        public string FormulaText { get; }
        public string Class { get; }
        /// <summary>Scaffold-ion keys in file order.</summary>
        public IReadOnlyList<string> IonKeys { get; }
        public IReadOnlyDictionary<string, ScaffoldIons> Ions { get; }
        public ParametersOrigin Origin { get; }
        public IReadOnlyList<Citation> Source { get; }

        internal long[] Formula { get; }
        internal string OriginText { get; }
        internal JsonObject Entry { get; }
    }

    /// <summary>scaffolds/&lt;list_id&gt;.json: an enumerated list of scaffolds a rule's composition ranges over.</summary>
    public sealed class ScaffoldList
    {
        internal ScaffoldList(JsonObject document, IReadOnlyList<ScaffoldEntry> entries) {
            Document = document;
            ListId = Validated.Str(document, "list_id");
            Entries = entries;
        }

        public string ListId { get; }
        public IReadOnlyList<ScaffoldEntry> Entries { get; }

        internal JsonObject Document { get; }
    }
}

using CompMs.Common.StructureFinder.DiagnosticRules.Chemistry;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.Common.StructureFinder.DiagnosticRules
{
    /// <summary>Where a rule set came from.</summary>
    public enum RuleSetSource
    {
        /// <summary>The set embedded in this assembly; its version must be pinned in ruleset-history.tsv.</summary>
        Embedded,
        /// <summary>A rule-set directory on disk (development); loaded unpinned.</summary>
        Directory,
        /// <summary>Files given in memory (tests); loaded unpinned.</summary>
        Inline,
    }

    /// <summary>A file of a rule set and its canonical sha256 (design 3.2).</summary>
    public sealed class RuleSetFile
    {
        internal RuleSetFile(string path, string sha256) {
            Path = path;
            Sha256 = sha256;
        }

        /// <summary>The path relative to the RuleSet folder, with '/'.</summary>
        public string Path { get; }
        public string Sha256 { get; }
    }

    /// <summary>What every output that uses a rule set records about it (design 3.3).</summary>
    public sealed class RuleSetIdentity
    {
        internal RuleSetIdentity(string version, string sha256, RuleSetSource source, bool pinned, IReadOnlyList<RuleSetFile> files) {
            Version = version;
            Sha256 = sha256;
            Source = source;
            Pinned = pinned;
            Files = files;
        }

        /// <summary>rule_set_version of ruleset.json.</summary>
        public string Version { get; }
        /// <summary>The rule-set sha256 over the canonical sha256 of every hashed file.</summary>
        public string Sha256 { get; }
        public RuleSetSource Source { get; }
        /// <summary>True when the version and sha256 were checked against ruleset-history.tsv (the embedded set).</summary>
        public bool Pinned { get; }
        /// <summary>Every hashed file in path order (code-point order).</summary>
        public IReadOnlyList<RuleSetFile> Files { get; }
    }

    /// <summary>
    /// A validated, immutable diagnostic-rule set (design 3). A load is all or nothing: any error refuses the whole
    /// set with a <see cref="DiagnosticRuleSetException"/> that lists every error, because a partial load would make
    /// the reported sha256 misstate what ran.
    /// </summary>
    public sealed class DiagnosticRuleSet
    {
        private DiagnosticRuleSet(RuleSetLoadReport report, RuleSetSource source) {
            var tables = report.Tables!;
            Elements = tables.Elements;
            var files = new List<RuleSetFile>();
            foreach (var kv in report.FileHashes) {
                files.Add(new RuleSetFile(kv.Key, kv.Value));
            }
            Identity = new RuleSetIdentity(report.RuleSetVersion!, report.RuleSetSha256!, source, report.PinChecked,
                                           new ReadOnlyCollection<RuleSetFile>(files));
            Adducts = new ReadOnlyCollection<AdductRow>(new List<AdductRow>(tables.Adducts));
            Units = new ReadOnlyCollection<UnitRow>(new List<UnitRow>(tables.Units));
            var motifs = new List<MotifRow>();
            foreach (var id in tables.MotifOrder) {
                motifs.Add(new MotifRow(tables.Motifs[id]));
            }
            Motifs = new ReadOnlyCollection<MotifRow>(motifs);
            var nesting = new List<MotifNesting>();
            foreach (var n in tables.Nesting) {
                nesting.Add(new MotifNesting(n));
            }
            Nesting = new ReadOnlyCollection<MotifNesting>(nesting);
            var lists = new List<string>(tables.Scaffolds.Keys);
            lists.Sort(StringComparer.Ordinal);
            var scaffolds = new List<ScaffoldList>();
            foreach (var id in lists) {
                scaffolds.Add(tables.Scaffolds[id]);
            }
            ScaffoldLists = new ReadOnlyCollection<ScaffoldList>(scaffolds);
            var rules = new List<Rule>();
            foreach (var kv in report.Rules) {
                rules.Add(new Rule(kv.Value.Doc, Elements, new ReadOnlyCollection<AdductTry>(kv.Value.Tried), report.SemanticSha256(kv.Key)));
            }
            Rules = new ReadOnlyCollection<Rule>(rules);
        }

        public RuleSetIdentity Identity { get; }

        /// <summary>Rules in ordinal rule_id order.</summary>
        public IReadOnlyList<Rule> Rules { get; }

        public ElementTable Elements { get; }

        /// <summary>tables/adducts.json in file order.</summary>
        public IReadOnlyList<AdductRow> Adducts { get; }

        /// <summary>tables/units.json in file order, which is the canonical unit order.</summary>
        public IReadOnlyList<UnitRow> Units { get; }

        public IReadOnlyList<MotifRow> Motifs { get; }

        public IReadOnlyList<MotifNesting> Nesting { get; }

        /// <summary>Scaffold lists in ordinal list_id order.</summary>
        public IReadOnlyList<ScaffoldList> ScaffoldLists { get; }

        /// <summary>The rule set shipped in this assembly; its version must match a ruleset-history.tsv row.</summary>
        public static DiagnosticRuleSet LoadEmbedded() => LoadFiles(DiagnosticRuleSetLoader.ReadEmbedded(), RuleSetSource.Embedded);

        /// <summary>A rule-set directory (rules/, scaffolds/, tables/, ruleset.json), loaded unpinned.</summary>
        public static DiagnosticRuleSet LoadDirectory(string root) => LoadFiles(DiagnosticRuleSetLoader.ReadDirectory(root), RuleSetSource.Directory);

        /// <summary>Files given as {path relative to RuleSet/ with '/': bytes}. Only <see cref="RuleSetSource.Embedded"/>
        /// checks the version pin.</summary>
        internal static DiagnosticRuleSet LoadFiles(IReadOnlyDictionary<string, byte[]> files, RuleSetSource source) {
            var report = DiagnosticRuleSetLoader.Validate(files, source == RuleSetSource.Embedded);
            if (report.Errors.Count > 0) {
                throw new DiagnosticRuleSetException(new ReadOnlyCollection<RuleLoadError>(report.Errors));
            }
            return new DiagnosticRuleSet(report, source);
        }
    }
}

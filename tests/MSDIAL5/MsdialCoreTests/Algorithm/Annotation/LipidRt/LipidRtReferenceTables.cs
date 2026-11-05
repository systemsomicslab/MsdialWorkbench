using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt.Tests
{
    /// <summary>
    /// Reads the evaluation tables of the lipid RT check's Python reference: the harness spot tables
    /// (&lt;dataset&gt;.tsv, .context.tsv, .meta.json) and the reference outcome files.
    /// </summary>
    /// <remarks>
    /// The files live outside the repository; <see cref="EnvironmentVariable"/> names a folder that holds them, either
    /// directly or in the subfolders "tables" and "parity". Tests that need them are skipped when it is not set.
    /// </remarks>
    internal static class LipidRtReferenceTables
    {
        public const string EnvironmentVariable = "MSDIAL_LIPIDRT_REFERENCE_DIR";

        public static string Root => Environment.GetEnvironmentVariable(EnvironmentVariable);

        public static bool IsAvailable => !string.IsNullOrEmpty(Root) && Directory.Exists(Root);

        private static readonly string[] SubFolders = { string.Empty, "tables", "parity", Path.Combine("harness", "tables"), "final", Path.Combine("final", "parity") };

        public static string Locate(string fileName) {
            foreach (var sub in SubFolders) {
                var path = Path.Combine(Root, sub, fileName);
                if (File.Exists(path)) {
                    return path;
                }
            }
            return null;
        }

        public static IEnumerable<string> TableDatasets(Func<string, bool> predicate) {
            var folder = new[] { Path.Combine(Root, "tables"), Path.Combine(Root, "harness", "tables"), Root }.First(Directory.Exists);
            return Directory.GetFiles(folder, "*.meta.json")
                .Select(p => Path.GetFileName(p).Substring(0, Path.GetFileName(p).Length - ".meta.json".Length))
                .Where(predicate)
                .OrderBy(x => x, StringComparer.Ordinal);
        }

        /// <summary>Tab-separated with Python csv quoting (a field holding a quote is quoted, quotes doubled).</summary>
        public static List<Dictionary<string, string>> ReadTsv(string path) {
            var rows = new List<Dictionary<string, string>>();
            using (var reader = new StreamReader(path, Encoding.UTF8)) {
                var header = SplitLine(reader.ReadLine());
                string line;
                while ((line = reader.ReadLine()) != null) {
                    if (line.Length == 0) {
                        continue;
                    }
                    var fields = SplitLine(line);
                    var row = new Dictionary<string, string>(StringComparer.Ordinal);
                    for (int i = 0; i < header.Count; i++) {
                        row[header[i]] = i < fields.Count ? fields[i] : string.Empty;
                    }
                    rows.Add(row);
                }
            }
            return rows;
        }

        private static List<string> SplitLine(string line) {
            var fields = new List<string>();
            var builder = new StringBuilder();
            var i = 0;
            while (true) {
                builder.Clear();
                if (i < line.Length && line[i] == '"') {
                    i++;
                    while (i < line.Length) {
                        if (line[i] == '"') {
                            if (i + 1 < line.Length && line[i + 1] == '"') {
                                builder.Append('"');
                                i += 2;
                                continue;
                            }
                            i++;
                            break;
                        }
                        builder.Append(line[i++]);
                    }
                    while (i < line.Length && line[i] != '\t') {
                        builder.Append(line[i++]);
                    }
                }
                else {
                    while (i < line.Length && line[i] != '\t') {
                        builder.Append(line[i++]);
                    }
                }
                fields.Add(builder.ToString());
                if (i >= line.Length) {
                    break;
                }
                i++; // the tab
            }
            return fields;
        }

        public static double ParseDouble(string text) {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : double.NaN;
        }

        public static int ParseInt(string text) => string.IsNullOrEmpty(text) ? 0 : int.Parse(text, CultureInfo.InvariantCulture);

        public static bool ParseBool(string text) => text == "1";

        public sealed class Meta
        {
            public double RtMin;
            public double RtMax;
            public IonMode Polarity;
            public string Scope;
        }

        public static Meta ReadMeta(string dataset) {
            using (var document = JsonDocument.Parse(File.ReadAllText(Locate(dataset + ".meta.json")))) {
                var root = document.RootElement;
                double Number(string key) => root.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : double.NaN;
                return new Meta
                {
                    RtMin = Number("rt_min"),
                    RtMax = Number("rt_max"),
                    Polarity = root.GetProperty("polarity").GetString() == "neg" ? IonMode.Negative : IonMode.Positive,
                    Scope = root.TryGetProperty("scope", out var s) ? s.GetString() : string.Empty,
                };
            }
        }

        /// <summary>The integer key of a harness spot id: "A211" is 211, "S3:108" is 108.</summary>
        public static int IdOf(string spotId) {
            var colon = spotId.IndexOf(':');
            return int.Parse(colon >= 0 ? spotId.Substring(colon + 1) : spotId.Substring(1), CultureInfo.InvariantCulture);
        }

        public static LipidRtTagLevel TagLevelOf(string token) {
            switch (token) {
                case "chain": return LipidRtTagLevel.Chain;
                case "class": return LipidRtTagLevel.Class;
                case "msms": return LipidRtTagLevel.Msms;
                default: return LipidRtTagLevel.None;
            }
        }

        public static bool IsMsmsTag(string tag) => tag.Length > 0 && (tag[0] == '3' || tag[0] == '4');

        /// <summary>A candidate exactly as the Python reference saw the row: composition and chains from the table.</summary>
        public static LipidRtCandidate CandidateOf(Dictionary<string, string> row) {
            var lipid = ParseBool(row["lipid"]) && ParseBool(row["parsed"]) && ParseInt(row["C"]) > 0;
            return new LipidRtCandidate
            {
                Id = ParseInt(row["row_id"]),
                Rt = ParseDouble(row["rt"]),
                Mz = ParseDouble(row["mz"]),
                Height = ParseDouble(row["height"]),
                Adduct = row["adduct"],
                Name = row["name"],
                Molecular = row["molecular"],
                Ontology = row["ontology"],
                ClassKey = row["class_key"],
                Carbon = ParseInt(row["C"]),
                DoubleBondEquivalent = ParseInt(row["DBe"]),
                Oxygen = ParseInt(row["O"]),
                IsLipid = lipid,
                IsLabelled = ParseBool(row["labelled"]),
                IsMsmsAnnotated = IsMsmsTag(row["tag"]),
                TagLevel = TagLevelOf(row["tag_level"]),
                WeightedDot = ParseDouble(row["weighted_dp"]),
                Chains = LipidRtChain.ParseList(row["chains"]),
                IsEligible = ParseBool(row["eligible"]),
                IneligibleReason = row["elig_reason"],
            };
        }

        public static LipidRtContextPeak ContextOf(Dictionary<string, string> row) {
            return new LipidRtContextPeak
            {
                Id = IdOf(row["spot_id"]),
                Rt = ParseDouble(row["rt"]),
                Mz = ParseDouble(row["mz"]),
                Height = ParseDouble(row["height"]),
                Adduct = row["adduct"],
                IsMsmsAnnotated = IsMsmsTag(row["tag"]),
            };
        }

        /// <summary>One unit of a dataset as the reference ran it: the unit's spots and context, the dataset's RT range.</summary>
        public sealed class Unit
        {
            public string Dataset;
            public string Name;
            public LipidRtRunInput Input;
            public Dictionary<int, string> SpotIdById;
        }

        public static List<Unit> LoadUnits(string dataset) {
            var meta = ReadMeta(dataset);
            var spots = ReadTsv(Locate(dataset + ".tsv"));
            var context = ReadTsv(Locate(dataset + ".context.tsv"));
            var units = new List<Unit>();
            foreach (var group in spots.GroupBy(r => r["unit"]).OrderBy(g => g.Key, StringComparer.Ordinal)) {
                var unitName = group.Key;
                var candidates = group.Select(CandidateOf).ToList();
                var peaks = context.Where(c => !c.TryGetValue("unit", out var u) || u.Length == 0 || u == unitName).Select(ContextOf).ToList();
                units.Add(new Unit
                {
                    Dataset = dataset,
                    Name = unitName,
                    Input = new LipidRtRunInput(candidates, peaks, meta.RtMin, meta.RtMax, meta.Polarity),
                    SpotIdById = group.ToDictionary(r => ParseInt(r["row_id"]), r => r["spot_id"]),
                });
            }
            return units;
        }

        public static LipidRtReason ReasonOf(string code) {
            switch (code) {
                case "conf_rt": return LipidRtReason.ConfirmedRt;
                case "conf_xpol_promoted": return LipidRtReason.ConfirmedPromotedByPolarity;
                case "ne_ineligible": return LipidRtReason.Ineligible;
                case "ne_gate": return LipidRtReason.Gate;
                case "ne_classgate": return LipidRtReason.ClassGate;
                case "ne_ambiguous": return LipidRtReason.Ambiguous;
                case "ne_support": return LipidRtReason.Support;
                case "ne_tie": return LipidRtReason.Tie;
                case "ne_artefact": return LipidRtReason.Artefact;
                case "ne_magnitude": return LipidRtReason.Magnitude;
                case "ne_margin": return LipidRtReason.Margin;
                case "ne_smalldeficit": return LipidRtReason.SmallDeficit;
                case "ne_ox_noflag": return LipidRtReason.OxNoFlag;
                case "ne_tail_noflag": return LipidRtReason.TailNoFlag;
                case "ne_policy_class": return LipidRtReason.PolicyClass;
                case "ne_policy_secondary": return LipidRtReason.PolicySecondary;
                case "ne_minordup": return LipidRtReason.MinorDuplicate;
                case "ne_plaus_soft": return LipidRtReason.PlausibilitySoft;
                case "ne_uncorroborated": return LipidRtReason.Uncorroborated;
                case "ne_xpol_conflict": return LipidRtReason.PolarityConflict;
                case "inc_order": return LipidRtReason.InconsistentOrder;
                case "inc_plaus": return LipidRtReason.InconsistentPlausibility;
                default: throw new FormatException($"Unknown reference code '{code}'");
            }
        }

        public static LipidRtOutcome OutcomeOf(string outcome) {
            switch (outcome) {
                case "Confirmed": return LipidRtOutcome.Confirmed;
                case "NotEvaluable": return LipidRtOutcome.NotEvaluable;
                case "Inconsistent": return LipidRtOutcome.Inconsistent;
                default: throw new FormatException($"Unknown reference outcome '{outcome}'");
            }
        }

        public static LipidRtFeatureStatus StatusOf(string status) {
            switch (status) {
                case "free": return LipidRtFeatureStatus.Free;
                case "core": return LipidRtFeatureStatus.Core;
                case "ambiguous": return LipidRtFeatureStatus.Ambiguous;
                case "excluded": return LipidRtFeatureStatus.Excluded;
                default: throw new FormatException($"Unknown reference status '{status}'");
            }
        }
    }
}
